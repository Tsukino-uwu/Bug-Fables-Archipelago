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

**244 of 244 done.**

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
- [x] DesertRoachVillage (95) — the hawk present from the start (until 302), his half of the Sand Castle Key a location
  (316, build step 92)
- [x] DesertOasis (96) — for the quest pass: Stratos and Delilah (until flag 496)
- [x] DesertOasisEntrance (97)
- [x] DesertWestDunes (98)
- [x] DesertSandCastle (107) — for the quest pass: the Sand Castle Key chain (the Heaven and Earth Keys, Event109)
- [x] DesertMountain (108)
- [x] DesertTrenchMiddle (109)
- [x] DesertJumpPuzzle (110)
- [x] DesertSouthern (111)
- [x] DesertScorpion (112) — for the enemy pass: the Scorpion fight (Event111, until 298; present from the start since
  2026-10-12, build step 92), its trigger between the bottom doors and the top one, no fleeing
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
- [x] GoldenHillsDungeonBoss (53) — the Summit (named 2026-10-12); the Ancient Tablet's scene a location (314, Jump);
  for the enemy pass: the Venus' Guardian fight (Event73, Jump up to it; its needs
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
- [x] DefiantRoot3 (61) — the Market (named by the user); its three shops' slots, locations 217-228 (added
  2026-10-10); for the quest pass: Kali's shop, shut until her board quest 36 is taken (flag 265; left so, the user),
  her Stolen Silk turn-in (a Lore Book, prize medal 13), Zasp's doll trade, Butomo's and Geno's trades, the Mechanical
  Claw's (medal 61); for the sellers' pass: its one-item sellers (the Heart Berry, the Magic Ice seller from 345, the
  charms) and the smithy

## Ancient Castle

- [x] SandCastleEntrance (116) — the Entrance (named by the user, for the enemy pass: no location carries it yet);
  its crystal's first light plays a short scene (`Event157`, flag 416)
- [x] SandCastleSlidePuzzle (117) — the Slide Puzzle
- [x] SandCastleStatueRoom (118) — the Statue Room (named by the user, for the enemy pass: no location carries it yet)
- [x] SandCastleBasement (119) — the Basement; its spots pending with the castle (build step 67)
- [x] SandCastleRoof (120) — the Roof; its spot pending with the castle; the boss door's rule refined with the boss
  key room
- [x] SandCastleMainRoom (121) — the Main Room, the castle's hub; its top reached from the middle right by the ledge
  outside, then the Slide Puzzle's upper part and the Pressure Puzzle; for the discovery sweep: discovery 16 up top
  (line 2, flag 306)
- [x] SandCastleBossKeyRoom (122) — the Boss Key Room; its spots pending with the castle; for the enemy pass: the
  key's Warden fight (Event115, three flying, Vi)
- [x] SandCastlePressurePuzzle (126) — the Pressure Puzzle; its key pending with the castle
- [x] SandCastleRockRoom (127) — the Rock Room; its berry pending with the castle
- [x] SandCastleBossRoom (128) — the boss room; its wall to the treasure room kept open (build step 68)
- [x] SandCastleTreasureRoom (129) — the treasure room; its artifact (345, Jump or Bee Fly) held out with the castle

## Bee Kingdom Hive

- [x] BeehiveOutside (62) — Outside the Beehive; its factory door kept open (build step 69); for the
  quest pass: Event88 at that door from 169 until 299 (the factory's story scene); for the discovery sweep: discovery
  25 (Event104, the left bridge, from 299 until 224; the user: keep it but faster, skip the warning line before it)
- [x] BeehiveThroneRoom (63) — the Throne Room; its door kept open from the main area (build step 70)
- [x] BeehiveScannerRoom (64) — the Scanner Room; kept between the outside and the inside, its gate open, its scan
  location 208 with flag 160 (build steps 72-74)
- [x] BeehiveMainArea (67) — the Main Area; the clothing stall's two sales, locations 209-210, after Mothiva's scene;
  for the quest pass: the edgy bee's trade (`edgebee`, flags 425-436: the Blackest Paint, key 139, for crystal berry
  #35)
- [x] HBsLab (68) — HB's Lab; HB asks for the Explorer Permit from the start (build step 75); for the quest pass:
  B.O.S.S.'s Spy Specs (flag 164) and Detector (165), at its 2nd and 5th fights, which list only bosses already met
- [x] BeehiveBalcony (69) — the Balcony; Beette's sale (location 78) at her price again (build step 76), its berries
  with Next 63; for the quest pass: Kenny (598 to 599) and Gen and Eri's talk (`Event86`, sets 167); for the discovery
  sweep: the telescope (`Event127`, discovery 26)
- [x] HoneycombsLab (70) — Honeycomb's Lab; no location; its first-entry scene (`Event80`, flag 175) moves nobody;
  for the quest pass: the hawk's return (the A.D.B.P. Enhancer, medal 28, flag 352) and the Chomper Seed's chain to
  Chompy (`Event134`, `Event135`, flags 341, 402, 405)
- [x] JaunesGallery (71) — Jaune's Gallery; its door kept open from the main area (build step 71); for the quest
  pass: Jaune's quest (Event162, flags 444-446)

## Honey Factory

- [x] HoneyFactoryEntrance (72) — the Lobby; the bottom a drop, Jump or Bee Fly back up; the processing door's Factory
  Pass lock kept, the pass never used up, its door an area the game pushes the party past (build step 79); the storage
  door open (build step 77); the shop's five slots, locations 212-216, its berries with Next 63
- [x] HoneyFactoryWorkerRooms (74) — the Worker Rooms; the office and the sleeping quarters cut off from each other;
  the desk's Shock Candy, location 229 (Jump or Bee Fly); the Factory Pass, location 235 (flag 178, Jump and the
  Beemerang Toss or Bee Fly); for the quest pass: Malbee's Power Exchange (324 to 330); for the discovery sweep: the
  portrait (discovery 15); for the sellers' pass: the quarters' Magic Seed (25 berries, line 11)
