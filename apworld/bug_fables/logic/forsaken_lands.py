"""Forsaken Lands, the Barren Lands (the game's area 7, MapControl.areaid): its spots and ways between maps that aren't
doors. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import Location, Source, Transfer

LOCATIONS = (
    # Where the game teaches Bee Fly (flag 19); the later chapters' story-order stand-in, every ability taught before
    # it. The name is provisional.
    Location("Barren Lands: Fly Spot", 73, "BarrenLandsBeefly",
             Source(event=150, flag=19),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash"), reach=LATER_CHAPTERS),
)
TRANSFERS = (
    # One way: from inside, the gate only opens once it has been opened from outside (flag 384; termite_capitol's
    # HELD_UNTIL).
    Transfer("gate", "TermiteOutside", "TermiteMainPlaza", LATER_CHAPTERS, two_way=False),
)
