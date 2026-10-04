"""Metal Island (the game's area 17, MapControl.areaid): its dock. No spots yet; the boat over from the pier is the
Outskirts'."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY
from ..data_types import ItemEntity, Transfer

TRANSFERS = (
    Transfer("submarine", "MetalIsland1", "MetalLake", LATER_CHAPTERS & SUBMARINE),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("MetalIsland1", "Fixedsub - Duplicate", SUBMARINE_KEY),
)