- [x] HoneyFactoryCore (75) — one region, its door free; for the quest and enemy passes: the chapter 3 finale
  (`Event99`, from 218, enemy 46, prize medal 6, sets 299), its gate open only then; to check again (the user,
  2026-10-10): the finale from the start, researched (Known issues, chapter 4's opening): left to the story, or open
  from the start played once, the fight a location (the user's pick pending); in game, three things to look at: the
  cube the finale hides in `DesertTrenchMiddle`, quest 28's Madeleine scenery at the Golden Hills dungeon entrance,
  and leaving the storage escort midway
- [x] FactoryProcessingFirstRoom (83) — the First Room; its switch (location 70, the Shield's) a basic attack, the
  platforms it starts the only way across (the Shield); the bottom a drop, up to the right only; the tram to Malbee's
  Room, both ways after her fight (211), mapped with that room
- [x] FactoryProcessing2 (84) — the Second Room; up to the pump door: Jump or Bee Fly, a basic attack, the Shield, and
  Beemerang Halt or Bee Fly; down a drop; no location
- [x] FactoryProcessingPump (85) — the Pump Room; up on the cranks (Beemerang Halt), then the platform loop (Jump and
  the Shield); the Shell Ointment behind the boxes, location 230; Malbee's door behind the Factory Pass scanner (the
  pass shown three times, build step 79; arriving from her room before 217 not seen yet); for the quest pass: the
  broken Mender (325 to 327)
- [x] FactoryProcessingPuzzle1 (86) — Gen and Eri's Room; one region, its door free; its Factory Pass, location 236
  (213, "By the Lever": Jump, Freeze, the Shield, a basic attack, Gen and Eri's fight with Vi, the holler), held out;
  for the quest pass and Next 68: Gen and Eri's scene (`Event97`) needs them, gone from 211 (with Malbee's fight
  first, not seen); then 236 joins seeds
- [x] FactoryProcessingPuzzle2 (87) — the Training Grounds; one region, its door free; its Factory Pass, location 237
  (212, "Behind the Gate": Beemerang Halt, Jump, the Shield, a basic attack for a lever); for the quest pass: the
  broken Mender (325 to 328)
- [x] FactoryProcessingPuzzle3 (88) — the Pressure Plate Room; one region, its door free; its puzzle (Jump, Freeze,
  the horn) to crystal berry #16, "Behind the Pipe", location 231, and its Factory Pass, "Behind the Gate", location
  238 (215)
- [x] FactoryProcessingMalbee (89) — Malbee's Room; its fight (`Event98`, three Abomihoneys, flag 211) a story event
  that starts the tram, both ways to the First Room's right side; the platform Jump or Bee Fly up, a drop down; for
  the quest pass: the broken Mender (325 to 326)
- [x] FactoryStorageMaze (90) — the Storage Maze; its box tops up with Jump or Bee Fly; the medal Shock
  Trooper, "Hole in the Boxes", location 232, and crystal berry #17, "Lever Puzzle", location 233 (a basic attack,
  Jump, Beemerang Halt); the Overseer's door behind its pass lock, an area the game pushes the party past (build step
  79)
- [x] FactoryStorageElevator (91) — the Storage Elevator; the top and the bottom joined by the elevator, a basic attack
  each way; no location
- [x] FactoryStorageMiniboss (92) — the Storage Mini-boss Room; one region; the Magic Seed on top of the boxes,
  location 234 (Jump or Bee Fly); its fight's Factory Pass, location 239 (Jump, Beemerang Halt); for the enemy pass:
  Ahoneynation (`Event101`)
- [x] FactoryStorageOverseer (93) — the Storage Overseer Room; one region, its door free, no location; for the quest
  pass: the overseer's escort (`Event102`, flag 218: Jump, Beemerang Halt and Freeze, or Bee Fly)

## Rubber Prison

- [x] RubberPrisonPier (218) — the Pier; four floors of bridges, each dropped from onto any below, no way back up
  inside the room (the drops count with Points of No Return until the rooms behind each floor give a way back); the
  ground floor's upper left behind the gate levers' tutorial gates (Jump or Bee Fly round), its lower part (the gate
  lever, the dock) below, the lift once its lever is hit; the third floor bridge (build step 78) and save crystal (80)
  kept; the Wasp Driller's Prison Key, "Second Floor Fight", a location since build step 82 (Kabbu); the second
  floor's right door opened by the Warden's Office's crank (583), written with it; for the Giant's Lair Bridge: the
  ground floor's right door (567); for the enemy pass: its four enemies (gone from 79) and the Wasp Driller; for the
  discovery sweep: discovery 47, recorded on arriving in any prison room
