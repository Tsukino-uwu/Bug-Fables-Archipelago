from collections.abc import Iterable

from BaseClasses import CollectionState, ItemClassification
from rule_builder.rules import Rule
from test.bases import WorldTestBase

from ..data_tables import ARTIFACTS, DOOR_RULES, LOCATIONS, STORY_EVENTS, TRANSFERS
from ..items import BugFablesItem


def rule_parts(rule: Rule | None) -> Iterable[Rule]:
    """A rule and every rule inside it (the children of an And or an Or), unresolved."""
    if rule is None:
        return
    yield rule
    for child in getattr(rule, "children", ()):
        yield from rule_parts(child)


def logic_rules() -> Iterable[tuple[str, Rule | None]]:
    """Every rule the logic writes, with where it is: each door gate, transfer, and spot (its own rule and its reach)."""
    for gate in DOOR_RULES:
        yield f"{gate.map}: {gate.door}", gate.rule
    for transfer in TRANSFERS:
        yield f"{transfer.from_map} to {transfer.to_map} ({transfer.name})", transfer.rule
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
