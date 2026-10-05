"""The Outskirts: its spots, its door gates, what reaching its spots needs until the rooms are mapped, and what the seed
changes there."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import (ALL_ATTACKS, ANY_ATTACK, BOAT_TICKET, LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY, WHOLE_PARTY,
                            CanUse, one_way)
from ..data_types import (Added, Area, DialogueFlag, EntityRef, Give, ItemEntity, ItemShop, Location, Pickup,
                          Source, Transfer)

# Past the Explorer Permit gate (the "Past the Gate" area): cautious until the rooms past it are measured, every member
# (when members are items) and every move item (when moves are).
PAST_GATE = Has("Explorer Permit") & WHOLE_PARTY & ALL_ATTACKS
DOOR_RULES = ()
# East Road 2's water before its top door: crossed on Icicle platforms either way, or from below only by turning the
# crank with the Beemerang Halt, which leaves the top with no way back but the ice.
EAST2_ICE = CanUse("Icicle") & CanUse("Jump")
EAST2_UP = EAST2_ICE | one_way(CanUse("Beemerang Halt"), EAST2_ICE)
# GoldenPathTunnel2's climb from its bottom door to its top one: Icicle, the Horn Dash, Bee Fly, Jump, and an attack for
# its lever.
TUNNEL2_UP = CanUse("Jump") & CanUse("Icicle") & CanUse("Horn Dash") & CanUse("Bee Fly") & ANY_ATTACK
# The Golden Path tunnel's way up: ice frozen, knocked into place with the horn, then jumped on (the vanilla way;
# Freeze alone is a harder jump the logic doesn't count, the user, 2026-10-05).
ICE_CLIMB = CanUse("Freeze") & CanUse("Horn Slash") & CanUse("Jump")
LOCATIONS = (
    Location("Outskirts: Maki and Eetl's Gift", 1, "BugariaOutskirtsOutsideCity",
             Source(event=16, flag=15, give=Give(map="BugariaOutskirtsOutsideCity", type=1, item=27)), quiet=True,
             no_jump=True),
    # The horn tutorial: the scene cuts the grass itself, so it needs no member. The room needs nothing (seen); reach
    # stands in for the corridors around it, not yet mapped.
    Location("Outskirts: Near Snakemouth Den, Horn Tutorial", 2, "NearSnakemouth",
             Source(event=10, flag=17, give=Give(map="NearSnakemouth", type=-1, item=10)), reach=PAST_GATE),
    Location("Outskirts: Artis's Gift", 3, "BugariaOutskirtsOutsideCity",
             Source(npc="ShwEmArtys", flag=32, give=Give(map="BugariaOutskirtsOutsideCity", type=2, item=11))),
    # Cut grass copies its own one-time flag onto the item it drops, so this is an ordinary pickup. Only Kabbu's horn
    # cuts it.
    Location("Outskirts: Golden Path, Grass by the Dirt Spot", 12, "BOGoldenPath",
             Source(flag=74, pickup=Pickup(map="BOGoldenPath", type=0, item=2)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    # Crystal berry #6, dug up in a mound on the right side, flown to with Vi.
    Location("Outskirts: Golden Path, Dig Spot", 87, "BOGoldenPath",
             Source(berry=6, pickup=Pickup(map="BOGoldenPath", type=3, item=0)),
             rule=CanUse("Bee Fly") & CanUse("Beetle Dig"), category="crystal_berry", no_jump=True, area="Right"),
    # The first boss's prize medal: the mod pays prizes as if Hard Mode were on, so it waits at Artis.
    Location("Outskirts: Artis's Prize for Snakemouth Den", 13, "BugariaOutskirtsOutsideCity",
             Source(event=33, var=13, at_least=3, give=Give(map="BugariaOutskirtsOutsideCity", type=2, item=5)),
             rule=Has("Snakemouth Den Cleared")),
    # No gate of its own: in the game only the Outskirts rocks, removed by the seed, keep it out of reach.
    Location("Outskirts: Ladybug Siblings' House", 14, "BugariaOutskirtsOutsideCity",
             Source(flag=679, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=8)), no_jump=True),
    # Crystal berry #0, in the middle of the room outside the cave (its areas below).
    Location("Outskirts: Snakemouth Den Entrance, by the Cave", 19, "OutsideSnakemouth",
             Source(berry=0, pickup=Pickup(map="OutsideSnakemouth", type=3, item=0)),
             category="crystal_berry", no_jump=True, reach=PAST_GATE),
    # A Drowsy Cake under a stone, knocked loose with Kabbu's horn.
    Location("Outskirts: East Road, Boulder", 25, "BugariaOutskirtsEast1",
             Source(flag=735, pickup=Pickup(map="BugariaOutskirtsEast1", type=0, item=147)),
             rule=CanUse("Horn Slash") & CanUse("Jump"), category="hidden_item"),
    # A Dark Cherry dug up across the water: Icicle platforms and Jump, there and back.
    Location("Outskirts: East Road, Dig Spot", 88, "BugariaOutskirtsEast1",
             Source(flag=633, pickup=Pickup(map="BugariaOutskirtsEast1", type=0, item=121)),
             rule=CanUse("Icicle") & CanUse("Jump") & CanUse("Beetle Dig"), category="dig_spot"),
    # The HP Plus medal, hidden inside the waterfall on the lower ground: Icicle, then the Beemerang to grab it.
    Location("Outskirts: East Road, Inside the Waterfall", 89, "BugariaOutskirtsEast1",
             Source(flag=137, pickup=Pickup(map="BugariaOutskirtsEast1", type=2, item=0)),
             rule=CanUse("Icicle") & CanUse("Beemerang Toss"), category="hidden_item", no_jump=True, area="Lower"),
    # A Tangy Berry dug up across the water: Icicle platforms and Jump, there and back.
    Location("Outskirts: East Road to the Pier, Dig Spot", 90, "BugariaOutskirtsEast2",
             Source(flag=669, pickup=Pickup(map="BugariaOutskirtsEast2", type=0, item=77)),
             rule=CanUse("Icicle") & CanUse("Jump") & CanUse("Beetle Dig"), category="dig_spot"),
    # Crystal berry #10, on the pier by the boat.
    Location("Outskirts: Pier, Behind the Dock", 26, "BugariaPier",
             Source(berry=10, pickup=Pickup(map="BugariaPier", type=3, item=0)), rule=CanUse("Jump"),
             category="crystal_berry"),
    Location("Outskirts: Pier, Ship's Wheel", 27, "BugariaPier",
             Source(discovery=49), category="discovery", no_jump=True),
    # Recorded by the map's auto-start scene on arriving through either door; placed in the middle, the cautious way.
    Location("Outskirts: Snakemouth Den Entrance, Arrival", 28, "OutsideSnakemouth",
             Source(discovery=0), category="discovery", no_jump=True, reach=PAST_GATE),
    # A dig spot copies its own one-time flag onto the item it digs up, as cut grass does: an ordinary pickup.
    Location("Outskirts: Snakemouth Den Entrance, Dig Spot", 79, "OutsideSnakemouth",
             Source(flag=683, pickup=Pickup(map="OutsideSnakemouth", type=0, item=2)), rule=CanUse("Beetle Dig"),
             category="dig_spot", no_jump=True, reach=PAST_GATE),
    # Respawning pickups in grass: hidden only by a regional flag, so they come back; the game's own again once checked.
    # Only Kabbu's horn cuts grass.
    Location("Outskirts: Snakemouth Den Path, Grass on the Left", 80, "BugariaOutskirtsSnakemouthCorridor2",
             Source(regional=13, pickup=Pickup(map="BugariaOutskirtsSnakemouthCorridor2", type=0, item=0)),
             rule=CanUse("Horn Slash"), category="hidden_item", no_jump=True, reach=PAST_GATE),
    # Crystal berry #3, behind Chuck's house, past a big rock only the Horn Dash breaks.
    Location("Outskirts: Chuck's Abode, Behind the House", 82, "ChucksAbode",
             Source(berry=3, pickup=Pickup(map="ChucksAbode", type=3, item=0)), rule=CanUse("Horn Dash"),
             category="crystal_berry", no_jump=True, reach=PAST_GATE),
    # The Golden Path tunnel (its areas below).
    Location("Outskirts: Golden Path Tunnel, On Top of the Stump", 83, "GoldenPathTunnel",
             Source(flag=725, pickup=Pickup(map="GoldenPathTunnel", type=0, item=88)),
             rule=CanUse("Jump") & CanUse("Beemerang Toss"), area="Top Right"),
    # Dottle's ball, a key item.
    Location("Outskirts: Golden Path Tunnel, Behind the Boulder", 84, "GoldenPathTunnel",
             Source(flag=82, pickup=Pickup(map="GoldenPathTunnel", type=1, item=24)), no_jump=True),
    # The Life Cast. Its flag is shared with a second spot in GoldenPathTunnel2 (one medal, two spots), which the mod,
    # matching pickups by map, doesn't swap yet.
    Location("Outskirts: Golden Path Tunnel, Upper Ledge", 85, "GoldenPathTunnel",
             Source(flag=462, pickup=Pickup(map="GoldenPathTunnel", type=2, item=72)), no_jump=True, area="Upper Left"),
    # In a hidden room, walked into through the wall between the two doors on the right.
    Location("Outskirts: Golden Path Tunnel, Hidden Room Dig Spot", 86, "GoldenPathTunnel",
             Source(flag=488, pickup=Pickup(map="GoldenPathTunnel", type=1, item=52)), rule=CanUse("Beetle Dig"),
             category="dig_spot", no_jump=True),
    Location("Outskirts: Near Snakemouth Den, Grass by the Cave Door", 81, "NearSnakemouth",
             Source(regional=7, pickup=Pickup(map="NearSnakemouth", type=0, item=1)),
             rule=CanUse("Horn Slash"), category="hidden_item", no_jump=True, reach=PAST_GATE),
    # Crystal berry #30, dug up on a raised spot outside the city (Jump).
    Location("Outskirts: Outside the City, Dig Spot", 91, "BugariaOutskirtsOutsideCity",
             Source(berry=30, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=3, item=0)),
             rule=CanUse("Jump") & CanUse("Beetle Dig"), category="crystal_berry"),
    # A Dark Cherry dug up below the house.
    Location("Outskirts: Outside the City, Dig Spot Below the House", 92, "BugariaOutskirtsOutsideCity",
             Source(flag=642, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=121)),
             rule=CanUse("Beetle Dig"), category="dig_spot", no_jump=True),
    # A Danger Spud behind a fence: Beetle Dig goes under the fence, then digs it up.
    Location("Outskirts: Outside the City, Dig Spot Behind the Fence", 93, "BugariaOutskirtsOutsideCity",
             Source(flag=487, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=64)),
             rule=CanUse("Beetle Dig"), category="dig_spot", no_jump=True),
    # A Burly Tea on the right of the table by the painting; the house is open from the start.
    Location("Outskirts: Madeleine's House, Table Right", 44, "BugariaOutskirtsOutsideCity",
             Source(flag=686, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=81))),
    # A Lore Book on the left of the same table.
    Location("Outskirts: Madeleine's House, Table Left", 45, "BugariaOutskirtsOutsideCity",
             Source(flag=392, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=1, item=52))),
    # The caravan's shop, there from the start (in the game it comes after the first boss); first purchase a check, then
    # its own item.
    Location("Outskirts: Caravan, Item Shop 1", 63, "BugariaOutskirtsOutsideCity",
             Source(item_shop=ItemShop(map="BugariaOutskirtsOutsideCity", keeper="Crickerly2", item=2)),
             category="item_shop", no_jump=True),
    Location("Outskirts: Caravan, Item Shop 2", 64, "BugariaOutskirtsOutsideCity",
             Source(item_shop=ItemShop(map="BugariaOutskirtsOutsideCity", keeper="Crickerly2", item=3)),
             category="item_shop", no_jump=True),
    Location("Outskirts: Caravan, Item Shop 3", 65, "BugariaOutskirtsOutsideCity",
             Source(item_shop=ItemShop(map="BugariaOutskirtsOutsideCity", keeper="Crickerly2", item=11)),
             category="item_shop", no_jump=True),
    # Where the story's second member joins in the opening (Event16), whoever starts.
    Location("Outskirts: Outside the City, Opening", 66, "BugariaOutskirtsOutsideCity",
             Source(event=16, flag=15), category="party_member", quiet=True, no_jump=True),
    # The opening puts a Crunchy Leaf in the bag for its tutorial battle (Event16, items[0].Add(0), never taken back);
    # the mod's opening skip does the same unless this is a location.
    Location("Outskirts: Outside the City, Tutorial Battle", 75, "BugariaOutskirtsOutsideCity",
             Source(event=16, flag=15, added=Added(type=0, item=0)), quiet=True, no_jump=True),
)
TRANSFERS = (
    # The sailor sails only for the Boat Ticket (with Progressive Boat on, its first copy), from the dock, where the
    # boat back lands too.
    Transfer("boat", "BugariaPier", "MetalIsland1", BOAT_TICKET, from_area="Dock"),
    # Every dock takes the submarine (and the Boat Ticket, inside the later chapters' stand-in).
    Transfer("submarine", "BugariaPier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
)
# The submarine's docks exist with its key item in the bag, whatever the story's flags (448 here).
PRESENT_WITH_ITEM = (
    ItemEntity("BugariaPier", "Fixedsub - Duplicate", SUBMARINE_KEY),
)
KEPT_OPEN = (
    # The locked-door check at Madeleine's house; the house is open from the start.
    EntityRef("BugariaOutskirtsOutsideCity", "lockeddoor"),
    # The town's arrival scene: it lines up a companion who only joins after the first boss, so the real door is kept
    # present instead.
    EntityRef("BugariaOutskirtsOutsideCity", "DoorBugaria - Duplicate"),
    # A miner at the Outskirts rocks, gone with them.
    EntityRef("BugariaOutskirtsOutsideCity", "MiningAnt"),
    # His fellow miner.
    EntityRef("BugariaOutskirtsOutsideCity", "MinerAntWalk"),
    # Eetl turns the party back from the first boss until chapter 2, cutting off Snakemouth Den.
    EntityRef("BugariaOutskirtsOutsideCity", "eetlblocker1 - Duplicate"),
    # The same scene's second trigger, from flag 114 (Eetl following the party, Event63) until 67.
    EntityRef("BugariaOutskirtsOutsideCity", "eetlblocker1"),
    # The Golden Path's blocker (Event12) until chapter 2: it spans the path to the Hermit's cave too, not only the
    # tunnel, which needs flag 67 of its own.
    EntityRef("BOGoldenPath", "blocker"),
    # The save tutorial (Event19): its actors leave at the first boss, its trigger only at its own flag 30, so a file
    # that skipped the opening walked into it with no one there.
    EntityRef("BugariaOutskirtsOutsideCity", "SaveEventTrigger"),
    # Turns the party back from Chuck's Abode until the first boss ('the cave first'); nothing inside waits on the
    # story.
    EntityRef("NearSnakemouth", "BlockLeft"),
    # Turns the party back from the shortcut to the first corridor until the first boss.
    EntityRef("NearSnakemouth", "BlockRight"),
    # The guard at the gate before the cave (from chapter 2, flag 67), and his sign: away all game, as the gate is.
    EntityRef("NearSnakemouth", "guard"),
    EntityRef("NearSnakemouth", "sign"),
    # The guard who keeps the gate to the Lost Sands shut until chapter 3 (flag 130): the gate is open from the start.
    EntityRef("BOLostSandsEntrance", "antguardclosed"),
    # The Crickerly who stands there before the caravan opens.
    EntityRef("BugariaOutskirtsOutsideCity", "Crickerly1"),
    # A moth waiting for the rocks to be cleared; gone with the rocks.
    EntityRef("BugariaOutskirtsOutsideCity", "FuzzyMoth"),
)
KEPT_PRESENT = (
    # The gatekeeper who opens the Snakemouth Den gate for the Explorer Permit; he leaves at the spider scene (flag 27),
    # which a file that got past the gate another way can reach first.
    EntityRef("BugariaOutskirtsOutsideCity", "FxdColGatekeeper"),
    # The door into Madeleine's house; she and her butler stay away, so none of her story starts early.
    EntityRef("BugariaOutskirtsOutsideCity", "doormadeleine"),
    # The Outskirts quest board, likewise.
    EntityRef("BugariaOutskirtsOutsideCity", "QuestBoard"),
    # The real door into the city, which the game makes only after the arrival scene (removed).
    EntityRef("BugariaOutskirtsOutsideCity", "DoorBugaria"),
    # The Golden Path door, made only after the first boss (flag 41); past it the tunnel waits for chapter 2 (67).
    EntityRef("BugariaOutskirtsOutsideCity", "LoadZoneGoldenPath"),
    # The Golden Path's tunnel onward, made only from chapter 2 (flag 67) (the user, 2026-10-04: open from the start).
    EntityRef("BOGoldenPath", "Loadzonetunnel"),
    # The way into Chuck's Abode, made only after the first boss.
    EntityRef("NearSnakemouth", "loadingzonechuck"),
    # The shortcut back to the first corridor, made only after the first boss.
    EntityRef("NearSnakemouth", "loadingzonefields"),
    # The same guard stood aside by the open gate, as from flag 130.
    EntityRef("BOLostSandsEntrance", "antguardopen"),
    # The caravan's shopkeeper, made while the map builds its entities so the game builds its shop slots.
    EntityRef("BugariaOutskirtsOutsideCity", "Crickerly2"),
    # A ladybug sibling outside their house; their other lines answer to the lost-brother quest's own flags.
    EntityRef("BugariaOutskirtsOutsideCity", "LaydbugGirl"),
    # The other ladybug sibling.
    EntityRef("BugariaOutskirtsOutsideCity", "LaydbugBoy"),
)
SCENERY_HIDDEN = (
    # The lock on Madeleine's house.
    EntityRef("BugariaOutskirtsOutsideCity", "Base/lock (1)"),
    # The rock pile that cuts the Outskirts off until the first boss; the Golden Path exit behind it still waits for the
    # boss on its own.
    EntityRef("BugariaOutskirtsOutsideCity", "Base/BlockingRocks"),
    # The Snakemouth Den gate closed again from the first boss (41) until chapter 2 (67); the Explorer Permit's own gate
    # (until flag 28) stays.
    EntityRef("BugariaOutskirtsOutsideCity", "Base/Gate/SnekGate (1)"),
    # The gate before the cave that the game puts up from chapter 2 (flag 67) for good. Never up in a seed: the horn
    # tutorial's scene moves the party past it (the user, 2026-10-05).
    EntityRef("NearSnakemouth", "map1v4 (1)/snakemouthgate"),
    EntityRef("NearSnakemouth", "map1v4 (1)/snakemouthgate/Gate"),
    # The closed gate to the Lost Sands (hidden from flag 130).
    EntityRef("BOLostSandsEntrance", "Base/WoodenGate2"),
)
MAP_AREAS = (
    # Behind the Explorer Permit gate: only the door to Snakemouth Den's corridor. Arriving through it, the game walks
    # the party into the closed gate and then moves it past: a one-way out, the permit its way back.
    Area("BugariaOutskirtsOutsideCity", "Past the Gate", ("DoorSnakemouth",), Has("Explorer Permit"),
         out=one_way(None, Has("Explorer Permit"))),
    # Outside the cave: the door into the den stands on a small ledge, dropped from freely, climbed only with Jump.
    Area("OutsideSnakemouth", "Cave Ledge", ("LoadingZoneInside",), CanUse("Jump"),
         out=one_way(None, CanUse("Jump"))),
    # The corridor's side, past the grass: Kabbu's horn cuts through, both ways.
    Area("OutsideSnakemouth", "Corridor Side", ("loading zone outside",), CanUse("Horn Slash")),
    # The first corridor: two gaps and two ledges between its two ends, crossed only with Jump, both ways.
    Area("BugariaOutskitsSnakemouthCorridor1", "Left", ("DoorSnakemouth",), CanUse("Jump")),
    # Its door to Seedling Haven, across water: Jump and Leif's Icicle, both ways.
    Area("BugariaOutskitsSnakemouthCorridor1", "Haven Door", ("loadzonehaven",), CanUse("Jump") & CanUse("Icicle")),
    # The second corridor: grass across the middle, cut with Kabbu's horn, both ways.
    Area("BugariaOutskirtsSnakemouthCorridor2", "Left", ("DoorSnakemouth",), CanUse("Horn Slash")),
    # The Golden Path: its Outskirts door up ledges from the middle (Jump), dropped down from.
    Area("BOGoldenPath", "Right", ("LoadZoneBugaria",), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
    # Its door to the Hermit's cave, across water: Icicle platforms and Jump, both ways.
    Area("BOGoldenPath", "Left", ("loadzonecave",), CanUse("Icicle") & CanUse("Jump")),
    # The first East Road: its right door past a gap (Jump), its lower ground down ledges (a drop; Jump back up), and
    # the Cave of Trials' door down there inside grass (the horn, both ways).
    Area("BugariaOutskirtsEast1", "Right", ("loadzone right",), CanUse("Jump")),
    Area("BugariaOutskirtsEast1", "Lower", (), one_way(None, CanUse("Jump")), out=CanUse("Jump")),
    Area("BugariaOutskirtsEast1", "Cave Door", ("loadzonecave",), CanUse("Horn Slash"),
         to="BugariaOutskirtsEast1 (Lower)"),
    # The second East Road's top door, across water from the rest (EAST2_UP; back down only on the ice).
    Area("BugariaOutskirtsEast2", "Top", ("loadzonenorth",), EAST2_UP, out=EAST2_ICE),
    # GoldenPathTunnel2's top, with its door down to the tunnel's upper ledge (its two doors share a name: by index).
    Area("GoldenPathTunnel2", "Top", ("loadzonebarrenlands - Duplicate#9",), TUNNEL2_UP,
         out=one_way(None, TUNNEL2_UP)),
    # The pier's dock, where the boat leaves and lands: up a ledge (Jump), dropped from through the house.
    Area("BugariaPier", "Dock", (), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
    # The Golden Path tunnel: the Forsaken Lands' door past a big boulder only the Horn Dash breaks, both ways.
    Area("GoldenPathTunnel", "Left", ("loadzonebarrenlands",), CanUse("Horn Dash")),
    # The Golden Hills' door up top: ice frozen, knocked into place with the horn, and jumped on; dropped down from.
    Area("GoldenPathTunnel", "Top Right", ("loadzonegoldenhills",), ICE_CLIMB, out=one_way(None, ICE_CLIMB)),
    # Tunnel2's door, high on the left: no way up from inside the room, only a drop to the left part, never back.
    Area("GoldenPathTunnel", "Upper Left", ("loadzonegpt2",), False_(), out=one_way(None, False_()),
         to="GoldenPathTunnel (Left)"),
)
SCENERY_PRESENT = (
    # The caravan's stall.
    EntityRef("BugariaOutskirtsOutsideCity", "Base/Stall"),
    # The open gate to the Lost Sands (shown from flag 130).
    EntityRef("BOLostSandsEntrance", "Base/WoodenGate2 (2)"),
)
DIALOGUE_FLAGS = (
    # The caravan husband's welcome and sell menu; before the first boss he only talks about the rocks.
    DialogueFlag("BugariaOutskirtsOutsideCity", "CHusband", 41, 691),
)
