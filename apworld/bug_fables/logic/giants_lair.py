"""Giant's Lair (the game's area 15, MapControl.areaid): its rooms as mapped so far, its ways between maps that aren't
doors, and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import Rule

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import ALWAYS_SET, Area, FlagSwap, Location, Pickup, Source, Transfer

# Up a bounce pad onto a rock, Jump, then the rock knocked with the horn into the hand, which opens the next pad; the
# hand lets go whenever the room is left.
_HAND_ROCK: Rule = CanUse("Jump") & CanUse("Horn Slash")

LOCATIONS = (
    Location("Giant's Lair: Giant's Hand, Dig Spot", 247, "GiantLairDeadLands1",
             Source(berry=49, pickup=Pickup(map="GiantLairDeadLands1", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
)

# The Giant's Lair Entrance (GiantLairEntrance; named by the user, 2026-10-11): one region, nothing needed between its
# doors (its left door's ledge back up by a bounce pad and the rock beside it), a save crystal, the Venus bud's scene
# (Event194, flag 569; then its healing bud) and a scene to the Roach Village from 586 (Event201).
MAP_AREAS = (
    # The Giant's Hand (GiantLairDeadLands1; named by the user, 2026-10-11): its left (the door to the Entrance, a dig
    # spot) the map's own region; across the platforms to the lower right with Bee Fly, both ways; up from there to the
    # right door's ledge only by the hand's rock, a free drop down (the user, 2026-10-11).
    Area("GiantLairDeadLands1", "Right", (), CanUse("Bee Fly")),
    Area("GiantLairDeadLands1", "Right Door", ("loadzoneforward",), _HAND_ROCK, out=one_way(None, _HAND_ROCK),
         to="GiantLairDeadLands1 (Right)"),
)

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
