from __future__ import annotations

import logging
from collections.abc import Mapping
from typing import Any

from BaseClasses import Item, ItemClassification, Location, LocationProgressType, Region, Tutorial
from rule_builder.rules import Has, HasAll
from worlds.AutoWorld import WebWorld, World

from .data_tables import (ARTIFACTS, DOORS, ITEM_NAME_TO_ID, ITEMS, DIALOGUE_FLAGS, HELD_UNTIL, KEPT_OPEN, PRESENT_FROM, KEPT_PRESENT, SCENERY_HIDDEN, SCENERY_PRESENT, LOCATION_NAME_TO_ID, LOCATIONS, REGIONS, STORY_EVENTS,
                          WORLD_VERSION, vanilla_item)
from .doors import shuffle_coupled
from .options import BugFablesOptions, EntranceRandomizer, ShopContents

GAME = "Bug Fables"
SHOP_CATEGORIES = ("shop", "item_shop")
_CLASSIFICATIONS = {
    "progression": ItemClassification.progression,
    "useful": ItemClassification.useful,
    "filler": ItemClassification.filler,
    "trap": ItemClassification.trap,
}


class BugFablesItem(Item):
    game = GAME


class BugFablesLocation(Location):
    game = GAME


class BugFablesWebWorld(WebWorld):
    theme = "grassFlowers"
    tutorials = [
        Tutorial(
            "Multiworld Setup Guide",
            "Setting up the Bug Fables randomizer mod and connecting to a room.",
            "English",
            "setup_en.md",
            "setup/en",
            ["Tsukino"],
        )
    ]


