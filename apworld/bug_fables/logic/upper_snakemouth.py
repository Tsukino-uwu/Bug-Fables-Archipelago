"""Upper Snakemouth (the game's area 24, MapControl.areaid): its spots, and what the seed changes there. Its
rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, CanUse
from ..data_types import ALWAYS_SET, FlagSwap, Location, Source

LOCATIONS = (
    # Where the game teaches the Icicle (flag 171); the later chapters' story-order stand-in, every ability taught
    # before it. The story reaches it after the submarine; its path reads no submarine flag, so the sub is this
    # stand-in's caution, not a gate.
    Location("Upper Snakemouth: Entrance", 74, "UpperSnekTransition",
             Source(event=180, flag=171),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly") & SUBMARINE, reach=LATER_CHAPTERS),
)

# The lights-out switches that start lit once flag 41 (the first boss) is set, and the plate room's hidden switch, hit
# while it is: as in vanilla, and none can set 41 (build step 60).
ACTIVATION_FLAGS = (
    *(FlagSwap("UpperSnekSwitchPuzzle", f"TOGSwitch{n}", 41, ALWAYS_SET) for n in (1, 4, 8)),
    FlagSwap("UpperSnekPressurePlateRoom", "testswitch", 41, ALWAYS_SET),
)
