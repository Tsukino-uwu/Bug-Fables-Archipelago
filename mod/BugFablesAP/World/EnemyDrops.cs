using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Enemysanity (slot_data location_enemies): each listed map enemy is kept on its map whatever the story's flags, and
    // its death, while its check isn't done, drops a pickup made the game's way (EntityControl.CreateItem, as its own
    // held-key drop is). Taking it sends the check at once, as a respawning pickup's is: no save flag, the server says
    // what's done. ItemSwap shows the seed's item on it and in its line.
    internal static class EnemyDrops
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static FieldInfo deathState;
        private static FieldInfo deathOwner;
        // A dying enemy's location and where it stood, from its death's first step to its last.
        private static readonly Dictionary<object, KeyValuePair<long, Vector3>> dying =
            new Dictionary<object, KeyValuePair<long, Vector3>>();
        // Each drop on the map and its location.
        private static readonly Dictionary<NPCControl, long> drops = new Dictionary<NPCControl, long>();

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo death = AccessTools.Method(typeof(EntityControl), "Death", new[] { typeof(bool) });
            MethodInfo step = death != null ? AccessTools.EnumeratorMoveNext(death) : null;
            deathState = step != null ? AccessTools.Field(step.DeclaringType, "<>1__state") : null;
            deathOwner = step != null ? AccessTools.Field(step.DeclaringType, "<>4__this") : null;
            if (deathState == null || deathOwner == null)
            {
                log.LogError($"[enemies] Enemysanity NOT installed (Death's MoveNext {step != null}): no enemy drops.");
                return;
            }
            if (Hooks.Install(typeof(EnemyDrops), "enemysanity", "enemies drop no checks"))
            {
                log.LogInfo("[enemies] Enemysanity installed on MapControl.CreateEntities and EntityControl.Death's steps");
            }
        }

        private static bool On => randomizerOn != null && randomizerOn() && connection?.LocationEnemies != null
            && connection.LocationEnemies.Count > 0;

        private static long LocationFor(NPCControl npc)
        {
            if (npc == null || npc.entitytype != NPCControl.NPCType.Enemy || MainManager.map == null)
            {
                return -1;
            }
            string key = MainManager.map.mapid + ":" + npc.mapid;
            foreach (KeyValuePair<long, string> entry in connection.LocationEnemies)
            {
                if (entry.Value == key)
                {
                    return entry.Key;
                }
            }
            return -1;
        }

        // The drop's location, or -1 for anything else.
        internal static long LocationOf(NPCControl npc) =>
            npc != null && drops.TryGetValue(npc, out long at) ? at : -1;

        internal static List<KeyValuePair<NPCControl, long>> Live => drops.Where(d => d.Key != null).ToList();

        // Every listed enemy exists from the start: none is missable.
        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPostfix]
        private static void AfterCreate(MapControl __instance)
        {
            drops.Clear();
            if (!On)
            {
                return;
            }
            int kept = 0;
            foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true))
            {
                bool flagged = (npc.requires != null && npc.requires.Length > 0 && npc.requires[0] > -1)
                    || (npc.limit != null && npc.limit.Length > 0 && npc.limit[0] > -1);
                if (flagged && LocationFor(npc) >= 0)
                {
                    KeptOpen.KeepPresent(npc);
                    kept++;
                }
            }
            if (kept > 0)
            {
                log.LogInfo($"[enemies] {__instance.mapid}: {kept} story-flagged enemies kept present (Enemysanity)");
            }
        }

        [HarmonyPatch(typeof(EntityControl), "Death", new[] { typeof(bool) })]
        [HarmonyPatch(MethodType.Enumerator)]
        [HarmonyPrefix]
        private static void BeforeDeathStep(object __instance)
        {
            if (!On || (int)deathState.GetValue(__instance) != 0 || dying.ContainsKey(__instance))
            {
                return;
            }
            EntityControl entity = deathOwner.GetValue(__instance) as EntityControl;
            long at = LocationFor(entity != null ? entity.npcdata : null);
            if (at < 0)
            {
                return;
            }
            if (connection.IsDone(at))
            {
                log.LogInfo($"[enemies] location {at} defeated again, already done: no drop");
                return;
            }
            dying[__instance] = new KeyValuePair<long, Vector3>(at, entity.transform.position);
        }

        // At its death's end, where the game drops its own items.
        [HarmonyPatch(typeof(EntityControl), "Death", new[] { typeof(bool) })]
        [HarmonyPatch(MethodType.Enumerator)]
        [HarmonyPostfix]
        private static void AfterDeathStep(object __instance, bool __result)
        {
            if (__result || !dying.TryGetValue(__instance, out KeyValuePair<long, Vector3> death))
            {
                return;
            }
            dying.Remove(__instance);
            if (MainManager.map == null || MainManager.battle != null)
            {
                log.LogInfo($"[enemies] location {death.Key}: no drop (no map, or a battle running)");
                return;
            }
            Vector3 bounce = MainManager.RandomItemBounce(4f, 10f);
            // A key item that never despawns, as the game's held-key drop; ItemSwap makes it the seed's item.
            NPCControl drop = EntityControl.CreateItem(death.Value + Vector3.up * 0.5f, 1, 0, bounce, -1);
            if (drop == null)
            {
                return;
            }
            drop.entity.LateVelocity(bounce);
            drops[drop] = death.Key;
            log.LogInfo($"[enemies] location {death.Key}: an enemy's drop made on {MainManager.map.mapid}");
        }
    }
}
