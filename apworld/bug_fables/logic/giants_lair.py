"""Giant's Lair (the game's area 15, MapControl.areaid): its rooms as mapped so far, its ways between maps that aren't
doors, and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import Has, Rule

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, CanUse, Member, one_way
from ..data_types import ALWAYS_SET, Area, EntityRef, FlagSwap, ItemShop, Location, Pickup, Source, StoryEvent, Transfer

# Up a bounce pad onto a rock, Jump, then the rock knocked with the horn into the hand, which opens the next pad; the
# hand lets go whenever the room is left.
_HAND_ROCK: Rule = CanUse("Jump") & CanUse("Horn Slash")
# Crane Crossing's coin stacks from its left: the droplet by the flower frozen, the ice knocked with the horn to climb
# the gold coins, then Bee Fly over to the silver ones.
_COINS: Rule = CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Bee Fly")
# Its right: Bee Fly, and an attack on the switch that moves a platform across.
_TO_THE_RIGHT: Rule = CanUse("Bee Fly") & ANY_ATTACK
_FRIDGE_FIGHT = "Outside the Fridge Fight Won"
# Each floor's climb inside the fridge: droplets and geysers frozen, ice knocked into place with the horn, Jump onto it,
# Bee Fly across to the next bounce pad (on the top floor, to the lift and into it).
_FRIDGE_CLIMB: Rule = CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump") & CanUse("Bee Fly")
_UP: Rule = CanUse("Jump") | CanUse("Bee Fly")
_ROACH_SCENE = "Roach Village Scene Seen"
_LEFT_LEVERS = "Kitchen Stove Levers Hit"

LOCATIONS = (
    Location("Giant's Lair: Giant's Hand, Dig Spot", 247, "GiantLairDeadLands1",
             Source(berry=49, pickup=Pickup(map="GiantLairDeadLands1", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
    # A respawning Mystery Berry by a spear stuck in the ground (regional flag 9): Jump or Bee Fly (the user).
    Location("Giant's Lair: Crane Crossing, By the Spear", 248, "GiantLairDeadLands2",
             Source(regional=9, pickup=Pickup(map="GiantLairDeadLands2", type=0, item=156)),
             rule=CanUse("Jump") | CanUse("Bee Fly"), no_jump=True, area="Middle Right"),
    # The Status Mirror on a small ledge by the crossing (a book, a pipe, a stone, a big teddy beside it): Bee Fly from
    # the middle right, never from the right door (the user).
    Location("Giant's Lair: Crane Crossing, Ledge by the Crossing", 249, "GiantLairDeadLands2",
             Source(flag=611, pickup=Pickup(map="GiantLairDeadLands2", type=2, item=75)), rule=CanUse("Bee Fly"),
             no_jump=True, area="Middle Right"),
    # Behind the milk, the 2nd floor's upper left: up its climb's ice and a drop to it, or Bee Fly across the gap from
    # the 1st floor's bounce pad; never reached down from the top (seen).
    Location("Giant's Lair: Inside the Fridge, Behind the Milk", 250, "GiantLairFridgeInside",
             Source(berry=48, pickup=Pickup(map="GiantLairFridgeInside", type=3, item=0)),
             rule=(CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump")) | CanUse("Bee Fly"),
             category="crystal_berry", no_jump=True, area="Second Floor"),
    # Amber's shop on the Roach Village's free ground (Leaf Salad, Magic Seed, Glazed Honey, Burly Berry), there from
    # its scene (586): first purchase a check, then its own item.
    *(Location(f"Giant's Lair: Roach Village, Item Shop {slot}", 250 + slot, "GiantLairRoachVillage",
               Source(item_shop=ItemShop(map="GiantLairRoachVillage", keeper="amber", item=item)),
               category="item_shop", no_jump=True)
      for slot, item in enumerate((16, 11, 20, 3), start=1)),
    # A respawning Magic Seed dug up on the right (regional flag 30): Bee Fly up to it, then Beetle Dig.
    Location("Giant's Lair: Kitchen Stove, Dig Spot", 255, "GiantLairBeforeBoss",
             Source(regional=30, pickup=Pickup(map="GiantLairBeforeBoss", type=0, item=11)),
             rule=CanUse("Bee Fly") & CanUse("Beetle Dig"), category="dig_spot", no_jump=True),
)

STORY_EVENTS = (
    # Outside the Fridge (GiantLairFridgeOutside): walking towards the middle from either door starts Event198, a fight
    # with DeadLanderA, B and G (enemies 87-89; B flies and never comes down, so Vi), which can't be fled; then 593
    # (the user, 2026-10-11: from both sides, seen).
    StoryEvent("Giant's Lair: Outside the Fridge, Dead Lander Fight", _FRIDGE_FIGHT, "GiantLairFridgeOutside",
               Source(flag=593), rule=Member("Vi"), no_jump=True),
    StoryEvent("Giant's Lair: Outside the Fridge, Dead Lander Fight from the Right", _FRIDGE_FIGHT,
               "GiantLairFridgeOutside", Source(flag=593), rule=Member("Vi"), no_jump=True, area="Right"),
    # The Roach Village's scene on its free ground (Event199, until 586), which opens the roach lift's way up.
    # The two levers below the middle's left (flags 667, 668, each starting Event216), each putting out one of the two
    # big fires; Horn Slash only, down from the middle's left and back up by Jump or Bee Fly.
    StoryEvent("Giant's Lair: Kitchen Stove, Bottom Left Levers", _LEFT_LEVERS, "GiantLairBeforeBoss",
               Source(flag=668), rule=CanUse("Horn Slash") & _UP, no_jump=True, area="Middle Left"),
    StoryEvent("Giant's Lair: Roach Village, Arrival Scene", _ROACH_SCENE, "GiantLairRoachVillage", Source(flag=586),
               no_jump=True),
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
    # Crane Crossing (GiantLairDeadLands2; named by the user, 2026-10-11): its left (the door to the Giant's Hand on a
    # ledge, back up by a bounce pad; the flower's droplet) the map's own region. The silver coin stack in the middle up
    # from it by the coins, a drop back; from the stack a drop to the middle right, back up with Jump. The middle right
    # to the right (the door to the fridge outside) with Bee Fly and the switch, which sends a toy crane's platform
    # across; back by the red bounce pad, free. Bee Fly alone also crosses, harder: left out, as the game expects the
    # switch (the user, 2026-10-11).
    Area("GiantLairDeadLands2", "Silver Coins", (), _COINS, out=one_way(None, _COINS)),
    Area("GiantLairDeadLands2", "Middle Right", (), one_way(None, CanUse("Jump")), out=CanUse("Jump"),
         to="GiantLairDeadLands2 (Silver Coins)"),
    Area("GiantLairDeadLands2", "Right", ("loadzonenext",), _TO_THE_RIGHT, out=one_way(None, _TO_THE_RIGHT),
         to="GiantLairDeadLands2 (Middle Right)"),
    # Outside the Fridge (GiantLairFridgeOutside; named by the user, 2026-10-11): its left (the door to Crane Crossing, a
    # save crystal) the map's own region; its right (the fridge's door) past the fight, nothing else needed.
    Area("GiantLairFridgeOutside", "Right", ("fridgeloadzone",), Has(_FRIDGE_FIGHT)),
    # Inside the Fridge (GiantLairFridgeInside; named 2026-10-11): its 1st floor (the door) the map's own
    # region. Each floor up by its climb; down a free drop at a time round the puzzle areas' edges (a drop into a
    # puzzle area needs Jump or Bee Fly back, which every climb holds). The lift at the top, a drop from it.
    Area("GiantLairFridgeInside", "Second Floor", (), _FRIDGE_CLIMB, out=one_way(None, _FRIDGE_CLIMB)),
    Area("GiantLairFridgeInside", "Top Floor", (), _FRIDGE_CLIMB, out=one_way(None, _FRIDGE_CLIMB),
         to="GiantLairFridgeInside (Second Floor)"),
    Area("GiantLairFridgeInside", "Lift", (), _FRIDGE_CLIMB, out=one_way(None, _FRIDGE_CLIMB),
         to="GiantLairFridgeInside (Top Floor)"),
    # The Roach Village (GiantLairRoachVillage; named 2026-10-11): its free ground the map's own region, with its
    # scene (Event199, setting 586, which brings Amber's shop and the elder), the save crystal and the roach lift. The
    # left door (to the plains) is blocked until 595, set by the elder's talk upstairs (Event202, from 586), up by Jump
    # or Bee Fly; free in. The fridge's lift up a ledge, Jump or Bee Fly, a drop down from it.
    Area("GiantLairRoachVillage", "Left Door", ("loadzoneplains",), _UP, out=one_way(None, _UP)),
    Area("GiantLairRoachVillage", "Fridge Lift", (), _UP, out=one_way(None, _UP)),
    # The Sapling Plains (GiantLairSaplingPlains; named 2026-10-11): its door and a save crystal the map's own region;
    # up by Jump or Bee Fly to the last fight's scene (Event200, until 555), where the ending leaves from.
    Area("GiantLairSaplingPlains", "Last Fight", (), _UP),
    # The Kitchen Stove (GiantLairBeforeBoss; named 2026-10-11): its right (the village's door, the scene by it, a dig
    # spot) the map's own region. The middle's two halves each up from their side by Jump or Bee Fly, two burners'
    # fires between them until the two levers below the left turn the heat off; the left door's side up into the
    # middle's left the same way. Up on the right by Bee Fly and Horn Dash (platforms, the boulder, 665) and the lever
    # below the right (666, Horn Slash, Jump or Bee Fly back up), which with the boulder broken starts the air pipe's
    # wind, to the bounce pad to the room above, whose drop lands in the middle's left.
    Area("GiantLairBeforeBoss", "Middle Right", (), _UP),
    Area("GiantLairBeforeBoss", "Middle Left", (), Has(_LEFT_LEVERS),
         to="GiantLairBeforeBoss (Middle Right)", landings=(("GiantLairBeforeBoss2", "loadzoneleft"),)),
    Area("GiantLairBeforeBoss", "Left", ("loadzonefinal",), _UP, to="GiantLairBeforeBoss (Middle Left)"),
    Area("GiantLairBeforeBoss", "Upper", ("loadzoneup",),
         CanUse("Bee Fly") & CanUse("Horn Dash") & CanUse("Horn Slash") & _UP),
    # Above the Stove (GiantLairBeforeBoss2; named 2026-10-11): its right (the bounce pad's door) the map's own region. The gate
    # in the middle opens only while the ice block sits on the right's pressure plate (no flag: shut again on leaving):
    # Jump and Bee Fly over the platforms, the droplet frozen, the block knocked along with the horn. On the left, the
    # switches hit with the Beemerang, Jump and Bee Fly over platforms, the Shield over the thorns, to the hole down.
    Area("GiantLairBeforeBoss2", "Left Right", (), CanUse("Jump") & CanUse("Bee Fly") & CanUse("Freeze")
         & CanUse("Horn Slash")),
    Area("GiantLairBeforeBoss2", "Left Left", ("loadzoneleft",), CanUse("Beemerang Toss") & CanUse("Jump")
         & CanUse("Bee Fly") & CanUse("Shield"), to="GiantLairBeforeBoss2 (Left Right)"),
)

TRANSFERS = (
    Transfer("lift", "GiantLairFridgeInside", "GiantLairRoachVillage", from_area="Lift", to_area="Fridge Lift"),
    # The roach lift, free down from the village; up from the Entrance only from 586 (its trigger), the village's scene.
    Transfer("lift", "GiantLairRoachVillage", "GiantLairEntrance", two_way=False),
    Transfer("lift", "GiantLairEntrance", "GiantLairRoachVillage", Has(_ROACH_SCENE), two_way=False),
    # The ending: from the Sapling's plains to the city's end.
    Transfer("story", "GiantLairSaplingPlains", "BugariaEndPlaza", LATER_CHAPTERS, two_way=False,
             from_area="Last Fight"),
)

# The hidden switch under the platforms before the boss, hit while flag 41 (the first boss) is set: always on, as in
# vanilla, and it can never set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("GiantLairBeforeBoss2", "platformswitch", 41, ALWAYS_SET),
)

# Amber's shop leaves the Roach Village at 555 (the ending): kept for good, so its checks never go away.
LIMIT_FLAGS = (
    FlagSwap("GiantLairRoachVillage", "amber", 555, -1),
)

# The Kitchen Stove: a trigger by the left door (Event216, until 670) turns the party back right with a line, so the
# left door's side is never a dead end.
KEPT_OPEN = (
    EntityRef("GiantLairBeforeBoss", "event before final"),
)
