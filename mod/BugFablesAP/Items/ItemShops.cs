using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Item shops: the first purchase of each item in each shop is a check, then the shop's own item again. The buy
    // line's additem is taken out, and berries down by the price (flagvar[1]) once the dialogue ends mean it was
    // bought.
    internal static class ItemShops
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;

        // Shelves whose scenery behind reaches into a check's backdrop: every slot a step toward the camera, the row kept
        // even. {(map, shopkeeper): step}; dev `shelfforward` tunes it.
        internal static readonly Dictionary<KeyValuePair<string, string>, float> ShelvesForward =
            new Dictionary<KeyValuePair<string, string>, float>
            {
                { new KeyValuePair<string, string>("BugariaCommercial", "ButterflyShopkeeper"), 0.1f },
            };

        // pending: the slot whose buy talk is open; watching: a purchase to confirm by the berries paid.
        private static long pending = -1;
        private static long watching = -1;
        private static int moneyBefore;
        private static int price;

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (Hooks.Install(typeof(ItemShops), "itemshop", "item shops sell their own items"))
            {
                log.LogInfo(
                    "[itemshop] installed on NPCControl.CreateDescWindow, Interact and MainManager.GetDialogueText");
            }
        }

        // This slot's location while its check isn't done, else -1. Matched by the item the map stocks there, not the
        // one it holds, which Shuffle Shop Inventories changes.
        internal static long LocationOf(NPCControl npc)
        {
            Dictionary<long, ApConnection.ItemShopSlot> shops = connection?.LocationItemShops;
            if (shops == null || npc == null || npc.interacttype != NPCControl.Interaction.Shop || npc.entity == null
                || npc.entity.animid != 0 || npc.shopkeeper == null || MainManager.map == null)
            {
                return -1;
            }
            string map = MainManager.map.mapid.ToString();
            int stocked = ShopInventories.ShopItemOf(npc);
            int item = stocked >= 0 ? stocked : npc.entity.animstate;
            foreach (KeyValuePair<long, ApConnection.ItemShopSlot> entry in shops)
            {
                if (entry.Value.Map == map && entry.Value.Keeper == npc.shopkeeper.name
                    && entry.Value.Item == item)
                {
                    return connection.IsDone(entry.Key) ? -1 : entry.Key;
                }
            }
            return -1;
        }

        private sealed class Saved
        {
            internal int Item;
            internal string Name;
            internal string Description;
        }

        [HarmonyPatch(typeof(NPCControl), nameof(NPCControl.CreateDescWindow), typeof(bool))]
        [HarmonyPrefix]
        private static void BeforeShow(NPCControl __instance, out Saved __state)
        {
            __state = null;
            if (randomizerOn == null || !randomizerOn())
            {
                return;
            }
            long at = LocationOf(__instance);
            if (at < 0)
            {
                return;
            }
            int item = __instance.entity.animstate;
            ItemSwap.LookOf(at, out string name, out _, out string description);
            __state = new Saved { Item = item, Name = MainManager.itemdata[0, item, 0],
                Description = MainManager.itemdata[0, item, 2] };
            MainManager.itemdata[0, item, 0] = name ?? __state.Name;
            MainManager.itemdata[0, item, 2] = description ?? __state.Description;
        }

        [HarmonyPatch(typeof(NPCControl), nameof(NPCControl.Interact), typeof(string))]
        [HarmonyPrefix]
        private static void BeforeInteract(NPCControl __instance, out Saved __state)
        {
            BeforeShow(__instance, out __state);
            if (__state != null)
            {
                pending = LocationOf(__instance);
            }
        }

        [HarmonyPatch(typeof(NPCControl), nameof(NPCControl.CreateDescWindow), typeof(bool))]
        [HarmonyPatch(typeof(NPCControl), nameof(NPCControl.Interact), typeof(string))]
        [HarmonyPostfix]
        private static void AfterShow(Saved __state)
        {
            if (__state == null)
            {
                return;
            }
            MainManager.itemdata[0, __state.Item, 0] = __state.Name;
            MainManager.itemdata[0, __state.Item, 2] = __state.Description;
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.GetDialogueText), typeof(int))]
        [HarmonyPostfix]
        private static void AfterLine(ref string __result)
        {
            const string add = "|additem,0,var,0|";
            if (pending < 0 || __result == null || !__result.Contains(add) || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            __result = __result.Replace(add, "");
            watching = pending;
            pending = -1;
            moneyBefore = MainManager.instance.money;
            price = MainManager.instance.flagvar[GameVars.ShopPrice];
            log.LogInfo($"[itemshop] location {watching}: its buy line read, additem taken out (berries {moneyBefore}, price {price})");
        }

        internal static void Tick()
        {
            MainManager mm = MainManager.instance;
            if (mm == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            if (watching >= 0 && !mm.message)
            {
                long at = watching;
                watching = -1;
                if (price > 0 && mm.money <= moneyBefore - price)
                {
                    connection.QueueRespawnCheck(at, mm.flagstring[ItemReceiver.SeedSlot]);
                    HoldUps.FoundAt(at, "item shop purchase");
                    log.LogInfo(
                        $"[itemshop] location {at}: bought ({moneyBefore} -> {mm.money} berries): check queued");
                }
                else
                {
                    log.LogInfo(
                        $"[itemshop] location {at}: not bought (berries {moneyBefore} -> {mm.money}, price {price})");
                }
            }
            if (Time.frameCount % 15 != 0 || MainManager.map == null || connection?.LocationItemShops == null)
            {
                return;
            }
            foreach (NPCControl npc in MainManager.map.GetComponentsInChildren<NPCControl>(true))
            {
                if (npc.interacttype != NPCControl.Interaction.Shop || npc.entity == null || npc.entity.animid != 0
                    || npc.entity.sprite == null)
                {
                    continue;
                }
                Forward(npc);
                long at = LocationOf(npc);
                if (at < 0)
                {
                    // Its check is done (or it was never one): the item it holds (its own, or its Shuffle Shop
                    // Inventories restock), asked of the game, not remembered (a hot reload once remembered the seed's
                    // look as the original).
                    Sprite own = MainManager.GetItemSprite(false, npc.entity.animstate);
                    if (npc.entity.sprite.sprite != own)
                    {
                        npc.entity.sprite.sprite = own;
                    }
                    ItemSwap.Mark(npc.entity, null);
                    continue;
                }
                ItemSwap.LookOf(at, out _, out Sprite sprite, out _);
                if (sprite != null && npc.entity.sprite.sprite != sprite)
                {
                    npc.entity.sprite.sprite = sprite;
                }
                ItemSwap.Mark(npc.entity, ItemSwap.MarkColorOf(at));
            }
        }

        // The slot's sprite, and the backdrop on it, a step toward the camera: its holder's local depth, which Mark
        // keeps.
        private static void Forward(NPCControl npc)
        {
            Transform held = npc.entity.spritetransform;
            if (held == null || npc.shopkeeper == null || !ShelvesForward.TryGetValue(
                new KeyValuePair<string, string>(MainManager.map.mapid.ToString(), npc.shopkeeper.name), out float step))
            {
                return;
            }
            if (held.localPosition.z != -step)
            {
                held.localPosition = new Vector3(held.localPosition.x, held.localPosition.y, -step);
            }
        }
    }
}
