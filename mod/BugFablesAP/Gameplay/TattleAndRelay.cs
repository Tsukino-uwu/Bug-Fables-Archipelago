using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // In a seed the field's Tattle is there from the start and works with any party, and the battle menu shows Strategy
    // and Relay from the start. The flags (10 Tattle, 15 Strategy, 16 Relay) stay the game's: scenes wait on them (flag
    // 10 is the limit of the horn tutorial's Wood Boring), so only the places that read them for these are patched.
    internal static class TattleAndRelay
    {
        private const int Tattle = 10;
        // Set by the overseer's escort (Event102) while it runs; it turns Tattle off for that long, which is kept.
        private const int EscortTouch = 102;
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static readonly HashSet<string> reported = new HashSet<string>();

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(AnyTattler), "tattle", "Tattle still needs its tutorial and Kabbu in the party");
            Hooks.Install(typeof(DigCheck), "tattle", "the pause menu's dig and Bed Bug still wait for the tutorial");
            Hooks.Install(typeof(BattleMenu), "relay", "the battle menu waits for the story's Strategy and Relay");
        }

        private static bool On => randomizerOn != null && randomizerOn();

        // PlayerControl.GetInput answers the help key only once Tattle is taught (flags[10]) and with Kabbu in the
        // party (HasPlayer(1)), and speaks from his character (GetEntity(-5)): in a seed, taught, and any member will
        // do, the leader speaking for a missing Kabbu.
        [HarmonyPatch(typeof(PlayerControl), "GetInput")]
        private static class AnyTattler
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "tattle");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo hasPlayer = AccessTools.Method(typeof(MainManager), nameof(MainManager.HasPlayer));
                MethodInfo getEntity = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetEntity),
                    new[] { typeof(int) });
                int[] asks = Calls(code, hasPlayer, 1);
                int[] speakers = Calls(code, getEntity, -5);
                int[] taught = FlagReads(code, Tattle);
                if (asks.Length != 1 || speakers.Length != 2 || taught.Length != 1)
                {
                    log.LogError($"[tattle] NOT installed in PlayerControl.GetInput: expected one HasPlayer(1), two "
                        + $"GetEntity(-5) and one read of flag {Tattle}, found {asks.Length}, {speakers.Length} and "
                        + $"{taught.Length}");
                    return code;
                }
                ReadAs(code, taught[0], nameof(TattleTaught));
                code[asks[0]].operand = AccessTools.Method(typeof(TattleAndRelay), nameof(InParty));
                foreach (int at in speakers)
                {
                    code[at].operand = AccessTools.Method(typeof(TattleAndRelay), nameof(Speaker));
                }
                log.LogInfo("[tattle] installed in PlayerControl.GetInput: Tattle from the start, any member tattles");
                return code;
            }
        }

        // PauseMenu.CanDig, behind the pause menu's dig (key item 37) and the Bed Bug's rest (89), reads flags[10] too.
        [HarmonyPatch(typeof(PauseMenu), "CanDig")]
        private static class DigCheck
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "tattle");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                int[] taught = FlagReads(code, Tattle);
                if (taught.Length != 1)
                {
                    log.LogError($"[tattle] NOT installed in PauseMenu.CanDig: expected one read of flag {Tattle}, "
                        + $"found {taught.Length}");
                    return code;
                }
                ReadAs(code, taught[0], nameof(TattleTaught));
                log.LogInfo("[tattle] installed in PauseMenu.CanDig: the dig and the Bed Bug as if Tattle were taught");
                return code;
            }
        }

        // BattleControl.SetMaxOptions counts Strategy from flag 15 and Relay from 16; in a seed both count. Relay greys
        // itself out with one member (the same method's own check).
        [HarmonyPatch(typeof(BattleControl), "SetMaxOptions")]
        private static class BattleMenu
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "relay");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                int[] reads = FlagReads(code, 15).Concat(FlagReads(code, 16)).ToArray();
                if (reads.Length != 2)
                {
                    log.LogError("[relay] NOT installed in BattleControl.SetMaxOptions: expected the reads of flags 15 "
                        + $"and 16, found {reads.Length}");
                    return code;
                }
                foreach (int at in reads)
                {
                    ReadAs(code, at, nameof(MenuFlag));
                }
                log.LogInfo("[relay] installed in BattleControl.SetMaxOptions: Strategy and Relay shown in a seed");
                return code;
            }
        }

        // The call at each index whose argument is the constant just before it.
        private static int[] Calls(List<CodeInstruction> code, MethodInfo method, int argument) =>
            Enumerable.Range(1, code.Count - 1).Where(i => code[i].Calls(method) && code[i - 1].LoadsConstant(argument))
                .ToArray();

        // A flag read, flags[n]: ldc n, ldelem.u1.
        private static int[] FlagReads(List<CodeInstruction> code, int flag) =>
            Enumerable.Range(1, code.Count - 1)
                .Where(i => code[i].opcode == OpCodes.Ldelem_U1 && code[i - 1].LoadsConstant(flag)).ToArray();

        // The read becomes a call taking the same array and index, changed in place so a label on it stays (this game's
        // HarmonyX can't move CodeInstruction.labels; see FrameSites.cs).
        private static void ReadAs(List<CodeInstruction> code, int at, string method)
        {
            code[at].opcode = OpCodes.Call;
            code[at].operand = AccessTools.Method(typeof(TattleAndRelay), method);
        }

        private static bool TattleTaught(bool[] flags, int flag)
        {
            if (flags[flag] || !On)
            {
                return flags[flag];
            }
            bool escort = MainManager.instance.entitytouchevent == EscortTouch;
            Report(escort ? "escort" : "taught", escort
                ? $"[tattle] flag {flag} off during the overseer's escort: Tattle stays off, as the escort wants"
                : $"[tattle] flag {flag} not set: read as set in a seed (Tattle from the start)");
            return !escort;
        }

        private static bool InParty(int member)
        {
            MainManager mm = MainManager.instance;
            if (MainManager.HasPlayer(member) || !On || mm.playerdata == null || mm.playerdata.Length == 0)
            {
                return MainManager.HasPlayer(member);
            }
            Report("party", $"[tattle] member {member} not in the party: the others tattle");
            return true;
        }

        private static EntityControl Speaker(int member)
        {
            EntityControl own = MainManager.GetEntity(member);
            if (own != null || !On)
            {
                return own;
            }
            EntityControl leader = MainManager.instance.playerdata[0].entity;
            string who = leader != null ? leader.name : "none";
            Report("speaker", $"[tattle] member {member} missing: the leader ({who}) speaks");
            return leader;
        }

        private static bool MenuFlag(bool[] flags, int flag) => flags[flag] || On;

        // Once per map and kind, so the log says what each guard decided without repeating it every frame.
        private static void Report(string kind, string message)
        {
            string map = MainManager.map != null ? MainManager.map.mapid.ToString() : "no map";
            if (reported.Add(kind + " " + map))
            {
                log.LogInfo(message + " on " + map);
            }
        }
    }
}
