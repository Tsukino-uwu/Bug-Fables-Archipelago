using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The festival night at will (slot_data day_night): on those maps the mod keeps its own night, answers the story's
    // night flags for each existence check without writing them, and loads the night or day version of a room to
    // match. A map's switch NPC (time_switches) offers nightfall or morning; the first nightfall is the story's scene.
    internal static class DayNight
    {
        internal sealed class Pair
        {
            internal string Day;
            internal string Night;
            internal int From;
            internal int Until;
            internal int FirstEvent;
        }

        internal sealed class Switch
        {
            internal string Map;
            internal string Entity;
            internal Vector3 At;
            // What it says, then its staying and switching choices (by day, at night); none for a plain moved entity.
            internal string[] Day;
            internal string[] Night;
            // Another map's entity whose data row is added to this map's, named Entity (the map has none of its own).
            internal string CopyMap;
            internal string CopyEntity;
        }

        internal sealed class Camera
        {
            internal string Map;
            internal int Event;
            internal Vector3 From;
            internal Vector3 To;
        }

        internal sealed class Move
        {
            internal string Map;
            internal string Entity;
            internal Vector3 Local;
        }

        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;

        // The mod's night, this session only; null until a day/night map loads (then the story's own state).
        private static bool? night;
        private static Pair swappedPair;
        private static bool savedFrom;
        private static bool savedUntil;

        private static List<Pair> Pairs => seed?.Invoke()?.DayNightMaps;

        // The dev console's trial spots for a map's switch ("map/entity"), this session only; the seed's spot otherwise.
        internal static readonly Dictionary<string, Vector3> TrialSpots = new Dictionary<string, Vector3>();

        // The dev console's switchhere: this map's switch moved to the player now, and kept there for the session.
        internal static string MoveSwitchHere()
        {
            if (MainManager.map == null || MainManager.player == null)
            {
                return "switchhere: no map or player";
            }
            string map = EntityMap(MainManager.map.mapid.ToString());
            Switch sw = seed?.Invoke()?.TimeSwitches?.FirstOrDefault(s => s.Map == map);
            NPCControl npc = sw == null ? null : MainManager.map.GetComponentsInChildren<NPCControl>(true)
                .FirstOrDefault(n => n.name == sw.Entity);
            if (npc == null)
            {
                return "switchhere: no switch on " + map;
            }
            Vector3 here = MainManager.player.transform.position;
            TrialSpots[map + "/" + sw.Entity] = here;
            npc.transform.position = here;
            if (npc.entity != null)
            {
                npc.entity.startpos = here;
            }
            return $"switchhere: {map}'s {sw.Entity} at {here} for this session";
        }

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            if (Hooks.Install(typeof(DayNight), "night", "the festival night stays the story's"))
            {
                log.LogInfo("[night] installed on MainManager.CheckIfCanExist, LoadMap and SetVariables, "
                    + "MapControl.CreateEntities and EventControl.StartEvent");
            }
        }

        private static bool On => randomizerOn != null && randomizerOn() && Pairs != null && Pairs.Count > 0;

        private static Pair PairOf(string map) => Pairs?.FirstOrDefault(p => p.Day == map || p.Night == map);

        // The map whose entity lists a map reads: a night map has its day map's entities (readdatafromothermap), so
        // the seed names them under the day map (kept entities, pickups, gives, shops, the detector). Scenery is each
        // map's own.
        internal static string EntityMap(string map) => Pairs?.FirstOrDefault(p => p.Night == map)?.Day ?? map;

        private static bool StoryNight(Pair p)
        {
            bool[] flags = MainManager.instance.flags;
            return flags[p.From] && !flags[p.Until];
        }

        private static bool NightFor(Pair p)
        {
            if (!night.HasValue)
            {
                night = StoryNight(p);
                log.LogInfo($"[night] the session starts at {(night.Value ? "night" : "day")}, as the story has it");
            }
            return night.Value;
        }

        // A new file or the title screen: the next day/night map starts from the story's state again.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.SetVariables))]
        [HarmonyPostfix]
        private static void AfterSetVariables()
        {
            night = null;
        }

        // For the length of one check: the night flag set and the after-festival flag cleared at night; by day both
        // follow whether the festival is over. The save's own values come back in the finalizer.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.CheckIfCanExist), typeof(int[]), typeof(int[]),
            typeof(int))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void BeforeCheck(out bool __state)
        {
            __state = false;
            if (!On || MainManager.map == null || swappedPair != null)
            {
                return;
            }
            Pair p = PairOf(MainManager.map.mapid.ToString());
            if (p == null)
            {
                return;
            }
            bool[] flags = MainManager.instance.flags;
            bool atNight = NightFor(p);
            savedFrom = flags[p.From];
            savedUntil = flags[p.Until];
            swappedPair = p;
            __state = true;
            flags[p.From] = atNight || savedUntil;
            flags[p.Until] = !atNight && savedUntil;
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.CheckIfCanExist), typeof(int[]), typeof(int[]),
            typeof(int))]
        [HarmonyFinalizer]
        private static void AfterCheck(bool __state)
        {
            if (!__state || swappedPair == null)
            {
                return;
            }
            bool[] flags = MainManager.instance.flags;
            flags[swappedPair.From] = savedFrom;
            flags[swappedPair.Until] = savedUntil;
            swappedPair = null;
        }

        // Every way into a day/night room (a door, a warp, a scene) lands in the version the mod's night asks for.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.LoadMap), typeof(int))]
        [HarmonyPrefix]
        private static void BeforeLoadMap(ref int id)
        {
            if (!On || !Enum.IsDefined(typeof(MainManager.Maps), id))
            {
                return;
            }
            string name = ((MainManager.Maps)id).ToString();
            Pair p = PairOf(name);
            if (p == null)
            {
                return;
            }
            string want = NightFor(p) ? p.Night : p.Day;
            if (want != name)
            {
                id = (int)(MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), want);
                log.LogInfo($"[night] {name} asked, {want} loaded (the mod's {(night.Value ? "night" : "day")})");
            }
        }

        // CreateEntities reads a map's entity rows and their names (Data/EntityData/<id> and its names file), split by
        // line: a switch copied from another map gets that entity's row and name added to both, before the trailing
        // empty line, so the game builds it like any other.
        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditCreate, "night");

        private static IEnumerable<CodeInstruction> EditCreate(List<CodeInstruction> code)
        {
            MethodInfo split = AccessTools.Method(typeof(string), nameof(string.Split), new[] { typeof(char[]) });
            MethodInfo add = AccessTools.Method(typeof(DayNight), nameof(AddCopies))
                ?? throw new MissingMethodException(nameof(DayNight), nameof(AddCopies));
            int done = 0;
            for (int i = 0; i < code.Count && done < 2; i++)
            {
                if (code[i].Calls(split))
                {
                    code.InsertRange(i + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldc_I4, done),
                        new CodeInstruction(OpCodes.Call, add),
                    });
                    done++;
                }
            }
            log.LogInfo($"[night] {(done == 2 ? "installed" : "NOT installed")} in MapControl.CreateEntities ({done} of 2 splits: copied switches)");
            return code;
        }

        // names: 0 the rows, 1 their names.
        private static string[] AddCopies(string[] lines, MapControl map, int names)
        {
            if (randomizerOn == null || !randomizerOn() || map == null || lines == null)
            {
                return lines;
            }
            string here = EntityMap(map.mapid.ToString());
            List<Switch> copies = (seed?.Invoke()?.TimeSwitches ?? new List<Switch>())
                .Where(s => s.Map == here && s.CopyMap != null).ToList();
            if (copies.Count == 0)
            {
                return lines;
            }
            List<string> result = lines.ToList();
            foreach (Switch copy in copies)
            {
                int from = (int)(MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), copy.CopyMap);
                string[] sourceNames = Resources.Load<TextAsset>("Data/EntityData/Names/" + from + "names")?.ToString()
                    .Split('\n');
                string[] sourceRows = Resources.Load<TextAsset>("Data/EntityData/" + from)?.ToString().Split('\n');
                int row = sourceNames == null ? -1 : Array.FindIndex(sourceNames, n => n.Trim() == copy.CopyEntity);
                if (row < 0 || sourceRows == null || row >= sourceRows.Length)
                {
                    log.LogWarning($"[night] {map.mapid}: {copy.CopyEntity} NOT FOUND in {copy.CopyMap}'s entities: no switch copied");
                    continue;
                }
                result.Insert(Math.Max(0, result.Count - 1), names == 1 ? copy.Entity : sourceRows[row]);
                if (names == 1)
                {
                    log.LogInfo($"[night] {map.mapid}: {copy.CopyMap}'s {copy.CopyEntity} added as {copy.Entity}");
                }
            }
            return result.ToArray();
        }

        // On load: scenery set where a scene would leave it, and the map's switch NPC moved to its spot with the
        // nightfall or morning prompt as its only talk.
        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPostfix]
        private static void AfterCreate(MapControl __instance)
        {
            if (randomizerOn == null || !randomizerOn() || __instance == null)
            {
                return;
            }
            string map = __instance.mapid.ToString();
            foreach (Move move in (seed?.Invoke()?.SceneryMoved ?? new List<Move>()).Where(m => m.Map == map))
            {
                Transform t = __instance.transform.Find(move.Entity);
                if (t != null)
                {
                    t.localPosition = move.Local;
                }
                log.LogInfo($"[night] {map}: {move.Entity} {(t != null ? "set to " + move.Local : "NOT FOUND")}");
            }
            string entityMap = EntityMap(map);
            foreach (Switch moved in (seed?.Invoke()?.EntitiesMoved ?? new List<Switch>()).Where(m => m.Map == entityMap))
            {
                NPCControl npc = __instance.GetComponentsInChildren<NPCControl>(true)
                    .FirstOrDefault(n => n.name == moved.Entity);
                if (npc != null)
                {
                    npc.transform.position = moved.At;
                    if (npc.entity != null)
                    {
                        npc.entity.startpos = moved.At;
                    }
                }
                log.LogInfo($"[night] {map}: {moved.Entity} {(npc != null ? "stands at " + moved.At : "NOT FOUND")}");
            }
            Pair p = PairOf(map);
            if (p == null || !On)
            {
                return;
            }
            bool atNight = NightFor(p);
            foreach (Switch sw in (seed?.Invoke()?.TimeSwitches ?? new List<Switch>()).Where(s => s.Map == p.Day))
            {
                NPCControl npc = __instance.GetComponentsInChildren<NPCControl>(true)
                    .FirstOrDefault(n => n.name == sw.Entity);
                if (npc == null || npc.entity == null)
                {
                    log.LogWarning($"[night] {map}: switch {sw.Entity} NOT FOUND: no day/night switch here");
                    continue;
                }
                Vector3 at = TrialSpots.TryGetValue(p.Day + "/" + sw.Entity, out Vector3 trial) ? trial : sw.At;
                npc.transform.position = at;
                npc.entity.startpos = at;
                npc.dialogues = new[] { new Vector3(-1f, AddPrompt(__instance, p, atNight ? sw.Night : sw.Day), 0f) };
                log.LogInfo($"[night] {map}: {sw.Entity} is the day/night switch at {at}{(at != sw.At ? " (a trial spot)" : "")} ({(atNight ? "morning" : "nightfall")} offered)");
            }
        }

        // The prompt as the game writes the nightfall one (GoldenSettlement1 line 19): its yes goes to a line of its
        // own that starts the story's nightfall event, which StartEvent turns into the switch once the story had one.
        private static int AddPrompt(MapControl map, Pair p, string[] words)
        {
            List<string> lines = (map.dialogues ?? new string[0]).ToList();
            int yes = lines.Count + 1;
            lines.Add($"{words[0]}|prompt,map,2.5,2,-11,{yes},@{words[1]},@{words[2]},none|");
            lines.Add($"|event,{p.FirstEvent}|");
            map.dialogues = lines.ToArray();
            return yes - 1;
        }

        // The nightfall event on a day/night map: the story's own scene the first time (its speech, its discovery, its
        // flag), the mod's switch every time after.
        [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
        [HarmonyPrefix]
        private static bool BeforeStartEvent(int id)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null)
            {
                return true;
            }
            string here = MainManager.map.mapid.ToString();
            Camera cam = seed?.Invoke()?.SceneCameras?.FirstOrDefault(c => c.Event == id && c.Map == EntityMap(here));
            if (cam != null)
            {
                MainManager.instance.StartCoroutine(MoveCamera(cam));
            }
            if (!On)
            {
                return true;
            }
            Pair p = PairOf(MainManager.map.mapid.ToString());
            if (p == null || id != p.FirstEvent)
            {
                return true;
            }
            if (!MainManager.instance.flags[p.From])
            {
                night = true;
                log.LogInfo($"[night] the first nightfall: the story's Event{id} plays");
                return true;
            }
            MainManager.instance.StartCoroutine(Flip(p));
            return false;
        }

        // While the scene runs, its fixed camera point is the moved one (it sets the point, then follows the player).
        private static IEnumerator MoveCamera(Camera cam)
        {
            MainManager mm = MainManager.instance;
            yield return null;
            bool moved = false;
            while (mm.inevent && MainManager.lastevent == cam.Event)
            {
                if (mm.camtargetpos.HasValue && (mm.camtargetpos.Value - cam.From).sqrMagnitude < 0.01f)
                {
                    mm.camtargetpos = cam.To;
                    moved = true;
                }
                yield return null;
            }
            log.LogInfo($"[night] Event{cam.Event}'s camera point {(moved ? "moved to " + cam.To : "never set")}");
        }

        // A fade, the other version of the room with the party where it stood, and the line's end done (its |event|
        // leaves minipause for a scene to clear).
        private static IEnumerator Flip(Pair p)
        {
            MainManager mm = MainManager.instance;
            while (mm.message)
            {
                yield return null;
            }
            MainManager.PlayTransition(0, 0, 0.02f, Color.black);
            yield return null;
            SpriteRenderer fade = MainManager.GetTransitionSprite();
            while (fade != null && fade.color.a < 0.95f)
            {
                yield return null;
            }
            Vector3[] spots = mm.playerdata.Select(d => d.entity != null ? d.entity.transform.position : Vector3.zero)
                .ToArray();
            night = !night.GetValueOrDefault();
            string to = night.Value ? p.Night : p.Day;
            log.LogInfo($"[night] switched to {(night.Value ? "night" : "day")}: loading {to}");
            MainManager.LoadMap((int)(MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), to), recreateplayers: true);
            yield return null;
            MainManager.ChangeMusic(MainManager.map.music[0]);
            for (int i = 0; i < mm.playerdata.Length && i < spots.Length; i++)
            {
                if (mm.playerdata[i].entity != null)
                {
                    mm.playerdata[i].entity.transform.position = spots[i];
                }
            }
            MainManager.ResetCamera(instant: true);
            yield return null;
            MainManager.PlayTransition(1, 0, 0.02f, Color.black);
            mm.minipause = false;
            mm.overridefollower = false;
            MainManager.EndOfMessage();
        }
    }
}
