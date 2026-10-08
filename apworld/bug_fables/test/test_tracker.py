"""Universal Tracker regenerates a seed from its slot_data with no yaml (build step 40): the rebuilt world must be the
seed's own, location for location and door for door, and reach the same locations with the same items."""
from __future__ import annotations

import json
from collections.abc import Iterable, Mapping
from random import Random
from typing import Any
from unittest import TestCase

from BaseClasses import CollectionState, LocationProgressType
from test.general import setup_multiworld
from worlds.AutoWorld import call_all
from worlds.generic.Rules import exclusion_rules

from . import entrance_graph, generate_like_main
from .test_doors import PLANDO
from ..data_tables import TRACKER_ORDER
from ..world import BugFablesWorld

# Universal Tracker's TrackerCore.TMain: these steps, its exclusions after set_rules, nothing after generate_basic.
TRACKER_STEPS = ("generate_early", "create_regions", "create_items", "set_rules", "connect_entrances", "generate_basic")
DOORS_PRESET = {"entrance_randomizer": "coupled", "enemy_shuffle": "enemies_only", "starting_location": "anywhere",
                "shuffle_discoveries": True}
SHOP, OPENING, PIER = ("Bugaria City: Commercial District, Medal Shop 2", "Outskirts: Maki and Eetl's Gift",
                       "Outskirts: Pier, Ship's Wheel")
CASES: dict[str, dict[str, Any]] = {
    "defaults": {},
    "coupled": DOORS_PRESET,
    "decoupled": {"entrance_randomizer": "decoupled", "music_shuffle": True},
    "room swap": {"entrance_randomizer": "room_swap"},
    "coupled plando": {"entrance_randomizer": "coupled", "plando_connections": PLANDO},
    "decoupled plando": {"entrance_randomizer": "decoupled", "plando_connections": PLANDO},
    "room swap plando": {"entrance_randomizer": "room_swap", "plando_connections": PLANDO},
    # Short of filler without crystal berries (2026-10-08: the default seed has enough since the room mapping).
    "filler only, fallen back": {"shop_contents": "filler_only", "shuffle_crystal_berries": False},
    "filler only, held": {"shop_contents": "filler_only", "shuffle_discoveries": True, "starting_party_member": "off",
                          "filler_starting_checks": False},
    "fallback and exclusions": {"shop_contents": "filler_only", "shuffle_crystal_berries": False,
                                "exclude_locations": [SHOP, OPENING, PIER]},
    "random member, moves, jump": {"starting_party_member": "random_member", "shuffle_field_moves": True,
                                   "shuffle_jump": True},
    "vi": {"starting_party_member": "vi"},
    "story party": {"starting_party_member": "off", "shuffle_crystal_berries": False, "shuffle_quests": False},
    "seven artifacts": {"artifacts_required": 7},
    "boat apart": {"progressive_boat": False, "shop_contents": "anything"},
    "points of no return": {"points_of_no_return": True, "shuffle_item_shops": False},
    "minimal, coupled": {"accessibility": "minimal", "entrance_randomizer": "coupled"},
    "start inventory": {"start_inventory": {"Explorer Permit": 1}, "shuffle_medal_shops": False},
}


def as_sent(slot_data: Mapping[str, Any]) -> dict[str, Any]:
    """slot_data as the server sends it: through JSON."""
    return json.loads(json.dumps(slot_data))


def regenerate(slot_data: Mapping[str, Any], seed: int) -> BugFablesWorld:
    """The world as Universal Tracker rebuilds it: every option at its default (its empty yaml), the slot_data as
    re_gen_passthrough, its steps and exclusions, and no precollected item with an id (the server sends those)."""
    multiworld = setup_multiworld(BugFablesWorld, steps=(), seed=seed)
    multiworld.generation_is_fake = True
    multiworld.re_gen_passthrough = {BugFablesWorld.game: slot_data}
    for step in TRACKER_STEPS:
        call_all(multiworld, step)
        if step == "set_rules":
            exclusion_rules(multiworld, 1, multiworld.worlds[1].options.exclude_locations.value)
    multiworld.precollected_items = {player: [item for item in items if item.code is None]
                                     for player, items in multiworld.precollected_items.items()}
    return multiworld.worlds[1]


def spots(world: BugFablesWorld) -> set[tuple[str, int | None, LocationProgressType]]:
    """Every location and event, with how fill treats it (priority, which Universal Tracker never applies, as default)."""
    return {(loc.name, loc.address, LocationProgressType.DEFAULT
             if loc.progress_type == LocationProgressType.PRIORITY else loc.progress_type)
            for loc in world.get_locations()}


def reached(world: BugFablesWorld, names: Iterable[str]) -> set[str]:
    """The locations in logic holding these items, swept for events as Universal Tracker sweeps."""
    state = CollectionState(world.multiworld)
    for name in names:
        state.collect(world.create_item(name), prevent_sweep=True)
    state.sweep_for_advancements(locations=[loc for loc in world.get_locations() if loc.address is None])
    return {loc.name for loc in world.multiworld.get_reachable_locations(state, world.player)
            if loc.address is not None}


