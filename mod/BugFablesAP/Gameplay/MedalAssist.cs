using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The panel's Difficulty, Detector and Spy Specs rows: a BadgeIsEquipped postfix answers yes for party-wide checks
    // (Spy Specs by where it's asked).
    // Hardest is flag 614, which the save must never keep from the panel: SaveFile writes the save's own value.
    internal static class MedalAssist
    {
        internal const int HardModeMedal = 11;
        internal const int DetectorMedal = 2;
        internal const int SpySpecsMedal = 17;

        private static Func<bool> active;
        private static Func<bool> randomizer;
        private static Func<bool> hard;
        private static Func<bool> detector;
        private static Func<bool> spyHp;
        private static Func<bool> spyFree;
        private static Func<bool> hardest;
        private static ManualLogSource log;

        internal const int HardestFlag = 614;
        private static bool forced;

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerOn, Func<bool> settingsOn,
            Func<bool> hardOn, Func<bool> hardestOn, Func<bool> detectorOn, Func<bool> spyHpOn, Func<bool> spyFreeOn)
        {
            log = logger;
            active = settingsOn;
            randomizer = randomizerOn;
            hard = hardOn;
            hardest = hardestOn;
            detector = detectorOn;
            spyHp = spyHpOn;
            spyFree = spyFreeOn;
            if (!Hooks.Install(typeof(MedalAssist), "medals", "Difficulty, Detector and Spy Specs do nothing"))
            {
                return;
            }
            // Without all three a save could keep the panel's 614, so Hardest stays off.
            bool saves = Hooks.Install(typeof(HardestSaves), "medals", "Hardest does nothing");
            if (!saves)
            {
                hardest = () => false;
            }
            bool spy = Hooks.Install(typeof(SpyAsks), "medals", "Spy Specs' HP and Free do nothing (Both still works)");
            log.LogInfo("[medals] installed on MainManager.BadgeIsEquipped"
                + (saves ? ", SaveFile, Load and SetVariables" : "")
                + (spy ? ", StartBattle's and Tattle's MoveNext and ShowItemList" : ""));
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
            if ((id == HardModeMedal && (hard() || payingPrize)) || (id == DetectorMedal && detector())
                || (id == SpySpecsMedal && SpyOn()))
            {
                __result = true;
            }
        }

        // Spy Specs in halves, by where the game asks: a battle's start (every enemy's HP bar), or the Spy action (no
        // aim, the turn kept) and the battle menu's icon beside Spy. Anywhere else only with both, as the medal.
        private enum SpyAsk { Elsewhere, BattleStart, Spy, Menu }
        private static SpyAsk spyAsk;

        private static bool SpyOn()
        {
            bool on = spyAsk == SpyAsk.BattleStart ? spyHp()
                : spyAsk == SpyAsk.Elsewhere ? spyHp() && spyFree()
                : spyFree();
            if (on && spyAsk == SpyAsk.BattleStart)
            {
                log.LogInfo("[medals] Spy Specs: every enemy's HP bar shows this battle");
            }
            else if (on && spyAsk == SpyAsk.Spy)
            {
                log.LogInfo("[medals] Spy Specs: Spy with no aim, keeping the turn");
            }
            return on;
        }

        // Each sets who is asking for its own run and puts the outer one back, so a nested call can't clear it.
        private static class SpyAsks
        {
            [HarmonyPatch(typeof(BattleControl), nameof(BattleControl.StartBattle), MethodType.Enumerator)]
            [HarmonyPrefix]
            private static void BeforeStart(out SpyAsk __state) => Enter(SpyAsk.BattleStart, out __state);

            [HarmonyPatch(typeof(BattleControl), nameof(BattleControl.StartBattle), MethodType.Enumerator)]
            [HarmonyFinalizer]
            private static void AfterStart(SpyAsk __state) => spyAsk = __state;

            [HarmonyPatch(typeof(BattleControl), "Tattle", MethodType.Enumerator)]
            [HarmonyPrefix]
            private static void BeforeSpy(out SpyAsk __state) => Enter(SpyAsk.Spy, out __state);

            [HarmonyPatch(typeof(BattleControl), "Tattle", MethodType.Enumerator)]
            [HarmonyFinalizer]
            private static void AfterSpy(SpyAsk __state) => spyAsk = __state;

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShowItemList), typeof(int),
                typeof(UnityEngine.Vector2), typeof(bool), typeof(bool))]
            [HarmonyPrefix]
            private static void BeforeList(out SpyAsk __state) => Enter(SpyAsk.Menu, out __state);

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShowItemList), typeof(int),
                typeof(UnityEngine.Vector2), typeof(bool), typeof(bool))]
            [HarmonyFinalizer]
            private static void AfterList(SpyAsk __state) => spyAsk = __state;

            private static void Enter(SpyAsk asking, out SpyAsk outer)
            {
                outer = spyAsk;
                spyAsk = asking;
            }
        }
    }
}
