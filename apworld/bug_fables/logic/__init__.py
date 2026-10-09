"""The logic, one module per game area: its spots and story events, its door gates and ways between maps that aren't
doors, what reaching its spots needs until the rooms are mapped, and the entities the seed changes there. Rules are
Archipelago's Rule Builder rules (custom_rules.py holds Bug Fables' own).

Every map is a region, and so is each part of one a roadblock cuts off (MAP_AREAS); every door is an entrance of its
region (regions.py, from data/doors.json). Menu, the
origin, is in regions.py.
"""
from __future__ import annotations

from ..data_types import (Area, Artifact, DayNight, DialogueFlag, DoorRow, DoorRule, EntityMove, EntityRef, FlagEntity,
                          FlagSwap, FlagWith, FreeSale, ItemEntity, Location, MapScene, Roadblock, SceneCamera,
                          SceneryMove, StoryEvent, TimeSwitch, Transfer)
from . import (ancient_castle, bandit_hideout, bee_kingdom_hive, bugaria_city, chomper_caves, defiant_root,
               far_grasslands, fishing_village, forsaken_lands, giants_lair, golden_hills, golden_path,
               golden_settlement, honey_factory, lost_sands, metal_island, metal_lake, outskirts, rubber_prison,
               snakemouth_den, stream_mountain, termite_capitol, upper_snakemouth, wasp_kingdom_hive,
               wild_swamplands)

# One per game area (MapControl.areaid, named after its AreaNames entry): the first three as the story starts, then
# by area number. An area's module holds what's on its maps.
AREAS = (outskirts, snakemouth_den, bugaria_city, lost_sands, golden_hills, golden_path, golden_settlement,
         forsaken_lands, far_grasslands, wild_swamplands, defiant_root, ancient_castle, bee_kingdom_hive, honey_factory,
         rubber_prison, giants_lair, metal_lake, metal_island, termite_capitol, wasp_kingdom_hive, bandit_hideout,
         stream_mountain, chomper_caves, fishing_village, upper_snakemouth)

DOOR_RULES: tuple[DoorRule, ...] = tuple(rule for area in AREAS for rule in getattr(area, "DOOR_RULES", ()))
TRANSFERS: tuple[Transfer, ...] = tuple(t for area in AREAS for t in getattr(area, "TRANSFERS", ()))
# Parts of a map cut off from the rest, each its own region (regions.py).
MAP_AREAS: tuple[Area, ...] = tuple(a for area in AREAS for a in getattr(area, "MAP_AREAS", ()))
# The Extra Roadblocks choices (roadblock_options.py), each by its option key.
ROADBLOCKS: tuple[Roadblock, ...] = tuple(r for area in AREAS for r in getattr(area, "ROADBLOCKS", ()))
# By id, so moving a spot from one module to another never changes a seed.
LOCATIONS: tuple[Location, ...] = tuple(sorted(
    (loc for area in AREAS for loc in getattr(area, "LOCATIONS", ())), key=lambda loc: loc.id))
# Universal Tracker's list order (custom_ut_sort): the areas as the story reaches them, each one's spots by name.
TRACKER_ORDER: dict[str, int] = {loc.name: rank for rank, loc in enumerate(
    loc for area in AREAS for loc in sorted(getattr(area, "LOCATIONS", ()), key=lambda loc: loc.name))}
STORY_EVENTS: tuple[StoryEvent, ...] = tuple(e for area in AREAS for e in getattr(area, "STORY_EVENTS", ()))
ARTIFACTS: tuple[Artifact, ...] = tuple(sorted(
    (a for area in AREAS for a in getattr(area, "ARTIFACTS", ())), key=lambda artifact: artifact.number))
KEPT_OPEN: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "KEPT_OPEN", ()))
KEPT_PRESENT: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "KEPT_PRESENT", ()))
SCENERY_HIDDEN: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENERY_HIDDEN", ()))
SCENERY_PRESENT: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENERY_PRESENT", ()))
HELD_UNTIL: tuple[FlagEntity, ...] = tuple(e for area in AREAS for e in getattr(area, "HELD_UNTIL", ()))
PRESENT_FROM: tuple[FlagEntity, ...] = tuple(e for area in AREAS for e in getattr(area, "PRESENT_FROM", ()))
PRESENT_WITH_ITEM: tuple[ItemEntity, ...] = tuple(e for area in AREAS for e in getattr(area, "PRESENT_WITH_ITEM", ()))
HELD_UNTIL_ITEM: tuple[ItemEntity, ...] = tuple(e for area in AREAS for e in getattr(area, "HELD_UNTIL_ITEM", ()))
DIALOGUE_FLAGS: tuple[DialogueFlag, ...] = tuple(e for area in AREAS for e in getattr(area, "DIALOGUE_FLAGS", ()))
# Entities whose flags are repointed: those that borrow a goal flag, so none can set it and each behaves as it would
# with it set (build step 60); a pickup the story would take away, kept until it's taken (build step 64).
ACTIVATION_FLAGS: tuple[FlagSwap, ...] = tuple(e for area in AREAS for e in getattr(area, "ACTIVATION_FLAGS", ()))
LIMIT_FLAGS: tuple[FlagSwap, ...] = tuple(e for area in AREAS for e in getattr(area, "LIMIT_FLAGS", ()))
# Map start-up scenes that would move the party where the logic doesn't expect it: never played in a seed.
SCENES_KEPT_AWAY: tuple[MapScene, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENES_KEPT_AWAY", ()))
FREE_SALES: tuple[FreeSale, ...] = tuple(e for area in AREAS for e in getattr(area, "FREE_SALES", ()))
DAY_NIGHT: tuple[DayNight, ...] = tuple(e for area in AREAS for e in getattr(area, "DAY_NIGHT", ()))
TIME_SWITCHES: tuple[TimeSwitch, ...] = tuple(e for area in AREAS for e in getattr(area, "TIME_SWITCHES", ()))
SCENERY_MOVED: tuple[SceneryMove, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENERY_MOVED", ()))
# Scenery with no flag of its own, switched off on load (a night map's copy of something its day map hides by flag).
SCENERY_OFF: tuple[EntityRef, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENERY_OFF", ()))
ENTITIES_MOVED: tuple[EntityMove, ...] = tuple(e for area in AREAS for e in getattr(area, "ENTITIES_MOVED", ()))
# Doors the client adds to a map or sends elsewhere, and flags it sets with another (the Bee Kingdom's Scanner Room).
DOOR_ROWS: tuple[DoorRow, ...] = tuple(e for area in AREAS for e in getattr(area, "DOOR_ROWS", ()))
FLAGS_WITH: tuple[FlagWith, ...] = tuple(e for area in AREAS for e in getattr(area, "FLAGS_WITH", ()))
SCENE_CAMERAS: tuple[SceneCamera, ...] = tuple(e for area in AREAS for e in getattr(area, "SCENE_CAMERAS", ()))
