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
# The Piano Puzzle's plates, held down by enemies frozen and knocked onto them (Freeze, Horn Slash), raise its piano
# keys (Jump between them); without the Extra Freeze medal the ice melts too soon (seen).
_PLATES = CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump") & Has("Extra Freeze")
_PIANO = "UpperSnekPressurePlateRoom"

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
    # The Gear Machine (UpperSnekBeforeBoss; named 2026-10-12): a respawning Mushroom (regional flag 0) on top of the
    # machine, by Jump or Bee Fly.
    Location("Upper Snakemouth: Gear Machine, On Top of the Machine", 307, "UpperSnekBeforeBoss",
             Source(regional=0, pickup=Pickup(map="UpperSnekBeforeBoss", type=0, item=13)), rule=_UP),
    # The Piano Puzzle (UpperSnekPressurePlateRoom; named 2026-10-12): one free floor, its one door at the bottom. The
    # Extra Freeze medal (flag 522) in a broken test tube, free. The Small Gear (521) behind the right door, which the
    # middle and right plates open (it stays open while the party is inside). Crystal Berry #42 up on the left by the
    # wind pipe, the way up opened by the upper right and left plates.
    Location("Upper Snakemouth: Piano Puzzle, Broken Test Tube", 308, _PIANO,
             Source(flag=522, pickup=Pickup(map=_PIANO, type=2, item=59)), no_jump=True),
    Location("Upper Snakemouth: Piano Puzzle, Pressure Plate Door", 309, _PIANO,
             Source(flag=521, pickup=Pickup(map=_PIANO, type=1, item=157)), rule=_PLATES),
    Location("Upper Snakemouth: Piano Puzzle, By the Wind Pipe", 310, _PIANO,
             Source(berry=42, pickup=Pickup(map=_PIANO, type=3, item=0)), rule=_PLATES, category="crystal_berry"),
    # The Icicle Climb (UpperSnekPlatformRoom; named 2026-10-12): its bottom floor (the door, two switches, a bounce pad
    # up) free; moving platforms climbed to the top, their switches hit from above and below by the Icicle, Jump or Bee
    # Fly between them, to a Lab Card (flag 523) in a broken test tube.
    Location("Upper Snakemouth: Icicle Climb, Broken Test Tube", 311, "UpperSnekPlatformRoom",
             Source(flag=523, pickup=Pickup(map="UpperSnekPlatformRoom", type=1, item=160)),
             rule=CanUse("Icicle") & _UP),
    # The River Puzzle (UpperSnekRiverPuzzle; named 2026-10-12): its door at the bottom free; a Zombeetle carrying a
    # Lab Card (`ShwKEYZombeetle`, the card's flag 526) behind a barrier, reached by Jump, the Icicle and the Beemerang
    # Halt; back out with Beetle Dig (and Jump or Bee Fly).
    Location("Upper Snakemouth: River Puzzle, Fight Behind the Barrier", 312, "UpperSnekRiverPuzzle",
             Source(flag=526, pickup=Pickup(map="UpperSnekRiverPuzzle", type=1, item=160)),
             rule=CanUse("Jump") & CanUse("Icicle") & CanUse("Beemerang Halt") & CanUse("Beetle Dig")),
    # The Geyser Room (UpperSnekGeizerRoom; named 2026-10-12): its door on the right free; the Big Gear (527) at the
    # far left below the pump. Bee Fly reaches it alone; otherwise the Icicle and Jump to the wooden lever (a basic
    # attack), which starts the geysers, frozen and jumped across (Freeze). Back by a shortcut and a drop by the lever.
    Location("Upper Snakemouth: Geyser Room, Below the Pump", 313, "UpperSnekGeizerRoom",
             Source(flag=527, pickup=Pickup(map="UpperSnekGeizerRoom", type=1, item=159)),
             rule=CanUse("Bee Fly") | (_ACROSS & ANY_ATTACK & CanUse("Freeze"))),
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
    # The Gear Machine: its bottom (the right door, the Venus bud, the machine) the map's own region. The three gears
    # in their slots (Event59 indices 13-15; 529-531) open the round door (528) to the boss room; arriving by it pushes
    # the party past, so without them that door is a one-way in.
    Area("UpperSnekBeforeBoss", "Left Door", ("loadzoneboss",),
         Has("Small Gear") & Has("Medium Gear") & Has("Big Gear"), out=True_()),
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
