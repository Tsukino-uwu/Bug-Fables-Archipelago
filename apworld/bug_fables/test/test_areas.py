"""The logic's area modules (logic/) on the map regions: how they fit together, and that every name a rule uses
exists."""
from BaseClasses import EntranceType
from rule_builder.rules import Has

from . import BugFablesTestBase, logic_rules, rule_parts
from ..abilities import ABILITIES
from ..custom_rules import CanUse, Member, MoveItem
from ..data_tables import (ARTIFACTS, DOOR_RULES, DOORS, ENCOUNTERS, ITEMS, LOCATIONS, MAPS, ONE_WAYS, REGIONS,
                           ROOM_STARTS, STARTS, STORY_EVENTS, TRANSFERS, UNUSED_MAPS, door_name, door_region)

ALL_SPOTS = (*LOCATIONS, *STORY_EVENTS, *ARTIFACTS)
# The unused room and the debug room: never part of anything (room-logic.md, the model).
UNUSED = ("SnakemouthEmpty", "TestRoom")


class TestAreas(BugFablesTestBase):
    options = {"shuffle_discoveries": True}

    def test_every_spot_is_in_a_map(self) -> None:
        for spot in ALL_SPOTS:
            with self.subTest(spot=spot.name):
                self.assertIn(spot.region, MAPS)

    def test_every_spot_is_in_its_source_map(self) -> None:
        # Where the game data names the spot's map, the logic must put it there.
        for spot in ALL_SPOTS:
            for named in (spot.source.pickup, spot.source.give, spot.source.item_shop):
                if named is not None:
                    with self.subTest(spot=spot.name):
                        self.assertEqual(spot.region, named.map)

    def test_names_are_unique(self) -> None:
        spots = [spot.name for spot in ALL_SPOTS]
        self.assertEqual(len(spots), len(set(spots)))

    def test_every_door_is_an_entrance(self) -> None:
        # Where the game has it, with the doors as they are (Archipelago's entrance randomizer shuffles these); a door a
        # roadblock cuts off stands in its map area's region.
        for connection in DOORS.connections:
            for end, other in ((connection.a, connection.b), (connection.b, connection.a)):
                with self.subTest(door=door_name(end.map, end.door)):
                    entrance = self.multiworld.get_entrance(door_name(end.map, end.door), self.player)
                    self.assertEqual(entrance.parent_region.name, door_region(end.map, end.door))
                    self.assertEqual(entrance.connected_region.name, door_region(other.map, other.door))

    def test_every_one_way_door_is_a_one_way_entrance(self) -> None:
        # A fog maze's wrong turn or a drop: an entrance where the game has it, one-way for Archipelago's randomizer.
        for door in ONE_WAYS:
            with self.subTest(door=door_name(door.map, door.door)):
                entrance = self.multiworld.get_entrance(door_name(door.map, door.door), self.player)
                self.assertEqual(entrance.parent_region.name, door.map)
                self.assertEqual(entrance.connected_region.name, door.to)
                self.assertEqual(entrance.randomization_type, EntranceType.ONE_WAY)

    def test_door_gates_and_transfers_name_real_places(self) -> None:
        doors = {(end.map, end.door) for c in DOORS.connections for end in (c.a, c.b)}
        for gate in DOOR_RULES:
            with self.subTest(gate=gate):
                self.assertIn((gate.map, gate.door), doors)
        for transfer in TRANSFERS:
            with self.subTest(transfer=transfer.name):
                self.assertIn(transfer.from_map, MAPS)
                self.assertIn(transfer.to_map, MAPS)

    def test_every_region_reachable_with_everything(self) -> None:
        state = self.multiworld.get_all_state()
        for name in REGIONS:
            with self.subTest(region=name):
                self.assertTrue(state.can_reach_region(name, self.player))

    def test_locations_in_id_order(self) -> None:
        # Moving a spot from one module to another must never change a seed.
        ids = [location.id for location in LOCATIONS]
        self.assertEqual(ids, sorted(ids))

    def test_every_name_a_rule_uses_exists(self) -> None:
        # A misspelt name in a rule would make a spot unreachable, or quietly need nothing.
        items = {item.name for item in ITEMS} | {event.item for event in STORY_EVENTS}
        members = {item.name for item in ITEMS if item.member}
        for where, rule in logic_rules():
            for part in rule_parts(rule):
                with self.subTest(where=where, rule=str(part)):
                    if isinstance(part, Has):
                        self.assertIn(part.item_name, items)
                    elif isinstance(part, (CanUse, MoveItem)):
                        self.assertIn(part.ability, ABILITIES)
                    elif isinstance(part, Member):
                        self.assertIn(part.name, members)


