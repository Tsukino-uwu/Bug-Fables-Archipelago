"""Wild Swamplands (the game's area 9, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import Location, Source

LOCATIONS = (
    # Where the game teaches the Horn Dash (flag 39); the later chapters' story-order stand-in, every ability taught
    # before it. The name is provisional.
    Location("Swamplands: Bridge", 72, "SwamplandsBridge",
             Source(event=131, flag=39),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig"),
             reach=LATER_CHAPTERS),
)
