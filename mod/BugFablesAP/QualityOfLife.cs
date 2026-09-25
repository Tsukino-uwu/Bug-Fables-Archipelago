using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static ConfigEntry<bool> SkipCutscenes;
        // Items from other players: which ones get the hold-up animation (the user, 2026-09-25). Your own finds always do.
        // Default All (the user, once bursts were fast with the skip button held: fun and noticeable, not tedious).
        internal static readonly string[] ItemAnimations = { "All", "Progression", "Off" };
        internal static ConfigEntry<string> ItemAnimation;
        // Shop prices (the user, 2026-09-25): Normal by default, Half or Free on request. The logic never counts on them.
        internal static readonly string[] ShopPriceValues = { "Normal", "Half", "Free" };
        internal static ConfigEntry<string> ShopPrices;

        // Skip cutscenes (the user, 2026-09-25: scenes and fluff that give no checks). Each scene is read in full first
        // (EventControl.EventN): one that only moves the camera and the party, talks, and sets flags is skipped by
        // setting those flags instead of starting it; one that also changes the world is run by the game itself at
        // speed, its lines answered, so it ends exactly as it would. A scene that gives an item, changes the party or
        // starts a battle may go too, as long as everything it gives can still be received (the user, 2026-09-25): the
        // mod then does what the scene leaves behind and keeps its checks (the opening, below).
        private sealed class Scene
        {
            internal string Map;
            internal int Event;
            internal int[] Flags; // null: fast-forward instead of skipping
        }

        private static readonly Scene[] Scenes =
        {
            // The bridge message: party and camera moves, three lines, flag 11 (EventControl.cs:274-333; its lines carry
            // no commands, ScriptDump). Flag 11 also hides its own trigger (BridgeMessage, limit 11).
            new Scene { Map = "SnakemouthBridgeRoom", Event = 0, Flags = new[] { 11 } },
            // Hitting the rope: moves the rope away, plays the bridge's Fall animation and fixes it fallen, then flags 7
            // and 11 (EventControl.cs:334-407). The bridge's end state is set by the scene itself, not by its flags, so
            // it's fast-forwarded.
            new Scene { Map = "SnakemouthBridgeRoom", Event = 1, Flags = null },
        };

        // The Metal Island boat's fares: the pier sailor's lines 16 (300 berries) and 19 (90), each
        // |checkmoney,N,20||money,-N| (ScriptDump's money column, 2026-09-25). The trip back charges nothing.
        private const string BoatMap = "BugariaPier";
        private static readonly int[] FareLines = { 16, 19 };
        private static readonly System.Text.RegularExpressions.Regex MoneyToken =
            new System.Text.RegularExpressions.Regex(@"\|(checkmoney|money),[^|]*\|", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static Harmony harmony;
        private static readonly MethodInfo endEvent = AccessTools.Method(typeof(EventControl), "EndEvent", Type.EmptyTypes);

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
            SkipCutscenes = config.Bind("QualityOfLife", "SkipCutscenes", true,
                "Scenes that give nothing are skipped or pass by fast (a list that grows scene by scene). The opening after the "
                + "slides (Maki's talk and the tutorial battle) is skipped too: Vi joins and the first check is sent, and you "
                + "can walk out of the building.");
            ItemAnimation = config.Bind("QualityOfLife", "ItemAnimation", "All", new ConfigDescription(
                "Which items received from other players are shown held up, as when you find one: Progression (items that "
                + "unlock something), All, or Off. They always arrive either way; your own finds are always shown.",
                new AcceptableValueList<string>(ItemAnimations)));
            ShopPrices = config.Bind("QualityOfLife", "ShopPrices", "Normal", new ConfigDescription(
                "Medal shop prices, in berries and crystal berries: Normal, Half or Free.",
                new AcceptableValueList<string>(ShopPriceValues)));
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
            harmony.Patch(getLine, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeGetLine)));
            MethodInfo startEvent = AccessTools.Method(typeof(EventControl), nameof(EventControl.StartEvent), new[] { typeof(int), typeof(NPCControl) });
            if (startEvent == null)
            {
                log.LogError("[qol] EventControl.StartEvent(int, NPCControl) not found: Skip cutscenes does nothing.");
                return;
            }
            harmony.Patch(startEvent, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeStartEvent)));
        }

        // The opening (the user, 2026-09-25: skip the scenes and the fight, "just start playing the game"). After the
        // slides you play Kabbu alone inside the starting building, and Event16's trigger (entity 9, hidden by flag 15)
        // stands between you and the door. Event16 (EventControl.cs:3538-3822) is Maki's talk, Vi joining, the tutorial
        // battle, the Explorer Permit (location 1's giveitem) and Kina's and Eetl's talk. The one scene given an item, a
        // battle and a party change on purpose: it never starts, and the mod leaves what it leaves behind, the game's
        // way: Vi and Kabbu in the party (ChangeParty, then SetPlayers), the tutorial's Crunchy Leaf in the bag, Vi's
        // stand-in (Beee) and the blockingbox gone, the exit (entity 2) back with the default camera, entity 11 at
        // animstate 0, flag 15 and quest 11 on the board. Flag 15 marks location 1 done, so LocationChecks sends it,
        // and the hold-up shows the seed's item there. Done on a later frame, outside the trigger that started it.
        private const string OpeningMap = "BugariaOutskirtsOutsideCity";
        private const int OpeningEvent = 16;
        private const long OpeningLocation = 7_720_001; // Outskirts: Maki and Eetl's Gift (apworld id 1)
        private static bool openingPending;

        private static void RunOpening()
        {
            MainManager mm = MainManager.instance;
            Vector3 at = MainManager.player.transform.position;
            MainManager.ChangeParty(new[] { 0, 1 }, true, true);
            mm.items[0].Add(0);
            foreach (string name in new[] { "Beee", "blockingbox" })
            {
                GameObject thing = GameObject.Find(name);
                if (thing != null)
                {
                    UnityEngine.Object.Destroy(thing);
                }
                else
                {
                    log.LogWarning($"[qol] opening: {name} not found");
                }
            }
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            EntityControl exit = MainManager.GetEntity(2);
            if (exit != null && exit.npcdata != null)
            {
                exit.gameObject.SetActive(true);
                exit.npcdata.vectordata[4] = MainManager.defaultcamoffset;
                exit.npcdata.vectordata[5] = MainManager.defaultcamangle;
            }
            EntityControl eleven = MainManager.GetEntity(11);
            if (eleven != null)
            {
                eleven.animstate = 0;
            }
            mm.flags[15] = true;
            mm.boardquests[1].Insert(0, 11);
            HoldUps.FoundAt(OpeningLocation, "the opening's gift (location 1)");
            log.LogInfo($"[qol] opening done without Event16: party {string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())}, "
                + $"characters {mm.playerdata.Count(p => p.entity != null)}, exit {(exit != null ? "active " + exit.gameObject.activeSelf : "NOT found")}, flag 15 {mm.flags[15]}");
        }

        // Every scene starts here (EventControl.cs:74). A listed scene to skip gets its flags and never starts.
        private static bool BeforeStartEvent(int id)
        {
            if (id == OpeningEvent && SkipCutscenes.Value && randomizerOn() && MainManager.map != null
                && MainManager.map.mapid.ToString() == OpeningMap && !MainManager.instance.flags[15])
            {
                openingPending = true;
                endEvent?.Invoke(null, null);
                log.LogInfo("[qol] Event16 (the opening) skipped: the mod does what it leaves behind on the next free frame");
                return false;
            }
            Scene scene = SceneFor(id);
            if (scene == null || scene.Flags == null || !SkipCutscenes.Value || !randomizerOn())
            {
                return true;
            }
            foreach (int flag in scene.Flags)
            {
                MainManager.instance.flags[flag] = true;
            }
            // Whatever starts a scene may have frozen the player first (a trigger sets minipause, NPCControl.cs:5512-5525),
            // and the scene's own end undoes it. A skipped scene never ends, which froze the user at the bridge
            // (2026-09-25): so end it the game's way, EndEvent(), all resets (EventControl.cs:146-187).
            endEvent?.Invoke(null, null);
            log.LogInfo($"[qol] skipped Event{id} on {scene.Map}: set flags {string.Join(", ", scene.Flags.Select(f => f.ToString()).ToArray())}, "
                + (endEvent != null ? "ended it the game's way" : "EndEvent NOT found: the player may stay frozen"));
            return false;
        }

        private static Scene SceneFor(int id)
        {
            string map = MainManager.map == null ? null : MainManager.map.mapid.ToString();
            return map == null ? null : Scenes.FirstOrDefault(s => s.Event == id && s.Map == map);
        }

        // A listed scene to fast-forward is running now.
        private static bool InFastScene()
        {
            if (!SkipCutscenes.Value || !MainManager.instance.inevent)
            {
                return false;
            }
            Scene scene = SceneFor(MainManager.lastevent);
            return scene != null && scene.Flags == null;
        }

        // A hold-up's Giveitem shows its follow-up line (GetDialogueText(redirect), MainManager.cs:11592); the mod's own
        // hold-ups ask for ItemSwap.EmptyLine, answered here with the game's |end| (sets end, which skips the final wait
        // for a press, MainManager.cs:11909-11910, :14171). An empty answer left an empty box waiting (the user,
        // 2026-09-25). Any other number is the game's (a negative one reads commondialogue, MainManager.cs:10186).
        private static bool BeforeGetLine(int id, ref string __result)
        {
            if (id != ItemSwap.EmptyLine)
            {
                return true;
            }
            __result = "|end|";
            return false;
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
            if (openingPending && MainManager.player != null && !mm.inevent && !mm.message && MainManager.battle == null)
            {
                openingPending = false;
                RunOpening();
            }
            bool slides = on && ((SkipIntro.Value && InIntroSlides()) || InFastScene());
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
                    log.LogInfo($"[qol] Event{MainManager.lastevent}: passing it by at speed");
                }
                Time.timeScale = IntroSpeed;
            }
            else if (speeding)
            {
                speeding = false;
                Time.timeScale = 1f;
                log.LogInfo("[qol] scene over: normal speed");
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
