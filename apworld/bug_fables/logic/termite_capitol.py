"""Termite Capitol (the game's area 18, MapControl.areaid): its spots, dock and ways between maps that aren't doors, and
what the seed changes there. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY, CanUse
from ..data_types import ItemEntity, Location, Source, Transfer

LOCATIONS = (
    # The Termite King hands over the submarine after the Colosseum (flag 379); never behind the sub itself. The later
    # chapters' story-order stand-in.
    Location("Termite Capitol: Termite King's Reward", 76, "TermiteRoyalChamber",
             Source(event=164, flag=379),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly"), reach=LATER_CHAPTERS),
)
TRANSFERS = (
    Transfer("submarine", "TermitePier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    Transfer("arena", "TermiteColiseum1", "TermiteColiseum2", LATER_CHAPTERS),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (379 here).
PRESENT_WITH_ITEM = (
    ItemEntity("TermitePier", "Fixedsub", SUBMARINE_KEY),
)
# The scientist and the queen show the dock off (Event165), so they wait for it too.
HELD_UNTIL_ITEM = (
    ItemEntity("TermitePier", "FixedScientist", SUBMARINE_KEY),
    ItemEntity("TermitePier", "FixedQueen", SUBMARINE_KEY),
)
# The gate opens from inside too (slot_data's termite_gate_from_inside): the mod marks it opened (flag 384) as its scene
# starts there, so the scene takes the opened gate's way through instead of looking for the outside guards.
