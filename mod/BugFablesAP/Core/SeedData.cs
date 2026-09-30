using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BugFablesAP
{
    // What a login reads from the seed's slot_data, parsed whole into one read-only record before anything is
    // published. The connection keeps it after a drop, so the seed's rules stay in force while offline.
    internal sealed class SeedData
    {
        internal readonly Dictionary<long, int> LocationFlags;
        internal readonly Dictionary<long, ApConnection.Give> LocationGives;
        // Items the story puts straight into the bag at a location: {type, item}; left out while in a seed.
        internal readonly Dictionary<long, int[]> LocationAdded;
        internal readonly Dictionary<long, ApConnection.Pickup> LocationPickups;
        // {location id: {var, at_least}}: done when a number slot reaches a value (a boss prize: its slot at 3).
        internal readonly Dictionary<long, int[]> LocationVars;
        internal readonly Dictionary<long, int> LocationBerries;
        internal readonly Dictionary<long, int> LocationDiscoveries;
        // Checks that show no item of their own: the receiver shows the player's own item from them.
        internal readonly HashSet<long> SilentLocations;
        // The opening's checks: their items arrive with no hold-up.
        internal readonly HashSet<long> QuietLocations;
        internal readonly Dictionary<long, int[]> LocationShops;
        internal readonly Dictionary<long, ApConnection.ItemShopSlot> LocationItemShops;
        internal readonly Dictionary<long, int> ItemKinds;
        internal readonly List<ApConnection.Blocker> KeptOpen;
        internal readonly List<ApConnection.Blocker> KeptPresent;
        // Scenery entities are paths inside the map, as MapDump writes them.
        internal readonly List<ApConnection.Blocker> SceneryHidden;
        internal readonly List<ApConnection.Blocker> SceneryPresent;
        internal readonly List<ApConnection.Blocker> HeldUntil;
        internal readonly List<ApConnection.Blocker> PresentFrom;
        internal readonly List<ApConnection.DialogueFlag> DialogueFlags;
        internal readonly List<DoorShuffle.Target> DoorTargets;
        // {"map:entity index": enemy ids}: the fight a map enemy starts instead of its own (Enemy Shuffle).
        internal readonly Dictionary<string, int[]> EnemySwaps;
        // Music Shuffle, {name: name played in its place}: tracks by the game's Musics names, jingles by sound name.
        internal readonly Dictionary<string, string> MusicMap;
        internal readonly Dictionary<string, string> JingleMap;
        // Shuffle Shop Inventories: what an item shop slot or a respawning pickup holds once it isn't a check.
        internal readonly List<ApConnection.InventorySpot> ShopInventories;
        // Where a new file begins (Starting Location): the map and a save point's entity index (-1 for none); null for
        // the game's own start. StartFrom: the map whose door leads in, for a start entered as if through that door.
        internal readonly KeyValuePair<string, int>? Start;
        internal readonly string StartFrom;
        // The goal: this many artifacts, as the game counts them. 0 when slot_data has none.
        internal readonly int ArtifactsRequired;
        internal readonly int OwnSlot;
        // The one member a new file starts with (0 Vi, 1 Kabbu, 2 Leif; 3 all three), when the seed names one.
        internal readonly bool StartingMemberGiven;
        internal readonly int StartingMember;
        internal readonly bool MovesShuffled;
        internal readonly bool JumpShuffled;
        internal readonly bool AbilityItems;

        internal SeedData(Dictionary<string, object> data, int ownSlot)
        {
            LocationFlags = SlotData.ByLocation(data, "location_flags", v => v.Value<int>());
            LocationGives = SlotData.ByLocation(data, "location_gives", v => new ApConnection.Give
            {
                Map = v.Value<string>("map"),
                Type = v.Value<int>("type"),
                Item = v.Value<int>("item"),
            });
            LocationPickups = SlotData.ByLocation(data, "location_pickups", v => new ApConnection.Pickup
            {
                Map = v.Value<string>("map"),
                Flag = v.Value<int>("flag"),
                Event = v.Value<int?>("event") ?? -1,
                Berry = v.Value<int?>("berry") ?? -1,
                Regional = v.Value<int?>("regional") ?? -1,
            });
            KeptOpen = SlotData.List(data, "kept_open", ReadBlocker);
            KeptPresent = SlotData.List(data, "kept_present", ReadBlocker);
            SceneryHidden = SlotData.List(data, "scenery_hidden", ReadBlocker);
            SceneryPresent = SlotData.List(data, "scenery_present", ReadBlocker);
            HeldUntil = SlotData.List(data, "held_until", ReadBlocker);
            PresentFrom = SlotData.List(data, "present_from", ReadBlocker);
            DialogueFlags = SlotData.List(data, "dialogue_flags", e => new ApConnection.DialogueFlag
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                From = e.Value<int>("flag"),
                To = e.Value<int>("to"),
            });
            LocationVars = SlotData.ByLocation(data, "location_vars",
                v => new[] { v.Value<int>("var"), v.Value<int>("at_least") });
            LocationBerries = SlotData.ByLocation(data, "location_berries", v => v.Value<int>());
            LocationAdded = SlotData.ByLocation(data, "location_added",
                v => new[] { v.Value<int>("type"), v.Value<int>("item") });
            LocationDiscoveries = SlotData.ByLocation(data, "location_discoveries", v => v.Value<int>());
            List<long> silent = SlotData.List(data, "silent_locations", e => e.Value<long>());
            SilentLocations = silent != null ? new HashSet<long>(silent) : null;
            List<long> quiet = SlotData.List(data, "quiet_locations", e => e.Value<long>());
            QuietLocations = quiet != null ? new HashSet<long>(quiet) : null;
            LocationShops = SlotData.ByLocation(data, "location_shops",
                v => new[] { v.Value<int>("shop"), v.Value<int>("medal") });
            LocationItemShops = SlotData.ByLocation(data, "location_item_shops", v => new ApConnection.ItemShopSlot
            {
                Map = v.Value<string>("map"),
                Keeper = v.Value<string>("keeper"),
                Item = v.Value<int>("item"),
            });
            DoorTargets = SlotData.List(data, "door_targets", e => new DoorShuffle.Target
            {
                Map = e.Value<string>("map"),
                Door = e.Value<string>("door"),
                LikeMap = e.Value<string>("like_map"),
                LikeDoor = e.Value<string>("like_door"),
            });
            EnemySwaps = SlotData.Object(data, "enemy_swaps")?.Properties()
                .ToDictionary(p => p.Name, p => p.Value.ToObject<int[]>());
            MusicMap = SlotData.Object(data, "music_map")?.Properties()
                .ToDictionary(p => p.Name, p => p.Value.Value<string>());
            JingleMap = SlotData.Object(data, "jingle_map")?.Properties()
                .ToDictionary(p => p.Name, p => p.Value.Value<string>());
            ShopInventories = SlotData.List(data, "shop_inventories", e => new ApConnection.InventorySpot
            {
                Map = e.Value<string>("map"),
                Keeper = e.Value<string>("keeper"),
                Regional = e.Value<int?>("regional") ?? -1,
                Item = e.Value<int>("item"),
                To = e.Value<int>("to"),
            });
            JObject startData = SlotData.Object(data, "start");
            if (startData != null
                && (startData["map"] == null || startData["entity"] == null && startData["from"] == null))
            {
                startData = null;
            }
            StartFrom = startData?.Value<string>("from");
            Start = startData != null
                ? new KeyValuePair<string, int>(startData.Value<string>("map"),
                    startData["entity"] != null ? startData.Value<int>("entity") : -1)
                : (KeyValuePair<string, int>?)null;
            OwnSlot = ownSlot;
            object member = null;
            StartingMemberGiven = data != null && data.TryGetValue("starting_member", out member) && member != null;
            StartingMember = StartingMemberGiven ? Convert.ToInt32(member) : -1;
            ItemKinds = SlotData.ByLocation(data, "item_kinds", v => v.Value<int>());
            MovesShuffled = data != null && data.TryGetValue("shuffle_moves", out object moves) && moves is bool movesOn
                && movesOn;
            JumpShuffled = data != null && data.TryGetValue("shuffle_jump", out object jump) && jump is bool jumpOn
                && jumpOn;
            AbilityItems = data != null && data.TryGetValue("ability_items", out object abilities)
                && abilities is bool abilitiesOn && abilitiesOn;
            ArtifactsRequired = data != null && data.TryGetValue("artifacts_required", out object required)
                && required != null
                ? Convert.ToInt32(required) : 0;
        }

        // Every location whose item a find shows, in the order they were scouted before.
        internal List<long> ScoutedLocations() =>
            (LocationFlags?.Keys ?? Enumerable.Empty<long>()).Concat(LocationVars?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationBerries?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationDiscoveries?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationShops?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationItemShops?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationPickups?.Keys ?? Enumerable.Empty<long>()).Distinct().ToList();

        private static ApConnection.Blocker ReadBlocker(JToken e)
        {
            return new ApConnection.Blocker
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                Flag = e["flag"] != null ? e.Value<int>("flag") : -1,
            };
        }
    }
}
