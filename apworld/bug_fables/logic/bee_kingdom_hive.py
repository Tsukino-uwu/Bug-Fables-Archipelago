"""Bee Kingdom Hive (the game's area 12, MapControl.areaid): its spots, and what the seed changes there, mapped room by
room (room-checklist.md)."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..data_types import (ALWAYS_SET, Area, DialogueFlag, DoorRow, EntityRef, FlagWith, Give, Location, Pickup, Source,
                          StoryEvent, Transfer)

# Mothiva's scene in the Main Area (Event86, flag 173): talking to her, from the start; it brings the clothing stall.
_MOTHIVA_SEEN = "Mothiva's Show Seen"

LOCATIONS = (
    # Beette sells the Flower Key (the plaza's red house) on the balcony at her price, 150 berries (the user,
    # 2026-10-10); her first talk sets 227 for the offer, her next line 228. The berries in the logic come with Next 63.
    Location("Bee Kingdom Hive: Balcony, Beette's Sale", 78, "BeehiveBalcony",
             Source(flag=228, give=Give(map="BeehiveBalcony", type=1, item=54)), no_jump=True),
    # Jaune's Gallery (JaunesGallery; named by the user, 2026-10-09): one region, its door free; a Bad Book on the left
    # side, behind paintings lying on the floor, nothing needed.
    Location("Bee Kingdom Hive: Jaune's Gallery, Behind the Paintings", 207, "JaunesGallery",
             Source(flag=622, pickup=Pickup(map="JaunesGallery", type=1, item=174)), no_jump=True),
    # The Scanner Room (BeehiveScannerRoom; named by the user, 2026-10-09): its scan (Event84's first part, flag 159),
    # where the mod also sets 160, which teaches Leif Bubble Shield Lite, the game's own (as Pep Talk at the farm).
    Location("Bee Kingdom Hive: Scanner Room, Scan", 208, "BeehiveScannerRoom", Source(event=84, flag=159),
             no_jump=True),
    # The Main Area's clothing stall (from 173, Mothiva's scene, until 252) sells the Bee Hat (40 berries, flag 251),
    # then, the room entered again, the Pretty Ribbon (50 berries, 252): behind her scene and at their prices (the
    # user, 2026-10-09); the berries in the logic come with Next 63, as for every seller.
    Location("Bee Kingdom Hive: Main Area, Clothing Stall 1", 209, "BeehiveMainArea",
             Source(flag=251, give=Give(map="BeehiveMainArea", type=1, item=99)), rule=Has(_MOTHIVA_SEEN),
             no_jump=True),
    Location("Bee Kingdom Hive: Main Area, Clothing Stall 2", 210, "BeehiveMainArea",
             Source(flag=252, give=Give(map="BeehiveMainArea", type=1, item=94)), rule=Has(_MOTHIVA_SEEN),
             no_jump=True),
)
STORY_EVENTS = (
    StoryEvent("Bee Kingdom Hive: Main Area, Mothiva's Show", _MOTHIVA_SEEN, "BeehiveMainArea",
               Source(event=86, flag=173)),
    # HB's Lab (HBsLab; named by the user, 2026-10-10): shown the Explorer Permit, HB's line 53 sets 161 and the
    # computer runs B.O.S.S.; asked from the start in a seed (DIALOGUE_FLAGS).
    StoryEvent("Bee Kingdom Hive: HB's Lab, Explorer Permit Shown", "B.O.S.S. Unlocked", "HBsLab",
               Source(flag=161), rule=Has("Explorer Permit")),
)
DIALOGUE_FLAGS = (
    # HB's question for a crystal that recorded fights (line 50, from 219, after chapter 3's end in the game): asked
    # from the start (the user, 2026-10-10: "can ask for the explorer permit from the start, instead of having to do
    # other things").
    DialogueFlag("HBsLab", "HB", 219, ALWAYS_SET),
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
    # Outside the Beehive's main door into the Scanner Room, gone in the game from 160, which the scan now sets
    # (FLAGS_WITH): the room stays between the outside and the inside (the user, 2026-10-09).
    EntityRef("BeehiveOutside", "loadzonecorridor"),
)
KEPT_OPEN = (
    # Its "Out For Lunch" sign before it, there until 299.
    EntityRef("BeehiveMainArea", "jaune sign"),
    # Outside the Beehive's door straight into the main area (from 160): its main door always leads into the Scanner
    # Room instead (the user, 2026-10-09: keep the room between the outside and the inside).
    EntityRef("BeehiveOutside", "loadzoneinside"),
    # The Scanner Room's second trigger at its top, whose scene warps the party to the main area and HB's Lab: its
    # top door is the way on instead.
    EntityRef("BeehiveScannerRoom", "eventtrigger2"),
)
SCENERY_HIDDEN = (
    # Outside the Beehive's factory door's closed model, hidden in the game from 169.
    EntityRef("BeehiveOutside", "Base/Door"),
    # The throne room door's closed model in the main area, hidden in the game from 169.
    EntityRef("BeehiveMainArea", "Base/ThroneDoors"),
    # A cube before Jaune's door, hidden in the game from 299: with the sign gone it still shut the way (seen).
    EntityRef("BeehiveMainArea", "Base/Cube"),
    # The Scanner Room's gate at its top, hidden in the game from 159 (the scan): open from the start (the user,
    # 2026-10-09: "we should keep the gate open").
    EntityRef("BeehiveScannerRoom", "Base/Door"),
)
SCENERY_PRESENT = (
    # And its open model, shown in the game from 169.
    EntityRef("BeehiveMainArea", "Base/ThroneDoors (1)"),
)
# The Scanner Room's way on, a door at its top into the main area's bottom, copied from its bottom door, and the main
# area's bottom exit sent back into it, landing inside the gate (the user, 2026-10-09: "copy the bottom entrance/door
# how it works, place it where the gate is, and redirect how you come in/out of it"); spots mirrored from the bottom
# door's but the walk into it. The main area's arrival is Outside the Beehive's door into it.
_CAMERA = ((0.0, 0.0, 0.0),) * 4
DOOR_ROWS = (
    # Its walk in ends on its own spot: past it the corridor is shut, so a walk there only ends when the game gives up.
    DoorRow("BeehiveScannerRoom", "loadzoneinside", (67, 0, 0, 0, 0),
            ((0.0, 0.0, 10.3), (0.0, 0.0, -30.02), (0.0, 0.0, -25.1), *_CAMERA),
            copy=("BeehiveScannerRoom", "loadzoneoutside"), at=(0.0, 0.0, 10.3)),
    DoorRow("BeehiveMainArea", "loadzoneoutside - Duplicate", (64, 0, 0, 0, 0),
            ((0.0, 0.0, -30.5), (0.0, 0.0, 13.7), (0.0, 0.0, 7.5), *_CAMERA)),
)
# The scan (Event84's first part, 159) also sets 160, which its kept-away second part set: Leif's Bubble Shield Lite,
# the game's own, and HB gone from beside his lab (the user, 2026-10-09: "lets just give 160 alongside the scan").
FLAGS_WITH = (FlagWith(84, 159, 160),)
MAP_AREAS = (
    # Outside the Beehive (BeehiveOutside; named by the user, 2026-10-09), two parts with no way between
    # them inside the room: the bottom (the elevator bee and the hive's main door) the map's own region, nothing needed
    # across it; the left, a bridge between the hive's side door and the factory's door, cut off.
    Area("BeehiveOutside", "Left", ("loadzone inside factory side", "loadzone factory"), False_()),
    # The Throne Room (BeehiveThroneRoom; named by the user, 2026-10-09): one region, its one door free, open from
    # both sides (KEPT_PRESENT).
    # The Main Area (BeehiveMainArea; named by the user, 2026-10-09): one region, every door free (the Throne Room's
    # and Jaune's Gallery's kept open, its bottom exit into the Scanner Room's top).
    # HB's Lab (HBsLab): one region, its one door free; the gate at its top only scenery.
    # The Balcony (BeehiveBalcony; named by the user, 2026-10-10): one region, its one door free.
    # Honeycomb's Lab (HoneycombsLab; named by the user, 2026-10-10): one region, its one door free; no spot yet.
)
TRANSFERS = (
    # The bottom's elevator bee sends the party down to Defiant Root's Beehive Lift for nothing, onto its platform.
    Transfer("elevator", "BeehiveOutside", "DefiantRoot2", two_way=False, to_area="Elevator"),
)
