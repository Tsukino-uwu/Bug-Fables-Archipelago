"""The Outskirts: its regions and the ways out of them, its spots, and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ALL_ATTACKS, WHOLE_PARTY, CanUse
from ..data_types import Added, DialogueFlag, EntityRef, Exit, Give, ItemShop, Location, Pickup, Region, Source

REGIONS = (
    Region("Bugaria Outskirts", exits=(
        # Cautious until the rooms past the gate are measured: every member (when members are items) and every move item
        # (when moves are).
        Exit("Past the Outskirts Gate", Has("Explorer Permit") & WHOLE_PARTY & ALL_ATTACKS),
        Exit("Golden Path", Has("Snakemouth Den Cleared")),
        Exit("Bugaria City"),
        Exit("Metal Island", Has("Boat Ticket")),
    )),
    Region("Past the Outskirts Gate", exits=(
        # Grass in the second corridor and outside the cave, then the door room's horn puzzle down the trapdoor.
        Exit("Snakemouth Den", CanUse("Horn Slash")),
    )),
    Region("Golden Path"),
)
LOCATIONS = (
    Location("Outskirts: Maki and Eetl's Gift", 1, "Bugaria Outskirts",
             Source(event=16, flag=15, give=Give(map="BugariaOutskirtsOutsideCity", type=1, item=27)), quiet=True,
             no_jump=True),
    # The horn tutorial: the scene cuts the grass itself, so it needs no member.
    Location("Outskirts: Near Snakemouth Den, Reward", 2, "Past the Outskirts Gate",
             Source(event=10, flag=17, give=Give(map="NearSnakemouth", type=-1, item=10))),
    Location("Outskirts: Artis's Gift", 3, "Bugaria Outskirts",
             Source(npc="ShwEmArtys", flag=32, give=Give(map="BugariaOutskirtsOutsideCity", type=2, item=11))),
    # Cut grass copies its own one-time flag onto the item it drops, so this is an ordinary pickup.
    Location("Outskirts: Golden Path, Grass", 12, "Golden Path",
             Source(flag=74, pickup=Pickup(map="BOGoldenPath", type=0, item=2))),
    # The first boss's prize medal: the mod pays prizes as if Hard Mode were on, so it waits at Artis.
    Location("Outskirts: Artis's Prize for Snakemouth Den", 13, "Bugaria Outskirts",
             Source(event=33, var=13, at_least=3, give=Give(map="BugariaOutskirtsOutsideCity", type=2, item=5)),
             rule=Has("Snakemouth Den Cleared")),
    # No gate of its own: in the game only the Outskirts rocks, removed by the seed, keep it out of reach.
    Location("Outskirts: Ladybug Siblings' House", 14, "Bugaria Outskirts",
             Source(flag=679, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=8)), no_jump=True),
    # Crystal berry #0, outside the cave: behind grass from the Outskirts' side (the horn), open from the cave's side,
    # which room-level logic will count.
    Location("Outskirts: Snakemouth Den Entrance", 19, "Past the Outskirts Gate",
             Source(berry=0, pickup=Pickup(map="OutsideSnakemouth", type=3, item=0)),
             rule=CanUse("Horn Slash"),
             category="crystal_berry"),
    # A Drowsy Cake under a stone, knocked loose with Kabbu's horn.
    Location("Outskirts: East Road, Stone", 25, "Bugaria Outskirts",
             Source(flag=735, pickup=Pickup(map="BugariaOutskirtsEast1", type=0, item=147)),
             rule=CanUse("Horn Slash")),
    # Crystal berry #10, on the pier by the boat.
    Location("Outskirts: Pier", 26, "Bugaria Outskirts",
             Source(berry=10, pickup=Pickup(map="BugariaPier", type=3, item=0)), category="crystal_berry"),
    Location("Outskirts: Pier, Statue", 27, "Bugaria Outskirts",
             Source(discovery=49), category="discovery"),
    # Recorded by the scene on first arriving outside Snakemouth Den.
    Location("Outskirts: Snakemouth Den Entrance, Arrival", 28, "Past the Outskirts Gate",
             Source(discovery=0), category="discovery"),
    # A Burly Tea on the right of the table by the painting; the house is open from the start.
    Location("Outskirts: Madeleine's House, Table Right", 44, "Bugaria Outskirts",
             Source(flag=686, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=0, item=81))),
    # A Lore Book on the left of the same table.
    Location("Outskirts: Madeleine's House, Table Left", 45, "Bugaria Outskirts",
             Source(flag=392, pickup=Pickup(map="BugariaOutskirtsOutsideCity", type=1, item=52))),
    # The caravan's shop, there from the start (in the game it comes after the first boss); first purchase a check, then
    # its own item.
    Location("Outskirts: Caravan, Item Shop 1", 63, "Bugaria Outskirts",
             Source(item_shop=ItemShop(map="BugariaOutskirtsOutsideCity", keeper="Crickerly2", item=2)),
             category="item_shop", no_jump=True),
    Location("Outskirts: Caravan, Item Shop 2", 64, "Bugaria Outskirts",
             Source(item_shop=ItemShop(map="BugariaOutskirtsOutsideCity", keeper="Crickerly2", item=3)),
             category="item_shop", no_jump=True),
    Location("Outskirts: Caravan, Item Shop 3", 65, "Bugaria Outskirts",
             Source(item_shop=ItemShop(map="BugariaOutskirtsOutsideCity", keeper="Crickerly2", item=11)),
             category="item_shop", no_jump=True),
    # Where the story's second member joins in the opening (Event16), whoever starts.
    Location("Outskirts: Outside the City, Opening", 66, "Bugaria Outskirts",
             Source(event=16, flag=15), category="party_member", quiet=True, no_jump=True),
    # The opening puts a Crunchy Leaf in the bag for its tutorial battle (Event16, items[0].Add(0), never taken back);
    # the mod's opening skip does the same unless this is a location.
    Location("Outskirts: Outside the City, Tutorial Battle", 75, "Bugaria Outskirts",
             Source(event=16, flag=15, added=Added(type=0, item=0)), quiet=True, no_jump=True),
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
    # Turns the party back from Chuck's Abode until the first boss ('the cave first'); nothing inside waits on the
    # story.
    EntityRef("NearSnakemouth", "BlockLeft"),
    # Turns the party back from the shortcut to the first corridor until the first boss.
    EntityRef("NearSnakemouth", "BlockRight"),
    # The Crickerly who stands there before the caravan opens.
    EntityRef("BugariaOutskirtsOutsideCity", "Crickerly1"),
    # A moth waiting for the rocks to be cleared; gone with the rocks.
    EntityRef("BugariaOutskirtsOutsideCity", "FuzzyMoth"),
)
KEPT_PRESENT = (
    # The door into Madeleine's house; she and her butler stay away, so none of her story starts early.
    EntityRef("BugariaOutskirtsOutsideCity", "doormadeleine"),
    # The Outskirts quest board, likewise.
    EntityRef("BugariaOutskirtsOutsideCity", "QuestBoard"),
    # The real door into the city, which the game makes only after the arrival scene (removed).
    EntityRef("BugariaOutskirtsOutsideCity", "DoorBugaria"),
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
)
SCENERY_PRESENT = (
    # The caravan's stall.
    EntityRef("BugariaOutskirtsOutsideCity", "Base/Stall"),
)
DIALOGUE_FLAGS = (
    # The caravan husband's welcome and sell menu; before the first boss he only talks about the rocks.
    DialogueFlag("BugariaOutskirtsOutsideCity", "CHusband", 41, 691),
)
