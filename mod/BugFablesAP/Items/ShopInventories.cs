using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Shuffle Shop Inventories (slot_data shop_inventories): an item shop slot or a respawning pickup that isn't an
    // undone check holds the seed's item instead of its own. The swap is the game's own for a random medal (basestate,
    // itemstate, animstate, then UpdateItem), so the price, name, sprite and what's added all stay the game's.
    internal static class ShopInventories
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;

        private const string SlotName = "Fixedshop";

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (Hooks.Install(typeof(ShopInventories), "inventories", "shops and respawning pickups keep their own items"))
            {
                log.LogInfo("[inventories] installed on MapControl.CreateEntities");
            }
        }

        // The item the map stocks in a shop slot: slot n is made from its keeper's data[n] and keeps that name. -1 when
        // it can't be read; such a slot is never swapped.
        internal static int ShopItemOf(NPCControl slot)
        {
            int[] stock = slot?.shopkeeper?.data;
            string name = slot?.name;
            int at = name == null ? -1 : name.IndexOf(SlotName, StringComparison.Ordinal);
            return stock != null && at >= 0 && int.TryParse(name.Substring(at + SlotName.Length), out int n) && n >= 0
                && n < stock.Length ? stock[n] : -1;
        }

        private static bool IsShopSlot(NPCControl npc) =>
            npc.interacttype == NPCControl.Interaction.Shop && npc.entity != null && npc.entity.item
            && npc.entity.animid == 0 && npc.shopkeeper != null;

        private static bool IsRespawning(NPCControl npc) =>
            npc.objecttype == NPCControl.ObjectTypes.Item && npc.entity != null && npc.entity.animid == 0
            && npc.activationflag <= 0 && npc.regionalflag >= 0;

        // After the map builds its entities, before their Start: no frame shows the game's own item.
        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPostfix]
        private static void AfterCreate(MapControl __instance)
        {
            Stock(__instance, "on arrival");
        }

        // Again every 15 frames: a check done during the visit, or a login after the map was built.
        internal static void Tick()
        {
            if (Time.frameCount % 15 == 0 && MainManager.map != null)
            {
                Stock(MainManager.map, "during the visit");
            }
        }

        private static void Stock(MapControl map, string when)
        {
            List<ApConnection.InventorySpot> spots = connection?.ShopInventories;
            if (spots == null || spots.Count == 0 || map == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            string mapName = map.mapid.ToString();
            if (!spots.Exists(s => s.Map == mapName))
            {
                return;
            }
            foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
            {
                if (IsShopSlot(npc))
                {
                    StockSlot(npc, mapName, spots, when);
                }
                else if (IsRespawning(npc))
                {
                    StockPickup(npc, mapName, spots, when);
                }
            }
        }

        // Its own item while its check isn't done, so the shelf's seed item and the check stay as ItemShops has them.
        private static void StockSlot(NPCControl slot, string map, List<ApConnection.InventorySpot> spots, string when)
        {
            int own = ShopItemOf(slot);
            if (own < 0)
            {
                return;
            }
            ApConnection.InventorySpot spot =
                spots.Find(s => s.Map == map && s.Keeper == slot.shopkeeper.name && s.Item == own);
            bool undone = ItemShops.LocationOf(slot) >= 0;
            int to = spot == null || undone ? own : spot.To;
            int before = slot.entity.animstate;
            if (to == own && before == own)
            {
                return;
            }
            if (Hold(slot.entity, to) && before != to)
            {
                log.LogInfo(to == own
                    ? $"[inventories] {map} {slot.shopkeeper.name} {slot.name}: back to its own {Name(own)} ({when}): check not done"
                    : $"[inventories] {map} {slot.shopkeeper.name} {slot.name}: {Name(own)} restocks as {Name(to)} ({when})");
            }
        }

        // An undone check is left alone: its pickup gives nothing and shows the seed's item (ItemSwap).
        private static void StockPickup(NPCControl npc, string map, List<ApConnection.InventorySpot> spots, string when)
        {
            ApConnection.InventorySpot spot = spots.Find(s => s.Map == map && s.Keeper == null
                && s.Regional == npc.regionalflag);
            if (spot == null || npc.entity.animstate == spot.To || Undone(map, npc))
            {
                return;
            }
            if (npc.entity.animstate != spot.Item)
            {
                log.LogWarning($"[inventories] {map} {npc.name}: holds {Name(npc.entity.animstate)}, not the seed's "
                    + $"{Name(spot.Item)}; left as it is");
                return;
            }
            Hold(npc.entity, spot.To);
            log.LogInfo($"[inventories] {map} {npc.name}: {Name(spot.Item)} comes back as {Name(spot.To)} ({when})");
        }

        private static bool Undone(string map, NPCControl npc)
        {
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            if (pickups == null)
            {
                return false;
            }
            foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
            {
                if (entry.Value.Map == map && entry.Value.Regional >= 0 && ItemSwap.IsPickup(entry.Value, npc))
                {
                    return !connection.IsDone(entry.Key);
                }
            }
            return false;
        }

        // Every field the game reads an item entity's item from, as NPCControl's random medal sets them.
        private static bool Hold(EntityControl entity, int item)
        {
            if (entity.animstate == item && entity.itemstate == item)
            {
                return false;
            }
            entity.basestate = item;
            entity.itemstate = item;
            entity.animstate = item;
            entity.UpdateItem();
            return true;
        }

        private static string Name(int item) =>
            MainManager.itemdata != null && item >= 0 && item < MainManager.itemdata.GetLength(1)
                ? $"{MainManager.itemdata[0, item, 0]} ({item})" : item.ToString();
    }
}
