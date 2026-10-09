"""Forsaken Lands, the Barren Lands (the game's area 7, MapControl.areaid): its spots and ways between maps that aren't
doors. Its rooms aren't mapped yet."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import LATER_CHAPTERS, CanUse, ItemOnHand, one_way
from ..data_types import Area, EntityRef, Location, Pickup, Source, StoryEvent, Transfer

LOCATIONS = (
    # Where the game teaches Bee Fly (flag 19): escorting the queen to the termites, she orders Vi to fly the broken
    # bridge. The later chapters' story-order stand-in, every ability taught before it.
    Location("Forsaken Lands: Broken Bridge", 73, "BarrenLandsBeefly",
             Source(event=150, flag=19),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash"), reach=LATER_CHAPTERS, area="Left"),
    # A Plumpling Pie behind the mine cart in the ant tunnel's room by the Termite gate: nothing needed (the user).
    Location("Forsaken Lands: Ant Tunnel, Behind the Mine Cart", 159, "BarrenLandsAntTunnel",
             Source(flag=739, pickup=Pickup(map="BarrenLandsAntTunnel", type=0, item=180)), no_jump=True),
    # Crystal berry #38, dropped with the berries the first time an item is left at the pink spider's hole (Event167),
    # up on the ledge by the sign: Jump, and an item to trade (the user, 2026-10-07).
    Location("Forsaken Lands: Pink Spider, First Trade", 160, "BarrenLandsPinkSpider",
             Source(berry=38, pickup=Pickup(map="BarrenLandsPinkSpider", type=3, item=0)),
             rule=CanUse("Jump") & ItemOnHand(), category="crystal_berry", area="Ledge"),
    # A Lore Book dug up in the Abandoned City's pit, from its bottom with Beetle Dig. Dropping into the pit from above
    # also reaches it, but leaving it needs Beetle Dig too: a trap without it, never a way the logic counts.
    Location("Forsaken Lands: Abandoned City, Dig Spot", 161, "AbandonedCity",
             Source(flag=499, pickup=Pickup(map="AbandonedCity", type=1, item=52)), rule=CanUse("Beetle Dig"),
             category="dig_spot", no_jump=True),
    # A respawning Magic Seed in grass by the fountain, midway up the room (regional flag 10): Bee Fly to reach it, the
    # horn to cut it (the user, 2026-10-07).
    Location("Forsaken Lands: Abandoned City, Grass by the Fountain", 162, "AbandonedCity",
             Source(regional=10, pickup=Pickup(map="AbandonedCity", type=0, item=11)),
             rule=CanUse("Bee Fly") & CanUse("Horn Slash"), category="hidden_item", no_jump=True),
    # A respawning Squash in grass in the pumpkin room's top right (regional flag 7): the horn (the user, 2026-10-07).
    Location("Forsaken Lands: Pumpkin Patch, Grass in the Top Right", 163, "BarrenLandsPumpkins",
             Source(regional=7, pickup=Pickup(map="BarrenLandsPumpkins", type=0, item=125)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True, area="Top Right"),
    # A Lore Book on an isolated platform by the wind pipes' bottom (the ring's bottom part): Bee Fly (the user).
    Location("Forsaken Lands: Wind Pipes, Isolated Platform", 164, "BarrenLandsCloud",
             Source(flag=455, pickup=Pickup(map="BarrenLandsCloud", type=1, item=52)), rule=CanUse("Bee Fly"),
             no_jump=True, area="Bottom"),
    # The dome overlook's vista, up its springs: discovery 41, "Termite Kingdom", with or without the queen (line 1 or 2,
    # flag 470); nothing needed (the user, 2026-10-07).
    Location("Forsaken Lands: Dome Overlook, Vista", 165, "BarrenLandsRock", Source(discovery=41),
             category="discovery", no_jump=True),
    # A respawning Squash in grass by the log (regional flag 11): the horn (the user).
    Location("Forsaken Lands: Dome Overlook, Grass by the Log", 166, "BarrenLandsRock",
             Source(regional=11, pickup=Pickup(map="BarrenLandsRock", type=0, item=125)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    # The tunnel side's three, on its bottom (the user, 2026-10-07): a Lore Book lying in the tall
    # grass (scenery, not a bush), nothing needed; a Squash dropped once by each of two grass patches, the horn.
    Location("Forsaken Lands: Tunnel Side, In the Tall Grass", 167, "BarrenLandsSideGPT",
             Source(flag=463, pickup=Pickup(map="BarrenLandsSideGPT", type=1, item=52)), no_jump=True),
    Location("Forsaken Lands: Tunnel Side, Grass by the Tree", 168, "BarrenLandsSideGPT",
             Source(flag=678, pickup=Pickup(map="BarrenLandsSideGPT", type=0, item=125)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    Location("Forsaken Lands: Tunnel Side, Bush by the Tall Grass", 169, "BarrenLandsSideGPT",
             Source(flag=677, pickup=Pickup(map="BarrenLandsSideGPT", type=0, item=125)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
)
# The broken bridge room's ruler, knocked down from its upper right (Event146, flag 382): a switch only Kabbu's horn
# hits (its data[4] 1, NPCControl), the bridge then joining the upper right and the left. The Horn Slash: without it
# Kabbu's Dash and Horn Dash hit no switch.
STORY_EVENTS = (
    StoryEvent("Forsaken Lands: Broken Bridge, Ruler Knocked Down", "Broken Bridge Ruler Down", "BarrenLandsBeefly",
               Source(flag=382), rule=CanUse("Horn Slash"), area="Upper Right"),
)
# The cloud room, a one-way ring of four parts, each with one door (the user, 2026-10-07): the right door (the map's
# own region) up to the top door with Jump, Horn Dash and Bee Fly; the top on to the left door, and the left on to the
# bottom edge's wrong turn, each with Horn Dash and Bee Fly; the bottom back to the right with Bee Fly. Back to any
# part only round the ring.
_CLOUD_RING = CanUse("Jump") & CanUse("Horn Dash") & CanUse("Bee Fly")
_CLOUD_STEP = CanUse("Horn Dash") & CanUse("Bee Fly")
TRANSFERS = (
    # Both ways, needing nothing (the user, 2026-10-07): from outside its first scene (Event149) runs without the queen,
    # the leader standing in; from inside the mod opens it as if from outside (termite_gate_from_inside).
    Transfer("gate", "TermiteOutside", "TermiteMainPlaza"),
    # The broken bridge room's upper right (its door to the miniboss room) down onto its right side's ledge, a drop (on
    # down to the bottom door too): back up only by the left side and the ruler (the user, 2026-10-07).
    # The cloud room's bottom back round to its right door (the map's own region), with Bee Fly.
    Transfer("wind", "BarrenLandsCloud", "BarrenLandsCloud", one_way(CanUse("Bee Fly"), _CLOUD_RING), two_way=False,
             from_area="Bottom"),
    Transfer("drop", "BarrenLandsBeefly", "BarrenLandsBeefly", two_way=False,
             way_back=CanUse("Bee Fly") & Has("Broken Bridge Ruler Down"), from_area="Upper Right"),
)
_SIDE_ROOM_UP = CanUse("Jump") & CanUse("Horn Dash") & CanUse("Bee Fly")
# Up to the pumpkin room's high right door from its top right.
_PUMPKIN_RIGHT_DOOR = CanUse("Horn Dash") & CanUse("Horn Slash") & CanUse("Jump") & CanUse("Bee Fly")
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
    # The pink spider's room: the ledge with her sign and hole, Jump up, a drop down (the user, 2026-10-07).
    Area("BarrenLandsPinkSpider", "Ledge", (), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
    # The mushroom maze: from its right door to its left (to the pink spider's room) along the path, burrowing under a
    # plank with Beetle Dig (the user, 2026-10-07).
    Area("BarrenLandsMushrooms", "Left", ("loadzonepinkspider",), CanUse("Beetle Dig")),
    # The Abandoned City's top (its door to the tent): Jump up from the bottom, a drop down (the user, 2026-10-07).
    Area("AbandonedCity", "Top", ("loadzonetent",), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
    # The pumpkin room (the user, 2026-10-07): its bottom and left doors the map's own region; its top right (the wrong
    # turn up top, by the boulders) up with Jump, the horn and Bee Fly, down with Bee Fly; its high right door (to the
    # rock room) from there: Horn Dash through the boulders, the horn to knock a stone over, Jump onto it, Bee Fly over
    # the gap. From that door, a drop to the top right's near side needs nothing; one behind the boulders is a trap
    # without Horn Dash, never a way the logic counts. Both drops one-way.
    Area("BarrenLandsPumpkins", "Top Right", ("returnloadzone",),
         CanUse("Jump") & CanUse("Horn Slash") & CanUse("Bee Fly"), out=CanUse("Bee Fly")),
    Area("BarrenLandsPumpkins", "Right Door", ("loadzoneright - Duplicate",), _PUMPKIN_RIGHT_DOOR,
         out=one_way(None, _PUMPKIN_RIGHT_DOOR), to="BarrenLandsPumpkins (Top Right)"),
    Area("BarrenLandsCloud", "Top", ("loadzonenorth",), one_way(_CLOUD_RING, _CLOUD_RING), out=False_(),
         landings=(("BarrenLandsTanks", "returnloadzone"), ("BarrenLandsCloud", "returnzone"))),
    Area("BarrenLandsCloud", "Left", ("loadzoneleft",), one_way(_CLOUD_STEP, _CLOUD_RING), out=False_(),
         to="BarrenLandsCloud (Top)"),
    Area("BarrenLandsCloud", "Bottom", ("returnzone",), one_way(_CLOUD_STEP, _CLOUD_RING), out=False_(),
         to="BarrenLandsCloud (Left)", landings=(("BarrenLandsPumpkins", "returnloadzone"),)),
    # The side room's high door (to the Golden Path tunnel): Jump, Horn Dash and Bee Fly up, a drop down (the user,
    # 2026-10-07).
    Area("BarrenLandsSideGPT", "Top", ("loadzone cave",), _SIDE_ROOM_UP, out=one_way(None, _SIDE_ROOM_UP)),
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
