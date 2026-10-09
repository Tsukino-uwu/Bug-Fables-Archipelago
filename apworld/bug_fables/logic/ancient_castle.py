"""Ancient Castle, the Sand Castle (the game's area 11, MapControl.areaid): its spots, and what the seed changes there,
mapped room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import CanUse, one_way
from ..data_types import ALWAYS_SET, Area, DoorRule, FlagSwap, Location, Pickup, Source, StoryEvent, Transfer

_SLIDE_SOLVED = "Sand Castle Slide Puzzle Solved"
# The Slide Puzzle's upper gap: filled once the puzzle is solved, or flown over.
_SLIDE_GAP = Has(_SLIDE_SOLVED) | CanUse("Bee Fly")
_UP = CanUse("Jump") | CanUse("Bee Fly")
# The Basement's small platforms: the big crystal lit (the Beemerang Toss) and Jump, or Bee Fly.
_SMALL_PLATFORMS = (CanUse("Jump") & CanUse("Beemerang Toss")) | CanUse("Bee Fly")

LOCATIONS = (
    # Every spot in the castle is pending (the user, 2026-10-09; build step 67): its door needs the Sand Castle Key,
    # whose chain (chapters 2 to 4, the bandit hideout, the roach village's hawk) the stand-in doesn't hold yet.
    # The Slide Puzzle (SandCastleSlidePuzzle; named by the user, 2026-10-09): the medal Frostbite in a small space
    # behind its bottom's top left corner, below the upper left door, burrowed into and out of, Beetle Dig.
    Location("Ancient Castle: Slide Puzzle, Behind the Cracked Wall", 198, "SandCastleSlidePuzzle",
             Source(flag=285, pickup=Pickup(map="SandCastleSlidePuzzle", type=2, item=46)), rule=CanUse("Beetle Dig"),
             no_jump=True, pending=True, area="Bottom"),
    # The Basement (SandCastleBasement; named by the user, 2026-10-09), from its middle: crystal berry #23 on a
    # platform off the moving ones and a Shaved Ice on a tiny platform, each reached on the small platforms (the big
    # crystal lit by the Beemerang Toss, then Jump, their switches hit by any attack) or by Bee Fly; the Ancient Key
    # behind a barrier on the north side, lowered by its crank (Beemerang Halt) and reached with Jump, or flown in and
    # out of, Bee Fly.
    Location("Ancient Castle: Basement, Switch Puzzle", 199, "SandCastleBasement",
             Source(berry=23, pickup=Pickup(map="SandCastleBasement", type=3, item=0)), rule=_SMALL_PLATFORMS,
             category="crystal_berry", no_jump=True, pending=True, area="Middle"),
    Location("Ancient Castle: Basement, Tiny Platform", 200, "SandCastleBasement",
             Source(flag=731, pickup=Pickup(map="SandCastleBasement", type=0, item=47)), rule=_SMALL_PLATFORMS,
             no_jump=True, pending=True, area="Middle"),
    Location("Ancient Castle: Basement, Behind the Barrier", 201, "SandCastleBasement",
             Source(flag=288, pickup=Pickup(map="SandCastleBasement", type=1, item=114)),
             rule=(CanUse("Jump") & CanUse("Beemerang Halt")) | CanUse("Bee Fly"), no_jump=True, pending=True,
             area="Middle"),
)
STORY_EVENTS = (
    # The Slide Puzzle's block (`icepillar`), knocked by the horn onto the plate on its bottom (Event113 sets 284 for
    # good): it fills the upper gap and opens the upper left door.
    StoryEvent("Ancient Castle: Slide Puzzle, Block Knocked into Place", _SLIDE_SOLVED, "SandCastleSlidePuzzle",
               Source(flag=284), rule=CanUse("Horn Slash"), area="Bottom"),
)

MAP_AREAS = (
    # The castle's entrance (SandCastleEntrance; the user, 2026-10-09): its left door the map's own region, its right
    # door across the middle on a bridge that shows only while the crystal is lit (a StencilSwitch, reset each visit),
    # the Beemerang Toss from either side; or flown over, Bee Fly. Both ways.
    Area("SandCastleEntrance", "Right", ("loadzone right",), CanUse("Beemerang Toss") | CanUse("Bee Fly")),
    # The Slide Puzzle (SandCastleSlidePuzzle; the user, 2026-10-09): its bottom right door the map's own region;
    # the puzzle's floor below it, a drop down a small ledge, Jump back up. The upper right (its door from the main
    # room) and the upper left (the door to the pressure plate room) across a gap, filled once the puzzle is solved,
    # or Bee Fly; neither has a way down to the rest but the drop below, nor any way back up inside the room.
    Area("SandCastleSlidePuzzle", "Bottom", (), one_way(None, CanUse("Jump")), out=CanUse("Jump")),
    Area("SandCastleSlidePuzzle", "Upper Right", ("loadzoneleftup",), False_()),
    Area("SandCastleSlidePuzzle", "Upper Left", ("loadzonepressure",), _SLIDE_GAP,
         to="SandCastleSlidePuzzle (Upper Right)"),
    # The statue room (SandCastleStatueRoom; the user, 2026-10-09): its left door the map's own region, its right door
    # past a block in the middle, gone over on platforms: from the left the crystals' puzzle and the way across take
    # Icicle (its ice also lights the crystals) and Jump, or Bee Fly alone; from the right Jump or Bee Fly.
    Area("SandCastleStatueRoom", "Right", ("loadzonerock",), (CanUse("Jump") & CanUse("Icicle")) | CanUse("Bee Fly"),
         out=_UP),
    # The Basement (SandCastleBasement; the user, 2026-10-09): its door on an isolated ledge, the map's own region; the
    # middle (a safe platform by the big crystal, the moving platforms around it) across the gap with Jump or Bee Fly,
    # both ways.
    Area("SandCastleBasement", "Middle", (), _UP),
)
TRANSFERS = (
    # The Slide Puzzle's upper left and upper right down to the puzzle's floor, drops: no way back up inside the
    # room.
    *(Transfer("drop", "SandCastleSlidePuzzle", "SandCastleSlidePuzzle", two_way=False, way_back=False_(),
               from_area=side, to_area="Bottom") for side in ("Upper Left", "Upper Right")),
)
DOOR_RULES = (
    # The Slide Puzzle's door to the pressure plate room, shut from its side until the puzzle is solved; arriving
    # through it before then, the game pushes the party past it.
    DoorRule("SandCastleSlidePuzzle", "loadzonepressure", Has(_SLIDE_SOLVED)),
)

# Hidden switches hit while flag 41 (the first boss) is set: the entrance's crystal scene and the basement's platforms
# always on, as in vanilla, and neither can set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("SandCastleEntrance", "switch", 41, ALWAYS_SET),
    FlagSwap("SandCastleBasement", "platformenabler", 41, ALWAYS_SET),
)
