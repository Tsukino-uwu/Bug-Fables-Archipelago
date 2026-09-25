"""The entrance randomizer's coupled shuffle.

It grows the world outwards from one area, since a random pairing strands dead-end rooms joined to each other.
"""
from __future__ import annotations

from random import Random
from typing import Any

Door = tuple[str, str]  # (map, door entity name)


def _partners(connections: list[dict[str, Any]]) -> dict[Door, Door]:
    partner: dict[Door, Door] = {}
    for c in connections:
        a, b = (c["a"]["map"], c["a"]["door"]), (c["b"]["map"], c["b"]["door"])
        partner[a], partner[b] = b, a
    return partner


def _areas(maps: set[str], fixed: list[list[str]]) -> dict[str, str]:
    """Each map's area: maps joined by fixed doors share one."""
    parent = {m: m for m in maps}

    def find(m: str) -> str:
        while parent[m] != m:
            parent[m] = parent[parent[m]]
            m = parent[m]
        return m

    for a, b in fixed:
        parent.setdefault(a, a)
        parent.setdefault(b, b)
        parent[find(a)] = find(b)
    return {m: find(m) for m in parent}


def shuffle_coupled(connections: list[dict[str, Any]], fixed: list[list[str]], random: Random) -> list[dict[str, str]]:
    """Doors paired anew, as door_targets: each door leads where its new partner's old partner led."""
    partner = _partners(connections)
    doors = sorted(partner)
    area = _areas({m for m, _ in doors}, fixed)
    by_area: dict[str, list[Door]] = {}
    for d in doors:
        by_area.setdefault(area[d[0]], []).append(d)

    unreached = sorted(by_area)
    first = unreached.pop(random.randrange(len(unreached)))
    open_doors = list(by_area[first])
    pairs: list[tuple[Door, Door]] = []
    while unreached:
        x = open_doors.pop(random.randrange(len(open_doors)))
        choices = unreached
        if not open_doors and len(unreached) > 1:
            # x was the last open door: join it to an area that brings new open doors, or the rest is cut off.
            choices = [a for a in unreached if len(by_area[a]) > 1] or unreached
        target = choices[random.randrange(len(choices))]
        unreached.remove(target)
        entry = list(by_area[target])
        y = entry.pop(random.randrange(len(entry)))
        pairs.append((x, y))
        open_doors.extend(entry)
        if not open_doors and unreached:
            raise RuntimeError("Bug Fables: the door shuffle ran out of open doors before every area was reached")
    random.shuffle(open_doors)
    pairs.extend(zip(open_doors[0::2], open_doors[1::2]))

    targets = []
    for x, y in pairs:
        for door, joined in ((x, y), (y, x)):
            like = partner[joined]
            if like != door:
                targets.append({"map": door[0], "door": door[1], "like_map": like[0], "like_door": like[1]})
    return targets


def arrivals(connections: list[dict[str, Any]], targets: list[dict[str, str]]) -> dict[Door, Door]:
    """Where each door leads once the targets are applied: the door the party arrives next to."""
    partner = _partners(connections)
    like = {(t["map"], t["door"]): (t["like_map"], t["like_door"]) for t in targets}
    return {d: partner[like.get(d, d)] for d in partner}
