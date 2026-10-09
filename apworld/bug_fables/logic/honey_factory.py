"""Honey Factory (the game's area 13, MapControl.areaid): its spots, and what the seed changes there. Its rooms
aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import ALWAYS_SET, EntityRef, FlagSwap, Location, Source

LOCATIONS = (
    # Where the game teaches the Shield (flag 20); the later chapters' story-order stand-in, every ability taught before
    # it.
    Location("Honey Factory: First Room, Switch", 70, "FactoryProcessingFirstRoom",
             Source(event=95, flag=20),
             rule=CanUse("Beemerang Halt") & CanUse("Dash"), reach=LATER_CHAPTERS),
)

# The door back out to the Bee Kingdom's Beehive Lift, made in the game only from 299: kept present with its other half
# (bee_kingdom_hive.py) (the user, 2026-10-09).
KEPT_PRESENT = (
    EntityRef("HoneyFactoryEntrance", "loadzoneoutside"),
)
SCENERY_HIDDEN = (
    # That door's closed model, hidden in the game from 299.
    EntityRef("HoneyFactoryEntrance", "Base/DoorE"),
)
# The pump room's hidden platform switch, hit while flag 41 (the first boss) is set: always on, as in vanilla, and it
# can never set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("FactoryProcessingPump", "platformcontrol", 41, ALWAYS_SET),
)
