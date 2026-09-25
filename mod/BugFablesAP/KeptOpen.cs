using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Areas that close later are kept open (the user, 2026-09-24, as Pokémon Emerald keeps Mirage Island visible): the
    // logic assumes a place stays reachable, so a blocker the story puts up for a while must not lock the player out
    // of locations behind it. The seed lists those blockers in slot_data ("kept_open": map and entity name); nothing
    // is hard-coded here.
    //
    // The game decides whether an entity exists with MainManager.CheckIfCanExist(requires, limit, regionalflag), which
    // returns "hide" (MainManager.cs:7762), called when a map loads, by the entity itself (NPCControl.cs:438) and every
    // other frame (MapControl.cs:920). It isn't told which entity it's asking about, but each entity's limit array is
    // its own. So after a map creates its entities, a listed blocker gets a marker array of its own, and any check made
    // with that array answers "hide". No save data is written; with the Archipelago mod disabled nothing is touched.
    internal static class KeptOpen
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static readonly HashSet<int[]> markers = new HashSet<int[]>();
        // The other way round (slot_data's kept_present): an entity the story only makes later gets a marker
        // `requires` array, and a check made with it answers "exists" (the user, 2026-09-25: no dead end in chapter 1,
        // open world by default). Set before the entity's own Start (NPCControl.cs:438), which would switch it off.
        private static readonly HashSet<int[]> presentMarkers = new HashSet<int[]>();

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            var create = AccessTools.Method(typeof(MapControl), "CreateEntities");
            var check = AccessTools.Method(typeof(MainManager), nameof(MainManager.CheckIfCanExist), new[] { typeof(int[]), typeof(int[]), typeof(int) });
            if (create == null || check == null)
            {
                log.LogError($"[open] NOT installed (CreateEntities {create != null}, CheckIfCanExist {check != null}): blockers the seed "
                    + "keeps open will still block.");
                return;
            }
            harmony = new Harmony(guid + ".open." + DateTime.UtcNow.Ticks);
            harmony.Patch(create, postfix: new HarmonyMethod(typeof(KeptOpen), nameof(AfterCreate)));
            harmony.Patch(check, prefix: new HarmonyMethod(typeof(KeptOpen), nameof(BeforeCheck)));
            var scenery = AccessTools.Method(typeof(ConditionChecker), "Start");
            if (scenery != null)
            {
                harmony.Patch(scenery, prefix: new HarmonyMethod(typeof(KeptOpen), nameof(BeforeSceneryStart)));
            }
            else
            {
                log.LogError("[open] ConditionChecker.Start not found: scenery the seed removes (the Outskirts rocks) will stay.");
            }
            log.LogInfo($"[open] installed on MapControl.CreateEntities and MainManager.CheckIfCanExist{(scenery != null ? " and ConditionChecker.Start" : "")}");
        }

        // The lists arrive with slot_data at login. A map loaded before that (the first map after a plugin reload or a
        // seed change, before the login) was built as vanilla: the user saw the Outskirts rocks back (2026-09-25).
        // So when a new set of lists arrives, it's applied to the map already loaded as well.
        private static object appliedFor;

        internal static void Tick()
        {
            object lists = connection?.KeptOpen;
            MapControl map = MainManager.map;
            if (lists == null || ReferenceEquals(lists, appliedFor) || map == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            appliedFor = lists;
            log.LogInfo($"[open] the seed's lists arrived with {map.mapid} already loaded: applying them to it");
            AfterCreate(map);
            foreach (ConditionChecker scenery in map.GetComponentsInChildren<ConditionChecker>(true))
            {
                if (!Listed(scenery) || !scenery.gameObject.activeSelf)
                {
                    continue;
                }
                MarkHidden(scenery, map.mapid.ToString());
                // What ConditionChecker.Start does to hide an object (ConditionChecker.cs:41-55).
                NPCControl data = scenery.GetComponent<NPCControl>();
                if (data != null && data.entity != null)
                {
                    data.entity.iskill = true;
                }
                else
                {
                    UnityEngine.Animator animator = scenery.GetComponent<UnityEngine.Animator>();
                    if (animator != null)
                    {
                        UnityEngine.Object.Destroy(animator);
                    }
                    scenery.transform.position = new UnityEngine.Vector3(0f, 9999f, 0f);
                    scenery.gameObject.SetActive(false);
                }
            }
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void AfterCreate(MapControl __instance)
        {
            List<ApConnection.Blocker> blockers = connection?.KeptOpen;
            if (blockers == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            string map = __instance.mapid.ToString();
            foreach (ApConnection.Blocker blocker in blockers.Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true).Where(n => n.name == blocker.Entity))
                {
                    var marker = new[] { -1 };
                    markers.Add(marker);
                    npc.limit = marker;
                    if (npc.entity != null)
                    {
                        npc.entity.iskill = true;
                    }
                    log.LogInfo($"[open] {map}: {npc.name} kept out of the way (the seed keeps this area open)");
                }
            }
            foreach (ApConnection.Blocker way in (connection.KeptPresent ?? new List<ApConnection.Blocker>()).Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true).Where(n => n.name == way.Entity))
                {
                    var marker = new[] { -1 };
                    presentMarkers.Add(marker);
                    npc.requires = marker;
                    if (npc.entity != null)
                    {
                        npc.entity.iskill = false;
                    }
                    log.LogInfo($"[open] {map}: {npc.name} made present (the seed keeps this way open)");
                }
            }
            // held_until: a real story flag as the entity's requires, so the game's own check keeps it away until then
            // and brings it back after. Only added to: an entity that already needs something keeps that too.
            foreach (ApConnection.Blocker held in (connection.HeldUntil ?? new List<ApConnection.Blocker>()).Where(b => b.Map == map && b.Flag >= 0))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true).Where(n => n.name == held.Entity))
                {
                    var requires = (npc.requires ?? new int[0]).Where(f => f >= 0).ToList();
                    if (!requires.Contains(held.Flag))
                    {
                        requires.Add(held.Flag);
                    }
                    npc.requires = requires.ToArray();
                    bool waiting = !MainManager.instance.flags[held.Flag];
                    if (npc.entity != null && waiting)
                    {
                        npc.entity.iskill = true;
                    }
                    log.LogInfo($"[open] {map}: {npc.name} held until flag {held.Flag} ({(waiting ? "not set: kept away" : "set: as the game has it")})");
                }
            }
        }

        // Scenery switched by flags (ConditionChecker, ConditionChecker.cs:37-57) decides at its own Start whether to
        // hide. A listed object (scenery_hidden: map plus its path inside the map, as MapDump writes it) gets a marker
        // limit first, so that check answers "hide": the Outskirts rocks, gone from flag 41 in the game, are gone from
        // the start (the user, 2026-09-25).
        private static void BeforeSceneryStart(ConditionChecker __instance)
        {
            if (randomizerOn != null && randomizerOn() && Listed(__instance))
            {
                MarkHidden(__instance, MainManager.map.mapid.ToString());
            }
        }

        private static bool Listed(ConditionChecker scenery)
        {
            List<ApConnection.Blocker> hidden = connection?.SceneryHidden;
            if (hidden == null || MainManager.map == null)
            {
                return false;
            }
            string map = MainManager.map.mapid.ToString();
            string path = PathOf(scenery.transform, MainManager.map.transform);
            return hidden.Any(b => b.Map == map && b.Entity == path);
        }

        private static void MarkHidden(ConditionChecker scenery, string map)
        {
            var marker = new[] { -1 };
            markers.Add(marker);
            scenery.limit = marker;
            log.LogInfo($"[open] {map}: scenery {PathOf(scenery.transform, MainManager.map.transform)} removed (the seed keeps this area open)");
        }

        private static string PathOf(UnityEngine.Transform t, UnityEngine.Transform root)
        {
            var parts = new List<string>();
            for (UnityEngine.Transform at = t; at != null && at != root; at = at.parent)
            {
                parts.Add(at.name);
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static bool BeforeCheck(int[] requires, int[] limit, ref bool __result)
        {
            if (limit != null && markers.Contains(limit))
            {
                __result = true;
                return false;
            }
            if (requires != null && presentMarkers.Contains(requires))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
