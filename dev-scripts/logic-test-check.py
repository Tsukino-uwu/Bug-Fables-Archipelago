"""Proves the Logic Test apworld plays the seed we generated, not a different one.

    python <this repo>/dev-scripts/logic-test-check.py [--seeds N] [--negative]

Run from your Archipelago checkout, with Bug Fables linked and worlds/logic_test copied in (development.md,
"Play-testing the logic"). The Logic Test generates the games under test a second time, reads that copy's spheres,
and puts that copy's items in its own locations. When the copy differs from the real seed, it fills the gaps from
whatever is left over, without a word. Then a stall in play could come from the mismatch, not from the logic.

For each preset, both count_events values, three room layouts and N seeds, this generates the room in-process and
requires the copy's Bug Fables slot_data to equal the real one's, and every relocated item to be the one its
sphere records. --negative gives the copy the wrong RNG seed and requires every run to be flagged.
"""
from __future__ import annotations

import argparse
import json
import logging
import os
import sys
from argparse import Namespace

sys.path.insert(0, os.getcwd())

BF, LT, APQ = "Bug Fables", "Logic Test", "APQuest"
PRESETS = {
    "default": {},
    "doors": {"entrance_randomizer": "coupled", "enemy_shuffle": "enemies_only", "starting_location": "anywhere",
              "shuffle_discoveries": True},
    "small": {"shuffle_quests": False, "shuffle_crystal_berries": False, "shuffle_medal_shops": False,
              "shuffle_item_shops": False, "shop_contents": "filler_only"},
    "moves": {"shuffle_field_moves": True, "shuffle_jump": True, "starting_party_member": "random_member"},
    "minimal": {"accessibility": "minimal", "entrance_randomizer": "coupled", "starting_location": "anywhere"},
}
LAYOUTS = ("bf,lt", "lt,bf", "bf,apq,lt")


def build_args(slots: list[tuple[str, dict]]) -> Namespace:
    import worlds
    from BaseClasses import PlandoOptions

    args = Namespace()
    for player, (game, chosen) in enumerate(slots, 1):
        for key, option in worlds.AutoWorldRegister.world_types[game].options_dataclass.type_hints.items():
            values = getattr(args, key, {})
            values[player] = option.from_any(chosen.get(key, option.default))
            setattr(args, key, values)
    players = range(1, len(slots) + 1)
    args.multi = len(slots)
    args.game = {p: game for p, (game, _) in enumerate(slots, 1)}
    args.name = {p: f"P{p}" for p in players}
    args.sprite = {p: None for p in players}
    args.sprite_pool = {p: None for p in players}
    args.plando = PlandoOptions.from_option_string("bosses, connections, texts")
    args.race = False
    args.outputname = args.outputpath = None
    args.skip_output = True
    args.skip_prog_balancing = False
    args.spoiler = 0
    args.spoiler_only = args.csv_output = False
    return args


def problems_in(slots: list[tuple[str, dict]], seed: int, copies: dict) -> list[str]:
    from Main import main as generate

    multiworld = generate(build_args(slots), seed=seed)
    lt = next(w for w in multiworld.worlds.values() if w.game == LT)
    copy = copies.pop("nested")
    found = []
    for nested_player, player in enumerate(lt.under_test, 1):
        real, copied = multiworld.worlds[player], copy.worlds[nested_player]
        if real.game == BF and (json.dumps(real.fill_slot_data(), sort_keys=True)
                                != json.dumps(copied.fill_slot_data(), sort_keys=True)):
            found.append(f"player {player}'s slot_data differs in the copy")
    leftovers = 0
    for i, sphere in enumerate(lt.spheres, start=1):
        for held_at, (name, player, item, owner) in zip(lt.sphere_locations[i - 1], sphere):
            leftovers += (held_at.item.name, held_at.item.player) != (item, owner)
            key = multiworld.get_location(name, player).item
            if (key.name, key.player) != (f"KEY_{i}", lt.player):
                found.append(f"{name} holds {key.name}, not KEY_{i}")
    if leftovers:
        found.append(f"{leftovers} item(s) relocated from leftovers, not the ones their sphere records")
    return found


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--seeds", type=int, default=3, help="seeds per case (default 3)")
    parser.add_argument("--negative", action="store_true", help="prove a wrong copy is caught")
    options = parser.parse_args()
    logging.getLogger().setLevel(logging.ERROR)

    import worlds.logic_test.pass_a as pass_a
    import worlds.logic_test.world as lt_world

    copies: dict = {}
    run_under_test = lt_world.run_under_test

    def keep_copy(ut_worlds, seed):
        copies["nested"] = run_under_test(ut_worlds, seed)
        return copies["nested"]

    lt_world.run_under_test = keep_copy
    presets = PRESETS
    if options.negative:
        right_seed = pass_a.under_test_rng_seed
        pass_a.under_test_rng_seed = lambda seed, player: right_seed(seed, player) + 1
        presets = {"doors": PRESETS["doors"]}

    runs = flagged = 0
    for preset, chosen in presets.items():
        for count_events in (False, True):
            games = {"bf": (BF, chosen), "lt": (LT, {"count_events": count_events}), "apq": (APQ, {})}
            for layout in LAYOUTS:
                for seed in range(1, options.seeds + 1):
                    runs += 1
                    try:
                        found = problems_in([games[g] for g in layout.split(",")], seed, copies)
                    except Exception as error:  # noqa: BLE001 - a failed generation is a finding too
                        found = [f"{type(error).__name__}: {error}"]
                    if found:
                        flagged += 1
                        if not options.negative:
                            print(f"FAIL {preset} count_events={count_events} {layout} seed {seed}: {'; '.join(found)}")
    if options.negative:
        print(f"Logic Test check, negative: {flagged} of {runs} wrong copies flagged")
        sys.exit(0 if flagged == runs else 1)
    print(f"Logic Test check: {runs - flagged} of {runs} generations reproduced exactly")
    sys.exit(1 if flagged else 0)


if __name__ == "__main__":
    main()
