"""slot_data: everything the client acts on. Its keys are the contract with the mod (SeedData.cs)."""
from __future__ import annotations

from collections.abc import Callable, Iterable, Mapping
from typing import TYPE_CHECKING, Any

from .data_tables import (DAY_NIGHT, DIALOGUE_FLAGS, ENTITIES_MOVED, FREE_SALES, HELD_UNTIL, HELD_UNTIL_ITEM,
                          ITEM_NAME_TO_ID, ITEMS, KEPT_OPEN, KEPT_PRESENT, LOCATION_NAME_TO_ID, PRESENT_FROM,
                          PRESENT_WITH_ITEM, ROADBLOCKS, SCENE_CAMERAS, SCENERY_HIDDEN, SCENERY_MOVED, SCENERY_PRESENT,
                          STORY_ONLY_MAPS, TIME_SWITCHES, WORLD_VERSION)
from .data_types import (DayNight, DialogueFlag, EntityMove, EntityRef, FlagEntity, FreeSale, ItemEntity, SceneCamera,
                         SceneryMove, Source, TimeSwitch)
from .options import ShopContents

if TYPE_CHECKING:
    from .world import BugFablesWorld

# The options slot_data's "options" carries: what the mod acts on, and what Universal Tracker's regeneration needs to
# build the same locations, doors, rules, exclusions and goal.
SLOT_OPTIONS: tuple[str, ...] = (
    "artifacts_required", "shuffle_quests", "shuffle_crystal_berries", "shuffle_discoveries", "shuffle_hidden_items",
    "shuffle_dig_spots", "enemy_sanity",
    "shuffle_medal_shops", "shuffle_item_shops", "shuffle_termacade", "shop_contents", "entrance_randomizer",
    "filler_starting_checks",
    "shuffle_field_moves", "shuffle_jump", "points_of_no_return", "progressive_boat", "extra_roadblocks",
    "exclude_locations")
# Every other option, and why it isn't sent.
NOT_SENT: dict[str, str] = {
    "enemy_shuffle": "its result is enemy_swaps",
    "starting_location": "its result is start",
    "starting_party_member": "its result is starting_member",
    "music_shuffle": "its result is music_map and jingle_map",
    "shuffle_shop_inventories": "its result is shop_inventories",
    "plando_connections": "its doors are in door_targets",
    "progression_balancing": "fill only",
    "accessibility": "fill only",
    "local_items": "fill only",
    "non_local_items": "fill only",
    "priority_locations": "fill only",
    "item_links": "fill only",
    "plando_items": "fill only",
    "start_inventory": "the server sends its items",
    "start_hints": "the server's",
    "start_location_hints": "the server's",
}


def options_for_slot(world: BugFablesWorld) -> dict[str, Any]:
    """SLOT_OPTIONS as this seed applied them: the goal as capped, Filler Starting Checks as it stood, and Shop Contents
    after its fallback. Toggles as JSON booleans."""
    options = world.options.as_dict(*SLOT_OPTIONS, toggles_as_bools=True)
    options["artifacts_required"] = world.artifacts_required
    options["filler_starting_checks"] = world.filler_starting_checks
    if world.shops_fell_back:
        options["shop_contents"] = ShopContents.option_no_progression
    return options


def _by_location(world: BugFablesWorld, key: str, value: Callable[[Source], Any] | None = None) -> dict[str, Any]:
    """{location id: what its source says}, for each included location whose source has key. JSON keys are strings."""
    return {str(LOCATION_NAME_TO_ID[loc.name]): (value(loc.source) if value else getattr(loc.source, key))
            for loc in world.included_locations if getattr(loc.source, key) is not None}


def _pickup(source: Source) -> dict[str, Any]:
    pickup = {"map": source.pickup.map, "flag": -1 if source.flag is None else source.flag}
    if source.pickup.story:
        pickup["event"] = source.event
    if source.berry is not None:
        pickup["berry"] = source.berry
    # A respawning pickup: its regional flag is wiped on area change.
    if source.regional is not None:
        pickup["regional"] = source.regional
    return pickup


def _entities(entries: Iterable[EntityRef | FlagEntity | ItemEntity | DialogueFlag | FreeSale | DayNight | TimeSwitch
                                  | SceneryMove | EntityMove | SceneCamera]) -> list[dict[str, Any]]:
    return [entry.to_slot() for entry in entries]


