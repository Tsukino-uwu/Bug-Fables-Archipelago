"""Golden Path (the game's area 5, MapControl.areaid): its spots and the parts of its rooms, mapped room by room
(room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import True_

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, ItemShop, Location, Pickup, Source, Transfer

# The beetle's horn quest in the settlement (GoldenSettlement3, line 34, flag 274), then Tanjerin by the settlement
# entrance's minigame door (flag 275): not gone through yet, so the later chapters' stand-in until the quest pass.
HORN_QUEST = LATER_CHAPTERS

LOCATIONS = (
    # The cable car station: crystal berry #8 up on its right, by the bounce pad past grass (the horn).
    Location("Golden Path: Cable Car Station, High Ledge", 134, "GoldenHillsCableCar",
             Source(berry=8, pickup=Pickup(map="GoldenHillsCableCar", type=3, item=0)), rule=CanUse("Horn Slash"),
             category="crystal_berry", no_jump=True),
    Location("Golden Path: Cable Car Station, Dig Spot", 135, "GoldenHillsCableCar",
             Source(flag=397, pickup=Pickup(map="GoldenHillsCableCar", type=0, item=121)),
             rule=CanUse("Jump") & CanUse("Beetle Dig"), category="dig_spot"),
    # A respawning Clear Water in a bush between the bounce pad and the save crystal (regional flag 0).
    Location("Golden Path: Cable Car Station, Bush by the Save Crystal", 136, "GoldenHillsCableCar",
             Source(regional=0, pickup=Pickup(map="GoldenHillsCableCar", type=0, item=12)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
    # A Sweet Dew on a wooden platform between boulders, on the crank path's left side.
    Location("Golden Path: Crank Path, Boulder Platform", 137, "GoldenHillsPath2",
             Source(flag=726, pickup=Pickup(map="GoldenHillsPath2", type=0, item=50)),
             rule=CanUse("Jump") & CanUse("Beemerang Halt"), area="Left"),
    # The settlement entrance's caravan stall, kept present for good (in the game it leaves after the Golden Hills boss,
    # flag 88, and the snail's shop takes its spot): first purchase a check, then its own item.
    Location("Golden Path: Settlement Entrance, Caravan Shop 1", 138, "GoldenSettlementEntrance",
             Source(item_shop=ItemShop(map="GoldenSettlementEntrance", keeper="Crickerly", item=2)),
             category="item_shop", no_jump=True),
    Location("Golden Path: Settlement Entrance, Caravan Shop 2", 139, "GoldenSettlementEntrance",
             Source(item_shop=ItemShop(map="GoldenSettlementEntrance", keeper="Crickerly", item=147)),
             category="item_shop", no_jump=True),
    Location("Golden Path: Settlement Entrance, Caravan Shop 3", 140, "GoldenSettlementEntrance",
             Source(item_shop=ItemShop(map="GoldenSettlementEntrance", keeper="Crickerly", item=11)),
             category="item_shop", no_jump=True),
    # Crystal berry #22, dug up in the middle of the settlement entrance.
    Location("Golden Path: Settlement Entrance, Dig Spot", 141, "GoldenSettlementEntrance",
             Source(berry=22, pickup=Pickup(map="GoldenSettlementEntrance", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
)
MAP_AREAS = (
    # The cable car station's right door (to the tunnel), up high: down a drop; back up by Jump, or the bounce pad past
    # grass (the horn).
    Area("GoldenHillsCableCar", "Right Door", ("loadzonetunnel",), CanUse("Jump") | CanUse("Horn Slash"),
         out=one_way(None, CanUse("Jump") | CanUse("Horn Slash"))),
    # Its left side (the door to the second path): Jump across, both ways.
    Area("GoldenHillsCableCar", "Left", ("loadzonepath",), CanUse("Jump")),
    # The crank path: its left side (the door toward the settlement) across from the right door by its fixed cranks
    # (Beemerang Halt) and Jump, both ways.
    Area("GoldenHillsPath2", "Left", ("loadzonesettlement",), CanUse("Jump") & CanUse("Beemerang Halt")),
    # Its high middle door (to the pitcher path), to and from the left side with Jump and Halt, both ways.
    Area("GoldenHillsPath2", "Top Middle", ("loadzonepitcherarea",), CanUse("Jump") & CanUse("Beemerang Halt"),
         to="GoldenHillsPath2 (Left)"),
    # The settlement entrance's minigame door, behind a rock (`Base/Big Plain Rock`) gone at flag 275 (the horn quest,
    # then Tanjerin); coming out of it, the game pushes the party through the rock.
    Area("GoldenSettlementEntrance", "Minigame Door", ("loadzone minigame",), HORN_QUEST, out=True_()),
)
# The caravan's stall there for good, the snail's shop that takes its spot after the boss kept away (the user,
# 2026-10-07: the snail's goods overlapped; the caravan's other stalls to be decided one at a time).
KEPT_PRESENT = (
    EntityRef("GoldenSettlementEntrance", "Crickerly"),
    EntityRef("GoldenSettlementEntrance", "Husband"),
    EntityRef("GoldenSettlementEntrance", "CaravanBadge - Duplicate"),
)
KEPT_OPEN = (
    EntityRef("GoldenSettlementEntrance", "snailguy"),
)
SCENERY_PRESENT = (
    EntityRef("GoldenSettlementEntrance", "Base/Stall"),
)
SCENERY_HIDDEN = (
    EntityRef("GoldenSettlementEntrance", "Base/snailmerchant"),
)
TRANSFERS = (
    # The crank path's high middle door down to its right side with Jump alone, a drop; up again with Jump and Halt (the
    # way back). An Area has one link, so this second one, inside the room, is a transfer.
    Transfer("drop", "GoldenHillsPath2", "GoldenHillsPath2", CanUse("Jump"), two_way=False,
             way_back=CanUse("Jump") & CanUse("Beemerang Halt"), from_area="Top Middle"),
)
