from ..data_tables import ENCOUNTERS, ENEMY_LOCATIONS
from ..enemysanity import KEY_CHAIN_AREAS
from . import BugFablesTestBase


class TestEnemysanityOff(BugFablesTestBase):
    # Off by default: no enemy location, nothing for the client.
    def test_no_enemy_locations(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        self.assertFalse(names & {loc.name for loc in ENEMY_LOCATIONS})
        self.assertEqual(self.world.fill_slot_data()["location_enemies"], {})


class TestEnemysanityOn(BugFablesTestBase):
    options = {"enemy_sanity": True}

    def test_every_map_enemy_a_location(self) -> None:
        # Every map enemy but those in the key chains' areas, held out as pending (build step 67).
        enemies = self.world.fill_slot_data()["location_enemies"]
        held = {f"{e.map}:{e.entity}" for e in ENCOUNTERS if e.area in KEY_CHAIN_AREAS}
        self.assertTrue(held)
        self.assertEqual(len(enemies), len(ENEMY_LOCATIONS) - len(held))
        self.assertEqual(set(enemies.values()), {f"{e.map}:{e.entity}" for e in ENCOUNTERS} - held)

    def test_named_by_room_and_enemy(self) -> None:
        # The room as the map's own spots call it; numbered per enemy within the room (the user's pattern).
        names = {loc.name for loc in ENEMY_LOCATIONS}
        self.assertIn("Outskirts: Golden Path, Seedling 1", names)
        self.assertIn("Outskirts: Golden Path, Flying Seedling 2", names)

    def test_ids_fixed_and_apart(self) -> None:
        # From the data, never reused, clear of the hand-written spots.
        self.assertTrue(all(loc.id >= 1000 for loc in ENEMY_LOCATIONS))
        self.assertEqual(len({loc.id for loc in ENEMY_LOCATIONS}), len(ENEMY_LOCATIONS))
