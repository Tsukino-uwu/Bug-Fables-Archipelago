"""Chapters 2-7: the spots where the game teaches each later ability, what reaching them needs until the rooms are
mapped, and the ways between maps that aren't doors."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ALL_ATTACKS, WHOLE_PARTY, CanUse
from ..data_types import Location, Source, Transfer
from .bugaria_city import INNER_CITY

# Chapters 2-7 until they get room-level logic: past chapter 2's start, with everything the story used before (the
# permit, the Boat Ticket, the first boss, the whole party and its attacks). More cautious than the game.
LATER_CHAPTERS = (INNER_CITY & Has("Explorer Permit") & Has("Boat Ticket") & Has("Snakemouth Den Cleared")
                  & WHOLE_PARTY & ALL_ATTACKS)
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
    # Icicle (flag 171).
    Location("Upper Snakemouth: Entrance", 74, "UpperSnekTransition",
             Source(event=180, flag=171),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly"), reach=LATER_CHAPTERS),
)
# The ways that join the parts the doors alone leave apart (MEASURED.md, "Transfers that aren't doors"), each as
# cautious as the chapters it belongs to. The ones inside a part the doors already join wait for the room mapping.
TRANSFERS = (
    Transfer("elevator", "DefiantRoot2", "BeehiveOutside", LATER_CHAPTERS),
    Transfer("submarine", "BugariaPier", "MetalLake", LATER_CHAPTERS),
    Transfer("submarine", "TermitePier", "MetalLake", LATER_CHAPTERS),
    Transfer("submarine", "MetalIsland1", "MetalLake", LATER_CHAPTERS),
    Transfer("submarine", "RubberPrisonPier", "MetalLake", LATER_CHAPTERS),
    Transfer("submarine", "FishingVillage", "MetalLake", LATER_CHAPTERS),
    Transfer("submarine", "MysteryIsland", "MetalLake", LATER_CHAPTERS),
    Transfer("ant tunnel", "AntTunnels", "RubberPrisonGiantLairBridge", LATER_CHAPTERS),
    Transfer("ant tunnel", "AntTunnels", "MetalIsland2", LATER_CHAPTERS),
    Transfer("gate", "TermiteOutside", "TermiteMainPlaza", LATER_CHAPTERS),
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
