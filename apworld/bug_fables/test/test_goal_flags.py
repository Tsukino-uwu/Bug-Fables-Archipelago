from . import BugFablesTestBase
from ..data_tables import ACTIVATION_FLAGS, ARTIFACT_WRITERS, ARTIFACTS, DIALOGUE_FLAGS, LIMIT_FLAGS


class TestGoalFlags(BugFablesTestBase):
    # The client counts only the goal's flags, and keeps one on only when one of its own events set it (build step 60):
    # a seed's goal can't be reached by an artifact the logic doesn't hold, nor by anything borrowing its flag.
    def test_goal_flags_are_the_artifacts_with_their_events(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["goal_flags"], [{"flag": 41, "events": [26]}])

    def test_each_artifact_set_by_its_own_event(self) -> None:
        for artifact in ARTIFACTS:
            self.assertIn(artifact.source.event, ARTIFACT_WRITERS[artifact.source.flag], artifact.name)

    def test_no_entity_repointed_to_an_artifact_flag(self) -> None:
        for swap in (*ACTIVATION_FLAGS, *LIMIT_FLAGS, *DIALOGUE_FLAGS):
            self.assertNotIn(swap.to, ARTIFACT_WRITERS, swap)

    # Cutting it set flag 41 and sent the goal (seen 2026-10-08); its drop also carried 41, so location 182 never matched.
    def test_swamp_grass_sets_nothing(self) -> None:
        swaps = self.world.fill_slot_data()["activation_flags"]
        self.assertIn({"map": "Swamplands2", "entity": "blockgrass", "flag": 41, "to": -1}, swaps)
        self.assertIn({"map": "Swamplands2", "entity": "funGrass - Duplicate - Duplicate", "flag": 41, "to": -1}, swaps)

    # In vanilla flag 41 is set before the book room: its hidden switch stays hit, and the gate on it never exists.
    def test_book_room_gate_never_exists(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "DesertBookArea", "entity": "switch", "flag": 41, "to": 691}, data["activation_flags"])
        self.assertIn({"map": "DesertBookArea", "entity": "eventtrigger", "flag": 41, "to": 691}, data["limit_flags"])
