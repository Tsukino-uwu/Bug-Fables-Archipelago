# Room checklist

Every room in the game, to map for the logic one by one, top to bottom. Once every box is ticked, this file is kept
as it is, a frozen record.

- **Tick a room** when it's mapped as [`room-logic.md`](room-logic.md#how-a-room-gets-mapped)
  describes: drafted from the data, seen on screen, written into the logic, tested.
- **Everything starts unchecked,** rooms with logic already written included: every room is checked from scratch.
- **A room ticked with "for the quest pass"** is mapped except for a quest or chain in it, gone through after every
  room (`room-logic.md`, "How a room gets mapped"): those notes are the quest pass's list.
- **Vanilla only:** what the vanilla game expects (`room-logic.md`, rule 2), with every field ability considered and
  every way checked without Jump too.
- Rooms are the game's own maps (`MainManager.Maps`, its number in brackets), grouped by game area
  (`MapControl.areaid`) in the story order the logic modules use; night and story variants are maps of their own.

**98 of 244 done.**

## Outskirts

- [x] NearSnakemouth (1)
- [x] OutsideSnakemouth (2)
- [x] BugariaOutskirtsOutsideCity (16)
- [x] BugariaOutskitsSnakemouthCorridor1 (17)
- [x] BugariaOutskirtsSnakemouthCorridor2 (18)
- [x] ChucksAbode (27) — for the quest pass: Chuck's quest
- [x] GoldenPathTunnel (35)
- [x] BOGoldenPath (36)
- [x] BugariaPier (54) — for the quest pass: the quest board (on the dock, Jump)
- [x] BugariaOutskirtsEast1 (55)
- [x] BugariaOutskirtsEast2 (56)
- [x] BOLostSandsEntrance (57)
- [x] Blank (115) — scene-only (Event111), never a start
- [x] BugariaAssociationAttack (130) — story-only, out of the shuffle
- [x] SeedlingHaven (136) — for the quest pass: the Seedling King bounty and its Crystal Fruit
- [x] CaveOfTrials (185) — for the quest pass: the altar (the Mysterious Piece), the trials and their two items
- [x] GoldenPathTunnel2 (200)
- [x] HermitCave (227) — for the quest pass: the hermit's quest (54, started by talking, no board)

## Snakemouth Den

