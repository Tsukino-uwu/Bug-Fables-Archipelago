using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Items are remote only: at a location, the game's own item must not reach the inventory, and the item-get
    // shows what the seed put there instead. The check itself is still sent from the location's flag
    // (LocationChecks), which the game sets as before.
    //
    // Every |giveitem| runs inside one long coroutine, MainManager.SetText (the 10-argument overload), so the
    // swap is a transpiler on its MoveNext. Measured in the IL on 2026-09-24 (ilspycmd, '<SetText>d__731'): the
    // Giveitem block calls GetItemSprite(bool, int) once (the only call in the whole method), then writes the
    // name to flagstring[0], then adds with List<int>.Add (items and key items) or List<int[]>.Add (medals),
    // and then loads the string "ItemGet" for its sound. Those three calls, between that anchor and "ItemGet",
    // are replaced by the static methods below. If any of them isn't found exactly once there, nothing is
    // patched and the log says so: the patch never guesses.
    internal static class ItemSwap
    {
        // Archipelago item ids are this plus the game's item id (apworld data_tables.py, ITEM_ID_BASE).
        private const long ItemIdBase = 7_710_000;

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // Set by Sprite() when a location's giveitem starts, used and cleared by the Add stand-ins. Game thread.
        private static long pendingLocation = -1;
        private static string pendingName;

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo setText = AccessTools.Method(typeof(MainManager), "SetText", new[]
            {
                typeof(string), typeof(int), typeof(float?), typeof(bool), typeof(bool), typeof(Vector3),
                typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl),
            });
            MethodInfo moveNext = setText == null ? null : AccessTools.EnumeratorMoveNext(setText);
            if (moveNext == null)
            {
                log.LogError("[swap] NOT installed: MainManager.SetText's coroutine wasn't found. Vanilla items would be given at locations.");
                return;
            }
            harmony = new Harmony(guid + ".swap." + DateTime.UtcNow.Ticks);
            harmony.Patch(moveNext, transpiler: new HarmonyMethod(typeof(ItemSwap), nameof(Transpile)));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            MethodInfo getSprite = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetItemSprite));
            MethodInfo addItem = AccessTools.Method(typeof(List<int>), nameof(List<int>.Add));
            MethodInfo addBadge = AccessTools.Method(typeof(List<int[]>), nameof(List<int[]>.Add));

            int[] anchors = code.Select((c, i) => c.Calls(getSprite) ? i : -1).Where(i => i >= 0).ToArray();
            int start = anchors.Length == 1 ? anchors[0] : -1;
            int end = start < 0 ? -1 : code.FindIndex(start, c => c.opcode == OpCodes.Ldstr && c.operand as string == "ItemGet");
            int[] itemAdds = Between(code, start, end, addItem);
            int[] badgeAdds = Between(code, start, end, addBadge);
            if (start < 0 || end < 0 || itemAdds.Length != 1 || badgeAdds.Length != 1)
            {
                log.LogError($"[swap] NOT installed: expected one GetItemSprite (found {anchors.Length}), an \"ItemGet\" after it "
                    + $"({(end >= 0 ? "found" : "missing")}), and one item add ({itemAdds.Length}) and one medal add ({badgeAdds.Length}) "
                    + "between them. The game's code differs from what was measured; vanilla items would be given at locations.");
                return code;
            }
            // Same stack shape as the originals: (bool, int) -> Sprite; (list, value) -> void. Labels stay on the
            // instructions, so jumps into them still land.
            code[start].operand = AccessTools.Method(typeof(ItemSwap), nameof(Sprite));
            code[itemAdds[0]].opcode = OpCodes.Call;
            code[itemAdds[0]].operand = AccessTools.Method(typeof(ItemSwap), nameof(AddItem));
            code[badgeAdds[0]].opcode = OpCodes.Call;
            code[badgeAdds[0]].operand = AccessTools.Method(typeof(ItemSwap), nameof(AddBadge));
            log.LogInfo($"[swap] installed in MainManager.SetText's Giveitem (instructions {start}, {itemAdds[0]}, {badgeAdds[0]}, ending at {end})");
            return code;
        }

        private static int[] Between(List<CodeInstruction> code, int start, int end, MethodInfo method)
        {
            if (start < 0 || end < 0)
            {
                return new int[0];
            }
            return Enumerable.Range(start, end - start).Where(i => code[i].Calls(method)).ToArray();
        }

        // Stands in for MainManager.GetItemSprite at the start of a Giveitem. Decides whether this grant is a
        // location's, and if so returns the sprite of what's really there.
        public static Sprite Sprite(bool badge, int id)
        {
            pendingLocation = -1;
            pendingName = null;
            long location = FindLocation(badge, id);
            if (location < 0)
            {
                return MainManager.GetItemSprite(badge, id);
            }
            Dictionary<long, ScoutedItemInfo> scouts = connection.Scouts;
            ScoutedItemInfo info = null;
            scouts?.TryGetValue(location, out info);
            Sprite sprite = null;
            string name;
            if (info == null)
            {
                name = "an Archipelago item";
            }
            else if (info.ItemGame == ApConnection.Game && info.ItemId >= ItemIdBase)
            {
                int gameId = (int)(info.ItemId - ItemIdBase);
                sprite = MainManager.GetItemSprite(false, gameId);
                name = MainManager.itemdata[0, gameId, 0];
                if (info.Player.Slot != connection.OwnSlot)
                {
                    name = info.Player.Name + "'s " + name;
                }
            }
            else
            {
                // Another game's item. The Archipelago icon replaces this sprite once it's in the mod.
                name = info.Player.Name + "'s " + info.ItemDisplayName;
            }
            pendingLocation = location;
            pendingName = name;
            log.LogInfo($"[swap] location {location}: giveitem {(badge ? "medal" : "item")} {id} on {MapName()} is a location; showing '{name}'"
                + (info == null ? " (not scouted yet)" : ""));
            return sprite ?? MainManager.GetItemSprite(badge, id);
        }

        // Stands in for items[type].Add(id) in Giveitem.
        public static void AddItem(List<int> list, int id)
        {
            if (!TakePending("item " + id))
            {
                list.Add(id);
            }
        }

        // Stands in for badges.Add(entry) in Giveitem.
        public static void AddBadge(List<int[]> list, int[] entry)
        {
            if (!TakePending("medal " + (entry != null && entry.Length > 0 ? entry[0] : -1)))
            {
                list.Add(entry);
            }
        }

        // True when this add is a location's: the vanilla item is kept out, and the item-get box names what's
        // really there (it reads flagstring[0], which the game set to the vanilla name just before).
        private static bool TakePending(string what)
        {
            if (pendingLocation < 0)
            {
                return false;
            }
            MainManager.instance.flagstring[0] = pendingName;
            log.LogInfo($"[swap] location {pendingLocation}: kept {what} out of the inventory");
            pendingLocation = -1;
            pendingName = null;
            return true;
        }

        private static long FindLocation(bool badge, int id)
        {
            Dictionary<long, ApConnection.Give> gives = connection.LocationGives;
            string map = MapName();
            // A dropped connection keeps the rules in force: the tables stay from the last login.
            if (!randomizerOn() || gives == null || map == null)
            {
                return -1;
            }
            foreach (KeyValuePair<long, ApConnection.Give> entry in gives)
            {
                ApConnection.Give give = entry.Value;
                bool sameKind = badge ? give.Type == 2 : give.Type == 0 || give.Type == 1;
                if (sameKind && give.Item == id && give.Map == map)
                {
                    return entry.Key;
                }
            }
            return -1;
        }

        private static string MapName()
        {
            MapControl map = MainManager.map;
            return map == null ? null : map.mapid.ToString();
        }
    }
}
