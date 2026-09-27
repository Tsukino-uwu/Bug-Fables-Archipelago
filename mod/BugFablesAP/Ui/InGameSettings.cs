using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // With Archipelago on (or Use on normal saves), the pause menu's Settings list gets Quality of life, Gameplay and Graphics rows at the
    // top, each opening the panel's page of that name. A settings row is an id whose label is
    // menutext[settingsindex[id]], so both tables get three entries; the game gives every row but a few arrows, removed here.
    internal static class InGameSettings
    {
        private const int QolId = 26, GameplayId = 27, GraphicsId = 28;
        private static readonly int[] PageIds = { QolId, GameplayId, GraphicsId };
        private static readonly string[] PageLabels = { "Quality of life", "Gameplay", "Graphics" };
        private static readonly ApMenu.Page[] Pages = { ApMenu.Page.Qol, ApMenu.Page.Gameplay, ApMenu.Page.Graphics };

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
            List<string> text = MainManager.menutext.ToList();
            int[] index = MainManager.settingsindex;
            if (index.Length <= GraphicsId)
            {
                Array.Resize(ref index, GraphicsId + 1);
            }
            for (int p = 0; p < PageIds.Length; p++)
            {
                int at = text.IndexOf(PageLabels[p]);
                if (at < 0)
                {
                    at = text.Count;
                    text.Add(PageLabels[p]);
                }
                index[PageIds[p]] = at;
            }
            MainManager.menutext = text.ToArray();
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
            list.InsertRange(0, PageIds);
            __result = list.ToArray();
        }

        // The game draws left/right arrows on every settings row but a named few: take them off the page rows.
        private static void AfterShowList(int type)
        {
            if (type != 17 || MainManager.instance?.itemlist == null || MainManager.listvar == null || !InGame())
            {
                return;
            }
            Transform[] all = MainManager.instance.itemlist.GetComponentsInChildren<Transform>(true);
            for (int m = 0; m < MainManager.listvar.Length; m++)
            {
                if (Array.IndexOf(PageIds, MainManager.listvar[m]) < 0)
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
            int page = Array.IndexOf(PageIds, MainManager.listvar[option]);
            if (page < 0 || !MainManager.GetKey(4, hold: false))
            {
                return true;
            }
            MainManager.PlaySound("Confirm", -1);
            MainManager.instance.inputcooldown = 5f;
            log.LogInfo("[settings] opening the " + PageLabels[page] + " page from Settings");
            ApMenu.ShowInGame(log, Pages[page]);
            return false;
        }
    }
}
