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
    // Hardest is the HARDEST code's level: its extras read flag 614 directly (about 35 places), so it means that flag.
    // The game keeps no other record of a typed code (EventControl.cs:2450-2455), so the save must never be left
    // with a 614 the panel put there (the user, 2026-09-24: switchable, the save stays clean). While the panel says
    // Hardest the flag is on in play; SaveFile writes the save's own value (a prefix clears the panel's 614, a
    // postfix puts it back); switching down clears only a 614 the panel set. Loading a save or starting a new one
    // forgets the panel's mark, since the flags then are the save's own.
    // Only on randomizer saves: with the Archipelago mod disabled, the game is untouched.
    internal static class MedalAssist
    {
        internal const int HardModeMedal = 11;
        internal const int DetectorMedal = 2;

        private static Func<bool> active;
        private static Func<bool> hard;
        private static Func<bool> detector;
        private static Func<bool> hardest;
        private static Harmony harmony;
        private static ManualLogSource log;

        internal const int HardestFlag = 614;
        // Whether flag 614 is on because the panel put it there (not the save's own).
        private static bool forced;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerOn, Func<bool> hardOn,
            Func<bool> hardestOn, Func<bool> detectorOn)
        {
            log = logger;
            active = randomizerOn;
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
                // Without all three, a save could keep the panel's 614: Hardest stays off instead.
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
            // A hot reload must not leave the panel's 614 behind: the new instance couldn't tell it from the save's.
            if (forced && MainManager.instance?.flags != null)
            {
                MainManager.instance.flags[HardestFlag] = false;
                forced = false;
            }
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // Every frame: keep flag 614 in step with the panel, touching only a 614 the panel set.
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

        // lite loads are the file select's previews; a full load replaces the flags with the save's own.
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

        // Boss prize medals are always paid out as if Hard Mode were on (the user, 2026-09-24), so a prize can never be
        // skipped and its location is "beat this boss". Most bosses test Hard Mode in their own event and, when it's
        // off, write their prize slot as missed (2) directly (e.g. Event26: flagvar[13] = 2; eight events do this);
        // others go through AddPrizeMedal. So each tick, a slot reading 2 is paid properly by the game's own
        // AddPrizeMedal(slot) with Hard Mode answered "yes" for that call: it writes 1 (earned), sets flag 56 (a prize
        // waits at Artis, whose Event33 hands it over) and counts it (flagvar[55]), as the Hard Mode path does
        // (MainManager.cs:3981). Never in battle (a retry rolls flagvar back) or in an event.
        private static bool payingPrize;

        internal static void PayPrizes()
        {
            MainManager mm = MainManager.instance;
            if (active == null || !active() || mm == null || mm.flagvar == null || mm.prizeflags == null || MainManager.map == null
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
