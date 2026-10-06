"""Lost Sands (the game's area 3, MapControl.areaid): its spots, its ways between maps that aren't doors, and what the
seed changes there, mapped room by room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, EntityRef, Location, Pickup, Source, Transfer

# The Rusty Key, bought at the Defiant Root well (line 3, which also sets flag 239, making the hideout door): not an
# item yet, so the later chapters' stand-in until its sale is a location.
RUSTY_KEY = LATER_CHAPTERS

LOCATIONS = (
    # Where the game teaches the Dash (flag 699); the later chapters' story-order stand-in. Its map is the Outskirts'
    # (BOLostSandsEntrance, area 0) at the desert's border; it stays with the area its name gives.
    Location("Lost Sands: Entrance", 69, "BOLostSandsEntrance",
             Source(event=221, flag=699),
             rule=CanUse("Beemerang Halt"), reach=LATER_CHAPTERS),
    Location("Lost Sands: Badlands, Center Pillar", 114, "DesertBadlands",
             Source(flag=413, pickup=Pickup(map="DesertBadlands", type=2, item=0)),
             rule=CanUse("Jump") & CanUse("Bee Fly")),
    Location("Lost Sands: Badlands, Rock Ledge", 115, "DesertBadlands",
             Source(flag=729, pickup=Pickup(map="DesertBadlands", type=0, item=65)),
             rule=CanUse("Jump") & CanUse("Beemerang Toss")),
)
TRANSFERS = (
    # Chapter 3's end: the attack on the city.
    Transfer("story", "DesertSandCastle", "BugariaAssociationAttack", LATER_CHAPTERS, two_way=False),
)
# The border gate to the Far Grasslands, open from the start: the game makes its door and breaks the gate only in
# chapter 5 (flag 348), and a start or a shuffled door arriving there before landed behind it, falling forever.
KEPT_PRESENT = (
    EntityRef("DesertFGBorder", "loadzonefg"),
)
SCENERY_HIDDEN = (
    EntityRef("DesertFGBorder", "Base/Gate"),
)
SCENERY_PRESENT = (
    EntityRef("DesertFGBorder", "Base/GateBroken"),
)
MAP_AREAS = (
    # The entrance's door to the book area, up a ledge: no way up from inside the room, only a drop down.
    Area("DesertEntrance", "Ledge", ("WarpBookZone",), False_(), out=one_way(None, False_())),
    # The badlands' door to the bandit hideout, up two ledges (Jump) behind a grate the Rusty Key opens (flag 258);
    # arriving while it's shut, the game pushes the party past it, and the ledges drop back down freely.
    Area("DesertBadlands", "Hideout Door", ("loadzone hideout",), CanUse("Jump") & RUSTY_KEY,
         out=one_way(None, CanUse("Jump") & RUSTY_KEY)),
)
