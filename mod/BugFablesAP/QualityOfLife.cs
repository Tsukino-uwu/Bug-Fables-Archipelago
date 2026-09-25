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
            internal int OnlyWhileUnset = -1; // skipped only while this flag is unset (a scene with a later, needed part)
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
            // The barkeeper's first talk (the user, 2026-09-25: it played with Leif's stand-in, naming Leif): camera and party
            // moves, one line, then flag 158 (EventControl.cs:13052-13080). The same scene later takes bounties and gives
            // their rewards (the else branch), so it's skipped only while flag 158 is unset.
            new Scene { Map = "UndergroundBar", Event = 83, Flags = new[] { 158 }, OnlyWhileUnset = 158 },
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
            FreeBoat = config.Bind("QualityOfLife", "FreeBoat", true,
                "The boat to Metal Island costs nothing (the user, 2026-09-25: no farming berries in Archipelago).");
            SkipCutscenes = config.Bind("QualityOfLife", "SkipCutscenes", true,
                "The new game's intro (its story slides, the talk after them, Maki's talk and the tutorial battle) is skipped: "
                + "Vi joins and the first check is sent. Other scenes you don't need to watch are skipped or pass by fast (a list "
                + "that grows scene by scene).");
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
        private static bool openingFailed; // one try per session: a failure is logged, never retried every frame
        // Dev only ([Debug] TestStart): a map the opening ends with a warp to, a stand-in for a random start.
        internal static string TestStart;
        private static bool startPending;
        // With a test start, the slides' black backdrop stays up from Event8's cut until the start map has loaded behind the
        // transfer's own fade (the user, 2026-09-25: the house showed between the scene's end and the warp).
        private static GameObject heldBack;
        private static bool transferring;
        private static float heldSince;

        private static bool TestStartSet => !string.IsNullOrEmpty(TestStart);
        // "Map" or "Map@FromMap": the start map, and optionally the map whose door into it the party arrives through.
        private static string StartMapName => TestStart.Split('@')[0].Trim();

        // Where the opening runs: the start map (a test start's, else the starting building's).
        private static bool AtStart(string map) =>
            map != null && (TestStartSet ? string.Equals(map, StartMapName, StringComparison.OrdinalIgnoreCase) : map == OpeningMap);

        // Arriving as if through a door (the user, 2026-09-25: position zero put the party at the plaza's origin, behind
        // its statue). A door holds its target: data[0] the map, vectordata[1] where the party appears, vectordata[2]
        // where it then walks (NPCControl.cs:5461). The door lies on the map left behind, so it's read from that map's
        // entity table (Data/EntityData/<map>, fields split by '}'), at the positions MapControl.CreateEntities reads
        // (MapControl.cs:1540-1566, as EntityDump): the data count at 60, the vectordata count at 71, each followed by
        // its values (vectors as x, y, z). Null when no door leads there.
        private static Vector3[] DoorInto(MainManager.Maps target, string fromMap)
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
                    log.LogInfo($"[qol] test start: arriving through {map}'s door (entity {i}): appear at {v[1]}, walk to {v[2]}");
                    return v;
                }
            }
            return null;
        }

        // Event8's talk after the slides, cut (the user, 2026-09-25: hiding it "looks dumb"; skip it and warp before any
        // dialogue). Its first step after the slides' backdrop is destroyed is ChangeParty({1}, fromscratch, keep the old
        // entities), Kabbu alone (EventControl.cs:2740-2755); Vi is still in the party then. A prefix refuses that call and
        // stops the scene (scenes run as StartCoroutine("Event" + id), EventControl.cs:143); the rest of that step still
        // runs (the HUD hidden, the blockingbox, the exit hidden, two moves) and the scene stops at its next yield. On the
        // next frame the mod ends it as its own end does (EventControl.cs:2858-2866): HUD back, ResetCamera, the
        // building's music, EndEvent, and the fade-in the talk would have played. The opening then follows as before.
        private static bool event8Cut;

        private static bool BeforeChangeParty(int[] ids, bool fromscratch, bool destroyoldentity)
        {
            MainManager mm = MainManager.instance;
            if (randomizerOn == null || !randomizerOn() || !SkipCutscenes.Value || mm == null || MainManager.map == null
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

        // With Skip cutscenes (Skip intro folded in, the user, 2026-09-25), the cut comes before the slides (the user, 2026-09-25: still saw them): their first step is
        // their black backdrop, NewSolidColor("back"), after the building's map has loaded (EventControl.cs:2655). The
        // scene stops there; the backdrop is made and parented in that same step, and removed with the scene's end.
        private static void BeforeSolidColor(string name)
        {
            MainManager mm = MainManager.instance;
            if (name != "back" || randomizerOn == null || !randomizerOn() || !SkipCutscenes.Value || mm == null || MainManager.map == null
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
            if (back != null && TestStartSet)
            {
                heldBack = back.gameObject;
                heldSince = Time.realtimeSinceStartup;
            }
            else if (back != null)
            {
                UnityEngine.Object.Destroy(back.gameObject);
            }
            mm.hud[0].transform.parent.gameObject.SetActive(true);
            // Stand the party where Event8 would have, after its slides: Kabbu 2.5 to the left of entity 4
            // (EventControl.cs:2770). Cut before the slides, the party stood where a new game spawns it, under the house (the
            // user, 2026-09-25, seen once the test start's warp no longer moved it away). Done here, before the fade-in:
            // done in RunOpening, the fade-in first showed the spawn point, then a jump.
            EntityControl four = !TestStartSet && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap
                ? MainManager.GetEntity(4) : null;
            if (four != null && mm.playerdata != null)
            {
                Vector3 spot = four.transform.position + Vector3.left * 2.5f;
                for (int i = 0; i < mm.playerdata.Length; i++)
                {
                    if (mm.playerdata[i].entity != null)
                    {
                        mm.playerdata[i].entity.transform.position = spot + new Vector3(-0.6f * i, 0f, 0.1f * i);
                    }
                }
            }
            MainManager.ResetCamera();
            if (four != null && MainManager.player != null)
            {
                MainManager.MainCamera.transform.position = MainManager.player.transform.position + mm.camoffset;
            }
            // The building's music only when the game starts there: with a test start it played briefly before the start
            // map's own (the user, 2026-09-25).
            if (!TestStartSet)
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
            else
            {
                MainManager.PlayTransition(1, 0, 0.02f, Color.black);
            }
            log.LogInfo($"[qol] Event8 ended the game's way; inevent={mm.inevent}");
        }

        // Shops' "see more medals" first (the user, 2026-09-25: faster to reshuffle). A shopkeeper's greeting ends in
        // |prompt,map,Y,N,target1..targetN,text1..textN| (MainManager.cs:12213-12222); the reshuffle is the choice with target
        // -199 and text -195 (Shades's line 1, Merab's line 34). That choice and its text move to the front, in the map's
        // dialogue table in memory, once per map load (a reload reads the table afresh), only with Archipelago on.
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

        private static void RunOpening()
        {
            MainManager mm = MainManager.instance;
            // Where the player stands: EndEvent8 put the party where Event8 would have, before the fade-in. Placing it here
            // too snapped the player back, since this runs once the fade-in is over and the player can walk during it
            // (the user, 2026-09-25).
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
                    log.LogInfo($"[qol] opening: no {name} (Event8 cut before making it)");
                }
            }
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            // The building's own entities, only when the opening runs there: elsewhere these numbers are other things.
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
            mm.flags[15] = true;
            mm.boardquests[1].Insert(0, 11);
            HoldUps.FoundAt(OpeningLocation, "the opening's gift (location 1)");
            log.LogInfo($"[qol] opening done without Event16 on {MainManager.map.mapid}: party {string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())}, "
                + $"characters {mm.playerdata.Count(p => p.entity != null)}, exit {(exit != null ? "active " + exit.gameObject.activeSelf : inBuilding ? "NOT found" : "not here")}, flag 15 {mm.flags[15]}");
        }

        // Every scene starts here (EventControl.cs:74). A listed scene to skip gets its flags and never starts.
        private static bool BeforeStartEvent(int id)
        {
            if (id == OpeningEvent && SkipCutscenes.Value && randomizerOn() && MainManager.map != null
                && MainManager.map.mapid.ToString() == OpeningMap)
            {
                // Also once the opening is done: its trigger stays in the room until the map reloads (limit 15 is read on
                // load), and the scene run after the mod's opening crashed looking for Vi's stand-in (2026-09-25).
                openingPending = !MainManager.instance.flags[15];
                endEvent?.Invoke(null, null);
                log.LogInfo(openingPending ? "[qol] Event16 (the opening) skipped: the mod does what it leaves behind on the next free frame"
                    : "[qol] Event16 (the opening) refused: already done");
                return false;
            }
            Scene scene = SceneFor(id);
            if (scene == null || scene.Flags == null || !SkipCutscenes.Value || !randomizerOn()
                || (scene.OnlyWhileUnset >= 0 && MainManager.instance.flags[scene.OnlyWhileUnset]))
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
            // Not only when its trigger is walked into: as soon as the player is free on the map with flag 15 unset (the user,
            // 2026-09-25, stood by Maki waiting, the trigger looking like the scene's start). Also covers a file saved there.
            if (on && SkipCutscenes.Value && !openingPending && !openingFailed && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap
                && !mm.flags[15] && mm.flags[691])
            {
                openingPending = true;
                startPending = TestStartSet && !transferring;
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
            // The test start: straight from the cut scene's end, before anything else shows.
            if (startPending && MainManager.player != null && !mm.inevent && !mm.message && MainManager.battle == null)
            {
                startPending = false;
                transferring = true;
                try
                {
                    // The game's own map transfer to where the map puts an arriving party; not the console's warp, which
                    // then stepped beside the save point, a second move (the user, 2026-09-25).
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
            bool slides = on && ((SkipCutscenes.Value && InIntroSlides()) || InFastScene());
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
