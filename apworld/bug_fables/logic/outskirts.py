"""The Outskirts: its spots, its door gates, what reaching its spots needs until the rooms are mapped, and what the seed
changes there."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import (ALL_ATTACKS, BOAT_TICKET, LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY, WHOLE_PARTY,
                            CanUse)
from ..data_types import (Added, DialogueFlag, EntityRef, Give, ItemEntity, ItemShop, Location, Pickup,
                          Source, Transfer)

# Past the Explorer Permit gate (inside the Outskirts map, so not a door): cautious until the rooms past it are
# measured, every member (when members are items) and every move item (when moves are).
PAST_GATE = Has("Explorer Permit") & WHOLE_PARTY & ALL_ATTACKS
DOOR_RULES = ()
LOCATIONS = (
    Location("Outskirts: Maki and Eetl's Gift", 1, "BugariaOutskirtsOutsideCity",
             Source(event=16, flag=15, give=Give(map="BugariaOutskirtsOutsideCity", type=1, item=27)), quiet=True,
             no_jump=True),
    # The horn tutorial: the scene cuts the grass itself, so it needs no member.
    Location("Outskirts: Near Snakemouth Den, Horn Tutorial", 2, "NearSnakemouth",
             Source(event=10, flag=17, give=Give(map="NearSnakemouth", type=-1, item=10)), reach=PAST_GATE),
    Location("Outskirts: Artis's Gift", 3, "BugariaOutskirtsOutsideCity",
             Source(npc="ShwEmArtys", flag=32, give=Give(map="BugariaOutskirtsOutsideCity", type=2, item=11))),
    # Cut grass copies its own one-time flag onto the item it drops, so this is an ordinary pickup. Only Kabbu's horn
    # cuts it.
    Location("Outskirts: Golden Path, Grass by the Dirt Spot", 12, "BOGoldenPath",
             Source(flag=74, pickup=Pickup(map="BOGoldenPath", type=0, item=2)), rule=CanUse("Horn Slash")),
    # The first boss's prize medal: the mod pays prizes as if Hard Mode were on, so it waits at Artis.
    Location("Outskirts: Artis's Prize for Snakemouth Den", 13, "BugariaOutskirtsOutsideCity",
             Source(event=33, var=13, at_least=3, give=Give(map="BugariaOutskirtsOutsideCity", type=2, item=5)),
             rule=Has("Snakemouth Den Cleared")),
    # No gate of its own: in the game only the Outskirts rocks, removed by the seed, keep it out of reach.
    Location("Outskirts: Ladybug Siblings' House", 14, "BugariaOutskirtsOutsideCity",
             Source(flag=679, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=8)), no_jump=True),
    # Crystal berry #0, outside the cave: behind grass from the Outskirts' side (the horn), open from the cave's side,
    # which room-level logic will count.
    Location("Outskirts: Snakemouth Den Entrance, by the Cave", 19, "OutsideSnakemouth",
             Source(berry=0, pickup=Pickup(map="OutsideSnakemouth", type=3, item=0)),
             rule=CanUse("Horn Slash"),
             category="crystal_berry", reach=PAST_GATE),
    # A Drowsy Cake under a stone, knocked loose with Kabbu's horn.
    Location("Outskirts: East Road, Boulder", 25, "BugariaOutskirtsEast1",
             Source(flag=735, pickup=Pickup(map="BugariaOutskirtsEast1", type=0, item=147)),
             rule=CanUse("Horn Slash")),
    # Crystal berry #10, on the pier by the boat.
    Location("Outskirts: Pier, Behind the Dock", 26, "BugariaPier",
             Source(berry=10, pickup=Pickup(map="BugariaPier", type=3, item=0)), category="crystal_berry"),
    Location("Outskirts: Pier, Ship's Wheel", 27, "BugariaPier",
             Source(discovery=49), category="discovery"),
    # Recorded by the scene on first arriving outside Snakemouth Den.
    Location("Outskirts: Snakemouth Den Entrance, Arrival", 28, "OutsideSnakemouth",
             Source(discovery=0), category="discovery", reach=PAST_GATE),
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
    # The sailor sails only for the Boat Ticket (with Progressive Boat on, its first copy).
    Transfer("boat", "BugariaPier", "MetalIsland1", BOAT_TICKET),
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
    # The guard who closes the way to the cave from chapter 2 (flag 67) on, for good.
    EntityRef("NearSnakemouth", "guard"),
    # His sign.
    EntityRef("NearSnakemouth", "sign"),
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
    # The gate by the cave that closes it from chapter 2 on, and its door.
    EntityRef("NearSnakemouth", "map1v4 (1)/snakemouthgate"),
    EntityRef("NearSnakemouth", "map1v4 (1)/snakemouthgate/Gate"),
)
SCENERY_PRESENT = (
    # The caravan's stall.
    EntityRef("BugariaOutskirtsOutsideCity", "Base/Stall"),
)
DIALOGUE_FLAGS = (
    # The caravan husband's welcome and sell menu; before the first boss he only talks about the rocks.
    DialogueFlag("BugariaOutskirtsOutsideCity", "CHusband", 41, 691),
)
