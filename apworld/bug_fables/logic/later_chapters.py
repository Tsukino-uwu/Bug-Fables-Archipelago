"""Chapters 2-7: the spots where the game teaches each later ability, what reaching them needs until the rooms are
mapped, and the ways between maps that aren't doors."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ALL_ATTACKS, BOAT_TICKET, SUBMARINE, WHOLE_PARTY, CanUse
from ..data_types import FlagEntity, ItemEntity, Location, Source, Transfer
from .bugaria_city import INNER_CITY

# Chapters 2-7 until they get room-level logic: past chapter 2's start, with everything the story used before (the
# permit, the Boat Ticket, the first boss, the whole party and its attacks). More cautious than the game.
LATER_CHAPTERS = (INNER_CITY & Has("Explorer Permit") & BOAT_TICKET & Has("Snakemouth Den Cleared")
                  & WHOLE_PARTY & ALL_ATTACKS)
# The submarine's key item in the bag, the Subaquatic Maritime Neotransport (CustomItems.cs), whichever item gives it.
SUBMARINE_KEY = 212
# Where the game teaches each ability (its flag). Until chapters 2-7 get room-level logic, each needs every ability
# taught before it (story order). The names are provisional.
LOCATIONS = (
    # Beemerang Halt (flag 21).
    Location("Golden Settlement: Festival, Wacka Worm Game", 68, "GoldenSettlement2",
             Source(event=55, flag=21), reach=LATER_CHAPTERS),
    # Dash (flag 699).
    Location("Lost Sands: Entrance", 69, "BOLostSandsEntrance",
             Source(event=221, flag=699),
             rule=CanUse("Beemerang Halt"), reach=LATER_CHAPTERS),
    # Shield (flag 20).
    Location("Honey Factory: First Room, Switch", 70, "FactoryProcessingFirstRoom",
             Source(event=95, flag=20),
             rule=CanUse("Beemerang Halt") & CanUse("Dash"), reach=LATER_CHAPTERS),
    # Beetle Dig (flag 18).
    Location("Bugaria Hideout: Cell", 71, "HideoutCell",
             Source(event=109, flag=18),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield"), reach=LATER_CHAPTERS),
    # Horn Dash (flag 39).
    Location("Swamplands: Bridge", 72, "SwamplandsBridge",
             Source(event=131, flag=39),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig"),
             reach=LATER_CHAPTERS),
    # Bee Fly (flag 19).
    Location("Barren Lands: Fly Spot", 73, "BarrenLandsBeefly",
             Source(event=150, flag=19),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash"), reach=LATER_CHAPTERS),
    # Icicle (flag 171). The story reaches it after the submarine; its path reads no submarine flag, so the sub is
    # this story-order stand-in's caution, not a gate.
    Location("Upper Snakemouth: Entrance", 74, "UpperSnekTransition",
             Source(event=180, flag=171),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly") & SUBMARINE, reach=LATER_CHAPTERS),
    # The Termite King hands over the submarine after the Colosseum (flag 379); never behind the sub itself.
    Location("Termite Capitol: Throne Room", 76, "TermiteRoyalChamber",
             Source(event=164, flag=379),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly"), reach=LATER_CHAPTERS),
)
# The ways that join the parts the doors alone leave apart (MEASURED.md, "Transfers that aren't doors"), each as
# cautious as the chapters it belongs to. The ones inside a part the doors already join wait for the room mapping.
TRANSFERS = (
    Transfer("elevator", "DefiantRoot2", "BeehiveOutside", LATER_CHAPTERS),
    # Every dock takes the submarine (and the Boat Ticket, inside the later chapters' stand-in).
    Transfer("submarine", "BugariaPier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    Transfer("submarine", "TermitePier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    Transfer("submarine", "MetalIsland1", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    Transfer("submarine", "RubberPrisonPier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    Transfer("submarine", "FishingVillage", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    Transfer("submarine", "MysteryIsland", "MetalLake", LATER_CHAPTERS & SUBMARINE),
    # The tunnel's prison door needs flag 79, set only inside the prison, which the sub alone reaches before it.
    Transfer("ant tunnel", "AntTunnels", "RubberPrisonGiantLairBridge", LATER_CHAPTERS & SUBMARINE),
    Transfer("ant tunnel", "AntTunnels", "MetalIsland2", LATER_CHAPTERS),
    # One way: from inside, the gate only opens once it has been opened from outside (flag 384; HELD_UNTIL).
    Transfer("gate", "TermiteOutside", "TermiteMainPlaza", LATER_CHAPTERS, two_way=False),
    Transfer("arena", "TermiteColiseum1", "TermiteColiseum2", LATER_CHAPTERS),
    Transfer("lift", "GiantLairFridgeInside", "GiantLairRoachVillage", LATER_CHAPTERS),
    Transfer("lift", "GiantLairRoachVillage", "GiantLairEntrance", LATER_CHAPTERS),
    Transfer("elevator", "GoldenHillsDungeonEntrance", "GoldenHillsDungeonUpperMain", LATER_CHAPTERS),
    # Chapter 3's end: the attack on the city, then back in the palace.
    Transfer("story", "DesertSandCastle", "BugariaAssociationAttack", LATER_CHAPTERS, two_way=False),
    Transfer("story", "BugariaCastleAttack", "AntPalace2", LATER_CHAPTERS, two_way=False),
    # The ending: from the Sapling's plains to the city's end, the throne, and back to the plaza.
    Transfer("story", "GiantLairSaplingPlains", "BugariaEndPlaza", LATER_CHAPTERS, two_way=False),
    Transfer("story", "BugariaEndBridge", "BugariaEndThrone", LATER_CHAPTERS, two_way=False),
    Transfer("story", "BugariaEndThrone", "BugariaMainPlaza", LATER_CHAPTERS, two_way=False),
)
# The submarine's docks exist with its key item in the bag, whatever the story's flags (379 at the Termite pier, 448
# elsewhere, none on Mystery Island).
PRESENT_WITH_ITEM = (
    ItemEntity("TermitePier", "Fixedsub", SUBMARINE_KEY),
    ItemEntity("BugariaPier", "Fixedsub - Duplicate", SUBMARINE_KEY),
    ItemEntity("MetalIsland1", "Fixedsub - Duplicate", SUBMARINE_KEY),
    ItemEntity("FishingVillage", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
    ItemEntity("RubberPrisonPier", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
    ItemEntity("MysteryIsland", "Fixedsub", SUBMARINE_KEY),
)
# The scientist and the queen show the dock off (Event165), so they wait for it too.
HELD_UNTIL_ITEM = (
    ItemEntity("TermitePier", "FixedScientist", SUBMARINE_KEY),
    ItemEntity("TermitePier", "FixedQueen", SUBMARINE_KEY),
)
# Opened from inside before it was ever opened from outside (flag 384), the gate's scene looks for guards the other
# side doesn't have and stops, and the sub can land a party inside first.
HELD_UNTIL = (
    FlagEntity("TermiteMainPlaza", "gate", 384),
)
