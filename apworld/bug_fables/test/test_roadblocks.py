from BaseClasses import CollectionState

from . import BugFablesTestBase
from ..data_tables import door_name

CAVE_SIDE = "NearSnakemouth (Cave Side)"
CROSSINGS = ("NearSnakemouth to NearSnakemouth (Cave Side)", "NearSnakemouth (Cave Side) to NearSnakemouth")
BARRIER_SCENERY = ({"map": "NearSnakemouth", "entity": "map1v4 (1)/snakemouthgate"},
                   {"map": "NearSnakemouth", "entity": "map1v4 (1)/snakemouthgate/Gate"})
BARRIER_NPCS = ({"map": "NearSnakemouth", "entity": "guard"}, {"map": "NearSnakemouth", "entity": "sign"})


class TestSnakemouthBarrierOff(BugFablesTestBase):
    # Not chosen: the barrier and its guard stay away all game, and crossing needs nothing.
    def test_pieces_kept_away(self) -> None:
        data = self.world.fill_slot_data()
        for piece in BARRIER_SCENERY:
            self.assertIn(piece, data["scenery_hidden"])
            self.assertNotIn(piece, data["scenery_present"])
        for npc in BARRIER_NPCS:
            self.assertIn(npc, data["kept_open"])
            self.assertNotIn(npc, data["kept_present"])

    def test_crossing_free(self) -> None:
        state = CollectionState(self.multiworld)
        for name in CROSSINGS:
            with self.subTest(entrance=name):
                self.assertTrue(self.multiworld.get_entrance(name, self.player).access_rule(state))


class TestSnakemouthBarrierOn(BugFablesTestBase):
    # Chosen: the barrier, its guard and sign stand from the start, and Beetle Dig crosses it both ways.
    options = {"extra_roadblocks": ["Snakemouth Barrier"]}

    def test_pieces_stand(self) -> None:
        data = self.world.fill_slot_data()
        for piece in BARRIER_SCENERY:
            self.assertIn(piece, data["scenery_present"])
            self.assertNotIn(piece, data["scenery_hidden"])
        for npc in BARRIER_NPCS:
            self.assertIn(npc, data["kept_present"])
            self.assertNotIn(npc, data["kept_open"])

    def test_crossing_needs_dig(self) -> None:
        state = CollectionState(self.multiworld)
        for name in CROSSINGS:
            with self.subTest(entrance=name):
                entrance = self.multiworld.get_entrance(name, self.player)
                self.assertFalse(entrance.access_rule(state))
        self.collect_by_name(["Beetle Dig", "Kabbu"])
        for name in CROSSINGS:
            with self.subTest(entrance=name, dig=True):
                self.assertTrue(self.multiworld.get_entrance(name, self.player).access_rule(self.multiworld.state))

    def test_cave_door_behind_it(self) -> None:
        # The cave door stands behind the barrier, and coming back through it lands there too.
        door = self.multiworld.get_entrance(door_name("NearSnakemouth", "loading zone cave"), self.player)
        self.assertEqual(door.parent_region.name, CAVE_SIDE)
        back = self.multiworld.get_entrance(door_name("BugariaOutskirtsSnakemouthCorridor2", "DoorMap1"), self.player)
        self.assertEqual(back.connected_region.name, CAVE_SIDE)


class TestSnakemouthBarrierDecoupled(BugFablesTestBase):
    # With doors shuffled one way, the barrier still holds: every region reachable with everything, the seed fills.
    options = {"extra_roadblocks": ["Snakemouth Barrier"], "entrance_randomizer": "decoupled"}

    def test_cave_side_reachable_with_everything(self) -> None:
        self.assertTrue(self.multiworld.get_all_state().can_reach_region(CAVE_SIDE, self.player))
