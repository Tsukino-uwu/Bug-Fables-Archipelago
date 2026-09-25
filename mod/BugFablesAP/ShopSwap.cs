using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Medal shops (the user, 2026-09-25). A medal shop is a counter of item entities: each slot is an NPCControl with
    // interacttype Shop, animid 2 (medal) and animstate the medal id, made by the shopkeeper's SetBadgeShop from
    // avaliablebadgepool (NPCControl.cs:1504-1580); the shopkeeper's dialogues[9].x is the badgeshops index. Looking at a
    // slot opens CreateDescWindow(shop), which reads the medal's name and description from badgedata[id, 0] and [id, 1]
    // (NPCControl.cs:4183-4228); interacting copies the name into the buy prompt's text and the price into flagvar[1]
    // (NPCControl.Interact, :4360-4372), and the shopkeeper's buy line gives the medal with giveitem, which ItemSwap swaps
    // as for a gift (the location's give). So for a slot whose medal is a location: the shelf shows the seed's item's
    // sprite, and while those two methods run the medal's name and description read as the seed's item.
    //
    // Shop prices (a Quality of life row: Normal, Half, Free) scale the medal table's price columns in memory, 5 for
    // berries and 7 for crystal berries (MainManager.cs:3474-3488), and put them back when the setting or the mod is off.
    internal static class ShopSwap
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static string[,] originalPrices;
        private static string appliedPrices = "Normal";

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo desc = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.CreateDescWindow), new[] { typeof(bool) });
            MethodInfo interact = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.Interact), new[] { typeof(string) });
            if (desc == null || interact == null)
            {
                log.LogError($"[shop] NOT installed (CreateDescWindow {desc != null}, Interact {interact != null}): shop shelves show their own medals.");
                return;
            }
            harmony = new Harmony(guid + ".shop." + DateTime.UtcNow.Ticks);
            var before = new HarmonyMethod(typeof(ShopSwap), nameof(BeforeShow));
            var after = new HarmonyMethod(typeof(ShopSwap), nameof(AfterShow));
            harmony.Patch(desc, prefix: before, postfix: after);
            harmony.Patch(interact, prefix: before, postfix: after);
            MethodInfo shelf = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.SetBadgeShop), new[] { typeof(bool) });
            if (shelf != null)
            {
                harmony.Patch(shelf, prefix: new HarmonyMethod(typeof(ShopSwap), nameof(BeforeShelf)));
            }
            log.LogInfo("[shop] installed on NPCControl.CreateDescWindow and Interact");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
            SetPrices("Normal");
        }

        // More medals on show at once (the user, 2026-09-25: a QoL thing): Merab's shelf 5 instead of 3 (a 6th past her last
        // spot was hard to reach, the user), Shades's 6 instead of 2 (hers sat far apart; the user asked for 6).
        // The shelf has one slot per entry of the shopkeeper's data, each at vectordata[j] (NPCControl.cs:1531-1534); before
        // it's built, the spots are laid out evenly: {shop: (slots spanning her first to last spot, slots in all)}.
        private static readonly Dictionary<int, int[]> ShelfSlots = new Dictionary<int, int[]> { { 0, new[] { 5, 5 } }, { 1, new[] { 6, 6 } } };

        // Shopkeepers already stretched: the game rebuilds the shelf on the same shopkeeper after a purchase (SetBadgeShop
        // with refresh), and stretching the stretched spots again drifted the shelf right (a 6th slot appeared).
        private static readonly HashSet<NPCControl> stretched = new HashSet<NPCControl>();

        private static void BeforeShelf(NPCControl __instance)
        {
            stretched.RemoveWhere(n => n == null);
            if (stretched.Contains(__instance))
            {
                return;
            }
            if (randomizerOn == null || !randomizerOn() || __instance.interacttype == NPCControl.Interaction.CaravanBadge
                || __instance.dialogues == null || __instance.dialogues.Length < 10
                || !ShelfSlots.TryGetValue((int)__instance.dialogues[9].x, out int[] layout)
                || __instance.data == null || __instance.data.Length < 2 || __instance.data.Length >= layout[1]
                || __instance.vectordata == null || __instance.vectordata.Length < __instance.data.Length)
            {
                return;
            }
            int shown = __instance.data.Length;
            int slots = layout[1];
            Vector3 first = __instance.vectordata[0];
            Vector3 step = (__instance.vectordata[shown - 1] - first) / (layout[0] - 1);
            var spots = new Vector3[slots];
            var data = new int[slots];
            for (int j = 0; j < slots; j++)
            {
                spots[j] = first + step * j;
                data[j] = __instance.data[Math.Min(j, shown - 1)];
            }
            __instance.vectordata = spots;
            __instance.data = data;
            stretched.Add(__instance);
            log.LogInfo($"[shop] shop {(int)__instance.dialogues[9].x}'s shelf: {slots} slots instead of {shown}");
        }

        // The location a shop slot stands for, or -1.
        private static long LocationOf(NPCControl npc)
        {
            Dictionary<long, int[]> shops = connection?.LocationShops;
            if (shops == null || npc == null || npc.interacttype != NPCControl.Interaction.Shop || npc.entity == null
                || npc.entity.animid != 2 || npc.shopkeeper == null || npc.shopkeeper.dialogues == null || npc.shopkeeper.dialogues.Length < 10)
            {
                return -1;
            }
            int shop = (int)npc.shopkeeper.dialogues[9].x;
            int medal = npc.entity.animstate;
            foreach (KeyValuePair<long, int[]> entry in shops)
            {
                if (entry.Value[0] == shop && entry.Value[1] == medal)
                {
                    return entry.Key;
                }
            }
            return -1;
        }

        private sealed class Saved
        {
            internal int Medal;
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
            int medal = __instance.entity.animstate;
            ItemSwap.LookOf(at, out string name, out _, out string description);
            __state = new Saved { Medal = medal, Name = MainManager.badgedata[medal, 0], Description = MainManager.badgedata[medal, 1] };
            MainManager.badgedata[medal, 0] = name ?? __state.Name;
            MainManager.badgedata[medal, 1] = description ?? __state.Description;
        }

        private static void AfterShow(Saved __state)
        {
            if (__state == null)
            {
                return;
            }
            MainManager.badgedata[__state.Medal, 0] = __state.Name;
            MainManager.badgedata[__state.Medal, 1] = __state.Description;
        }

        internal static void Tick()
        {
            if (Time.frameCount % 15 != 0)
            {
                return;
            }
            bool on = randomizerOn != null && randomizerOn();
            SetPrices(on ? QualityOfLife.ShopPrices?.Value ?? "Normal" : "Normal");
            MapControl map = MainManager.map;
            if (!on || map == null || connection?.LocationShops == null)
            {
                return;
            }
            foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
            {
                long at = LocationOf(npc);
                if (at < 0 || npc.entity.sprite == null)
                {
                    continue;
                }
                ItemSwap.LookOf(at, out _, out Sprite sprite, out _);
                if (sprite != null && npc.entity.sprite.sprite != sprite)
                {
                    npc.entity.sprite.sprite = sprite;
                }
            }
        }

        // Normal, Half or Free, applied to the medal table's price columns; the originals are kept to put back.
        private static void SetPrices(string setting)
        {
            string[,] table = MainManager.badgedata;
            if (table == null || setting == appliedPrices)
            {
                return;
            }
            int rows = table.GetLength(0);
            if (originalPrices == null)
            {
                originalPrices = new string[rows, 2];
                for (int i = 0; i < rows; i++)
                {
                    originalPrices[i, 0] = table[i, 5];
                    originalPrices[i, 1] = table[i, 7];
                }
            }
            for (int i = 0; i < rows; i++)
            {
                table[i, 5] = Scaled(originalPrices[i, 0], setting);
                table[i, 7] = Scaled(originalPrices[i, 1], setting);
            }
            log.LogInfo($"[shop] prices: {setting}");
            appliedPrices = setting;
        }

        private static string Scaled(string original, string setting)
        {
            if (setting == "Normal" || !int.TryParse(original, out int price))
            {
                return original;
            }
            return setting == "Free" ? "0" : Math.Max(1, (price + 1) / 2).ToString();
        }
    }
}