class TestUnusedMaps(BugFablesTestBase):
    # Every door shuffled both ways, a random start and enemies shuffled: none may reach an unused map.
    options = {"entrance_randomizer": "decoupled", "starting_location": "anywhere", "enemy_shuffle": "enemies_only"}

    def test_listed(self) -> None:
        for name in UNUSED:
            self.assertIn(name, UNUSED_MAPS)

    def test_part_of_nothing(self) -> None:
        entrances = [e for e in self.multiworld.get_entrances(self.player) if e.parent_region is not None]
        slot = self.world.fill_slot_data()
        uses = {
            "region": {region.name for region in self.multiworld.get_regions(self.player)},
            "entrance": {region.name for e in entrances for region in (e.parent_region, e.connected_region) if region},
            "transfer": {m for t in TRANSFERS for m in (t.from_map, t.to_map)},
            "spot": {spot.region for spot in ALL_SPOTS},
            "encounter": {e.map for e in ENCOUNTERS},
            "save point": {s.map for s in STARTS},
            "room start": {m for s in ROOM_STARTS for m in (s.map, s.from_map)},
            "one-way door": {m for w in ONE_WAYS for m in (w.map, w.to)},
            "door target": {t[key] for t in slot["door_targets"] for key in ("map", "like_map")},
            "start": {slot["start"].get("map"), slot["start"].get("from")},
        }
        for name in UNUSED:
            for kind, maps in uses.items():
                with self.subTest(map=name, kind=kind):
                    self.assertNotIn(name, maps)


class TestExplorerPermitGate(BugFablesTestBase):
    # The gate outside the city stands between the Outskirts and Snakemouth Den's corridor, both ways.
    PAST = "BugariaOutskirtsOutsideCity (Past the Gate)"

    def test_corridor_door_behind_it(self) -> None:
        door = self.multiworld.get_entrance(door_name("BugariaOutskirtsOutsideCity", "DoorSnakemouth"), self.player)
        self.assertEqual(door.parent_region.name, self.PAST)

    def test_crossing_needs_permit(self) -> None:
        crossings = [self.multiworld.get_entrance(name, self.player) for name in (
            f"BugariaOutskirtsOutsideCity to {self.PAST}", f"{self.PAST} to BugariaOutskirtsOutsideCity")]
        for entrance in crossings:
            self.assertFalse(entrance.access_rule(self.multiworld.state))
        self.collect_by_name(["Explorer Permit"])
        for entrance in crossings:
            self.assertTrue(entrance.access_rule(self.multiworld.state))


class TestExplorerPermitGateOneWay(BugFablesTestBase):
    # Arriving from the corridor, the game moves the party past the gate: with Points of No Return on, leaving is free.
    options = {"points_of_no_return": True}

    def test_leaving_free(self) -> None:
        gate = TestExplorerPermitGate.PAST
        out = self.multiworld.get_entrance(f"{gate} to BugariaOutskirtsOutsideCity", self.player)
        self.assertTrue(out.access_rule(self.multiworld.state))
        into = self.multiworld.get_entrance(f"BugariaOutskirtsOutsideCity to {gate}", self.player)
        self.assertFalse(into.access_rule(self.multiworld.state))


class TestOutsideSnakemouth(BugFablesTestBase):
    # The den's door on a ledge (Jump up, a drop down), the corridor's side past grass (the horn), the berry between.
    options = {"shuffle_jump": True, "shuffle_field_moves": True}

    def crossing(self, a: str, b: str) -> bool:
        return self.multiworld.get_entrance(f"{a} to {b}", self.player).access_rule(self.multiworld.state)

    def test_doors_in_their_areas(self) -> None:
        for door, area in (("LoadingZoneInside", "Cave Ledge"), ("loading zone outside", "Corridor Side")):
            with self.subTest(door=door):
                entrance = self.multiworld.get_entrance(door_name("OutsideSnakemouth", door), self.player)
                self.assertEqual(entrance.parent_region.name, f"OutsideSnakemouth ({area})")

    def test_crossings(self) -> None:
        ledge, side, middle = "OutsideSnakemouth (Cave Ledge)", "OutsideSnakemouth (Corridor Side)", "OutsideSnakemouth"
        for a, b in ((middle, ledge), (ledge, middle), (middle, side), (side, middle)):
            with self.subTest(way=f"{a} to {b}"):
                self.assertFalse(self.crossing(a, b))
        self.collect_by_name(["Jump"])
        self.assertTrue(self.crossing(middle, ledge))
        self.assertTrue(self.crossing(ledge, middle))
        self.assertFalse(self.crossing(middle, side))
        self.collect_by_name(["Horn Slash", "Kabbu"])
        self.assertTrue(self.crossing(middle, side))
        self.assertTrue(self.crossing(side, middle))


class TestFirstCorridor(BugFablesTestBase):
    # Corridor1: its left end past gaps and ledges (Jump), its Seedling Haven door across water (Jump and Icicle).
    options = {"shuffle_jump": True, "shuffle_field_moves": True}
    MAP = "BugariaOutskitsSnakemouthCorridor1"

    def crossing(self, a: str, b: str) -> bool:
        return self.multiworld.get_entrance(f"{a} to {b}", self.player).access_rule(self.multiworld.state)

    def test_crossings(self) -> None:
        left, haven = f"{self.MAP} (Left)", f"{self.MAP} (Haven Door)"
        ways = ((self.MAP, left), (left, self.MAP), (self.MAP, haven), (haven, self.MAP))
        for a, b in ways:
            self.assertFalse(self.crossing(a, b))
        self.collect_by_name(["Jump"])
        self.assertTrue(self.crossing(self.MAP, left) and self.crossing(left, self.MAP))
        self.assertFalse(self.crossing(self.MAP, haven))
        self.collect_by_name(["Progressive Freeze", "Progressive Freeze", "Leif"])
        self.assertTrue(self.crossing(self.MAP, haven) and self.crossing(haven, self.MAP))
