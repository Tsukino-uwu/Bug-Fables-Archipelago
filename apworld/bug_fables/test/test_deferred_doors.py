"""Universal Tracker's deferred entrances (build step 55): a rebuilt seed keeps its shuffled doors unconnected until the
player has gone through them, as the doors key tells, unless its host.yaml turns that off."""
from __future__ import annotations

from collections.abc import Mapping
from typing import Any
from unittest import TestCase

from BaseClasses import CollectionState
from test.general import setup_multiworld
from worlds.AutoWorld import call_all
from worlds.generic.Rules import exclusion_rules

from . import generate_like_main
from .test_tracker import TRACKER_STEPS, as_sent, reached
from ..data_tables import ONE_WAYS, door_name
from ..universal_tracker import DOORS_TAKEN_KEY
from ..world import BugFablesWorld


def rebuild(slot_data: Mapping[str, Any], seed: int, deferred: str) -> BugFablesWorld:
    """test_tracker.regenerate, with enforce_deferred_connections set as Universal Tracker's TrackerCore sets it."""
    multiworld = setup_multiworld(BugFablesWorld, steps=(), seed=seed)
    multiworld.generation_is_fake = True
    multiworld.re_gen_passthrough = {BugFablesWorld.game: slot_data}
    multiworld.enforce_deferred_connections = deferred
    for step in TRACKER_STEPS:
        call_all(multiworld, step)
        if step == "set_rules":
            exclusion_rules(multiworld, 1, multiworld.worlds[1].options.exclude_locations.value)
    return multiworld.worlds[1]


def connected(world: BugFablesWorld) -> dict[str, str | None]:
    return {e.name: e.connected_region.name if e.connected_region else None for e in world.multiworld.get_entrances(1)}


class DeferredDoorsBase(TestCase):
    options: dict[str, Any] = {"entrance_randomizer": "coupled"}

    def setUp(self) -> None:
        self.real = generate_like_main(self.options, 1)
        self.slot_data = as_sent(self.real.fill_slot_data())
        self.world = rebuild(self.slot_data, 101, "default")
        self.doors = [door_name(*x) for x, _ in self.real.door_pairings]


class TestCoupledDoorsDeferred(DeferredDoorsBase):
    def test_every_shuffled_door_waits(self) -> None:
        now = connected(self.world)
        self.assertTrue(self.doors)
        self.assertEqual([door for door in self.doors if now[door] is not None], [])
        # What is not a shuffled door (fixed links, transfers, a map's areas) stays as the seed has it.
        real = connected(self.real)
        self.assertEqual({k: v for k, v in now.items() if k not in self.doors},
                         {k: v for k, v in real.items() if k not in self.doors})
        self.assertEqual(self.world.found_entrances_datastorage_key, DOORS_TAKEN_KEY)
        self.assertEqual(DOORS_TAKEN_KEY.format(team=0, player=1), "bug_fables_doors_0_1")

    def test_the_tracker_sweeps_while_doors_wait(self) -> None:
        # Universal Tracker's state allows unconnected entrances while it defers (TrackerCore's CollectionState).
        state = CollectionState(self.world.multiworld, True)
        self.assertTrue(self.world.multiworld.get_reachable_locations(state, 1))

    def test_a_door_taken_and_its_way_back(self) -> None:
        real = connected(self.real)
        x, y = next((x, y) for x, y in self.real.door_pairings if (y, x) in set(self.real.door_pairings))
        self.world.reconnect_found_entrances(DOORS_TAKEN_KEY, [door_name(*x)])
        now = connected(self.world)
        self.assertEqual(now[door_name(*x)], real[door_name(*x)])
        self.assertEqual(now[door_name(*y)], real[door_name(*y)])
        self.assertEqual(sum(1 for door in self.doors if now[door] is not None), 2)

    def test_every_door_taken_is_the_seed_again(self) -> None:
        self.world.reconnect_found_entrances(DOORS_TAKEN_KEY, self.doors)
        self.assertEqual(connected(self.world), connected(self.real))
        progression = [item.name for item in self.real.multiworld.itempool if item.player == 1 and item.advancement]
        sent = [item.name for item in self.real.multiworld.precollected_items[1] if item.code is not None]
        self.assertEqual(reached(self.world, [*sent, *progression]), reached(self.real, progression))

    def test_anything_else_in_the_key_is_left_alone(self) -> None:
        for value in (None, {}, {"a": 1}, [1, None, ["x"]], ["Nowhere: nodoor"], "a string"):
            with self.subTest(value=value):
                self.world.reconnect_found_entrances(DOORS_TAKEN_KEY, value)
        now = connected(self.world)
        self.assertEqual([door for door in self.doors if now[door] is not None], [])


class TestDecoupledDoorsDeferred(DeferredDoorsBase):
    options = {"entrance_randomizer": "decoupled"}

    def test_only_the_door_taken(self) -> None:
        real = connected(self.real)
        door = self.doors[0]
        self.world.reconnect_found_entrances(DOORS_TAKEN_KEY, [door])
        now = connected(self.world)
        self.assertEqual(now[door], real[door])
        self.assertEqual([d for d in self.doors if now[d] is not None], [door])

    def test_a_one_way_copy_is_its_door(self) -> None:
        # The mod writes the door the player went through; a story copy of a one-way is the same entrance here.
        w = next(w for w in ONE_WAYS if w.copies)
        base = door_name(w.map, w.door)
        self.assertIn(base, self.doors)
        self.world.reconnect_found_entrances(DOORS_TAKEN_KEY, [door_name(w.map, w.copies[0])])
        self.assertEqual(connected(self.world)[base], connected(self.real)[base])


class TestNotDeferred(TestCase):
    def test_off_shows_every_door(self) -> None:
        real = generate_like_main({"entrance_randomizer": "coupled"}, 1)
        world = rebuild(as_sent(real.fill_slot_data()), 101, "off")
        self.assertEqual(connected(world), connected(real))
        self.assertIsNone(getattr(world, "found_entrances_datastorage_key", None))

    def test_no_shuffled_doors_nothing_to_defer(self) -> None:
        real = generate_like_main({}, 1)
        world = rebuild(as_sent(real.fill_slot_data()), 101, "on")
        self.assertEqual(connected(world), connected(real))
        self.assertIsNone(getattr(world, "found_entrances_datastorage_key", None))

    def test_a_real_generation_never_defers(self) -> None:
        # Only Universal Tracker sets enforce_deferred_connections; the generator never does.
        world = generate_like_main({"entrance_randomizer": "coupled"}, 1)
        self.assertEqual([e.name for e in world.multiworld.get_entrances(1) if e.connected_region is None], [])
