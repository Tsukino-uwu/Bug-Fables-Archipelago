"""Golden Hills (the game's area 4, MapControl.areaid): its ways between maps that aren't doors. Its rooms aren't mapped
yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, Transfer

# The Wooden Crank (key item 58), found in the dungeon: not an item yet, so the later chapters' stand-in until its
# room is mapped.
WOODEN_CRANK = LATER_CHAPTERS

TRANSFERS = (
    Transfer("elevator", "GoldenHillsDungeonEntrance", "GoldenHillsDungeonUpperMain", LATER_CHAPTERS),
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
)
