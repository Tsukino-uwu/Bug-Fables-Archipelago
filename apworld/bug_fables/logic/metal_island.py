"""Metal Island (the game's area 17, MapControl.areaid): the island's rooms; the boat over from the pier is the
Outskirts'."""
from __future__ import annotations

from rule_builder.rules import False_, Rule

from ..custom_rules import SUBMARINE, SUBMARINE_KEY, CanUse, one_way
from ..data_types import Area, DoorRule, EntityRef, ItemEntity, ItemShop, Location, Pickup, Source, Transfer

_UP: Rule = CanUse("Jump") | CanUse("Bee Fly")

LOCATIONS = (
    # Metal Island's town: the statue to Mothington X in its middle, discovery 44, free.
    Location("Metal Island: Town, Mothington X Statue", 259, "MetalIsland1", Source(discovery=44),
             category="discovery", no_jump=True),
    # Crystal berry #36 on the Chef's roof: up a ledge by the house (Jump or Bee Fly), dug under a fence (Beetle Dig)
    # to a green bounce pad up onto the roof; a drop back down.
    Location("Metal Island: Town, Chef's Roof", 258, "MetalIsland1",
             Source(berry=36, pickup=Pickup(map="MetalIsland1", type=3, item=0)),
             rule=_UP & CanUse("Beetle Dig"), category="crystal_berry", no_jump=True),
    # The tea seller's shop on the town's ground, free: first purchase a check, then its own item.
    *(Location(f"Metal Island: Town, Tea Shop {slot}", 259 + slot, "MetalIsland1",
               Source(item_shop=ItemShop(map="MetalIsland1", keeper="FxdColteaseller", item=item)),
               category="item_shop", no_jump=True)
      for slot, item in enumerate((81, 80), start=1)),
)

TRANSFERS = (
    Transfer("submarine", "MetalIsland1", "MetalLake", SUBMARINE, from_area="Dock"),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("MetalIsland1", "Fixedsub - Duplicate", SUBMARINE_KEY),
)

MAP_AREAS = (
    # Metal Island's town (MetalIsland1; the game's name): its ground the map's own region (the hotel door, the save
    # crystal, the shops, the statue). The dock below (the boat, the submarine) a drop down, back up by Jump or Bee Fly.
    # The Chef's house on the right (a room of the same map) up by Jump or Bee Fly, both ways. The left log house open
    # (below).
    Area("MetalIsland1", "Dock", (), one_way(None, _UP), out=_UP),
    Area("MetalIsland1", "Chef's House", (), _UP),
)

# The log house's door on the left, made in the game only after the ending (555): open from the start, with its two
# ladybugs (inside and by the door, also from 555; post-story chatter, no quest), for the room's look (decided
# 2026-10-11).
KEPT_PRESENT = (
    EntityRef("MetalIsland1", "doorloghouse"),
    EntityRef("MetalIsland1", "ladybugorange"),
    EntityRef("MetalIsland1", "FxdColladybugred"),
)
# Its closed door, shown until 555: hidden with it.
SCENERY_HIDDEN = (
    EntityRef("MetalIsland1", "Log House/Door"),
)

DOOR_RULES = (
    # The Metal Island Hotel (MetalIsland2; named 2026-10-11): everything free but its top left door to the Auditorium, whose guard
    # (until 315) lets the party by only once every card master of Bugaria is beaten (Event122). The card masters
    # aren't in the logic, so the door is shut for the logic and left out of the door shuffle; the guard stays.
    DoorRule("MetalIsland2", "loadzone aud", False_()),
    DoorRule("MetalIslandAuditorium", "loadzone", False_()),
)
