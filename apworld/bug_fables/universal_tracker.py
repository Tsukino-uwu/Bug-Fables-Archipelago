"""Universal Tracker: a seed rebuilt from its slot_data, with no yaml. Universal Tracker regenerates this world on its
own with re_gen_passthrough["Bug Fables"] holding the seed's slot_data, and every roll then comes from that instead of
the random (its docs/apworld-integration.md, "Generating without a YAML"). Its map tab: the plain diagrams in ut_map/,
drawn by dev-scripts/ut-map.py (its docs/map-integration.md, the internal approach)."""
from __future__ import annotations

import json
import pkgutil
from collections.abc import Mapping
from typing import TYPE_CHECKING, Any

from .data_tables import (
    DOOR_LABELS,
    ENEMY_LOCATIONS,
    ENEMY_NAMES,
    LOCATION_NAME_TO_ID,
    ONE_WAYS,
    door_name,
    game_door_name,
)
from .options import BugFablesOptions, EntranceRandomizer

if TYPE_CHECKING:
    from .world import BugFablesWorld

# The doors the player has gone through: the mod adds each door's entrance name to this data storage key, and the
# PopTracker pack reads it too. Universal Tracker fills in {team} and {player} (its TrackerClient's key.format).
DOORS_TAKEN_KEY = "bug_fables_doors_{team}_{player}"
# The map the player is on, its MapControl.mapid's name, written by the mod at each map load (build step 57).
MAP_KEY = "bug_fables_map_{team}_{player}"
# The map tab's files, read the first time Universal Tracker asks: the tabs in maps.json's order, and follow.json's
# marker point on each tab for each map the player can be on.
_TABS: dict[str, Any] = {}


def map_file(path: str):
    """A file of the map tab (ut_map/), its bytes, read from the world's package as Universal Tracker reads it."""
    raw = pkgutil.get_data(__name__, f"ut_map/{path}")
    if raw is None:
        raise FileNotFoundError(f"bug_fables: ut_map/{path} is missing from the world package")
    return raw


def _tab_data() -> tuple[list[str], dict[str, dict[str, list[int]]]]:
    if not _TABS:
        for name in ("maps", "follow"):
            _TABS[name] = json.loads(map_file(f"{name}.json").decode("utf-8"))
        _TABS["names"] = [tab["name"] for tab in _TABS["maps"]]
    return _TABS["names"], _TABS["follow"]


def map_page_index(value: Any) -> int:
    """The tab for the player's map: its room's own. The World before the mod has said (an empty or missing key),
    and -1, keeping the tab, for a map with no tab (the title screen)."""
    names, _ = _tab_data()
    if not value:
        return 0
    return names.index(value) if isinstance(value, str) and value in names else -1


def location_icon_coords(index: Any, value: Any) -> tuple[int, int, str] | None:
    """The marker on the shown tab: by the player's area on the World, their room on its area's tab, its title on
    the room's own; none on other tabs, before a tab is shown or before the mod has said."""
    names, follow = _tab_data()
    if not isinstance(index, int) or not isinstance(value, str) or not 0 <= index < len(names):
        return None
    point = follow.get(names[index], {}).get(value)
    return (point[0], point[1], "images/you.png") if point else None


# Universal Tracker's map tab (tracker_world on the world class).
TRACKER_WORLD: dict[str, Any] = {
    "map_page_folder": "ut_map",
    "map_page_maps": "maps.json",
    "map_page_locations": "locations.json",
    "map_page_layouts": "layout.json",
    "map_page_setting_key": MAP_KEY,
    "map_page_index": map_page_index,
    "location_setting_key": MAP_KEY,
    "location_icon_coords": location_icon_coords,
}
# The mod's doors key names each door as the game does (game_door_name): each to its entrance. A one-way door's story
# copies are other doors in the game but one entrance here.
_TAKEN = {**{game_door_name(*door): label for door, label in DOOR_LABELS.items()},
          **{game_door_name(w.map, copy): door_name(w.map, w.door) for w in ONE_WAYS for copy in w.copies}}
_ONE_WAYS = {(w.map, w.door) for w in ONE_WAYS}


