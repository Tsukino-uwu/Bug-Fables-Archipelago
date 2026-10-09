"""Wild Swamplands (the game's area 9, MapControl.areaid): its spots, and what the seed changes there, mapped
room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import Has, True_

from ..custom_rules import ANY_ATTACK, CanUse, Member, one_way
from ..data_types import (ALWAYS_SET, Area, EntityRef, FlagSwap, Location, MapScene, Pickup, Source, StoryEvent,
                          Transfer)

_UP = CanUse("Jump") | CanUse("Bee Fly")
_TREE_DOWN = "Swamp Tree Knocked Down"
_BRIDGE_DOWN = "Swamp Lower Bridge Knocked Down"
# The long swamp room's crossing from its west part to its middle.
_CROSS_WEST = (CanUse("Jump") & CanUse("Freeze")) | CanUse("Bee Fly")
_LIFT = "Swamp Lift Running"
# Crank Pond's lift up to its right door: the crank behind a boulder, then Jump on.
_CRANK_LIFT = CanUse("Horn Dash") & CanUse("Beemerang Halt") & CanUse("Jump")
# Swamplands5's left side to its right: Bee Fly, or the center platform moved by the right side's lever, hit from the
# top left (past a boulder, Horn Dash) with the Beemerang over Jump, once the bottom left's lever (past thorns: the
# Shield or Bee Fly; any attack) has moved the platform in its way; then Jump across.
_LEFT_TO_RIGHT = CanUse("Bee Fly") | (CanUse("Jump") & CanUse("Horn Dash") & CanUse("Beemerang Toss") & ANY_ATTACK
                                      & (CanUse("Shield") | CanUse("Bee Fly")))
# Swamplands7's ice blocks: one frozen from a droplet (Freeze), knocked into place (the horn) and jumped on (Jump); and
# one brought up from its bottom middle to its middle on a platform, past a boulder (Horn Dash).
_ICE_BLOCK = CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump")
_ICE_BLOCK_UP = CanUse("Horn Dash") & _ICE_BLOCK

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
    # Crystal berry #27, dug up under a boulder on the right side (the user, 2026-10-09).
    Location("Wild Swamplands: Junction, Dig Spot", 183, "Swamplands5",
             Source(berry=27, pickup=Pickup(map="Swamplands5", type=3, item=0)),
             rule=CanUse("Horn Dash") & CanUse("Beetle Dig"), category="crystal_berry"),
    # A Clear Bomb on a vine high above the center platform, coming back each visit (regional flag 34): from the right
    # side, the platform lowered by its lever, a Beemerang held behind it (Halt), Jump on, the Beemerang let go raises
    # it, and the Toss hits the vine. It falls on the left side: back on the right, the lever lowers the platform again,
    # then Jump across and back (dropped to the left with the platform up, the way back is the long one; the user,
    # 2026-10-09).
    Location("Wild Swamplands: Junction, Vine above the Platform", 184, "Swamplands5",
             Source(regional=34, pickup=Pickup(map="Swamplands5", type=0, item=36)),
             rule=CanUse("Jump") & CanUse("Beemerang Halt")),
    # Crank Pond (Swamplands6; named by the user, 2026-10-09): a Burly Berry behind a tree near the right door, coming
    # back each visit (regional flag 22; its other hiding flag, 281, is set by nothing), and a Crunchy Leaf in the grass
    # in the bottom left (regional flag 19), the horn.
    Location("Wild Swamplands: Crank Pond, Behind the Tree", 185, "Swamplands6",
             Source(regional=22, pickup=Pickup(map="Swamplands6", type=0, item=3)), no_jump=True,
             area="Upper Right"),
    Location("Wild Swamplands: Crank Pond, Grass in the Bottom Left", 186, "Swamplands6",
             Source(regional=19, pickup=Pickup(map="Swamplands6", type=0, item=0)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    # Ice Block Climb's medal (Swamplands7; named by the user, 2026-10-09), Eternal Venom, on a stump in its upper left:
    # nothing once there.
    Location("Wild Swamplands: Ice Block Climb, On Top of the Stump", 187, "Swamplands7",
             Source(flag=355, pickup=Pickup(map="Swamplands7", type=2, item=27)), no_jump=True, area="Upper Left"),
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
    # Swamplands5's lift between its right side and its top right, started by the lever up there (any attack; it sets
    # 354 and runs for good).
    StoryEvent("Wild Swamplands: Junction, Lift Lever Hit", _LIFT, "Swamplands5", Source(flag=354), rule=ANY_ATTACK,
               area="Top Right"),
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
    # The long swamp room (the user, 2026-10-09), its middle the map's own region. Its left door in a tiny part of its
    # own, past a boulder, Horn Dash both ways; from there to the middle, Jump and Freeze, or Bee Fly; back the other
    # way Freeze isn't needed, but without it there's no way back. The right side past grass and a boulder (the horn
    # and Horn Dash) and a jump (Jump), or Bee Fly for both, both ways; its door to Swamplands5 in a part of its own,
    # Horn Dash and Beetle Dig both ways.
    Area("Swamplands4", "West", (), CanUse("Bee Fly") | one_way(CanUse("Jump"), _CROSS_WEST),
         out=_CROSS_WEST),
    Area("Swamplands4", "Left Door", ("loadzone left",), CanUse("Horn Dash"), to="Swamplands4 (West)"),
    Area("Swamplands4", "Right", (), ((CanUse("Horn Slash") & CanUse("Horn Dash")) | CanUse("Bee Fly"))
         & (CanUse("Jump") | CanUse("Bee Fly"))),
    Area("Swamplands4", "Right Door", ("loadzoneright",), CanUse("Horn Dash") & CanUse("Beetle Dig"),
         to="Swamplands4 (Right)"),
    # Swamplands5 (the user, 2026-10-09): its right side (the door to Swamplands6, the dig spot, a lever moving the
    # center platform) the map's own region. Its left door's side, across the center: Bee Fly both ways; to the left,
    # the lever (any attack) and Jump, the platform then crossed back with Jump, or the Shield down onto the thorns,
    # one-way; to the right as _LEFT_TO_RIGHT says. Its top right (the doors to Swamplands7 and 8, the save crystal, the
    # lift's lever) a drop down to the right side; back up the lift once started, Jump to get on.
    Area("Swamplands5", "Left", ("loadzoneleft",),
         (CanUse("Jump") & ANY_ATTACK) | CanUse("Bee Fly") | one_way(CanUse("Shield"), _LEFT_TO_RIGHT),
         out=_LEFT_TO_RIGHT),
    Area("Swamplands5", "Top Right", ("loadzone north", "loadzoneright"), Has(_LIFT) & CanUse("Jump"),
         out=one_way(None, Has(_LIFT) & CanUse("Jump"))),
    # Crank Pond (the user, 2026-10-09): its door from the Junction and its left side the map's own region. The middle
    # (a crank) by Jump or Bee Fly, both ways. The lower right from the middle by the crank (Beemerang Halt) and Jump,
    # or Bee Fly; back, the lily pad its grass (the horn) starts, or Bee Fly. The upper right (the door to Swamplands8,
    # the Burly Berry) a drop down; up, the lift: its crank behind a boulder (Horn Dash, Beemerang Halt), Jump on.
    Area("Swamplands6", "Middle", (), _UP),
    Area("Swamplands6", "Lower Right", (), (CanUse("Beemerang Halt") & CanUse("Jump")) | CanUse("Bee Fly"),
         out=CanUse("Horn Slash") | CanUse("Bee Fly"), to="Swamplands6 (Middle)"),
    Area("Swamplands6", "Upper Right", ("loadzone right",), _CRANK_LIFT, out=one_way(None, _CRANK_LIFT),
         to="Swamplands6 (Lower Right)"),
    # Swamplands7 (the user, 2026-10-09): its middle the map's own region. The bottom middle a drop down from it; back
    # up, an ice block brought up on the platform (_ICE_BLOCK_UP). The bottom right (the door to the Junction) to and
    # from the bottom middle by an ice block, or Bee Fly. The upper middle: that ice block pushed onto a plate below
    # and right of the middle lowers a platform, Freeze again melts the block and it rises with the party; a drop back
    # down. The upper left (the medal's stump) from the upper middle by an ice block or Bee Fly, back by Bee Fly only
    # (or its drop to the left, below). The left (its door, a red bounce pad up to the middle): to and from the middle
    # by Bee Fly, or across once an ice block brought up opens the way; the pad one-way.
    Area("Swamplands7", "Bottom Middle", (), one_way(None, _ICE_BLOCK_UP), out=_ICE_BLOCK_UP),
    Area("Swamplands7", "Bottom Right", ("loadzone south",), CanUse("Bee Fly") | _ICE_BLOCK,
         to="Swamplands7 (Bottom Middle)"),
    Area("Swamplands7", "Upper Middle", (), _ICE_BLOCK_UP, out=one_way(None, _ICE_BLOCK_UP)),
    Area("Swamplands7", "Upper Left", (), CanUse("Bee Fly") | one_way(_ICE_BLOCK, CanUse("Bee Fly") | _ICE_BLOCK_UP),
         out=CanUse("Bee Fly"), to="Swamplands7 (Upper Middle)"),
    Area("Swamplands7", "Left", ("loadzone left",), CanUse("Bee Fly") | _ICE_BLOCK_UP,
         out=one_way(None, CanUse("Bee Fly") | _ICE_BLOCK_UP)),
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
    # Crank Pond's lily pad, started by cutting its grass on the right (the horn): it goes back and forth, so it also
    # takes the party from the lower right to the left side and back.
    Transfer("lily pad", "Swamplands6", "Swamplands6", CanUse("Horn Slash"), two_way=False, from_area="Lower Right"),
    # Swamplands7's upper left down to its left, a drop; and the left down to the bottom right with Jump. Back from
    # either, round the room: the red pad to the middle, then an ice block brought up.
    Transfer("drop", "Swamplands7", "Swamplands7", two_way=False, way_back=_ICE_BLOCK_UP, from_area="Upper Left",
             to_area="Left"),
    Transfer("ledges", "Swamplands7", "Swamplands7", CanUse("Jump"), two_way=False, way_back=_ICE_BLOCK_UP,
             from_area="Left", to_area="Bottom Right"),
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
# Swamplands5's centipede scene (Event147, on the first entry by any door, until 383) leaves the party at the left
# door, which would strand a first entry from the top right without its lift: never played (the user, 2026-10-09).
SCENES_KEPT_AWAY = (
    MapScene("Swamplands5", 147, 383),
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
