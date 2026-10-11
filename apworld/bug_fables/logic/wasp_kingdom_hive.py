"""Wasp Kingdom Hive (the game's area 19, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import False_, True_

from ..custom_rules import CanUse
from ..data_types import Area, EntityRef

MAP_AREAS = (
    # Outside the hive (WaspKingdomOutside; 2026-10-11): from the cave's door the game pushes the party past a boulder
    # (rock, until 332), so that way in is free; back out to the cave, the boulder broken by Horn Dash (it stays broken).
    Area("WaspKingdomOutside", "Left", ("loadzonecave",), CanUse("Horn Dash"), out=True_()),
    # The hive's door (to the main hall): coming out, the party is put past its barrier; from outside the barrier
    # keeps it shut.
    Area("WaspKingdomOutside", "Hive Door", ("loadzoneinside#16",), False_(), out=True_()),
)
# The two wasp troopers on patrol and their see area (Event138, until 347) guard only the hive's door, shut by its own
# barrier; caught, the party is put back where it came in, which from the lake's gate is inside the see area, a loop.
# Kept away (2026-10-11).
KEPT_OPEN = (
    EntityRef("WaspKingdomOutside", "trooper"),
    EntityRef("WaspKingdomOutside", "trooper - Duplicate"),
    EntityRef("WaspKingdomOutside", "see area"),
)
