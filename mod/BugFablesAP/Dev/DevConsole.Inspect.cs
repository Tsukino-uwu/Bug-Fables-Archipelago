using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    internal static partial class DevConsole
    {
        private static string Script(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "script <map>";
            }
            TextAsset asset = Resources.Load<TextAsset>("Data/Dialogues" + MainManager.languageid + "/Maps/"
                + parts[1]);
            if (asset == null)
            {
                return "script: no dialogue table for " + parts[1];
            }
            string[] rows = asset.ToString().Replace("\r\n", "\n").Split('\n');
            var sb = new System.Text.StringBuilder("[dev] script " + parts[1] + ":");
            var token = new System.Text.RegularExpressions.Regex(@"\|([a-zA-Z]+)((?:,[^|]*)?)\|");
            for (int i = 0; i < rows.Length; i++)
            {
                var tokens = token.Matches(rows[i]).Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value.ToLowerInvariant() + m.Groups[2].Value).ToArray();
                if (tokens.Length > 0)
                {
                    sb.Append("\n  ").Append(i).Append(": ").Append(string.Join(" ", tokens));
                }
            }
            log.LogInfo(sb.ToString());
            return "script of " + parts[1] + " logged";
        }

        // Every solid collider under and around the player: what an invisible wall is.
        private static string Solids()
        {
            if (MainManager.player == null)
            {
                return "solids: no player";
            }
            Vector3 me = MainManager.player.transform.position;
            var sb = new System.Text.StringBuilder($"[dev] solids around {me}:");
            if (Physics.Raycast(me + Vector3.up * 0.5f, Vector3.down, out RaycastHit below, 30f, ~0,
                QueryTriggerInteraction.Ignore))
            {
                sb.Append("\n  under: ").Append(Describe(below.collider));
            }
            foreach (Collider c in Physics.OverlapSphere(me, 4f, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => MainManager.player.transform != c.transform
                && !c.transform.IsChildOf(MainManager.player.transform)))
            {
                sb.Append("\n  near: ").Append(Describe(c));
            }
            log.LogInfo(sb.ToString());
            return "solids logged";
        }

        private static string Describe(Collider c)
        {
            string path = c.name;
            for (Transform t = c.transform.parent; t != null; t = t.parent)
            {
                path = t.name + "/" + path;
            }
            var parts = new System.Text.StringBuilder();
            for (Transform t = c.transform; t != null && t != MainManager.map?.transform; t = t.parent)
            {
                parts.Append(" | ").Append(t.name).Append(": ")
                    .Append(string.Join(", ", t.GetComponents<Component>().Select(k => k.GetType().Name).ToArray()));
                ConditionChecker check = t.GetComponent<ConditionChecker>();
                if (check != null)
                {
                    parts.Append(" [ConditionChecker requires ")
                        .Append(string.Join(",", (check.requires ?? new int[0]).Select(f => f.ToString()).ToArray()))
                        .Append(" limit ")
                        .Append(string.Join(",", (check.limit ?? new int[0]).Select(f => f.ToString()).ToArray()))
                        .Append(" region ").Append(check.regionID).Append("]");
                }
            }
            return $"{path} <{c.GetType().Name}> layer {c.gameObject.layer}, bounds {c.bounds.center} size {c.bounds.size}{parts}";
        }

        private static string Tree()
        {
            if (MainManager.map == null || MainManager.player == null)
            {
                return "tree: no map or player";
            }
            Vector3 me = MainManager.player.transform.position;
            NPCControl nearest = MainManager.map.GetComponentsInChildren<NPCControl>(true)
                .Where(n => n.objecttype == NPCControl.ObjectTypes.Item && n.entity != null)
                .OrderBy(n => (n.entity.transform.position - me).sqrMagnitude).FirstOrDefault();
            if (nearest == null)
            {
                return "tree: no pickup on this map";
            }
            EntityControl e = nearest.entity;
            var sb = new System.Text.StringBuilder();
            sb.Append($"[dev] tree of {nearest.name} (animid {e.animid}, model {(e.model != null ? e.model.name : "none")}, spin {e.spin}, "
                + $"sprite {(e.sprite != null && e.sprite.sprite != null ? e.sprite.sprite.name : "none")}):");
            Walk(e.transform, e.transform, sb);
            log.LogInfo(sb.ToString());
            return "tree of " + nearest.name + " logged";
        }

        private static void Walk(Transform t, Transform root, System.Text.StringBuilder sb)
        {
            Renderer r = t.GetComponent<Renderer>();
            sb.Append("\n  ").Append(new string(' ', Depth(t, root) * 2)).Append(t.name)
              .Append(t.gameObject.activeSelf ? "" : " [inactive]")
              .Append(r != null ? $" <{r.GetType().Name}{(r.enabled ? "" : " disabled")}>" : "");
            foreach (Transform child in t)
            {
                Walk(child, root, sb);
            }
        }

        private static int Depth(Transform t, Transform root)
        {
            int d = 0;
            for (Transform at = t; at != null && at != root; at = at.parent)
            {
                d++;
            }
            return d;
        }

        private static string Items()
        {
            MapControl map = MainManager.map;
            if (map == null || MainManager.player == null)
            {
                return "not now: no map";
            }
            int n = 0;
            foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
            {
                if (npc.objecttype != NPCControl.ObjectTypes.Item || npc.entity == null)
                {
                    continue;
                }
                n++;
                float distance = Vector3.Distance(npc.transform.position, MainManager.player.transform.position);
                log.LogInfo($"[dev] item on {map.mapid}: {npc.name} kind {npc.entity.animid} id {npc.entity.animstate} flag {npc.activationflag} "
                    + $"hidden {npc.entity.iskill} active {npc.gameObject.activeInHierarchy} {distance:0.0} away at {npc.transform.position}");
            }
            return $"{n} pickups on {map.mapid} (listed in the log); entity data read from {map.readdatafromothermap}";
        }

        private static string Flag(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "flag <n> [on|off]";
            }
            int n = int.Parse(parts[1]);
            if (parts.Length > 2)
            {
                MainManager.instance.flags[n] = parts[2].ToLowerInvariant() == "on" || parts[2] == "true"
                    || parts[2] == "1";
            }
            return $"flags[{n}] = {MainManager.instance.flags[n]}";
        }

        // Every text file the game loads from Resources/Data, searched for a word (case-insensitive); matches go to the
        // log.
        private static string TextSearch(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "textsearch <word>";
            }
            string word = string.Join(" ", parts.Skip(1).ToArray());
            int found = 0;
            foreach (TextAsset asset in Resources.LoadAll<TextAsset>("Data"))
            {
                string[] lines = asset.text.Split('\n');
                for (int i = 0; i < lines.Length && found < 200; i++)
                {
                    if (lines[i].IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found++;
                        string line = lines[i].Length > 300 ? lines[i].Substring(0, 300) + "..." : lines[i];
                        log.LogInfo($"[dev] textsearch '{word}': {asset.name}:{i}: {line.Trim()}");
                    }
                }
            }
            return $"textsearch '{word}': {found} lines logged" + (found >= 200 ? " (stopped at 200)" : "");
        }

        // Written straight, with no pop-up: for replaying a scene that records one.
        private static string Discovery(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "discovery <n> [on|off]";
            }
            int n = int.Parse(parts[1]);
            int library = (int)MainManager.Library.Discovery;
            if (parts.Length > 2)
            {
                MainManager.instance.librarystuff[library, n] = parts[2].ToLowerInvariant() == "on"
                    || parts[2] == "true" || parts[2] == "1";
            }
            return $"discovery {n} = {MainManager.instance.librarystuff[library, n]}";
        }
    }
}
