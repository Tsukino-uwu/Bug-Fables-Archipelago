using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev-only measurement: for every map, load its prefab the way MainManager does (Resources
    // "Prefabs/Maps/<map>", MainManager.cs:9652) WITHOUT instantiating it, and read serialized data only:
    //   - MapControl.autoevent: (flag, event) pairs; the map starts the event once while the flag is off, then
    //     sets the flag (MapControl.cs:874-883). These are story steps no entity or dialogue starts.
    //   - Hazards components by type (Hazards.cs:8). WalkableSpike is what the bubble shield walks over
    //     (Hazards.cs:207); Hole is a candidate for hover.
    //   - GlowTrigger components (GlowTrigger.cs): electric, which the bubble shield also blocks (:189).
    //   - Scenery switched by story flags, into bugfablesap-mapflags.tsv: ConditionChecker (hidden, or moved to
    //     activepos, by requires/limit flags, ConditionChecker.cs) and FlagAnimation (plays anims[i] while flags[i]
    //     is set, FlagAnimation.cs). A door's model can be one of these, apart from its load zone entity.
    // Nothing is instantiated, so no Awake/Start runs. Output goes to the BepInEx folder, not the repo.
    internal static class MapDump
    {
        internal static bool TryRun(ManualLogSource log)
        {
            if (MainManager.instance == null)
            {
                return false;
            }
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-mapdump.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("map\tautoevents(flag:event)\thazards(type:count)\tglowtriggers");
            var flagged = new StringBuilder();
            flagged.AppendLine("map\tcomponent\tobject\trequires\tlimit\tdetail");
            int maps = 0, missing = 0;
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/Maps/" + map);
                if (prefab == null)
                {
                    missing++;
                    continue;
                }
                maps++;
                MapControl control = prefab.GetComponent<MapControl>();
                string autos = control != null && control.autoevent != null
                    ? string.Join(" ", control.autoevent.Select(v => (int)v.x + ":" + (int)v.y).ToArray())
                    : "";
                var hazards = new SortedDictionary<string, int>();
                foreach (Hazards h in prefab.GetComponentsInChildren<Hazards>(true))
                {
                    string key = h.type.ToString();
                    hazards[key] = hazards.TryGetValue(key, out int n) ? n + 1 : 1;
                }
                int glow = prefab.GetComponentsInChildren<GlowTrigger>(true).Length;
                foreach (ConditionChecker c in prefab.GetComponentsInChildren<ConditionChecker>(true))
                {
                    flagged.Append(map).Append("\tConditionChecker\t").Append(PathOf(c.transform, prefab.transform)).Append('\t')
                        .Append(Join(c.requires)).Append('\t').Append(Join(c.limit)).Append('\t')
                        .Append("region " + c.regionID + (c.activepos.magnitude > 0.1f ? ", moves to " + c.activepos : ", hides")
                            + (c.dontdelete ? ", dontdelete" : "") + (c.spriteflagchange > -1 ? ", sprite on flag " + c.spriteflagchange : ""))
                        .AppendLine();
                }
                foreach (FlagAnimation f in prefab.GetComponentsInChildren<FlagAnimation>(true))
                {
                    flagged.Append(map).Append("\tFlagAnimation\t").Append(PathOf(f.transform, prefab.transform)).Append("\t\t\t")
                        .Append(f.flags == null ? "" : string.Join(" ", f.flags.Select((flag, i) =>
                            flag + ":" + (f.anims != null && i < f.anims.Length ? f.anims[i] : "?")).ToArray()))
                        .AppendLine();
                }
                sb.Append(map).Append('\t').Append(autos).Append('\t')
                  .Append(string.Join(" ", hazards.Select(kv => kv.Key + ":" + kv.Value).ToArray())).Append('\t')
                  .Append(glow).AppendLine();
            }
            File.WriteAllText(outPath, sb.ToString());
            File.WriteAllText(Path.Combine(Paths.BepInExRootPath, "bugfablesap-mapflags.tsv"), flagged.ToString());
            Resources.UnloadUnusedAssets();
            log.LogInfo($"[dump] {maps} map prefabs read ({missing} with no prefab) -> {outPath}");
            return true;
        }

        private static string Join(int[] values) => values == null ? "" : string.Join(" ", values.Where(v => v != -1).Select(v => v.ToString()).ToArray());

        private static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (Transform at = t; at != null && at != root; at = at.parent)
            {
                parts.Add(at.name);
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}
