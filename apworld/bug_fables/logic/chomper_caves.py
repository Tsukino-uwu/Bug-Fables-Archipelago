"""Chomper Caves (the game's area 22, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion

from ..custom_rules import CanUse, one_way
from ..data_types import Area

# ChomperCave1's bridge, knocked down from its upper area (Horn Slash), stays: the only way up from the bottom.
_BRIDGE = CanReachRegion("ChomperCave1 (Upper)") & CanUse("Horn Slash")

MAP_AREAS = (
    # ChomperCave1 (2026-10-12): its bottom (the door out to the Golden Path, the low door to ChomperCaves2, the save
    # crystal) the map's own region; the upper area (the high door to ChomperCaves2, the boss door, the Venus healer)
    # joined to it by the bridge (689), which up from the bottom needs knocked down first; down, a drop.
    Area("ChomperCave1", "Upper", ("loadzone high", "loadzoneboss"), _BRIDGE, out=one_way(None, _BRIDGE)),
)
