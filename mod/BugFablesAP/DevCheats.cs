using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev-only config cheats. One-shot: each resets its setting so a reload can't apply it twice.
    internal static class DevCheats
    {
        // Adds berries as the game's reward code does: clamped to 0..999, shown on the counter.
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