- [x] RubberPrisonCheckpointCorridor (219) — the Checkpoint Corridor; the left and the right an attack apart, both
  ways (two gates on flag 535, a lever between them and one on each side, the right one kept present, build step 81);
  the top left door behind the Explorer Permit's lock (until 538), the party pushed past it on arrival; no location;
  for the enemy pass: the Wasp Driller (gone from 79), coming at the party shortly after the pushed-past arrival; for
  the Giant's Lair Bridge: the guard on the right (from 79)
- [x] RubberPrisonSpikeRoom (220) — the Spike Room; the top across the spiked floor, Bee Fly or the Shield; "In the
  Cage", the Prison Key, Bee Fly and an attack; the top door behind the Prison Key's lock (537), pushed past on
  arrival (build step 82); for the enemy pass: a Wasp Bomber (gone from 79)
- [x] RubberPrisonCells1 (221) — the Cell Block Crossroads; up by the bottom lever's rock (Beetle Dig, the horn, Jump
  or Bee Fly) or the rock kept once the right wood lever is hit (540), a drop down; the top door in its own barrier
  (541); the bottom doors behind the Prison Key's lock (539) and the right lever's gate (540), pushed past on arrival;
  no location; for the enemy pass: a ruffian, a wasp scout and a wasp trooper (the last two gone from 79)
- [x] RubberPrisonCells2 (222) — Electric Cells; the bottom doors across the electric floor (the Shield or Bee Fly);
  up on the electric platforms, the Shield only, down a drop; "Middle Right Cell" (the Prison Key) and "Bottom Right
  Cell" (a Poison Cake); the top door behind the Prison Key's lock (542); with Next 62: "Middle Center Cell" (the red
  History Book); for the quest pass: Cenn and Pisci (from 498), the prisoner (until 680); for the enemy pass: a
  ruffian (gone from 79)
- [x] RubberPrisonLibrary (223) — the Library; up the stairs, Jump or Bee Fly; the rail's invisible walls hidden (build
  step 83); with Next 62: "On Top of the Bookshelf" (the yellow History Book) and "Book Puzzle" (the Wooden Crank
  behind the shelf the four History Books slide away); for the enemy pass: a wasp and a ruffian (gone from 79)
- [x] RubberPrisonCafeteria (224) — the Cafeteria; its left a drop down, back up with Jump or Bee Fly; its cannon
  lever (549) never needed; with Next 62: "Behind the Cannons" (the blue History Book, 548); for the enemy pass:
  a Wasp Bomber (gone from 79)
- [x] RubberPrisonGym (225) — the Gym; its door free; up to the top left by its crank puzzle, Beemerang Halt, a drop
  back down; "Crank Puzzle" there, the Prison Key (build step 82); loading it sets 535 off; for the enemy pass: a Wasp
  Driller (top left) and a trooper, gone from 79; from 79, six prisoners and a soldier
- [x] RubberPrisonSecurity (226) — the Security Room; its left (the crank, the trapped guards) Beetle Dig away; the
  bottom right (the save crystal, the door to the Guard Floor) a drop down a ledge, Jump or Bee Fly back up; the crank
  and PC (552, 553) only toggle the Cafeteria's tables (550); no location; for the quest pass: Tectec (from 555); for
  the enemy pass: a wasp trooper (gone from 79)
- [x] RubberPrisonOffice (229) — the Warden's Office (its sign's name); one area; its crank slot (the Wooden Crank,
  Beemerang Halt to turn, 583) opens the Pier's second floor right door, written now; with Next 62: "Empty Bookshelf"
  (the green History Book, 563: Freeze, the horn, Jump and Bee Fly); for the enemy pass: a Wasp Driller (gone from 79)
- [x] RubberPrisonThirdFloor (230) — the Guard Floor; the bottom right (its doors to Electric Cells and the Pier, its
  own lever behind a gate, not counted); the pocket across spikes (the Shield), up to the middle left on its copied
  bounce pad (build step 84), a drop back down; the stairs between the middle left and the bottom left Jump or Bee Fly
  (the bottom left's only way in when its gate to the pocket is up); in the middle left the kept upper lever (build step
  81), the Gym door behind a gate (pushed past on arrival), the upper right's stairs behind another (Jump or Bee Fly);
  the upper right (the Security Room's door) a drop to the bottom right, nothing back up its stairs; no location; for
  the enemy pass: a Wasp Bomber, a ruffian and a burglar (gone from 79)
- [x] RubberPrisonGiantLairBridge (231) — the Giant's Lair Bridge; its left (the door to the Pier's second floor, the
  save crystal, the Venus flower) free; the top left door behind the shortcut lock (567: the Prison Key, or the
  fight), pushed past on arrival; the fight between the left and the middle (the ULTIMAX Tank, Vi for its flying
  summon; flag 79), the only gate; the middle's ant tunnel (from 79) and the right's Giant's Lair door a walk on; the
  ant tunnel's way in (`bugaria_city.py`) now the fight; the fight from either side; the right side out of order
  fixed (build step 85: the chapter scene held until 79, the right door kept, the sealed wall hidden; seen); the Wasp
  Queen's talk after the fight hands over the sixth artifact (346), a location since build step 86, "Queen's Gift";
  for the enemy pass: the fight, and the troopers and tank there until 79

