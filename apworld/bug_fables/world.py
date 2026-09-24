from __future__ import annotations

import logging
from collections.abc import Mapping
from typing import Any

from BaseClasses import Item, ItemClassification, Location, Region, Tutorial
from rule_builder.rules import Has, HasAll
from worlds.AutoWorld import WebWorld, World

from .data_tables import ARTIFACTS, ITEM_NAME_TO_ID, ITEMS, LOCATION_NAME_TO_ID, LOCATIONS, REGIONS, WORLD_VERSION
from .options import BugFablesOptions

GAME = "Bug Fables"
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
    _filler = [item["name"] for item in ITEMS if item["classification"] == "filler"]

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

    def create_regions(self) -> None:
        regions = {data["name"]: Region(data["name"], self.player, self.multiworld) for data in REGIONS}
        self.multiworld.regions += regions.values()

        for data in REGIONS:
            for exit_data in data["exits"]:
                requires = exit_data.get("requires", [])
                rule = HasAll(*requires) if requires else None
                self.create_entrance(regions[data["name"]], regions[exit_data["to"]], rule)

        for loc in LOCATIONS:
            region = regions[loc["region"]]
            region.locations.append(
                BugFablesLocation(self.player, loc["name"], LOCATION_NAME_TO_ID[loc["name"]], region)
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
        pool: list[Item] = [
            self.create_item(item["name"]) for item in ITEMS if item["classification"] == "progression"
        ]
        unfilled = len(self.multiworld.get_unfilled_locations(self.player))
        pool += [self.create_filler() for _ in range(unfilled - len(pool))]
        self.multiworld.itempool += pool

    def set_rules(self) -> None:
        self.set_completion_rule(Has("Artifact", count=self.artifacts_required))

    def get_filler_item_name(self) -> str:
        return self.random.choice(self._filler)

    def fill_slot_data(self) -> Mapping[str, Any]:
        # The world version lets the client refuse a mismatched build. The client sends the goal once the
        # game's own artifact count (its 7 artifact flags) reaches artifacts_required.
        # location_flags tells the client which game flag marks each location done ({location id: flag}), so
        # it only ever watches what this generator placed. JSON object keys are strings.
        return {
            "world_version": WORLD_VERSION,
            "artifacts_required": self.artifacts_required,
            "location_flags": {str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["flag"] for loc in LOCATIONS},
            # Which |giveitem| hands out each location's vanilla item, so the client can keep it out of the
            # inventory and show the seed's item instead. Locations without a known one are left out.
            "location_gives": {
                str(LOCATION_NAME_TO_ID[loc["name"]]): loc["source"]["give"]
                for loc in LOCATIONS
                if "give" in loc["source"]
            },
            # The inventory list each of this world's items belongs to (0 item, 1 key item), so the client can
            # show a found Bug Fables item the way the game shows that kind.
            "item_kinds": {str(ITEM_NAME_TO_ID[item["name"]]): item["kind"] for item in ITEMS},
        }
