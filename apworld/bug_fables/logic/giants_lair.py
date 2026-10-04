"""Giant's Lair (the game's area 15, MapControl.areaid): its ways between maps that aren't doors. Its rooms aren't mapped
yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import Transfer

TRANSFERS = (
    Transfer("lift", "GiantLairFridgeInside", "GiantLairRoachVillage", LATER_CHAPTERS),
    Transfer("lift", "GiantLairRoachVillage", "GiantLairEntrance", LATER_CHAPTERS),
    # The ending: from the Sapling's plains to the city's end.
    Transfer("story", "GiantLairSaplingPlains", "BugariaEndPlaza", LATER_CHAPTERS, two_way=False),
)
