from __future__ import annotations

import logging
from collections.abc import Mapping
from typing import Any

from BaseClasses import Item, ItemClassification, Location, LocationProgressType, Region, Tutorial
from rule_builder.rules import Has, HasAll
from worlds.AutoWorld import WebWorld, World

from .data_tables import (ARTIFACTS, ITEM_NAME_TO_ID, ITEMS, DIALOGUE_FLAGS, HELD_UNTIL, KEPT_OPEN, PRESENT_FROM, KEPT_PRESENT, SCENERY_HIDDEN, LOCATION_NAME_TO_ID, LOCATIONS, REGIONS, STORY_EVENTS,
                          WORLD_VERSION, vanilla_item)
from .options import BugFablesOptions, ShopContents

GAME = "Bug Fables"
# Location categories Shop Contents applies to: medal shops and item shops.
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
    Key items are shuffled; every item, your own included, arrives from the server.
    """

    game = GAME
    web = BugFablesWebWorld()
    item_name_to_id = ITEM_NAME_TO_ID
    location_name_to_id = LOCATION_NAME_TO_ID
    origin_region_name = "Menu"
    options_dataclass = BugFablesOptions
    options: BugFablesOptions

    # The goal: at least this many artifacts. Set in generate_early, capped at what this world includes.
    artifacts_required: int = 1

    _items_by_name = {item["name"]: item for item in ITEMS}
    # Only padding fills leftover locations, in any number; a filler that isn't padding (the Hard Mode medal) is
    # a real item, in the pool once.
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
        # The locations this seed has: a category whose option is off (quests) isn't part of it, and the game hands
        # those out as usual (the client only acts on what slot_data lists).
        self.included_locations = [loc for loc in LOCATIONS if self._category_on(loc.get("category"))]

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

        # Story steps other rules need (Leif joining), each an event in the region where it happens.
        for event in STORY_EVENTS:
            regions[event["region"]].add_event(
                event["name"], event["item"], location_type=BugFablesLocation, item_type=BugFablesItem
            )

        # One event per artifact, in the region where the game grants it. The game counts artifacts from flags
        # (MEASURED.md, SaveProgressIcons), so these hold no real item: they exist so fill can prove the goal.
        for artifact in ARTIFACTS:
            regions[artifact["region"]].add_event(
                artifact["name"], "Artifact", location_type=BugFablesLocation, item_type=BugFablesItem
            )

    def create_item(self, name: str) -> BugFablesItem:
        data = self._items_by_name[name]
        return BugFablesItem(name, _CLASSIFICATIONS[data["classification"]], ITEM_NAME_TO_ID[name], self.player)

    def create_items(self) -> None:
        # Exactly the included locations' own vanilla items (an item found at two spots is in the pool twice), then
        # padding for any location without one. An item whose vanilla spot isn't a location in this seed (quests
        # off, or not added yet) isn't in the pool: the game hands it out there as usual.
        pool: list[Item] = [self.create_item(name) for name in
                            (vanilla_item(loc) for loc in self.included_locations) if name is not None]
        unfilled = len(self.multiworld.get_unfilled_locations(self.player))
        pool += [self.create_filler() for _ in range(unfilled - len(pool))]
        self.multiworld.itempool += pool

    def set_rules(self) -> None:
        # What shop locations may hold (the user, 2026-09-25: shops are many easy checks in one place and can soak up the
        # important items, as in Tevi). Filler Only uses Archipelago's excluded type (no progression, no useful);
        # No Progression refuses progression items from any game.
        for loc in self.included_locations:
            if loc.get("category") not in SHOP_CATEGORIES:
                continue
            location = self.get_location(loc["name"])
            if self.options.shop_contents == ShopContents.option_filler_only:
                location.progress_type = LocationProgressType.EXCLUDED
            elif self.options.shop_contents == ShopContents.option_no_progression:
                location.item_rule = lambda item: not item.advancement
        # A location or story event needing more than its region says so in its own requires list.
        for loc in self.included_locations:
            if loc.get("requires"):
                self.set_rule(self.get_location(loc["name"]), HasAll(*loc["requires"]))
        for event in STORY_EVENTS:
            if event.get("requires"):
                self.set_rule(self.get_location(event["name"]), HasAll(*event["requires"]))
        self.set_completion_rule(Has("Artifact", count=self.artifacts_required))

    def pre_fill(self) -> None:
        # Filler Only when the room can't hold it (the user, 2026-09-25: fall back with a warning). An excluded spot takes
        # only an item that is neither progression nor useful, from any game (Fill.py, distribute_items_restrictive), so a
        # room with fewer of those than excluded spots fails to generate: a solo seed has 17 filler items for Merab's 22
        # copies. Then this world's shops take No Progression instead, which every seed can hold.
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
        # The world version lets the client refuse a mismatched build. The client sends the goal once the
        # game's own artifact count (its 7 artifact flags) reaches artifacts_required.
        # location_flags tells the client which game flag marks each location done ({location id: flag}), so
        # it only ever watches what this generator placed. JSON object keys are strings.
        return {
            "world_version": WORLD_VERSION,
            "artifacts_required": self.artifacts_required,
            "location_flags": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["flag"] for loc in self.included_locations
                               if "flag" in loc["source"]},
            # Crystal berry locations, done when their crystalbflags index is set ({location id: index}).
            "location_berries": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["berry"]
                                 for loc in self.included_locations if "berry" in loc["source"]},
            # Journal discovery locations, done when librarystuff[0, n] is set ({location id: n}).
            "location_discoveries": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["discovery"]
                                     for loc in self.included_locations if "discovery" in loc["source"]},
            # Shop stock locations, one per copy a shop ever stocks (a shop's copies are its locations in id order); the client
            # marks a copy done in the save when it is bought ({location id: {shop, medal}}).
            "location_shops": {str(LOCATION_NAME_TO_ID[loc["name"]]): {"shop": loc["source"]["shop"], "medal": loc["source"]["medal"]}
                               for loc in self.included_locations if "shop" in loc["source"]},
            # Item shop locations: the first purchase of an item in a shop ({location id: {map, keeper, item}}); the keeper is
            # the shopkeeper's entity name, the item its stock entry. After the check, the shop sells its own item again.
            "location_item_shops": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["item_shop"]
                                    for loc in self.included_locations if "item_shop" in loc["source"]},
            # Locations marked done by a number slot reaching a value instead of a flag (a boss prize handed over:
            # its prize slot reaching 3).
            "location_vars": {str(LOCATION_NAME_TO_ID[loc["name"]]): {"var": loc["source"]["var"],
                                                                       "at_least": loc["source"]["at_least"]}
                              for loc in self.included_locations if "var" in loc["source"]},
            # Which |giveitem| hands out each location's vanilla item, so the client can keep it out of the
            # inventory and show the seed's item instead. Locations without a known one are left out.
            "location_gives": {
                str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["give"]
                for loc in self.included_locations
                if "give" in loc["source"]
            },
            # Which locations are items lying in the world, known by their map and their own activationflag, so
            # the client can keep the vanilla item out when it's picked up.
            "location_pickups": {
                str(LOCATION_NAME_TO_ID[loc["name"]]): {"map": loc["source"]["pickup"]["map"], "flag": loc["source"].get("flag", -1),
                                                       **({"event": loc["source"]["event"]}
                                                          if loc["source"]["pickup"].get("story") else {}),
                                                       **({"berry": loc["source"]["berry"]}
                                                          if "berry" in loc["source"] else {}),
                                                       # A respawning pickup: no flag of its own, only a regional
                                                       # flag the game wipes on every area change. The client sends
                                                       # its check at the first pickup and leaves it vanilla after.
                                                       **({"regional": loc["source"]["regional"]}
                                                          if "regional" in loc["source"] else {})}
                for loc in self.included_locations
                if "pickup" in loc["source"]
            },
            # Blockers the story puts up for a while that the client keeps out of the way, so an area with locations
            # never closes (the logic assumes it stays reachable).
            "kept_open": [{"map": b["map"], "entity": b["entity"]} for b in KEPT_OPEN],
            # Ways the story only makes later (a door, a bounce mushroom) that the client makes exist from the start.
            "kept_present": [{"map": e["map"], "entity": e["entity"]} for e in KEPT_PRESENT],
            # Map scenery the story removes later (the Outskirts rocks) that the client removes from the start; entity
            # is the object's path inside the map.
            "scenery_hidden": [{"map": e["map"], "entity": e["entity"]} for e in SCENERY_HIDDEN],
            # Entities with no gate of their own that the client keeps away until a story flag (the town's first-entry
            # scene, reachable once the rocks are gone).
            "held_until": [{"map": e["map"], "entity": e["entity"], "flag": e["flag"]} for e in HELD_UNTIL],
            # Ways the story makes at a late flag that the client makes at an earlier one (the door back down to the fall
            # room from the trapdoor on, not the first boss).
            "present_from": [{"map": e["map"], "entity": e["entity"], "flag": e["flag"]} for e in PRESENT_FROM],
            # An entity's dialogue line repointed to another flag (the bar entrance's way down answers to a flag every new
            # game sets, instead of a story flag with other effects).
            "dialogue_flags": [{"map": e["map"], "entity": e["entity"], "flag": e["flag"], "to": e["to"]} for e in DIALOGUE_FLAGS],
            # Where each of this world's items goes (0 item, 1 key item, 2 medal), so the client gives it the right
            # way, shows a found one the way the game shows that kind, and knows a medal's id is offset.
            "item_kinds": {str(ITEM_NAME_TO_ID[item["name"]]): item["kind"] for item in ITEMS},
        }
