"""Chapters 2-7: one stand-in region, and the spots where the game teaches each later ability."""
from __future__ import annotations

from ..custom_rules import CanUse
from ..data_types import Location, Region, Source

REGIONS = (
    # Chapters 2-7 as one region until they get room-level logic: past chapter 2's start, with everything the story used
    # before (the permit, the Boat Ticket, the first boss, the whole party and its attacks). More cautious than the
    # game.
    Region("Later Chapters"),
)
# Where the game teaches each ability (its flag). Until chapters 2-7 get room-level logic, each needs every ability
# taught before it (story order). The names are provisional.
LOCATIONS = (
    # Beemerang Halt (flag 21).
    Location("Golden Settlement: Festival, Wacka Worm Game", 68, "Later Chapters",
             Source(event=55, flag=21)),
    # Dash (flag 699).
    Location("Lost Sands: Entrance", 69, "Later Chapters",
             Source(event=221, flag=699),
             rule=CanUse("Beemerang Halt")),
    # Shield (flag 20).
    Location("Honey Factory: First Room, Switch", 70, "Later Chapters",
             Source(event=95, flag=20),
             rule=CanUse("Beemerang Halt") & CanUse("Dash")),
    # Beetle Dig (flag 18).
    Location("Bugaria Hideout: Cell", 71, "Later Chapters",
             Source(event=109, flag=18),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield")),
    # Horn Dash (flag 39).
    Location("Swamplands: Bridge", 72, "Later Chapters",
             Source(event=131, flag=39),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")),
    # Bee Fly (flag 19).
    Location("Barren Lands: Fly Spot", 73, "Later Chapters",
             Source(event=150, flag=19),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash")),
    # Icicle (flag 171).
    Location("Upper Snakemouth: Entrance", 74, "Later Chapters",
             Source(event=180, flag=171),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly")),
)
