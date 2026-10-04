from unittest import TestCase

from . import BugFablesTestBase
from ..options import BugFablesOptions
from ..slot_data import NOT_SENT, SLOT_OPTIONS


class TestOptionsSent(TestCase):
    # A new option must be sent in "options" or say why not: Universal Tracker regenerates the seed from what's sent.
    def test_every_option_is_sent_or_has_a_reason(self) -> None:
        self.assertFalse(set(SLOT_OPTIONS) & set(NOT_SENT))
        self.assertEqual(set(SLOT_OPTIONS) | set(NOT_SENT), set(BugFablesOptions.type_hints))
        self.assertEqual(len(SLOT_OPTIONS), len(set(SLOT_OPTIONS)))


class TestOptionsInSlotData(BugFablesTestBase):
    options = {"exclude_locations": ["Outskirts: Pier, Ship's Wheel", "Outskirts: Pier, Behind the Dock"]}

    def test_options_as_json_values(self) -> None:
        # The mod reads toggles as JSON booleans, choices and ranges as numbers, location sets as sorted lists.
        options = self.world.fill_slot_data()["options"]
        self.assertEqual(list(options), list(SLOT_OPTIONS))
        self.assertIs(options["shuffle_quests"], True)
        self.assertIs(options["shuffle_discoveries"], False)
        self.assertIs(options["progressive_boat"], True)
        self.assertEqual(options["shop_contents"], 1)
        self.assertEqual(options["entrance_randomizer"], 0)
        self.assertEqual(options["exclude_locations"],
                         ["Outskirts: Pier, Behind the Dock", "Outskirts: Pier, Ship's Wheel"])

    def test_option_copies_are_gone(self) -> None:
        data = self.world.fill_slot_data()
        for key in ("artifacts_required", "shuffle_moves", "shuffle_jump", "points_of_no_return"):
            self.assertNotIn(key, data)


