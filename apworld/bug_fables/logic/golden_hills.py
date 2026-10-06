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
# Across the right crank room, all of it (the user: assume every need for the room), or Bee Fly alone.
RIGHT_CRANK_ROOM = (CanUse("Shield") & CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump")
                    & CanUse("Beemerang Toss") & CanUse("Beemerang Halt")) | CanUse("Bee Fly")

LOCATIONS = (
    # Crystal berry #9, high over the left hall's flytraps by its right door: Jump.
    Location("Golden Hills: Left Hall, above the Flytraps", 127, "GoldenHillsDungeonLeftMain",
             Source(berry=9, pickup=Pickup(map="GoldenHillsDungeonLeftMain", type=3, item=0)), rule=CanUse("Jump"),
             category="crystal_berry"),
    # A Hustle Candy on a small stump behind a flower, in the thorns midway across the right crank room; the room's
    # every need, as the user asked (the shield over the thorns, the Beemerang to grab it), or Bee Fly.
    Location("Golden Hills: Right Crank Room, Stump behind the Flower", 128, "GoldenHillsDungeonRightCrank",
             Source(flag=727, pickup=Pickup(map="GoldenHillsDungeonRightCrank", type=0, item=178)),
             rule=RIGHT_CRANK_ROOM),
    # The left crank half room: everything up its fixed cranks, Jump and Beemerang Halt.
    Location("Golden Hills: Left Crank Half Room, Behind the Bush", 129, "GoldenHillsDungeonLeftCrankHalf",
             Source(flag=121, pickup=Pickup(map="GoldenHillsDungeonLeftCrankHalf", type=2, item=36)),
             rule=CanUse("Jump") & CanUse("Beemerang Halt")),
    # A respawning Burly Berry in grass by a crank (regional flag 7): the game's own again once checked.
    Location("Golden Hills: Left Crank Half Room, Grass by the Crank", 130, "GoldenHillsDungeonLeftCrankHalf",
             Source(regional=7, pickup=Pickup(map="GoldenHillsDungeonLeftCrankHalf", type=0, item=3)),
             rule=CanUse("Jump") & CanUse("Beemerang Halt") & CanUse("Horn Slash"), category="hidden_item"),
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
    # fixed cranks, Beemerang Halt) and platforms (Jump), or flown across (Bee Fly), both ways.
    Area("GoldenHillsDungeonLeftMain", "Left", ("loadzonecrankleft",),
         (CanUse("Jump") & CanUse("Beemerang Halt")) | CanUse("Bee Fly")),
    # Its top left door, up the platform its Wooden Crank slot works (flag 110) with Beemerang Halt; down from it a
    # drop to the left side.
    Area("GoldenHillsDungeonLeftMain", "Top Left", ("load zone crank half",), WOODEN_CRANK & CanUse("Beemerang Halt"),
         out=one_way(None, WOODEN_CRANK & CanUse("Beemerang Halt")), to="GoldenHillsDungeonLeftMain (Left)"),
)
