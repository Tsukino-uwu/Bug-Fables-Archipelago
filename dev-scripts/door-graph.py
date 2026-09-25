"""Dev-only: the map-to-map door graph, with the doors that have no way back marked, and each door paired with its way back.

Reads EntityDump's output (every map's entities). A door to another map (`DoorOtherMap`) sends the party to the map
in its data[0], placing it at vectordata[1] (NPCControl.OnTriggerEnter -> MainManager.TransferMap(data[0], vectordata[0],
vectordata[1], vectordata[2])). One row per door:

    from map, door entity, to map, required flags, hiding flags, back, pair, distance

"back" lists the doors on the target map that lead back to this map. "NONE" means no door leads back: a one-way
candidate, or a way back that isn't a door (an event, a drop, a fall). "pair" is the one of those the party arrives
next to: the door whose position is nearest to where this door places the party, with that distance (in 3D: Rubber
Prison's pier stacks doors floor above floor). Nothing nearer than FAR is no pair: Barren Lands' "return" zones put
the party 25-75 units from any door. A pair is "mutual" when the back door's own pair is this door or a variant of it:
doors on one map within SAME of each other are one door in different story states (Golden Settlement's day and night
copies, flags 85/86, the night one leading to the night map). The entrance randomizer's coupled mode needs the pairs
mutual. Pairing needs a dump with the position column (EntityDump from 2026-09-25 on).

The dump can't see connections inside a map (ledges, drops, switch barriers) or transfers started by events; those come
from play and go into agent_docs/MEASURED.md.

    python dev-scripts/door-graph.py <bugfablesap-entitydump.tsv> [<map name prefix>] [<decompiled folder>]
    python dev-scripts/door-graph.py <bugfablesap-entitydump.tsv> --export apworld/bug_fables/data/doors.json

--export writes the entrance randomizer's door table (see export() for what goes in and what stays fixed).

Reads only; the game's code and data never leave your machine.
"""
import collections
import csv
import importlib.util
import json
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


FAR = 10.0
SAME = 1.0


def distance(a: tuple[float, float, float], b: tuple[float, float, float]) -> float:
    return math.dist(a, b)


def same_door(a: dict, b: dict) -> bool:
    return a is b or (a["map"] == b["map"] and a["position"] is not None and b["position"] is not None
                      and distance(a["position"], b["position"]) < SAME)


def pair_doors(doors: list[dict]) -> dict[tuple[str, int], tuple[dict, float]]:
    """(map, entity index) -> (back door on the target map, distance from the arrival point to it). By index, not
    name: a map can hold two doors of one name, swapped by story flags (WaspKingdomOutside's loadzoneinside)."""
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
        best = min(back, key=lambda b: distance(arrive, b["position"]))
        if distance(arrive, best["position"]) < FAR:
            pairs[(d["map"], d["index"])] = (best, distance(arrive, best["position"]))
    return pairs


def export(doors: list[dict], pairs: dict, out: Path) -> None:
    """The entrance randomizer's door table: every door that pairs mutually with its way back, as connections (a, b):
    going through a arrives next to b, and through b next to a. The mod finds a door by its map and name, so a door
    stays fixed (out of the shuffle) when its name isn't unique on its map, when it has a story variant at the same
    spot (it would need rewriting together with its variants, and a night variant leads to the night map), or when it
    leads into its own map. Its pair then stays fixed too. Every other door between maps is listed in "fixed" as
    (from map, to map), so the generator can count the maps it already joins."""
    by_key = {(d["map"], d["index"]): d for d in doors}
    name_count = collections.Counter((d["map"], d["name"]) for d in doors)

    def shuffleable(d: dict) -> bool:
        return (name_count[(d["map"], d["name"])] == 1 and d["to"] != d["map"]
                and not any(o is not d and same_door(o, d) for o in doors if o["map"] == d["map"]))

    connections, taken = [], set()
    for d in doors:
        key = (d["map"], d["index"])
        pair = pairs.get(key)
        if key in taken or not pair or not shuffleable(d) or not shuffleable(pair[0]):
            continue
        b = pair[0]
        theirs = pairs.get((b["map"], b["index"]))
        if theirs is None or theirs[0] is not d:
            continue
        taken |= {key, (b["map"], b["index"])}
        connections.append({"a": {"map": d["map"], "door": d["name"]}, "b": {"map": b["map"], "door": b["name"]}})
    fixed = sorted({(d["map"], d["to"]) for key, d in by_key.items() if key not in taken})
    table = {
        "_about": "Generated by dev-scripts/door-graph.py --export from the mod's EntityDump; do not edit by hand.",
        "connections": connections,
        "fixed": [list(f) for f in fixed],
    }
    out.write_text(json.dumps(table, indent=1) + "\n", encoding="utf-8")
    print(f"# exported {len(connections)} connections ({2 * len(connections)} doors), {len(fixed)} fixed map links -> {out}",
          file=sys.stderr)


def main() -> None:
    dump = Path(sys.argv[1])
    out = Path(sys.argv[3]) if len(sys.argv) > 3 and sys.argv[2] == "--export" else None
    prefix = sys.argv[2] if len(sys.argv) > 2 and out is None else ""
    decompiled = Path(sys.argv[3]) if len(sys.argv) > 3 and out is None else HERE.parent / "decompiled"
    names = map_names(decompiled)

    doors = []
    for r in csv.DictReader(dump.open(encoding="utf-8"), delimiter="\t"):
        if r["objecttype"] != "DoorOtherMap" or not r["data"].split():
            continue
        target = int(r["data"].split()[0])
        doors.append({
            "map": r["map"], "index": int(r["index"]), "name": r["name"],
            "to": names[target] if 0 <= target < len(names) else f"?{target}",
            "requires": flags(r["requires"]), "limit": flags(r["limit"]),
            "vectordata": r.get("vectordata"), "position": point(r.get("position")),
        })

    leading_to = collections.defaultdict(list)  # (from, to) -> door names
    for d in doors:
        leading_to[(d["map"], d["to"])].append(d["name"])
    pairs = pair_doors(doors)
    if out is not None:
        export(doors, pairs, out)
        return

    rows = []
    for d in doors:
        if prefix and not (d["map"].startswith(prefix) or d["to"].startswith(prefix)):
            continue
        back = leading_to.get((d["to"], d["map"]))
        pair = pairs.get((d["map"], d["index"]))
        if pair:
            theirs = pairs.get((d["to"], pair[0]["index"]))
            mutual = theirs is not None and same_door(theirs[0], d)
            paired = (pair[0]["name"] + ("" if mutual else " (not mutual)"), f"{pair[1]:.1f}")
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
