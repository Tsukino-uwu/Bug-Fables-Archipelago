"""Wild Swamplands (the game's area 9, MapControl.areaid): its spots, and what the seed changes there, mapped
room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import Has, True_

from ..custom_rules import CanUse, Member, one_way
from ..data_types import ALWAYS_SET, Area, EntityRef, FlagSwap, Location, Pickup, Source, StoryEvent, Transfer

_UP = CanUse("Jump") | CanUse("Bee Fly")
_TREE_DOWN = "Swamp Tree Knocked Down"
_BRIDGE_DOWN = "Swamp Lower Bridge Knocked Down"

LOCATIONS = (
    # Where the game teaches the Horn Dash (Event131, flag 39): the boulder at the bridge's bottom, talked to with
    # nothing (it can't be broken otherwise), there until then (the user, 2026-10-09: with Kabbu out too, Leif in his
    # place). Arriving through the bottom door pushes the party past it, so it never blocks the door.
    Location("Wild Swamplands: Bridge, Boulder", 72, "SwamplandsBridge", Source(event=131, flag=39), no_jump=True,
             area="Bottom"),
    # The lily pad pond (Swamplands2; the user, 2026-10-08): a Poison Bomb dug up in its top part, Beetle Dig; a
    # respawning Honey Drop in grass by the bottom door (regional flag 6, which the dig spot shares), the horn.
    Location("Wild Swamplands: Lily Pad Pond, Dig Spot", 181, "Swamplands2",
             Source(flag=737, pickup=Pickup(map="Swamplands2", type=0, item=31)), rule=CanUse("Beetle Dig"),
             category="dig_spot", no_jump=True, area="Top"),
    Location("Wild Swamplands: Lily Pad Pond, Grass in the Bottom Right", 182, "Swamplands2",
             Source(regional=6, pickup=Pickup(map="Swamplands2", type=0, item=1)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
)
STORY_EVENTS = (
    # Leafbug Crossing's tree, knocked down by the horn from the middle (Event129: the hidden switch `eventhit`, hit by
    # the horn or the Dash, sets 335; the Leafbugs on the right leave): a bridge up to the right for good.
    StoryEvent("Wild Swamplands: Leafbug Crossing, Tree Knocked Down", _TREE_DOWN, "Swamplands3", Source(flag=335),
               rule=CanUse("Horn Slash")),
    # The swamp bridge's small bridge, knocked down by the horn from the top right, beside the green bounce pad (its
    # switch `@Bridge1`, Event94, sets 337; the game takes the Dash too): it joins the top right and the top door's
    # platform for good.
    StoryEvent("Wild Swamplands: Bridge, Lower Bridge Knocked Down", _BRIDGE_DOWN, "SwamplandsBridge", Source(flag=337),
               rule=CanUse("Horn Slash"), area="Top Right"),
)
MAP_AREAS = (
    # The swamp boss's room (the user, 2026-10-08): its bottom (the door, the save crystal, the healing flower) the
    # map's own region; its top door across lily pads, Jump, past the boss in the middle (Event137, until 359), fought
    # from either side: Leif, as it burrows, and it summons nothing. Both ways.
    Area("SwamplandsBoss", "Top", ("loadzonenorth",), CanUse("Jump") & Member("Leif")),
    # The swamp's second room (the user, 2026-10-08): its bottom door the map's own region; its top door up across lily
    # pads and grass-blocked ledges, Jump and the horn or Bee Fly; back down the horn or Bee Fly, no Jump. The Leafbug
    # ambush before the top door (Event128) for the enemy pass.
    Area("Swamplands2", "Top", ("loadzonenorth",), CanUse("Jump") & (CanUse("Horn Slash") | CanUse("Bee Fly")),
         out=CanUse("Horn Slash") | CanUse("Bee Fly")),
    # Leafbug Crossing (the user, 2026-10-08): its middle, where the tree is knocked down for good (the story event
    # above), the map's own region; the bottom (its left door) to and from it with Jump or Bee Fly; the upper right (its
    # door to the swamp bridge) to and from it over the fallen tree, Jump or Bee Fly. Before then, only a drop from the
    # upper right into the middle (below).
    Area("Swamplands3", "Bottom", ("loadzone back",), _UP),
    Area("Swamplands3", "Right", ("loadzoneright",), Has(_TREE_DOWN) & _UP),
    # The swamp bridge (the user, 2026-10-08), kept up: the bridge the map's own region, its left and right doors free
    # between them (in vanilla it falls, and the right side is reached from Swamplands7 only). The top right, a drop
    # down a path from the right side (the green bounce pad, the small bridge's switch), Jump back up (or Bee Fly to
    # the top door, then the red pad). The top door (to the boss) on a platform across the small bridge: from the top
    # right by Bee Fly, or over ledges with Jump once the small bridge is down, back the same way. Its red bounce pad, a
    # drop down a small ledge from the door and Jump back up: it sends the party up to the bridge's left end, whose
    # boulder, broken by Horn Dash, opens another red pad back down beside it (the user's sketch, 2026-10-09). The
    # bottom (its door to Swamplands4, the save crystal, the boulder that teaches the Horn Dash), where the collapse put
    # the party in vanilla: a drop from the bridge, its invisible walls taken away, and back up to the left end by its
    # bounce pad, both kept for a seed (below; seen 2026-10-09).
    Area("SwamplandsBridge", "Top Right", (), one_way(None, _UP), out=CanUse("Jump")),
    Area("SwamplandsBridge", "Top", ("loadzone boss",), CanUse("Bee Fly") | (Has(_BRIDGE_DOWN) & CanUse("Jump")),
         out=Has(_BRIDGE_DOWN) & CanUse("Jump"), to="SwamplandsBridge (Top Right)"),
    Area("SwamplandsBridge", "Red Bounce Pad", (), CanUse("Horn Dash"), out=True_()),
    Area("SwamplandsBridge", "Bottom", ("loadzonesouth",), True_()),
)
TRANSFERS = (
    # Leafbug Crossing's upper right down into the middle, a drop: back up, the tree knocked down and Jump or Bee Fly.
    Transfer("drop", "Swamplands3", "Swamplands3", two_way=False, way_back=CanUse("Horn Slash") & _UP,
             from_area="Right"),
    # The swamp bridge's top door down a small ledge to its red bounce pad, a drop; back up, Jump (or Bee Fly from the
    # top right).
    Transfer("drop", "SwamplandsBridge", "SwamplandsBridge", two_way=False, way_back=_UP, from_area="Top",
             to_area="Red Bounce Pad"),
    Transfer("ledge", "SwamplandsBridge", "SwamplandsBridge", CanUse("Jump"), two_way=False,
             from_area="Red Bounce Pad", to_area="Top"),
)

# The swamp bridge stays up (the user, 2026-10-04): its collapse's trigger (Event130, which also needs Maki following)
# and the three leafbugs standing for it on the right kept away (the user, 2026-10-08).
KEPT_OPEN = (
    EntityRef("SwamplandsBridge", "eventtrigger"),
    EntityRef("SwamplandsBridge", "leafbug"),
    EntityRef("SwamplandsBridge", "leafbug - Duplicate"),
)
# The bridge's invisible walls taken away (the user, 2026-10-09), as the collapse takes them (until 336): a drop from
# the bridge down to the bottom.
SCENERY_HIDDEN = (
    EntityRef("SwamplandsBridge", "Base/BridgeWalls"),
)
# Bounce pads are always there (the user, 2026-10-08): the bridge's bottom one, made only after the swamp's boss (359),
# keeps the bottom from being a dead end.
KEPT_PRESENT = (
    EntityRef("SwamplandsBridge", "spring - Duplicate - Duplicate"),
)
_GRASS = ("blockgrass", "blockgrass - Duplicate", "blockgrass - Duplicate - Duplicate", "blockgrass 4", "blockgrass 5",
          "blockgrass 7", "blockgrass 8", "blockgrass 9", "funGrass", "funGrass - Duplicate",
          "funGrass - Duplicate - Duplicate")
# Grass that sets flag 41 (the first boss) when cut or when its drop is picked up, which counted an artifact (seen
# 2026-10-08): it sets nothing, as it changes nothing in vanilla, where 41 is already set. A hidden switch and a plate
# hit while 41 is set: always on, as in vanilla (build step 60).
ACTIVATION_FLAGS = (
    *(FlagSwap("Swamplands2", grass, 41, -1) for grass in _GRASS),
    FlagSwap("Swamplands6", "blockgrass - Duplicate", 41, -1),
    FlagSwap("Swamplands7", "PressurePlate - Duplicate - Duplicate - Duplicate - Duplicate", 41, ALWAYS_SET),
    FlagSwap("Swamplands8", "emptyswitch", 41, ALWAYS_SET),
)