- [x] SnakemouthBridgeRoom (11)
- [x] SnakemouthDoorRoom (12)
- [x] SnakemouthFallRoom (13)
- [x] SnakemouthLake (14) — for the quest pass: the ladybug kid (location 10)
- [x] SnakemouthUndergrondDoor (19)
- [x] SnakemouthMushroomPit (20)
- [x] SnakemouthTreasureRoom (21)
- [x] SnakemouthUndergroundRightA (22)
- [x] SnakemouthUndergroundRightB (23)
- [x] SnakemouthUndergroundLeftA (24)
- [x] SnakemouthUndergroundLeftB (25)
- [x] SnakemouthTop (184) — for the quest pass: the Sophie Petal (Doctor Isau's request, `DefiantRoot1`), a
  location and a progression item
- [x] UpperSnekEntrance (208)

## Bugaria City

- [x] AntTunnels (3) — for the quest pass: its NPCs, if any give a quest
- [x] BugariaMainPlaza (9) — for the quest pass: the quest board
- [x] BugariaCommercial (10) — for the quest pass: the Lore Book dig spot (flag 388); for the discovery sweep:
  the Termacade (discovery 42, the greeter's first talk, needing nothing)
- [x] BugariaTheater (26) — for the quest pass: Chubee's play (the stage takes Jump; 30 berries, line 63)
- [x] BugariaResidential (28) — for the quest pass: the Old Book delivery, the moth house (flag 130)
- [x] UndergroundBar (30) — for the discovery sweep: the one recorded on entering (Event80); Shades' shop waits
  for all 50 crystal berries as locations (build step 11)
- [x] AntPalace1 (31)
- [x] AntPalace2 (32) — for the quest pass: crystal berries #5 (line 15) and #12 (line 48, after flag 130), story
  rewards, the guard and the queen up two ledges (Jump); for the discovery sweep: its four discoveries
- [x] AntBridge (33) — for the quest pass: Maki's and Kina's quest; for the discovery sweep: discovery 11 (line 1)
- [x] AntPalaceLibrary (34) — for the quest pass: the librarian's turn-ins (Lore Books, crystal berry #25 at line
  27; Bad Books, 35 berries each); for the discovery sweep: the portrait and the librarian's
- [x] AntPalaceWarRoom (37) — for the quest pass: the old ant (flags 701, 709)
- [x] AntMinesBreakRoom (73)
- [x] BugariaPlazaAttack (123)
- [x] BugariaBridgeAttack (124)
- [x] BugariaCastleAttack (125)
- [x] BugariaEndPlaza (240)
- [x] BugariaEndBridge (241)
- [x] BugariaEndThrone (242)

## Lost Sands

- [x] DesertEntrance (4)
- [x] DesertBadlands (5) — for the quest pass: the Rusty Key's sale (DefiantRootWell line 3), the hideout door's
  stand-in
- [x] DesertBookArea (6)
- [x] DesertRockFormation (7) — for the discovery sweep: the Tardigrade Idol (31), on taking the medal
- [x] DesertTrenchSouth (8)
- [x] DesertDREastEntrance (76)
- [x] DesertFGBorder (77)
- [x] DesertDRSouthEntrance (78) — for the quest pass: every caravan shop, checked with the quests (the user)
- [x] DesertBadgeAlcove (79)
- [x] DesertCaravanMap (80) — for the enemy pass: the caravan robbery scene (Event93, until flag 201), its fight
  needing logic once bosses and enemies are shuffled
- [x] DesertSandPitArea (81)
- [x] DesertBeforeGH (82)
- [x] DesertRoachVillage (95) — for the quest pass: the hawk (flags 300-302), key item 105 (line 1)
- [x] DesertOasis (96) — for the quest pass: Stratos and Delilah (until flag 496)
- [x] DesertOasisEntrance (97)
- [x] DesertWestDunes (98)
- [x] DesertSandCastle (107) — for the quest pass: the Sand Castle Key chain (the Heaven and Earth Keys, Event109)
- [x] DesertMountain (108)
- [x] DesertTrenchMiddle (109)
- [x] DesertJumpPuzzle (110)
- [x] DesertSouthern (111)
- [x] DesertScorpion (112) — for the enemy pass: the Scorpion fight (Event111, flags 303 to 298)
- [x] DesertEastmost (113)

## Golden Hills

- [x] GoldenHillsDungeonEntrance (45) — for the enemy pass: the Mothiva and Zasp fight (Event67, on placing the Big Crank)
- [x] GoldenHillsDungeonLeftMain (46)
- [x] GoldenHillsDungeonCrankLeft (47) — its crank spot added with the Wooden Crank step (Next 62)
- [x] GoldenHillsDungeonRightCrank (48) — for the quest pass: the butler (Butler Missing!, Event103); its crank half with Next 62
- [x] GoldenHillsLowerRightCrank (49) — its crank spot with Next 62; for the enemy pass: the Chomper, Beemerang Halt alone
- [x] GoldenHillsDungeonLeftCrankHalf (50) — its crank half with Next 62 (Vi for the Venus Buds)
- [x] GoldenHillsDungeonUpperMain (51) — for the quest pass: the mole cricket (flags 130 to 577), the offerings' chain
- [x] GoldenHillsDungeonUpperSide (52) — its crank spot with Next 62
- [x] GoldenHillsDungeonBoss (53) — for the enemy pass: the Venus' Guardian fight (Event73, Jump up to it; its needs untested)
- [x] GoldenPitcher1 (203)
- [x] GoldenPitcher2 (205)
- [x] PitcherPlantArena (239) — for the quest pass: the pitcher's bounty fight (Event124, flag 494), with the other bounties: it gives the Crystal Fang; what the fight needs, tested then (the user)

## Golden Path

- [x] GoldenHillsCableCar (29) — for the quest pass: the CableCar quest (its NPC, Event91, the cranks), then the Super Block+ medal (flag 534, Jump)
- [x] GoldenHillsPath2 (38) — for the quest pass: the sleepy NPC (the CableCar quest, until flag 182)
- [x] GoldenSettlementEntrance (39) — for the quest pass: the horn quest and Tanjerin (the minigame door's rock, flags 274-275); the caravan's other stalls, one at a time (the user)
- [x] GoldenHillsPath3 (44)
- [x] GoldenSMinigame (114) — for the quest pass: the mayor's late visit (20 worms, the Desert Key, quest 46, flags 557-559)

## Golden Settlement

- [x] GoldenSettlement1 (40) — for the quest pass: Aria's offering (Queen's Dinner, flag 393) and the festival chain, the
  quest board (from 86), Samira, the night's talkers, the Mothiva Doll's trade (Defiant Root); for the enemy pass: the
  festival fight (Event58); for the discovery sweep: the balcony (Event53) and The Golden Festival (014); for the
  sellers' pass: the mosquito girl's Berry Juice and the moth merchant's night goods; Kut the chef free day and night
- [x] GoldenSettlement1Night (41) — the square at night: the same room (build step 52), mapped with it
- [x] GoldenSettlement2 (42) — for the quest pass: the farmer's board quest (quest 50, three Clear Waters, Heavy
  Sleeper), the tough bee's Red Paint for Root Cloth (flag 444), Bomby's hat (flag 309, after the festival); for the
  discovery sweep: discovery 12 (the aphid girl, needing nothing)
