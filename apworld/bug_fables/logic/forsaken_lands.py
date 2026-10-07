"""Forsaken Lands, the Barren Lands (the game's area 7, MapControl.areaid): its spots and ways between maps that aren't
doors. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import EntityRef, Location, Source, Transfer

LOCATIONS = (
    # Where the game teaches Bee Fly (flag 19): escorting the queen to the termites, she orders Vi to fly the broken
    # bridge. The later chapters' story-order stand-in, every ability taught before it.
    Location("Forsaken Lands: Broken Bridge", 73, "BarrenLandsBeefly",
             Source(event=150, flag=19),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash"), reach=LATER_CHAPTERS),
)
TRANSFERS = (
    # Both ways: from inside the mod opens it as if it had been opened from outside (termite_gate_from_inside).
    Transfer("gate", "TermiteOutside", "TermiteMainPlaza", LATER_CHAPTERS),
)
# Patton's lab, opened by the escort to the termites (flag 376): open from the start (the user, 2026-10-07), its door
# entity there and its slab gone, as the game has them from 376.
KEPT_PRESENT = (
    EntityRef("BarrenLandsEntrance", "doorpatton"),
)
SCENERY_HIDDEN = (
    EntityRef("BarrenLandsEntrance", "PattonsHouse/PopCan (1)"),
)
