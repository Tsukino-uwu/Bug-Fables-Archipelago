"""Item and location tables, read from data/*.json.

pkgutil.get_data rather than open(): inside a packaged .apworld (a zip) there is no file path to open.
"""
from __future__ import annotations

import json
import pkgutil
from typing import Any

ITEM_ID_BASE = 7_710_000
LOCATION_ID_BASE = 7_720_000


def _load(name: str) -> dict[str, Any]:
    raw = pkgutil.get_data(__name__, f"data/{name}")
    if raw is None:
        raise FileNotFoundError(f"bug_fables: data/{name} is missing from the world package")
    return json.loads(raw.decode("utf-8"))


ITEMS: list[dict[str, Any]] = _load("items.json")["items"]
_LOCATION_DATA = _load("locations.json")
LOCATIONS: list[dict[str, Any]] = _LOCATION_DATA["locations"]
REGIONS: list[dict[str, Any]] = _LOCATION_DATA["regions"]
ARTIFACTS: list[dict[str, Any]] = _LOCATION_DATA["artifacts"]

ITEM_NAME_TO_ID: dict[str, int] = {item["name"]: ITEM_ID_BASE + item["game_id"] for item in ITEMS}
LOCATION_NAME_TO_ID: dict[str, int] = {loc["name"]: LOCATION_ID_BASE + loc["id"] for loc in LOCATIONS}

if len(set(ITEM_NAME_TO_ID.values())) != len(ITEMS):
    raise ValueError("bug_fables: two items share a game_id")
if len(set(LOCATION_NAME_TO_ID.values())) != len(LOCATIONS):
    raise ValueError("bug_fables: two locations share an id")
