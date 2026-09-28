using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Dev only: every method the mod has patched, one row per patch (target, kind, patch method, priority), sorted, to
    // compare before and after a refactor of how hooks are installed. Owners are left out: their ids carry a load time.
    internal static class PatchDump
    {
        internal static void Run(ManualLogSource log, string guid)
        {
            FrameRate.InstallForDump();
            var rows = new List<string>();
            int stale = 0;
            foreach (MethodBase target in Harmony.GetAllPatchedMethods())
            {
                Patches info = Harmony.GetPatchInfo(target);
                if (info == null)
                {
                    continue;
                }
                foreach ((string kind, IEnumerable<Patch> patches) in new[]
                {
                    ("prefix", (IEnumerable<Patch>)info.Prefixes), ("postfix", info.Postfixes),
                    ("transpiler", info.Transpilers), ("finalizer", info.Finalizers),
                })
                {
                    List<Patch> ours = patches.Where(p => p.owner.StartsWith(guid)).ToList();
                    if (ours.Count > 1)
                    {
                        // Order matters where one target has several of ours of a kind: the order they run in.
                        log.LogInfo($"[dump] order of {kind}{(kind.EndsWith("x") ? "es" : "s")} on {Describe(target)}: " + string.Join(", ", ours
                            .OrderByDescending(p => p.priority).ThenBy(p => p.index)
                            .Select(p => $"{TopType(p.PatchMethod.DeclaringType)?.Name}.{p.PatchMethod.Name}").ToArray()));
                    }
                    foreach (Patch patch in ours)
                    {
                        MethodInfo method = patch.PatchMethod;
                        if (method.DeclaringType?.Assembly != typeof(PatchDump).Assembly)
                        {
                            stale++;
                        }
                        rows.Add($"{Describe(target)}\t{kind}\t{TopType(method.DeclaringType)?.Name}.{method.Name}\t{patch.priority}");
                    }
                }
            }
            rows.Sort(StringComparer.Ordinal);
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-patches.tsv");
            File.WriteAllLines(outPath, rows);
            log.LogInfo($"[dump] {rows.Count} patches on our owners -> {outPath}; HarmonyX "
                + typeof(Harmony).Assembly.GetName().Version + (stale > 0 ? $"; {stale} from an older load (stale)" : ""));
        }

        private static string Describe(MethodBase m) =>
            $"{m.DeclaringType?.FullName}.{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name).ToArray())})";

        private static Type TopType(Type t)
        {
            while (t != null && t.IsNested)
            {
                t = t.DeclaringType;
            }
            return t;
        }
    }
}
