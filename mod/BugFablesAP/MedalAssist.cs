using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Archipelago panel's Difficulty and Detector rows (the user, 2026-09-24). Both only ADD a way in: the game
    // asks "is this medal equipped?" through MainManager.BadgeIsEquipped(id, playerid), and every party-wide check
    // (playerid -1) of the Hard Mode medal (#11) and the Detector (#2) goes through it. A postfix answers yes when
    // the panel says so; equipping the medal still works as the game made it. The medals menu equips from the
    // badges list itself, not through this, so nothing there changes. No save data is written.
    //
    // Hardest (the HARDEST code's extras, which read flag 614 directly) isn't here yet: see apimplementation.md.
    // Only on randomizer saves: with the Archipelago mod disabled, the game is untouched.
    internal static class MedalAssist
    {
        internal const int HardModeMedal = 11;
        internal const int DetectorMedal = 2;

        private static Func<bool> active;
        private static Func<bool> hard;
        private static Func<bool> detector;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource log, string guid, Func<bool> randomizerOn, Func<bool> hardOn, Func<bool> detectorOn)
        {
            active = randomizerOn;
            hard = hardOn;
            detector = detectorOn;
            var method = AccessTools.Method(typeof(MainManager), nameof(MainManager.BadgeIsEquipped), new[] { typeof(int), typeof(int) });
            if (method == null)
            {
                log.LogError("[medals] NOT installed: MainManager.BadgeIsEquipped(int, int) wasn't found; Difficulty and Detector do nothing.");
                return;
            }
            harmony = new Harmony(guid + ".medals." + DateTime.UtcNow.Ticks);
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(MedalAssist), nameof(Postfix)));
            log.LogInfo("[medals] installed on MainManager.BadgeIsEquipped");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void Postfix(int id, int playerid, ref bool __result)
        {
            if (__result || playerid != -1 || active == null || !active())
            {
                return;
            }
            if ((id == HardModeMedal && hard()) || (id == DetectorMedal && detector()))
            {
                __result = true;
            }
        }
    }
}
