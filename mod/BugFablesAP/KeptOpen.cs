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
            log.LogInfo("[open] installed on MapControl.CreateEntities and MainManager.CheckIfCanExist");
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
        }

        private static bool BeforeCheck(int[] limit, ref bool __result)
        {
            if (limit != null && markers.Contains(limit))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
