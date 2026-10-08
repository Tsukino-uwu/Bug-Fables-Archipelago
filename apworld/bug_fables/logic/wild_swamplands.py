"""Wild Swamplands (the game's area 9, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse, Member
from ..data_types import Area, Location, Source

LOCATIONS = (
    # Where the game teaches the Horn Dash (flag 39); the later chapters' story-order stand-in, every ability taught
    # before it.
    Location("Wild Swamplands: Bridge, Boulder", 72, "SwamplandsBridge",
             Source(event=131, flag=39),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig"),
             reach=LATER_CHAPTERS),
)
MAP_AREAS = (
    # The swamp boss's room (the user, 2026-10-08): its bottom (the door, the save crystal, the healing flower) the map's
    # own region; its top door across lily pads, Jump, past the boss in the middle (Event137, until 359), fought from
    # either side: Leif, as it burrows, and it summons nothing. Both ways.
    Area("SwamplandsBoss", "Top", ("loadzonenorth",), CanUse("Jump") & Member("Leif")),
)
