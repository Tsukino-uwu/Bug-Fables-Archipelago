"""Dev-only: which story flags gate which doors, and which event sets each flag.

Joins EntityDump's output (every map's entities) with the decompiled game code (who sets each flag), and prints
one row per door to another map that has required or hiding flags:

    from map, to map, required flags, hiding flags

Each flag is followed by the chapter of the event(s) that set it, in brackets, by the event-number rule in
agent_docs/MEASURED.md ("Chapters"): P = prologue, 1-7 = chapter, dlg = set only by dialogue (not in code).

    python dev-scripts/gate-table.py <bugfablesap-entitydump.tsv> [<decompiled folder>]

Reads only; the game's code and data never leave your machine.
"""
import collections
import csv
import re
import sys
from pathlib import Path

# Chapter title-card events (MEASURED.md): an event numbered below the first is prologue, and so on.
CHAPTER_STARTS = [(16, "P"), (45, "1"), (74, "2"), (105, "3"), (120, "4"), (142, "5"), (194, "6")]
CODE_FILES = ["EventControl.cs", "MainManager.cs", "BattleControl.cs", "NPCControl.cs", "PlayerControl.cs",
              "MapControl.cs"]


def chapter(setter: str) -> str:
    m = re.match(r"EventControl\.Event(\d+)$", setter)
    if not m:
        return "?"
    n = int(m.group(1))
    for start, name in CHAPTER_STARTS:
        if n < start:
            return name
    return "7"


def flag_setters(decompiled: Path) -> dict[int, set[str]]:
    setters: dict[int, set[str]] = collections.defaultdict(set)
    method = re.compile(r"(?:private|public|internal)\s+(?:static\s+)?(?:IEnumerator|void|bool|int)\s+(\w+)\s*\(")
    for name in CODE_FILES:
        current = "?"
        for line in (decompiled / name).read_text(encoding="utf-8", errors="ignore").splitlines():
            m = method.search(line)
            if m:
                current = m.group(1)
            for flag in re.findall(r"flags\[(\d+)\]\s*=\s*true", line):
                setters[int(flag)].add(f"{name[:-3]}.{current}")
    return setters


def map_names(decompiled: Path) -> list[str]:
    source = (decompiled / "MainManager.cs").read_text(encoding="utf-8", errors="ignore")
    body = re.search(r"public enum Maps\s*\{(.*?)\}", source, re.S).group(1)
    return [x.strip().split("=")[0].strip() for x in body.split(",") if x.strip()]


def main() -> None:
    dump = Path(sys.argv[1])
    decompiled = Path(sys.argv[2]) if len(sys.argv) > 2 else Path(__file__).resolve().parent.parent / "decompiled"
    setters = flag_setters(decompiled)
    names = map_names(decompiled)

    def describe(flags: list[str]) -> str:
        return ",".join(f"{f}[{'/'.join(sorted(chapter(s) for s in setters.get(int(f), [])) or ['dlg'])}]"
                        for f in flags)

    rows = []
    for r in csv.DictReader(dump.open(encoding="utf-8"), delimiter="\t"):
        if r["objecttype"] != "DoorOtherMap":
            continue
        required = [f for f in r["requires"].split() if f != "-1"]
        hiding = [f for f in r["limit"].split() if f != "-1"]
        if not required and not hiding:
            continue
        target = int(r["data"].split()[0])
        rows.append((r["map"], names[target] if 0 <= target < len(names) else str(target),
                     describe(required), describe(hiding)))
    for row in sorted(rows):
        print("\t".join(row))


if __name__ == "__main__":
    main()
