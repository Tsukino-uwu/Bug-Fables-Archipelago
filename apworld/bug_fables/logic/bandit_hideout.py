"""Bandit Hideout (the game's area 20, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import Location, Source

LOCATIONS = (
    # Where the game teaches Beetle Dig (flag 18); the later chapters' story-order stand-in, every ability taught before
    # it. The name is provisional.
    Location("Bugaria Hideout: Cell", 71, "HideoutCell",
             Source(event=109, flag=18),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield"), reach=LATER_CHAPTERS),
)
