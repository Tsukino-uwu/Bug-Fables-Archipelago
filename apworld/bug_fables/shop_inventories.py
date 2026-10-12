"""Shuffle Shop Inventories: what an item shop restocks and a respawning pickup comes back with, once neither is a
check, rolled at generation and sent in slot_data. No check, location or rule depends on it."""
from __future__ import annotations

import logging
from dataclasses import dataclass
from random import Random
from typing import Any

from .data_tables import LOCATIONS
from .data_types import Location

# Enough that a shop selling one item twice never survives (about half of all shuffles avoid it).
TRIES = 100


@dataclass(frozen=True, slots=True)
class Spot:
    """An item shop's slot (keeper) or a respawning pickup (regional), and the item the game puts there."""

    map: str
    item: int
    keeper: str | None = None
    regional: int | None = None

    def to_slot(self, to: int) -> dict[str, Any]:
        where = {"keeper": self.keeper} if self.keeper is not None else {"regional": self.regional}
        return {"map": self.map, **where, "item": self.item, "to": to}


def _spot(location: Location) -> Spot | None:
    source = location.source
    if source.item_shop is not None:
        return Spot(source.item_shop.map, source.item_shop.item, keeper=source.item_shop.keeper)
    if source.regional is not None and source.pickup is not None and source.pickup.type == 0:
        return Spot(source.pickup.map, source.pickup.item, regional=source.regional)
    return None


# Every item shop slot and respawning pickup the world knows, whatever the yaml leaves out; their items are the pool.
SPOTS: tuple[Spot, ...] = tuple(spot for spot in map(_spot, LOCATIONS) if spot is not None)


def _sells_twice(spots: tuple[Spot, ...], items: list[int]) -> bool:
    seen: set[tuple[str, str, int]] = set()
    for spot, item in zip(spots, items):
        if spot.keeper is not None:
            if (spot.map, spot.keeper, item) in seen:
                return True
            seen.add((spot.map, spot.keeper, item))
    return False


def shuffle(spots: tuple[Spot, ...], random: Random) -> list[dict[str, Any]]:
    """Each spot takes another's item: a permutation, so every item is still sold or found as often as before, and no
    shop sells one item twice."""
    items = [spot.item for spot in spots]
    for _ in range(TRIES):
        random.shuffle(items)
        if not _sells_twice(spots, items) or _repair(spots, items, random):
            break
    else:
        logging.warning("Bug Fables: no shop inventory shuffle without a repeat in %d tries; one shop sells an item "
                        "twice.", TRIES)
    return [spot.to_slot(to) for spot, to in zip(spots, items)]


def _shop(spot: Spot) -> tuple[str, str] | None:
    return (spot.map, spot.keeper) if spot.keeper is not None else None


def _repair(spots: tuple[Spot, ...], items: list[int], random: Random) -> bool:
    """Swaps each item a shop sells twice with one from elsewhere that neither shop already sells; True once no shop
    repeats. Still a permutation of the same items."""
    for _ in range(len(items)):
        stock: dict[tuple[str, str], list[int]] = {}
        for index, spot in enumerate(spots):
            if _shop(spot) is not None:
                stock.setdefault(_shop(spot), []).append(index)
        repeat = next((i for slots in stock.values() for n, i in enumerate(slots)
                       if items[i] in (items[j] for j in slots[:n])), None)
        if repeat is None:
            return True
        here = _shop(spots[repeat])
        sells = {items[j] for j in stock[here] if j != repeat}
        partners = [j for j in range(len(items)) if j != repeat and _shop(spots[j]) != here
                    and items[j] not in sells
                    and (_shop(spots[j]) is None
                         or items[repeat] not in {items[k] for k in stock[_shop(spots[j])] if k != j})]
        if not partners:
            return False
        j = random.choice(partners)
        items[repeat], items[j] = items[j], items[repeat]
    return not _sells_twice(spots, items)
