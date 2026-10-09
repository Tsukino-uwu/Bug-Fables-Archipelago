"""Defiant Root (the game's area 10, MapControl.areaid): its spots, and what the seed changes there, mapped room by room
(room-checklist.md)."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse, one_way
from ..data_types import Area, FlagSwap, Give, Location, Pickup, Source, Transfer

# The Desert Key (92), the mayor's in the Wacka Worm room once his quest 46 is taken (Event55, flags 557-559): not an
# item yet, so the later chapters' stand-in until the quest pass.
DESERT_KEY = LATER_CHAPTERS

LOCATIONS = (
    # The Square (DefiantRoot1; named by the user, 2026-10-09): a Lore Book behind a box at the far left, and Morty's
    # Bed Bug (his first line; Pibu's sale of it, line 28, stays the game's), both on the ground, nothing needed.
    Location("Defiant Root: Square, Behind the Box", 189, "DefiantRoot1",
             Source(flag=142, pickup=Pickup(map="DefiantRoot1", type=1, item=52)), no_jump=True),
    Location("Defiant Root: Square, Morty's Gift", 190, "DefiantRoot1",
             Source(flag=157, npc="Morty", give=Give(map="DefiantRoot1", type=1, item=89)), no_jump=True),
    # On the rooftops: a Berry Juice on the right, crystal berry #15 on the left; nothing once up.
    Location("Defiant Root: Square, Right Rooftop", 191, "DefiantRoot1",
             Source(flag=688, pickup=Pickup(map="DefiantRoot1", type=0, item=39)), no_jump=True, area="Rooftops"),
    Location("Defiant Root: Square, Left Rooftop", 192, "DefiantRoot1",
             Source(berry=15, pickup=Pickup(map="DefiantRoot1", type=3, item=0)), category="crystal_berry",
             no_jump=True, area="Rooftops"),
    # The mayor's storage, behind its back door on the rooftops, locked until the Desert Key is used (flag 560): a Lore
    # Book and Dark Cherries.
    Location("Defiant Root: Square, Mayor's Storage 1", 193, "DefiantRoot1",
             Source(flag=489, pickup=Pickup(map="DefiantRoot1", type=1, item=52)), rule=DESERT_KEY, no_jump=True,
             area="Rooftops"),
    Location("Defiant Root: Square, Mayor's Storage 2", 194, "DefiantRoot1",
             Source(flag=490, pickup=Pickup(map="DefiantRoot1", type=0, item=121)), rule=DESERT_KEY, no_jump=True,
             area="Rooftops"),
    # The Well (DefiantRootWell; named by the user): a Leaf Croissant on top of boxes on its right side, Jump.
    Location("Defiant Root: Well, By the Boxes", 195, "DefiantRootWell",
             Source(flag=734, pickup=Pickup(map="DefiantRootWell", type=0, item=148)), rule=CanUse("Jump"),
             area="Right"),
)
MAP_AREAS = (
    # The town (DefiantRoot1; the user, 2026-10-09): its ground the map's own region, with its four doors (the well's
    # down a hole, out again with nothing), the save crystal and every NPC. The rooftops (the mayor's front door, Berry
    # Juice, crystal berry #15 and the Spicy Berry seller, the storage's locked back door) up with Jump, a drop back
    # down.
    Area("DefiantRoot1", "Rooftops", (), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
    # The well (DefiantRootWell; the user, 2026-10-09): its bottom left (the landing from the town, and a bounce pad
    # back up to its door) the map's own region; its right side (the door to the hideout's garden, a Leaf Croissant)
    # to and from it by burrowing, Beetle Dig.
    Area("DefiantRootWell", "Right", ("loadzonehideout",), CanUse("Beetle Dig")),
)
TRANSFERS = (
    Transfer("elevator", "DefiantRoot2", "BeehiveOutside", LATER_CHAPTERS),
)
# Crystal berry #15 on the town's rooftops is there until flag 201, which entering DesertDRSouthEntrance or the caravan
# robbery (Event93, which gives #15 itself) sets: never in a seed, so only taking #15 hides it (build step 64).
LIMIT_FLAGS = (
    FlagSwap("DefiantRoot1", "crystalberryskip", 201, -1),
)
