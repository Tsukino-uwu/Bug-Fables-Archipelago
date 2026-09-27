from BaseClasses import CollectionState, ItemClassification
from test.bases import WorldTestBase

from ..items import BugFablesItem


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
