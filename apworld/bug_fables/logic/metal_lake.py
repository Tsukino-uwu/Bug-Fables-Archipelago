"""Metal Lake, Mystery Island included (the game's area 16, MapControl.areaid): its dock. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY
from ..data_types import ItemEntity, Transfer

TRANSFERS = (
    Transfer("submarine", "MysteryIsland", "MetalLake", LATER_CHAPTERS & SUBMARINE),
)
# The submarine's dock exists with its key item in the bag (Mystery Island's needs no story flag).
PRESENT_WITH_ITEM = (
    ItemEntity("MysteryIsland", "Fixedsub", SUBMARINE_KEY),
)
