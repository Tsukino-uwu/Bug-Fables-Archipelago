"""Bugaria City (the palace's rooms included): its spots and story events, its door gates, what reaching its spots needs
until the rooms are mapped, and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, CanUse
from ..data_types import (Area, DialogueFlag, EntityRef, FlagEntity, Give, ItemShop, Location, Pickup, Source,
                          StoryEvent, Transfer)

DOOR_RULES = (
    # The palace hall's doors (the plaza's, the mine's, the library's and the war room's) are all kept present by the
    # seed (below).
)
TRANSFERS = (
    # Down to the underground bar by talking to someone in the commercial district (Event61), open from the start (the
    # line is repointed below), behind the grass; the way back up is a door.
    Transfer("way down", "BugariaCommercial", "UndergroundBar", two_way=False, from_area="Bar Corner"),
    # The tunnel's prison door needs flag 79, set only inside the prison, which the sub alone reaches before it.
    Transfer("ant tunnel", "AntTunnels", "RubberPrisonGiantLairBridge", LATER_CHAPTERS & SUBMARINE),
    Transfer("ant tunnel", "AntTunnels", "MetalIsland2", LATER_CHAPTERS),
    # Each far end's miner opens its shortcut to the tunnel hub, for free in a seed (free_ant_tunnels); the hub's way
    # back out needs that end's flag, set only by having been there, so the way out adds nothing and isn't listed.
    Transfer("ant tunnel", "GoldenSettlementEntrance", "AntTunnels", two_way=False),
    Transfer("ant tunnel", "DefiantRoot2", "AntTunnels", two_way=False),
    Transfer("ant tunnel", "BarrenLandsAntTunnel", "AntTunnels", two_way=False),
    Transfer("ant tunnel", "FGCave", "AntTunnels", two_way=False),
    # Chapter 3's end, back in the palace after the attack on the city.
    Transfer("story", "BugariaCastleAttack", "AntPalace2", LATER_CHAPTERS, two_way=False),
    # The ending: the city's end, the throne, and back to the plaza.
    Transfer("story", "BugariaEndBridge", "BugariaEndThrone", LATER_CHAPTERS, two_way=False),
    Transfer("story", "BugariaEndThrone", "BugariaMainPlaza", LATER_CHAPTERS, two_way=False),
)
LOCATIONS = (
    # Charge Up on the red house's roof: the Flower Key opens the house (flag 229), its bounce pads need no Jump.
    Location("Bugaria City: Main Plaza, Red House Rooftop", 95, "BugariaMainPlaza",
             Source(flag=230, pickup=Pickup(map="BugariaMainPlaza", type=2, item=52)), rule=Has("Flower Key"),
             no_jump=True),
    # Crystal berry #29, dug up from the mound at the top of the plaza.
    Location("Bugaria City: Main Plaza, Dig Spot", 96, "BugariaMainPlaza",
             Source(berry=29, pickup=Pickup(map="BugariaMainPlaza", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
    # A Lore Book hidden behind a bookshelf.
    Location("Ant Palace: Library, Behind the Bookshelf", 15, "AntPalaceLibrary",
             Source(flag=71, pickup=Pickup(map="AntPalaceLibrary", type=1, item=52))),
    # Royal Calling on the war room's table; the game makes it only after the ending (flag 555), the seed from the
    # start (the user, 2026-10-04).
    Location("Ant Palace: War Room, Table", 77, "AntPalaceWarRoom",
             Source(flag=717, pickup=Pickup(map="AntPalaceWarRoom", type=2, item=80))),
    # Board quest 33's reward, from a cicada in a residential house once the old book (Quest Book) is delivered.
    Location("Bugaria City: Residential District, Old Book Delivery Reward 1", 16, "BugariaResidential",
             Source(flag=243, give=Give(map="BugariaResidential", type=1, item=52)),
             rule=Has("Old Book Delivered"),
             category="quest"),
    # The same cicada hands over the old book once the quest is taken.
    Location("Bugaria City: Residential District, Old Book Delivery Start", 17, "BugariaResidential",
             Source(flag=241, give=Give(map="BugariaResidential", type=1, item=93)), category="quest"),
    # The same reward also pays 15 berries: two checks on one flag, sent together.
    Location("Bugaria City: Residential District, Old Book Delivery Reward 2", 18, "BugariaResidential",
             Source(flag=243, give=Give(map="BugariaResidential", type=-1, item=15)),
             rule=Has("Old Book Delivered"),
             category="quest"),
    # The Bad Book, outdoors on top of a house, reached past grass that Kabbu's horn cuts.
    Location("Bugaria City: Residential District, Rooftop", 32, "BugariaResidential",
             Source(flag=621, pickup=Pickup(map="BugariaResidential", type=1, item=174)),
             rule=CanUse("Horn Slash")),
    # On top of a house by the fountain; reaching it needs Leif's ice.
    Location("Bugaria City: Residential District, Fountain Rooftop", 33, "BugariaResidential",
             Source(flag=59, pickup=Pickup(map="BugariaResidential", type=2, item=18)),
             rule=CanUse("Freeze")),
    # Merab's medal shop, full stock from a new game: one location per copy she ever stocks, done by a per-copy bit in
    # the save.
    Location("Bugaria City: Commercial District, Medal Shop 1", 34, "BugariaCommercial",
             Source(shop=0, medal=0, give=Give(map="BugariaCommercial", type=2, item=0)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 2", 35, "BugariaCommercial",
             Source(shop=0, medal=1, give=Give(map="BugariaCommercial", type=2, item=1)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 3", 36, "BugariaCommercial",
             Source(shop=0, medal=7, give=Give(map="BugariaCommercial", type=2, item=7)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 4", 37, "BugariaCommercial",
             Source(shop=0, medal=12, give=Give(map="BugariaCommercial", type=2, item=12)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 5", 38, "BugariaCommercial",
             Source(shop=0, medal=30, give=Give(map="BugariaCommercial", type=2, item=30)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 6", 39, "BugariaCommercial",
             Source(shop=0, medal=86, give=Give(map="BugariaCommercial", type=2, item=86)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 7", 40, "BugariaCommercial",
             Source(shop=0, medal=84, give=Give(map="BugariaCommercial", type=2, item=84)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 8", 41, "BugariaCommercial",
             Source(shop=0, medal=87, give=Give(map="BugariaCommercial", type=2, item=87)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 9", 42, "BugariaCommercial",
             Source(shop=0, medal=88, give=Give(map="BugariaCommercial", type=2, item=88)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 10", 43, "BugariaCommercial",
             Source(shop=0, medal=81, give=Give(map="BugariaCommercial", type=2, item=81)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 2's end.
    Location("Bugaria City: Commercial District, Medal Shop 11", 46, "BugariaCommercial",
             Source(shop=0, medal=21, give=Give(map="BugariaCommercial", type=2, item=21)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 2's end.
    Location("Bugaria City: Commercial District, Medal Shop 12", 47, "BugariaCommercial",
             Source(shop=0, medal=22, give=Give(map="BugariaCommercial", type=2, item=22)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 2's end.
    Location("Bugaria City: Commercial District, Medal Shop 13", 48, "BugariaCommercial",
             Source(shop=0, medal=48, give=Give(map="BugariaCommercial", type=2, item=48)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 3's end.
    Location("Bugaria City: Commercial District, Medal Shop 14", 49, "BugariaCommercial",
             Source(shop=0, medal=33, give=Give(map="BugariaCommercial", type=2, item=33)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 3's end.
    Location("Bugaria City: Commercial District, Medal Shop 15", 50, "BugariaCommercial",
             Source(shop=0, medal=56, give=Give(map="BugariaCommercial", type=2, item=56)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 3's end.
    Location("Bugaria City: Commercial District, Medal Shop 16", 51, "BugariaCommercial",
             Source(shop=0, medal=74, give=Give(map="BugariaCommercial", type=2, item=74)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 5's start.
    Location("Bugaria City: Commercial District, Medal Shop 17", 52, "BugariaCommercial",
             Source(shop=0, medal=45, give=Give(map="BugariaCommercial", type=2, item=45)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 5's start; the second TP Plus copy.
    Location("Bugaria City: Commercial District, Medal Shop 18", 53, "BugariaCommercial",
             Source(shop=0, medal=1, give=Give(map="BugariaCommercial", type=2, item=1)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 6's start; the second Ambusher copy.
    Location("Bugaria City: Commercial District, Medal Shop 19", 54, "BugariaCommercial",
             Source(shop=0, medal=86, give=Give(map="BugariaCommercial", type=2, item=86)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 6's start.
    Location("Bugaria City: Commercial District, Medal Shop 20", 55, "BugariaCommercial",
             Source(shop=0, medal=62, give=Give(map="BugariaCommercial", type=2, item=62)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 6's start.
    Location("Bugaria City: Commercial District, Medal Shop 21", 56, "BugariaCommercial",
             Source(shop=0, medal=41, give=Give(map="BugariaCommercial", type=2, item=41)), category="shop",
             no_jump=True),
    # In vanilla stocked once a battle helper is unlocked.
    Location("Bugaria City: Commercial District, Medal Shop 22", 57, "BugariaCommercial",
             Source(shop=0, medal=85, give=Give(map="BugariaCommercial", type=2, item=85)), category="shop",
             no_jump=True),
    # Madame Butterfly's item shop: the first purchase of each item is the check, then the shop sells its own item.
    Location("Bugaria City: Commercial District, Item Shop 1", 58, "BugariaCommercial",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=0)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 2", 59, "BugariaCommercial",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=1)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 3", 60, "BugariaCommercial",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=17)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 4", 61, "BugariaCommercial",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=26)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 5", 62, "BugariaCommercial",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=13)),
             category="item_shop", no_jump=True),
)
STORY_EVENTS = (
    # The quest's middle step: the old book handed to a reader in the palace library.
    StoryEvent("Old Book Delivered", "Old Book Delivered", "AntPalaceLibrary",
               Source(flag=242),
               rule=Has("Quest Book"),
               category="quest"),
    # Chapter 2's title card in the palace, started in the hall (Chapter1StartEvent); it lines up the companion who
    # joins after the first boss, so it needs the first boss.
    StoryEvent("Chapter 2 Start", "Chapter 2 Started", "AntPalace1",
               Source(event=45, flag=67),
               rule=Has("Snakemouth Den Cleared")),
)
KEPT_OPEN = (
    # The miners where the arcade opens from flag 350 (after the first submarine landing): the arcade is kept present.
    EntityRef("BugariaCommercial", "MinerAnt"),
    EntityRef("BugariaCommercial", "Fixedminingant"),
    # Turns the party back from the library door until chapter 2 (Event12).
    EntityRef("AntPalace1", "makiblocker1"),
    # And from the war room door.
    EntityRef("AntPalace1", "makiblocker2"),
    # A stand-in in front of the inn portrait until chapter 2 ('Let's hurry to the castle').
    EntityRef("BugariaMainPlaza", "Discovery Pre Briefing"),
    # The same stand-in in front of the plaza statue.
    EntityRef("BugariaMainPlaza", "Discovery Pre Briefing - Duplicate"),
    # One of three plaza blockers that turn the party back until chapter 2.
    EntityRef("BugariaMainPlaza", "MM"),
    # The second plaza blocker.
    EntityRef("BugariaMainPlaza", "blockereetl2"),
    # The third plaza blocker.
    EntityRef("BugariaMainPlaza", "blockereetl2 - Duplicate"),
    # Maki turning the party back toward the palace, from the bridge scene (flag 66) until the briefing (67).
    EntityRef("BugariaMainPlaza", "MakiBlock"),
)
KEPT_PRESENT = (
    # The arcade, its door, sign, games and helpers, made only from flag 350: there from the start (the user,
    # 2026-10-06).
    EntityRef("BugariaCommercial", "arcadedoor"),
    EntityRef("BugariaCommercial", "arcadesign"),
    EntityRef("BugariaCommercial", "termiteoutside"),
    EntityRef("BugariaCommercial", "Fixedtermiteexchanger"),
    EntityRef("BugariaCommercial", "termitegamehelper"),
    EntityRef("BugariaCommercial", "dungeongame"),
    EntityRef("BugariaCommercial", "kartgame"),
    EntityRef("BugariaCommercial", "score check"),
    EntityRef("BugariaCommercial", "beegame"),
    # The way down to the ant tunnels, made only from chapter 2 (flag 67): the tunnels lead up here, so it stays open
    # (the user, 2026-10-04).
    EntityRef("AntPalace1", "MineLoadZone"),
    # The library door, made only from chapter 2 (flag 67) (the user, 2026-10-04); its blocker is kept away.
    EntityRef("AntPalace1", "Loadzonelibrary"),
    # The war room door, opposite, likewise (the user, 2026-10-04).
    EntityRef("AntPalace1", "loadzonewarroom"),
    # The medal on the war room's table, made only after the ending (flag 555): location 77.
    EntityRef("AntPalaceWarRoom", "royal medal"),
    # The plaza statue, a journal discovery the game makes only from chapter 2.
    EntityRef("BugariaMainPlaza", "StatueDesc"),
    # The inn portrait, a journal discovery, also only from chapter 2.
    EntityRef("BugariaMainPlaza", "InnPortrait"),
    # The town's quest board, made from chapter 2; each quest's own requirements are checked before the logic counts on
    # it.
    EntityRef("BugariaMainPlaza", "QuestBoard"),
    # A plaza exit into the town, made only from chapter 2.
    EntityRef("BugariaMainPlaza", "LoadingZoneCommercial"),
    # A plaza exit into the town, made only from chapter 2.
    EntityRef("BugariaMainPlaza", "LoadingZoneResidential"),
    # A plaza exit into the town, made only from chapter 2.
    EntityRef("BugariaMainPlaza", "loadingzone theater"),
)
SCENERY_HIDDEN = (
    # The empty lot's fence, there until the arcade opens (flag 350): the arcade is kept present.
    EntityRef("BugariaCommercial", "Model/Base/EmptyLotFence"),
    # The railing round the mine shaft until chapter 2 (flag 67), which boxed in a party coming up from the tunnels.
    EntityRef("AntPalace1", "Base/mineblock"),
    # Plaza scenery hidden from chapter 2, likely a wall across the way into town.
    EntityRef("BugariaMainPlaza", "Cube"),
)
HELD_UNTIL = (
    # Maki's swap on the palace bridge waits for the follower who joins after the first boss, so the scenes run in story
    # order.
    FlagEntity("AntBridge", "makiautoevent", 114),
    # Chapter 2's briefing waits for Maki's swap, whatever the way into the palace.
    FlagEntity("AntPalace1", "Chapter1StartEvent", 66),
)
DIALOGUE_FLAGS = (
    # Lets the inn be used before chapter 2 (691 is set by every new game).
    DialogueFlag("BugariaMainPlaza", "Innkeeper", 67, 691),
    # The way down to the underground bar; its own flag (135) has many other story effects, so the line is repointed
    # instead.
    DialogueFlag("BugariaCommercial", "HideoutEntrance", 135, 691),
)

SCENERY_PRESENT = (
    # The arcade's building, made only from flag 350, with its door and games (KEPT_PRESENT).
    EntityRef("BugariaCommercial", "Model/TermiteArcade"),
)

MAP_AREAS = (
    # The commercial district's corner above its plaza door, behind grass (the horn), both ways: the way down to the
    # underground bar, a dig spot, and where the bar's door comes back up.
    Area("BugariaCommercial", "Bar Corner", (), CanUse("Horn Slash"), landings=(("UndergroundBar", "LoadZone"),)),
)
