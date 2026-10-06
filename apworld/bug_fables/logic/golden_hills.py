"""Golden Hills (the game's area 4, MapControl.areaid): its ways between maps that aren't doors. Its rooms aren't mapped
yet."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, Location, Pickup, Source, StoryEvent, Transfer

# The Wooden Crank (key item 58), found in the dungeon: not an item yet, so the later chapters' stand-in until its
# room is mapped. In a seed it won't be used up (apimplementation.md, Next 62), so one is enough for every slot.
WOODEN_CRANK = LATER_CHAPTERS
# The Big Crank (key item 60), the same way.
BIG_CRANK = LATER_CHAPTERS
# The Sun and Moon Offerings (key items 55, 56), given in the Golden Settlement (GoldenSettlement2, lines 45, 67, 77):
# not items yet, so the later chapters' stand-in until that chain is gone through in the quest pass.
SUN_OFFERING = LATER_CHAPTERS
MOON_OFFERING = LATER_CHAPTERS
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
    # A respawning Magic Seed in grass by a crank in the left crank room (regional flag 0): the room's every need.
    Location("Golden Hills: Left Crank Room, Grass by the Crank", 131, "GoldenHillsDungeonCrankLeft",
             Source(regional=0, pickup=Pickup(map="GoldenHillsDungeonCrankLeft", type=0, item=11)),
             rule=CanUse("Beemerang Halt") & CanUse("Jump") & CanUse("Horn Slash"), category="hidden_item"),
    # The left crank half room: everything up its fixed cranks, Jump and Beemerang Halt.
    Location("Golden Hills: Left Crank Half Room, Behind the Bush", 129, "GoldenHillsDungeonLeftCrankHalf",
             Source(flag=121, pickup=Pickup(map="GoldenHillsDungeonLeftCrankHalf", type=2, item=36)),
             rule=CanUse("Jump") & CanUse("Beemerang Halt")),
    # A respawning Burly Berry in grass by a crank (regional flag 7): the game's own again once checked.
    Location("Golden Hills: Left Crank Half Room, Grass by the Crank", 130, "GoldenHillsDungeonLeftCrankHalf",
             Source(regional=7, pickup=Pickup(map="GoldenHillsDungeonLeftCrankHalf", type=0, item=3)),
             rule=CanUse("Jump") & CanUse("Beemerang Halt") & CanUse("Horn Slash"), category="hidden_item"),
)
STORY_EVENTS = (
    # The upper hall's two shrines (Event72), each fed its own offering, which it keeps (flags 125, 126); a wrong one
    # starts a fight. Together they open the boss door's gate. The left one is behind grass (the horn).
    StoryEvent("Golden Hills: Upper Hall, Sun Offering", "Upper Hall Sun Shrine Fed", "GoldenHillsDungeonUpperMain",
               Source(flag=125), rule=CanUse("Horn Slash") & SUN_OFFERING),
    StoryEvent("Golden Hills: Upper Hall, Moon Offering", "Upper Hall Moon Shrine Fed", "GoldenHillsDungeonUpperMain",
               Source(flag=126), rule=MOON_OFFERING, area="Upper Right"),
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
    # The upper hall's upper right (its upper right door and the Moon shrine), behind a barrier its lever lowers (any
    # attack, flag 127, Event50), starting a platform that stays, boarded from below with Jump: the lever is only up
    # there, so nothing from below first; down without Jump, no way back up.
    Area("GoldenHillsDungeonUpperMain", "Upper Right", ("loadzonehigh",), False_(),
         out=ANY_ATTACK & one_way(None, CanUse("Jump"))),
    # Its top door (to the boss), behind a gate both shrines open; arriving while shut, a pocket with only that door.
    Area("GoldenHillsDungeonUpperMain", "Boss Door", ("loadzoneboss",),
         Has("Upper Hall Sun Shrine Fed") & Has("Upper Hall Moon Shrine Fed")),
    # The upper side room's upper door: up with Jump and a platform turned by the crank its slot makes (the Wooden
    # Crank, flag 128, with Beemerang Halt); down a drop.
    Area("GoldenHillsDungeonUpperSide", "Upper Door", ("loadzonehigh",),
         WOODEN_CRANK & CanUse("Jump") & CanUse("Beemerang Halt"),
         out=one_way(None, WOODEN_CRANK & CanUse("Jump") & CanUse("Beemerang Halt"))),
)
