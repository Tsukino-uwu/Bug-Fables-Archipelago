"""Enemysanity: every map enemy's won fight is a location, its check dropped as a pickup (the mod guide, step 44).

Built from data/enemies.json (each enemy's area, first enemy's game name and fixed location id). Named "Area: Room,
Enemy N" (the user, 2026-10-04): the room as the map's own named spots call it, else the map's name until the room is
named; N counts that enemy within the room. Every one is kept present whatever the story's flags (the client), so none
is missable; until each room is mapped they wait for the later chapters, the cautious stand-in.
"""
from __future__ import annotations

from collections import Counter

from .custom_rules import LATER_CHAPTERS
from .data_types import Encounter, Location, Source

# MapControl.areaid's names, as the area modules (logic/) call them.
AREA_NAMES: dict[int, str] = {
    0: "Outskirts", 1: "Snakemouth Den", 2: "Bugaria City", 3: "Lost Sands", 4: "Golden Hills", 5: "Golden Path",
    6: "Golden Settlement", 7: "Forsaken Lands", 8: "Far Grasslands", 9: "Wild Swamplands", 10: "Defiant Root",
    11: "Ancient Castle", 12: "Bee Kingdom Hive", 13: "Honey Factory", 14: "Rubber Prison", 15: "Giant's Lair",
    16: "Metal Lake", 17: "Metal Island", 18: "Termite Capitol", 19: "Wasp Kingdom Hive", 20: "Bandit Hideout",
    21: "Stream Mountain", 22: "Chomper Caves", 23: "Fishing Village", 24: "Upper Snakemouth",
}


def room_names(spots: tuple[Location, ...]) -> dict[str, str]:
    """Each map's "Area: Room", as most of its named spots begin."""
    prefixes: dict[str, Counter[str]] = {}
    for spot in spots:
        if ", " in spot.name:
            prefixes.setdefault(spot.region, Counter())[spot.name.split(", ")[0]] += 1
    return {region: counts.most_common(1)[0][0] for region, counts in prefixes.items()}


def enemy_key(encounter: Encounter) -> str:
    return f"{encounter.map}:{encounter.entity}"


def enemy_locations(encounters: tuple[Encounter, ...], spots: tuple[Location, ...],
                    maps: tuple[str, ...]) -> tuple[Location, ...]:
    rooms = room_names(spots)
    counts: Counter[tuple[str, str]] = Counter()
    found = []
    for encounter in sorted(encounters, key=lambda e: (e.map, e.entity)):
        if encounter.map not in maps or encounter.location is None:
            continue
        room = rooms.get(encounter.map, f"{AREA_NAMES.get(encounter.area, 'Bug Fables')}: {encounter.map}")
        counts[(room, encounter.name)] += 1
        found.append(Location(f"{room}, {encounter.name} {counts[(room, encounter.name)]}", encounter.location,
                              encounter.map, Source(enemy=enemy_key(encounter)), category="enemy",
                              reach=LATER_CHAPTERS))
    return tuple(found)
