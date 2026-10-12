"""Stream Mountain (the game's area 21, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import True_

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import Area

# Across the water: Icicle platforms and Jump, Bee Fly, or the tunnel under it (Beetle Dig).
_ACROSS = (CanUse("Icicle") & CanUse("Jump")) | CanUse("Bee Fly") | CanUse("Beetle Dig")
# StreamMountain2's crystal switch, hit from the upper right, raises or lowers the water in the bottom; it stays.
_WATER = CanUse("Beemerang Toss") | (CanUse("Bee Fly") & ANY_ATTACK)

MAP_AREAS = (
    # StreamMountain1 (2026-10-12): its right door (from the desert) the map's own region; water in the middle, crossed
    # both ways by Icicle and Jump, Bee Fly, or a tunnel dug through (Beetle Dig), to the left door.
    Area("StreamMountain1", "Left", ("loadzoneleft",), _ACROSS, out=_ACROSS),
    # StreamMountain2 (2026-10-12): its upper right (the right door, the crystal switch) the map's own region. The
    # bottom (its door, two bridges, a green bounce pad up to the upper right), dropped into with the water lowered.
    # The upper left (its door) by Bee Fly, or over the two bridges knocked down at the bottom (Horn Slash; they stay)
    # with the water raised (no Icicle on this water); back by a drop, then the pad, or into the water, which the game
    # answers by putting the party at the upper right. Arriving by the bottom door with the water raised does the same.
    Area("StreamMountain2", "Bottom", ("loadzonebottom",), _WATER, out=True_()),
    Area("StreamMountain2", "Upper Left", ("loadzoneleft",), CanUse("Bee Fly") | (CanUse("Horn Slash") & _WATER),
         out=True_()),
)