def passthrough(world: BugFablesWorld) -> Mapping[str, Any] | None:
    """The seed's slot_data while Universal Tracker regenerates it; None in a real generation."""
    return getattr(world.multiworld, "re_gen_passthrough", {}).get(world.game)


def _refused(message: str) -> Exception:
    """Universal Tracker's own refusal (its TrackerException: "your options are incompatible", the reason in its
    /faris_asked); the same message as a ValueError where it isn't installed."""
    try:
        from worlds.tracker import TrackerException
    except ImportError:
        return ValueError(message)
    return TrackerException(message=message)


def apply_options(world: BugFablesWorld, slot_data: Mapping[str, Any]) -> None:
    """The options as the seed applied them (slot_data's options); every other option at its default, as in Universal
    Tracker's empty yaml. A seed from another version of the world is refused: no support for older versions."""
    version = slot_data.get("world_version")
    ours = world.world_version.as_simple_string()
    if version != ours:
        raise _refused(f"Bug Fables: this seed was generated with world version {version}, and this apworld is "
                       f"{ours}: use the latest release of both")
    if "options" not in slot_data:
        raise _refused("Bug Fables: this seed's slot_data has no options, so it comes from an older apworld: use the "
                       "latest release")
    sent = slot_data["options"]
    world.options = BugFablesOptions(**{name: option.from_any(sent.get(name, option.default))
                                        for name, option in BugFablesOptions.type_hints.items()})


def enemy_aliases(world: BugFablesWorld) -> dict[int, str]:
    """With Enemysanity and Enemy Shuffle on, each enemy spot's real fight, which Universal Tracker shows after the
    location's name (location_id_to_alias): "fights Seedling, Seedling". The names themselves never change."""
    if not world.options.enemy_sanity or not world.enemy_swaps:
        return {}
    return {LOCATION_NAME_TO_ID[loc.name]: "fights " + ", ".join(ENEMY_NAMES.get(i, str(i))
                                                                for i in world.enemy_swaps[loc.source.enemy])
            for loc in ENEMY_LOCATIONS if loc.source.enemy in world.enemy_swaps}


def without_alias(label: str, known: Mapping[str, Any]) -> str:
    """A list line's location name, the alias Universal Tracker puts after it (" (fights …)") set aside."""
    return label if label in known or " (" not in label else label.rsplit(" (", 1)[0]


def defer_doors(world: BugFablesWorld) -> None:
    """Universal Tracker's deferred entrances: while it rebuilds a seed with shuffled doors, each one stays unconnected
    until the player has gone through it, so the tracker never shows the spoiler's doors. Its host.yaml
    enforce_deferred_entrances decides: "on" and "default" (its default) defer, "off" shows every door."""
    world.deferred_doors = {}
    world.way_back = {}
    deferring = getattr(world.multiworld, "enforce_deferred_connections", "off") in ("on", "default")
    if not world.door_pairings or not deferring:
        return
    pairs = set(world.door_pairings)
    coupled = world.options.entrance_randomizer != EntranceRandomizer.option_decoupled
    for x, y in world.door_pairings:
        entrance = world.get_entrance(door_name(*x))
        region = entrance.connected_region
        region.entrances.remove(entrance)
        entrance.connected_region = None
        world.deferred_doors[entrance.name] = region
        # Coupled and Room Swap: walking through x comes out of y, whose door leads back (a one-way lands, no door).
        if coupled and x not in _ONE_WAYS and (y, x) in pairs:
            world.way_back[entrance.name] = door_name(*y)
    world.found_entrances_datastorage_key = DOORS_TAKEN_KEY


def reconnect_doors(world: BugFablesWorld, taken: Any) -> None:
    """The doors the player has gone through, from the doors key (None before the first), each named as the game names
    it: each deferred one connected again, with its way back when the doors are coupled. Anything but a list of names
    is left alone."""
    for listed in taken if isinstance(taken, list) else ():
        if not isinstance(listed, str):
            continue
        name = _TAKEN.get(listed, listed)
        for door in (name, world.way_back.get(name)):
            region = world.deferred_doors.pop(door, None) if door else None
            if region is not None:
                world.get_entrance(door).connect(region)
