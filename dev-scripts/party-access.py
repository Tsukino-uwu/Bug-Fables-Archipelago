"""Dev-only: every way the game's code reaches for a party member or a follower, from the decompiled code.

A scene written for Vi, Kabbu and Leif looks them up in many ways; with a smaller party each one is a place that can find
nothing. This lists each way, how often it's used, and in which methods (events by number), so each can be covered once
instead of found one crash at a time (the user, 2026-09-25).

    python dev-scripts/party-access.py [<decompiled folder>] [--where <pattern name>]

Reads only; the game's code never leaves your machine.
"""
import collections
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent

# (name, regex, what it means). Order matters only for the printout.
PATTERNS = [
    ("GetEntity(-1) leader", r"GetEntity\(-1\)", "the first party member by position"),
    ("GetEntity(-2) 2nd", r"GetEntity\(-2\)", "the second party member by position"),
    ("GetEntity(-3) 3rd", r"GetEntity\(-3\)", "the third party member by position"),
    ("GetEntity(-4) Vi", r"GetEntity\(-4\)", "Vi by character"),
    ("GetEntity(-5) Kabbu", r"GetEntity\(-5\)", "Kabbu by character"),
    ("GetEntity(-6) Leif", r"GetEntity\(-6\)", "Leif by character"),
    ("GetEntity(-(4 + n))", r"GetEntity\(-\(4 \+", "a member by character, computed"),
    ("GetEntity(1000+)", r"GetEntity\(10\d\d\)", "a follower by index (map.tempfollowers)"),
    ("tempfollowers[..]", r"tempfollowers\[", "a follower by index, directly"),
    ("GetExtraFollower", r"GetExtraFollower\(", "a follower by character"),
    ("extrafollowers.Add", r"extrafollowers\.Add\(", "a character made a follower"),
    ("extrafollowers.Remove", r"extrafollowers\.Remove\(", "a follower taken off"),
    ("AddFollower", r"AddFollower\(", "a follower character made"),
    ("playerdata[0]", r"playerdata\[0\]", "the first member's data"),
    ("playerdata[1]", r"playerdata\[1\]", "the second member's data"),
    ("playerdata[2]", r"playerdata\[2\]", "the third member's data"),
    ("playerdata[i] loop", r"playerdata\[[a-z]\w*\]", "a member's data by a variable index"),
    ("playerdata.Length", r"playerdata\.Length", "the party's size"),
    ("GetPartyEntities(true)", r"GetPartyEntities\((idorder: )?true\)", "the party as Vi, Kabbu, Leif slots"),
    ("GetPartyEntities()", r"GetPartyEntities\(\)", "the party in its order"),
    ("partyorder", r"partyorder", "the party's order list"),
    ("HasPlayer", r"HasPlayer\(", "is this member in the party"),
    ("ChangeParty", r"ChangeParty\(", "the party changed"),
    ("SetPlayers(pos)", r"SetPlayers\(\w", "party characters remade, at positions"),
    ("SetPlayers()", r"SetPlayers\(\)", "party characters made without removing the old ones"),
    ("PartyMover", r"PartyMover\(", "every member walked to the player"),
    (".following =", r"\.following = ", "a character set to follow another"),
    ("MainManager.player", r"MainManager\.player\b", "the leader's controls"),
    ("|next,-4..-6|", r"next,-[456]", "a dialogue line handed to a member (in code-built text)"),
]

EVENT = re.compile(r"(?:private|public|internal)[^\n(]*\s(\w+)\(")


def scan(decompiled: Path) -> dict[str, collections.Counter]:
    found = {name: collections.Counter() for name, _, _ in PATTERNS}
    for path in sorted(decompiled.glob("*.cs")):
        method = path.stem
        for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
            m = EVENT.search(line)
            if m and "{" not in line and "=" not in line.split("(")[0]:
                method = f"{path.stem}.{m.group(1)}"
            for name, pattern, _ in PATTERNS:
                n = len(re.findall(pattern, line))
                if n:
                    found[name][method] += n
    return found


def main() -> None:
    args = [a for a in sys.argv[1:]]
    where = None
    if "--where" in args:
        i = args.index("--where")
        where = args[i + 1]
        del args[i:i + 2]
    decompiled = Path(args[0]) if args else HERE.parent / "decompiled"
    found = scan(decompiled)
    if where:
        for method, n in found[where].most_common():
            print(f"{n}\t{method}")
        return
    print("way\tuses\tmethods\tevents\tmeaning")
    for name, _, meaning in PATTERNS:
        c = found[name]
        events = sorted({m for m in c if ".Event" in m and m.split(".Event")[1].isdigit()},
                        key=lambda m: int(m.split(".Event")[1]))
        print(f"{name}\t{sum(c.values())}\t{len(c)}\t{len(events)}\t{meaning}")


if __name__ == "__main__":
    main()
