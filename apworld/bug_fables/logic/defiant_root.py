"""Defiant Root (the game's area 10, MapControl.areaid): its ways between maps that aren't doors. Its rooms aren't mapped
yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import Transfer

TRANSFERS = (
    Transfer("elevator", "DefiantRoot2", "BeehiveOutside", LATER_CHAPTERS),
)
