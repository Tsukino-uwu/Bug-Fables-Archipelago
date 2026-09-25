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
            harmony.Patch(create, prefix: new HarmonyMethod(typeof(KeptOpen), nameof(BeforeCreate)), postfix: new HarmonyMethod(typeof(KeptOpen), nameof(AfterCreate)));
            var made = AccessTools.Method(typeof(EntityControl), nameof(EntityControl.CreateNewEntity), new[] { typeof(string) });
            if (made != null)
            {
                harmony.Patch(made, postfix: new HarmonyMethod(typeof(KeptOpen), nameof(AfterNewEntity)));
            }
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

        // A shopkeeper kept present needs its shop slots, which the map builds inside CreateEntities, right after reading
        // that keeper, only if the keeper exists by then (MapControl.cs:1708-1745): the marker set in AfterCreate comes too
        // late. So while the map builds, the entity just made is remembered (every entity starts as CreateNewEntity(name),
        // MapControl.cs:1466), and a check made with that entity's own requires array answers "exists" when it's listed.
        // The caravan (the user, 2026-09-25).
        private static bool creating;
        private static EntityControl lastMade;
        private static string creatingMap;

        private static void BeforeCreate(MapControl __instance)
        {
            creating = true;
            lastMade = null;
            creatingMap = __instance.mapid.ToString();
        }

        private static void AfterNewEntity(EntityControl __result)
        {
            if (creating)
            {
                lastMade = __result;
            }
        }

        private static bool KeptPresentHere(string name)
        {
            List<ApConnection.Blocker> present = connection?.KeptPresent;
            return present != null && creatingMap != null && present.Any(b => b.Map == creatingMap && b.Entity == name);
        }

        private static void AfterCreate(MapControl __instance)
        {
            creating = false;
            lastMade = null;
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
            // present_from: the entity's requires replaced by an earlier story flag, so the game makes it from then on.
            foreach (ApConnection.Blocker from in (connection.PresentFrom ?? new List<ApConnection.Blocker>()).Where(b => b.Map == map && b.Flag >= 0))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true).Where(n => n.name == from.Entity))
                {
                    npc.requires = new[] { from.Flag };
                    bool now = MainManager.instance.flags[from.Flag];
                    if (npc.entity != null && now)
                    {
                        npc.entity.iskill = false;
                    }
                    log.LogInfo($"[open] {map}: {npc.name} present from flag {from.Flag} ({(now ? "set: present" : "not set yet")})");
                }
            }
            // dialogue_flags: an entity picks the last of its lines whose flag is set (NPCControl.cs:4320-4326); one entry's
            // flag is repointed, so that line answers to another flag.
            foreach (ApConnection.DialogueFlag swap in (connection.DialogueFlags ?? new List<ApConnection.DialogueFlag>()).Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true).Where(n => n.name == swap.Entity && n.dialogues != null))
                {
                    for (int d = 0; d < npc.dialogues.Length; d++)
                    {
                        if ((int)npc.dialogues[d].x == swap.From)
                        {
                            npc.dialogues[d].x = swap.To;
                            log.LogInfo($"[open] {map}: {npc.name}'s line {(int)npc.dialogues[d].y} now answers to flag {swap.To} instead of {swap.From}");
                        }
                    }
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
            // scenery_present: the other way round, a marker requires answering "exists" (the caravan's stall).
            List<ApConnection.Blocker> shown = connection?.SceneryPresent;
            if (randomizerOn != null && randomizerOn() && shown != null && MainManager.map != null)
            {
                string map = MainManager.map.mapid.ToString();
                string path = PathOf(__instance.transform, MainManager.map.transform);
                if (shown.Any(b => b.Map == map && b.Entity == path))
                {
                    var marker = new[] { -1 };
                    presentMarkers.Add(marker);
                    __instance.requires = marker;
                    log.LogInfo($"[open] {map}: scenery {path} shown (the seed shows it from the start)");
                }
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
            if (creating && requires != null && lastMade != null && lastMade.npcdata != null && ReferenceEquals(requires, lastMade.npcdata.requires)
                && randomizerOn != null && randomizerOn() && KeptPresentHere(lastMade.name))
            {
                __result = false;
                return false;
            }
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
