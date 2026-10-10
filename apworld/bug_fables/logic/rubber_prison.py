"""Rubber Prison (the game's area 14, MapControl.areaid): its spots, and what the seed changes there, mapped room by
room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY, CanUse, one_way
from ..data_types import ALWAYS_SET, Area, DoorRule, EntityRef, FlagSwap, ItemEntity, Source, StoryEvent, Transfer

_UP = CanUse("Jump") | CanUse("Bee Fly")
# The Pier's lift lever hit (`switch`, flag 566): its lift runs for good.
_LIFT_RUNNING = "Pier Lift Running"

# The Pier (RubberPrisonPier; named by the user, 2026-10-10): bridges stacked above each other, each a floor dropped
# from onto any floor below, either side, with no way back up inside the room (the user). The Second Floor's Wasp
# Driller (`ShwKEY wasp`) drops a Prison Key (flag 584), which stays the game's own pickup until the Prison Key is
# never used up (apimplementation.md, Next 69); its spot, once a location: "Rubber Prison: Pier, Second Floor Fight"
# (the user's name), on the floor itself, nothing needed.
STORY_EVENTS = (
    # The lift lever on the ground floor's upper right, any attack: the lift between the dock and it runs for good.
    StoryEvent("Rubber Prison: Pier, Lift Lever Hit", _LIFT_RUNNING, "RubberPrisonPier", Source(flag=566),
               rule=ANY_ATTACK),
)
_FLOORS = ("Top Floor", "Third Floor", "Second Floor")
_GROUND = ("Ground Upper Left", None, "Ground Lower Left", "Ground Lower Right")
MAP_AREAS = (
    # The ground floor's upper part: its right (the door to the Giant's Lair Bridge, the save crystal, the lift lever)
    # the map's own region; its left (the door to the checkpoint corridor, the top of the stairs) behind two gates.
    # Every gate lever in the prison flips one flag (535), raising some gates and lowering others in every room, back
    # again on the next hit (MEASURED.md); these two are the levers' tutorial, and unlike the prison's other gates,
    # jumped or flown round while up; lowered by the lever below, whose way back up the stairs takes Jump or Bee Fly
    # too.
    Area("RubberPrisonPier", "Ground Upper Left", ("loadzoneleft",), _UP),
    # The lower part's left (the gate lever, the foot of the stairs): a drop from the upper left, back up the stairs
    # with Jump or Bee Fly.
    Area("RubberPrisonPier", "Ground Lower Left", (), one_way(None, _UP), out=_UP,
         to="RubberPrisonPier (Ground Upper Left)"),
    # The lower part's right (the dock): a drop from the upper right; back up on the lift once it runs, Jump or Bee Fly
    # onto it, or round by the platforms, the stairs and the gates.
    Area("RubberPrisonPier", "Ground Lower Right", (), one_way(None, _UP), out=_UP & Has(_LIFT_RUNNING)),
    # The floors above, each one bridge, its doors free: the top floor's two sides joined by two swinging platforms,
    # crossed with nothing; the third floor's bridge kept (SCENERY_PRESENT).
    Area("RubberPrisonPier", "Top Floor", ("loadzonesecurity", "loadzonelibrary"), False_()),
    Area("RubberPrisonPier", "Third Floor", ("loadzone3rdfloor", "loadzoneoffice"), False_()),
    Area("RubberPrisonPier", "Second Floor", ("loadzonecell2",), False_()),
    # Two doors to the Giant's Lair Bridge, each shut until another room opens it, the party pushed past it when
    # arriving (seen): the second floor's right one by the Office's crank (Event193, flag 583: the Wooden Crank and
    # Beemerang Halt, the user), the ground floor's right one from the bridge's side (flag 567). Neither room is mapped,
    # so neither counts yet.
    Area("RubberPrisonPier", "Second Floor Right Door", ("loadzoneboss",), False_(), out=one_way(None, False_()),
         to="RubberPrisonPier (Second Floor)"),
    Area("RubberPrisonPier", "Shortcut Door", ("bossshortcut",), False_(), out=one_way(None, False_())),
)

# The checkpoint corridor's gates open and shut by switches (the user, 2026-10-04: "has to be considered a oneway").
# From the yard its gates may be shut (their switch on that side comes only from flag 79), so it never leads on; that
# needs the corridor split into two areas (room-logic.md), not yet possible: Known issues.
DOOR_RULES = (
    # Across to the yard from the far side: the switches take an attack.
    DoorRule("RubberPrisonCheckpointCorridor", "loadzoneexit", ANY_ATTACK),
    # Back to the spike room: its prison door (until flag 538) opens with the Explorer Permit.
    DoorRule("RubberPrisonCheckpointCorridor", "loadzoneforward", Has("Explorer Permit")),
)

TRANSFERS = (
    Transfer("submarine", "RubberPrisonPier", "MetalLake", LATER_CHAPTERS & SUBMARINE, from_area="Ground Lower Right"),
    # The Pier's lower part, left and right, across platforms with Jump or Bee Fly, both ways.
    Transfer("platforms", "RubberPrisonPier", "RubberPrisonPier", _UP, from_area="Ground Lower Left",
             to_area="Ground Lower Right"),
    # Every drop from a floor onto any below it.
    *(Transfer("drop", "RubberPrisonPier", "RubberPrisonPier", two_way=False, way_back=False_(), from_area=floor,
               to_area=below)
      for i, floor in enumerate(_FLOORS) for below in (*_FLOORS[i + 1:], *_GROUND)),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("RubberPrisonPier", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
)
# The rock just inside the yard's left door, broken only by Horn Dash (until flag 589): coming in that way without it
# stranded the party (the user, 2026-10-04).
KEPT_OPEN = (
    EntityRef("RubberPrisonPier", "rock"),
)
SCENERY_PRESENT = (
    # The Pier's third floor bridge, which the Office's crank breaks (Event193, flag 583, opening the second floor's
    # right door): kept, so the floor's two sides stay joined (the user, 2026-10-10: "i don't want the bridge to
    # collapse").
    EntityRef("RubberPrisonPier", "Base/BrokenBridge"),
)

# The cells' hidden lift switch, hit while flag 41 (the first boss) is set: always on, as in vanilla, and it can never
# set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("RubberPrisonCells2", "FixedSwitch", 41, ALWAYS_SET),
)
