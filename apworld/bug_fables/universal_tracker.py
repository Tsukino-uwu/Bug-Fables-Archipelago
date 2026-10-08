"""Universal Tracker: a seed rebuilt from its slot_data, with no yaml. Universal Tracker regenerates this world on its
own with re_gen_passthrough["Bug Fables"] holding the seed's slot_data, and every roll then comes from that instead of
the random (its docs/apworld-integration.md, "Generating without a YAML")."""
from __future__ import annotations

from collections.abc import Mapping
from typing import TYPE_CHECKING, Any

from .options import BugFablesOptions

if TYPE_CHECKING:
    from .world import BugFablesWorld


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
