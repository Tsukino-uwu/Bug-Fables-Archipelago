using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Shuffle Termacade: the prize stand's list (ShowItemList's list 26) shows and names the seed's item for each prize
    // whose check isn't done. The list reads the game's own name, description and sprite tables as it is built, so
    // those entries are the seed's only while it builds. Buying is ItemSwap's give swap.
    internal static class Termacade
    {
        private const int PrizeList = 26;

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static readonly List<Action> restore = new List<Action>();

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (Hooks.Install(typeof(Termacade), "termacade", "the prize stand shows its own prizes"))
            {
                log.LogInfo("[termacade] installed on MainManager.ShowItemList");
            }
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShowItemList))]
        [HarmonyPrefix]
        private static void BeforeList(int type)
        {
            Restore();
            Dictionary<long, int> prizes = connection?.LocationPrizes;
            int[,] table = MainManager.termacadeprize;
            if (type != PrizeList || prizes == null || table == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            // Every look is read before any entry changes: one prize's own item can be another's seed item.
            var looks = new List<KeyValuePair<int, Look>>();
            foreach (KeyValuePair<long, int> entry in prizes)
            {
                int row = entry.Value;
                if (connection.IsDone(entry.Key) || row < 0 || row >= table.GetLength(0))
                {
                    continue;
                }
                ItemSwap.LookOf(entry.Key, out string shownName, out Sprite shownSprite, out string shownDescription);
                looks.Add(new KeyValuePair<int, Look>(row,
                    new Look { Name = shownName, Sprite = shownSprite, Description = shownDescription }));
            }
            foreach (KeyValuePair<int, Look> look in looks)
            {
                int row = look.Key;
                string name = look.Value.Name, description = look.Value.Description;
                Sprite sprite = look.Value.Sprite;
                bool medal = table[row, 0] == 2;
                int item = table[row, 1];
                if (medal)
                {
                    Swap(MainManager.badgedata, item, 0, name);
                    Swap(MainManager.badgedata, item, 1, description);
                }
                else
                {
                    Swap(MainManager.itemdata, item, 0, name);
                    Swap(MainManager.itemdata, item, 2, description);
                }
                int sheet = medal ? 1 : 0;
                Sprite own = MainManager.itemsprites[sheet, item];
                if (sprite != null)
                {
                    MainManager.itemsprites[sheet, item] = sprite;
                    restore.Add(() => MainManager.itemsprites[sheet, item] = own);
                }
            }
        }

        // A finalizer, so the game's own entries come back even if the list throws.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShowItemList))]
        [HarmonyFinalizer]
        private static void AfterList()
        {
            Restore();
        }

        private sealed class Look
        {
            internal string Name, Description;
            internal Sprite Sprite;
        }

        private static void Swap(string[,] table, int row, int column, string text)
        {
            if (text == null || row >= table.GetLength(0))
            {
                return;
            }
            string own = table[row, column];
            table[row, column] = text;
            restore.Add(() => table[row, column] = own);
        }

        private static void Swap(string[,,] table, int row, int column, string text)
        {
            if (text == null || row >= table.GetLength(1))
            {
                return;
            }
            string own = table[0, row, column];
            table[0, row, column] = text;
            restore.Add(() => table[0, row, column] = own);
        }

        private static void Restore()
        {
            for (int i = restore.Count - 1; i >= 0; i--)
            {
                restore[i]();
            }
            restore.Clear();
        }
    }
}
