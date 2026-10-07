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
        // Enemysanity: {location id: "map:entity"}, the map enemy whose won fight drops the check.
        internal readonly Dictionary<long, string> LocationEnemies;
        // Checks that show no item of their own: the receiver shows the player's own item from them.
        internal readonly HashSet<long> SilentLocations;
        // The opening's checks: their items arrive with no hold-up.
        internal readonly HashSet<long> QuietLocations;
        internal readonly Dictionary<long, int[]> LocationShops;
        internal readonly Dictionary<long, ApConnection.ItemShopSlot> LocationItemShops;
        // The Termacade's prize stand: {location: its row in the stand}.
        internal readonly Dictionary<long, int> LocationPrizes;
        internal readonly Dictionary<long, int> ItemKinds;
        internal readonly List<ApConnection.Blocker> KeptOpen;
        internal readonly List<ApConnection.Blocker> KeptPresent;
        // Scenery entities are paths inside the map, as MapDump writes them.
        internal readonly List<ApConnection.Blocker> SceneryHidden;
        internal readonly List<ApConnection.Blocker> SceneryPresent;
        internal readonly List<ApConnection.Blocker> HeldUntil;
        internal readonly List<ApConnection.Blocker> PresentFrom;
        // Entities tied to one of the mod's key items (Blocker.Item) in the bag: the submarine's docks, and who shows
        // them off.
        internal readonly List<ApConnection.Blocker> PresentWithItem;
        internal readonly List<ApConnection.Blocker> HeldUntilItem;
        internal readonly List<ApConnection.DialogueFlag> DialogueFlags;
        internal readonly List<DoorShuffle.Target> DoorTargets;
        // Story-only maps: no Warp or map travel there. Empty for a seed from an older apworld.
        internal readonly HashSet<string> NoTravelMaps;
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
        // slot_data's "options", the options as the seed applied them; none means a seed from an older apworld.
        internal readonly bool OptionsMissing;
        // The goal: this many artifacts, as the game counts them. 0 when slot_data has none.
        internal readonly int ArtifactsRequired;
        internal readonly int OwnSlot;
        // The one member a new file starts with (0 Vi, 1 Kabbu, 2 Leif; 3 all three), when the seed names one.
        internal readonly bool StartingMemberGiven;
        internal readonly int StartingMember;
        internal readonly bool MovesShuffled;
        internal readonly bool JumpShuffled;
        internal readonly bool AbilityItems;
        internal readonly bool SubmarineItem;
        // The ant tunnels' miners dig for free.
        internal readonly bool FreeAntTunnels;
        // The Termite gate opens from inside before it was ever opened from outside.
        internal readonly bool TermiteGateFromInside;
        // {map: dialogue lines}: sellers' lines whose price reads 0.
        internal readonly Dictionary<string, int[]> FreeSales;
        // Day maps whose night the mod switches at will, each map's switch NPC, and scenery set as a scene leaves it.
        internal readonly List<DayNight.Pair> DayNightMaps;
        internal readonly List<DayNight.Switch> TimeSwitches;
        internal readonly List<DayNight.Move> SceneryMoved;
        internal readonly List<DayNight.Switch> EntitiesMoved;
        internal readonly List<DayNight.Camera> SceneCameras;
        internal readonly bool PointsOfNoReturn;

        internal SeedData(Dictionary<string, object> data, int ownSlot)
        {
            LocationFlags = SlotData.ByLocation(data, "location_flags", v => v.Value<int>());
            LocationGives = SlotData.ByLocation(data, "location_gives", v => new ApConnection.Give
            {
                Map = v.Value<string>("map"),
                Type = v.Value<int>("type"),
                Item = v.Value<int>("item"),
                Npc = v.Value<string>("npc"),
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
            PresentWithItem = SlotData.List(data, "present_with_item", ReadBlocker);
            HeldUntilItem = SlotData.List(data, "held_until_item", ReadBlocker);
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
            LocationEnemies = SlotData.ByLocation(data, "location_enemies", v => v.Value<string>());
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
            LocationPrizes = SlotData.ByLocation(data, "location_prizes", v => v.Value<int>());
            NoTravelMaps = new HashSet<string>(SlotData.List(data, "no_travel_maps", e => e.Value<string>())
                ?? new List<string>());
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
            AbilityItems = data != null && data.TryGetValue("ability_items", out object abilities)
                && abilities is bool abilitiesOn && abilitiesOn;
            SubmarineItem = data != null && data.TryGetValue("submarine_item", out object submarine)
                && submarine is bool submarineOn && submarineOn;
            FreeSales = SlotData.List(data, "free_sales", e => e)?.GroupBy(e => e.Value<string>("map"))
                .ToDictionary(g => g.Key, g => g.SelectMany(e => e["lines"].Values<int>()).ToArray());
            DayNightMaps = SlotData.List(data, "day_night", e => new DayNight.Pair
            {
                Day = e.Value<string>("day"),
                Night = e.Value<string>("night"),
                From = e.Value<int>("from"),
                Until = e.Value<int>("until"),
                FirstEvent = e.Value<int>("first_event"),
            });
            TimeSwitches = SlotData.List(data, "time_switches", e => new DayNight.Switch
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                At = Vector(e["at"]),
                Day = e["day"].Values<string>().ToArray(),
                Night = e["night"].Values<string>().ToArray(),
                CopyMap = e["copy"]?.Value<string>("map"),
                CopyEntity = e["copy"]?.Value<string>("entity"),
            });
            EntitiesMoved = SlotData.List(data, "entities_moved", e => new DayNight.Switch
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                At = Vector(e["at"]),
            });
            SceneCameras = SlotData.List(data, "scene_cameras", e => new DayNight.Camera
            {
                Map = e.Value<string>("map"),
                Event = e.Value<int>("event"),
                From = Vector(e["from"]),
                To = Vector(e["to"]),
            });
            SceneryMoved = SlotData.List(data, "scenery_moved", e => new DayNight.Move
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                Local = Vector(e["local"]),
            });
            FreeAntTunnels = data != null && data.TryGetValue("free_ant_tunnels", out object tunnels)
                && tunnels is bool tunnelsFree && tunnelsFree;
            TermiteGateFromInside = data != null && data.TryGetValue("termite_gate_from_inside", out object gate)
                && gate is bool gateOpens && gateOpens;
            JObject options = SlotData.Object(data, "options");
            OptionsMissing = options == null;
            MovesShuffled = SlotData.On(options, "shuffle_field_moves");
            JumpShuffled = SlotData.On(options, "shuffle_jump");
            PointsOfNoReturn = SlotData.On(options, "points_of_no_return");
            ArtifactsRequired = SlotData.Number(options, "artifacts_required", 0);
        }

        // Every location whose item a find shows, in the order they were scouted before.
        internal List<long> ScoutedLocations() =>
            (LocationFlags?.Keys ?? Enumerable.Empty<long>()).Concat(LocationVars?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationBerries?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationDiscoveries?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationShops?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationItemShops?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationPrizes?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationPickups?.Keys ?? Enumerable.Empty<long>())
                .Concat(LocationEnemies?.Keys ?? Enumerable.Empty<long>()).Distinct().ToList();

        private static UnityEngine.Vector3 Vector(JToken xyz)
        {
            float[] v = xyz.Values<float>().ToArray();
            return new UnityEngine.Vector3(v[0], v[1], v[2]);
        }

        private static ApConnection.Blocker ReadBlocker(JToken e)
        {
            return new ApConnection.Blocker
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                Flag = e["flag"] != null ? e.Value<int>("flag") : -1,
                Item = e["item"] != null ? e.Value<int>("item") : -1,
            };
        }
    }
}