def build_slot_data(world: BugFablesWorld) -> Mapping[str, Any]:
    # Extra Roadblocks: a chosen one's pieces stand from the start, the others' are kept away all game.
    chosen = world.options.extra_roadblocks.value
    up = [block for block in ROADBLOCKS if block.name in chosen]
    down = [block for block in ROADBLOCKS if block.name not in chosen]
    return {
        "world_version": WORLD_VERSION,
        # The options as this seed applied them (SLOT_OPTIONS): the goal, field moves, Jump, Points of No Return and the
        # rest Universal Tracker regenerates from.
        "options": options_for_slot(world),
        "location_flags": _by_location(world, "flag"),
        "location_berries": _by_location(world, "berry"),
        "location_discoveries": _by_location(world, "discovery"),
        # Enemysanity: {location id: "map:entity"}, the map enemy whose won fight drops the check; empty when it's off.
        # The client keeps each one present whatever the story's flags.
        "location_enemies": _by_location(world, "enemy"),
        # One location per copy a shop ever stocks; a shop's copies are its locations in id order.
        "location_shops": _by_location(world, "shop", lambda source: {"shop": source.shop, "medal": source.medal}),
        "location_item_shops": _by_location(world, "item_shop", lambda source: source.item_shop.to_slot()),
        # The Termacade's prize stand: {location id: its row in the stand}; empty with Shuffle Termacade off.
        "location_prizes": _by_location(world, "prize"),
        # Done when a number slot reaches a value, not a flag (a boss prize handed over).
        "location_vars": _by_location(world, "var", lambda source: {"var": source.var, "at_least": source.at_least}),
        # Story-only maps: the pause menu offers no Warp or map travel there, as the game gives no way out mid-scene.
        "no_travel_maps": sorted(STORY_ONLY_MAPS),
        # Checks that show no item of their own (only a story flag): the client shows the player's own item there.
        "silent_locations": sorted(LOCATION_NAME_TO_ID[loc.name] for loc in world.included_locations
                                   if loc.source.present() <= {"event", "flag", "added"}),
        # The opening's checks: their items arrive with no hold-up, so a new file doesn't start with a string of boxes.
        "quiet_locations": sorted(LOCATION_NAME_TO_ID[loc.name] for loc in world.included_locations if loc.quiet),
        # Items the story puts straight into the bag at a location: the client leaves them out.
        "location_added": _by_location(world, "added", lambda source: source.added.to_slot()),
        # A give with "npc" is that character's only: another giveitem of the same item on the map stays the game's.
        "location_gives": _by_location(world, "give", lambda source: {**source.give.to_slot(),
                                                                      **({"npc": source.npc} if source.npc else {})}),
        "location_pickups": _by_location(world, "pickup", _pickup),
        # Story blockers the client keeps away, so an area the logic counts as reachable never closes.
        "kept_open": _entities((*KEPT_OPEN, *(npc for block in down for npc in block.npcs))),
        "kept_present": _entities((*KEPT_PRESENT, *(npc for block in up for npc in block.npcs))),
        "scenery_hidden": _entities((*SCENERY_HIDDEN, *(piece for block in down for piece in block.scenery))),
        "scenery_present": _entities((*SCENERY_PRESENT, *(piece for block in up for piece in block.scenery))),
        "held_until": _entities(HELD_UNTIL),
        "present_from": _entities(PRESENT_FROM),
        # Entities tied to one of the mod's key items in the bag: made with it whatever their own requirement, or kept
        # away until it, on top of their own (the submarine's docks, and who shows them off).
        "present_with_item": _entities(PRESENT_WITH_ITEM),
        "held_until_item": _entities(HELD_UNTIL_ITEM),
        "dialogue_flags": _entities(DIALOGUE_FLAGS),
        # Sellers' lines the client makes free ([{"map", "lines"}]): their price commands and written price to 0.
        "free_sales": _entities(FREE_SALES),
        # Day maps whose night the client switches at will ([{"day", "night", "from", "until", "first_event"}]), each
        # map's switch NPC ([{"map", "entity", "at", "day", "night"}], each of the last two its line, then its staying
        # and switching choices), and scenery set where a scene would leave it.
        "day_night": _entities(DAY_NIGHT),
        "time_switches": _entities(TIME_SWITCHES),
        "scenery_moved": _entities(SCENERY_MOVED),
        # Entities standing somewhere else than their own spot ([{"map", "entity", "at"}]).
        "entities_moved": _entities(ENTITIES_MOVED),
        # A scene's fixed camera point moved with its characters ([{"map", "event", "from", "to"}]).
        "scene_cameras": _entities(SCENE_CAMERAS),
        "door_targets": world.door_targets,
        # {"map:entity": [enemy ids]}: the fight a map enemy starts instead of its own.
        "enemy_swaps": world.enemy_swaps,
        # {"map", "from"}: the room a new file begins in, as if entering through the door from "from"; empty for the
        # game's own start.
        "start": world.start,
        # The one member a new file starts with (0 Vi, 1 Kabbu, 2 Leif); 3 all three; -1 is the story's party.
        "starting_member": world.starting_member,
        # Every learned ability is an item: the mod answers the game's ability checks from the items received.
        "ability_items": True,
        # The submarine is an item (its key item, whichever item gives it): the mod answers the docks' story checks from
        # the bag.
        "submarine_item": True,
        # The ant tunnels' miners dig for free: each shortcut opens once its far end is reached (the user, 2026-10-04).
        "free_ant_tunnels": True,
        # The Termite gate opens from inside before it was ever opened from outside: the client marks it opened (flag
        # 384) as its scene starts there (the user, 2026-10-04).
        "termite_gate_from_inside": True,
        # Music Shuffle, {name: name played in its place}: tracks by the game's Musics names, jingles by their sound
        # names; both empty when it's off.
        "music_map": world.music_map,
        "jingle_map": world.jingle_map,
        # Shuffle Shop Inventories, [{"map", "keeper" or "regional", "item", "to"}]: what an item shop slot restocks and
        # a respawning pickup comes back with once neither is a check; empty when it's off.
        "shop_inventories": world.shop_inventories,
        # An item's kind, as data_tables names them (ITEM_KIND and the rest).
        "item_kinds": {str(ITEM_NAME_TO_ID[item.name]): item.kind for item in ITEMS},
    }
