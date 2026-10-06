"""Golden Hills (the game's area 4, MapControl.areaid): its ways between maps that aren't doors. Its rooms aren't mapped
yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import EntityRef, Transfer

TRANSFERS = (
    Transfer("elevator", "GoldenHillsDungeonEntrance", "GoldenHillsDungeonUpperMain", LATER_CHAPTERS),
)
KEPT_OPEN = (
    # The dungeon's arrival scene (Event62): it sets only its own flag, 111, read by nothing else (the user: skip it).
    EntityRef("GoldenHillsDungeonEntrance", "eventtrigger"),
    EntityRef("GoldenHillsDungeonEntrance", "eventtrigger - Duplicate"),
)
