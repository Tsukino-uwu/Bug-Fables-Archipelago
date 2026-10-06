"""Lost Sands (the game's area 3, MapControl.areaid): its spots, its ways between maps that aren't doors, and what the
seed changes there, mapped room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, DoorRule, EntityRef, Location, Pickup, Source, StoryEvent, Transfer

# The Rusty Key, bought at the Defiant Root well (line 3, which also sets flag 239, making the hideout door): not an
# item yet, so the later chapters' stand-in until its sale is a location.
RUSTY_KEY = LATER_CHAPTERS
# The Sand Castle Key (113), made from the Heaven Key (105, the roach village) and the Earth Key (106, Astotheles) in
# Event109: not items yet, so the later chapters' stand-in until the chain is gone through in the quest pass.
SAND_CASTLE_KEY = LATER_CHAPTERS

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
             rule=CanUse("Horn Dash") & CanUse("Beetle Dig"), category="dig_spot", no_jump=True, area="Left"),
    # Meditation on a small platform on the badge alcove's ledge: Jump.
    Location("Lost Sands: Badge Alcove, Platform on the Upper Left", 120, "DesertBadgeAlcove",
             Source(flag=262, pickup=Pickup(map="DesertBadgeAlcove", type=2, item=56)), rule=CanUse("Jump"),
             area="Ledge"),
    Location("Lost Sands: Badge Alcove, Grass by the Right Door", 121, "DesertBadgeAlcove",
             Source(flag=210, pickup=Pickup(map="DesertBadgeAlcove", type=0, item=90)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    # Crystal berry #14, dug up on a high ledge over the caravan camp: Jump and Bee Fly up.
    Location("Lost Sands: Caravan Camp, Dig Spot", 122, "DesertCaravanMap",
             Source(berry=14, pickup=Pickup(map="DesertCaravanMap", type=3, item=0)),
             rule=CanUse("Jump") & CanUse("Bee Fly") & CanUse("Beetle Dig"), category="crystal_berry"),
    # Strong Start on a high ledge: grass cut (the horn), Jump, and a bridge the horn lowers (flag 690).
    Location("Lost Sands: Golden Hills Border, Left Ledge", 123, "DesertBeforeGH",
             Source(flag=415, pickup=Pickup(map="DesertBeforeGH", type=2, item=23)),
             rule=CanUse("Horn Slash") & CanUse("Jump")),
    Location("Lost Sands: Roach Village, Dig Spot", 124, "DesertRoachVillage",
             Source(berry=21, pickup=Pickup(map="DesertRoachVillage", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
    # The oasis's top right, through its cave door or up its platform: the Crimson Ore in the cave, and the Berry Jam on
    # a sandpile below it, dropped to (back up by the platform, as for the area).
    Location("Lost Sands: Oasis, Crimson Cave", 125, "DesertOasis",
             Source(flag=319, pickup=Pickup(map="DesertOasis", type=1, item=98)), no_jump=True, area="Top Right"),
    Location("Lost Sands: Oasis, On Top of the Sandpile", 126, "DesertOasis",
             Source(flag=733, pickup=Pickup(map="DesertOasis", type=0, item=173)),
             rule=one_way(None, Has("Oasis Platform Running") & CanUse("Jump")), no_jump=True, area="Top Right"),
)
STORY_EVENTS = (
    # The oasis's platform switch, up in its top right only (any attack, flag 255); the platform then keeps running.
    StoryEvent("Lost Sands: Oasis, Platform Switch Hit", "Oasis Platform Running", "DesertOasis", Source(flag=255),
               rule=ANY_ATTACK, area="Top Right"),
    # The south trench's bridge, knocked over by the horn from the left side (flag 282); it stays down.
    StoryEvent("Lost Sands: South Trench, Bridge Knocked Down", "South Trench Bridge Down", "DesertTrenchSouth",
               Source(flag=282), rule=CanUse("Horn Slash"), area="Left"),
    # The sand pit's eight bridges, each knocked over by the horn from one side (flags 202-209) and staying down.
    StoryEvent("Lost Sands: Sand Pit, Bottom Bridge Knocked Down", "Sand Pit Bottom Bridge Down", "DesertSandPitArea",
               Source(flag=202), rule=CanUse("Horn Slash"), area="Bottom"),
    # Three around the middle, reached over small platforms: the top doors' two and the lower left one.
    StoryEvent("Lost Sands: Sand Pit, Middle Bridges Knocked Down", "Sand Pit Middle Bridges Down", "DesertSandPitArea",
               Source(flag=209), rule=CanUse("Horn Slash") & CanUse("Jump")),
    StoryEvent("Lost Sands: Sand Pit, Left Bridges Knocked Down", "Sand Pit Left Bridges Down", "DesertSandPitArea",
               Source(flag=208), rule=CanUse("Horn Slash"), area="Left"),
    StoryEvent("Lost Sands: Sand Pit, Right Bridge Knocked Down", "Sand Pit Right Bridge Down", "DesertSandPitArea",
               Source(flag=204), rule=CanUse("Horn Slash"), area="Right"),
    StoryEvent("Lost Sands: Sand Pit, Top Right Bridge Knocked Down", "Sand Pit Top Right Bridge Down",
               "DesertSandPitArea", Source(flag=205), rule=CanUse("Horn Slash"), area="Top Right"),
    # The thorn field's bridge, knocked down by the horn from its high right side (flag 261); it stays down.
    StoryEvent("Lost Sands: Thorn Field, Bridge Knocked Down", "Thorn Field Bridge Down", "DesertJumpPuzzle",
               Source(flag=261), rule=CanUse("Horn Slash"), area="Right"),
)
TRANSFERS = (
    # Chapter 3's end: the attack on the city.
    Transfer("story", "DesertSandCastle", "BugariaAssociationAttack", LATER_CHAPTERS, two_way=False),
)
# The border gate to the Far Grasslands, open from the start: the game makes its door and breaks the gate only in
# chapter 5 (flag 348), and a start or a shuffled door arriving there before landed behind it, falling forever.
# The castle door, made only once the Sand Castle Key opens the lock in front of it (Event59, then Event112, flag 280),
# and up a step (Jump). Coming out of it while the castle is shut lands in front of it, free to walk away.
DOOR_RULES = (
    DoorRule("DesertSandCastle", "loadzone castle", SAND_CASTLE_KEY & CanUse("Jump")),
)
KEPT_PRESENT = (
    EntityRef("DesertFGBorder", "loadzonefg"),
    # The oasis entrance's red bounce pad, gone from chapter 4 (flag 300) in the game, kept for good (the user).
    EntityRef("DesertOasisEntrance", "mush"),
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
    # The badge alcove's left door, on a ledge: a drop down to the rest, with no way back up from inside the room.
    Area("DesertBadgeAlcove", "Ledge", ("loadzone left",), False_(), out=one_way(None, False_())),
    # The sand pit: the map's own region is its middle platforms, holding no door; each door joins it over bridges
    # (each way the same once they're down), and Bee Fly crosses everything.
    Area("DesertSandPitArea", "Bottom", ("loadzone book area",),
         CanUse("Bee Fly") | Has("Sand Pit Bottom Bridge Down")),
    # Up its upper bridge and over platforms (Jump), or along the lower bridges with the middle's one (no Jump).
    Area("DesertSandPitArea", "Left", ("loadzone mountain",),
         CanUse("Bee Fly")
         | (Has("Sand Pit Left Bridges Down") & (CanUse("Jump") | Has("Sand Pit Middle Bridges Down")))),
    Area("DesertSandPitArea", "Top Left", ("loadzone caravan area",),
         CanUse("Bee Fly") | (Has("Sand Pit Middle Bridges Down") & CanUse("Jump"))),
    Area("DesertSandPitArea", "Top Right", ("loadzone scorpion",),
         CanUse("Bee Fly") | (Has("Sand Pit Middle Bridges Down") & CanUse("Jump"))),
    # The right door reaches only the top right one, over two bridges, one knocked over from each side.
    Area("DesertSandPitArea", "Right", ("loadzone right",),
         CanUse("Bee Fly") | (Has("Sand Pit Right Bridge Down") & Has("Sand Pit Top Right Bridge Down")),
         to="DesertSandPitArea (Top Right)"),
    # The oasis's bottom door, past a gate dug under (Beetle Dig); from it, out by digging or jumping.
    Area("DesertOasis", "Bottom", ("loadzonesouth",), CanUse("Beetle Dig"),
         out=CanUse("Beetle Dig") | CanUse("Jump")),
    # Its top right (the cave door), up high: down by a drop, or on the platform once its switch is hit (up there
    # only); back up on the platform, boarded with Jump.
    Area("DesertOasis", "Top Right", ("loadzone cave",), Has("Oasis Platform Running") & CanUse("Jump"),
         out=one_way(None, Has("Oasis Platform Running") & CanUse("Jump"))),
    # The oasis entrance's right side (the oasis door), past spikes the bubble shield crosses, or flown over (Bee
    # Fly), both ways; back left, also its red bounce pad (kept present), a one-way.
    Area("DesertOasisEntrance", "Right", ("loadzoneside",), CanUse("Shield") | CanUse("Bee Fly"),
         out=CanUse("Shield") | CanUse("Bee Fly") | one_way(None, CanUse("Shield") | CanUse("Bee Fly"))),
    # The trench's middle: its right side (the map's own region: the bottom, bottom-right and right doors) and its top
    # door, past a gate dug under both ways.
    Area("DesertTrenchMiddle", "Top", ("loadzone north",), CanUse("Beetle Dig")),
    # Its middle, up high (the bottom-left door, to the south trench's ledge): drops down to either side, and no way
    # up from the right.
    Area("DesertTrenchMiddle", "Middle", ("loadzone south2",), False_(), out=one_way(None, False_())),
    # Its left side (the roach village door): dropped to from the middle, back up with Jump and Bee Fly.
    Area("DesertTrenchMiddle", "Left", ("loadzone village",), one_way(None, CanUse("Jump") & CanUse("Bee Fly")),
         out=CanUse("Jump") & CanUse("Bee Fly"), to="DesertTrenchMiddle (Middle)"),
    # The thorn field's right door, up high: from the left, the thorns crossed (the bubble shield) or the bridge, then
    # Jump; down from it a free drop, back up the same way.
    Area("DesertJumpPuzzle", "Right", ("loadzone right",),
         CanUse("Jump") & (CanUse("Shield") | Has("Thorn Field Bridge Down")),
         out=one_way(None, CanUse("Jump") & (CanUse("Shield") | Has("Thorn Field Bridge Down")))),
    # The southern desert's right door, past thorns: the bubble shield or Bee Fly, both ways.
    Area("DesertSouthern", "Right", ("loadzone right",), CanUse("Shield") | CanUse("Bee Fly")),
    # Its top door (to the roach village), on a ledge: a drop down to the left side, no way back up inside the room.
    Area("DesertSouthern", "Ledge", ("loadzone north",), False_(), out=one_way(None, False_())),
)
