"""Golden Settlement (the game's area 6, MapControl.areaid): its spots, and what the seed changes there. Its rooms aren't
mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import EntityRef, Location, Source

LOCATIONS = (
    # Where the game teaches Beemerang Halt (flag 21); the later chapters' story-order stand-in.
    Location("Golden Settlement: Festival, Wacka Worm Game", 68, "GoldenSettlement2",
             Source(event=55, flag=21), reach=LATER_CHAPTERS),
)
# The gate to the desert (the user, 2026-10-04): open from the start, and the invisible wall behind it, which stands
# until the desert side has been reached (flag 170), gone.
SCENERY_HIDDEN = (
    EntityRef("GoldenSettlementEntrance", "Base/Cube"),
    EntityRef("GoldenSettlementEntrance", "Base/DesertGate/WoodenGate2"),
    EntityRef("GoldenSettlementEntrance", "Base/DesertGate/WoodenGate2 (1)"),
)
SCENERY_PRESENT = (
    EntityRef("GoldenSettlementEntrance", "Base/WoodenGate2 (2)"),
    EntityRef("GoldenSettlementEntrance", "Base/WoodenGate2 (3)"),
)
