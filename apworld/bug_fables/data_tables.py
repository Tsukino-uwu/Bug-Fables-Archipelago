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
STORY_EVENTS: list[dict[str, Any]] = _LOCATION_DATA.get("story_events", [])
KEPT_OPEN: list[dict[str, Any]] = _LOCATION_DATA.get("kept_open", [])
KEPT_PRESENT: list[dict[str, Any]] = _LOCATION_DATA.get("kept_present", [])
SCENERY_HIDDEN: list[dict[str, Any]] = _LOCATION_DATA.get("scenery_hidden", [])
HELD_UNTIL: list[dict[str, Any]] = _LOCATION_DATA.get("held_until", [])

# Medal ids (MainManager.BadgeTypes) overlap item ids (MainManager.Items), so medals get their own range.
MEDAL_KIND = 2
MEDAL_ID_OFFSET = 1_000
# Berries (money) are handed out by the same giveitem, type -1; as items their game_id is the amount.
MONEY_KIND = 3
MONEY_ID_OFFSET = 2_000
# Crystal berries: a counted currency, one item (game_id 0); each berry spot is known by its crystalbflags index.
CRYSTAL_KIND = 4
CRYSTAL_ID_OFFSET = 3_000


def item_id(item: dict[str, Any]) -> int:
    offset = {MEDAL_KIND: MEDAL_ID_OFFSET, MONEY_KIND: MONEY_ID_OFFSET, CRYSTAL_KIND: CRYSTAL_ID_OFFSET}.get(item["kind"], 0)
    return ITEM_ID_BASE + offset + item["game_id"]


ITEM_NAME_TO_ID: dict[str, int] = {item["name"]: item_id(item) for item in ITEMS}
LOCATION_NAME_TO_ID: dict[str, int] = {loc["name"]: LOCATION_ID_BASE + loc["id"] for loc in LOCATIONS}

if len(set(ITEM_NAME_TO_ID.values())) != len(ITEMS):
    raise ValueError("bug_fables: two items share an id (same kind and game_id)")
if len(set(LOCATION_NAME_TO_ID.values())) != len(LOCATIONS):
    raise ValueError("bug_fables: two locations share an id")


def vanilla_item(location: dict[str, Any]) -> str | None:
    """The name of the item the game hands out at a location (its give or pickup; berries as "N Berries"), or None."""
    source = location["source"].get("give") or location["source"].get("pickup")
    if source is None:
        return None
    if source["type"] == 3:
        # A crystal berry spot: its item is always the one Crystal Berry item (its own index is only its identity).
        kind, game_id = CRYSTAL_KIND, 0
    else:
        kind, game_id = (MONEY_KIND if source["type"] == -1 else source["type"]), source["item"]
    for item in ITEMS:
        if item["kind"] == kind and item["game_id"] == game_id:
            return item["name"]
    return None

