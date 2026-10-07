"""Golden Settlement (the game's area 6, MapControl.areaid): its spots, and what the seed changes there. Its rooms
aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse
from ..data_types import DayNight, EntityMove, EntityRef, Location, SceneCamera, SceneryMove, Source, TimeSwitch

LOCATIONS = (
    # Where the game teaches Beemerang Halt (flag 21), after the mayor's Wacka Worm game, played by Vi with the
    # Beemerang (the mod refuses it otherwise); the later chapters' story-order stand-in.
    Location("Golden Settlement: Festival, Wacka Worm Game", 68, "GoldenSettlement2",
             Source(event=55, flag=21), rule=CanUse("Beemerang Toss"), reach=LATER_CHAPTERS),
)
# The invisible wall behind the gate to the desert, which stands until the desert side has been reached (flag 170),
# gone. The gate itself stays the game's: shut until its lever is hit from the desert side (the user, 2026-10-07).
SCENERY_HIDDEN = (
    EntityRef("GoldenSettlementEntrance", "Base/Cube"),
    # The square's altar statue over the path to the Golden Hills dungeon, there until the festival's fight (flag 103,
    # Event58), swapped from the start for the moved one (below): the world open, the festival left as it is.
    EntityRef("GoldenSettlement1", "Base/Altar"),
)
SCENERY_PRESENT = (
    EntityRef("GoldenSettlement1", "Base/Altar (1)"),
)
# The festival night (flag 85 until the fight's 86) any time (the user, 2026-10-07): the mod's own night on these maps,
# switched by an NPC in each; the first nightfall is the story's own scene (Event52: its speech, discovery 13, flag 85).
DAY_NIGHT = (
    DayNight("GoldenSettlement1", "GoldenSettlement1Night", 85, 86, 52),
    DayNight("GoldenSettlement2", "GoldenSettlement2Night", 85, 86, 52),
    DayNight("GoldenSettlement3", "GoldenSettlement3Night", 85, 86, 52),
)
# What every switch says (the user, 2026-10-07): by day Aria's own nightfall prompt (GoldenSettlement1 line 19), word
# for word; at night the user's.
_DAY = ("Oh? Are you here for the festival? It should start as soon as the sun sets.", "Keep exploring.",
        "Wait for nightfall.")
_NIGHT = ("Oh? Are you enjoying the festival? It should end at daybreak.", "Keep exploring.", "Wait for dawn.")
# The square's switch: Aria before the festival (her talk the nightfall prompt), moved off the arena (Jump) to the
# ground in front of it, as far from Leif and Celia as on the arena (the user, 2026-10-07).
TIME_SWITCHES = (
    TimeSwitch("GoldenSettlement1", "Aria", (-1.8, 0.0, -1.8), _DAY, _NIGHT),
)
# Leif and Celia before the festival, on the arena with Aria for the arrival scene (Event51, which talks from where they
# stand): moved down below the arena's right side, as far apart as they stood up there (the user, 2026-10-07).
ENTITIES_MOVED = (
    EntityMove("GoldenSettlement1", "leif", (2.0, 0.0, -3.0)),
    EntityMove("GoldenSettlement1", "celia", (3.8, 0.0, -1.7)),
    # The scene's trigger, where it was from Aria on the arena.
    EntityMove("GoldenSettlement1", "first event", (0.6, 0.0, -2.45)),
)
# The arrival scene aims its camera at a fixed point on the arena: moved down with them.
SCENE_CAMERAS = (
    SceneCamera("GoldenSettlement1", 51, (0.0, 1.25, 5.0), (0.6, 1.25, -3.6)),
)
# The night square's statue has no flag of its own: slid aside as the fight's scene does (Event58, local z to 0), so
# the dungeon's door never lands behind it at night.
SCENERY_MOVED = (
    SceneryMove("GoldenSettlement1Night", "Base/Altar", (0.0, -26.0, 0.0)),
)
# The square's south door, gone on the festival night, and the trigger that turns the party back: the square open
# by night as by day (the user, 2026-10-07). The switch Aria, gone from the night on, kept too. A night map has its
# day map's entities, so these hold for both (the client reads a night map's entries under its day map's name).
KEPT_PRESENT = (
    EntityRef("GoldenSettlement1", "Loadzonesouth"),
    EntityRef("GoldenSettlement1", "Aria"),
)
KEPT_OPEN = (
    EntityRef("GoldenSettlement1", "blocker"),
)
