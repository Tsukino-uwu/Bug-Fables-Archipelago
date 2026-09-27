"""The enemy shuffle."""
from __future__ import annotations

from random import Random
from typing import Any


def shuffle_encounters(encounters: list[dict[str, Any]], random: Random) -> dict[str, list[int]]:
    """Each map enemy gets another map enemy's fight of the same size, so every fight still happens somewhere."""
    by_size: dict[int, list[dict[str, Any]]] = {}
    for encounter in encounters:
        by_size.setdefault(len(encounter["ids"]), []).append(encounter)
    swaps = {}
    for group in by_size.values():
        fights = [list(encounter["ids"]) for encounter in group]
        random.shuffle(fights)
        for encounter, fight in zip(group, fights):
            swaps[f'{encounter["map"]}:{encounter["entity"]}'] = fight
    return swaps
