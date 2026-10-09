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

**149 of 244 done.**

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
- [x] BugariaResidential (28) — for the quest pass: the Old Book delivery, the moth house (flag 130); rechecked
  2026-10-08 (the fountain rooftop by Bee Fly too, the banker a new location)
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

- [x] GoldenHillsDungeonEntrance (45) — for the enemy pass: the Mothiva and Zasp fight (Event67, on placing the Big
  Crank)
- [x] GoldenHillsDungeonLeftMain (46)
- [x] GoldenHillsDungeonCrankLeft (47) — its crank spot added with the Wooden Crank step (Next 62)
- [x] GoldenHillsDungeonRightCrank (48) — for the quest pass: the butler (Butler Missing!, Event103); its crank half
  with Next 62
- [x] GoldenHillsLowerRightCrank (49) — its crank spot with Next 62; for the enemy pass: the Chomper, Beemerang Halt
  alone
- [x] GoldenHillsDungeonLeftCrankHalf (50) — its crank half with Next 62 (Vi for the Venus Buds)
- [x] GoldenHillsDungeonUpperMain (51) — for the quest pass: the mole cricket (flags 130 to 577), the offerings' chain
- [x] GoldenHillsDungeonUpperSide (52) — its crank spot with Next 62
- [x] GoldenHillsDungeonBoss (53) — for the enemy pass: the Venus' Guardian fight (Event73, Jump up to it; its needs
  untested)
- [x] GoldenPitcher1 (203)
- [x] GoldenPitcher2 (205)
- [x] PitcherPlantArena (239) — for the quest pass: the pitcher's bounty fight (Event124, flag 494), with the other
  bounties: it gives the Crystal Fang; what the fight needs, tested then (the user)

## Golden Path

- [x] GoldenHillsCableCar (29) — for the quest pass: the CableCar quest (its NPC, Event91, the cranks), then the Super
  Block+ medal (flag 534, Jump)
- [x] GoldenHillsPath2 (38) — for the quest pass: the sleepy NPC (the CableCar quest, until flag 182)
- [x] GoldenSettlementEntrance (39) — for the quest pass: the horn quest and Tanjerin (the minigame door's rock, flags
  274-275); the caravan's other stalls, one at a time (the user)
- [x] GoldenHillsPath3 (44)
- [x] GoldenSMinigame (114) — for the quest pass: the mayor's late visit (20 worms, the Desert Key, quest 46, flags
  557-559)

## Golden Settlement

- [x] GoldenSettlement1 (40) — for the quest pass: Aria's offering (Queen's Dinner, flag 393) and the festival chain,
  the quest board (from 86), Samira, the night's talkers, the Mothiva Doll's trade (Defiant Root); for the enemy pass:
  the festival fight (Event58); for the discovery sweep: the balcony (Event53) and The Golden Festival (014); for the
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
- [x] TermiteOutside (173)
- [x] BarrenLandsBeefly (180) — for the quest pass: the hungry termite (flags 475-476, item 145)
- [x] BarrenLandsAntTunnel (181)
- [x] BarrenLandsMiniboss (182) — for the enemy pass: the Primal Weevil fight (Event151, summons a Weevil)
- [x] BarrenLandsPinkSpider (189) — for the quest pass: Layna's Nero (flags 465-468); for the sellers' pass: the
  spider's later trades (berries only)
- [x] BarrenLandsTanks (190)
- [x] BarrenLandsMushrooms (191) — for the quest pass: Layna's scene (Event172, flags 465-468)
- [x] AbandonedCity (192) — for the quest pass: Rebecca's scene (flags 701-709)
- [x] BarrenLandsPumpkins (193)
- [x] BarrenLandsCloud (194)
- [x] BarrenLandsRock (195)
- [x] AbandonedCityTent (196) — for the quest pass: the False Monarch bounty (its Crystal Crown, key item 146, and
  discovery 40), with the other bounties
- [x] BarrenLandsSideGPT (199)

## Far Grasslands

- [x] FGCave (135)
- [x] FarGrasslands1 (137) — for the quest pass: the wasp twin (flags 638-639); for the swamp: Maki the follower
  (Event125)
- [x] FarGrasslandsOutsideCave (138) — for the quest pass: Riz (from flag 509)
- [x] FarGrasslandsWizard (139)
- [x] FarGrasslands2 (140)
- [x] FarGrasslandsLake (141)
- [x] FarGrasslandsOutsideVillage (142) — for the enemy pass: Riz's fight (Event176, flag 509)
- [x] FarGrasslands3 (143)
- [x] FGOutsideSwamplands (146) — for the quest pass: Madeleine (flags 375-389)
- [x] FarGrasslands4 (153)
- [x] WizardTowerBasement (186) — the fall scene and its wizard kept away in a seed (build step 59)
- [x] WizardTowerStairs (187)
- [x] WizardTowerAttic (188) — for the quest pass: the wizard's quest 52 (Find The Ingredients!, his line 4, Jump up a
  ledge); for the sellers' pass: the capsule machine (a Longleg Summoner, 10-19 berries, each time)
