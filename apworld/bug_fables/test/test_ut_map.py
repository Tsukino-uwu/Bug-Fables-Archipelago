"""Universal Tracker's map tab (ut_map/, drawn by dev-scripts/ut-map.py): every pin a real location or entrance, every
spot and door on its room's tab, no two pins of a kind at one point, every picture there, and the tab and marker
functions safe on what Universal Tracker hands them before the mod has said anything."""
from __future__ import annotations

import json
from typing import Any
from unittest import TestCase

from ..data_tables import LOCATIONS, MAP_AREA_IDS, MAPS
from ..enemysanity import AREA_NAMES
from ..regions import MENU, logic_entrances
from ..universal_tracker import MAP_KEY, TRACKER_WORLD, location_icon_coords, map_file, map_page_index


def read(name: str) -> Any:
    return json.loads(map_file(name).decode("utf-8"))


def pin_groups(nodes: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """The pin groups as Universal Tracker flattens them: a node with map_locations, or its children's."""
    found = []
    for node in nodes:
        if "map_locations" in node:
            found.append(node)
        else:
            found.extend(pin_groups(node.get("children", [])))
    return found


class TestUTMapTab(TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.maps = read("maps.json")
        cls.tabs = [m["name"] for m in cls.maps]
        cls.groups = pin_groups(read("locations.json"))
        cls.locations = {loc.name: loc for loc in LOCATIONS}
        cls.entrances = {e.name: e for e in logic_entrances() if e.from_map != MENU}

    def test_every_pin_is_a_location_or_an_entrance(self) -> None:
        for group in self.groups:
            names = [s["name"] for s in group["sections"]]
            kinds = {"location" if n in self.locations else "entrance" if n in self.entrances else n for n in names}
            self.assertTrue(kinds <= {"location", "entrance"}, f"{group['name']}: {kinds}")
            self.assertEqual(len(kinds), 1, f"{group['name']} mixes locations and entrances")
            for place in group["map_locations"]:
                self.assertIn(place["map"], self.tabs)

    def test_every_spot_and_door_on_its_room_tab(self) -> None:
        on_room = {}
        for group in self.groups:
            for place in group["map_locations"]:
                if place["map"] in MAPS:
                    for section in group["sections"]:
                        on_room.setdefault(section["name"], []).append(place["map"])
        for loc in LOCATIONS:
            if not loc.pending:
                self.assertEqual(on_room.get(loc.name), [loc.region], loc.name)
        for name, entrance in self.entrances.items():
            if entrance.door:
                self.assertEqual(on_room.get(name), [entrance.from_map.split(" (", 1)[0]], name)

    def test_no_two_pins_of_a_kind_at_one_point(self) -> None:
        # Universal Tracker keys pins by their place on a tab: one would hide the other.
        seen = set()
        for group in self.groups:
            kind = "location" if group["sections"][0]["name"] in self.locations else "entrance"
            for place in group["map_locations"]:
                key = (place["map"], kind, place["x"], place["y"])
                self.assertNotIn(key, seen, group["name"])
                seen.add(key)

    def test_every_picture_there_and_every_pin_on_it(self) -> None:
        sizes = {}
        for tab in self.maps:
            data = map_file(tab["img"])
            self.assertEqual(data[:8], b"\x89PNG\r\n\x1a\n", tab["img"])
            sizes[tab["name"]] = (int.from_bytes(data[16:20], "big"), int.from_bytes(data[20:24], "big"))
        self.assertTrue(map_file("images/you.png").startswith(b"\x89PNG"))
        for group in self.groups:
            for place in group["map_locations"]:
                width, height = sizes[place["map"]]
                self.assertTrue(0 <= place["x"] <= width and 0 <= place["y"] <= height, group["name"])

    def test_a_tab_for_every_room_and_the_layout_names_only_tabs(self) -> None:
        self.assertEqual(len(self.tabs), len(set(self.tabs)))
        self.assertTrue(set(MAPS) <= set(self.tabs))
        self.assertEqual(self.tabs[0], "World")

        def maps_in(node: Any) -> list[str]:
            if isinstance(node, dict):
                return [*node.get("maps", []), *(m for v in node.values() for m in maps_in(v))]
            return [m for v in node for m in maps_in(v)] if isinstance(node, list) else []
        named = maps_in(read("layout.json"))
        self.assertTrue(named and set(named) <= set(self.tabs))
        self.assertEqual(set(MAPS) - set(named), set())

    def test_every_map_has_its_area(self) -> None:
        # The area tabs group rooms by the game's own areas (data/map_areas.json).
        self.assertEqual(set(MAPS) - set(MAP_AREA_IDS), set())
        self.assertTrue(set(MAP_AREA_IDS.values()) <= set(AREA_NAMES))
        self.assertEqual(MAP_AREA_IDS["BugariaBridgeAttack"], 1)
        self.assertEqual(MAP_AREA_IDS["SnakemouthLake"], 2)

    def test_the_tab_follows_the_player(self) -> None:
        self.assertEqual(map_page_index(""), 0)
        self.assertEqual(map_page_index(None), 0)
        self.assertEqual(map_page_index("BugariaPier"), self.tabs.index("BugariaPier"))
        self.assertEqual(map_page_index("TitleScreen"), -1)
        self.assertEqual(map_page_index(7), -1)

    def test_the_marker(self) -> None:
        world, pier = self.tabs.index("World"), self.tabs.index("BugariaPier")
        self.assertIsNone(location_icon_coords(None, "BugariaPier"))
        self.assertIsNone(location_icon_coords(world, ""))
        self.assertIsNone(location_icon_coords(world, None))
        self.assertIsNone(location_icon_coords(pier, "SnakemouthLake"))
        for index in (world, pier):
            x, y, icon = location_icon_coords(index, "BugariaPier")
            self.assertEqual(icon, "images/you.png")
            self.assertTrue(x > 0 and y > 0)

    def test_the_keys_as_universal_tracker_fills_them(self) -> None:
        self.assertEqual(MAP_KEY.format(player=1, team=0), "bug_fables_map_0_1")
        self.assertEqual(TRACKER_WORLD["map_page_setting_key"], MAP_KEY)
        self.assertEqual(TRACKER_WORLD["location_setting_key"], MAP_KEY)
