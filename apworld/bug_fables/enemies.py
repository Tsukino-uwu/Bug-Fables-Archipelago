"""The enemy shuffle."""
from __future__ import annotations

from collections.abc import Sequence
from random import Random

from .data_types import Encounter


def shuffle_encounters(encounters: Sequence[Encounter], random: Random) -> dict[str, list[int]]:
    """Each map enemy gets another map enemy's fight of the same size, so every fight still happens somewhere."""
    by_size: dict[int, list[Encounter]] = {}
    for encounter in encounters:
        by_size.setdefault(len(encounter.ids), []).append(encounter)
    swaps = {}
    for group in by_size.values():
        fights = [list(encounter.ids) for encounter in group]
        random.shuffle(fights)
        for encounter, fight in zip(group, fights):
            swaps[f"{encounter.map}:{encounter.entity}"] = fight
    return swaps
