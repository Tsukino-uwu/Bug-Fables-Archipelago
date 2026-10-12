"""Chomper Caves (the game's area 22, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion

from ..custom_rules import CanUse
from ..data_types import Area

MAP_AREAS = (
    # ChomperCave1 (2026-10-12): its bottom (the door out to the Golden Path, the low door to ChomperCaves2, the save
    # crystal) the map's own region; the upper area (the high door to ChomperCaves2, the boss door, the Venus healer)
    # joined to it by a bridge knocked down from above (Horn Slash; it stays, 689): so up from the bottom only once the
    # upper area was reached by its own doors.
    Area("ChomperCave1", "Upper", ("loadzone high", "loadzoneboss"),
         CanReachRegion("ChomperCave1 (Upper)") & CanUse("Horn Slash"), out=CanUse("Horn Slash")),
)
