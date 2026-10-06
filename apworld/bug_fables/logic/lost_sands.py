"""Lost Sands (the game's area 3, MapControl.areaid): its spots, its ways between maps that aren't doors, and what the
seed changes there, mapped room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, Location, Pickup, Source, StoryEvent, Transfer

# The Rusty Key, bought at the Defiant Root well (line 3, which also sets flag 239, making the hideout door): not an
# item yet, so the later chapters' stand-in until its sale is a location.
RUSTY_KEY = LATER_CHAPTERS

LOCATIONS = (
    # Where the game teaches the Dash (flag 699); the later chapters' story-order stand-in. Its map is the Outskirts'
    # (BOLostSandsEntrance, area 0) at the desert's border; it stays with the area its name gives.
    Location("Lost Sands: Entrance", 69, "BOLostSandsEntrance",
             Source(event=221, flag=699),
             rule=CanUse("Beemerang Halt"), reach=LATER_CHAPTERS),
    Location("Lost Sands: Badlands, Center Pillar", 114, "DesertBadlands",
             Source(flag=413, pickup=Pickup(map="DesertBadlands", type=2, item=0)),
             rule=CanUse("Jump") & CanUse("Bee Fly")),
    Location("Lost Sands: Badlands, Rock Ledge", 115, "DesertBadlands",
             Source(flag=729, pickup=Pickup(map="DesertBadlands", type=0, item=65)),
             rule=CanUse("Jump") & CanUse("Beemerang Toss")),
    # Under the giant book, hidden until the boulder breaks; free from the book area's north half.
    Location("Lost Sands: Book Area, Under the Book", 116, "DesertBookArea",
             Source(flag=730, pickup=Pickup(map="DesertBookArea", type=0, item=128)), no_jump=True, area="North"),
    # The Tardigrade Shield on the rock formation's idol, high up: Jump, Freeze and the horn.
    Location("Lost Sands: Rock Formation, Tardigrade Idol", 117, "DesertRockFormation",
             Source(flag=343, pickup=Pickup(map="DesertRockFormation", type=2, item=51)),
             rule=CanUse("Jump") & CanUse("Freeze") & CanUse("Horn Slash")),
    # On the south trench's top-left ledge: a drop from its door, Jump from the left side.
    Location("Lost Sands: South Trench, Ledge Mushroom", 118, "DesertTrenchSouth",
             Source(flag=713, pickup=Pickup(map="DesertTrenchSouth", type=0, item=96)), no_jump=True, area="Ledge"),
    # Behind a rock Horn Dash breaks, on the Defiant Root entrance's left side.
    Location("Lost Sands: Defiant Root Entrance, Dig Spot", 119, "DesertDREastEntrance",
             Source(flag=398, pickup=Pickup(map="DesertDREastEntrance", type=0, item=121)),
             rule=CanUse("Horn Dash") & CanUse("Beetle Dig"), no_jump=True, area="Left"),
)
STORY_EVENTS = (
    # The south trench's bridge, knocked over by the horn from the left side (flag 282); it stays down.
    StoryEvent("Lost Sands: South Trench, Bridge Knocked Down", "South Trench Bridge Down", "DesertTrenchSouth",
               Source(flag=282), rule=CanUse("Horn Slash"), area="Left"),
)
TRANSFERS = (
    # Chapter 3's end: the attack on the city.
    Transfer("story", "DesertSandCastle", "BugariaAssociationAttack", LATER_CHAPTERS, two_way=False),
)
# The border gate to the Far Grasslands, open from the start: the game makes its door and breaks the gate only in
# chapter 5 (flag 348), and a start or a shuffled door arriving there before landed behind it, falling forever.
KEPT_PRESENT = (
    EntityRef("DesertFGBorder", "loadzonefg"),
)
SCENERY_HIDDEN = (
    EntityRef("DesertFGBorder", "Base/Gate"),
)
SCENERY_PRESENT = (
    EntityRef("DesertFGBorder", "Base/GateBroken"),
)
MAP_AREAS = (
    # The entrance's door to the book area, up a ledge: no way up from inside the room, only a drop down.
    Area("DesertEntrance", "Ledge", ("WarpBookZone",), False_(), out=one_way(None, False_())),
    # The badlands' door to the bandit hideout, up two ledges (Jump) behind a grate the Rusty Key opens (flag 258);
    # arriving while it's shut, the game pushes the party past it, and the ledges drop back down freely.
    Area("DesertBadlands", "Hideout Door", ("loadzone hideout",), CanUse("Jump") & RUSTY_KEY,
         out=one_way(None, CanUse("Jump") & RUSTY_KEY)),
    # The book area's north half (its top and right doors): the boulder broken (Horn Dash) or the sand pit flown over
    # (Bee Fly), both ways.
    Area("DesertBookArea", "North", ("warp north", "WarpPuzzle"), CanUse("Horn Dash") | CanUse("Bee Fly")),
    # The south trench's left side, across a gap from its right and top-right doors: flown over (Bee Fly) or on the
    # bridge, knocked over from the left by the horn.
    Area("DesertTrenchSouth", "Left", ("loadzone left",), CanUse("Bee Fly") | Has("South Trench Bridge Down"),
         out=CanUse("Bee Fly") | CanUse("Horn Slash")),
    # Its top-left door and the mushroom, on a ledge over the left side: a drop down, Jump back up.
    Area("DesertTrenchSouth", "Ledge", ("loadzone north2",), CanUse("Jump"), out=one_way(None, CanUse("Jump")),
         to="DesertTrenchSouth (Left)"),
    # The Defiant Root entrance's left side (the door to the Defiant Root): flown over (Bee Fly) from the right, or
    # left by the crank (the horn) and Beemerang Halt; the crank resets on leaving the room.
    Area("DesertDREastEntrance", "Left", ("loadzoneDR",), CanUse("Bee Fly"),
         out=CanUse("Bee Fly") | (CanUse("Horn Slash") & CanUse("Beemerang Halt"))),
)
