"""Bandit Hideout (the game's area 20, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import CanUse
from ..data_types import EntityRef, Give, Location, Source

LOCATIONS = (
    # The Cell (HideoutCell; 2026-10-12): its middle (the door, the save crystal) free; the left cell, where the capture
    # left the party, dug into (Beetle Dig) to its trigger for the scene that teaches Dig (Event109, flag 18; the
    # capture itself is skipped in a seed, so this is reached walking in).
    Location("Bandit Hideout: Cell", 71, "HideoutCell", Source(event=109, flag=18), rule=CanUse("Beetle Dig")),
    # The right cell, dug into (Beetle Dig): its dig spot starts Event217, Cerise appearing with Crystal Berry 19.
    Location("Bandit Hideout: Cell, Dig Spot", 293, "HideoutCell",
             Source(event=217, flag=674, berry=19, give=Give(map="HideoutCell", type=3, item=19)),
             rule=CanUse("Beetle Dig"), category="crystal_berry", no_jump=True),
)
# The stealth bandits (each NPC's battle data is the catch, Event108, to the cell, and its sight) and the catch triggers
# around the table: kept away, so only the way across a room is logic (2026-10-11). The game swaps them itself at the
# story's capture (flag 11) for regular enemies, made only from 11 on: those kept present instead.
# The story's capture at the entrance's altar (eventcheck, Event109 until 18) takes every item into a saved string and
# drops the party in the cell: skipped in a seed (2026-10-11, the user's pick); the door below and Astotheles' fight
# trigger, made only from 18, kept present instead, and the wall over that door (Base/FakeWalls, a ConditionChecker
# limited by 18) switched off: flag 18's whole change to the room (its altar is scenery, no flag's).
KEPT_OPEN = tuple(EntityRef(m, e) for m, names in (
    ("HideoutEntrance", ("eventcheck",)),
    ("HideoutCentralRoom", ("bandit1", "bandit2", "bandit2 - Duplicate")),
    ("HideoutLeftA", ("burglar", "bandit1 - Duplicate")),
    ("HideoutGarden", ("burglar - Duplicate", "bandit1 - Duplicate - Duplicate", "thief", "burglar - Duplicate - Duplicate",
                       "bandit1 - Duplicate - Duplicate - Duplicate")),
    ("HideoutRightA", ("burglarpatrol", "bandittrigger", "tabletrigger", "tabletrigger - Duplicate")),
) for e in names)
KEPT_PRESENT = tuple(EntityRef(m, e) for m, names in (
    ("HideoutEntrance", ("loadzone below", "event2")),
    ("HideoutCentralRoom", ("enemy bandit",)),
    ("HideoutLeftA", ("burglar enemy",)),
    ("HideoutGarden", ("enemyburglar", "enemy bandit")),
    ("HideoutRightA", ("enemyburglar - Duplicate",)),
) for e in names)
SCENERY_OFF = (
    EntityRef("HideoutEntrance", "Base/FakeWalls"),
)
