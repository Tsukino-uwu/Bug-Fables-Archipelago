"""Giant's Lair (the game's area 15, MapControl.areaid): its ways between maps that aren't doors, and what the seed
changes there. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import ALWAYS_SET, FlagSwap, Transfer

TRANSFERS = (
    Transfer("lift", "GiantLairFridgeInside", "GiantLairRoachVillage", LATER_CHAPTERS),
    Transfer("lift", "GiantLairRoachVillage", "GiantLairEntrance", LATER_CHAPTERS),
    # The ending: from the Sapling's plains to the city's end.
    Transfer("story", "GiantLairSaplingPlains", "BugariaEndPlaza", LATER_CHAPTERS, two_way=False),
)

# The hidden switch under the platforms before the boss, hit while flag 41 (the first boss) is set: always on, as in
# vanilla, and it can never set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("GiantLairBeforeBoss2", "platformswitch", 41, ALWAYS_SET),
)
