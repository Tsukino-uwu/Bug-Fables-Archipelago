"""Golden Path (the game's area 5, MapControl.areaid): its spots and the parts of its rooms, mapped room by room
(room-checklist.md)."""
from __future__ import annotations

from ..custom_rules import CanUse, one_way
from ..data_types import Area, Location, Pickup, Source, Transfer

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
)
TRANSFERS = (
    # The crank path's high middle door down to its right side with Jump alone, a drop; up again with Jump and Halt (the
    # way back). An Area has one link, so this second one, inside the room, is a transfer.
    Transfer("drop", "GoldenHillsPath2", "GoldenHillsPath2", CanUse("Jump"), two_way=False,
             way_back=CanUse("Jump") & CanUse("Beemerang Halt"), from_area="Top Middle"),
)
