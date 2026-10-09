"""Honey Factory (the game's area 13, MapControl.areaid): its spots, and what the seed changes there, mapped room by
room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import ALWAYS_SET, Area, DoorRule, EntityRef, FlagSwap, ItemShop, Location, Pickup, Source

# The Factory Pass (key item 95), found in the factory: not an item yet, so the later chapters' stand-in until its
# rooms are mapped. In a seed it won't be used up (apimplementation.md, Next 67), so one opens every lock. The Worker
# Rooms' pass (flag 178, up high in the office) stays the game's own pickup until then: Jump and the Beemerang Toss,
# or Bee Fly alone (the user, 2026-10-10).
FACTORY_PASS = LATER_CHAPTERS
_UP = CanUse("Jump") | CanUse("Bee Fly")

LOCATIONS = (
    # Where the game teaches the Shield (flag 20); the later chapters' story-order stand-in, every ability taught before
    # it.
    Location("Honey Factory: First Room, Switch", 70, "FactoryProcessingFirstRoom",
             Source(event=95, flag=20),
             rule=CanUse("Beemerang Halt") & CanUse("Dash"), reach=LATER_CHAPTERS),
    # The Lobby's shop on its bottom floor, opened by talking to the bee outside it (Event80, flag 176), nothing needed
    # (the user, 2026-10-10: "lets add the shop at the bottom as locations"): first purchase a check, then its own item.
    *(Location(f"Honey Factory: Lobby, Shop {slot}", 212 + slot - 1, "HoneyFactoryEntrance",
               Source(item_shop=ItemShop(map="HoneyFactoryEntrance", keeper="shopbee - Duplicate", item=item)),
               category="item_shop", no_jump=True, area="Bottom")
      for slot, item in enumerate((10, 9, 20, 49, 43), start=1)),
    # The Worker Rooms' office (named by the user, 2026-10-10): a Shock Candy on the desk, Jump or Bee Fly.
    Location("Honey Factory: Worker Rooms, On the Desk", 229, "HoneyFactoryWorkerRooms",
             Source(flag=728, pickup=Pickup(map="HoneyFactoryWorkerRooms", type=0, item=75)), rule=_UP, no_jump=True),
)

KEPT_PRESENT = (
    # The door back out to Outside the Beehive, made in the game only from 299: kept present with its other half
    # (bee_kingdom_hive.py) (the user, 2026-10-09).
    EntityRef("HoneyFactoryEntrance", "loadzoneoutside"),
    # The Lobby's door to the storage, made in the game only from 211 (Event98): open from the start (the user,
    # 2026-10-10: "we open the storage door"); the storage elevator's half has no flag.
    EntityRef("HoneyFactoryEntrance", "loadzonestorage"),
)
SCENERY_HIDDEN = (
    # That door's closed model, hidden in the game from 299.
    EntityRef("HoneyFactoryEntrance", "Base/DoorE"),
    # The storage door's closed model, hidden in the game from 211.
    EntityRef("HoneyFactoryEntrance", "Base/DoorS"),
)
# The pump room's hidden platform switch, hit while flag 41 (the first boss) is set: always on, as in vanilla, and it
# can never set 41 (build step 60).
ACTIVATION_FLAGS = (
    FlagSwap("FactoryProcessingPump", "platformcontrol", 41, ALWAYS_SET),
)
MAP_AREAS = (
    # The Lobby (HoneyFactoryEntrance; named by the user, 2026-10-10): the upper area (the save crystal, the doors to
    # the outside, the core, processing and the storage) the map's own region; the bottom (the office's and the sleeping
    # quarters' doors, the shop, opened by talking to the bee outside it) a drop down, Jump or Bee Fly back up.
    Area("HoneyFactoryEntrance", "Bottom", ("loadzoneoffice", "loadzonesleep"), one_way(None, _UP), out=_UP),
    # The Worker Rooms (HoneyFactoryWorkerRooms; named by the user, 2026-10-10), two parts with no way between them
    # inside the room: the office (its door, the desk, the portrait, the PC) the map's own region, nothing needed to go
    # in, out or to the portrait; the sleeping quarters (the beds door and three workers), cut off.
    Area("HoneyFactoryWorkerRooms", "Sleeping Quarters", ("loadzonebeds",), False_()),
    # HoneyFactoryCore (2026-10-10): one region, its one door free; the gate at its top shut until the chapter 3 finale
    # (Event99, which sets 299 and ends in the room), behind it only the empty boss arena (the user).
)
DOOR_RULES = (
    # The Lobby's processing door, locked until the Factory Pass is used on it (keything, Event59 key index 4, then
    # Event89, flag 179); kept locked (the user, 2026-10-10). Arriving from the first room, the game pushes the party
    # past the lock (seen).
    DoorRule("HoneyFactoryEntrance", "loadzone processing", FACTORY_PASS),
)