class TestTrackerRebuildsTheSeed(TestCase):
    def assert_same_seed(self, real: BugFablesWorld, rebuilt: BugFablesWorld, seed: int) -> None:
        self.assertEqual(as_sent(rebuilt.fill_slot_data()), as_sent(real.fill_slot_data()))
        self.assertEqual(entrance_graph(rebuilt), entrance_graph(real))
        self.assertEqual([e.name for e in rebuilt.multiworld.get_entrances(1) if e.connected_region is None], [])
        self.assertEqual(spots(rebuilt), spots(real))
        self.assertEqual(set(rebuilt.door_pairings), set(real.door_pairings))
        # The items the server would send: the seed's start inventory, then subsets of its progression.
        sent = [item.name for item in real.multiworld.precollected_items[1] if item.code is not None]
        progression = [item.name for item in real.multiworld.itempool if item.player == 1 and item.advancement]
        random = Random(seed)
        holds = [[], progression, *([name for name in progression if random.random() < 0.5] for _ in range(6))]
        for held in holds:
            self.assertEqual(reached(rebuilt, [*sent, *held]), reached(real, held))

    def test_every_case(self) -> None:
        for case, options in CASES.items():
            for seed in (1, 2):
                with self.subTest(case=case, seed=seed):
                    real = generate_like_main(options, seed)
                    if case.startswith("filler only") or case.startswith("fallback"):
                        self.assertEqual(real.shops_fell_back, case != "filler only, held")
                    rebuilt = regenerate(as_sent(real.fill_slot_data()), seed + 100)
                    self.assert_same_seed(real, rebuilt, seed)


class TestTrackerExplains(TestCase):
    """What Universal Tracker's /explain and /get_logical_path do with this world (its TrackerClient.py, v0.3.4), on a
    door-shuffled seed rebuilt from its slot_data: every rule explains itself, and every path walks real entrances."""

    def test_rules_and_paths(self) -> None:
        real = generate_like_main({"entrance_randomizer": "coupled", "plando_connections": PLANDO}, 5)
        world = regenerate(as_sent(real.fill_slot_data()), 105)
        progression = [item.name for item in real.multiworld.itempool if item.player == 1 and item.advancement]
        state = CollectionState(world.multiworld)
        for name in progression[::2]:
            state.collect(world.create_item(name), prevent_sweep=True)
        state.sweep_for_advancements(locations=[loc for loc in world.get_locations() if loc.address is None])
        rules = [*(loc.access_rule for loc in world.get_locations()),
                 *(entrance.access_rule for region in world.get_regions() for entrance in region.entrances
                   if entrance.parent_region)]
        for rule in rules:
            if hasattr(rule, "explain_json"):
                for parts in (rule.explain_json(state), rule.explain_json()):
                    self.assertTrue(parts)
                    self.assertTrue(all(isinstance(part.get("text"), str) for part in parts))
            else:
                self.assertIsInstance(rule(state), bool)
        reachable = [loc for loc in world.get_locations() if loc.address is not None and loc.can_reach(state)]
        self.assertTrue(reachable)
        for location in reachable:
            # get_logical_path's walk: state.path back from the location's region, (region, entrance) by name.
            step, names = state.path.get(location.parent_region, (str(location.parent_region), None)), []
            while step:
                name, step = step
                names.append(str(name))
            for entrance in names[::-1][1::2]:
                with self.subTest(location=location.name, entrance=entrance):
                    world.get_entrance(entrance)


class TestTrackerHooks(TestCase):
    def test_no_yaml_needed(self) -> None:
        # Universal Tracker skips its own first generation and regenerates from the slot_data it is handed back.
        self.assertTrue(BugFablesWorld.ut_can_gen_without_yaml)
        slot_data = {"world_version": "any"}
        self.assertIs(BugFablesWorld.interpret_slot_data(slot_data), slot_data)

    def test_another_version_is_refused(self) -> None:
        slot_data = as_sent(generate_like_main({}, 1).fill_slot_data())
        for changed in ({**slot_data, "world_version": "0.0.1"}, {k: v for k, v in slot_data.items() if k != "options"}):
            with self.subTest(keys=sorted(set(slot_data) ^ set(changed))), self.assertRaises(ValueError):
                regenerate(changed, 3)

    def test_the_list_in_story_order(self) -> None:
        # Areas as the story reaches them, each one's spots by name; an entrance or anything unknown after them all.
        world = generate_like_main({}, 1)
        order = sorted((loc.name for loc in world.get_locations() if loc.address is not None),
                       key=lambda name: world.custom_ut_sort("", name))
        self.assertTrue(order[0].startswith("Outskirts: "))
        outskirts = [name for name in order if name.startswith("Outskirts: ")]
        self.assertEqual(outskirts, sorted(outskirts))
        self.assertLess(order.index("Outskirts: Pier, Behind the Dock"),
                        order.index("Bugaria City: Commercial District, Medal Shop 1"))
        self.assertEqual(world.custom_ut_sort("BugariaPier", "BugariaPier: loadzone"), len(TRACKER_ORDER))

    def test_another_game_s_passthrough_is_not_read(self) -> None:
        multiworld = setup_multiworld(BugFablesWorld, steps=(), seed=4, options={"entrance_randomizer": "coupled"})
        multiworld.re_gen_passthrough = {"Another Game": {"options": {}}}
        for step in TRACKER_STEPS:
            call_all(multiworld, step)
        world = multiworld.worlds[1]
        self.assertIsNone(world.passthrough)
        self.assertTrue(world.door_targets)