## Giant's Lair

- [x] GiantLairEntrance (232) — the Giant's Lair Entrance (named by the user); one region: its left door on a ledge,
  back up by a bounce pad and the rock beside it (seen); the Venus bud's scene in the middle (Event194, 569; then a
  healing bud), fast-forwarded with *Skip cutscenes* (seen); the right door and a save crystal; a scene to the Roach
  Village from 586 (Event201); no location
- [x] GiantLairDeadLands1 (233) — the Giant's Hand (named by the user); its left (the Entrance's door, the dig spot,
  Crystal Berry 49, Beetle Dig: "Giant's Lair: Giant's Hand, Dig Spot") the map's own region; the lower right across
  the platforms with Bee Fly, both ways; the right door's ledge up by the hand's rock only (Jump and Horn Slash, the hand
  letting go whenever the room is left), a free drop down; for the enemy pass: three Dead Landers and the hand, a
  respawning Dead Lander γ
- [x] GiantLairDeadLands2 (234) — Crane Crossing (named by the user); its left (the Giant's Hand's door on a ledge,
  a bounce pad back up) the map's own region; up the coin stacks to the silver coins (Freeze, Horn Slash, Bee Fly), a
  drop back; down to the middle right, back up with Jump; the middle right's Mystery Berry by the spear (respawning,
  Jump or Bee Fly: "By the Spear") and the Status Mirror on a ledge by the crossing (Bee Fly: "Ledge by the
  Crossing"); on to the right (the door to the fridge outside) with Bee Fly and the switch's crane platform, back by
  a red bounce pad; Bee Fly alone across, harder, left out (Next 71); a red crystal that only heals; for the enemy
  pass: three Dead Landers and a hand, a respawning Dead Lander γ
- [x] GiantLairFridgeOutside (235) — Outside the Fridge (named by the user); its left (Crane Crossing's door, a save
  crystal) the map's own region; the right (the fridge's door) past the explorers' fight (Event198: Dead Landers α, β
  and γ, β flying for good, so Vi; no fleeing; flag 593), started from either side (seen); nothing else needed; its
  scene fast-forwarded with *Skip cutscenes*; Roy's quest's last step here (quest 62, Event223, from 703: the same
  fight, then a Bond Berry), out of the seed until that quest is gone through (build step 44); no location; for the
  enemy pass: the fight
- [x] GiantLairFridgeInside (236) — Inside the Fridge; its 1st floor (the door to Outside the Fridge) the map's own
  region; each floor up by its climb (Freeze, Horn Slash, Jump, Bee Fly: droplets and geysers frozen, ice knocked
  into place, Bee Fly to the next bounce pad, on the top floor to the lift and into it); down a free drop a floor at a
  time round the puzzle areas' edges (a drop into one needs Jump or Bee Fly back); the lift (to the Roach Village)
  at the top, a drop from it; Crystal Berry 48 behind the milk on the 2nd floor (location 250): from the 2nd floor up
  its ice with Freeze, Horn Slash and Jump and a drop to it, or Bee Fly across the gap, never down from the top; for
  the enemy pass: three enemies
