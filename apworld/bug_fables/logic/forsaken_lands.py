"""Forsaken Lands, the Barren Lands (the game's area 7, MapControl.areaid): its spots and ways between maps that aren't
doors. Its rooms aren't mapped yet."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, Location, Source, StoryEvent, Transfer

LOCATIONS = (
    # Where the game teaches Bee Fly (flag 19): escorting the queen to the termites, she orders Vi to fly the broken
    # bridge. The later chapters' story-order stand-in, every ability taught before it.
    Location("Forsaken Lands: Broken Bridge", 73, "BarrenLandsBeefly",
             Source(event=150, flag=19),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash"), reach=LATER_CHAPTERS, area="Left"),
)
# The broken bridge room's ruler, knocked down from its upper right (Event146, flag 382): a switch only Kabbu's horn
# hits (its data[4] 1, NPCControl), the bridge then joining the upper right and the left.
_HORN = CanUse("Horn Slash") | CanUse("Horn Dash")
STORY_EVENTS = (
    StoryEvent("Forsaken Lands: Broken Bridge, Ruler Knocked Down", "Broken Bridge Ruler Down", "BarrenLandsBeefly",
               Source(flag=382), rule=_HORN, area="Upper Right"),
)
TRANSFERS = (
    # Both ways, needing nothing (the user, 2026-10-07): from outside its first scene (Event149) runs without the queen,
    # the leader standing in; from inside the mod opens it as if from outside (termite_gate_from_inside).
    Transfer("gate", "TermiteOutside", "TermiteMainPlaza"),
    # The broken bridge room's upper right (its door to the miniboss room) down onto its right side's ledge, a drop (on
    # down to the bottom door too): back up only by the left side and the ruler (the user, 2026-10-07).
    Transfer("drop", "BarrenLandsBeefly", "BarrenLandsBeefly", two_way=False,
             way_back=CanUse("Bee Fly") & Has("Broken Bridge Ruler Down"), from_area="Upper Right"),
)
MAP_AREAS = (
    # The CD room's raised strip (its left and right edges, where the side room's left edge lands too) over its floor
    # (the top and bottom doors): a drop down, Jump back up the ledges.
    Area("BarrenLandsCD", "Upper", ("loadzoneright", "returnloadzoneleft"), CanUse("Jump"),
         out=one_way(None, CanUse("Jump")), landings=(("BarrenLandsSideGPT", "returnzone left"),)),
    # The Termite gate's left door (to the ant tunnel), behind two rocks broken with Horn Dash, both ways.
    Area("TermiteOutside", "Left", ("loadzonecave",), CanUse("Horn Dash")),
    # The broken bridge room (the user, 2026-10-07): its right side's ledge (the wrong turn up top) is the map's own
    # region, a drop down to its bottom door, Jump back up the small ledge; the left door across the gap from the
    # bottom with Bee Fly, both ways; the upper right (the door to the miniboss room) joined to the left only by the
    # knocked-down ruler, both ways.
    Area("BarrenLandsBeefly", "Bottom", ("loadzonesouth",), one_way(None, CanUse("Jump")), out=CanUse("Jump")),
    Area("BarrenLandsBeefly", "Left", ("loadzonecd",), CanUse("Bee Fly"), to="BarrenLandsBeefly (Bottom)"),
    Area("BarrenLandsBeefly", "Upper Right", ("loadzoneminiboss",), Has("Broken Bridge Ruler Down"),
         to="BarrenLandsBeefly (Left)"),
)
# Patton's lab, opened by the escort to the termites (flag 376): open from the start (the user, 2026-10-07), its door
# entity there and its slab gone, as the game has them from 376.
KEPT_PRESENT = (
    EntityRef("BarrenLandsEntrance", "doorpatton"),
)
SCENERY_HIDDEN = (
    EntityRef("BarrenLandsEntrance", "PattonsHouse/PopCan (1)"),
)
# The trigger in front of the Termite gate that runs its first opening as the party walks up (until flag 384): kept
# away, so the gate opens only when talked to, as every later time (the user, 2026-10-07).
KEPT_OPEN = (
    EntityRef("TermiteOutside", "event"),
)
