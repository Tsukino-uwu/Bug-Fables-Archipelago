"""Universal Tracker's glitched logic (build step 104): its own copy of a seed holds an item a real seed never does, and
the harder ways the logic leaves out count with it, so the tracker shows them apart from the logic's."""
from random import Random
from unittest import TestCase

from ..custom_rules import GLITCHES_ITEM
from ..logic.giants_lair import _TO_THE_RIGHT
from ..logic.outskirts import ICE_CLIMB
from ..world import BugFablesWorld
from . import BugFablesTestBase, generate_like_main
from .test_tracker import as_sent, reached, regenerate


class TestGlitchedWays(BugFablesTestBase):
    # Moves and Jump as items, so each way's needs are items a state can hold or lack.
    options = {"shuffle_field_moves": True, "shuffle_jump": True}

    def test_the_golden_path_tunnel_climb(self) -> None:
        # Freeze and Jump alone is the harder climb; Horn Slash's is the logic's.
        rule = ICE_CLIMB.resolve(self.world)
        self.assertFalse(rule(self.state_with("Leif", "Progressive Freeze", "Jump")))
        self.assertTrue(rule(self.state_with("Leif", "Progressive Freeze", "Jump", GLITCHES_ITEM)))
        self.assertTrue(rule(self.state_with("Leif", "Progressive Freeze", "Jump", "Horn Slash")))
        self.assertFalse(rule(self.state_with("Leif", "Jump", GLITCHES_ITEM)))

    def test_the_crane_crossing(self) -> None:
        # Bee Fly alone is the harder crossing; with any attack it's the logic's.
        rule = _TO_THE_RIGHT.resolve(self.world)
        self.assertFalse(rule(self.state_with("Bee Fly")))
        self.assertTrue(rule(self.state_with("Bee Fly", GLITCHES_ITEM)))
        self.assertTrue(rule(self.state_with("Bee Fly", "Horn Slash")))
        self.assertFalse(rule(self.state_with(GLITCHES_ITEM)))


class TestGlitchedItem(TestCase):
    def test_universal_tracker_can_make_it(self) -> None:
        # Universal Tracker makes it through the multiworld's create_item, then collects it into a copy of the state.
        self.assertEqual(BugFablesWorld.glitches_item_name, GLITCHES_ITEM)
        world = generate_like_main({}, 1)
        item = world.multiworld.create_item(GLITCHES_ITEM, world.player)
        self.assertIsNone(item.code)
        self.assertTrue(item.advancement)

    def test_never_in_a_real_seed(self) -> None:
        world = generate_like_main({"entrance_randomizer": "coupled", "shuffle_field_moves": True}, 2)
        self.assertNotIn(GLITCHES_ITEM, world.item_name_to_id)
        self.assertNotIn(GLITCHES_ITEM, {item.name for item in world.multiworld.itempool})
        self.assertNotIn(GLITCHES_ITEM, {item.name for item in world.multiworld.precollected_items[world.player]})
        self.assertFalse(world.multiworld.get_all_state().has(GLITCHES_ITEM, world.player))

    def test_only_adds_to_the_logic(self) -> None:
        # In the tracker's rebuilt seed: whatever is held, the glitched reach holds every spot in logic, and with the
        # whole pool it adds nothing.
        real = generate_like_main({"shuffle_field_moves": True, "shuffle_jump": True}, 3)
        world = regenerate(as_sent(real.fill_slot_data()), 103)
        # The start inventory as the server sends it, then subsets of the pool's progression.
        sent = [item.name for item in real.multiworld.precollected_items[1] if item.code is not None]
        progression = [item.name for item in real.multiworld.itempool if item.player == 1 and item.advancement]
        random = Random(3)
        for held in ([], *([name for name in progression if random.random() < 0.5] for _ in range(4))):
            with self.subTest(held=len(held)):
                self.assertLessEqual(reached(world, [*sent, *held]), reached(world, [*sent, *held, GLITCHES_ITEM]))
        everything = [*sent, *progression]
        self.assertEqual(reached(world, [*everything, GLITCHES_ITEM]), reached(world, everything))
