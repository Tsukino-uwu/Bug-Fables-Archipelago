"""Dev-only: what starts each story event, from the dumps and the decompiled code.

    python dev-scripts/event-triggers.py <entitydump.tsv> <scriptdump.tsv> <mapdump.tsv> <event> [<event> ...]

Looks in entity eventids, triggers, dig spots, pickups, locked doors, |event,N| lines, map autoevents and StartEvent(N).
"""
import collections
import csv
import re
import sys
from pathlib import Path

DECOMPILED = Path(__file__).resolve().parent.parent / "decompiled"
CODE_FILES = ["EventControl.cs", "MainManager.cs", "NPCControl.cs", "BattleControl.cs", "MapControl.cs",
              "PlayerControl.cs"]
DATA_SLOT = {"EventTrigger": 0, "DigSpot": 1, "Item": 1}


def main() -> None:
    entity_dump, script_dump, map_dump = (Path(p) for p in sys.argv[1:4])
    wanted = [int(e) for e in sys.argv[4:]]
    found: dict[int, list[str]] = collections.defaultdict(list)

    for r in csv.DictReader(entity_dump.open(encoding="utf-8"), delimiter="\t"):
        if r["map"] == "TestRoom":
            continue
        gate = f"requires={r['requires'] or '-'} hidden-by={r['limit'] or '-'}"
        if int(r["eventid"]) > 0:
            found[int(r["eventid"])].append(f"talk/touch {r['map']}/{r['name']} {gate}")
        data = [int(x) for x in r["data"].split()] if r["data"] else []
        slot = DATA_SLOT.get(r["objecttype"])
        # A dig spot starts an event only when data[0] >= 2; 0 buries an item, 1 a crystal berry.
        if r["objecttype"] == "DigSpot" and (not data or data[0] < 2):
            slot = None
        if slot is not None and len(data) > slot and data[slot] > 0:
            found[data[slot]].append(f"{r['objecttype']} {r['map']}/{r['name']} {gate}")
        selectors = r["dialogues"].split()
        if r["interact"] == "LockedDoor" and len(selectors) > 1:
            opens = int(float(selectors[1].split(":")[1]))
            if opens > 0:
                found[opens].append(f"locked door {r['map']}/{r['name']} {gate}")

    for line in script_dump.read_text(encoding="utf-8").splitlines()[1:]:
        cols = (line.split("\t") + ["", "", "", ""])[:4]
        for e in re.findall(r"event,(\d+)", cols[3]):
            found[int(e)].append(f"dialogue {cols[0]} line {cols[1]}")

    for line in map_dump.read_text(encoding="utf-8").splitlines()[1:]:
        cols = line.split("\t")
        for pair in cols[1].split():
            flag, event = pair.split(":")
            found[int(event)].append(f"auto-start on {cols[0]} (while flag {flag} is off)")

    for name in CODE_FILES:
        current = "?"
        for number, text in enumerate((DECOMPILED / name).read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
            m = re.search(r"(?:private|public)\s+(?:static\s+)?\w+\s+(\w+)\s*\(", text)
            if m:
                current = m.group(1)
            for e in re.findall(r"StartEvent\((\d+)", text):
                found[int(e)].append(f"code {name}:{number} in {current}")

    for event in wanted:
        print(f"Event{event}:")
        for how in found.get(event, ["(nothing found)"]):
            print(f"    {how}")


if __name__ == "__main__":
    main()
