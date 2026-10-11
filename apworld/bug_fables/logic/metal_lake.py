"""Metal Lake, Mystery Island included (the game's area 16, MapControl.areaid): the lake and the island."""
from __future__ import annotations

from rule_builder.rules import Rule

from ..custom_rules import SUBMARINE, SUBMARINE_KEY, CanUse
from ..data_types import Area, ItemEntity, Location, Pickup, Source, Transfer

_ICICLE_UP: Rule = CanUse("Icicle") & CanUse("Jump")

# Metal Lake (MetalLake; mapped 2026-10-11): the submarine's lake, no doors, no pickups, only its six docks; every dock
# reaches every other by the submarine. Never a start and never shuffled (no door leads in or out).
TRANSFERS = (
    Transfer("submarine", "MysteryIsland", "MetalLake", SUBMARINE),
)
# The submarine's dock exists with its key item in the bag (Mystery Island's needs no story flag).
PRESENT_WITH_ITEM = (
    ItemEntity("MysteryIsland", "Fixedsub", SUBMARINE_KEY),
)

LOCATIONS = (
    # The camp sign at Mystery Island's back, up by Jump: discovery 46.
    Location("Metal Lake: Mystery Island, Camp Sign", 257, "MysteryIsland", Source(discovery=46), rule=CanUse("Jump"),
             category="discovery", no_jump=True, area="Back"),
    # Mystery Island's back: up by Jump to the discovery's top, Bee Fly across to the dig spot, then Beetle Dig.
    Location("Metal Lake: Mystery Island, Dig Spot", 256, "MysteryIsland",
             Source(flag=653, pickup=Pickup(map="MysteryIsland", type=0, item=121)),
             rule=CanUse("Jump") & CanUse("Bee Fly") & CanUse("Beetle Dig"), category="dig_spot", no_jump=True,
             area="Back"),
)
MAP_AREAS = (
    # Mystery Island (MysteryIsland; the game's name): the dock (the submarine, a save crystal) the map's own region; three areas cut
    # off from it and from each other: the door inside and the Venus bud's, each by Icicle and Jump or by Bee Fly,
    # both ways; the back (the camp sign's top, the dig spot) by Icicle and Jump.
    Area("MysteryIsland", "Door", ("loadzone",), _ICICLE_UP | CanUse("Bee Fly")),
    Area("MysteryIsland", "Venus", (), _ICICLE_UP | CanUse("Bee Fly")),
    Area("MysteryIsland", "Back", (), _ICICLE_UP),
)