- [x] GiantLairRoachVillage (237) — Roach Village (the game's name); its free ground the map's own region: the
  scene (Event199, sets 586), Amber's shop (locations 251-254, from 586, kept past 555), the roach lift down to the
  Entrance (free; one way in the logic, its way up from the Entrance needing the scene seen); the left door free in, out
  only from 595 (the elder upstairs, Event202, Jump or Bee Fly); the fridge's lift up a ledge, Jump or Bee Fly
- [x] GiantLairSaplingPlains (238) — the Sapling Plains; its door and a save crystal the map's own region; the last
  fight's scene (Event200, until 555; two fights) up by Jump or Bee Fly, where the ending leaves to the city's end
  (seen won); its scene fast-forwarded; no location
- [x] GiantLairBeforeBoss (244) — Kitchen Stove; its right (the village's door, the scene, a dig spot: Bee Fly and
  Beetle Dig, a respawning Magic Seed, location 255) the map's own region; the middle's halves up by Jump or Bee Fly
  from each side, the burners' fires between them until the two levers below the left (Horn Slash) turn the heat
  off; the left door (the Sapling Plains) up into the middle's left the same way, its blocker kept away; up the right
  by Bee Fly, Horn Dash and the lever below the right (the air pipe) to the bounce pad to room 245, whose drop lands
  in the middle's left; the pad and the drop never shuffled (build step 88)
- [x] GiantLairBeforeBoss2 (245) — Above the Stove, a shelf above the Kitchen Stove; its right (the bounce pad's door) the
  map's own region; the gate in the middle open only while the ice block sits on the right's pressure plate (no flag)
  by Jump, Bee Fly, Freeze and Horn Slash; the left's right part past it; its left part (the hole down into the
  stove's middle left) by the Beemerang on the switches, Jump, Bee Fly and the Shield over the thorns; no location

## Metal Lake

- [x] MetalLake (183) — Metal Lake, the submarine's lake: no doors, no pickups, six docks (Termite, Metal Island,
  Bugaria, the prison, the Fishing Village, Peacock Island), each reaching every other by the submarine alone (the
  later-chapters stand-in dropped from the rides); never a start or shuffled, having no door; for the enemy pass: two
  wasps (until 79) and Striders
- [x] MysteryIsland (206) — Peacock Island (the game's name, discovery 46); the dock (the submarine, a save crystal) the
  map's own region; the door inside and the Venus bud each their own area, by Icicle and Jump or Bee Fly from the dock;
  the back by Icicle and Jump: the Camp Sign (discovery 46, Jump, location 257) and the Dig Spot (Dark Cherries, Jump,
  Bee Fly, Beetle Dig, location 256); four grass patches hide nothing
- [x] MysteryIslandInside (207) — Peacock Island Cave; the door the map's own region; up along the cave's stream by
  Icicle and Jump, both ways, to the top: the Peacock Spider's bounty fight (Event124, until 502; the Crystal Feather),
  for the quest pass; after it a red bounce pad back down to the door; no location

## Metal Island

- [x] MetalIsland1 (94) — Metal Island (the game's name); the town's ground the map's own region: the hotel door, the
  save crystal, the tea seller (Tea Shop 1 and 2, Burly and Spicy Tea, locations 260-261), the Mothington X Statue
  (discovery 44, location 259), the quest board; the dock below (the boat, the submarine) a drop, back up by Jump or
  Bee Fly; Master Slice's house (the Chef's) up by Jump or Bee Fly; the Chef's Roof (crystal berry #36, location 258)
  by Jump or Bee Fly and Beetle Dig under a fence to a bounce pad; the left log house kept open (from 555 in the game:
  the door, its closed-door scenery hidden, and its two ladybugs kept present; seen); quests for the quest pass
- [x] MetalIsland2 (131) — Metal Island Hotel; everything free: the town's door, the inn (its Tangy Jam each stay, no
  location, Next 74), the rooms, the storage ant, the ant tunnel's end (its miner digs, a free far end); the top left
  door to the Auditorium open from the start (its blocking guard and invisible wall, there until every Bugaria card
  master is beaten, flag 315, kept away; the guard who lets you by kept present; seen); Carmina's room reached only
  by the Auditorium's dig
- [x] MetalIslandAuditorium (228) — Metal Island Auditorium; its bottom (the hotel door, three NPCs, a save crystal)
  the map's own region; the top and the stage by Jump or Bee Fly, both ways (a drop down); from the top a dig (Beetle
  Dig) into Carmina's room in the hotel, one way (Event192: her scene and fight once, flag 562), out into the lobby;
  the judge's Spy Cards tournament (25 berries, Event210: the Card Trophy, key item 184, once) for the card pass

## Termite Capitol

- [x] TermiteMainPlaza (174) — Main Plaza, the Capitol's entrance; everything free: the gate (a ride from outside),
  the doors to the industrial district and the Royal Chamber, the save crystal, the shop computer (Shop 1-4: Squash,
  Shell Ointment, Mega-Rush, Aphid Shake; locations 262-265), DineMite's Mite Burger (dialogue, repeatable), the poster;
  the worker's request (a Drowsy Cake, then a Shock Candy, flags 626 and 627, for the TP Plus medal) for the quest pass
- [x] TermiteRoyalChamber (175) — Royal Chamber; the floor free, its scenes (Event152 until 386; the King's reward,
  Event164, from 409 until 379, location 76, still the later-chapters stand-in for the Colosseum) played there; the
  Termite Statue (discovery 24, location 266); the thrones' platform by Jump or Bee Fly, Crystal Berry 33 hidden behind
  the thrones the same way (location 267)
- [x] TermiteIndustrial (176) — Industrial District; the street, both buildings' elevators and the shops free; the
  Rooftop Vista on the right roof (discovery 43, location 269); the Apartment Balcony's Lore Book by Bee Fly from the
  left roof (270); the bomb shop (Bomb Shop 1-7, 271-277) and Bomb Master on its stand (Bomb Shop Medal, 278). Outside
  the Colosseum, an area of the same map behind its in-map door pair, free both ways: the Lore Book Between the Boxes
  (268) and the Plushie for Terrie (279: the plushie, once the Colosseum is beaten; the beggar's berry not a check)
- [x] TermitePier (177) — Pier; the street door, the building and the sign free; the submarine's dock a one-way drop,
  Jump or Bee Fly back up (the submarine leaves and lands there); Crystal Berry 28 on an isolated pillar in the
  water, Bee Fly both ways (location 280)
- [x] TermiteColiseum1 (178) — Colosseum Lobby; free; the guards' two openings scenery; the receptionist's fights
  (Event163, the arena transfer: three fights in a row, the whole party) from flag 386 (the King's scene in the
  Royal Chamber) on; Crickerly's stall kept
  present (Caravan Shop 1-5, locations 281-285)
- [x] TermiteColiseum2 (179) — Colosseum Arena; no door, reached only by the receptionist's scene (Event163: three
  fights in a row, the whole party); no pickup or check of its own

