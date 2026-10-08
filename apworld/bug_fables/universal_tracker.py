"""Universal Tracker: a seed rebuilt from its slot_data, with no yaml. Universal Tracker regenerates this world on its
own with re_gen_passthrough["Bug Fables"] holding the seed's slot_data, and every roll then comes from that instead of
the random (its docs/apworld-integration.md, "Generating without a YAML")."""
from __future__ import annotations

from collections.abc import Mapping
from typing import TYPE_CHECKING, Any

from .data_tables import ONE_WAYS, door_name
from .options import BugFablesOptions, EntranceRandomizer

if TYPE_CHECKING:
    from .world import BugFablesWorld

# The doors the player has gone through: the mod adds each door's entrance name to this data storage key, and the
# PopTracker pack reads it too. Universal Tracker fills in {team} and {player} (its TrackerClient's key.format).
DOORS_TAKEN_KEY = "bug_fables_doors_{team}_{player}"
# A one-way door's story copies are other doors in the game but one entrance here.
_COPY_OF = {door_name(w.map, copy): door_name(w.map, w.door) for w in ONE_WAYS for copy in w.copies}
_ONE_WAYS = {(w.map, w.door) for w in ONE_WAYS}


def passthrough(world: BugFablesWorld) -> Mapping[str, Any] | None:
    """The seed's slot_data while Universal Tracker regenerates it; None in a real generation."""
    return getattr(world.multiworld, "re_gen_passthrough", {}).get(world.game)


def apply_options(world: BugFablesWorld, slot_data: Mapping[str, Any]) -> None:
    """The options as the seed applied them (slot_data's options); every other option at its default, as in Universal
    Tracker's empty yaml. A seed from another version of the world is refused: no support for older versions."""
    version = slot_data.get("world_version")
    ours = world.world_version.as_simple_string()
    if version != ours:
        raise ValueError(f"Bug Fables: this seed was generated with world version {version}, and this apworld is "
                         f"{ours}: use the latest release of both")
    if "options" not in slot_data:
        raise ValueError("Bug Fables: this seed's slot_data has no options, so it comes from an older apworld: use the "
                         "latest release")
    sent = slot_data["options"]
    world.options = BugFablesOptions(**{name: option.from_any(sent.get(name, option.default))
                                        for name, option in BugFablesOptions.type_hints.items()})


def defer_doors(world: BugFablesWorld) -> None:
    """Universal Tracker's deferred entrances: while it rebuilds a seed with shuffled doors, each one stays unconnected
    until the player has gone through it, so the tracker never shows the spoiler's doors. Its host.yaml
    enforce_deferred_entrances decides: "on" and "default" (its default) defer, "off" shows every door."""
    world.deferred_doors = {}
    world.way_back = {}
    deferring = getattr(world.multiworld, "enforce_deferred_connections", "off") in ("on", "default")
    if not world.door_pairings or not deferring:
        return
    pairs = set(world.door_pairings)
    coupled = world.options.entrance_randomizer != EntranceRandomizer.option_decoupled
    for x, y in world.door_pairings:
        entrance = world.get_entrance(door_name(*x))
        region = entrance.connected_region
        region.entrances.remove(entrance)
        entrance.connected_region = None
        world.deferred_doors[entrance.name] = region
        # Coupled and Room Swap: walking through x comes out of y, whose door leads back (a one-way lands, no door).
        if coupled and x not in _ONE_WAYS and (y, x) in pairs:
            world.way_back[entrance.name] = door_name(*y)
    world.found_entrances_datastorage_key = DOORS_TAKEN_KEY


def reconnect_doors(world: BugFablesWorld, taken: Any) -> None:
    """The doors the player has gone through, from the doors key (None before the first): each deferred one connected
    again, with its way back when the doors are coupled. Anything but a list of names is left alone."""
    for name in taken if isinstance(taken, list) else ():
        if not isinstance(name, str):
            continue
        name = _COPY_OF.get(name, name)
        for door in (name, world.way_back.get(name)):
            region = world.deferred_doors.pop(door, None) if door else None
            if region is not None:
                world.get_entrance(door).connect(region)
