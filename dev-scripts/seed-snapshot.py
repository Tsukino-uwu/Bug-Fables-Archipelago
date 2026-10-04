"""Generates fixed seeds and writes what a seed decides, so a refactor can prove it changed nothing.

    python dev-scripts/seed-snapshot.py --archipelago <your Archipelago checkout> --out <folder>

For each of CI's three presets, alone and with APQuest, it runs Generate.py with a fixed seed and writes
<case>.slot.json (Bug Fables' slot_data, as MultiServer decodes it, keys in their own order) and <case>.spoiler.txt.
Take a snapshot before a change and one after, then compare the two folders (diff -r): they must be identical.
The world must be linked into the checkout (development.md, "The apworld's tests").
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import tempfile
import zipfile
import zlib
from pathlib import Path

PRESETS = [
    {},
    {"entrance_randomizer": "coupled", "enemy_shuffle": "enemies_only", "starting_location": "anywhere",
     "shuffle_discoveries": True},
    {"shuffle_quests": False, "shuffle_crystal_berries": False, "shuffle_medal_shops": False,
     "shuffle_item_shops": False, "shop_contents": "filler_only"},
]


def generate(archipelago: Path, options: dict, with_apquest: bool, seed: int, work: Path) -> Path:
    players, out = work / "players", work / "out"
    players.mkdir(parents=True)
    out.mkdir()
    (players / "bf.yaml").write_text(
        "name: BugTester\ngame: Bug Fables\nBug Fables: " + json.dumps(options) + "\n", encoding="utf-8")
    if with_apquest:
        (players / "aq.yaml").write_text("name: QuestTester\ngame: APQuest\nAPQuest: {}\n", encoding="utf-8")
    return run_generate(archipelago, players, out, seed)


def run_generate(archipelago: Path, players: Path, out: Path, seed: int) -> Path:
    env = dict(os.environ, SKIP_REQUIREMENTS_UPDATE="1")
    subprocess.run([sys.executable, "Generate.py", "--player_files_path", str(players), "--outputpath", str(out),
                    "--seed", str(seed), "--spoiler", "3"], cwd=archipelago, env=env, check=True,
                   stdout=subprocess.DEVNULL)
    zips = list(out.glob("AP_*.zip"))
    if len(zips) != 1:
        raise RuntimeError(f"expected one AP_*.zip in {out}, found {len(zips)}")
    return zips[0]


def decode(archipelago: Path, archive: Path) -> tuple[dict, str]:
    sys.path.insert(0, str(archipelago))
    from Utils import restricted_loads  # the checkout's own, as MultiServer.decompress uses it
    with zipfile.ZipFile(archive) as z:
        data = z.read(next(n for n in z.namelist() if n.endswith(".archipelago")))
        spoiler = z.read(next(n for n in z.namelist() if n.endswith("_Spoiler.txt"))).decode("utf-8")
    if data[0] > 3:
        raise RuntimeError("multidata format newer than 3")
    multidata = restricted_loads(zlib.decompress(data[1:]))
    slot = next(s for s, info in multidata["slot_info"].items() if info.game == "Bug Fables")
    return multidata["slot_data"][slot], spoiler


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--archipelago", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()
    archipelago = args.archipelago.resolve()
    args.out.mkdir(parents=True, exist_ok=True)
    for i, options in enumerate(PRESETS):
        for with_apquest in (False, True):
            case = f"preset{i}" + ("-apquest" if with_apquest else "")
            with tempfile.TemporaryDirectory() as work:
                archive = generate(archipelago, options, with_apquest, i + 1, Path(work))
                slot_data, spoiler = decode(archipelago, archive)
            (args.out / f"{case}.slot.json").write_text(
                json.dumps(slot_data, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
            (args.out / f"{case}.spoiler.txt").write_text(spoiler, encoding="utf-8")
            print(f"{case}: {len(slot_data)} slot_data keys")


if __name__ == "__main__":
    main()
