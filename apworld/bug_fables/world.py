from __future__ import annotations

import logging
from collections.abc import Mapping
from random import Random
from typing import Any

from worlds.AutoWorld import World

from . import (entrances, items, locations, music, regions, rules, shop_inventories, slot_data, universal_tracker,
               web_world)
from .data_tables import (ARTIFACTS, DOORS, ENCOUNTERS, ITEM_NAME_TO_ID, LOCATION_NAME_TO_ID, LOCATIONS, ONE_WAYS,
                          ROOM_STARTS, STORY_EVENTS, TRACKER_ORDER)
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
    # The boat's items by what they are, so "!hint Submarine" finds it with Progressive Boat on or off (a group hints
    # every copy of every item in it).
    item_name_groups = {
        "Submarine": {"Progressive Boat", "Subaquatic Maritime Neotransport"},
        "Boat": {"Progressive Boat", "Boat Ticket"},
    }
    origin_region_name = regions.MENU
    options_dataclass = BugFablesOptions
    options: BugFablesOptions

    artifacts_required: int = 1
    # -1 is the story's party; 0-2 the one member a new file starts with; ALL_MEMBERS the whole party.
    starting_member: int = -1
    ALL_MEMBERS = 3
    # Filler Starting Checks as this seed applies it (off with the doors that can shrink the start).
    filler_starting_checks: bool = True
    # Shop Contents' Filler Only fell back to No Progression (pre_fill, rules.fall_back_from_filler_only).
    shops_fell_back: bool = False
    # Universal Tracker regenerates a seed from its slot_data with no yaml; passthrough is that slot_data while it does.
    ut_can_gen_without_yaml = True
    passthrough: Mapping[str, Any] | None = None
    # Universal Tracker's deferred entrances: each shuffled door's region until the player goes through it, and with
    # coupled doors the door each leads back through (universal_tracker.defer_doors).
    deferred_doors: dict[str, Any]
    way_back: dict[str, str]

    @staticmethod
    def interpret_slot_data(slot_data: dict[str, Any]) -> dict[str, Any]:
        # Returned whole: Universal Tracker then regenerates the world with it as re_gen_passthrough.
        return slot_data

    def custom_ut_sort(self, region_label: str, location_label: str) -> int:
        # Universal Tracker's list in the story's order (TRACKER_ORDER); anything else, such as an entrance, last.
        return TRACKER_ORDER.get(location_label, len(TRACKER_ORDER))

    def reconnect_found_entrances(self, key: str, value: Any) -> None:
        # Universal Tracker, at connect and on each change of the key it was given (found_entrances_datastorage_key).
        universal_tracker.reconnect_doors(self, value)

    def generate_early(self) -> None:
        self.passthrough = universal_tracker.passthrough(self)
        if self.passthrough is not None:
            universal_tracker.apply_options(self, self.passthrough)
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
        # The seed's own rolls, with Universal Tracker: the party here, before the locations and rules depend on it.
        if self.passthrough is not None:
            self.starting_member = self.passthrough["starting_member"]
        elif choice == StartingPartyMember.option_random_member:
            self.starting_member = self.random.randrange(len(items.MEMBERS))
        elif choice == StartingPartyMember.option_all_three:
            self.starting_member = self.ALL_MEMBERS
        elif choice != StartingPartyMember.option_off:
            self.starting_member = choice.value - StartingPartyMember.option_vi
        self.included_locations = [loc for loc in LOCATIONS
                                   if not loc.pending and locations.category_on(self, loc.category)]
        # A quest's step events follow its category: without the quest's items they couldn't be reached.
        self.included_events = [event for event in STORY_EVENTS if locations.category_on(self, event.category)]
        # Fights are decided here; the client only replays the list.
        self.enemy_swaps = {}
        if self.passthrough is not None:
            self.enemy_swaps = dict(self.passthrough["enemy_swaps"])
        elif self.options.enemy_shuffle == EnemyShuffle.option_enemies_only:
            self.enemy_swaps = shuffle_encounters(ENCOUNTERS, self.random)
        # The start too: a room picked here, sent in slot_data; empty is the game's own start.
        self.start = {}
        if self.passthrough is not None:
            self.start = dict(self.passthrough["start"])
        elif self.options.starting_location == StartingLocation.option_anywhere:
            self.start = self.random.choice(ROOM_STARTS).to_slot()
        # Coupled and Room Swap can leave the start with only the opening's spots, and a seed whose opening takes only
        # filler then fails to generate: the option stands down for it, as Filler Only shops do.
        self.filler_starting_checks = bool(self.options.filler_starting_checks)
        if self.filler_starting_checks and self.options.entrance_randomizer in (
                EntranceRandomizer.option_coupled, EntranceRandomizer.option_room_swap):
            self.filler_starting_checks = False
            logging.warning(
                "Bug Fables: player %s (%s) asked for Filler Starting Checks with the Entrance Randomizer on %s, whose "
                "doors can leave the start too small for it; this seed's opening checks may hold any item.",
                self.player, self.player_name, self.options.entrance_randomizer.current_option_name,
            )

    def moves_shuffled(self) -> bool:
        return bool(self.options.shuffle_field_moves.value)

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def connect_entrances(self) -> None:
        if self.passthrough is not None:
            # Universal Tracker: the seed's own doors, from the door_targets the mod rewrites doors from.
            self.door_targets = list(self.passthrough["door_targets"])
            self.door_pairings = entrances.replay(self, self.door_targets)
            universal_tracker.defer_doors(self)
            return
        # Doors are decided here, on the region graph, and sent in slot_data; the client never decides a door itself.
        self.door_pairings = entrances.shuffle(self)
        # With the doors off nothing is rewritten, a one-way's story copies included.
        shuffled = self.options.entrance_randomizer != EntranceRandomizer.option_off
        self.door_targets = entrances.door_targets(self.door_pairings, DOORS.connections,
                                                   ONE_WAYS if shuffled else ())

    def generate_basic(self) -> None:
        if self.passthrough is not None:
            # Universal Tracker: the seed's own, so the rebuilt world's slot_data is the seed's, key for key.
            self.music_map = dict(self.passthrough["music_map"])
            self.jingle_map = dict(self.passthrough["jingle_map"])
            self.shop_inventories = list(self.passthrough["shop_inventories"])
            return
        # Archipelago's step for rolls that don't affect logic; the logic's own (doors, fights) come before the rules.
        # Each draws from its own stream, taken whether it's on or not, so no option here changes another's roll.
        music_random = Random(self.random.getrandbits(64))
        shop_random = Random(self.random.getrandbits(64))
        self.music_map = {}
        self.jingle_map = {}
        if self.options.music_shuffle:
            self.music_map = music.shuffle(music.POOL, music_random)
            self.jingle_map = music.shuffle(music.JINGLES, music_random)
        self.shop_inventories = []
        if self.options.shuffle_shop_inventories:
            self.shop_inventories = shop_inventories.shuffle(shop_inventories.SPOTS, shop_random)

    def write_spoiler_header(self, spoiler_handle: Any) -> None:
        # Nothing written here: the shuffled doors go to the spoiler's own Entrances section.
        entrances.write_spoiler(self)

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
