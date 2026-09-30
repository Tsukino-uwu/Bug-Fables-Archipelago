"""What each progression item gates in the logic: every location and story event lost without it, and, with --pairs,
what two items gate only together (a rule with an "or"). For reading against the game: an item that loses nothing, or
less than the game needs it for, points at a missing rule; one that loses more points at a rule too strict.

    python <this repo>/dev-scripts/item-gates.py [--pairs] [--option name=value ...]

Run from your Archipelago checkout, with Bug Fables linked (development.md, "The apworld's tests"). By default every
member, move and Jump is an item and every category is in (as TestClassifications), so a rule can name everything.
Story events are earned, never handed out: unlike Archipelago's collect_all_but, which collects placed event items too,
the state here holds only the item pool and sweeps, so a boss beaten counts only where the boss can be reached.
"""
from __future__ import annotations

import argparse
import itertools
import os
import sys

sys.path.insert(0, os.getcwd())

GAME = "Bug Fables"
EVERYTHING = {"starting_party_member": "vi", "shuffle_field_moves": True, "shuffle_jump": True,
              "shuffle_discoveries": True}


def reachable_without(multiworld, player: int, names: frozenset[str]) -> set[str]:
    from BaseClasses import CollectionState

    state = CollectionState(multiworld)
    for item in multiworld.itempool:
        if item.name not in names:
            state.collect(item, prevent_sweep=True)
    state.sweep_for_advancements()
    return {location.name for location in multiworld.get_locations(player) if location.can_reach(state)}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--pairs", action="store_true", help="also what two items gate only together")
    parser.add_argument("--option", action="append", default=[], metavar="NAME=VALUE",
                        help="an option on top of the defaults (repeatable)")
    args = parser.parse_args()

    from test.general import setup_multiworld
    from worlds import AutoWorldRegister

    options = dict(EVERYTHING)
    for pair in args.option:
        name, _, value = pair.partition("=")
        options[name] = {"true": True, "false": False}.get(value.lower(), value)
    multiworld = setup_multiworld(AutoWorldRegister.world_types[GAME], options=options)
    player = 1
    events = {location.name for location in multiworld.get_locations(player) if location.address is None}

    everything = reachable_without(multiworld, player, frozenset())
    unreachable = {location.name for location in multiworld.get_locations(player)} - everything
    progression = sorted({item.name for item in multiworld.itempool if item.player == player and item.advancement})
    print(f"# What each progression item gates\n\nOptions: {options}\n")
    print(f"{len(everything)} locations and events reachable with everything; {len(progression)} progression items.")
    if unreachable:
        print(f"\n**Unreachable even with everything:** {', '.join(sorted(unreachable))}")

    lost = {name: everything - reachable_without(multiworld, player, frozenset({name})) for name in progression}
    print("\n## One item missing\n")
    for name in sorted(progression, key=lambda n: (-len(lost[n]), n)):
        print(f"### Without {name}: {len(lost[name])} lost\n")
        for spot in sorted(lost[name]):
            print(f"- {spot}{' (event)' if spot in events else ''}")
        print()

    if args.pairs:
        print("## Two items missing: what neither loses alone\n")
        found = False
        for a, b in itertools.combinations(progression, 2):
            only_together = everything - reachable_without(multiworld, player, frozenset({a, b})) - lost[a] - lost[b]
            if only_together:
                found = True
                print(f"### Without {a} and {b}: {len(only_together)} more\n")
                for spot in sorted(only_together):
                    print(f"- {spot}{' (event)' if spot in events else ''}")
                print()
        if not found:
            print("None: no two items stand in for each other anywhere.")


if __name__ == "__main__":
    main()
