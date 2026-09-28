from __future__ import annotations

import logging
from collections.abc import Mapping
from typing import Any

from worlds.AutoWorld import World

from . import items, locations, regions, rules, slot_data, web_world
from .data_tables import ARTIFACTS, DOORS, ENCOUNTERS, ITEM_NAME_TO_ID, LOCATION_NAME_TO_ID, LOCATIONS, ROOM_STARTS, STORY_EVENTS
from .doors import shuffle_coupled
from .enemies import shuffle_encounters
from .options import BugFablesOptions, EnemyShuffle, EntranceRandomizer, StartingLocation, StartingPartyMember


class BugFablesWorld(World):
    """
    Bug Fables: The Everlasting Sapling is a paper-style RPG about a team of three explorers.
    Items, medals, key items and berries are shuffled; every item, your own included, arrives from the server.
    """

    game = items.GAME
    web = web_world.BugFablesWebWorld()
    item_name_to_id = ITEM_NAME_TO_ID
    location_name_to_id = LOCATION_NAME_TO_ID
    origin_region_name = "Menu"
    options_dataclass = BugFablesOptions
    options: BugFablesOptions

    artifacts_required: int = 1
    # -1 is the story's party; 0-2 the one member a new file starts with; ALL_MEMBERS the whole party.
    starting_member: int = -1
    ALL_MEMBERS = 3

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
        choice = self.options.starting_party_member
        if choice == StartingPartyMember.option_random_member:
            self.starting_member = self.random.randrange(len(items.MEMBERS))
        elif choice == StartingPartyMember.option_all_three:
            self.starting_member = self.ALL_MEMBERS
        elif choice != StartingPartyMember.option_off:
            self.starting_member = choice.value - StartingPartyMember.option_vi
        self.included_locations = [loc for loc in LOCATIONS if locations.category_on(self, loc.category)]
        # A quest's step events follow its category: without the quest's items they couldn't be reached.
        self.included_events = [event for event in STORY_EVENTS if locations.category_on(self, event.category)]
        # Doors are decided here and sent in slot_data; the client never decides a door itself.
        self.door_targets = []
        if self.options.entrance_randomizer == EntranceRandomizer.option_coupled:
            self.door_targets = shuffle_coupled(DOORS.connections, DOORS.fixed, self.random)
        # Like doors, fights are decided here; the client only replays the list.
        self.enemy_swaps = {}
        if self.options.enemy_shuffle == EnemyShuffle.option_enemies_only:
            self.enemy_swaps = shuffle_encounters(ENCOUNTERS, self.random)
        # The start too: a room picked here, sent in slot_data; empty is the game's own start.
        self.start = {}
        if self.options.starting_location == StartingLocation.option_anywhere:
            self.start = self.random.choice(ROOM_STARTS).to_slot()

    def moves_shuffled(self) -> bool:
        return bool(self.options.shuffle_field_moves.value)

    def jump_shuffled(self) -> bool:
        return bool(self.options.shuffle_jump.value)

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def create_item(self, name: str) -> items.BugFablesItem:
        return items.create_item(self, name)

    def create_items(self) -> None:
        items.create_all_items(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def pre_fill(self) -> None:
        rules.fall_back_from_filler_only(self)

    def get_filler_item_name(self) -> str:
        return items.random_filler_name(self)

    def fill_slot_data(self) -> Mapping[str, Any]:
        return slot_data.build_slot_data(self)
