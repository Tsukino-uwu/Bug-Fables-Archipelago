"""Chomper Caves (the game's area 22, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion

from ..custom_rules import CanUse, one_way
from ..data_types import Area, Location, Pickup, Source

LOCATIONS = (
    # The Droplet Room (ChomperCaves2; named 2026-10-12): the Berserker medal (flag 338) in an isolated corner at the far left of its bottom,
    # dug to and from (Beetle Dig).
    Location("Chomper Caves: Droplet Room, Behind the Crack", 302, "ChomperCaves2",
             Source(flag=338, pickup=Pickup(map="ChomperCaves2", type=2, item=8)), rule=CanUse("Beetle Dig"),
             no_jump=True),
)
# ChomperCave1's bridge, knocked down from its upper area (Horn Slash), stays: the only way up from the bottom.
_BRIDGE = CanReachRegion("ChomperCave1 (Upper)") & CanUse("Horn Slash")

_ICE_CLIMB = CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump")

MAP_AREAS = (
    # ChomperCave1 (2026-10-12): its bottom (the door out to the Golden Path, the low door to ChomperCaves2, the save
    # crystal) the map's own region; the upper area (the high door to ChomperCaves2, the boss door, the Venus healer)
    # joined to it by the bridge (689), which up from the bottom needs knocked down first; down, a drop.
    Area("ChomperCave1", "Upper", ("loadzone high", "loadzoneboss"), _BRIDGE, out=one_way(None, _BRIDGE)),
    # ChomperCaves2: its bottom (the low door) the map's own region; up to the upper area (the high door) by freezing
    # droplets, knocking the ice down and climbing it (Freeze, Horn Slash, Jump); a one-way drop back.
    Area("ChomperCaves2", "Upper", ("loadzonehigh",), _ICE_CLIMB, out=one_way(None, _ICE_CLIMB)),
)
