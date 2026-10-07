"""Golden Settlement (the game's area 6, MapControl.areaid): its spots, and what the seed changes there. Its rooms
aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import EntityRef, Location, Source

LOCATIONS = (
    # Where the game teaches Beemerang Halt (flag 21), after the mayor's Wacka Worm game, played by Vi with the
    # Beemerang (the mod refuses it otherwise); the later chapters' story-order stand-in.
    Location("Golden Settlement: Festival, Wacka Worm Game", 68, "GoldenSettlement2",
             Source(event=55, flag=21), rule=CanUse("Beemerang Toss"), reach=LATER_CHAPTERS),
)
# The invisible wall behind the gate to the desert, which stands until the desert side has been reached (flag 170),
# gone. The gate itself stays the game's: shut until its lever is hit from the desert side (the user, 2026-10-07).
SCENERY_HIDDEN = (
    EntityRef("GoldenSettlementEntrance", "Base/Cube"),
)
