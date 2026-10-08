"""Ancient Castle, the Sand Castle (the game's area 11, MapControl.areaid): what the seed changes there. Its rooms
aren't mapped yet."""
from __future__ import annotations

from ..data_types import ALWAYS_SET, FlagSwap

# Hidden switches hit while flag 41 (the first boss) is set: the entrance's crystal scene and the basement's platforms
# always on, as in vanilla, and neither can set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("SandCastleEntrance", "switch", 41, ALWAYS_SET),
    FlagSwap("SandCastleBasement", "platformenabler", 41, ALWAYS_SET),
)
