"""Golden Settlement (the game's area 6, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import Location, Source

LOCATIONS = (
    # Where the game teaches Beemerang Halt (flag 21); the later chapters' story-order stand-in.
    Location("Golden Settlement: Festival, Wacka Worm Game", 68, "GoldenSettlement2",
             Source(event=55, flag=21), reach=LATER_CHAPTERS),
)
