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
    // and Relay from the start; the story's flags 15 and 16 stay the game's, since their scenes (the opening's tutorial
    // battle, Leif's joining) wait on them.
    internal static class TattleAndRelay
    {
        private const int TattleFlag = 10;
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(TattleTaught), "tattle", "Tattle waits for its tutorial");
            Hooks.Install(typeof(AnyTattler), "tattle", "Tattle still needs Kabbu in the party");
            Hooks.Install(typeof(BattleMenu), "relay", "the battle menu waits for the story's Strategy and Relay");
        }

        private static bool On => randomizerOn != null && randomizerOn();

        // Set as a map loads, as the Tattle tutorial (Event2) sets it; the overseer's escort turns it off and on again
        // itself (Event102), so its room is left alone.
        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        private static class TattleTaught
        {
            [HarmonyPrefix]
            private static void BeforeCreate(MapControl __instance)
            {
                MainManager mm = MainManager.instance;
                if (!On || mm == null || mm.flags[TattleFlag]
                    || __instance.mapid == MainManager.Maps.FactoryStorageOverseer)
                {
                    return;
                }
                mm.flags[TattleFlag] = true;
                log.LogInfo($"[tattle] flag {TattleFlag} set on {__instance.mapid}: Tattle on from the start in a seed");
            }
        }

        // PlayerControl.GetInput answers the help key only with Kabbu in the party (HasPlayer(1)) and speaks from his
        // character (GetEntity(-5)): any member will do, the leader speaking for a missing Kabbu.
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
                if (asks.Length != 1 || speakers.Length != 2)
                {
                    log.LogError($"[tattle] NOT installed in PlayerControl.GetInput: expected one HasPlayer(1) and two "
                        + $"GetEntity(-5), found {asks.Length} and {speakers.Length}");
                    return code;
                }
                code[asks[0]].operand = AccessTools.Method(typeof(TattleAndRelay), nameof(InParty));
                foreach (int at in speakers)
                {
                    code[at].operand = AccessTools.Method(typeof(TattleAndRelay), nameof(Speaker));
                }
                log.LogInfo("[tattle] installed in PlayerControl.GetInput: any member tattles");
                return code;
            }
        }

        // The call at each index whose argument is the constant just before it.
        private static int[] Calls(List<CodeInstruction> code, MethodInfo method, int argument) =>
            Enumerable.Range(1, code.Count - 1).Where(i => code[i].Calls(method) && code[i - 1].LoadsConstant(argument))
                .ToArray();

        private static bool InParty(int member)
        {
            MainManager mm = MainManager.instance;
            return MainManager.HasPlayer(member) || (On && mm.playerdata != null && mm.playerdata.Length > 0);
        }

        private static EntityControl Speaker(int member)
        {
            EntityControl own = MainManager.GetEntity(member);
            return own != null || !On ? own : MainManager.instance.playerdata[0].entity;
        }

        // BattleControl.SetMaxOptions counts Strategy from flag 15 and Relay from 16; in a seed both count. Relay greys
        // itself out with one member (the same method's own check).
        [HarmonyPatch(typeof(BattleControl), "SetMaxOptions")]
        private static class BattleMenu
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "relay");

            // flags[15] and flags[16]: ldc 15 or 16, ldelem.u1.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                int[] reads = Enumerable.Range(1, code.Count - 1).Where(i => code[i].opcode == OpCodes.Ldelem_U1
                    && (code[i - 1].LoadsConstant(15) || code[i - 1].LoadsConstant(16))).ToArray();
                if (reads.Length != 2)
                {
                    log.LogError($"[relay] NOT installed in BattleControl.SetMaxOptions: expected the reads of flags 15 "
                        + $"and 16, found {reads.Length}");
                    return code;
                }
                // Changed in place, so any label on the read stays on it.
                foreach (int at in reads)
                {
                    code[at].opcode = OpCodes.Call;
                    code[at].operand = AccessTools.Method(typeof(TattleAndRelay), nameof(MenuFlag));
                }
                log.LogInfo("[relay] installed in BattleControl.SetMaxOptions: Strategy and Relay shown in a seed");
                return code;
            }
        }

        private static bool MenuFlag(bool[] flags, int flag) => flags[flag] || On;
    }
}
