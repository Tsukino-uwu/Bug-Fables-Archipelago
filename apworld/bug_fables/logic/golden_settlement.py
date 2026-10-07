"""Golden Settlement (the game's area 6, MapControl.areaid): its spots, and what the seed changes there. Its rooms
aren't mapped yet."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import (Area, DayNight, EntityMove, EntityRef, Give, ItemShop, Location, Pickup, SceneCamera,
                          SceneryMove, Source, StoryEvent, TimeSwitch)

LOCATIONS = (
    # Where the game teaches Beemerang Halt (flag 21), after the mayor's Wacka Worm game at night, played by Vi with
    # the Beemerang (the mod refuses it otherwise).
    Location("Golden Settlement: Farm, Wacka Worm Game (Night)", 68, "GoldenSettlement2",
             Source(event=55, flag=21), rule=CanUse("Beemerang Toss")),
    # Its prize, the Sun Offering (line 45, flag 96), the same game won.
    Location("Golden Settlement: Farm, Wacka Worm Prize (Night)", 152, "GoldenSettlement2",
             Source(flag=96, give=Give(map="GoldenSettlement2", type=1, item=55)), rule=CanUse("Beemerang Toss")),
    # The eating contest at night, entered by talking to Zasp in the square (flag 93): always won (the mod), its prize
    # the Moon Offering (line 67, flag 101) (the user, 2026-10-07).
    Location("Golden Settlement: Farm, Eating Contest (Night)", 153, "GoldenSettlement2",
             Source(flag=101, give=Give(map="GoldenSettlement2", type=1, item=56)), rule=Has("Eating Contest Entered")),
    # Chubee after losing the contest to Leif: the Weak Stomach medal (lines 79-80, flag 102), every time as the contest
    # is always won (the user, 2026-10-07).
    Location("Golden Settlement: Chubee's Gift (Night)", 156, "GoldenSettlement2",
             Source(flag=102, npc="chubee", give=Give(map="GoldenSettlement2", type=2, item=24)),
             rule=Has("Eating Contest Entered")),
    # Crystal berry #4 inside the windmill, open once its crank is turned with the Beemerang Halt, by day and by night.
    Location("Golden Settlement: Farm, Windmill", 154, "GoldenSettlement2",
             Source(berry=4, pickup=Pickup(map="GoldenSettlement2", type=3, item=0)), rule=CanUse("Beemerang Halt"),
             category="crystal_berry", no_jump=True),
    # Crystal berry #7 dug up on the farm, by day only (the user, 2026-10-07): Beetle Dig to reach it and to dig.
    Location("Golden Settlement: Farm, Dig Spot (Day)", 157, "GoldenSettlement2",
             Source(berry=7, pickup=Pickup(map="GoldenSettlement2", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
    # The windmill farmer at night, once the windmill is open: a Hard Seed (line 21, flag 91). He stands there whenever
    # the night is on, so the reward can't be missed.
    Location("Golden Settlement: Farm, Farmer's Reward (Night)", 155, "GoldenSettlement2",
             Source(flag=91, npc="farmer ant night", give=Give(map="GoldenSettlement2", type=0, item=23)),
             rule=CanUse("Beemerang Halt"), no_jump=True),
    # A Lore Book hidden in the grass behind the square's lever platform, by day and by night alike: the horn.
    Location("Golden Settlement: Square, Grass by the Lever", 144, "GoldenSettlement1",
             Source(flag=87, pickup=Pickup(map="GoldenSettlement1", type=1, item=52)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    # The Mothiva Doll inside the Sunset Inn, there from the festival night on (so whenever the square's switch makes it
    # night): Jump.
    Location("Golden Settlement: Square, Sunset Inn (Night)", 145, "GoldenSettlement1",
             Source(flag=106, pickup=Pickup(map="GoldenSettlement1", type=1, item=57)), rule=CanUse("Jump")),
    # The square's shop, open by day and by night: first purchase a check, then its own item.
    *(Location(f"Golden Settlement: Square, Shop {slot}", 146 + slot - 1, "GoldenSettlement1",
               Source(item_shop=ItemShop(map="GoldenSettlement1", keeper="shopkeeper", item=item)),
               category="item_shop", no_jump=True)
      for slot, item in enumerate((17, 48, 1, 12, 23), start=1)),
    # The farm's night scene (Event56, flag 98): the party meets Smugbee, and Kabbu learns Pep Talk, which stays the
    # game's (the user, 2026-10-07: a location, battle skills not items); its trigger only at night.
    Location("Golden Settlement: Farm, Smugbee (Night)", 151, "GoldenSettlement2", Source(event=56, flag=98)),
    # The Energy Converter in the power plant's top area: discovery 23, "The Power Plant" (line 8).
    Location("Golden Settlement: Power Plant, Energy Converter", 158, "PowerPlant", Source(discovery=23),
             category="discovery", no_jump=True, area="Top"),
)
STORY_EVENTS = (
    # The power plant's switches, hit in the right order from its bottom area (Event169, flag 456): the door up opens.
    StoryEvent("Golden Settlement: Power Plant, Door Opened", "Power Plant Door Open", "PowerPlant", Source(flag=456),
               rule=ANY_ATTACK),
    # Talking to Zasp in the square at night enters Leif in the farm's eating contest (flag 93).
    StoryEvent("Golden Settlement: Square, Zasp's Challenge", "Eating Contest Entered", "GoldenSettlement1",
               Source(flag=93)),
)
# The power plant's top area (its door to the Broodmother's lair), behind the door its switches open; the bottom (the
# farm's door, the switches, the save crystal) is the map's own region.
MAP_AREAS = (
    Area("PowerPlant", "Top", ("loadzonebroodmother",), Has("Power Plant Door Open")),
)
# The invisible wall behind the gate to the desert, which stands until the desert side has been reached (flag 170),
# gone. The gate itself stays the game's: shut until its lever is hit from the desert side (the user, 2026-10-07).
SCENERY_HIDDEN = (
    EntityRef("GoldenSettlementEntrance", "Base/Cube"),
    # The square's altar statue over the path to the Golden Hills dungeon, there until the festival's fight (flag 103,
    # Event58), swapped from the start for the moved one (below): the world open, the festival left as it is.
    EntityRef("GoldenSettlement1", "Base/Altar"),
    # The farm's gate to the power plant, there until the Power Plant board quest is taken (flag 226): open from the
    # start, by day and by night, with its door below (the user, 2026-10-07).
    EntityRef("GoldenSettlement2", "Base/WoodenGate2"),
)
SCENERY_PRESENT = (
    EntityRef("GoldenSettlement1", "Base/Altar (1)"),
)
# The night farm's own gate to the power plant, which has no flag (the day farm's goes with flag 226, above).
SCENERY_OFF = (
    EntityRef("GoldenSettlement2Night", "Base/WoodenGate2"),
)
# The festival night (flag 85 until the fight's 86) any time (the user, 2026-10-07): the mod's own night on these maps,
# switched by an NPC in each; the first nightfall is the story's own scene (Event52: its speech, discovery 13, flag 85).
# The square's arrival scene (Event51, flag 84) is skipped if the night comes first; from the farm or the houses the
# first nightfall stays in the room, with the scene's flag and discovery 13 but not its speech (the user, 2026-10-07).
DAY_NIGHT = tuple(DayNight(room, room + "Night", 85, 86, 52, skips=(84,), first_map="GoldenSettlement1",
                           first_discovery=13)
                  for room in ("GoldenSettlement1", "GoldenSettlement2", "GoldenSettlement3"))
# What every switch says (the user, 2026-10-07): by day Aria's own nightfall prompt (GoldenSettlement1 line 19), word
# for word; at night the user's.
_DAY = ("Oh? Are you here for the festival? It should start as soon as the sun sets.", "Keep exploring.",
        "Wait for nightfall.")
_NIGHT = ("Oh? Are you enjoying the festival? It should end at daybreak.", "Keep exploring.", "Wait for dawn.")
# The square's switch: Aria before the festival (her talk the nightfall prompt), moved off the arena (Jump) to the
# ground in front of it, as far from Leif and Celia as on the arena (the user, 2026-10-07).
TIME_SWITCHES = (
    TimeSwitch("GoldenSettlement1", "Aria", (-1.8, 0.0, -1.8), _DAY, _NIGHT),
    # The farm and the houses have no Aria: hers is copied in, where the user chose.
    TimeSwitch("GoldenSettlement2", "Aria", (7.2, 0.0, 5.6), _DAY, _NIGHT, copy=("GoldenSettlement1", "Aria")),
    TimeSwitch("GoldenSettlement3", "Aria", (1.2, 0.0, 4.7), _DAY, _NIGHT, copy=("GoldenSettlement1", "Aria")),
)
# Leif and Celia before the festival, on the arena with Aria for the arrival scene (Event51, which talks from where they
# stand): moved down below the arena's right side, as far apart as they stood up there (the user, 2026-10-07).
ENTITIES_MOVED = (
    EntityMove("GoldenSettlement1", "leif", (2.0, 0.0, -3.0)),
    EntityMove("GoldenSettlement1", "celia", (3.8, 0.0, -1.7)),
    # The scene's trigger, where it was from Aria on the arena.
    EntityMove("GoldenSettlement1", "first event", (0.6, 0.0, -2.45)),
)
# The arrival scene aims its camera at a fixed point on the arena: moved down with them.
SCENE_CAMERAS = (
    SceneCamera("GoldenSettlement1", 51, (0.0, 1.25, 5.0), (0.6, 1.25, -3.6)),
)
# The night square's statue has no flag of its own: slid aside as the fight's scene does (Event58, local z to 0), so
# the dungeon's door never lands behind it at night.
SCENERY_MOVED = (
    SceneryMove("GoldenSettlement1Night", "Base/Altar", (0.0, -26.0, 0.0)),
)
# Each exit between the three rooms is one door entity per festival state, on one spot: the one before the festival kept
# in every state and the night and after-festival copies kept away, so an exit is always the same door (the night
# picks the version of the room it lands in); the square's south door, gone on the night, so open then too, and the
# trigger that turns the party back away (the user, 2026-10-07). The switch Aria, gone from the night on, kept. A night
# map has its day map's entities, so these hold for both (the client reads a night map's under its day map's name).
_DAY_DOORS = {"GoldenSettlement1": ("Loadzonesouth", "loadzonefarm", "loadzonehouses1"),
              "GoldenSettlement2": ("loadzoneplaza",), "GoldenSettlement3": ("loadzone outside",)}
_DOOR_COPIES = {"GoldenSettlement1": ("Loadzonesouth - Duplicate", "loadzonefarmnight", "loadzonefarm - Duplicate",
                                      "loadzonehouses2", "loadzonehouses3"),
                "GoldenSettlement2": ("loadzoneplazanight", "loadzoneplaza - Duplicate"),
                "GoldenSettlement3": ("loadzone outside - Duplicate", "loadzone outside - Duplicate - Duplicate")}
KEPT_PRESENT = (
    *(EntityRef(room, door) for room, doors in _DAY_DOORS.items() for door in doors),
    EntityRef("GoldenSettlement2", "loadzonepp"),
    *(EntityRef(room, "Aria") for room in _DAY_DOORS),
)
KEPT_OPEN = (
    *(EntityRef(room, door) for room, doors in _DOOR_COPIES.items() for door in doors),
    EntityRef("GoldenSettlement1", "blocker"),
)
