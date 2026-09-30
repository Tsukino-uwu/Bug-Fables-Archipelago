using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;

namespace BugFablesAP
{
    // Dev only: everything the mod read from the seed's slot_data, one sorted line per entry, to compare before and
    // after a change to how slot_data is read. Written once a login has brought the seed.
    internal static class SeedDump
    {
        internal static void Run(ManualLogSource log, ApConnection c)
        {
            var rows = new List<string>();
            void Add(string table, object key, string value) => rows.Add($"{table}\t{key}\t{value}");
            void Each<T>(string table, IEnumerable<KeyValuePair<long, T>> entries, Func<T, string> text)
            {
                foreach (KeyValuePair<long, T> e in entries ?? Enumerable.Empty<KeyValuePair<long, T>>())
                {
                    Add(table, e.Key, text(e.Value));
                }
            }
            string Ints(IEnumerable<int> values) => values == null ? "null"
                : string.Join(",", values.Select(v => v.ToString()).ToArray());
            void Blockers(string table, IEnumerable<ApConnection.Blocker> list)
            {
                int i = 0;
                foreach (ApConnection.Blocker b in list ?? Enumerable.Empty<ApConnection.Blocker>())
                {
                    Add(table, i++, b.Item >= 0 ? $"{b.Map}|{b.Entity}|item {b.Item}" : $"{b.Map}|{b.Entity}|{b.Flag}");
                }
            }

            Each("location_flags", c.LocationFlags, v => v.ToString());
            Each("location_gives", c.LocationGives, v => $"{v.Map}|{v.Type}|{v.Item}");
            Each("location_pickups", c.LocationPickups, v => $"{v.Map}|{v.Flag}|{v.Event}|{v.Berry}|{v.Regional}");
            Each("location_vars", c.LocationVars, v => Ints(v));
            Each("location_berries", c.LocationBerries, v => v.ToString());
            Each("location_added", c.LocationAdded, v => Ints(v));
            Each("location_discoveries", c.LocationDiscoveries, v => v.ToString());
            Each("location_shops", c.LocationShops, v => Ints(v));
            Each("location_item_shops", c.LocationItemShops, v => $"{v.Map}|{v.Keeper}|{v.Item}");
            Each("item_kinds", c.ItemKinds, v => v.ToString());
            foreach (long id in c.SilentLocations ?? new HashSet<long>())
            {
                Add("silent_locations", id, "");
            }
            foreach (long id in c.QuietLocations ?? new HashSet<long>())
            {
                Add("quiet_locations", id, "");
            }
            Blockers("kept_open", c.KeptOpen);
            Blockers("kept_present", c.KeptPresent);
            Blockers("scenery_hidden", c.SceneryHidden);
            Blockers("scenery_present", c.SceneryPresent);
            Blockers("held_until", c.HeldUntil);
            Blockers("present_from", c.PresentFrom);
            Blockers("present_with_item", c.PresentWithItem);
            Blockers("held_until_item", c.HeldUntilItem);
            int d = 0;
            foreach (ApConnection.DialogueFlag f in c.DialogueFlags ?? new List<ApConnection.DialogueFlag>())
            {
                Add("dialogue_flags", d++, $"{f.Map}|{f.Entity}|{f.From}|{f.To}");
            }
            int t = 0;
            foreach (DoorShuffle.Target door in c.DoorTargets ?? new List<DoorShuffle.Target>())
            {
                Add("door_targets", t++, $"{door.Map}|{door.Door}|{door.LikeMap}|{door.LikeDoor}");
            }
            foreach (KeyValuePair<string, int[]> swap in c.EnemySwaps ?? new Dictionary<string, int[]>())
            {
                Add("enemy_swaps", swap.Key, Ints(swap.Value));
            }
            foreach (KeyValuePair<string, string> track in c.MusicMap ?? new Dictionary<string, string>())
            {
                Add("music_map", track.Key, track.Value);
            }
            foreach (KeyValuePair<string, string> jingle in c.JingleMap ?? new Dictionary<string, string>())
            {
                Add("jingle_map", jingle.Key, jingle.Value);
            }
            int s = 0;
            foreach (ApConnection.InventorySpot spot in c.ShopInventories ?? new List<ApConnection.InventorySpot>())
            {
                Add("shop_inventories", s++, $"{spot.Map}|{spot.Keeper}|{spot.Regional}|{spot.Item}|{spot.To}");
            }
            Add("start", "", c.Start.HasValue ? $"{c.Start.Value.Key}|{c.Start.Value.Value}" : "null");
            Add("start_from", "", c.StartFrom ?? "null");
            Add("own_slot", "", c.OwnSlot.ToString());
            Add("artifacts_required", "", c.ArtifactsRequired.ToString());
            Add("starting_member", "", $"{PartyMembers.SeedSaysMember}|{PartyMembers.SeedStartMember}");
            Add("shuffle_moves", "", FieldMoves.MovesShuffled.ToString());
            Add("shuffle_jump", "", FieldMoves.JumpShuffled.ToString());
            Add("ability_items", "", Abilities.AbilityItems.ToString());
            Add("submarine_item", "", (c.Seed?.SubmarineItem ?? false).ToString());
            rows.Sort(StringComparer.Ordinal);
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-seed.tsv");
            File.WriteAllLines(outPath, rows);
            log.LogInfo($"[dump] {rows.Count} seed entries -> {outPath}");
        }
    }
}
