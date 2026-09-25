using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The panel's "Quality of life" page (the user, 2026-09-25: a sub-menu of on/off rows to speed the game up).
    // Each row only changes how fast things play out, never what the game gives or where: no flag, item or party is
    // touched here. Like every panel setting, nothing happens while the Archipelago mod is disabled.
    internal static class QualityOfLife
    {
        internal static ConfigEntry<bool> FastText;
        internal static ConfigEntry<bool> SkipIntro;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        // private static in MainManager (MainManager.cs:2749, :2799): the line being shown, and the lines so far.
        private static readonly FieldInfo currentDialogue = AccessTools.Field(typeof(MainManager), "currentdialogue");
        private static readonly FieldInfo diagString = AccessTools.Field(typeof(MainManager), "diagstring");
        private static bool speeding;

        // Game speed while the intro slides run: their fades are per-frame lerps scaled by Time.smoothDeltaTime
        // (MainManager.TieFramerate, MainManager.cs:9567), and the game itself speeds up cooking with timeScale 2.5
        // (MainManager.cs:5546). EndEvent puts it back to 1 (EventControl.cs:184).
        private const float IntroSpeed = 8f;

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            FastText = config.Bind("QualityOfLife", "FastText", false,
                "Dialogue appears at once instead of letter by letter, as if the skip button were held (the game's own "
                + "skip). Each box still waits for a press, and lines the game marks unskippable stay as they are.");
            SkipIntro = config.Bind("QualityOfLife", "SkipIntro", false,
                "A new game's four story slides pass by on their own, fast. The rest of the opening plays as normal.");
        }

        internal static void Tick()
        {
            MainManager mm = MainManager.instance;
            if (mm == null || !MainManager.basicload)
            {
                return;
            }
            bool on = randomizerOn();
            bool slides = on && SkipIntro.Value && InIntroSlides();
            if (slides)
            {
                // Each slide's line waits for a press at its end (MainManager.cs:14169-14174); answer it.
                if (mm.message)
                {
                    mm.waitinput = false;
                    mm.skiptext = true;
                }
                if (!speeding)
                {
                    speeding = true;
                    log.LogInfo("[qol] intro slides: passing them by");
                }
                Time.timeScale = IntroSpeed;
            }
            else if (speeding)
            {
                speeding = false;
                Time.timeScale = 1f;
                log.LogInfo("[qol] intro slides over: normal speed");
            }

            if (on && FastText.Value && TextTyping(mm))
            {
                mm.skiptext = true;
            }
        }

        // The same conditions under which holding the skip button works (MainManager.cs:5125-5140): a dialogue box
        // is open, not a prompt or list, not marked |noskip|, and on the newest line rather than one looked back at.
        private static bool TextTyping(MainManager mm)
        {
            if (!mm.message || mm.prompt || mm.itemlist != null || mm.inlist || MainManager.noskip || mm.waitinput)
            {
                return false;
            }
            var lines = diagString?.GetValue(null) as List<string>;
            return lines != null && currentDialogue != null && (int)currentDialogue.GetValue(null) == lines.Count;
        }

        // Event8's slides: a black "back" sprite under the GUI camera, made before the first slide and destroyed
        // after the last (EventControl.cs:2656-2735).
        private static bool InIntroSlides()
        {
            return MainManager.lastevent == 8 && MainManager.instance.inevent && MainManager.GUICamera != null
                && MainManager.GUICamera.transform.Find("back") != null;
        }

        // A hot reload mid-intro must not leave the game fast.
        internal static void Disable()
        {
            if (speeding)
            {
                speeding = false;
                Time.timeScale = 1f;
            }
        }
    }
}
