"""Dev-only: the map-to-map door graph, with the doors that have no way back marked.

Reads EntityDump's output (every map's entities). A door to another map (`DoorOtherMap`) sends the party to the map
in its data[0] (NPCControl.OnTriggerEnter -> MainManager.TransferMap(data[0], ...)). One row per door:

    from map, door entity, to map, required flags, hiding flags, back

"back" lists the doors on the target map that lead back to this map. "NONE" means no door leads back: a one-way
candidate, or a way back that isn't a door (an event, a drop, a fall). The dump can't see connections inside a map
(ledges, drops, switch barriers) or transfers started by events; those come from play and go into
agent_docs/MEASURED.md.

    python dev-scripts/door-graph.py <bugfablesap-entitydump.tsv> [<map name prefix>] [<decompiled folder>]

Reads only; the game's code and data never leave your machine.
"""
import collections
import csv
import importlib.util
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
        to = names[target] if 0 <= target < len(names) else f"?{target}"
        doors.append((r["map"], r["name"], to, flags(r["requires"]), flags(r["limit"])))

    leading_to = collections.defaultdict(list)  # (from, to) -> door names
    for src, name, to, _, _ in doors:
        leading_to[(src, to)].append(name)

    rows = []
    for src, name, to, req, lim in doors:
        if prefix and not (src.startswith(prefix) or to.startswith(prefix)):
            continue
        back = leading_to.get((to, src))
        rows.append((src, name, to, req, lim, " | ".join(back) if back else "NONE"))
    print("from\tdoor\tto\trequires\thidden by\tback")
    for row in sorted(rows):
        print("\t".join(row))
    missing = sum(1 for r in rows if r[5] == "NONE")
    print(f"# {len(rows)} doors, {missing} with no door back", file=sys.stderr)


if __name__ == "__main__":
    main()
