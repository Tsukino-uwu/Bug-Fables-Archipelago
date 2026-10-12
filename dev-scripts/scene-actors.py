"""Scenes that can freeze out of story order: a scene walks or waits on a map character that its room only makes
under flags the scene's own start doesn't need (chapter 5's palace scene and Neolith, 2026-10-12).

    python dev-scripts/scene-actors.py <bugfablesap-entitydump.tsv> [<decompiled folder>]

For each Event<n> in EventControl.cs, the maps it starts on are those whose entity table starts it (an EventTrigger's
data, or an entity's eventid); its `MainManager.LoadMap(<n>)` calls move it to another map; each
`MainManager.GetEntities(new int[] {...})` grabs those slots of the map it is on then. A grabbed character whose
`requires` holds a flag neither the scene's start requires nor the scene sets before it is listed: out of order it may
be missing. Its `limit` flags are listed too (it may be gone). Entities the apworld keeps present are marked; holds the
mod adds (held_until) are not read. A lead, read in game before fixing.
"""
from __future__ import annotations

import csv
import re
import sys
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent


def flags(text: str) -> set[int]:
    return {int(f) for f in text.split() if f.lstrip("-").isdigit() and int(f) >= 0}


def main() -> None:
    dump = Path(sys.argv[1])
    decompiled = Path(sys.argv[2]) if len(sys.argv) > 2 else REPO / "decompiled"
    manager = (decompiled / "MainManager.cs").read_text(encoding="utf-8")
    enum = re.search(r"public enum Maps\s*\{(.*?)\}", manager, re.S).group(1)
    map_names = [n.strip().split("=")[0].strip() for n in enum.split(",") if n.strip()]

    slots: dict[str, dict[int, dict]] = defaultdict(dict)
    starts: dict[int, list[tuple[str, set[int], set[int], str]]] = defaultdict(list)
    for row in csv.DictReader(dump.open(encoding="utf-8"), delimiter="\t"):
        slots[row["map"]][int(row["index"])] = row
        event = None
        if row["objecttype"] == "EventTrigger" and row["data"].split():
            event = int(row["data"].split()[0])
        elif row["eventid"].lstrip("-").isdigit() and int(row["eventid"]) > 0:
            event = int(row["eventid"])
        if event:
            starts[event].append((row["map"], flags(row["requires"]), flags(row["limit"]), row["name"]))

    kept = set()
    for path in (REPO / "apworld" / "bug_fables" / "logic").glob("*.py"):
        for m in re.finditer(r'FlagEntity\("(\w+)", "([^"]+)", ALWAYS_SET\)', path.read_text(encoding="utf-8")):
            kept.add((m.group(1), m.group(2)))

    source = (decompiled / "EventControl.cs").read_text(encoding="utf-8")
    bodies = re.split(r"\n\tprivate IEnumerator (Event\d+)\(\)", source)
    found = 0
    for name, body in zip(bodies[1::2], bodies[2::2]):
        event = int(name[5:])
        for start_map, needs, until, by in starts.get(event, []):
            current = start_map
            for m in re.finditer(r"MainManager\.LoadMap\((\d+)\)|GetEntities\(new int\[\d+\] \{ ([\d, ]+) \}\)", body):
                # Flags the scene itself sets before this point make their characters.
                set_before = {int(f) for f in re.findall(r"flags\[(\d+)\] = true", body[:m.start()])}
                if m.group(1):
                    index = int(m.group(1))
                    current = map_names[index] if index < len(map_names) else f"map {index}"
                    continue
                for slot in (int(s) for s in m.group(2).split(",")):
                    row = slots.get(current, {}).get(slot)
                    if row is None:
                        continue
                    extra = flags(row["requires"]) - needs - set_before
                    gone = flags(row["limit"])
                    if not extra:
                        continue
                    found += 1
                    mark = " (kept present)" if (current, row["name"]) in kept else ""
                    print(f"{name} (started on {start_map} by {by}, needs {sorted(needs) or '-'}, until "
                          f"{sorted(until) or '-'}): {current} #{slot} {row['name']} needs {sorted(extra)}"
                          + (f", gone from {sorted(gone)}" if gone else "") + mark)
    print(f"{found} grabbed characters with flags their scene's start doesn't need")


if __name__ == "__main__":
    main()
