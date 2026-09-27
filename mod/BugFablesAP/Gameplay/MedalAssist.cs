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
        private static Harmony harmony;
        private static ManualLogSource log;

        internal const int HardestFlag = 614;
        private static bool forced;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerOn, Func<bool> settingsOn,
            Func<bool> hardOn, Func<bool> hardestOn, Func<bool> detectorOn)
        {
            log = logger;
            active = settingsOn;
            randomizer = randomizerOn;
            hard = hardOn;
            hardest = hardestOn;
            detector = detectorOn;
            var method = AccessTools.Method(typeof(MainManager), nameof(MainManager.BadgeIsEquipped), new[] { typeof(int), typeof(int) });
            if (method == null)
            {
                log.LogError("[medals] NOT installed: MainManager.BadgeIsEquipped(int, int) wasn't found; Difficulty and Detector do nothing.");
                return;
            }
            harmony = new Harmony(guid + ".medals." + DateTime.UtcNow.Ticks);
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(MedalAssist), nameof(Postfix)));
            var save = AccessTools.Method(typeof(MainManager), nameof(MainManager.SaveFile), new[] { typeof(UnityEngine.Vector3?) });
            var load = AccessTools.Method(typeof(MainManager), nameof(MainManager.Load), new[] { typeof(int), typeof(bool) });
            var reset = AccessTools.Method(typeof(MainManager), nameof(MainManager.SetVariables));
            if (save == null || load == null || reset == null)
            {
                // Without all three a save could keep the panel's 614, so Hardest stays off.
                log.LogError($"[medals] Hardest NOT installed (SaveFile {save != null}, Load {load != null}, "
                    + $"SetVariables {reset != null}); Hardest does nothing.");
                hardest = () => false;
            }
            else
            {
                harmony.Patch(save, prefix: new HarmonyMethod(typeof(MedalAssist), nameof(BeforeSave)),
                    postfix: new HarmonyMethod(typeof(MedalAssist), nameof(AfterSave)));
                harmony.Patch(load, postfix: new HarmonyMethod(typeof(MedalAssist), nameof(AfterLoad)));
                harmony.Patch(reset, postfix: new HarmonyMethod(typeof(MedalAssist), nameof(AfterReset)));
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
            harmony?.UnpatchSelf();
            harmony = null;
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

        private static void BeforeSave(ref bool __state)
        {
            __state = forced && MainManager.instance?.flags != null;
            if (__state)
            {
                MainManager.instance.flags[HardestFlag] = false;
            }
        }

        private static void AfterSave(bool __state)
        {
            if (__state)
            {
                MainManager.instance.flags[HardestFlag] = true;
            }
        }

        // lite loads are the file select's previews.
        private static void AfterLoad(bool lite)
        {
            if (!lite)
            {
                forced = false;
            }
        }

        private static void AfterReset()
        {
            forced = false;
        }

        // Boss prizes are always paid as if Hard Mode were on: a prize slot reading 2 (missed) is paid by the game's own
        // AddPrizeMedal with Hard Mode answered yes for that call. Never in battle (a retry rolls flagvar back).
        private static bool payingPrize;

        internal static void PayPrizes()
        {
            MainManager mm = MainManager.instance;
            if (randomizer == null || !randomizer() || mm == null || mm.flagvar == null || mm.prizeflags == null || MainManager.map == null
                || mm.inbattle || MainManager.battle != null || mm.inevent)
            {
                return;
            }
            for (int i = 0; i < mm.prizeflags.Length; i++)
            {
                if (mm.flagvar[mm.prizeflags[i]] != 2)
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
