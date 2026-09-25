using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // A fifth pause-menu button, "Warp to start", behind a Yes / No box: the game's own map transfer to where a new
    // game begins. The logic never counts on it. Confirm on it is caught before the game would open a "window 5".
    internal static class WarpButton
    {
        private static ManualLogSource log;
        private static Func<bool> on;
        private static Harmony harmony;

        private static readonly FieldInfo optionField = AccessTools.Field(typeof(PauseMenu), "option");
        private static readonly FieldInfo maxField = AccessTools.Field(typeof(PauseMenu), "maxoptions");
        private static readonly FieldInfo spritesField = AccessTools.Field(typeof(PauseMenu), "sprites");
        private static readonly FieldInfo boxesField = AccessTools.Field(typeof(PauseMenu), "boxes");
        private static readonly MethodInfo prepareExit = AccessTools.Method(typeof(PauseMenu), "PrepareExit");

        private const int Button = 4;
        private const int IconSprite = 34;
        // By its save point: entity 1 (SaveTutorial) before flag 41, entity 22 (SaveAfterTutorial) after.
        private const MainManager.Maps StartMap = MainManager.Maps.BugariaOutskirtsOutsideCity;

        private static SpriteRenderer icon;
        private static PauseMenu builtFor;
        private static Transform confirmBox;
        private static SpriteRenderer leaf;
        private static bool yes;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> enabled)
        {
            log = logger;
            on = enabled;
            MethodInfo update = AccessTools.Method(typeof(PauseMenu), "Update");
            MethodInfo updateText = AccessTools.Method(typeof(PauseMenu), "UpdateText");
            if (update == null || updateText == null || optionField == null || maxField == null || spritesField == null
                || boxesField == null || prepareExit == null)
            {
                log.LogError("[warp] NOT installed: PauseMenu's Update, UpdateText or fields weren't found; no warp button.");
                return;
            }
            harmony = new Harmony(guid + ".warp." + DateTime.UtcNow.Ticks);
            harmony.Patch(update, prefix: new HarmonyMethod(typeof(WarpButton), nameof(BeforeUpdate)));
            harmony.Patch(updateText, postfix: new HarmonyMethod(typeof(WarpButton), nameof(AfterUpdateText)));
            // Window 0 hands IconAnim four icons and it indexes them by option: hand it five.
            MethodInfo iconAnim = AccessTools.Method(typeof(PauseMenu), "IconAnim");
            if (iconAnim != null)
            {
                harmony.Patch(iconAnim, prefix: new HarmonyMethod(typeof(WarpButton), nameof(BeforeIconAnim)));
            }
            log.LogInfo("[warp] installed on PauseMenu.Update and UpdateText");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
            // Take this instance's icon and box with it, or a hot reload leaves the old icon behind.
            CloseConfirm();
            if (icon != null)
            {
                UnityEngine.Object.Destroy(icon.gameObject);
                icon = null;
            }
            builtFor = null;
        }

        private static bool BeforeUpdate(PauseMenu __instance)
        {
            if (__instance.windowid != 0 || MainManager.battle != null || !on())
            {
                CloseConfirm();
                return true;
            }
            SpriteRenderer[] sprites = (SpriteRenderer[])spritesField.GetValue(__instance);
            if (!ReferenceEquals(builtFor, __instance) || icon == null)
            {
                // Window 0 builds over a few frames. Coming back from another page, sprites is briefly that page's shorter array.
                if (sprites == null || sprites.Length <= 13 + Button || sprites[16] == null)
                {
                    return true;
                }
                AddButton(__instance, sprites);
            }
            int option = (int)optionField.GetValue(__instance);
            if (confirmBox != null)
            {
                Confirm(__instance);
                return false;
            }
            if (option == Button && MainManager.instance.inputcooldown <= 0f && MainManager.GetKey(4, hold: false))
            {
                MainManager.PlaySound("Confirm", 10);
                OpenConfirm(__instance);
                return false;
            }
            return true;
        }

        private static void BeforeIconAnim(PauseMenu __instance, ref int[] values)
        {
            if (__instance.windowid == 0 && icon != null && ReferenceEquals(builtFor, __instance) && values != null && values.Length == 4
                && values[0] == 13)
            {
                values = new[] { 13, 14, 15, 16, 13 + Button };
            }
        }

        private static void AddButton(PauseMenu menu, SpriteRenderer[] sprites)
        {
            builtFor = menu;
            // Five across: -4..4 two apart, inside the 11-wide box (the game's four sit at -3..3).
            for (int n = 0; n < 4; n++)
            {
                sprites[13 + n].transform.localPosition = new Vector3(-4f + 2f * n, 3f);
            }
            // Made as BuildWindow makes the other four, so IconAnim outlines and wiggles it like them.
            icon = MainManager.NewUIObject("menuicon" + Button, sprites[16].transform.parent, new Vector3(4f, 3f), Vector3.one,
                MainManager.guisprites[IconSprite]).GetComponent<SpriteRenderer>();
            sprites[13 + Button] = icon;
            maxField.SetValue(menu, 5);
        }

        private static void AfterUpdateText(PauseMenu __instance)
        {
            if (__instance.windowid != 0 || icon == null || !ReferenceEquals(builtFor, __instance)
                || (int)optionField.GetValue(__instance) != Button)
            {
                return;
            }
            // The game just wrote this option's labels: replace them.
            Transform labels = ((DialogueAnim[])boxesField.GetValue(__instance))[0].transform;
            MainManager.DestroyText(labels);
            __instance.StartCoroutine(MainManager.SetText("|single|Go back to where the game started.", 0, 99999f, false, false,
                new Vector3(-5f, 0.1f), Vector3.zero, Vector2.one, labels, null));
            __instance.StartCoroutine(MainManager.SetText("|center||single|Warp", 0, 99999f, false, false,
                new Vector3(0f, 7.5f), Vector3.zero, Vector2.one, labels, null));
        }

        private static void OpenConfirm(PauseMenu menu)
        {
            yes = false;
            confirmBox = MainManager.Create9Box(new Vector3(0f, 0f, 5f), new Vector2(7f, 3f), 1, 30, Color.white, grow: false);
            confirmBox.parent = menu.transform;
            confirmBox.localPosition = new Vector3(0f, -0.5f, -1f);
            DrawConfirm(menu);
            MainManager.instance.inputcooldown = 10f;
        }

        private static void DrawConfirm(PauseMenu menu)
        {
            // Yes and No at fixed spots so they don't shift when switching; the chosen one coloured, the leaf beside it.
            MainManager.DestroyText(confirmBox);
            menu.StartCoroutine(MainManager.SetText("|center||sort,40|Warp to the start?", 0, 99999f, false, false,
                new Vector3(0f, 0.6f), Vector3.zero, Vector2.one * 0.8f, confirmBox, null));
            menu.StartCoroutine(MainManager.SetText("|center||sort,40|" + (yes ? "|color,1|" : "") + "Yes", 0, 99999f, false, false,
                new Vector3(-1.5f, -0.6f), Vector3.zero, Vector2.one * 0.8f, confirmBox, null));
            menu.StartCoroutine(MainManager.SetText("|center||sort,40|" + (yes ? "" : "|color,1|") + "No", 0, 99999f, false, false,
                new Vector3(1.5f, -0.6f), Vector3.zero, Vector2.one * 0.8f, confirmBox, null));
            if (leaf == null)
            {
                // The game's menu cursor, set up as it sets up its own.
                leaf = new GameObject("warpleaf").AddComponent<SpriteRenderer>();
                leaf.sprite = MainManager.cursorsprite[0];
                leaf.sortingOrder = 41;
                leaf.gameObject.layer = 5;
                leaf.transform.parent = confirmBox;
                leaf.transform.localEulerAngles = Vector3.zero;
                leaf.transform.localScale = Vector3.one;
                leaf.gameObject.AddComponent<SpriteBounce>().MessageBounce();
            }
            leaf.transform.localPosition = new Vector3(yes ? -2.4f : 0.9f, -0.45f, 0f);
        }

        private static void Confirm(PauseMenu menu)
        {
            if (MainManager.instance.inputcooldown > 0f)
            {
                return;
            }
            if (MainManager.GetKey(2, hold: false) || MainManager.GetKey(3, hold: false))
            {
                yes = !yes;
                MainManager.PlayScrollSound();
                DrawConfirm(menu);
            }
            else if (MainManager.GetKey(5, hold: false))
            {
                MainManager.PlaySound("Cancel", 10);
                CloseConfirm();
                MainManager.instance.inputcooldown = 10f;
            }
            else if (MainManager.GetKey(4, hold: false))
            {
                if (!yes)
                {
                    MainManager.PlaySound("Cancel", 10);
                    CloseConfirm();
                    MainManager.instance.inputcooldown = 10f;
                    return;
                }
                MainManager.PlaySound("Confirm", 10);
                CloseConfirm();
                log.LogInfo($"[warp] warp to start chosen on {MainManager.map?.mapid}");
                prepareExit.Invoke(menu, null);
                MainManager.instance.StartCoroutine(WarpWhenUnpaused());
            }
        }

        private static void CloseConfirm()
        {
            if (confirmBox != null)
            {
                UnityEngine.Object.Destroy(confirmBox.gameObject);
                confirmBox = null;
                leaf = null;
            }
        }

        private static IEnumerator WarpWhenUnpaused()
        {
            // PrepareExit shrinks the boxes; DestroyPause follows 0.25 s later and clears pause.
            float since = Time.realtimeSinceStartup;
            while ((MainManager.instance.pause || MainManager.pausemenu != null) && Time.realtimeSinceStartup - since < 3f)
            {
                yield return null;
            }
            if (MainManager.player == null || MainManager.instance.inevent || MainManager.instance.message || MainManager.battle != null)
            {
                log.LogWarning("[warp] not now: an event, dialogue or battle started; nothing done");
                yield break;
            }
            Vector3 target = SavePointSpot();
            log.LogInfo($"[warp] to {StartMap} at {target}");
            yield return MainManager.TransferMap((int)StartMap, target);
        }

        // Entity table fields 6-8, then a step toward the camera so the party lands beside the save point, not in it.
        private static Vector3 SavePointSpot()
        {
            int entity = MainManager.instance.flags[41] ? 22 : 1;
            TextAsset data = Resources.Load<TextAsset>("Data/EntityData/" + (int)StartMap);
            string[] lines = data == null ? new string[0] : data.ToString().Split('\n');
            if (entity < lines.Length)
            {
                string[] f = lines[entity].Split('}');
                if (f.Length > 8)
                {
                    try
                    {
                        return new Vector3(Convert.ToSingle(f[6]), Convert.ToSingle(f[7]) + 0.5f, Convert.ToSingle(f[8]) - 2f);
                    }
                    catch (FormatException)
                    {
                    }
                }
            }
            log.LogWarning("[warp] the save point's spot wasn't found; landing at the map's origin");
            return Vector3.zero;
        }
    }
}
