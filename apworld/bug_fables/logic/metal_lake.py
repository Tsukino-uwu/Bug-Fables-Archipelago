"""Metal Lake, Mystery Island included (the game's area 16, MapControl.areaid): its dock. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import SUBMARINE, SUBMARINE_KEY
from ..data_types import ItemEntity, Transfer

# Metal Lake (MetalLake; mapped 2026-10-11): the submarine's lake, no doors, no pickups, only its six docks; every dock
# reaches every other by the submarine. Never a start and never shuffled (no door leads in or out).
TRANSFERS = (
    Transfer("submarine", "MysteryIsland", "MetalLake", SUBMARINE),
)
# The submarine's dock exists with its key item in the bag (Mystery Island's needs no story flag).
PRESENT_WITH_ITEM = (
    ItemEntity("MysteryIsland", "Fixedsub", SUBMARINE_KEY),
)
