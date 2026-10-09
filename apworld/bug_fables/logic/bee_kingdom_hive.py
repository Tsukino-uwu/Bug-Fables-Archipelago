"""Bee Kingdom Hive (the game's area 12, MapControl.areaid): its spots, and what the seed changes there, mapped room by
room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_

from ..custom_rules import LATER_CHAPTERS
from ..data_types import Area, EntityRef, FreeSale, Give, Location, Pickup, Source, Transfer

LOCATIONS = (
    # Beette sells the Flower Key (the plaza's red house) on the balcony, free in a seed; her next line sets flag 228.
    Location("Bee Kingdom Hive: Balcony, Beette's Sale", 78, "BeehiveBalcony",
             Source(flag=228, give=Give(map="BeehiveBalcony", type=1, item=54)), reach=LATER_CHAPTERS),
    # Jaune's Gallery (JaunesGallery; named by the user, 2026-10-09): one region, its door free; a Bad Book on the left
    # side, behind paintings lying on the floor, nothing needed.
    Location("Bee Kingdom Hive: Jaune's Gallery, Behind the Paintings", 207, "JaunesGallery",
             Source(flag=622, pickup=Pickup(map="JaunesGallery", type=1, item=174)), no_jump=True),
)
KEPT_PRESENT = (
    # Beette herself, made only after chapter 3 (flag 299) (the user, 2026-10-04: "make it appear always").
    EntityRef("BeehiveBalcony", "smug bee"),
    # Outside the Beehive's factory door, made in the game only from 299, open from the start with its other half inside
    # (honey_factory.py) (the user, 2026-10-09: "always have this door be open").
    EntityRef("BeehiveOutside", "loadzone factory"),
    # The main area's door to the throne room, made in the game only from 169: open from the start (the user,
    # 2026-10-09: "i think we should just keep it open"); the throne room's half has no flag.
    EntityRef("BeehiveMainArea", "loadzone throne"),
    # The main area's door to Jaune's Gallery, made in the game only from 299: open from the start (the user,
    # 2026-10-09: "can we remove this sign/block from this entrance"); the gallery's half has no flag.
    EntityRef("BeehiveMainArea", "loadzonejaune"),
)
KEPT_OPEN = (
    # Its "Out For Lunch" sign before it, there until 299.
    EntityRef("BeehiveMainArea", "jaune sign"),
)
SCENERY_HIDDEN = (
    # Outside the Beehive's factory door's closed model, hidden in the game from 169.
    EntityRef("BeehiveOutside", "Base/Door"),
    # The throne room door's closed model in the main area, hidden in the game from 169.
    EntityRef("BeehiveMainArea", "Base/ThroneDoors"),
    # A cube before Jaune's door, hidden in the game from 299: with the sign gone it still shut the way (seen).
    EntityRef("BeehiveMainArea", "Base/Cube"),
)
SCENERY_PRESENT = (
    # And its open model, shown in the game from 169.
    EntityRef("BeehiveMainArea", "Base/ThroneDoors (1)"),
)
FREE_SALES = (
    # Her offer ("150 berries for the house") and the sale's price commands.
    FreeSale("BeehiveBalcony", (20, 21)),
)
MAP_AREAS = (
    # Outside the Beehive (BeehiveOutside; named by the user, 2026-10-09), two parts with no way between
    # them inside the room: the bottom (the elevator bee and the hive's main door) the map's own region, nothing needed
    # across it; the left, a bridge between the hive's side door and the factory's door, cut off.
    Area("BeehiveOutside", "Left", ("loadzone inside factory side", "loadzone factory"), False_()),
    # The Throne Room (BeehiveThroneRoom; named by the user, 2026-10-09): one region, its one door free, open from
    # both sides (KEPT_PRESENT).
)
TRANSFERS = (
    # The bottom's elevator bee sends the party down to Defiant Root's Beehive Lift for nothing, onto its platform.
    Transfer("elevator", "BeehiveOutside", "DefiantRoot2", two_way=False, to_area="Elevator"),
)
