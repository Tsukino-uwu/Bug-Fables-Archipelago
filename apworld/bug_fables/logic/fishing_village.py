"""Fishing Village (the game's area 23, MapControl.areaid): its one room, the village and its dock."""
from __future__ import annotations

from ..custom_rules import SUBMARINE, SUBMARINE_KEY
from ..data_types import ItemEntity, Location, Pickup, Source, Transfer

LOCATIONS = (
    # The village and its dock, one room, all free (2026-10-12): a Lore Book (flag 331) behind a plant on the left.
    Location("Fishing Village: Behind the Plant", 304, "FishingVillage",
             Source(flag=331, pickup=Pickup(map="FishingVillage", type=1, item=52)), no_jump=True),
)
TRANSFERS = (
    Transfer("submarine", "FishingVillage", "MetalLake", SUBMARINE),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("FishingVillage", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
)
