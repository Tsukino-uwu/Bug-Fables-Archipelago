"""Dev-only: the map-to-map door graph, with the doors that have no way back marked, and each door paired with its way back.

Reads EntityDump's output (every map's entities). A door to another map (`DoorOtherMap`) sends the party to the map
in its data[0], placing it at vectordata[1] (NPCControl.OnTriggerEnter -> MainManager.TransferMap(data[0], vectordata[0],
vectordata[1], vectordata[2])). One row per door:

    from map, door entity, to map, required flags, hiding flags, back, pair, distance

"back" lists the doors on the target map that lead back to this map. "NONE" means no door leads back: a one-way
candidate, or a way back that isn't a door (an event, a drop, a fall). "pair" is the one of those the party arrives
next to: the door whose position is nearest (on the ground plane) to where this door places the party, with that
distance. A pair is "mutual" when the back door's own pair is this door; the entrance randomizer's coupled mode needs
the pairs mutual. Pairing needs a dump with the position column (EntityDump from 2026-09-25 on).

The dump can't see connections inside a map (ledges, drops, switch barriers) or transfers started by events; those come
from play and go into agent_docs/MEASURED.md.

    python dev-scripts/door-graph.py <bugfablesap-entitydump.tsv> [<map name prefix>] [<decompiled folder>]

Reads only; the game's code and data never leave your machine.
"""
import collections
import csv
import importlib.util
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent


def map_names(decompiled: Path) -> list[str]:
    # The same enum reader as gate-table.py (MainManager.Maps, in order).
    spec = importlib.util.spec_from_file_location("gate_table", HERE / "gate-table.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.map_names(decompiled)


def flags(field: str) -> str:
    return ",".join(f for f in field.split() if f != "-1")


def point(field: str | None) -> tuple[float, float, float] | None:
    if not field:
        return None
    x, y, z = (float(v) for v in field.split(":"))
    return x, y, z


def ground_distance(a: tuple[float, float, float], b: tuple[float, float, float]) -> float:
    return math.hypot(a[0] - b[0], a[2] - b[2])


def pair_doors(doors: list[dict]) -> dict[tuple[str, str], tuple[str, float]]:
    """(map, door) -> (back door on the target map, distance from the arrival point to it)."""
    leading_to = collections.defaultdict(list)  # (from, to) -> doors
    for d in doors:
        leading_to[(d["map"], d["to"])].append(d)
    pairs = {}
    for d in doors:
        vectors = [point(v) for v in (d["vectordata"] or "").split()]
        if len(vectors) < 2:
            continue
        arrive = vectors[1]
        back = [b for b in leading_to.get((d["to"], d["map"]), []) if b["position"] is not None]
        if not back:
            continue
        best = min(back, key=lambda b: ground_distance(arrive, b["position"]))
        pairs[(d["map"], d["name"])] = (best["name"], ground_distance(arrive, best["position"]))
    return pairs


def main() -> None:
    dump = Path(sys.argv[1])
    prefix = sys.argv[2] if len(sys.argv) > 2 else ""
    decompiled = Path(sys.argv[3]) if len(sys.argv) > 3 else HERE.parent / "decompiled"
    names = map_names(decompiled)

    doors = []
    for r in csv.DictReader(dump.open(encoding="utf-8"), delimiter="\t"):
        if r["objecttype"] != "DoorOtherMap" or not r["data"].split():
            continue
        target = int(r["data"].split()[0])
        doors.append({
            "map": r["map"], "name": r["name"],
            "to": names[target] if 0 <= target < len(names) else f"?{target}",
            "requires": flags(r["requires"]), "limit": flags(r["limit"]),
            "vectordata": r.get("vectordata"), "position": point(r.get("position")),
        })

    leading_to = collections.defaultdict(list)  # (from, to) -> door names
    for d in doors:
        leading_to[(d["map"], d["to"])].append(d["name"])
    pairs = pair_doors(doors)

    rows = []
    for d in doors:
        if prefix and not (d["map"].startswith(prefix) or d["to"].startswith(prefix)):
            continue
        back = leading_to.get((d["to"], d["map"]))
        pair = pairs.get((d["map"], d["name"]))
        if pair:
            mutual = pairs.get((d["to"], pair[0]), (None,))[0] == d["name"]
            paired = (pair[0] + ("" if mutual else " (not mutual)"), f"{pair[1]:.1f}")
        else:
            paired = ("", "")
        rows.append((d["map"], d["name"], d["to"], d["requires"], d["limit"], " | ".join(back) if back else "NONE") + paired)
    print("from\tdoor\tto\trequires\thidden by\tback\tpair\tdistance")
    for row in sorted(rows):
        print("\t".join(row))
    missing = sum(1 for r in rows if r[5] == "NONE")
    lonely = sum(1 for r in rows if "not mutual" in r[6])
    print(f"# {len(rows)} doors, {missing} with no door back, {lonely} paired but not mutually", file=sys.stderr)


if __name__ == "__main__":
    main()
