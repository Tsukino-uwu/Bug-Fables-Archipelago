"""Far Grasslands (the game's area 8, MapControl.areaid): its spots, and what the seed changes there. Its rooms are
being mapped."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import Area, Source, StoryEvent

STORY_EVENTS = (
    # The border cave's two gates, each opened for good by its lever on the far side (Event136 sets the lever's
    # activationflag, which hides that side's SnekGate): a basic attack (the user, 2026-10-07).
    StoryEvent("Far Grasslands: Border Cave, Left Gate Opened", "Border Cave Left Gate Open", "FGCave",
               Source(flag=362), rule=ANY_ATTACK, area="Left"),
    StoryEvent("Far Grasslands: Border Cave, Right Gate Opened", "Border Cave Right Gate Open", "FGCave",
               Source(flag=361), rule=ANY_ATTACK, area="Right"),
)
MAP_AREAS = (
    # The border cave (the user, 2026-10-07): its bottom door the map's own region; the ant tunnel's miner at the top past
    # grass, the horn both ways; its left and right doors (to the Broodmother's lair and the Wasp Kingdom) each behind a
    # gate its own side's lever opens.
    Area("FGCave", "Tunnel", (), CanUse("Horn Slash")),
    Area("FGCave", "Left", ("loadzonebroodmother",), Has("Border Cave Left Gate Open")),
    Area("FGCave", "Right", ("loadzone wasp",), Has("Border Cave Right Gate Open")),
)
