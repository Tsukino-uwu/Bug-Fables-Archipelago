using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev-only helpers for testing, off unless set in the config. Each is one-shot: it resets its own setting
    // after running, so a hot reload or a restart can't apply it twice.
    internal static class DevCheats
    {
        // Adds berries the way the game's own reward code does (MainManager.cs:11534): clamped to 0..999 and
        // shown on the money counter. Waits until a save is loaded and the player is on a map.
        internal static void Tick(ManualLogSource log, ConfigEntry<int> giveMoney)
        {
            if (giveMoney.Value == 0 || MainManager.instance == null || MainManager.map == null)
            {
                return;
            }
            MainManager mm = MainManager.instance;
            int before = mm.money;
            mm.showmoney = 1f;
            mm.money = Mathf.Clamp(mm.money + giveMoney.Value, 0, 999);
            log.LogInfo($"[cheat] money {before} -> {mm.money} (asked for {giveMoney.Value:+#;-#;0})");
            giveMoney.Value = 0;
        }
    }
}
