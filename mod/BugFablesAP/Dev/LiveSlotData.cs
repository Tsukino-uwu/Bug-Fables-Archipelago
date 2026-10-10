using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json.Linq;

namespace BugFablesAP
{
    // Dev only: the console's `liveslot` lays dev-scripts/live-slot-data.py's file over the login's slot_data, so an
    // apworld change shows in the running game with no new seed or file. What a seed rolls stays the server's.
    internal static class LiveSlotData
    {
        internal static readonly string DefaultPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-live.json");

        internal static string Apply(ApConnection connection, string path)
        {
            Dictionary<string, object> login = connection?.SlotDataAtLogin;
            if (login == null || connection.Seed == null)
            {
                return "liveslot: no seed yet (log in first)";
            }
            if (!File.Exists(path))
            {
                return "liveslot: no file at " + path + " (run dev-scripts/live-slot-data.py)";
            }
            var merged = new Dictionary<string, object>(login);
            var changed = new List<string>();
            foreach (JProperty key in JObject.Parse(File.ReadAllText(path)).Properties())
            {
                // The goal stays the login's: a goal flag the file adds would be turned off by the guard, or counted.
                if (key.Name == "goal_flags")
                {
                    continue;
                }
                JToken token = key.Value;
                if (key.Name == "options" && token is JObject options && login.TryGetValue("options", out object seed)
                    && seed != null && JToken.FromObject(seed)["artifacts_required"] is JToken required)
                {
                    options = (JObject)options.DeepClone();
                    options["artifacts_required"] = required;
                    token = options;
                }
                // As the login hands them over: objects and lists as tokens, a bare value as itself.
                object value = token is JValue bare ? bare.Value : token;
                bool same = login.TryGetValue(key.Name, out object old)
                    && JToken.DeepEquals(old == null ? JValue.CreateNull() : JToken.FromObject(old), token);
                if (!same)
                {
                    changed.Add(key.Name);
                }
                merged[key.Name] = value;
            }
            // Parsed whole first: a file that doesn't parse leaves the seed as it was.
            connection.UseSeed(new SeedData(merged, connection.Seed.OwnSlot));
            return changed.Count == 0
                ? "liveslot: applied, nothing differs from the seed"
                : "liveslot: applied; differs from the seed: " + string.Join(", ", changed.ToArray());
        }
    }
}
