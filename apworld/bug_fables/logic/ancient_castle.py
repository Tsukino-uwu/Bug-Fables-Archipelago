"""Ancient Castle, the Sand Castle (the game's area 11, MapControl.areaid): its spots, and what the seed changes there,
mapped room by room (room-checklist.md)."""
from __future__ import annotations

from ..custom_rules import CanUse
from ..data_types import ALWAYS_SET, Area, FlagSwap

MAP_AREAS = (
    # The castle's entrance (SandCastleEntrance; the user, 2026-10-09): its left door the map's own region, its right
    # door across the middle on a bridge that shows only while the crystal is lit (a StencilSwitch, reset each visit),
    # the Beemerang Toss from either side; or flown over, Bee Fly. Both ways.
    Area("SandCastleEntrance", "Right", ("loadzone right",), CanUse("Beemerang Toss") | CanUse("Bee Fly")),
)

# Hidden switches hit while flag 41 (the first boss) is set: the entrance's crystal scene and the basement's platforms
# always on, as in vanilla, and neither can set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("SandCastleEntrance", "switch", 41, ALWAYS_SET),
    FlagSwap("SandCastleBasement", "platformenabler", 41, ALWAYS_SET),
)