## Wasp Kingdom Hive

- [x] WaspKingdomOutside (147) — Outside the Hive; the top right door free both ways; the left (the cave's) pushes the party past a
  boulder, Horn Dash back out; the hive's left door (to `WaspKingdom1`, a one-way drop) inside grass that grows back,
  Horn Slash both ways; the hive's door and the lake's gate behind barriers (the hive's from 555), each putting
  an arriving party past it; the hive's door one way out (from the Main Hall, its barrier there off), the lake's gate still shut both ways (both ends from 555);
  the patrol and its see area kept away (they guarded only the barred door, and caught a party arriving from the lake
  in a loop: arriving there seen free with them away); no pickup
- [x] WaspKingdom1 (163) — Pantry; the grass door's one-way drop lands at its bottom left; up to its one door (to
  `WaspKingdom2`) by Jump or Bee Fly, down free; its two stealth wasps kept away; a party talk on entering (until
  367), nothing given; no pickup
- [x] WaspKingdom2 (164) — Central Hall; the bottom (the Pantry's door, the lower left to 243) free; up on the wooden lift, its lever
  in a small house (Beetle Dig, then any attack; reset on leaving), boarded by Jump or Bee Fly; down a free drop; the top (save crystal,
  upper left door; the top middle opening scenery); the right door behind the Wasp Key's lock (Event59 index 9, used
  up, 363), arriving there pushed past it, out shut until the key is an item; its two stealth wasps kept away; no pickup
- [x] WaspKingdom3 (165) — Fountain Room; one door (the Central Hall's), everything free with its two stealth wasps
  away; the Wasp Key (key item 119, flag 364), which then lay free under the left ledge, moved on the roof: Beemerang
  Halt and Bee Fly (location 286; seen reached so; may go back to the floor, free, later); the Wasp Key opens the
  Central Hall's lock
- [x] WaspKingdom4 (166) — Barrier Room; the left door's area; a lever (any attack) and a copied twig dug under (Beetle Dig, build
  step 89) to the top, both ways; across the top under its own twig (Dig) to a lever (any attack) opening the right
  door's barrier; no way back from the right; its five stealth wasps kept away; no pickup
- [x] WaspKingdom5 (167) — Food Storage; the bottom (left and right doors, save crystal) free; the top behind a key
  door on the ground, the Wasp Key both ways (Event59 index 9, kept in a seed; 365); the vent (to the main hall) on a
  ledge, down free, up by Jump; a party talk on entering (until 368), nothing given; one regular enemy; no pickup
- [x] WaspKingdomPrison (168) — Prison; free between its two doors (the bottom right opening scenery); the cells, the
  messenger ant's among them, burrowed into (Beetle Dig); Dark Cherries dug up in the bottom right cell past a boulder
  (Horn Dash, then Dig; flag 369, location 287)
- [x] WaspKingdomJayde (169) — Jayde's Room; free; her meeting on arriving (the room's own scene, Event139, while
  366 is off) gives the game's second Wasp Key (location 291, Arrival) and sets 366, which swaps her for her shop
  (Shop 1-3: Jayde's Stew, Magic Seed, Mushroom Salad; 288-290); seen in a seed, walked in. The dev warp marks a
  room's own scenes seen, so warping straight in skips it
- [x] WaspKingdomMainHall (170) — Main Hall; the vent from the Food Storage opens on its own ledge, a drop to the floor with no way
  back up (the dev warp's landing there sticks; walked through, fine); the front door to Outside the Hive (both ends
  from 555) kept present, its barrier here (`Base/gate`) off and the outside one kept: one way out, seen; the throne room's door up top by Jump
  or Bee Fly, a drop back; everyone else here from 555; no pickup
- [x] WaspKingdomThrone (171) — Throne Room; its bottom door free; up a ledge (Jump or Bee Fly) the fight with General Ultimax, a
  Wasp Driller and a Wasp Bomber (Event140, until 370; Kabbu for the Driller); the Queen's door beyond, Jump or Bee Fly
  both ways; everyone else here from 555; no pickup
- [x] WaspKingdomQueen (172) — Queen's Chamber; its door back made only from 555; its scene on entering (Event141, until 555) sends the
  party to Bugaria's main plaza once Ultimax is beaten (371), every visit (seen); before 371 the room started his fight
  itself, which hangs (seen): that trigger (`event2`) kept away, the scene held until 371, the door back kept present,
  so an early visit is quiet and walks out (seen); nothing to find
- [x] WaspKingdomDrillRoom (243) — Drill Room; one door (the Central Hall's lower left); a respawning mound (no flag of
  its own, regional flag 6) dropping a berry pile (item 186), Beetle Dig (location 292, not yet seen sent: needs a
  seed that has it)

## Bandit Hideout

- [x] HideoutEntrance (99) — Entrance; free throughout; the story's capture skipped (its altar trigger `eventcheck` kept away):
  flag 18's other changes made instead, the door below and Astotheles' fight trigger (`event2`) kept present and the
  wall over that door (`Base/FakeWalls`, a ConditionChecker until 18) off, seen; Astotheles (until 301), his diary and
  note (from 301). The Dig location (flag 18, location 71) moved here from the Cell (build step 90): an item on the
  altar, its collider fitted to its model, by Jump or Bee Fly (seen). For the quest pass: Astotheles' fight at the far
  right (`event2`, Event109, reached freely, once: its 301 limit kept, seen) gives the Earth Key (106), half the Sand
  Castle Key (seen)
- [x] HideoutCell (100) — Cell; the middle (door, save crystal; the crystal's copies swapped by 11) free; the left cell
  dug into (Beetle Dig), its Dig scene's trigger kept away (location 71 is on the Entrance's altar); the right cell dug into, its dig
  spot starting Event217, Cerise with Crystal Berry 19 (674, 682; location 293, seen given); a droplet that only drips
- [x] HideoutCentralRoom (101) — Central Room; free between its four doors (the Cell's, left, bottom, right); a talk in the middle on
  the first visit (Event109's part here, until 259; sets 259 and 401, nothing given) from any door; its stealth bandits
  away, two regular enemies kept; no flag-driven scenery; no pickup
- [x] HideoutLeftA (102) — West Passage; its right door's lower area; up to the upper left (the garden door) by a frozen droplet
  knocked over and jumped onto (Freeze, Horn Slash, Jump), a free drop back; the crates that make steps from the
  escape on (11, ConditionCheckers) left hidden on purpose (2026-10-12, seen both ways); its stealth bandits away, a
  regular burglar kept; no pickup
- [x] HideoutStairsRoom (103) — Crank Room; its north door and a crank (the map's own region); the bottom (the door up to the
  Entrance, another crank) across a platform the cranks flip (Beemerang Halt) or flown over (Bee Fly), both ways; that
  door, made only from 11, made from the start (seen), and its save crystal and Venus healer (from 11) too; no
  flag-driven scenery; no pickup
- [x] HideoutGarden (104) — its right door's bottom right; the bottom left (the well door, made only from 11, made from
  the start) dug to (Beetle Dig) both ways; the upper area (the storage door, the Stolen Silk by a box, location 294)
  up from the bottom left by Jump or Bee Fly, a one-way drop to either side; its stealth bandits away, three regular
  enemies kept; no flag-driven scenery
- [x] HideoutWestStorage (105) — Treasury; the door and save crystal free; under a barrier (Beetle Dig) and up on boxes
  (Jump or Bee Fly) to the story's scene (Event109, until 260: talk only, sets 260, its wasps and bandits leave and the
  blocked path opens), then the Lore Book by the stolen-goods chest (location 295); that chest's switch (the items'
  return, sets 11) kept away (its scenery's open look from 11); for the quest pass: the bandits' scene from 512 until
  513 (`munevent`)
- [x] HideoutRightA (106) — Dining Hall; its door's floor free; the kitchen (right) by the green bounce pad (Jump or
  Bee Fly) and the platforms, back by its own pad: Crystal Berry 20 dug up (Beetle Dig, 296) and a respawning Squash
  by the cauldron (from 301, made from the start; 297); a Bond Berry on the middle table, dropped onto from the
  platforms (Jump or Bee Fly; takeable only from 11, made from the start; 298, seen taken), the table's food props
  kept (hidden from 11 in vanilla) but their own display Bond Berry hidden (seen); its stealth bandits and catch triggers away, two regular enemies kept; for the
  quest pass: the Orange Horn (from 139, set at the Golden Settlement's entrance) and the cook (from 301)

## Stream Mountain

- [x] StreamMountain1 (132) — Water Crossing; its right door (from the desert, pressed to enter); water in the middle crossed both ways
  by Icicle and Jump, Bee Fly, or a tunnel dug through (Beetle Dig), to the left door; a regular enemy; no pickup
- [x] StreamMountain2 (133) — Water Switch Room; the upper right (right door, crystal switch: Beemerang Toss, or Bee Fly and an attack,
  raises or lowers the bottom's water, which stays); the bottom (its door, bridges 317/318 knocked down by Horn Slash,
  which stay; a bounce pad up) dropped into with the water lowered; the upper left (left door) by Bee Fly or the
  bridges with the water raised (no ice here), back free (a drop, then the pad or the water's respawn); arriving by
  the bottom door into raised water puts the party at the upper right (the game's own respawn, seen); no pickup
- [x] StreamMountain3 (134) — Climb; its right door at the bottom; up its bounce pads to the upper part (the exit to
  the Desert Oasis) by Jump or Bee Fly, a free drop back; the Antlion Jaws medal (flag 418, location 299) at the top
  by frozen geysers (Freeze and Jump); three regular enemies
- [x] StreamMountain4 (202) — Bridge Puzzle; the right door and crystal switch (any attack); the middle (the Clear Water
  among the logs, location 300; a bounce pad up) dropped into with the water lowered; the left (the boss door) by the
  bridge (Jump or Bee Fly, Horn Slash; 491) and the logs held up by Beemerang Halt, then Jump; a one-way drop back.
  Its logs floated only from chapter 1's end (41): repointed to an always-set flag (`float_flags`, seen)
- [x] StreamMountain5 (204) — Wyrm's Den; free (its shallow water walked through): its door, a save crystal, a Venus healer; no flag-driven scenery or floats; for the
  quest pass: the Tidal Wyrm's bounty fight in the middle (Event124, until 496; a Coal Crystal, key item 29; seen won): location 301, Bounty Fight, the Coal Crystal an item

## Chomper Caves

- [x] ChomperCave1 (155) — Cave Entrance; the bottom (the door out, the low door, the save crystal) and the upper area (the high door,
  the boss door, the Venus healer) joined by a bridge knocked down from above (Horn Slash; 689, it stays); no pickup
- [x] ChomperCaves2 (156) — Droplet Room; its bottom (the low door; the Berserker medal behind a crack at the far left,
  dug to and from, Beetle Dig, flag 338, location 302); up to the high door by frozen droplets knocked down and
  climbed (Freeze, Horn Slash, Jump), a one-way drop back; five regular enemies
- [x] ChomperCaves3 (157) — Chomper Nest; free in and out; walking up starts the Mother Chomper's fight (Event132,
  two Chompers with her, until 339), then the Chomper Seed lies where she sat (flag 340, location 303, an item now)

## Fishing Village

- [x] FishingVillage (144) — the village and its dock, one free area; a Lore Book behind a plant (flag 331, location
  304); the submarine's dock with its key item; Riz and the messenger ant from 509 (the story); no flag-driven scenery

## Upper Snakemouth

- [x] UpperSnekTransition (209) — Icicle Pond; its right door the map's own region; the water (Icicle and Jump)
  to the left platform, a dig barrier (Beetle Dig) to the left door and its switch (Icicle, or Bee Fly and an attack;
  518, for good), which opens that door, a one-way in until then; its scene (Event180, until 171; "Fight", location 74)
  from either door, moving a party from the left to the right once (not in the logic)
- [x] UpperSnekSwitchPuzzle (210) — the Pipe Puzzle; one free room, its one door on the right; switches 2, 4, 5 and 7
  lit, then the wood lever (a basic attack) open the round door (`Base/CircleDoor`, 519) to the gear (key item 158,
  520; "Puzzle Reward", location 306); for the gears' room: the three gears as items (pending)
- [x] UpperSnekBeforeBoss (211) — the Gear Machine; its bottom (both doors' side, the Venus bud) free; the Mushroom
  on top of the machine (respawning, regional 0; Jump or Bee Fly; location 307); the three gears in their slots (529-531)
  open the boss door's round door (528), pushed past on arrival (seen); a hidden computer room at the top right (Bee
  Fly from the machine's top, then Beetle Dig through a crack), only three monitors' text; for the discovery sweep:
  the monitors; for the gears' rooms: the gears as items
- [x] UpperSnekPressurePlateRoom (212) — the Piano Puzzle; one free floor, its one door at the bottom; the Extra
  Freeze medal in a broken test tube (522, free, location 308); its plates held by enemies frozen and knocked onto them
  (Freeze, Horn Slash), raising piano keys (Jump), the ice lasting long enough only with Extra Freeze (seen; the medal
  progression): the Small Gear behind the plate door (521, location 309; the door stays open while inside) and Crystal
  Berry #42 by the wind pipe (location 310)
- [x] UpperSnekBossRoom (213) — the Laboratory; one free area, its door on the right; no item; the Zommoth fight
  (`Event182`, started at the far left, fought on the way back; until 532), its closing choice sending the party to
  Bugaria's main plaza once (not in the logic); for the enemy pass: the fight
- [x] UpperSnekMiddleRoom (214) — the Flooded Hall, a hub of platforms in water (Icicle and Jump, or Bee Fly); the
  middle (save crystal, bottom door) the map's own region; the lower top left up to both upper doors (Jump or Bee Fly),
  the upper top right's hanging bridge a drop to the middle; both upper doors behind Lab Card locks (Event59 index 12;
  524, 525), pushed past on arrival (seen); the Sleep Bomb behind the waterfall (740, Icicle and Jump, location 305);
  for the Lab Card's rooms: two cards (523, 526), two locks, a kept key like the Factory Pass (pending)
- [x] UpperSnekPlatformRoom (215) — the Icicle Climb; its bottom floor (the door, two switches, a bounce pad) free;
  moving platforms up to the top, their switches hit from above and below by the Icicle (Jump or Bee Fly between
  them), to a Lab Card in a broken test tube (523, location 311); the Lab Card kept, one in a seed (2026-10-12)
- [x] UpperSnekRiverPuzzle (216) — the River Puzzle; its door at the bottom free; a Zombeetle carrying a Lab Card
  (`ShwKEYZombeetle`, 526) behind a barrier, reached by Jump, the Icicle and the Beemerang Halt, back out with Beetle
  Dig (location 312, the Lab Card kept: a random item in a seed); for the enemy pass: the Zombeetle
- [x] UpperSnekGeizerRoom (217) — the Geyser Room; its door on the right free; the Big Gear below the pump at the
  far left (527, location 313): Bee Fly alone, or the Icicle and Jump to the wooden lever (a basic attack) and the
  geysers frozen and jumped across (Freeze); back by a shortcut and a drop by the lever

## Not rooms

Never part of anything (`UNUSED_MAPS`, `data_tables.py`): `TestRoom` (0), `SnakemouthEmpty` (15).
