from . import BugFablesTestBase

BARRIER_SCENERY = ({"map": "NearSnakemouth", "entity": "map1v4 (1)/snakemouthgate"},
                   {"map": "NearSnakemouth", "entity": "map1v4 (1)/snakemouthgate/Gate"})
BARRIER_NPCS = ({"map": "NearSnakemouth", "entity": "guard"}, {"map": "NearSnakemouth", "entity": "sign"})


class TestSnakemouthBarrierGone(BugFablesTestBase):
    # Taken out of Extra Roadblocks (the horn tutorial moves the party past it): a yaml still naming it puts nothing up.
    options = {"extra_roadblocks": ["Snakemouth Barrier"]}

    def test_pieces_kept_away(self) -> None:
        data = self.world.fill_slot_data()
        for piece in BARRIER_SCENERY:
            self.assertIn(piece, data["scenery_hidden"])
            self.assertNotIn(piece, data["scenery_present"])
        for npc in BARRIER_NPCS:
            self.assertIn(npc, data["kept_open"])
            self.assertNotIn(npc, data["kept_present"])

    def test_no_cave_side(self) -> None:
        regions = {region.name for region in self.multiworld.get_regions(self.player)}
        self.assertNotIn("NearSnakemouth (Cave Side)", regions)
