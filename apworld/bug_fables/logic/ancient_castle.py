"""Ancient Castle, the Sand Castle (the game's area 11, MapControl.areaid): its spots, and what the seed changes there,
mapped room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion, False_, Has

from ..custom_rules import ANY_ATTACK, CanUse, Member, one_way
from ..data_types import ALWAYS_SET, Area, DoorRule, FlagSwap, Location, Pickup, Source, StoryEvent, Transfer

_SLIDE_SOLVED = "Sand Castle Slide Puzzle Solved"
# The Slide Puzzle's upper gap: filled once the puzzle is solved, or flown over.
_SLIDE_GAP = Has(_SLIDE_SOLVED) | CanUse("Bee Fly")
_UP = CanUse("Jump") | CanUse("Bee Fly")
# The Basement's small platforms: the big crystal lit (the Beemerang Toss) and Jump, or Bee Fly.
_SMALL_PLATFORMS = (CanUse("Jump") & CanUse("Beemerang Toss")) | CanUse("Bee Fly")
# The main room's two lifts, each started for good by its switch: the lower one between the bottom and the middle
# right (flag 290), the upper one between the bottom and the top right (291), passing the middle right.
_LOWER_LIFT = "Sand Castle Lower Lift Running"
_UPPER_LIFT = "Sand Castle Upper Lift Running"
_TO_MIDDLE_RIGHT = (Has(_LOWER_LIFT) | Has(_UPPER_LIFT)) & CanUse("Jump")
_TO_TOP_RIGHT = Has(_UPPER_LIFT) & CanUse("Jump")
# The two Ancient Keys (114), each used up by one of the main room's two locks. While the castle's spots are pending
# both are the game's own pickups, neither behind a lock, so either lock opens once both are reached: the Basement's
# behind its barrier, the pressure plate room's (refined when that room is mapped).
_ANCIENT_KEYS = (CanReachRegion("SandCastleBasement (Middle)")
                 & ((CanUse("Jump") & CanUse("Beemerang Halt")) | CanUse("Bee Fly"))
                 & CanReachRegion("SandCastlePressurePuzzle"))

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
    # The Roof (SandCastleRoof; named by the user, 2026-10-09): a Frost Bomb behind the left statue, nothing needed.
    Location("Ancient Castle: Roof, Behind the Left Statue", 202, "SandCastleRoof",
             Source(flag=732, pickup=Pickup(map="SandCastleRoof", type=0, item=44)), no_jump=True, pending=True),
    # The Boss Key Room (SandCastleBossKeyRoom; named by the user, 2026-10-09): a Cold Salad behind a block, round the
    # room's edge, nothing needed; the Big Ancient Key on its right side, by the three statues whose Wardens (flying)
    # fight the party as it's taken (Event115).
    Location("Ancient Castle: Boss Key Room, Behind the Block", 203, "SandCastleBossKeyRoom",
             Source(flag=641, pickup=Pickup(map="SandCastleBossKeyRoom", type=0, item=53)), no_jump=True, pending=True),
    Location("Ancient Castle: Boss Key Room, By the Three Statues", 204, "SandCastleBossKeyRoom",
             Source(flag=294, pickup=Pickup(map="SandCastleBossKeyRoom", type=1, item=115)), rule=Member("Vi"),
             no_jump=True, pending=True, area="Right"),
)
STORY_EVENTS = (
    # The Slide Puzzle's block (`icepillar`), knocked by the horn onto the plate on its bottom (Event113 sets 284 for
    # good): it fills the upper gap and opens the upper left door.
    StoryEvent("Ancient Castle: Slide Puzzle, Block Knocked into Place", _SLIDE_SOLVED, "SandCastleSlidePuzzle",
               Source(flag=284), rule=CanUse("Horn Slash"), area="Bottom"),
    # The Main Room's (named by the user, 2026-10-09) two lift switches, any attack, each starting its lift for good:
    # the lower one's on the middle right (`switch1`, 290), the upper one's on the top right (`switch2`, 291).
    StoryEvent("Ancient Castle: Main Room, Lower Lift Switch Hit", _LOWER_LIFT, "SandCastleMainRoom", Source(flag=290),
               rule=ANY_ATTACK, area="Middle Right"),
    StoryEvent("Ancient Castle: Main Room, Upper Lift Switch Hit", _UPPER_LIFT, "SandCastleMainRoom", Source(flag=291),
               rule=ANY_ATTACK, area="Top Right"),
)

MAP_AREAS = (
    # The castle's entrance (SandCastleEntrance; the user, 2026-10-09): its left door the map's own region, its right
    # door across the middle on a bridge that shows only while the crystal is lit (a StencilSwitch, reset each visit),
    # the Beemerang Toss from either side; or flown over, Bee Fly. Both ways.
    Area("SandCastleEntrance", "Right", ("loadzone right",), CanUse("Beemerang Toss") | CanUse("Bee Fly")),
    # The Slide Puzzle (SandCastleSlidePuzzle; the user, 2026-10-09): its bottom right door the map's own region;
    # the puzzle's floor below it, a drop down a small ledge, Jump or Bee Fly back up. The upper right (its door from
    # the main room) and the upper left (the door to the pressure plate room) across a gap, filled once the puzzle is
    # solved, or Bee Fly; neither has a way down to the rest but the drop below, nor any way back up inside the room.
    Area("SandCastleSlidePuzzle", "Bottom", (), one_way(None, _UP), out=_UP),
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
    # The main room (SandCastleMainRoom; the user, 2026-10-09), in five parts: its bottom (the entrance, the Slide
    # Puzzle's lower door, the basement's, the save crystal, the healing flower, and the statue room's door behind a
    # lock) the map's own region. The middle left (the Slide Puzzle's upper door), cut off: a drop to the bottom. The
    # middle right (the rock room's door, the lower lift's switch): a drop to the bottom; back up on a running lift,
    # Jump. The top right (a roof door, the boss key room's door behind a lock, the upper lift's switch): a drop to the
    # bottom by the healing flower, or the upper lift down once it runs; back up on it with Jump. The top left (the
    # other roof door, the pressure plate room's), cut off: a drop to the bottom by the save crystal, Jump.
    Area("SandCastleMainRoom", "Middle Left", ("loadzoneslideup",), False_(), out=one_way(None, False_())),
    Area("SandCastleMainRoom", "Middle Right", ("loadzone rock",), _TO_MIDDLE_RIGHT,
         out=one_way(None, _TO_MIDDLE_RIGHT)),
    Area("SandCastleMainRoom", "Top Right", ("loadzone roof right", "loadzonebosskey"), _TO_TOP_RIGHT,
         out=one_way(None, _TO_TOP_RIGHT)),
    Area("SandCastleMainRoom", "Top Left", ("loadzone roof left", "loadzonepressure"), False_(),
         out=one_way(CanUse("Jump"), _TO_TOP_RIGHT)),
    # The Boss Key Room (SandCastleBossKeyRoom; the user, 2026-10-09): its door and the room's edge the map's own
    # region; its right side (the key) across a gap filled by a block pushed with the horn from the puzzle below, then
    # Jump, or Bee Fly; back, Jump or Bee Fly (the bounce pad alone falls short). The puzzle below is a drop, Jump or
    # Bee Fly back up, with nothing in it.
    Area("SandCastleBossKeyRoom", "Right", (), (CanUse("Jump") & CanUse("Horn Slash")) | CanUse("Bee Fly"),
         out=_UP),
)
TRANSFERS = (
    # The Slide Puzzle's upper left and upper right down to the puzzle's floor, drops: no way back up inside the
    # room.
    *(Transfer("drop", "SandCastleSlidePuzzle", "SandCastleSlidePuzzle", two_way=False, way_back=False_(),
               from_area=side, to_area="Bottom") for side in ("Upper Left", "Upper Right")),
    # The main room's top right across to its top left, Jump, a drop; back round by the bottom and the upper lift. And
    # off the upper lift, on its way down from the top right, onto the middle right; back by the bottom and the lift.
    Transfer("ledge", "SandCastleMainRoom", "SandCastleMainRoom", CanUse("Jump"), two_way=False,
             way_back=_TO_TOP_RIGHT, from_area="Top Right", to_area="Top Left"),
    Transfer("lift", "SandCastleMainRoom", "SandCastleMainRoom", Has(_UPPER_LIFT), two_way=False,
             way_back=_TO_TOP_RIGHT, from_area="Top Right", to_area="Middle Right"),
)
DOOR_RULES = (
    # The Slide Puzzle's door to the pressure plate room, shut from its side until the puzzle is solved; arriving
    # through it before then, the game pushes the party past it.
    DoorRule("SandCastleSlidePuzzle", "loadzonepressure", Has(_SLIDE_SOLVED)),
    # The Roof's (SandCastleRoof; the user, 2026-10-09) boss door, locked from its side until the Big Ancient Key (115)
    # is used; arriving from the boss room, the game pushes the party past the lock. While the castle's spots are
    # pending the key is the game's own pickup, so its rule is: the Boss Key Room's right side reached and its fight
    # won, Vi for the flying Wardens (the key's item once the spots come back, build step 67).
    DoorRule("SandCastleRoof", "loadzoneboss", CanReachRegion("SandCastleBossKeyRoom (Right)") & Member("Vi")),
    # The main room's two locks (`key1`, `key2`, Event59 key index 7, each using up an Ancient Key): the statue room's
    # door on its bottom, the boss key room's on its top right. Arriving through either, the game pushes the party
    # past the lock.
    DoorRule("SandCastleMainRoom", "loadzone statue", _ANCIENT_KEYS),
    DoorRule("SandCastleMainRoom", "loadzonebosskey", _ANCIENT_KEYS),
)

# Hidden switches hit while flag 41 (the first boss) is set: the entrance's crystal scene and the basement's platforms
# always on, as in vanilla, and neither can set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("SandCastleEntrance", "switch", 41, ALWAYS_SET),
    FlagSwap("SandCastleBasement", "platformenabler", 41, ALWAYS_SET),
)
