# Room checklist

Every room in the game, to map for the logic one by one, top to bottom. Once every box is ticked, this file is kept
as it is, a frozen record.

- **Tick a room** when it's mapped as [`room-logic.md`](room-logic.md#how-a-room-gets-mapped)
  describes: drafted from the data, seen on screen, written into the logic, tested.
- **Everything starts unchecked,** rooms with logic already written included: every room is checked from scratch.
- **Vanilla only:** what the vanilla game expects (`room-logic.md`, rule 2), with every field ability considered and
  every way checked without Jump too.
- Rooms are the game's own maps (`MainManager.Maps`, its number in brackets), grouped by game area
  (`MapControl.areaid`) in the story order the logic modules use; night and story variants are maps of their own.

**13 of 244 done.**

## Outskirts

- [x] NearSnakemouth (1)
- [x] OutsideSnakemouth (2)
- [x] BugariaOutskirtsOutsideCity (16)
- [x] BugariaOutskitsSnakemouthCorridor1 (17)
- [x] BugariaOutskirtsSnakemouthCorridor2 (18)
- [x] ChucksAbode (27)
- [x] GoldenPathTunnel (35)
- [x] BOGoldenPath (36)
- [x] BugariaPier (54)
- [x] BugariaOutskirtsEast1 (55)
- [x] BugariaOutskirtsEast2 (56)
- [x] BOLostSandsEntrance (57)
- [x] Blank (115) — scene-only (Event111), never a start
- [ ] BugariaAssociationAttack (130)
- [ ] SeedlingHaven (136)
- [ ] CaveOfTrials (185)
- [ ] GoldenPathTunnel2 (200)
- [ ] HermitCave (227)

## Snakemouth Den

- [ ] SnakemouthBridgeRoom (11)
- [ ] SnakemouthDoorRoom (12)
- [ ] SnakemouthFallRoom (13)
- [ ] SnakemouthLake (14)
- [ ] SnakemouthUndergrondDoor (19)
- [ ] SnakemouthMushroomPit (20)
- [ ] SnakemouthTreasureRoom (21)
- [ ] SnakemouthUndergroundRightA (22)
- [ ] SnakemouthUndergroundRightB (23)
- [ ] SnakemouthUndergroundLeftA (24)
- [ ] SnakemouthUndergroundLeftB (25)
- [ ] SnakemouthTop (184)
- [ ] UpperSnekEntrance (208)

## Bugaria City

- [ ] AntTunnels (3)
- [ ] BugariaMainPlaza (9)
- [ ] BugariaCommercial (10)
- [ ] BugariaTheater (26)
- [ ] BugariaResidential (28)
- [ ] UndergroundBar (30)
- [ ] AntPalace1 (31)
- [ ] AntPalace2 (32)
- [ ] AntBridge (33)
- [ ] AntPalaceLibrary (34)
- [ ] AntPalaceWarRoom (37)
- [ ] AntMinesBreakRoom (73)
- [ ] BugariaPlazaAttack (123)
- [ ] BugariaBridgeAttack (124)
- [ ] BugariaCastleAttack (125)
- [ ] BugariaEndPlaza (240)
- [ ] BugariaEndBridge (241)
- [ ] BugariaEndThrone (242)

## Lost Sands

- [ ] DesertEntrance (4)
- [ ] DesertBadlands (5)
- [ ] DesertBookArea (6)
- [ ] DesertRockFormation (7)
- [ ] DesertTrenchSouth (8)
- [ ] DesertDREastEntrance (76)
- [ ] DesertFGBorder (77)
- [ ] DesertDRSouthEntrance (78)
- [ ] DesertBadgeAlcove (79)
- [ ] DesertCaravanMap (80)
- [ ] DesertSandPitArea (81)
- [ ] DesertBeforeGH (82)
- [ ] DesertRoachVillage (95)
- [ ] DesertOasis (96)
- [ ] DesertOasisEntrance (97)
- [ ] DesertWestDunes (98)
- [ ] DesertSandCastle (107)
- [ ] DesertMountain (108)
- [ ] DesertTrenchMiddle (109)
- [ ] DesertJumpPuzzle (110)
- [ ] DesertSouthern (111)
- [ ] DesertScorpion (112)
- [ ] DesertEastmost (113)

## Golden Hills

- [ ] GoldenHillsDungeonEntrance (45)
- [ ] GoldenHillsDungeonLeftMain (46)
- [ ] GoldenHillsDungeonCrankLeft (47)
- [ ] GoldenHillsDungeonRightCrank (48)
- [ ] GoldenHillsLowerRightCrank (49)
- [ ] GoldenHillsDungeonLeftCrankHalf (50)
- [ ] GoldenHillsDungeonUpperMain (51)
- [ ] GoldenHillsDungeonUpperSide (52)
- [ ] GoldenHillsDungeonBoss (53)
- [ ] GoldenPitcher1 (203)
- [ ] GoldenPitcher2 (205)
- [ ] PitcherPlantArena (239)

## Golden Path

- [ ] GoldenHillsCableCar (29)
- [ ] GoldenHillsPath2 (38)
- [ ] GoldenSettlementEntrance (39)
- [ ] GoldenHillsPath3 (44)
- [ ] GoldenSMinigame (114)

## Golden Settlement

- [ ] GoldenSettlement1 (40)
- [ ] GoldenSettlement1Night (41)
- [ ] GoldenSettlement2 (42)
- [ ] GoldenSettlement2Night (43)
- [ ] GoldenSettlement3 (65)
- [ ] GoldenSettlement3Night (66)
- [ ] PowerPlant (197)

## Forsaken Lands

- [ ] BarrenLandsEntrance (149)
- [ ] BarrenLandsCD (150)
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
