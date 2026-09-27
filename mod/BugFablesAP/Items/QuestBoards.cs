using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Every quest board lists every open quest: the game shows the five bounties only at the underground bar's board
    // and hides them on all others. The quests it hides everywhere (the story chapters, Leif's) stay hidden.
    internal static class QuestBoards
    {
        private static readonly HashSet<int> HiddenEverywhere = new HashSet<int> { 11, 12, 13, 14, 15, 16, 17, 26, 30 };

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static string lastLogged;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            MethodInfo list = AccessTools.Method(typeof(MainManager), "GetQuestsBoard", new[] { typeof(int) });
            if (list == null)
            {
                log.LogError("[quests] MainManager.GetQuestsBoard(int) not found: bounties stay at the bar's board only.");
                return;
            }
            harmony = new Harmony(guid + ".quests." + DateTime.UtcNow.Ticks);
            harmony.Patch(list, prefix: new HarmonyMethod(typeof(QuestBoards), nameof(BeforeList)));
            log.LogInfo("[quests] installed on MainManager.GetQuestsBoard");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // Only the open list (0) is filtered by board; taken (1) and done (2) are left to the game.
        private static bool BeforeList(int type, ref int[] __result)
        {
            MainManager mm = MainManager.instance;
            if (type != 0 || randomizerOn == null || !randomizerOn() || mm?.boardquests == null)
            {
                return true;
            }
            var shown = new List<int>();
            foreach (int id in mm.boardquests[0])
            {
                if (!HiddenEverywhere.Contains(id))
                {
                    shown.Add(id);
                }
            }
            if (shown.Count == 0)
            {
                shown.Add(0);
            }
            __result = shown.ToArray();
            string decision = $"[quests] board on {MainManager.map?.name} lists {string.Join(",", __result)} (open: {string.Join(",", mm.boardquests[0])})";
            if (decision != lastLogged)
            {
                log.LogInfo(decision);
                lastLogged = decision;
            }
            return false;
        }
    }
}