class TestSlotData(BugFablesTestBase):
    # A missing location could never be sent; a wrong flag would send the wrong check.
    def test_every_location_has_its_flag(self) -> None:
        data = self.world.fill_slot_data()
        flags, variables, berries = data["location_flags"], data["location_vars"], data["location_berries"]
        discoveries = data["location_discoveries"]
        shops = data["location_shops"]
        item_shops = data["location_item_shops"]
        respawns = {loc for loc, pickup in data["location_pickups"].items() if "regional" in pickup}
        ids = {str(loc.address) for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        self.assertEqual(set(flags) | set(variables) | set(berries) | set(discoveries) | set(shops) | set(item_shops)
                         | respawns, ids)
        self.assertEqual(len(flags) + len(variables) + len(berries) + len(discoveries) + len(shops) + len(item_shops)
                         + len(respawns),
                         len(ids))
        self.assertEqual(flags[str(self.world.location_name_to_id["Outskirts: Maki and Eetl's Gift"])], 15)
        self.assertEqual(flags[str(self.world.location_name_to_id["Outskirts: Artis's Gift"])], 32)

    def test_world_version_is_the_manifest_one(self) -> None:
        import json
        import pkgutil
        manifest = json.loads(pkgutil.get_data("worlds.bug_fables", "archipelago.json").decode("utf-8"))
        self.assertEqual(self.world.fill_slot_data()["world_version"], manifest["world_version"])

    def test_gives_name_the_vanilla_item(self) -> None:
        # A wrong giveitem would let the vanilla item through or swallow an unrelated grant.
        gives = self.world.fill_slot_data()["location_gives"]
        medal = gives[str(self.world.location_name_to_id["Outskirts: Artis's Gift"])]
        self.assertEqual(medal, {"map": "BugariaOutskirtsOutsideCity", "type": 2, "item": 11})
        permit = gives[str(self.world.location_name_to_id["Outskirts: Maki and Eetl's Gift"])]
        self.assertEqual(permit, {"map": "BugariaOutskirtsOutsideCity", "type": 1, "item": 27})

    def test_item_kinds_cover_every_item(self) -> None:
        kinds = self.world.fill_slot_data()["item_kinds"]
        self.assertEqual(set(kinds), {str(i) for i in self.world.item_name_to_id.values()})
        self.assertEqual(kinds[str(self.world.item_name_to_id["Explorer Permit"])], 1)


class TestPickups(BugFablesTestBase):
    # A missing pickup entry gives the vanilla item; a wrong flag swaps an unrelated pickup.
    def test_pickups_are_in_slot_data(self) -> None:
        pickups = self.world.fill_slot_data()["location_pickups"]
        medal = str(self.world.location_name_to_id["Snakemouth Den: Underground Door Room, Behind the Wall"])
        self.assertEqual(pickups[medal], {"map": "SnakemouthUndergrondDoor", "flag": 60})

    def test_pickups_are_not_gives(self) -> None:
        data = self.world.fill_slot_data()
        self.assertFalse(set(data["location_pickups"]) & set(data["location_gives"]))

    def test_pickup_flag_is_its_location_flag(self) -> None:
        data = self.world.fill_slot_data()
        for location, pickup in data["location_pickups"].items():
            if "berry" in pickup or "regional" in pickup:
                continue  # a crystal berry is known by its index, a respawning pickup by its regional flag
            self.assertEqual(data["location_flags"][location], pickup["flag"])


class TestStoryPickup(BugFablesTestBase):
    # A story pickup has no flag: the client knows it by the event it starts.
    def test_story_pickup_known_by_its_event(self) -> None:
        pickups = self.world.fill_slot_data()["location_pickups"]
        trapdoor = str(self.world.location_name_to_id["Snakemouth Den: Door Room, Trapdoor"])
        self.assertEqual(pickups[trapdoor], {"map": "SnakemouthDoorRoom", "flag": 14, "event": 5})

    def test_ordinary_pickups_have_no_event(self) -> None:
        pickups = self.world.fill_slot_data()["location_pickups"]
        medal = str(self.world.location_name_to_id["Snakemouth Den: Underground Door Room, Behind the Wall"])
        self.assertNotIn("event", pickups[medal])


class TestKeptOpen(BugFablesTestBase):
    # The logic assumes Snakemouth Den stays reachable, so Eetl's blocker must be kept away.
    def test_eetls_blocker_is_kept_open(self) -> None:
        kept = self.world.fill_slot_data()["kept_open"]
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "eetlblocker1 - Duplicate"}, kept)
        # The same scene's second trigger, standing while Eetl follows the party.
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "eetlblocker1"}, kept)

    def test_save_tutorial_trigger_is_kept_away(self) -> None:
        # Its actors leave at the first boss but its trigger stays until the scene's own flag, so it played to no one.
        kept = self.world.fill_slot_data()["kept_open"]
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "SaveEventTrigger"}, kept)

    def test_plaza_discoveries_open_before_the_briefing(self) -> None:
        # The statue and inn portrait exist only from flag 67; before it a stand-in turns the player away.
        slot = self.world.fill_slot_data()
        for entity in ("Discovery Pre Briefing", "Discovery Pre Briefing - Duplicate"):
            self.assertIn({"map": "BugariaMainPlaza", "entity": entity}, slot["kept_open"])
        for entity in ("StatueDesc", "InnPortrait"):
            self.assertIn({"map": "BugariaMainPlaza", "entity": entity}, slot["kept_present"])

    def test_follower_swap_waits_for_the_first_follower(self) -> None:
        # Crossed first, the bridge scene would leave the follower from after the first boss stuck for good.
        held = self.world.fill_slot_data()["held_until"]
        self.assertIn({"map": "AntBridge", "entity": "makiautoevent", "flag": 114}, held)
        self.assertIn({"map": "AntPalace1", "entity": "Chapter1StartEvent", "flag": 66}, held)

    def test_inn_open_before_the_briefing(self) -> None:
        flags = self.world.fill_slot_data()["dialogue_flags"]
        self.assertIn({"map": "BugariaMainPlaza", "entity": "Innkeeper", "flag": 67, "to": 691}, flags)


