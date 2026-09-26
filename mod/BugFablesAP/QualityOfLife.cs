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
    // The logic never counts on any of them. With the Archipelago mod disabled only Use on normal saves turns them on.
    internal static class QualityOfLife
    {
        internal static ConfigEntry<bool> FastText;
        internal static readonly string[] TravelValues = { "Off", "Warp", "Map", "Both" };
        internal static ConfigEntry<string> Travel;
        // The Warp is always there with a random start (the logic counts on it to re-enter the start) and with the
        // entrance randomizer (the escape from a dead end), whatever Travel says.
        internal static bool WarpOn => (Travel != null && (Travel.Value == "Warp" || Travel.Value == "Both"))
            || (SeedStart?.Invoke()).HasValue || (EntrancesShuffled?.Invoke() ?? false);
        internal static Func<bool> EntrancesShuffled;
        internal static bool MapOn => Travel != null && (Travel.Value == "Map" || Travel.Value == "Both");
        // Which travel buttons go without their Yes / No box (the same four values as Travel).
        internal static ConfigEntry<string> SkipConfirm;
        internal static bool SkipWarpConfirm => SkipConfirm != null && (SkipConfirm.Value == "Warp" || SkipConfirm.Value == "Both");
        internal static bool SkipMapConfirm => SkipConfirm != null && (SkipConfirm.Value == "Map" || SkipConfirm.Value == "Both");
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
        // Tenths of the normal price: 10 normal, 5 half, 0 free.
        internal const int FullPrice = 10;
        internal static ConfigEntry<int> MedalPrices;
        internal static ConfigEntry<string> EnemyScaling;

        // A scene that only moves, talks and sets flags is skipped by setting its flags; one that also changes the
        // world is fast-forwarded by the game itself, so it ends exactly as it would.
        private sealed class Scene
        {
            internal string Map;
            internal int Event;
            internal int[] Flags; // null: fast-forward instead of skipping
            internal int OnlyWhileUnset = -1; // skipped only while this flag is unset (a scene with a later, needed part)
            internal int Discovery = -1; // a journal discovery the scene records, recorded by the skip too
        }

        private static readonly Scene[] Scenes =
        {
            // The bridge message: moves, three lines, flag 11 (which also hides its trigger).
            new Scene { Map = "SnakemouthBridgeRoom", Event = 0, Flags = new[] { 11 } },
            // Hitting the rope: the bridge's fallen state is set by the scene itself, not its flags, so fast-forwarded.
            new Scene { Map = "SnakemouthBridgeRoom", Event = 1, Flags = null },
            // The Tattle tutorial: Vi and Kabbu walk, one line with no commands, flag 10 (which also hides its trigger).
            new Scene { Map = "SnakemouthBridgeRoom", Event = 2, Flags = new[] { 10 } },
            // The door room's puzzle solved: it moves the rocks, removes two entities and drops the trapdoor's Mushroom, so fast-forwarded.
            new Scene { Map = "SnakemouthDoorRoom", Event = 4, Flags = null },
            // The trapdoor: fast-forwarded, not skipped (a skip showed no opening or fall, just a teleport: the user); the
            // trapdoor landing below then places the party.
            new Scene { Map = "SnakemouthDoorRoom", Event = 5, Flags = null },
            // The spider: two battles, party changes, flag 27 and discovery 1, so fast-forwarded (the battles at normal speed).
            new Scene { Map = "SnakemouthFallRoom", Event = 6, Flags = null },
            // The barkeeper's first talk; the same scene later handles bounties, so skipped only while 158 is unset.
            new Scene { Map = "UndergroundBar", Event = 83, Flags = new[] { 158 }, OnlyWhileUnset = 158 },
            // Arriving outside Snakemouth Den: walk, one line, discovery 0 (a location); the map's autostart sets its flag 22.
            new Scene { Map = "OutsideSnakemouth", Event = 11, Flags = new int[0], Discovery = 0 },
        };

        private static Harmony harmony;
        private static readonly MethodInfo endEvent = AccessTools.Method(typeof(EventControl), "EndEvent", Type.EmptyTypes);

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        // Private in MainManager: the line being shown, and the lines so far.
        private static readonly FieldInfo currentDialogue = AccessTools.Field(typeof(MainManager), "currentdialogue");
        private static readonly FieldInfo diagString = AccessTools.Field(typeof(MainManager), "diagstring");
        private static bool speeding;

        // The slides' fades are lerps scaled by Time.smoothDeltaTime, so timeScale speeds them; EndEvent resets it.
        private const float IntroSpeed = 8f;

        // The panel's two buttons: every Quality of life row off (a choice to its "nothing extra" value), or back to
        // each setting's own default. Enemy scaling and Shop prices live on the Gameplay page and aren't touched.
        internal static void DisableAll()
        {
            foreach (ConfigEntry<bool> setting in new[] { FastText, SkipCutscenes, ItemBackgrounds, ApMenu.Detector })
            {
                if (setting != null)
                {
                    setting.Value = false;
                }
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
        }

        internal static void ResetAll()
        {
            foreach (ConfigEntryBase setting in new ConfigEntryBase[] { FastText, Travel, SkipConfirm, SkipCutscenes, ItemAnimation, ItemColors, ItemIcons, ItemBackgrounds, ApMenu.Detector })
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
                + "skip), but still requires a button press to proceed. Holding the skip button also moves through boxes "
                + "much faster than the game's own hold. Lines the game marks unskippable stay as they are.");
            SkipCutscenes = config.Bind("QualityOfLife", "SkipCutscenes", true,
                "Scenes you don't need to watch are skipped or pass by fast (a list that grows scene by scene). The new "
                + "game's intro is always skipped with Archipelago enabled, whatever this says.");
            ItemAnimation = config.Bind("QualityOfLife", "ItemAnimation", "All", new ConfigDescription(
                "Which items received from other players are shown held up, as when you find one: Progression (items that "
                + "unlock something), All, or Off. They always arrive either way; your own finds are always shown.",
                new AcceptableValueList<string>(ItemAnimations)));
            ItemColors = config.Bind("QualityOfLife", "ItemColors", "Rarity", new ConfigDescription(
                "The colours for how much an item matters, in the \"You got\" box and the starburst behind it: Rarity, a loot "
                + "game's ladder (filler green, useful blue, progression purple, trap red); Archipelago, its own text client's "
                + "(plum, slate blue, cyan, salmon), darkened to read on the box; Off, the game's red text and its own "
                + "starburst colours.",
                new AcceptableValueList<string>(ItemColorValues)));
            ItemIcons = config.Bind("QualityOfLife", "ItemIcons", "OtherGames", new ConfigDescription(
                "Which items that aren't yours show the Archipelago icon, on the ground, on shelves and when found: OtherGames "
                + "(another game's items; another Bug Fables player's show their real sprite), AllPlayers (every item that isn't "
                + "yours), or Off (they look like the item the game had there, a surprise).",
                new AcceptableValueList<string>(ItemIconValues)));
            ItemBackgrounds = config.Bind("QualityOfLife", "ItemBackgrounds", true,
                "A check's item, on the ground or on a shop shelf, yours included, has the pickup's starburst behind it in its "
                + "Archipelago class colour (progression, useful, filler, trap), so you can tell from afar whether it matters. "
                + "Off: no backdrop until it's picked up, a surprise.");
            MedalPrices = config.Bind("Gameplay", "MedalPrices", FullPrice, new ConfigDescription(
                "Medal shop prices, in berries and crystal berries, in tenths of the normal price: 10 normal, 5 half, 0 free. "
                + "Any price above free is at least 1. Switch it on the Gameplay page.", new AcceptableValueRange<int>(0, FullPrice)));
            EnemyScaling = config.Bind("QualityOfLife", "EnemyScaling", "PartyLevel", new ConfigDescription(
                "How tough enemies are, wherever you meet them: PartyLevel scales every enemy to the party's level, so "
                + "every area plays fair in any order; Artifacts scales them to the artifacts found, as vanilla's "
                + "difficulty follows the story (levelling ahead makes it easier); Off keeps each enemy's own stats. "
                + "Difficulty (Hard, Hardest) still applies on top. Never changes a check.",
                new AcceptableValueList<string>(BugFablesAP.EnemyScaling.Modes)));
            Travel = config.Bind("QualityOfLife", "Travel", "Both", new ConfigDescription(
                "Travel buttons in the pause menu, each behind a Yes / No box: Warp (back to where the game started, or to the seed's start), Map (the "
                + "map, where confirm on an area you've been to travels to its save point), Both, or Off. Not shown in battle.",
                new AcceptableValueList<string>(TravelValues)));
            SkipConfirm = config.Bind("QualityOfLife", "SkipConfirm", "Off", new ConfigDescription(
                "Which travel buttons act without their Yes / No box: Warp (warps as soon as it's picked), Map (confirm on an "
                + "area you've been to travels there at once), Both, or Off (both ask first).",
                new AcceptableValueList<string>(TravelValues)));
            // A follow-up line is fetched inside the running dialogue, not through a new SetText.
            MethodInfo getLine = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetDialogueText), new[] { typeof(int) });
            if (getLine == null)
            {
                log.LogError("[qol] MainManager.GetDialogueText(int) not found: item hold-ups' empty follow-up line goes unanswered.");
                return;
            }
            harmony = new Harmony(Plugin.Guid + ".qol." + DateTime.UtcNow.Ticks);
            harmony.Patch(getLine, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeGetLine)));
            MethodInfo startEvent = AccessTools.Method(typeof(EventControl), nameof(EventControl.StartEvent), new[] { typeof(int), typeof(NPCControl) });
            if (startEvent == null)
            {
                log.LogError("[qol] EventControl.StartEvent(int, NPCControl) not found: Skip cutscenes does nothing.");
                return;
            }
            harmony.Patch(startEvent, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeStartEvent)));
            MethodInfo changeParty = AccessTools.Method(typeof(MainManager), nameof(MainManager.ChangeParty), new[] { typeof(int[]), typeof(bool), typeof(bool) });
            if (changeParty == null)
            {
                log.LogError("[qol] MainManager.ChangeParty(int[], bool, bool) not found: Event8's talk plays.");
                return;
            }
            harmony.Patch(changeParty, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeChangeParty)));
            MethodInfo solid = AccessTools.Method(typeof(MainManager), nameof(MainManager.NewSolidColor),
                new[] { typeof(string), typeof(Color), typeof(float), typeof(Vector3), typeof(Vector2) });
            if (solid == null)
            {
                log.LogError("[qol] MainManager.NewSolidColor not found: the slides play (fast).");
                return;
            }
            harmony.Patch(solid, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeSolidColor)));
            // The title screen resets the game's variables; the opening's own state is per file, so it resets there too.
            MethodInfo reset = AccessTools.Method(typeof(MainManager), nameof(MainManager.SetVariables));
            if (reset == null)
            {
                log.LogError("[qol] MainManager.SetVariables not found: the opening's state can carry into the next file.");
                return;
            }
            harmony.Patch(reset, postfix: new HarmonyMethod(typeof(QualityOfLife), nameof(ResetFileState)));
            // Every music change ends in this overload.
            MethodInfo music = AccessTools.Method(typeof(MainManager), nameof(MainManager.ChangeMusic),
                new[] { typeof(AudioClip), typeof(float), typeof(int), typeof(bool) });
            if (music == null)
            {
                log.LogError("[qol] MainManager.ChangeMusic(AudioClip, float, int, bool) not found: the opening map's music plays before a seed start.");
                return;
            }
            harmony.Patch(music, prefix: new HarmonyMethod(typeof(QualityOfLife), nameof(BeforeChangeMusic)));
        }

        // A new file with a seed start goes from the title screen straight to that start: no music of the opening map.
        private static bool HoldingMusic()
        {
            MainManager mm = MainManager.instance;
            return randomizerOn != null && randomizerOn() && Seeded.HasValue && !TestStartSet && mm?.flags != null
                && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap
                && (!mm.flags[15] || openingPending || startPending || transferring);
        }

        private static bool heldMusicLogged;

        private static void BeforeChangeMusic(ref AudioClip musicclip, int id)
        {
            if (musicclip == null || id != 0 || !HoldingMusic())
            {
                return;
            }
            if (!heldMusicLogged)
            {
                heldMusicLogged = true;
                log.LogInfo($"[qol] seed start: {musicclip.name} held back on the opening map (silence until the start)");
            }
            musicclip = null;
        }

        private static void ResetFileState()
        {
            if (openingPending || startPending || openingFailed || event8Cut || partyThenFade || transferring)
            {
                log.LogInfo($"[qol] title screen: the last file's opening state cleared (pending {openingPending}, start {startPending}, "
                    + $"failed {openingFailed}, transferring {transferring})");
            }
            openingPending = startPending = openingFailed = event8Cut = partyThenFade = transferring = heldMusicLogged = false;
            PartyMembers.SetReceived(System.Linq.Enumerable.Empty<int>());
        }

        // Always with Archipelago on: a seed's start ends the opening with a transfer, and its tutorial battle
        // was written for two, never played with one member. Skip cutscenes is for later scenes.
        // The opening: Event16 (Maki's talk, Vi joining, the tutorial battle, location 1) never starts; the mod does
        // what it would leave behind, the game's way, on a later frame. Flag 15 marks location 1 done.
        private const string OpeningMap = "BugariaOutskirtsOutsideCity";
        private const int OpeningEvent = 16;
        private const long OpeningLocation = 7_720_001; // Outskirts: Maki and Eetl's Gift (apworld id 1)
        private static bool openingPending;
        private static bool openingFailed; // one try per session: a failure is logged, never retried every frame
        // Dev only ([Debug] TestStart): a map the opening ends with a warp to, a stand-in for a random start.
        internal static string TestStart;
        // The seed's start (Starting Location): the opening ends with a transfer beside that save point instead.
        internal static Func<KeyValuePair<string, int>?> SeedStart;
        // A room start: the map whose door leads into the start map.
        internal static Func<string> SeedStartFrom;

        // A room start's door spots (appear, walk to), read from the door in the "from" map; null for a save-point start.
        internal static Vector3[] SeedStartDoor(MainManager.Maps map)
        {
            string from = SeedStartFrom?.Invoke();
            return from == null ? null : DoorInto(map, from);
        }
        internal static Func<bool> SeedKnown;
        private static KeyValuePair<string, int>? Seeded => SeedStart?.Invoke();
        // With Archipelago on, or off with Use on normal saves: fast text and the scene list, never the intro.
        internal static Func<bool> SettingsOn;
        private static bool startPending;
        // With a test start, the slides' backdrop stays up until the start map has loaded behind the transfer's fade.
        private static GameObject heldBack;
        private static bool transferring;
        private static float heldSince;

        private static bool TestStartSet => !string.IsNullOrEmpty(TestStart);
        // "Map" or "Map@FromMap": the start map, and optionally the map whose door into it the party arrives through.
        private static string StartMapName => TestStart.Split('@')[0].Trim();

        private static bool AtStart(string map) =>
            map != null && (TestStartSet ? string.Equals(map, StartMapName, StringComparison.OrdinalIgnoreCase) : map == OpeningMap);

        // Arriving as if through a door: data[0] the map, vectordata[1] where the party appears, [2] where it walks.
        // Read from the entity table of the map left behind; fields split by '}', data count at 60, vectordata at 71.
        internal static Vector3[] DoorInto(MainManager.Maps target, string fromMap)
        {
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                if (fromMap != null && !string.Equals(map.ToString(), fromMap, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                TextAsset table = Resources.Load<TextAsset>("Data/EntityData/" + (int)map);
                if (table == null)
                {
                    continue;
                }
                string[] lines = table.ToString().Split('\n');
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string[] f = lines[i].Split('}');
                    if (f.Length < 81 || f[1].Trim() != "DoorOtherMap" || f[60].Trim() == "0" || f[61].Trim() != ((int)target).ToString())
                    {
                        continue;
                    }
                    int count = int.Parse(f[71].Trim());
                    if (count < 3)
                    {
                        continue;
                    }
                    var v = new Vector3[count];
                    for (int k = 0; k < count; k++)
                    {
                        v[k] = new Vector3(float.Parse(f[72 + k * 3].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(f[73 + k * 3].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(f[74 + k * 3].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                    }
                    log.LogInfo($"[qol] arriving in {target} through {map}'s door (entity {i}): appear at {v[1]}, walk to {v[2]}");
                    return v;
                }
            }
            return null;
        }

        // The trapdoor scene lands the party on its own spots; once it ends, the party enters the fall room again the way the
        // opened trapdoor leads in (the door room's way down), as any door arrival does.
        private const int TrapdoorEvent = 5;
        private static bool trapdoorLanding;

        private static void TickTrapdoorLanding(MainManager mm, string here)
        {
            // The scene's second half (the landing talk in the fall room) is replaced: it ends on the black screen right after
            // the fall, before its own placing (which at speed left the party far left, the camera swinging after them).
            if (here == "SnakemouthFallRoom" && mm.inevent && MainManager.lastevent == TrapdoorEvent)
            {
                CutTrapdoorScene();
            }
            // In the door room only once the scene was skipped (flag 14 set, no scene running).
            bool skipped = here == "SnakemouthDoorRoom" && mm.flags[14];
            if (here != "SnakemouthFallRoom" && !skipped)
            {
                if (here != "SnakemouthDoorRoom")
                {
                    trapdoorLanding = false;
                }
                return;
            }
            if (MainManager.player == null || mm.inevent || mm.message || mm.minipause || MainManager.battle != null
                || mm.intransition || MainManager.roomtransition)
            {
                return;
            }
            trapdoorLanding = false;
            Vector3[] door = DoorInto(MainManager.Maps.SnakemouthFallRoom, "SnakemouthDoorRoom");
            if (door == null)
            {
                log.LogWarning("[qol] after the trapdoor scene: no door from SnakemouthDoorRoom into the fall room; the party stays where the scene left it");
                return;
            }
            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)MainManager.Maps.SnakemouthFallRoom, MainManager.player.transform.position, door[1], door[2]));
            log.LogInfo("[qol] after the trapdoor scene" + (skipped ? " (skipped)" : "") + ": entering the fall room through the door room's way down");
        }

        private static void CutTrapdoorScene()
        {
            MainManager.events.StopCoroutine("Event" + TrapdoorEvent);
            // The scene turned the party's gravity off and forced their animations; the landing's transfer needs them normal.
            foreach (EntityControl member in MainManager.GetPartyEntities() ?? new EntityControl[0])
            {
                if (member == null)
                {
                    continue;
                }
                member.LockRigid(false);
                if (member.rigid != null)
                {
                    member.rigid.useGravity = true;
                }
                member.overrideanim = false;
                member.overrridejump = false;
                member.animstate = 0;
            }
            MainManager.map?.RestoreLimit(false);
            endEvent?.Invoke(null, null);
            MainManager.ChangeMusic("Cave0");
            log.LogInfo("[qol] the trapdoor scene ended after the fall (its landing talk replaced by the door arrival)");
        }

        // Event8's talk after the slides is cut: its first step is ChangeParty({1}) (Kabbu alone); refusing it stops the
        // scene, and the next frame the mod ends it as its own end does.
        private static bool event8Cut;

        private static bool BeforeChangeParty(int[] ids, bool fromscratch, bool destroyoldentity)
        {
            MainManager mm = MainManager.instance;
            if (randomizerOn == null || !randomizerOn() || mm == null || MainManager.map == null
                || MainManager.lastevent != 8 || !mm.inevent || MainManager.map.mapid.ToString() != OpeningMap || mm.flags[15]
                || ids == null || ids.Length != 1 || ids[0] != 1 || !fromscratch || destroyoldentity || MainManager.events == null)
            {
                return true;
            }
            MainManager.events.StopCoroutine("Event8");
            event8Cut = true;
            log.LogInfo("[qol] Event8's talk after the slides cut: Kabbu-alone party refused, the scene stopped");
            return false;
        }

        // The cut before the slides: their first step is the black backdrop, NewSolidColor("back").
        private static void BeforeSolidColor(string name)
        {
            MainManager mm = MainManager.instance;
            if (name != "back" || randomizerOn == null || !randomizerOn() || mm == null || MainManager.map == null
                || MainManager.lastevent != 8 || !mm.inevent || MainManager.map.mapid.ToString() != OpeningMap || mm.flags[15]
                || MainManager.events == null || event8Cut)
            {
                return;
            }
            MainManager.events.StopCoroutine("Event8");
            event8Cut = true;
            log.LogInfo("[qol] Event8 cut before its slides: the scene stopped");
        }

        private static void EndEvent8()
        {
            MainManager mm = MainManager.instance;
            Transform back = MainManager.GUICamera == null ? null : MainManager.GUICamera.transform.Find("back");
            // A start elsewhere keeps the slides' black backdrop until the new map has loaded, so the opening map isn't seen.
            if (back != null && (TestStartSet || Seeded.HasValue))
            {
                heldBack = back.gameObject;
                heldSince = Time.realtimeSinceStartup;
            }
            else if (back != null)
            {
                UnityEngine.Object.Destroy(back.gameObject);
            }
            mm.hud[0].transform.parent.gameObject.SetActive(true);
            MainManager.ResetCamera();
            // The party change waits for the next frame, behind the black screen: before EndEvent, FixEntities met a
            // character being replaced (NullReferenceException).
            partyThenFade = !TestStartSet && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap;
            if (Seeded.HasValue && !TestStartSet)
            {
                // Leaving for the seed's start: silence until that map starts its own music.
                MainManager.FadeMusic(0.05f);
            }
            else if (!TestStartSet)
            {
                MainManager.ChangeMusic(Resources.Load<AudioClip>("Audio/Music/Inside0"));
                MainManager.music[0].clip = Resources.Load<AudioClip>("Audio/Musics/Field0");
                MainManager.music[0].volume = 0f;
                MainManager.music[0].Play();
            }
            endEvent?.Invoke(null, null);
            if (TestStartSet)
            {
                startPending = true; // at once, still behind the backdrop
            }
            else if (Seeded.HasValue)
            {
                startPending = !transferring; // after the party is set; the transfer fades in
            }
            else if (!partyThenFade)
            {
                MainManager.PlayTransition(1, 0, 0.02f, Color.black);
            }
            log.LogInfo($"[qol] Event8 ended the game's way; inevent={mm.inevent}");
        }

        private static bool partyThenFade;

        private static int partySetFrame = -10;

        private static void PartyThenFade()
        {
            partyThenFade = false;
            partySetFrame = Time.frameCount;
            MainManager mm = MainManager.instance;
            EntityControl four = MainManager.GetEntity(4);
            if (four != null && mm.playerdata != null)
            {
                SetOpeningParty(four.transform.position + Vector3.left * 2.5f);
                if (mm.camtarget != null)
                {
                    MainManager.MainCamera.transform.position = mm.camtarget.position + mm.camoffset;
                }
            }
            else
            {
                log.LogWarning("[qol] opening: entity 4 not found; the party stays where it is");
            }
            // A seed start's transfer fades in on arrival; fading in here would destroy the fade it waits on.
            if (startPending && Seeded.HasValue)
            {
                log.LogInfo("[qol] opening party set; the fade-in is left to the seed start's transfer");
                return;
            }
            MainManager.PlayTransition(1, 0, 0.02f, Color.black);
        }

        // The reshuffle choice (prompt target -199) moves to the front, in the map's dialogue table, once per map load.
        private static MapControl reorderedMap;

        private static void RerollFirst(MapControl map)
        {
            if (map.dialogues == null)
            {
                return;
            }
            for (int line = 0; line < map.dialogues.Length; line++)
            {
                string text = map.dialogues[line];
                if (text == null || !text.Contains(",-199,"))
                {
                    continue;
                }
                int at = text.IndexOf("|prompt,map,", StringComparison.Ordinal);
                while (at >= 0)
                {
                    int end = text.IndexOf('|', at + 1);
                    if (end < 0)
                    {
                        break;
                    }
                    string[] f = text.Substring(at + 1, end - at - 1).Split(',');
                    int n;
                    if (f.Length >= 4 && int.TryParse(f[3], out n) && f.Length >= 4 + 2 * n)
                    {
                        int k = Array.IndexOf(f, "-199", 4, n) - 4;
                        if (k > 0)
                        {
                            var targets = f.Skip(4).Take(n).ToList();
                            var texts = f.Skip(4 + n).Take(n).ToList();
                            string target = targets[k], label = texts[k];
                            targets.RemoveAt(k);
                            texts.RemoveAt(k);
                            targets.Insert(0, target);
                            texts.Insert(0, label);
                            string command = string.Join(",", f.Take(4).Concat(targets).Concat(texts).Concat(f.Skip(4 + 2 * n)).ToArray());
                            text = text.Substring(0, at + 1) + command + text.Substring(end);
                            end = at + 1 + command.Length;
                            log.LogInfo($"[qol] {map.mapid} line {line}: the reshuffle choice moved to the top of its prompt");
                        }
                    }
                    at = text.IndexOf("|prompt,map,", end, StringComparison.Ordinal);
                }
                map.dialogues[line] = text;
            }
        }

        // Vi and Kabbu (or the one starting member) set before the fade-in, so the first frame already has the right party.
        private static void SetOpeningParty(Vector3 at)
        {
            MainManager mm = MainManager.instance;
            MainManager.ChangeParty(new[] { 0, 1 }, true, true);
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            MainManager.ResetCamera();
            // ResetCamera aims at MainManager.player, which may be the old leader Unity destroys only at frame end.
            if (mm.playerdata.Length > 0 && mm.playerdata[0].entity != null)
            {
                mm.camtarget = mm.playerdata[0].entity.transform;
            }
        }

        private static void RunOpening()
        {
            MainManager mm = MainManager.instance;
            // Where the player already is: placing the party anywhere else snapped back a player walking during the fade-in.
            SetOpeningParty(MainManager.player.transform.position);
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
                    log.LogInfo($"[qol] opening: no {name} (Event8 cut before making it)");
                }
            }
            // The building's own entity numbers, only there: elsewhere they're other things.
            bool inBuilding = MainManager.map.mapid.ToString() == OpeningMap;
            EntityControl exit = inBuilding ? MainManager.GetEntity(2) : null;
            if (exit != null && exit.npcdata != null)
            {
                exit.gameObject.SetActive(true);
                exit.npcdata.vectordata[4] = MainManager.defaultcamoffset;
                exit.npcdata.vectordata[5] = MainManager.defaultcamangle;
            }
            EntityControl eleven = inBuilding ? MainManager.GetEntity(11) : null;
            if (eleven != null)
            {
                eleven.animstate = 0;
            }
            EntityControl trigger = inBuilding ? MainManager.GetEntity(9) : null;
            if (trigger != null && trigger.name == "EventTrigger")
            {
                trigger.gameObject.SetActive(false); // gone as on a reload with flag 15 (its limit)
            }
            // The scene walks Maki out and destroys him; flag 15 (his limit) only hides him from the next map load.
            EntityControl maki = inBuilding ? MainManager.GetEntity(4) : null;
            if (maki != null && maki.name == "Maki")
            {
                UnityEngine.Object.Destroy(maki.gameObject);
            }
            mm.flags[15] = true;
            mm.boardquests[1].Insert(0, 11);
            HoldUps.FoundAt(OpeningLocation, "the opening's gift (location 1)");
            log.LogInfo($"[qol] opening done without Event16 on {MainManager.map.mapid}: party {string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())}, "
                + $"characters {mm.playerdata.Count(p => p.entity != null)}, exit {(exit != null ? "active " + exit.gameObject.activeSelf : inBuilding ? "NOT found" : "not here")}, "
                + $"Maki {(maki != null && maki.name == "Maki" ? "removed" : inBuilding ? "NOT found" : "not here")}, flag 15 {mm.flags[15]}");
        }

        private static bool BeforeStartEvent(int id)
        {
            if (id == TrapdoorEvent && randomizerOn() && MainManager.map != null && MainManager.map.mapid.ToString() == "SnakemouthDoorRoom")
            {
                trapdoorLanding = true;
            }
            if (id == OpeningEvent && randomizerOn() && MainManager.map != null
                && MainManager.map.mapid.ToString() == OpeningMap)
            {
                // Also once done: its trigger stays until the map reloads, and running the scene then crashes.
                openingPending = !MainManager.instance.flags[15];
                endEvent?.Invoke(null, null);
                log.LogInfo(openingPending ? "[qol] Event16 (the opening) skipped: the mod does what it leaves behind on the next free frame"
                    : "[qol] Event16 (the opening) refused: already done");
                return false;
            }
            Scene scene = SceneFor(id);
            if (scene == null || scene.Flags == null || !SkipCutscenes.Value || SettingsOn == null || !SettingsOn()
                || (scene.OnlyWhileUnset >= 0 && MainManager.instance.flags[scene.OnlyWhileUnset]))
            {
                return true;
            }
            foreach (int flag in scene.Flags)
            {
                MainManager.instance.flags[flag] = true;
            }
            if (scene.Discovery >= 0)
            {
                MainManager.UpdateJounal(MainManager.Library.Discovery, scene.Discovery);
            }
            // A trigger freezes the player (minipause) and only the scene's end undoes it: end it the game's way.
            endEvent?.Invoke(null, null);
            log.LogInfo($"[qol] skipped Event{id} on {scene.Map}: set flags {string.Join(", ", scene.Flags.Select(f => f.ToString()).ToArray())}, "
                + (scene.Discovery >= 0 ? $"discovery {scene.Discovery} now {MainManager.instance.librarystuff[(int)MainManager.Library.Discovery, scene.Discovery]}, " : "")
                + (endEvent != null ? "ended it the game's way" : "EndEvent NOT found: the player may stay frozen"));
            return false;
        }

        private static Scene SceneFor(int id)
        {
            string map = MainManager.map == null ? null : MainManager.map.mapid.ToString();
            return map == null ? null : Scenes.FirstOrDefault(s => s.Event == id && s.Map == map);
        }

        private static bool InFastScene()
        {
            // A battle a scene starts is played at the game's own speed.
            if (!SkipCutscenes.Value || !MainManager.instance.inevent || MainManager.battle != null || MainManager.instance.inbattle)
            {
                return false;
            }
            Scene scene = SceneFor(MainManager.lastevent);
            return scene != null && scene.Flags == null;
        }

        // The mod's hold-ups ask for ItemSwap.EmptyLine as their follow-up: answer |end|, which skips the wait for a press.
        private static bool BeforeGetLine(int id, ref string __result)
        {
            if (id != ItemSwap.EmptyLine)
            {
                return true;
            }
            __result = "|end|";
            return false;
        }

        internal static void Tick()
        {
            MainManager mm = MainManager.instance;
            if (mm == null || !MainManager.basicload)
            {
                return;
            }
            bool on = randomizerOn();
            if (on && !openingPending && !openingFailed && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap
                && !mm.flags[15] && mm.flags[691])
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
            if (openingPending && AtStart(here) && MainManager.player != null && !mm.inevent && !mm.message && !mm.minipause
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
                log.LogInfo("[qol] the seed has no start of its own (Starting Location off): staying at the game's start");
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
                            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map, MainManager.player.transform.position, entry[1], entry[2]));
                            log.LogInfo($"[qol] the seed's start (Starting Location): transferring to {map}, entering from {SeedStartFrom?.Invoke()}");
                            return;
                        }
                        if (seeded.Value.Value < 0)
                        {
                            transferring = false;
                            log.LogError($"[qol] the seed's start {map} (from {SeedStartFrom?.Invoke()}): no door found; staying at the game's start");
                            return;
                        }
                        // A save-point start: beside it, as Warp to Start lands.
                        Vector3 spot = WarpButton.SavePointSpot(map, seeded.Value.Value);
                        MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map, spot));
                        log.LogInfo($"[qol] the seed's start (Starting Location): transferring to {map}, beside save point {seeded.Value.Value}, at {spot}");
                        return;
                    }
                    // The game's own transfer to a door's spots; the console's warp steps beside the save point.
                    var start = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), StartMapName, true);
                    string[] parts = TestStart.Split('@');
                    Vector3[] door = DoorInto(start, parts.Length > 1 ? parts[1].Trim() : null);
                    if (door != null)
                    {
                        MainManager.instance.StartCoroutine(MainManager.TransferMap((int)start, MainManager.player.transform.position, door[1], door[2]));
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
