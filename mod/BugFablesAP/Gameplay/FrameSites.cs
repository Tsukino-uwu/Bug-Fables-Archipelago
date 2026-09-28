using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The places the game does something a fixed amount per rendered frame (a counter, a spin, a smoothing step), found by
    // reading its code, each patched on the exact instructions so that above 60 fps it happens
    // as much per second as at 60. Every helper is the plain operation when the Uncap FPS row is off or inside a physics
    // step. A site whose instructions aren't found exactly as expected is left alone and logged.
    internal static class FrameSites
    {
        private enum Kind { InsertAfter, InsertBefore, ReplaceWithCall }

        private sealed class Edit
        {
            internal string Name;
            internal Func<List<CodeInstruction>, int, bool> At;
            internal Kind Kind;
            internal string Helper;
            internal int Expected; // -1: any number above 0
        }

        private static ManualLogSource log;
        private static readonly Dictionary<MethodBase, List<Edit>> edits = new Dictionary<MethodBase, List<Edit>>();
        private static readonly List<string> report = new List<string>();

        // ---- Helpers the patched code calls. ----

        // What one frame is worth in sixtieths of a second (1 at 60 fps and below).
        private static float Step => FrameRate.Step;

        private static float Scale(float x) => x * Step;

        private static Vector3 ScaleVec(Vector3 v) => v * Step;

        // A counter's per-frame 1: counted once every 1/60 s.
        private static int IntStep(int one) => FrameRate.OnTick ? one : 0;

        // A counter read right after it moved: on the frames it didn't move, a value no check matches.
        private static int TickValue(int value) => FrameRate.OnTick ? value : int.MinValue;

        // A smoothing factor applied once a frame, converted to converge at the same speed per second.
        private static float LerpT(float t)
        {
            float k = Step;
            return k >= 1f ? t : 1f - Mathf.Pow(1f - Mathf.Clamp01(t), k);
        }

        private static float PowStep(float factor) => Mathf.Pow(factor, Step);

        private static void Rotate(Transform t, Vector3 angles) => t.Rotate(angles * Step);

        private static void Rotate3(Transform t, float x, float y, float z)
        {
            float k = Step;
            t.Rotate(x * k, y * k, z * k);
        }

        // enabled = !enabled every frame (a blink): at most once per renderer each 1/60 s. A one-off toggle still happens.
        private static readonly Dictionary<int, long> toggledAt = new Dictionary<int, long>();

        private static void SetEnabledOnTick(Renderer r, bool value)
        {
            if (!FrameRate.Active || Time.inFixedTimeStep || r == null)
            {
                if (r != null)
                {
                    r.enabled = value;
                }
                return;
            }
            long tick = FrameRate.Tick60;
            int id = r.GetInstanceID();
            if (toggledAt.TryGetValue(id, out long last) && last == tick)
            {
                return;
            }
            toggledAt[id] = tick;
            if (toggledAt.Count > 4096)
            {
                toggledAt.Clear();
            }
            r.enabled = value;
        }

        // `FloorToInt(a) % n == 0` on a time-driven a: true once per whole value, as at 60, not on every frame within it.
        private static int lastEvent26 = int.MinValue, lastEvent99 = int.MinValue;

        private static int OncePerValueEvent26(float a) => Once(Mathf.FloorToInt(a), ref lastEvent26);

        private static int OncePerValueEvent99(float a) => Once((int)a, ref lastEvent99);

        private static int Once(int value, ref int last)
        {
            if (!FrameRate.Active)
            {
                return value;
            }
            if (value == last)
            {
                return -1;
            }
            last = value;
            return value;
        }

        // WaitForSeconds resumes on the first frame at or after its time: at 60 fps a 0.02 s letter wait is two frames.
        private static float WaitTime(float seconds) => FrameRate.Active ? Mathf.Ceil(seconds * 60f - 0.001f) / 60f : seconds;

        // ---- Matching. ----

        private static bool Named(CodeInstruction i, string member) => i.operand is MemberInfo m && m.Name == member;

        private static bool Calls(CodeInstruction i, string type, string method) =>
            (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) && i.operand is MethodInfo m && m.Name == method
            && m.DeclaringType != null && m.DeclaringType.Name == type;

        private static bool Field(CodeInstruction i, string field) => (i.opcode == OpCodes.Ldfld || i.opcode == OpCodes.Ldsfld) && Named(i, field);

        private static bool LoadsFloat(CodeInstruction i, float value) => i.opcode == OpCodes.Ldc_R4 && i.operand is float f && Mathf.Approximately(f, value);

        private static bool LoadsInt(CodeInstruction i, long value) => i.LoadsConstant(value);

        private static CodeInstruction At(List<CodeInstruction> c, int i) => i >= 0 && i < c.Count ? c[i] : new CodeInstruction(OpCodes.Nop);

        private static bool RotateVec(CodeInstruction i) => Calls(i, "Transform", "Rotate") && ((MethodInfo)i.operand).GetParameters().Length == 1
            && ((MethodInfo)i.operand).GetParameters()[0].ParameterType == typeof(Vector3);

        private static bool Rotate3Floats(CodeInstruction i) => Calls(i, "Transform", "Rotate") && ((MethodInfo)i.operand).GetParameters().Length == 3;

        private static bool AnyLerp(CodeInstruction i) => Calls(i, "Vector3", "Lerp") || Calls(i, "Mathf", "Lerp") || Calls(i, "Color", "Lerp");

        // A lerp whose factor is the constant right before it.
        private static Func<List<CodeInstruction>, int, bool> LerpWithConstant(params float[] factors) =>
            (c, i) => AnyLerp(c[i]) && factors.Any(f => LoadsFloat(At(c, i - 1), f));

        // A per-frame "field ± 1" counter: the 1 between the field's load and the add or sub.
        private static Func<List<CodeInstruction>, int, bool> CounterOne(string field, bool add) =>
            (c, i) => LoadsInt(c[i], 1) && Field(At(c, i - 1), field) && At(c, i + 1).opcode == (add ? OpCodes.Add : OpCodes.Sub);

        private static void Add(MethodBase method, string name, Kind kind, string helper, int expected, Func<List<CodeInstruction>, int, bool> at)
        {
            if (method == null)
            {
                report.Add($"{name}: method NOT found");
                return;
            }
            if (!edits.TryGetValue(method, out List<Edit> list))
            {
                edits[method] = list = new List<Edit>();
            }
            list.Add(new Edit { Name = name, At = at, Kind = kind, Helper = helper, Expected = expected });
        }

        private static MethodBase M(Type t, string name, params Type[] args) => args.Length == 0 ? AccessTools.Method(t, name) : AccessTools.Method(t, name, args);

        private static MethodBase Iter(Type t, string name, params Type[] args)
        {
            MethodInfo m = args.Length == 0 ? AccessTools.Method(t, name) : AccessTools.Method(t, name, args);
            return m == null ? null : AccessTools.EnumeratorMoveNext(m);
        }

        // ---- The sites. ----

        internal static void Install(ManualLogSource logger, Harmony harmony, IEnumerable<MethodBase> blinkers)
        {
            log = logger;
            edits.Clear();
            report.Clear();

            // Gameplay.
            Add(M(typeof(FishAI), "DoAI"), "fish approach and nibble", Kind.InsertBefore, nameof(ScaleVec), 2,
                (c, i) => Calls(c[i], "Vector3", "op_Addition") && Calls(At(c, i - 1), "Vector3", "op_Multiply") && Calls(At(c, i + 1), "Transform", "set_position"));
            Add(M(typeof(ScrewPlatform), "Update"), "screw platform", Kind.InsertAfter, nameof(Scale), 1,
                (c, i) => c[i].opcode == OpCodes.Ldloc_1 && Calls(At(c, i - 1), "MainManager", "TieFramerate") && At(c, i + 1).opcode == OpCodes.Sub);
            Add(M(typeof(WackaWorm), "Update"), "Wacka Worm reaction", Kind.InsertAfter, nameof(IntStep), 1, CounterOne("timer", add: false));
            MethodBase doBehavior = typeof(NPCControl).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "DoBehavior" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType.IsByRef);
            Add(doBehavior, "disguise countdown", Kind.InsertAfter, nameof(IntStep), 1, CounterOne("disguisecooldown", add: false));
            Add(doBehavior, "disguise turns", Kind.InsertAfter, nameof(TickValue), 2,
                (c, i) => Field(c[i], "disguisecooldown") && (LoadsInt(At(c, i + 1), 80) || LoadsInt(At(c, i + 1), 40)));
            Add(doBehavior, "wander retries", Kind.InsertAfter, nameof(IntStep), 1, CounterOne("trycount", add: true));
            Add(doBehavior, "enemy height settling", Kind.InsertBefore, nameof(LerpT), 5, LerpWithConstant(0.1f));
            Add(M(typeof(NPCControl), "Update"), "dizzy enemy drop", Kind.InsertAfter, nameof(Scale), 1,
                (c, i) => LoadsFloat(c[i], 0.075f) && Field(At(c, i - 1), "height") && At(c, i + 1).opcode == OpCodes.Sub);
            Add(M(typeof(NPCControl), "Update"), "gate slides and sound fades", Kind.InsertBefore, nameof(LerpT), 4,
                (c, i) => AnyLerp(c[i]) && (Field(At(c, i - 1), "x") || Field(At(c, i - 1), "y")) && Field(At(c, i - 4), "vectordata"));
            Add(Iter(typeof(BattleControl), "DoAction"), "dig skill aim", Kind.InsertBefore, nameof(LerpT), 1, LerpWithConstant(0.025f));
            Add(M(typeof(PlayerControl), "LateUpdate"), "Vi's hover rise", Kind.InsertBefore, nameof(LerpT), 1, LerpWithConstant(0.05f));
            Add(M(typeof(MapControl), "LateUpdate"), "map culling grace", Kind.InsertAfter, nameof(Scale), 1,
                (c, i) => LoadsFloat(c[i], 1f) && Field(At(c, i - 1), "alivetime") && At(c, i + 1).opcode == OpCodes.Sub);

            // Scenes.
            Add(Iter(typeof(EntityControl), "Drop"), "battle drop fall", Kind.InsertAfter, nameof(PowStep), 1,
                (c, i) => LoadsFloat(c[i], 1.1f) && At(c, i + 1).opcode == OpCodes.Mul);
            Add(M(typeof(EntityControl), "ReturnFromAction"), "return from dig", Kind.InsertBefore, nameof(LerpT), 1, LerpWithConstant(0.1f));
            Add(Iter(typeof(EventControl), "Event26"), "scene 26 turns", Kind.ReplaceWithCall, nameof(OncePerValueEvent26), 1,
                (c, i) => Calls(c[i], "Mathf", "FloorToInt") && LoadsInt(At(c, i + 1), 20) && At(c, i + 2).opcode == OpCodes.Rem);
            Add(Iter(typeof(EventControl), "Event26"), "scene 26 fade", Kind.InsertBefore, nameof(LerpT), 1, LerpWithConstant(0.1f));
            Add(Iter(typeof(EventControl), "Event99"), "scene 99 turns", Kind.ReplaceWithCall, nameof(OncePerValueEvent99), 1,
                (c, i) => c[i].opcode == OpCodes.Conv_I4 && LoadsInt(At(c, i + 1), 50) && At(c, i + 2).opcode == OpCodes.Rem);
            Add(Iter(typeof(MainManager), "SetText", typeof(string), typeof(int), typeof(float?), typeof(bool), typeof(bool), typeof(Vector3),
                typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl)), "text waits", Kind.InsertBefore, nameof(WaitTime), -1,
                (c, i) => c[i].opcode == OpCodes.Newobj && c[i].operand is ConstructorInfo ctor && ctor.DeclaringType == typeof(WaitForSeconds));

            // Looks.
            Add(M(typeof(EntityControl), "UpdateFlip"), "spins", Kind.ReplaceWithCall, nameof(Rotate), 1,
                (c, i) => RotateVec(c[i]) && Field(At(c, i - 1), "spin"));
            Add(M(typeof(EntityControl), "UpdateFlip"), "sprite turning", Kind.InsertBefore, nameof(LerpT), 2,
                (c, i) => Calls(c[i], "Mathf", "LerpAngle") && Calls(At(c, i - 1), "EntityControl", "GetFlipSpeed"));
            Add(M(typeof(EntityControl), "UpdateFlip"), "dig spin", Kind.InsertAfter, nameof(Scale), 1,
                (c, i) => LoadsFloat(c[i], 15f) && At(c, i + 1).opcode == OpCodes.Newobj);
            Add(M(typeof(EntityControl), "Follow"), "followers catching up", Kind.InsertBefore, nameof(LerpT), 2, LerpWithConstant(0.075f, 0.1f));
            Add(M(typeof(EntityControl), "StopForceMove", typeof(int), typeof(bool)), "followers braking", Kind.InsertBefore, nameof(LerpT), 1, LerpWithConstant(0.5f));
            Add(M(typeof(EntityControl), "AnimSpecificQuirks"), "Watcher eye", Kind.InsertBefore, nameof(LerpT), 1, LerpWithConstant(0.1f));
            Add(M(typeof(BattleControl), "Update"), "battle EXP counter", Kind.InsertAfter, nameof(IntStep), 1, CounterOne("idletimer", add: true));
            Add(Iter(typeof(BattleControl), "CounterAnimation"), "damage number spins", Kind.ReplaceWithCall, nameof(Rotate), 5, (c, i) => RotateVec(c[i]));
            Add(Iter(typeof(BattleControl), "CounterAnimation"), "damage number slides", Kind.InsertBefore, nameof(LerpT), 3, LerpWithConstant(0.3f));
            Add(Iter(typeof(BattleControl), "EnemyTornadoToss"), "enemy beemerang spin", Kind.ReplaceWithCall, nameof(Rotate3), 3, (c, i) => Rotate3Floats(c[i]));
            Add(M(typeof(PrefabParticle), "LateUpdate"), "particle lifetime", Kind.InsertAfter, nameof(Scale), 1,
                (c, i) => LoadsFloat(c[i], 1f) && At(c, i - 1).opcode == OpCodes.Ldind_R4 && At(c, i + 1).opcode == OpCodes.Sub);
            Add(M(typeof(PrefabParticle), "LateUpdate"), "particle spin", Kind.ReplaceWithCall, nameof(Rotate), 1,
                (c, i) => RotateVec(c[i]) && Field(At(c, i - 1), "childspin"));
            Add(M(typeof(PrefabParticle), "LateUpdate"), "particle drift", Kind.InsertAfter, nameof(Scale), 1,
                (c, i) => Field(c[i], "speed") && Calls(At(c, i + 1), "Vector3", "op_Multiply"));
            foreach (MethodBase m in blinkers)
            {
                Add(m, "blinking in " + m.DeclaringType?.Name + "." + m.Name, Kind.ReplaceWithCall, nameof(SetEnabledOnTick), -1, IsBlink);
            }

            foreach (MethodBase method in edits.Keys.ToList())
            {
                try
                {
                    // Patch runs the transpiler at once; this game's HarmonyX can't pass it the original method.
                    patching = method;
                    harmony.Patch(method, transpiler: new HarmonyMethod(typeof(FrameSites), nameof(Transpile)));
                }
                catch (Exception e)
                {
                    report.Add($"{method.DeclaringType?.Name}.{method.Name}: NOT patched ({e.GetBaseException().Message})");
                }
            }
            log.LogInfo("[fps] frame sites: " + string.Join("; ", report.ToArray()));
        }

        // enabled = !enabled: get_enabled, 0, ceq, set_enabled.
        internal static bool IsBlink(List<CodeInstruction> c, int i) =>
            Calls(c[i], "Renderer", "set_enabled") && At(c, i - 1).opcode == OpCodes.Ceq && LoadsInt(At(c, i - 2), 0) && Calls(At(c, i - 3), "Renderer", "get_enabled");

        private static MethodBase patching;

        // Never throws: a transpiler that throws stays registered on its method, and every later patch of that method, by
        // any feature, fails with it until the game restarts.
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> original = instructions.ToList();
            try
            {
                return Apply(new List<CodeInstruction>(original));
            }
            catch (Exception e)
            {
                report.Add($"{patching?.DeclaringType?.Name}.{patching?.Name}: left as it is ({e.GetBaseException().Message})");
                return original;
            }
        }

        private static List<CodeInstruction> Apply(List<CodeInstruction> code)
        {
            if (patching == null || !edits.TryGetValue(patching, out List<Edit> list))
            {
                return code;
            }
            var planned = new List<KeyValuePair<int, Edit>>();
            foreach (Edit e in list)
            {
                var found = new List<int>();
                for (int i = 0; i < code.Count; i++)
                {
                    if (e.At(code, i))
                    {
                        found.Add(i);
                    }
                }
                bool ok = e.Expected < 0 ? found.Count > 0 : found.Count == e.Expected;
                report.Add($"{e.Name} {(ok ? "" : "NOT ")}patched ({found.Count}{(e.Expected < 0 ? "" : " of " + e.Expected)})");
                if (ok)
                {
                    planned.AddRange(found.Select(i => new KeyValuePair<int, Edit>(i, e)));
                }
            }
            // From the end, so earlier indices stay valid.
            foreach (KeyValuePair<int, Edit> p in planned.OrderByDescending(p => p.Key))
            {
                MethodInfo helper = AccessTools.Method(typeof(FrameSites), p.Value.Helper);
                switch (p.Value.Kind)
                {
                    case Kind.InsertAfter:
                        code.Insert(p.Key + 1, new CodeInstruction(OpCodes.Call, helper));
                        break;
                    case Kind.InsertBefore:
                        // Right after the value it takes is pushed (CodeInstruction.labels isn't usable in this game's
                        // HarmonyX, so a branch landing on the target itself skips the helper: none of the sites has one).
                        code.Insert(p.Key, new CodeInstruction(OpCodes.Call, helper));
                        break;
                    case Kind.ReplaceWithCall:
                        code[p.Key].opcode = OpCodes.Call;
                        code[p.Key].operand = helper;
                        break;
                }
            }
            return code;
        }
    }
}
