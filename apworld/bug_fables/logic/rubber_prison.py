"""Rubber Prison (the game's area 14, MapControl.areaid): its spots, and what the seed changes there, mapped room by
room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion, False_, Has

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY, WOODEN_CRANK, CanUse, Member, one_way
from ..data_types import (ALWAYS_SET, Area, EntityRef, FlagSwap, ItemEntity, Location, Pickup, Source, StoryEvent,
                          Transfer)

# The one Prison Key, never used up at the prison's four locks in a seed (build step 82).
PRISON_KEY = Has("Prison Key")
_UP = CanUse("Jump") | CanUse("Bee Fly")
# The Pier's lift lever hit (`switch`, flag 566): its lift runs for good.
_LIFT_RUNNING = "Pier Lift Running"
# The Cell Block Crossroads' right wood lever hit (Event186, flag 540): its bottom right gate down for good, and its
# rock kept in the knocked spot it climbs from.
_CROSSROADS_RIGHT_GATE = "Crossroads Right Gate Open"
# The Office's crank turned (Event193's first branch, flag 583): the Pier's second floor right door open for good.
_OFFICE_CRANK = "Office Crank Turned"
# The Cell Block Crossroads' barrier lever hit from inside the top door's area (flag 541): the barrier down for good.
_CROSSROADS_BARRIER = "Crossroads Barrier Lowered"
# Up from the Cell Block Crossroads' bottom: the bottom lever (Beetle Dig to its cell, an attack) opens the left cell,
# whose rock the horn knocks out as a step, then Jump or Bee Fly; or, the right lever once hit, the rock kept there.
_CROSSROADS_UP = (CanUse("Beetle Dig") & CanUse("Horn Slash") & ANY_ATTACK | Has(_CROSSROADS_RIGHT_GATE)) & _UP
# Electric Cells' electric floor, between its bottom doors and the bottom row: the Shield or Bee Fly.
_ACROSS_THE_FLOOR = CanUse("Shield") | CanUse("Bee Fly")

# The Pier (RubberPrisonPier; named by the user, 2026-10-10): bridges stacked above each other, each a floor dropped
# from onto any floor below, either side, with no way back up inside the room (the user).
LOCATIONS = (
    # The Pier's Second Floor Wasp Driller (`ShwKEY wasp`) drops a Prison Key on defeat, its pickup flag the enemy's
    # limit, 584 (EntityControl's Death). The fight: the Wasp Driller and a Wasp Bomber, which Kabbu flips.
    Location("Rubber Prison: Pier, Second Floor Fight", 240, "RubberPrisonPier",
             Source(flag=584, pickup=Pickup(map="RubberPrisonPier", type=1, item=161)), rule=Member("Kabbu"),
             no_jump=True, area="Second Floor"),
    # The Spike Room's Prison Key, in the cage of three gates its lever drops: Bee Fly and an attack (the user).
    Location("Rubber Prison: Spike Room, In the Cage", 241, "RubberPrisonSpikeRoom",
             Source(flag=536, pickup=Pickup(map="RubberPrisonSpikeRoom", type=1, item=161)),
             rule=CanUse("Bee Fly") & ANY_ATTACK, no_jump=True),
    # Electric Cells' middle right cell, the Prison Key's, opened by the middle left cell's wood lever (flag 400, reset
    # on every load), that cell behind a 535 gate. Every way into the middle row comes from the top, dropped after its
    # 535 lever, or has the Shield to ride up to it: an attack for each lever.
    Location("Rubber Prison: Electric Cells, Middle Right Cell", 242, "RubberPrisonCells2",
             Source(flag=545, pickup=Pickup(map="RubberPrisonCells2", type=1, item=161)), rule=ANY_ATTACK,
             no_jump=True, area="Middle"),
    # Its bottom right cell's Poison Cake, behind a 535 gate: the bottom lever (Beetle Dig to its cell, an attack), or
    # the top lever hit before dropping down (the Shield to ride up, or the drop counted with Points of No Return).
    Location("Rubber Prison: Electric Cells, Bottom Right Cell", 243, "RubberPrisonCells2",
             Source(flag=631, pickup=Pickup(map="RubberPrisonCells2", type=0, item=74)),
             rule=ANY_ATTACK & (CanUse("Beetle Dig")
                                | CanReachRegion("RubberPrisonCells2 (Upper)") & one_way(None, CanUse("Shield"))),
             no_jump=True),
    # The Gym's Prison Key, on its top left, up the room's crank puzzle: Beemerang Halt, no Jump needed (the user).
    Location("Rubber Prison: Gym, Crank Puzzle", 244, "RubberPrisonGym",
             Source(flag=551, pickup=Pickup(map="RubberPrisonGym", type=1, item=161)),
             rule=CanUse("Beemerang Halt"), no_jump=True, area="Top Left"),
)
STORY_EVENTS = (
    # The lift lever on the ground floor's upper right, any attack: the lift between the dock and it runs for good.
    StoryEvent("Rubber Prison: Pier, Lift Lever Hit", _LIFT_RUNNING, "RubberPrisonPier", Source(flag=566),
               rule=ANY_ATTACK, no_jump=True),
    # The Cell Block Crossroads' wood levers, up on the left and right: the left one first, then the right, each Bee Fly
    # and an attack (the user).
    StoryEvent("Rubber Prison: Cell Block Crossroads, Right Wood Lever Hit", _CROSSROADS_RIGHT_GATE,
               "RubberPrisonCells1", Source(flag=540), rule=CanUse("Bee Fly") & ANY_ATTACK, no_jump=True,
               area="Upper"),
    StoryEvent("Rubber Prison: Cell Block Crossroads, Barrier Lever Hit", _CROSSROADS_BARRIER, "RubberPrisonCells1",
               Source(flag=541), rule=ANY_ATTACK, no_jump=True, area="Top Door"),
    # The Office (RubberPrisonOffice; named by the user, 2026-10-10, the game's "a normal office"): one area. Its crank
    # slot takes the Wooden Crank, then turned with Beemerang Halt (the user). Waiting for the Wooden Crank (Next 62):
    # "Empty Bookshelf" (the green History Book on top of it, 563: Freeze, the horn, Jump and Bee Fly).
    StoryEvent("Rubber Prison: Office, Crank Turned", _OFFICE_CRANK, "RubberPrisonOffice", Source(flag=583),
               rule=WOODEN_CRANK & CanUse("Beemerang Halt"), no_jump=True),
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
    # Beemerang Halt, the user), the ground floor's right one from the bridge's side (flag 567, its room not mapped, so
    # not counted yet).
    Area("RubberPrisonPier", "Second Floor Right Door", ("loadzoneboss",), Has(_OFFICE_CRANK),
         out=one_way(None, Has(_OFFICE_CRANK)),
         to="RubberPrisonPier (Second Floor)"),
    Area("RubberPrisonPier", "Shortcut Door", ("bossshortcut",), False_(), out=one_way(None, False_())),
    # The Checkpoint Corridor (RubberPrisonCheckpointCorridor; named by the user, 2026-10-10): two gates that follow flag
    # 535 opposite ways, one always up, with a lever on each side of each (the right one kept present), so the left
    # (the map's own region) and the right (the door to the Pier) are an attack apart, both ways (the user).
    Area("RubberPrisonCheckpointCorridor", "Right", ("loadzoneexit",), ANY_ATTACK),
    # The top left door to the spike room, behind a locked door (the Explorer Permit, open for good from 538): arriving
    # from the spike room, the game pushes the party past it (seen), a one-way without the permit.
    Area("RubberPrisonCheckpointCorridor", "Top Left Door", ("loadzoneforward",), Has("Explorer Permit"),
         out=one_way(None, Has("Explorer Permit"))),
    # The Spike Room (RubberPrisonSpikeRoom; named by the user, 2026-10-10): its bottom (the door to the corridor, the
    # cage) the map's own region; the top across the spiked floor, Bee Fly or the Shield, both ways; the top door behind
    # a Prison Key lock (until 537), the party pushed past it on arrival (seen).
    Area("RubberPrisonSpikeRoom", "Top", (), CanUse("Bee Fly") | CanUse("Shield")),
    Area("RubberPrisonSpikeRoom", "Top Door", ("loadzonenorth",), PRISON_KEY, out=one_way(None, PRISON_KEY),
         to="RubberPrisonSpikeRoom (Top)"),
    # The Cell Block Crossroads (RubberPrisonCells1; named by the user, 2026-10-10): its bottom (the door to the spike
    # room, the save crystal) the map's own region. The upper level (its two wood levers) a drop down, its way back the
    # way up. The top door in its own barrier, whose lever inside lowers it for good (541). The bottom left door behind
    # a Prison Key lock (until 539), the bottom right one behind the gate the right wood lever opens (540): the party
    # pushed past either on arrival (seen).
    Area("RubberPrisonCells1", "Upper", (), _CROSSROADS_UP, out=one_way(None, _CROSSROADS_UP)),
    Area("RubberPrisonCells1", "Top Door", ("loadzone3rdfloor",), Has(_CROSSROADS_BARRIER), out=ANY_ATTACK,
         to="RubberPrisonCells1 (Upper)"),
    Area("RubberPrisonCells1", "Left Door", ("loadzoneleft",), PRISON_KEY, out=one_way(None, PRISON_KEY)),
    Area("RubberPrisonCells1", "Right Door", ("loadzoneright",), Has(_CROSSROADS_RIGHT_GATE),
         out=one_way(None, Has(_CROSSROADS_RIGHT_GATE))),
    # Electric Cells (RubberPrisonCells2; named by the user, 2026-10-10): three rows of cells. The bottom row (the bottom
    # lever's cell, the Poison Cake's) the map's own region, each bottom door across the electric floor; the electric
    # moving platforms up to the middle row and the top, the Shield only; down from either, a drop. The top door
    # behind a Prison Key lock (until 542), the party pushed past it on arrival (seen).
    Area("RubberPrisonCells2", "Left Door", ("loadzoneleft",), _ACROSS_THE_FLOOR),
    Area("RubberPrisonCells2", "Right Door", ("loadzoneright",), _ACROSS_THE_FLOOR),
    Area("RubberPrisonCells2", "Middle", (), CanUse("Shield"), out=one_way(None, CanUse("Shield"))),
    Area("RubberPrisonCells2", "Upper", (), CanUse("Shield"), out=one_way(None, CanUse("Shield"))),
    Area("RubberPrisonCells2", "Top Door", ("loadzone3rdfloor",), PRISON_KEY, out=one_way(None, PRISON_KEY),
         to="RubberPrisonCells2 (Upper)"),
    # The Library (RubberPrisonLibrary; named by the user, 2026-10-10): its bottom (the door, the book puzzle's shelf)
    # the map's own region; up the stairs, Jump or Bee Fly. Waiting for the Wooden Crank (Next 62): "On Top of the
    # Bookshelf" (the yellow History Book, 564: Jump and the Beemerang Toss, or Bee Fly, from the upper level) and "Book
    # Puzzle" (the Wooden Crank behind the shelf, 561: the shelf moved, all four History Books placed in order, 554).
    Area("RubberPrisonLibrary", "Upper", (), _UP),
    # The Cafeteria (RubberPrisonCafeteria): its right (the door, raised) the map's own region; the left a drop down,
    # back up with Jump or Bee Fly. Its cannon lever (549) only turns the cannons' rolling rocks off. Waiting for the
    # Wooden Crank (Next 62): "Behind the Cannons" (the blue History Book on the left, 548, the user's name), nothing
    # needed.
    Area("RubberPrisonCafeteria", "Left", (), one_way(None, _UP), out=_UP),
    # The Gym (RubberPrisonGym): its door free, the map's own region; up to the top left by the room's crank puzzle,
    # Beemerang Halt (no Jump); back down a drop (the user, 2026-10-10). Loading it sets the prison's gate flag 535 off.
    Area("RubberPrisonGym", "Top Left", (), CanUse("Beemerang Halt"), out=one_way(None, CanUse("Beemerang Halt"))),
    # The Security Room (RubberPrisonSecurity; named by the user, 2026-10-10): its right (the door to the Pier, the 535
    # lever, the PC) the map's own region; the left (the crank, the trapped guards) Beetle Dig away, both ways; the bottom
    # right (the save crystal, the door to the Third Floor) a drop down a ledge, back up with Jump or Bee Fly. The crank
    # (Beemerang Halt) powers the PC (552), which hit (553) toggles the Cafeteria's tables (550): never needed.
    Area("RubberPrisonSecurity", "Left", (), CanUse("Beetle Dig")),
    Area("RubberPrisonSecurity", "Bottom Right", ("loadzone3rdfloor",), one_way(None, _UP), out=_UP),
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
    # Electric Cells' top row down onto the middle row, a drop, its way back the Shield's platforms. Waiting for the
    # Wooden Crank (Next 62) there: "Middle Center Cell" (the red History Book, 582, behind a 535 gate: an attack).
    Transfer("drop", "RubberPrisonCells2", "RubberPrisonCells2", two_way=False, way_back=CanUse("Shield"),
             from_area="Upper", to_area="Middle"),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("RubberPrisonPier", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
)
# The rock just inside the Pier's ground floor left door, broken only by Horn Dash (until flag 589): coming in that
# way without it stranded the party (the user, 2026-10-04).
KEPT_OPEN = (
    EntityRef("RubberPrisonPier", "rock"),
)
KEPT_PRESENT = (
    # The Pier's third floor save crystal on its right, gone from flag 41 in the game: kept (the user, 2026-10-10).
    EntityRef("RubberPrisonPier", "SavePoint  - Duplicate"),
    # The two gate levers the game adds only from flag 79 (the Giant's Lair Bridge's scene, Event193), shown from the
    # start so each area the gates cut off has a lever of its own (the user, 2026-10-10): the Checkpoint Corridor's
    # right one (its name shared with the middle lever, always there) and the Third Floor's upper one.
    EntityRef("RubberPrisonCheckpointCorridor", "switch - Duplicate - Duplicate"),
    EntityRef("RubberPrisonThirdFloor", "globalswitch - Duplicate - Duplicate"),
)
SCENERY_PRESENT = (
    # The Pier's third floor bridge, which the Office's crank breaks (Event193, flag 583, opening the second floor's
    # right door): kept, so the floor's two sides stay joined (the user, 2026-10-10: "i don't want the bridge to
    # collapse").
    EntityRef("RubberPrisonPier", "Base/BrokenBridge"),
)
SCENERY_HIDDEN = (
    # The Library's right staircase rail: two invisible copies of its collider stacked above it, hidden so the rail is
    # jumped over onto the shelves (seen). Once the book puzzle slides its shelf away, that's the only landing left for
    # "On Top of the Bookshelf"'s Jump and Beemerang Toss (the user, 2026-10-10).
    EntityRef("RubberPrisonLibrary", "Base/fence/fence (2)"),
    EntityRef("RubberPrisonLibrary", "Base/fence/fence (3)"),
)

# The cells' hidden lift switch, hit while flag 41 (the first boss) is set: always on, as in vanilla, and it can never
# set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("RubberPrisonCells2", "FixedSwitch", 41, ALWAYS_SET),
)
