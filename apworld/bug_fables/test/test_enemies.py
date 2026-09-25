from collections import Counter
from random import Random

from . import BugFablesTestBase
from ..data_tables import ENCOUNTERS
from ..world import shuffle_encounters


class TestEnemiesOffByDefault(BugFablesTestBase):
    def test_no_fight_changed(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["enemy_swaps"], {})


class TestEnemiesOnly(BugFablesTestBase):
    options = {"enemy_shuffle": "enemies_only"}

    def test_every_map_enemy_listed(self) -> None:
        swaps = self.world.fill_slot_data()["enemy_swaps"]
        self.assertEqual(set(swaps), {f'{e["map"]}:{e["entity"]}' for e in ENCOUNTERS})

    def test_fights_keep_their_size(self) -> None:
        swaps = self.world.fill_slot_data()["enemy_swaps"]
        for e in ENCOUNTERS:
            key = f'{e["map"]}:{e["entity"]}'
            self.assertEqual(len(swaps[key]), len(e["ids"]), key)

    def test_every_fight_still_happens_once(self) -> None:
        # A permutation within each size: the same fights, only at other places.
        swaps = self.world.fill_slot_data()["enemy_swaps"]
        self.assertEqual(Counter(tuple(f) for f in swaps.values()), Counter(tuple(e["ids"]) for e in ENCOUNTERS))

    def test_fights_move(self) -> None:
        swaps = self.world.fill_slot_data()["enemy_swaps"]
        moved = sum(1 for e in ENCOUNTERS if swaps[f'{e["map"]}:{e["entity"]}'] != e["ids"])
        self.assertGreater(moved, len(ENCOUNTERS) // 2)


class TestEnemyShuffleSeeded(BugFablesTestBase):
    def test_same_seed_same_fights(self) -> None:
        self.assertEqual(shuffle_encounters(ENCOUNTERS, Random(7)), shuffle_encounters(ENCOUNTERS, Random(7)))
