"""Item and location tables, read from data/*.json.

pkgutil.get_data rather than open(): inside a packaged .apworld (a zip) there is no file path to open.
"""
from __future__ import annotations

import json
import pkgutil
from typing import Any

ITEM_ID_BASE = 7_710_000
LOCATION_ID_BASE = 7_720_000


def _load_manifest() -> dict[str, Any]:
    raw = pkgutil.get_data(__name__, "archipelago.json")
    if raw is None:
        raise FileNotFoundError("bug_fables: archipelago.json is missing from the world package")
    return json.loads(raw.decode("utf-8"))


def _load(name: str) -> dict[str, Any]:
    raw = pkgutil.get_data(__name__, f"data/{name}")
    if raw is None:
        raise FileNotFoundError(f"bug_fables: data/{name} is missing from the world package")
    return json.loads(raw.decode("utf-8"))


# The one place the world's version is written: slot_data reports it to the client, and the manifest carries it.
WORLD_VERSION: str = _load_manifest()["world_version"]
ITEMS: list[dict[str, Any]] = _load("items.json")["items"]
_LOCATION_DATA = _load("locations.json")
LOCATIONS: list[dict[str, Any]] = _LOCATION_DATA["locations"]
REGIONS: list[dict[str, Any]] = _LOCATION_DATA["regions"]
ARTIFACTS: list[dict[str, Any]] = _LOCATION_DATA["artifacts"]

# Medal ids (MainManager.BadgeTypes) overlap item ids (MainManager.Items), so medals get their own range.
MEDAL_KIND = 2
MEDAL_ID_OFFSET = 1_000


def item_id(item: dict[str, Any]) -> int:
    return ITEM_ID_BASE + (MEDAL_ID_OFFSET if item["kind"] == MEDAL_KIND else 0) + item["game_id"]


ITEM_NAME_TO_ID: dict[str, int] = {item["name"]: item_id(item) for item in ITEMS}
LOCATION_NAME_TO_ID: dict[str, int] = {loc["name"]: LOCATION_ID_BASE + loc["id"] for loc in LOCATIONS}

if len(set(ITEM_NAME_TO_ID.values())) != len(ITEMS):
    raise ValueError("bug_fables: two items share an id (same kind and game_id)")
if len(set(LOCATION_NAME_TO_ID.values())) != len(LOCATIONS):
    raise ValueError("bug_fables: two locations share an id")


def vanilla_item(location: dict[str, Any]) -> str | None:
    """The name of the item the game hands out at a location (its give or pickup), or None (money, or unknown)."""
    source = location["source"].get("give") or location["source"].get("pickup")
    if source is None:
        return None
    for item in ITEMS:
        if item["kind"] == source["type"] and item["game_id"] == source["item"]:
            return item["name"]
    return None

