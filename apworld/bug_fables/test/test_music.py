from typing import Any

from . import BugFablesTestBase
from ..music import JINGLES, KEPT, POOL, TRACKS


class TestMusicOffByDefault(BugFablesTestBase):
    def test_nothing_swapped(self) -> None:
        data = self.world.fill_slot_data()
        self.assertEqual(data["music_map"], {})
        self.assertEqual(data["jingle_map"], {})


class TestMusicShuffle(BugFablesTestBase):
    options = {"music_shuffle": True}

    def test_every_track_still_plays_once(self) -> None:
        music_map = self.world.fill_slot_data()["music_map"]
        self.assertEqual(set(music_map), set(POOL))
        self.assertEqual(sorted(music_map.values()), sorted(POOL))

    def test_kept_tracks_never_move(self) -> None:
        # Title plays before the client connects; the elevator's pair crossfades outside the music player.
        music_map = self.world.fill_slot_data()["music_map"]
        self.assertFalse(KEPT & (set(music_map) | set(music_map.values())))
        self.assertEqual(set(POOL) | KEPT, set(TRACKS))

    def test_tracks_move(self) -> None:
        music_map = self.world.fill_slot_data()["music_map"]
        self.assertGreater(sum(1 for track, played in music_map.items() if track != played), len(POOL) // 2)

    def test_jingles_swap_among_themselves(self) -> None:
        jingle_map = self.world.fill_slot_data()["jingle_map"]
        self.assertEqual(set(jingle_map), set(JINGLES))
        self.assertEqual(sorted(jingle_map.values()), sorted(JINGLES))


class TestMusicChangesNothingElse(BugFablesTestBase):
    # The option is cosmetic: with the same seed, everything the fill and the client act on must be the same.
    auto_construct = False

    def generated(self, options: dict[str, Any]) -> tuple[dict[str, Any], list[str], object]:
        self.options = options
        self.world_setup(seed=42)
        data = {key: value for key, value in self.world.fill_slot_data().items()
                if key not in ("music_map", "jingle_map")}
        return data, [item.name for item in self.multiworld.itempool], self.multiworld.random.getstate()

    def test_same_seed_same_world(self) -> None:
        base = {"enemy_shuffle": "enemies_only", "entrance_randomizer": "coupled", "starting_location": "anywhere"}
        off = self.generated(base)
        on = self.generated({**base, "music_shuffle": True})
        self.assertEqual(off, on)
