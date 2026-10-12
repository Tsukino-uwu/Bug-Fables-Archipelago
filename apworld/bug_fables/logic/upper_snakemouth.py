"""Upper Snakemouth (the game's area 24, MapControl.areaid): its spots, and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import True_

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import ALWAYS_SET, Area, FlagSwap, Location, Source

_ACROSS = CanUse("Icicle") & CanUse("Jump")
# Icicle Pond's switch, hit with the Icicle, or flown up to with Bee Fly and an attack.
_SWITCH = CanUse("Icicle") | (CanUse("Bee Fly") & ANY_ATTACK)

LOCATIONS = (
    # Where the game teaches the Icicle (Event180, until flag 171): its scene and fight on entering by either door. From
    # the left door it also moves the party to the right side, once; not a way across in the logic.
    Location("Upper Snakemouth: Icicle Pond, Fight", 74, "UpperSnekTransition", Source(event=180, flag=171),
             no_jump=True),
)
MAP_AREAS = (
    # Icicle Pond (UpperSnekTransition; named 2026-10-12): its right door (from the entrance) the map's own region;
    # water in the middle, crossed both ways by Icicle and Jump, to an empty platform on the left; from there a dig
    # barrier (Beetle Dig, both ways) to the left door and its switch. The switch (flag 518, for good) opens the left
    # door; arriving by it pushes the party past, so without the switch that door is a one-way in.
    Area("UpperSnekTransition", "Left Platform", (), _ACROSS),
    Area("UpperSnekTransition", "Left", (), CanUse("Beetle Dig"), to="UpperSnekTransition (Left Platform)"),
    Area("UpperSnekTransition", "Left Door", ("loadzoneforward",), _SWITCH, out=True_(),
         to="UpperSnekTransition (Left)"),
)

# The lights-out switches that start lit once flag 41 (the first boss) is set, and the plate room's hidden switch, hit
# while it is: as in vanilla, and none can set 41 (build step 60).
ACTIVATION_FLAGS = (
    *(FlagSwap("UpperSnekSwitchPuzzle", f"TOGSwitch{n}", 41, ALWAYS_SET) for n in (1, 4, 8)),
    FlagSwap("UpperSnekPressurePlateRoom", "testswitch", 41, ALWAYS_SET),
)
