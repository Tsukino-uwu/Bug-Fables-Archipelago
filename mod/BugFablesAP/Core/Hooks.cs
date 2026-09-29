using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Installs Harmony hooks marked with attributes, one group (class) at a time, each with its own Harmony instance: a
    // group whose target is missing installs nothing instead of half, and every group comes off with the plugin.
    internal static class Hooks
    {
        // New for every loaded build, so a hot-reloaded plugin never shares an id with the one it replaces.
        private static readonly long LoadId = DateTime.UtcNow.Ticks;
        private static readonly List<Harmony> installed = new List<Harmony>();
        private static ManualLogSource log;

        internal static void Init(ManualLogSource logger)
        {
            log = logger;
        }

        // True when every hook in the group is in. Otherwise the group's partial patches come off, the reason is logged
        // with what the player loses, and it returns false (or throws, for a group the mod can't run without).
        internal static bool Install(Type group, string tag, string ifMissing, bool required = false)
        {
            var harmony = new Harmony($"{Plugin.Guid}.{tag}.{group.Name}.{LoadId}");
            try
            {
                harmony.PatchAll(group);
                installed.Add(harmony);
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    harmony.UnpatchSelf();
                }
                catch (Exception undo)
                {
                    log?.LogError(
                        $"[{tag}] removing the half-installed {group.Name} threw: {undo.GetBaseException().Message}");
                }
                log?.LogError($"[{tag}] NOT installed ({group.Name}): {e.GetBaseException().Message}; {ifMissing}");
                if (required)
                {
                    throw;
                }
                return false;
            }
        }

        // For hooks whose targets are decided at install time: the caller patches with it; UninstallAll removes them.
        internal static Harmony Create(string tag)
        {
            var harmony = new Harmony($"{Plugin.Guid}.{tag}.{LoadId}");
            installed.Add(harmony);
            return harmony;
        }

        internal static void UninstallAll()
        {
            for (int i = installed.Count - 1; i >= 0; i--)
            {
                try
                {
                    installed[i].UnpatchSelf();
                }
                catch (Exception e)
                {
                    log?.LogError($"[hooks] removing {installed[i].Id} threw: {e.GetBaseException().Message}");
                }
            }
            installed.Clear();
        }

        // A transpiler that throws stays registered and breaks its method for every later patch until the game
        // restarts, so on any error the method keeps its own code and the reason is logged. The edit finds everything
        // it needs before it changes anything, as FrameSites does.
        internal static IEnumerable<CodeInstruction> Safe(IEnumerable<CodeInstruction> instructions,
            Func<List<CodeInstruction>, IEnumerable<CodeInstruction>> edit, string tag)
        {
            List<CodeInstruction> original = instructions.ToList();
            try
            {
                return edit(new List<CodeInstruction>(original)).ToList();
            }
            catch (Exception e)
            {
                log?.LogError($"[{tag}] a transpiler failed, its method left as it is: {e.GetBaseException().Message}");
                return original;
            }
        }
    }
}