- [x] GoldenSettlement2Night (43) — the farm at night: the same room (build step 52), mapped with it
- [x] GoldenSettlement3 (65) — for the quest pass: Tanjerin's horn quest (flags 272-274), Kenny's Lore Book (flags 602,
  409, 603), the card master's duels (Event106, 10 berries); for the sellers' pass: Jayde's stew (later chapter)
- [x] GoldenSettlement3Night (66) — the houses at night: the same room (build step 52), mapped with it
- [x] PowerPlant (197) — for the quest pass: the Power Plant board quest (31, flags 225-226), its workers and guard

## Forsaken Lands

- [x] BarrenLandsEntrance (149) — for the quest pass: Patton's meeting (Event159); for the sellers' pass: his stat and
  potion conversions
- [x] BarrenLandsCD (150)
- [ ] TermiteOutside (173)
- [ ] BarrenLandsBeefly (180)
- [ ] BarrenLandsAntTunnel (181)
- [ ] BarrenLandsMiniboss (182)
- [ ] BarrenLandsPinkSpider (189)
- [ ] BarrenLandsTanks (190)
- [ ] BarrenLandsMushrooms (191)
- [ ] AbandonedCity (192)
- [ ] BarrenLandsPumpkins (193)
- [ ] BarrenLandsCloud (194)
- [ ] BarrenLandsRock (195)
- [ ] AbandonedCityTent (196)
- [ ] BarrenLandsSideGPT (199)

## Far Grasslands

- [ ] FGCave (135)
- [ ] FarGrasslands1 (137)
- [ ] FarGrasslandsOutsideCave (138)
- [ ] FarGrasslandsWizard (139)
- [ ] FarGrasslands2 (140)
- [ ] FarGrasslandsLake (141)
- [ ] FarGrasslandsOutsideVillage (142)
- [ ] FarGrasslands3 (143)
- [ ] FGOutsideSwamplands (146)
- [ ] FarGrasslands4 (153)
- [ ] WizardTowerBasement (186)
- [ ] WizardTowerStairs (187)
- [ ] WizardTowerAttic (188)
- [ ] BroodmotherLair (198)
- [ ] FGClearing (201)

## Wild Swamplands

- [ ] SwamplandsEntrance (145)
- [ ] Swamplands2 (148)
- [ ] Swamplands3 (151)
- [ ] SwamplandsBridge (152)
- [ ] SwamplandsBoss (154)
- [ ] Swamplands4 (158)
- [ ] Swamplands5 (159)
- [ ] Swamplands6 (160)
- [ ] Swamplands7 (161)
- [ ] Swamplands8 (162)

## Defiant Root

- [ ] DefiantRoot1 (58)
- [ ] DefiantRootWell (59)
- [ ] DefiantRoot2 (60)
- [ ] DefiantRoot3 (61)

## Ancient Castle

- [ ] SandCastleEntrance (116)
- [ ] SandCastleSlidePuzzle (117)
- [ ] SandCastleStatueRoom (118)
- [ ] SandCastleBasement (119)
- [ ] SandCastleRoof (120)
- [ ] SandCastleMainRoom (121)
- [ ] SandCastleBossKeyRoom (122)
- [ ] SandCastlePressurePuzzle (126)
- [ ] SandCastleRockRoom (127)
- [ ] SandCastleBossRoom (128)
- [ ] SandCastleTreasureRoom (129)

## Bee Kingdom Hive

- [ ] BeehiveOutside (62)
- [ ] BeehiveThroneRoom (63)
- [ ] BeehiveScannerRoom (64)
- [ ] BeehiveMainArea (67)
- [ ] HBsLab (68)
- [ ] BeehiveBalcony (69)
- [ ] HoneycombsLab (70)
- [ ] JaunesGallery (71)

## Honey Factory

