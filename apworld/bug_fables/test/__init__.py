from collections.abc import Iterable, Mapping
from typing import Any

from BaseClasses import CollectionState, EntranceType, ItemClassification
from rule_builder.rules import Rule
from test.bases import WorldTestBase
from test.general import setup_multiworld
from worlds.AutoWorld import call_all
from worlds.generic.Rules import exclusion_rules

from ..data_tables import ARTIFACTS, DOOR_RULES, LOCATIONS, STORY_EVENTS, TRANSFERS
from ..items import BugFablesItem
from ..world import BugFablesWorld

MAIN_STEPS = ("generate_early", "create_regions", "create_items", "set_rules", "connect_entrances", "generate_basic",
              "pre_fill")


def generate_like_main(options: Mapping[str, Any], seed: int,
                       steps: tuple[str, ...] = MAIN_STEPS) -> BugFablesWorld:
    """A solo world built step by step as Main.py builds it, the player's exclusions applied right after set_rules
    (WorldTestBase never applies them)."""
    multiworld = setup_multiworld(BugFablesWorld, steps=(), seed=seed, options=dict(options))
    for step in steps:
        call_all(multiworld, step)
        if step == "set_rules":
            exclusion_rules(multiworld, 1, multiworld.worlds[1].options.exclude_locations.value)
    return multiworld.worlds[1]


def entrance_graph(world: BugFablesWorld) -> set[tuple[str, str, str | None, EntranceType]]:
    """Every entrance: its name, where it is, where it leads, and how the randomizer treats it."""
    return {(e.name, e.parent_region.name, e.connected_region.name if e.connected_region else None,
             e.randomization_type) for e in world.multiworld.get_entrances(world.player)}


def rule_parts(rule: Rule | None) -> Iterable[Rule]:
    """A rule and every rule inside it (the children of an And or an Or, a WayBack's child), unresolved."""
    if rule is None:
        return
    yield rule
    for child in getattr(rule, "children", ()):
        yield from rule_parts(child)
    yield from rule_parts(getattr(rule, "child", None))


def logic_rules() -> Iterable[tuple[str, Rule | None]]:
    """Every rule the logic writes, with where it is: each door gate, transfer, and spot (its own rule and its reach)."""
    for gate in DOOR_RULES:
        yield f"{gate.map}: {gate.door}", gate.rule
    for transfer in TRANSFERS:
        yield f"{transfer.from_map} to {transfer.to_map} ({transfer.name})", transfer.rule
        yield f"{transfer.from_map} to {transfer.to_map} ({transfer.name}, way back)", transfer.way_back
    for spot in (*LOCATIONS, *STORY_EVENTS):
        yield spot.name, spot.rule
    for spot in (*LOCATIONS, *STORY_EVENTS, *ARTIFACTS):
        yield f"{spot.name} (reach)", spot.reach


class BugFablesTestBase(WorldTestBase):
    game = "Bug Fables"

    def state_with(self, *names: str) -> CollectionState:
        """A state holding exactly these items or events, unswept."""
        state = CollectionState(self.multiworld)
        self.add(state, *names)
        return state

    def add(self, state: CollectionState, *names: str) -> None:
        for name in names:
            state.collect(BugFablesItem(name, ItemClassification.progression, None, self.player), prevent_sweep=True)