class TestRespawningPickups(BugFablesTestBase):
    # A respawning pickup has no flag: without "regional" the client would never recognise it.
    def test_known_by_regional_flag(self) -> None:
        data = self.world.fill_slot_data()
        spot = str(self.world.location_name_to_id["Snakemouth Den: Underground Bridge Room, Behind Pillar"])
        self.assertEqual(data["location_pickups"][spot],
                         {"map": "SnakemouthUndergroundRightB", "flag": -1, "regional": 28})
        self.assertNotIn(spot, data["location_flags"])

    def test_vanilla_item_in_pool(self) -> None:
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Honey Drop", pool)

    def test_needs_the_underground(self) -> None:
        self.assertFalse(self.can_reach_location("Snakemouth Den: Underground Bridge Room, Behind Pillar"))
        self.collect_by_name(["Explorer Permit", "Leif"])
        self.assertTrue(self.can_reach_location("Snakemouth Den: Underground Bridge Room, Behind Pillar"))


class TestKeptPresent(BugFablesTestBase):
    # The trapdoor must never be a dead end; the Peculiar Gem slot, not the big door, is the real gate.
    def test_ways_back_are_present(self) -> None:
        present = self.world.fill_slot_data()["kept_present"]
        self.assertIn({"map": "SnakemouthFallRoom", "entity": "JumpShroom"}, present)
        self.assertIn({"map": "SnakemouthFallRoom", "entity": "LoadingZoneDoorRoom"}, present)
        self.assertIn({"map": "SnakemouthDoorRoom", "entity": "DoorLoadZone"}, present)

    def test_way_back_down_from_the_trapdoor(self) -> None:
        # The door back down exists from the trapdoor (flag 14), or the kept mushroom strands the party.
        self.assertIn({"map": "SnakemouthDoorRoom", "entity": "LoadZoneFallRoom", "flag": 14},
                      self.world.fill_slot_data()["present_from"])

    def test_fall_room_blocker_is_kept_open(self) -> None:
        self.assertIn({"map": "SnakemouthFallRoom", "entity": "blocker"}, self.world.fill_slot_data()["kept_open"])

    def test_near_snakemouth_exits_open_before_the_boss(self) -> None:
        data = self.world.fill_slot_data()
        for trigger, door in (("BlockLeft", "loadingzonechuck"), ("BlockRight", "loadingzonefields")):
            with self.subTest(door=door):
                self.assertIn({"map": "NearSnakemouth", "entity": trigger}, data["kept_open"])
                self.assertIn({"map": "NearSnakemouth", "entity": door}, data["kept_present"])


class TestOutskirtsRocks(BugFablesTestBase):
    # The Outskirts rocks go from the start, so the town opens without its early scenes (arrival, plaza blockers).
    def test_rocks_are_removed(self) -> None:
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Base/BlockingRocks"},
                      self.world.fill_slot_data()["scenery_hidden"])

    def test_town_open_from_the_start(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "DoorBugaria - Duplicate"}, data["kept_open"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "DoorBugaria"}, data["kept_present"])
        self.assertIn({"map": "AntPalace1", "entity": "Chapter1StartEvent", "flag": 66}, data["held_until"])

    def test_plaza_blockers_removed(self) -> None:
        kept = self.world.fill_slot_data()["kept_open"]
        for entity in ("MM", "blockereetl2", "blockereetl2 - Duplicate"):
            with self.subTest(entity=entity):
                self.assertIn({"map": "BugariaMainPlaza", "entity": entity}, kept)
        data = self.world.fill_slot_data()
        for entity in ("LoadingZoneCommercial", "LoadingZoneResidential", "loadingzone theater"):
            with self.subTest(entity=entity):
                self.assertIn({"map": "BugariaMainPlaza", "entity": entity}, data["kept_present"])
        self.assertIn({"map": "BugariaMainPlaza", "entity": "Cube"}, data["scenery_hidden"])

    def test_boat_sailor_always_there(self) -> None:
        # The boat scene ran with Leif alone once stand-ins filled the missing seats, so the sailor no longer waits.
        held = self.world.fill_slot_data()["held_until"]
        self.assertFalse(any(e["entity"] == "boatsailor" for e in held), held)


