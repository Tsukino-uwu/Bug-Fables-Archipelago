"""Upper Snakemouth (the game's area 24, MapControl.areaid): its spots, and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import Has, True_

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import ALWAYS_SET, Area, FlagSwap, Location, Pickup, Source, Transfer

_ACROSS = CanUse("Icicle") & CanUse("Jump")
# Icicle Pond's switch, hit with the Icicle, or flown up to with Bee Fly and an attack.
_SWITCH = CanUse("Icicle") | (CanUse("Bee Fly") & ANY_ATTACK)
# The Flooded Hall's platforms, water between them: Icicle and Jump, or Bee Fly.
_WATER = (CanUse("Icicle") & CanUse("Jump")) | CanUse("Bee Fly")
_UP = CanUse("Jump") | CanUse("Bee Fly")
_MIDDLE = "UpperSnekMiddleRoom"

LOCATIONS = (
    # Where the game teaches the Icicle (Event180, until flag 171): its scene and fight on entering by either door. From
    # the left door it also moves the party to the right side, once; not a way across in the logic.
    Location("Upper Snakemouth: Icicle Pond, Fight", 74, "UpperSnekTransition", Source(event=180, flag=171),
             no_jump=True),
    # The Flooded Hall's Sleep Bomb (flag 740), on an island behind the waterfall: from the middle by Icicle and Jump.
    # Bee Fly from the upper top right also lands there, a dead end with no way off: left out.
    Location("Upper Snakemouth: Flooded Hall, Behind the Waterfall", 305, _MIDDLE,
             Source(flag=740, pickup=Pickup(map=_MIDDLE, type=0, item=46)), rule=_ACROSS),
    # The Pipe Puzzle (UpperSnekSwitchPuzzle; named 2026-10-12): one free room, its one door on the right. Switches 2,
    # 4, 5 and 7 lit, then the wood lever (a basic attack each) open the round door on the left (flag 519) to a gear
    # (key item 158, flag 520).
    Location("Upper Snakemouth: Pipe Puzzle, Puzzle Reward", 306, "UpperSnekSwitchPuzzle",
             Source(flag=520, pickup=Pickup(map="UpperSnekSwitchPuzzle", type=1, item=158)), rule=ANY_ATTACK,
             no_jump=True),
)
MAP_AREAS = (
    # Icicle Pond (UpperSnekTransition; named 2026-10-12): its right door (from the entrance) the map's own region;
    # water in the middle, crossed both ways by Icicle and Jump, to an empty platform on the left; from there a dig
    # barrier (Beetle Dig, both ways) to the left door and its switch. The switch (flag 518, for good) opens the left
    # door; arriving by it pushes the party past, so without the switch that door is a one-way in.
    Area("UpperSnekTransition", "Left Platform", (), _ACROSS),
    Area("UpperSnekTransition", "Left", (), CanUse("Beetle Dig"), to="UpperSnekTransition (Left Platform)"),
    Area("UpperSnekTransition", "Left Door", ("loadzoneforward",), _SWITCH, out=True_(),
         to="UpperSnekTransition (Left)"),
    # The Flooded Hall (UpperSnekMiddleRoom; named 2026-10-12): the middle (the save crystal, the bottom door) the map's
    # own region; the right, bottom left, left and lower top left doors each on a platform across the water (_WATER,
    # both ways). From the lower top left, a tilted bridge to the upper top left (Jump or Bee Fly up, a free drop down)
    # and stairs of platforms to the upper top right (Jump or Bee Fly, both ways). Each upper door behind a Lab Card
    # lock (Event59 index 12; 525, 524); arriving by one pushes the party past, so without the card it is a one-way in.
    *(Area(_MIDDLE, name, (door,), _WATER) for name, door in (
        ("Right", "loadzoneentrance"), ("Bottom Left", "loadzoneriverorom"), ("Left", "loadzoneboss"),
        ("Lower Top Left", "loadzone switch"))),
    Area(_MIDDLE, "Upper Top Left", (), _UP, out=True_(), to=f"{_MIDDLE} (Lower Top Left)"),
    Area(_MIDDLE, "Upper Top Left Door", ("loadzonegeizer",), Has("Lab Card"), out=True_(),
         to=f"{_MIDDLE} (Upper Top Left)"),
    Area(_MIDDLE, "Upper Top Right", (), _UP, to=f"{_MIDDLE} (Lower Top Left)"),
    Area(_MIDDLE, "Upper Top Right Door", ("loadzonepressure",), Has("Lab Card"), out=True_(),
         to=f"{_MIDDLE} (Upper Top Right)"),
)
TRANSFERS = (
    # The Flooded Hall's hanging bridge, a drop from the upper top right to the middle; back over the water, up the
    # stairs.
    Transfer("hanging bridge", _MIDDLE, _MIDDLE, True_(), two_way=False, way_back=_WATER & _UP,
             from_area="Upper Top Right"),
)

# The lights-out switches that start lit once flag 41 (the first boss) is set, and the plate room's hidden switch, hit
# while it is: as in vanilla, and none can set 41 (build step 60).
ACTIVATION_FLAGS = (
    *(FlagSwap("UpperSnekSwitchPuzzle", f"TOGSwitch{n}", 41, ALWAYS_SET) for n in (1, 4, 8)),
    FlagSwap("UpperSnekPressurePlateRoom", "testswitch", 41, ALWAYS_SET),
)
