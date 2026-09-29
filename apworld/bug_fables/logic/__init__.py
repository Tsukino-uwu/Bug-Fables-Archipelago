"""The logic, one module per game area: its regions and the ways out of them, its spots and story events, and the
entities the seed changes there. Rules are Archipelago's Rule Builder rules (custom_rules.py holds Bug Fables' own).

A spot lives in its region's module; only exits lead from one module into another. Menu, the origin, is in regions.py.
"""
from __future__ import annotations

from ..data_types import Artifact, DialogueFlag, EntityRef, FlagEntity, Location, Region, StoryEvent
from . import bugaria_city, later_chapters, metal_island, outskirts, snakemouth_den

AREAS = (outskirts, snakemouth_den, bugaria_city, metal_island, later_chapters)

REGIONS: tuple[Region, ...] = tuple(region for area in AREAS for region in area.REGIONS)
# By id, so moving a spot from one module to another never changes a seed.
LOCATIONS: tuple[Location, ...] = tuple(sorted(
    (loc for area in AREAS for loc in getattr(area, "LOCATIONS", ())), key=lambda loc: loc.id))
STORY_EVENTS: tuple[StoryEvent, ...] = tuple(e for area in AREAS for e in getattr(area, "STORY_EVENTS", ()))
ARTIFACTS: tuple[Artifact, ...] = tuple(sorted(
    (a for area in AREAS for a in getattr(area, "ARTIFACTS", ())), key=lambda artifact: artifact.number))
KEPT_OPEN: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "KEPT_OPEN", ()))
KEPT_PRESENT: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "KEPT_PRESENT", ()))
SCENERY_HIDDEN: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENERY_HIDDEN", ()))
SCENERY_PRESENT: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENERY_PRESENT", ()))
HELD_UNTIL: tuple[FlagEntity, ...] = tuple(e for area in AREAS for e in getattr(area, "HELD_UNTIL", ()))
PRESENT_FROM: tuple[FlagEntity, ...] = tuple(e for area in AREAS for e in getattr(area, "PRESENT_FROM", ()))
DIALOGUE_FLAGS: tuple[DialogueFlag, ...] = tuple(e for area in AREAS for e in getattr(area, "DIALOGUE_FLAGS", ()))
