"""Honey Factory (the game's area 13, MapControl.areaid): its spots, and what the seed changes there, mapped room by
room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, CanUse, one_way
from ..data_types import (ALWAYS_SET, Area, DoorRule, EntityRef, FlagSwap, ItemShop, Location, Pickup, Source,
                          StoryEvent, Transfer)

# The Factory Pass (key item 95), found in the factory: not an item yet, so the later chapters' stand-in until its
# rooms are mapped. In a seed it won't be used up (apimplementation.md, Next 67), so one opens every lock. The Worker
# Rooms' pass (flag 178, up high in the office) stays the game's own pickup until then: Jump and the Beemerang Toss,
# or Bee Fly alone (the user, 2026-10-10); so does the first puzzle room's (213): Jump, Freeze, the Shield and a basic
# attack, past Gen and Eri's fight (two Bee-Boops, Vi to be safe) and the holler (Tattle, any member: mod step 52); and
# the second's (212): Beemerang Halt, Jump and the Shield, and a basic attack for a switch (Halt brings the Toss).
FACTORY_PASS = LATER_CHAPTERS
_UP = CanUse("Jump") | CanUse("Bee Fly")
# The First Room's switch hit (Event95, flag 20): its moving platforms run from then on, for good.
_PLATFORMS_RUNNING = "First Room Platforms Running"
_ON_THE_PLATFORMS = Has(_PLATFORMS_RUNNING) & CanUse("Shield")
_PROCESSING2_UP = _UP & ANY_ATTACK & CanUse("Shield") & (CanUse("Beemerang Halt") | CanUse("Bee Fly"))
# The pump room's moving platforms, a loop round its upper part (always running: ACTIVATION_FLAGS), Jump and the Shield.
_PUMP_LOOP = CanUse("Jump") & CanUse("Shield")
# Up from its floor: the cranks (Beemerang Halt) to a platform with nothing on it, then the loop.
_PUMP_UP = CanUse("Beemerang Halt") & _PUMP_LOOP

LOCATIONS = (
    # The First Room's switch (FactoryProcessingFirstRoom; the user, 2026-10-10), on its right side, hit with a basic
    # attack: the scene where the game teaches the Shield (Event95, flag 20).
    Location("Honey Factory: First Room, Switch", 70, "FactoryProcessingFirstRoom", Source(event=95, flag=20),
             rule=ANY_ATTACK, no_jump=True),
    # The Lobby's shop on its bottom floor, opened by talking to the bee outside it (Event80, flag 176), nothing needed
    # (the user, 2026-10-10: "lets add the shop at the bottom as locations"): first purchase a check, then its own item.
    *(Location(f"Honey Factory: Lobby, Shop {slot}", 212 + slot - 1, "HoneyFactoryEntrance",
               Source(item_shop=ItemShop(map="HoneyFactoryEntrance", keeper="shopbee - Duplicate", item=item)),
               category="item_shop", no_jump=True, area="Bottom")
      for slot, item in enumerate((10, 9, 20, 49, 43), start=1)),
    # The Worker Rooms' office (named by the user, 2026-10-10): a Shock Candy on the desk, Jump or Bee Fly.
    Location("Honey Factory: Worker Rooms, On the Desk", 229, "HoneyFactoryWorkerRooms",
             Source(flag=728, pickup=Pickup(map="HoneyFactoryWorkerRooms", type=0, item=75)), rule=_UP, no_jump=True),
    # The Pump Room's respawning Shell Ointment (regional flag 4), behind boxes in the
    # upper right's left part, across from its door: the Shield or Bee Fly.
    Location("Honey Factory: Pump Room, Behind the Boxes", 230, "FactoryProcessingPump",
             Source(regional=4, pickup=Pickup(map="FactoryProcessingPump", type=0, item=97)),
             rule=CanUse("Shield") | CanUse("Bee Fly"), no_jump=True, area="Upper Right"),
)

STORY_EVENTS = (
    # The same switch hit, which starts the room's platforms for good.
    StoryEvent("Honey Factory: First Room, Platforms Running", _PLATFORMS_RUNNING, "FactoryProcessingFirstRoom",
               Source(event=95, flag=20), rule=ANY_ATTACK, no_jump=True),
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
    # The First Room (FactoryProcessingFirstRoom; the user, 2026-10-10): the right (the door to the Lobby, the switch)
    # the map's own region; the left (the door on) across a gap, on the moving platforms once the switch has started
    # them, the Shield, both ways. Bee Fly across works only before the switch, which stays hit, so it never counts
    # (room-logic.md, rule 3). The bottom below: a drop from either side, Jump or Bee Fly up to the right only.
    Area("FactoryProcessingFirstRoom", "Left", ("loadzoneforward",), _ON_THE_PLATFORMS),
    Area("FactoryProcessingFirstRoom", "Bottom", (), one_way(None, _UP), out=_UP),
    # The Second Room (FactoryProcessing2; named by the user, 2026-10-10): the bottom right (the door back to the first
    # room) the map's own region; up to the top left (the door to the pump room) Jump or Bee Fly, a basic attack for a
    # switch, the Shield, and Beemerang Halt or Bee Fly; back down a drop, a one-way.
    Area("FactoryProcessing2", "Top Left", ("loadzone pump",), _PROCESSING2_UP,
         out=one_way(None, _PROCESSING2_UP)),
    # The Pump Room (FactoryProcessingPump; named by the user, 2026-10-10): the floor (the doors to the second room and
    # to Malbee's room, the save crystal, the cranks) the map's own region; its top left corner (the door to puzzle 1)
    # across the Shield or Bee Fly, both ways; the upper left (the door to puzzle 3) and the upper right (the door to
    # puzzle 2), each reached on the loop, left by a drop: free from the upper left, Bee Fly from the upper right.
    Area("FactoryProcessingPump", "Bottom Top Left", ("loadzonepuzzle1",), CanUse("Shield") | CanUse("Bee Fly")),
    Area("FactoryProcessingPump", "Upper Left", ("loadzonepuzzle3",), _PUMP_UP, out=one_way(None, _PUMP_UP)),
    Area("FactoryProcessingPump", "Upper Right", ("loadzonepuzzle2",), _PUMP_UP,
         out=one_way(CanUse("Bee Fly"), _PUMP_UP)),
    # FactoryProcessingPuzzle1 (the user, 2026-10-10): one region, its one door free; its only pickup, a Factory Pass,
    # stays the game's own until Next 67 (FACTORY_PASS).
    # FactoryProcessingPuzzle2 (the user, 2026-10-10): the same; the drop right of its door a one-way without Jump, to
    # the pass's side only.
    # HoneyFactoryCore (2026-10-10): one region, its one door free; the gate at its top shut until the chapter 3 finale
    # (Event99, which sets 299 and ends in the room), behind it only the empty boss arena (the user).
)
TRANSFERS = (
    # The pump room's platform loop between its upper left and upper right, both ways.
    Transfer("platforms", "FactoryProcessingPump", "FactoryProcessingPump", _PUMP_LOOP, from_area="Upper Left",
             to_area="Upper Right"),
    # The First Room's drop from the left to the bottom: back round by the right and the platforms.
    Transfer("drop", "FactoryProcessingFirstRoom", "FactoryProcessingFirstRoom", None, two_way=False,
             way_back=_UP & _ON_THE_PLATFORMS, from_area="Left", to_area="Bottom"),
)
DOOR_RULES = (
    # The Lobby's processing door, locked until the Factory Pass is used on it (keything, Event59 key index 4, then
    # Event89, flag 179); kept locked (the user, 2026-10-10). Arriving from the first room, the game pushes the party
    # past the lock (seen).
    DoorRule("HoneyFactoryEntrance", "loadzone processing", FACTORY_PASS),
    # The pump room's door to Malbee's room, behind the key scanner (Event59 key index 4; three passes, then Event96,
    # flag 217); its closed model Base/DoorE until 217.
    DoorRule("FactoryProcessingPump", "loadzonemalbee", FACTORY_PASS),
)
