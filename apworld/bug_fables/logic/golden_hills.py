"""Golden Hills (the game's area 4, MapControl.areaid): its ways between maps that aren't doors. Its rooms aren't mapped
yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, Location, Pickup, Source, Transfer

# The Wooden Crank (key item 58), found in the dungeon: not an item yet, so the later chapters' stand-in until its
# room is mapped. In a seed it won't be used up (apimplementation.md, Next 62), so one is enough for every slot.
WOODEN_CRANK = LATER_CHAPTERS
# The Big Crank (key item 60), the same way.
BIG_CRANK = LATER_CHAPTERS

LOCATIONS = (
    # Crystal berry #9, high over the left hall's flytraps by its right door: Jump.
    Location("Golden Hills: Left Hall, above the Flytraps", 127, "GoldenHillsDungeonLeftMain",
             Source(berry=9, pickup=Pickup(map="GoldenHillsDungeonLeftMain", type=3, item=0)), rule=CanUse("Jump"),
             category="crystal_berry"),
)
TRANSFERS = (
    # The middle platform (Event68, loading the other map): up once the Big Crank is in its slot (flag 118; placing it
    # starts the Mothiva and Zasp fight, Event67) and turned with Beemerang Halt; down by Beemerang Halt on the upper
    # room's own crank, always there, with nothing below to come back up by without the Big Crank.
    Transfer("elevator", "GoldenHillsDungeonEntrance", "GoldenHillsDungeonUpperMain",
             BIG_CRANK & CanUse("Beemerang Halt"), two_way=False, way_back=CanUse("Beemerang Halt")),
    Transfer("elevator", "GoldenHillsDungeonUpperMain", "GoldenHillsDungeonEntrance", CanUse("Beemerang Halt"),
             two_way=False, way_back=BIG_CRANK & CanUse("Beemerang Halt")),
)
KEPT_OPEN = (
    # The dungeon's arrival scene (Event62): it sets only its own flag, 111, read by nothing else (the user: skip it).
    EntityRef("GoldenHillsDungeonEntrance", "eventtrigger"),
    EntityRef("GoldenHillsDungeonEntrance", "eventtrigger - Duplicate"),
)
MAP_AREAS = (
    # The dungeon entrance's top right door, up the small platform the Wooden Crank works (its slot, flag 109) with
    # Beemerang Halt; down from it a drop, back up the same way.
    Area("GoldenHillsDungeonEntrance", "Top Right", ("loadzone topright",), WOODEN_CRANK & CanUse("Beemerang Halt"),
         out=one_way(None, WOODEN_CRANK & CanUse("Beemerang Halt"))),
    # The left hall: its far left side (the crank room's door) across from the right door over turning bridges (the
    # fixed cranks, Beemerang Halt) and platforms (Jump), both ways.
    Area("GoldenHillsDungeonLeftMain", "Left", ("loadzonecrankleft",), CanUse("Jump") & CanUse("Beemerang Halt")),
    # Its top left door, up the platform its Wooden Crank slot works (flag 110) with Beemerang Halt; down from it a
    # drop to the left side.
    Area("GoldenHillsDungeonLeftMain", "Top Left", ("load zone crank half",), WOODEN_CRANK & CanUse("Beemerang Halt"),
         out=one_way(None, WOODEN_CRANK & CanUse("Beemerang Halt")), to="GoldenHillsDungeonLeftMain (Left)"),
)