- [x] BroodmotherLair (198) — for the enemy pass: the Broodmother fight (Event171, on entering by either door until 459;
  flying, summons Midges); for the quest pass: its 40 berries (line 4), quest 31 completed even untaken (posted but
  untaken, it stays on the boards: the user's call), prize medal 18
- [x] FGClearing (201) — for the quest pass: Maki and Kina's quest 55 (Event207 on the platform, Jump: nine Leafbugs,
  two gifts); the Mechanical Claw's trade at DefiantRoot3 (lines 189-190, medal 61), which makes the claw progression

## Wild Swamplands

- [x] SwamplandsEntrance (145) — its opening talk skipped with Skip cutscenes (the mod guide, step 10)
- [x] Swamplands2 (148) — for the enemy pass: the Leafbug ambush (Event128, until 334, from either side)
- [x] Swamplands3 (151)
- [x] SwamplandsBridge (152) — kept up in a seed (build step 61); for `ChomperCave1`: its bridge reads 337 too
- [x] SwamplandsBoss (154) — mapped early (2026-10-08) while testing its boss; for the quest pass: Kabbu's postgame
  grave scene (`kabbuevent`, flags 555-645)
- [x] Swamplands4 (158)
- [x] Swamplands5 (159) — the Junction; its centipede scene kept away (build step 62)
- [x] Swamplands6 (160) — Crank Pond
- [x] Swamplands7 (161) — Ice Block Climb
- [x] Swamplands8 (162) — Fenced Pond; for the quest pass: Seb (from 389 until 390; his line 4 sets 390, which opens
  the Outskirts house `doormadeleine`, and his talk moves the party to the Outskirts)

## Defiant Root

- [x] DefiantRoot1 (58) — the Square; crystal berry #15 kept until taken (build step 64); for the quest pass: the
  mayor (45 berries from 300; his quest 46 and the Desert Key, the storage's stand-in), Isau's Sophie Petal trade (HP
  Core, flag 396), Eremi's trades (from 300), the storage (193-194, pending: the Desert Key needs quest 46, chapter 5);
  for the sellers' pass: Morty's re-rental of the Bed Bug (30 berries, line 28; the game's own after his check, build
  step 66) and the Spicy Berry seller on the left rooftop (Jump); for
  the discovery sweep: discovery 29 (the museum's signs)
- [x] DefiantRootWell (59) — the Well; for the quest pass: Astotheles' Rusty Key (line 3, from 300 until 239)
- [x] DefiantRoot2 (60) — the Beehive Lift; the inn's upstairs door kept open (build step 65); for the quest pass: the
  innkeeper's daughter (`TermiteIndustrial` line 21 sets 408, she comes home), Kenny (599-600), the ant guard's warp
  to the Lost Sands entrance (line 38, from 300); for the sellers' pass: the inn's rest (12 berries) and the daughter's
  service (7 berries, from 408)
- [x] DefiantRoot3 (61) — the Market (named by the user); no locations; for the quest pass: Kali's shop, shut until
  her board quest 36 is taken (flag 265; left so, the user), her Stolen Silk turn-in (a Lore Book, prize medal 13),
  Zasp's doll trade, Butomo's and Geno's trades, the Mechanical Claw's (medal 61); for the sellers' pass: the item
  shop, the poison seller, the Magic Ice seller (from 345), the bakery and the smithy

## Ancient Castle

- [x] SandCastleEntrance (116) — the Entrance (named by the user, for the enemy pass: no location carries it yet);
  its crystal's first light plays a short scene (`Event157`, flag 416)
- [x] SandCastleSlidePuzzle (117) — the Slide Puzzle
- [x] SandCastleStatueRoom (118) — the Statue Room (named by the user, for the enemy pass: no location carries it yet)
- [x] SandCastleBasement (119) — the Basement; its spots pending with the castle (build step 67)
- [x] SandCastleRoof (120) — the Roof; its spot pending with the castle; the boss door's rule refined with the boss
  key room
- [x] SandCastleMainRoom (121) — the Main Room, the castle's hub; its top reached through the Slide Puzzle (solved)
  and the Pressure Puzzle; for the discovery sweep: discovery 16 up top (line 2, flag 306)
- [x] SandCastleBossKeyRoom (122) — the Boss Key Room; its spots pending with the castle; for the enemy pass: the
  key's Warden fight (Event115, three flying, Vi)
- [x] SandCastlePressurePuzzle (126) — the Pressure Puzzle; its key pending with the castle
- [x] SandCastleRockRoom (127) — the Rock Room; its berry pending with the castle
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

- [ ] WaspKingdomOutside (147) — to do when mapped (the user, 2026-10-08): rework its bottom door's arrival (the gate
  from the lake) so a party can enter or start there; today it lands past its gate inside the patrol's see area, a loop
  (`MEASURED.md`, the lake)
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