class BugFablesWorld(World):
    """
    Bug Fables: The Everlasting Sapling is a paper-style RPG about a team of three explorers.
    Items, medals, key items and berries are shuffled; every item, your own included, arrives from the server.
    """

    game = GAME
    web = BugFablesWebWorld()
    item_name_to_id = ITEM_NAME_TO_ID
    location_name_to_id = LOCATION_NAME_TO_ID
    origin_region_name = "Menu"
    options_dataclass = BugFablesOptions
    options: BugFablesOptions

    artifacts_required: int = 1

    _items_by_name = {item["name"]: item for item in ITEMS}
    # Only padding fills leftover slots; other filler (the Hard Mode medal) enters only as a location's vanilla item.
    _padding = [item["name"] for item in ITEMS if item.get("padding")]

    def generate_early(self) -> None:
        wanted = self.options.artifacts_required.value
        available = len(ARTIFACTS)
        if wanted > available:
            logging.warning(
                "Bug Fables: player %s (%s) asked for %d artifacts, but this version of the world includes %d; "
                "the goal is lowered to %d.",
                self.player, self.player_name, wanted, available, available,
            )
        self.artifacts_required = min(wanted, available)
        self.included_locations = [loc for loc in LOCATIONS if self._category_on(loc.get("category"))]
        # Doors are decided here and sent in slot_data; the client never decides a door itself.
        self.door_targets = []
        if self.options.entrance_randomizer == EntranceRandomizer.option_coupled:
            self.door_targets = shuffle_coupled(DOORS["connections"], DOORS["fixed"], self.random)

    def _category_on(self, category: str | None) -> bool:
        if category == "quest":
            return bool(self.options.shuffle_quests.value)
        if category == "crystal_berry":
            return bool(self.options.shuffle_crystal_berries.value)
        if category == "discovery":
            return bool(self.options.shuffle_discoveries.value)
        if category == "shop":
            return bool(self.options.shuffle_medal_shops.value)
        if category == "item_shop":
            return bool(self.options.shuffle_item_shops.value)
        return True

    def create_regions(self) -> None:
        regions = {data["name"]: Region(data["name"], self.player, self.multiworld) for data in REGIONS}
        self.multiworld.regions += regions.values()

        for data in REGIONS:
            for exit_data in data["exits"]:
                requires = exit_data.get("requires", [])
                rule = HasAll(*requires) if requires else None
                self.create_entrance(regions[data["name"]], regions[exit_data["to"]], rule)

        for loc in self.included_locations:
            region = regions[loc["region"]]
            region.locations.append(
                BugFablesLocation(self.player, loc["name"], LOCATION_NAME_TO_ID[loc["name"]], region)
            )

        for event in STORY_EVENTS:
            regions[event["region"]].add_event(
                event["name"], event["item"], location_type=BugFablesLocation, item_type=BugFablesItem
            )

        # The game counts artifacts from flags, so these events hold no real item: they let fill prove the goal.
        for artifact in ARTIFACTS:
            regions[artifact["region"]].add_event(
                artifact["name"], "Artifact", location_type=BugFablesLocation, item_type=BugFablesItem
            )

    def create_item(self, name: str) -> BugFablesItem:
        data = self._items_by_name[name]
        return BugFablesItem(name, _CLASSIFICATIONS[data["classification"]], ITEM_NAME_TO_ID[name], self.player)

    def create_items(self) -> None:
        # The included locations' vanilla items (duplicates kept), then padding; an item whose spot is off stays vanilla.
        pool: list[Item] = [self.create_item(name) for name in
                            (vanilla_item(loc) for loc in self.included_locations) if name is not None]
        unfilled = len(self.multiworld.get_unfilled_locations(self.player))
        pool += [self.create_filler() for _ in range(unfilled - len(pool))]
        self.multiworld.itempool += pool

    def set_rules(self) -> None:
        for loc in self.included_locations:
            if loc.get("category") not in SHOP_CATEGORIES:
                continue
            location = self.get_location(loc["name"])
            if self.options.shop_contents == ShopContents.option_filler_only:
                location.progress_type = LocationProgressType.EXCLUDED
            elif self.options.shop_contents == ShopContents.option_no_progression:
                location.item_rule = lambda item: not item.advancement
        for loc in self.included_locations:
            if loc.get("requires"):
                self.set_rule(self.get_location(loc["name"]), HasAll(*loc["requires"]))
        for event in STORY_EVENTS:
            if event.get("requires"):
                self.set_rule(self.get_location(event["name"]), HasAll(*event["requires"]))
        self.set_completion_rule(Has("Artifact", count=self.artifacts_required))

    def pre_fill(self) -> None:
        # A room with fewer excludable items than excluded spots fails to generate, so Filler Only falls back.
        if self.options.shop_contents != ShopContents.option_filler_only:
            return
        shops = [self.get_location(loc["name"]) for loc in self.included_locations if loc.get("category") in SHOP_CATEGORIES]
        excludable = sum(1 for item in self.multiworld.itempool if item.excludable)
        excluded = sum(1 for location in self.multiworld.get_unfilled_locations()
                       if location.progress_type == LocationProgressType.EXCLUDED)
        if excludable >= excluded:
            return
        logging.warning(
            "Bug Fables: player %s (%s) asked for Shop Contents: Filler Only, but the room has %d filler items for %d "
            "excluded locations; this seed's shops use No Progression instead.",
            self.player, self.player_name, excludable, excluded,
        )
        for location in shops:
            location.progress_type = LocationProgressType.DEFAULT
            location.item_rule = lambda item: not item.advancement

    def get_filler_item_name(self) -> str:
        return self.random.choice(self._padding)

    def fill_slot_data(self) -> Mapping[str, Any]:
        # The client acts only on what is listed here. JSON object keys are strings.
        return {
            "world_version": WORLD_VERSION,
            "artifacts_required": self.artifacts_required,
            "location_flags": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["flag"] for loc in self.included_locations
                               if "flag" in loc["source"]},
            "location_berries": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["berry"]
                                 for loc in self.included_locations if "berry" in loc["source"]},
            "location_discoveries": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["discovery"]
                                     for loc in self.included_locations if "discovery" in loc["source"]},
            # One location per copy a shop ever stocks; a shop's copies are its locations in id order.
            "location_shops": {str(LOCATION_NAME_TO_ID[loc["name"]]): {"shop": loc["source"]["shop"], "medal": loc["source"]["medal"]}
                               for loc in self.included_locations if "shop" in loc["source"]},
            "location_item_shops": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["item_shop"]
                                    for loc in self.included_locations if "item_shop" in loc["source"]},
            # Done when a number slot reaches a value, not a flag (a boss prize handed over).
            "location_vars": {str(LOCATION_NAME_TO_ID[loc["name"]]): {"var": loc["source"]["var"],
                                                                       "at_least": loc["source"]["at_least"]}
                              for loc in self.included_locations if "var" in loc["source"]},
            "location_gives": {
                str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["give"]
                for loc in self.included_locations
                if "give" in loc["source"]
            },
            "location_pickups": {
                str(LOCATION_NAME_TO_ID[loc["name"]]): {"map": loc["source"]["pickup"]["map"], "flag": loc["source"].get("flag", -1),
                                                       **({"event": loc["source"]["event"]}
                                                          if loc["source"]["pickup"].get("story") else {}),
                                                       **({"berry": loc["source"]["berry"]}
                                                          if "berry" in loc["source"] else {}),
                                                       # A respawning pickup: its regional flag is wiped on area change.
                                                       **({"regional": loc["source"]["regional"]}
                                                          if "regional" in loc["source"] else {})}
                for loc in self.included_locations
                if "pickup" in loc["source"]
            },
            # Story blockers the client keeps away, so an area the logic counts as reachable never closes.
            "kept_open": [{"map": b["map"], "entity": b["entity"]} for b in KEPT_OPEN],
            "kept_present": [{"map": e["map"], "entity": e["entity"]} for e in KEPT_PRESENT],
            "scenery_hidden": [{"map": e["map"], "entity": e["entity"]} for e in SCENERY_HIDDEN],
            "scenery_present": [{"map": e["map"], "entity": e["entity"]} for e in SCENERY_PRESENT],
            "held_until": [{"map": e["map"], "entity": e["entity"], "flag": e["flag"]} for e in HELD_UNTIL],
            "present_from": [{"map": e["map"], "entity": e["entity"], "flag": e["flag"]} for e in PRESENT_FROM],
            "dialogue_flags": [{"map": e["map"], "entity": e["entity"], "flag": e["flag"], "to": e["to"]} for e in DIALOGUE_FLAGS],
            "door_targets": self.door_targets,
            # 0 item, 1 key item, 2 medal, 3 berries, 4 crystal berry.
            "item_kinds": {str(ITEM_NAME_TO_ID[item["name"]]): item["kind"] for item in ITEMS},
        }
