"""Bandit Hideout (the game's area 20, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import EntityRef, Location, Source

LOCATIONS = (
    # Where the game teaches Beetle Dig (flag 18); the later chapters' story-order stand-in, every ability taught before
    # it. Pending (the user, 2026-10-09; build step 67): the hideout's front door needs the Rusty Key, sold only from
    # chapter 4's start (300), whose chain the stand-in doesn't hold, and the capture strips the seed's ability items.
    Location("Bandit Hideout: Cell", 71, "HideoutCell",
             Source(event=109, flag=18),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield"), reach=LATER_CHAPTERS, pending=True),
)
# The stealth bandits (each NPC's battle data is the catch, Event108, to the cell, and its sight) and the catch triggers
# around the table: kept away, so only the way across a room is logic (2026-10-11). The game swaps them itself at the
# story's capture (flag 11) for regular enemies, made only from 11 on: those kept present instead.
KEPT_OPEN = tuple(EntityRef(m, e) for m, names in (
    ("HideoutCentralRoom", ("bandit1", "bandit2", "bandit2 - Duplicate")),
    ("HideoutLeftA", ("burglar", "bandit1 - Duplicate")),
    ("HideoutGarden", ("burglar - Duplicate", "bandit1 - Duplicate - Duplicate", "thief", "burglar - Duplicate - Duplicate",
                       "bandit1 - Duplicate - Duplicate - Duplicate")),
    ("HideoutRightA", ("burglarpatrol", "bandittrigger", "tabletrigger", "tabletrigger - Duplicate")),
) for e in names)
KEPT_PRESENT = tuple(EntityRef(m, e) for m, names in (
    ("HideoutCentralRoom", ("enemy bandit",)),
    ("HideoutLeftA", ("burglar enemy",)),
    ("HideoutGarden", ("enemyburglar", "enemy bandit")),
    ("HideoutRightA", ("enemyburglar - Duplicate",)),
) for e in names)
