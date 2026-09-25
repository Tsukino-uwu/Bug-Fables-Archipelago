using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Item shops: the first purchase of each item in each shop is a check, then the shop's own item again. The buy line's
    // additem is taken out, and berries down by the price (flagvar[1]) once the dialogue ends mean it was bought.
    internal static class ItemShops
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // pending: the slot whose buy talk is open; watching: a purchase to confirm by the berries paid.
        private static long pending = -1;
        private static long watching = -1;
        private static int moneyBefore;
        private static int price;

        // The game's own sprite of each slot the mod changed.
        private static readonly Dictionary<EntityControl, Sprite> original = new Dictionary<EntityControl, Sprite>();

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo desc = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.CreateDescWindow), new[] { typeof(bool) });
            MethodInfo interact = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.Interact), new[] { typeof(string) });
            MethodInfo line = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetDialogueText), new[] { typeof(int) });
            if (desc == null || interact == null || line == null)
            {
                log.LogError($"[itemshop] NOT installed (CreateDescWindow {desc != null}, Interact {interact != null}, GetDialogueText {line != null}): item shops sell their own items.");
                return;
            }
            harmony = new Harmony(guid + ".itemshop." + DateTime.UtcNow.Ticks);
            var after = new HarmonyMethod(typeof(ItemShops), nameof(AfterShow));
            harmony.Patch(desc, prefix: new HarmonyMethod(typeof(ItemShops), nameof(BeforeShow)), postfix: after);
            harmony.Patch(interact, prefix: new HarmonyMethod(typeof(ItemShops), nameof(BeforeInteract)), postfix: after);
            harmony.Patch(line, postfix: new HarmonyMethod(typeof(ItemShops), nameof(AfterLine)));
            log.LogInfo("[itemshop] installed on NPCControl.CreateDescWindow, Interact and MainManager.GetDialogueText");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // This slot's location while its check isn't done, else -1.
        private static long LocationOf(NPCControl npc)
        {
            Dictionary<long, ApConnection.ItemShopSlot> shops = connection?.LocationItemShops;
            if (shops == null || npc == null || npc.interacttype != NPCControl.Interaction.Shop || npc.entity == null
                || npc.entity.animid != 0 || npc.shopkeeper == null || MainManager.map == null)
            {
                return -1;
            }
            string map = MainManager.map.mapid.ToString();
            foreach (KeyValuePair<long, ApConnection.ItemShopSlot> entry in shops)
            {
                if (entry.Value.Map == map && entry.Value.Keeper == npc.shopkeeper.name && entry.Value.Item == npc.entity.animstate)
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
            __state = new Saved { Item = item, Name = MainManager.itemdata[0, item, 0], Description = MainManager.itemdata[0, item, 2] };
            MainManager.itemdata[0, item, 0] = name ?? __state.Name;
            MainManager.itemdata[0, item, 2] = description ?? __state.Description;
        }

        private static void BeforeInteract(NPCControl __instance, out Saved __state)
        {
            BeforeShow(__instance, out __state);
            if (__state != null)
            {
                pending = LocationOf(__instance);
            }
        }

        private static void AfterShow(Saved __state)
        {
            if (__state == null)
            {
                return;
            }
            MainManager.itemdata[0, __state.Item, 0] = __state.Name;
            MainManager.itemdata[0, __state.Item, 2] = __state.Description;
        }

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
            price = MainManager.instance.flagvar[1];
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
                    log.LogInfo($"[itemshop] location {at}: bought ({moneyBefore} -> {mm.money} berries): check queued");
                }
                else
                {
                    log.LogInfo($"[itemshop] location {at}: not bought (berries {moneyBefore} -> {mm.money}, price {price})");
                }
            }
            if (Time.frameCount % 15 != 0 || MainManager.map == null || connection?.LocationItemShops == null)
            {
                return;
            }
            foreach (NPCControl npc in MainManager.map.GetComponentsInChildren<NPCControl>(true))
            {
                if (npc.interacttype != NPCControl.Interaction.Shop || npc.entity == null || npc.entity.animid != 0 || npc.entity.sprite == null)
                {
                    continue;
                }
                long at = LocationOf(npc);
                if (at < 0)
                {
                    if (original.TryGetValue(npc.entity, out Sprite own))
                    {
                        npc.entity.sprite.sprite = own; // its check is done: the shop's own item again
                        original.Remove(npc.entity);
                    }
                    continue;
                }
                ItemSwap.LookOf(at, out _, out Sprite sprite, out _);
                if (sprite != null && npc.entity.sprite.sprite != sprite)
                {
                    if (!original.ContainsKey(npc.entity))
                    {
                        original[npc.entity] = npc.entity.sprite.sprite;
                    }
                    npc.entity.sprite.sprite = sprite;
                }
            }
        }
    }
}
