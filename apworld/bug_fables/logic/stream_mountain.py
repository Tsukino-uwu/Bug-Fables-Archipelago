"""Stream Mountain (the game's area 21, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from ..custom_rules import CanUse
from ..data_types import Area

# Across the water: Icicle platforms and Jump, Bee Fly, or the tunnel under it (Beetle Dig).
_ACROSS = (CanUse("Icicle") & CanUse("Jump")) | CanUse("Bee Fly") | CanUse("Beetle Dig")

MAP_AREAS = (
    # StreamMountain1 (2026-10-12): its right door (from the desert) the map's own region; water in the middle, crossed
    # both ways by Icicle and Jump, Bee Fly, or a tunnel dug through (Beetle Dig), to the left door.
    Area("StreamMountain1", "Left", ("loadzoneleft",), _ACROSS, out=_ACROSS),
)
