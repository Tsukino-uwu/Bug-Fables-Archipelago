using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BugFablesAP
{
    // Reading slot_data: a key that's missing or of the wrong shape reads as null, and the caller keeps null.
    internal static class SlotData
    {
        internal static JObject Object(Dictionary<string, object> slotData, string key)
        {
            return slotData != null && slotData.TryGetValue(key, out object raw) ? raw as JObject : null;
        }

        // {"location id": value}, as the apworld writes per-location tables.
        internal static Dictionary<long, T> ByLocation<T>(Dictionary<string, object> slotData, string key,
            Func<JToken, T> read)
        {
            return Object(slotData, key)?.Properties().ToDictionary(p => long.Parse(p.Name), p => read(p.Value));
        }

        internal static List<T> List<T>(Dictionary<string, object> slotData, string key, Func<JToken, T> read)
        {
            return slotData != null && slotData.TryGetValue(key, out object raw) && raw is JArray list
                ? list.Select(read).ToList() : null;
        }
    }
}
