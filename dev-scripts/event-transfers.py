"""Dev-only: every map change a story event makes, from the decompiled code.

Doors aren't the only way between maps: an event can call MainManager.LoadMap(id) and place the party itself (the bar's
hatch, a boat, a guard catching you). This lists, per event method in EventControl.cs, each LoadMap call and its target:

    event, line, target map

LoadMap() with no argument reloads the current map; an argument that isn't a plain number or a Maps name is printed as
it stands (a variable, worked out by reading the code). Then run event-triggers.py on the events listed to see what
starts each one.

    python dev-scripts/event-transfers.py [<decompiled folder>]

Reads only; the game's code never leaves your machine.
"""
import importlib.util
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
EVENT = re.compile(r"IEnumerator Event(\d+)\(")
LOAD = re.compile(r"LoadMap\(([^;]*?)\)\s*;")


def map_names(decompiled: Path) -> list[str]:
    spec = importlib.util.spec_from_file_location("gate_table", HERE / "gate-table.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.map_names(decompiled)


def target(arg: str, names: list[str]) -> str:
    arg = arg.strip()
    if not arg:
        return "(reload the current map)"
    first = arg.split(",")[0].strip()
    if first.isdigit():
        n = int(first)
        return names[n] if n < len(names) else f"?{n}"
    m = re.search(r"Maps\.(\w+)", first)
    return m.group(1) if m else f"({first})"


def main() -> None:
    decompiled = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE.parent / "decompiled"
    names = map_names(decompiled)
    event = None
    rows = []
    for number, line in enumerate((decompiled / "EventControl.cs").read_text(encoding="utf-8").splitlines(), 1):
        m = EVENT.search(line)
        if m:
            event = int(m.group(1))
            continue
        for call in LOAD.finditer(line):
            rows.append((event, number, target(call.group(1), names)))
    print("event\tline\ttarget")
    for row in rows:
        print("\t".join(str(v) for v in row))
    print(f"# {len(rows)} LoadMap calls in {len({r[0] for r in rows})} events", file=sys.stderr)


if __name__ == "__main__":
    main()
