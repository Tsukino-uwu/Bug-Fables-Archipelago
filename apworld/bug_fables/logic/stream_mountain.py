"""Stream Mountain (the game's area 21, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import True_

from ..custom_rules import ANY_ATTACK, CanUse, one_way
from ..data_types import ALWAYS_SET, Area, FlagSwap, Location, Pickup, Source

# Across the water: Icicle platforms and Jump, Bee Fly, or the tunnel under it (Beetle Dig).
_ACROSS = (CanUse("Icicle") & CanUse("Jump")) | CanUse("Bee Fly") | CanUse("Beetle Dig")
# StreamMountain2's crystal switch, hit from the upper right, raises or lowers the water in the bottom; it stays.
_WATER = CanUse("Beemerang Toss") | (CanUse("Bee Fly") & ANY_ATTACK)

LOCATIONS = (
    # The Climb (StreamMountain3; named 2026-10-12): the Antlion Jaws medal (flag 418) at the very top, from the upper
    # part by geysers frozen and jumped up (Freeze and Jump).
    Location("Stream Mountain: Climb, Above the Geysers", 299, "StreamMountain3",
             Source(flag=418, pickup=Pickup(map="StreamMountain3", type=2, item=63)),
             rule=CanUse("Freeze") & CanUse("Jump"), area="Upper", no_jump=True),
    # StreamMountain4's Clear Water (item 12, flag 493), lying in its middle once there.
    Location("Stream Mountain: Bridge Puzzle, Among the Logs", 300, "StreamMountain4",
             Source(flag=493, pickup=Pickup(map="StreamMountain4", type=0, item=12)), area="Middle", no_jump=True),
)
MAP_AREAS = (
    # StreamMountain1 (2026-10-12): its right door (from the desert) the map's own region; water in the middle, crossed
    # both ways by Icicle and Jump, Bee Fly, or a tunnel dug through (Beetle Dig), to the left door.
    Area("StreamMountain1", "Left", ("loadzoneleft",), _ACROSS, out=_ACROSS),
    # StreamMountain2 (2026-10-12): its upper right (the right door, the crystal switch) the map's own region. The
    # bottom (its door, two bridges, a green bounce pad up to the upper right), dropped into with the water lowered.
    # The upper left (its door) by Bee Fly, or over the two bridges knocked down at the bottom (Horn Slash; they stay)
    # with the water raised (no Icicle on this water); back by a drop, then the pad, or into the water, which the game
    # answers by putting the party at the upper right. Arriving by the bottom door with the water raised does the same.
    # The Climb: its right door at the bottom the map's own region; up its bounce pads to the upper part (the exit to
    # the Desert Oasis) by Jump or Bee Fly, a free drop back.
    Area("StreamMountain3", "Upper", ("loadzone exit",), CanUse("Jump") | CanUse("Bee Fly"),
         out=one_way(None, CanUse("Jump") | CanUse("Bee Fly"))),
    # The Bridge Puzzle (StreamMountain4; named 2026-10-12): its right door and crystal switch (any attack) the map's own region; the middle (the
    # Clear Water, a bounce pad up) dropped into with the water lowered; the left (the boss door) over the bridge,
    # knocked down from its ledge (Jump or Bee Fly, Horn Slash), then across the floating logs with the water held up by
    # Beemerang Halt, released to jump off at the far left; a one-way drop back into the middle.
    Area("StreamMountain4", "Middle", (), ANY_ATTACK, out=one_way(None, ANY_ATTACK)),
    Area("StreamMountain4", "Left", ("loadzone boss",),
         CanUse("Horn Slash") & CanUse("Jump") & CanUse("Beemerang Halt"),
         out=one_way(None, CanUse("Horn Slash") & CanUse("Jump") & CanUse("Beemerang Halt")),
         to="StreamMountain4 (Middle)"),
    Area("StreamMountain2", "Bottom", ("loadzonebottom",), _WATER, out=one_way(None, _WATER)),
    Area("StreamMountain2", "Upper Left", ("loadzoneleft",), CanUse("Bee Fly") | (CanUse("Horn Slash") & _WATER),
         out=True_()),
)
# StreamMountain4's logs (and its fallen bridge) float on its water only from chapter 1's end (flag 41, an artifact
# flag a seed never sets): without them the room can't be crossed. Repointed to a flag always set (2026-10-12).
FLOAT_FLAGS = (
    FlagSwap("StreamMountain4", "Base/WaterTop", 41, ALWAYS_SET),
)
