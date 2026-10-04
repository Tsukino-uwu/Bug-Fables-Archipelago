from . import BugFablesTestBase
from ..data_tables import DOORS, ROOM_STARTS
from ..data_types import EntityRef, RoomStart
from ..logic import KEPT_PRESENT


class TestStartOffByDefault(BugFablesTestBase):
    def test_game_start(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["start"], {})


class TestStartAnywhere(BugFablesTestBase):
    options = {"starting_location": "anywhere"}

    def test_start_is_a_room_entered_through_a_door(self) -> None:
        start = self.world.fill_slot_data()["start"]
        self.assertIn(start, [room.to_slot() for room in ROOM_STARTS])
        self.assertIn({start["map"], start["from"]}, [{c.a.map, c.b.map} for c in DOORS.connections])

    def test_every_room_can_be_picked_both_ways(self) -> None:
        # A connection between two maps gives an arrival into each, unless the landing door is absent on a new file.
        for c in DOORS.connections:
            for end, other in ((c.a, c.b), (c.b, c.a)):
                if end.map != other.map and ((end.map, end.door) not in DOORS.gated
                                             or EntityRef(end.map, end.door) in KEPT_PRESENT):
                    self.assertIn(RoomStart(map=end.map, from_map=other.map), ROOM_STARTS)

    def test_never_lands_at_a_door_the_game_hides(self) -> None:
        # The desert border's door to the Far Grasslands is made only in chapter 5 (flag 348): a start there fell
        # forever until the seed kept it present. Every landing door exists on a new file, or the seed keeps it.
        landing = {(end.map, other.map): end.door for c in DOORS.connections
                   for end, other in ((c.a, c.b), (c.b, c.a))}
        for start in ROOM_STARTS:
            door = landing[(start.map, start.from_map)]
            with self.subTest(start=start):
                self.assertTrue((start.map, door) not in DOORS.gated or EntityRef(start.map, door) in KEPT_PRESENT)
        self.assertIn(RoomStart(map="DesertFGBorder", from_map="FarGrasslands1"), ROOM_STARTS)

    def test_same_seed_same_start(self) -> None:
        # generate_early ran once with the seed's random; asking twice gives what it chose, not a new pick.
        self.assertEqual(self.world.fill_slot_data()["start"], self.world.fill_slot_data()["start"])
