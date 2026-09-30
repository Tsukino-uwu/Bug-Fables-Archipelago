"""Dev-only: every fixed HP number in the enemies' scripts (a heal, an HP set outright, a threshold), with its enemy.

    python dev-scripts/enemy-numbers.py [<decompiled folder>]

Enemy scaling scales an enemy's HP; a number written into its script doesn't scale with it. Each site listed here is
read and classified in MEASURED.md ("Fixed numbers in the enemies' scripts"), then scaled or kept. Relative forms
(a share of maxhp, HPPercent) are listed too, marked "already scales".
"""
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent

ENEMY = r"enemydata\[[^\]]+\]"
PATTERNS = [
    ("heal", re.compile(r"Heal\(ref " + ENEMY + r",\s*(?P<n>\d+)\)")),
    ("heal (either)", re.compile(r"Heal\(ref " + ENEMY + r",\s*[^;]*\?\s*(?P<n>\d+)\s*:")),
    ("hp set", re.compile(ENEMY + r"\.hp\s*=\s*(?P<n>[1-9]\d*);")),
    ("hp change", re.compile(ENEMY + r"\.hp\s*[-+]=\s*(?P<n>\d+);")),
    ("threshold", re.compile(ENEMY + r"\.hp\s*(?:<=|<|>=|>)\s*(?P<n>[1-9]\d*)(?![\d.f])")),
]
RELATIVE = re.compile(ENEMY + r"\.maxhp|HPPercent\(")
CASE = re.compile(r"case MainManager\.Enemies\.(\w+):")
METHOD = re.compile(r"^\t(?:private|public|internal)[^(\n]*\s(\w+)\(")


def scan(decompiled: Path):
    path = decompiled / "BattleControl.cs"
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    method, enemy = "", ""
    for number, line in enumerate(lines, 1):
        m = METHOD.match(line)
        if m:
            method, enemy = m.group(1), ""
        c = CASE.search(line)
        if c and method == "DoAction":
            enemy = c.group(1)
        where = f"{method}" + (f" / {enemy}" if enemy else "")
        for kind, pattern in PATTERNS:
            for found in pattern.finditer(line):
                yield number, where, kind, found.group("n"), line.strip()
        if RELATIVE.search(line) and ("Heal(" in line or ".hp" in line):
            yield number, where, "relative", "-", line.strip()


def main() -> None:
    decompiled = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE.parent / "decompiled"
    if not (decompiled / "BattleControl.cs").exists():
        sys.exit(f"no BattleControl.cs in {decompiled} (the decompiled game, development.md)")
    rows = list(scan(decompiled))
    print("line\twhere\tkind\tnumber\tcode")
    for number, where, kind, n, code in rows:
        print(f"{number}\t{where}\t{kind}\t{n}\t{code[:110]}")
    fixed = [r for r in rows if r[2] != "relative"]
    print(f"\n{len(fixed)} fixed numbers, {len(rows) - len(fixed)} relative", file=sys.stderr)


if __name__ == "__main__":
    main()
