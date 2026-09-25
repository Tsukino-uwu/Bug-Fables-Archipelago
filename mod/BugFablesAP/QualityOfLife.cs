using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The panel's "Quality of life" page (the user, 2026-09-25: a sub-menu of on/off rows that speed the game up and
    // make it smoother). The rows never change what the game gives or where: no flag, item or party is touched here.
    // Free boat waives a fare the player could always earn by battling, so the logic never counts on it. Like every
    // panel setting, nothing happens while the Archipelago mod is disabled. Every row is on by default (the user,
    // 2026-09-25).
    internal static class QualityOfLife
    {
        internal static ConfigEntry<bool> FastText;
        internal static ConfigEntry<bool> SkipIntro;
        internal static ConfigEntry<bool> FreeBoat;
        internal static ConfigEntry<bool> WarpButton;

        // The Metal Island boat's fares: the pier sailor's lines 16 (300 berries) and 19 (90), each
        // |checkmoney,N,20||money,-N| (ScriptDump's money column, 2026-09-25). The trip back charges nothing.
        private const string BoatMap = "BugariaPier";
        private static readonly int[] FareLines = { 16, 19 };
        private static readonly System.Text.RegularExpressions.Regex MoneyToken =
            new System.Text.RegularExpressions.Regex(@"\|(checkmoney|money),[^|]*\|", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static Harmony harmony;

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
            FastText = config.Bind("QualityOfLife", "FastText", true,
                "Dialogue text is instant instead of letter by letter, as if the skip button were held (the game's own "
                + "skip), but still requires a button press to proceed. Holding the skip button also moves through boxes "
                + "much faster than the game's own hold. Lines the game marks unskippable stay as they are.");
            SkipIntro = config.Bind("QualityOfLife", "SkipIntro", true,
                "A new game's four story slides pass by on their own, fast. The rest of the opening plays as normal.");
            FreeBoat = config.Bind("QualityOfLife", "FreeBoat", true,
                "The boat to Metal Island costs nothing (the user, 2026-09-25: no farming berries in Archipelago).");
            WarpButton = config.Bind("QualityOfLife", "WarpButton", true,
                "A fifth button in the pause menu, Warp to Start, takes the party back to where the game began (after a "
                + "Yes / No box). Not shown in battle.");
            // A fare line is fetched inside the running dialogue (a prompt's answer jumps to it), not through a new SetText,
            // so the line itself is changed as the game reads it (MainManager.GetDialogueText, MainManager.cs:10169).
            MethodInfo getLine = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetDialogueText), new[] { typeof(int) });
            if (getLine == null)
            {
                log.LogError("[qol] MainManager.GetDialogueText(int) not found: Free boat does nothing, the fare stays.");
                return;
            }
            harmony = new Harmony(Plugin.Guid + ".qol." + DateTime.UtcNow.Ticks);
            harmony.Patch(getLine, postfix: new HarmonyMethod(typeof(QualityOfLife), nameof(AfterGetLine)));
        }

        private static void AfterGetLine(int id, ref string __result)
        {
            if (__result == null || Array.IndexOf(FareLines, id) < 0 || MainManager.map == null
                || MainManager.map.mapid.ToString() != BoatMap || !randomizerOn() || !FreeBoat.Value)
            {
                return;
            }
            string free = MoneyToken.Replace(__result, "");
            if (free != __result)
            {
                __result = free;
                log.LogInfo($"[qol] boat fare waived (pier line {id})");
            }
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

            bool skippable = on && Skippable(mm);
            if (skippable && FastText.Value && !mm.waitinput)
            {
                mm.skiptext = true;
            }
            // Holding the skip button advances a box, then waits out inputcooldown (16 after a box, 10 when a new
            // dialogue opens; MainManager.cs:5147, :10738), counted down one per frame (:7298). With the text already
            // instant that wait is all that's left (the user, 2026-09-25: holding didn't feel faster), so while it's
            // held the wait is cut short, as part of Fast text (the user found it faster and folded it in, 2026-09-25).
            // The game's own hold branch still does the advancing.
            if (skippable && FastText.Value && MainManager.GetKey(5, hold: true) && mm.inputcooldown > HeldCooldown)
            {
                mm.inputcooldown = HeldCooldown;
            }
        }

        // Frames between boxes while the skip button is held with Fast text on: the game's own 4 while a box is
        // still typing (MainManager.cs:5151), instead of 16 between boxes.
        private const float HeldCooldown = 4f;

        // The same conditions under which holding the skip button works (MainManager.cs:5125-5140): a dialogue box
        // is open, not a prompt or list, not marked |noskip|, and on the newest line rather than one looked back at.
        private static bool Skippable(MainManager mm)
        {
            if (!mm.message || mm.prompt || mm.itemlist != null || mm.inlist || MainManager.noskip)
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
            harmony?.UnpatchSelf();
            harmony = null;
            if (speeding)
            {
                speeding = false;
                Time.timeScale = 1f;
            }
        }
    }
}
