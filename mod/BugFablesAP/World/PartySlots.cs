using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Fights use a member's number as a party slot in places, true in vanilla, where the story's party is Vi, Kabbu and
    // Leif in that order. With members as items the party can be any of them in any order, so each such place asks here
    // for the member's slot. Only while Archipelago is on.
    internal static class PartySlots
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(BattleStart), "party",
                "a party of two with Leif in front never finishes loading a battle");
        }

        // The slot holding this member. Without him: the number itself while it is a slot (a scene's own small party,
        // whose member stands in for him), else the leader's slot. Always a slot the party has.
        internal static int SlotOfMember(int member)
        {
            try
            {
                MainManager.BattleData[] party = MainManager.instance?.playerdata;
                if (party == null || party.Length == 0 || randomizerOn == null || !randomizerOn())
                {
                    return member;
                }
                for (int i = 0; i < party.Length; i++)
                {
                    if (party[i].trueid == member)
                    {
                        return i;
                    }
                }
                return member >= 0 && member < party.Length ? member : 0;
            }
            catch (Exception e)
            {
                log?.LogError($"[party] finding member {member}'s slot threw: {e.GetBaseException().Message}");
                return member;
            }
        }

        private static BattleControl leaderLogged;
        private static BattleControl strikeLogged;

        // The field leader (partyorder holds members) put in front at a battle's start.
        private static int LeaderSlot(int member)
        {
            int slot = SlotOfMember(member);
            if (slot != member && MainManager.battle != leaderLogged)
            {
                leaderLogged = MainManager.battle;
                log.LogInfo($"[party] battle start: the field leader, member {member}, is slot {slot} of "
                    + $"{MainManager.instance.playerdata.Length}; that slot goes in front");
            }
            return slot;
        }

        // A dizzy enemy's first strike goes to whoever the start put in front.
        private static int StrikeSlot(int member)
        {
            int slot = SlotOfMember(member);
            if (slot != member && MainManager.battle != strikeLogged)
            {
                strikeLogged = MainManager.battle;
                log.LogInfo($"[party] first strike: member {member} is slot {slot}");
            }
            return slot;
        }

        private static class BattleStart
        {
            [HarmonyPatch(typeof(BattleControl), nameof(BattleControl.StartBattle), MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "party");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                FieldInfo partyorder = AccessTools.Field(typeof(MainManager), nameof(MainManager.partyorder));
                FieldInfo partypointer = AccessTools.Field(typeof(BattleControl), nameof(BattleControl.partypointer));
                FieldInfo battle = AccessTools.Field(typeof(MainManager), nameof(MainManager.battle));
                FieldInfo trueid = AccessTools.Field(typeof(MainManager.BattleData),
                    nameof(MainManager.BattleData.trueid));
                // while (battle.partypointer[0] != instance.partyorder[0]): partyorder[0] read.
                List<int> leader = Enumerable.Range(2, Math.Max(0, code.Count - 2))
                    .Where(i => code[i].opcode == OpCodes.Ldelem_I4 && code[i - 1].LoadsConstant(0)
                        && Reads(code[i - 2], partyorder))
                    .ToList();
                // if (playerdata[n].trueid == battle.partypointer[0]): the trueid read.
                List<int> strike = Enumerable.Range(0, Math.Max(0, code.Count - 2))
                    .Where(i => Reads(code[i], trueid) && Reads(code[i + 1], battle)
                        && Reads(code[i + 2], partypointer))
                    .ToList();
                if (leader.Count != 1 || strike.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"expected the leader loop and the first strike once each, found {leader.Count} and {strike.Count}");
                }
                MethodInfo leaderSlot = AccessTools.Method(typeof(PartySlots), nameof(LeaderSlot));
                MethodInfo strikeSlot = AccessTools.Method(typeof(PartySlots), nameof(StrikeSlot));
                // From the end, so the earlier index stays valid.
                foreach (KeyValuePair<int, MethodInfo> edit in new[]
                    {
                        new KeyValuePair<int, MethodInfo>(leader[0], leaderSlot),
                        new KeyValuePair<int, MethodInfo>(strike[0], strikeSlot),
                    }.OrderByDescending(e => e.Key))
                {
                    code.Insert(edit.Key + 1, new CodeInstruction(OpCodes.Call, edit.Value));
                }
                log.LogInfo("[party] installed in BattleControl.StartBattle (the leader at the start 1 of 1, "
                    + "the first strike 1 of 1)");
                return code;
            }

            private static bool Reads(CodeInstruction i, FieldInfo field) =>
                (i.opcode == OpCodes.Ldfld || i.opcode == OpCodes.Ldsfld) && Equals(i.operand, field);
        }
    }
}
