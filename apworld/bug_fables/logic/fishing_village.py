"""Fishing Village (the game's area 23, MapControl.areaid): its dock. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY
from ..data_types import ItemEntity, Transfer

TRANSFERS = (
    Transfer("submarine", "FishingVillage", "MetalLake", LATER_CHAPTERS & SUBMARINE),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("FishingVillage", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
)
