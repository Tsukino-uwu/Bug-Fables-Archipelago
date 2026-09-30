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
            Hooks.Install(typeof(Eaten), "party", "a member eaten in a party the story never had can freeze a fight");
            Hooks.Install(typeof(SkillSlots), "party",
                "Heavy Strike and the Vi and Leif team attack read whoever stands in the member's slot");
            Hooks.Install(typeof(FightEvents), "party",
                "a fight's scripted line for a member the party lacks can stop the fight");
            Hooks.Install(typeof(ScriptedFights), "party",
                "the Beast, Zommoth and the Everlasting King still name members by slot");
            Hooks.Install(typeof(ScriptedScenes), "party",
                "the Beast's and Zommoth's scenes still name members by slot");
            Hooks.Install(typeof(SceneLineups), "party",
                "four later scenes still crash placing a member the party lacks");
        }

        private static readonly HashSet<string> positionLogged = new HashSet<string>();

        // A scene's k-th character in line: past the party's end, the stand-in scenes use for that place (step 11),
        // or nobody's.
        private static ref MainManager.BattleData ScenePosition(MainManager.BattleData[] party, int k, int evt)
        {
            if (!On || k < party.Length)
            {
                return ref party[k];
            }
            ref MainManager.BattleData standIn = ref Nobody();
            try
            {
                standIn.entity = MainManager.GetEntity(-1 - k);
            }
            catch (Exception e)
            {
                log.LogError($"[party] Event{evt}: finding the stand-in for place {k} threw: {e.GetBaseException().Message}");
            }
            if (positionLogged.Add($"{evt}/{k}/{party.Length}"))
            {
                log.LogInfo($"[party] Event{evt}: place {k} is beyond a party of {party.Length}: "
                    + (standIn.entity != null ? $"its stand-in ({standIn.entity.name}) placed" : "no stand-in"));
            }
            return ref standIn;
        }

        private const int Beast = 69;
        private const int EverlastingKing = 91;
        private const int Zommoth = 96;

        // The Beast (Centipede): Vi and Leif are knocked out and Kabbu fights on, powered up. Kabbu's part goes to Kabbu,
        // else the leader; the other two to their own member if he's there and not already cast, else nobody.
        private static int SurvivorPart(int k)
        {
            int kabbu = SlotOf(1);
            if (kabbu < 0)
            {
                kabbu = SlotOfMember(MainManager.instance.partyorder[0]);
            }
            if (k == 1)
            {
                return kabbu;
            }
            int own = SlotOf(k);
            return own >= 0 && own != kabbu ? own : -1;
        }

        // Zommoth: Leif sits the fight out until the beam. Only Leif, and never the party's only member.
        private static int LeifSitsOutPart(int k)
        {
            if (k != 2)
            {
                return MemberPart(k);
            }
            int leif = SlotOf(2);
            return leif >= 0 && MainManager.instance.playerdata.Length > 1 ? leif : -1;
        }

        // The Everlasting King's two exchanges: Vi and Kabbu (lines 199, 200) while data[2] is 0, else Kabbu and Leif
        // (201, 202). A missing member's line goes to someone other than the one he answers.
        private static int KingSpeaker(int k, int phase)
        {
            int partner = k == 1 ? (phase == 0 ? 0 : 2) : 1;
            return Speaker(k, SlotOf(partner));
        }

        private static readonly Dictionary<Type, FieldInfo[]> actionFields = new Dictionary<Type, FieldInfo[]>();

        // The enemy acting in a DoAction step, with its slot in enemydata; -1 for a member's own action.
        internal static int ActingEnemy(object step, out int actionid)
        {
            actionid = -1;
            Type type = step.GetType();
            if (!actionFields.TryGetValue(type, out FieldInfo[] fields))
            {
                actionFields[type] = fields = new[] { AccessTools.Field(type, "entity"), AccessTools.Field(type, "actionid") };
            }
            if (fields[0] == null || fields[1] == null || !(fields[0].GetValue(step) is EntityControl entity)
                || entity == null || entity.CompareTag("Player"))
            {
                return -1;
            }
            actionid = (int)fields[1].GetValue(step);
            MainManager.BattleData[] enemies = MainManager.battle?.enemydata;
            return enemies != null && actionid >= 0 && actionid < enemies.Length ? enemies[actionid].animid : -1;
        }

        private static int CastInFight(object step, int k, out string who)
        {
            int enemy = ActingEnemy(step, out int actionid);
            switch (enemy)
            {
                case Beast:
                    who = "the Beast";
                    return SurvivorPart(k);
                case Zommoth:
                    who = "Zommoth";
                    return LeifSitsOutPart(k);
                case EverlastingKing:
                    who = "the Everlasting King";
                    int[] data = MainManager.battle.enemydata[actionid].data;
                    return KingSpeaker(k, data != null && data.Length > 2 ? data[2] : 0);
                default:
                    who = enemy >= 0 ? ((MainManager.Enemies)enemy).ToString() : "a member's action";
                    return MemberPart(k);
            }
        }

        // The slot a fight's fixed read k means, -1 for nobody; the plain k with Archipelago off.
        private static int FightSlot(int k, object step)
        {
            if (!On)
            {
                return k;
            }
            try
            {
                int slot = CastInFight(step, k, out string who);
                if (slot != k)
                {
                    LogCast(who, k, slot);
                }
                return slot;
            }
            catch (Exception e)
            {
                log.LogError($"[party] casting a fight's member {k} threw: {e.GetBaseException().Message}");
                return k < MainManager.instance.playerdata.Length ? k : -1;
            }
        }

        private static ref MainManager.BattleData FightElem(MainManager.BattleData[] party, int k, object step)
        {
            int slot = FightSlot(k, step);
            if (slot < 0 || slot >= party.Length)
            {
                return ref Nobody();
            }
            return ref party[slot];
        }

        private static MainManager.BattleData FightValue(MainManager.BattleData[] party, int k, object step) =>
            FightElem(party, k, step);

        // A slot passed as an argument (the Beast's revive): never nobody, since the survivor always exists.
        private static int FightArgument(int k, object step)
        {
            int slot = FightSlot(k, step);
            return slot >= 0 ? slot : SlotOfMember(MainManager.instance.partyorder[0]);
        }

        private static readonly MethodInfo GetSingleTargetBySlot = AccessTools.Method(typeof(BattleControl),
            "GetSingleTarget", new[] { typeof(int) });
        private static readonly FieldInfo PlayerTargetId = AccessTools.Field(typeof(BattleControl), "playertargetID");
        private static readonly FieldInfo PlayerTargetEntity = AccessTools.Field(typeof(BattleControl),
            "playertargetentity");

        // The Beast's hits {Vi, Leif, Kabbu}: a part nobody plays is no target, and the hit that follows is skipped.
        private static void SingleTarget(BattleControl battle, int k, object step)
        {
            int slot = FightSlot(k, step);
            if (slot < 0)
            {
                PlayerTargetId.SetValue(battle, -1);
                PlayerTargetEntity.SetValue(battle, null);
                return;
            }
            GetSingleTargetBySlot.Invoke(battle, new object[] { slot });
        }

        private static ref MainManager.BattleData TargetElem(MainManager.BattleData[] party, int slot)
        {
            if (slot < 0 || slot >= party.Length)
            {
                return ref Nobody();
            }
            return ref party[slot];
        }

        private static void StartDeath(EntityControl entity)
        {
            if (entity != null)
            {
                entity.StartDeath();
            }
        }

        private static void SetOverrideAnim(EntityControl entity, bool value)
        {
            if (entity != null)
            {
                entity.overrideanim = value;
            }
        }

        private static void SetAnimState(EntityControl entity, int value)
        {
            if (entity != null)
            {
                entity.animstate = value;
            }
        }

        // playerdata[k] in a scene around a scripted fight, cast as the fight casts it.
        private static ref MainManager.BattleData SceneElem(MainManager.BattleData[] party, int k, int evt)
        {
            if (!On)
            {
                return ref party[k];
            }
            int slot;
            try
            {
                slot = evt == 137 ? SurvivorPart(k) : evt == 182 ? LeifSitsOutPart(k) : MemberPart(k);
            }
            catch (Exception e)
            {
                log.LogError($"[party] casting Event{evt}'s member {k} threw: {e.GetBaseException().Message}");
                slot = k < party.Length ? k : -1;
            }
            if (slot != k)
            {
                LogCast($"Event{evt}", k, slot);
            }
            if (slot < 0 || slot >= party.Length)
            {
                return ref Nobody();
            }
            return ref party[slot];
        }

        private static bool On => randomizerOn != null && randomizerOn() && MainManager.instance?.playerdata != null;

        private static int SlotOf(int member)
        {
            MainManager.BattleData[] party = MainManager.instance.playerdata;
            for (int i = 0; i < party.Length; i++)
            {
                if (party[i].trueid == member)
                {
                    return i;
                }
            }
            return -1;
        }

        // Member k's part: member k, else whoever stands in slot k (a scene's own small party puts the member playing
        // the part there), else nobody.
        private static int MemberPart(int k)
        {
            int slot = SlotOf(k);
            return slot >= 0 ? slot : k < MainManager.instance.playerdata.Length ? k : -1;
        }

        // A line for member k: member k, else a member the story doesn't have yet (as scenes cast him), else anyone
        // but the one he answers (avoid), else the leader. Never nobody while the party has anyone.
        private static int Speaker(int k, int avoid = -1)
        {
            int slot = SlotOf(k);
            if (slot >= 0)
            {
                return slot;
            }
            MainManager.BattleData[] party = MainManager.instance.playerdata;
            for (int i = 0; i < party.Length; i++)
            {
                if (i != avoid && !PartyFit.InStoryParty(party[i].trueid))
                {
                    return i;
                }
            }
            for (int i = 0; i < party.Length; i++)
            {
                if (i != avoid)
                {
                    return i;
                }
            }
            return SlotOfMember(MainManager.instance.partyorder[0]);
        }

        // Nobody plays the part: reads find a member with no HP and no body, writes go nowhere.
        private static MainManager.BattleData scratch;

        private static ref MainManager.BattleData Nobody()
        {
            scratch = default;
            scratch.condition = new List<int[]>();
            scratch.weakness = new List<BattleControl.AttackProperty>();
            return ref scratch;
        }

        private static readonly Dictionary<Type, FieldInfo> idFields = new Dictionary<Type, FieldInfo>();
        private static readonly HashSet<string> castLogged = new HashSet<string>();
        private static BattleControl castBattle;

        private static int EventId(object step)
        {
            Type type = step.GetType();
            if (!idFields.TryGetValue(type, out FieldInfo field))
            {
                idFields[type] = field = AccessTools.Field(type, "id");
            }
            return field != null ? (int)field.GetValue(step) : -1;
        }

        private static void LogCast(string what, int k, int slot)
        {
            if (castBattle != MainManager.battle)
            {
                castBattle = MainManager.battle;
                castLogged.Clear();
            }
            string key = what + k;
            if (!castLogged.Add(key))
            {
                return;
            }
            string who = slot < 0 ? "nobody"
                : $"member {MainManager.instance.playerdata[slot].trueid} (slot {slot})";
            log.LogInfo($"[party] {what}: member {k}'s part played by {who}");
        }

        // playerdata[k] in a fight's scripted lines (EventDialogue), k a constant meaning member k.
        private static ref MainManager.BattleData EventDialogueElem(MainManager.BattleData[] party, int k, object step)
        {
            if (!On)
            {
                return ref party[k];
            }
            int slot;
            int id = -1;
            try
            {
                id = EventId(step);
                // Case 5, the spider's second fight: Kabbu's line, even with a party of one.
                slot = id == 5 ? Speaker(k) : MemberPart(k);
            }
            catch (Exception e)
            {
                log.LogError($"[party] casting EventDialogue {id}'s member {k} threw: {e.GetBaseException().Message}");
                slot = k < party.Length ? k : -1;
            }
            if (slot != k)
            {
                LogCast($"EventDialogue {id}", k, slot);
            }
            if (slot < 0)
            {
                return ref Nobody();
            }
            return ref party[slot];
        }

        private static IEnumerable<int> ConstantSlotReads(List<CodeInstruction> code)
        {
            FieldInfo playerdata = AccessTools.Field(typeof(MainManager), nameof(MainManager.playerdata));
            return Enumerable.Range(2, Math.Max(0, code.Count - 2))
                .Where(i => code[i].opcode == OpCodes.Ldelema && Equals(code[i].operand, typeof(MainManager.BattleData))
                    && (code[i - 1].LoadsConstant(0) || code[i - 1].LoadsConstant(1) || code[i - 1].LoadsConstant(2))
                    && Reads(code[i - 2], playerdata));
        }

        private static class ScriptedFights
        {
            [HarmonyPatch(typeof(BattleControl), "DoAction", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "party");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                FieldInfo playerdata = AccessTools.Field(typeof(MainManager), nameof(MainManager.playerdata));
                FieldInfo battleentity = AccessTools.Field(typeof(MainManager.BattleData),
                    nameof(MainManager.BattleData.battleentity));
                MethodInfo revive = AccessTools.Method(typeof(BattleControl), "RevivePlayer",
                    new[] { typeof(int), typeof(int), typeof(bool) });
                MethodInfo clearStatus = AccessTools.Method(typeof(BattleControl), "ClearStatus");
                MethodInfo startDeath = AccessTools.Method(typeof(EntityControl), nameof(EntityControl.StartDeath));
                FieldInfo overrideanim = AccessTools.Field(typeof(EntityControl), nameof(EntityControl.overrideanim));
                FieldInfo animstate = AccessTools.Field(typeof(EntityControl), nameof(EntityControl.animstate));
                bool IsConstSlot(int i) => i >= 2 && (code[i - 1].LoadsConstant(0) || code[i - 1].LoadsConstant(1)
                    || code[i - 1].LoadsConstant(2)) && Reads(code[i - 2], playerdata);
                bool IsBattleData(CodeInstruction c) => Equals(c.operand, typeof(MainManager.BattleData));

                List<int> refs = Enumerable.Range(0, code.Count)
                    .Where(i => code[i].opcode == OpCodes.Ldelema && IsBattleData(code[i]) && IsConstSlot(i)).ToList();
                List<int> values = Enumerable.Range(0, code.Count)
                    .Where(i => code[i].opcode == OpCodes.Ldelem && IsBattleData(code[i]) && IsConstSlot(i)).ToList();
                // The Beast's RevivePlayer(1, 5, false): the 1.
                List<int> revives = Enumerable.Range(0, Math.Max(0, code.Count - 3))
                    .Where(i => code[i].LoadsConstant(1) && code[i + 1].LoadsConstant(5) && code[i + 2].LoadsConstant(0)
                        && code[i + 3].Calls(revive)).ToList();
                // Its only GetSingleTarget(int), then the first playerdata[playertargetID] after it.
                List<int> targets = Enumerable.Range(0, code.Count)
                    .Where(i => code[i].Calls(GetSingleTargetBySlot)).ToList();
                int targetRead = targets.Count == 1
                    ? code.FindIndex(targets[0], c => c.opcode == OpCodes.Ldelema && IsBattleData(c)) : -1;
                // ClearStatus(ref playerdata[hits]).
                List<int> clears = Enumerable.Range(3, Math.Max(0, code.Count - 4))
                    .Where(i => code[i].opcode == OpCodes.Ldelema && code[i + 1].Calls(clearStatus)
                        && code[i - 1].opcode == OpCodes.Ldfld && code[i - 1].operand is FieldInfo f
                        && f.Name.StartsWith("<hits>") && code[i - 2].opcode == OpCodes.Ldarg_0
                        && Reads(code[i - 3], playerdata)).ToList();
                // A body a nobody part has none of: StartDeath() and the animation stores right after a fixed read.
                List<int> deaths = refs.Where(i => i + 2 < code.Count && Reads(code[i + 1], battleentity)
                    && code[i + 2].opcode == OpCodes.Callvirt && Equals(code[i + 2].operand, startDeath))
                    .Select(i => i + 2).ToList();
                List<int> anims = refs.Where(i => i + 3 < code.Count && Reads(code[i + 1], battleentity)
                    && code[i + 3].opcode == OpCodes.Stfld
                    && (Equals(code[i + 3].operand, overrideanim) || Equals(code[i + 3].operand, animstate)))
                    .Select(i => i + 3).ToList();
                if (refs.Count != 29 || values.Count != 2 || revives.Count != 1 || targets.Count != 1 || targetRead < 0
                    || clears.Count != 1 || deaths.Count != 2 || anims.Count != 6)
                {
                    throw new InvalidOperationException(
                        $"expected 29 fixed reads, 2 by value, 1 revive, 1 target and its read, 1 ClearStatus, "
                        + $"2 deaths, 6 animation stores; found {refs.Count}, {values.Count}, {revives.Count}, "
                        + $"{targets.Count} ({targetRead}), {clears.Count}, {deaths.Count}, {anims.Count}");
                }

                MethodInfo elem = AccessTools.Method(typeof(PartySlots), nameof(FightElem));
                MethodInfo value = AccessTools.Method(typeof(PartySlots), nameof(FightValue));
                MethodInfo argument = AccessTools.Method(typeof(PartySlots), nameof(FightArgument));
                MethodInfo single = AccessTools.Method(typeof(PartySlots), nameof(SingleTarget));
                MethodInfo targetElem = AccessTools.Method(typeof(PartySlots), nameof(TargetElem));
                var edits = new List<KeyValuePair<int, Action<int>>>();
                void Add(IEnumerable<int> at, Action<int> edit) =>
                    edits.AddRange(at.Select(i => new KeyValuePair<int, Action<int>>(i, edit)));
                // An array read becomes the step (this enumerator) and a call, changed in place.
                Action<int> ToCall(MethodInfo m) => i =>
                {
                    code[i].opcode = OpCodes.Ldarg_0;
                    code[i].operand = null;
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Call, m));
                };
                Add(refs.Concat(clears), ToCall(elem));
                Add(values, ToCall(value));
                Add(targets, ToCall(single));
                Add(revives, i => code.InsertRange(i + 1,
                    new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, argument) }));
                Add(new[] { targetRead }, i =>
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = targetElem;
                });
                Add(deaths, i =>
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = AccessTools.Method(typeof(PartySlots), nameof(StartDeath));
                });
                Add(anims, i =>
                {
                    string setter = Equals(code[i].operand, overrideanim) ? nameof(SetOverrideAnim) : nameof(SetAnimState);
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = AccessTools.Method(typeof(PartySlots), setter);
                });
                // From the end, so earlier indices stay valid.
                foreach (KeyValuePair<int, Action<int>> e in edits.OrderByDescending(e => e.Key))
                {
                    e.Value(e.Key);
                }
                log.LogInfo("[party] installed in BattleControl.DoAction (fixed reads 29 of 29 and 2 of 2, the Beast's "
                    + "revive, target, status and knock-outs, 6 of 6 animation stores)");
                return code;
            }
        }

        private static class ScriptedScenes
        {
            [HarmonyPatch(typeof(EventControl), "Event137", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileBeast(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, 137, 1, 3), "party");

            [HarmonyPatch(typeof(EventControl), "Event182", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileZommoth(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, 182, 2, 1), "party");

            // playerdata[k]: the Beast's scene reads Kabbu's lockitems 3 times, Zommoth's freezes Leif once.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code, int evt, int k, int expected)
            {
                FieldInfo playerdata = AccessTools.Field(typeof(MainManager), nameof(MainManager.playerdata));
                List<int> reads = Enumerable.Range(2, Math.Max(0, code.Count - 2))
                    .Where(i => code[i].opcode == OpCodes.Ldelema
                        && Equals(code[i].operand, typeof(MainManager.BattleData))
                        && code[i - 1].LoadsConstant(k) && Reads(code[i - 2], playerdata))
                    .ToList();
                if (reads.Count != expected)
                {
                    throw new InvalidOperationException($"expected Event{evt}'s {expected} reads of slot {k}, found {reads.Count}");
                }
                MethodInfo sceneElem = AccessTools.Method(typeof(PartySlots), nameof(SceneElem));
                foreach (int i in reads.OrderByDescending(i => i))
                {
                    code[i].opcode = OpCodes.Ldc_I4;
                    code[i].operand = evt;
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Call, sceneElem));
                }
                log.LogInfo($"[party] installed in EventControl.Event{evt} (slot {k} {expected} of {expected})");
                return code;
            }
        }

        private static class SceneLineups
        {
            [HarmonyPatch(typeof(EventControl), "Event52", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile52(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, 52, 3, 0), "party");

            [HarmonyPatch(typeof(EventControl), "Event122", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile122(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, 122, 2, 0), "party");

            [HarmonyPatch(typeof(EventControl), "Event130", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile130(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, 130, 1, 0), "party");

            [HarmonyPatch(typeof(EventControl), "Event138", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile138(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, 138, 1, 4), "party");

            // playerdata[1] and [2] by place in line; Event138 also indexes by partyorder[k], a member's number.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code, int evt, int places, int byMember)
            {
                FieldInfo playerdata = AccessTools.Field(typeof(MainManager), nameof(MainManager.playerdata));
                FieldInfo partyorder = AccessTools.Field(typeof(MainManager), nameof(MainManager.partyorder));
                List<int> reads = Enumerable.Range(2, Math.Max(0, code.Count - 2))
                    .Where(i => code[i].opcode == OpCodes.Ldelema
                        && Equals(code[i].operand, typeof(MainManager.BattleData))
                        && (code[i - 1].LoadsConstant(1) || code[i - 1].LoadsConstant(2))
                        && Reads(code[i - 2], playerdata))
                    .ToList();
                List<int> members = Enumerable.Range(2, Math.Max(0, code.Count - 3))
                    .Where(i => code[i].opcode == OpCodes.Ldelem_I4 && Reads(code[i - 2], partyorder)
                        && code[i + 1].opcode == OpCodes.Ldelema
                        && Equals(code[i + 1].operand, typeof(MainManager.BattleData)))
                    .ToList();
                if (reads.Count != places || members.Count != byMember)
                {
                    throw new InvalidOperationException($"expected Event{evt}'s {places} places and {byMember} member reads, "
                        + $"found {reads.Count} and {members.Count}");
                }
                MethodInfo position = AccessTools.Method(typeof(PartySlots), nameof(ScenePosition));
                MethodInfo slotOfMember = AccessTools.Method(typeof(PartySlots), nameof(SlotOfMember));
                var edits = reads.Select(i => new KeyValuePair<int, bool>(i, true))
                    .Concat(members.Select(i => new KeyValuePair<int, bool>(i, false)))
                    .OrderByDescending(e => e.Key);
                foreach (KeyValuePair<int, bool> e in edits)
                {
                    if (e.Value)
                    {
                        code[e.Key].opcode = OpCodes.Ldc_I4;
                        code[e.Key].operand = evt;
                        code.Insert(e.Key + 1, new CodeInstruction(OpCodes.Call, position));
                    }
                    else
                    {
                        code.Insert(e.Key + 1, new CodeInstruction(OpCodes.Call, slotOfMember));
                    }
                }
                log.LogInfo($"[party] installed in EventControl.Event{evt} (places {places} of {places}"
                    + (byMember > 0 ? $", member reads {byMember} of {byMember})" : ")"));
                return code;
            }
        }

        private static class FightEvents
        {
            [HarmonyPatch(typeof(BattleControl), "EventDialogue", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "party");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                List<int> reads = ConstantSlotReads(code).ToList();
                if (reads.Count != 20)
                {
                    throw new InvalidOperationException($"expected 20 fixed slot reads, found {reads.Count}");
                }
                MethodInfo elem = AccessTools.Method(typeof(PartySlots), nameof(EventDialogueElem));
                foreach (int i in reads.OrderByDescending(i => i))
                {
                    // The ldelema becomes the step (this enumerator), then the call: changed in place.
                    code[i].opcode = OpCodes.Ldarg_0;
                    code[i].operand = null;
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Call, elem));
                }
                log.LogInfo("[party] installed in BattleControl.EventDialogue (fixed slot reads 20 of 20)");
                return code;
            }
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

        private static BattleControl eatenLogged;
        private static BattleControl skillLogged;

        // An eaten member's HP, read by his number.
        private static int EatenSlot(int member)
        {
            int slot = SlotOfMember(member);
            if (slot != member && MainManager.battle != eatenLogged)
            {
                eatenLogged = MainManager.battle;
                log.LogInfo($"[party] eaten: member {member} read from slot {slot}");
            }
            return slot;
        }

        // A skill that names its member (Heavy Strike: Kabbu; the Vi and Leif team attack) by his number.
        private static int SkillSlot(int member)
        {
            int slot = SlotOfMember(member);
            if (slot != member && MainManager.battle != skillLogged)
            {
                skillLogged = MainManager.battle;
                log.LogInfo($"[party] a skill's member {member} is slot {slot}");
            }
            return slot;
        }

        private static bool Reads(CodeInstruction i, FieldInfo field) =>
            (i.opcode == OpCodes.Ldfld || i.opcode == OpCodes.Ldsfld) && Equals(i.operand, field);

        private static FieldInfo TrueId => AccessTools.Field(typeof(MainManager.BattleData),
            nameof(MainManager.BattleData.trueid));

        private static class Eaten
        {
            [HarmonyPatch(typeof(BattleControl), "AdvanceTurnEntity")]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "party");

            // playerdata[t.trueid].hp, twice in the Eaten tick (the other trueid reads name a medal's wearer).
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                List<int> reads = Enumerable.Range(1, Math.Max(0, code.Count - 2))
                    .Where(i => code[i - 1].opcode == OpCodes.Ldarg_1 && Reads(code[i], TrueId)
                        && code[i + 1].opcode == OpCodes.Ldelema)
                    .ToList();
                if (reads.Count != 2)
                {
                    throw new InvalidOperationException($"expected the eaten tick's two reads, found {reads.Count}");
                }
                MethodInfo eatenSlot = AccessTools.Method(typeof(PartySlots), nameof(EatenSlot));
                foreach (int i in reads.OrderByDescending(i => i))
                {
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Call, eatenSlot));
                }
                log.LogInfo("[party] installed in BattleControl.AdvanceTurnEntity (the eaten tick 2 of 2)");
                return code;
            }
        }

        private static class SkillSlots
        {
            [HarmonyPatch(typeof(BattleControl), "DoAction", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "party");

            // GetPlayerData(k, frombattleentity: true) with a constant k finds slot k (its battleid), meaning member k.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo getPlayerData = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetPlayerData),
                    new[] { typeof(int), typeof(bool) });
                List<int> members = Enumerable.Range(0, Math.Max(0, code.Count - 2))
                    .Where(i => (code[i].LoadsConstant(0) || code[i].LoadsConstant(1) || code[i].LoadsConstant(2))
                        && code[i + 1].LoadsConstant(1) && code[i + 2].Calls(getPlayerData))
                    .ToList();
                if (members.Count != 5)
                {
                    throw new InvalidOperationException(
                        $"expected Heavy Strike's three and the team attack's two, found {members.Count}");
                }
                MethodInfo skillSlot = AccessTools.Method(typeof(PartySlots), nameof(SkillSlot));
                foreach (int i in members.OrderByDescending(i => i))
                {
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Call, skillSlot));
                }
                log.LogInfo("[party] installed in BattleControl.DoAction (a skill's named member 5 of 5)");
                return code;
            }
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
                FieldInfo trueid = TrueId;
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
        }
    }
}
