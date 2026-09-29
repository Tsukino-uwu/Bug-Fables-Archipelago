"""Bugaria City (the palace's rooms included, in the Inner City): its regions, spots and story events, and what the
seed changes there."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ALL_ATTACKS, WHOLE_PARTY, CanUse
from ..data_types import (DialogueFlag, EntityRef, Exit, FlagEntity, Give, ItemShop, Location, Pickup, Region, Source,
                          StoryEvent)

REGIONS = (
    # Open from the start: the game's door in (flag 107) is kept present.
    Region("Bugaria City", exits=(
        Exit("Bugaria Inner City", Has("Chapter 2 Started")),
    )),
    # The Ant Palace's rooms and the city districts; their doors require chapter 2 (flag 67).
    Region("Bugaria Inner City", exits=(
        Exit("Later Chapters", Has("Explorer Permit") & Has("Boat Ticket") & Has("Snakemouth Den Cleared")
             & WHOLE_PARTY & ALL_ATTACKS),
    )),
)
LOCATIONS = (
    # A Lore Book hidden behind a bookshelf.
    Location("Ant Palace: Library, Bookshelf", 15, "Bugaria Inner City",
             Source(flag=71, pickup=Pickup(map="AntPalaceLibrary", type=1, item=52))),
    # Board quest 33's reward, from a cicada in a residential house once the old book (Quest Book) is delivered.
    Location("Bugaria City: Residential District, Old Book Delivery Reward", 16, "Bugaria Inner City",
             Source(flag=243, give=Give(map="BugariaResidential", type=1, item=52)),
             rule=Has("Old Book Delivered"),
             category="quest"),
    # The same cicada hands over the old book once the quest is taken.
    Location("Bugaria City: Residential District, Old Book Delivery Start", 17, "Bugaria Inner City",
             Source(flag=241, give=Give(map="BugariaResidential", type=1, item=93)), category="quest"),
    # The same reward also pays 15 berries: two checks on one flag, sent together.
    Location("Bugaria City: Residential District, Old Book Delivery Berries", 18, "Bugaria Inner City",
             Source(flag=243, give=Give(map="BugariaResidential", type=-1, item=15)),
             rule=Has("Old Book Delivered"),
             category="quest"),
    # The Bad Book, outdoors on top of a house, reached past grass that Kabbu's horn cuts.
    Location("Bugaria City: Residential District, Rooftop", 32, "Bugaria City",
             Source(flag=621, pickup=Pickup(map="BugariaResidential", type=1, item=174)),
             rule=CanUse("Horn Slash")),
    # On top of a house by the fountain; reaching it needs Leif's ice.
    Location("Bugaria City: Residential District, Fountain Rooftop", 33, "Bugaria City",
             Source(flag=59, pickup=Pickup(map="BugariaResidential", type=2, item=18)),
             rule=CanUse("Freeze")),
    # Merab's medal shop, full stock from a new game: one location per copy she ever stocks, done by a per-copy bit in
    # the save.
    Location("Bugaria City: Commercial District, Medal Shop 1", 34, "Bugaria City",
             Source(shop=0, medal=0, give=Give(map="BugariaCommercial", type=2, item=0)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 2", 35, "Bugaria City",
             Source(shop=0, medal=1, give=Give(map="BugariaCommercial", type=2, item=1)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 3", 36, "Bugaria City",
             Source(shop=0, medal=7, give=Give(map="BugariaCommercial", type=2, item=7)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 4", 37, "Bugaria City",
             Source(shop=0, medal=12, give=Give(map="BugariaCommercial", type=2, item=12)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 5", 38, "Bugaria City",
             Source(shop=0, medal=30, give=Give(map="BugariaCommercial", type=2, item=30)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 6", 39, "Bugaria City",
             Source(shop=0, medal=86, give=Give(map="BugariaCommercial", type=2, item=86)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 7", 40, "Bugaria City",
             Source(shop=0, medal=84, give=Give(map="BugariaCommercial", type=2, item=84)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 8", 41, "Bugaria City",
             Source(shop=0, medal=87, give=Give(map="BugariaCommercial", type=2, item=87)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 9", 42, "Bugaria City",
             Source(shop=0, medal=88, give=Give(map="BugariaCommercial", type=2, item=88)), category="shop",
             no_jump=True),
    Location("Bugaria City: Commercial District, Medal Shop 10", 43, "Bugaria City",
             Source(shop=0, medal=81, give=Give(map="BugariaCommercial", type=2, item=81)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 2's end.
    Location("Bugaria City: Commercial District, Medal Shop 11", 46, "Bugaria City",
             Source(shop=0, medal=21, give=Give(map="BugariaCommercial", type=2, item=21)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 2's end.
    Location("Bugaria City: Commercial District, Medal Shop 12", 47, "Bugaria City",
             Source(shop=0, medal=22, give=Give(map="BugariaCommercial", type=2, item=22)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 2's end.
    Location("Bugaria City: Commercial District, Medal Shop 13", 48, "Bugaria City",
             Source(shop=0, medal=48, give=Give(map="BugariaCommercial", type=2, item=48)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 3's end.
    Location("Bugaria City: Commercial District, Medal Shop 14", 49, "Bugaria City",
             Source(shop=0, medal=33, give=Give(map="BugariaCommercial", type=2, item=33)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 3's end.
    Location("Bugaria City: Commercial District, Medal Shop 15", 50, "Bugaria City",
             Source(shop=0, medal=56, give=Give(map="BugariaCommercial", type=2, item=56)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 3's end.
    Location("Bugaria City: Commercial District, Medal Shop 16", 51, "Bugaria City",
             Source(shop=0, medal=74, give=Give(map="BugariaCommercial", type=2, item=74)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 5's start.
    Location("Bugaria City: Commercial District, Medal Shop 17", 52, "Bugaria City",
             Source(shop=0, medal=45, give=Give(map="BugariaCommercial", type=2, item=45)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 5's start; the second TP Plus copy.
    Location("Bugaria City: Commercial District, Medal Shop 18", 53, "Bugaria City",
             Source(shop=0, medal=1, give=Give(map="BugariaCommercial", type=2, item=1)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 6's start; the second Ambusher copy.
    Location("Bugaria City: Commercial District, Medal Shop 19", 54, "Bugaria City",
             Source(shop=0, medal=86, give=Give(map="BugariaCommercial", type=2, item=86)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 6's start.
    Location("Bugaria City: Commercial District, Medal Shop 20", 55, "Bugaria City",
             Source(shop=0, medal=62, give=Give(map="BugariaCommercial", type=2, item=62)), category="shop",
             no_jump=True),
    # In vanilla stocked from chapter 6's start.
    Location("Bugaria City: Commercial District, Medal Shop 21", 56, "Bugaria City",
             Source(shop=0, medal=41, give=Give(map="BugariaCommercial", type=2, item=41)), category="shop",
             no_jump=True),
    # In vanilla stocked once a battle helper is unlocked.
    Location("Bugaria City: Commercial District, Medal Shop 22", 57, "Bugaria City",
             Source(shop=0, medal=85, give=Give(map="BugariaCommercial", type=2, item=85)), category="shop",
             no_jump=True),
    # Madame Butterfly's item shop: the first purchase of each item is the check, then the shop sells its own item.
    Location("Bugaria City: Commercial District, Item Shop 1", 58, "Bugaria City",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=0)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 2", 59, "Bugaria City",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=1)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 3", 60, "Bugaria City",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=17)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 4", 61, "Bugaria City",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=26)),
             category="item_shop", no_jump=True),
    Location("Bugaria City: Commercial District, Item Shop 5", 62, "Bugaria City",
             Source(item_shop=ItemShop(map="BugariaCommercial", keeper="ButterflyShopkeeper", item=13)),
             category="item_shop", no_jump=True),
)
STORY_EVENTS = (
    # The quest's middle step: the old book handed to a reader in the palace library.
    StoryEvent("Old Book Delivered", "Old Book Delivered", "Bugaria Inner City",
               Source(flag=242),
               rule=Has("Quest Book"),
               category="quest"),
    # Chapter 2's title card in the palace; it lines up the companion who joins after the first boss, so it needs the
    # first boss.
    StoryEvent("Chapter 2 Start", "Chapter 2 Started", "Bugaria City",
               Source(event=45, flag=67),
               rule=Has("Snakemouth Den Cleared")),
)
KEPT_OPEN = (
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
)
KEPT_PRESENT = (
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
