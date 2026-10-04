"""Rubber Prison (the game's area 14, MapControl.areaid): its dock. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY
from ..data_types import EntityRef, ItemEntity, Transfer

TRANSFERS = (
    Transfer("submarine", "RubberPrisonPier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("RubberPrisonPier", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
)
# The rock just inside the yard's left door, broken only by Horn Dash (until flag 589): coming in that way without it
# stranded the party (the user, 2026-10-04).
KEPT_OPEN = (
    EntityRef("RubberPrisonPier", "rock"),
)
