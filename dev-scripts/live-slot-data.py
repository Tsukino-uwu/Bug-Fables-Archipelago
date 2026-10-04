"""Writes the slot_data the apworld on disk gives a player file, for the dev console's `liveslot` to apply in game.

    python dev-scripts/live-slot-data.py --archipelago <your Archipelago checkout> --yaml <player file> --out <file>

It generates the player file alone twice, with two fixed seeds, and keeps only the keys both give alike: what the
apworld decides (blockers, flags, gives), never what a seed rolls (placements, shuffles), which stay the server's.
The world must be linked into the checkout (development.md, "The apworld's tests").
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import shutil
import tempfile
from pathlib import Path

_spec = importlib.util.spec_from_file_location("seed_snapshot", Path(__file__).with_name("seed-snapshot.py"))
seed_snapshot = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(seed_snapshot)


def slot_data_for(archipelago: Path, yaml: Path, seed: int) -> dict:
    with tempfile.TemporaryDirectory() as work:
        players = Path(work) / "players"
        players.mkdir()
        shutil.copy(yaml, players / yaml.name)
        out = Path(work) / "out"
        out.mkdir()
        archive = seed_snapshot.run_generate(archipelago, players, out, seed)
        return seed_snapshot.decode(archipelago, archive)[0]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--archipelago", required=True, type=Path)
    parser.add_argument("--yaml", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()
    archipelago = args.archipelago.resolve()
    first = slot_data_for(archipelago, args.yaml.resolve(), 1)
    second = slot_data_for(archipelago, args.yaml.resolve(), 2)
    fixed = {key: value for key, value in first.items() if second.get(key) == value}
    rolled = sorted(key for key in first if key not in fixed)
    args.out.write_text(json.dumps(fixed, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{len(fixed)} keys fixed by the apworld written to {args.out}; left to the seed: {', '.join(rolled)}")


if __name__ == "__main__":
    main()
