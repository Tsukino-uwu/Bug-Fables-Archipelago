"""Lost Sands (the game's area 3, MapControl.areaid): its spots and ways between maps that aren't doors. Its rooms
aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import Location, Source, Transfer

LOCATIONS = (
    # Where the game teaches the Dash (flag 699); the later chapters' story-order stand-in. Its map is the Outskirts'
    # (BOLostSandsEntrance, area 0) at the desert's border; it stays with the area its name gives. The name is
    # provisional.
    Location("Lost Sands: Entrance", 69, "BOLostSandsEntrance",
             Source(event=221, flag=699),
             rule=CanUse("Beemerang Halt"), reach=LATER_CHAPTERS),
)
TRANSFERS = (
    # Chapter 3's end: the attack on the city.
    Transfer("story", "DesertSandCastle", "BugariaAssociationAttack", LATER_CHAPTERS, two_way=False),
)
