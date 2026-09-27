using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // The Graphics page's Render scale (supersampling) and Anti-aliasing (MSAA) rows (mod guide, step 28).
    // Render scale borrows the game's own render-scale path (MainManager.SetRenderTexture): the world and 3DGUI cameras
    // draw into a texture, which the GUI camera's quad shows on screen; here the texture is larger than the screen.
    internal static class RenderQuality
    {
        internal static readonly string[] Scales = { "100", "150", "200" };
        internal static readonly string[] AntiAliasing = { "Off", "2x", "4x", "8x" };
        internal static ConfigEntry<string> Scale;
        internal static ConfigEntry<string> Msaa;

        private static Func<bool> settingsOn;
        private static ManualLogSource log;
        private static int vanillaMsaa = -1;
        private static RenderTexture ours;
        private static string lastDecision;

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> on)
        {
            log = logger;
            settingsOn = on;
            Scale = config.Bind("Graphics", "RenderScale", "100", new ConfigDescription(
                "Supersampling: the world is drawn at this percent of your resolution and shrunk to fit, for smoother edges. "
                + "100 is the game's own. The menus and text stay at your resolution. Switch it on the Graphics page.",
                new AcceptableValueList<string>(Scales)));
            Msaa = config.Bind("Graphics", "AntiAliasing", "Off", new ConfigDescription(
                "Multisample anti-aliasing (MSAA) on the 3D edges: Off (the game's own), 2x, 4x or 8x. Switch it on the Graphics page.",
                new AcceptableValueList<string>(AntiAliasing)));
            vanillaMsaa = QualitySettings.antiAliasing;
            log.LogInfo($"[gfx] ready (the game's MSAA {vanillaMsaa})");
        }

        internal static void Disable()
        {
            StandDown("the plugin unloaded");
            if (vanillaMsaa >= 0)
            {
                QualitySettings.antiAliasing = vanillaMsaa;
            }
        }

        private static bool On => settingsOn != null && settingsOn();

        private static int WantedMsaa()
        {
            if (!On || Msaa == null)
            {
                return vanillaMsaa;
            }
            switch (Msaa.Value)
            {
                case "2x": return 2;
                case "4x": return 4;
                case "8x": return 8;
                default: return vanillaMsaa;
            }
        }

        private static float WantedScale() => On && Scale != null && int.TryParse(Scale.Value, out int percent) ? percent / 100f : 1f;

        internal static void Tick()
        {
            if (vanillaMsaa < 0 || MainManager.MainCamera == null || MainManager.GUICamera == null)
            {
                return;
            }
            int msaa = WantedMsaa();
            if (QualitySettings.antiAliasing != msaa)
            {
                QualitySettings.antiAliasing = msaa;
                log.LogInfo($"[gfx] MSAA {msaa}");
            }
            float scale = WantedScale();
            // The game's own render scale below 100% (and its minigames) own the texture; ours steps aside.
            if (scale <= 1f || MainManager.downsample != 0)
            {
                StandDown(scale <= 1f ? "render scale 100%" : "the game's own render scale is in use");
                return;
            }
            int width = Mathf.RoundToInt(Screen.width * scale), height = Mathf.RoundToInt(Screen.height * scale);
            int samples = Mathf.Max(1, msaa);
            if (ours != null && MainManager.MainCamera.targetTexture == ours && ours.width == width && ours.height == height
                && ours.antiAliasing == samples)
            {
                return;
            }
            Apply(width, height, samples);
        }

        private static void Apply(int width, int height, int samples)
        {
            Camera ui3d = MainManager.MainCamera.transform.childCount > 1 ? MainManager.MainCamera.transform.GetChild(1).GetComponent<Camera>() : null;
            Transform quad = MainManager.GUICamera.transform.childCount > 0 ? MainManager.GUICamera.transform.GetChild(0) : null;
            Renderer shown = quad != null ? quad.GetComponent<Renderer>() : null;
            if (ui3d == null || shown == null)
            {
                Decide($"not applied: the game's render texture objects weren't found (3DGUI camera {ui3d != null}, screen quad {shown != null})");
                return;
            }
            var texture = new RenderTexture(width, height, 24) { antiAliasing = samples, filterMode = FilterMode.Bilinear };
            quad.gameObject.SetActive(true);
            MainManager.MainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            MainManager.MainCamera.targetTexture = texture;
            ui3d.targetTexture = texture;
            shown.material.mainTexture = texture;
            shown.material.mainTextureScale = Vector2.one;
            RenderTexture old = ours;
            ours = texture;
            if (old != null)
            {
                old.Release();
            }
            lastDecision = null;
            log.LogInfo($"[gfx] rendering at {width}x{height} (screen {Screen.width}x{Screen.height}, MSAA {samples})");
        }

        // Back to the screen through the game's own SetRenderTexture(0), only while the texture on the camera is ours.
        private static void StandDown(string why)
        {
            if (ours == null)
            {
                return;
            }
            if (MainManager.MainCamera != null && MainManager.MainCamera.targetTexture == ours && MainManager.downsample == 0)
            {
                MainManager.SetRenderTexture(0);
            }
            ours.Release();
            ours = null;
            Decide("back to the screen: " + why);
        }

        private static void Decide(string decision)
        {
            if (decision != lastDecision)
            {
                lastDecision = decision;
                log.LogInfo("[gfx] " + decision);
            }
        }
    }
}
