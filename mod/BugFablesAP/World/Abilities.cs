using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Every learned field ability is an item (slot_data ability_items). Where the game asks whether the party has learned
    // one in order to use it (in the field, a skill list, a battle), the key item in the bag answers. The story's own
    // reads of those flags, and the flags themselves, stay the game's: the scene that teaches an ability is its check.
    internal static class Abilities
    {
        // Key items, after the four moves' (CustomItems.MoveKeyItem, 201-204).
        internal const int Halt = 205, Dash = 206, HornDash = 207, BeeFly = 208, Dig = 209, Icicle = 210, Shield = 211;

        // Each learned ability's key item: its name, its member (0 Vi, 1 Kabbu, 2 Leif), the game flag it answers for, and
        // its field skill (skilldata's row: the game's own name and description, in the player's language).
        internal static readonly (int Key, string Name, int Member, int Flag, int Skill)[] Keys =
        {
            (Halt, "Beemerang Halt", 0, 21, 35), (Dash, "Dash", 1, 699, 49), (HornDash, "Horn Dash", 1, 39, 38),
            (BeeFly, "Bee Fly", 0, 19, 36), (Dig, "Beetle Dig", 1, 18, 39), (Icicle, "Icicle", 2, 171, 41), (Shield, "Shield", 2, 20, 42),
        };

        // The game's name and description for a key's field skill; the English name and none before its table loads.
        internal static string SkillText(int skill, int field, string fallback)
        {
            string[,] data = MainManager.skilldata;
            return data != null && skill < data.GetLength(0) && !string.IsNullOrEmpty(data[skill, field]) ? data[skill, field] : fallback;
        }

        private static readonly Dictionary<int, int> keyForFlag = Keys.ToDictionary(k => k.Flag, k => k.Key);

        // From slot_data; false with no seed or a seed from before abilities were items.
        internal static volatile bool AbilityItems;

        // The field-ability items by their game id (the apworld's items.json, kind 6).
        internal static string ItemName(int gameId)
        {
            switch (gameId)
            {
                case 0: return AbilityItems ? "Progressive Beemerang" : FieldMoves.Name(0);
                case 2: return AbilityItems ? "Progressive Freeze" : FieldMoves.Name(2);
                case 4: return "Progressive Dash";
                case 5: return "Bee Fly";
                case 6: return "Beetle Dig";
                case 7: return "Shield";
                default: return FieldMoves.Name(gameId);
            }
        }

        // The member an item's abilities belong to; -1 for the whole party's (Jump).
        internal static int Member(int gameId) => gameId >= 0 && gameId <= 2 ? gameId : gameId == 4 || gameId == 6 ? 1 : gameId == 5 ? 0 : gameId == 7 ? 2 : -1;

        internal static string Description(int gameId) => gameId == FieldMoves.Jump ? "The whole party can jump."
            : PartyMembers.Name(Member(gameId)) + " learns " + ItemName(gameId) + ".";

        // The key item a received copy gives: the next level of a progressive item. With Shuffle Field Moves off the
        // party starts with the Toss and the Freeze, so their first copy is already the upgrade.
        internal static int KeyFor(int gameId, List<int> bag)
        {
            switch (gameId)
            {
                case 0: return FieldMoves.MovesShuffled && !bag.Contains(CustomItems.MoveKeyItem(0)) ? CustomItems.MoveKeyItem(0) : Halt;
                case 2: return FieldMoves.MovesShuffled && !bag.Contains(CustomItems.MoveKeyItem(2)) ? CustomItems.MoveKeyItem(2) : Icicle;
                case 4: return bag.Contains(Dash) ? HornDash : Dash;
                case 5: return BeeFly;
                case 6: return Dig;
                case 7: return Shield;
                default: return CustomItems.MoveKeyItem(gameId);
            }
        }

        internal static string KeyName(int key)
        {
            foreach (var k in Keys)
            {
                if (k.Key == key)
                {
                    return k.Name;
                }
            }
            return FieldMoves.Name(key - CustomItems.FirstMove);
        }

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static readonly FieldInfo flagsField = AccessTools.Field(typeof(MainManager), "flags");

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            harmony = new Harmony(guid + ".abilities." + DateTime.UtcNow.Ticks);
            // Where each measured read is (the unlock scenes): the field's uses, the thrown Beemerang's hold
            // (flag 21 only; NPCControl's other reads are story state), and the skill lists.
            int field = Install(Methods(typeof(PlayerControl)), keyForFlag.Keys.ToArray(), nameof(TranspileAll));
            int halt = Install(Methods(typeof(NPCControl)), new[] { 21 }, nameof(TranspileHalt));
            int skills = Install(new MethodBase[] { AccessTools.Method(typeof(MainManager), "RefreshSkills") }, keyForFlag.Keys.ToArray(), nameof(TranspileAll));
            string counts = $"field {field} of 8, the Beemerang's hold {halt} of 2, skill lists {skills} of 15";
            if (field != 8 || halt != 2 || skills != 15)
            {
                log.LogError($"[abilities] reads found differ from what was measured ({counts}): an ability may follow its "
                    + "story flag instead of its item");
            }
            log.LogInfo($"[abilities] installed: the learned abilities' reads answered from the bag ({counts})");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // A class's own methods and those of its nested types (coroutines, lambdas).
        private static IEnumerable<MethodBase> Methods(Type type)
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            IEnumerable<Type> types = new[] { type }.Concat(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic));
            return types.SelectMany(t => t.GetMethods(all).Cast<MethodBase>().Concat(t.GetConstructors(all))).Where(m => m.GetMethodBody() != null);
        }

        // Patches every method reading one of the flags as `flags[n]`; returns how many reads it has.
        private static int Install(IEnumerable<MethodBase> methods, int[] flags, string transpiler)
        {
            int total = 0;
            foreach (MethodBase m in methods)
            {
                int reads = CountReads(m, flags);
                if (reads == 0)
                {
                    continue;
                }
                try
                {
                    harmony.Patch(m, transpiler: new HarmonyMethod(typeof(Abilities), transpiler));
                    total += reads;
                }
                catch (Exception e)
                {
                    log.LogError($"[abilities] couldn't patch {m.DeclaringType?.Name}.{m.Name}: {e.GetBaseException().Message}");
                }
            }
            return total;
        }

        private static int CountReads(MethodBase m, int[] flags)
        {
            List<KeyValuePair<OpCode, object>> body;
            try
            {
                body = PatchProcessor.ReadMethodBody(m).ToList();
            }
            catch
            {
                return 0;
            }
            int count = 0;
            for (int i = 2; i < body.Count; i++)
            {
                if (body[i].Key == OpCodes.Ldelem_U1 && body[i - 2].Key == OpCodes.Ldfld && Equals(body[i - 2].Value, flagsField)
                    && Constant(body[i - 1].Key, body[i - 1].Value) is int n && flags.Contains(n))
                {
                    count++;
                }
            }
            return count;
        }

        private static int? Constant(OpCode op, object operand) =>
            op == OpCodes.Ldc_I4 || op == OpCodes.Ldc_I4_S ? Convert.ToInt32(operand) : (int?)null;

        private static IEnumerable<CodeInstruction> TranspileAll(IEnumerable<CodeInstruction> instructions) =>
            Transpile(instructions, keyForFlag.Keys.ToArray());

        private static IEnumerable<CodeInstruction> TranspileHalt(IEnumerable<CodeInstruction> instructions) =>
            Transpile(instructions, new[] { 21 });

        // `ldfld flags; ldc.i4 n; ldelem.u1` becomes `ldfld flags; ldc.i4 n; call Learned`: the same stack, bool[] and int in,
        // bool out. Labels stay on the instruction.
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, int[] flags)
        {
            List<CodeInstruction> code = instructions.ToList();
            MethodInfo learned = AccessTools.Method(typeof(Abilities), nameof(Learned));
            for (int i = 2; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldelem_U1 && code[i - 2].LoadsField(flagsField)
                    && Constant(code[i - 1].opcode, code[i - 1].operand) is int n && flags.Contains(n))
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = learned;
                }
            }
            return code;
        }

        // The game's flags[index], or, for a learned ability in a seed that makes it an item, whether its key item has come.
        public static bool Learned(bool[] flags, int index)
        {
            if (!AbilityItems || randomizerOn == null || !randomizerOn() || !keyForFlag.TryGetValue(index, out int key))
            {
                return flags[index];
            }
            List<int>[] items = MainManager.instance?.items;
            return items != null && items.Length > 1 && items[1].Contains(key);
        }
    }
}
