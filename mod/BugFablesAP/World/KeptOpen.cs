using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Keeps story blockers out of the way (and brings later entities in) for the lists in slot_data. CheckIfCanExist
    // isn't told which entity asks, but each entity's limit/requires array is its own, so a marker array identifies it.
    internal static class KeptOpen
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static readonly HashSet<int[]> markers = new HashSet<int[]>();
        // Marker `requires` arrays answering "exists"; set before the entity's own Start, which would switch it off.
        private static readonly HashSet<int[]> presentMarkers = new HashSet<int[]>();
        // Marker `requires` arrays of entities tied to one of the mod's key items: the item, and the requires the game
        // checks once it is in the bag (none, or the entity's own).
        private static readonly Dictionary<int[], KeyValuePair<int, int[]>> itemMarkers =
            new Dictionary<int[], KeyValuePair<int, int[]>>();

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (!Hooks.Install(typeof(KeptOpen), "open", "blockers the seed keeps open will still block"))
            {
                return;
            }
            Hooks.Install(typeof(NewEntities), "open", "a kept-present shopkeeper's shop is built without it");
            Hooks.Install(typeof(Insides), "open", "an entity kept away flashes when entering or leaving a house");
            Hooks.Install(typeof(ShopGoods), "open", "a kept-away shopkeeper's goods stay laid out");
            bool scenery = Hooks.Install(typeof(Scenery), "open",
                "scenery the seed removes (the Outskirts rocks) will stay");
            log.LogInfo($"[open] installed on MapControl.CreateEntities and MainManager.CheckIfCanExist{(scenery ? " and ConditionChecker.Start" : "")}");
        }

        // A map loaded before slot_data arrived was built as vanilla, so new lists are applied to it too.
        private static object appliedFor;

        internal static void Tick()
        {
            object lists = connection?.KeptOpen;
            MapControl map = MainManager.map;
            if (lists == null || ReferenceEquals(lists, appliedFor) || map == null || randomizerOn == null
                || !randomizerOn())
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
                // Hides it the way ConditionChecker.Start does.
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

        // A kept-present shopkeeper must exist while CreateEntities builds its shop slots, before AfterCreate runs;
        // so the entity just made is remembered and a check with its own requires array answers "exists".
        private static bool creating;
        private static EntityControl lastMade;
        private static string creatingMap;

        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPrefix]
        private static void BeforeCreate(MapControl __instance)
        {
            creating = true;
            lastMade = null;
            creatingMap = DayNight.EntityMap(__instance.mapid.ToString());
        }

        private static class NewEntities
        {
            [HarmonyPatch(typeof(EntityControl), nameof(EntityControl.CreateNewEntity), typeof(string))]
            [HarmonyPostfix]
            private static void AfterNewEntity(EntityControl __result)
            {
                if (creating)
                {
                    lastMade = __result;
                }
            }
        }

        private static bool KeptPresentHere(string name)
        {
            List<ApConnection.Blocker> present = connection?.KeptPresent;
            return present != null && creatingMap != null && present.Any(b => b.Map == creatingMap && b.Entity == name);
        }

        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPostfix]
        private static void AfterCreate(MapControl __instance)
        {
            creating = false;
            lastMade = null;
            List<ApConnection.Blocker> blockers = connection?.KeptOpen;
            if (blockers == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            string map = DayNight.EntityMap(__instance.mapid.ToString());
            foreach (ApConnection.Blocker blocker in blockers.Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == blocker.Entity))
                {
                    var marker = new[] { -1 };
                    markers.Add(marker);
                    npc.limit = marker;
                    if (npc.entity != null)
                    {
                        npc.entity.iskill = true;
                    }
                    log.LogInfo($"[open] {map}: {npc.name} kept out of the way (the seed keeps this area open)");
                    // A shopkeeper's goods are laid out after every entity exists, each pointing back to it.
                    foreach (NPCControl good in __instance.GetComponentsInChildren<NPCControl>(true)
                        .Where(g => g.shopkeeper == npc && g.entity != null))
                    {
                        good.entity.iskill = true;
                        good.gameObject.SetActive(false);
                        log.LogInfo($"[open] {map}: {good.name}, {npc.name}'s goods, kept out of the way");
                    }
                }
            }
            foreach (ApConnection.Blocker way in (connection.KeptPresent ?? new List<ApConnection.Blocker>())
                .Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == way.Entity))
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
            // A one-time pickup whose check the server has is kept away in every save; a respawning one is the game's
            // own again and a story one starts its scene, so both stay.
            foreach (KeyValuePair<long, ApConnection.Pickup> found in
                (connection.LocationPickups ?? new Dictionary<long, ApConnection.Pickup>())
                .Where(p => p.Value.Map == map && p.Value.Regional < 0 && p.Value.Event < 0
                    && connection.IsDone(p.Key)))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.objecttype == NPCControl.ObjectTypes.Item && ItemSwap.IsPickup(found.Value, n)))
                {
                    KeepAway(npc);
                    log.LogInfo($"[open] {map}: {npc.name} kept away (location {found.Key} is already checked)");
                }
            }
            // present_from: the entity's requires replaced by an earlier story flag, so the game makes it from then on.
            foreach (ApConnection.Blocker from in (connection.PresentFrom ?? new List<ApConnection.Blocker>())
                .Where(b => b.Map == map && b.Flag >= 0))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == from.Entity))
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
            // dialogue_flags: an entity picks the last line whose flag is set; repoint one line's flag.
            foreach (ApConnection.FlagSwap swap in
                (connection.DialogueFlags ?? new List<ApConnection.FlagSwap>()).Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == swap.Entity && n.dialogues != null))
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
            // activation_flags: before the entity's own Start reads it (a switch hit while its flag is set) or a cut grass
            // writes it, so a borrowed goal flag is never set.
            foreach (ApConnection.FlagSwap swap in
                (connection.ActivationFlags ?? new List<ApConnection.FlagSwap>()).Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == swap.Entity && n.activationflag == swap.From))
                {
                    npc.activationflag = swap.To;
                    log.LogInfo($"[open] {map}: {npc.name}'s activation flag now {swap.To} instead of {swap.From}");
                }
            }
            // limit_flags: one limit flag repointed, and whether the entity exists asked again, as CreateEntities did.
            foreach (ApConnection.FlagSwap swap in
                (connection.LimitFlags ?? new List<ApConnection.FlagSwap>()).Where(b => b.Map == map))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == swap.Entity && n.limit != null && n.limit.Contains(swap.From)
                        && !markers.Contains(n.limit)))
                {
                    npc.limit = npc.limit.Select(f => f == swap.From ? swap.To : f).ToArray();
                    bool hidden = MainManager.CheckIfCanExist(npc.requires, npc.limit, npc.regionalflag);
                    if (npc.entity != null)
                    {
                        npc.entity.iskill = hidden;
                    }
                    log.LogInfo($"[open] {map}: {npc.name} now there until flag {swap.To} instead of {swap.From} ({(hidden ? "set: kept away" : "not set: present")})");
                }
            }
            // held_until: added to the entity's requires (never replacing them), so the game keeps it away until then.
            foreach (ApConnection.Blocker held in (connection.HeldUntil ?? new List<ApConnection.Blocker>())
                .Where(b => b.Map == map && b.Flag >= 0))
            {
                foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true)
                    .Where(n => n.name == held.Entity))
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
            // present_with_item: made with the key item in the bag, whatever its own requires; held_until_item: kept
            // away until the key item, on top of its own requires.
            foreach (ApConnection.Blocker with in (connection.PresentWithItem ?? new List<ApConnection.Blocker>())
                .Where(b => b.Map == map && b.Item >= 0))
            {
                TieToItem(__instance, map, with, false);
            }
            foreach (ApConnection.Blocker until in (connection.HeldUntilItem ?? new List<ApConnection.Blocker>())
                .Where(b => b.Map == map && b.Item >= 0))
            {
                TieToItem(__instance, map, until, true);
            }
        }

        private static void TieToItem(MapControl map, string mapName, ApConnection.Blocker tie, bool keepOwn)
        {
            foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true).Where(n => n.name == tie.Entity))
            {
                int[] own = keepOwn ? npc.requires ?? new int[0] : new int[0];
                var marker = new[] { -1 };
                itemMarkers[marker] = new KeyValuePair<int, int[]>(tie.Item, own);
                npc.requires = marker;
                bool hidden = MainManager.CheckIfCanExist(marker, npc.limit, npc.regionalflag);
                if (npc.entity != null)
                {
                    npc.entity.iskill = hidden;
                }
                log.LogInfo($"[open] {mapName}: {npc.name} {(keepOwn ? "held until" : "present with")} key item {tie.Item} ({(HasKeyItem(tie.Item) ? "in the bag" : "not in the bag")}: {(hidden ? "kept away" : "present")})");
            }
        }

        private static bool HasKeyItem(int id)
        {
            List<int>[] items = MainManager.instance?.items;
            return items != null && items.Length > 1 && items[1].Contains(id);
        }

        // scenery_hidden: a marker limit before ConditionChecker.Start, so its own check answers "hide".
        private static class Scenery
        {
            [HarmonyPatch(typeof(ConditionChecker), "Start")]
            [HarmonyPrefix]
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

        // Hidden by the mod now and on every later rebuild of the map: the game's own existence check answers "gone".
        // Made to exist whatever its requires and limit, as the kept-present list does (Enemysanity's enemies).
        internal static void KeepPresent(NPCControl npc)
        {
            var marker = new[] { -1 };
            presentMarkers.Add(marker);
            npc.requires = marker;
            if (npc.entity != null)
            {
                npc.entity.iskill = false;
            }
        }

        internal static void KeepAway(NPCControl npc)
        {
            var marker = new[] { -1 };
            markers.Add(marker);
            npc.limit = marker;
            if (npc.entity != null)
            {
                npc.entity.iskill = true;
            }
        }

        // A shopkeeper's goods ask nothing of their own when they start: those of a kept-away shopkeeper go too.
        private static class ShopGoods
        {
            [HarmonyPatch(typeof(NPCControl), "Start")]
            [HarmonyPostfix]
            private static void AfterStart(NPCControl __instance)
            {
                NPCControl keeper = __instance.shopkeeper;
                if (__instance.interacttype != NPCControl.Interaction.Shop || keeper == null || keeper.limit == null
                    || !markers.Contains(keeper.limit) || randomizerOn == null || !randomizerOn())
                {
                    return;
                }
                if (__instance.entity != null)
                {
                    __instance.entity.iskill = true;
                }
                __instance.gameObject.SetActive(false);
                log.LogInfo($"[open] {__instance.name}, {keeper.name}'s goods, kept out of the way as they start");
            }
        }

        // Entering or leaving a house turns that inside's entities on without asking whether they exist: the ones kept
        // away go off again in the same frame, before anything is drawn.
        private static class Insides
        {
            [HarmonyPatch(typeof(MapControl), nameof(MapControl.RefreshInsides))]
            [HarmonyPostfix]
            private static void AfterRefreshInsides(MapControl __instance)
            {
                if (__instance.entities == null || randomizerOn == null || !randomizerOn())
                {
                    return;
                }
                foreach (EntityControl entity in __instance.entities)
                {
                    if (entity != null && entity.npcdata != null && entity.npcdata.limit != null
                        && markers.Contains(entity.npcdata.limit) && entity.gameObject.activeSelf)
                    {
                        entity.gameObject.SetActive(false);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.CheckIfCanExist), typeof(int[]), typeof(int[]),
            typeof(int))]
        [HarmonyPrefix]
        private static bool BeforeCheck(ref int[] requires, int[] limit, ref bool __result)
        {
            if (requires != null && itemMarkers.TryGetValue(requires, out KeyValuePair<int, int[]> tied))
            {
                if (randomizerOn == null || !randomizerOn() || !HasKeyItem(tied.Key))
                {
                    __result = true;
                    return false;
                }
                // With the item, the game's own check on what the marker stands for.
                requires = tied.Value;
                return true;
            }
            if (creating && requires != null && lastMade != null && lastMade.npcdata != null
                && ReferenceEquals(requires, lastMade.npcdata.requires)
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