class TestBarAndBoards(BugFablesTestBase):
    # The bar's entrance line answers to flag 691, set on every new game, instead of story flag 135.
    def test_bar_entrance_repointed(self) -> None:
        self.assertIn({"map": "BugariaCommercial", "entity": "HideoutEntrance", "flag": 135, "to": 691},
                      self.world.fill_slot_data()["dialogue_flags"])

    def test_quest_boards_present(self) -> None:
        present = self.world.fill_slot_data()["kept_present"]
        self.assertIn({"map": "BugariaMainPlaza", "entity": "QuestBoard"}, present)
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "QuestBoard"}, present)


class TestCaravan(BugFablesTestBase):
    # The caravan is there from the start: keeper present (so its shop slots are built), stall shown.
    def test_caravan_open(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Crickerly2"}, data["kept_present"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Crickerly1"}, data["kept_open"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Base/Stall"}, data["scenery_present"])
        caravan = [e for e in data["location_item_shops"].values() if e["keeper"] == "Crickerly2"]
        self.assertEqual(sorted(e["item"] for e in caravan), [2, 3, 11])

    def test_no_rock_lines(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "FuzzyMoth"}, data["kept_open"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "CHusband", "flag": 41, "to": 691},
                      data["dialogue_flags"])
        for sibling in ("LaydbugGirl", "LaydbugBoy"):
            self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": sibling}, data["kept_present"])

    def test_reachable_from_the_start(self) -> None:
        self.assertTrue(self.can_reach_location("Outskirts: Caravan, Item Shop 1"))


class TestMadeleinesHouse(BugFablesTestBase):
    # The house is open from the start: door kept, lock and locked-door check removed.
    def test_house_opened(self) -> None:
        data = self.world.fill_slot_data()
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "doormadeleine"}, data["kept_present"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "lockeddoor"}, data["kept_open"])
        self.assertIn({"map": "BugariaOutskirtsOutsideCity", "entity": "Base/lock (1)"}, data["scenery_hidden"])

    def test_tutorial_leaf_is_a_location(self) -> None:
        # The opening's Crunchy Leaf would otherwise reach the bag from the game, not the server.
        data = self.world.fill_slot_data()
        leaf = str(self.world.location_name_to_id["Outskirts: Outside the City, Tutorial Battle"])
        self.assertEqual(data["location_added"], {leaf: {"type": 0, "item": 0}})
        self.assertEqual(data["location_flags"][leaf], 15)
        self.assertIn(int(leaf), data["silent_locations"])
        pool = [item.name for item in self.multiworld.itempool if item.player == self.player]
        self.assertIn("Crunchy Leaf", pool)

    def test_opening_checks_are_quiet(self) -> None:
        # A new file would otherwise start with a hold-up for each opening check.
        quiet = self.world.fill_slot_data()["quiet_locations"]
        names = ["Outskirts: Maki and Eetl's Gift", "Outskirts: Outside the City, Opening",
                 "Outskirts: Outside the City, Tutorial Battle"]
        self.assertEqual(quiet, sorted(self.world.location_name_to_id[name] for name in names))


class TestSettlementDesertGate(BugFablesTestBase):
    # The gate opens only from its switch, and an invisible wall stands behind it until the desert side is reached.
    def test_gate_open_and_wall_gone(self) -> None:
        data = self.world.fill_slot_data()
        for path in ("Base/Cube", "Base/DesertGate/WoodenGate2", "Base/DesertGate/WoodenGate2 (1)"):
            self.assertIn({"map": "GoldenSettlementEntrance", "entity": path}, data["scenery_hidden"])
        for path in ("Base/WoodenGate2 (2)", "Base/WoodenGate2 (3)"):
            self.assertIn({"map": "GoldenSettlementEntrance", "entity": path}, data["scenery_present"])


class TestFlowerKeySeller(BugFablesTestBase):
    # The game makes her only after chapter 3; the seed keeps her on the balcony from the start.
    def test_beette_present(self) -> None:
        self.assertIn({"map": "BeehiveBalcony", "entity": "smug bee"}, self.world.fill_slot_data()["kept_present"])
