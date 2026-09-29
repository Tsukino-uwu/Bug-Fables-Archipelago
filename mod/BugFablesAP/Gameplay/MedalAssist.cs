using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The panel's Difficulty and Detector rows: a BadgeIsEquipped postfix answers yes for party-wide checks. Hardest is
    // flag 614, which the save must never keep from the panel: SaveFile writes the save's own value.
    internal static class MedalAssist
    {
        internal const int HardModeMedal = 11;
        internal const int DetectorMedal = 2;

        private static Func<bool> active;
        private static Func<bool> randomizer;
        private static Func<bool> hard;
        private static Func<bool> detector;
        private static Func<bool> hardest;
        private static ManualLogSource log;

        internal const int HardestFlag = 614;
        private static bool forced;

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerOn, Func<bool> settingsOn,
            Func<bool> hardOn, Func<bool> hardestOn, Func<bool> detectorOn)
        {
            log = logger;
            active = settingsOn;
            randomizer = randomizerOn;
            hard = hardOn;
            hardest = hardestOn;
            detector = detectorOn;
            if (!Hooks.Install(typeof(MedalAssist), "medals", "Difficulty and Detector do nothing"))
            {
                return;
            }
            // Without all three a save could keep the panel's 614, so Hardest stays off.
            if (!Hooks.Install(typeof(HardestSaves), "medals", "Hardest does nothing"))
            {
                hardest = () => false;
            }
            log.LogInfo("[medals] installed on MainManager.BadgeIsEquipped, SaveFile, Load and SetVariables");
        }

        internal static void Disable()
        {
            // A hot reload must not leave the panel's 614: the new instance couldn't tell it from the save's.
            if (forced && MainManager.instance?.flags != null)
            {
                MainManager.instance.flags[HardestFlag] = false;
                forced = false;
            }
        }

        internal static void Tick()
        {
            bool[] flags = MainManager.instance?.flags;
            if (flags == null || MainManager.map == null || hardest == null)
            {
                return;
            }
            bool want = active() && hardest();
            if (want && !flags[HardestFlag])
            {
                flags[HardestFlag] = true;
                forced = true;
                log.LogInfo("[medals] Hardest on (flag 614 set for play; saves keep their own value)");
            }
            else if (!want && forced)
            {
                flags[HardestFlag] = false;
                forced = false;
                log.LogInfo("[medals] Hardest off (the panel's flag 614 cleared)");
            }
        }

        private static class HardestSaves
        {
            [HarmonyPatch(typeof(MainManager), nameof(MainManager.SaveFile), typeof(UnityEngine.Vector3?))]
            [HarmonyPrefix]
            private static void BeforeSave(ref bool __state)
            {
                __state = forced && MainManager.instance?.flags != null;
                if (__state)
                {
                    MainManager.instance.flags[HardestFlag] = false;
                }
            }

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.SaveFile), typeof(UnityEngine.Vector3?))]
            [HarmonyPostfix]
            private static void AfterSave(bool __state)
            {
                if (__state)
                {
                    MainManager.instance.flags[HardestFlag] = true;
                }
            }

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.Load), typeof(int), typeof(bool))]
            [HarmonyPostfix]
            private static void AfterLoad(bool lite)
            {
                if (!lite)
                {
                    forced = false;
                }
            }

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.SetVariables))]
            [HarmonyPostfix]
            private static void AfterReset()
            {
                forced = false;
            }
        }

        // lite loads are the file select's previews.

        // Boss prizes are always paid as if Hard Mode were on: a prize slot reading 2 (missed) is paid by the game's
        // own AddPrizeMedal with Hard Mode answered yes for that call. Never in battle (a retry rolls flagvar back).
        private static bool payingPrize;
        // Slot 1 is a dialogue gift the game marks missed when skipped (Event58), not a boss prize.
        private const int NotABossPrize = 1;

        internal static void PayPrizes()
        {
            MainManager mm = MainManager.instance;
            if (randomizer == null || !randomizer() || mm == null || mm.flagvar == null || mm.prizeflags == null
                || MainManager.map == null
                || mm.inbattle || MainManager.battle != null || mm.inevent)
            {
                return;
            }
            for (int i = 0; i < mm.prizeflags.Length; i++)
            {
                if (i == NotABossPrize || mm.flagvar[mm.prizeflags[i]] != 2)
                {
                    continue;
                }
                payingPrize = true;
                try
                {
                    MainManager.AddPrizeMedal(i);
                }
                finally
                {
                    payingPrize = false;
                }
                log.LogInfo($"[medals] prize {i} (flagvar[{mm.prizeflags[i]}]) was missed; paid as Hard Mode, now {mm.flagvar[mm.prizeflags[i]]}: waiting at Artis");
            }
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.BadgeIsEquipped), typeof(int), typeof(int))]
        [HarmonyPostfix]
        private static void Postfix(int id, int playerid, ref bool __result)
        {
            if (__result || playerid != -1 || active == null || !active())
            {
                return;
            }
            if ((id == HardModeMedal && (hard() || payingPrize)) || (id == DetectorMedal && detector()))
            {
                __result = true;
            }
        }
    }
}
