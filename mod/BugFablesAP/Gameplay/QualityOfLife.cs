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
    // The panel's "Quality of life" page: rows that speed the game up without changing what it gives or where.
    // The logic counts on none of them, except the Warp with Points of No Return. With the Archipelago mod disabled only
    // Use on normal saves turns them on.
    internal static partial class QualityOfLife
    {
        internal static ConfigEntry<bool> FastText;
        internal static readonly string[] TravelValues = { "Off", "Warp", "Map", "Both" };
        internal static ConfigEntry<string> Travel;
        // Always on with a random start, door shuffle, Shuffle Jump, ability items or Points of No Return: a way out the
        // logic never counts, except with Points of No Return, where it is the way back to the start.
        internal static bool WarpOn => (Travel != null && (Travel.Value == "Warp" || Travel.Value == "Both"))
            || (SeedStart?.Invoke()).HasValue || (EntrancesShuffled?.Invoke() ?? false) || FieldMoves.JumpShuffled
            || Abilities.AbilityItems || (PointsOfNoReturn?.Invoke() ?? false);
        internal static Func<bool> EntrancesShuffled;
        internal static Func<bool> PointsOfNoReturn;
        internal static bool MapOn => Travel != null && (Travel.Value == "Map" || Travel.Value == "Both");
        // Which travel buttons go without their Yes / No box (the same four values as Travel).
        internal static ConfigEntry<string> SkipConfirm;
        internal static bool SkipWarpConfirm => SkipConfirm != null
            && (SkipConfirm.Value == "Warp" || SkipConfirm.Value == "Both");
        internal static bool SkipMapConfirm => SkipConfirm != null
            && (SkipConfirm.Value == "Map" || SkipConfirm.Value == "Both");
        internal static ConfigEntry<bool> SkipCutscenes;
        internal static readonly string[] ItemAnimations = { "All", "Progression", "Off" };
        internal static ConfigEntry<string> ItemAnimation;
        internal static readonly string[] ItemColorValues = { "Rarity", "Archipelago", "Off" };
        internal static ConfigEntry<string> ItemColors;
        internal static bool ApColors => ItemColors == null || ItemColors.Value != "Off";
        internal static bool RarityColors => ItemColors == null || ItemColors.Value == "Rarity";
        // Which items not your own show the drawn Archipelago icon: another game's, every other player's, or none.
        internal static readonly string[] ItemIconValues = { "OtherGames", "AllPlayers", "Off" };
        internal static ConfigEntry<string> ItemIcons;
        internal static string IconMode => ItemIcons?.Value ?? "OtherGames";
        internal static ConfigEntry<bool> ItemBackgrounds;
        // The Spy Specs medal in halves: HP (every enemy's HP bar), Free (Spy needs no aim and keeps the turn), or Both.
        internal static readonly string[] SpySpecsValues = { "Off", "HP", "Free", "Both" };
        internal static ConfigEntry<string> SpySpecs;
        internal static bool SpyHp => SpySpecs != null && (SpySpecs.Value == "HP" || SpySpecs.Value == "Both");
        internal static bool SpyFree => SpySpecs != null && (SpySpecs.Value == "Free" || SpySpecs.Value == "Both");
        // Tenths of the normal price: 10 normal, 5 half, 0 free.
        internal const int FullPrice = 10;
        internal static ConfigEntry<int> MedalPrices;
        internal static ConfigEntry<string> EnemyScalingMode;
        // Ten pips, like the volume rows: Off, eight caps, and the monitor's own refresh rate.
        internal static readonly string[] UncapValues = { "Off", "90", "100", "120", "144", "165", "180", "240", "360",
            "Monitor" };
        internal static ConfigEntry<string> UncapFps;

        // A scene that only moves, talks and sets flags is skipped by setting its flags; one that also changes the
        // world is fast-forwarded by the game itself, so it ends exactly as it would.
        private sealed class Scene
        {
            internal string Map;
            internal int Event;
            internal int[] Flags; // null: fast-forward instead of skipping
            internal int OnlyWhileUnset =
                -1; // skipped only while this flag is unset (a scene with a later, needed part)
            internal int Discovery = -1; // a journal discovery the scene records, recorded by the skip too
        }

        private static readonly Scene[] Scenes =
        {
            // The bridge message: moves, three lines, flag 11 (which also hides its trigger).
            new Scene { Map = "SnakemouthBridgeRoom", Event = 0, Flags = new[] { 11 } },
            // Hitting the rope: the bridge's fallen state is set by the scene itself, not its flags, so fast-forwarded.
            new Scene { Map = "SnakemouthBridgeRoom", Event = 1, Flags = null },
            // The Tattle tutorial: Vi and Kabbu walk, one line with no commands, flag 10 (which also hides its
            // trigger).
            new Scene { Map = "SnakemouthBridgeRoom", Event = 2, Flags = new[] { 10 } },
            // The door room's puzzle solved: it moves the rocks, removes two entities and drops the trapdoor's
            // Mushroom, so fast-forwarded.
            new Scene { Map = "SnakemouthDoorRoom", Event = 4, Flags = null },
            // The trapdoor: fast-forwarded, not skipped (a skip would show no opening or fall, just a teleport); the
            // trapdoor landing below then places the party.
            new Scene { Map = "SnakemouthDoorRoom", Event = 5, Flags = null },
            // The spider: two battles, party changes, flag 27 and discovery 1, so fast-forwarded (the battles at normal
            // speed).
            new Scene { Map = "SnakemouthFallRoom", Event = 6, Flags = null },
            // The barkeeper's first talk; the same scene later handles bounties, so skipped only while 158 is unset.
            new Scene { Map = "UndergroundBar", Event = 83, Flags = new[] { 158 }, OnlyWhileUnset = 158 },
            // Arriving outside Snakemouth Den: walk, one line, discovery 0 (a location); the map's autostart sets its
            // flag 22.
            new Scene { Map = "OutsideSnakemouth", Event = 11, Flags = new int[0], Discovery = 0 },
        };

        // A talk trigger that starts by itself on entering (a DialogueTrigger with data[2] 1) opens its line with no
        // StartEvent, so it is skipped as the room's entities are made: its flags set and the trigger kept away.
        private sealed class TalkScene
        {
            internal string Map;
            internal string Entity;
            internal int[] Flags;
        }

        private static readonly TalkScene[] TalkScenes =
        {
            // The swamp's first room: a party remark on entering (line 1: flag 357, a bubble), nothing given.
            new TalkScene { Map = "SwamplandsEntrance", Entity = "initialmessage", Flags = new[] { 357 } },
            // The Defiant Root's first visit: a party talk on entering by any door (line 52: flag 170), nothing given.
            new TalkScene { Map = "DefiantRoot1", Entity = "first visit auto dialogue trigger", Flags = new[] { 170 } },
        };

        private static readonly MethodInfo endEvent = AccessTools.Method(typeof(EventControl), "EndEvent",
            Type.EmptyTypes);

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        // Private in MainManager: the line being shown, and the lines so far.
        private static readonly FieldInfo currentDialogue = AccessTools.Field(typeof(MainManager), "currentdialogue");
        private static readonly FieldInfo diagString = AccessTools.Field(typeof(MainManager), "diagstring");
        private static bool speeding;

        // The slides' fades are lerps scaled by Time.smoothDeltaTime, so timeScale speeds them; EndEvent resets it.
        private const float IntroSpeed = 8f;

        // The panel's two buttons: every Quality of life row off (a choice to its "nothing extra" value), or back to
        // each setting's own default. Enemy scaling and Medal prices live on the Gameplay page and aren't touched.
        internal static void DisableAll()
        {
            foreach (ConfigEntry<bool> setting in new[] { FastText, SkipCutscenes, ItemBackgrounds, ApMenu.Detector })
            {
                if (setting != null)
                {
                    setting.Value = false;
                }
            }
            if (SpySpecs != null)
            {
                SpySpecs.Value = "Off";
            }
            if (ItemAnimation != null)
            {
                ItemAnimation.Value = "Off";
            }
            if (ItemColors != null)
            {
                ItemColors.Value = "Off";
            }
            if (ItemIcons != null)
            {
                ItemIcons.Value = "Off";
            }
            if (Travel != null)
            {
                Travel.Value = "Off";
            }
            if (SkipConfirm != null)
            {
                SkipConfirm.Value = "Off";
            }
            if (UncapFps != null)
            {
                UncapFps.Value = "Off";
            }
        }

        internal static void ResetAll()
        {
            foreach (ConfigEntryBase setting in new ConfigEntryBase[] { FastText, Travel, SkipConfirm, SkipCutscenes,
                ItemAnimation, ItemColors, ItemIcons, ItemBackgrounds, ApMenu.Detector, SpySpecs, UncapFps })
            {
                if (setting != null)
                {
                    setting.BoxedValue = setting.DefaultValue;
                }
            }
        }

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            FastText = config.Bind("QualityOfLife", "FastText", true,
                "Dialogue text is instant instead of letter by letter, as if the skip button were held (the game's own "
                + "skip), but still requires a button press to proceed. Holding the skip button also moves through "
                + "boxes much faster than the game's own hold. Lines the game marks unskippable stay as they are.");
            SkipCutscenes = config.Bind("QualityOfLife", "SkipCutscenes", true,
                "Scenes you don't need to watch are skipped or pass by fast (a list that grows scene by scene). The "
                + "new game's intro is always skipped with Archipelago enabled, whatever this says.");
            ItemAnimation = config.Bind("QualityOfLife", "ItemAnimation", "All", new ConfigDescription(
                "Which received items are shown held up, as when you find one: Progression (items that unlock "
                + "something), All, or Off. Replays on a new file or a reconnect count too; starting items never do, "
                + "and your own finds a scene already showed aren't shown twice. They always arrive either way.",
                new AcceptableValueList<string>(ItemAnimations)));
            ItemColors = config.Bind("QualityOfLife", "ItemColors", "Rarity", new ConfigDescription(
                "The colours for how much an item matters, in the \"You got\" box and the starburst behind it: Rarity, "
                + "a loot game's ladder (filler green, useful blue, progression purple, trap red); Archipelago, its "
                + "own text client's (plum, slate blue, cyan, salmon), darkened to read on the box; Off, the game's "
                + "red text and its own starburst colours.",
                new AcceptableValueList<string>(ItemColorValues)));
            ItemIcons = config.Bind("QualityOfLife", "ItemIcons", "OtherGames", new ConfigDescription(
                "Which items that aren't yours show the Archipelago icon, on the ground, on shelves and when found: "
                + "OtherGames (another game's items; another Bug Fables player's show their real sprite), AllPlayers "
                + "(every item that isn't yours), or Off (another game's items look like the item the game had there, a "
                + "surprise; another Bug Fables player's show their real sprite).",
                new AcceptableValueList<string>(ItemIconValues)));
            ItemBackgrounds = config.Bind("QualityOfLife", "ItemBackgrounds", true,
                "A check's item, on the ground or on a shop shelf, yours included, has the pickup's starburst behind "
                + "it in its class colour (progression, useful, filler, trap, as Item colors colours them; with Item "
                + "colors Off, the game's own colour for the item's kind), so you can tell from afar whether it "
                + "matters. Off: no backdrop until it's picked up, a surprise.");
            SpySpecs = config.Bind("QualityOfLife", "SpySpecs", "Off", new ConfigDescription(
                "The Spy Specs medal's effects, in halves: HP (every enemy's HP bar shows), Free (Spy needs no aiming, "
                + "always works and doesn't use the turn), Both (as if the medal were equipped), or Off (the default; "
                + "left to the medal). Switch it on the Quality of life page.",
                new AcceptableValueList<string>(SpySpecsValues)));
            MedalPrices = config.Bind("Gameplay", "MedalPrices", FullPrice, new ConfigDescription(
                "Medal shop prices, in berries and crystal berries, in tenths of the normal price: 10 normal, 5 half, "
                + "0 free. Any price above free is at least 1. Switch it on the Gameplay page.",
                new AcceptableValueRange<int>(0, FullPrice)));
            EnemyScalingMode = config.Bind("QualityOfLife", "EnemyScaling", "PartyLevel", new ConfigDescription(
                "How tough enemies are, wherever you meet them: PartyLevel scales every enemy to the party's level, so "
                + "every area plays fair in any order; Artifacts scales them to the artifacts found, as vanilla's "
                + "difficulty follows the story (levelling ahead makes it easier); Off keeps each enemy's own stats. "
                + "Difficulty (Hard, Hardest) still applies on top. Never changes a check.",
                new AcceptableValueList<string>(EnemyScaling.Modes)));
            Travel = config.Bind("QualityOfLife", "Travel", "Both", new ConfigDescription(
                "Travel buttons in the pause menu, each behind a Yes / No box: Warp (back to where the game started, "
                + "or to the seed's start), Map (the map, where confirm on an area you've been to travels to its save "
                + "point), Both, or Off. Not shown in battle. In a seed the Warp is always there.",
                new AcceptableValueList<string>(TravelValues)));
            SkipConfirm = config.Bind("QualityOfLife", "SkipConfirm", "Off", new ConfigDescription(
                "Which travel buttons act without their Yes / No box: Warp (warps as soon as it's picked), Map "
                + "(confirm on an area you've been to travels there at once), Both, or Off (both ask first).",
                new AcceptableValueList<string>(TravelValues)));
            UncapFps = config.Bind("QualityOfLife", "UncapFps", "Off", new ConfigDescription(
                "Experimental. A frame rate above the game's 30 or 60: 90 to 360, or Monitor (the display's own "
                + "refresh rate; a 60 Hz display keeps the game's own). With VSync when it divides the monitor's "
                + "refresh rate or reaches it (no tearing), else as a limit. Motion is drawn between the game's "
                + "physics steps, and whatever the game counts in frames still runs at 60 per second, so it plays as "
                + "it does at 60. Off, the default: the game's own FPS and VSync settings.",
                new AcceptableValueList<string>(UncapValues)));
            // Installed in this order, each only if the one before went in, as they depend on each other.
            // A follow-up line is fetched inside the running dialogue, not through a new SetText.
            if (!Hooks.Install(typeof(LineHook), "qol", "item hold-ups' empty follow-up line goes unanswered")
                || !Hooks.Install(typeof(EventHook), "qol", "Skip cutscenes does nothing"))
            {
                return;
            }
            Hooks.Install(typeof(TalkHook), "qol", "a talk that starts by itself plays with Skip cutscenes on");
            if (exitBattle != null && battleInEvent != null && battleAction != null)
            {
                Hooks.Install(typeof(SpiderHook), "qol", "the first spider fight runs its three turns");
            }
            else
            {
                log.LogError("[qol] BattleControl.ExitBattle, inevent or action not found: the first spider fight runs its three turns.");
            }
            // The title screen resets the game's variables; the opening's own state is per file, so it resets there
            // too. Every music change ends in the ChangeMusic overload patched.
            if (!Hooks.Install(typeof(PartyHook), "qol", "Event8's talk plays")
                || !Hooks.Install(typeof(SlideHook), "qol", "the slides play (fast)")
                || !Hooks.Install(typeof(ResetHook), "qol", "the opening's state can carry into the next file"))
            {
                return;
            }
            Hooks.Install(typeof(MusicHook), "qol", "the opening map's music plays before a seed start");
        }

        // A new file with a seed start goes from the title screen straight to that start: no music of the opening map.
        private static bool HoldingMusic()
        {
            MainManager mm = MainManager.instance;
            return randomizerOn != null && randomizerOn() && Seeded.HasValue && !TestStartSet && mm?.flags != null
                && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap
                && (!mm.flags[GameFlags.PermitEvent] || openingPending || startPending || transferring);
        }

        private static bool heldMusicLogged;

        private static class MusicHook
        {
            [HarmonyPatch(typeof(MainManager), nameof(MainManager.ChangeMusic), typeof(AudioClip), typeof(float),
                typeof(int), typeof(bool))]
            [HarmonyPrefix]
            private static void BeforeChangeMusic(ref AudioClip musicclip, int id)
            {
                if (musicclip == null || id != 0 || !HoldingMusic())
                {
                    return;
                }
                if (!heldMusicLogged)
                {
                    heldMusicLogged = true;
                    log.LogInfo(
                        $"[qol] seed start: {musicclip.name} held back on the opening map (silence until the start)");
                }
                musicclip = null;
            }
        }

        private static class ResetHook
        {
            [HarmonyPatch(typeof(MainManager), nameof(MainManager.SetVariables))]
            [HarmonyPostfix]
            private static void ResetFileState()
            {
                if (openingPending || startPending || openingFailed || event8Cut || partyThenFade || transferring)
                {
                    log.LogInfo($"[qol] title screen: the last file's opening state cleared (pending {openingPending}, start {startPending}, "
                        + $"failed {openingFailed}, transferring {transferring})");
                }
                openingPending = startPending = openingFailed = event8Cut = partyThenFade = transferring =
                    heldMusicLogged = false;
                PartyMembers.SetReceived(System.Linq.Enumerable.Empty<int>());
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
            if (on && !openingPending && !openingFailed && MainManager.map != null
                && MainManager.map.mapid.ToString() == OpeningMap
                && !mm.flags[GameFlags.PermitEvent] && mm.flags[GameFlags.NewGame])
            {
                openingPending = true;
                // Whether the seed has a start is asked at the transfer: before the login it isn't known yet.
                startPending = !transferring;
                log.LogInfo("[qol] the opening is due (flag 15 unset on the starting map): doing it at the start, on a free frame");
            }
            string here = MainManager.map == null ? null : MainManager.map.mapid.ToString();
            if (heldBack != null && (here != OpeningMap || Time.realtimeSinceStartup - heldSince > 10f))
            {
                UnityEngine.Object.Destroy(heldBack);
                heldBack = null;
                log.LogInfo($"[qol] the slides' backdrop removed on {here}");
            }
            if (transferring && here != OpeningMap)
            {
                transferring = false;
            }
            if (openingPending && AtStart(here) && MainManager.player != null && !mm.inevent && !mm.message
                && !mm.minipause
                && MainManager.battle == null && !mm.intransition && !MainManager.roomtransition)
            {
                openingPending = false;
                try
                {
                    RunOpening();
                }
                catch (Exception e)
                {
                    openingFailed = true;
                    log.LogError($"[qol] opening failed: {e}");
                }
            }
            if (on && MainManager.map != null && MainManager.map != reorderedMap)
            {
                reorderedMap = MainManager.map;
                RerollFirst(MainManager.map);
            }
            if (partyThenFade)
            {
                try
                {
                    PartyThenFade();
                }
                catch (Exception e)
                {
                    MainManager.PlayTransition(1, 0, 0.02f, Color.black);
                    log.LogError($"[qol] opening party failed: {e}");
                }
            }
            if (trapdoorLanding)
            {
                TickTrapdoorLanding(mm, here);
            }
            if (event8Cut && !mm.message)
            {
                event8Cut = false;
                try
                {
                    EndEvent8();
                }
                catch (Exception e)
                {
                    log.LogError($"[qol] ending Event8 failed: {e}");
                }
            }
            if (startPending && !TestStartSet && !Seeded.HasValue && SeedKnown != null && SeedKnown())
            {
                startPending = false;
                log.LogInfo(
                    "[qol] the seed has no start of its own (Starting Location off): staying at the game's start");
            }
            if (startPending && (TestStartSet || Seeded.HasValue) && !partyThenFade
                && Time.frameCount > partySetFrame + 1 && MainManager.player != null && !mm.inevent
                && !mm.message && MainManager.battle == null)
            {
                startPending = false;
                transferring = true;
                try
                {
                    KeyValuePair<string, int>? seeded = Seeded;
                    if (seeded.HasValue && !TestStartSet)
                    {
                        var map = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), seeded.Value.Key, true);
                        // A room start: walking in through its door, as the game's own door transfer does.
                        Vector3[] entry = SeedStartDoor(map);
                        if (entry != null)
                        {
                            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map,
                                MainManager.player.transform.position, entry[1], entry[2]));
                            log.LogInfo($"[qol] the seed's start (Starting Location): transferring to {map}, entering from {SeedStartFrom?.Invoke()}");
                            return;
                        }
                        // Every start arrives through a door; a save-point start (not built) will send its door too.
                        transferring = false;
                        log.LogError($"[qol] the seed's start {map} (from {SeedStartFrom?.Invoke()}, entity {seeded.Value.Value}): no door to arrive through; staying at the game's start");
                        return;
                    }
                    // The game's own transfer to a door's spots; the console's warp steps beside the save point.
                    var start = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), StartMapName, true);
                    string[] parts = TestStart.Split('@');
                    Vector3[] door = DoorInto(start, parts.Length > 1 ? parts[1].Trim() : null);
                    if (door != null)
                    {
                        MainManager.instance.StartCoroutine(MainManager.TransferMap((int)start,
                            MainManager.player.transform.position, door[1], door[2]));
                    }
                    else
                    {
                        MainManager.instance.StartCoroutine(MainManager.TransferMap((int)start, Vector3.zero));
                        log.LogWarning($"[qol] test start: no door leads into {start}; arriving at its origin");
                    }
                    log.LogInfo($"[qol] test start (Debug.TestStart): transferring to {start}");
                }
                catch (Exception e)
                {
                    log.LogError($"[qol] test start {TestStart} failed: {e.Message}");
                }
            }
            bool settings = SettingsOn != null && SettingsOn();
            bool slides = (on && InIntroSlides()) || (settings && InFastScene());
            if (slides)
            {
                // Each slide's line waits for a press at its end; answer it.
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

            bool skippable = settings && Skippable(mm);
            if (skippable && FastText.Value && !mm.waitinput)
            {
                mm.skiptext = true;
            }
            // Holding skip waits out inputcooldown (16 frames) between boxes; with instant text that's all that's left,
            // so it's cut short. The game's own hold branch still does the advancing.
            if (skippable && FastText.Value && MainManager.GetKey(5, hold: true) && mm.inputcooldown > HeldCooldown)
            {
                mm.inputcooldown = HeldCooldown;
            }
        }

        // The game's own cooldown while a box is still typing.
        private const float HeldCooldown = 4f;

        private static bool Skippable(MainManager mm)
        {
            if (!mm.message || mm.prompt || mm.itemlist != null || mm.inlist || MainManager.noskip)
            {
                return false;
            }
            var lines = diagString?.GetValue(null) as List<string>;
            return lines != null && currentDialogue != null && (int)currentDialogue.GetValue(null) == lines.Count;
        }

        // Event8's slides: a black "back" sprite under the GUI camera, there from the first slide to the last.
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