- [ ] HoneyFactoryEntrance (72)
- [ ] HoneyFactoryWorkerRooms (74)
- [ ] HoneyFactoryCore (75)
- [ ] FactoryProcessingFirstRoom (83)
- [ ] FactoryProcessing2 (84)
- [ ] FactoryProcessingPump (85)
- [ ] FactoryProcessingPuzzle1 (86)
- [ ] FactoryProcessingPuzzle2 (87)
- [ ] FactoryProcessingPuzzle3 (88)
- [ ] FactoryProcessingMalbee (89)
- [ ] FactoryStorageMaze (90)
- [ ] FactoryStorageElevator (91)
- [ ] FactoryStorageMiniboss (92)
- [ ] FactoryStorageOverseer (93)

## Rubber Prison

- [ ] RubberPrisonPier (218)
- [ ] RubberPrisonCheckpointCorridor (219)
- [ ] RubberPrisonSpikeRoom (220)
- [ ] RubberPrisonCells1 (221)
- [ ] RubberPrisonCells2 (222)
- [ ] RubberPrisonLibrary (223)
- [ ] RubberPrisonCafeteria (224)
- [ ] RubberPrisonGym (225)
- [ ] RubberPrisonSecurity (226)
- [ ] RubberPrisonOffice (229)
- [ ] RubberPrisonThirdFloor (230)
- [ ] RubberPrisonGiantLairBridge (231)

## Giant's Lair

- [ ] GiantLairEntrance (232)
- [ ] GiantLairDeadLands1 (233)
- [ ] GiantLairDeadLands2 (234)
- [ ] GiantLairFridgeOutside (235)
- [ ] GiantLairFridgeInside (236)
- [ ] GiantLairRoachVillage (237)
- [ ] GiantLairSaplingPlains (238)
- [ ] GiantLairBeforeBoss (244)
- [ ] GiantLairBeforeBoss2 (245)

## Metal Lake

- [ ] MetalLake (183)
- [ ] MysteryIsland (206)
- [ ] MysteryIslandInside (207)

## Metal Island

- [ ] MetalIsland1 (94)
- [ ] MetalIsland2 (131)
- [ ] MetalIslandAuditorium (228)

## Termite Capitol

- [ ] TermiteMainPlaza (174)
- [ ] TermiteRoyalChamber (175)
- [ ] TermiteIndustrial (176)
- [ ] TermitePier (177)
- [ ] TermiteColiseum1 (178)
- [ ] TermiteColiseum2 (179)

## Wasp Kingdom Hive

- [ ] WaspKingdomOutside (147)
- [ ] WaspKingdom1 (163)
- [ ] WaspKingdom2 (164)
- [ ] WaspKingdom3 (165)
- [ ] WaspKingdom4 (166)
- [ ] WaspKingdom5 (167)
- [ ] WaspKingdomPrison (168)
- [ ] WaspKingdomJayde (169)
- [ ] WaspKingdomMainHall (170)
- [ ] WaspKingdomThrone (171)
- [ ] WaspKingdomQueen (172)
- [ ] WaspKingdomDrillRoom (243)

## Bandit Hideout

- [ ] HideoutEntrance (99)
- [ ] HideoutCell (100)
- [ ] HideoutCentralRoom (101)
- [ ] HideoutLeftA (102)
- [ ] HideoutStairsRoom (103)
- [ ] HideoutGarden (104)
- [ ] HideoutWestStorage (105)
- [ ] HideoutRightA (106)

## Stream Mountain

- [ ] StreamMountain1 (132)
- [ ] StreamMountain2 (133)
- [ ] StreamMountain3 (134)
- [ ] StreamMountain4 (202)
- [ ] StreamMountain5 (204)

## Chomper Caves

- [ ] ChomperCave1 (155)
- [ ] ChomperCaves2 (156)
- [ ] ChomperCaves3 (157)

## Fishing Village

- [ ] FishingVillage (144)

## Upper Snakemouth

- [ ] UpperSnekTransition (209)
- [ ] UpperSnekSwitchPuzzle (210)
- [ ] UpperSnekBeforeBoss (211)
- [ ] UpperSnekPressurePlateRoom (212)
- [ ] UpperSnekBossRoom (213)
- [ ] UpperSnekMiddleRoom (214)
- [ ] UpperSnekPlatformRoom (215)
- [ ] UpperSnekRiverPuzzle (216)
- [ ] UpperSnekGeizerRoom (217)

## Not rooms

Never part of anything (`UNUSED_MAPS`, `data_tables.py`): `TestRoom` (0), `SnakemouthEmpty` (15).
