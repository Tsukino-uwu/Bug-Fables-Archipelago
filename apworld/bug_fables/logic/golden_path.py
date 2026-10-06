"""Golden Path (the game's area 5, MapControl.areaid): its spots and the parts of its rooms, mapped room by room
(room-checklist.md)."""
from __future__ import annotations

from ..custom_rules import CanUse, one_way
from ..data_types import Area, Location, Pickup, Source

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
)
MAP_AREAS = (
    # The cable car station's right door (to the tunnel), up high: down a drop; back up by Jump, or the bounce pad past
    # grass (the horn).
    Area("GoldenHillsCableCar", "Right Door", ("loadzonetunnel",), CanUse("Jump") | CanUse("Horn Slash"),
         out=one_way(None, CanUse("Jump") | CanUse("Horn Slash"))),
    # Its left side (the door to the second path): Jump across, both ways.
    Area("GoldenHillsCableCar", "Left", ("loadzonepath",), CanUse("Jump")),
)
