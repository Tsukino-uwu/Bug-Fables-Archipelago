using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // With Archipelago on (or Use on normal saves), the pause menu's Settings list gets Quality of life and Gameplay rows at the top, each opening
    // the panel's page of that name. A settings row is an id whose label is
    // menutext[settingsindex[id]], so both tables get two entries; the game gives every row but a few arrows, removed here.
    internal static class InGameSettings
    {
        private const int QolId = 26, GameplayId = 27;
        private const string QolLabel = "Quality of life", GameplayLabel = "Gameplay";

        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static ManualLogSource log;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            var settings = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetSettings));
            var showList = AccessTools.Method(typeof(MainManager), nameof(MainManager.ShowItemList),
                new[] { typeof(int), typeof(Vector2), typeof(bool), typeof(bool) });
            var update = AccessTools.Method(typeof(PauseMenu), "Update");
            if (settings == null || showList == null || update == null)
            {
                log.LogError($"[settings] NOT installed (GetSettings {settings != null}, ShowItemList {showList != null}, "
                    + $"PauseMenu.Update {update != null}); the pages are reached from the main menu only.");
                return;
            }
            harmony = new Harmony(guid + ".settings." + DateTime.UtcNow.Ticks);
            harmony.Patch(settings, postfix: new HarmonyMethod(typeof(InGameSettings), nameof(AfterGetSettings)));
            harmony.Patch(showList, postfix: new HarmonyMethod(typeof(InGameSettings), nameof(AfterShowList)));
            harmony.Patch(update, prefix: new HarmonyMethod(typeof(InGameSettings), nameof(BeforePauseUpdate)));
            log.LogInfo("[settings] installed on MainManager.GetSettings, ShowItemList and PauseMenu.Update");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // In game and on the main menu alike (the user: one Settings screen, not two).
        private static bool InGame() => randomizerOn != null && randomizerOn() && MainManager.pausemenu != null;

        // The labels live at the end of menutext, re-added whenever the game reloads it (a language change).
        private static void EnsureTables()
        {
            string[] text = MainManager.menutext;
            int qol = Array.IndexOf(text, QolLabel), gameplay = Array.IndexOf(text, GameplayLabel);
            if (qol < 0 || gameplay < 0)
            {
                List<string> grown = text.ToList();
                qol = grown.Count;
                grown.Add(QolLabel);
                gameplay = grown.Count;
                grown.Add(GameplayLabel);
                MainManager.menutext = grown.ToArray();
            }
            int[] index = MainManager.settingsindex;
            if (index.Length <= GameplayId)
            {
                Array.Resize(ref index, GameplayId + 1);
            }
            index[QolId] = qol;
            index[GameplayId] = gameplay;
            MainManager.settingsindex = index;
        }

        private static void AfterGetSettings(ref int[] __result)
        {
            if (!InGame())
            {
                return;
            }
            EnsureTables();
            List<int> list = __result.ToList();
            list.InsertRange(0, new[] { QolId, GameplayId });
            __result = list.ToArray();
        }

        // The game draws left/right arrows on every settings row but a named few: take them off the two page rows.
        private static void AfterShowList(int type)
        {
            if (type != 17 || MainManager.instance?.itemlist == null || MainManager.listvar == null || !InGame())
            {
                return;
            }
            Transform[] all = MainManager.instance.itemlist.GetComponentsInChildren<Transform>(true);
            for (int m = 0; m < MainManager.listvar.Length; m++)
            {
                if (MainManager.listvar[m] != QolId && MainManager.listvar[m] != GameplayId)
                {
                    continue;
                }
                foreach (Transform bar in all.Where(t => t != null && t.name == "Bar" + m))
                {
                    foreach (Transform child in bar.Cast<Transform>().Where(c => c.name.StartsWith("slider")).ToList())
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                }
            }
        }

        // Confirm on a page row opens that page; while a page is open, the (switched off) pause menu reads nothing.
        private static bool BeforePauseUpdate(PauseMenu __instance)
        {
            if (ApMenu.Open != null)
            {
                return false;
            }
            if (__instance.windowid != 4 || !InGame() || MainManager.instance.inputcooldown > 0f || MainManager.listvar == null)
            {
                return true;
            }
            int option = MainManager.instance.option;
            if (option < 0 || option >= MainManager.listvar.Length)
            {
                return true;
            }
            int id = MainManager.listvar[option];
            if ((id != QolId && id != GameplayId) || !MainManager.GetKey(4, hold: false))
            {
                return true;
            }
            MainManager.PlaySound("Confirm", -1);
            MainManager.instance.inputcooldown = 5f;
            log.LogInfo("[settings] opening the " + (id == QolId ? QolLabel : GameplayLabel) + " page from Settings");
            ApMenu.ShowInGame(log, gameplay: id == GameplayId);
            return false;
        }
    }
}
