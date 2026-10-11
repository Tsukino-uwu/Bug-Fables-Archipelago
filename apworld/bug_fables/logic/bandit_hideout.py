"""Bandit Hideout (the game's area 20, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from rule_builder.rules import True_

from ..custom_rules import CanUse, one_way
from ..data_types import (ALWAYS_SET, Area, EntityRef, FlagEntity, Give, Location, Pickup, PlacedPickup, SceneryFit,
                          Source, Transfer)

LOCATIONS = (
    # Where the story teaches Dig (flag 18), moved from the Cell's scene to an item lying on the Entrance's altar
    # (2026-10-12): the capture that led to the cell is skipped, and the cell's own teaching trigger kept away.
    Location("Bandit Hideout: Entrance, Altar", 71, "HideoutEntrance",
             Source(flag=18, pickup=Pickup(map="HideoutEntrance", type=0, item=0)),
             rule=CanUse("Jump") | CanUse("Bee Fly")),
    # The Cell (HideoutCell; 2026-10-12): its middle (the door, the save crystal) free; the left and right cells dug
    # into (Beetle Dig).
    # The Stolen Silk (key item 108, flag 270) by a box on the right of the Garden's upper area.
    Location("Bandit Hideout: Garden, By the Box", 294, "HideoutGarden",
             Source(flag=270, pickup=Pickup(map="HideoutGarden", type=1, item=108)), area="Upper", no_jump=True),
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
# trigger, made only from 18, made from the start instead (PRESENT_FROM: their limits still count, so the fight
# ends at 301), and the wall over that door (Base/FakeWalls, a ConditionChecker
# limited by 18) switched off: flag 18's whole change to the room (its altar is scenery, no flag's).
KEPT_OPEN = tuple(EntityRef(m, e) for m, names in (
    ("HideoutEntrance", ("eventcheck",)),
    ("HideoutCell", ("eventrigger",)),
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
SCENERY_OFF = (
    EntityRef("HideoutEntrance", "Base/FakeWalls"),
)
# The altar's collider stands 7.9 high over a short model: fitted to the model, so the Dig location's item lies on it.
SCENERY_FITTED = (
    SceneryFit("HideoutEntrance", "Base/AncientPillarSmall"),
)
PICKUPS_PLACED = (
    PlacedPickup("HideoutEntrance", "Base/AncientPillarSmall", 0, 0, 18),
)
PRESENT_FROM = (
    FlagEntity("HideoutEntrance", "loadzone below", ALWAYS_SET),
    FlagEntity("HideoutEntrance", "event2", ALWAYS_SET),
    # The stairs room's door up to the Entrance, made only from the escape (11): from the start, so the way down from
    # the Entrance has its way back.
    FlagEntity("HideoutStairsRoom", "loadzoneup", ALWAYS_SET),
    # Its save crystal and Venus healer, also only from 11 (2026-10-12).
    FlagEntity("HideoutStairsRoom", "save", ALWAYS_SET),
    FlagEntity("HideoutStairsRoom", "venushealer - Duplicate - Duplicate", ALWAYS_SET),
    # The Garden's well door (to the Defiant Root's well), made only from the escape (11): from the start, a second way
    # out (2026-10-12).
    FlagEntity("HideoutGarden", "loadzone well", ALWAYS_SET),
)
MAP_AREAS = (
    # HideoutLeftA (2026-10-12): its right door's lower area the map's own region; the upper left (the garden door) up a
    # frozen droplet knocked over and jumped onto (Freeze, Horn Slash, Jump), a free drop back. The crates that make
    # steps there from the escape on (11, ConditionCheckers) are left out: the droplet keeps the room's own puzzle.
    Area("HideoutLeftA", "Upper Left", ("loadzone garden",), CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump"),
         out=True_()),
    # The stairs room (HideoutStairsRoom; 2026-10-12): its north door and a crank the map's own region; the bottom
    # across a platform flipped by the cranks on each side (Beemerang Halt) or flown over (Bee Fly), both ways, with the
    # door up to the Entrance and the other crank.
    Area("HideoutStairsRoom", "Bottom", ("loadzoneup",), CanUse("Beemerang Halt") | CanUse("Bee Fly"),
         out=CanUse("Beemerang Halt") | CanUse("Bee Fly")),
    # The Garden (HideoutGarden; 2026-10-12): its right door's bottom right the map's own region; the bottom left (the
    # well door) dug to (Beetle Dig) both ways; the upper area (the storage door, the Stolen Silk) up from the bottom left
    # by Jump or Bee Fly, a drop back to either side (TRANSFERS for the right one).
    Area("HideoutGarden", "Bottom Left", ("loadzone well",), CanUse("Beetle Dig"), out=CanUse("Beetle Dig")),
    Area("HideoutGarden", "Upper", ("loadzonestorage",), CanUse("Jump") | CanUse("Bee Fly"),
         out=one_way(None, CanUse("Jump") | CanUse("Bee Fly")), to="HideoutGarden (Bottom Left)"),
)
TRANSFERS = (
    Transfer("drop", "HideoutGarden", "HideoutGarden", two_way=False,
             way_back=CanUse("Beetle Dig") & (CanUse("Jump") | CanUse("Bee Fly")), from_area="Upper"),
)
