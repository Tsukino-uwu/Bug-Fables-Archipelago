# The Archipelago side: how it works, and how it was built

This is the Archipelago half of the Bug Fables randomizer: the apworld, seeds, the server, and how the mod
connects, sends what the player finds and receives items. It has two parts: **how we built it**, step by
step, and **how it works**, a plain explainer of how any game talks to Archipelago. The game side (the mod
itself, probing the game) has its own guide: [documentation.md](documentation.md).

The explainer follows Archipelago's own [network protocol
doc](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/network%20protocol.md) (read at version 0.6.7). Where
this file and that doc disagree, that doc is right.

## Contents

**By topic** (the steps are numbered in the order they were built; the game side of each is in the
[mod guide](documentation.md#the-steps)):

- **The logic, what needs what:** how the rules were found,
  [8](#build-step-8-the-logic-first-part-every-gate-read-from-the-games-data-in-progress); how they're written,
  [24](#build-step-24-the-logic-second-part-the-rules-for-writing-it-room-by-room); the Python modules,
  [29](#build-step-29-the-logic-third-part-python-modules-per-area-on-the-rule-builder); regions and rules explained,
  [How it works 11](#11-the-logic-explained-regions-exits-rules-and-this-worlds-layout); the open world,
  [9](#build-step-9-the-open-world-story-blockers-removed-in-the-logic-and-the-mod), the wizard's tower
  [59](#build-step-59-the-wizards-tower-open-its-fall-scene-kept-away); Points of No Return, the Warp as
  the way back, [37](#build-step-37-points-of-no-return-the-warp-counted-as-the-way-back).
- **The entrance randomizer:**
  [12](#build-step-12-the-entrance-randomizer-doors-shuffled-by-archipelagos-own-experimental), Room Swap
  [30](#build-step-30-the-entrance-randomizers-room-swap-whole-rooms-trade-places-experimental), Decoupled
  [31](#build-step-31-the-entrance-randomizers-decoupled-each-door-one-way-experimental), connection plando
  [32](#build-step-32-the-entrance-randomizers-connection-plando-doors-pinned-in-the-yaml), one-way doors (the fog maze)
  [38](#build-step-38-one-way-doors-in-the-entrance-randomizer-the-forsaken-lands-fog-maze); in the game, the mod
  guide's [13](documentation.md#13-the-entrance-randomizer-in-the-game-doors-rewritten-at-map-load).
- **What goes in the item pool:** the pool's rules,
  [1](#build-step-1-the-apworlds-layout-item-classes-and-location-names); the Boat Ticket,
  [16](#build-step-16-the-boat-ticket-metal-island-behind-a-custom-key-item), and the submarine,
  [36](#build-step-36-progressive-boat-the-boat-ticket-and-the-submarine-as-items); party members and moves, the design,
  [13](#build-step-13-party-members-and-moves-as-items-the-design-in-progress); party members,
  [18](#build-step-18-starting-party-member-the-other-members-shuffled-as-items) and
  [20](#build-step-20-all-three-the-default-every-member-from-the-start-no-member-items); moves and abilities,
  [21](#build-step-21-shuffle-field-moves-the-three-starting-moves-as-items),
  [22](#build-step-22-shuffle-jump-jump-as-an-item) and
  [23](#build-step-23-the-seven-learned-field-abilities-as-items-always-progressive).
- **What counts as a location:** [6](#build-step-6-sending-checks-read-from-the-games-own-flags),
  [10](#build-step-10-more-kinds-of-location-crystal-berries-quests-discoveries-pickups-boss-medals),
  [11](#build-step-11-shops-as-locations-medal-shops-item-shops-the-caravan),
  [26](#build-step-26-the-tutorials-crunchy-leaf-as-a-location-items-the-story-adds),
  [27](#build-step-27-checked-pickups-hidden-in-every-save-new-files-too).
- **Other yaml options:** the goal, [3](#build-step-3-the-goal-artifacts-required), and its guard,
  [60](#build-step-60-the-goal-guard-a-goal-flag-set-only-by-its-own-events); Enemy Shuffle,
  [14](#build-step-14-enemy-shuffle-which-enemies-each-fight-has-in-progress); Starting Location,
  [15](#build-step-15-starting-location-a-new-file-starts-in-a-random-room-experimental); Music Shuffle,
  [33](#build-step-33-music-shuffle-songs-and-jingles-swapped-per-seed) and the factory's songs,
  [43](#build-step-43-music-shuffle-the-honey-factorys-two-songs-the-elevators-crossfade-a-plain-fade); Shuffle Shop
  Inventories,
  [34](#build-step-34-shuffle-shop-inventories-what-shops-restock-and-pickups-respawn-with); Filler Starting Checks,
  [35](#build-step-35-filler-starting-checks-the-openings-automatic-checks-hold-filler).
- **Connecting to the server:** [2](#build-step-2-the-mods-first-login-to-an-archipelago-server),
  [4](#build-step-4-auto-connect-retries-and-a-dropped-connection),
  [5](#build-step-5-a-compressed-websocket-connection),
  [7](#build-step-7-receiving-items-each-once-counted-in-the-save), and How it works 1 to 7.
- **Shared with other players:** Item colors,
  [19](#build-step-19-item-colors-archipelagos-colours-for-players-and-item-classes); DeathLink,
  [25](#build-step-25-deathlink-a-panel-row-deaths-sent-and-received).
- **Releases and safety:** [17](#build-step-17-releases-the-three-downloads-and-how-theyre-built),
  [28](#build-step-28-the-preflight-nothing-unpublishable-in-the-repo-or-a-release).
- **slot_data and trackers:** the seed's options in one `options` dict,
  [39](#build-step-39-the-seeds-options-in-slot_data-one-options-dict-the-mod-reads); Universal Tracker,
  [40](#build-step-40-universal-tracker-the-seed-rebuilt-from-slot_data-with-no-yaml) and
  [41](#build-step-41-universal-trackers-list-order-and-explanations), explained in [How it works
  12](#12-universal-tracker-how-its-implemented); the PopTracker pack,
  [42](#build-step-42-the-poptracker-pack-first-part-its-own-repo-the-logic-exported-from-the-apworld); every key, [How
  it works 7](#7-slot_data-the-seeds-settings-and-this-worlds-keys).

**How we built it**

1. [Build step 1: the apworld's layout, item classes and location names](#build-step-1-the-apworlds-layout-item-classes-and-location-names)
2. [Build step 2: the mod's first login to an Archipelago server](#build-step-2-the-mods-first-login-to-an-archipelago-server)
3. [Build step 3: the goal, Artifacts Required](#build-step-3-the-goal-artifacts-required)
4. [Build step 4: auto-connect, retries and a dropped connection](#build-step-4-auto-connect-retries-and-a-dropped-connection)
5. [Build step 5: a compressed websocket connection](#build-step-5-a-compressed-websocket-connection)
6. [Build step 6: sending checks, read from the game's own flags](#build-step-6-sending-checks-read-from-the-games-own-flags)
7. [Build step 7: receiving items, each once, counted in the save](#build-step-7-receiving-items-each-once-counted-in-the-save)
8. [Build step 8: the logic, first part: every gate read from the game's data (in progress)](#build-step-8-the-logic-first-part-every-gate-read-from-the-games-data-in-progress)
9. [Build step 9: the open world, story blockers removed in the logic and the mod](#build-step-9-the-open-world-story-blockers-removed-in-the-logic-and-the-mod)
10. [Build step 10: more kinds of location (crystal berries, quests, discoveries, pickups, boss medals)](#build-step-10-more-kinds-of-location-crystal-berries-quests-discoveries-pickups-boss-medals)
11. [Build step 11: shops as locations (medal shops, item shops, the caravan)](#build-step-11-shops-as-locations-medal-shops-item-shops-the-caravan)
12. [Build step 12: the entrance randomizer, doors shuffled by Archipelago's own (experimental)](#build-step-12-the-entrance-randomizer-doors-shuffled-by-archipelagos-own-experimental)
13. [Build step 13: party members and moves as items, the design (in progress)](#build-step-13-party-members-and-moves-as-items-the-design-in-progress)
14. [Build step 14: Enemy Shuffle, which enemies each fight has (in progress)](#build-step-14-enemy-shuffle-which-enemies-each-fight-has-in-progress)
15. [Build step 15: Starting Location, a new file starts in a random room (experimental)](#build-step-15-starting-location-a-new-file-starts-in-a-random-room-experimental)
16. [Build step 16: the Boat Ticket, Metal Island behind a custom key item](#build-step-16-the-boat-ticket-metal-island-behind-a-custom-key-item)
17. [Build step 17: releases, the three downloads and how they're built](#build-step-17-releases-the-three-downloads-and-how-theyre-built)
18. [Build step 18: Starting Party Member, the other members shuffled as items](#build-step-18-starting-party-member-the-other-members-shuffled-as-items)
19. [Build step 19: Item colors, Archipelago's colours for players and item classes](#build-step-19-item-colors-archipelagos-colours-for-players-and-item-classes)
20. [Build step 20: All Three (the default), every member from the start, no member items](#build-step-20-all-three-the-default-every-member-from-the-start-no-member-items)
21. [Build step 21: Shuffle Field Moves, the three starting moves as items](#build-step-21-shuffle-field-moves-the-three-starting-moves-as-items)
22. [Build step 22: Shuffle Jump, Jump as an item](#build-step-22-shuffle-jump-jump-as-an-item)
23. [Build step 23: the seven learned field abilities as items (always, progressive)](#build-step-23-the-seven-learned-field-abilities-as-items-always-progressive)
24. [Build step 24: the logic, second part: the rules for writing it, room by room](#build-step-24-the-logic-second-part-the-rules-for-writing-it-room-by-room)
25. [Build step 25: DeathLink, a panel row, deaths sent and received](#build-step-25-deathlink-a-panel-row-deaths-sent-and-received)
26. [Build step 26: the tutorial's Crunchy Leaf as a location (items the story adds)](#build-step-26-the-tutorials-crunchy-leaf-as-a-location-items-the-story-adds)
27. [Build step 27: checked pickups hidden in every save, new files too](#build-step-27-checked-pickups-hidden-in-every-save-new-files-too)
28. [Build step 28: the preflight, nothing unpublishable in the repo or a release](#build-step-28-the-preflight-nothing-unpublishable-in-the-repo-or-a-release)
29. [Build step 29: the logic, third part: Python modules per area on the Rule Builder](#build-step-29-the-logic-third-part-python-modules-per-area-on-the-rule-builder)
30. [Build step 30: the entrance randomizer's Room Swap, whole rooms trade places (experimental)](#build-step-30-the-entrance-randomizers-room-swap-whole-rooms-trade-places-experimental)
31. [Build step 31: the entrance randomizer's Decoupled, each door one way (experimental)](#build-step-31-the-entrance-randomizers-decoupled-each-door-one-way-experimental)
32. [Build step 32: the entrance randomizer's connection plando, doors pinned in the yaml](#build-step-32-the-entrance-randomizers-connection-plando-doors-pinned-in-the-yaml)
33. [Build step 33: Music Shuffle, songs and jingles swapped per seed](#build-step-33-music-shuffle-songs-and-jingles-swapped-per-seed)
34. [Build step 34: Shuffle Shop Inventories, what shops restock and pickups respawn with](#build-step-34-shuffle-shop-inventories-what-shops-restock-and-pickups-respawn-with)
35. [Build step 35: Filler Starting Checks, the opening's automatic checks hold filler](#build-step-35-filler-starting-checks-the-openings-automatic-checks-hold-filler)
36. [Build step 36: Progressive Boat, the Boat Ticket and the submarine as items](#build-step-36-progressive-boat-the-boat-ticket-and-the-submarine-as-items)
37. [Build step 37: Points of No Return, the Warp counted as the way back](#build-step-37-points-of-no-return-the-warp-counted-as-the-way-back)
38. [Build step 38: one-way doors in the entrance randomizer (the Forsaken Lands' fog maze)](#build-step-38-one-way-doors-in-the-entrance-randomizer-the-forsaken-lands-fog-maze)
39. [Build step 39: the seed's options in slot_data, one `options` dict the mod reads](#build-step-39-the-seeds-options-in-slot_data-one-options-dict-the-mod-reads)
40. [Build step 40: Universal Tracker, the seed rebuilt from slot_data with no yaml](#build-step-40-universal-tracker-the-seed-rebuilt-from-slot_data-with-no-yaml)
41. [Build step 41: Universal Tracker's list order and explanations](#build-step-41-universal-trackers-list-order-and-explanations)
42. [Build step 42: the PopTracker pack, first part: its own repo, the logic exported from the apworld](#build-step-42-the-poptracker-pack-first-part-its-own-repo-the-logic-exported-from-the-apworld)
43. [Build step 43: Music Shuffle, the Honey Factory's two songs, the elevator's crossfade a plain fade](#build-step-43-music-shuffle-the-honey-factorys-two-songs-the-elevators-crossfade-a-plain-fade)
44. [Build step 44: every quest available from the start, none of it done for you](#build-step-44-every-quest-available-from-the-start-none-of-it-done-for-you)
45. [Build step 45: the ant tunnels, the miners dig for free](#build-step-45-the-ant-tunnels-the-miners-dig-for-free)
46. [Build step 46: Beette's sale, a free location](#build-step-46-beettes-sale-a-free-location)
47. [Build step 47: Enemysanity, every map enemy a location](#build-step-47-enemysanity-every-map-enemy-a-location)
48. [Build step 48: Extra Roadblocks, and a map split into areas](#build-step-48-extra-roadblocks-and-a-map-split-into-areas)
49. [Build step 49: hidden items and dig spots, two location toggles](#build-step-49-hidden-items-and-dig-spots-two-location-toggles)
50. [Build step 50: the Termacade, its gift and prize stand](#build-step-50-the-termacade-its-gift-and-prize-stand)
51. [Build step 51: Minigame Prizes, and Wacka Worm only for Vi](#build-step-51-minigame-prizes-and-wacka-worm-only-for-vi)
52. [Build step 52: the festival night at will, a switch NPC](#build-step-52-the-festival-night-at-will-a-switch-npc)
53. [Build step 53: the festival's offerings as items, the contest always won](#build-step-53-the-festivals-offerings-as-items-the-contest-always-won)
54. [Build step 54: Riz always offers his fight](#build-step-54-riz-always-offers-his-fight)
55. [Build step 55: Universal Tracker's deferred entrances, shuffled doors hidden until taken](#build-step-55-universal-trackers-deferred-entrances-shuffled-doors-hidden-until-taken)
56. [Build step 56: the PopTracker pack's export at world 0.3.0](#build-step-56-the-poptracker-packs-export-at-world-030)
57. [Build step 57: the maps visited and the map the player is on, for the trackers](#build-step-57-the-maps-visited-and-the-map-the-player-is-on-for-the-trackers)
58. [Build step 58: the PopTracker pack's Archipelago interface](#build-step-58-the-poptracker-packs-archipelago-interface)
59. [Build step 59: the wizard's tower open, its fall scene kept away](#build-step-59-the-wizards-tower-open-its-fall-scene-kept-away)
60. [Build step 60: the goal guard, a goal flag set only by its own events](#build-step-60-the-goal-guard-a-goal-flag-set-only-by-its-own-events)
61. [Build step 61: the swamp bridge kept up, its collapse kept away](#build-step-61-the-swamp-bridge-kept-up-its-collapse-kept-away)
62. [Build step 62: a map's own start-up scene kept away (the Junction's centipede)](#build-step-62-a-maps-own-start-up-scene-kept-away-the-junctions-centipede)
63. [Build step 63: the Dash without the Horn Slash](#build-step-63-the-dash-without-the-horn-slash)
64. [Build step 64: crystal berry #15 kept until taken](#build-step-64-crystal-berry-15-kept-until-taken)
65. [Build step 65: the Defiant Root inn's upstairs door open](#build-step-65-the-defiant-root-inns-upstairs-door-open)
66. [Build step 66: a give the game repeats, its check done, is the game's own (Morty's Bed Bug)](#build-step-66-a-give-the-game-repeats-its-check-done-is-the-games-own-mortys-bed-bug)
67. [Build step 67: what lies behind the Sand Castle Key and the Rusty Key held out](#build-step-67-what-lies-behind-the-sand-castle-key-and-the-rusty-key-held-out)
68. [Build step 68: the Ancient Castle boss room's wall open](#build-step-68-the-ancient-castle-boss-rooms-wall-open)
69. [Build step 69: the Honey Factory's door from Outside the Beehive open](#build-step-69-the-honey-factorys-door-from-outside-the-beehive-open)
70. [Build step 70: the Bee Kingdom's Throne Room door open](#build-step-70-the-bee-kingdoms-throne-room-door-open)
71. [Build step 71: Jaune's Gallery open from the start](#build-step-71-jaunes-gallery-open-from-the-start)
72. [Build step 72: the Scanner Room's gate open](#build-step-72-the-scanner-rooms-gate-open)
73. [Build step 73: the Scanner Room kept between the outside and the inside](#build-step-73-the-scanner-room-kept-between-the-outside-and-the-inside)
74. [Build step 74: the scan a location, flag 160 with it](#build-step-74-the-scan-a-location-flag-160-with-it)
75. [Build step 75: HB asks for the Explorer Permit from the start](#build-step-75-hb-asks-for-the-explorer-permit-from-the-start)
76. [Build step 76: Beette's Flower Key at its price again](#build-step-76-beettes-flower-key-at-its-price-again)
77. [Build step 77: the Honey Factory's storage door open](#build-step-77-the-honey-factorys-storage-door-open)

**How it works**

1. [The big picture: generator, seed, server, game](#1-the-big-picture-generator-seed-server-game)
2. [Opening the connection: a websocket](#2-opening-the-connection-a-websocket)
3. [Logging in: the packets, in order](#3-logging-in-the-packets-in-order)
4. [Sending checks: LocationChecks](#4-sending-checks-locationchecks)
5. [Receiving items: the index kept in the save](#5-receiving-items-the-index-kept-in-the-save)
6. [Finishing: telling the server the goal is done](#6-finishing-telling-the-server-the-goal-is-done)
7. [slot_data: the seed's settings, and this world's keys](#7-slot_data-the-seeds-settings-and-this-worlds-keys)
8. [The rule: use what Archipelago provides, never reinvent it](#8-the-rule-use-what-archipelago-provides-never-reinvent-it)
9. [How this mod does it: threads, config, custom key items](#9-how-this-mod-does-it-threads-config-custom-key-items)
10. [Silent failures: things that go wrong quietly](#10-silent-failures-things-that-go-wrong-quietly)
11. [The logic explained: regions, exits, rules, and this world's layout](#11-the-logic-explained-regions-exits-rules-and-this-worlds-layout)
12. [Universal Tracker: how it's implemented](#12-universal-tracker-how-its-implemented)

## Where it stands

Each step's own status is its last line (**Status:**). This section holds only what's next and what's known to
be wrong.

**Next** (decided from 2026-09-24 on; each item dated):

1. **Every key item and medal in the pool,** on logic that follows the vanilla story order: each chapter
   entered once the chapter before is finished and the story's own keys and abilities are in hand, its rooms
   split into regions as build step 24 describes. Medal gifts get a yaml on/off toggle, as medal shops have
   (*Shuffle Medal Shops*, build step 11).
   Shops (medal shops, item shops, the caravan): see build step 11. Other kinds of location (boss prize medals,
   placeholders, journal entries, enemy drops): see build step 10.
2. **Entrance randomizer (experimental):** every door, coupled or decoupled (build step 31), on Archipelago's own
   entrance randomizer, every map a region (2026-09-30, not yet seen in game); the one-way doors (the fog maze's
   wrong turns, drops) shuffled among themselves (2026-10-02, build step 38); next, sorting the other transfers into
   chosen and forced, the doors still fixed (Known issues), and the room-by-room logic that removes the label. See
   build step 12. **Every entrance shuffled, story transfers aside** (the user, 2026-10-07: "I preferably want every
   entrance to be randomized except maybe the story things"; "there has to be a really good reason for us to not
   randomize a specific entrance/door, they shouldn't just be disabled without me knowing about them or why"). A
   door is left out only for a reason the user has agreed to: so far chapter 3's story-only attack maps (2026-10-05)
   and the story transfers. Agreed but not built: the Golden Settlement's day and night copies, each pair one room
   (three rooms) whose exits shuffle like any door (build step 52's To do). Not looked at yet, for the door pass
   (`room-logic.md`): the Beehive's story copies (the Scanner Room kept between the outside and the inside, build step
   73: its made and re-pointed doors join the shuffle once `DoorShuffle` and the door data read `door_rows`),
   `TermiteIndustrial`'s in-map pair and the Sand Castle basement's two parked doors.
   **How each room gets mapped** (2026-09-27): the checklist in `room-logic.md`; the tester says what needs
   what, the agent turns it into areas and rules.
3. **Field abilities shuffled as items** (every learned ability built, build step 23) (by the game's names: Beemerang
   Halt, Bee Fly, Dash, Horn Dash, Beetle Dig, Icicle, Shield; `MEASURED.md`, every field ability). With *Starting Party
   Member* Off, party members stay where the story puts them. The three attacks and Jump as items: built, see build
   steps 21 and 22. Party members as items (*Starting Party Member*): built, see build step 18.
4. **Open world, one gate at a time** (always on, never an option; 2026-09-26): see build step 9.
5. **A two-player room** (planned 2026-09-25): the tester's slot plus a second one the agent drives. Seen
   (2026-09-26, build steps 18 and 19): items from another player arriving live, every class both ways, and the
   multiworld names ("You found Other's Key!", the mod guide's step 9). Replays after a new save or a reconnect are
   held up like any other item since 2026-09-28, the quiet start aside (the mod guide, step 9). *Item animation:
   Progression* holding up only progression items: seen 2026-10-04.
6. **A full bag:** never holds up the queue; a bag item that doesn't fit drops at the party's feet (build step 7):
   built, not yet seen.
7. **Goal:** the mod counts the seed's goal flags (`goal_flags`, guarded since build step 60) and sends "goal reached"
   at the required number: done, seen (build step 3).
8. **A release: three separate downloads** (2026-09-25): built, see build step 17; v0.1.0 out
   (2026-09-26), v0.2.0 (2026-09-27), v0.3.0 (2026-10-04). The next one: `dev-scripts/release.ps1 -Version vX.Y.Z`
   after bumping both versions. **When (the user, 2026-10-08):** once every room's logic is mapped, which makes the
   game fully playable; after that the quests, the enemies and the discoveries; past that, logic tweaks and maybe new
   features and options. So the root README carries no Status line, as the project only grows more complete.
9. **The chat feed**, then the in-game text client (see the design list in the mod guide, step 2), so players never
   need the Launcher's Text Client (2026-09-29: DeathLinks shown too, a filter per kind of message, hints and
   commands from the text line, Enter to type in the field and in battles, a Chat menu in the panel). It is the
   answer to Next 43, item 18.
10. **A "Quality of life" page in the Archipelago panel** (2026-09-25): on/off rows that speed the game
    up and make it smoother: skips first, others later. Battle tutorials skipped too, seen 2026-09-27 (the mod guide,
    step 10).
11. **Map fast travel, built (2026-09-26; the mod guide, step 10), seen travelling to the Outskirts** (planned
    2026-09-25), apart from the Warp to Start button. On the pause menu's map
    (window 6, which lists areas), pick an area you've been to and confirm (Yes / No) to travel to its save point
    through the game's own map transfer. The game already records visited areas (`librarystuff[4, area]`, set by
    `MainManager.UpdateArea`). The logic never counts on it, like the warp. **One row with the warp
    (2026-09-26):** the Warp button's on/off becomes *Travel: Off / Warp / Map / Both* (Warp to Start only, map fast
    travel only, or both), so the two are set together. **The look (2026-09-26):** alone, either button
    looks like the Warp button does now; with Both, the two get different background colours. The icons, decided and
    seen (2026-09-26, the mod guide's step 10): Map the round blue map, Warp the scroll; Both makes six buttons, 1.7
    apart, seen fitting in the pause menu. **The order:** both sit to
    the right of the game's buttons, Warp first, Map last. Left from the first button wraps round to Map for quick
    access, and Warp sits in between, so it's reached by accident less often. **How it's picked (2026-09-26):** on the
    pause menu's map, target a visited area and press confirm: a "Travel to \<area>?" Yes / No box (No first). Confirm
    flips an area's description pages today (`PauseMenu.cs:1407-1433`, with Z the other way, wrapping), so while map
    travel is on, Z alone flips pages; nothing is lost, as Z wraps round. The game's map: a free cursor (`sprites[0]`)
    snapping to the visited areas' markers (`sprites[area + 1]`), `option` the area. Each area needs a travel spot: a
    save point in it, from the entity dump and each map's area (`MapControl.areaid`, now in the map dump).
12. **A quest board in the starting house** (2026-09-25): the quests every board lists, taken without
    walking to the town or the bar. Every board shows the same list (`MEASURED.md`, "The quest board"), so it adds no
    quests, only a shorter way. Every board lists bounties too (built, build step 9); next, the house's board from
    the start. **Decided (2026-09-25): every quest on the board from a new file**, the quests themselves
    still to do, each with its own logic (reaching its NPC, what it needs), so quests can be done along the way. First
    the `QuestDump` (a Debug setting). **Dumped 2026-09-25** (`MEASURED.md`, "Every board quest, dumped"): no quest's
    accept flag does anything outside its own quest, so opening them all is safe for the save; the unlock conditions
    are story flags and visited areas, which the mod would skip by adding every quest to the open list on a new file
    (six are added only by a dialogue, not by the table). Each quest's logic (its NPC's map, which the table lists, and
    any items) is still to write, per quest, before its locations exist.
13. **Bounties as locations, a yaml toggle** (2026-09-25): *Shuffle Bounties*, its own category, off by
    default (five hard optional bosses; progression shouldn't sit behind them unless the player asks). Today they are
    not locations and pay their vanilla rewards. First, measure what each bounty pays and when (on the spot or on
    reporting back); then the logic for reaching each boss. Its own build step when built.
14. **Enemy shuffle** (2026-09-26): `enemies_only` built; bosses, `both`, `chaos` and the map look next.
    See build step 14.
15. **Enemy scaling, a panel setting** (2026-09-26): Off / Party level / Artifacts, Party level by default,
    balancing an area met earlier or later than vanilla would. Built, seen on screen (2026-09-26); a mod-side setting
    with no logic, so its design and status live in the mod guide, step 17.
16. **EXP multiplier, a panel setting** (2026-09-26): built and seen, `documentation.md` step 19. *EXP
    multiplier* on the Gameplay page, 1x to 10x (first planned as 1x-5x on Quality of life), default 1x: an opt-in for a
    faster, easier game. Levels still give HP, TP and MP, so it helps even without moves being shuffled. It stacks on
    top of enemy scaling's EXP. No check and no logic depend on it. Only while Archipelago is enabled, or with *Use on
    normal saves*.
17. **Berry multiplier, a panel setting** (2026-09-26): built and seen, `documentation.md` step 19. *Berry
    multiplier* on the Gameplay page, 1x to 10x, default 1x, the same opt-in. Only the berries picked up in the world
    (lying there or dropped after a fight), never a check's reward from the server. Only while Archipelago is enabled,
    or with *Use on normal saves*.
18. **Random start** (2026-09-26): `anywhere` built, experimental; *Save Points* and *Any Room* designed
    2026-09-30 (replacing the planned `towns`), to build. See build step 15.
19. **Traps, an idea for later** (2026-09-26; not planned yet). A trap sent to this game takes effect when
    the server delivers it, after any open text box, like any received item. Held up at pickup: its own icon on a red
    starburst. One icon per trap, so the player knows what's coming. Examples: a Mistake (the item) poisons
    the party at the start of the next fight (dropped, below: traps never harm); a crystal berry (or something icy)
    freezes the player in an ice block for 1-3 seconds. Each trap: only with Archipelago on, never a soft-lock (a freeze
    always ends, even in a scene), nothing written to the save the game wouldn't write, never in logic. The game's own
    effects to reuse (code read 2026-09-26, not yet measured): fight conditions (`MainManager.BattleCondition`: Poison,
    Freeze, Numb, Sleep, Inked, Sticky and more), map hazards (`Hazards.cs`, three `HazardAction` kinds, likely the
    knockback), falling off a map (put back at `lastpos`, `PlayerControl.cs:688-691`), and ice (`EntityControl.Freeze`,
    the ice block). **Traps annoy, never harm** (2026-09-28, after Celeste's flipped screen and Zelda's freeze and
    chickens): a trap never changes how a fight or a run goes, so no debuffs, no lost turns, nothing that can bring a
    Game Over (with DeathLink that would kill the whole room). Each wears off on its own: a few seconds or a timer on
    the overworld, cosmetic only in a fight. Ideas: frozen in an ice block for 1-3 s, reversed controls, a flipped
    camera, slippery movement (the game has no slippery floor, code read 2026-09-28: `EntityControl.inice`, set on ice
    maps, only gives a character its icy look, so it would be the mod's own), a silly look for the party in one fight. A
    lost turn (`EventStop`, `MEASURED.md`) was considered and dropped for this reason; the game's own conditions are
    never touched. First measure how each is applied. A yaml option (how many traps), so its own build step when built.
    **Frozen in an ice block, code read 2026-09-29 (not measured):** nothing in the game freezes the player on the
    field (`PlayerControl.frozencube` is declared, never used). Leif's ice freezes a map enemy through
    `NPCControl.freezecooldown` (300, 5 s), whose `Update` calls `EntityControl.Freeze()` (the ice cube) and later
    `BreakIce()`. `Freeze()` itself is safe on the player: its NPC-only lines are guarded and `CheckSpecialID` sizes a
    cube for every entity. The mod would bring the rest: its own timer and `BreakIce()` (which hops), the controls
    locked (`CancelAction()` first, as the dash ignores `lockkeys`), and a thaw at a battle start, a map transfer and a
    scene. A battle's Freeze (`SetCondition`, 2 turns on the party) costs turns, so it stays out, as above.
    **Disguised traps (the user, 2026-09-29):** on the ground the player can't tell their own trap from an item they
    want; a shop may tell (its description already says "A trap", `ClassWord`), and the pickup does.
    - *The player's own trap:* on the ground, a real progression or useful Bug Fables item not yet received, drawn
      exactly as a location holding it would be (`Describe`, `MarkColorOf`): its sprite and its class's backdrop, so
      the two always match ("a fake useful item with a purple background would be obvious"); with Item colors off the
      game's colour for its kind, with Item backgrounds off none. Picked up: red, its real look. A shelf shows it too.
    - *Another Bug Fables player's trap:* the trap item's own sprite on red, everywhere (the icon with *Archipelago
      icon: All players*), so you know it's a trap for someone else.
    - *Another game's trap:* the Archipelago icon on red, as today.

    **The look is always an item you don't have yet** ("you might want the hookshot so you will obviously try to go
    for the item until you have it"). The apworld picks, per trap location, an ordered list of wanted items with their
    class in that seed (a class can depend on options), with the seed's random, sent in `slot_data`; the mod shows the
    first not fully received (every copy, starting items included), the next once it arrives, and the last when
    nothing wanted is left. Fixed for the seed, never re-rolled on a visit, which would give it away. A fixed look can
    show a unique item you already own, which gives the trap away to anyone; this one only to a player who remembers
    what that spot showed. Only the two ground paths change (`ItemSwap.TickGround`, `ItemSwap.Redraws`); shelves and the
    pickup keep the real look. Scouting creates no hints (`HintCreationPolicy.None`); a hint the player asks for tells
    the truth, as in every game. **A yaml option, OoT's name and values:** *Trap Appearance*: Major Only (default:
    the wanted items above) / Junk Only / Anything, as OoT's and CV64's `ice_trap_appearance` (OoT picks per trap
    location at generation, from its own pool: `worlds/oot/Options.py:1130-1135`, `__init__.py:1081-1086`, 0.6.7).
    No Archipelago doc forbids hiding a trap before pickup; hints always show it, marked "avoid"
    (`network protocol.md:389`). Built with the traps, in their build step. Open: the `slot_data` shape (Next 43,
    item 15).

20. **Enemy group sizes, a yaml option** (2026-09-26): its own option, apart from *Enemy Shuffle*, off by
    default, for example `vanilla / shuffled / random` (fights of any size swap places; or 1-4 enemies rolled per
    fight). The game takes any number of ids: 4 on the field, the rest in reserve (`BattleControl.cs:787-800`). Only
    ordinary map fights change size. A boss or special fight is one unit of several slots (the Sand Wyrm's head and
    tail, Mother Chomper with two Fly Traps, the Wasp General's squad, Zasp and Mothiva, Cenn and Pisci, Stratos and
    Delilah, Maki's team; the rematch machine's switch lists them) and always moves whole, as the shuffle already
    moves whole id lists; a fight is never two bosses, nor a boss mixed with ordinary enemies. Built after enemy
    scaling, so a bigger group stays fair. **Summoners mostly guard themselves** (code read 2026-09-26, 24 `SummonEnemy`
    calls): the ordinary ones only summon when alone or nearly (Burglar, Wasp Healer, Leafbug Archer, Bloatshroom,
    Chomper Brute alone; Leafbug Ninja under 3), bosses too (Bee Boss and Mother Chomper alone; Pitcher and Seedling
    King under 3; Midge Broodmother with a free spot). Only boss-internal parts have no count check (Venus's plants,
    Pisci's add, the Sand Wyrm's tail, the Everlasting King's tablets), and boss units move whole. So a bigger ordinary
    group mostly just stops a summoner summoning, as the game itself does. Still a guard before building: each branch
    read, and one full-field fight. Its own build step when built.

21. **Boat Ticket** (Discord, 2026-09-26): built, see build step 16. Since 2026-09-30, with *Progressive Boat* on (the
    default), the Progressive Boat's first copy, the submarine its second (Next 51, build step 36).
22. **Healing save crystals, an idea for later** (suggested on Discord; 2026-09-26): an item that makes the
    blue save crystals (save only) act like the yellow ones (save and heal), a nice filler or useful check. The colour
    is not baked into the art (code read, 2026-09-26): a save point is tinted in code from its entity data, yellow when
    `data[2] == 0`, red when `data[1] >= 10` (`NPCControl.cs:1190-1217`), so the mod could turn every blue crystal
    yellow by setting its data before the map builds it, as the enemy look test does. **Built as a panel setting
    instead (2026-09-28):** *Healing crystals* on the Gameplay page, `documentation.md` step 30; the heal is
    in the crystal's hit, not the prompt (`MEASURED.md`, save crystals).

23. **Progressive items** (2026-09-26; built, build step 23): items that unlock in a fixed order however they're
    found, as Pseudoregalia's progressive sword (three copies of one item; the first gives the sword, the second
    breaking blocks, the third the ranged attack). Archipelago counts copies of one item (`Has(item, count)`), so the
    logic is simple. Candidates: each member's field abilities in their game order, and other chains; decided when
    abilities become items (Next 3, build step 13).
    **Decided (2026-09-27): three progressive items, always, never an option, each *progression*** (every
    level unlocks checks): Vi's *Beemerang Toss* then *Beemerang Halt*; Kabbu's *Dash* then *Horn Dash*; Leif's
    *Freeze* then *Icicle* (the game's names and flags, `MEASURED.md`, every field ability). Why always: a second level
    without the first wouldn't work; in the game the Halt, the Horn Dash and the Icicle each extend the first
    (the Halt holds a thrown Beemerang, the Icicle is a second tap during the Freeze, the Horn Dash changes the Dash).
    With Shuffle Field Moves (build step 21), the Beemerang and Freeze items become the progressive ones' first copy.
    **The Dash without the Horn Slash (decided 2026-09-27):** the Dash starts as a second tap
    during the Horn Slash (`PlayerControl.cs:1083`), and its hitbox carries the slash's own tag (`BeetleHorn`, `:1139`),
    so today it cuts grass, pushes rocks and hits switches like the slash (grass takes `BeetleHorn` or the Horn Dash's
    `BeetleDash`, `NPCControl.cs:4764`). Proposed: with the Horn Slash locked, a double tap still starts the Dash, the
    first tap doing no slash and the dash's hitbox no horn effect, as the Horn Dash adds rock breaking to it; the Horn
    Dash then breaks rocks but leaves grass and the rest to the Horn Slash. The logic: grass and pushing need the Horn
    Slash, speed the Dash, rocks the Horn Dash; no progressive item needs another. **With the Horn Slash received the
    Dash is the game's own again**: it cuts grass and does everything the slash does, the Horn Dash too. **Built
    (2026-10-09): build step 63.**
    **Why it suits the logic:** grass is always "Horn Slash", never "Horn Slash or Dash"; the grass rules
    (`CanUse("Horn Slash")`) stayed right once the Dash became an item.
    **Every learned ability an item, always (2026-09-27):** Beemerang Halt, Bee Fly, Dash, Horn Dash, Beetle
    Dig, Icicle and Shield are always in the pool, not behind an option ("randomizing things the player would have
    found"); the three starting moves stay under Shuffle Field Moves ("removing things"). **Each unlock scene a location
    now** (as the party members' joining spots, no temporary double grant to forget), with a story-order rule
    until chapters 2-7 get room-level logic: each needs every ability learned before it, chapter 1 done, and the members
    and moves when those are items (more cautious than the game). **How (proposed):** the scene runs untouched and
    still sets its flag, which is the check; using the ability follows the item, the game's ability checks answered
    from the received items, never by writing a story flag. Its own build step.
24. **The panel's settings on normal saves** (2026-09-26; built, `documentation.md` step 18): an opt-in row so Quality
    of life and Gameplay also apply with Archipelago off. A deliberate exception to "vanilla stays vanilla", which only
    the project owner can make; off by default. **Named: *Use on normal saves*, ON / OFF**, help line "Quality of life
    and Gameplay also apply with Archipelago off." Only the two pages' settings; nothing tied to a seed (items, checks,
    the shuffles).

25. **Consumable keys, an idea for later** (2026-09-26): custom items used up on a door, as the game's own
    `removeitem` takes an item (`items[kind].Remove(id)`). The rule the crystal berries set (build step 11): no action
    may make a check unreachable, so a key either opens one named door, or the keys and the doors that take them are
    exactly as many, with no door that could waste one.
26. **Enemy stats randomized, planned for later** (2026-09-27): an option Off / Enemies / Bosses / Both that
    changes HP, defence and EXP per enemy type. Decided at generation (`slot_data`, per enemy id), applied after enemy
    scaling as a fixed multiplier, so the two stack. Defence is the risk: damage is attack minus defence, so a raised
    defence can make an early enemy unhurtable; HP and EXP take a wide range (about x0.5 to x2), defence at most +-1 and
    never above what a fight at that point can get through, bosses especially.
27. **Enemy attacks randomized: not planned** (asked about, 2026-09-27): each enemy's attacks are written for that
    enemy in `BattleControl.DoAction` (its own animation numbers, shape, positions, summons), so another enemy's attack
    would miss animations or wait forever on one and soft-lock the battle. At most, later, swaps between enemies built
    alike, tested one by one.
28. **Artifacts shuffled, an idea for later** (2026-09-27): the goal's artifacts found anywhere, not only at
    the chapter ends. Each chapter end (its artifact flag) becomes an ordinary location, and an Archipelago-only
    *Artifact* item goes into the pool; the goal counts Artifacts received. The item never sets the game's artifact
    flags: they drive the story (flag 41 is the logic's "Snakemouth Den Cleared"). **Decided (2026-09-27):**
    the pause menu's icons show Artifacts received (drawn only; the save's own count, which the file select shows, stays
    the game's). **Any order:** seven distinct items, *Artifact 1* to *7*, each drawn with its own icon, so
    Artifact 4 can come before 1; the goal is any N of them. The game draws the first N of `StartMenu.psprite` from a
    flag count (`PauseMenu.cs:2397-2402`), so the mod draws the received ones itself. Each item is tied to its chapter:
    its chapter's icon, and the game's own name for that chapter's artifact if it has one (`textsearch`);
    the chapter-end locations keep place names. **Icons:** the received ones only; all 7 with the missing
    ones faded is worth a look on screen when it's built, kept only if it looks good. The pool: always 7, *Artifacts
    Required* 1-7, so the game's 7 icons can show it (agreed); a bigger pool breaks nothing in Archipelago but needs a
    filler slot per Artifact and a display past 7 icons, so later if asked. A yaml
    option, so its own build step. **The option (the user, 2026-10-09):** *Artifact Shuffle*, two choices: off, each
    artifact where vanilla gives it (today's), or on, the seven anywhere in the multiworld; no mode shuffling them only
    among the chapter ends (Pokémon Crystal's `randomize_badges` has one, read 2026-10-09, `licensing.md`). Until it's
    built, the pause menu draws each set artifact flag's own icon (the mod guide, step 48). **Names so far** (each the
    user's yes): chapter 4's, in the castle's treasure room on the central pedestal of a shrine between three statues,
    "Ancient Castle: Treasure Room, Shrine" (2026-10-09).
29. **A bosses goal, an idea for later** (2026-09-27): a *Goal* option (Artifacts / Bosses) and *Bosses
    Required* (all, or a number): each boss's beaten flag an event the goal counts, as artifacts are today. **Decided
    (2026-09-27): story bosses only**; the bounties maybe a side setting later, once *Shuffle Bounties* gives
    them logic (Next 13). Only a real
    choice once the world reaches past chapter 1 (today: one artifact, one story boss). Its own build step.
    **Several goals at once, asked 2026-09-29, as Super Metroid's objectives** (0.6.7, read the same day): SM's
    `objective` (up to 5 of about 40 goals) or `custom_objective` (N picked at random), every one selected required
    before the last boss (`worlds/sm/Options.py:318-370`); Satisfactory's `GoalSelection` with `GoalRequirement`, any
    one or all (`worlds/satisfactory/Options.py:468-499`); Hollow Knight's `Goal: any`, whose rule requires logical
    access to every goal (`worlds/hk/__init__.py:497-501`). Still to choose: all selected (SM's way) or any one (then
    the logic needs them all, as HK). Either way one `completion_condition`, the mod's `CLIENT_GOAL`, and every chosen
    goal reachable in logic. Goals so far: artifacts (built) and story bosses (this item); a count of Tattle entries
    (Next 44) would fit the same way (proposed).
30. **Library discovery milestones as locations, an idea for later** (2026-09-27): the librarian's 10
    rewards (one per 5 discoveries, 5 to 50; `MEASURED.md`, "Journal rewards") as locations, "done" when `flagvar[53]`
    reaches 1 ... 10 (the `location_vars` kind, as Artis's prize). Rule: milestone k needs 5 × k discoveries reachable,
    so only the first fits today's 5 listed discoveries. Five of the rewards are crystal berries (43-47). Still to find:
    the library's region and any story flag the librarian needs; how the mod turns `EventControl.GiveItem` into a check.
31. **The Explorer Permit split, an idea for later** (2026-09-27): one permit per gate, so one item never
    opens four areas (as custom roadblocks spread Surf's reach in Pokémon Emerald's apworld). The gates (`MEASURED.md`,
    "What the Explorer Permit opens"): the Outskirts gate, the Rubber Prison's `PrisonDoor` (the locked-door routine's
    list, index 16), and B.O.S.S. and the Cave of Trials (the wiki's word; which item they take is measured in game as
    the first part of this step, before they're gated). The names: Snakemouth, Prison, Lab and Trial Permit; proposed:
    the Explorer Permit stays the Snakemouth one (the game's own gate and lines), plus three of the mod's own items as
    the Boat Ticket was made (build step 16). **Proposed (asked for vanilla kept optional):** a yaml choice *Explorer
    Permit*: Vanilla (one permit, every permit gate behind it, the mod leaves the gates alone), Split (the three new
    permits in the pool, each gate checking its own); only these two ("either 4 permits, or 1 permit vanilla"). Through
    `slot_data`, so the gates change only in a Split seed; the new items always exist in the item table (Archipelago's
    names are fixed) and enter the pool only with Split, each taking a filler slot as the ticket does. **Default:
    Split** (2026-09-27). Its own build step.
32. **Key items shown without browsing, a Quality of life row, an idea for later** (2026-09-27): at a
    key-item prompt, the mod asks "Show the \<item>?" (Yes / No) when you have the item it takes, and says you don't
    otherwise, as the Boat Ticket's sailor does, instead of the game's list to pick from. Every key-item prompt, not
    only the permits. Its own step in the mod guide.
33. **Sprint, an idea for later** (2026-09-27; code read, not measured). Decided so far:
    - **The button:** the HUD key (key 7, the Y button's "drop down"), on the overworld only. Its only field use
      (`PlayerControl.GetInput`) shows the HUD (HP, TP, berries) for 300 frames or hides it; little is lost, since the
      HUD shows itself when the player stands still. Its uses in the pause menu, the shop list
      (`MainManager`) and one battle spot (`BattleControl`) stay. Keys can be rebound, so the mod follows the key, not
      "Y". **In battle** the key only shows or hides the EXP bar (2026-09-27, seen on screen): little lost there too,
      if a later feature ever needs a battle button.
    - **A toggle, not a hold:** each press turns the sprint on or off, so a controller needs no held button.
    - **Whoever leads, the same for all three** (only the leader is controlled; the other two follow). A sprint is
      faster walking: it stops when the stick is let go and steers like walking. The game's dash keeps moving by itself,
      stops at a wall and turns slowly (`DashBehavior` eases toward the stick by 2.5% a frame; remembered as
      hard to steer).
    - **Kabbu's Dash stays the game's ability and a gate** (flag 699, and its upgrade the Horn Dash, flag 39; Next 3,
      the Dash as an item), with everything it breaks: that is its hitbox (`tbox`, tagged `BeetleHorn`, or `BeetleDash`
      with the Horn Dash, that
      `NPCControl`, `Hornable` and `ShakeHorn` react to), which the sprint never has. The dash starts as a second tap of
      the horn slash within 15 frames (`DoActionTap`, case 1); every leader's tap and hold is taken, hence the HUD key.
    - **Sprint and dash stack:** with the sprint on, the dash goes faster too (fun, and the toggle turns it off). Speed
      comes from `PlayerControl.basespeed` (5) in `RefreshSpeed`: walking `(basespeed + friction) × 1.3`, dashing
      `basespeed × 2.5`, the submarine `basespeed / 1.8`.
    - **Poses for Vi and Leif:** looked for on screen first (a dev command stepping through `animstate` numbers), the
      normal walk made faster if none fits. Kabbu's dash poses are 116/117, but each member's numbers mean different
      poses (battle sets 116 on `playerdata[2]`).
    - **A yaml option, its own build step:** *Sprint*: Start With (default) / Shuffled (an item; the button does
      nothing until it arrives) / Off (the HUD key keeps the game's own use, for players who don't want it). The
      speeds are a panel setting (taste, no logic).

    Still open: the speed values; whether it stays on across maps, fights and scenes (proposed: on until pressed
    again); feedback on a press (proposed: the HUD's own down/up sounds); that Y is key 7 by default on a controller;
    whether the followers keep up. **Before it ships:** a faster run or dash may jump further. If it reaches even one
    location the logic thinks locked, the item is progression and that reach goes into the logic; if not, useful.
34. **Early Jump, an idea for later** (2026-09-27): *Shuffle Jump* (build step 22) becomes Off / On /
    Early, where Early puts Jump in an early sphere, since it gates the most. Archipelago may already have this built in
    (an early-items setting): check `world api.md` at the targeted tag before building one of ours.
35. **Skip cutscenes and Speed up cutscenes, two rows, an idea for later** (2026-09-27). Today one row,
    *Skip cutscenes* (`QualityOfLife.cs`, `Scenes`; the mod guide, step 10), both skips and fast-forwards (8x speed,
    lines answered, battles at normal speed), chosen per scene by what is safe. The idea splits it by what a scene is:
    - **Skip:** scenes with no mechanic or check tied to them. Off: they play as in vanilla.
    - **Speed up:** story beats, scenes that change the world, and scenes that give an item or reward. Off: they play
      as in vanilla, at normal speed.
    - **Always, with Archipelago on, whatever the two rows say:** what the randomizer needs gone, or scripted parts
      that only slow the pace: the opening (the intro slides, the new-game combat tutorial) and the scripted first
      spider fight. **Also always (2026-09-27):** every tutorial and scripted scene, and the "can't go this
      way yet" scenes that turn the player back. Those are gates, so each goes through the open world (build step 9):
      cut only in the same change that fixes its gate and logic, never on its own (2026-09-27).

    How a scene is cut (skipped or fast-forwarded) stays per scene: a Skip scene that can't be cut out safely is sped
    up instead. **A scene the player should see happen is sped up, never cut out** (2026-09-27: the bridge
    falling, the fall through the trapdoor, the spider scene; a cut looks wrong, as the trapdoor's skip showed: just a
    teleport). A scene that only talks is cut out: without it the player just walks past, as normal (example: arriving
    at Snakemouth Den). **In short:** something happens or a check = Speed up; only dialogue = Skip;
    tutorials, scripted parts and turn-backs = always. A guide, not a shortcut: every scene is still read and sorted by
    hand. **Sorted (2026-09-27):**

    | Scene | Today | Decided |
    |---|---|---|
    | The opening: slides (Event8), Maki's talk, Vi joining, the tutorial battle (Event16) | always | always |
    | The first spider fight, ended at its start (`BeforeCheckEvent`) | *Skip cutscenes* | always |
    | The bridge message (Event0) | skipped | Skip |
    | The Tattle tutorial (Event2) | skipped | always (a tutorial) |
    | The barkeeper's first talk (Event83) | skipped | Skip |
    | Arriving outside Snakemouth Den (Event11) | skipped | Skip (the skip records discovery 0 itself) |
    | The rope (Event1) | fast-forwarded | Speed up (the bridge falls) |
    | The door room's puzzle solved (Event4, drops a Mushroom) | fast-forwarded | Speed up |
    | The trapdoor (Event5) | fast-forwarded | Speed up |
    | The spider scene (Event6) | fast-forwarded | Speed up |

    The spider scene is one game coroutine, played as scene, scripted fight, scene, second fight, scene: talk, the first
    fight (Kabbu alone, `flagvar[11]` 0), talk, the second fight (two enemies, `flagvar[11]` 2, a real one), then Leif's
    part, flags 30 and 27 and discovery 1. Its first fight ends at once always; the rest is sped up. Defaults proposed:
    both rows on. Only while Archipelago is enabled, as every row. A panel setting, so its own step in the mod guide.
36. **We Owe Ya! does something from the start, an idea for later** (2026-09-27). Today the medal calls a
    random helper only from those the story or a side quest has unlocked, so received early it does nothing (a tester
    saw it; `MEASURED.md`, We Owe Ya!'s helpers). The idea: with Archipelago on, the medal picks from every helper.
    Build it by changing only the medal's pick at a battle's start, **never by setting the helpers' flags**: those are
    story and quest state. Still to decide: every helper, or a set. It changes how a seed plays, so its own step.
    Until then the game page tells players (`docs/en_Bug Fables.md`).
37. **Attack boost, a panel setting** (2026-09-28): built, shown on the medals screen (seen), `documentation.md`
    step 27. *Attack boost* on the Gameplay page, Off / +1, off by default: +1 on each hit a party member lands, an
    opt-in for a hard fight. No check and no logic depend on it. Only while Archipelago is enabled, or with *Use on
    normal saves*.
38. **A Graphics page, render scale and MSAA** (2026-09-28): built, seen, then removed the same day (240 to
    about 95 fps for little visible gain), `documentation.md` step 28.
39. **DeathLink** (2026-09-28): built, not yet seen, build step 25. A row in the Archipelago panel on the
    main menu, not a yaml option, so it can be switched mid-seed by going back to the main menu. **Auto-save between
    rooms**, its own Gameplay row, built, not yet seen (`documentation.md` step 31), so a death costs one room rather
    than a long way back.
40. **The boat to Metal Island, always there in a seed** (2026-09-28, playing vanilla): chapter 6's story
    takes the boat away (wasps attack it). In a seed the pier's boat should always be available, since the logic
    (build step 16) gates Metal Island on the Boat Ticket and the pier only. Until then the logic is less cautious than
    the game past that scene. First step: read the scene and the flag that removes the boat, and whether a seed can
    reach it. Not built; nothing changed while the tester plays vanilla. **The submarine never replaces the boat
    (decided by the user, 2026-10-04,** after seeing both docked at the Bugaria pier and at the far end): first planned
    to take the boat away once the submarine arrives, but "both fit/work together without overlapping so you can still
    use both"; the boat stays as a feature, never a boat flag cleared for the submarine. That is what the mod does
    today (the docks follow the submarine in the bag, build step 36; nothing touches the boat).
41. **A "!" over each unrecorded discovery, with the Detector on** (2026-09-28): today the Detector puts the
    game's "!" over the leader and beeps when a room has a check left (the mod guide, step 14); this shows where. The
    "!" is the game's emoticon (`EntityControl.emoticonid`, held with `emoticoncooldown`, and `alwaysemoticon` exists).
    **Only the ones you interact with** (2026-09-28): a stone to read, a statue to look at (the pier statue,
    `HiddenEvent`, the `AncientHouseDiscovery` grass), so the player knows to walk up to it. The ones recorded by just
    being there (arriving outside Snakemouth, the fall room's scene) need none, so no "!" of the mod's own at a spot.
    **Dig spots too** (2026-09-28): an undug one holding a check. A dig spot buries an item, a crystal berry
    or an event (`MEASURED.md`, dig spots); 12 hold plain berries, no check today (one dug up on screen). **Those as
    checks** (an idea): real locations, a new kind (its own build step, likely a yaml option); first measure whether
    they come back on re-entering (no one-time flag, likely), since a check needs a lasting record the save already
    has a place for. Not built.
42. **Playing the logic with the Logic Test apworld** (2026-09-29): installed in the Archipelago checkout, its
    copy of our seed checked (`logic-test-check.py`, with the tests), and a hosted seed played through by a script
    without the game, every sphere's items arriving (`development.md`, "Play-testing the logic"). Next: the game
    connects to such a room (its data package is 2.18 MB), then a seed played through. It matters most for the
    experimental options (build steps 12 and 15).
43. **Archipelago's way, everywhere: the full review** (2026-09-29, after the rule in How it works §8). Everything
    Archipelago publishes for a world and a client, read against the project: every doc (0.6.7, diffed against
    `main`), the generic player guides, APQuest and MultiClient.Net's docs. The evidence for each item is in
    [archipelago-review.md](archipelago-review.md), same numbers. Each is a step of its own, in this order:
     - **Bugs:** 1. the shop fallback: its two bugs fixed 2026-10-03 (build step 11), priority and plando on a shop
       still in Known issues; 2. failed connect attempts left open, one more client on the
       slot per retry (Known issues); 3. two items named "Leif" (Known issues); 4. a shop test that can't fail;
       5. respawning checks leaving the outbox before the server confirms them.
     - **Required:** 6. the door shuffle in `connect_entrances`: done 2026-09-30 (build step 12). **Recommended,
       the style guide:** 7. `style.md` (brackets, a trailing blank line, long Markdown lines).
     - **The apworld and the website:** 8. option groups (the first, "Aesthetic Options", came with build step 33),
       presets, reST option texts with rich text, a bug report
       page, the WebWorld's `game`; 9. `topology_present`; 10. location and item groups (the first item groups,
       *Submarine* and *Boat*, came with build step 36); 11. `World.world_version` (done 2026-10-08: slot_data and
       Universal Tracker's check read core's copy of the manifest's version, How it works 7),
       `Region.add_locations`, `options.as_dict` (done 2026-10-03, build step 39:
       slot_data's `options`); 12. `start_inventory_from_pool`; 13. the Rule Builder's `OptionFilter` for Jump (done
       2026-10-03, build step 42), `__str__` and `@override` on our rules, a caching benchmark; 14. Universal Tracker
       and PopTracker (Universal Tracker with no yaml built 2026-10-03, build step 40; its deferred entrances
       2026-10-08, build step 55; the mod's data storage keys, build steps 55 and 57; its map tab waits for the
       PopTracker pack's map; the pack started 2026-10-03, build step 42); 15. slot_data only
       what's necessary (decided 2026-09-29: the fixed tables built into the mod from the apworld's data, the seed's
       locations from the server, a world-version check on connect).
     - **Tests:** 16. the base in `test/bases.py` and Archipelago's generic tests in CI (the generic tests done
       2026-10-08: `AP_TEST_WORLDS=bug_fables`, new in 0.6.8, build step 17); 17. test hygiene (no repeated
       default runs, plain `TestCase` where no multiworld is used, options written out, `assertAccessDependency`).
     - **The client:** 18. room messages shown in game: the in-game text client (Next 9), the Launcher's Text
       Client named in the player docs until then; 19. the Connect
       packet (a kept `uuid`, the right version, DeathLink's tag, hooks before connecting); 20. the rest (a refusal
       with no codes, `InvalidPacket`, the library's `SetGoalAchieved`, `AllLocations`, `GetRaceModeAsync`,
       `ColorUtils` and Analyzers, `ClientPlaying`).
     - **Docs and process:** 21. the player docs against Archipelago's own guides; 22. `development.md` (Python and a
       venv, `--log_network`, a local WebHost preview, `/send_location`, the world maintainer's duties).
     - **With the room mapping:** 23. Archipelago's entrance randomizer in place of `doors.py`: done 2026-09-30
       (build step 12), ahead of the room mapping, on one region per map.
     - **A second look at what we kept:** 24. the enemy shuffle stays in `generate_early` (reversed 2026-09-30, the
       user: it is logic, since fights that can't be fled and Tattle checks depend on it); 25. items through the
       library's queue (to check first); 26. Archipelago's DeathLink yaml option, the panel switch kept too (decided
       2026-09-29); 27. the library's cache bug: reported by the user as MultiClient.Net #143, our patch until a fix;
       28. upstream #141, which would retire our compression switch once released (#142 doesn't cover our net40 build).
     - **Kept** (Archipelago has nothing for them) **and doesn't apply** (with why): on the review page.
44. **Shuffle Bestiary: Tattle checks, an idea for later** (asked again 2026-09-29, parked since 2026-09-25 in build
    step 10): each enemy spied a location, not just fought, as Pokémon Emerald's apworld's dexsanity needs a catch. The
    bestiary has 92 entries (`librarylimit[1]`); the check is `librarystuff[1, id]` turning true, read as discoveries
    are (`location_discoveries`). **Spy, then a death (code read 2026-09-29):** the entry is written to memory as the
    Tattle text closes (`BattleControl.Tattle` calls `UpdateJounal(Bestiary)`, which in a battle sets
    `librarystuff[1, id]` at once), and `LocationChecks` runs in battles too, so while connected the check goes out
    before the fight ends and stays done whatever follows. Retry keeps the entry as well (`GameOver` restores flags, not
    the bestiary); Reload save loses it unless saved, which matters only if the check wasn't sent (disconnected): spy
    again. To measure first: what allows Spy in a battle
    (`disablespy` in the tutorial fights, a flag?), a place each enemy is always fought (following the enemy shuffle's
    `enemy_swaps`), bosses spied only in their own fight, the 23 missable ids (Event65's `excludeids`), and the
    auto-spy row (build step 10). A yaml option, so its own build step when built.
45. **Room Swap (experimental)** (2026-09-29): whole rooms trade places with rooms of as many doors, a value of the
    entrance randomizer. Built, not yet seen in game; see build step 30. Next: the user plays a seed with it; later,
    doors matched by side.
46. **Every optional feature Archipelago offers** (2026-09-30, the user: "we should try to support all available
    things archipelago has/does, that includes plando"): connection plando built, not yet seen in game (2026-09-30,
    build step 32). Next, a sweep of the
    rest, each read in Archipelago's own code and guides before it's built or written off: item plando proven on a seed,
    the options Archipelago provides for a world to add (`Options.py`), and boss plando once bosses are shuffled.
47. **Music Shuffle** (2026-09-30, the user: in the yaml): built, area music seen (2026-10-04), build step 33. Next,
    **Sound Effect Shuffle** (`sfx_shuffle`, smw's name), its own step. It widens the same `PlaySound` and `StopSound`
    hooks to every sound (dialogue bleeps out), and also swaps `SoundIsPlaying`, the entity sounds and
    `PlayClipAtPoint`. A loop stopped by name (`Rumble`, 21 times) must stop the sound that replaced it.
48. **Shuffle Shop Inventories** (2026-09-30, the user: what shops restock and respawning items come back with,
    randomized, never checks): built, a shop's restocks seen (2026-10-04), build step 34. Next, **the
    game's other item shops and respawning pickups** join the pool, as they become locations or as spots of their own
    (measured first: each keeper's `data`, each item with only a regional flag).
49. **Filler Starting Checks** (2026-09-30, the user: the items a new file gets on connecting are filler only): built,
    seen in game (2026-10-04), build step 35.
50. **Fights with the party the seed has** (2026-09-30, the user: stand-ins in battle, the members present playing the
    parts; and scripted fights kept whole under enemy scaling): built, not yet seen in game. The mod guide's step 11
    (a member's number used as a slot: the battle start's leader, the eaten tick, skills, scenes placing a missing
    slot), step 36 (the spider's line, the Beast, Zommoth, the Everlasting King cast from the party) and step 17 (the
    10-HP scripted end, the fixed numbers in enemy scripts). Next, the user sees each on screen; the log's install lines
    first (`[party] installed in …`, `[scale] installed in …`, every count matched).
51. **The submarine as an item, the game's Surf** (2026-09-30, the user: one item that opens much of the world, as
    Surf does in Pokémon Emerald): the *Progressive Boat*, the Boat Ticket then the Subaquatic Maritime Neotransport,
    or with the yaml option *Progressive Boat* off the two apart; the docks there only with it. Built (build step 36,
    the mod guide's step 37), not yet seen in game. Next, the user sees it: the install lines, the item's look (its
    sprite is lent until one is picked on screen), both copies arriving, the docks before and after, a crossing to each.
52. **One world, one shape: chapter flags suspended** (the user, 2026-09-30: "The world should always be and stay in 1
    consistent state for the player, not randomly unlocking/locking with flags changing roadblocks. Story/cutscenes are
    fine to keep where it makes sense, but the game should be openworld and not linear"). **Deferred until the room
    logic covers every area**, so nothing is hidden or changed before its logic exists (the user). The game has no
    chapter number: a chapter is story flags, its end the artifact flag (41, 88, 299, 345, 347, 346, 555) and its start
    a title-card scene's flags (`MEASURED.md`, Chapters). A flag reaches the map through every entity's `requires` and
    `limit` (`CheckIfCanExist`), scenery (`ConditionChecker`, `FlagAnimation`), dialogue lines, map music, auto-start
    scenes, flags written on a map's load (`MapControl.cs:630-707`) and shop stock. **The method:** room-logic.md's
    planned flag cross-reference (doors only today, `gate-table.py`) widened to every reader of each chapter flag, then
    each effect sorted: a roadblock that appears or goes away is held in one state by build step 9's lists, never by
    setting or clearing the flag (build step 9 rules that out); a scene is kept where it makes sense, or held; a
    chapter's shop stock and NPC moves are listed and decided with the user. With the world's shape held by the seed, a
    death's reload only replays scenes (build step 25). Its own build step when built.
53. **Bosses the server remembers** (the user's idea, 2026-09-30: "tie fake checks to bosses to track if they have been
    defeated etc. so you won't have to re defeat bosses later"). A story boss beaten once stays beaten in every save,
    even after a death loads one from before the fight: kept out of the map the way checked pickups are (build step
    27), never by writing its flag; the flags its scene sets are Next 52's to answer. Two ways Archipelago provides, to
    choose when built: **(a)** each boss a real location holding an item from the pool (`world api.md`: a location may
    be a boss drop), a yaml category of its own, which also adds early locations (the fill error's fix, Known issues)
    and gives Next 29's bosses goal its record; **(b)** the slot's data storage, for data "just saved for later"
    (`network protocol.md`, `Set`). Not a location holding nothing: a location with an id holds an item, and one with
    none is an event, which the server never hears of (`world api.md`, events). **Read (2026-09-30):** Super Metroid's
    apworld (0.6.7, `worlds/sm/__init__.py:186-200`) makes its bosses events, a locked "Boss" item with
    `address = None`, logic only; our "First Boss Beaten" is the same. Its own build step when built.
54. **Points of No Return** (2026-09-30, the user: the Warp counted as the way back from a one-way): built, build step
    37 (the mod guide's step 38); it changes no seed until rooms are mapped (Next 2). Next, the user sees a seed with it
    on and Travel Off, the Warp still in the pause menu.
55. **Spy Specs, a panel setting** (2026-09-30, the user's idea): built, the mod guide's step 39, a Quality of life
    row, off by default; since 2026-10-04 in halves (Off, HP, Free, Both; Both is the medal). No check and no logic
    depend on it. Seen (2026-10-04): the row and HP's, Free's and Off's battle effects; Both waived by the user.
56. **Every quest available from the start** (2026-10-04, the user): one quest at a time, the lost kid's first (its
    reward, location 10, held back until then); Madeleine's house then locked as in the game, her two quests
    opened. See build step 44.
57. **Ant tunnel tickets** (a possible future plan, 2026-10-04, the user: "log down B as a possible future plan"): a
    yaml option, beside build step 45's free miners, where each tunnel shortcut opens from an item instead (one ticket
    per tunnel, six, or one progressive ticket), like the Boat Ticket. Each ticket gates its tunnel, so each is
    progression; each needs a name (the user's) and an icon. Not decided. Since Next 63 (2026-10-08) the choice it
    would offer is the miners' vanilla prices (behind the berry rule) or the tickets.
58. **Enemysanity** (an idea, 2026-10-04, the user: "each enemy, not boss, drops an item/has a location ... would also
    allow people to see what item the enemy has before fighting it"): the game's own held-key drop works for any map
    enemy (`MEASURED.md`, a map enemy's drops), so each could carry a guaranteed, flagged drop the pickup swap shows,
    held visibly before the fight (the mod drawing it for most enemies). **Findings the same day:** only 37 story flags
    are free, so no flag per enemy; instead the respawning pickups' flag-less path (check sent on pickup, done-ness
    from the server): each enemy a location keyed `map:entity` (as Enemy Shuffle keys them), a drop added on a won
    fight while its check isn't done, the swap showing the seed's item, the enemy respawning as ever. The two held keys
    are plain pickups to the mod already (`MEASURED.md`). The drop made with the game's public `CreateItem`; 255 enemies
    always there, 25 the story removes, 27 it adds later. **Nothing may be missable** (the user: "a location always have
    to be present/reachable ... should never have anything be missable"): each of the 25 is kept present past its
    flag, or, where that would break a scene, left out of the seed. Open: the option, the held sprite for most kinds,
    the location count. Not decided.
59. **Include Story Rooms** (decided 2026-10-04, the user; to build): a yaml option (`include_story_rooms`) for the
    entrance randomizer, **off by default until the out-of-order scenes are handled** (Known issues), then the default
    may flip. Off, a story version of a room (the attacked plaza `BugariaPlazaAttack`, the city's ending rooms, the
    castle attack and the like) keeps its doors as the game has them, reached only the way the story reaches it; on,
    shuffled like any door. Found in play: a chapter 1 file walked into the attacked plaza through a shuffled door.
    First: list the story-version maps (the map list and the scenes that load them). Its own build step.
60. **The Termacade** (decided 2026-10-06, the user; to build, its own step). **The greeter's gift** (`termiteoutside`
    on `BugariaCommercial`, flag 351, 15 tokens, the token count `flagvar[27]`): a location, "Arcade Gift" (the user's
    word), its 15 tokens a filler item from the server, "similar to how we do it for the 15 & 30 berries". **The token
    prize stand:** "everything that is in the token prize shop to be in the pool. but the token shop itself should
    only be able to contain filler items": each prize a location Archipelago keeps to filler (an excluded location,
    `LocationProgressType.EXCLUDED`), its prize (Empower+ and the rest) in the pool, so no token farming is ever
    required. The arcade itself is open from the start (build step 9). Built: build step 50.
61. **The ending when the goal is reached** (proposed 2026-10-06, the user; to build, its own step): "we should
    send/use the ending cutscene when/if reaching the goal maybe ? so you get a proper 'the game is done' instead of
    just 'Ohh items got sent out, nothing special happened'"; "not the 2 rooms, you had to pass, but the credits/ending
    scene itself and afterwards showing 'the end'". Two ways (the user): put the party in the ending's plaza
    (`BugariaEndPlaza`) and let the ending play out, or the credits alone. First: how the game starts its credits, and
    what it leaves behind (the title, the save), so the save is never harmed.
62. **The Wooden Crank, never used up** (decided 2026-10-06, the user; to build, its own step). In the game a crank
    slot's lock (`Event59`) takes the Wooden Crank (key item 58) away; the game has four, three in the Golden Hills
    dungeon for its three slots and one in the Rubber Prison for its one. Like A Link to the Past's big keys, not its
    counted small keys (read in `worlds/alttp`, 0.6.8): in a seed the mod leaves the crank in the bag when placed
    (the slot's own flag still set), so the pool holds one Wooden Crank and every slot needs only it; the other crank
    pickups become ordinary locations. The user: "Option2 is probly better ... i don't think the cranks unlock that
    many locations/things anyway". Built once the rooms holding the cranks are mapped; until then a stand-in. The
    crank spots, mapped (each added with this step, since a crank item before the mod change could be used up):
    "Golden Hills: Left Crank Room, Atop the Hill" (`GoldenHillsDungeonCrankLeft`, flag 112, Beemerang Halt, Jump and
    the horn; the user's name); "Golden Hills: Right Crank Room, Far Right Ledge" (`GoldenHillsDungeonRightCrank`,
    the Big Crank Top Half, key item 59, flag 113; the bubble shield, Freeze, the horn, Jump, the Beemerang and
    Beemerang Halt, or Bee Fly alone; the user's name); "Golden Hills: Lower Right Crank Room, Top Right Ledge"
    (`GoldenHillsLowerRightCrank`, flag 116; Jump, Freeze, the horn and Beemerang Halt; the user's name); "Golden
    Hills: Left Crank Half Room, Top of the Hill" (`GoldenHillsDungeonLeftCrankHalf`, the Big Crank Bottom Half, key
    item 61, flag 117; Jump, Beemerang Halt, and Vi for the Venus Buds' scene fight, Event70; the user's name);
    "Golden Hills: Upper Side Room, Behind the Bushes" (`GoldenHillsDungeonUpperSide`, flag 129; Jump and Beemerang
    Halt from either door, the slot not needed; the user's description).
    **Merged items, the whole only** (the user, 2026-10-06: "think making it only the whole item makes it easier logic
    wise"): the pool holds the Big Crank (60), not its halves (59, 61), and the Sand Castle Key (113), not the Heaven
    and Earth Keys (105, 106); the halves' and the keys' spots hold any item, filler making up the count. The user's
    worry, a crash between receiving the halves and the merge, wouldn't lose anything either way (received items are
    counted in the save and replayed), but the whole is simpler. The Big Crank (60) is made from two halves, so its
    halves are designed here too.
63. **Berries in the logic** (decided 2026-10-08, the user; to build after the rooms, with the enemy pass, its own
    steps then). Today no berry price is a rule: ordinary berries were taken as renewable from battles (build step 11).
    The banker's Platinum Card ("Bugaria City: Residential District, Banker", 550 berries in all) raised it. The user:
    any location or NPC that takes berries needs, in the logic, a way to farm them again reachable somewhere, "no
    matter how slow/tedious", proven for every seed.
    - **How other worlds do it** (read at 0.6.8, worlds with a `licensing.md` row): ALttP and OoT ignore the amount
      (ALttP: only the shop's region, `alttp/Shops.py:473-480`; OoT: a price over 99, 200 or 500 rupees needs 1, 2 or 3
      Progressive Wallets, `oot/Rules.py:181-190`), which holds because rupees refill from grass and pots almost
      everywhere. Hollow Knight gates its geo fees on `Can_Replenish_Geo`, an event true when any farm room is reachable
      (`hk/GeneratedRules.py:12`); Archipelago's FAQ suggests it (`docs/apworld_dev_faq.md:165`: expensive purchases
      "might logically require access to a place where you can quickly farm money"). Small money items are filler
      everywhere; only rare bundles are ever counted (Messenger, Undertale).
    - **Why HK's way here:** in Bug Fables about 99 walkable rooms and whole areas (Metal Island, the hive, Termite
      City, the towns) have no renewable source, and map enemies come back only on an area change (`MEASURED.md`,
      berries), so a random start or shuffled doors could reach a shop with no farm.
    - **Berry items never count** (the user: "consumable berries similar to how consumable keys work in other
      apworlds"): future one-time berry checks (bushes that drop berries) would be filler.
    - **Enemies only** (the user, over grass, which needs the horn): one event item (name the user's) given by an event
      location at each ordinary map enemy (never a boss or mini-boss) with no `requires` or `limit` flag, not a timer
      enemy, whose fights pay money (the enemy table's money column, to dump), in the area of its room the enemy pass
      confirms. Its rule: the fight won with plain attacks (`WHOLE_PARTY` at first; per fight with question 8 once
      measured, summons included; a fight needs no move item: battle attacks are never items, and touching the enemy
      starts it). Not on the start map until the logic starts at the spawn.
    - **Ordinary enemies respawn on room re-entry, always on in a seed** (the user: "on room re entry only ... instead
      of on a timer ... just going between 2 different rooms back/forth ... always be on for the seed"; "only for
      enemies, not for boss or mini boss stuffs"): as a map loads, the mod clears its own ordinary enemies' regional
      flags before the game builds its entities. Left alone: bosses and mini-bosses (any that is a map enemy marked by
      hand in the enemy pass, `data/enemies.json` having no such mark), enemies with a `limit` or `requires` flag, the
      20 timer enemies, any regional index another entity of the area shares. Its own step in the mod guide.
    - **Where the rule goes:** a `Berries(price)` rule resolving to the event (serialized as its `Has`, as `ItemOnHand`
      is), on every location with a price (Merab's 22, the 33 item-shop slots, the Moth's Sale 40, the Banker 550, Whack
      Farms 10, Beette 150, the hive's clothing stall 40 and 50), in each way of `ItemOnHand`, and on the ant tunnels
      once their prices are back. Prices at 1x and full price (the panel's settings are client-only), each at most 999
      (the wallet).
    - **Prices back to vanilla, per NPC** (the user: "make everything cost the games vanilla amount, or require an
      item/s"; "it would have to be a per npc decision"): each paid service is *always an item* (the boat: the Boat
      Ticket stays), *a yaml choice, item or berries* (the ant tunnels, Next 57's tickets), or *berries*; decided row
      by row. The ant tunnel miners (`free_ant_tunnels`, build step 45) go back to their prices: `free_ant_tunnels`
      dropped from slot_data (the mod reads a missing key as "not free", so older seeds keep theirs). Beette's Flower
      Key is back at 150 already, `free_sales` gone (build step 76, 2026-10-10).
    - **Tests then:** berry items never in a rule; every priced location needs the event; prices within 1-999; no farm
      on the start map, a timer enemy or a limit enemy; `Berries(...).to_dict()` equals the event's `Has`; the ant
      tunnels and Beette depend on the event.
64. **The Sand Castle Key's and the Rusty Key's chains, opened** (decided 2026-10-09, the user; their own steps, after
    the mapping session or when the user says): what build step 67 holds out comes back once each chain is open.
    **The direction** (the user, 2026-10-09): "we want to eventually just make most of the game openworld/metroidvania,
    and not connected/tied to whatever chapters are doing no matter in what order you do them"; "you should be able to
    do any area/any chapter in/out of order". So, as build step 9
    does for blockers, each chapter-gated piece of a chain is opened rather than modelled with a chapter stand-in:
    Astotheles at the well and the roach village's hawk present from the start (both wait for 300), the Dash's trigger
    (`dashevent`, waiting for 88 and 138), and whatever else a key or an ability's scene waits for in the story; the
    keys then depend only on items and rooms. Where a piece can't be opened, a stand-in that holds its whole chain
    (the rooms and flags build step 67 lists), never "later chapters" alone. **First, the capture** (Known issues).
    Then 30 berries for the Rusty Key with Next 63, and the castle's own keys (two Ancient Keys, each used up by one of
    the main room's locks, and the boss key): key logic when the main room is mapped. Location 69's reach (build step
    67) is the stopgap until its trigger is opened. The Peculiar Gem (Upper Snakemouth's slot, "later chapters" now)
    comes from the castle boss room's fight, added outright, not through `Giveitem` (`MEASURED.md`, the boss room): its
    stand-in becomes that room once the castle's door opens.
65. **The file select's artifact icons, each one's own** (the user, 2026-10-09: "something we could log/maybe attempt
    later on. not a high prio"): the pause menu shows each set artifact flag's icon (the mod guide, step 48), but the
    file select draws the first N from the count each save's summary line stores (`LoadData.progression`), which says
    nothing of which ones. Adding them to that line would be a new save format (never). The way left: read each file's
    flags at the file select, read-only, the game's save format parsed by the mod; more work, and a misread only draws
    wrong icons. Its own step in the mod guide.
66. **Faster room changes** (the user, 2026-10-09: "lets save it until later, lets finish mapping out logic for all
    rooms first"; a Quality of life row, its own step in the mod guide). A door's room change takes about 2 s, nearly
    all the game's own (`MEASURED.md`, the dev load timer): the fade out with the walk into the door, about 0.25 s
    more walking in the black before the load starts, the load itself (about 70 ms), two fixed waits in
    `TransferMap` (0.1 s and 0.3 s), the fade in, then the walk into the room. The plan, agreed in outline: keep both
    short fades ("wouldn't it look weird without at least a tiny blackfade ? i don't want it to feel like loading,
    but i also don't want it to be instant/weird"), start the load as soon as the screen is black, cut the fixed
    waits to the frames the camera and sprites need, keep the visible walk in (maybe quicker): about 1.2 s a change.
67. **The Factory Pass, never used up** (decided 2026-10-10, the user, mapping the factory's Lobby; its own step). In
    the game each Factory Pass lock (`Event59`, key index 4) takes a pass (key item 95) away: the Lobby's processing
    door (one), the pump room's scanner (three) and the storage maze's card lock (one); the game has five passes for
    them: four pickups, in the worker rooms and the three processing puzzle rooms, and one given after the storage
    mini-boss room's fight (`Event101`, flag 221; found by a review, 2026-10-10). The Wooden Crank's way (Next 62): in a
    seed the mod leaves the pass in the bag at each lock, so the pool holds one Factory Pass, progression, and every
    lock needs only it; the other pass pickups become ordinary locations. Asked by the user: "similar to the cranks
    where we made it into just 1 reusable instead of having 3-4". Built once the rooms holding passes and locks are
    mapped; until then the Lobby's processing door has a stand-in (`FACTORY_PASS`, the later chapters,
    `logic/honey_factory.py`), and the Worker Rooms' pass (flag 178: Jump and the Beemerang Toss, or Bee Fly alone, the
    user) stays the game's own pickup, since a location there could hold another item while the door still needs a pass.
68. **Chapter-gated characters and scenes, fixed for good** (decided 2026-10-10, the user: "are there any other
    npc/flags that only excist at/after certain chapters ? ... that will become a real issue for the goal of having
    things be openworld/anythign doable in any order"; "a really important thing to take a look at and properly fix
    instead of just pausing/delaying the npc/scenes"). Found with the Core's finale: chapter 4's opening walks Neolith,
    made only after chapter 2's end (Known issues). First a census, from the entity dump and the scenes' code: every
    character, door and object the game makes only from a chapter flag (or removes at one), and every scene that waits
    on one or moves one; then each fixed so it works in any order (the character there whenever its scene or service
    needs it, as build step 9 opens blockers), a hold (`held_until`) only where nothing else works and the user agrees.
    With Next 64 (the key chains opened), the same direction. After the room mapping, or when the user says.

**Known issues:**

- **Chapter 4's opening may freeze after an early chapter 3 finale** (found 2026-10-10 by a research workflow, from the
  code; not seen in game). With the storage door open (build step 77) a seed can do the overseer's escort (flag 218)
  and the Core's finale (`Event99`, flag 299) early; entering the Throne Room then starts chapter 4's opening, which
  walks Neolith, made only after chapter 2's end, and waits for him for good. The fix: hold that scene until chapter
  2's end, as chapter 2's briefing is held (build step 9). Making the finale available from the start (the user's
  choice pending, `room-checklist.md`, the Core) needs it too: all seven of its characters present through
  `present_from` (so it plays once), and Vi for B-33's fight.
- **The bandit hideout's capture takes the seed's ability items** (found 2026-10-09 by the key-chain research, from the
  code; not seen in game). `Event109`'s capture moves every item and key item into `flagstring[8]` until the storage
  chest gives them back, the mod's own move and ability keys (201-211), the Boat Ticket and the submarine included.
  The mod reads abilities from the bag (`Abilities.cs`), so the cell can't be dug out of unless Beetle Dig arrives
  after the capture, and the chest (a horn-only switch) can't be hit without the Horn Slash with Shuffle Field Moves;
  a progressive item received meanwhile gets the wrong level (`KeyFor` on an emptied bag). The Warp still leaves. The
  hideout's spots are held out meanwhile (build step 67); the fix is Next 64's first part.

- **The Rubber Prison's checkpoint corridor from the yard** (2026-10-04): in the game its gates may be shut from the
  yard's side, so it never leads on; the logic still lets the yard reach the spike room through it (as before
  2026-10-04). It needs the corridor split into two areas, the yard's and the spike room's (`room-logic.md`, the
  model), which the apworld can do since build step 48 (`MAP_AREAS`); until it's written the prison's spots wait for
  the later chapters. Build step 9.
- **Story scenes reached out of order freeze** (seen 2026-10-04, decoupled doors, a new file): the trigger `eventgl`
  on `RubberPrisonGiantLairBridge` (until flag 568) starts Event194, whose first branch talks to `vanessa` (entity 11,
  made only from flag 79): absent, the scene froze (freed with the dev `unstick`; it froze again on the next step there).
  Played with a stand-in, it would set 346 and on the next visit play chapter 5's title and load the Giant's Lair
  (`LoadMap(232)`). Options (the user, later): hold the trigger until its actor exists, or a stand-in where a scene
  changes nothing beyond itself; first a sweep for every scene trigger naming an entity absent on a new file. Build
  step 12.
- **Maki with the swamp bridge kept up** (built: build step 61). Still to see: the small bridge's flag 337 also loads
  `ChomperCave1`'s bridge down, whose own switch sets 689 (when that room is mapped). **Maki** (the user,
  2026-10-08): the Far Grasslands' arrival scene (Event125) makes him a follower who fights alongside in the Far
  Grasslands and the swamp, and the collapse is what removes him in vanilla. With the bridge kept up he may stay
  through the swamp (no harm to the logic, which counts only the party's own attacks), but never at the swamp's boss
  fight, so as not to trivialise it, nor with the party in the Wasp Kingdom (its hive, the Wasp General). His hits
  scale with enemy scaling since 2026-10-08 (build step 54). **Seen (2026-10-08):** at the swamp's boss the game itself
  sets him aside: he followed the party into `SwamplandsBoss` and left it as the boss scene (Event137) began, the room
  having its own injured Maki until 359; the user: fine, nothing to build for the boss.
- **Shop Contents and the player's own placements** (found by the audit, 2026-09-29): a player's
  `priority_locations` on a shop is dropped (under Filler Only with a warning in the generator's log, under No
  Progression silently), and plando aimed at one fails silently. The fallback's two bugs (an excluded shop set back to
  normal, item rules replaced) were fixed on 2026-10-03 (build step 11). Next 43, item 1.
- **Failed connect attempts are left open** (found by the full review, 2026-09-29; read in the code): if reading
  slot_data fails right after a successful login (`ApConnection.cs`), that logged-in connection is neither kept nor
  closed, and the retry logs in again, so every retry adds a client on the slot. A mod and an apworld of different
  versions would set it off. Refused and timed-out attempts also leave their sockets open. Next 43, item 2.
- **A party of two with Leif in front hangs every battle's start** (found planning battle stand-ins, 2026-09-30; read
  in the code, not seen). The start turns the party until the field leader is in front, comparing a slot with the
  leader's member number (`BattleControl.cs:1284`), so with Vi and Leif, or Kabbu and Leif, and Leif leading, it
  never stops. Only with members as items (build step 18). Fix built, not yet seen: the mod guide, step 11.
- **The second spider fight softlocks with a one-member start** (found the same way, 2026-09-30; read in the code,
  not seen). Every second turn it gives Kabbu a line by reading the party's second slot (`EventDialogue` case 5),
  which a party of one doesn't have, so the fight stops if the Web is still up on turn 2. Fix built, not yet seen:
  the mod guide, step 36.
- **The travel buttons can land on the pause menu's map as it closes** (found by the fact check, 2026-10-02; read in
  the code, not seen). Back from the map to the main page, the game builds the page 0.2 s later, and until then the
  Warp button's prefix sees the map's markers in place of the page's icons. With area 15 visited, it moves markers
  into the button row and puts the Warp and Map icons on the closing map; with one of areas 12-14 not visited, it
  throws once instead. Only with the Warp or Map row on; page 2 can't set it off. The fix to try once seen: take the
  array only when its slots 13-16 are the main page's own icons. The mod guide, step 10.
- **A shuffled door left the party respawning in a hazard forever** (seen by the user, 2026-10-02: "one of the
  entrances in chapter 2 i think", the pause menu out of reach, so a hard softlock). Which door is unknown. The
  respawn-loop guard (the mod guide, step 40) now ends any such loop with the Warp, and logs the last door walked
  through and whether `door_targets` had rewritten it. The next loop names the door, which then gets its own fix.
  The guard is seen ending the swamp's original loop (2026-10-04, the mod guide, step 40); not yet met in a shuffled
  seed.
- **Some doors are still never shuffled** (build step 38, 2026-10-02). These keep their destination in every mode:
  - story copies that have pairs: the Golden Settlement by day and night; the Beehive's entrance, now the Scanner
    Room's two doors (build step 73), fixed until the door pass;
  - doors whose name another door on the map shares: `GoldenPathTunnel2`, `WaspKingdomOutside`. The mod finds doors
    by name;
  - `TermiteIndustrial`'s pair inside its own map, three doors of one name.

  Each needs the mod to tell the copies apart (by entity index) before it can be shuffled. **Decided (the user,
  2026-10-02): later, one entrance or room at a time.** The Sand Castle's two
  right-hand basement doors are left out entirely (parked at height 99): whether they can be reached at all is to see
  in play.
- **A fill error with *minimal* accessibility and Shuffle Jump, next to another game** (found by the fuzzer with
  APQuest, 2026-09-30: 1 of 10000). The failing pair (Bug Fables minimal, Decoupled doors, a random start, moves and
  Jump shuffled, crystal berries and discoveries off, shops with no progression; APQuest with its Hammer) failed 4 of
  400 seeds both before and after build step 36, so the step didn't cause it. One option changed at a time, 400 seeds
  each: Filler Starting Checks off 1, no random start 6, no door shuffle 4, moves not shuffled 5, **Jump not shuffled
  0, accessibility full 0**. How: for a *minimal* player, once its goal is reachable the fill stops checking access
  for its items (`Fill.py`, 100-103, 0.6.7), and ours is reachable early (one artifact), so our progression can take
  the last reachable spots before the other game's key item. A generation error, never an impossible seed. Measured
  and not kept: Jump as a local early item with *minimal* and Shuffle Jump, 1 of 400 on the same pair. **Decided
  (the user, 2026-09-30): the fix is more locations, not a workaround.** A 100% generate rate is always the aim; the
  real fix is the first of Archipelago's alternatives to a local early item (`apworld_dev_faq.md`, "My game has a
  restrictive start"): more early spots,
  those needing no Jump above all (the room mapping, Next 2), and a goal past chapter 1. Until then a fuzzer or CI
  `FillError` with *minimal* and Shuffle Jump is this issue. Re-measured as locations come: the failing pair over 400
  seeds (its yaml options above), done at 0 there and 0 of 10000 fuzzed.
- **Two items named "Leif"** (found by the full review, 2026-09-29; read in the code): with the story's party, the
  story event *Leif Joins* makes an event item "Leif" with no id, while the real member item "Leif" has one;
  Archipelago's `world api.md` requires one id per item name. Next 43, item 3.

- **Found by the review for build step 28 (2026-09-29), each confirmed in the code; `docs/reviewing.md` lists them for
  reviewers:**
  - **Names from the server ran as game text commands.** Player and item names went into the game's text unescaped;
    the game runs `|...|` commands inside a substituted string (`MainManager.cs:12688-12704`), among them `flag` and
    `money`, and the save's separators could break a save. **Fixed (2026-09-29, the mod guide's step 33): every server
    string goes through `ServerText`; not yet seen in game.**
  - **wss:// accepts any certificate** (websocket-sharp's default, `return true`), and a bare address falls back to
    plain ws://, password included. What to do is the user's decision: Mono in Unity may hold no root certificates, so
    checking them could turn every connection into ws://. Measure that first. **The probe is built (2026-09-29):**
    the dev build's `TlsProbe` logs what this Mono's own check decides for each wss:// server, and still accepts the
    certificate. Next: a connection to archipelago.gg with it on, run once the user says so.
  - **MultiClient.Net 6.7.1's cache path used the server's game name and checksum unsanitised** (its
    `GetFileSystemSafeFileName` returns its input). **Fixed in the mod (2026-09-29, the mod guide's step 34): two
    patches make both a plain file name; not yet seen in game.** Reported upstream by the user as MultiClient.Net
    [#143](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/issues/143) (2026-09-29; text only, never
    code from us). Our patch stays until a release carries a fix.
- **Horn rules:** written as `CanUse("Horn Slash")` since the horn became an item (build step 21): locations
  11, 19, 25, 30 and 32, and 31 through the Den's entrance (build step 13). Not location 2: the horn tutorial cuts its
  grass itself and played through with Leif alone (2026-09-25). **Upper Snakemouth, when it gets room-level logic**
  (today its one location, 74, sits under the later chapters' story-order rule, build step 23)**:** the big door in the
  door room stays shut until flag 14 (its closed halves stand until the trapdoor fall, `MEASURED.md`, scenery switched
  by flags), so its rule is the trapdoor (the door room's horn puzzle: the Horn) and the Peculiar Gem for the slot
  behind it (2026-09-27). Also location 19 (crystal berry #0 outside Snakemouth Den): the horn from the Outskirts' side,
  or the way round through the cave (2026-09-26; `MEASURED.md`): today it takes the Horn, more cautious than the game;
  room-level regions would add the cave.
- **Grass on a Horn Dash route** (2026-10-09): without the Horn Slash the Horn Dash cuts no grass, so a mapped Horn Dash
  route that crosses grass would need the Horn Slash too. Ten rooms to recheck on screen once every room is mapped (the
  user's call); build step 63.
- **Crystal berry #2 (location 20)** sits behind the Underground's need (its `reach`), which needs Leif, though the
  room's upper-left entrance needs nothing. More cautious than the game, so safe; room-level regions would split it.
- **Uncap FPS (mod guide, step 24) still speeds some things up.** Each to compare at 60 and above on screen, then
  step at the game's own rate, as the other per-frame sites are:
  - **Being hit plays too fast, for enemies and the party** (a tester, 2026-09-27, FPS unlocked). Cause not read yet;
    with the `ShakeSprite` fix below, nothing odd was seen in combat on screen (2026-09-28; no before/after seen).
  - **A frozen enemy shimmering after a knock, and the leader blurry on platforms and bridges** (2026-09-27; "really
    blurry/bad", 2026-09-30): drawn only at physics steps. Every character is now drawn smoothed (mod guide, step 24),
    seen sharp on a conveyor and in flight (2026-09-30); the Rubber Prison's swinging platforms, drawn smoothed too,
    seen smooth with the party on one (2026-10-01); bridges and a frozen enemy not yet seen with it.
  - **Bushes shaking before the leaf gang's ambush looked blurry** (2026-09-27, at 240; the swamp,
    Event128): `ShakeObject` fixed (mod guide, step 24), not yet seen. Shaky text: fixed and seen.
  - **Hits:** a character's own shake (`ShakeSprite`) was per frame and is fixed, seen without anything odd
    (2026-09-28); which part of a hit looked fast is still to be told apart on screen (the flinch pose is timed in
    seconds, the screen shake in physics steps).
  - **Other shakes still rolled every frame** (code read, 2026-09-27, not seen): a numb character's twitch (a 5% roll
    per frame, `EntityControl.Numb`), the fountain (`ObjectTypes.Geizer`, which Freeze freezes) and the crumbling
    platform (`NPCControl`), Heavy Strike's charge sound (its pitch rises per frame). Each to be compared on
    screen. **The screen shake is fine:** the swamp bridge's collapse (Event130, a `ShakeScreen`) looked normal at 240
    (2026-09-27), so it's not the fast hit either.

---

# How we built it

## Build step 1: the apworld's layout, item classes and location names

The apworld started deliberately tiny: two early locations, one key item (the Explorer Permit), the gate
it opens, and "open that gate" as a temporary goal. Items and locations lived in simple JSON files, so
growing the world was mostly adding data (the logic moved to Python modules, one per area, in build step 29; it is
still mostly adding data). It follows the layout of `worlds/apquest`, Archipelago's own teaching example, and writes
its rules with Archipelago's Rule Builder.

**One file per job, as APQuest does** (2026-09-27, a refactor that changed nothing a seed contains): `world.py` holds
only the `World` class, whose steps call module functions that take the world: `items.py` (the item class and the
pool), `locations.py` (the location class, which categories a seed includes, placing locations and events),
`regions.py` (regions and exits), `rules.py` (what each spot needs, the shop policy, the goal), `slot_data.py`
(everything the client reads), `web_world.py`, and the two shuffles `doors.py` (since 2026-09-30 `entrances.py`) and
`enemies.py`. `data_tables.py`
loads the JSON and names the id scheme's kinds (`ITEM_KIND` ... `MOVE_KIND`); `options.py` has the one table from a
location category to its yaml toggle (`CATEGORY_OPTIONS`). **How "changed nothing" was proven:** before the split, a
script generated 13 option sets x 2 seeds and saved each seed's regions, locations, pool, the locations each
progression item's absence locks, the fill and `slot_data`; after it, the same output byte for byte. The script was
first shown to catch a one-word rule change. Tests are split by subject too (`test_logic.py` gates, `test_slot_data.py`,
`test_pool.py`, `test_categories.py`, `test_shops.py`), and `BugFablesTestBase.state_with` builds a state holding
just the named items or events.

**Typed data** (2026-09-28, a refactor that changes nothing a seed contains, as the Pokémon Crystal apworld keeps its
data): each data file's entries become frozen dataclasses in `data_types.py`, read once in `data_tables.py`, so code
reads `item.kind` instead of `item["kind"]`, and a key a record doesn't have is refused when the world loads instead
of being silently ignored. A file's schema lives in its record's docstring. One table per commit, each proven by
`seed-snapshot.py` (identical slot_data and spoilers), the tests and the fuzzer. Done for every table: items,
encounters, doors and room starts, the client's entity lists, then regions, exits, locations, story events and
artifacts together (then `rules.requires` read exits and spots alike; since build step 29 they carry Rule Builder
rules in their `rule` and `reach` fields, which `rules.set_all_rules` joins). The schema
strings that headed `items.json` and `locations.json` are now those docstrings. `TestDataRecords` proves an unknown
key is refused; with the check switched off, it fails. (Since build step 29 the logic's records are written in Python,
`logic/`, where a misspelt field fails by itself; the data files left are items, doors, enemies and save points.)

We wrote **tests**, including one that proves the gate really needs the permit. To make sure that test
could fail, we removed the rule on purpose, watched the test fail, and put the rule back. Archipelago's
own test suite passes for it too. Since 2026-09-28 every apworld change is also fuzzed: 10000 seeds from random
yamls (`development.md`, "Fuzzing the apworld"), which finds the option combinations no test thought of; since
2026-09-29 CI fuzzes every push too (build step 17). A change
meant to alter nothing (a refactor) is also proven with a seed snapshot: fixed seeds before and after, whose slot_data
and spoilers must come out identical (`development.md`, "Proving a refactor changed nothing").

All of those check the logic against itself. **Since 2026-09-29 the logic can be tested against the game:** the Logic
Test apworld (`development.md`, "Play-testing the logic") turns a seed into spheres played one at a time, with every
location a check you must do. Stuck in a sphere means the logic is looser than the game; a key from a later sphere
means it's stricter. It works only if its second generation of our world equals the real one, which it doesn't check
itself. So `logic-test-check.py` checks it, and runs with the tests whenever the Logic Test is in the checkout.

To try it, the world folder is linked into a local copy of Archipelago (run from source), and seeds are
generated with `Generate.py`.

**Packaging it as a `.apworld` file** (to generate with an installed Archipelago, or to share): run the
"Build APWorlds" component from the Archipelago checkout, for this world only:
`python Launcher.py "Build APWorlds" -- "Bug Fables"`. It writes `build/apworlds/bug_fables.apworld`, leaves
out `__pycache__`, and adds two fields to the file's `archipelago.json`, `version` and `compatible_version`.
Don't write those fields by hand. A hand-zipped copy made the generator warn "Invalid or missing manifest
file ... will stop working with Archipelago 0.7.0" (2026-09-24). The built copy goes in the installed
Archipelago's `custom_worlds` folder.

**Rules the world follows** (2026-09-24, matching Archipelago's own definitions in
`BaseClasses.py`, `ItemClassification`):

- **Items are classified the Archipelago way.** *Progression*: anything logic depends on (it unlocks a
  location). *Useful*: especially good to have; never placed on an excluded location. *Filler*: can be
  ignored; with traps, the only kinds an excluded location gets. *Trap*: detrimental to receive; a yaml option may swap
  filler for traps.
- **The pool is each included location's own vanilla item** (2026-09-24, once two locations held an HP Plus medal),
  then the member, ability and the mod's own items the options add, then padding for the rest (test `TestPool`, which
  also fails if a location's vanilla item is missing from `items.json`). *Padding* is a mark in `items.json` for
  the filler that may fill leftover locations in any number (a Crunchy Leaf). An item without it, like
  the Hard Mode medal, is a real item and goes in once (2026-09-24; test `TestMedals`). The G-Bug Ranger Plushie
  (a key item) joined as *useful* on 2026-09-24, so a test could put it on Artis's medal. Its own vanilla
  spot at the Bugaria theater isn't a location yet, so the game still hands that copy out there.
- **Which class each item gets** (2026-09-24): **if an item can unlock even one location, at any point,
  even if only sometimes or not always, it is progression. No ifs or maybes.** The three, in the user's words
  (2026-10-08): "filler = useless; useful = want to guarantee the player can get it at some point; progression =
  required, unlocks 1 or more checks, always required even if it only sometimes unlock a location/check". Every field
  ability is *progression*; a key item is *progression* when any rule in the logic uses it, even for a single location.
  **Every key item and every medal is at least *useful*, never filler** (the user, 2026-10-08: "a key item or medal etc
  should always be useful"), the Hard Mode medal (#11) included (filler from 2026-09-24 to 2026-10-08, since in a seed
  it only makes fights harder; the user: useful). Crystal berries buy medals at the crystal berry shop: *useful* while
  her shop stays vanilla (the user, 2026-10-08), *progression* in the same change that puts that shop in the seed,
  since every spot there needs them. `TestClassifications` checks both directions: an item a rule uses is
  progression, and a progression item is used by some rule; `TestKeyItemsAndMedalsAreUseful` holds the rest. **What
  it changed** (a read-only review, 2026-10-08): 23 fewer filler in a default seed (22 Crystal Berry copies and Hard
  Mode), so *Shop Contents: Filler Only* now falls back to No Progression in a default solo seed (build step 11's
  fallback; the tests that expected it to hold now turn hidden items on); no option set fails generation. Archipelago
  keeps a useful item off excluded spots always, and off unreachable ones only with *Accessibility: Full*
  (`Fill.py`, `forbid_important_item_rule`, at 0.6.8).
- **Logic lives on regions and locations, never on items.** An item doesn't say what it unlocks. A region's
  exits say what they need (the Golden Path's door needs the first boss; the permit gate, inside the Outskirts map, is
  each spot's `reach` until the rooms are mapped), and every location belongs to a region. A location needing something
  more than its region adds that to itself.
- **A location is named after where it is, never after what it gives** (2026-09-24). Once items
  are shuffled, a hint like "your Hover is at Outskirts: Explorer Permit" points at the wrong thing. The
  form is `<Area>: <Room>, <Spot>` (2026-09-24): the game's own area name; a short room name from a
  landmark, left out for a one-map area; and the spot as **just a landmark**, a noun of one to three words with
  no articles or verbs, like `Snakemouth Den: Bridge Room, Pillar` ("Ledge", "Chest", "Waterfall"; a qualifier
  like "Top of Pillar" only when a room needs telling apart; two rooftops became "Rooftop" and "Fountain Rooftop",
  not "On Top of the House by the Fountain", found too descriptive, 2026-09-25). Not a sentence and not a hint at how to
  get it ("On Top of a Pillar", "Under a Rock" are too much). **When a room has more than one of that landmark, add
  `by the <Thing>`** after it, naming something a player can see next to it: `Snakemouth Den: Lake, Bush by the
  Tablet` (2026-09-24: this is how to tell apart which bush, rock or pillar). A spot with no landmark of its own is
  just `by the <Thing>`: `Outskirts: Snakemouth Den Entrance, by the Cave` (the user, 2026-10-04). It says where the
  spot is, never what to do there: the berry is inside that bush, so "Bush" is right. Gifts are `<Area>: <Who>'s Gift`
  or `<Who>'s Reward`, like `Outskirts: Maki and Eetl's Gift`. A character's name only when players will remember it
  (main and recurring ones); a minor one is described instead ("Ladybug Kid's Reward", "Ladybug Siblings' House", for
  Leby and Dib) (2026-09-24). A spot there only by day or only at night (the festival's, build step 52) ends with
  "(Day)" or "(Night)": `Golden Settlement: Square, Sunset Inn (Night)` (the user, 2026-10-07). Never the item, the
  flag or a mechanic ("Beemerang" goes stale once abilities are shuffled), Title Case, one word per kind of landmark
  everywhere. The test
  `TestLocationNames` fails if a location's name contains its own vanilla item's name. Renaming a location
  never changes its id or flag. **Every name, new or renamed, is the user's to approve before it goes in**
  (2026-10-04): names had been made up without asking (2, 23, 25, 30, 68-74 and 76 among them). The same day the
  user went through them in game, warped to each, and named them: from their description of what's next to the spot,
  shaped into this form (no hunting for the game's word for a landmark), the area always the game's `AreaNames`
  entry (*Bandit Hideout*, *Wild Swamplands*, *Forsaken Lands* had been wrong). Propose, then wait for a yes.

**Status:** done; the world has since grown to 75 locations (70 by default) and 58 items (counted 2026-09-30).

*Code: `apworld/bug_fables/world.py` (`BugFablesWorld`), `regions.py`, `locations.py`, `items.py`, `rules.py`, the
data in `data/items.json` (read by `data_tables.py`) and the logic in `logic/` (build step 29), tests in
`test/test_logic.py` (`TestPermitGate`).*

## Build step 2: the mod's first login to an Archipelago server

We generated a seed with the tiny world, started a local Archipelago server (`MultiServer.py`), and had the
mod log in from inside the running game, using the official .NET client library
(Archipelago.MultiClient.Net).

The first try timed out. The server's own log showed what happened: the library first tried a secure
connection, which the plain local server rejected. Giving the address as `ws://…` fixed it, and the mod
logged in. This also proved the game's runtime can run the client library, which had been an open risk.

**Lesson:** when two programs talk, read the logs on *both* ends. For the same reason, a server on your own
computer is entered as `ws://127.0.0.1` with port `38281`. (The mod's default address is now
`archipelago.gg`, for hosted rooms.)

**The version in Connect** (2026-10-08): `network protocol.md` asks for the Archipelago version the client supports;
the library sends its own default (0.6.0, `archipelago-review.md` item 19) unless given one, so the mod names the one
the apworld targets, 0.6.8 (`ApConnection.TargetedArchipelago`), changed with CI's `AP_TAG`. The server (0.6.8)
uses it to refuse a client older than a slot's minimum, or any other version when it runs with strict compatibility,
and prints it when the client joins.

**Status:** works (2026-09-24, local server; hosted rooms on archipelago.gg since build step 5). The 0.6.8 in
Connect: built 2026-10-08, not yet seen (the server's line as the game joins names it).

*Code: `mod/BugFablesAP/Core/ApConnection.cs` (`ConnectOnWorker`); the address settings in `Plugin.cs` (`Awake`).*

## Build step 3: the goal, Artifacts Required

The game shows up to 7 artifacts on the pause menu and on each save file. Reading how it draws them showed
they aren't items at all: the game counts how many of 7 story milestones you've reached. That makes a good
goal. It's cheap for the mod to check, it's real progress, and **"any N of 7"** doesn't care about order, so
it keeps working with options like a random start.

The apworld has an option, *Artifacts Required* (1 to 7). Each artifact is an **event** in the region where
the game grants it, and the goal is "have N of them". An event holds no real item; it exists so the
generator can prove the goal is reachable. The world only includes the first artifact so far, so a request
for more is lowered, with a warning, instead of producing a seed that can't be won. That rule has a test,
and so does the permit gate: remove the permit rule and the permit tests fail.

One rule came out of this for every later option: **every seed can be completed from wherever it starts.**
Whatever an area or the goal needs is written into the logic, and the mod never hands things out to patch
a gap.

**The mod reports the goal (2026-09-26).** It reads `artifacts_required` from `slot_data` (inside `options` since
build step 39) and each frame compares it
with the game's own count, `MainManager.SaveProgressIcons()` (the seven artifact flags; `MEASURED.md`). Once the count
is reached it sends Archipelago's `StatusUpdate` with `ClientGoal`, the way `adding games.md` asks (never an event),
through MultiClient.Net 6.7.1's own `SetGoalAchieved()` (since 2026-10-08; before, the mod built the same
`StatusUpdatePacket` itself, which the library's `ArchipelagoSessionActions.cs` sends; on a worker thread, as its send
waits on the connection's ping). It's sent once per login while reached, so a send lost with the
connection goes again at the next one, and the server keeps it. **Since build step 60 it counts only the seed's goal
flags** (`goal_flags`, the artifacts the world includes), no longer the game's count of all seven: before, an
artifact the logic doesn't hold could reach the goal sooner than the logic proves it. The log says what it decided:
`[goal] 0 of 1 artifacts`, then `[goal] sent: ...`. **Seen (2026-09-26):** beating the spider boss (a dev file)
logged `[goal] reached, 1 of 1 artifacts` and `[goal] sent`, and the server released the slot's remaining items and
logged "Team #1 has completed all of their games!".

**Status:** in progress: the goal is in the apworld, with only the first artifact so far; the mod sends "goal reached"
at the required count, seen working (2026-09-26), and again read from `slot_data`'s `options` (2026-10-04: the
first boss, `[goal] sent`, the server's "completed their goal"); more artifacts come with more of the world (Next 1).
Sent through `SetGoalAchieved()` since 2026-10-08 (the same packet): not yet seen. Only the seed's goal flags count
since build step 60 (2026-10-08): not yet seen.

*Code: `apworld/bug_fables/options.py` (`ArtifactsRequired`), `world.py` (`generate_early` lowers the
number), `locations.py` (`create_all_locations` adds the artifact events), test `TestArtifactsCapped`; the
mod: `LocationChecks.CheckGoal`, `ApConnection.SendGoal`.*

## Build step 4: auto-connect, retries and a dropped connection

Players enter the room's address, port and slot in an Archipelago panel on the main menu. While the
Archipelago mod is enabled and those are filled in, **the mod connects by itself**, with no Connect button.
Failures are sorted into two kinds, using the refusal codes the client library reports:

- **Refused** (a wrong slot or password): the reason is shown, and nothing is retried until a detail changes.
- **Unreachable, or the connection dropped**: it retries on its own, waiting 2, 4, 8, 15, then 30 seconds.

A dropped server turned out to be invisible: an idle connection doesn't notice the other side is gone. So
while connected, the mod asks the server something tiny every 5 seconds (the documented read-only value
`_read_race_mode`), and treats 15 seconds of silence, a failed send or a socket error as a lost connection.

Tested against a local server: a wrong slot was refused and left alone; with the server stopped the mod kept
retrying, and when the server came back it connected by itself.

**A dropped connection must be closed by force.** Stopping the server while connected made the game lag and
eat memory (2026-09-24: five threads spinning, memory growing about 2.5 MB a second). The client library
keeps reading "while the socket is open", and in this game's version of .NET a dead socket still reports
itself open, so the read failed and retried forever. Asking the library to disconnect politely doesn't help,
because the goodbye can't reach a dead server. The mod now aborts the socket itself whenever a connection
is lost or replaced, which ends the loop. It also gives every connect attempt 12 seconds: the library's
login step can wait forever, and a stuck attempt had stopped all further retries. Measured after the fix
(2026-09-24): stopping the server while connected logged `socket closed: Open -> Aborted`. The game's CPU fell
back instead of climbing, its thread count went down, and its memory stayed flat. The tester's on-screen check
that the game stays smooth is still to come. When the server came back, the mod reconnected by itself
within about 6 seconds. (Since build step 5 the socket is a different library's, and the mod closes it with
that library's own close call instead. See step 5, point 5.)

**Status:** works: refusal, retry and reconnect tested on a local server, the drop measured (2026-09-24); seen smooth
by the tester (2026-10-04): the server down about two minutes, no stutter, two checks picked up meanwhile queued and
sent on the reconnect, which came by itself.

*Code: `Plugin.cs` (`AutoConnect`); `ApConnection.cs`: `ConnectOnWorker` (refused or retry),
`RetrySeconds` and `ScheduleRetry` (the waits), `Watchdog` (the 5-second ping, 15 seconds of silence, the
12-second connect deadline), `MarkLost` and `KillSocket` (closing a lost socket).*

## Build step 5: a compressed websocket connection

The Archipelago server tells every client that doesn't compress its traffic: *"your client does not support
compressed websocket connections! It may stop working in the future."* It's only a warning today, so this
step is optional. We did it anyway, as a worked example. Here's how it goes, in the order we found things out.

**Versions we ship** (read from the project file and the DLLs, 2026-09-24): Archipelago.MultiClient.Net 6.7.1
(its net40 build), websocket-sharp 1.0.2.34775 (the copy bundled in that package's net40 folder), and
Newtonsoft.Json 11.0.1 (the netstandard2.0 copy bundled in the same package; see point 7).

**1. Find out what "compressed" means here.** Websockets have a standard compression add-on called
*permessage-deflate*. The client offers it when it connects, and the server accepts or declines. The
server's code shows it looks only for that add-on, set up with both window sizes at 11 and a memory level, and the
one its answer always carries is `server_max_window_bits=11` ([`MultiServer.py`,
tag 0.6.7](https://github.com/ArchipelagoMW/Archipelago/blob/0.6.7/MultiServer.py#L56-L60)).

**2. Check what the client library can do.** The library comes in several builds, one per kind of .NET. The
build we'd used runs on .NET's own websocket, and the version of .NET inside this game has no compression
at all. The library's older builds (net35, net40) run on a different websocket library, **websocket-sharp**,
which does support compression, but nothing in Archipelago's library turns it on.

**3. Look for someone who tried first.** The library has an open pull request,
[#141](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/pull/141), doing exactly this. It warns
that websocket-sharp **refuses the server's answer when it includes `server_max_window_bits`**.
We confirmed that in both codebases. websocket-sharp accepts only two named settings in the answer, and the
server always adds the window setting. So just switching compression on would make every connection fail.
That setting only limits how the *server* compresses, and any decompressor can read it, so it's safe to
ignore.

**4. The change, in three parts:**

- Build against the library's **net40 build** instead of letting NuGet pick. The project file points at
  those DLLs by hand, and the mod ships websocket-sharp next to it.
- A **Harmony patch** on the private library method that creates the websocket switches compression on in
  the moment between creating the socket and connecting it. It also routes websocket-sharp's own error
  messages into our log, since otherwise they go only to the console.
- A second patch removes `server_max_window_bits` from the server's answer before websocket-sharp checks it.
  Every other setting is still checked as before.

Both patched methods are **private**: `ArchipelagoSocketHelper.CreateWebSocket` in the client library, and
`WebSocket.validateSecWebSocketExtensionsServerHeader` in websocket-sharp. Private methods can be renamed or
changed by any library update without warning. If the check's patch can't be installed, neither is; if only the
socket's can't, compression is never asked for. Either way the log says `[ws] NOT installed (…): …; compression left
off`, and the connection works uncompressed. A changed
method that keeps its name would not be caught that way, so re-test compression after updating either
library.

*Code: `mod/BugFablesAP/BugFablesAP.csproj` (the net40 references); `WebSocketCompression.cs` (`Enable`, the
patches `AfterCreate` and `BeforeValidate`).*

**5. Mind what the new layer changes.** Swapping the websocket library is not free:

- Before every send, the client library's net40 build checks that the connection is alive
  (`webSocket.IsAlive` in `ArchipelagoSocketHelper`). In websocket-sharp, that check sends a ping and
  **waits for the answer, up to 5 seconds** (a client socket's wait time). websocket-sharp's own send doesn't
  ping; the check before it does. So the mod never sends from the game's own thread. Otherwise the game would
  stutter on every send and freeze when the server is gone.
- Closing a lost connection works differently too, so the socket fix from step 4 was redone for the new
  layer, and its drop test runs again. The mod now closes the socket with websocket-sharp's `Close`, which
  waits up to 5 seconds for the server's reply, so that happens off the game's thread too.

*Code: `ApConnection.cs`: sends run on background threads in `Watchdog` (the keepalive), `SendChecks`,
`Scout` and `Connect`; closing is `KillSocket`.*

**6. Prove it on both ends.** The mod logs the compression it agreed with the server, read back from the
socket itself (`[ap] connected over ws, compression: permessage-deflate; ...`, in
`ApConnection.ConnectOnWorker`). The server stops posting its warning.

**7. The first run failed, and not because of compression.** The handshake passed, the server logged the
connection, and then the login timed out without a word. The library reports socket errors only through an
event we hadn't been listening to yet during the connect, so the mod now logs them there too, with the
full stack. That showed `PlatformNotSupportedException` from **Newtonsoft.Json**, the JSON library. Its net40
build compiles small pieces of code at runtime, and this game's .NET can't; the BepInEx log says so at every
start (`Supports SRE: False`). The message had already been decompressed correctly by then. The fix is to
ship the net40 client library with the **netstandard2.0** Newtonsoft.Json. Both are the same version, so they
fit together. Lesson: when you swap one library build, every library that comes with it is swapped too.

**Checked (2026-09-24, local server):** it works. The mod logs in compressed, the server's warning is gone,
switching the mod off closes the connection cleanly, stopping the server is caught and leaves the game at
normal CPU and flat memory, and the mod reconnects by itself, compressed, when the server comes back. A
`Compression` setting in the config (section `Connection`, on by default, defined in `Plugin.Awake`) turns it
off if it ever misbehaves. The mod sets compression explicitly both ways, on or off, so the setting still
works if a library update starts turning compression on by itself (pull request #141 would). Checked on
both ends (2026-09-24, local server): with the setting on, the mod logged `compression: permessage-deflate…`
and the server said nothing; switched off and hot-reloaded, the mod logged `compression off (setting)` and
`compression: none`, and the server posted its warning. **A hosted room on archipelago.gg
works too (2026-09-24):** with a bare `archipelago.gg` address, the mod connected over `wss` (encrypted),
compressed, and the room's log showed no warning. The TLS worry didn't come true. The mod now logs which kind
of connection it made (`connected over wss, compression: ...`), because a bare address tries `wss://` first
and falls back to `ws://` without saying which one worked.

**Status:** works (2026-09-24): compressed on a local server and on archipelago.gg, checked on both ends.

## Build step 6: sending checks, read from the game's own flags

Sending checks came before receiving items because it's easier to test: the tester can reload a save and
redo the same find as often as needed. The test location is Artis's medal, the first medal in the game,
right after the Explorer Permit. It was added to the apworld as a third location for exactly that reason.

**How the mod knows a location is done.** The game already remembers every finished event with a *flag* in
the save. The probes showed which flag belongs to which location (`MEASURED.md`). So:

- The apworld's logic (`logic/`) lists each location with its flag. The apworld sends that list to the mod
  in `slot_data` as `location_flags`. The generator stays the only source of truth: the mod only watches the
  flags of locations the seed actually has.
- Every frame, while a randomizer save is being played, the mod reads those few flags. When one becomes true,
  it sends that location's check. It only reads flags; it never changes them.
- **That per-frame read makes no garbage.** It once rebuilt its status text and every shop's sorted list each frame,
  68 KB a frame, which forced a 42 ms garbage collection every couple of seconds, a visible hitch (found 2026-09-27
  with the console's `frames`; the mod guide, step 25). The lists are built once per `slot_data` now, and the
  status text only when it changes.

**Offline play needs no extra queue.** The flags are saved with the game, so a location finished while the
server is down is found again at the next login and sent then. **But the seed must be known first:** the mod
keeps no copy of `slot_data` on disk, so until the first login of a game run it can't tell a location from any
other pickup. The file select therefore refuses randomizer files until then (2026-09-24;
`documentation.md`, step 8). A drop after that login keeps everything in force. Within a session, the client library
keeps every check the server hasn't confirmed and sends it again with the next one.

**What the log shows:** `[check] watching ...` (which locations and flags), then `[check] location ... is
done (flag N set) on <map>/<area>: sending`, `[check] sent ...`, and `[check] now checked on the server: ...`.

**Tests:** the apworld checks that every location has its flag in `slot_data` (the permit's is 15, the
medal's 32), and that the world version is written in one place only (the manifest). Both fail without the
change. The world version went to 0.2.0.

*Code: `apworld/bug_fables/slot_data.py` (`build_slot_data`), test `TestSlotData`; in the mod,
`LocationChecks.cs` (`Tick`), `ApConnection.cs` (`SendChecks`); `location_flags` is read in `SeedData.cs`.*

**Not yet:** the game still hands out its own item at the location, the medal here. Replacing that with the
server's item is the next step (done since: the mod guide, step 9). A save from another seed would have sent its
finished locations here; build step 7 ties each save to its seed, which closed that.

**Seen working (2026-09-24, local server).** The tester loaded a save from before Artis, already past the
permit. On loading, the mod sent the permit's location at once (flag 15 was already set: the save acted as
the outbox). Talking to Artis sent the medal's location (flag 32). For both, the mod logged `sending`, the
server's confirmation and `sent`, and the server logged `BugTester sent ... (Outskirts: Explorer Permit)` and
`(Outskirts: Artis's Medal)`. (Those two were later renamed `Outskirts: Maki and Eetl's Gift` and
`Outskirts: Artis's Gift`; same ids and flags.)

**Status:** works, seen on screen (2026-09-24, local server).

## Build step 7: receiving items, each once, counted in the save

Every item comes from the server, the player's own included. The server keeps a numbered list of everything
it has sent to a slot, and replays the whole list at every login. The client's job is to give each item
**exactly once**.

**The count lives in the save.** The mod keeps "how many of the server's items this save already has" in the
game's own save, so saving, loading and starting over all stay correct. A new save starts at 0 and gets
everything; an older save gets exactly what it's missing. The rules forbid a new save format, so the mod
uses a slot the game already saves but never uses. Finding one took a measurement:

- The game saves two small arrays of numbers and text for its scripts (`flagvar`, 70 numbers, and
  `flagstring`, 15 texts). Its code uses most slots, and its dialogue scripts can use any slot by number.
- A one-off dump from the running game listed every slot any of its 2,437 text files touches. Together with the
  code, that left **number slot 60 used by nothing**, and text slot 5 as well (`MEASURED.md`).
- So slot 60 holds the count, and text slot 5 holds **the seed's name**. That ties each save to its seed: a
  save from another seed neither receives items nor sends checks. That closes the gap left open in step 6.
  **Why it's needed** (2026-09-24): it's the seed that counts, not the address, so the same
  room hosted elsewhere is fine. But the count only means something within one seed, since items can be owned
  more than once and "give what's missing" can't be told from the bag. A save from another seed would skip or
  double items, and its old flags would send checks this seed never had.
- **For test files only, `AdoptSeed`** (Debug, off by default; 2026-09-24): a save tied to another
  seed is re-tied to the connected one with its count back to 0, so a new test seed doesn't mean replaying the
  opening. The old seed's items and flags stay in the save, which is why it's never for a real game.

**Only when it's safe, one item per frame:** only while the player is free (no battle, dialogue, cutscene, pause
or map change). Never during a battle, because retrying a lost battle puts the count back (it restores every
`flagvar`) and the bag and key items too, but not medals, money or storage, so their replay would give them twice
(`MEASURED.md`, Game Over).

**Medals** (2026-09-24; tested: Hard Mode, sent from Artis's location, arrived in the medals menu, seen on
screen) go in through the game's own `MainManager.AddBadge`,
unequipped, like any medal found. The game numbers medals separately from items, and the two ranges overlap,
so a medal's Archipelago id is offset by 1000 (`data_tables.py`, `item_id`; the mod's `ItemIds.cs`), and
`item_kinds` marks it kind 2.

**Where items go:** key items to key items, ordinary items to the bag, then storage when the bag is full. The same
operations the game's own code uses put them there. Items are given strictly in order, so the count stays right.

**A full bag never blocks the queue** (2026-10-06). A bag item with the bag and storage both full used to wait for
room, and every item behind it waited too, key items and medals included. Decided 2026-09-24: never block progress,
then by letting key items skip past a held item, which needs a second count in the save. **Decided again (the user,
2026-10-06):** the game's own way instead. Thrown-away consumables are expected, but key items and medals must always
be taken, full bag or not. **How the game does it** (`MEASURED.md`, the full bag's toss): taking a floor item with a
full bag asks what to throw out, the new item or one from the bag, and whichever it is drops to the floor as a
pickup; an NPC's `giveitem` gives nothing with a full bag, the scenes check for room first. **Built:** with both full,
the receiver drops the bag item at the party's feet with the game's `EntityControl.CreateItem`, as a bush drops its
item (no despawn timer), and counts it given; taking it brings up the game's own throw-away prompt. A thrown item is
lost on leaving the room, as in the game. Every bag item is filler (`TestBagItemsAreFiller`, in `test_logic.py`), so
nothing useful is ever at stake. Built, not yet seen in game.

**Crystal berries** (2026-09-25) raise the game's berry counter, the one the pause menu shows and
Shades's shop spends. The game also keeps a *total found*, which it counts from the berry spots picked up; a
text command shows it, and 50 unlocks a logbook entry. In a seed, spots and berries are different things: a
spot sends a check, and the berry comes from the server. So the mod keeps **berries received** in number slot
69 (the last one found unused, `MEASURED.md`), and in a seed the total is that count plus berries picked up at
spots that aren't locations in this seed (all of them when the seed doesn't shuffle berries). Received berries
replay with everything else, so a fresh save rebuilds the count. A save that received berries before this
change counts only the berries it receives afterwards (test files only). Built, not yet seen in game.
Which dialogue shows the total isn't known yet; the mod logs each time the game asks for it (`[berries]`).

**Nothing given before the seed's tables are read** (an outside review, 2026-09-28). At login the connection was
published before `slot_data` was parsed, on the connection thread. A game frame in that gap saw a live session with no
item table, skipped the item as unknown and still counted it, so it was lost for good (rare: the first login of a
launch with a save already loaded). Now the session is published last, and the receiver waits while the table is
missing (`[recv] waiting: the seed's item table isn't loaded`). Built, not yet seen in game.

**Not held up by a fade that's already gone** (2026-09-28: items came "5-10+ sec" after getting control).
The receiver waited while the game reported a transition, and a dimmer fade-out reports one for its full 10 s
failsafe, long after it looks clear (`MEASURED.md`, a dimmer fade-out never finishes early). Ignoring the invisible
tail left about 870 frames (the opening's fade-in, speed 0.02, takes about 3 s to reach 2%), so giving and showing
were split: an item goes into the bag during a fade (only a map load holds it), and only its hold-up waits for the
screen (a dimmer above 25%, about 1 s into the opening's fade-in; 2% was tried first and took about 3 s; or another
transition until it ends). Items seen given 5 frames after the opening (2026-09-28, log); the 1 s box
wait was kept ("we keep it"), and the new-game start confirmed on screen the same day.

**Seen working (2026-09-24).** The server already held the Explorer Permit and the G-Bug Ranger Plushie from
the swap test. On loading, the save tied itself to the seed, and both arrived in key items as soon as the
player was free (seen on screen). Talking to Artis again showed the plushie but gave no second one: each
item comes once per seed, and the count in the save keeps it that way.

**Status:** works, seen on screen (2026-09-24): items and medals, each once; crystal berries seen (2026-10-04, the
count up by one from the server console); the same item three times and items from the server console seen
(2026-10-04); a login with a save loaded seen (2026-10-04: 9 items listed at login, 9 in the save, the 10th arriving);
a full bag's drop at the party's feet built 2026-10-06, not yet seen; nothing given before the seed's tables are read:
built 2026-09-28, not yet
seen.

*Code: `mod/BugFablesAP/Items/ItemReceiver.cs`: `CountSlot` and `SeedSlot` (the two save slots),
`SaveMatchesSeed`, `Tick` (one item per frame), `Busy` (is the player free), `Give` (where each item goes).
`CrystalBerryTotal.cs`: the berries-received slot and the total. The slot survey was `VarDump.cs`.*

---

## Build step 8: the logic, first part: every gate read from the game's data (in progress)

The logic has to tell the truth about every gate in the game, and the game has hundreds of maps. Instead of
playing through and noting each blocked path, we read the gates out of the game's own data.

1. **Every entity, with its flags.** The mod's `EntityDump` (the mod guide, step 7) lists every entity on
   every map, including each door to another map, the map it leads to, the flags it needs to exist and the
   flags that hide it.
2. **Who sets each flag.** Each flag's setters come from the decompiled code (`flags[N] = true`, and the
   event it sits in). A few are set only by dialogue lines.
3. **Which chapter that is.** Story events are numbered in story order: the chapter-end events and the
   chapter title cards both rise with the chapters. So an event's number places it in a chapter
   (`MEASURED.md`, "Chapters"; to confirm in game).
4. **The join.** `dev-scripts/gate-table.py` combines the three into one table: each gated door, its flags,
   and the chapter each flag belongs to. 59 doors turned out to depend on only 22 flags.

What it showed, for the design:

- **Some doors need an ability's flag** (dig, bubble shield). So a received ability item has to turn on the
  game's own flag, which every door and move already checks, rather than the mod faking the ability. (No longer so:
  since 2026-09-27 the key item in the bag answers the game's ability checks and the flags stay the game's, build
  step 23.)
- **Some doors vanish later in the story.** Logic can only say "reachable from here on", never "until
  chapter N", so each of those is judged by hand: an alternate version of the same map (day and night), or
  a place that really closes, whose locations then need another way in or must not be locations.

**Gates the data can't see.** The dump shows that a map has a dig spot or a breakable rock and a pickup, but
not whether the one blocks the way to the other; and hover gaps and hazards for the bubble shield are just
level geometry, with no object at all. Decided (2026-09-24): **a cautious default, then checks
on screen.** Every location on a map with an ability's obstacle needs that ability. Each gate the tester
confirms, or rules out, in game adds or loosens a rule, and hover and bubble-shield rules come only from those
checks. Cautious logic never makes a seed impossible; it only makes placement less random until it's refined.
**Some of it is readable after all.** The bubble shield walks across one kind of hazard, `WalkableSpike`
(the game switches that hazard's collision off while the shield is up), and hazards are components on each
map's prefab. So `MapDump` in the mod reads every map prefab without instantiating it: its hazards by
type, its electric triggers, and its auto-start events (story steps the map itself starts). That gives the
bubble shield's maps from data. Hover has no object at all; pits (`Hole` hazards) are only candidates.
Obstacles for moves that are never shuffled (Kabbu's horn on grass, Vi's beemerang on switches) aren't gates. (No
longer so: since 2026-09-27 the three moves can be items, build step 21, and every such obstacle is a need,
`room-logic.md`, question 5.)

**Where each story step starts.** `dev-scripts/event-triggers.py` looks in every place the game starts an
event: talking to an entity, trigger objects, dig spots, pickups, switches, AND gates, pressure plates, locked
doors, dialogue lines, a map's own auto-start list and literal calls in code (switches, AND gates and plates added
2026-09-28 after an audit found them missing; rerun, no gate changed). It found the start of all but one of the gate
events. Two surprises: one gate is a locked door that needs a key item (so a key item gates a whole area), and dig
spots bury items, which are locations the floor-pickup count had missed, each needing dig.

**Indoor pickups are behind a door** (found on screen, 2026-09-24). A pickup the logic had open from the start
turned out to be inside a house that opens later, and the dump hadn't kept which building interior an entity
is in. It does now (`insideid`). An indoor pickup's region is behind its building's door and that door's
flags, never just its map. The wrongly placed location was retired: its id is never reused.

**Checking locations with the tester, in game.** The data lists every pickup on a map, but not whether chapter 1
can reach it. So the dev console warps the tester next to each candidate, and the tester says whether it's reachable
and names its landmark (verdicts, 2026-09-24): the Snakemouth bridge room and lake pickups are reachable with
Vi's beemerang; the two in the mushroom pit need Leif, to freeze the water droplets; the top of Snakemouth and
the east Outskirts map are closed in chapter 1. The Snakemouth top's door has no story flag, so an ability or the
level itself blocks it. The east map's door has none either; an invisible blocker outside the city, there from
the first boss until chapter 2 starts (flag 67), is the likely reason, still to test.

**Leif and the water droplets** (2026-09-24): a room with water droplets needs Leif to freeze them, and
so does every room reached only through one. The dump marks droplets (`Dropplet` entities), so the split comes
from data: from the Snakemouth entrance, follow the doors without entering a droplet room. What that reaches
(entrance, bridge room, door room, fall room, lake) was the region *Snakemouth Den*; every other Snakemouth room
was *Snakemouth Den Underground*, whose entrance needs the story event *Leif* (since 2026-09-30 every map is a region,
and this split is the `DEN` and `UNDERGROUND` needs in `logic/snakemouth_den.py`). Leif joins at the lake (Event14,
flag 16), which is on the open side, so the logic can't go in circles. The first boss's treasure room is
underground, so Artifact 1 needs Leif too. Story events like this one live in their area's module (`logic/`) under
`STORY_EVENTS`; a location can also have a rule of its own. Tests `TestLeif` fail without the rule.
**Three kinds of rule, kept apart** (2026-09-24, with entrance rando in mind): what it takes to *reach*
a room (on the connections into it), what a spot needs *once you're in the room* (on the location, e.g. the
mushroom pit's Gummies need Leif's ice while its medal needs nothing), and what it takes to *cross* a room from
one door to another (a connection through it; a room split by an obstacle becomes two regions). The in-room
rule is written even when the region already implies it, so a different way into the room can't lose it (test
`TestInRoomRules`).
**Into chapter 2** (a play-through, 2026-09-24): two more story events, each a region gate from the gate
table. *City Opened* (Event60, flag 107, after the first boss) opens the door from outside the city into *Bugaria
City*; *Chapter 2 Started* (Event45 at the Ant Palace, flag 67) opens the palace rooms and city districts (*Ant
Palace*, since renamed *Bugaria Inner City*: it also holds the districts). Story events could have their own
`requires` then (the city needed the first boss). Since then the city needs nothing (build step 9), *City Opened* is
gone, and chapter 2's need is `INNER_CITY` in `logic/bugaria_city.py`, a story event's own need its `rule`. Locations
there: a Lore Book behind the library bookshelf (test `TestChapterTwo`, which fails without the gate), and the old book
delivery, board quest 33, whose reward is a Lore Book (category quest; played through on screen). **Mid-quest items are
shuffled too** (2026-09-24): otherwise a quest's middle stays vanilla. The same cicada hands over the old book (Quest
Book, flag 241), which becomes its own location (*Old Book Delivery Start*); the Quest Book is a progression item, and
the reward (*Old Book Delivery Reward 1*, flag 243) requires it (test `TestMidQuestItem`). **The quest's middle step is
its own event** (2026-09-25: book from the cicada, handed to a reader in the palace library, back for both
rewards): *Old Book Delivered* (flag 242, the library) needs the book, and both rewards need that event (test
`TestOldBookChain`), so a room-level world can't expect the rewards without the library. A step event carries its
quest's category and is left out with it (`included_events`), since without the quest's items it couldn't be reached.
With Shuffle Quests off the whole quest stays vanilla together. Still to see in game: that the recipient accepts a Quest
Book received from the server.
**Mapping connections, one-way included** (2026-09-25: for room-level regions and a later entrance
rando). An entrance shuffle can only pair a two-way door with another two-way door; a one-way link marked two-way
can strand the player. So every connection is recorded with its direction. How:
1. `dev-scripts/door-graph.py <entitydump> [map prefix]` lists every door between maps, its gating flags, and the
   doors on the target map that lead back ("NONE": a one-way candidate).
2. Play decides the rest, since the dump can't see inside a map: a drop off a ledge, a barrier opened from one
   side, a door whose other side can't be climbed back to. The tester's findings go into `MEASURED.md` as they're
   seen (the first: Snakemouth's switch-room ledges, paired doors that are one-way in play).
3. When rooms become regions, the table plus those notes become the region graph, each exit one-way or two-way.
**The general rule** (2026-09-24): if reaching something uses an ability, the logic requires that
ability. Leif is in effect the freeze ability. Some droplet rooms are optional, so this is stricter than the game,
which is the safe direction: never impossible, only less random.

**An impossible seed, and what it taught** (2026-09-25). The tester stood in the Outskirts with nothing left to
reach: the seed had put the Explorer Permit on *Outskirts: Favor Reward*, which the logic thought was open from
the start. Its entry mixed two sources. Its check (flag 17) is set by a scene on `NearSnakemouth`, past the permit
gate (Event10, 10 berries, written in the event's code), while its "30 berries on the Outskirts" came from a
ScriptDump line that belongs to an unrelated NPC who appears only much later. So a location's flag, its give and
its region must all be traced to **the same scene**, and a give written in an event's code won't show up in the
ScriptDump at all. The fix moved it past the gate as *Outskirts: Near Snakemouth Den, Horn Tutorial* with its real give,
and the test `test_only_two_locations_before_the_gate` (since renamed `test_only_what_play_showed_before_the_gate`,
build step 9) pinned what the tester knew from play: before the permit,
only Maki and Eetl's gift and Artis's gift are reachable. It fails on the old data.

Still to do: the one event not found and characters that block a path. The region graph is built per map, with its
tests (build step 12); rooms within a map wait for build step 24.

**Status:** in progress: the one event not found and characters that block a path; the region graph built per map (build
step 12), rooms within a map waiting for build step 24.

*Code: `dev-scripts/gate-table.py`, `dev-scripts/event-triggers.py`; the dumps in `mod/BugFablesAP/Dev/EntityDump.cs`,
`MapDump.cs` and `ScriptDump.cs`.*

---

## Build step 9: the open world, story blockers removed in the logic and the mod

The world is open by default: each gate the story would close (a blocker, a door, a guard, a story flag) is opened
by the seed on its own, from lists in `slot_data` decided at generation, tested on screen and known to the logic.
This step is those lists (`kept_open`, `kept_present`, `held_until`, `present_from` and the rest) and each gate
opened with them.

**The open start is not an option: every seed starts open** (2026-09-26: building everything twice, for a
linear and an open game, isn't worth it; open, metroidvania-like games work best in Archipelago). This replaces the
"open start" yaml option planned on 2026-09-24 (skip the prologue and tutorial, optionally with Leif from the start (the
new-game party `{0, 1}`, `MainManager.cs:3591`, becoming `{0, 1, 2}`; early cutscenes are written for two, so tested on
a fresh file). A full story strip, as the Metroid Fusion apworld does, isn't the plan: here every cutscene also changes
the world through flags.) Leif from the start is now *Starting Party Member* (build step 18).
**Open world is the default, not an option** (2026-09-25: nobody picks a linear game in
Archipelago). The target: the world open as if the story were done, nothing collected, the ending gated by the
artifact count. **Built one gate at a time, never by forcing chapters done** (decided 2026-09-25): "chapter done"
is the artifact flag the goal counts, and a finished world is hundreds of story flags, many of which remove
locations (bosses beaten, characters gone, quests closed, cutscene gifts skipped). So each gate (a blocker, a
door, a guard, a story flag) is opened by the seed on its own, tested on screen, and known to the logic; key
items and abilities become the real gates (the Peculiar Gem for Upper Snakemouth); story events and bosses stay
as locations. The goal stays "collect N artifacts". The ending's gate is researched without spoiling it for the
tester, who hasn't finished the game. Regions were whole areas at first; since 2026-09-30 every map is a region (build
step 12); each room's areas as regions (doors from the
dump, `dev-scripts/door-graph.py`; build step 24) come as the gates open (2026-09-24; areas within a room since
2026-09-27).
**No gate may depend on a flag a reload can undo** (the user, 2026-09-30, asking what a death without saving costs
with DeathLink): story flags live only in the save, so a Game Over that loads an older save undoes them (build step
25). Every gate is held by what a reload can't undo: `slot_data`'s lists, a received item, or a checked location, as
checked pickups are hidden (build step 27). A death then only replays scenes. Checked when each gate is built; the
world's one shape is Next 52, a beaten boss kept beaten Next 53.
**Areas and doors that close later are kept open** (2026-09-24), as Pokémon Emerald's apworld keeps Mirage
Island visible: the mod makes the game's `CheckIfCanExist` answer "hide" for a list of blockers
sent in `slot_data`, decided at generation, with no save writes. Each is checked in game first; where forcing
one open breaks the story state, its locations are left out instead. **First case, built 2026-09-24:** after the
first boss, Eetl turns you back outside the city (`eetlblocker1 - Duplicate`, Event12, until chapter 2's flag
67), closing the way back to Snakemouth Den. Event12 only walks the player and sets no flags, so it's
safe to remove. Its area's module (`logic/`) lists it under `KEPT_OPEN`, `slot_data` carries it, and the
mod's `KeptOpen` gives that entity a marker `limit` array after the map creates it, which its prefix
on `CheckIfCanExist` answers with "hide" (test `TestKeptOpen`). **Seen (2026-10-04): a second trigger**, `eetlblocker1`,
stands from flag 114 (Eetl following the party) and turned the party back; kept away too (`MEASURED.md`, save crystals).
**The save tutorial too** (2026-10-04, the user: "we can just remove event19 all together"): its trigger outlives its
actors (`MEASURED.md`, save crystals), so after the first boss it played to no one; `SaveEventTrigger` is kept away the
same way (test `test_save_tutorial_trigger_is_kept_away`), so Event19 never
plays in a seed and the spider scene's own crystal lesson stays. Day/night map pairs are made
reachable both ways (like the Emerald apworld's Shoal Cave tides). One-way drops stay as they are: the logic handles
one-way connections.
**Keeping ways present** (2026-09-25: no dead end in chapter 1, and the Gem opens chapter 5 whenever
it's found). The reverse of kept open: an area's module lists under `KEPT_PRESENT` entities the story only makes
later, `slot_data` carries them, and the mod's `KeptOpen` gives each a marker `requires` array right after the map
creates its entities, which its `CheckIfCanExist` prefix answers with "exists". First three: Snakemouth's big door
to Upper Snakemouth (its model already looks open from the trapdoor fall, flag 14, per the map dump; the Peculiar
Gem slot behind it is the real gate), and the fall room's bounce mushroom and door back up (from the first boss in
vanilla). The fall room's chapter 1 blocker joins `kept_open`. The ordinary door down into the fall room is left
alone: before the trapdoor event it would skip that event, where Leif's joining starts. Tests `TestKeptPresent`.
**Built 2026-09-25, not yet seen in game;** it needs a fresh file played through chapter 1, to see that the spider
fight and Leif's joining still play out with the way back up open.
**Seen, and a hole found** (2026-09-25): the bounce mushroom and the door at its top were there before the
first boss, and the tester went up. But then there was **no way back down**: the door room's door to the fall room
(`LoadZoneFallRoom`) also needs 41, and before the boss the fall room is reached only by the trapdoor drop. Going up
before the spider fight cut the file off from Leif, the spider fight and the lake. Keeping one direction of a
connection open means checking the other direction too. The fix, a new `slot_data` list, `present_from` (map, entity,
flag): the entity's `requires` becomes that earlier flag, so the door exists from the trapdoor (14) on. Not from the
start, which would skip the trapdoor scene where Leif's joining begins. Test `test_way_back_down_from_the_trapdoor`.
**Confirmed on screen (2026-09-25):** on a chapter 1 file before the first boss, the tester went up and down between the
fall room and the door room several times, and through the big door and back; then on to the spider fight. The
spider fight (Event6, discovery 1) and Leif's joining at the lake (Event14, flag 16) then played out as in the game,
with no errors: the ways kept open don't disturb the chapter 1 story (2026-09-25).
**The Outskirts rocks** (2026-09-25: before chapter 1 is done, a rock pile cuts the Outskirts off, so
only the way to chapter 1 is left). Everything that changes on that map at the end of chapter 1 is flag 41, read
from the entity dump and the map dump's flag-scenery list: the rocks (`Base/BlockingRocks`, scenery hidden from
41), the Golden Path exit (needs 41), NPCs swapping. The rocks guard three things: the ladybug siblings' house (no
gate of its own), the bottom-right exit (whose load zone still needs 41, so it stays a dead end) and the town
door. The town door's first visit is a trigger with no gate of its own either (Event60: the first-entry scene
into the city, which sets flag 107), and the city is written for chapter 2. So two new `slot_data` lists:
`scenery_hidden` (map plus the object's path in the map; the mod's prefix on `ConditionChecker.Start` gives it a
marker `limit`, which the existing check answers with "hide") and `held_until` (map, entity, flag; the mod adds the
flag to the entity's own `requires`, so the game keeps the trigger away until the first boss and brings it back
after). Rocks and house were chosen now, the town as a later gate of its own. The house's logic stayed cautious
(it needed the first boss) until play showed otherwise. Tests `TestOutskirtsRocks`.
**Seen (2026-09-25), chapter 1 file:** the rocks were gone, the tester walked into the house (the ladybug
siblings, who come with flag 41, weren't there) and took its item: the seed's Crystal Berry, check sent from flag 679.
So the house now needs nothing. With the rocks gone the east road opened too; there the tester knocked a stone with
Kabbu's horn and found a Drowsy Cake (flag 735), now a location (*Outskirts: East Road, Boulder*), and picked
up crystal berry #10 at the pier with no abilities (*Outskirts: Pier, Behind the Dock*). The miners working
at the rocks (gone from 41 in the game) mine nothing now, so they join `kept_open`. The test
`test_only_what_play_showed_before_the_gate` pins the locations reachable before the permit (five then, with the pier's
crystal berry; the shops and more have joined since). **The town door does nothing before the first boss** (seen on
screen, 2026-09-25: "the entrance does not work/do anything"), which is the held trigger. **The lists must reach a map
already loaded:** after a plugin reload or a seed change, a map loaded before the login is built as vanilla: the rocks
were seen coming back until the tester left and re-entered. `KeptOpen.Tick` now applies a newly arrived set of lists to
the current map (the same marks as at map load, and the scenery hidden the way `ConditionChecker.Start` hides it); the
log shows it removing the miners on the Outskirts right after a reload.
**The town waits for its companion** (2026-09-25). Tried with Leif (as asked: try the city with Leif): its first-entry
scene lines up Vi, Kabbu and Leif by character (`GetEntity(-4)`, `-5`, `-6`), so the hold moved to Leif's flag 16 for
one seed. With Leif added, the scene loaded the plaza and threw `ArgumentOutOfRange`: its fourth entry is
`GetEntity(1000)`, the map's first temporary follower, a companion who joins in Event63, the scene outside the city
after the first boss, which also sets flag 114 (`EventControl.cs:10034-10035`). Held until 114 for one seed, but that is
the first boss in practice, and the town is wanted open from the start ("especially if we are trying to make
this game openworld"). The arrival scene gives no check and its only effect is flag 107, which nothing but the city
doors reads (entity dump, map dump, ScriptDump). So **the scene is removed** (`kept_open`) and **the real door kept
present** (`kept_present`): the town is a plain door from the start, no companion needed. The plaza has no scene of
its own, and its blockers keep the party in it until chapter 2 starts. That start (Event45, the palace) lines up the
same companion, so its trigger is held until 114 (`held_until`), and in logic *Chapter 2 Start* now requires the
first boss explicitly, while the city region needs nothing and the *Entering the City* story event is gone. Tests
`test_town_open_from_the_start`, `test_the_city_is_open_from_the_start`, `test_chapter_two_needs_the_first_boss`.
**The companion in ordinary lines:** a theater NPC's line in the city also asked for the companion
(`GetEntity(1000)` inside `SetText`) and threw. The city is written for after chapter 1, so more lines will. A
fallback was chosen over closing the town again: `PartyFit` answers a follower lookup that finds nobody with the party's
leader (no crash; the companion's line comes from the leader) and logs each map and id once, so a scene that truly
needs him can be held back individually. Scenes that read `map.tempfollowers[0]` directly, like the palace's Event45,
aren't covered and stay held. **Seen (2026-09-25):** the plaza NPC's conversation played through; the log
shows the companion asked for on `BugariaMainPlaza` outside any scene and the leader answering.
**The rest of the town** (2026-09-25: "can we remove the block here"): three blockers in the plaza (`MM`,
`blockereetl2` and its duplicate, Event12, until flag 67) kept the party in the plaza. No city map has a scene that
starts on its own, and every other city scene trigger needs flag 67 or later (entity dump, map dump), so they join
`kept_open` and the districts can be walked early. The palace's own blockers stay (the story goes on there). The
districts' checks came into the logic one by one as they were seen; today only the old book quest needs chapter 2.
A fourth, `MakiBlock` (a dialogue trigger from the bridge scene, flag 66, until 67), had Maki turn the party back
toward the palace; kept away too (2026-10-04, the user: "lets remove this block"). Test
`test_plaza_blockers_removed`. With the blockers gone the exits still did nothing (seen on screen): the plaza's
doors to Commercial, Residential and the theater require flag 67 themselves, and a `Cube` in the plaza hides at 67. The
three doors join `kept_present` and the cube `scenery_hidden`. Lesson: an area closed "until chapter N" is closed by
several things at once (blockers, doors, scenery); list every entity and scenery piece gated by that flag before opening
it. First find in the open town: the Bad Book (key item 174, flag 621) outdoors in the residential district, reached
with Kabbu's horn before chapter 2 (2026-09-25): a location in the *Bugaria City* region then (its
map's, `BugariaResidential`, since every map is a region), open from the start. Then the
Bug Me Not! medal (flag 59), also outdoors in the residential district, which needed Leif's ice: the same region,
requiring Leif (test `TestTownMedal`).
**The bar and the quest boards** (2026-09-25). The way down to the underground bar (Shades's crystal-berry
shop, and a bounty board with no gate) is a spot examined on the Commercial map whose last line, the way down, needs
flag 135. That flag also brings story characters and a scene (Event79) to the district, a battle helper and more, so
it isn't set: a new `slot_data` list, `dialogue_flags` (map, entity, flag, to), repoints that one line to flag 691,
which the new-game scene always sets. An entity picks the last line whose flag is set (`NPCControl.cs:4320-4326`), so
the way down is always taken. The town's and the Outskirts' quest boards (requires 67) join `kept_present`; each quest
still needs checking before the logic counts on it. Tests `TestBarAndBoards`. Seen (2026-09-25, `log.md`): the
bar and both quest boards.
**Madeleine's house on the Outskirts, open from the start** (2026-09-25). Everything tied to it is flag 390,
set by one conversation in a later area: a locked-door check (`lockeddoor`, until 390, now `kept_open`), the real door
(`doormadeleine`, from 390, `kept_present`) and a lock (`Base/lock (1)`, `scenery_hidden`). Inside, only her and her
butler need 390, so they stay away and none of her story starts early; the rest is two plain pickups, a Burly Tea
(flag 686, the retired location 4, back under id 44) and a Lore Book (flag 392, id 45), both reachable from the start.
Test `TestMadeleinesHouse`. **Seen (2026-09-25):** walked in on a chapter 1 file and took both; the swap
showed the seed's items (a Sleep Resistance medal, a Crystal Berry) and both checks went out.
**The boat to Metal Island crashed with two in the party** (2026-09-25). With the rocks gone the tester reached the
pier on a chapter 1 file, paid the fare, and the boat scene (Event107) threw IndexOutOfRange: it seats three party
members (`p[0..2]`, `EventControl.cs:17944-17946`), and in the game the pier is behind the rocks until the first
boss, so Leif is always there. Opening a gate means checking every scene behind it for what the story guaranteed.
The sailor joins `held_until`, waiting for Leif (flag 16); Leif was chosen over the first boss, because a party
rule suits a random start later, when party members may be items and this becomes a party-size check. **Removed
(2026-09-26):** with the stand-ins (the mod guide, step 11) the boat scene asked for Vi and Kabbu, got
invisible stand-ins, and the boat left with Leif alone, the fare waived by Free boat. The sailor is always there, as in
vanilla ("before we added the bandage workaround"), and Metal Island's checks need no party member for the boat.
**The plaza's discoveries open from the start** (2026-09-25): before chapter 2's briefing (flag 67) a
stand-in (`Discovery Pre Briefing`, a Check saying "We can check this out later. Let's hurry to the castle.") stands
where the plaza statue and the inn portrait will be; the discoveries themselves (`StatueDesc`, Event38, discovery 5;
`InnPortrait`, Event37, discovery 4) require 67. Both stand-ins are kept out of the way and both discoveries kept
present (`kept_open`, `kept_present`); the two events ask for members by name and set no story flag. Test
`TestKeptOpen`. Seeds generated solo and with APQuest; **seen:** the statue can be examined.
**The inn from the start too** (2026-09-25: couldn't stay while escorted): the innkeeper's default line
(53) hands the talk to the follower, "We mustn't keep the Queen waiting."; from flag 67 line 1 offers a stay. The
line's flag is repointed to 691 (set by every new game, `dialogue_flags`, as for the bar entrance). Test
`TestKeptOpen.test_inn_open_before_the_briefing`; seeds solo and with APQuest. Not yet seen. Also found: the stay
costs a fixed 9 berries, "3 berries a bug" written for three (`checkmoney,9` then `money,-9`, line 2), whatever the
party's size.
**Chapter 2's three opening scenes held in story order** (2026-09-25: "it's the only place the follower is
removed and changed to another"): after the first boss a follower joins outside the city (Event63: follower 30, flag
114); the palace bridge swaps them for Maki (Event44: removes 30, adds Maki, flag 66), the only place follower 30
leaves; the briefing (Event45) uses Maki and clears the list. In the game the town opens only after chapter 1, so the
bridge always comes after the boss. **The briefing waits for the swap itself** (2026-09-25: a shuffled door
or a random start inside the palace could reach it without the bridge, with no follower or the wrong one): its hold
moved from 114 to 66, so first boss, follower, swap, briefing, whatever the way in. No new logic: the bridge is in the
town, which *Chapter 2 Start* already needs. With the town open from the start the bridge could come first: Maki early,
and follower 30 never leaving. The bridge's trigger (`makiautoevent`) is now held until 114, like the briefing's already
was (`held_until`). Test `TestKeptOpen.test_follower_swap_waits_for_the_first_follower`. Not seen (the tester's file is
past it).

**Every quest board lists every open quest** (2026-09-25: all boards should act the same). The game keeps
one list of open quests, but each board filters it: the five bounties show only on the underground bar's board, and
every other board hides them (`MEASURED.md`, "The quest board"). In a randomizer save the mod drops that filter, so
any board, the town's or the starting house's, offers bounties too; what the game hides everywhere (the chapter
entries, Leif's) stays hidden. The logic needs no change: it never counted on a board, only on the quest's own
region. **Next, the starting house's board from the start:** it waits for chapter 2 (flag 67), and so does its
caretaker, an Eetl in the house whose line takes the quest; whether to keep him present early or have the mod play
that line is decided after reading it in game. *Code: `QuestBoards.cs`.*

**The exits near Snakemouth Den open from the start** (2026-09-26: "go to the cave first", a turn-back
before chapter 2). On `NearSnakemouth` two Event12 triggers (`BlockLeft`, `BlockRight`, hidden by 41) guard two doors
made only from 41: `loadingzonechuck` into Chuck's Abode and `loadingzonefields`, a shortcut to the first corridor.
The triggers are `kept_open` and the doors `kept_present`; no scenery there waits on 41 (map dump). Inside Chuck's
Abode nothing waits on the story (Chuck, a save point, crystal berry #3 behind a rock). No logic change: nothing there
is a location yet. **Owed:** Chuck's quest (flag 44, the Mighty Pebble) becomes reachable from here, so when it becomes
a location its rule is this map's region plus the quest's own needs (as pointed out: take the quest
from a board, have the chef in town (by the two shops) cook a Hearty Breakfast, deliver it to Chuck here. **Decided
(2026-09-26):** the rule requires the chef's cooking, even if a Hearty Breakfast can also be found or bought (the
cautious side); the cook's own gates to check in code before the rule is written).
Test `test_near_snakemouth_exits_open_before_the_boss`. **Seen (2026-09-26):** walked into Chuck's Abode before the
boss; resting and the save point work there (a dead end with a rest and a save, before the cave).

**The way to Snakemouth Den never closes again** (2026-10-04, the user: "its not allowed to be closed after it has
been opened"; seen closed after the first boss). The Explorer Permit opens the Outskirts gate (`Base/Gate/SnekGate`,
gone from flag 28), but the game closes the way twice more (the map dump's flag-scenery list, the entity dump): a second
gate, `Base/Gate/SnekGate (1)`, shown from the first boss (41) until chapter 2 (67), and from 67 for good a gate by the
cave on `NearSnakemouth` (`map1v4 (1)/snakemouthgate` and its `Gate`) with a `guard` and his `sign` in front of the door
to the cave. The scenery is `scenery_hidden`, the guard and sign `kept_open` (unless *Extra Roadblocks* puts them up
from the start, build step 48); the permit's own gate stays. And its gatekeeper (`FxdColGatekeeper`, who opens it for
the permit) leaves at the spider scene (limit 27), so a file that got
past the gate another way (a dev warp, seen; a random start inside) could never open it: he is `kept_present` (the
user: "the npc should always be here, and open the gate with the permit"). The logic already counted the cave open
past the permit, so only the mod changes. Test `TestSnakemouthGateStaysOpen`.

**The desert border's gate (2026-10-04).** A random start fell forever behind the shut gate between the Lost Sands and
the Far Grasslands (build step 15). The game makes the border's door to the Far Grasslands (`loadzonefg`) and breaks the
gate only from flag 348 (chapter 5): the entity dump and the map dump name the door (`requires 348`), the intact gate
`Base/Gate` (until 348) and `Base/GateBroken` (from 348). The logic already had the door as always passable (no
`DoorRule`), so the game was stricter than the logic. **Decided (the user):** open it, "this one is a softlock if it
stays a oneway so i think we should just open it up", as the mod opens everything else: the door `kept_present`, the
intact gate hidden and the broken one shown (`logic/lost_sands.py`); flag 348 itself is never set. The guards and
chapter 5's own scene there are left to the story. An exception, on the user's word, to Next 52's "nothing opens before
its logic exists"; the logic already counted the door open. **And the rule from here (the user, 2026-10-04):** "we will
have to check every entrance/door anyway along with every single flag in the game ... so we should either open things
up or mark them as oneways": every door and flag gets that verdict as its area is mapped (`room-logic.md`).
**The Golden Path door (2026-10-04, the user: "can we open the entrance where im standing").** The game makes
`LoadZoneGoldenPath` only from flag 41 (the first boss), and the logic had a `DoorRule` for it. Past it is one room
(`BOGoldenPath`: location 12, a dig spot, the Hermit's cave); its tunnel onward needs 67. Its blocker (Event12, until 67)
turned out to span the way to the cave too, so it is kept away (`MEASURED.md`, the Golden Path). **The tunnel too**
(the user, the same day: "yes open it, and keep it open from the start"): `Loadzonetunnel` kept present. It joins
rooms the logic already had (the Golden Hills on to the Barren Lands and the Lost Sands); their few locations still
wait for the later chapters (`LATER_CHAPTERS`), so the logic promises nothing new past it until those rooms are mapped.
**The Golden Settlement's desert gate** (the same day, met walking in): its switch swung it open, but an invisible
wall behind it stands until the desert side has been reached (flag 170). The logic already counted that door open, so
the game was stricter: the gate is shown open from the start and the wall hidden (`logic/golden_settlement.py`; test
`TestSettlementDesertGate`; `MEASURED.md`, the Golden Settlement's desert gate). No attack is needed for the switch.
**Corrected 2026-10-07:** keeping the gate open was my reading, never the user's ask; the user: the door is blocked
"until you come from that entrance specifically so you can hit the lever behind the door and open it". The gate is the
game's again, its lever (any attack, the desert side only) an event the door needs both ways; the wall stays hidden.
**Beette, the Flower Key's seller** (the same day, the user: "make it appear always if required"): the `smug bee` on
`BeehiveBalcony`, made only after chapter 3 (flag 299), is kept present (`logic/bee_kingdom_hive.py`, test
`TestFlowerKeySeller`). Her sale is still the game's own, not a location; the key and the red house it opens weren't in
the logic yet (`MEASURED.md`, the Flower Key); her sale became a location the same day (build step 46), and the red
house's roof one on 2026-10-06 (the main plaza, room by room), the key progression with it.
**The Rubber Prison yard's rock** (the user: "it makes you get stuck/softlocked normally"): `rock` just inside
`RubberPrisonPier`'s left door, broken only by Horn Dash, until flag 589, is kept away (`logic/rubber_prison.py`, test
`TestPrisonYardRock`; `MEASURED.md`, the Rubber Prison yard's rock). **Its checkpoint corridor, one-way both ways**
(the user: "this entrance is potentially a oneway if the door is closed ... the other side of this room is also a
oneway if you don't have the explorer permit to go back / a basic attack to hit the switches"): from the yard the
should never lead on; across to the yard from the far side it needs any attack for the switches (`ANY_ATTACK`); back
to the spike room its prison door needs the Explorer Permit (corrected by the user: "the permit is for opening the door
for the other entrance"). Test `TestPrisonCorridor`. **The yard side isn't written yet:** a never-passable door
(`False_`) is skipped by Archipelago's `create_entrance`, and forced into being it broke the decoupled entrance
randomizer (the batched run, 2026-10-04); it needs the corridor split into two areas (Known issues). Now `kept_present`
from the start, the rule gone; location 12's beetle grass takes Kabbu's horn, so it needs `Horn Slash` of its own (the
boss had implied it). Tests `TestGoldenPath`.
**The arcade, always there** (the user, 2026-10-06: "we should make it so the arcade is always here"): on
`BugariaCommercial`, the arcade's door, sign, games, helper and exchanger are made only from flag 350 (Elizant's
welcome after the first submarine landing), and two miners stand in that corner until it. The nine are kept present
and the two miners kept away, and its building (`Model/TermiteArcade`, from 350) shown and the empty lot's fence
(`Model/Base/EmptyLotFence`, until 350) hidden (`logic/bugaria_city.py`). Seen through `liveslot` (2026-10-06, the
user: "yee it works now").

**Status:** in progress: the Outskirts rocks, the fall room both ways, the town and its districts, the plaza's companion
fallback and statue, Madeleine's house, and the bar with its quest board seen on screen (2026-09-25); the exits near
Snakemouth Den seen (2026-09-26); Eetl's blocker seen gone (2026-10-04); Maki's plaza block kept away (2026-10-04,
in the log, not yet seen); chapter 2's held scenes seen in order after the first boss (2026-10-04,
decoupled doors: Eetl, the bridge with Maki, the briefing), before it not yet tried; every board listing bounties (built
2026-09-25) and the inn not yet seen (the boat's hold was removed, 2026-09-26); the open start is always on, not an
option (2026-09-26); the desert border's gate seen broken from a new file, the door both ways with no fall (2026-10-04;
its guard still stands, left to the story); the Golden Path door, the way to the Hermit's cave and the tunnel onward
seen open from a new file (2026-10-04); the Golden Settlement's desert gate and its wall seen open
(2026-10-04); the way to Snakemouth Den kept open after the permit and its gatekeeper kept: seen (2026-10-04, past
the first boss, the gatekeeper took the permit and the gate opened); chapter 2's gate, guard and sign seen gone
(2026-10-04, flag 67 read back set on the file); the Lost Sands' guard kept away and its gate open (`BOLostSandsEntrance`,
until flag 130, `MEASURED.md`), seen (2026-10-05), test `test_lost_sands_gate_is_kept_open`.

*Code: the lists in `logic/*.py` (`KEPT_OPEN`, `KEPT_PRESENT`, `SCENERY_HIDDEN`, `SCENERY_PRESENT`, `HELD_UNTIL`,
`PRESENT_FROM`, `HELD_UNTIL_ITEM`, `DIALOGUE_FLAGS`, gathered in `logic/__init__.py`), sent by `slot_data.py`; in the
mod `KeptOpen.cs`, `PartyFit.cs` (the follower fallback) and `QuestBoards.cs`.*

---

## Build step 10: more kinds of location (crystal berries, quests, discoveries, pickups, boss medals)

Beyond floor pickups and gifts, a location can be a berry reward, a crystal berry, a pickup that comes back, a story
pickup, a quest's reward or its middle, a boss's prize medal, a journal entry, or later an enemy's first defeat.
Optional kinds each get a yaml toggle that states how many checks it adds.

**Berries are shuffled like items** (2026-09-24): a berry reward is the same `giveitem` as an item, type
-1, so it's a location, and its amount goes into the pool as an item such as *10 Berries* (kind 3, its own id range;
filler). Two checks can share one flag: the delivery quest pays 15 berries and a Lore Book at flag 243, so both
are locations and are sent together. In the mod, receiving berries uses the game's own money reward (capped at
999); at a berry location the command is turned, just before it runs, into a hand-over the item swap already
handles (`BerryPrefix`). **The pool is now the included locations' vanilla items**, plus the items the options add
(members, abilities, the mod's own), plus padding: an item whose vanilla spot isn't a location (the Plushie at the
theater) stays with the game (test `TestPermitGate.test_pool_is_the_locations_items`). **Crystal berries** (2026-09-24:
the first thing you pick up): a counted currency (`flagvar[14]`, the crystal berry shop's counter), 50 berry spots each
known by its `crystalbflags` index. A berry location's check is that index (`location_berries`), the pickup is
recognised by it (`data[0]`), and all of them hold the one item *Crystal Berry* (kind 4). The mod undoes the count the
pickup code already raised, keeps the berry's "taken" mark, shows the seed's item (a berry is a 3D model, so the model
is hidden for a sprite), and drops the first-berry tutorial; receiving one raises the count. First location: berry #0
outside the cave (test `TestCrystalBerries`). They're a yaml category, *Shuffle Crystal Berries*, on by default (some
are obscure, like quests; test `TestCrystalBerriesOff`).
**Respawning pickups** (2026-09-24, always shuffled, no option): some floor items have no flag of their
own, only a *regional* flag the game wipes on every area change, so they come back. They're locations too: the
first pickup sends the check and gives nothing, and once the check is done the spot is the game's own again, with
its vanilla item each time it comes back (so it stays useful locally; with *Shuffle Shop Inventories*, another
spot's item, build step 34). How it works:
1. The apworld marks such a location with `source.regional`, its regional flag, and `slot_data` sends it inside
   `location_pickups` (`"regional": N`, flag -1). The client recognises the pickup by map plus regional flag.
2. The game sets nothing that stays in the save, so the check can't be read back later like a flag. The mod sends
   it from the pickup itself: `ItemSwap`'s pickup prefix queues it, and `LocationChecks` sends the queue each tick
   while connected.
3. "Done" is the server's checked list from the last login, its updates, and the checks queued here. A pickup made
   while the connection is down waits in the queue, tagged with its save's seed, and is sent after the next login
   to that seed. The queue is only memory: if the game closes first, the spot shows the seed's item again and the
   pickup is made again. Nothing is lost and nothing is doubled.
Tests: `TestRespawningPickups` (the client gets the regional flag and no flag entry; the vanilla item is in the
pool; the logic's region) and `TestSlotData` (every location watched exactly one way). First three: chapter 1's
Snakemouth underground (a Honey Drop, a Mushroom and a Crunchy Leaf). **Seen in play (2026-09-24):**
each first pickup showed the seed's item and sent its check, and after an area change the Honey Drop came back
and gave a real Honey Drop with no check. Two are named from the tester's description (*Underground Door Room,
Pillar*; *Underground Bridge Room, Behind Pillar*, hidden from the camera); the Mushroom's landmark is still to come.
**A quest reward from the code** (2026-09-24): the lost ladybug kid at the lake gives a Lore Book once you've
beaten his monsters (Event31: `giveitem,1,52`, then flag 55, the check). He only appears after the first boss,
which is a story event of its own (*Snakemouth Den Cleared*, flag 41), and his cutscene moves all three party
members, so the location requires Leif and that event (test `TestLostKid`). Added from the code. The cutscene
also expects the kid's sister, the ladybug girl, to be following you (`FindEntity` of her character type): after
the first boss you talk to her outside the city and she comes along. Faked flags crashed it twice (no Leif, then
no sister), so it gets tested when a file reaches the first boss by play.
**Story pickups** (2026-09-24): some pickups have no "taken" flag of their own; the story makes them
appear and hides them for good (the trapdoor Mushroom in the Snakemouth door room exists between flags 13 and 14,
and taking it starts Event5, which sets 14). The rule: a pickup is a location if, once taken, a story flag hides
it for good; items that come back are not. A story pickup is known by **the story event picking it up starts**
(`source.event`, sent in `slot_data`; the pickup's `data[1]`), not by its entity name: the play-through log showed
the scene creating its own copy, `tempitem`, while the map's `MushroomItem` only appears on a later visit, and both
start Event5. Its check is the flag that event sets, and an option that skips the event can leave the location
out (test `TestStoryPickup`). For a future story strip or open world, the
mod could force such a pickup to exist (like the doors kept open) and send its check on pickup, making it
independent of the story.
**Hard Mode boss prize medals** (23, `MEASURED.md`) are always paid, with no option, and each becomes a location
when its boss is in the logic (today the first, location 13): every boss pays
its prize as if Hard Mode were on, whatever the player's setting, so a prize can never be skipped and its
location is simply "beat this boss". The mod does that by widening the Hard Mode test inside the game's
own `AddPrizeMedal`, never by writing the prize slot itself. **The Archipelago panel gets a
row, "Difficulty: Normal / Hard / Hardest",** that only adds a way in: *Hard* acts as if the Hard Mode medal
(Artis's, #11) were equipped, *Hardest* as if the save had been started with the HARDEST code (the game's
two levels, `MEASURED.md`). Equipping the medal or typing the code still works as the game made it, and
the prize medals are paid out on every setting. Logic never needs either (2026-09-24). **Only boss prizes**
(an audit, 2026-09-28): one of the 23 slots (medal 24) is a dialogue gift the game marks missed when skipped, so the
missed-prize payout skips it and the game sells it at the caravan as usual (`MEASURED.md`, Hard Mode boss prizes). For
*Hardest*: its extras read flag 614 directly in about 35 places, so it means setting that flag. **Measured
2026-09-24: the game keeps no other record of a typed code.** `flagstring[10]` is only the typing buffer,
emptied as soon as the code is accepted (`EventControl.cs:2450-2455`), so flag 614 is the code's only trace,
so the mod marks a 614 it set itself, and keeps it out of every save (chosen 2026-09-24:
switchable, the save stays clean; the mod guide, step 15).
**Built 2026-09-24 (not yet seen in game):** each tick outside battles and events, a prize slot reading "missed"
(2) is paid through the game's own `AddPrizeMedal(slot)` with Hard Mode answered "yes" for that call, because a
boss's event can write 2 directly on Normal (the first boss's does; the others go through `AddPrizeMedal`,
`MEASURED.md`, Hard Mode boss prizes). Artis's `Event33` then hands the prize over
with a `giveitem` the swap handles, and the location is done when the slot reaches 3 (`location_vars`, a number
slot instead of a flag). First location: *Outskirts: Artis's Prize for Snakemouth Den* (Quick Flea, seen
for sale at the caravan and bought after a Normal kill: the missed-prize path, confirmed on screen).
**Planned (2026-09-25): everything in, placeholders for what isn't checked.** Every item, medal and other spot in
the game to be added, so everything is randomized. A spot whose logic and name haven't been checked yet is
a *Placeholder*: "Placeholder" in its name, and it holds filler only (Archipelago's excluded type), so no progression
item from any game lands where the logic may be wrong. Its own vanilla item still goes into the pool and lands at a
checked spot. Each placeholder is promoted to a normal location once its requirements and name are checked, one at a
time. **Optional categories** (2026-09-24): a location can carry a `category`; its yaml option decides whether
the seed includes it. *Shuffle Quests* (on by default) covers quest-board and side-quest rewards; one-off NPC gifts
will have their own toggle. With a category off, its locations aren't created, their vanilla items stay out of the
pool, and they're left out of `slot_data`, so the client never swaps them and the game hands them out as usual
(tests `TestQuestsOff`, `TestQuestsOnByDefault`).
**Each toggle says how many checks it adds** (2026-09-25: so people know what they're getting into). The
option's description, which Archipelago copies into the yaml template, ends "Checks added in this version: N.", with
N counted from the location data when the world loads (`options.category_count`), so it never goes stale; test
`TestOptionCounts`. Seen in a generated template on 2026-09-25.
**Journal locations, each its own yaml option (2026-09-25).** The journal is `librarystuff[type, n]`,
set through `MainManager.UpdateJounal`, so a check can be "this entry became true", with no item to swap.
*Shuffle Discoveries* comes first (the simplest). **Built 2026-09-25**, opt-in (off by default): a location
source `discovery: n`, sent as `slot_data`'s `location_discoveries`, and the mod's `LocationChecks` sends the
check when `librarystuff[0, n]` turns true. No item to swap: a discovery gives none, so its pool slot is padding.
Where each discovery is came from four sources: `|discovery,N|` in map dialogue (ScriptDump), about 37
`UpdateJounal` calls in events (which event sets which), a few set from story flags in code, and each map's own
list (`MapControl.discoveryids`, MapDump). The five in regions the logic already has are locations: the pier
statue (49), the arrival outside Snakemouth (0, Event11), the spider fight (1, Event6), the bridge room's hidden
spot (2, Event13) and the Underground Door Room's statue (3, Event27; once called its grass, corrected
2026-10-04). **First check seen:** the tester had examined the pier statue before the option existed; joining the
new seed, the mod found discovery 49 recorded and sent *Outskirts: Pier, Ship's Wheel* (then *Pier, Statue*).
**First discovery seen live** (2026-09-25): the tester examined the bridge room's hidden spot, Event13 recorded
discovery 2, and the check went out with the seed's item back. Tests `TestDiscoveriesOn`,
`TestDiscoveriesOffByDefault`. **Parked:** *Shuffle Bestiary* (Next 44; an entry
comes only from Spy in battle or from Event65's catch-up NPC, who sells entries for enemies already fought, 19
berries, 49 for bosses, except the 23 in `excludeids`, which are the missable ones; seeing an enemy on the map
records nothing) and *Shuffle Recipes* (each needs its ingredients, which the seed may shuffle, so it waits
until the logic knows where ingredients come from). Two Quality of life rows go with the bestiary:
auto-spy (a fought enemy counts as spied) and free entries at the catch-up NPC, perhaps folded with Free boat into
one "no NPC costs" row.
**Later idea, a yaml option (2026-09-25): enemy drops.** The first defeat of each ordinary enemy type is
a check (bosses and one-off fights left out), shown as a guaranteed drop; after that the type's drops are the
game's own, as with respawning pickups. The game already counts defeats per type in the save
(`enemyencounter[id, 1]`, raised on each win, `BattleControl.cs:30712`), so the check can be "that count reached
1", with no drop to swap. The logic needs, per type, a place where it's always fought.
**Or per placed enemy, "enemy sanity" (2026-09-25), its own opt-in toggle:** each enemy standing on a
map (map plus entity index) is its own check on its first defeat, so the same enemy type in another room is
another check; afterwards it's the game's own again, as with respawning pickups. The entity dump holds 327 placed
enemies on 124 maps (some are one spot in different story states, swapped by flags, so fewer real spots). To
measure first: how a won battle knows which map enemy started it, and whether the mod has to keep what's done
(like respawning pickups, since nothing in the save marks a single map enemy beaten).

**Dig spots (2026-10-05, found mapping `OutsideSnakemouth`):** a dig spot that buries an item (`data[0]` 0) copies its
own one-time flag onto the item it drops, as cut grass does (`NPCControl`, `DigSpot`), so it's an ordinary pickup
location with Beetle Dig as its rule. The first, *Outskirts: Snakemouth Den Entrance, Dig Spot* (a Spicy Berry, flag
683), dug in a seed before it was a location: no check, the item granted locally. Fifteen such spots with a flag in
the entity dump, the other fourteen not yet locations (a survey, 2026-10-05; `MEASURED.md`, "World pickups").

**Status:** in progress: respawning pickups and the game's missed-prize path (2026-09-24), discoveries (2026-09-25) seen
on screen, crystal berry spots too (the mod guide, step 9), received berries too (2026-09-28, build step 27); a berry
location's hand-over too (2026-10-04, *Outskirts: Near Snakemouth Den, Reward*); the prize payout seen (2026-10-04,
Artis on Normal handed the seed's item for the first boss); the lost kid's reward not yet seen in game; Placeholders
planned; bestiary, recipes and enemy checks parked.

*Code: `options.py` (`CATEGORY_OPTIONS`, `category_count`), `locations.py` (`category_on`), `slot_data.py`
(`location_berries`, `location_discoveries`, `location_vars`, `location_pickups`); in the mod `LocationChecks.cs`,
`ItemSwap.Pickups.cs` (`BerryPrefix`) and `CrystalBerryTotal.cs`.*

---

## Build step 11: shops as locations (medal shops, item shops, the caravan)

Every medal a shop stocks, and the first purchase of each item in an item shop, is a location. The shelf shows the
seed's item, a purchase sends the check, and purchases are made permanent like checks, so no reload or spending
order can lock one away.

**Medal shops (2026-09-25), being built.** Each medal a shop stocks is a location; the shelf shows the
seed's item, buying runs the shopkeeper's `giveitem` (swapped as for a gift), and the check is the medal leaving the
stock (`badgeshops[shop]`), which the save keeps. A done location shows as sold, so a reloaded save never charges
twice. A *Medal prices* bar on the Gameplay page (tenths of the normal price, 10 by default; the mod guide, step 10,
item 7; a dev cheat since 2026-10-09) scales the price columns. Merab's (berries) first:
berries can always be earned, so no lockout. **Shades's shop takes crystal berries, a consumable** (the
concern: consumable keys, lockout, savescumming): crystal berries are spent nowhere else (measured), and her stock
arrives in tiers whose Normal prices add up to 18, 25, 27, 40 and 50, exactly every berry in the game. **A tiered
rule (each item needs its tier's running total) was proposed and is wrong** (the question: what happens when the
stock grows): with a later tier already on the shelf, berries spent there starve an earlier tier, and a rule that
raised the need once a later tier opens would not be monotonic, which Archipelago's fill doesn't allow. **Decided
(2026-09-25): every Shades location requires all 50 crystal berries, and her full stock (all 13) is on the
shelf from a new game.** The first stops spending order from locking anything out; the second stops a story event
that never runs (as the open world skips or bypasses scenes) from leaving a tier's medals, and their checks, never
appearing. The same holds for Merab's later additions when they become locations. Crystal berries become
progression (useful until then, the user, 2026-10-08: "progression if the shade shop contain any progression, else
useful"; every spot there needs them, so they are progression whenever her shop is in the seed, whatever it holds:
Archipelago's fill counts only progression, `World.collect_item` skipping any item that isn't `advancement`, read at
0.6.8, and a spot gated by a useful item would never be reachable).
**Also wanted (2026-09-25): Shades's counter showing 3 or 4 medals** instead of 2. The slot count is the
shopkeeper's `data` length and each slot's place its `vectordata` entry (`NPCControl.cs:1530-1534`), so longer arrays
with new counter positions, set before the shelf is built. Built: 4 on her counter (the mod guide, step 12).
**Full stock from the start for both medal shops, duplicates as their own locations** (2026-09-25: "a 2nd
copy is a 2nd check", like the delivery quest's two checks on one flag). The story adds some medals twice (Merab: TP
Plus 1 and Ambusher 86; Shades: medal 6), so each copy is a location: Merab 22 (20 medals, two doubled), Shades 13
(costing exactly 50). **The mod owns each shop's stock:** the shelf is the full list minus the copies whose checks are
done. Buying removes a copy as the game does; one copy fewer than expected marks the next undone copy done. A reloaded
save with extra copies, or the story adding stock, is trimmed back to the list (a done location shows as sold). An
offline purchase stays in the save's stock and its check goes out on reconnecting.
**Built for Merab's (2026-09-25; seen since, the mod guide, step 12):** her 12 later copies are locations *Medal Shop
11* to *22* (ids 46-57), with 10 new medal items. "One copy fewer than expected" turned out unworkable: a fresh file
holds 10 of the 22 and would read as 12 purchases. So the save keeps a bit per copy bought (`flagvar[7]`, the mod guide,
step 12; slot_data's `location_shops` lists each copy's location with its shop and medal),
set by the purchase's swapped `giveitem`, and the stock is set from those bits and the server's checks. Test
`TestMedalShop` pins the 22 copies in story order.
**Item shops** (endless consumables): the first purchase of each item in each shop is a check that shows
and gives the seed's item, then the shop sells its own item again, like respawning pickups, so restocking still works
(with *Shuffle Shop Inventories*, another spot's item, build step 34).
Their own yaml toggle, *Shuffle Item Shops*, default on, apart from *Shuffle Medal Shops*. Built after the medal shops.
**Built for Madame Butterfly's shop (2026-09-25, seen working):** five locations (*Item Shop 1* to *5*, ids 58-62), one
per stock entry, known by map, shopkeeper and item (`location_item_shops`); each puts its own item in the pool, with
no `give` entry, so an unrelated `giveitem` of the same item on that map is never swapped. *Shop Contents* covers
them too. The buy line adds the item with `additem` (no item-get box), so the mod takes that command out when the line
is read and treats berries paid as the purchase; the check goes out through the respawning pickups' queue. Tests
`TestItemShop*`. The other shops follow the same data.
**The caravan from the start (2026-09-25, seen: all three bought, each check sent, then her own items):** its
keeper `Crickerly2` kept present, its stall (`Base/Stall`, scenery) shown through a new `scenery_present` list,
and `Crickerly1`, who stands there before it, kept away; its three items (Spicy Berry, Burly Berry, Magic Seed)
are *Outskirts: Caravan, Item Shop 1* to *3* (ids 63-65), reachable from the start. Its stock is fixed (the keeper's own
data); Crickerly's later stands on other maps are other shops. **Lines about the rocks**: every Outskirts line was
searched (`line` dev command): the waiting moth (`FuzzyMoth`, line 76) is kept away, the caravan husband's welcome
(line 78) answers to flag 691 instead of 41 (line 75 was the rocks), Crickerly1 (line 74) is gone with the caravan. Gen
and Eri, Artis and Eetl keep their flag-41 lines: those are after the first boss (the river, Artis's prize, Eetl leading
into chapter 2). The Seen (2026-09-25): the moth gone, nobody mentioning the rocks any more. The
ladybug siblings (flag 41) are present from the start too, so the map doesn't feel empty; their everyday
lines are neutral and their quest lines answer to the quest's own flags. Test `TestCaravan`.
**Shop Contents** (2026-09-25: shops are many easy checks in one place and soak up the good items, as in
Tevi): a yaml choice, *Anything*, *No Progression* (default) or *Filler Only*. No Progression is an `item_rule` on each
shop location refusing progression items from any game; Filler Only is Archipelago's excluded type (no progression,
no useful, `BaseClasses.py:1502`). 36 seeds (solo, with a second game, with discoveries) all generated.
**Filler Only falls back when the room can't hold it** (2026-09-25): an excluded spot takes only an item
that is neither progression nor useful, from any game, and with Merab's 22 copies a solo seed has 17 such items, so
generation failed. In `pre_fill`, once every world's items exist, the room's excludable items are counted against its
excluded spots; if short, this world's shops take No Progression instead, with a warning naming the player. 18 seeds
(solo, with APQuest, with discoveries, each Shop Contents) generated; the fallback fired solo and with APQuest (18
filler for 22 spots), not with discoveries on (22 for 22). Tests
`TestShopContents*`. **Its two bugs fixed (2026-10-03, Next 43 item 1):** the fallback runs after Archipelago has
applied the player's own exclusions and item rules (`Main.py`, 120-140), so it now leaves a shop the player excluded
excluded, and adds the No Progression rule with `add_item_rule` beside the item rules already there (local and
non-local items) instead of replacing them. It records that it fell back (`shops_fell_back`), which slot_data's
`options` sends as No Progression (build step 39). Tests `TestShopFallbackKeepsThePlayersRules`: both failed with the
old loop. **The caravan is there from the start**, built with the item shops. **Reloads refund currency** (the tester
caught this: buy, reload, keep the check and the berries), so purchases are made **permanent like checks**: spending
is tallied on the server (per-slot storage), each save brought in line on load (crystal berries exactly: received
minus spent; ordinary berries: the save's own paid record against the server's tally, the higher wins), and a
purchase made offline is recorded in the save and queued.
**Built for Merab's (2026-09-25):** no server tally needed. The save's bits (a bit per copy) are its paid
record, and the server's checked list says what was bought anywhere. A copy the server has checked with no bit in
the save was bought in another save, so on a save tied to the seed, outside events, the mod charges its price here
(clamped at 0, the rest forgiven) and sets its bit. Crystal berries (Shades's) still want the exact count.
**Seen in the log (2026-09-25):** the tester loaded a save with 0 berries that had bought nothing, while the server held
all 22 copies' checks: each was charged (0 -> 0, forgiven) and marked paid. Forgiving can't be farmed: a save only
ever ends with fewer berries. A save that has berries losing exactly the prices isn't seen yet (same code path). A done
location shows as sold. **Nothing requires a shop bought out**: the sold-out flags (587 Merab's, 588 Shades's, set
in `MainManager.cs:14283-14291`) only change dialogue (an NPC's line 159 on the Commercial map; Shades's
greeting, `checktrue,588,92`). A check that ever depends on a bought-out shop would need the full total, 50, still safe;
a seed holding fewer than 50 crystal berries leaves the unaffordable tiers out of the pool instead. **The bar is "no
action can make a check unreachable"**, not just "the logic never asks for it" (a consumable that can lock a check away
is a broken seed). Shades's checks meet it because (1) crystal berries buy nothing but her stock, (2) the stock costs
exactly 50, (3) all 50 are always obtainable and never taken away, (4) purchases are permanent. Every berry spent buys
one of her items, so what's left always costs what's left to collect. **Shades's shop is only shuffled when the seed
holds all 50 crystal berries**; otherwise it stays vanilla. Merab's has no such risk: ordinary berries are renewable
from battles. **Confirmed (2026-09-26):** a player who turns crystal berries off doesn't want to deal with them, so
Shades's shop is then not a location at all: nothing from the seed goes there (no progression, useful or filler),
and she sells her own medals. No berry-spot events keep her shop shuffled (proposed and dropped: they would make that
player collect every berry). When her shop is built, both yaml texts say so: *Shuffle Medal Shops* that Shades's
shop joins only with *Shuffle Crystal Berries* on, and *Shuffle Crystal Berries* that turning it off leaves her
shop vanilla.

**Status:** in progress: Merab's medal shop (her full stock of 22 from a new game, seen 2026-09-25; the mod guide,
step 12), Madame Butterfly's item shop and the caravan seen on screen (2026-09-25); since then, with their rooms, the
settlement entrance's caravan stall, the Golden Settlement square's shop (2026-10-07), the Honey Factory Lobby's
(*Honey Factory: Lobby, Shop 1* to *5*, ids 212-216, 2026-10-10) and, back in the mapped Defiant Root Market, its three
shops (*Defiant Root: Market, Item Shop 1* to *5*, *Poison Shop 1* to *4*, *Bakery Shop 1* to *3*, ids 217-228,
2026-10-10; its one-item sellers wait for a later sweep, the user), both seen the same day through the dev `liveslot`:
the Lobby's shop opening on talking to the bee outside it, the Market's twelve slots each naming an Archipelago item;
Shades's shop not built (it waits for all 50
crystal berries as locations); the other item shops to follow, each with its room.

*Code: `options.py` (`ShuffleMedalShops`, `ShuffleItemShops`, `ShopContents`), `rules.py` (`SHOP_CATEGORIES`,
`fall_back_from_filler_only`), `slot_data.py` (`location_shops`, `location_item_shops`); in the mod `ShopSwap.cs`
and `ItemShops.cs`; tests `test_shops.py`.*

---

## Build step 12: the entrance randomizer, doors shuffled by Archipelago's own (experimental)

Every map-to-map door shuffled, as a yaml option labelled *experimental* until every door's logic is done. The
apworld pairs the doors and sends the result as `door_targets` in `slot_data`; the mod rewrites each door as its
map loads.

**Entrance randomizer (2026-09-25):** every map-to-map door, as an option labelled *experimental* until
every door's logic is done: until then a shuffled seed may be unfinishable (the one exception to "never
impossible", while that option is on; *Warp to start* gets the player out of a dead end). Coupled (a door and its way
back stay a pair) by default, decoupled as a choice. Order: a proof of concept (the mod rewriting a door's
destination, seen on screen), then every door, then the room-by-room logic that removes the experimental label.
**Every room's survey includes every flag it reads** (2026-09-28, after chapter 6 took the boat away,
Next 40), **and looks across rooms** (a follower or a quest from elsewhere: the throne room needs Maki). Not a logic
step of its own: gates and roadblocks are checked with everything else in the room, because they change the logic.
The survey itself, with the rule for removing a follower's need: `room-logic.md`, "Chains" (moved there 2026-09-30).
**The proof of concept, seen (2026-09-25):** one door, then a coupled swap of two connections both ways
(the mod guide, step 13). **Every door, built (2026-09-25):** the yaml option *Entrance Randomizer (experimental)*,
*Off* (default) or *Coupled*; decoupled later (built 2026-09-30, build step 31). How it was built:
1. **A door table** (`data/doors.json`), exported by `dev-scripts/door-graph.py --export` from EntityDump: every
   door paired with its way back (the door the party arrives next to), 254 connections, 508 doors. Doors stay
   fixed when the mod couldn't tell them apart by name, when they have a story variant at the same spot, or when they
   lead into their own map; the table lists the map links those make. (Since build step 38: 255 connections, 510
   doors, and the one-way doors in a list of their own, shuffled too.)
2. **The shuffle** (`doors.py`, replaced by Archipelago's own on 2026-09-30, below), in `generate_early`, with the
   world's own random: the same doors joined in new
   pairs, x with y meaning x leads where y's old partner led (so you arrive next to y) and y where x's old partner
   led. Sent as `door_targets`, which the mod already applies.
3. **Every area stays reachable.** A plain random pairing strands areas: two dead-end rooms joined to each other are
   cut off. Measured: stranded in 20 of 20 seeds on the real table. So the shuffle grows the world outwards from one
   area (maps joined by fixed doors count as one): an open door of the reached part is joined to a door of an area
   not reached yet, taking an area with more doors whenever only one open door is left; the doors left at the end
   are paired at random. Tests `TestDoors*`: every way back leads back, every map is reachable, and a hub with dead
   ends is never stranded over 300 seeds (a plain random pairing strands 266 of them; that test went with `doors.py`).
4. Six seeds generated with it on, three solo and three with APQuest. **Seen (2026-09-25), one generated
   pair both ways:** the town gate led into the desert (`DesertBeforeGH`, the log: rewritten like `DesertSouthern`'s
   left exit), and the exit there back to the start in front of the gate. The logic still assumes the vanilla doors
   (the named allowance).
5. **A dropped connection leaves the doors as the seed has them** (code: `door_targets` arrive with slot_data at login
   and are never cleared by a drop; each map's doors are rewritten from memory as it loads). **Seen
   (2026-09-25):** with the server stopped, the town gate to the desert and back several times (15 rewrites in the log
   while offline); on restarting the server the mod logged back in by itself.
6. **Found while roaming:** a shuffled door reaches areas the seed has no locations in yet, whose items are the
   game's own (the tester picked up the desert's Strong Start medal, flag 415). The Placeholders plan (every spot a
   location, unchecked ones filler-only) closes that.
7. **A trap found while roaming (2026-09-25):** in the bandit hideout's garden (`HideoutGarden`) a guard
   (`burglar`) caught the party, which starts the game's own caught scene (Event108, the log) and puts the party in
   `HideoutCell`, whose one door leads to the central room. **Getting out needs the dig ability**
   (correcting a first guess of Leif's ice: you dig under the bars in the sand). The code agrees: in the story the
   cell is where dig is learnt: Event109, the first capture, takes the beemerang (flag 11 off), sends the party to
   the cell (`LoadMap(100)`) and there sets flag 18, dig; Event108, caught again later, expects you to dig out. So
   without dig it's a dead end (the Warp button is the way out), and the hideout's garden needs dig in the logic
   once the logic follows the doors. A guard catching you is a transfer that isn't a door.
8. **Transfers that aren't doors (decided, 2026-09-25).** The game also moves the party by the dialogue
   script commands `|transfer|` and `|warp|` (`MainManager.cs:13263-13270`) and by story events (about 88 `LoadMap`
   calls in `EventControl`). Decided: **entrances the player chooses** (the bar's hatch, elevators, the boat; the
   submarine's docks, the user, 2026-09-30) are shuffled like doors; **places the game sends you** (caught by guards, a
   fall, a story scene) keep their destination, since the scene expects to end there, and become one-ways in the logic.
   **Seen with decoupled doors (2026-10-04):** the trapdoor still dropped into the fall room and the first boss's scene
   still ended on the Golden Path, and chapter 2's briefing still took the party into the throne room, whose own door
   then led where the seed put it (`DesertEntrance`, as the spoiler says); the doors around them shuffled. **Kept as it
   is (the user, 2026-10-04: "we just leave it as it is, if its not considered within logic anyway"):** weighed against
   returning the party to the scene's room afterwards or shuffling the landing; a one-time trip the logic never counts
   can't make a seed impossible, only move the player somewhere early. The rule for each: `room-logic.md`, question 2
   (moved there 2026-09-30). First
   step: list every such transfer from the data (the script dump and `event-triggers.py`), map, trigger and target.
   **Listed (2026-09-25):** ScriptDump gained a column of the moving commands on each dialogue line (7 lines,
   all `|warp|` or `|loadmap|`), and `dev-scripts/event-transfers.py` lists each event method's `LoadMap` calls and
   targets from the decompiled code (88 calls in 63 events); `event-triggers.py` on those events says what starts
   each. It found the bar's hatch (Event61) and the hideout cell (Events 108/109) at once (`MEASURED.md`,
   "Transfers that aren't doors"). Next: sort them into chosen and forced, reading each event.

9. **Quests that cross rooms** (2026-09-25: a reward mustn't be expected when its middle steps can't be
   reached). It was safe while its steps shared one big region (the old book's residential house and the palace
   library were both *Bugaria Inner City*) or passed on the way (the lost kid's sister waits outside the city, on the
   way to Snakemouth). With doors shuffled neither holds; since 2026-09-30 each spot sits in its own map. The rule
   before the label comes off, every quest step in another room a logic event in that room's area and the reward
   needing the whole chain, is in `room-logic.md`, "Chains" (moved there 2026-09-30). The library visit is done (build
   step 8). Taking the quest is "reach any board" today (build step 9), which a quest's unlock can make too little
   (the user, 2026-09-30: some quests appear only after a scene). Known gap today: the lost kid's reward (location 10)
   doesn't require the sister's step.

**The Warp is always there with the entrance randomizer** (2026-09-26: "so we never get impossible
seeds/softlocks, even if we will check/make logic for things"). Coupled doors can always be retraced, but a one-way
transfer (a drop, a fall, a scripted move) could land the player in a pocket whose way out needs an item not yet found:
the seed stays possible, the player is stuck. The Warp to Start is that escape, shown whatever the Travel setting, as
with a random start (build step 15). The logic never counts it (`room-logic.md`, rule 9).

**Archipelago's own entrance randomizer, with the room-by-room logic** (2026-09-29, the user: "shouldn't we use
officially made things?"): `doors.py` shuffled the door table on its own because, when it was built, no door was an
entrance in the logic (it had ten large regions), so Archipelago's generic entrance randomizer (`entrance_rando.py`,
`randomize_entrances`) had nothing to shuffle. Once the rooms are regions (build step 24), every door is an `Entrance`
of its room's region, and `randomize_entrances` replaces `doors.py`: coupled mode, one-ways paired only with one-ways,
placed with the logic so the logic follows the doors, its `pairings` turned into the same `door_targets` the mod
already reads (How it works §8; Archipelago's `entrance randomization.md`).

**Rooms as regions, done first (2026-09-30; the user: "implement proper archipelago entrance rando now … whatever
Archipelago does").** The graph changed, not the rules. The rules stay what they were, moved to where the graph needs
them; room-by-room rules (build step 24) replace them later.

1. **One region per map:** Menu, then the 240 maps of the door table (`SnakemouthEmpty`, an unused room nothing leads
   into, and `TestRoom`, the debug room, left out; the user: "looked like a empty/test map", "TestRoom sounds obvious")
   and `MetalLake`,
   `TermiteColiseum2`, `BugariaEndThrone`, reached only by transfers. 244 regions, 581 entrances (582 until the Termite
   gate went one way, build step 36; 584 since the one-way doors, build step 38). **Unused and test
   maps are never part of anything** (the user, 2026-09-30): no region, no logic, never the target of a door, a
   transfer or a spawn, never reachable (`room-logic.md`, the model). Test `TestUnusedMaps`, with every door shuffled
   both ways and a random start: no region, entrance, transfer, spot, encounter, start or shuffled door names one; it
   fails with the list emptied.
2. **Every door an entrance of its map's region**, named where it is, `"<map>: <door>"` (the naming the entrance
   randomization doc recommends), connected as the game has it: 508. Of the 39 fixed doors between two maps, 37 are
   plain entrances; the two out of `SnakemouthEmpty` and `TestRoom` go with those maps. (Since build step 38: 510 doors,
   the 17 one-way doors are one-way entrances named the same way, and 21 fixed links are left.)
3. **The transfers that join the door graph's parts** (the doors alone split it into 10), each a `Transfer` in its
   area's module, from the decompiled events and the dumps (read 2026-09-29): the boat (`Boat Ticket`), the Beehive
   elevator, the submarine docks, the ant tunnels, the termite gate, the arena, the Roach Village lifts, the Golden
   Hills elevator, the attack on the city and the ending (one-way), and the way down to the underground bar (one-way, by
   talking to someone in the commercial district, the user). Chapters 2-7's are as cautious as those chapters. Transfers
   inside a part the doors already join wait for the room mapping.
4. **Where a door is itself the gate, its exit has the rule:** the Golden Path door (made only after the first boss)
   and the palace hall's doors to the library, the war room and the mine (chapter 2). The rule goes with the door
   wherever the shuffle sends it.
5. **What the big regions needed became each spot's `reach`:** a spot sits in its map's region, keeps its own `rule`,
   and gains `reach`, the old region's requirement (`PAST_GATE`, `DEN`, `UNDERGROUND`, `GOLDEN_PATH`, `INNER_CITY`,
   `LATER_CHAPTERS`, named in their modules). In-map gates (the Permit gate, the grass, the droplets) stay there until
   the rooms are split into areas.
6. **Proven the same with the doors off:** before the change, which spots 300 random item states reach under 7 option
   sets (the defaults, the story's party, each single member, moves and Jump shuffled, categories off and on); after it,
   all 2100 came out identical.

Tests (`test_areas.py`): every spot in a map and in its source's map, every door an entrance where the game has it,
gates and transfers naming real places, every region reachable with everything (which found the three maps nothing
leads into).

**Archipelago's entrance randomizer in place of `doors.py` (2026-09-30).** `entrances.py` replaces `doors.py`, which
is gone, its grow-outwards pass with it (Archipelago's randomizer grows the world from the start itself):

1. **In `connect_entrances`**, where the doc says it belongs: each door's entrance typed two-way and split with
   `disconnect_entrance_for_randomization`, then `randomize_entrances(world, coupled=True,
   target_group_lookup={0: [0]})`. It places the doors with the logic, so the logic now follows the doors.
2. **Its `pairings` become `door_targets`** (the doc: "used to populate slot data"): a pairing (x, y) means x now
   leads to y's side, so x leads where y's partner leads in the game and you arrive next to y. The mod is unchanged.
   A test proves the two agree: each door rewritten as `door_targets` says arrives in the map its entrance leads to in
   the region graph, so what the generator proved is what the mod does.
3. **The spoiler lists the shuffled doors** in its Entrances section (`spoiler.set_entrance` from
   `write_spoiler_header`, as The Messenger does), each pair once, both ways.
4. **The preflight's import list widened for it** (the user's call, 2026-09-29): `entrance_rando`'s two functions,
   `BaseClasses`' `Entrance` and `EntranceType`.
5. **`PlandoConnections`: first left out** (no developer doc mentions it, only the player's plando guide, where
   "support for connection plando may vary", and nothing requires it; The Messenger has it as an extra), **then built
   the same day** (build step 32), as every optional feature is supported.

The experimental label stays: the rules inside rooms aren't mapped yet (build step 24), so a shuffled seed can still
put the party where the game needs more than the logic knows; the Warp stays the way out.

**Story-only rooms out of the shuffle** (the user, 2026-10-05, mapping the attack on the city): chapter 3's copies of
the city's rooms (`STORY_ONLY_MAPS` in `data_tables.py`: maps 130, 123, 124, 125) hold no location, quest or
discovery, and a shuffled door into them led to a dead end of scene characters. Their door pairs join the fixed links
when the door table loads, so they're never shuffled (Room Swap leaves them too) and never a start; the story's own
transfer still reaches them. The post-game copies (240-242) are decided when they're mapped. Test `TestStoryOnlyMaps`.
**No Warp or map travel there** (the user: warping out mid-attack could never be undone, since the story's landing runs
once and the maps only lead to each other; "disable warp + map, when inside the story maps ? so nothing can get
broken"): `slot_data`'s `no_travel_maps` lists them, and the mod's pause menu offers neither button on those maps
(`Plugin.cs`, `SeedData.NoTravelMaps`), as the game gives no way out mid-scene. Seen 2026-10-05: on map 130 both
buttons gone (the user), with the Warp forced on by Shuffle Jump and the ability items; both back on Seedling Haven.

**Doors that share a name, shuffled too** (the user, 2026-10-05: "why can't we randomize this link ? its 2 different
rooms"). Three maps hold two doors of one entity name (GoldenPathTunnel2, WaspKingdomOutside, TermiteIndustrial's
story copies), which `door-graph.py` used to leave out as fixed links, since the mod finds a door by map and name.
Such a door is now named `name#row`, its line in the map's entity table: `door-graph.py` writes it, and the mod
(`DoorShuffle.cs`) reads that row for the door copied and, for the door rewritten, takes the live door of that name
standing on the row's starting spot (the game makes entities in table order but keeps no row; doors never move). Three
pairs joined the shuffle (Tunnel2's two, the Wasp Kingdom's outside to its main hall), and the outside's other door
became a one-way; TermiteIndustrial's copies stay out, one spot. Seen: a `TestDoors` rewrite of Tunnel2's `#10` led out
by that door only, its same-named neighbour untouched, and East Road 2's left door rewritten as `#9` landed on the
Golden Path tunnel's upper ledge (the user). A coupled seed's spoiler pairs all three with other rooms.

**Room Swap** (2026-09-29), whole rooms moved instead of single doors, is a value of the same option with a step of its
own: build step 30.

**Status:** in progress (experimental): every door, coupled, built, and a generated pair seen both ways, offline too
(2026-09-25); every map a region and every door an entrance, and Archipelago's own entrance randomizer in place
of `doors.py`, built, not yet seen in game (2026-09-30); next, sorting the other transfers and the room-by-room logic.

*Code: `regions.py` (every map a region, every door an entrance), `entrances.py` (the shuffles, `door_targets`, the
spoiler), `logic/` (`DOOR_RULES`, `TRANSFERS`, each spot's `reach`), `data/doors.json`; `DoorShuffle.cs` in the mod;
tests `test_doors.py`, `test_areas.py`.*

---

## Build step 13: party members and moves as items, the design (in progress)

Field abilities, the basic moves and party members as items, each unusable until it arrives. This step is the design
and the rehearsal behind it (a dev setting that starts the game with one member); each part is built in its own step:
members (18, 20), the attacks (21), Jump (22), the learned abilities (23).

**Also wanted (2026-09-25): the basic moves as items**, a yaml option apart from the abilities: Vi's
beemerang, Kabbu's horn, Leif's freeze, and jumping, each unusable until its item arrives. Proposed: *Shuffle Basic
Moves* (the three field moves) and *Shuffle Jump* (its own toggle: it gates the most), both off by default. **The mod
side is small** (code read, 2026-09-25): the three moves are one method, `PlayerControl.DoActionTap`
(`PlayerControl.cs:1008`), switching on the leader's `animid` (0 Vi, 1 Kabbu, 2 Leif), so a prefix can refuse the move;
the game itself takes the beemerang away in the bandit hideout (flag 11, `case 0` checks it), so Vi without it is a
state the game already knows. The jump is its own method, `DoJump`, called from the button it shares with talking
(`PlayerControl.cs:372-392`), so it can be refused without touching talk. **The logic is the cost:** every spot that
needs a move (a ledge, a beemerang switch, grass, water to freeze) becomes a rule, seen room by room, and the start
must have checks that need none of them.
**Later idea, a yaml option (2026-09-25; off by default, confirmed 2026-09-26): party members as items.** Start with one
random member and find the other two, each its own item, on top of the abilities. **Its shape (2026-09-25,
later):** *Starting Party Member: Off / Vi / Kabbu / Leif / Random*; Off is the story's party, otherwise the game starts
with that one member and the other two are items. Prompted by the stand-ins for missing members in scenes, which make
one-member play look possible; the story's own joining scenes (Kabbu at the start, Leif at the lake) must then add
nobody. **The two joining moments become the two locations** (2026-09-25): with the option on, whoever starts, the
other two members are items, and the story has exactly two joining moments: Vi's in the opening scene and Leif's right
after the spider scene (where the mod now has him join). Both become locations whatever the start (Vi's even when you
start as Vi), named after the place, not the member ("Snakemouth Den: Fall Room, After the Spider"), so two items get
exactly two spots and no filler has to go. With the option off they're no locations; the members just join.
**Rehearsal built (2026-09-25), the mod only:** a dev setting `TestStartMember` (0 Vi, 1 Kabbu, 2 Leif) and a prefix
on `MainManager.ChangeParty(ids, fromscratch, destroyoldentity)`, which every party change of the story goes through
(the two-argument form forwards to it; about 20 calls in `EventControl`, some already one member: Kabbu alone after
the slides, Vi alone in one scene). The prefix keeps in `ids` only the starting member and those received (dev:
`addmember`), so the opening's Vi-and-Kabbu becomes the one member; it runs last so the opening skip's own prefix sees
the story's ids. **First test, Leif alone (2026-09-25):** the start and the camera right after four fixes
(the mod guide, step 11, item 1); Artis's talk (lines for Vi and Kabbu, now stand-ins in conversations too) gave the
permit; the gate scene played; the grass tutorial (Event10) played once stand-ins arrive at once, its reward sent.
**Then a real gate:** the corridor after the tutorial (`BugariaOutskirtsSnakemouthCorridor2`) needs the horn to
cross, so Snakemouth Den needs Kabbu (or the horn, once moves are items) in the logic.
**Further (2026-09-25):** the trapdoor scene, landing in the right spot once stand-ins stay where a scene
puts them; the spider fight's lead-in once stand-ins get their physics body when made; and **the spider fight itself
with Leif alone** (seen in a screenshot: Leif alone against the spider, the battle menu working). In the story that
fight is Kabbu alone at first, then Vi joins.
**The first boss and after, Leif alone (2026-09-25):** the boss scene (Event26) and the one after the bridge
(Event63) played with Leif acting the story leader's part, no error in the log; the follower joined crossing the
bridge; the way back to the cave is closed after the boss; what the seed keeps open (the caravan, the ladybug
siblings) still works; the NPCs in the starting house moved away as the story has them. Chapter 2 next.
**Decided (2026-09-25): the members present act out the missing ones' parts** in scenes, where they would
be and what they would do, instead of standing idle beside invisible stand-ins ("looks more fun"). Plan: the real
leader plays the story's leader (the first member of the party the story expects); only other missing members stay
invisible stand-ins; in a party list by member (Vi, Kabbu, Leif) the leader's own slot then gets an invisible
stand-in, so a scene moving "each member" never moves the player twice (why this was set aside at first).
Animations play by number, so the leader shows his own animation with that number: the field action is
`animstate` 100 for everyone (Vi's throw and Kabbu's horn, `PlayerControl.cs:1029`, `:1076`), so Leif acting Kabbu's
horn swing casts his ice (wanted: "Kabbu using the horn, Leif using ice"). States a character lacks show
as `Animator.GotoState: State could not be found`; the mod logs the number and maps it to the closest one. To build
after the current replay of the trapdoor and the spider fight, then replay the same scenes to compare. Opt-in only:
fighting with one or two changes the game a lot. Open questions: the story may need all three after chapter 1, and
adding a member outside the story's own event hasn't worked yet (log.md, 2026-09-24: `ChangeParty` left Leif without a
character). **Solved 2026-09-25:** without `fromscratch`, `ChangeParty`'s copy loop never runs (`for m < 0`,
`MainManager.cs:3805`), so the party list came out empty. `ChangeParty({0, 1, 2}, fromscratch: true)` rebuilds all
members (stats from defaults, then the stat bonuses reapplied), and `SetPlayers(positions)` makes their characters.
Seen on screen: Leif joined the party and fought on a file where he'd never joined (dev command `addleif`). Next
measured: the trapdoor, the spider fight and Leif's own joining scene with him already there.
**Chapter 1 scenes with Leif added early** (2026-09-25): Event2 (the Tattle tutorial, bridge room) played
fine; it moves only the first two (`GetEntity(-4)`, `(-5)`), so Leif stood still in it and was left behind until it
ended, which looked odd on screen. Scenes switch normal following off (`overridefollower`) and move only who they
name. Cosmetic fix for the open start: during a scene, a member the scene never moves walks along behind the one
ahead of him.
**The trapdoor scene broke with three** (2026-09-25): Event5 recreates the party with `SetPlayers(positions)` and a
list of two positions, and `SetPlayers` indexes it for every member: IndexOutOfRange, the scene stopped halfway, the
tester stuck in the fall room (freed with `unstick`). The mod's `PartyFit` now lengthens a short position list before
the game uses it (each extra member a step behind the last listed one), which covers every scene that places the
party this way. **Retest:** `SetPlayers` then took the lengthened list, but the scene then places each member from
**its own** two-long list (`for m < playerdata.Length`, `array[m]`, `EventControl.cs:1476-1484`), which a fix outside
the scene can't reach. Stopped there (two fixes on one scene). `unstick` now also resets the party's bodies (gravity,
physics, forced animation), which the crash had left as the scene set them.
**The scene's own list (2026-09-26):** the one read that loop makes (`array[m]`, the only `Vector3` read after
`SetPlayers` in Event5's step method, `MEASURED.md`) is swapped by a Harmony transpiler for `PartyFit.PlaceAt`: the
same value inside the list, and past its end a step behind the last listed member, as the lengthened `SetPlayers` list
has him. The loop is Event5's alone among the scenes (a search of `EventControl`). **Seen (2026-09-27):**
down the trapdoor with Leif, Vi and Kabbu, the scene ran to its end, no error; the landing spot moved to a door
arrival (the mod guide, step 11).
**Parked design (2026-09-25), for party members as items:** scenes find members by **character**
(`GetEntity(-4)` Vi, `(-5)` Kabbu, `(-6)` Leif search the party by `animid`; `-1` to `-3` are positions), so the
leader's order never matters. Two rules then: (1) a member a scene doesn't know about (Leif early in chapter 1) **steps
out** while it runs and rejoins after (the `addleif` method: `ChangeParty` with `fromscratch`, then `SetPlayers`); (2) a
scene that needs a member who isn't there (only Leif, no Vi) **waits**, held in the game and a rule in the logic for
any check it gives, like the boat. First step when this is picked up: list chapter 1's scenes by the characters they
use, from the code.
Scenes that need a particular member must then become rules. The horn tutorial near Snakemouth (Event10, a
location) is not one: the scene cuts the grass itself, and it played through with Leif alone once stand-ins
arrived at once (2026-09-25; the mod guide, step 11, item 3). The way down to Shades's shop is:
grass on the way there has to be cut with the horn (2026-09-25), so her locations will need Kabbu.

**Rules name the move, not the member (2026-09-26):** "assume horn/boomerang/ice for logic, same as
having Kabbu/Vi/Leif", so the logic already holds for a random start, one member and missing moves before any move is
an item. A spot or exit lists `abilities` (Horn, Beemerang, Ice, Jump); the world turns each into who has it today
(`ABILITY_HOLDERS` in `rules.py` then; since build step 29 `ABILITIES` in `abilities.py`, through `CanUse`: Horn Kabbu,
Beemerang Vi, Ice Leif; Jump the whole party, so nothing), only when members
are items, as for `members` (since build step 29, with the story's party Leif too, who joins late). The two horn spots
(25, 32) moved from `members` to `abilities`, and location 19 (crystal berry #0 outside the den, behind grass from the
Outskirts' side) got the Horn: cautious, since the cave's side needs no horn, which room-level regions will count.
Location 30 (the bridge room's hidden spot, behind grass) got the Horn too (2026-09-26). Both sit in regions that need
all three members today, so the Horn changes nothing yet; it keeps the rule true once regions stop asking for everyone.
The way into Snakemouth Den needs the Horn too (grass in the second corridor and outside the cave), and so does the
trapdoor spot (location 11: the door room's horn puzzle, 2026-09-26); test `test_the_den_needs_the_horn`.
Tests `TestAbilities`; three seeds with a random start and APQuest generated. **Next, after the current tests:** the
three attacks as items, one per member, and Jump as one item for the whole party; then every move's spot
from `MEASURED.md` (where a move is needed) written as `abilities`.

**Status:** in progress: a one-member party (Leif) seen through chapter 1 into chapter 2 (2026-09-25); *Starting Party
Member* built as its own step (build step 18); the three attacks and Jump built as their own steps (21, 22); the seven
learned abilities built as build step 23.

## Build step 14: Enemy Shuffle, which enemies each fight has (in progress)

A yaml option that changes who you fight at each place. It is not enemy checks (build step 10): nothing here is a
location, only the fights move.

**Decided (2026-09-26):**
- *Enemy Shuffle*, `off / enemies_only / bosses_only / both / chaos`, **off by default**. `both` swaps enemies with
  enemies and bosses with bosses; `chaos` puts them in one pool. It is in the yaml, not the panel, so a slot plays
  the same for anyone on it.
- **Each enemy on each map gets its own fight**, fixed by the seed and sent in `slot_data`, never decided at runtime.
- **A boss's reward stays with its place:** Snakemouth Den pays Snakemouth Den's prize, whatever boss was there.
- **The fight first, the look later:** the enemy walking around the map still looks like the original for now. The
  goal is for it to match its fight ("it would feel weird to run into a seedling and then fight an octopus"), which
  is its own later piece of work.
- **The party rule:** only fights that can't be fled are limited to enemies the party guaranteed at that point can
  hit. Map fights can always be fled, so they shuffle freely. If enemy checks come, map fights count too. **Only the
  base attack counts** (2026-09-26): skills cost TP, which can run out, and items are used up, so the logic
  never relies on them; the free base attack is always there. Skills can still win a fight the logic rules out,
  which is allowed (more cautious than the game, never less).

**Measured first** (`MEASURED.md`, "Battles, for enemy shuffle"), so the design rests on the game's code:
1. Every fight goes through one function, `BattleControl.StartBattle`. A map enemy passes itself as `calledfrom`.
2. A boss's prize and story flags come from the event after any win, never from the enemy beaten.
3. The game's own rematch machine fights every listed boss on one neutral stage, which is the evidence that bosses
   can be fought outside their story event. Two story fights change their boss once it has started, so they can't
   be swapped yet.
4. The entity dump got a `battleids` column and the enemy table its own file (the mod guide, step 7). That gave
   327 map enemies on 124 maps, fights of 1 to 4 enemies, no boss on any map, and each enemy's start position (air,
   ground, underground).

**Built for `enemies_only` (2026-09-26):**
1. **The data:** `dev-scripts/enemy-table.py --export` writes `data/enemies.json` from the dump: each map enemy
   (map, entity index) and its fight, 325 of them (TestRoom left out). Generated, never edited by hand.
2. **The option:** `enemy_shuffle` (`options.py`), with only `off` and `enemies_only` for now. The other three come
   when their parts are built. An option value that does nothing would mislead.
3. **The shuffle:** in `generate_early`, `shuffle_encounters` (`enemies.py`) groups the fights by size and shuffles each
   group with the seed's random. A lone enemy stays a lone enemy, and every fight still happens exactly once, somewhere
   else. The result goes out as `slot_data` `enemy_swaps`: `{"map:entity index": [enemy ids]}`.
4. **The mod:** `EnemyShuffle.cs` puts a prefix on `BattleControl.StartBattle`. When a map enemy starts a fight, it
   looks up `map:entity index` (the entity's own `mapid` is its row in the map's table, the same index the dump
   writes) and hands the game a copy of the seed's fight. It must be a copy because the game's own `EnemyCheck`
   rewrites the array it's given. It only acts with Archipelago enabled and a seed known, and logs what it decided
   for every map fight.
5. **Tests** (`test/test_enemies.py`):
   - off gives no swaps;
   - `enemies_only` lists every map enemy;
   - sizes are kept;
   - every fight happens exactly once;
   - most fights move;
   - the same seed gives the same fights.

**The map look, first test (2026-09-26, seen on screen):** a dev console command, `enemylook <enemy id|off>`,
reloads the current map with every ordinary map enemy looking like one enemy. The mod sets each map enemy's
`animid` right after `MapControl.CreateEntities` and before the entity's own `Start`, which builds the model from
it, so the enemy sets itself up as the new character, the way the game would. The look takes that enemy's animation
set (the enemy table's column 0, the same value the rematch machine uses). With Spuder (enemy 2) on
`BugariaOutskirtsEast1`, the three map enemies looked like the spider; the fight was still the seed's. **The movement
stays the original's**: it comes from the map enemy's own row in the map data, so Spuder burrowed like the Underling
it replaced. So the real step copies an enemy's movement from a map where it appears naturally, not only its look.
**A boss from a map enemy, look and fight (2026-09-26, seen on screen):** with `enemylook 46` and
`enemyfight 46 9 0` (a second dev command: every map fight starts with those ids), the Outskirts enemies looked like
the Bee Boss, and touching one started a fight with the Bee Boss, a Seedling and a Cordyceps Ant. So a boss can be
shown and fought from a map enemy, which `chaos` needs. **Decided: the map model is the strongest enemy in
its fight**, so the player knows what they're walking into: a boss if the fight has one, else the highest base HP
(the enemy table dump). Fixed data, so the seed decides it and sends it with each fight.
**Wanted:** a Quality of life row, *Enemy movement: their own / the original's*, default their own; the
original's is there for fun ("looks fun when something does something else than the model is supposed to").

**To fix later (2026-09-26): the map movement.** A swapped look keeps the original enemy's movement
(Spuder burrowed like the Underling). Whether bosses can move around the map on their own at all is unknown; they
never do in vanilla. To measure before the real look step.

**The movement, fixed in the test (2026-09-26, seen on screen):** a map enemy's movement is its map row's
behaviours (fields 2-3), collider (11-12), radii and timers (13-21) (`MapControl.cs:1475-1497`); hovering comes from
the look itself (`CheckSpecialID` raises a flier to its minimum height). `enemylook <id> move` finds a **donor**: the
first map row, any map, whose fight starts with that enemy, and copies its movement fields onto the enemy before its
`Start`. With the Flying Seedling (donor `NearSnakemouth:7`) the Outskirts enemies walked around as they should,
not burrowing like the Underlings they replaced (seen on screen). A boss has no map row, so a boss look has no donor:
its movement is still to decide: **tested per boss later, whatever looks best** (2026-09-26). In the real step,
the seed can pick each donor at generation.

**Scene-only enemies stay out (2026-09-27):** Leif in the web (enemy 12, the spider scene's second
fight) is in no map encounter, and scene fights (`calledfrom` not a map enemy) are never swapped, so neither the fight
nor the enemy is shuffled. Test `TestSceneOnlyEnemies`.

**Next:**
- see the shuffled fights in the game;
- then bosses: each scripted fight read one by one, keyed by its event and its original ids. **A boss fight's rule
  follows the fight placed there** (the user, 2026-10-06): the room logic writes the vanilla boss's need (the first
  boss, the Spider: Vi, who hits it in the air); with bosses shuffled, the spot may hold another boss or, in `chaos`,
  ordinary enemies, and its rule becomes what that fight needs. Every mode (enemies, bosses, both, chaos) stays
  mixable;
- then `both` and `chaos`;
- then the map look.

**Status:** in progress: `enemies_only` built (2026-09-26), the apworld tests pass, a seed generated with a second
game (APQuest) carrying all 325 fights in `slot_data`, and **seen on screen** (2026-09-26): on
`BugariaOutskirtsEast1` an Underling + Flying Seedling map enemy started a Flying Seedling + Seedling fight, as the
seed and the log (`[enemies] BugariaOutskirtsEast1:4: 30 10 -> 10 9`) said; bosses,
`both`, `chaos` and the map look to come.

*Code: `options.py` (`EnemyShuffle`), `enemies.py` (`shuffle_encounters`), `data/enemies.json` (from
`dev-scripts/enemy-table.py`), `world.py` (`generate_early`); in the mod `EnemyShuffle.cs`; tests `test_enemies.py`.*

## Build step 15: Starting Location, a new file starts in a random room (experimental)

A yaml option for where a new file begins. **Experimental** (ruled 2026-09-26), like the entrance
randomizer: "random start should be fully random… random spawn is experimental just like entrance rando". The logic
still starts outside Bugaria, so a seed started elsewhere may not be finishable until the room-by-room logic exists.

**Decided (2026-09-26):**
- *Starting Location (experimental)*: `off / anywhere` for now (`towns`, and a named spot if players ask, later), off
  by default. **Not `random`:** Archipelago reserves that word for every Choice (any option can be set to random), so
  the generator refuses it as a value.
- **Fully random:** beside any save point in the game, even mid-dungeon (replaced the same day by any room, below).
- **Warp to Start goes to the seed's start**; the pause menu's map keeps fast travel to the areas you've visited.

**Built (2026-09-26):**
1. **The data:** `dev-scripts/save-points.py --export` writes `data/starts.json` from the entity dump: every save
   point (map, entity index), 72 of them (TestRoom's and duplicate copies left out). The start uses a save point's
   spot, not the save point itself, so one that only exists in some story states is still a fine spot.
2. **The option and the pick:** `starting_location`; `generate_early` picks one save point with the seed's random
   and sends it as `slot_data` `start`: `{"map", "entity"}`, or `{}` for the game's own start.
3. **The mod:** the opening's one-time transfer (the Quality of life skip's end, where a dev `TestStart` already
   warped: it only happens once per file, so no save field is spent on "started") goes beside the seed's save point,
   as Warp to Start lands (`WarpButton.SavePointSpot`). Warp to Start goes there too. The transfer hangs on the intro
   skip's end, so **a seed start always skips the intro**, whatever *Skip cutscenes* says (2026-09-26);
   the setting still governs every other scene.
4. **Tests** (`test/test_start.py`): off gives `{}`; `anywhere` gives a room from `ROOM_STARTS`, entered through a
   door of its `from` map; every connection gives both rooms as starts; the start is fixed.

**First tries in game (2026-09-26), three failures and how each was found:**
- **Frozen, with a `NullReferenceException` in `TransferMap`.** The transfer's first step is the game's fade to black,
  and it waits on that fade's sprite; the intro skip's own fade-in started a frame later, and `PlayTransition` destroys
  the running fade's sprite. Read from the stack trace and the game's `PlayTransition`. Now, with a seed start, the
  skip sets the party but leaves the fade-in to the transfer, which waits a frame after the party is set.
- **Left at the game's start, no transfer.** The mod's "opening due" state outlived the file it belonged to (a file
  quit before its opening ran), so the next file skipped the step that books the start. Found in the log of the file
  before. The state now resets with the game's own `SetVariables` (the title screen), and the intro skip's end books
  the start as well. The start is also asked at the transfer, not before the login.
- **A glimpse of the opening map before the transfer.** The skip removed the slides' black backdrop at once; with a
  seed start it now stays until the new map has loaded, as the dev `TestStart` already did.

**Seen (2026-09-26):** a new file ended at the seed's start, Rubber Prison's cell block, the pause map
marking only Rubber Prison visited; Warp to Start went there from the Outskirts.

**A worked example: starting on Metal Island** (2026-09-26). Leaving needs nothing (the island
sailor is unchanged); coming back needs the Boat Ticket, or the Warp (the seed's start) or map travel. Nothing can be
missed, and nothing there has to be filler: the logic already gates Metal Island on the ticket, so it never expects the
island's checks before the ticket, and the ticket can't be placed behind its own gate. Starting there only gives an
early look. From it came **the rule for any start**: it's safe while every way out of it is free and every way back
in is something the logic already gates (`room-logic.md`, spawn question S6, moved there 2026-09-30).

**The rule that keeps every random start valid (2026-09-26: "really important for the logic"; revised 2026-09-29):**
with a random start, **Warp to Start is always available**, whatever the Travel setting, and **the logic never counts
it**; leaving a start counts in the logic only together with what it takes to get back in (`room-logic.md`, rules 4
and 9, and spawn question S6). Until 2026-09-29 the Warp closed that case by counting in the logic. The mod shows the
Warp with a seed start even when Travel is Off or Map.

**No music between the menu and the start (2026-09-26: "as if I'm going from the start menu directly to a
random spawn"):** the game starts the opening map's music as a new file loads. With a seed start, the mod turns any new
music into silence while the file is still on the opening map (a prefix on `ChangeMusic`'s full overload, which every
music change reaches), and its own opening music is a fade-out instead. Seen on screen: "looks/feels instant now".

**Any room, not just save points (2026-09-26: "an actual random area ... somewhere in a dungeon"):**
- **Values:** off / towns / random as proposed, but Archipelago reserves `random`, so `anywhere` is the fully random
  one; `towns` is still to come (the save-point table, `data/starts.json`, stays for it; replaced 2026-09-30 by the
  designed *Save Points*, below).
- **The pool** (`ROOM_STARTS` in `data_tables.py`): every room entered through a door, both ways of each connection in
  `doors.json`. `slot_data` `start` is `{"map", "from"}`: the room, and the map whose door leads in.
- **The mod** reads the door in the `from` map that leads into the room (`QualityOfLife.DoorInto`, the dev test start's
  reader), and arrives as the game's own door transfer does: appear, then walk in. Warp to Start lands where that walk
  ends. A save-point start (`{"map", "entity"}`) still works.
- **Stuck starts are accepted while experimental** (now `room-logic.md`, the spawns' verdict): item and entrance
  logic everywhere comes later.
- **Chapter 1 test**: seeds regenerated until the start was a Snakemouth Den room (seed 17,
  `SnakemouthFallRoom` from `SnakemouthDoorRoom`). **Seen (2026-09-26):** the new file arrived right where
  the trapdoor scene drops you; jumping up out of the room is one-way, and the Warp brought the party back down.

**"Any room" isn't quite any room, read in the code (2026-09-30; not seen in game).** Four ways today's start falls
short:

- **12 real rooms can never be picked.** `ROOM_STARTS` is built from `doors.json`'s paired connections only (494
  pairs over 231 maps). Nine rooms have only the table's fixed doors: `UndergroundBar`, `BarrenLandsPinkSpider`,
  `BeehiveScannerRoom`, `GiantLairBeforeBoss2`, `GoldenPathTunnel2`, `GoldenSettlement1Night`,
  `GoldenSettlement2Night`, `GoldenSettlement3`, `GoldenSettlement3Night`. Three are reached only by scenes:
  `MetalLake`, `TermiteColiseum2`, `BugariaEndThrone`.
- **The odds follow the doors:** a room has one entry per door pair, 1 to 8.
- **The spot isn't the door's own arrival.** `DoorInto` takes the first door in the `from` map that leads in, so for
  the 14 pairs joined by two doors the logic can't know which. The mod calls `TransferMap` without the door, so the
  party skips the door's arrival jump and camera (`MEASURED.md`, "Save crystals, saving, Game Over and room
  transfers"). The map's auto-start scenes aren't held either.
- **Warp to Start lands on the same spot**, so a spawn boxed in by obstacles would have no way out at all.

The checks every spawn now gets are in `room-logic.md`, "Where the party can appear".

**Decided (2026-09-30), the truly random start, to build** (the user, one question at a time: "i want random spawn to
actually be random not just 'semi random'"):

- **Three values, each described in the option's text:** *Off* (the game's start), *Save Points* (beside any save
  point, each equally likely) and *Any Room* (any room equally likely, then one of its spawns). `anywhere` stays
  readable as *Any Room*, so older yamls and seeds still work. *Save Points* replaces the planned `towns`.
- **Both kinds of spawn:** a door's arrival, as the game's own door does it, and wherever the game itself puts you in
  that map (a scene's or a dialogue warp's spot); the underground bar's is where the hatch (Event61) puts you.
- **Only actual rooms:** "random spawn should only be for actual rooms, not test rooms/minigames/submarine lake etc".
  Read in the scenes that load them: `TermiteColiseum2` is the tournament (Event163, battles, then `TermiteColiseum1`),
  `BugariaEndThrone` the ending (Event205, then Event204), `MetalLake` the submarine's lake (Event153). None is a start;
  each stays in the logic as the transfer it is.
- **The same rooms as the entrance randomizer**, with unused and test maps never part of anything (build step 12).
- **Every room gets a verdict,** confirmed or denied at its room check ("we will have to double check every single
  room"). The night maps wait on a way between day and night in a seed: in the story it's a one-time window (Event52
  sets flag 85 and loads `GoldenSettlement1Night`, Event58 sets 86 and loads `GoldenSettlement1`).

**A start at a door the game hasn't made yet (found 2026-10-04).** A seed started at the desert border
(`DesertFGBorder`) as if through Far Grasslands' door: the party fell below the map, the respawn-loop guard warped it to
the start, which was the same spot, and so on (the user: "this is a softlock/oneway. while the door is closed", with a
screenshot of the shut gate and its guard). `gate-table.py` named it: the border's door back (`loadzonefg`) requires flag
348, set in chapter 5, so on a new file it doesn't exist and its landing lies behind the shut gate over nothing. Every
start lands at the room's own door back, so the rule is: **never a landing door the game makes only from a story flag,
unless the seed keeps it present.** `door-graph.py --export` now writes `gated` into `data/doors.json` (35 doors with
their flags; the rest of the table re-exported byte-identical), and `ROOM_STARTS` leaves out such starts (471 of them
left). The desert gate itself was opened in the same change (build step 9), so that start came back, safely. Test
`test_never_lands_at_a_door_the_game_hides`: it failed on many starts with the rule off, the palace hall from the library
among them (flag 67).

**Status:** in progress (experimental): `anywhere` (any room) works, seen on screen (2026-09-26): a new file starts in
the seed's room; the Warp back to it through its door, with Travel Off and on Map, seen (2026-10-04); no start at a door
the game hides on a new file (2026-10-04), not yet seen in a new seed; the truly random
start designed (2026-09-30), to build; the logic from the start to come; the intro is always skipped with a seed start.

*Code: `options.py` (`StartingLocation`), `data_tables.py` (`ROOM_STARTS`, `STARTS`), `data/starts.json` (from
`dev-scripts/save-points.py`), `world.py` (`generate_early`); in the mod `QualityOfLife.Opening.cs` (`DoorInto`,
`SeedStartDoor`) and `WarpButton.cs` (`SavePointSpot`); tests `test_start.py`.*

## Build step 16: the Boat Ticket, Metal Island behind a custom key item

The first of the mod's own items (custom gates, "How this mod does it"), suggested on Discord. **Decided
(2026-09-26):** a progression key item, always in the pool; the pier sailor sails to Metal Island only with it, the
trip free and the ticket kept; the logic gates Metal Island on it, so Metal Island isn't open from the start; Free boat
(the Quality of life row) goes.

**Built (2026-09-26):**
1. **The item** (`CustomItems.cs`): id 200 in the game's item and sprite tables (the game's items end at 186, both
   hold 256), the Platinum Card's fields and sprite (item 176, chosen after mockups of combined icons), its
   own name "Boat Ticket" and the description "A boat ticket. Maybe we should visit the pier." (field 2, the one
   the menus show; `MEASURED.md`, "The item table's fields"). Only with Archipelago on. Seen on screen.
2. **In the pool** (`items.json`): kind 1 (key item), game id 200, progression, with `always`: it enters once in every
   seed. When every location already holds its vanilla item (the default seed had 59 for 59 then), one ordinary item or
   berries with a copy left makes room, picked with the seed's random; never a medal, never an item's last copy.
3. **The logic** (`logic/metal_island.py`): a Metal Island region, reached from the Outskirts (the pier) with the Boat
   Ticket. No locations there yet. Since 2026-09-30 every map is a region, and the boat a transfer from `BugariaPier` to
   `MetalIsland1` needing one Progressive Boat (or the Boat Ticket with *Progressive Boat* off).
4. **The sailor** (`BoatTicket.cs`): a postfix on `MainManager.GetDialogueText` on `BugariaPier`, since every line of
   his, the first included, comes through it. His lines as approved, line by line (`log.md`,
   2026-09-26): the offer "Would you fancy traveling to Metal Island? Show me your ticket.", the
   choice "Let's go!" / "Not yet!" with the ticket or "I lost my ticket!" without, the ticket checked where the fare
   was (lines 16 and 19), "...Ticket's in order. Hop on!" or "What?! No ticket, no trip! Get out of here!". The card
   Masters' discount (line 18) makes the same offer. English only.
5. **Free boat removed** (`QualityOfLife.cs`, `ApMenu.cs`): its fare rewrite and its row.
6. **Tests** (`test/test_boat_ticket.py` then; since 2026-09-30 `test_progressive_boat.py`, build step 36): once in the
   pool, progression; Metal Island unreachable without it, reachable with it. `TestPool` now allows the one filler copy
   the ticket takes. *Shop Contents: Filler Only* in a solo seed with discoveries on is now one filler short and falls
   back to No Progression, as it already did without discoveries; with other games' filler in the room it holds.
7. **When no duplicate is left** (the fuzzer, 2026-09-28: 1163 of 10000 seeds failed with *Shuffle Field Moves* on and
   item shops and discoveries off): the last copy of an ordinary item or berries gives way too, only then. No
   progression, useful item or medal ever does, so the fill and the logic are unchanged; the seed has a few fewer
   plain items. `TestSmallPool` (the smallest option set), then 0 of 10000 fuzzed seeds failed, and 0 of 2000 with
   APQuest.

**Seen (2026-09-26):** with the ticket, the sailor offered "Would you fancy traveling to / Metal Island?
Show me your ticket." (a `|line|` before the name, which wrapped mid-name at first), "Let's go!" / "Not yet!", and
"...Ticket's in order. Hop on! Our destination: Metal Island!", then the boat.

Without it (the ticket taken with the dev console's `take key 200`), the choice read "I lost my ticket!" and "Let's go!"
got the refusal; seen on screen.

**Since 2026-09-30, with *Progressive Boat* on (the default),** the ticket arrives as the Progressive Boat's first copy
and the submarine as its second; off, the ticket is its own item, as here (build step 36).

**Status:** works both ways, seen on screen (2026-09-26). Since 2026-09-30 the ticket is the Progressive Boat's first
copy by default (build step 36).

*Code: `data/items.json`, `logic/metal_island.py`; in the mod `CustomItems.cs` and `BoatTicket.cs`; tests
`test_progressive_boat.py`.*

## Build step 17: releases, the three downloads and how they're built

Three separate downloads on a GitHub release (2026-09-25/26), made the way MeshGhost makes its TEVI release.

| Download | What it is |
|---|---|
| `bugfables-archipelago.zip` | The mod. Extract it into the Bug Fables folder, next to `Bug Fables.exe`; it holds `BepInEx/plugins/BugFablesAP/` and `README.txt`. |
| `bug_fables.apworld` | The world, for Archipelago's `custom_worlds` folder. |
| `bug_fables.yaml` | The player options template. |

**Names.** No version in any file name: the release and its tag carry it, and `releases/latest/download/<name>`
links stay the same. The apworld's name is fixed: Archipelago 0.6.7 imports the module named after the file
(`worlds/__init__.py`, `world_name = Path(apworld.path).stem`), and 0.6.8 the same way (`add_apworld_spec`,
`world_name = Path(container.path).stem`, read 2026-10-08), so it must match the folder inside, `bug_fables`.
The template generator names the yaml `Bug Fables.yaml`; GitHub turns spaces in asset names into dots, so it ships
as `bug_fables.yaml`.

**The layout.** A subfolder of `BepInEx/plugins` works: BepInEx 5.4.23.5's chainloader scans `plugins` with
`SearchOption.AllDirectories` (`BepInEx/Bootstrap/TypeLoader.cs`), and its runtime resolver
(`BepInEx.Preloader/Entrypoint.cs`, `LocalResolve`) looks for a missing assembly in every subfolder of `plugins`
(`Utility.TryResolveDllAssembly`). The folder holds the four DLLs, `LICENSE.txt` (ours), `THIRD-PARTY-NOTICES.txt`
(the three libraries' MIT notices, which the NuGet package doesn't carry; `licensing.md`). The zip's top level
holds only `BepInEx/` and `README.txt`, where someone opening the zip sees it (2026-09-26: it was three
folders down at first). A plain name, though it lands next to `Bug Fables.exe` (overwriting it is
fine). `build-release.ps1` refuses any other file at the top level, in `-Check` too.
BepInEx is not bundled; the player installs it first.

**How it was built:**
1. **The mod is built locally and committed.** CI can't build it: it compiles against the game's own
   `Assembly-CSharp.dll`, which never enters the repo. `dev-scripts/build-release.ps1` builds Release and stages
   `release/mod/` (with no debug info: the pdb isn't shipped, and its path would put the build machine's folders into
   the DLL; checked with `strings`), and writes `release/built-from.txt`: each source file's git blob hash (line endings
   normalised, so a Windows and a Linux checkout agree) and each shipped DLL's SHA-256, written with LF line endings
   like every file (since 2026-10-04, `.gitattributes`' `eol=lf`). `.gitignore` lets exactly those four DLLs in.
   Since 2026-09-29 it builds HEAD in two clean clones that must come out byte for byte the same (the mod guide,
   step 32).
2. **A stale gate.** `build-release.ps1 -Check` recomputes both lists and fails if they differ (since 2026-09-29 it
   runs preflight's release sections instead: Release staging and the three DLL sections, build step 28). It runs when
   releasing: a job in the release workflow and `release.ps1`'s preflight. It ran on every push at first, which kept
   `main` red between releases, where a DLL older than its sources is expected; moved on 2026-09-28. Tried both ways
   (2026-09-26): a probe line in a `.cs` file failed it, naming the file; removing it passed.
   **The dev tools don't ship** (2026-09-28, reversing the 2026-09-26 choice to ship them switched off). The Release
   build leaves `Dev/` out (`BugFablesAP.csproj`: `<Compile Remove="Dev/**">` outside Debug, and `DEV` defined only
   in Debug), so the download has no console, cheats, probes or dumps, and no `[Debug]` settings. The same run
   refuses a release otherwise:
   - a `Config.Bind("Debug", ...)` outside `Dev/`;
   - a `[Debug]` setting on by default, for dev installs;
   - one of `Dev/`'s own types, or a "Dev only" text, found in the built DLL. `-Check` repeats that last check on the
     committed DLL. Since 2026-09-29 preflight's DLL sections do it, refusing any of `Dev/`'s types and any name or
     string the source lacks, on the fresh build and in `-Check`.

   Tried both ways (2026-09-28). A probe `[Debug]` bind in a feature file failed it, naming the key and file. The first
   version of the check counted every file as in `Dev/`, because PowerShell's `-match` ignores case and the checkout
   sat under a folder named `dev`; it is case-sensitive now. The release DLL went from 389,632 to 308,224 bytes, and it
   loaded in game (`copy-dev -Layout Release`) with no dev line in the log and every feature installed.
3. **CI** (`.github/workflows/ci.yml`, every push and pull request, and called by the release): the apworld on a
   Python matrix (3.11, 3.12, 3.13, what Archipelago's own CI tests, the same at 0.6.7 and 0.6.8). Each leg checks out
   Archipelago at `AP_TAG` (`0.6.8` since 2026-10-08, `0.6.7` before), installs it the way Archipelago's own
   `unittests.yml` does, WebHost's packages included (then sets `SKIP_REQUIREMENTS_UPDATE=1`: on the
   first run, 2026-09-26, two worlds' pins clashed over `typing-extensions` on Python 3.12 and 3.13, and `Launcher.py`
   stopped at a press-Enter prompt with no one to press it), runs our tests together with Archipelago's general tests
   scoped to this world (`AP_TEST_WORLDS=bug_fables pytest`, `tests.md` at 0.6.8; until then only our own folder ran,
   so the general tests never saw this world), and generates three presets
   (default, every experimental option on, every location toggle off) with APQuest as a second game. The whole suite
   takes about 3 seconds, so the matrix splits by Python version, not by test file: every job pays the install.
   A `build` job of its own (on 3.13) builds the apworld (`Launcher.py "Build APWorlds" -- "Bug Fables"`, with our
   `LICENSE` copied in) and the template (`Launcher.py "Generate Template Options" -- --skip_open_folder`), then
   generates once more the way a player would: the built `.apworld` in `custom_worlds`, the template as the yaml, no
   loose world. Until 2026-09-29 those were extra steps of the 3.13 leg, and a changed matrix would have stopped them
   without a word, until the release found no files. A `fuzz` job runs `test-apworld.ps1` as we do locally: the
   tests, the Logic Test check and 10000 fuzzed seeds, with the fuzzer and the Logic Test at pinned commits
   (`development.md`, "Fuzzing the apworld"). No one waits on CI after a push; a failure arrives by email.
4. **The release** (`.github/workflows/release.yml`, run by hand): a guard first (the version is `vX.Y.Z` and
   matches `Plugin.cs` and `world_version`; the tag is free; nothing unpublishable in the highlights or in any commit
   subject the generated notes will publish, checked by the preflight's text rules since 2026-09-29, build step 28),
   then CI, the preflight workflow and the stale gate, then the publish job zips `release/mod`'s `BepInEx` and
   `README.txt` and attaches
   the three files. The body is the highlights (changes and new features, or nothing), a section on what the code
   may do that changed since the last release, and GitHub's generated notes.
   `softprops/action-gh-release` is pinned to a commit, since it runs with write access. Since 2026-09-29 (build
   step 28) the files are checked against the commit before they are uploaded and again once published, and the
   apworld and yaml carry GitHub's provenance attestation.
5. **One command cuts it:** `dev-scripts/release.ps1 -Version v0.1.0 [-HighlightsFile notes.md]` (add `-Prerelease`
   only for a test build: a pre-release never shows as Latest, which hides it; 2026-09-26; since releases became
   immutable on 2026-09-29, a pre-release stays a draft with its files until published from the releases page). It
   refuses unless the versions match, `main` is clean and not behind, and the tag is free. Then its preflight runs
   the stale gate; a stale DLL is rebuilt and committed on the spot, and the gate runs again. Then it pushes, waits
   for CI and the preflight workflow to go green, dispatches the release and waits for it to publish. Running it
   is the go-ahead to push.
   **The highlights' format (2026-09-27, the standard from v0.2.0 on):** short one-line bullets under
   `### Features`, `### Logic` and `### Bug Fixes` (a heading left out when empty), as other Archipelago mods write
   theirs. What a player notices only: no internal fixes, and no "update both, regenerate" line (players are assumed
   to be on the latest version). A fix to the base game's own bug says so ("a base-game stutter"). GitHub adds the
   Full Changelog link below.
   **New features in the player docs (the user, 2026-10-01):** labelled with the release they come in, "New in
   0.3.0", never "new in this version", which stops being true at the next release; a label with its version stays
   accurate however old it gets.

6. **Rehearsed before the first run (2026-09-26):** every CI step on a fresh clone of Archipelago `0.6.7`: the
   tests, the three two-game presets, Build APWorlds (its manifest gained `version` and `compatible_version` on its
   own), the template, and a seed from the built `.apworld` in `custom_worlds` with the template as the yaml (loaded
   as v0.1.0, no manifest warning). The zip, built the same way, extracted next to `Bug Fables.exe` lands only in
   `BepInEx/plugins/BugFablesAP/`.
7. **Trying the download in game:** `copy-dev.ps1 -Layout Release` swaps a dev install for exactly what the zip
   holds, and `-Layout Dev` swaps back (`development.md`, step 4 of the build-and-copy list).

**Versions.** The mod's `Plugin.Version` and the apworld's `world_version` both equal the tag without its `v`.
v0.1.0 is the first (the mod was 0.0.1 and the world 0.2.0 before). v0.2.0 is the second (2026-09-27), cut from `main`
as it stood before the tester played chapters 5 to 7, with highlights written from the commits since v0.1.0.

**Status:** works: v0.1.0 published by `release.ps1` (2026-09-26), every job green, then remade the same day from
a later `main` for the zip's top-level README and switched from pre-release to a full release (a pre-release
is hidden from Latest). The downloads fetched back and checked; the DLL is the build seen to load and connect on screen.
v0.2.0 published by `release.ps1` (2026-09-27), every job green, after a stale check of every doc against the code;
the downloads fetched back: the zip's DLL matches `built-from.txt` and reports 0.2.0, the apworld's manifest says 0.2.0.
The stale gate moved from push CI into the release workflow (2026-09-28), with `release/` rebuilt, so `main` is green
between releases; `-Check` passes locally, and the moved job first runs at the next release.
The dev tools are out of the release build (2026-09-28): the gate checks it, and the Release DLL ran in game with none.
v0.3.0 published (2026-10-04), the full suite, the Logic Test check and both fuzzers clean first. Not yet seen in game:
a release zip on a clean install (each DLL so far was the source the user had been playing), for the next release.

*Code: `dev-scripts/build-release.ps1`, `release.ps1` and `verify-release.py`; `.github/workflows/ci.yml` and
`release.yml`.*

## Build step 18: Starting Party Member, the other members shuffled as items

The first part of build step 13 made real: a yaml option that starts a new file with one party member and makes the
other two items. Its design (2026-09-25) and the rehearsal behind it are in build step 13; this is the
option itself. **Decided (2026-09-26): cautious logic.** The gates measured so far become rules, and
everything past the Outskirts gate needs all three members until the rooms there are measured.

**Built (2026-09-26):**
1. **The option** (`options.py`): *Starting Party Member: Off / Vi / Kabbu / Leif / Random Member*, Off by default.
   Not "Random": Archipelago reserves that word, and it would pick Off too. The seed picks a random member in
   `generate_early`, with its own random.
2. **The items** (`items.json`): Vi, Kabbu and Leif, a new kind 5 (game id 0, 1, 2; Archipelago id base + 4000 + the
   member), progression. With the option on, the starting member is start inventory (`push_precollected`), which the
   server sends the client like any item, and the other two go in the pool. With it off, none of them exists.
3. **Two locations, whoever starts** (`logic/`, category `party_member`, only with the option on): *Outskirts:
   Outside the City, Opening* (flag 15, where Vi joins in the opening) and *Snakemouth Den: Fall Room, After the Spider*
   (flag 27, where the mod has Leif join). Two items, two spots, no filler removed. The story's "Leif Joins" event
   (category `story_party`) exists only with the option off, so Leif isn't handed out for free.
4. **The rules** (`logic/`, `Member` rules, which count only with the option on): the way past the Outskirts gate
   needs Vi, Kabbu and Leif (on top of the permit), which covers the measured gates past it (the horn corridor needs
   Kabbu, the first boss needs Vi); *East Road, Boulder* and *Residential District, Rooftop* need Kabbu (his horn). The
   Leif rules that already existed (droplets, the fountain rooftop) now need the item instead of the story's event.
5. **slot_data** `starting_member`: -1 for Off, else 0 Vi, 1 Kabbu, 2 Leif.
6. **The mod** (`ApConnection.cs`, `PartyMembers.cs`, `ItemReceiver.cs`, `ItemSwap.cs`): the seed's
   `starting_member` replaces the dev `TestStartMember` for the member guard (the mod guide, step 11), so the opening's
   Vi and Kabbu become the one member. A received member (kind 5) joins on the spot, the way the dev `addmember` does;
   the members allowed are the start plus those among the items this save has counted (`flagvar[60]`), recomputed
   every frame and cleared on the title screen, so one file's members never carry into another. A hold-up for a
   member shows the pause menu's party icon (`guisprites[94 + member]`) in the member's colour (`charcolor`). An item
   kind the mod doesn't know is now skipped with a log line; before, it went into the bag as an ordinary item.
   **The opening is always skipped with a starting member (2026-09-26),** whatever *Skip cutscenes* says: the
   opening's tutorial battle was written for two and was never played with one. Then made the rule for every
   Archipelago file (the mod guide, step 10, item 5).
   **Fixed after the first play (2026-09-26):** the rooftop pickup showed Vi's icon far too large and the
   Crunchy Leaf's description (a member's number read as item 0). The icon is now scaled to an item sprite's size, a
   member gets no description box (the game has no item row for him), and no article: "You got Vi!", the line's
   space after the article dropped for that one line (`MEASURED.md`, the found-item line). Seen in a hold-up.
7. **Tests** (`test/test_party.py`): Off adds nothing; each start is start inventory with the other two in the pool;
   both locations exist with flags 15 and 27; the gate needs all three; the horn spots need Kabbu; a random start is one
   of the three. `TestClassifications` counts `members` too. Generated with APQuest for all four choices: every seed
   finished generating (Leif start: Kabbu on the Pier, Vi on the Fountain Rooftop, which needs Leif).

**Known before the first play:** with all three needed before Snakemouth Den, the trapdoor scene (Event5) always runs
with three members, and it broke with three before (build step 13: its own two-long position list). A three-member
spider scene is also still open (the mod guide, step 11). Both come up in the first play past the gate.
The trapdoor's own list is now patched (build step 13, 2026-09-26). Test seed for the first play: a Leif start, Vi and
Kabbu on the two opening locations, the permit on Madeleine's table (plando).
**Vi joined with no box (2026-09-26):** your own item gets no arrival hold-up, since the location's own scene
shows it; Kabbu's did (the opening's gift line, swapped). But the two joining moments are only a story flag and show
nothing, so Vi arrived silently. `slot_data` now lists `silent_locations` (every location whose source is only an event
and a flag: exactly these two), and the mod's receiver gives your own item from one of them the usual hold-up, as it
does another player's. Tested (`test_joining_moments_are_silent`). **Seen (2026-09-26, a new seed):** both boxes,
Kabbu's then Vi's. Vi already stood in the party during Kabbu's box (a member joins on arrival, the box waits its turn);
left as it is (the order doesn't matter, the player can't act in between).

**Seen (2026-09-26), a Kabbu start:** the opening left Kabbu alone and sent its check; Artis's gift was Leif,
who joined on the spot; the Fountain Rooftop held Vi, who joined too (party 1, 2, 0). Then each member's hold-up: "You
got Vi / Kabbu / Leif from TestPlayer!", item-sized in the starburst, each in his colour, no description. A member lying
on the ground at the new size, seen too (dev `spawn member`, a screenshot of Vi on the grass).

**Seen (2026-09-26), a Vi start with a second player:** Vi alone after the opening; Kabbu and Leif both
arrived from the other player's chests and joined (party 0, 1, 2).

**A story-party seed started Leif alone (2026-09-27):** the mod took the dev `TestStartMember` (left at 2)
whenever the seed's `starting_member` was -1, so a seed with the option off still used it. Now a seed that sends the key
decides, -1 included, and the dev setting only stands in with no seed (`PartyMembers.SeedSaysMember`). **Seen (the
same day):** a new file on that seed started in the town plaza with Kabbu and Vi. Warped to the fall room
(dev), the spider scene at speed, the first fight ended at once, and Leif joined after it, as the story has him.

**Status:** works, seen on screen: before the Outskirts gate with a Kabbu start and a Vi start, members arriving from
this world and from another player (2026-09-26); past the gate with a Leif start, the trapdoor and spider scenes with
all three, Leif back in the party after (2026-09-27).

*Code: `options.py` (`StartingPartyMember`), `world.py` (`generate_early`), `items.py`, `slot_data.py`
(`starting_member`); in the mod `PartyMembers.cs`; tests `test_party.py`.*

## Build step 19: Item colors, Archipelago's colours for players and item classes

Archipelago's own clients colour a message the same way everywhere, so players read "whose, and how important" at a
glance. This mod follows that standard in the game's item boxes (2026-09-26: it follows the expected
Archipelago colours, so it belongs here as well as in the mod guide).

**The standard** (your Archipelago checkout, `NetUtils.py`, `JSONtoTextParser`, and `data/client.kv`, read 2026-09-26):
- a player's name: magenta `EE00EE` when it's you, yellow `FAFAD2` for anyone else (`_handle_player_id`);
- an item by its flags (`_handle_item_name`): progression (`0b001`) plum `AF99EF`, useful (`0b010`) slate blue
  `6D8BE8`, trap (`0b100`) salmon `FA8072`, anything else (filler) cyan `00EEEE`;
- a location: green `00FF7F`.
The flags come with every item the client sees (`NetworkItem.flags`; MultiClient.Net's `ItemInfo.Flags`, and a
scouted item's), so the colour needs no table of our own.

**Built (2026-09-26):**
1. **The same meaning, darkened.** Those colours are picked for a dark window; the game's text box is near white, and
   pale yellow, plum, slate blue and cyan faded on it (three rounds on screen with the tester). Used: another player
   dark yellow `B8860B`, progression dark plum `8A63D2`, useful dark slate blue `4A6BD8`, filler dark cyan `008B8B`,
   trap `E9573F` (salmon was too pale; red would read as the game's own red item names). Your own name never shows:
   only another player's item gets a "from" or "'s".
2. **Where:** "You got \<item> from \<player>!" for an item another player found for you, and "You found \<player>'s
   \<item>!" for another player's item found here (a gift or a pickup). Your own finds keep the game's red.
3. **A choice:** the Quality of life row *Item colors: Rarity / Archipelago / Off* (the mod guide, steps 20 and 22).
   Archipelago's plum, slate blue and cyan are neighbours on the colour wheel and blurred together as big starbursts
   side by side, so the default is a loot game's ladder in the same order of importance (filler green, useful blue,
   progression purple, trap red); Archipelago keeps its own client's colours for those who know them.
4. **In the game** (the mod guide, step 9): the game's text colours by palette index only, so the colours are added
   after the game's own; that and the lines' wording are game-side, written up there.

**Seen (2026-09-26):** "You got Kabbu from TestPlayer!" in dark plum and dark yellow (a dev hold-up), and
every colour on screen while picking them. **Then with a real second slot** (an APQuest slot "Other", items placed by
plando both ways; `development.md`, "A second player"): found here, "You found Other's Key!" (a gift, progression),
"...Health Upgrade!" (a pickup, useful), "...Confetti Cannon!" (filler) and "...Math Trap!" (trap), each in its
colour; received, "You got Kabbu from Other!" and "You got Leif from Other!" when Other's chests were checked.

**Status:** works, seen on screen with a real second player (2026-09-26), every class and both directions; Rarity, the
default since, seen on a gift (the icon and its text in purple).

*Code: `HoldUps.cs` (the colours, `AddApColors`), the row in `QualityOfLife.cs` (`ItemColors`).*

## Build step 20: All Three (the default), every member from the start, no member items

*Starting Party Member* gets a sixth choice, **All Three**: a new file starts with Vi, Kabbu and Leif, and no member is
an item. It is the default (2026-09-27: "so that you can start with all 3 if you don't want to rando partners";
then "all 3 could probably be the default"). With *Off*, Leif joins at a fixed spot after the spider, behind the
Explorer Permit, so a late permit makes him late; with All Three nothing waits on him.

1. **The world:** `option_all_three = 5`, the default. `starting_member` becomes 3 (`ALL_MEMBERS`) and all three are
   start inventory; the pool has no member, and filler takes the two slots they'd have held. The two joining moments
   stay locations, as with one member (the category is on for any start); the story's "Leif Joins" event is off. Rules
   ask for members as before, and all three are held from the start, so none waits.
2. **The mod:** `starting_member` 3 allows every member (`PartyMembers.AllMembers`). The start inventory arrives as
   received items, and each member joins as a received one does. **A story party change no longer drops a member the
   story hasn't reached yet** (Leif before flag 16): the opening's "Vi and Kabbu" keeps Leif (`KeepMembersAhead`), in
   every mode. The spider scene (Event6) is the exception: its fights stay the story's, and Leif rejoins after it.
3. **Tests:** `TestStartAllThree` (start inventory, no member in the pool, the two locations, nothing waits),
   `TestPartyDefault`; the tests of the story party's logic (Leif's droplet rooms, the permit gate, the town medal, a
   shop count) now pin *Off*, whose logic they check. A default seed with APQuest generated: all three in Starting
   Items.

**First play (2026-09-27):** all three were there, but no member showed a box and Leif appeared a moment
after the start. No box: the members are start inventory, which the server has at login, and the receiver showed no
box for what it had at login. Since 2026-09-28 replays are held up too (the mod guide, step on Item animation), but
starting items are quiet, so the starting members still arrive with no box. Leif late: items are given only while the
player is free, after the opening skip and a map change. Now, with All Three, the opening's own party change adds
whoever the story hasn't reached yet, so Leif is there from the first frame; his item then finds him already in.

**Status:** works, seen on screen (2026-09-27): a new file starts with all three at once (log: the opening done
with party 0, 1, 2; Leif's item found him already in). On a fresh seed both opening spots showed their box (Poison
Resistance, then Sleep Resistance from the silent spot); a second file on a used seed showed only the gift's, since the
server already held the silent spot's item at login. Since 2026-09-28 the opening's checks are quiet: no box for either.

*Code: as build step 18 (`StartingPartyMember`'s `all_three`, the default); tests `test_party.py`
(`TestStartAllThree`).*

## Build step 21: Shuffle Field Moves, the three starting moves as items

Vi's Beemerang, Kabbu's Horn and Leif's Ice become items (2026-09-27: "field moves on/off, and jump as its
own on/off thing as well due to how much it impacts, both off by default"). The rules were already written in moves
(build step 13), so the logic only learns that a move is also an item.

1. **The game's own move** (`PlayerControl.DoActionTap`, `MEASURED.md`): the leader's field attack by his `animid`;
   Vi's is allowed before flag 41 and after it with flag 11 (set early), Kabbu's and Leif's are always on.
2. **The world:** option `shuffle_field_moves` (off). Three items, kind 6 (`MOVE_ID_OFFSET`), in the pool only with it
   on. `requires` (`rules.py`; since build step 29 `CanUse`) turns an ability into its member (when members are items;
   with the story's party only Leif, who joins late) and, with moves shuffled, its item. **Cautious like members**
   (chosen): the gate's exit lists `moves` (all three items, not who uses them, so the story's Leif isn't pulled before
   the gate); the measured spots before it name their ability (the two horn spots; the fountain rooftop and the droplets
   now say Ice). `slot_data` `shuffle_moves` (since build step 39, `options` `shuffle_field_moves`).
3. **The mod** (`FieldMoves.cs`): a prefix on `DoActionTap` refuses the leader's move until its item has been counted
   (recomputed every frame from the save's counted items, as members are), with the game's own
   `MainManager.PlayBuzzer()` (a short "can't" sound). Only with Archipelago on and the seed saying so. A
   move item's box shows its member's party icon and colour, no article, "\<member> can use the \<move>."
4. **Tests** (`test/test_moves.py`): off by default; the three in the pool; the gate needs every move; a horn spot
   needs the Horn, an ice spot the Ice; with the story's party the ice spot needs Leif too. Five seeds with both
   options, a random member and APQuest generated.

**First play (2026-09-27):** the jump stayed locked until its item, but **every attack worked from the
start**. The prefix on `DoActionTap` never ran: the method only builds its coroutine and is small enough for the
runtime to inline into its callers, where a patch never runs (no "refused" line for any attack in the log, while
`DoJump`'s patch worked). The gate moved to the coroutine's own first step (`MoveNext` at state 0), which ends it
before it sets `action`; the game clears `actionroutine` only at a tap's end, so the mod clears it a frame later.
**The moves as key items (the same day):** "make all these locked abilities into visual key items... so you
can see in your inventory that you have jump". Four key items of the mod's own after the Boat Ticket (201 Beemerang,
202 Horn, 203 Ice with the member's party icon; 204 Jump with the Archipelago icon; `CustomItems.cs`). Receiving a move
puts its key item in the bag, and the gate reads the bag, so the inventory shows exactly what works.
**The game's own names (2026-09-27: "we shouldn't make up names if there is something vanilla"):** the
game's `Skills` text names the three **Beemerang Toss**, **Horn Slash** and **Freeze** (`MEASURED.md`); items, rules
and key items use them. Jump has no entry there, so it stays "Jump".

**Seen:** the attacks now refused. **Holding the attack button buzzed nonstop** (B only; the jump fires
once per press): the game retries a held attack every few frames (`DoActionHold`), so the buzzer now plays only when a
refused press comes after a quiet 0.25 s, once per press. **Then it felt delayed** (in play): the game fires a tap
on the button's release, so the refusal came then. Now the attack's buzz plays on the press itself (a per-frame check
of the button while the leader's move is locked and the player is free), and the refusals stay silent; the jump
fires on its press and buzzes there. **Seen (2026-09-27): "works way better".**

**Seen (2026-09-27):** bought Freeze and Horn Slash at the caravan; each worked from then on, and Freeze
alone didn't unlock the others. (Buying first failed with the dev berry cheat on: it refilled the berries, and item
shops see a purchase as berries going down; the cheat is now a one-time top-up.)

**Save crystals without a move (2026-09-28):** the game only starts one from an attack's hit, so with no move
item there was no save or heal; confirm next to a crystal now uses it, as an NPC is talked to (`documentation.md`,
step 29).

**Kabbu's Dash without the Horn Slash (2026-10-09):** refusing his whole tap also refused the Dash, its second press;
now the tap starts the Dash once it's learned, with no horn hit: build step 63.

**Status:** works, seen on screen (2026-09-27): each attack locked until its own item, the buzz on the press,
Beemerang Toss from Madeleine's table; the key items in the bag (Freeze and Horn Slash with Leif's and Kabbu's party
icons, Jump with the Archipelago icon, "Kabbu can use Horn Slash." as the description; seen in a screenshot).

*Code: `options.py` (`ShuffleFieldMoves`), `rules.py`; in the mod `FieldMoves.cs` and `CustomItems.cs`; tests
`test_moves.py`.*

## Build step 22: Shuffle Jump, Jump as an item

Jump becomes one item for the whole party ("jump would just apply for any member/the whole party, unlike
the attacks"), behind its own option, `shuffle_jump` (off).

1. **The game's jump** (`PlayerControl.DoJump`, called only by the jump button): no gate of its own.
2. **Cautious logic** (chosen): with it on, every location and story event (artifacts included) needs Jump
   except those seen reachable without it, marked `no_jump` in the data. **Measured on screen (2026-09-27, the
   starting map and the town):** the ladybug siblings' house item needs no jump; Madeleine's house does, and so do
   Artis's two checks (the Hard Mode NPC) and the inn's item (not a location yet); the plaza statue discovery (not a
   location yet), the caravan and the Commercial District's two shops need nothing; the underground bar needs the Horn
   (grass); the inn review quest's completion needs nothing (if quest completions become locations). The two opening
   checks need nothing either (they happen on their own). Jump lands in one of those spots, or in another game.
3. **The Warp is forced on** with it (like a random start or the entrance randomizer), the way out of a spot
   you can't jump out of: `QualityOfLife.WarpOn` reads `FieldMoves.JumpShuffled` from `slot_data` `shuffle_jump`
   (inside `options` since build step 39).
4. **The mod:** a prefix on `DoJump` refuses the jump with the buzzer until the item is counted. Jump's box shows the
   Archipelago icon (it belongs to no member).
5. **Tests** (`test_moves.py`, `TestJump`): the measured spots are reachable with nothing; Madeleine's house and the
   first artifact need Jump.

**Status:** works, seen on screen (2026-09-27): the jump locked until its item (the Ladybug house), then free for
the whole party; the Warp stayed in the pause menu with Travel set to Off.

*Code: `options.py` (`ShuffleJump`), `slot_data.py` (`shuffle_jump`); in the mod `FieldMoves.cs`; tests
`test_moves.py` (`TestJump`).*

## Build step 23: the seven learned field abilities as items (always, progressive)

Decided (2026-09-27): every ability the story teaches is an item, **always** (not an option: "randomizing things the
player would have found"), and the scene that teaches it is its location, as the party members' joining spots are, so
no temporary double grant is ever left to remove. Three are **progressive, always**, in the game's own order (a second
level without the first "wouldn't work"): Beemerang Toss then Halt, Dash then Horn Dash, Freeze then Icicle. All are
*progression*: each unlocks checks.

1. **Read first** (`MEASURED.md`, the unlock scenes and every field ability): seven scenes in chapters 2-6, each
   setting one flag that is also a story gate (the Dig flag opens the hideout's next scenes, the Horn Dash flag removes
   a rock); the same flags give battle skills (`MainManager.RefreshSkills`).
2. **The ability table** (`abilities.py`): each ability by the game's name, its member, its item, its level (copies of
   the item it takes) and the option that makes its base level an item. With Shuffle Field Moves off the party starts
   with the Toss and the Freeze, so their progressive items take one copy, the upgrade. Rules name abilities, never
   items; `rules.requires` (since build step 29 `CanUse`) turns them into item counts (`HasAllCounts`), and the pool
   puts in enough copies for the highest level. Existing ids kept: the Beemerang and Freeze items were renamed, not
   renumbered.
3. **The locations:** the seven scenes, each checked by its own flag (`source.flag`), in a region *Later Chapters*
   past chapter 2's start that needs everything the story used before (the permit, the Boat Ticket, the first boss,
   the party and its attacks). Until chapters 2-7 get room-level logic, each needs every ability taught before it
   (story order): more cautious than the game. They show no item of their own (`silent_locations`). Their names are
   the user's (2026-10-04, build step 1). 7 locations and 7 items: the pool stays balanced in every option set.
4. **Combat stays basic**: no fight needs a battle skill or a medal in the logic, only each member's plain
   attack. It keeps enemies and bosses simple, and leaves room to play out of logic for fun (Kabbu with a medal that
   hits fliers might beat the spider the logic gives to Vi).
5. **The mod** (`Abilities.cs`): each learned ability is a key item of the mod's own (205-211, after the moves'),
   named and described from the game's own Skills text (`skilldata`) in the player's language. The receiver gives a
   progressive item's next level. Where the game reads an ability flag to *use* it (8 reads in `PlayerControl`, the
   thrown Beemerang's hold in `NPCControl`, 15 in `RefreshSkills`), the read (`ldfld flags; ldc.i4 n; ldelem.u1`)
   becomes a call answering from the bag; the story's reads and the flags themselves stay the game's, so every scene
   still runs and marks its own check. The counts come from the game's IL (an ILSpy IL dump), not its decompiled C#,
   and the mod logs how many it patched against them. A battle skill comes with its ability's key item
   (seven key items, not fourteen). The Warp is always on with it (`QualityOfLife.WarpOn`): a spot such as the
   hideout's cell is left by an ability that may not have come yet. Only with `slot_data` `ability_items`, so older
   seeds play as before.
6. **Tests** (`test_abilities.py`; `test_moves.py` and `test_party.py` updated): every learned ability in the pool;
   **exactly one location per ability**, by its flag (a guard against a second check for the same one);
   story order; the Horn Dash as the second copy. 395 tests pass; every option set of the scratch snapshot generates,
   is beatable and gains exactly the seven locations; a room with APQuest generates.

**Status:** built (2026-09-27): the logic and pool tested, the mod built, its patch counts taken from the game's IL and
confirmed in the running game (its log: 8 of 8, 2 of 2, 15 of 15). Seen (2026-10-04): a received ability working (Bee
Fly), its battle skill, the key items' text; a scene sending its check (2026-10-04: the Dash's, Event221 at the Lost
Sands' entrance, location 69, its flags 88 and 138 set by hand on a test file). Without the Horn Slash the Dash
only moves and the Horn Dash only breaks boulders, for Shuffle Field Moves: build step 63.

*Code: `abilities.py` (`item_count`, `item_copies`), `slot_data.py` (`ability_items`); in the mod `Abilities.cs` and
`CustomItems.cs` (ids 205-211); tests `test_abilities.py`.*

## Build step 24: the logic, second part: the rules for writing it, room by room

The logic is what Archipelago uses to prove a seed can be finished, and some options will lean on it hard: one party
member, a random start in any room, a decoupled entrance randomizer, shuffled attacks, no Jump. So before the rooms of
chapters 1-7 are mapped, this is how every part of it gets done (2026-09-27: "the logic has to be precise").

**The rules, the checklist, the method and the tests: [room-logic.md](room-logic.md)**, the one place every plan for
the room logic lives (gathered there 2026-09-30, the user: "to have it all in 1 place"). How they came about:

- **2026-09-27:** the method and the per-room checklist written with the user (their questions: one-ways per entrance
  and inside a room, roadblocks and ledges, what each location needs and whether you can get back, spawning anywhere);
  combat kept basic, with who can hit what.
- **2026-09-29:** the rules for writing it, 1 to 4 the user's ("to simplify things"), and rule 9, the Warp a safety
  net and never logic; the region explainer (How it works §11).
- **2026-09-30:** everything gathered into room-logic.md, from build steps 8, 9, 12, 15 and here, and two parts added
  (the user): every place the party can appear, checked like an entrance, and every story and quest chain, checked
  across the rooms it reaches. Then, the user's idea ("exclude 1 or a few certain progression items to see what/if
  they break anything"), `dev-scripts/item-gates.py`: what each progression item, and each pair, gates in the logic,
  with events earned rather than handed out, to read against the game (`development.md`, "What each item gates").
  Its first run: 13 progression items, from Jump (45 spots) to Bee Fly (2), no *or* anywhere yet. And an option the user
  asked for, *Points of No Return* (build step 37): off by default; with it on, rule 4's way back is dropped and the
  Warp counts as the way back to the start.

**Status:** planned (2026-09-27); the rules written 2026-09-29, the logic in Python since build step 29; every plan
gathered into `room-logic.md` with spawns and chains added (2026-09-30). No room mapped with it yet: today's rules are
still by large areas, kept as each spot's `reach` over one region per map (build step 12, 2026-09-30). The first room
mapped (2026-10-05, seen by the user): `BugariaOutskirtsOutsideCity`, the Explorer Permit gate an area of its own, its
pushed-through walk-in a one-way out (an `Area`'s `out`; a blocked walk-in that pushes the party through counts as a
one-way since, the user); then `NearSnakemouth` (needs nothing) and `OutsideSnakemouth` (a ledge and grass as areas),
the first corridor (Jump; Jump and Icicle to Seedling Haven's door) and the second (the horn), Chuck's Abode (its berry
behind a rock, the Horn Dash), the Golden Path tunnel (four parts), the Golden Path (Jump, Icicle), the pier (its dock
an area: Jump up, the boat leaving and landing there, `Transfer.from_area`), the first East Road (four parts), the
second (the crank or Icicle; an ability used for what it's made for counts, rule 2), the Lost Sands' entrance (its
guard's gate kept open), `Blank`, the attack map (story-only), Seedling Haven, the Cave of Trials (its altar for the
quest pass), GoldenPathTunnel2 (its climb), the Hermit's cave, 18 of 244 (the Outskirts done; East Road 2's top
corrected the same day: across water, not a drop); room 16's three dig spots added. A fixed (unshuffled) door link can
stand in an area (`Area.links`): the Golden Path tunnel's upper ledge, joined to Tunnel2, which no seed could reach
without it (2026-10-05, a generation failure). Locations now sit in the part of the room they're in (`Location.area`, as
Archipelago's regions hold locations, `world api.md`), and an area may join another (`Area.to`) by 2026-10-05.
Snakemouth Den from 2026-10-06: the bridge room (its banks two areas; the bridge a room event either bank can set, so a
story event may sit in an area too, `StoryEvent.area`; its vine berry location 94, kept present from the start), the
door room (the trapdoor an event opening the hole down; the big door kept open, its walk-in stuck behind it; the high
door to `SnakemouthTop` a drop), the fall room (its door-room door on a ledge: Jump up, a drop down), the lake (its top,
with Leif's scene, up by a switch and platforms), the underground door room (five parts; its big door an event needing
the two side rooms' switches, theirs cautious until mapped), the mushroom pit (its bottom door a drop down, Jump back
up), the treasure room (the Spider fight: Vi), the right underground room (Jump and an attack across, both ways), the
right bridge room (its top up by Jump, Freeze and the horn; its high door gated by the big switch), the first left room
(its high door by Jump, Freeze and the horn), the upper left room (the same, its high door gated by the big switch), the
top (nothing to go in or out; its Sophie Petal for the quest pass), the upper entrance (its top door shut until the gem
is placed, Jump and a stand-in for the gem), 31 of 244; Bugaria City from 2026-10-06: the ant tunnels (nothing to cross;
each tunnel only its flag), the main plaza (two new locations: the red house's roof, the Flower Key, and the dig spot's
berry, Beetle Dig), the commercial district (its bar corner behind grass, the horn; the arcade kept open), the theater
(two new locations: the moth's plushie sale and the spinner's crystal berry, the horn), the residential district (its
rooftops: the horn; Jump and Freeze), the underground bar (free in and out by its bounce pad), the palace hall (free),
the throne room (no items; two story berries later), the palace bridge (free), the library (free, its bookshelf's Lore
Book without Jump), the war room (its table's medal, Jump), the miners' break room (a new location: its dig spot's
berry, Beetle Dig), the attacked plaza (free), the attacked bridge (free), the attacked palace (free), the ending's
plaza, bridge and throne room (free, played through), 49 of 244; Lost Sands from 2026-10-06: the entrance (four ground
doors free; the ledge door to the book area a drop only), the badlands (two new locations; the hideout door Jump and the
Rusty Key, a stand-in until its sale is a location), the book area (two halves joined by Horn Dash or Bee Fly; a new
location under the book), the rock formation (a new location: the Tardigrade Idol, Jump, Freeze and the horn), the south
trench (its left side across a gap, by the bridge the horn knocks over or Bee Fly; a new location on the top-left ledge,
Jump), the Defiant Root entrance (its left side, Bee Fly, or the crank and Beemerang Halt leaving it; a new dig spot
behind a rock, Horn Dash), the Far Grasslands border (free, the gate kept open), the Defiant Root's south entrance
(free), the badge alcove (its left door a drop only; two new locations, a ledge medal with Jump and a grass drop with
the horn), the caravan camp (free; a new location: its dig spot's berry, Jump, Bee Fly and Beetle Dig), the sand pit
(its doors joined through the middle by eight bridges the horn knocks over, five events, or Bee Fly), the Golden Hills
border (free; a new location: its ledge medal, the horn and Jump), the roach village (free; a new location: its dig
spot's berry, Beetle Dig), the oasis (its bottom door dug under; its top right reached only from its cave door, back up
by a platform; two new locations), the oasis entrance (its right side the bubble shield or Bee Fly, both ways; its
bounce pad back kept present for good, the user's ask), the west dunes (free), the sand castle's front (the castle door
the Sand Castle Key and Jump, a stand-in until the key chain is gone through), the mountain (free; its bridge without
Jump a bit tricky, counted by the user's call), the trench's middle (three sides: the middle a drop to either, the left
back up with Jump and Bee Fly, the top door dug under), the thorn field (its high right door Jump and the bubble shield,
or the bridge the horn knocks down from the right), the southern desert (its right door the shield or Bee Fly; its top
door a drop only), the scorpion's room (free), the eastmost room (free), 72 of 244: Lost Sands done; Golden Hills from
2026-10-06: the dungeon's entrance (its top right door the Wooden Crank and Beemerang Halt, its elevator the Big Crank
and Halt up, Halt down; its arrival scene kept away, the user's ask), 73 of 244; the left hall (across Jump and
Beemerang Halt, its top left door the Wooden Crank and Halt; a new location: its berry, Jump), the left crank room (its
door free; its crank spot with Next 62), 75 of 244; the right crank room (its every need for both spots; a new location,
the candy on a stump), the lower right crank room (its door free; its crank spot with Next 62), 77 of 244; the left
crank half room (two new locations, Jump and Beemerang Halt; its crank half with Next 62), 78 of 244; the upper hall
(its boss door behind two shrines' offerings, two events, a stand-in for the offerings; its upper right only from its
own door), 79 of 244; the upper side room (its upper door and crank the Wooden Crank in its slot, Jump and Halt), the
boss room (free; its fight up two ledges, Jump), 81 of 244; the pitcher path (its top door Jump, the horn and Halt; a
new location behind thorns), 82 of 244; the upper pitcher path (its top left door Jump and Halt; a new location: its
berry, Jump and Halt), the pitcher plant arena (free; its bounty for the quest pass), 84 of 244: Golden Hills done;
Golden Path from 2026-10-07: the cable car station (its right door a drop, back by Jump or the horn; across Jump; three
new locations; its medal with the CableCar quest), 85 of 244; the crank path (between its doors Jump and Beemerang Halt;
a new location), 86 of 244; the settlement entrance (the caravan's stall kept present for good and the snail's shop that
takes its spot kept away, the user's choice after the snail's goods overlapped, its three items new locations; a new dig
spot; the minigame door behind the horn quest, a stand-in; the desert door behind its gate's lever, hit from the desert
side), 87 of 244; the cave path (across Jump and Beemerang Halt, back also Horn Slash, or Bee Fly; its bottom the
Shield, back up Shield and Jump; its Chomper Cave door, locked until the story's Shield, kept open and its wall hidden;
a new location: a dig spot, Horn Dash), 88 of 244; Whack Farms (free; its Wacka Worm prize a new location, Vi and the
Beemerang, build step 51; the mayor's visit for the quest pass), 89 of 244; Golden Settlement from 2026-10-07: the
square, by day and by night as one room (its night switchable, build step 52; four doors free; seven new locations: the
Lore Book in grass, the horn, the Mothiva Doll at night, Jump, and five shop slots), 91 of 244; the farm, by day and by
night (its power plant door kept open; seven new locations: the festival's games, Chubee's gift, the windmill's berry
and its farmer, the night scene, and a dig spot by day; the offerings items, build step 53), 93 of 244; the houses, by
day and by night (free; nothing to place), 95 of 244; the power plant (two areas, its switches opening the door between
them; one new location: its discovery), 96 of 244; the Forsaken Lands from 2026-10-07: the fog maze's start (free;
Patton's lab open from the start, its door and slab as the game has them from flag 376), 97 of 244; the room below it (a
raised strip, Jump back up), 98 of 244; outside the Termite gate (the gate needing nothing, its first opening skipped
and opened only when talked to, documentation step 10; the left door Horn Dash), 99 of 244; the broken bridge (Bee Fly
across, a ruler bridge knocked down with the horn from the upper right, drops; the Bee Fly spot placed on the left), 100
of 244; the ant tunnel's room (free; one new location: a Plumpling Pie, a new filler item), 101 of 244; the Primal
Weevil's room (free; its fight winnable with plain attacks, the summoned Weevil included), 102 of 244; the pink spider's
room (a ledge, Jump; one new location: crystal berry #38 from her first trade, with `ItemOnHand`, an item shop reached),
103 of 244; the tanks room (free), 104 of 244; the mushroom maze (Beetle Dig between its doors, its left one a one-way
exit by the user's choice), 105 of 244; the Abandoned City (Jump to its top; two new locations: a dig spot and a
respawning Magic Seed in grass, Bee Fly and the horn), 106 of 244; the pumpkin patch (three parts: Jump, the horn and
Bee Fly up; Horn Dash and the rest to its high door; a new location: a Squash in grass, a new filler item), 107 of 244;
the wind pipes (a one-way ring of four parts, Bee Fly with Horn Dash and Jump; a new location: a Lore Book on an
isolated platform), 108 of 244; the dome overlook (free; two new locations: its vista's discovery and a Squash in
grass), 109 of 244; the False Monarch's tent (free; its bounty for the quest pass), 110 of 244; the tunnel side (Jump,
Horn Dash and Bee Fly to its high door; three new locations: a Lore Book in tall grass and two Squash dropped once by
grass), 111 of 244, the Forsaken Lands done; the Far Grasslands from 2026-10-08: the border cave (each side door behind
a gate its own lever opens; the ant tunnel's miner past grass, the horn), 112 of 244; the crossroads (free; a new
location: a dig spot's crystal berry), 113 of 244; outside the border cave (its right door Horn Dash, its bottom left
Beetle Dig), 114 of 244; outside the wizard's tower (the tower side Shield or Bee Fly; the hole a one-way in, the horn;
the front door shut both ways until the wizard unlocks it, a door rule on both ends; a new location: the Lookout Rock's
discovery, Jump or the tower's stairs), 115 of 244; the west path (its top door Jump; a new location: a crystal berry on
the tree root), 116 of 244; the lake (the Wasp Kingdom's front gate kept shut, impassable until after the story, Maki's
turn-back before it kept away; three new locations: a Lore Book, a Hot Drink, a new filler item, and a dig spot, Icicle
or Bee Fly), 117 of 244; outside the Fishing Village (its door Jump; Riz's fight always offered, build step 54), 118 of
244; east of the crossroads (three parts: the horn to the bounce pad, Jump on to the right door, Jump and a drop back),
119 of 244; outside the swamp (its top Jump, Icicle and Horn Dash up, a drop then Icicle and Horn Dash down; its Wasp
Kingdom door, there in the game only after the swamp's boss, open from the start and Maki's turn-back kept away), 120 of
244; the swamp boss's room, out of order while testing its boss (Jump and Leif across, past the boss), 121 of 244; the
residential district rechecked (2026-10-08: the fountain rooftop by Bee Fly alone too; a new location, the banker's
Platinum Card, Jump, the card a new useful item), still 121; above the west path (three parts: the bottom right ringed
with thorns, the Shield or Bee Fly; the top right's clearing door behind a boulder, Horn Dash, arriving past it; the
raised left Jump and Horn Dash from the top right, a drop down by Bee Fly or onto the thorns with the Shield, Jump and
Bee Fly up), 122 of 244; the badlands rechecked (2026-10-08: the center pillar by Jump and the Beemerang too; the rock
ledge by Jump with Bee Fly, or Freeze and the horn, too), still 122; the tower's basement (the floor free; its door up a
ledge, Jump; two new locations: a Burly Tea behind the stairs, and crystal berry #37 on the bookshelf, Jump and Bee
Fly or the Beemerang; its fall scene and wizard kept away, the tower's front door open, build step 59), 123 of 244;
the tower's stairs (the bottom free; the attic's door up, Jump, a drop down), 124 of 244; the tower's attic (free; a
new location: a Bad Book beside the cauldron; the wizard up a ledge, Jump, and the capsule machine for the quest and
sellers' passes), 125 of 244; the Broodmother's lair (free between its doors; its fight, which starts on entering by
either door, for the enemy pass; its berries, quest and prize medal for the quest pass), 126 of 244; the clearing
(free; a new location: the Mechanical Claw behind a small bush, a new useful key item, progression once Defiant Root's
trade for it is a location; Maki and Kina's platform, Jump, for the quest pass), 127 of 244, the Far Grasslands done;
the Wild Swamplands from 2026-10-08: the swamp's first room (free; its opening talk skipped with Skip cutscenes, the
mod guide's step 10), 128 of 244; the lily pad pond (its top door Jump and the horn or Bee Fly up, the horn or Bee Fly
down; two new locations: a dig spot up top, Beetle Dig, and a Honey Drop in grass by the bottom door, the horn; its
Leafbug ambush for the enemy pass), 129 of 244; Leafbug Crossing (three parts in a row: the bottom, the middle with
its tree, the horn, and the upper right, each Jump or Bee Fly; before the tree falls, a drop from the upper right into
the middle), 130 of 244; the swamp bridge, kept up (build step 61), 131 of 244; the long swamp room (a boulder at each
door, Horn Dash, Beetle Dig too on the right; the middle by Jump and Freeze, or Bee Fly, back without Freeze one-way;
the right side by the horn, Horn Dash and Jump, or Bee Fly), 132 of 244; the Junction (four doors; a crane's platform
moved by levers between its left and right sides, Bee Fly or the long way round; a lift up to its top right; crystal
berry #27 and a Clear Bomb on a vine, locations 183 and 184; its centipede scene kept away, build step 62), 133 of 244;
Crank Pond (the middle's crank, Beemerang Halt and Jump, to the lower right, back by the lily pad, the horn; its right
door up a lift, Horn Dash, the Halt and Jump; a Burly Berry and a Crunchy Leaf in grass, locations 185 and 186), 134 of
244; Ice Block Climb (ice blocks frozen, knocked with the horn and jumped on, one from the droplet behind a boulder,
Horn Dash, brought up to open the middle's ways up and across; Bee Fly for some; the medal Eternal Venom on a stump,
location 187), 135 of 244; Fenced Pond (a double fence between its middle and its right side, taken down by a lever on
a ledge, Horn Dash, the horn and Jump, or Jump and Bee Fly; a Magic Seed dug up on the right, location 188), 136 of 244,
the swamp done; Defiant Root from 2026-10-09: the Square (the ground and its four doors free; the rooftops up with Jump,
a drop down; six new locations there, 189 to 194: a Lore Book behind a box and Morty's Bed Bug on the ground, a Berry
Juice and crystal berry #15 on the rooftops, kept there until taken, build step 64, and the mayor's storage's Lore Book
and Dark Cherries behind its locked door, pending until the quest pass since the Desert Key needs quest 46 from chapter
5, build step 44's way, as the review found; the Bed Bug a new useful key item), 137 of 244;
the Well (its landing and bounce pad up to the town; its right side, the hideout's door, by Beetle Dig; a Leaf
Croissant on boxes there, Jump, location 195), 138 of 244; the Market (one door, all of it free, no locations then,
its three shops' slots added 2026-10-10, locations 217-228; Kali's shop shut until her board quest is taken, left so),
139 of 244; the Beehive Lift (the ground free; the elevator's
platform and the inn's upstairs each up with Jump or Bee Fly, a drop down; two new locations: a Lore Book behind the
inn's high door, kept open, build step 65, Jump or Bee Fly inside, and the medal Fortify on its roof, Bee Fly, 196
and 197), 140 of 244, the Defiant Root done; the Ancient Castle from 2026-10-09: its entrance (the middle's bridge
shows while its crystal is lit, the Beemerang Toss from either side, or Bee Fly; no items), 141 of 244; the Slide
Puzzle (its floor a drop from every side, Jump or Bee Fly back up to its lower door; a block knocked into place with
the horn fills its upper gap and opens its upper left door, or Bee Fly crosses; the medal Frostbite burrowed to,
Beetle Dig, location 198), 142 of 244; the
Statue Room (over the middle's block on platforms: from the left Icicle and Jump, or Bee Fly; from the right Jump or
Bee Fly; no items), 143 of 244; the Basement (its door on an isolated
ledge, the middle by Jump or Bee Fly; three spots on its platforms, the Toss and Jump or Bee Fly, the Ancient Key
behind a barrier, Halt and Jump or Bee Fly, locations 199-201, pending with the castle's 198, build step 67), 144 of
244; the Roof (its doors and save crystal free; a Frost Bomb behind the left statue, 202, pending; the boss door
locked from its side until the Big Ancient Key, the boss key room reached while that key is the game's own pickup),
145 of 244; the Main Room (the hub, five parts: the bottom free; the middle left and the top left cut off, drops; the
middle right and the top right up their lifts, each started by its own switch, with Jump; the middle right and the
middle left joined by a ledge outside, Jump or Bee Fly, the way up to the top through the Slide Puzzle's upper part
and the Pressure Puzzle; the statue room's lock opened by the Basement's key, the boss key room's needing both keys'
spots while they're the game's own pickups), 146 of 244; the Boss Key Room (a Cold Salad
round the edge, nothing needed; its right side by the block pushed with the horn and Jump, or Bee Fly, back with Jump
or Bee Fly; the Big Ancient Key there, past three flying Wardens, Vi; locations 203-204, pending; the Roof's boss door
now needs that side and Vi), 147 of 244; the Pressure Puzzle (its two doors on one floor, the main room's shut from
inside until its plates, Freeze and the horn, open it for good; played the other way they raise platforms to an
Ancient Key, then Jump or Bee Fly, location 205, pending), 148 of 244; the Rock Room (its bottom's two sides joined
by a platform its switch starts; the top left up with Jump or Bee Fly; the top right past a rolling rock and thorns;
across the top once its boulder breaks, by Horn Dash or a rolling rock carried on the crystals' platforms; crystal
berry #24 in an alcove off the top right, Shield, then Jump or Bee Fly, location 206, pending), 149 of 244; the boss
room (one region, both doors free, the Watcher's fight on the way across needing nothing; its wall before the
treasure room's door kept open, build step 68; no items), 150 of 244; the treasure room (one region, its door free;
the castle's artifact on a platform, Jump or Bee Fly, held out of `ARTIFACTS` with the castle, build step 67), 151 of
244, the Ancient Castle done; the Bee Kingdom Hive from 2026-10-09: Outside the Beehive (the bottom,
the elevator bee down for nothing and the hive's main door, and the left, a bridge between the hive's side door and
the factory's, cut off from each other; the factory door kept open, build step 69; no items), 152 of 244; the Throne
Room (one region, its door free, kept open from the main area, build step 70; no items), 153 of 244; Jaune's Gallery
(one region, its door free, kept open from the main area, build step 71; a Bad Book behind the paintings, nothing
needed, location 207), 154 of 244; the Scanner Room (a corridor, one region, nothing needed across; kept between
the outside and the inside, its top door made and the main area's bottom exit sent into it, build step 73; its gate
open, build step 72; its scan location 208 with flag 160, build step 74), 155 of 244; the Main Area (one region,
every door free; the clothing stall's Bee Hat and then Pretty Ribbon, locations 209-210, after Mothiva's scene, a
story event that needs nothing, at their prices), 156 of 244; HB's Lab (one region, its door free; HB asks for the
Explorer Permit from the start and shown it opens B.O.S.S., build step 75), 157 of 244; the Balcony (one region, its
door free; Beette's sale, location 78, needing nothing in the room, at her price again, build step 76, its berries with
Next 63), 158 of 244; Honeycomb's Lab (one region, its door free, no location), 159 of 244, the Bee Kingdom Hive done;
the Honey Factory's Lobby (its bottom a drop, Jump or Bee Fly back up; the processing door's Factory Pass lock kept, a
stand-in until Next 67; the storage door open, build step 77; its shop's five slots, locations 212-216), 160 of 244;
the Worker Rooms (the office and the sleeping quarters cut off from each other; the desk's Shock Candy, location 229,
Jump or Bee Fly; the Factory Pass the game's own pickup until Next 67), 161 of 244; the Core (one region, its door
free, the gate to the boss arena the story's), 162 of 244; the First Room (its switch, the Shield's spot, a basic
attack, its story-order stand-in gone; across only on the platforms it starts, the Shield, since Bee Fly works only
before the switch; the bottom a drop, up to the right only; the tram left to the story), 163 of 244; the rest of
`room-checklist.md` to go.

## Build step 25: DeathLink, a panel row, deaths sent and received

**What it is:** Archipelago's DeathLink, one of its "bounce" features (`docs/network protocol.md` at 0.6.7,
"DeathLink"): a client wearing the `DeathLink` tag sends a `Bounce` with `time`, `source` and an optional `cause` when
its player dies, and the server passes it to every client on the team wearing the tag, the sender's own included. Each
game decides what "die" means.

**Decided (2026-09-28):**
- **A row in the Archipelago panel, *DeathLink*, ON / OFF, off by default; not a yaml option**, so a player can change
  their mind mid-seed. Only while Archipelago is enabled; the seed and the logic know nothing of it.
- **In the panel on the main menu, next to *Archipelago*, not on the Gameplay page** (2026-09-28: "its an AP
  setting"; first built on the Gameplay page, which the pause menu's Settings also opens). Changing your mind means
  going back to the main menu, on purpose: a death waiting in game can't be switched away from the pause
  menu, and returning to the title drops it, which costs as much as the death.
- **A death DeathLink caused never sends one** (a common apworld bug, two games killing each other in a
  loop, or a death queued before you're back in game that kills you and sends again). Only the game's own deaths send.
- **A death that can't land yet waits, and strikes at whatever comes first afterwards:** free on the map, or the
  party's turn in a normal battle. Never inside a cutscene, a text box, a menu, a door or a scripted fight.
- **In a battle: the game's own Game Over menu** (Retry, Retry after changing medals, Load, Title). **On the map:** a
  Game Over (music out, black, its sound), then the last save, as the menu's Load does.
- **Own team only, as it is** (the user, 2026-10-06). At 0.6.8 a `Bounce` may name other teams (`teams`) and say how
  its targets combine (`operator`: `or`, `and`, `legacy`; `network protocol.md`, "Bounce"), but the DeathLink section
  is unchanged, Archipelago's own `CommonClient` sends neither field, and MultiClient.Net 6.7.1 has none: the standard
  DeathLink stays on its own team, which ours already does (0.6.7's server allowed nothing else). Kept until another
  game or client shows how a cross-team one is done.

**What counts as a death here** (`MEASURED.md`, save crystals, saving, Game Over): only a party wipe in a battle.
Hazards and falls cost no HP; they put the party back. A scripted loss (`battlelossevent`, the story's own defeats)
isn't a Game Over, and neither sends nor receives.

**How it works:**
- **The connection** (`ApConnection.cs`): MultiClient.Net's `DeathLinkService` (read at `v6.7.1`: `EnableDeathLink` /
  `DisableDeathLink` change the tag with a `ConnectUpdate`, `SendDeathLink`, `OnDeathLinkReceived`). The tag is set
  **after** login from the row, and again whenever the row changes, so it never depends on what `Connect` carried
  (`client-requirements.md`, the tag-before-`slot_data` trap). A `ConnectUpdate` waits on the socket, so it runs off
  the game thread, as every send does. Received deaths wait in a queue for the game thread.
- **Our own echo:** the library drops a received death equal to the last one it sent, by source and time to the
  second, never by name alone (the two-clients-on-one-slot trap).
- **Sending** (`DeathLinkGame.cs`): a prefix on the first step of `BattleControl.GameOver` with its setup (only a wipe
  starts that; the game's own re-shows of the menu skip it). The cause names the slot, as the protocol asks:
  "\<slot>'s party was defeated in Bug Fables."
- **Receiving:** in a battle, at the party's turn (no action running, no death check, no Game Over yet), the game's own
  `DeadParty` is started, which runs the game's Game Over; that Game Over is marked as the link's, so it sends nothing.
  On the map, once the player is free and a save exists, our Game Over, then `MainManager.ReloadSave()`.
- **One at a time:** from a strike until play is back (the Game Over's menu answered, or free on the map after the
  reload), deaths that arrive join it. A waiting death is dropped if the row is switched off, or on the title screen.
- **Logged at every decision** (`[death]`): received, waiting and for what, joined, struck and how, not sent and why.

**What a death costs** (asked by the user 2026-09-28, and again 2026-09-30: "not all progression is tied to the
seed?"). Retry, in a battle that can't be fled, loses nothing. Load, and every death on the map, goes back to the last
save (`MEASURED.md`, Game Over):

- **Kept:** items (the save's count is lower, so the server gives them again, build step 7); checks, which stay done
  and are never sent twice; checked one-time pickups, which stay hidden (build step 27); the open world's lists, applied
  on every map load.
- **Lost:** the save's own story state since that save: scenes play again, a boss beaten since comes back, and a way
  the story opened closes again.

*Auto-save between rooms* (the mod guide, step 31) is the panel's answer today. The seed's answer is decided (the
user, 2026-09-30): the open world's rule that no gate depends on a flag a reload can undo (build step 9), a world
that keeps one shape (Next 52), and bosses the server remembers (Next 53). No flag is ever restored from the server.

**Status:** built (2026-09-28), not yet seen in game or in a room.

*Code: `DeathLinkGame.cs`; the service in `ApConnection.cs` (`SetDeathLinkTag`, `SendDeath`, `TakeDeath`); the row on
the panel's first page in `ApMenu.cs` and `ApMenu.Rows.cs`.*

---

## Build step 26: the tutorial's Crunchy Leaf as a location (items the story adds)

**Why:** on a new file a Crunchy Leaf was already in the bag before any check (2026-09-28). Items are
remote only, so it becomes a check (chosen over keeping it or taking it back).

**Where it came from:** the opening (`Event16`) adds it straight to the bag for its tutorial battle
(`items[0].Add(0)`, not a `giveitem`, and never taken back; items are locked in that battle, so it isn't used). With
the Archipelago mod on, the opening is skipped and the mod's skip redoes its effects, the leaf included
(`QualityOfLife.Opening.cs`). So the mod added it, and no game code needed patching.

**A new kind of location:** `source.added`, an item the story puts straight into the bag (type and item, as a
`giveitem` has). The apworld reads it as the location's vanilla item (so the leaf joins the pool) and lists it in
`slot_data` as `location_added`; it is done by the opening's flag 15, with the other two opening checks, and it's
silent (no scene of its own shows an item). The location: *Outskirts: Outside the City, Tutorial Battle*, reachable
from the start. The mod's opening skip leaves the leaf out when the seed has it as a location
(`[qol] opening: the tutorial leaf is a location; left out of the bag`).

**Tests:** `test_tutorial_leaf_is_a_location` (the slot_data entry, its flag, silent, the leaf in the pool). Two tests
moved with it: the permit gate's reachable set gains the location, and a solo *Filler Only* seed with discoveries on
now has exactly enough filler (the leaf adds one), so its shops stay filler-only. 400 tests pass; 0 of 10000 fuzzed
seeds fail.

**Seen (2026-09-28):** on a new file no leaf in the bag; the three opening checks sent together, and
their items (a Lore Book, Mistake, Bee Fly in one seed) in the bag with the three starting members, with no boxes.

**Status:** works, seen on screen (2026-09-28).

*Code: `logic/outskirts.py` (id 75), `data_tables.vanilla_item`, `slot_data.py` (`location_added`, the silent rule);
`ApConnection.cs` (`LocationAdded`), `QualityOfLife.Opening.cs` (`RunOpening`, `SeedAdded`), wired in `Plugin.cs`.*

---

## Build step 27: checked pickups hidden in every save, new files too

**Why:** on a new file every pickup was back, those already found included: the game keeps "picked up" in the save,
and a new save starts empty. Picking one up again sent and gave nothing (2026-09-28: one Meditation after
two pickups), so it was clutter. Pseudoregalia's way was chosen: a pickup whose check is done isn't there.

**What:** when a map's entities are made, a one-time pickup (a floor item or a crystal berry) whose location the server
has as checked is kept away, the way the open world keeps blockers away (build step 9): its hiding list is replaced by
a marker the game's own existence check answers "gone" to. Nothing is written to the save. **Not hidden:** a
respawning pickup (once its check is done it is the game's own item again, the named exception, build step 10) and a
story pickup (it starts its scene). Shops already left out a copy whose check is done (build step 11).
Logged: `[open] <map>: <entity> kept away (location <id> is already checked)`.

**Seen (2026-09-28):** on a new file the Ladybug Siblings' house was empty (its pickup found in an earlier
file; `LadybugMistake kept away (location 7720014 ...)`), and Meditation and 15 Berries (Artis's gift) arrived as
replays with their boxes. A check sent from outside (Madeleine's left table, checked by a second client on the slot)
arrived with its box while the player stood in the house; the pickup stayed until the house was entered again, then
was gone, the right one still there: hiding applies when a map is made, not live. **Live, for a shared slot** ("good
for if people share 1 slot"): every 15 frames, while the player is free, a one-time pickup on the current map whose
check is done but which this save hasn't taken (its flag, or its crystal berry mark, unset) is hidden the way the game
hides an entity, `SetActive(false)` (`NPCControl.Start`). A pickup this save took is left to the game: its scene may
still be running on it. Logged: `[swap] location <id>: found by another client on this slot; its pickup on <map>
hidden`. **Seen (2026-09-28):** standing in Madeleine's house, the right table checked by a second
client: the pickup went at once, and Poison Resistance arrived with its box.

**A flash between rooms** (the same day: gone outside and inside, but visible while going in or out).
A house is an *inside* of its map: `MapControl.RefreshInsides` turns that inside's entities on without asking whether
they exist (`MapControl.cs:1262-1265`). A postfix now turns every entity the mod keeps away off again in the same frame
(found pickups and the open world's blockers alike), and a pickup hidden live gets the same marker. **Seen
(2026-09-28):** no flash going in and out of the houses.

**Status:** works, seen on screen (2026-09-28): a floor item, on the next entry into its room; a crystal berry seen
(2026-10-04: the pier's, taken, then gone on a new file of the same seed).

*Code: `KeptOpen.cs` (`AfterCreate`, the found pickups), `ItemSwap.Pickups.cs` (`IsPickup`, now shared).*

---

## Build step 28: the preflight, nothing unpublishable in the repo or a release

**Why (2026-09-29):** the Archipelago community's Developer Code of Conduct makes whoever publishes a project answer
for all of it: where the code came from, what it does, and that a release holds exactly what the repository holds.
Everyone who runs the mod or the apworld trusts it with their machine. So the repository now refuses, by itself,
anything that couldn't be published or could harm whoever runs it, and a reviewer can check each of those claims
without taking the author's word for it ([docs/reviewing.md](../docs/reviewing.md)).

**One gate: `dev-scripts/preflight.py`.** It reads what git holds, which is exactly what a commit contains, never the
working copy, and runs a list of sections that each pass or fail on their own. It needs only Python's standard library
and git, so anyone can run it. Two files hold everything it decides by:
- **`dev-scripts/preflight-patterns.json`:** what each section refuses (credential formats, home-path patterns, game
  file names), and the few things let through by name (the four release DLLs, the libraries' hashes).
- **`docs/capabilities.md`:** everything the code may do that reaches beyond its own files, each with its reason.
  It is exact: something the code does that isn't listed fails, and so does a row nothing uses any more.
  Adding a row is the maintainer's decision.

A section that finds nothing to check fails rather than passing: "0 files scanned" would otherwise look like
"0 problems".

**The first sections (2026-09-29):**
- **Hooks armed:** this clone runs the hooks, and the hooks are marked executable (on Linux and macOS, git silently
  skips a hook that isn't).
- **Index sanity:** plain files only (no links, submodules or conflicts), names that work on every system, no two
  paths differing only in case.
- **Known kinds only:** every file matches a kind in the patterns file ("a C# file of the mod", "a player doc"); a
  file of any other kind fails. `.gitattributes` may set only `text`, `eol` and `binary`, nothing that changes what git
  stores or shows.
- **Binaries:** the only binary files are the four release DLLs, each a .NET assembly, and the three libraries are
  byte for byte the files NuGet ships.
- **Hidden characters:** no invisible, bidirectional or control character anywhere. Those can make code read
  differently from what runs (the "Trojan Source" trick). In code, the only non-ASCII characters are `é`, `…` and `±`.
- **Encoded blobs and long lines:** no long base64 or hex run, and no code line over 1000 characters, where a payload
  could hide.
- **Secrets:** no token, key or webhook in any file, the mod's DLL included.
- **Personal paths and names:** no home-folder path, and not this machine's user or computer name, in any file, the
  DLL included. This replaced the old pre-commit check, which saw only text and skipped its own folder.
- **Game files:** no game assembly, asset, save or decompiled code.
- **Hosts and addresses:** every host named anywhere is in the capabilities list with a reason, and no public IP
  address appears (the four-part version numbers that look like one are listed by name).
- **Licences:** every project cited has its row in `licensing.md` (the user's own work, the owners in
  `own_github_owners` in the patterns file, needs none), and the release carries the licence and every shipped
  library's notice.
- **Commit messages** (`--history` only): no credential, home path or hidden character in any message.

**The apworld's rules (2026-09-29).** The apworld is Python that runs on whichever machine generates a seed, the
archipelago.gg website's included, and Archipelago imports it on every start, even when nobody plays Bug Fables.
So it gets the strictest rules, read from its syntax tree, not by searching text:
- **Apworld imports:** every name it takes from Archipelago or Python is listed in the patterns file (38 names
  from 14 modules today; 30 from 12 when it was switched on), and a plain `import` only for `json`, `logging`
  and `pkgutil`, each with the few functions it may use (`json.loads`, `pkgutil.get_data`). Listing names matters
  because a module hands on everything it imported: Archipelago's `BaseClasses` can pass along a helper that runs
  programs.
- **Apworld runs nothing unexpected:**
  - Only listed builtins, so no `open`, `eval`, `exec` or `__import__`.
  - No hidden attributes (`__class__`, `__globals__`), not even named in a string.
  - No attribute that writes files, runs programs or opens connections, on any object.
  - No world hook that is handed files or settings to write (`generate_output`, `settings`).
  - Annotations are only type expressions. Archipelago evaluates option annotations as code.
  - No bare call at a module's top level, and no `while`, `with` or `try` there; assignments, `for` and `if` may
    still build tables at import, as `data_tables.py` and `options.py` do.
  - A lookup by a computed name (`getattr(x, name)`) must be listed in `docs/capabilities.md` with where the name
    comes from. There are four.
- **Apworld data and docs:** the data files are strict JSON (no repeated key, no `NaN`) with no hidden attribute
  name inside, the manifest has exactly its four keys, and the player docs, which the website renders, hold no
  raw HTML or script link.

**The mod's and the scripts' rules (2026-09-29):**
- **Mod source:** the C# is read with its comments blanked but its strings kept, since a name in a string can
  still be reached. Twelve kinds of call are refused outright, among them starting programs, web requests, raw
  sockets, loading code, native calls, the registry, base64 payloads, opening web pages, reading who the player is,
  JSON that picks its own type, and code written at run time. None appears today. What the mod does touch beyond
  the game is listed file by file in `docs/capabilities.md`:
  - the one server connection;
  - the randomizer's own saves;
  - the clipboard, on paste and copy;
  - finding game types by name;
  - the dev build's tools (dumps, the console's command file, hot reload's status, the save diff), which never ship.

  Reflection on a type named in the code (Harmony's everyday tool) is not listed: the target is in plain sight.

  **Text from the server is read in one place only (2026-09-29):** a player, item, location or game name, or the
  seed's name, read anywhere but `Core/ServerText.cs` fails. So no new text path can skip its cleaning (the mod
  guide, step 33).
- **Dev scripts and hooks:**
  - Python is read from its syntax tree: no `eval`, no `exec`, no `shell=True`, no pickle, sockets or web modules.
  - PowerShell and shell are read with comments blanked: no running text as code, no encoded commands, no downloads,
    no compiling, no system settings.
  - What each script does (runs programs, writes files, talks to GitHub, connects to a local test server) is listed
    per script, so anyone about to run one can see what it will do. The git hooks too, since 2026-10-01 (below).

**The compiled DLL itself (2026-09-29).** The mod's DLL is the one file nobody can rebuild without the game, so
it is read directly, the way the .NET runtime reads it: `dev-scripts/dotnet_metadata.py` parses its PE headers,
its metadata tables and every method's IL, with Python's standard library only. It is strict:
- the tables must fill their stream to the byte (one zero, then padding to 4, as the .NET writer lays it out);
- an unknown opcode stops it;
- it parses all four shipped DLLs: 7,700 method bodies from four different compilers.

Four sections read it:
- **Release staging:** the download holds exactly its seven files. Each DLL is the one `built-from.txt` records,
  and the sources that file lists are exactly those of the commit it names. Sources changed since then only warn
  (a committed DLL is older than its sources between releases); with `--release`, they fail.
- **Shipped DLL structure:** a plain compiled library.
  - Sections `.text`, `.rsrc` and `.reloc`, with no data after them.
  - Only `mscoree.dll!_CorDllMain` imported.
  - The five standard metadata streams.
  - No native or P/Invoke methods, no embedded resources, and no stored data outside the compiler's array
    initialisers.
- **Shipped DLL reach:**
  - It references only the 13 expected assemblies.
  - None of 42 denied kinds of call appears among its 12,762 references: processes, `System.Net`, loading code,
    `Marshal`, the registry, code written at run time, opening URLs, reading who the player is, JSON picking its
    own type.
  - No denied name or unlisted host in its strings, not even in attribute data.
  - Each file, clipboard, connection and by-name lookup is traced to the type that makes it, and must be listed
    for a source file declaring that type.
  - Every Harmony patch target outside the game is listed. The first run found three that were written down
    nowhere, all legitimate: MultiClient.Net's socket creation and one websocket-sharp method (compression), and
    Unity's `Animator.Play` (a guard). They now have their rows.
  - **A target given by name** (`[HarmonyPatch("Type, Assembly", "Method")]`, for a class `typeof` can't reach
    because it's internal) is read as a type and a method, never as a method name alone (2026-09-29).
  - **A patch newer than the committed DLL** (2026-09-29): between releases the DLL predates the source. A listed
    patch it lacks is only a warning if today's source makes it (a `[HarmonyPatch]` naming that method, in a file
    naming its type). Otherwise, or once the DLL is current (a release build, `--release`), a row with no patch
    fails. Three fixtures cover it: stale with no such source, current, and stale with the source.
- **The DLL says only what its source says:** read against the sources of the commit it was built from.
  - Every type, method, field and called member name, and every one of its 1,237 strings, must come from that
    source. The strings are matched to the literals, the pieces of interpolated strings, and the constants the
    compiler joins. Char literals aren't pieces, and the compiler joins a `const char` into the string beside it
    too, so a character joined to text is a one-character `const string` (`TextFit.Break`, since a release build
    refused `'s\x01` on 2026-10-04).
  - The names the compiler makes up (`<Run>b__3_0`, tuple fields, operators) are allowed only by exact name or
    when built from a source name.
  - None of `Dev/`'s types may be in it.

  This is the strongest check possible without the game: code in the DLL that the source doesn't have shows up
  as names and strings the source never wrote. What it can't show is how the IL wires the allowed pieces
  together; only a rebuild with the game (documentation step 32) or reading the decompiled DLL proves that.

Each has its fixtures: same-length byte patches to a copy of the DLL (a namespace renamed to `System.Net`, a
Harmony target moved outside the game, a URL written into a string, a type renamed, the native entry point flag,
data appended). **`build-release.ps1`** runs the three DLL sections on each fresh build before staging it, and
its `-Check` (the release's gate) is now preflight with `--release`.

**The workflows, the dependencies and the list itself (2026-09-29):**
- **Workflows:** every action pinned to a full commit hash, with its version in a comment. The four first-party
  actions were pinned by tag only until then; a tag can be moved, a commit can't.
  - The workflow-level permissions are empty, and a job asks only for what the patterns file lists (today: the
    release's publish job, to write the release and its attestations, and its verify job, to read it).
  - Only four triggers (push, pull request, a call from another workflow, a manual run), and only GitHub's own
    runners.
  - No secret but GitHub's own token, and no `${{ }}` inside a script, where text from outside would run as code.
  - No `continue-on-error` and no YAML anchors.
  - The release's publish job must wait for every gate.
  - What each workflow reaches is in the capability list, each action and repository by name (2026-10-01, below).
- **Dependencies pinned:**
  - Every package at one exact version, and the lock file agreeing with the project, a content hash for each.
    The SDK adds `NETStandard.Library` itself, so that one is listed by version in the patterns file.
  - Each NuGet feed mapped to its packages.
  - One SDK, rolling forward at most a patch.
  - Every MSBuild switch that keeps outside build files out.
- **Capabilities list:** every table in `docs/capabilities.md` is one a section checks, and every row has a
  reason. A table nothing enforced would read as if something did.

**Taken from MeshGhost's preflight (2026-09-30):** rules this project wrote down but nothing checked.
- **Line caps:** `CLAUDE.md` stays within its 150 lines (its RULE 0), counted the way `wc -l` counts. The cap and
  its reason are `line_caps` in the patterns file.
- **Durations:** no span of time where a date belongs (CLAUDE.md: cite dates, never durations), in any tracked file,
  in any commit message (`--history`), or in release notes and subjects (`--text-stdin`). The phrases are `durations`
  in the patterns file. A figure with its number ("2-3 years old") is a measurement, and passes. On its first run it
  found two lines in the mod guide, reworded with their dates, and one commit message quoting a phrase it had just
  removed, reworded before it was pushed.
- **Plain links** (in `.githooks/doc-coverage.py`, run by the pre-commit hook): every plain link in a tracked Markdown
  file leads to a tracked file or a folder holding one, as links into a heading already had to. All 213 led
  somewhere on its first run.

**The list covers the hooks and the workflows too (2026-10-01).** A fact check found the capability list, which
calls itself exactly what the code does, silent on two things. The four shell hooks were read only for denied calls,
so their `git` and Python runs had no rows. The Workflows section never compared the workflows with the list: six
pinned actions, three other repositories checked out, PyPI installs, the NuGet package's download, `gh release
download`, the release published. The user: "i want it to cover everything".
- **The hooks:** `sh_capabilities` in the patterns file (runs programs, talks to GitHub, writes files), read with
  comments blanked as the PowerShell scripts are, and a row for each of the four hooks. A redirect counts only where
  a word starts, so the `>` inside a message's text doesn't.
- **The workflows:** a sixth table, "CI workflows: what they reach", checked from the Workflows section. Uses
  actions, checks out other repositories, runs programs (any `run:` step), downloads packages and talks to GitHub
  (`workflow_capabilities`, read in the scripts), publishes to GitHub (a job with a write permission). Each action
  and each repository must be named in its row, in backticks; a name the workflow no longer uses fails too.
- **Three fixtures:** a hook doing all three things with no row; a workflow using an action its row doesn't name
  and calling `gh`; rows nothing matches (a workflow that doesn't exist, an action no longer used). With the name check
  blinded on purpose, the last two failed the test.
- **Drafted in a scratch clone**, so the guard asked only three times: the patterns file, the list, the commit.

**Checking a release against the repository (2026-09-29): `dev-scripts/verify-release.py`.** Given a release's
files and the tag, it checks:
- the mod zip holds exactly `release/mod` at the tag, byte for byte;
- the apworld holds exactly `apworld/bug_fables`, byte for byte, but for the licence, which CI copies in before
  building, and the two version fields Archipelago's builder adds to the manifest;
- the three libraries are the NuGet package's own files;
- the yaml, which Archipelago writes from the options, carries no credential, home path or hidden character;
- for a release made after the preflight, preflight's DLL sections pass on the zip's DLL.

It needs only git and Python, no network: download the files first. **Both earlier releases pass** (v0.1.0 and
v0.2.0, run 2026-09-29). A zip with one byte changed in a library, and an apworld with a file added, both fail.

**CI and the release (2026-09-29):**
- **Every push and pull request** (`.github/workflows/preflight.yml`), on Python 3.11 and 3.13: preflight on the
  commit, `--history` over everything ever pushed, and the gate's own test. A second job downloads the
  Archipelago.MultiClient.Net package from nuget.org and checks the three libraries against it.
- **The release** waits for that workflow too. Before uploading, it checks the files against the commit: the
  apworld is built in a job that also installs Archipelago's own dependencies from PyPI, so it is checked, not
  trusted.
- **Provenance:** it adds GitHub's signed provenance attestation to the apworld and the yaml, the two files CI
  builds; `gh attestation verify` checks them. Not to the mod zip, which would suggest CI built its DLL.
- **The notes:** the highlights only, then GitHub's Full Changelog link (the user, 2026-10-04, after v0.3.0's page
  carried all of `docs/capabilities.md` as a diff: "looks ugly/bloated"). Until then they carried that file's diff
  since the last release; a capability's history is now `git log -p` on it, or a `git diff` between two tags.
- **After publishing:** a last job downloads what was published and runs `verify-release.py` on it.
- **`release.ps1`** waits for both push workflows before dispatching.

**Where it runs so far:**
- **Every commit:** the pre-commit hook, quiet unless something fails: preflight, then `.githooks/doc-coverage.py`
  (the docs name every option, setting, `slot_data` key and source file; the indexes match their headings; every
  link, plain or into a heading, leads somewhere; the mod guide, "Keeping this guide honest").
- **Every push** (`.githooks/pre-push`): preflight on each pushed branch's tip, and `--history` on everything new in
  the push. A commit made past the other hooks is caught here, before it leaves the machine. When the push changes the
  gate itself, the gate's own test (below) runs too.
- **Every release:** the release guard runs its text rules on the release notes and on every commit subject the
  notes will publish. This replaced a separate pattern list.

**The gate is tested by making it fail** (`dev-scripts/negative-test-preflight.py`). A check that only ever runs on
a clean tree says "pass" whether it works or not. So for every section, and every mode it runs in (a commit, the
history, free text), the test plants a real violation and checks that the section reports FAIL and preflight exits
non-zero. It works in a throwaway clone outside the repo, with its link back to the repo removed. The clone holds what
the next commit contains (HEAD plus everything staged), or, from pre-push, exactly the commit being pushed:
1. **A clean baseline** in all three modes, so a failure afterwards is the plant's doing.
2. **One fixture per kind of violation** (76 on 2026-09-29, 81 on 2026-09-30, 84 on 2026-10-01, counted from the test
   itself): a bidi override in a doc, a homoglyph in code, every credential format at once (each must be named), a home
   path inside the DLL, a library changed by one byte, a symlink, a submodule, a stale host row, a secret committed and
   then removed, and more. The fake credentials and paths are assembled at run time, so the test file holds none itself.
3. **The hooks for real:** a normal commit carrying a credential is refused, and so is a gate change mixed with mod
   code. A commit made past the hooks is refused at push, and the test remote stays unchanged.
4. **Coverage is total:** a section without a fixture in a mode it runs in fails the test, and so does a credential
   format or denied kind of call in the patterns file with no sample.
5. **What must pass, passes:** a denied call that only sits in a comment must not trip the mod's section, and a
   tree put back to the DLL's own sources must not count as stale. The staleness fixtures start from that tree:
   between releases the real one is legitimately newer, which would hide what they plant.

It took about 25 s on 2026-09-29, 39 s on 2026-10-01 with 81 fixtures, and 46 s the same day with 84. **Tested the other
way round (2026-09-29):** with the Secrets section made blind on purpose, all four of its fixtures failed the test.
Writing the test also caught its own slips: a sample written out whole (preflight flagged the test file itself), a name
git on Windows refuses to hold, and a fixture that stopped reaching its section when a second table was added below it.
A plant that changes nothing now stops the test.

**The hooks around it:**
- **They find a Python that runs** (`.githooks/python.sh`). On this machine `python3` is the Microsoft Store's
  stand-in, which only prints an install hint, so each candidate is tried before use. A clone can name its own with
  `git config preflight.python <path>`.
- **`commit-msg` fails closed.** Its subject-length check used to pass silently when no Python answered.
- **A change to the gate is a commit of its own.** The preflight's files, the hooks, the workflows and the agent's
  guard (`.claude/`) can't be committed together with mod or apworld code, so every change to what is checked stands
  alone in the history.

**Switched on (2026-09-29):** the first run took 0.4 s over 162 files. It caught the hooks not being executable, the old
pattern list and hook spelling out home paths, and two slips in its own code: a real zero-width character where an
escape was meant, and a string that read as a URL. All were fixed before the first commit.

**The whole history, checked (2026-09-29):** `preflight.py --history` reads every file version and every commit
message ever pushed (964 commits, 2843 file versions, in 19 s). It found no credential, no home path, no hidden
character, no game file, and no binary other than the release DLLs. It first flagged two kinds of harmless thing:
- **Old data files written on one line.** JSON is only ever parsed as data, so the long-line rule now skips it; the
  encoded-blob rules still read it.
- **Three old versions of the path checker.** They spelled out the home-path patterns they refused. These three are
  exempt by their exact git hash (`history_reviewed` in the patterns file), each with its reason, so a different
  file can't hide behind the exemption.

**The pages for reviewers (2026-09-29):**
- **`docs/reviewing.md`** is for anyone checking the project before running it, the Archipelago Discord's
  Developer Advocates among them. It covers:
  - how AI is used;
  - the checks that need only git and Python;
  - what runs on whose machine;
  - where a server's data goes, with what is and isn't checked;
  - what's proven about the committed DLL, and what can't be;
  - what the gates can't prove.
- **`.github/SECURITY.md`** says how to report a problem.
- **The README** carries the AI notice and links to both.

Writing the server-data part meant tracing every server input through the mod. That turned up three gaps, each
confirmed in the code, listed under Known issues in "Where it stands" above.

**GitHub's own settings (2026-09-29, each on the user's yes, set with `gh api` and read back):**
- **Secret scanning and push protection:** already on. GitHub refuses a push carrying a known token format.
- **Private vulnerability reporting:** on, for `SECURITY.md`'s *Report a vulnerability* button.
- **Immutable releases:** a published release's files and tag can't be changed.
  - The release action already uploads to a draft before publishing, so releases work unchanged.
  - A pre-release, which it would publish first, now stays a draft with its files, to be published from the
    releases page.
- **Two rulesets, with no one allowed to bypass them:**
  - `main` can't be force-pushed or deleted, so history can't be rewritten out of sight.
  - `v*` tags can't be deleted or moved, so a release always points at the commit it was checked on.

**The coding agent's guard (2026-09-29, on the user's yes):** the hooks stop a commit, but an agent could still step
around them. Claude Code runs `.claude/hooks/agent-guard.py` before each shell command, file edit and page fetch its
agent makes here (`.claude/settings.json`).
- **It refuses** whatever gets past the git hooks: `--no-verify` (and its short forms), `git commit -n`, changing
  `core.hooksPath` (setting it to `.githooks` is allowed), git config set through the environment, and the plumbing that
  writes history by hand (`commit-tree`, `update-ref`). It reads each command word by word, so a commit message may
  name any of these; `bash -c`, `powershell -Command` and the like are read inside too.
- **It asks before any read of a GitHub project with no linked row in `agent_docs/licensing.md` as committed**:
  `gh api`, `gh repo` and `-R`, `curl` and its kin, `git clone`, `fetch`, `pull` and the like, and page fetches. The
  user's own work (the owners in `own_github_owners`) is exempt. First (2026-09-30, the user's call, after a term had
  been written from memory and a fork's history read with no row) it refused such a read, the licence file aside, and
  the agent then added the row itself. The user, 2026-10-01, after PopTracker's licence was read that way: a peek at
  the licence is a look at the project, so every read asks, the licence included; and a row counts only as committed,
  so a commit that adds a project asks too.
- **It asks the user first** before:
  - an edit to `docs/capabilities.md`, the patterns file, `.claude/` (the guard itself, and the local settings that
    could switch it off) or `.git/`, and a shell command naming the clone's `.git/config` or `.git/hooks`;
  - a commit that may carry one of those, which also covers a file a script wrote rather than an edit;
  - a `gh api` call that writes, since that changes GitHub without passing a hook.
- **Everyday work asks nothing** (the user, 2026-09-29: "I don't want to constantly have to confirm things"). The
  first version also asked before every commit touching any gate file and before every push. That was a prompt per
  commit of gate work, and one on each push the user had already asked for, so both went the same day.
- **It fails closed.** If it can't run (no Python, a crash, input it can't read), the settings' command exits 2 and
  Claude Code refuses the action.
- **It stays small.** It calls `py -3` or `python3` directly, about 0.3 s a call: the hooks' own finder would double
  that on every command.
- **The settings can't grow.** Claude Code runs a repo's hooks for anyone who opens it in Claude Code, so
  preflight's "Dev scripts and hooks" holds `.claude/settings.json` to `ask` and `deny` rules and the one listed
  command; the guard script is read like every dev script. A file `.claude/settings.local.json` is left out of git.
- **Proven:** the harness plants settings that do more (their own `env`, an allow list, another event, another
  command), and runs the guard on 59 cases (2026-10-01), among them a commit carrying `docs/capabilities.md` (asks),
  one carrying only `preflight.py` (doesn't), reads of an unlisted project, its licence included (ask), a commit
  adding a row (asks) and a read while that row is uncommitted (asks). Staging a guard missing its
  `--no-verify` check, and a command that exits 1 instead of 2, made both tests fail (2026-09-29).
- **Not a wall.** A determined script can still get round it. Pre-push, CI and the release checks are what catch that,
  and a weakened gate still shows up as a commit of its own.

**Status:** built (2026-09-29): the sections above, their test, `verify-release.py`, every place they run
(pre-commit, pre-push, CI on every push, the release), the reviewer pages, the GitHub settings and the agent's guard.
The CI half first ran on the push of 2026-09-29 (`0fc15ce`), all green: preflight on Python 3.11 and 3.13 (the tree,
all history, the harness's fixtures), the libraries byte for byte against NuGet's package, and `ci.yml`. The guard
went live in the session that made it; its licence check on GitHub reads was added 2026-09-30. Line caps and
Durations added 2026-09-30, the harness then at 81 fixtures. The capability list widened to the git hooks and the
workflows (2026-10-01), the harness at 84. Next: the TLS measurement (Known issues); the cache
fix waits for a look in game (the mod guide's step 34).

*Code: `dev-scripts/preflight.py`, `dev-scripts/preflight-patterns.json`, `dev-scripts/dotnet_metadata.py`;
`docs/capabilities.md`;
`dev-scripts/negative-test-preflight.py`; `.githooks/pre-commit`, `.githooks/doc-coverage.py`, `.githooks/pre-push`,
`.githooks/commit-msg`, `.githooks/python.sh`; `dev-scripts/verify-release.py`; `.github/workflows/preflight.yml`, and
the guard, publish and verify jobs
in `.github/workflows/release.yml`; `.claude/settings.json`, `.claude/hooks/agent-guard.py`.*

## Build step 29: the logic, third part: Python modules per area on the Rule Builder

**Why (2026-09-29):** the project follows Archipelago's own way in everything (How it works §8). Archipelago's Rule
Builder is "intended to be written first in Python", and APQuest keeps its regions, locations and rules in Python. Ours
were JSON (`data/locations.json`) in a format of our own, turned into rules by a function of our own.

**The gate first:** the preflight's list of names the apworld may take from Archipelago gains the Rule Builder's `Rule`,
so the world can register its own rules the way the Rule Builder's doc shows (a change to the gate is a commit of its
own).

**Decided with the user (2026-09-29):** one Python module per game area, not per room ("all of Snakemouth Den" in one;
the Outskirts' corridors and Outside Snakemouth in the Outskirts, as the location names already say). A room's logic
often reaches into its neighbours, and a tester checks an area in one sitting; one file per room would be hundreds of
files with nearly every exit crossing into another. None of it is in the mod: the mod only follows what the generator
decided (`slot_data`).

**Built (2026-09-29):**

1. **`logic/`**, one module per area: `outskirts.py`, `snakemouth_den.py`, `bugaria_city.py`, `metal_island.py`,
   `later_chapters.py`. Each lists its `REGIONS` (each with its exits and their rules), its `LOCATIONS`,
   `STORY_EVENTS` and `ARTIFACTS` (each with its own rule), and the entities the seed changes there (`KEPT_OPEN` and
   the rest). A spot lives in its region's module; only exits cross from one module into another. `logic/__init__.py`
   gathers them, `LOCATIONS` sorted by id so moving a spot between modules never changes a seed. The old JSON notes
   became comments. **Since 2026-09-30** (build step 12) no module lists `REGIONS`: every map is a region; a module
   lists its `DOOR_RULES` and `TRANSFERS`, and a spot sits in its map's region with its area's need as `reach`.
2. **The rules are the Rule Builder's:** `Has("Explorer Permit")` for an item or a story event, `&` for "and", `|` for
   "or". Bug Fables' own needs, the ones that depend on the options, are three rules registered the documented way
   (`custom_rules.py`, each resolving to Archipelago's own `HasAllCounts`): `CanUse("Horn Slash")` (the ability's item
   copies when it's an item, its member when members are items), `Member("Vi")` (only when members are items) and
   `MoveItem("Freeze")` (the item alone, for ground not measured yet). `rules.requires` and the `Needs` fields are gone.
   **When a need is "any N of these"** (none is yet): the Rule Builder's `AtLeast`, new in 0.6.8 (`rule builder.md`),
   not an `Or` of every combination; and an option filter on a set of values uses its new `in` operator.
3. **Menu, the origin, is made in code** (`regions.py`, as APQuest does), with its one exit to where a new game begins.
   Jump's blanket rule stays in `rules.py`: with Shuffle Jump, every spot not marked `no_jump` also needs `Has("Jump")`
   (since build step 42 written as `JUMP`, an `OptionFilter` on Shuffle Jump).

For example, the droplet rooms' need and a spot inside them (as it is today, every map a region):

```python
# Every Snakemouth room with water droplets, or reached only through one: Leif freezes the droplets.
UNDERGROUND = DEN & CanUse("Freeze")
...
Location("Snakemouth Den: Mushroom Pit, Mushroom by the Droplets", 9, "SnakemouthMushroomPit",
         Source(flag=724, pickup=Pickup(map="SnakemouthMushroomPit", type=0, item=144)),
         rule=CanUse("Freeze"), reach=UNDERGROUND),
```

**Proven the same:** before the change, every spot's rule, every spot's reachability and every exit's rule were
recorded on 300 random sets of items, under the defaults, the story's party, one member, moves shuffled, Jump
shuffled and both mixes; after it, all 1306 rows came out identical.

**Tests:** `test_areas.py` (today: every spot in a map and in its source's map; names unique; every door an entrance;
door gates and transfers naming real places; every region reachable with everything; ids in order; every name a rule
uses exists: item, story event, ability, member). `test_rules.py` (each custom rule under the option sets that change
it; `base & (A | B)` and `A | B` on real states; a way that needs nothing makes the "or" free). `TestClassifications`
now reads the items each rule uses through Archipelago's own `item_dependencies()`, in a seed where every member, move
and Jump is an item. With `CanUse` broken on purpose (never asking for the member), 7 tests fail. 445 tests, the Logic
Test check (90 of 90) and the fuzzer (0 of 10000, every room with APQuest) pass.

**Every game area its module (2026-10-04).** Asked whether to plan a file per area, the user chose to create them all
now: "better to make them all now, rather than moving things later". The areas are the game's own 25 (`MapControl
.areaid`, each map's area in the map dump; their names `MainManager.areanames`, logged by the new dev command `areas`):
`outskirts` 0, `bugaria_city` 1 (Ant Kingdom City), `snakemouth_den` 2, `lost_sands` 3, `golden_hills` 4,
`golden_path` 5, `golden_settlement` 6, `forsaken_lands` 7, `far_grasslands` 8, `wild_swamplands` 9, `defiant_root` 10,
`ancient_castle` 11, `bee_kingdom_hive` 12, `honey_factory` 13, `rubber_prison` 14, `giants_lair` 15, `metal_lake` 16,
`metal_island` 17, `termite_capitol` 18, `wasp_kingdom_hive` 19, `bandit_hideout` 20, `stream_mountain` 21,
`chomper_caves` 22, `fishing_village` 23, `upper_snakemouth` 24. The four old names stayed.

- **What goes where:** an area's module holds what's on its maps: a spot by its map, a way between maps by where it
  starts, a changed entity by its map. One exception: *Lost Sands: Entrance* sits on an Outskirts map at the desert's
  border (`BOLostSandsEntrance`, area 0) and stays with the area its name gives, where the tracker lists it.
- **`later_chapters.py` is gone.** Its spots, ways and docks went to their areas (the boat from the pier to the
  Outskirts' with them); its shared rules, `LATER_CHAPTERS` and `SUBMARINE_KEY`, joined `custom_rules.py` with
  `INNER_CITY` (which `bugaria_city.py` had defined, a cycle once that module used `LATER_CHAPTERS`).
- **`AREAS`'s order** (Universal Tracker's list order, build step 41): the three the story starts with, then by area
  number.
- **Proof** (`seed-snapshot.py`, before and after): every spoiler byte-identical; slot_data identical but for
  `present_with_item`'s order, a list the mod reads as a set. The tests pass (683).

**Status:** built (2026-09-29): the logic in `logic/`, its rules the Rule Builder's, proven identical to the JSON's;
since 2026-10-04 a module for each of the game's 25 areas, proven to change no seed.

*Code: `logic/`, `custom_rules.py`, `data_types.py`, `data_tables.py`, `regions.py`, `rules.py`; tests `test_areas.py`,
`test_rules.py`, `test_logic.py` (`TestClassifications`), `test_party.py`.*

## Build step 30: the entrance randomizer's Room Swap, whole rooms trade places (experimental)

Whole rooms trade places with rooms that have as many doors, so the map keeps the game's shape and only which room
sits where changes. A value of the yaml option *Entrance Randomizer (experimental)*, `room_swap`, sharing the door table
and `door_targets` with build step 12, but a step of its own (the user, 2026-09-29: "room swap deserves its own doc
section separated from entrance rando").

**What it is, and why it is a value of the entrance randomizer** (2026-09-29; the user asked what a shuffle like
Super Metroid's Map Rando is called, rooms with the same number of entrances swapping places):

- **A room swap is a coupled shuffle too:** each door still leads back where it came from. So *Room Swap* and
  *Coupled* on together would look like *Coupled* alone. One option, then, each value allowing everything the one
  before it does: *Off*, *Room Swap*, *Coupled*, *Decoupled* (build step 31). The user chose the name `room_swap`:
  "rooms" alone says less, and Hollow Knight's randomizer uses "room randomizer" for every transition shuffled.
- **No decoupled room swap:** a room put where a room with more doors stood leaves the neighbours' extra doors leading
  nowhere, and pairing such loose doors is the coupled shuffle. *Coupled* and *Decoupled* keep their one meaning:
  whether turning round takes you back.
- Super Metroid's Map Rando lays out a new map on a grid. Bug Fables' maps are separate scenes on no grid, so the
  swap keeps the game's own map.

**The numbers first** (measured on `data/doors.json`, 2026-09-29):

- **What moves is an area** as `doors.py` counted it: a map with the maps its fixed doors join (a fixed door can't be
  rewritten, so it travels with its room; since 2026-09-30 only fixed doors both ways, below). 215 areas; by doors: 72
  with 1, 75 with 2, 38 with 3, 14 with 4, 7 with 5, 4 with 6, one each with 7, 11 and 23, two with 8.
- **The doors alone split the world into 10 parts** (180, 22, 17, 5, 4, 4, 3, 3, 2 and 2 maps), which boats,
  elevators and scenes join (the `Beehive` and `Factory` maps, `RubberPrison` with `GiantLair`, the `Termite` maps...).
  A room swapped into another part strands its own part, so **rooms swap only within their part**: 198 of the 215
  areas have a partner there; 17 stay put, mostly hubs (`SandCastleMainRoom`, 11 doors; the area of
  `GoldenPathTunnel2`, 23).

**How it was built:**

1. **`shuffle_rooms`** (`doors.py`, since `entrances.py`'s `room_pairs`): the areas and their doors as the coupled
   shuffle had them; the parts are the
   same grouping with every door added to the fixed links. Areas are grouped by part and door count and each group
   shuffled: each area's place goes to another, whose doors take the place's doors in a random order (which side of a
   room a door is on isn't in the table). Each of the game's pairs (d, n) becomes the pair of doors now standing at
   d and n.
2. **The same `door_targets`:** the pairs go through the coupled shuffle's own last step (`_targets` then,
   now `entrances.door_targets`), so the mod needed no change, and the Warp is forced on as with any `door_targets`
   (build step 12).
3. **Nothing stranded, for free:** the map keeps the game's shape, so the coupled shuffle's grow-outwards pass isn't
   needed. (Wrong twice, found on 2026-09-30: below.)
4. **Ours, not Archipelago's:** its entrance randomizer (`randomize_entrances`) pairs single entrances by group and
   can't move a room's doors together (its `can_connect_to` hook refuses one pairing at a time and it never goes back),
   so this is custom work where Archipelago has none (How it works §8).

**On the region graph, and two things that were wrong (2026-09-30).** With every map a region (build step 12), the
swap moved onto the same entrances as Archipelago's randomizer, and the first test that could check every region
from the start found two holes:

1. **`_swap_rooms` in `entrances.py`** connects the split door entrances by hand for the pairs `room_pairs` draws
   (`_connect`), the dangling exit to the target named after the other door (the pattern of The
   Messenger's `connect_plando`), then the same `door_targets` and spoiler as the coupled shuffle. The logic follows the
   swap.
2. **A one-way fixed door joins nothing.** 19 of the 39 fixed doors go one way (drops in the Barren Lands, the Golden
   Settlement's night maps, the wizard tower, the wasp kingdom). An area joined by one could be entered on its far
   side with no way back (seed 6: five maps cut off). Areas and parts now join only fixed doors that go both ways:
   225 areas (75 with 1 door, 79 with 2, 41 with 3, 15 with 4, 8 with 5, 3 with 6, one each with 7 and 11, two with
   8), still 10 parts, 209 with a partner.
3. **A gated door moves with its room.** The Golden Path door needs the first boss; seed 4 put the Outskirts where that
   door was the only way on, with the boss behind it. Archipelago's randomizer follows the logic while it places; the
   swap can't, so each try is checked the way the randomizer checks (everything the seed holds, every region reached)
   and undone if it fails, up to 20 tries. Before the check 13 of 200 tries cut regions off; after it, 0 of 200.
   **A repair instead of retries (2026-10-08):** with the room mapping's one-way drops and gated parts, only about 4
   in 1000 random swaps kept every room reachable (seed 5: 12 of 3000; the parts most often cut off, the Golden Hills
   dungeon's upper ones, the desert's ledges, the border cave's gated sides), so 20 tries nearly always failed, and
   2000 still failed 2 fuzzer seeds in 10000. Now one random layout is repaired: two rooms of the same shape trade
   places, or one room's doors turn, four moves in five around what is cut off (a room with a door in a map that
   is), each kept unless it cuts off more. On 180 seeds every one was repaired, in about 190 tries (95% within 650,
   the worst 1870, about 1.5 ms each); it gives up after 10000. A random layout with no repair was tried first and
   did no better than retries (13 of 15). Test `TestRoomSwapRepairs`: the two fuzzer seeds, which the old retries
   failed. **And a start to fill from:** once every layout passed, 4 fuzzer seeds in 10000 hit a FillError: their
   swap left 3 to 7 spots open from the start (with the start's own items) where the game's layout opens 15 to 50.
   The repair now also keeps at least as many as the game's own layout opens, up to 15 (a start with 15 filled);
   the four then generated. Test `TestRoomSwapLeavesAStart` (two of them; 7 and 3 before). **Then the edge first:**
   one tracker seed in 10000 was killed at the fuzzer's 15 seconds, the repair taking 6734 tries: 61 regions cut off
   behind one wrong door, and its moves picked among the cut-off rooms, almost never the one that walls them off. A
   move now takes, four times in five, a room on the edge of what is cut off (a door pair with one side reached and
   the other not), else one standing where something is cut off: that seed in 1037 tries; 240 seeds over four setups
   in about 250 (95% within 1460, the worst 2470, 2.9 seconds). Test `TestRoomSwapRepairsQuickly` (that seed within
   3000 tries; the earlier moves needed 6734). **And a fresh start when stuck:** the next run had 2 seeds in 20000
   time out, one of them a random start where nothing was cut off by try 51 but the start never opened more than 7
   spots (15 wanted), and no one move helped in the other 9950. With nothing cut off, moves now take the edge of what
   the start opens (a door pair with one side opened from the start), and a layout that hasn't improved in 1000 tries
   starts over from a new random one: those two seeds in 2404 and 2030 tries; 300 seeds over five setups (a random
   start among them) in about 250, the worst 2322, 2.9 seconds. Test `TestRoomSwapRepairsQuickly`'s stuck start
   (within 3000; the moves without a fresh start gave up at 10000). **The start's target measured:** one more seed
   timed out climbing slowly to 15 start spots (5395 tries). Measured with no target, 474 swapped seeds opening 7 to
   14 spots all filled; two of the four FillErrors had Filler Starting Checks keeping the opening's spots to filler.
   So the count is now of spots that can take progression (not excluded), and the target 10: that seed in 2938
   tries; 300 seeds over five setups in about 200, the worst 1717, 1.4 seconds. The tries also got cheaper: the
   layout stays connected between tries and a move rewires only the doors whose target changed (a third of a try's
   time had gone into connecting and undoing all some 270 pairs), and only the start's reached regions are checked
   for spots. **The start's own rooms (2026-10-08):** CI's Universal Tracker run killed 1 seed in 2000 at 15 seconds;
   here it took 4532 tries, 3.8 seconds: three layouts, each stuck for 1000 tries, cut nothing off but opened only 7
   to 9 spots from the start. With the field moves and Jump shuffled, the start's way on is gated inside the rooms it
   reaches, with no door on the edge of what it opens, so the moves fell back to rooms around what was cut off, which
   can't open more. While the start opens fewer spots than wanted, a move now takes one of the rooms it reaches:
   trading it for a like room, or turning its doors, changes what the start opens. Tried on the same 3000 seeds (three
   setups with the moves shuffled) against the start first in the score (the worst seeds still about 2200), the two
   summed, and moves on the edge of what the start opens: the start's rooms with the score unchanged did best, about
   235 tries on average against about 400, 99% within 950 against 1650 to 1910, the worst 1380 against 2591; the
   default options unchanged (their start is never short). The restart limit, measured with both: with the earlier
   moves, 1500 seeds over three setups for each limit from 50 to 1000, 150 and 200 had the shortest tail (the worst
   2938 tries at 200, 5882 at 1000; 50 and 100 restarted layouts still converging, at 50 the median up from about 280
   to about 380); with the start's rooms, 200 again (the worst 1930 at 120, 2533 at 300). Now 200. That seed in 301
   tries, 0.4 seconds. Tests `TestRoomSwapRepairsQuickly`: two seeds with the moves shuffled within 1000 (the earlier
   moves took 2938 and 2196), and the stuck starts, that seed among them, within 1500. Each try also got about a tenth
   cheaper (about 0.8 ms): everything the seed holds is collected once and each try sweeps a copy, and each door's
   region is looked up once. **And the start counted once:** a new `CollectionState` already holds the start's items,
   and the count collected them again: a Progressive Dash in the start counted as Horn Dash (41 spots where the start
   opens 40). Test `test_the_start_holds_its_items_once`; the start test's own count had the same slip.

**Tests** (`test_doors.py`): what every mode shares (doors rewritten, only the table's doors named, every way back
leads back, every region reached, the spoiler listing each pair once, and **the mod doing what the logic proved**:
each door as `door_targets` rewrites it arrives where its entrance leads in the region graph); the parts stay whole;
**the map keeps its shape**: each area's part, its door count and the door counts of the areas its doors lead into
are the game's (the coupled shuffle breaks that in 50 of 50 seeds); on small made-up tables over 100 seeds, two parts
never trade rooms (without the part rule, broken in 93) and an area joined both ways moves whole (ignoring fixed
links, broken in 85). On the real table about 490 of the 508 doors are rewritten. Seeds generated alone and with
APQuest (the spoiler: *Room Swap*).

**The one-way doors stay as they are** (build step 38, 2026-10-02): rooms move whole, and a fog maze's wrong turn
still leads into the room it did. Only a one-way's story copy is rewritten, to follow its door. Test
`test_one_ways_stay_as_they_are`.

**Later:** doors matched by side (an exit on the right leads into a door on the left), once each door's side is read
from the entity dump.

**Status:** built, not yet seen in game (2026-09-29); on the region graph, the logic following it (2026-09-30);
experimental like build step 12: the rooms' own rules aren't mapped yet.

*Code: `entrances.py` (`room_pairs`, `_swap_rooms`, `door_targets`), `options.py`, `world.py`; tests `test_doors.py`.*

## Build step 31: the entrance randomizer's Decoupled, each door one way (experimental)

A fourth value of *Entrance Randomizer (experimental)*, `decoupled`: every door may lead to any other and its way back
is shuffled too, so turning round can take you somewhere else. Planned since build step 12 (2026-09-25: "coupled by
default, decoupled as a choice"); the user, 2026-09-29: added now, as its own step.

**How it was built (2026-09-30):**

1. **Archipelago's randomizer, uncoupled:** the same split door entrances as Coupled (build step 12), and
   `randomize_entrances(world, coupled=False, ...)`: its doc's "uncoupled randomization", nothing of ours. Every door
   stays two-way typed, so a door is only ever paired with a door.
2. **The same `door_targets`:** each door is rewritten on its own (x leads where y's partner leads), which is all the
   mod ever did, so the mod needed no change; a door of the pair no longer names the other.
3. **The spoiler** lists every door on its own (`=>`, 508 lines) instead of each pair once (`<=>`).

**Tests** (`test_doors.py`): what every shuffle shares (doors rewritten, only table doors named, every region reached,
the mod doing what the logic proved); the way back no longer always leads back; the spoiler lists every door.

**Status:** built, not yet seen in game (2026-09-30); experimental like build step 12.

*Code: `entrances.py` (`shuffle`, `write_spoiler`), `options.py`; tests `test_doors.py`.*

## Build step 32: the entrance randomizer's connection plando, doors pinned in the yaml

A player's yaml can pin doors: "this door leads there". Archipelago's own plando option for it, `plando_connections`
(`Options.PlandoConnections`, the plando guide's "Connection Plando"). Optional in the guide ("Support for connection
plando may vary"), built anyway (2026-09-30, the user: "we should try to support all available things archipelago
has/does, that includes plando"; How it works §8).

**How it was built (2026-09-30):**

1. **The option is Archipelago's class** with our names: `DoorPlando(PlandoConnections)`, whose `entrances` and
   `exits` are every shuffled door's entrance name, `"<map>: <door>"`, as the spoiler lists them (`DOOR_NAMES` in
   `data_tables.py`). Archipelago checks the names (any case), refuses a door used twice, and drops the whole option
   when the host hasn't turned plando's "connections" on.
2. **Placed first, the rest by the randomizer:** after the doors are split, each plando connection is joined by hand
   (The Messenger's pattern), then `randomize_entrances` places what's left. entrance: the entrance door leads next to
   the exit door; exit: the exit door leads back to the entrance door; both: both. Coupled always joins both ways (as
   The Messenger does), Decoupled only the ways asked for. A door used by two connections is refused with the player's
   name.
3. **Only with Coupled or Decoupled:** with the doors off, or on Room Swap (whose rooms move whole), the connections
   are ignored with a warning in the generation log.
4. **Planned doors are pairings like any other,** so `door_targets` and the spoiler carry them: the mod needed no
   change.
5. **The preflight's import list** gained `PlandoConnections` from `Options`, in a commit of its own.

**Checked:** seeds generated through Archipelago's own Generate with a plando connection, alone and with APQuest, under
Coupled, Decoupled and Room Swap: the planned door leads where the yaml says (`<=>` coupled, `=>` decoupled in the
spoiler), and Room Swap ignores it. Tests (`test_doors.py`): Coupled joins each planned door both ways, Decoupled
only the way asked for, names match in any case, an unknown door is refused, Room Swap ignores plando, and every
shuffle test holds with plando on.

**Status:** built, not yet seen in game (2026-09-30).

*Code: `options.py` (`DoorPlando`), `entrances.py` (`_plando`), `data_tables.py` (`DOOR_NAMES`); tests `test_doors.py`.*

## Build step 33: Music Shuffle, songs and jingles swapped per seed

A yaml option, off by default: every song plays in place of another, the same way every time the seed is played. No
item, check or rule depends on it.

**Decided (the user, 2026-09-30):**
- **In the yaml, not the panel.** Every Archipelago world with a music shuffle has it as a yaml option (about 23 at
  0.6.7). APQuest keeps its own cosmetics there too, in an "Aesthetic Options" group and in slot_data. The PC and mod
  worlds roll the tracks at generation and send them in slot_data: celeste_open_world `music_map`, sa2b `MusicMap`,
  saving_princess `music_table`.
  - A shuffle is a random result, so it comes from the seed: the same tracks every session, on any computer, for
    anyone playing the slot.
  - A panel row would need the mod to roll its own.
- **On/off**, named `music_shuffle`, the most common name (7 worlds).
- **The jingles go with it**: the victory fanfare, the game over and the chapter titles swap among themselves.
- **The world's first option group**, "Aesthetic Options", APQuest's name. The other options show under "Game Options"
  until Next 43, item 8.
- **Sound effects** get their own option and step, after this one is seen working.

**How it was built (2026-09-30):**

1. **The pool** (`music.py`): the game's 75 track names less seven that stay put (`MEASURED.md`, "Music and jingles"):
   - the title, which plays before the client connects (saving_princess leaves its title out for the same reason);
   - the four ambience beds Samira leaves out of her list;
   - the factory elevator's pair, which the game crossfades on a sound slot, outside the music player (in the pool
     since build step 43, the crossfade made a plain fade when shuffled).

   **And three names with no clip (2026-10-04):** `Beetle`, `Giant2` and `Giant3` are in the game's list but load
   nothing. Found in play: the first seed seen with the shuffle gave the pier `Beetle`, and the pier played its own
   song (the user: "the pier didn't randomize its music"; the mod's guard logged `[music] Beetle not found`). Measured
   for every name with the new dev command `musiccheck` (`development.md`): only those three. They left the pool
   (`NO_CLIP`), so every pool track now has a clip to play. The pool is 65 tracks.

   The jingles are 11 sounds the game plays at music volume.
2. **The roll:** in `generate_basic`, which `world api.md` gives "player-specific randomization that does not affect
   logic".
   - Each list becomes a permutation with the world's random, so every track still plays somewhere.
   - It runs after every roll the logic depends on, so turning it on changes nothing else in the seed. Since build
     step 34 it draws from a stream of its own, taken from the world's random whether it's on or not, so it and the
     shop inventories never change each other's roll.
   - slot_data `music_map` and `jingle_map`, `{name: name played in its place}`, are empty when the option is off.
3. **The mod** (`MusicShuffle.cs`), after reading how the game plays music.
   - **Why not swap the clip:** the game saves and replays the playing track by name, checks it by name (the victory
     fanfare follows only the regular battle themes) and Samira counts each track as it plays. Swapping the clip
     passed to `ChangeMusic` would break all three: a replayed track would be swapped twice, and Samira would count
     what played instead of what the game meant.
   - **What it does instead:** the game's player keeps its own track, muted. A second AudioSource plays the seed's
     track and follows the player's volume, fades, pitch and pauses every frame. The game does the same for a music
     zone: a track on its own source while the main player fades out.
   - **When it runs:** in LateUpdate, after the game's coroutines, so a track the game starts is muted before it's
     heard.
   - **Loop points** come from the game's own table, for the track that plays.
   - **After a battle** the game can resume the map track where it was; the voice resumes its own.
4. **Samira:** her list counts the game's track, never the played one, so the shuffle can't change her all-songs
   reward (her key item, a future location). While she plays a song, the mod steps aside: the song picked is the song
   heard.
5. **Jingles:** a prefix on the game's `PlaySound` and `StopSound` funnels swaps the clip. The stop swaps the same way,
   so the game's own stop by name (`Gameover`) stops the jingle it started.
6. **The guards:**
   - It acts only with Archipelago enabled and a seed whose map isn't empty.
   - A name the game doesn't have plays the game's own track, logged once.
   - Every switch is logged (`[music] Field0 plays as Battle4`).
   - Unloading on quit leaves the second AudioSource alone, since Unity has already destroyed it (it threw when the
     user closed the window, 2026-10-04).
7. **The preflight's import list** gained `OptionGroup` from `Options`, in a commit of its own.

**Tests** (`test_music.py`):
- off swaps nothing;
- on, every pool track plays exactly once, the kept tracks never move, and the jingles swap among themselves;
- the three names with no clip never play in a track's place (it failed with them back in the pool, 2026-10-04);
- the same seed with the option on or off gives the same slot_data otherwise, the same item pool and the same fill
  randomness. It failed with the roll moved to the start of `generate_early`.

**Checked** (2026-09-30), both through Archipelago's own Generate:
- The same seed with APQuest, the option off and on: slot_data is identical but for the two maps (68 tracks and 11
  jingles when on), and the spoilers differ only in the option's own line.
- `seed-snapshot.py` on CI's three presets, alone and with APQuest, before and after the change: only the two empty
  maps and the option's spoiler line are new.

**Status:** built (2026-09-30); area music and the victory fanfare heard swapped (2026-10-04); three names with no clip
taken out of the pool (2026-10-04), not yet seen in a new seed; game over, a chapter title, the title music and Samira
not yet seen.

*Code: `music.py`, `options.py` (`MusicShuffle`, `option_groups`), `web_world.py`, `world.py` (`generate_basic`),
`slot_data.py`; the mod's `MusicShuffle.cs`; tests `test_music.py`.*

## Build step 34: Shuffle Shop Inventories, what shops restock and pickups respawn with

A yaml option, **on by default**. It shuffles what item shops restock and what respawning floor items come back with,
among themselves. The first purchase of each item in an item shop and the first pickup of a respawning item are
still checks (build steps 10 and 11). This changes only what a spot sells or gives once it isn't a check, or from the
start in a shop that isn't a location. It never touches a check or a location.

**Decided (the user, 2026-09-30):**
- **The pool is those spots' own items:** food and other consumables, never a medal, key item or anything else. A shop
  may sell what another shop or a floor item had, and a floor item may be what a shop sold.
- **Like Music Shuffle: no logic tied to it.** Only checks and locations carry logic. If a later check (a recipe, a
  delivery) needs a certain consumable, that check carries its own rule and source; a restocked or respawned item is
  never one.
- **Named *Shuffle Shop Inventories*** (`shuffle_shop_inventories`), ALttP's option for shuffling its shops' default
  stock among them. That is the only precedent in the worlds at `0.6.7`, where no world names floor items that come
  back (the search: `licensing.md`). The help text says the respawning items join it and that no check or location is
  touched.
- **On by default:** it touches no logic, and it randomizes more.

**Approved with the plan (the user, 2026-09-30; the agent's proposals):**
- **A permutation**, as Music Shuffle does: each spot takes another spot's item, so every item is still sold or found
  as often as before.
- **No shop sells one item twice:** the roll is taken again until none does, up to 100 tries, with a warning if one
  ever gets through. About half of all shuffles pass on the first try.
- **The spots are every item shop slot and respawning pickup in the locations' data, whatever the yaml leaves out.**
  With *Shuffle Item Shops* off, the shops sell the seed's stock from the start. A new item shop or respawning pickup
  joins when it's added as a location.
- It stays inside CLAUDE.md's one named exception to "items are remote only": a spot whose check is done is the
  game's own again, and the game gives its item. Only which consumable it is comes from the seed.

**How it was built (2026-09-30):**

1. **The spots** (`shop_inventories.py`, `SPOTS`): every location whose source is an item shop slot (map, keeper, item)
   or a respawning pickup of an ordinary item (map, regional flag, item). Today: Madame Butterfly's 5, the caravan's 3,
   and Snakemouth's 3.
2. **The roll:** in `generate_basic`, like Music Shuffle's. Each roll there now has its own stream, taken from the
   world's random whether its option is on or not. Otherwise, turning Music Shuffle on moved the shop roll (the music
   test caught it).
3. **slot_data `shop_inventories`:** `[{"map", "keeper" or "regional", "item", "to"}]`, one entry per spot, empty when
   it's off. The client finds a shop slot by map, keeper and the item the game stocks there, and a pickup by map and
   regional flag, as it finds their locations.

**Tests** (`test_shop_inventories.py`):
- off swaps nothing;
- on, every spot appears once, the items only trade places and some move;
- no shop sells one twice, checked on the seed and on 500 rolls;
- with *Shuffle Item Shops* off the shops are still shuffled;
- the same seed with it on or off gives the same slot_data otherwise (music maps included), the same item pool and
  the same fill randomness.

**Checked** (2026-09-30):
- `test-apworld.ps1`: 538 tests pass, the Logic Test check reproduced 90 of 90, and the fuzzer failed 0 of 10000.
- `seed-snapshot.py` on CI's three presets, alone and with APQuest, before and after: slot_data is identical but for
  the new `shop_inventories` (11 spots in each, the preset with *Shuffle Item Shops* off included), and the spoilers
  differ only in the option's own line, so no item moved.

**The mod** (2026-09-30; the mod guide, step 35) swaps the item the way the game itself turns one item entity into
another, so the price, name, sprite and what's added stay the game's. **A tossed pickup is left alone**
(2026-10-06): taking an item with a full bag and throwing a bag item out puts the thrown item into the same floor
entity (the game's toss, `MEASURED.md`), which the mod first took for a respawning pickup holding the wrong item and
warned about every 15 frames. Only a consumable is ever thrown out: a check's pickup gives nothing there, and a key
item or medal the server sends never needs room (the user: those must always be picked up, full bag or not).

**Status:** built (2026-09-30), the tests pass and the mod builds; seen in game (2026-10-04) at Madame Butterfly's:
each bought check's slot restocked as another consumable, one bought and received as shown. A respawning pickup's
new item seen (2026-10-06): `SnakemouthUndergroundRightB`'s Crunchy Leaf came back as a Honey Drop, taken and received.

*Code: `shop_inventories.py`, `options.py` (`ShuffleShopInventories`), `world.py` (`generate_basic`), `slot_data.py`;
the mod's `ShopInventories.cs` and `ItemShops.cs`; tests `test_shop_inventories.py`.*

## Build step 35: Filler Starting Checks, the opening's automatic checks hold filler

A yaml option, **on by default**. The checks a new file sends by itself when the game begins hold filler only: no
progression, useful or trap item. **The user's ask (2026-09-30):** "it would be a bit boring to get multiple
progression/useful items before even starting to play the game, right when connecting/new save". Then: filler, "not
traps"; "specifically the items you get when you connect/new save"; and "not for example Leif's spider location", so
no other spot turns filler-only by accident.

**Which checks:** the opening skip sets flag 15 and the mod sends every flag-15 location at once. Those are exactly the
locations marked `quiet` (their items arrive with no hold-up; the mod guide, step 10, item 6): *Maki and Eetl's
Gift*, *Outside the City, Tutorial Battle*, and *Outside the City, Opening* when members are items. The option selects
them by `quiet` alone, so *Fall Room, After the Spider* (a member's spot, not quiet) never changes. The members a new
file starts with are start inventory, not locations: *Starting Party Member* decides them.

**Archipelago's way (read at 0.6.7):**
- A world may mark its own spot `LocationProgressType.EXCLUDED`. It then takes only items that are neither progression
  nor useful (`BaseClasses.py`, `Location.can_fill`), filled from the filler pool before anything else (`Fill.py`,
  "Remaining Excluded").
- `Main.py` expects worlds to do this: a player's `priority_locations` entry on such a spot is dropped with a warning.
  Pokémon Emerald excludes its own spots the same way (`licensing.md`).
- Excluded still admits traps, so an item rule refuses them, added with `worlds.generic.Rules.add_item_rule` so no other
  rule is lost. A trap drawn for one of these spots stays in the pool and lands elsewhere.
- **Plando:** a block putting a progression, useful or trap item on one of these spots fails, as on any excluded spot.
  The yaml text says to turn the option off for that.

**The name:** no world at 0.6.7 has such an option (the search: `licensing.md`), so the user chose between names of our
own: *Filler Starting Checks*, not "Starting Items", which is Archipelago's spoiler heading for start inventory.

**How it was built (2026-09-30):**
1. `options.py`: `FillerStartingChecks` (`DefaultOnToggle`), after *Starting Party Member*.
2. `rules.py` `set_all_rules`: with it on, each included `quiet` location gets `EXCLUDED` and the no-trap rule.
3. **The shops' *Filler Only* fallback is unchanged.** It counts every unfilled excluded spot, so the opening's spots
   take their filler first, and a solo room short of filler turns the shops back to No Progression, as before. The
   solo seed with discoveries on that had exactly enough filler for its shops (22 for 22) now falls back.
4. No `slot_data` key and no mod change: the mod sends these checks as before.
5. **Off with Coupled or Room Swap doors** (the user's choice, 2026-09-30). The first fuzzer run failed 4 of 10000 seeds
   (`FillError`), each with the Entrance Randomizer on Coupled or Room Swap, one starting member and moves and Jump
   shuffled. Measured per option set (fill only, 250-400 seeds each; failures with the option off, then on):
   - no door shuffle, every start, category, moves and Jump mix: 0 and 0;
   - Coupled, one member: 0 and 1-3%;
   - Room Swap, one member: 0 and 4%;
   - Room Swap, All Three, moves and Jump shuffled: 0 and 2 of 250;
   - Decoupled, one member: 0 and 0.

   In a failing Room Swap seed, the start with Jump reaches only the Outskirts' own seven spots, three of them the
   opening's; shops take no progression by default, so a start can look large and still have few such spots.
   - **Tried and dropped:** Archipelago's own answer to a restrictive start (`apworld_dev_faq.md`), Jump as a local
     early item. It left all four seeds failing.
   - **Also not separating failures from successes:** counting the start's open spots.
   - **Offered:** the option stands down only when the start is tight (a threshold to tune), or an option error. The
     user chose the simple rule. With Coupled or Room Swap the option doesn't apply to the seed, and the generator
     says so (`generate_early`, `world.filler_starting_checks`); Decoupled keeps it.

**Tests** (`test_starting_checks.py`, and `test_shops.py`):
- by default the three are excluded, a trap is refused, and after a fill each holds plain filler;
- no other spot is excluded;
- with a Vi start the opening spot is filler-only and the spider spot still takes a member;
- with the story's party two spots;
- with it off all three take anything;
- with Coupled or Room Swap it stands down, with Decoupled it holds;
- the smallest pool (every optional category off) still holds plain filler for each spot;
- the shops fall back after the opening's spots, and the "exactly enough" test pins the option off.

With the rule switched off, 11 of these checks fail.

**Checked** (2026-09-30):
- `test-apworld.ps1`: 581 tests pass, the Logic Test check reproduced 90 of 90, and the fuzzer failed 0 of 10000,
  alone and with APQuest in every room.
- The stand-down rule re-measured without any early item: no door shuffle and Decoupled, every start, category, moves
  and Jump mix, 0 failures either way.
- A default seed generated through `Generate.py` with APQuest: the spoiler shows a Drowsy Cake, 15 Berries and the
  Crunchy Leaf on the three spots, all filler.

**Status:** built (2026-09-30), the tests pass; seen in game (2026-10-04): a new file's three opening checks went out at
the start holding filler, as the spoiler placed them.

*Code: `options.py` (`FillerStartingChecks`), `world.py` (`generate_early`), `rules.py` (`set_all_rules`),
`data_types.py` (`quiet`); tests `test_starting_checks.py`, `test_shops.py`.*

## Build step 36: Progressive Boat, the Boat Ticket and the submarine as items

The submarine as an item: Bug Fables' own Surf, one item that opens much of the world (the user, 2026-09-30, after
Pokémon Emerald's Surf). Always in the pool, as the Boat Ticket was; a yaml option, *Progressive Boat*, on by default,
decides whether the two come as one progressive item or apart.

**Decided with the user (2026-09-30):**
- **One progressive item, *Progressive Boat*, two copies in every seed.** The first gives the Boat Ticket (build step
  16), the second the **Subaquatic Maritime Neotransport**, each its own key item in the bag, as *Progressive Dash*
  gives the Dash, then the Horn Dash (build step 23): "i want it to be progressive so it always sit behind the boat
  ticket, but also be its own unique key item in your inventory". So Metal Island's port needs no rule of its own:
  whoever has the sub has the ticket.
- **The name is the game's own**, the Termite King's in the throne room ("We call it the Subaquatic Maritime
  Neotransport!"). The queen calls it "Submarine for short".
- **Considered and dropped:** a separate item, with the ticket required at Metal Island's port always or behind a
  roadblock toggle as Emerald's *Extra Boulders* and *Modify Route 118* (each "aims to take some power away from Surf",
  `worlds/pokemon_emerald/options.py`, 0.6.7). The progressive order makes both unneeded.
- **The description, short:** "It is impossible for it to sink! ...Probably." The first sentence is the king's line,
  the second the team's doubt. Not "travels under the water": the sub sails on the water and dives only to dodge
  danger (the user).
- **Hints:** "submarine" finds it.
- **The docks appear and work only with its key item.**
- **The yaml option *Progressive Boat*, on by default** (asked the same day, "for people who want both split up"):
  off, the Boat Ticket and the submarine are two items in any order; "the boat ticket would be its own item that gets
  you to metal island, and the submarine would be its own item that can also get you there and to other locations".

**What the game does** (code read and the dumps, 2026-09-30; `MEASURED.md`, "The submarine"): no item, only flags. The
Colosseum won sets 409, the throne room's scene (Event164) 379, the Termite pier's scientist and queen (Event165) 447,
the first landing at the Bugaria pier 448, and Elizant's welcome there 350. One scene, Event153, runs all six docks:
one each at the Termite pier, the Bugaria pier, Metal Island, the fishing village, Rubber Prison's pier and Mystery
Island, all leading to the lake map, MetalLake.

**How it was built (the apworld, 2026-09-30):**
1. **The items** (`items.json`), each key item's Archipelago id its bag id, as every key item's is:
   - *Boat Ticket*, 200, unchanged;
   - *Subaquatic Maritime Neotransport*, 212, the mod's next free key item;
   - *Progressive Boat*, 213, a number never in the bag: the mod gives each copy's key item in turn.

   `always` became a count (the Progressive Boat's is 2), and each item carries `progressive_boat`: true for the
   Progressive Boat, false for the other two. `items.own_copies` puts a seed's own set in the pool, each copy in a
   filler slot as the ticket's was.
2. **The option** (`options.py`): `ProgressiveBoat`, a `DefaultOnToggle`, after *Points of No Return*.
3. **The rules** (`custom_rules.py`): `Boat(level)`, a rule of our own as `CanUse` is, resolved from the option: level 1
   is one Progressive Boat or the Boat Ticket, level 2 two Progressive Boats or the submarine. `BOAT_TICKET` and
   `SUBMARINE` name the two, so a rule says what it needs. The boat to Metal Island and the later chapters' stand-in
   take the ticket; the six docks take the sub. Not the Rule Builder's `OptionFilter`: it needs the option's class,
   and `options.py` imports the data tables, which import the logic, so the logic can't import the options.
   **Apart, the logic stays cautious:** the docks sit inside the later chapters' stand-in, which holds the ticket, so
   the logic never counts the sub alone as reaching Metal Island, though the game allows it (`room-logic.md`, rule 5:
   more cautious than the game, never less).
4. **What else the sub gates.** The ant tunnel's door into the prison needs flag 79, which only the prison itself sets
   (Event193), and before that only the sub's dock reaches the prison. So that tunnel takes the sub too, and with it
   everything past the prison, the Giant's Lair included. The Icicle's spot (74) takes it as the story-order stand-in's
   caution: the story reaches it after the sub, though its path reads no sub flag.
5. **A new location, 76, *Termite Capitol: Termite King's Reward*** (first *Throne Room*; the user's name,
   2026-10-04): the king's scene, its flag 379 the check. It has the story-order rule of the abilities taught
   before it, and never needs the sub itself. It shows no item of its own, as the teaching scenes don't
   (`silent_locations`).
6. **The Termite gate, one way.** The sub made this fix necessary. From inside the plaza, before the gate was ever
   opened from outside (flag 384), its scene loads the outside map, looks for two guards only the plaza has, and stops
   (`EventControl.cs`, Event149). A sub landing at the Termite pier puts a party inside first, so the logic's gate now
   goes from outside to inside only, and `held_until` keeps the inside gate away until 384. **Opened from inside
   instead (2026-10-04, the user: "can we open it ?", after seeing it closed):** Event149 read whole: with 384 set it
   only rumbles, fades and loads the other side, from either side; its first opening adds the guards' talk, the
   welcome cinematic and the escort follower's (96) release, and sets nothing but 384. So the mod
   (`TermiteGate.cs`, `slot_data`'s `termite_gate_from_inside`) marks 384 and releases 96 as the scene starts inside
   before 384, and the scene takes the opened gate's way through; the hold is gone and the logic's gate is two-way
   again (test `test_the_termite_gate_opens_both_ways`). Seen the same day: in and out through the gate from a file
   that had never opened it from outside.
7. **For the mod** (`slot_data`): `submarine_item`; `present_with_item`, the six docks, made with key item 212 whatever
   their flags; `held_until_item`, the Termite pier's scientist and queen, who show the dock off, kept away until it
   too. The records are `ItemEntity`s in each dock's area module under `logic/` (`logic/later_chapters.py` until
   2026-10-04, build step 29). The option itself needs no key: the mod knows each
   item by its id.
8. **Hints** (`world.py`): item groups *Submarine* (the Progressive Boat and the submarine's item) and *Boat* (the
   Progressive Boat and the Boat Ticket). Read at 0.6.7: `!hint` matches what's typed against item and group names
   together (`Utils.get_intended_text`), and a group hints every item in it (`MultiServer.py`, `get_hints`), so `!hint
   submarine` finds it with the option on or off. On, it shows both copies: a hint can't pick out the second. A group
   can't share an item's name (Archipelago's `test_item_name_group_conflict`).

**The pool stays balanced:** on, the second copy takes a filler slot and the throne room adds one; off, the submarine's
item does the same.

**Tests** (`test_progressive_boat.py`, in place of `test_boat_ticket.py`). On:
- two copies, both progression, and neither level's own item;
- Metal Island with one copy;
- the lake with two only;
- the prison, its bridge and the Giant's Lair with two only;
- the throne room with one, the Icicle's spot with two;
- the throne room the one check on flag 379, and silent;
- the `slot_data` lists exact, and the gate one way;
- the groups *Submarine* and *Boat* holding the item (the tests may not import Archipelago's matcher, `Utils`: the
  preflight's list of the apworld's imports).

Off: each item once and no Progressive Boat; the boat with the ticket; the lake, the prison and the Giant's Lair with
the submarine; the throne room without it; the hints. `TestClassifications` runs in both, each checking its own items.

With the docks' and the tunnel's sub rule taken out, 4 of the first set fail; with the tunnel's alone, 3. With the rule
ignoring the option, 26 tests fail.

**Checked** (2026-09-30):
- `test-apworld.ps1`: 601 tests pass, the Logic Test check reproduced 90 of 90, and the fuzzer failed 0 of 10000, alone
  and with APQuest in every room. An earlier run with APQuest, before the option, failed 1 of 10000: the fill error in
  Known issues, measured at the same rate without this step.
- `seed-snapshot.py` before and after, for CI's three presets alone and with APQuest: `slot_data` gained the three keys,
  the gate's `held_until` entry, location 76 (flag 379, silent) and `item_kinds` entries for 212 and 213; the Boat
  Ticket's 200 is unchanged, as are `door_targets`, `enemy_swaps` and `start`. The fill moved, as a new location and
  copy make it.

**Status:** built (2026-09-30), the apworld's tests pass and the mod builds (its side:
[the mod guide, step 37](documentation.md#37-the-submarines-docks-follow-its-key-item)); with *Progressive Boat* off,
the Boat Ticket and the submarine seen arriving apart, the submarine first, the docks following it alone (2026-10-04);
the throne room's check seen (2026-10-04: the king's scene sent location 76, its item held up after it; flags 386
and 409 set by hand on a test file); the Progressive Boat's copies seen in order (2026-10-04: the first the Boat
Ticket, key item 200, the second the submarine, 212).

*Code: `data/items.json`, `options.py` (`ProgressiveBoat`), `items.py` (`own_copies`), `custom_rules.py` (`Boat`,
`BOAT_TICKET`, `SUBMARINE`, `SUBMARINE_KEY`), the docks' area modules in `logic/`, `data_types.py` (`ItemEntity`),
`slot_data.py`, `world.py` (`item_name_groups`); tests `test_progressive_boat.py`, `test_logic.py`
(`TestClassificationsSplitBoat`).*

## Build step 37: Points of No Return, the Warp counted as the way back

A yaml option, off by default, that lets the logic send the player where only the Warp gets them out (the user,
2026-09-30: "you are expected to get stuck somewhere, but you can always proceed if you keep going"; off by default
because "relying on and constantly using 'warp'" isn't fun for everyone). From the user's own play of Metroid Fusion:
jump down into a room for an item with no way out, then warp back to the start; it lets the logic place items "in
more/weird places" (`references.md`).

**Why it fits Archipelago:** its logic unfolds from the origin region, and `world api.md` (0.6.7, lines 280-281) says
"AP assumes that a player will always be able to return to this starting region by resetting the game ('Save and
quit')". Bug Fables' own reset, loading a save, puts you back at the crystal you saved at; Warp to Start is the one way
back to the start. `apworld_dev_faq.md` (line 185) lists making the reset part of the logic, with players warned, as a
way to handle what can't be undone. So with the option on, the Warp is counted as the way back to the start, and as
nothing else: never a way in.

**Decided (2026-09-30):** built now, named *Points of No Return*, off by default, and the Warp forced on with it, as
for a random start and the entrance randomizer.

**Built (2026-09-30):**

1. **The option** (`options.py`, `PointsOfNoReturn`), and `slot_data` `points_of_no_return` (inside `options` since
   build step 39).
2. **`WayBack`** (`custom_rules.py`), a rule of Bug Fables' own like `Boat`: what getting back from a one-way needs,
   its child rule with the option off, nothing with it on. `one_way(rule, way_back)` writes a one-way with its way
   back, never joined by hand, so the option drops only the way back. A one-way transfer carries it as `way_back`
   (`data_types.py`), joined in `regions.py`. Nothing uses it yet: no room is mapped, so today the option changes no
   seed's logic (`item-gates.py`'s report identical before and after). The room mapping writes every one-way with it,
   except where the Warp can't be used (`room-logic.md`, rules 4 and 9, question 20, C9).
   The preflight then allowed only `Has`, `HasAllCounts` and `Rule` from the Rule Builder, so `WayBack` was a plain
   `Rule` with a `child` field, and "nothing" an empty `HasAllCounts`. Since build step 42 (2026-10-03, the user's yes
   to widening the preflight) it is Archipelago's own `WrapperRule`, which serializes its child, and "nothing" is
   `True_()`.
3. **The mod:** the Warp is forced on with it (the mod guide, step 38).
4. **Tests** (`test_points_of_no_return.py`): off by default and in `slot_data`; the way back needed with it off,
   nothing with it on; a one-way needs its own rule and its way back off, only its own rule on. The two "on" tests fail
   with the option check taken out. The test helpers follow a `WayBack`'s child and a transfer's `way_back`, so a
   misspelt name inside one is still caught.

**Status:** built (2026-09-30), the tests pass and the mod builds; changes no seed until rooms are mapped; seen in game
(2026-10-04): a seed with it on and Travel Off keeps the Warp in the pause menu, though abilities as items force it on
in every seed today, so the option's own part isn't singled out yet (the mod guide, step 38).

*Code: `options.py` (`PointsOfNoReturn`), `custom_rules.py` (`WayBack`, `one_way`), `data_types.py` (`Transfer`),
`regions.py`, `slot_data.py`; tests `test_points_of_no_return.py`, `test/__init__.py` (`rule_parts`, `logic_rules`).*

## Build step 38: one-way doors in the entrance randomizer (the Forsaken Lands' fog maze)

**Seen (the user, 2026-10-02):** "the fog maze entrances on the way to the termite kingdom don't seem to be randomized
during entrance rando". In the game, "the fog maze just sends you back every now and then unless you walk the right
path".

**What the maze is** (the EntityDump, read 2026-10-02; `MEASURED.md`, "The Forsaken Lands' fog maze"):

- Each wrong turn is an invisible load zone at a room's edge, named `return…`, and no door leads back to where it
  lands. There are 12 of them in 8 `BarrenLands*` maps.
- Four lead into their own map: walk off one edge and you're back at the room's entrance.
- One spot, `BarrenLandsCD`'s left edge, is two copies switched by flag 384, the Termite gate's first opening from
  outside (build step 36): back to `BarrenLandsEntrance` before it, a shortcut to `BarrenLandsCloud` after.
- The right path is ordinary doors in pairs, already shuffled.

**Why they stayed as they were:** `door-graph.py --export` kept only mutual pairs as `connections` and put every other
door's map link in `fixed`. The shuffle never touched `fixed`, so every one-way door kept its destination in every mode.

**Decided (the user, 2026-10-02):**

- Archipelago's own way: each one-way door is a one-way entrance (`EntranceType.ONE_WAY`), which `randomize_entrances`
  pairs only with one-ways (`BaseClasses.Entrance.can_connect_to`; `entrance randomization.md`, 0.6.7: "one-ways are
  only randomized with other one-ways"). A wrong turn still sends you somewhere, just a different somewhere per seed.
- Every one-way door, not only the fog maze's. Each candidate was read first, since some were listed as "to check in
  play" (`MEASURED.md`, "Doors paired with their way back").

**How it was built:**

1. **The door table** (`door-graph.py --export`, `data/doors.json`):
   - **A new list, `one_way`**, `{map, door, to, copies}`: a door with a unique name and no door back within reach of
     where it lands, self-loops included. A story copy at the same spot counts only when every copy is one-way: the
     one present first (no required flag) is the door, and the others follow it.
   - **19 one-ways, 17 in play** (the other 2 are on the unused maps):
     - the 12 fog edges;
     - the pink spider's room, in from `BarrenLandsMushrooms` and out to `BarrenLandsPumpkins`;
     - the underground bar's exit (its way in is the hatch, a transfer);
     - the wizard's basement drop (flag 449, like the tower's door beside it, which no rule gates either; both kept
       present in a seed since build step 59);
     - `GiantLairBeforeBoss2`'s left ladder down, which lands 26 units from any ladder up.
   - **One missed pair:** `GiantLairBeforeBoss: loadzoneup` and `GiantLairBeforeBoss2: loadzoneright`. The way back
     lands 11.3 from the ladder (the ladder's height), past the 10 the pairing allowed. With 12 the export gains exactly
     that pair and loses none: 255 connections, 510 doors. Measuring flat instead would have broken 10 real pairs.
   - **Parked doors are left out:** a load zone at height 90 or more. The game parks unused objects at 99 to 9999, and
     the Sand Castle's two right-hand basement doors are the only load zones there; nothing in the code moves them.
     Their link left `fixed` too: the logic no longer counts a way it can't prove (more cautious, never less).
   - **`fixed` keeps 22 links:** story copies that have pairs (the Golden Settlement by day and night, the Beehive),
     names two doors share (`GoldenPathTunnel2`, `WaspKingdomOutside`), `TermiteIndustrial`'s in-map pair.
2. **Regions** (`regions.py`): each one-way is an entrance of its map, named where it is (`"<map>: <door>"`, as every
   door), to the map it leads into, and one-way typed (Archipelago's default for an `Entrance`). It replaces the plain
   link `fixed` made, so the logic with the doors off is the same graph.
3. **The shuffle** (`entrances.py`), Coupled and Decoupled:
   - each one-way is split with `disconnect_entrance_for_randomization(..., one_way_target_name=...)`, the
     function's required name for a one-way's target;
   - the target is named for its landing, `"<to> as from <map>: <door>"`. That's apart from the door's own name,
     since coupled, Archipelago never joins an exit to a target of its own name, and a one-way may keep its own
     landing;
   - a pairing (x, y) of one-ways means x now lands where y did;
   - Room Swap leaves them connected as the game has them: rooms move whole, and a one-way still leads into the room
     it did.
4. **`door_targets`:**
   - a one-way x rewritten like y itself (the mod copies y's destination and landing), so the mod needed no change;
   - a story copy always gets an entry following its door, even when the door keeps its own landing, in every mode
     that rewrites (Room Swap too). So `BarrenLandsCD`'s left edge leads one place whatever the Termite gate.
   - That also ends an untrue link: `fixed` used to join `BarrenLandsCD` to `BarrenLandsCloud` with no rule, though
     the game has it only from flag 384.
5. **The spoiler** lists each one-way once, `=>`, to its landing's name.
6. **Plando** (build step 32):
   - `DoorPlando`'s entrances gain the one-way doors, its exits their landings;
   - Archipelago's own `can_connect` refuses a one-way with a two-way;
   - a one-way connection is joined one way only, whatever its direction, as The Messenger joins a one-way
     (`connect_plando`).

**Tests** (`test_doors.py`, `test_areas.py`):

- **The table:** the exact 17 one-ways and the twin's copy, the ladder pair, the parked doors in no list, landings
  named apart.
- **Coupled and Decoupled, plando too:** every one-way paired once, only with a one-way, one-way typed (fails with the
  split taken out: 7 tests). The mod lands each one-way and copy where its entrance leads in the region graph. The copy
  follows its door. Spoiler counts.
- **Room Swap:** the one-ways stay as they are.
- **Plando:** a planned fog edge in both modes; a one-way and a two-way refused both ways round.
- **Every one-way an entrance** where the game has it, and no unused map among them.

**Checked (2026-10-02):**

- `test-apworld.ps1`: 639 tests pass, the Logic Test check reproduces 90 of 90 generations, the fuzzer 0 failures in
  10000.
- 244 regions, 584 entrances.
- Six seeds generated through Archipelago's Generate, with APQuest in the room:
  - Coupled: 17 one-way `=>` lines beside the 255 pairs. One wrong turn led into the city's commercial district, as
    the bar's exit does: the pool is the whole game's.
  - Decoupled: 527 lines.
  - Room Swap: no one-way line.
  - A one-way plando under Coupled and Decoupled, and alone: the planned fog edge lands where the yaml says.

**Status:** built (2026-10-02), the tests and the fuzzer pass and seeds generate alone and with APQuest; not yet seen in
game.

*Code: `dev-scripts/door-graph.py` (`export`, `FAR`, `PARKED`), `data/doors.json`, `data_types.py` (`OneWayDoor`),
`data_tables.py` (`ONE_WAYS`, `one_way_landing`), `regions.py`, `entrances.py`, `world.py`, `options.py`
(`DoorPlando`); tests `test_doors.py`, `test_areas.py`.*

## Build step 39: the seed's options in slot_data, one `options` dict the mod reads

**Why:** Universal Tracker (Next 43 item 14; build step 40) rebuilds a seed from its slot_data with no yaml, so every
option that shapes the seed's locations, doors, rules and goal must be in it. Its docs say how: "store all options that
affect generation in your slot data", through `options.as_dict(...)`, "Take care not to include options that don't
affect generation and aren't useful for the game client" (Universal Tracker's `docs/apworld-integration.md`, branch
`tracker`, read 2026-10-03). That is also review item 11's `options.as_dict`, the way Archipelago gives option
values (`Options.py` 0.6.7, `CommonOptions.as_dict`). slot_data had four option copies written by hand
(`artifacts_required`, `shuffle_moves`, `shuffle_jump`, `points_of_no_return`), and no `progressive_boat` at all.

**Decided (the user, 2026-10-03):** the mod reads its option values from the new dict, and the copies go, the state
item 15 already decided; its own step, ahead of Universal Tracker. And no support for older versions (How it works §7):
the mod reads only the new keys.

**How it was built:**

1. **`options`** (`slot_data.py`): `options_for_slot` is `as_dict` over `SLOT_OPTIONS`, toggles as JSON booleans
   (`toggles_as_bools`), with three values as this seed applied them, since a tracker must see what the seed did, not
   what the yaml asked:
   - `artifacts_required` capped to the artifacts this version includes (build step 3);
   - `filler_starting_checks` off where Coupled or Room Swap stood it down (build step 35);
   - `shop_contents` No Progression when Filler Only fell back in `pre_fill` (build step 11), which a tracker never
     runs.
   `SLOT_OPTIONS` is the location categories, Shop Contents, the entrance randomizer, Filler Starting Checks, the field
   moves, Jump, Points of No Return, Progressive Boat, the goal and the player's `exclude_locations`. `NOT_SENT` names
   every other option and why: its result is already in slot_data (enemy shuffle, starting location and member, music,
   shop inventories, plando connections), fill only, or the server's.
2. **The fallback's two bugs** (Next 43 item 1), found on the way: a tracker rebuilding a fallen-back seed would see
   the player's excluded shop excluded while the seed had set it back. Fixed in build step 11.
3. **The mod** (`SeedData.cs`, `SlotData.cs`): `On` and `Number` read a JSON boolean or integer inside `options`;
   `MovesShuffled`, `JumpShuffled`, `PointsOfNoReturn` and `ArtifactsRequired` come from there. With no `options`, the
   main menu's status line says the seed comes from an older apworld and to use the latest release of everything
   (`ApConnection.cs`), and the log says its goal and option rules won't apply.
4. **Tests:**
   - `TestOptionsSent`: `SLOT_OPTIONS` and `NOT_SENT` don't overlap and together are every option, so a new option
     must be sent or say why not.
   - `TestOptionsInSlotData`: JSON booleans, numbers, a sorted location list; the four copies gone.
   - The applied values: the goal capped (`TestArtifactsCapped`), Filler Starting Checks off under Coupled,
     `shop_contents` 1 after the fallback and 2 without one.
   - `generate_like_main` (`test/__init__.py`): a world built step by step as `Main.py` builds it, the player's
     exclusions applied right after `set_rules` with Archipelago's `exclusion_rules`, which `WorldTestBase` never
     applies. Its four names joined the preflight's apworld list in a commit of their own (the user's yes).

**Checked (2026-10-03):**

- `seed-snapshot.py` before and after, CI's three presets alone and with APQuest: in every slot_data only the four
  copies left and `options` arrived; every other key and every spoiler identical.
- The tests pass, the fallback's two fail with the old loop, and the mod builds.

**Status:** built (2026-10-03), the tests pass and the mod builds; not yet seen in game (a fresh seed with Shuffle
Field Moves, Shuffle Jump and Points of No Return on: the moves refused, the jump refused, the Warp in the pause menu,
and the goal sent).

*Code: `slot_data.py` (`SLOT_OPTIONS`, `NOT_SENT`, `options_for_slot`), `rules.py` (`fall_back_from_filler_only`),
`world.py` (`shops_fell_back`); in the mod `SeedData.cs`, `SlotData.cs` (`On`, `Number`), `ApConnection.cs`; tests
`test_slot_data.py`, `test_shops.py`, `test_starting_checks.py`, `test/__init__.py` (`generate_like_main`).*

## Build step 40: Universal Tracker, the seed rebuilt from slot_data with no yaml

**Why:** Universal Tracker is the tracker Archipelago's player docs name beside PopTracker: it shows "what locations
are currently in-logic or not, using the actual generation logic" (`worlds/generic/docs/other_en.md`, 0.6.7, the
Universal Tracker section; review item 14). It regenerates the player's world inside itself and asks the world's own
rules what the items received reach. Anything random that isn't a yaml option or an item must come
back from slot_data, or it rolls its own: with the doors shuffled, its doors wouldn't be the seed's.

**What it does** (its `docs/apworld-integration.md`, `re-gen-passthrough.md` and `TrackerCore.py`, branch `tracker`,
read 2026-10-03; release v0.3.4, 2026-09-23, `minimum_ap_version` 0.6.2):

- It reruns only `generate_early`, `create_regions`, `create_items`, `set_rules`, `connect_entrances` and
  `generate_basic` (`TrackerCore.TMain`), never `pre_fill`, fill or `fill_slot_data`. After `set_rules` it applies
  Archipelago's `exclusion_rules` with the slot's `exclude_locations`.
- A world with `ut_can_gen_without_yaml = True` and a static `interpret_slot_data` that returns the slot_data needs no
  yaml: Universal Tracker writes one with every option at its default and regenerates the world alone, with
  `multiworld.re_gen_passthrough["Bug Fables"]` set to the slot_data (after JSON, as the server sent it) and
  `generation_is_fake` set.
- It drops precollected items that have an id: the server sends those as received items.

**Decided (the user, 2026-10-01 and 2026-10-03):** full support, every feature its docs offer that applies; no yaml;
every location in logic always shown, so no deferred entrances (every door connected from slot_data from the start).
The map tab and the mod's data storage keys wait for the PopTracker pack's map. **Since:** deferred entrances decided
yes on 2026-10-06 (hiding a door hides no location in logic; build steps 41 and 55), and the mod's keys built (build
steps 55 and 57); the map tab still waits.

**How it was built:**

1. **The door replay** (`entrances.py`), the doors rebuilt from `door_targets` alone, the table the mod rewrites doors
   from, so the tracker follows exactly what the game does and slot_data carries nothing new:
   - `pairings_from_targets` reads `door_targets` back into its pairings, `door_targets`' inverse: a two-way door
     leads to the partner of the door it's rewritten like, a one-way takes the landing of the one-way it's rewritten
     like, a door with no entry is as the game has it, a story copy is never read. It refuses a door the world doesn't
     shuffle, two doors sent to one place, or a door rewritten like one of another kind.
   - `replay` splits the doors as the shuffle does (`_split`, moved out of `shuffle` unchanged: the one-ways only for
     Coupled and Decoupled) and connects each pairing as Archipelago's randomizer connects it (`_connect`, as Room
     Swap and plando do). It refuses to leave a door unconnected, which Universal Tracker would list as unconnected
     with everything behind it.
   - Tests (`test_doors.py`): in every mode that shuffles, plando included, `door_targets` reads back into the seed's
     pairings, and a world built up to `set_rules` and replayed has the seed's entrances exactly (name, region, where
     it leads, randomization type). Any pairing reads back (200 random Decoupled, Coupled and Room Swap layouts);
     no entries is the game's own doors; a malformed table is refused.
   - `seed-snapshot.py` identical before and after: splitting draws nothing from the seed's random.
2. **Universal Tracker's hooks** (`world.py`, `universal_tracker.py`):
   - `ut_can_gen_without_yaml = True`, and a static `interpret_slot_data` that hands the slot_data back, so Universal
     Tracker regenerates with it.
   - `generate_early` reads the passthrough (`universal_tracker.passthrough`). With one, it first sets the options from
     slot_data's `options`, every other option at its default as in the empty yaml (`apply_options`). The options
     object is built the way Archipelago builds it (`MultiWorld.set_options`: the dataclass from each option's
     `from_any`), with no lookup by a computed name. It then takes the seed's own rolls instead of rolling: the
     starting member (before the locations and rules that depend on it), the enemy swaps and the start.
   - `connect_entrances` takes `door_targets` verbatim and replays it; `generate_basic` takes the music, jingles and
     shop inventories. So the rebuilt world's slot_data is the seed's, key for key.
   - **Refused** (no support for older versions, How it works §7): a seed whose `world_version` isn't this apworld's,
     or with no `options`. Universal Tracker shows its "not able to be generated" line, the reason in its log.
   - `generation_is_fake` is never read: Universal Tracker always regenerates this world with the passthrough, which
     carries every roll. `disable_ut` isn't set.
3. **Tests** (`test_tracker.py`, no Universal Tracker needed): `regenerate` mirrors `TrackerCore.TMain` (default
   options, the slot_data through JSON as the passthrough, its six steps through `call_all`, Archipelago's
   `exclusion_rules` after `set_rules`, precollected items with an id dropped). 18 cases, two seeds each: the defaults,
   the three door modes, plando in each, Filler Only fallen back and held, the fallback with excluded locations, each
   party start, field moves and Jump, seven artifacts, the boat apart, Points of No Return, minimal accessibility, a
   start inventory. In each, against the seed built as `Main.py` builds it: the whole slot_data, every entrance, every
   location with its exclusion, the door pairings, and the locations reached with nothing, with all progression and
   with six random handfuls of it (the seed's start inventory sent as the server sends it). Also: a different
   version or missing `options` is refused, and another game's passthrough is never read. **Every case failed with the
   passthrough ignored** (the defaults too: the shop inventories roll their own).

4. **Universal Tracker's own fuzzer hook** (`dev-scripts/tracker_fuzz_hook.py`, `test-apworld.ps1`; `development.md`,
   "Fuzzing the apworld"): with Universal Tracker v0.3.4 in the Archipelago checkout (the user's yes, 2026-10-03), a
   second fuzzer pass runs its `YamllessHook`, which regenerates every fuzzed seed with Universal Tracker's own code
   and checks each sphere against the real generation. Our subclass only empties Universal Tracker's class-level cache
   of regenerated worlds before each run, since fuzz.py's workers live for the whole run. Its `Hook` takes the same
   yaml-less branch for this world, so only `YamllessHook` runs.

**Checked (2026-10-03):**

- The tests pass; `seed-snapshot.py` identical before and after (a real generation has no passthrough).
- `test-apworld.ps1`: 668 tests, the Logic Test check 90 of 90, the fuzzer 0 failures in 10000, and Universal Tracker's
  hook 0 failures, 0 timeouts and 0 ignored in 10000.
- The hook fails when it should: with the passthrough ignored, 19 of 20 runs failed, each log naming a location "in
  server logic but not expected in UT".

**Status:** built (2026-10-03), the tests, the fuzzer and Universal Tracker's hook pass and seeds are unchanged; not yet
seen in Universal Tracker itself (the user connecting it with no yaml to a door-shuffled seed).

*Code: `universal_tracker.py` (`passthrough`, `apply_options`), `world.py` (`ut_can_gen_without_yaml`,
`interpret_slot_data`, the passthrough in `generate_early`, `connect_entrances` and `generate_basic`), `entrances.py`
(`_split`, `pairings_from_targets`, `replay`); the player guide's "Tracking your seed"; tests `test_tracker.py`,
`test_doors.py` (`DoorPairTests`, `TestDoorTargetsReadBack`), `test/__init__.py` (`entrance_graph`);
`dev-scripts/tracker_fuzz_hook.py`, `dev-scripts/test-apworld.ps1`.*

## Build step 41: Universal Tracker's list order and explanations

**Why:** the rest of what Universal Tracker's docs offer a world
(`docs/apworld-integration.md`, `re-gen-passthrough.md`, branch `tracker`, read 2026-10-03), each built or decided, none
left as a gap.

**Decided (the user, 2026-10-03):**

- **The list's order:** "areas in story order, then name". The old plan assumed the location ids follow the game; they
  follow the order the checks were added, so the order comes from the logic's areas instead.
- **`/explain`: Archipelago's standard.** `rule builder.md` (0.6.7) gives two equal ways to write a custom rule: its own
  `Resolved` class, whose explanation "can be overridden", or resolving to the built-in rules "instead of needing to
  define your own" (its `ComplicatedFilter` example), which explain themselves. Ours resolve to built-ins (`CanUse`,
  `Member`, `MoveItem`, `Boat`, `WayBack`), neither way is a "should", Universal Tracker's default `/explain` "will use
  the rule builder api", and APQuest (`main`) has no custom rules. So nothing is added; an override only where the
  output reads wrong.

**How it was built:**

1. **`custom_ut_sort`** (`world.py`), used when Universal Tracker's `sorting_method` is `apworld`, its default:
   `TRACKER_ORDER` (`logic/__init__.py`) ranks each location by its area in `AREAS`' order (the Outskirts, Snakemouth
   Den, Bugaria City, Metal Island, the later chapters) and by name within the area, so a room's spots sit together.
   Anything else Universal Tracker passes, such as an unconnected entrance's line, goes last.
2. **What `/explain` and `/get_logical_path` print** (read in its `TrackerClient.py`, v0.3.4): each spot's and door's
   `access_rule.explain_json(state)`, the built-ins' own lines, e.g. "Missing some of (Missing: Kabbu x1, Horn Slash
   x1)"; a spot with no rule prints "Location has a default access rule"; a door with none prints `True` or `False`.
   Both commands need its client, so the test does what they do instead (`TestTrackerExplains`, on a Coupled seed with
   plando rebuilt from its slot_data): every rule explains itself, with a state and without, and each reachable
   location's path, walked from `state.path` as `/get_logical_path` walks it, names real entrances.
3. **The rest of its docs, each decided:**
   - `location_id_to_alias`, for "a generically named location": our names are fixed and never change meaning per
     seed (a shop's numbered copies are always the same copies), so there is nothing to alias.
   - `glitches_item_name`, for spots reachable in the game though not in logic ("glitched" logic): our logic has no
     such rules; adding some is a logic decision.
   - `explain_rule`, `get_logical_path`, `explain_path`, `explain_spot`, `explain_more` and their sub-commands: the
     defaults read right so far; an override waits for the user's look at them.
   - Deferred entrances (`found_entrances_datastorage_key`, `reconnect_found_entrances`,
     `enforce_deferred_connections`): **decided yes (the user, 2026-10-06)**, shuffled doors hidden until taken, by
     default, as TUNIC does; Universal Tracker's own host.yaml setting `enforce_deferred_entrances` (its `setup.md`,
     default `"default"`) is the player's switch, so we add none. This replaces 2026-10-01's "no", which came from a
     misreading: hiding a door hides no location in logic. Build step 55. Deferred events (story events shown on the
     map tab until done): with the map tab, on the same keys.
   - `disable_ut`: not set.
   - Its client integration (`docs/client-integration.md`): for clients built on Archipelago's CommonClient; ours is a
     BepInEx mod. Tracker addons are installed by the player.
   - The map tab (`tracker_world`, `docs/map-integration.md`, re-read before it's built: it changes without this repo
     changing): once the PopTracker pack has its map (the user, 2026-10-03); the mod's keys it follows are written
     since build step 57. The plan for it (moved here from a local plan file, 2026-10-08):
     - **The pack as an external pack**, that doc's recommendation (no map images in an apworld): `tracker_world` with
       `external_pack_key`, `map_page_maps`, `map_page_locations` and `map_page_layouts` pointing into the pack; or
       its hybrid way (the JSONs in the apworld, the images outside), chosen when it's built.
     - **A host.yaml setting for the pack's path:** a `settings.Group` with a `FilePath`, `required = False`, as TUNIC
       has (`worlds/tunic/__init__.py`, 0.6.7). The preflight refuses a `settings` member today
       (`apworld_denied_members`, `dev-scripts/preflight-patterns.json`): the user's call first, and
       `archipelago-review.md`'s line that `settings api.md` doesn't apply changes with it.
     - The pack's section names are our location and entrance names, so no `poptracker_name_mapping`;
       `poptracker_entrance_mapping` only if a door's pin name differs.
     - With deferred entrances (build step 55) a door's pin starts unconnected: its colours (that doc's table) are
       checked on screen against that. No fog of war there: every room shows (fog of war is the pack's alone).
     - The rest of that doc, every part (the user, 2026-10-05: "support them all"; listed 2026-10-08 from v0.3.4):
       auto tabbing to the player's map (`map_page_setting_key`, the mod's map key written as
       `bug_fables_map_{team}_{player}`, which Universal Tracker fills in itself; `map_page_index`), the player's
       position icon (`location_setting_key`, the same key; `location_icon_coords` from the pack's layout), and
       `ut_map_page_hidden_locations`, `_entrances` and `_events` only where a map would show the wrong thing, never
       to hide what's reachable. Universal Tracker passes `map_page_index` and `location_icon_coords` the key's value
       or `""` before it's read (`None` when the server holds none; its `TrackerClient.py`, v0.3.4): an unknown value
       keeps the tab (`-1`) and hides the icon (`None`).
4. **Tests** (`test_tracker.py`): the list starts with the Outskirts, sorted by name within it, the Outskirts before
   Bugaria City, an entrance last; and the explanations above.

**Status:** built (2026-10-03), the tests pass; not yet seen in Universal Tracker (the list's order, `/explain` on a few
spots and `/get_logical_path` through shuffled doors).

*Code: `world.py` (`custom_ut_sort`), `logic/__init__.py` (`TRACKER_ORDER`), `data_tables.py`; tests
`test_tracker.py` (`TestTrackerExplains`, `test_the_list_in_story_order`).*

## Build step 42: the PopTracker pack, first part: its own repo, the logic exported from the apworld

**Why:** PopTracker is the other tracker Archipelago's player docs name (`worlds/generic/docs/other_en.md`), and the
user wants it fully supported (2026-10-01): a pack that follows a seed, with a map drawn by code, rooms placed where
Room Swap put them, and fog of war. Its plan is the pack's own `PLAN.md`.

**Decided (the user, 2026-10-03):**
- **Its own repo** (MIT, like this one), never inside this one. Each of its steps is still written up here, in this
  guide, since it's the Archipelago side of the project.
- **The logic is generated from the apworld, never written by hand.** The pack's export imports this apworld through
  your Archipelago checkout (where `development.md` links it in as `worlds/bug_fables`) and writes each rule with Rule
  Builder's `to_dict`. That's what `rule builder.md`'s Serialization section gives it for: "to facilitate exporting the
  rules to a client or tracker", the dumping "left up to the world dev". A pack ported by hand drifts from the apworld;
  a generated one can't.
- **How it's checked:** PopTracker's own `pack-checker`, which validates a pack against PopTracker's schemas (the
  Crystal pack's CI runs its action), and **parity tests with lupa**.
- **Why lupa:** Archipelago has no standard for testing a tracker's Lua. Its core runs no Lua (its `.lua` files are
  emulator connectors that run inside BizHawk), and its docs don't cover PopTracker packs. The Crystal pack has no Lua
  tests either: pack-checker only, its logic ported by hand. Ours is generated, so its Lua can be run beside the
  apworld's own logic and compared. lupa is a Python package that embeds Lua, so one Python test can generate seeds
  with Archipelago and ask both, the apworld and the pack's Lua, what's reachable with the same items. It runs Lua 5.4,
  the version PopTracker's own `.luarc.json` example sets. MIT, and only a test dependency: the pack stays MIT.

**How it was built:**

1. **Every rule serializes** (this repo). Rule Builder's `to_dict` writes a rule's fields as they are, so a custom
   rule holding another rule needs Archipelago's `WrapperRule`, which writes its child as a rule dict too. `WayBack`
   was a plain `Rule` with a `child`, since the preflight then allowed only `Has`, `HasAllCounts` and `Rule` (build
   step 37). On the user's yes it now allows `WrapperRule` and `True_` too: `WayBack` is a `WrapperRule`, and "needs
   nothing" is `True_()` in every custom rule (an empty `HasAllCounts` resolved to the same). `Boat` and `WayBack`
   stay custom rules that read an option: `rule builder.md`'s own `ComplicatedFilter` example does the same.
2. **One rule per spot** (`rules.py`, `spot_rule`): its reach, its own rule and Jump, the same in every seed, with
   the options resolving it. Jump's blanket rule is `JUMP`, `Has("Jump")` with an `OptionFilter` on Shuffle Jump and
   `filtered_resolution=True` (`rule builder.md`'s own example of a need one setting skips; `archipelago-review.md`
   item 13), where it used to be added by an `if` only when the option was on. The preflight allows `OptionFilter` too
   now, on the same yes. `set_all_rules` sets this rule and the pack exports it: one source. The entrances the same
   way (`regions.py`, `logic_entrances`): every entrance with its rule, as the game has it, which
   `create_and_connect_regions` makes and the pack exports.
3. **Tests** (`test_rule_export.py`): every rule the logic writes, every spot's whole rule and every entrance's rule
   goes through `to_dict`, JSON and `from_dict` unchanged, and a one-way keeps its way back's child (this fails with
   `WayBack` as a plain `Rule`); `logic_entrances` is exactly the world's entrances.
4. **The export** (the pack's `tools/export.py`): imports the apworld from your Archipelago checkout, never writing
   into it, and writes every entrance, location and story event with its rule's `to_dict`, plus the tables the
   custom rules read (`ABILITIES`, the boat's levels, the members, which option each category follows). It refuses
   a rule the pack's Lua can't evaluate, or an option filter on an option slot_data doesn't carry. The data goes in as
   a generated Lua table, since a pack's Lua can't read a JSON file of its own. Beside it: the items every rule can
   need, one pin per room with a check (one section per check, named as the location).
5. **The Lua** (`scripts/logic.lua`): evaluates each rule dict, the custom rules as their `_instantiate` resolves
   them and an option filter against slot_data's `options`, and sweeps regions as Archipelago does: through
   entrances, collecting story events where reached, until nothing changes. A door's destination comes from
   `door_targets`. The starting members count as held, since they're start inventory. `autotracking.lua` takes
   slot_data on connect, then the items and checks as they arrive.
6. **Its tests** (`tools/test.ps1`): the export, then the parity tests, then `pack-checker` in strict mode against
   PopTracker's own schemas at a pinned commit. One parity test generates seeds with random options (every door mode
   included) and random item sets, and asks both the apworld and the Lua which locations are reachable. The other
   compares each custom rule, with every argument, one by one: a rule the logic doesn't use yet (`WayBack`) is still
   checked. Breaking a rule in the Lua fails them (four breaks tried, each caught).
7. **The world map** (the user, 2026-10-03/04: "like on the map in game", then each area's rooms too): a World tab
   with one pin per area where the game's pause map puts it, with the lines the game draws between areas, all
   drawn by code from the numbers in `PauseMenu.cs` (`MEASURED.md`, the pause-menu map). The first try was turned
   180°; the user's screenshot of the pause map gave the right way round. Each area with checks has its own tab; an
   area pin shows its rooms' checks through PopTracker's section `ref`. Each map's area is the game's own
   (`MapControl.areaid`, from the mod's map dump), so a room can sit in an area its location names don't say.
8. **Each area's rooms, placed from the game's own door data** (the user's pick of three, 2026-10-03: the game's
   data first, a hand table to nudge later). The mod's entity dump gives each door's spot in its room and where it
   lands in the next (`MEASURED.md`, the map entity table); a room goes where its arrival point meets the door that
   leads there, so a door on a room's right leads to a room on its right. What the user's screenshots showed, and the
   rule each became:
   - lines through rooms and to nowhere: only the logic's doors count (the apworld's `doors.json`; the dump also
     lists signs and NPCs that move you between maps), and each pair of rooms gets one line, door to door;
   - one-way doors (the fog maze) loop anywhere: a ring at the door, no line;
   - a room entered from inside another (a house, a hall's back-wall door): a small box at its door;
   - rooms that can't sit side by side (Bugaria City's districts loop left and right, as the user knows it from
     playing; stacked floors): a pair of matching coloured rings, one at each door;
   - test and cutscene maps (`TestRoom`, `Blank`, `SnakemouthEmpty`): only the logic's maps are drawn.
   Night and story versions of a room (Golden Settlement at night, Bugaria City under attack) are their own maps in
   the logic; a tab of their own waits until one holds a check. The user, 2026-10-04: good enough until there are
   checks everywhere.

**Status:** in progress (2026-10-04): built, the tests pass (this repo's, and the pack's: parity on 200 seeds,
pack-checker strict). Seen in PopTracker 0.35.4: the pack loads, the colours with no items are the ones the Lua
predicted, a room's popup lists its checks, a clicked pin clears (2026-10-03); the world map like the pause map, and
each area's rooms (2026-10-04). Not yet seen: every item held, auto-tracking. Still placeholders: the icons, the
area names (the game's `AreaNames` text not read yet) and the room names. Exported again at world 0.3.0 (build step
56) and its Archipelago interface built (build step 58), 2026-10-08.

*Code: `rules.py` (`spot_rule`, `JUMP`), `regions.py` (`logic_entrances`, `MENU`), `custom_rules.py` (`WayBack`);
tests `test_rule_export.py`;
`dev-scripts/preflight-patterns.json` (`apworld_imports`).*

## Build step 43: Music Shuffle, the Honey Factory's two songs, the elevator's crossfade a plain fade

**Asked (the user, 2026-10-04):** "any reason to not include those 7?", the tracks build step 33 keeps out of the pool.
The answer, track by track:

- **The title:** it plays before the mod knows the seed. In the pool, the song meant to replace it would only play on
  the title screen after connecting, so one song would go unheard in the run. Kept out.
- **The four ambience beds** (`Wind`, `Water`, `MachineHum`, `Breathing`): sound, not songs. An area would play wind
  instead of music. Kept out, as Samira's list keeps them out.
- **The factory's `Dungeon2` and `Dungeon2b`:** real songs, kept out only for how the elevator switches between them.
  **Added (the user's choice).**

**How the elevator switches them (read first):** `EventControl.cs:17047-17051` (the factory's storage elevator) calls
`ChangeMusic(name, 0.025, 0, seamless: true)`, the game's only seamless change. `SwitchMusic`'s seamless branch starts
the next clip on a free sound slot at the playing clip's time, fades the music player down while the slot comes up,
then hands the clip to the player at the slot's time (`MainManager.cs:4694-4775`): made for two versions of one song in
step. Under the shuffle the player is muted and the mod's voice plays the swapped song, so that slot would sound the
game's own song through the fade, and a fade in step between two unrelated songs means nothing.

**Built:**

1. **The pool** (`music.py`): `Dungeon2` and `Dungeon2b` leave `KEPT`; the pool is 67 tracks.
2. **The mod** (`MusicShuffle.cs`, `KeepSeamless`): a prefix on `MainManager.ChangeMusic(AudioClip, float, int, bool)`
   turns `seamless` off when the playing song or the next one is swapped, and logs it (`[music] ...: the seamless
   switch made a plain fade`). The player then fades out and switches as for any other change, and the voice follows.
   With either song unswapped (a seed without the option, or a song shuffled onto itself) the game's crossfade stays.

**Tests:** `test_factory_songs_are_shuffled` (both in `music_map`, the pool 67): it failed with the old list restored.

**Status:** built (2026-10-04), the tests pass; seen in game the same day: in a seed with the factory's songs swapped
(Dungeon2 as Inside0, Dungeon2b as Field1), each elevator ride, up and down, faded plainly into the other swapped song,
the log's `seamless switch made a plain fade` each time. The user walked in from the storage maze: a dev warp straight
into the room left the last room's song playing (a dev tool's gap, not the shuffle's).

*Code: `music.py` (`KEPT`), `World/MusicShuffle.cs` (`KeepSeamless`, `SoundHooks`); tests `test_music.py`.*

## Build step 44: every quest available from the start, none of it done for you

**The rule (the user, 2026-10-04):** the game is open, metroidvania-like, but never "every flag done": as finished as
it can be while leaving everything undone. For quests: **open only the gate that makes a quest available; every step
of the quest itself is the game's.** Nothing is accepted, started or completed for you.

- **A board quest** joins the boards' open list from the start, the game's own way (its `QuestChecks` row met, or the
  game's own add). Taking it stays the player's: the game sets its own "taken" flag.
- **A quest waiting for a cutscene** is added directly; the cutscene doesn't play. Anything else that cutscene sets up
  and the quest needs (an NPC present, a door) is opened on its own, one entity at a time, as build step 9 does.
- **A quest an NPC gives** (not on a board, such as the Golden Path cave's): the NPC is there from the start (kept
  present if the story brings them later); talking starts it, as in the game.
- **Followers** join when the game itself starts the quest. Faked flags crashed the lost kid's scene twice (build
  step 10), so a follower is never added by the mod.
- **The logic** of each quest's locations needs all of it: reaching the board or the NPC, every place the quest sends
  you, and what each step needs.
- **When a gate's cutscene also changes the world**, the user decides.

**One quest at a time.** Each is read (what makes it available, each step, any follower), then gets its logic and a
test, then is seen on screen. **A quest not yet gone through is out of every seed** (`pending` on its locations: no
location, its item vanilla), so the logic never claims a quest the game keeps closed.

**Found the same day:** the lost kid's reward (location 10) claimed only Leif and the first boss, but in the game his
quest (board quest 5, *LadybugQuest*) joins the boards only at chapter 5's start (flag 348, `Event120`), then Leby
follows (flag 54) and Dib waits at the lake. Shuffle Quests is on by default, so a seed could have put a needed item
there out of reach. Location 10 is `pending` until this quest is opened. The Old Book delivery (16-18, board quest 33,
open once Bugaria City is visited) was played through (build step 9) and stays in.

**Tests:** `TestPendingQuest` (location 10 out of the seed, not in `location_gives`): fails without `pending`.

**Status:** started (2026-10-04): the rule written, location 10 held back; no quest opened yet.

*Code: `data_types.py` (`Location.pending`), `world.py` (`included_locations`), `options.py` (`category_count`),
`logic/snakemouth_den.py`; tests `test_categories.py`.*

## Build step 45: the ant tunnels, the miners dig for free

**Asked (the user, 2026-10-04):** "can we make them open for free/0 berries ? but you still need to reach them ?"
(their option A; tickets, option B, are Next 57). In the game the tunnel hub (`AntTunnels`, under the Ant Palace) has
one door per far end, each made from that end's flag, set by paying that end's miner, Diana (`Event48`; the prices and
flags in `MEASURED.md`, the ant tunnels). The ride (`Event49`) then goes both ways.

**The logic:** each far end leads to the hub once reached, since that's where the (now free) miner is:
`GoldenSettlementEntrance`, `DefiantRoot2`, `BarrenLandsAntTunnel` and `FGCave` to `AntTunnels`, one-way. The hub's
way back out needs that end's flag, set only by having been there, so it reaches nothing new and isn't listed. Metal
Island and the Rubber Prison keep their earlier, more cautious two-way transfers (`LATER_CHAPTERS`). From the hub the
palace is a plain door. **Seen the same day:** coming up into the palace hall boxed the party in, inside the mine
shaft's railing (`Base/mineblock`) with no way back down (`MineLoadZone`), both until chapter 2 (flag 67). The railing
is hidden and the door kept present, and the logic's chapter-2 rule on that door is gone.
The hall's library door too (the user: "remove the library blocker as well"): `Loadzonelibrary` kept present, its
blocker `makiblocker1` (Event12, until 67) kept away, its door rule gone. The library's location (15) and the Old Book
quest (16-18 and its delivery) had `INNER_CITY` only for that door: the reader's lines have no chapter gate (her item
menu takes key item 93 and sets flag 242), so the stand-in is gone and the whole quest is in logic from the start (the
user: "why are we using the placeholder ?").
And the war room door opposite (the user: "open the other path as well"): `loadzonewarroom` kept present, its
blocker `makiblocker2` kept away; inside, two NPCs and a medal from after the ending (flag 555). The hall has no door
rule left.
**Location 77, *Ant Palace: War Room, Table*** (the user: "we should make that medal always appear on the table",
the name theirs): the table's `royal medal`, Royal Calling (medal 80, flag 717), made only after the ending (555), is
kept present from the start and is a location; Royal Calling joins the item table (useful: a battle skill, no gate).
Applied live it shows on the table, but the running seed's server doesn't know location 77: its check needs a new seed.

**The game side:** `slot_data` sends `free_ant_tunnels: true`; the mod reads every miner's price as 0 (the mod guide,
step 42).

**Tests:** `TestAntTunnels` (the four ways in, the key).

**Status:** works, seen on screen (2026-10-04): the Golden Way's miner asked "0 berries for this tunnel!" with none in
the bag, applied live; tests not run yet (batched before the push).

*Code: `logic/bugaria_city.py` (`TRANSFERS`), `slot_data.py` (`free_ant_tunnels`); tests `test_logic.py`.*

## Build step 46: Beette's sale, a free location

**Asked (the user, 2026-10-04):** "probly better to just make it free ? and give the key as a location ?". Beette sells
the Flower Key (the plaza's red house) on the balcony for 150 berries, from chapter 3's end (`MEASURED.md`, the Flower
Key). Now she is there from the start (build step 9), her sale is location 78, *Bee Kingdom Hive: Balcony, Beette's
Sale* (the name the user's), done at her next line's flag 228, and the Flower Key is in the pool as its vanilla item.

**Free:** a new `slot_data` key, `free_sales` (`[{"map", "lines"}]`, from each area's `FREE_SALES`, `FreeSale` in
`data_types.py`): her lines 20 (the offer, "150 berries for the house") and 21 (`checkmoney,150` / `money,-150`). The
mod makes the price 0 in both (the mod guide, step 43).

**The logic:** the location waits for the later chapters (`LATER_CHAPTERS`), as the hive's rooms aren't mapped. The
Flower Key is `useful` for now: nothing in the logic needs it until the red house's spots are locations, and then it
becomes progression (`TestClassifications` holds the two together).

**Tests:** `TestFlowerKeySeller` (Beette present; the sale free and a location with its give).

**Status:** built (2026-10-04); the free price undone by build step 76 (2026-10-10), Beette and her location kept.

*Code: `logic/bee_kingdom_hive.py`, `data/items.json` (Flower Key); tests `test_slot_data.py`.*

## Build step 47: Enemysanity, every map enemy a location

**Asked (the user, 2026-10-04):** "lets make a on/off enemy sanity yaml option ... so we can look at enemy
location/placements with the room-logic.md when adding new locations". Worked out first as Next 58 from the game's own
held-key drop (`MEASURED.md`, a map enemy's drops).

**The option:** *Enemysanity* (`enemy_sanity`, off), a location category (`enemy`) like the other toggles, sent in
`slot_data`'s `options` for Universal Tracker.

**The locations:** one per map enemy in `data/enemies.json` (325), built by `enemysanity.py`. The export
(`enemy-table.py`) now adds each enemy's area, its first enemy's game name, its story flags and a **location id that a
re-export keeps** (from 1000; a new enemy gets the next free one). **Names (the user's pattern):** "Area: Room, Enemy
N", the room as the map's own named spots call it, else the map's name until the room is named (a rename keeps the
id); N counts that enemy within the room. **Never missable (the user):** every one is kept present whatever its
`requires` or `limit` (25 the story removes, 27 it adds later; no two share a spot). **Logic:** each waits for the
later chapters (`LATER_CHAPTERS`) until its room is mapped (`room-logic.md`), the cautious stand-in.

**slot_data:** `location_enemies`, `{location id: "map:entity"}` (the key Enemy Shuffle uses); the mod guide's step
44 does the rest.

**Tests:** `test_enemysanity.py` (off: none; on: every map enemy, the names, the ids).

**Status:** built (2026-10-04), not yet seen in game.

*Code: `enemysanity.py`, `data_types.py` (`Encounter`, `Source.enemy`), `options.py` (`EnemySanity`), `slot_data.py`
(`location_enemies`), `dev-scripts/enemy-table.py`, `data/enemies.json`; tests `test_enemysanity.py`.*

## Build step 48: Extra Roadblocks, and a map split into areas

**Asked (the user, 2026-10-04):** the gate by Snakemouth Den that the game puts up from chapter 2 "requires dig to get
across. i think we should have it disabled by default but allow it to be added as an obstacle in the yaml, similar to
how pokemon emerald add the custom roadblocks". Seen on screen first: with flag 67 set for one visit and the seed's
removal lifted, the gate stood on `NearSnakemouth` with the guard and his sign beside it.

**How Emerald does it** (`worlds/pokemon_emerald/options.py`, 0.6.7, read 2026-10-04): an obstacle it adds is a toggle
of its own (*Extra Boulders*), the ones it removes one list (*Remove Roadblocks*). **Decided (the user):** one list for
the added ones, *Extra Roadblocks* (`extra_roadblocks`, an `OptionSet`), its first entry *Snakemouth Barrier*; there
from the start when chosen (a roadblock is held in one state, build step 9), the guard and sign beside it; off by
default, and then the way stays open all game (build step 9).

**The logic needs a map split in two.** Each map was one region, but the barrier cuts `NearSnakemouth`: the door toward
the cave (`loading zone cave`, to the corridor) behind it, everything else in front. A rule on that door covers going
in; coming back through it lands behind the barrier, and with shuffled doors that arrival can come from any door, which
no door rule says. So a map may now hold **areas** (`Area` in `data_types.py`, `MAP_AREAS` in its area's module): an
area is a region of its own (`"<map> (<name>)"`) holding its doors, joined to its map's region both ways by its rule.
`door_region` says which region a door stands in, and the regions, the entrance randomizer (its targets, plando, the
room swap) and Universal Tracker's replay all go by it. A new obstacle is then data: the map, the doors behind it, what
crosses it, and its pieces; reachability, the shuffled doors and the seed's proof follow.

**The rule** is `CanUse("Beetle Dig")` behind Rule Builder's own option filter (`OptionFilter(ExtraRoadblocks,
"Snakemouth Barrier", "contains")`, `filtered_resolution=True`): free when the roadblock isn't chosen. The option class
lives in `roadblock_options.py` (first `roadblocks.py`, renamed 2026-10-08: WebHost unpickles an option only from a
module whose name ends in "options", which Archipelago's `test_pickle_dumps_default` caught), since `options.py` reads
the data tables, which read the logic. **The pieces** (`Roadblock`): the gate and its door as scenery, the guard and
sign as entities; chosen, `slot_data` lists them in `scenery_present` and `kept_present`, otherwise in `scenery_hidden`
and `kept_open` as before. The mod needed no change.

**Checked:** `test_roadblocks.py` (off: pieces away, the crossing free; on: pieces standing, the crossing needing Dig
both ways, the cave door and its arrival behind the barrier; decoupled: the area reachable with everything), failing
without the change. Nine seeds with APQuest, the barrier on, doors off, coupled and decoupled: all generate; in one,
Beetle Dig in sphere 3 and the den's Artifact in sphere 4. **Owed:** the PopTracker pack's export (a new region and an
option filter with `contains`): done 2026-10-08, build step 56.

**The Snakemouth Barrier taken out** (the user, 2026-10-05, seen on screen with it up): the horn tutorial's scene
moves the party past the gate, so it never held. Asked whether to drop the option with it: "Keep it, empty", ready for
the next roadblock. `valid_keys` is empty, so Archipelago accepts any value and an old yaml naming the barrier
generates as before with nothing up. The gate, guard and sign are in `SCENERY_HIDDEN` and `KEPT_OPEN` for good, and the
`Cave Side` area is gone; `Area` and `Roadblock` stay. Test: `test_roadblocks.py` (a yaml naming it puts nothing up, no
`Cave Side` region).

**A one-way door landing in an area** (2026-10-06, `BugariaCommercial`): the underground bar's door up lands behind
the commercial district's grass, which takes the horn, but a one-way door always landed in its map's own region, so
the logic had it arrive on the open side. An `Area` now lists the one-way doors that land in it (`landings`), and
`landing_region` gives a one-way's landing region to the region builder and the entrance randomizer alike; a landing
named for an area of another map, or for a door that is no one-way, refuses to load. The door tests compare each
one-way with its landing region.

**Status:** built (2026-10-04); its one roadblock seen in game and taken out (2026-10-05), the option kept empty.

*Code: `roadblock_options.py` (`ExtraRoadblocks`), `data_types.py` (`Area`, `Roadblock`), `logic/outskirts.py`
(`ROADBLOCKS`, `MAP_AREAS`), `logic/__init__.py`, `data_tables.py` (`REGIONS`, `door_region`, `landing_region`),
`regions.py`, `entrances.py`, `slot_data.py`; tests `test_roadblocks.py`, `test_areas.py`, `test_doors.py`.*

## Build step 49: hidden items and dig spots, two location toggles

**Asked (the user, 2026-10-05, mapping rooms):** "a yaml option to not include invisible items (things inside a
bush/grass) similar to how pokemon emerald has hidden items in/out of logic in the yaml". **How Emerald does it**
(`worlds/pokemon_emerald/options.py` and `__init__.py`, 0.6.7, read 2026-10-05): *Randomize Hidden Items*, a `Toggle`
off by default that adds the hidden items' location category; off, they're the game's own.

**Decided (the user):** hidden is "anything you can't visually see": "like hidden inside grass, or inside a thing you
need to hit for the item to appear"; dig spots are visible, so "a 2nd on/off for dig spots specifically"; both off, as
Emerald. Two categories in `CATEGORY_OPTIONS`, as the others: `hidden_item` (*Shuffle Hidden Items*,
`shuffle_hidden_items`) and `dig_spot` (*Shuffle Dig Spots*, `shuffle_dig_spots`), both sent in `slot_data`'s
`options`. Off, a category's locations aren't made and their pickups aren't in `location_pickups`, so the mod leaves
them to the game; the mod needed no change. The first ones: locations 12, 80 and 81 (grass; 80 and 81 respawn, a
location the first time) and 25 (a boulder) hidden, 79 a dig spot; more join as rooms are mapped (the dig spots:
build step 10). Test: `test_categories.py` (`TestHiddenAndDigOff`, `TestHiddenAndDigOn`).

**Status:** built (2026-10-05), not yet seen in game.

*Code: `options.py` (`ShuffleHiddenItems`, `ShuffleDigSpots`, `CATEGORY_OPTIONS`), `slot_data.py` (`SLOT_OPTIONS`),
`logic/outskirts.py`; tests `test_categories.py`.*

## Build step 50: the Termacade, its gift and prize stand

**Asked and decided (the user, 2026-10-06; Next 60):** the greeter's 15 tokens a location, "a filler location", with
the tokens an item "similar to how we do it for the 15 & 30 berries", always in; and "everything that is in the token
prize shop to be in the pool. but the token shop itself should only be able to contain filler items", behind its own
toggle, on by default. Names, the user's: *Bugaria City: Termacade, Arcade Gift* and *Prize 1* to *Prize 13* in the
stand's order. The ribbons, whose use isn't known yet, useful until the quest pass.

**How the game does it** (`MEASURED.md`, the commercial district): the token count is `flagvar[27]`. The greeter's line
sets variable 1 to 15 and runs `giveitem,1,110`, the Game Tokens key item, whose own item text carries
`|addvar,27,v1|`: giving the item adds the tokens. The arcade's score rewards give the same item on the same map. The
prize stand is `Data/Termacade` (`termacadeprize`: kind, item, price, once only, its flag), listed by `ShowItemList`'s
list 26 and sold by Event121, which takes the tokens, sets a once-only prize's flag and then runs `giveitem` with the
prize. Thirteen prizes (the dev `QuestDump` now writes them to `bugfablesap-termacade.tsv`): four items sold again and
again, five medals, three ribbons and Helper Boost once each (flags 310-314, 590-592, 718).

**Built:**
1. **A token kind** (7, ids from 6000; `15 Tokens`, filler), given by the receiver into `flagvar[27]`.
2. **The gift**, location 97: `Source(flag=351, npc="termiteoutside", give=..., tokens=15)`; `tokens` makes its vanilla
   item the token item, and `npc` goes into its `location_gives` entry, so only the greeter's `giveitem` is swapped,
   never a score reward's.
3. **The prizes**, locations 98-110, category `termacade` (*Shuffle Termacade*, `shuffle_termacade`): `Source(prize=row,
   give=...)`, with the flag where the prize has one; slot_data `location_prizes` (`{location: row}`).
4. **Kept to filler:** a location's `filler` marks it `LocationProgressType.EXCLUDED`, Archipelago's own way (`world
   api.md`: "will prevent progression and useful items from being placed at excluded locations"), the gift and every
   prize.
5. **The new items:** Bag of Flour and Spicy Fries (filler), the five medals and Helper Boost (useful), the three
   ribbons (useful for now).
6. **Tests** (`test_termacade.py`): the gift and prizes excluded and holding excludable items, the prizes' items and
   15 Tokens in the pool, `location_prizes` and the gift's give with its npc; off, the gift alone and no prize items.

**Seen** (2026-10-06, a new seed adopted by the test file): the list showed each prize's seed item and its
description; a flag-less prize bought went back to the stand's own; a once-only one bought showed Sold Out. A bug seen
there: Prize 9 showed Prize 4's seed item, because Prize 4's own item (the Tangy Berry) is Prize 9's seed item and its
name had already been swapped; every look is now read before any is swapped, and the row then showed right.

**The gift seen** (2026-10-06): with flag 351 cleared, the greeter's line showed the seed's Burly Berry and gave no
tokens. **A count left behind** (2026-10-07, the user): a Crystal Berry there read "You got 15 Crystal Berry!": Game
Tokens' article is the count, and a crystal berry, berries or tokens kept the give's own article. Now a crystal berry
takes the game's usual one (`menutext[125]`, set before every give) and berries and tokens none (their names carry the
number). **Seen:** "You got a Crystal Berry!". The same line records the Termacade discovery (42), a location of its
own once discoveries are swept.

**Status:** built (2026-10-06); the prize stand and the gift seen working.

*Code: `options.py` (`ShuffleTermacade`), `data_types.py` (`Source.tokens`, `Source.prize`, `Location.filler`),
`data_tables.py` (`TOKEN_KIND`, `vanilla_item`), `rules.py`, `slot_data.py` (`location_prizes`, a give's `npc`),
`logic/bugaria_city.py`, `data/items.json`; tests `test_termacade.py`. The mod: the guide's step 45.*

## Build step 51: Minigame Prizes, and Wacka Worm only for Vi

**Asked and decided (the user, 2026-10-07, mapping `GoldenSMinigame`):** the Wacka Worm keeper and the festival's
mayor stay, but the game doesn't start "if Vi is not present/obtained yet"; Vi and the Beemerang are the logic for both.
The keeper's 25-worm prize is a location, *Golden Path: Whack Farms, Minigame Prize* (the user's name). After a first
try of 16 worms: 25 in the minute is hard, so a yaml option, off by default, under which the location only holds
filler. **A plan, not built** (the user): the same toggle could later cover the arcade's and the card game's prizes, one
minigame/arcade/card game option, off by default.

**How the game does it** (`MEASURED.md`, the Golden Path rooms): both games are `Event54` (the keeper's line 4 after
`|money,-10|`, the mayor's line 37), Vi alone (`ChangeParty({0})`) throwing the Beemerang at the worms (`WackaWorm`
reads `player.beemerang`). `Event55` ends it: at the festival it teaches Beemerang Halt (flag 21) after the game, so
Halt is never needed to play; at Whack Farms, 25 worms the first time gives a Heart Berry (`giveitem,1,83`, the key
item pocket, flag 305), 30 the "MOREFARM" code and berries, and the mayor's late visit the Desert Key (quest 46, flag
559, for the quest pass).

**Built:**
1. **The location**, 143: `Source(event=55, flag=305, give=Give(map="GoldenSMinigame", type=1, item=83))`, rule
   `CanUse("Beemerang Toss")` (which holds Vi when members are items), category `minigame`. The Heart Berry joins
   `items.json` (useful). The festival's location (68) gets the same rule.
2. **Minigame Prizes** (`minigame_prizes`, off): off, every `minigame` location is `LocationProgressType.EXCLUDED`, as
   the Termacade's are (step 50); the location stays either way.
3. **The mod refuses the game** (`FieldMoves.cs`, a prefix on `StartEvent(54)`): without Vi in the party or with the
   Beemerang locked, the scene doesn't start, and at Whack Farms the 10 berries its line already took are given back
   (`[moves] Wacka Worm refused ...`). The line's `|event|` leaves `minipause` and `overridefollower` for the scene
   to clear and skips `EndOfMessage`; with no scene the player stood frozen (seen), so the mod does both once the
   message closes.

**Seen (2026-10-07):** without Vi, the keeper's game refused, the fee back, the player free after.

**Status:** built (2026-10-07); the game played without a freeze and the refusal seen; the prize not yet seen.

*Code: `options.py` (`MinigamePrizes`), `rules.py`, `logic/golden_path.py`, `logic/golden_settlement.py`,
`data/items.json`. The mod: `FieldMoves.cs` (`WormGame`).*

## Build step 52: the festival night at will, a switch NPC

**Asked and decided (the user, 2026-10-07, mapping `GoldenSettlement1`):** the Golden Settlement's festival night is a
window in the game (flag 85, from the nightfall scene, until the fight's 86), with about 30 NPCs of its own. One logic
must hold for normal play and the entrance randomizer, so the night is kept open for good: day and night switch "at
will", by an NPC in each of the three rooms (the user: one per room feels better with a shuffled entrance than one
room to find), the same NPC everywhere, with Aria's red "!", and only that one choice. In the square that NPC is Aria
herself (before the festival her only talk is the nightfall prompt), moved off the arena (Jump) to the ground in front
of it, with Leif and Celia and the arrival scene moved down beside her at the distances they had; the square open by
night as by day; the dungeon's way open from the start (the statue).

**How the game does it** (`MEASURED.md`, the festival's day and night): a night map has its day map's entities and only
its own scenery; flags 85 and 86 decide who is there, read only through data (entity requires and limits, three NPCs'
lines, one wall), and `Event52` (the nightfall) only sets 85 and loads the night square. Flag 86 also reaches beyond the
settlement, so it is never touched.

**Built:**
1. **The mod's own night** (`DayNight.cs`, slot_data `day_night`: `[{"day", "night", "from", "until", "first_event"}]`):
   on those maps, each `CheckIfCanExist` sees flag 85 set and 86 clear at night, and by day both as whether the
   festival is over; the save's values come back in a finalizer. `LoadMap` loads the version the night asks for, so
   every door, warp and scene lands right. Not saved: a session starts as the story has it.
2. **The switch** (`time_switches`: `[{"map", "entity", "at", "day", "night"}]`): the entity is kept present and moved
   to `at`, and its talk is the game's prompt shape with the seed's words. The user's: by day Aria's own line ("Oh? Are
   you here for the festival? It should start as soon as the sun sets." Keep exploring. / Wait for nightfall.), at night
   "Oh? Are you enjoying the festival? It should end at daybreak." Keep exploring. / Wait for dawn. Its yes runs the
   nightfall event: the story's own scene the first time (its speech, discovery 13, flag 85), the mod's swap after (a
   fade, the other version with the party where it stood, the line's end done).
3. **Entities moved** (`entities_moved`: `[{"map", "entity", "at"}]`) and **a scene's fixed camera point**
   (`scene_cameras`: `[{"map", "event", "from", "to"}]`, replaced while the scene runs): the square's arrival scene
   (`Event51`, its trigger, Leif and Celia) beside the switch Aria.
4. **Scenery moved** (`scenery_moved`: `[{"map", "entity", "local"}]`): the night square's statue, which has no flag,
   slid aside as the fight's scene slides it, so the dungeon's door never lands behind it.
5. **The square open at night:** its south door kept present and the `blocker` trigger kept away. A night map's entity
   lists are read under its day map's name (`DayNight.EntityMap`), so the seed names each entity once.
6. **Scenery switched off** (`scenery_off`, step 53's night gate): a night map's own copy of something its day map
   hides by flag.
7. **One door per exit:** each exit between the three rooms is a door entity per festival state on one spot; the one
   before the festival is kept present in every state and the nine night and after-festival copies kept away, so an
   exit is always the same door and the night only picks the version of the room it lands in (the user: three rooms,
   the night acting only within them; one time of day for all three).
8. **The farm's and the houses' switches** are the square's Aria copied in (`time_switches`' `"copy"`): her entity
   data row and name added to the room's as `CreateEntities` reads them (a transpiler after its two splits), so the
   game builds her like any other entity. Spots tried live with the dev `switchhere`, then written in.

9. **The first nightfall away from the square** (`day_night`'s `first_map`, `first_discovery`, `skips`): from the farm
   or the houses the night begins in place, by the quick swap, with what the story's scene leaves behind (flag 85,
   discovery 13 by the game's `UpdateJounal`) but not its speech, which only the square's first nightfall plays (the
   user: not taken to the square). The square's arrival scene (flag 84), if not played yet, is marked done then: it
   had come up at night after a first nightfall in the farm (the user chose it skipped over day-only).

**To do:** the door table (`door-graph.py`) marks the three rooms' links "fixed" (never shuffled), as their copies
stand on one spot; with one door per exit, a group of copies there becomes one door, as for one-ways, then the table
regenerated and the shuffle tested. The logic: the night maps fold into their three day rooms, room by room. **The
third state, after the festival** (the user, 2026-10-08: check it later on): from flag 86 the three rooms show their
after-festival versions (other copies of doors and people, Bomby's hat, Tanjerin's quest, the caravan's later stall);
each room to be checked in that state too, as the day and the night were.

**Seen (2026-10-07):** the switch Aria on the ground with her "!"; the arrival scene at ground level after its trigger
and camera moved; the first nightfall the story's scene, then day and night again by the quick swap; the dungeon's way
open by day, both ways; the farm's copied Aria at her spot by night and day; the square's bottom door and the dungeon's
way by day and by night, the houses in at night and back by day, the farm both ways at night and back by day. On new
saves, the first nightfall in each room: the square's the story's scene, the farm's and the houses' in place with no
speech and discovery 13 recorded; the farm's night scene from both entrances and after the switch.

**Status:** in progress (2026-10-07): the three switches and the one-door exits built and seen; the houses' spot by day,
the door table and the logic to come.

*Code: `data_types.py` (`DayNight`, `TimeSwitch`, `EntityMove`, `SceneCamera`, `SceneryMove`),
`logic/golden_settlement.py`, `slot_data.py`. The mod: `DayNight.cs`, `KeptOpen.cs` (`EntityMap`), `SeedData.cs`.*

## Build step 53: the festival's offerings as items, the contest always won

**Asked and decided (the user, 2026-10-07, mapping `GoldenSettlement2`):** the festival's games are locations now, not
in the quest pass: the Wacka Worm game's prize (the Sun Offering; Vi and the Beemerang, "a required minigame in the
base game, not an optional/hard one"), the eating contest's (the Moon Offering; the square at night first, to talk to
Zasp), Chubee's gift after it (the Weak Stomach medal; first planned for the caravan's stall from the start, then the
user: "just make it be Chubee's Gift (Night)"), the windmill's crystal berry (#4, the Halt's crank) and the farmer's
reward for opening it (a Hard Seed at night, the farmer there whenever it is night). The contest always won (the
user's pick over "a loss still gives it"). The farm's night scene (Smugbee, Kabbu's Pep Talk, which stays the game's:
battle skills are not items) a location too. Names, the user's: *Farm, Wacka Worm Prize (Night)*, *Farm, Eating
Contest (Night)*, *Chubee's Gift (Night)*, *Farm, Windmill*, *Farm, Farmer's Reward (Night)*, *Farm, Smugbee (Night)*,
and the Halt lesson renamed *Farm, Wacka Worm Game (Night)*.

**How the game does it** (`MEASURED.md`, the farm): the Wacka Worm win sets flag 96 and gives the Sun Offering (line
45); the contest (`Event57`) decides `won = b <= 0f`; won, the prize is the Moon Offering (line 67, flags 100 and 101)
and Chubee gives Weak Stomach (line 79's `checkflag,102,80`: while 102 is off; line 80 sets it); lost, Chubee takes a
Berry Juice for the offering (line 77). A skipped gift is sold at the caravan after the fight (`Event58`, prize slot 1).
The offerings are what the Golden Hills dungeon's shrines take.

**Built:**
1. **The locations** (152-156; 151 the night scene) in `logic/golden_settlement.py`; the contest's needs a story event
   in the square, *Zasp's Challenge* (flag 93, `Eating Contest Entered`).
2. **The Sun and Moon Offerings** are items (progression): the dungeon's shrine rules, a later-chapters stand-in until
   now, need them (`logic/golden_hills.py`). Weak Stomach is an item too, useful: every medal is (the user: a drawback
   has uses, Weak Stomach feeds the poison medals), Hard Mode too since 2026-10-08 (filler until then, as it does
   nothing in a seed); `TestKeyItemsAndMedalsAreUseful` holds it.
3. **The contest always won** (slot_data `contest_always_won`; `Festival.cs`, a transpiler in `Event57` passing `won`
   through `ForceWin`).

4. **The farm's power plant door open** (the user): kept present by day and by night, its day gate hidden by flag and
   the night map's own gate, which has no flag, switched off on load (slot_data `scenery_off`: `[{"map", "entity"}]`).

**Seen (2026-10-07, a new save):** the Smugbee scene's item with its box; the Wacka Worm prize (the seed's item for
the Sun Offering) and the Halt lesson's check; the contest lost on purpose, counted as won (Leif declared the
winner), its prize the seed's item; Chubee's gift in one box; the farmer's reward; the windmill berry; the power plant
door both ways at night.

**Status:** built (2026-10-07), seen.

*Code: `logic/golden_settlement.py`, `logic/golden_hills.py`, `data/items.json`, `slot_data.py`. The mod: `Festival.cs`,
`SeedData.cs`.*

## Build step 54: Riz always offers his fight

**Asked and decided (the user, 2026-10-08, mapping `FarGrasslandsOutsideVillage`):** Riz guards the Fishing Village's
door until his fight is won (flag 509). With Maki following he only turns the party back, and with the swamp bridge
kept up (Known issues) Maki never leaves, so the village would stay closed for good. Weighed: letting the party pass
with no fight, a third choice (fight with or without Maki), Maki leaving as the scene starts. The user's pick: Riz
always offers the fight, and Maki helps if he follows ("easier/less complicated"); then, since Maki helps, Riz gets
more HP (the user's pick of three: more HP only).

**How the game does it** (`MEASURED.md`, outside the village): `Event176` (trigger `rizevent`, until 509) asks
`MainManager.HasFollower(Maki)` first, before its first pause: yes, Riz's "Halt!" and the party turns back; no, from
the left Riz's exchange and a choice (`option == 0` fights), from the village side the fight at once. The fight is
enemy 97 (`Fisherman`, 75 HP, Ground, no Flip, no summons), the mini-boss music; won, flag 509. `EventControl.StartEvent`
starts a scene with `StartCoroutine("Event" + id)`, which runs it up to its first pause on the spot.

**Built:**
1. **The follower check answered "no"** for that one scene (slot_data `riz_fight_with_follower`; `RizFight.cs`): a
   prefix on `StartEvent` for 176 on this map sets a flag that a prefix on `HasFollower` reads (Maki only), cleared
   in `StartEvent`'s postfix. Logged: `[riz] Riz's scene, Maki following: the fight offered…`.
2. **Riz's HP x1.6 with Maki**, on top of enemy scaling (a `GetEnemyData` postfix at low priority, after scaling's),
   never above his vanilla 75 (`enemydata` column 1) and never below what scaling gave (the user: "so it can't inf
   scale even past the intended difficulty of the vanilla game").
3. **Maki's hits scaled** with enemy scaling (`documentation.md`, step 17), the user's pick over a bigger Riz bonus:
   unscaled, his fixed 6 outclassed every scaled enemy in his two areas.
4. **The logic:** the village door is the room's own part, Jump both ways; the fight in the way is won with plain
   attacks (`logic/far_grasslands.py`).

**Seen (2026-10-08):** with Maki following, from the village side, the fight at once, Riz at 27 then 32 HP (17 and 20
scaled, x1.6), Maki's hit 2 with the dev `onehit` off; before the seed data was loaded, the game's turn-back.

**Status:** built (2026-10-08), seen.

*Code: `logic/far_grasslands.py`, `slot_data.py`. The mod: `RizFight.cs`, `SeedData.cs`, `Plugin.cs`,
`EnemyScaling.cs`.*

## Build step 55: Universal Tracker's deferred entrances, shuffled doors hidden until taken

**Why:** with the doors shuffled, Universal Tracker rebuilds the seed from `door_targets` (build step 40), so it knows
every door: its list and `/get_logical_path` would show where doors lead before the player has been through them, the
layout spoiled. Its deferred entrances (`docs/apworld-integration.md`, "Deferred Entrances", and `map-integration.md`,
v0.3.4) keep each shuffled door unconnected until the client reports it taken.

**Decided (the user, 2026-10-06):** hidden by default, as TUNIC does (`worlds/tunic/__init__.py`, `connect_entrances`,
0.6.8: deferred on `"on"` or `"default"`). The player's switch is Universal Tracker's own `enforce_deferred_entrances`
under `universal_tracker:` in its `host.yaml` (its `setup.md`; `"off"` shows every door), so no option of ours. This
replaced 2026-10-01's "no" (build step 41).

**How it was built:**

1. **One key, shared with the PopTracker pack:** `bug_fables_doors_{team}_{slot}`, a list of entrance names, the
   contract in the pack's `PLAN.md` (its doors key). Universal Tracker fills in `{team}` and `{player}`
   (`TrackerClient.py`: `key.format(player=…, team=…)`), asks with `Get` and `SetNotify`, and calls the world's
   `reconnect_found_entrances(key, value)` at connect and at each change. One key for every door, as its
   `map-integration.md` example has (`EntrancesToReconnect`), rather than TUNIC's key per door.
2. **The apworld** (`universal_tracker.py`): once `replay` has connected the seed's doors, `defer_doors` disconnects
   each shuffled entrance (every door in `door_pairings`, one-ways included) when
   `multiworld.enforce_deferred_connections` is `on` or `default`, keeps its region, and sets
   `found_entrances_datastorage_key`. `reconnect_doors` connects each named door again, and with Coupled and Room Swap
   its way back too (a door walked through comes out of its partner, whose door leads back); never with Decoupled, nor
   for a one-way. A one-way's story copy counts as its door. Anything in the key but a list of names is ignored.
   Universal Tracker stops after `generate_basic` and sweeps with `allow_partial_entrances` while it defers
   (`TrackerCore.py`), and nothing of ours sweeps before that. Its fuzzer hook turns deferral off, so the hook still
   compares every sphere (development.md, Fuzzing the apworld, Universal Tracker's fuzzer hook, item 4).
3. **The mod** (`DoorShuffle.cs`, `ApConnection.cs`): a prefix on `MainManager.TransferMap`, whose `caller` is the door
   walked through, names it as the apworld does (`Map: door`, or `name#row` where its map has two doors of that name)
   while the doors are shuffled in a seed; a failure there only logs (`[doors] … taken but not recorded`), never
   stopping the door. `ApConnection.DoorTaken` adds it with one `Set` carrying its default `[]` and an `update`
   (which adds names not yet in the list; `network protocol.md`, Set), off the game thread, whole as Archipelago's own
   clients send a `Set`. **Why not MultiClient.Net's `DataStorage`** (its source at v6.7.1, read 2026-10-08, corrected
   from that day's first reading): it can send an `update` with a list (`operator +` with an `OperationSpecification`,
   `DataStorageElement.cs`), but never a default with it (`DataStorageHelper.SetValue`), and an `update` on a key not
   there yet makes the server raise and drop the client (`MultiServer.py`, `update_container_unique` and the Set
   handler, 0.6.8). Its way to a default, `Initialize`, is a second packet, and on net40 two sends aren't guaranteed to
   stay in order (its socket helper calls websocket-sharp's `SendAsync` per packet). So the list is the library's own
   `SetPacket` through its `Socket`, which its `helpers.md` and `packets.md` allow, in the one-`Set` form Archipelago's
   own `UndertaleClient.py` sends. Each login sends the whole list again (`update` adds only
   what's missing), and another room, team or slot starts from none, as Archipelago's CommonClient tells sessions
   apart since 0.6.8. A flat list of strings, well inside the server's JSON depth limit of 16 for what clients send
   (`NetUtils.decode`, 0.6.8). **The key's form**, `<game>_<what>_{team}_{slot}`, is the one several of Archipelago's
   own clients use (`pokemon_emerald`, `cvcotm`, `smw`) and the server's own `hints_{team}_{slot}` shape; the
   library's `Scope.Slot` (`Slot:{slot}:key`) leaves out the team, and slots repeat across teams.
4. **Tests** (`test_deferred_doors.py`): a rebuilt Coupled seed with deferral on has every shuffled door unconnected and
   nothing else changed; Universal Tracker's partial sweep still runs; a door taken comes back with its way back, and
   every door taken gives back the seed's own graph and locations; Decoupled brings back only the door taken; a
   one-way's copy counts as its door; `off`, a seed without shuffled doors and a real generation defer nothing; junk in
   the key is ignored.

**Status:** built (2026-10-08). Not yet seen: Universal Tracker showing a shuffled door only once it is walked through,
and the mod's `[doors] taken, sent to …` line.

*Code: `universal_tracker.py` (`defer_doors`, `reconnect_doors`, `DOORS_TAKEN_KEY`), `world.py`
(`reconnect_found_entrances`); test `test_deferred_doors.py`. The mod: `DoorShuffle.cs` (`Taken`, `NameOf`),
`ApConnection.cs` (`DoorTaken`, `SendStored`).*

## Build step 56: the PopTracker pack's export at world 0.3.0

**Why:** the pack's logic was exported at `a77eeae` (world 0.2.0, 75 locations); the world now has 498, maps split
into areas (build step 48) and rules the export didn't know (`CanReachRegion`, `ItemOnHand`), so run at HEAD it stopped.
Build step 48 had owed it.

**Decided (the user, 2026-10-08):** the export keeps reading the randomizer's checkout (recording its commit and
`world_version`) until the next release, then pins that release.

**How it was built:**

1. **`ItemOnHand` serialized as what it stands for** (`custom_rules.py`): its `to_dict` writes the `Or` of the item
   shops' `CanReachRegion`s, the way `rule builder.md` ("Custom serialization") lets a custom rule pick its format, so
   the pack reads only Archipelago's built-in rules; `rule_from_dict` reads it back as that `Or`.
2. **The export** (the pack's `tools/export.py`): `CanReachRegion` supported; locations and story events in their map
   area's region (`Area.region`), each still a section of its map's pin; a door's partner and a one-way's landing as
   the region `entrances.py` connects them to (`door_region`, `landing_region`); option filters `contains` and `in`
   (`rule_builder/options.py`, 0.6.8), each checked against the kind of option the Lua compares it for; option sets'
   defaults as sorted lists, as slot_data sends them; the pending quest left out, as `world.py` leaves it out of
   every seed; `world_version` from core. A rule, region or door the Lua can't follow stops the export. Doors named
   `name#row` (two of one name in a map) are drawn too.
3. **The Lua** (`scripts/logic.lua`): `CanReachRegion` read against the regions the sweep has reached, so a false
   answer is asked again on its next pass, as Archipelago rechecks such an entrance.
4. **Tests** (the pack's `tests/test_parity.py`): besides the locations, the regions reached and every entrance
   Archipelago made, door by door, each door mode as often as the others, with Extra Roadblocks and each filter kind
   rolled. Each new piece broken on purpose once (partners as bare maps, `contains` always true, `CanReachRegion`
   always true): each was caught.

**Status:** built (2026-10-08): the export at `31c1ad9`, world 0.3.0, 498 locations in 151 rooms of 22 areas; the
pack's tests pass (200 seeds, 112178 subtests) and pack-checker; this repo's `ItemOnHand` test written, run with the
suite at the next push. Not yet seen in PopTracker: the new rooms' pins and tabs.

*Code: `custom_rules.py` (`ItemOnHand.to_dict`); test `test_mapped_rooms.py` (`test_serialized_as_the_item_shops`).
The pack: `tools/export.py`, `scripts/logic.lua`, `tests/test_parity.py`.*

## Build step 57: the maps visited and the map the player is on, for the trackers

**Why:** the PopTracker pack's fog of war needs the maps visited (its `PLAN.md`, item 6), and its following the player
(item 9) and Universal Tracker's auto tabbing and position icon (build step 41) need the map the player is on. A
client tells trackers such things through data storage. Archipelago has no standard key for either; the map now has
precedents among its own clients (0.6.8): `smw`'s current level, `UndertaleClient.py`'s room per slot, TUNIC's
current map. A list added to with `update` is the protocol's own form (`network protocol.md`, Set).

**How it was built:**

1. **Two keys,** the contract in the pack's `PLAN.md`: `bug_fables_visited_{team}_{slot}`, a list of map names
   (`MapControl.mapid`, as the apworld names its maps), added to with the same one `Set` as the doors (build step 55);
   and `bug_fables_map_{team}_{slot}`, the map now, a string replaced at each load through the library's own
   `DataStorage[key] = map` (its `datastore.md`), one write at a time, always the newest. In every seed while
   Archipelago is enabled, not only with the doors shuffled.
2. **The hook** (`MapTracking.cs`): a postfix on `MapControl.CreateEntities`, which each map runs once as it's built
   (`MapControl.Start`); the title screen is a scene of its own, so only a file's maps count. A failure only logs
   (`[map] … loaded but not recorded`), never stopping the map. A save tied to another seed adds nothing to the slot's
   keys (`ItemReceiver.SaveMatchesSeed`, as checks and shops are gated; the doors too since 2026-10-08).
3. **Shared with the doors** (`ApConnection.cs`): one helper sends both lists; each login sends them whole again;
   another room, team or slot starts empty; the map is written again after each login. The library keeps a write's
   result to itself, so a lost one waits for the next login, and it doesn't promise two writes reach the server in
   order (each is held only by its own ping): a rare stale map lasts until the next load.
4. **Checked against Archipelago and MultiClient.Net** (2026-10-08, the user: "do a double check on both the
   Archipelago & Archipelago/MultiClient.Net repo's, to make sure we are following/doing what they are doing"): a
   read-only check of each one's source, then a skeptic on each. Changed on it: the map through the library's
   `DataStorage` (first written as a hand-built `SetPacket`), the goal through its `SetGoalAchieved()` (build step 3),
   sessions told apart by seed, team and slot, another seed's save kept out of the keys, and build step 55's reason
   for its one raw `Set` corrected. Kept, each
   for a reason the library's source gives: the raw `Set` for the lists (build step 55); the liveness `Get` (the
   library's `GetRaceModeAsync` reuses a pending request and hides a failed send); the reflection into its socket (no
   close for a dead connection, no public scheme or compression).

**Status:** built (2026-10-08), the mod builds. Not yet seen: the mod's `[visited] new, sent to …` and `[map] now …`
lines, and the keys on the server.

*Code: the mod: `MapTracking.cs`, `ApConnection.cs` (`MapLoaded`, `SendStored`, `SendMap`), `Plugin.cs`.*

## Build step 58: the PopTracker pack's Archipelago interface

**Why:** the rest of what PopTracker's Archipelago interface offers (`doc/AUTOTRACKING.md`), every part of it (the
user, 2026-10-05: "support them all"): the pack's `PLAN.md`, items 7, 10 and 15-21. Read first at PopTracker's master
`d2af7f1` (2026-10-07): its docs, its `CHANGELOG.md`, its `src/ap/` and its own `examples/ap-storage-example`.

**How it was built** (the pack's `scripts/autotracking.lua`):

1. **Data storage, PopTracker's way:** from the Clear handler, `SetNotify` then `Get` on the mod's three keys and
   Archipelago's `_read_client_status_{team}_{slot}` and `_read_hints_{team}_{slot}`, one handler for both answers
   (`Retrieved`, `SetReply`), each value's type checked, as its `ap-storage-example` does. The server notifies a
   `_read_` key's subscribers too (`MultiServer.py`, 0.6.8), and PopTracker passes every key on (`aptracker.h`).
2. **The goal:** a Goal item at the end of the grid, lit when the status reads PopTracker's
   `Archipelago.ClientStatus.GOAL`.
3. **Hints:** each of this slot's hinted checks takes the `Highlight` of its hint's status; PopTracker's states are
   Archipelago's `HintStatus` (Unspecified, NoPriority, Avoid, Priority; found is None).
4. **A DeathLink:** "DL" in red on the party's items for 10 seconds (`BadgeText`, `ScriptHost:AddOnFrameHandler`), a
   first look for the user to pick on screen.
5. **A new seed, team or slot** resets what the pack keeps from data storage, as Archipelago's CommonClient tells
   sessions apart.
6. **The doors:** each taken door's destination worked out in Lua from `door_targets`; showing it waits for a pick
   on screen (sections are fixed, and a pin has no text of its own).
7. **Not built, each for a reason:** the connect replay in one go (PopTracker defers logic updates for `ap` packs
   itself since 0.31.0, its `CHANGELOG.md`, so `BulkUpdate` would add nothing); whose item a hint is (no place on
   screen yet). `min_poptracker_version` 0.33.0, for `Archipelago.Seed`.
8. **Tests** (the pack's `tests/test_autotracking.py`): the Lua with PopTracker's `Archipelago`, `Tracker`,
   `ScriptHost` and `Highlight` stood in for as its docs describe them: the keys asked at connect, checks right at
   connect, junk values, a new seed, every door's destination against generated Coupled, Room Swap and Decoupled
   seeds, the goal, hints, a DeathLink and its timeout.

**Status:** built (2026-10-08), the pack's tests pass (200 seeds, 112178 subtests) and pack-checker. Not yet seen in
PopTracker: any of it.

*Code: the pack: `scripts/autotracking.lua`, `scripts/logic.lua` (`bf_door_led_to`), `tools/export.py` (the Goal
item, `region_maps`, `one_way_copies`), `tests/test_autotracking.py`, `manifest.json`.*

## Build step 59: the wizard's tower open, its fall scene kept away

**Asked and decided (the user, 2026-10-08, mapping `WizardTowerBasement`):** "does the spider set any flags here that
affect other things? or can the cutscene be skipped?" A sweep of the game code, the dumps and the mod, each claim then
checked (`MEASURED.md`, the tower's flags), found:
- the fall's scene (`Event166`) sets only 449, and 449 only decides four things: the hole's trigger, the hole's door,
  the front door outside and the basement's wizard;
- so a file inside the tower without the fall (a random start, a shuffled door) leaves by the front door once the
  attic wizard unlocks it, but can't come back that way. The user saw it so;
- with shuffled doors, the first drop lands in the basement whatever the shuffle says, since the scene loads it itself.

Weighed: the front door made from the wizard's unlock instead of the fall; the scene played from both of the basement's
ways in, unlocking the door too. The user's pick: "just remove/skip the basement cutscene with the spider and then
always have the tower door be open both in/out", and "also hide/remove the spider from the basement".

**Built** (the apworld only: the mod's own lists do it, as for the Wasp Kingdom door outside the swamp; vanilla is
untouched, since the lists apply only in a seed):
1. **Kept away** (`kept_open`): the hole's trigger (`basementevent`) and the basement's wizard.
2. **Kept present** (`kept_present`): the hole's door down (`loadzonebasement`) and the front door outside
   (`loadzonetower`). The hole is then a plain drop with no scene, and goes where a door shuffle sends it.
3. **Hidden** (`scenery_hidden`): the front door's model outside (`Base/Tower/Door`) and its lock on the stairs
   (`Base/DoorLock`).
4. **The logic:** the front door free both ways, its two door rules and the "Wizard Tower Door Unlocked" event gone;
   the hole still the horn, one way in. A random start may now land outside the front door (`ROOM_STARTS` takes
   kept-present doors). Test `TestWizardTower`.

5. **The attic wizard's door talk skipped** (`dialogue_flags`): seen still there in the first seed (the user: "the
   spider has its first dialouge at the top still (about opening the door)"). That talk (his line 3, the door locked,
   then opened) only sets 450, now of no use, so his line from 450 answers to 691 (set by every new game) and he opens
   with his ingredients talk, which gives quest 52 (for the quest pass). The user's pick over leaving it; that talk's
   "You're STILL HERE!?" stays, since the game has no line for a first meeting there.

**Status:** built (2026-10-08); seen the same day in seed `AP_72680396662085838756`: no scene and no wizard in the
basement, the front door both ways; then, through the dev `liveslot` and with 450 cleared, the wizard opening with his
ingredients talk ("yee it skips the door dialouge now").

*Code: `logic/far_grasslands.py` (`KEPT_OPEN`, `KEPT_PRESENT`, `SCENERY_HIDDEN`, `DIALOGUE_FLAGS`). The mod:
`World/KeptOpen.cs`, unchanged.*

## Build step 60: the goal guard, a goal flag set only by its own events

**Found (seen 2026-10-08):** in the lily pad pond (`Swamplands2`) the user cut grass and the mod sent the goal. The seed
needed one artifact, and the grass had set flag 41, the first boss's artifact flag: the game's developers gave 41 to 25
entities in later rooms as a flag always set by then. In vanilla that changes nothing; in a seed those rooms can come
before the first boss. The user: "we should make sure artifacts, or any other goals can not be accidently triggered in
wrong or different ways. ever", and "guard/guarantee they can't trigger where we don't intend them to", for every goal,
future ones too.

**Checked:** every way each of the seven artifact flags can be set: two readers, then four checkers and a critic over
their findings, from the game's code, the dumps and a read-only scan of the game's data file (`MEASURED.md`, "The goal
flags"). Only flag 41 is borrowed, and only by those 25 entities: 12 grass set it when cut (three through the item they
drop), and 13 hidden switches, a plate and lights-out switches read it as "on" (the moving platforms of five rooms stop
without it). Otherwise each artifact flag is set only by its own events. Two more holes turned up:
- the mod counted all seven artifacts while the logic holds only the first, so a random start beside a later artifact's
  scene could send the goal with no wrong write at all;
- a new file started with the secret codes (key 9 on the file select) wasn't held back before the seed was known, so
  the seed's tables could be missing while it ran.

**Built:**
1. **Borrowed flags repointed** (the apworld's `activation_flags` and `limit_flags`, in each area's module; the mod
   applies them as each map is built, before any entity reads its flag). The 12 grass set nothing (their activation
   flag -1, so their drops carry none either, which also lets the pond's Honey Drop, location 182, send its check). The
   13 switches and plates read flag 691 instead, set by every new file, so they behave as in vanilla, where 41 is set by
   then. The book room's gate (`DesertBookArea`), fed by one of those switches and there only until 41, is there until
   691: never, as in vanilla.
2. **The goal counts only the seed's goal flags** (`goal_flags`: each flag with every event that sets it, built from the
   world's artifacts and the game's own list, `ARTIFACT_WRITERS`). Today that is flag 41, set by Event26. An artifact
   the logic doesn't hold never counts; a future goal is a new row.
3. **A guard on those flags** (`GoalGuard`): a goal flag that turns on is kept only when one of its own events is
   running (the game's `lastevent`, with an event in progress) or the dev console set it; anything else turns it back
   off, logged as `[goal-guard] flag N turned on outside its events ... turned back off`. It checks before the goal is
   counted, after each physics step, at each frame's end, and right before a save or a map's build, so a stray flag is
   never counted, saved or built into a room. A file's flags as loaded or started are taken as they are. With the
   repoints in place it should never fire: each firing is a bug to report.
4. **The secret codes' new game held back** until the seed is known, like the other file choices (key 9 on an empty
   file with two or more codes, as the game has it; `documentation.md`, step 8).

The rule, in `CLAUDE.md`: a goal flag is set only by its own events, ever; a new goal joins the guard. Tests
`TestGoalFlags` (not yet run: the suite and the fuzzer run before the next push).

**Status:** built (2026-10-08), in seed `AP_70580691250444408633`; not yet seen in game.

*Code: `data_tables.py` (`ARTIFACT_WRITERS`), `data_types.py` (`FlagSwap`, `ALWAYS_SET`), `ACTIVATION_FLAGS` and
`LIMIT_FLAGS` in `logic/lost_sands.py`, `golden_settlement.py`, `wild_swamplands.py`, `ancient_castle.py`,
`honey_factory.py`, `rubber_prison.py`, `giants_lair.py` and `upper_snakemouth.py`, `slot_data.py`. The mod:
`Items/GoalGuard.cs`, `World/KeptOpen.cs` (the repoints), `Items/LocationChecks.cs` (`CheckGoal`), `Ui/MenuToggle.cs`
(`HoldBackFile`).*

## Build step 61: the swamp bridge kept up, its collapse kept away

**Asked and decided (the user, 2026-10-04):** the swamp bridge's collapse (`Event130` on `SwamplandsBridge`, flag 336)
never happens in a seed. In vanilla it drops the party to the room's bottom, by its door and the Horn Dash's lesson,
and is what removes Maki. What 336 changes was read first (`MEASURED.md`, `SwamplandsBridge`): only this room's
upper bridge and its walls, the scene's leafbugs and trigger, and the lake's turn-back (already kept away).

**Mapped on screen (2026-10-08 and 09, the user, a vanilla file and then a seed):** the scene crashed on a file with no
follower (it asks for the first one, Maki in vanilla); then, room by room as `room-logic.md` describes, with the
user's own sketch of the room. Along the way the user asked: "lets remove/hide the 3 leafbug npc's that appear during
the cutscene"; "we should always have the spring/bounce pads present" (the bottom's, made only after the swamp's
boss, "also prevents the bottom part from becoming a deadend"); and "can you remove the invisible walls that are around
the big bridge in the middle?", so the bottom is a drop from the bridge.

**Built** (the apworld only: the mod's own lists do it):
1. **Kept away** (`kept_open`): the collapse's trigger and its three leafbugs.
2. **Hidden** (`scenery_hidden`): the bridge's invisible walls (`Base/BridgeWalls`), as the collapse hides them.
3. **Kept present** (`kept_present`): the bottom's bounce pad, up to the bridge's left end.
4. **The logic** (`logic/wild_swamplands.py`): the bridge the map's own region with both its doors; the top right (the
   small bridge's switch, the Horn Slash) a drop from the right side, Jump back; the top door's platform from there by
   Bee Fly, or by Jump once the small bridge is down; its red pad a drop from the top door, Jump back, sending the party
   to the left end, whose boulder (Horn Dash) opens a pad back down; the bottom (its door, the save crystal, the lesson)
   free both ways. The lesson's check (location 72) needs nothing: talked to, the boulder plays it even without Kabbu,
   and arriving by the bottom door pushes the party past it. Its story-order stand-in is gone. A way inside a room
   may now land in a part of it (`Transfer.to_area`), as this room's loop needs. Tests `TestSwampBridge`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: no scene
and no leafbugs, the bottom's pad there and landing at the left end, the drop from the bridge to the bottom.

*Code: `logic/wild_swamplands.py` (`KEPT_OPEN`, `KEPT_PRESENT`, `SCENERY_HIDDEN`, `MAP_AREAS`, `TRANSFERS`, the small
bridge's story event), `data_types.py` and `regions.py` (`Transfer.to_area`). The mod: `World/KeptOpen.cs`,
unchanged.*

## Build step 62: a map's own start-up scene kept away (the Junction's centipede)

**Found (2026-10-09, mapping the Junction, `Swamplands5`):** some maps start a scene of their own on arrival while its
flag is off (`MapControl.autoevent`, run from the map's `LateUpdate`). The Junction's (`Event147`, a centipede passing,
until 383) plays on the first entry by any of its four doors and leaves the party at the left door. A first entry from
its top right (a shuffled door, a random start, or simply from Swamplands7 or 8) would then be on the left side,
whose way back up needs the lift, whose lever is up in the top right. Asked, the user's pick: never played in a seed.
It only sets 383 and moves a centipede that the map has standing out of sight anyway.

**Built:** `scenes_kept_away` in `slot_data` (`[{"map", "event", "flag"}]`, `SCENES_KEPT_AWAY` in each area's module):
as the map is built (`KeptOpen.AfterCreate`, before its `LateUpdate` checks), the mod sets the scene's flag, as the
game does when it starts the scene, and logs `[open] <map>: its scene EventN kept away`. The dev console's warp did the
same for every such scene already; a seed now does it for the listed ones. Test `TestJunction`.

**Status:** built (2026-10-09); not yet seen in game (the test file's 383 was already set by a dev warp).

*Code: `data_types.py` (`MapScene`), `logic/wild_swamplands.py` (`SCENES_KEPT_AWAY`), `slot_data.py`. The mod:
`World/KeptOpen.cs`, `Core/SeedData.cs`.*

## Build step 63: the Dash without the Horn Slash

**Found (2026-10-09):** a player reported that in v0.3.0 the Progressive Dash didn't dash at all; they had the Dash and
no Horn Slash yet. With Shuffle Field Moves the mod refused Kabbu's whole tap until the Horn Slash arrived (build step
21), and the Dash is a second press during that tap (`MEASURED.md`, every field ability). The logic counts the Dash and
the Horn Dash on the Progressive Dash alone (build step 23), so a seed could count on a Horn Dash nobody could use.

**The rule (the user, 2026-10-09)**, as decided on 2026-09-27 (Next 23): "horn slash = cuts grass / hit switches or
knock bridges down, push small rocks around; dash = only for mobility (it can do the horn slash things as well, once
horn slash is aquired); horn dash = break boulders (this does not require horn slash, its just an upgrade to dash
itself)". In the logic: "horn" always means the Horn Slash; "horn dash" is said only where there are boulders; no
mapped room needs the Dash itself (only the later chapters' story-order stand-ins name it, build step 23).

**Read first** (`MEASURED.md`, "Kabbu's horn tags"): Kabbu's hitbox carries the slash's tag `BeetleHorn`; a Dash's
carries the same tag, or `BeetleDash` once the Horn Dash is learned. Everything a horn hit does in the game reads one
of those two tags, and a boulder (`BreakableRock`) breaks only on `BeetleDash`.

**Built** (the mod, `FieldMoves.cs`, only with Shuffle Field Moves and the Horn Slash not yet received):
1. **The Dash starts:** Kabbu's tap is let through once the Dash is learned. Its first press swings nothing (no swing
   pose, no turn of his sprite) and makes no sound but the buzzer locked moves play on the press; the Dash's second
   press and the press that ends a Dash don't buzz. The turn was found by the review's last critic, after the commit.
2. **No horn hits:** the tap's three tag writes become "Untagged", so no grass, switch, ruler, push rock or save crystal
   reacts to Kabbu.
3. **The Horn Dash breaks boulders:** once it's learned, a dashing Kabbu's hitbox is tagged `BeetleDash` only while a
   boulder reads it, then set back, so the game's own boulder code runs as written (the boulder breaks, the Dash goes
   on).
4. With the Horn Slash received, all of it is the game's own again.

Without the Horn Slash the Horn Dash also no longer stuns field enemies, and the Dash and Horn Dash no longer break a
frozen fountain or hit save crystals, coiled vines or the prison's computer; none is needed to reach anything. The
user: fine, intended (2026-10-09).

**The logic, checked against the rule:** every Horn Dash rule in a mapped room sits in a room with a boulder and matches
the user's own "horn dash" for it (their messages from the mapping sessions, read again); every "horn" is the Horn
Slash. The one wrong rule was the broken bridge's ruler (`BarrenLandsBeefly`, a switch), written "Horn Slash or Horn
Dash": now the Horn Slash only. Two routes need both, for two obstacles, as the user described them: the pumpkin room's
high right door (a boulder, then a stone knocked over) and the long swamp room's middle to its right (grass and a
boulder). The user confirmed the check (2026-10-09). Test `TestDashWithoutTheHorn` (not yet run: the suite and the
fuzzer run before the next push).

**Still open: grass on a Horn Dash route.** In vanilla the Horn Dash cuts grass on its way, so grass on a route mapped
with every ability may never have come up. The review found grass near the Horn Dash routes of ten mapped rooms (by
position only). The user is "pretty sure" none blocks a way; it is rechecked on screen once every room is mapped
(their call, 2026-10-09), the rooms listed in the local TO-CHECK.

**Status:** built (2026-10-09); not yet seen in game.

*Code: the mod `World/FieldMoves.cs` (`HornLock`, `HornTag`, `SlashAnim`, `SlashSound`); `logic/forsaken_lands.py`,
`options.py`; tests `test_moves.py` (`TestDashWithoutTheHorn`).*

## Build step 64: crystal berry #15 kept until taken

**Found (2026-10-09, mapping the Square, `DefiantRoot1`):** crystal berry #15 on the Square's left rooftop is there
only until flag 201. The game sets 201 on entering the desert's `DesertDRSouthEntrance` and at the end of the caravan
robbery (`Event93`), which gives #15 itself. In vanilla the robbery comes first and the rooftop berry is the fallback
for a player who skipped it. In a seed the order is free: going through that desert room first took the berry away for
good, so a location there could never be checked (`MEASURED.md`, "Crystal berry #15 and flag 201").

**Asked and decided (the user, 2026-10-09):** keep it until it's taken. The robbery's own gift is looked at in the
enemy pass, with its fight.

**Built** (the apworld only: the mod already repoints an entity's limit flag, build step 60): `LIMIT_FLAGS` in
`logic/defiant_root.py` repoints the berry's limit from 201 to -1, which the game reads as no limit
(`MainManager.CheckIfCanExist`). The berry then disappears only once #15 is taken, which is also how its location,
`Defiant Root: Square, Left Rooftop` (192), is checked: by its own pickup, or by the robbery's gift if the robbery
plays first, so the check is never lost. No activation flag needs repointing: a crystal berry pickup never writes its
own (`NPCControl.CheckItem`), so taking it never ends the robbery. The swap runs as the map is built, before the
berry's own `Start` hides it when #15 is already taken. Test `TestDefiantRootSquare`.

**Found by the review (2026-10-09):** the robbery's gift (`|giveitem,3,15,7|`) reached none of the mod's give hooks: for
a crystal berry the game's `Giveitem` raises the count and marks the berry with no window, sprite or list add. So once
berry #15 was a location, the robbery would send 192 and also add a berry of its own, against "items are remote
only".
**Built (the mod):** a `SetText` prefix (`ItemSwap.CrystalGifts`) turns a `|giveitem,3,<n>,` whose berry is a seed
location into a hand-over of item 0 marked as that location, as berry rewards already are (`Berries`); deciding it
marks #n taken, as the game's give would, which sends the check. So the robbery shows the location's item and adds
none.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the
berry on its rooftop with flag 201 on. The robbery's hand-over not yet seen in game.

*Code: `logic/defiant_root.py` (`LIMIT_FLAGS`, the Square's locations), `logic/__init__.py`. The mod:
`World/KeptOpen.cs`, unchanged; `Items/ItemSwap.Pickups.cs` (`CrystalGifts`), `Items/ItemSwap.cs` (`FindLocation`).*

## Build step 65: the Defiant Root inn's upstairs door open

**Found (2026-10-09, mapping the Beehive Lift, `DefiantRoot2`):** the inn's high door, up a ledge (Jump or Bee Fly),
has a Lore Book behind it, and the door is shut by `Base/DoorLock` until flag 408. Only the innkeeper's daughter sets
408, when she is talked to in the Termite Capitol's industrial district (`TermiteIndustrial` line 21; she is there
from story flag 409). So in vanilla the room opens chapters later, and the logic would have to tie a Lore Book in
Defiant Root to a far room's story (`MEASURED.md`, the Beehive Lift).

**Asked and decided (the user, 2026-10-09):** keep it open in every seed. Her story stays the game's: she is still in
the Termite Capitol until talked to, and the inn shows her only from 408.

**Built** (the apworld only: the mod's own list does it): `scenery_hidden` gets `DefiantRoot2`'s `Base/DoorLock`, as
build step 59 opened the wizard's tower's. The door's own transfer needs nothing else. The Lore Book (`Defiant Root:
Beehive Lift, Above the Inn`, 196) needs reaching upstairs, then Jump or Bee Fly inside. Test `TestBeehiveLift`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the lock
gone, the door open, the Lore Book reached.

*Code: `logic/defiant_root.py` (`SCENERY_HIDDEN`, the Beehive Lift's areas and locations). The mod:
`World/KeptOpen.cs`, unchanged.*

## Build step 66: a give the game repeats, its check done, is the game's own (Morty's Bed Bug)

**Found by the review (2026-10-09):** Morty lends Pibu, his pillbug (the Bed Bug), once for free (line 24, location
190), and once it's used up he rents it again for 30 berries (line 28, the same `giveitem,1,89`). The mod matches a
give by map, kind, item and character only, so a re-rental after the check took the berries and kept the Bed Bug back,
for good (`MEASURED.md`, the Square; first written as a sale by another character, corrected).

**Asked and decided (the user, 2026-10-09):** a give the game repeats is the game's own once its check is done, beside
build step 10's respawning pickups: the second named exception to "items are remote only" (`CLAUDE.md`).

**Built:** a give can be marked `again` (`Give.again` in the apworld, `"again": true` in its `location_gives` entry).
When a talk begins (`SetText` with no message running, `ItemSwap.Repeats`), the mod notes which marked gives' checks
are already done (their flag set, or done on the server); a give of one of those in that talk is left to the game, and
logged (`given again ... the game's own`). The first lend sets its flag inside its own talk, after that note, so it is
still the check. Only Morty's give is marked. Test `TestDefiantRootSquare`.

**Status:** built (2026-10-09); not yet seen in game.

*Code: `data_types.py` (`Give.again`), `logic/defiant_root.py`. The mod: `Core/ApConnection.cs` (`Give.Again`),
`Core/SeedData.cs`, `Items/ItemSwap.Pickups.cs` (`Repeats`), `Items/ItemSwap.cs` (`FindLocation`).*

## Build step 67: what lies behind the Sand Castle Key and the Rusty Key held out

**Found (2026-10-09, mapping the Ancient Castle; a research workflow, each chain checked by a second agent):** the
castle's door from the desert needs the Sand Castle Key, and the bandit hideout's front door the Rusty Key, both with
"later chapters" as their stand-in. In the game both come after chapter 4's start (flag 300, the throne room's scene,
which waits on chapter 3's end in the Honey Factory and on Neolith, there from chapter 2's boss): the Bee Kingdom's
story and the factory's three keycard locks, the throne room, Astotheles' sale at the well, the hideout (the capture,
the cell dug out of, the storage chest hit with the horn, the Astotheles fight) and the roach village's hawk. The
stand-in holds none of it, so a castle spot, the hideout's cell (location 71) or the hideout's Enemysanity spots could
hold something the chain needs: an impossible seed. A gated door with no `DoorRule` gets no rule, which is how today's
logic walks into the factory before its story allows.

**Asked and decided (the user, 2026-10-09):** hold them out now, as build step 44 does for quests that open later;
keep mapping the castle's rooms; the chains get their own steps (Next 64).

**Built:** every castle location (198 to 206) and location 71 are `pending`, and the castle's artifact (flag 345,
the treasure room) stays out of `ARTIFACTS`, so the goal never counts it; Enemysanity's spots in the
castle and the hideout (areas 11 and 20, `KEY_CHAIN_AREAS`) too. Their rooms' ways are written as each is mapped, so
nothing waits but the spots. Also from the review: location 69 (the Dash's scene, `Lost Sands: Entrance`) had the same
kind of gap: its trigger needs flag 88 (chapter 2's boss, behind both offerings) and 138 (Gen and Eri's scene by the
second East Road's crank, there from the throne room's 130). Its reach now needs those rooms too. Tests
`TestSlidePuzzle`, `TestCastleBasement`, `TestDashScene`.

**Status:** built (2026-10-09); a logic change only, nothing to see in game.

*Code: `logic/ancient_castle.py`, `logic/bandit_hideout.py`, `logic/lost_sands.py` (location 69), `enemysanity.py`
(`KEY_CHAIN_AREAS`).*

## Build step 68: the Ancient Castle boss room's wall open

**Found (2026-10-09, mapping `SandCastleBossRoom`):** a wall stands before the boss room's door to the treasure room
until the Watcher's fight lowers it (flag 38). From the left the fight always starts before the wall is reached; from
the right, arriving before the fight, the wall isn't solid and the party walks through it both ways. The way back is
open but looks shut (`MEASURED.md`, the boss room).

**Asked and decided (the user, 2026-10-09):** "should we keep the right entrance door open? as its not solid anyway,
to make it more obvious that you can go back"; yes. The fight stays where it is, from either side.

**Built** (the apworld only: the mod's own list does it): `scenery_hidden` gets the boss room's `Base/CastlePlatform`,
as build step 65 opened the inn's door. Nothing a seed can reach changes: the room is one region either way. Test
`TestCastleBossRoom`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the
wall gone with the coffin still there, then the fight and its scene as before, its lowering played with the way open.

*Code: `logic/ancient_castle.py` (`SCENERY_HIDDEN`). The mod: `World/KeptOpen.cs`, unchanged.*

## Build step 69: the Honey Factory's door from Outside the Beehive open

**Found (2026-10-09, mapping Outside the Beehive, `BeehiveOutside`):** the bridge's door to the Honey
Factory (`loadzone factory`) and its other half inside (`HoneyFactoryEntrance`'s `loadzoneoutside`) exist in the game
only from flag 299; before that, from 169, a scene at the door (`Event88`) takes the party in. The logic had no rule on
the door, so it walked into the factory before the game allows (build step 67's note). Arriving through either half
while it's shut, the game pushes the party past it (`MEASURED.md`, Outside the Beehive).

**Asked and decided (the user, 2026-10-09):** "we should just always have this door be open instead"; and "we should
open the door properly from both sides" once the closed models still stood in the way.

**Built** (the apworld only: the mod's own lists do it): `kept_present` gets both halves of the door, and
`scenery_hidden` both closed models, `BeehiveOutside`'s `Base/Door` (hidden in the game from 169) and
`HoneyFactoryEntrance`'s `Base/DoorE` (from 299), as build step 65 opened the inn's. The door needs nothing, which the
logic already said, now true. `Event88`'s trigger, at the same spot from 169 until 299, is left as it is. Test
`TestOutsideTheBeehive`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: with
every flag off, the door open-looking from both sides, the factory entered and left through it.

*Code: `logic/bee_kingdom_hive.py`, `logic/honey_factory.py` (`KEPT_PRESENT`, `SCENERY_HIDDEN`). The mod:
`World/KeptOpen.cs`, unchanged.*

## Build step 70: the Bee Kingdom's Throne Room door open

**Found (2026-10-09, mapping the Throne Room, `BeehiveThroneRoom`):** the main area's half of the Throne Room's door
(`BeehiveMainArea`'s `loadzone throne`) exists in the game only from flag 169, its closed model until then; the Throne
Room's own half has no flag. Arriving from the Throne Room while it's shut, the game pushes the party out past it, so
the way works but looks closed (`MEASURED.md`, the Throne Room).

**Asked and decided (the user, 2026-10-09):** "i think we should just keep it open", as with the factory door (build
step 69).

**Built** (the apworld only: the mod's own lists do it): `kept_present` gets the main area's half, `scenery_hidden` its
closed model (`Base/ThroneDoors`, hidden in the game from 169) and `scenery_present` its open one (`Base/ThroneDoors
(1)`, shown from 169). The door needs nothing in the logic, as before, now true. Test `TestThroneRoom`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the
door open-looking from the main area, walked through both ways.

*Code: `logic/bee_kingdom_hive.py` (`KEPT_PRESENT`, `SCENERY_HIDDEN`, `SCENERY_PRESENT`). The mod: `World/KeptOpen.cs`,
unchanged.*

## Build step 71: Jaune's Gallery open from the start

**Found (2026-10-09, in the Bee Kingdom's main area, on the way to Jaune's Gallery):** the main area's door to the
gallery (`loadzonejaune`) exists in the game only from flag 299, an "Out For Lunch" sign stands before it until 299,
and a cube (`Base/Cube`) shuts the way until 299. The gallery's own half has no flag (`MEASURED.md`, Jaune's
Gallery).

**Asked and decided (the user, 2026-10-09):** "can we remove this sign/block from this entrance".

**Built** (the apworld only: the mod's own lists do it): `kept_present` gets the door, `kept_open` the sign and
`scenery_hidden` the cube, which still shut the way with only the first two (seen). The door needs nothing in the
logic, as before, now true; the gallery's Bad Book is location 207. Test `TestJaunesGallery`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the
sign and the cube gone, the gallery walked into and out of.

*Code: `logic/bee_kingdom_hive.py` (`KEPT_PRESENT`, `KEPT_OPEN`, `SCENERY_HIDDEN`, location 207). The mod:
`World/KeptOpen.cs`, unchanged.*

## Build step 72: the Scanner Room's gate open

**Found (2026-10-09, mapping the Bee Kingdom's Scanner Room, `BeehiveScannerRoom`):** a gate at the corridor's top
(`Base/Door`) stands closed until the scan sets flag 159 (`MEASURED.md`, the Scanner Room).

**Asked and decided (the user, 2026-10-09):** "we should keep the gate open".

**Built** (the apworld only: the mod's own list does it): `scenery_hidden` gets the room's `Base/Door`, as build step 68
hid the castle boss room's wall. Test `TestScannerRoom`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the gate
open before the scan, and the scan then playing as before.

*Code: `logic/bee_kingdom_hive.py` (`SCENERY_HIDDEN`).*

## Build step 73: the Scanner Room kept between the outside and the inside

**Found (2026-10-09):** the Scanner Room's only door is its bottom one. Outside the Beehive's main door is two doors at
one spot, to the Scanner Room until flag 160 and straight into the main area from 160, so once the story sets 160 the
room is out of the world. Its way on is `Event84`'s second part, which warps the party to the main area and HB's Lab
with map loads that ignore the entrance randomizer (`MEASURED.md`, the Scanner Room).

**Asked and decided (the user, 2026-10-09):** keep the room as a feature between the outside and the inside, a door at
its top where the gate is ("copy the bottom entrance/door how it works, place it where the gate is, and redirect how you
come in/out of it"), and the main area's bottom exit leading back into it; no scene warps.

**Built:** in `logic/bee_kingdom_hive.py`, `kept_present` gets Outside's main door into the room (`loadzonecorridor`,
gone in the game from 160) and `kept_open` its door straight into the main area (`loadzoneinside`) and the room's
warping trigger (`eventtrigger2`); `door_rows` (new records, `DoorRow`; the mod guide, step 49) a copy of the room's
bottom door at its top, named `loadzoneinside`, into the main area's bottom, and the main area's bottom exit
(`loadzoneoutside - Duplicate`) sent into the room's top, inside the gate. The logic needs nothing more: doors.json's
links between the three rooms are free both ways, as the new doors are, so what each reaches is the same. The doors join
the entrance randomizer at the door pass (Next 2), when `DoorShuffle` and the door data read `door_rows`. Test
`TestScannerRoom`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: in by
Outside's main door with 160 set, up through the top door into the main area, back through the main area's bottom exit
into the room's top, many times, about 2 s each way (the dev load timer).

*Code: `logic/bee_kingdom_hive.py` (`KEPT_PRESENT`, `KEPT_OPEN`, `DOOR_ROWS`), `data_types.py` (`DoorRow`),
`logic/__init__.py`, `data_tables.py`, `slot_data.py`. The mod: `World/DoorRows.cs`.*

## Build step 74: the scan a location, flag 160 with it

**Found (2026-10-09):** the scan (`Event84`'s first part) sets flag 159; flag 160, which teaches Leif the battle skill
Bubble Shield Lite, takes HB from beside his lab and switches Outside's main door, came only in its kept-away second
part.

**Asked and decided (the user, 2026-10-09):** the scan a location, like Pep Talk's farm scene (location 151), the skill
staying the game's; named "Bee Kingdom Hive: Scanner Room, Scan"; "lets just give 160 alongside the scan in the scanner
room, so its not missable or over complicating things"; sped up rather than skipped.

**Built:** location 208, `Source(event=84, flag=159)`, nothing needed (the scan plays walking past it from either end);
`flags_with` (new records, `FlagWith`; the mod guide, step 50) gives 160 with 159. With Skip cutscenes the scan is
fast-forwarded. Test `TestScannerRoom`.

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: 159 and
160 set by the scan, the check for 208 sent, the scan from either end, sped up.

*Code: `logic/bee_kingdom_hive.py` (location 208, `FLAGS_WITH`), `data_types.py` (`FlagWith`), `slot_data.py`. The mod:
`World/HiveScan.cs`, `Gameplay/QualityOfLife.cs`.*

## Build step 75: HB asks for the Explorer Permit from the start

**Found (2026-10-10, mapping HB's Lab, `HBsLab`):** B.O.S.S., the battle simulator on HB's computer, runs only from flag
161, set when HB is shown the Explorer Permit at her line 50. She asks that only from flag 219, after chapter 3's end in
the game (`MEASURED.md`, HB's Lab).

**Asked and decided (the user, 2026-10-10):** "can ask for the explorer permit from the start, instead of having to do
other things", rather than setting 161 on entering, which would skip the permit.

**Built** (the apworld only: the mod's own list does it): `dialogue_flags` re-points HB's line 50 from 219 to the
always-set 691, so she asks from the start and still answers 161 afterwards; a story event, "Explorer Permit Shown"
(flag 161), needs the Explorer Permit, already progression. B.O.S.S.'s two medals wait for the quest pass: it offers
only bosses already met. Opened with nobody met, its empty list froze the party; the mod now logs it off instead (the
mod guide, step 51). Test `TestHBsLab`.

**Status:** built (2026-10-10); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: HB asked
at once, the permit shown, the computer then offering Single Battles and Rush Mode.

*Code: `logic/bee_kingdom_hive.py` (`DIALOGUE_FLAGS`, the story event). The mod: `World/KeptOpen.cs`, unchanged.*

## Build step 76: Beette's Flower Key at its price again

**Asked (the user, 2026-10-10, mapping the Balcony, `BeehiveBalcony`):** "lets change the flower key back to vanilla
cost/price". Build step 46 made her sale free; Next 63 had planned this return with the berry rule.

**Built** (the apworld only): her `FreeSale` gone, and with it the last free sale, so `free_sales` leaves slot_data and
`FreeSale` leaves `data_types.py`. The mod keeps reading the key (the mod guide, step 43): a missing one means nothing
is free, and a seed made before this keeps her free. She stays present from the start (build step 9) and her sale stays
location 78. Its rule: nothing in the room (the user: everything there needs nothing), the 150 berries with Next 63's
`Berries(...)`, as for every seller. Tests `TestFlowerKeySeller` (no `free_sales`; the sale still a location with its
give), `TestBalcony`.

**Status:** built (2026-10-10); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot` (its
file with `free_sales` emptied, since `liveslot` only lays keys over the seed's): "So...? 150 berries for the house",
refused with 115 berries in the bag.

*Code: `logic/bee_kingdom_hive.py` (location 78), `data_types.py`, `logic/__init__.py`, `data_tables.py`,
`slot_data.py`. The mod: `World/FreeSales.cs`, unchanged.*

## Build step 77: the Honey Factory's storage door open

**Found (2026-10-10, mapping the Honey Factory's Lobby, `HoneyFactoryEntrance`):** the Lobby's door to the storage
(`loadzonestorage`, to the Storage Elevator) exists in the game only from flag 211 (`Event98`, a later factory scene),
with its closed model (`Base/DoorS`) until 211. The elevator's own half has no flag (`MEASURED.md`, the Lobby).

**Asked and decided (the user, 2026-10-10):** "we open the storage door", the NPCs left as they are (the worker who
says the overseer's stuck in the storage stays, the user's pick).

**Built** (the apworld only: the mod's own lists do it): `kept_present` gets the door and `scenery_hidden` its closed
model. The door needs nothing in the logic, as before, now true. Test `TestLobby`.

**Status:** built (2026-10-10); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the
door made present, its model gone, the user through it into the Storage Elevator (the log).

*Code: `logic/honey_factory.py` (`KEPT_PRESENT`, `SCENERY_HIDDEN`). The mod: `World/KeptOpen.cs`, unchanged.*

# How it works

## 1. The big picture: generator, seed, server, game

```text
 generator + apworld  ──(makes)──>  seed file  ──(loaded by)──>  server
                                                                   ▲
                                                        websocket  │  JSON messages
                                                                   ▼
                                                     your game + its client (the mod)
```

- The **apworld** runs once, when a seed is generated, and decides where every item goes. It never runs
  while anyone plays.
- The **server** hosts the seed. It knows which item sits at every location.
- The **client** lives in or next to the game. It only ever talks to the server.

## 2. Opening the connection: a websocket

The client opens a **websocket** to the server's address, for example `archipelago.gg:38281` or a local
`127.0.0.1:38281`. All messages are JSON, called "packets", each with a `cmd` naming its kind.

- `wss://` is the encrypted kind, `ws://` the plain kind. A local server you started yourself is plain, so
  write `ws://127.0.0.1:38281`. Without the prefix, a library may try the encrypted one first and time out.
- Rooms on the website can change port, so a client must let the player edit the port.

## 3. Logging in: the packets, in order

The server speaks first. The order, from the protocol doc:

1. The client opens the websocket.
2. The server sends **RoomInfo**: the room's details.
3. Optionally, the client asks for the **DataPackage**, the tables that turn item and location numbers into
   names.
4. The client sends **Connect**: the game's name, the player's slot name, the password if any, and how it
   wants items delivered.
5. The server answers **Connected** (you're in) or **ConnectionRefused** (with the reason).
6. The server sends **ReceivedItems** with anything the player is owed.

**The game name in Connect must match the apworld's `game` exactly** (here, `Bug Fables`).

**How items are delivered** is chosen in Connect with three switches (`items_handling`):

| Switch | Meaning |
|---|---|
| items from other worlds | always wanted by a normal client |
| items from your own world | on: even items you find yourself come from the server |
| your starting inventory | on: the server sends it on connect |

This mod turns all three on, which makes it a **"remote items"** client: picking something up never gives
it directly, and everything arrives from the server. Remote only fits a mod: it has no patched placements, so local
items would first need each seed's scouts cached, plus a second code path.

## 4. Sending checks: LocationChecks

When the player completes a location, the client sends **LocationChecks** with that location's number.

- **Duplicates are harmless.** The doc says the server ignores repeats. So after a reconnect a client can
  simply send every location it knows is done, which is also how checks made while offline get delivered.
- The server then sends the item at that spot to whoever it belongs to, you included in a remote-items
  game.

## 5. Receiving items: the index kept in the save

Items arrive in **ReceivedItems** packets. Each carries an `index`: the item's position in that player's
list of everything ever received.

- **After every connect, the server sends the whole list again**, including items from past sessions. So
  the client must remember **how many items it has already given the player**, and skip those.
- **Keep that number in the player's save**, per the doc. Then loading an older save gives back exactly
  what that save hadn't had yet, and a brand-new save gets everything.
- An `index` of **0** means "this is the full list"; anything else continues from where the last packet
  stopped. If the numbers don't line up, the client sends **Sync** and gets the full list again.
- A client must cope with **any item arriving any number of times**. Admins can send items, and starting
  inventory repeats.

## 6. Finishing: telling the server the goal is done

When the player reaches the goal, the client sends **StatusUpdate** with status **30** (goal reached).
Nothing else marks a slot as finished.

## 7. slot_data: the seed's settings, and this world's keys

The apworld can hand the client a small dictionary, **slot_data**, which arrives inside Connected. It's the
only way a setting chosen at generation (an option, a version number) reaches the game. This world puts in:

- `world_version`, the manifest's (`archipelago.json`), as core loads it onto the world class
  (`World.world_version`), written in the login line (`[ap] logged in: … world_version …`); the mod doesn't compare it
  yet (a refusal on a mismatch is planned, Next 43 item 15);
- `options` (build step 39): the options as this seed applied them, from Archipelago's `options.as_dict`, toggles as
  JSON booleans. The mod reads the goal (`artifacts_required`, capped to what the world includes), whether the
  attacks and Jump are items (`shuffle_field_moves`, `shuffle_jump`, build steps 21 and 22) and Points of No Return
  (`points_of_no_return`: the logic counts the Warp as the way back, so the mod keeps it on, build step 37). The rest
  is there so Universal Tracker can rebuild the seed (build step 40): the location categories, Shop Contents (No
  Progression after its fallback), the entrance randomizer, Filler Starting Checks as it stood, Progressive Boat and
  the player's `exclude_locations`. `slot_data.py` names every other option and why it isn't sent. Universal Tracker
  takes the seed's rolls from the keys below as they are: `door_targets`, `enemy_swaps`, `start`, `starting_member`,
  `music_map`, `jingle_map` and `shop_inventories`;
- how each location is done: `location_flags` (a game flag), `location_berries` (a crystal berry),
  `location_discoveries` (a journal discovery), `location_shops` and `location_item_shops` (a shop's copy or first
  purchase), `location_vars` (a number reaching a value, a boss prize);
- `location_gives`, the `giveitem` that hands out a gift location's vanilla item;
- `location_added`, an item the story puts straight into the bag at a location (build step 26);
- `location_pickups`, the map and flag of each location that is an item lying in the world;
- `silent_locations`, checks that show no item of their own (a member joining), where the player's own item is shown;
- `quiet_locations`, the opening's checks, whose items arrive with no hold-up (the mod guide, Item animation);
- the open world (build step 9): `kept_open`, `kept_present`, `scenery_hidden`, `scenery_present`, `held_until`,
  `present_from` and `dialogue_flags`, the story's blockers and scenery the mod keeps the way the logic assumes;
- the goal guard (build step 60): `goal_flags`, the goal's flags and the events that set them, and `activation_flags`
  and `limit_flags`, entities that borrow a goal flag repointed (and, build step 64, a pickup the story would take
  away);
- `scenes_kept_away` (build step 62): maps' own start-up scenes never played, their flag set as the map is built;
- `submarine_item`, `present_with_item` and `held_until_item`: the submarine is an item, and its docks are made, and
  who shows them off kept away, by its key item in the bag (build step 36);
- `door_targets` (the entrance randomizer), `enemy_swaps` (enemy shuffle) and `start` (the starting location);
- `starting_member`: 0 Vi, 1 Kabbu, 2 Leif alone, 3 all three, -1 the story's party (build steps 18 and 20);
- `ability_items`, always true: the game's ability checks answered from the items (build step 23);
- `music_map` and `jingle_map`, the songs and jingles swapped (build step 33), and `shop_inventories`, what shop slots
  restock and respawning pickups come back with (build step 34);
- `item_kinds`, which inventory list each of its items goes to.

The mod does nothing from its own knowledge of the game's locations: every table it acts on comes from here. One
exception: the opening skip names location 1 (the opening's gift) itself, for its hold-up.

**No support for older versions** (the user, 2026-10-03): "never any intentional fallback/support for old versions of
the apworld, yaml or the mod. people are expected to use the latest release for all of them." When a key moves, the
mod reads only the new one. A seed with no `options` connects, and the main menu's status line says it comes from an
older apworld and to use the latest release of everything.

## 8. The rule: use what Archipelago provides, never reinvent it

**The rule (2026-09-29):** whatever Archipelago or its official client library already does, we use, the way its
[docs](https://github.com/ArchipelagoMW/Archipelago/tree/main/docs) describe it (the local checkout at the targeted tag
says what our version has). We never build our own version of it. In the user's words: "there is a reason we are using
whatever archipelago does for websockets etc, and not trying to do something dumb/stupid like reinventing and
rebuilding archipelago inside the game just to connect/work with archipelago". A home-made version is more code to get
wrong, it drifts as Archipelago changes, and nobody who knows Archipelago can read it. **Recommendations too**
(2026-09-29, the user: "we should do all standards & recommendations that Archipelago mentions"): what the docs call
"should", "recommended" or "encouraged" (option groups, presets, a bug report page) is done like a requirement.
**Proper fixes only** (2026-09-30, the user: "i don't want any workaround/placeholder fixes for logic/location things,
i want proper fixes/whatever archipelago itself recommends and does"; part of this rule): a problem in the logic, the
locations or generation gets the fix Archipelago recommends and does, never a guard or placeholder that hides it. The
first case: the fill error with *minimal* and Shuffle Jump (Known issues) waits for more early locations, the first of
Archipelago's alternatives once its first remedy, a local early item, was measured and dropped, rather than a guard
raising *minimal* to *full*.
**Optional features too** (2026-09-30, the user: "we should try to support all available things archipelago has/does,
that includes plando"): what Archipelago offers a world as optional, connection plando first, is supported, not
written off as optional. **Read all of it** (2026-09-29, the user: "we should read and take a look at
everything/anything Archipelago. don't skip/assume"): every doc, every generic guide and the reference world, APQuest,
including the ones that look meant for someone else (the world maintainer's duties, the website's API); what doesn't
apply is written down as not applying, with why.

**The connection: a library.** Writing all of the above by hand is possible, but libraries exist for most languages;
the protocol doc lists them. For C# (Unity, BepInEx) it's **Archipelago.MultiClient.Net**. It handles:

- the handshake and every packet;
- turning numbers into names;
- a single call to log in.

**The apworld: Archipelago's own tools.**

- Rules with the Rule Builder, written in Python as its doc intends ("The rule builder is intended to be written first
  in Python"): `&` for and, `|` for or, and a game's own needs as custom rules registered the way the doc shows.
- Regions, exits and locations in Python modules, as `worlds/apquest` does.
- Tables generated from the game's own data (the door graph, the enemies) may stay JSON, read with `pkgutil`, as core
  worlds do (Pokémon Emerald's `data.py`, KDL3's `regions.py`).
- Doors shuffled by Archipelago's entrance randomizer (`entrance_rando.py`, its doc `entrance randomization.md`).
- Tests on `WorldTestBase`; packaging with the "Build APWorlds" launcher component; code in `style.md`'s style.

**Our own work only where Archipelago has nothing.** What no library or Archipelago tool can do for you:

- deciding **when** it's safe to hand the player an item in your game;
- **saving** the received-item count in your game's save;
- knowing **which spot** in your game is which location.

Those are the parts that make each game's client different. Anything still home-made that Archipelago does provide is
listed in "Where it stands" (Next), each replaced in a step of its own.

## 9. How this mod does it: threads, config, custom key items

- The login call can take several seconds, so it runs **off the game's own thread**
  (`ApConnection.Connect`). The result is handed back to the game thread through fields the game reads each
  frame, and log lines through a queue (`Post`, `Tick`). The game never freezes on connect.
- Connection settings (address, port, slot, password, compression) and the Archipelago mod switch live in
  the mod's BepInEx config file (`Plugin.Awake`).
- The client library, websocket-sharp and the JSON library sit in `BepInEx/plugins`, loaded once, and the mod itself
  reloads on its own during development.

**Custom gates are the mod's own items** (2026-09-26). Where the randomizer wants a gate vanilla doesn't have,
the mod makes an item of its own, added to the game's item table at runtime (an existing sprite, its own name and
description), and the gate is "has the item": the logic reasons about it like any key item, and the mod checks it in
the game. The Boat Ticket is the first (Next 21), the submarine the second, by default both levels of one progressive
item (build step 36); the party members as items (build step 13) are the same idea on the
location side, turning a story moment vanilla never made a check into one. Each new item takes a filler slot in the
pool, so a seed needs one to spare, which a test pins.

**Custom items' text** (2026-09-26): written to fit the game, as the menus and art do. The game's own voice,
seen in play: items are witty ("Don't say you weren't warned."), medals strictly informative, and **key items say what
they open, then a small joke** ("This keycard can open doors in the Honey Factory. Whoever lost this probably got
fired."; "This key opens a door in Rubber Prison. A guard probably dropped it while fleeing from the Wasps."; the
Platinum Card's is plain). A custom key item follows that: what it opens, and a light line in the team's voice; never a
rules explanation. (The key items' texts aren't in `itemdata[0, id, 1]`, which holds "Desc" for them; they were read
on screen.)

**Icon ideas for custom items** (2026-09-26, from a labelled contact sheet of the item sprites; `SpriteDump`,
the mod guide, step 7). An icon is only lent: the item has its own name and description, and a reused or similar icon
is fine in the menus as long as those differ (seen in the Key Items list). Item sprite ids
(`itemsprites[0, id]`):

| Id | The game's item | Could stand for |
|---|---|---|
| 176 | Platinum Card (silver) | **the Boat Ticket** (chosen); 95 (Factory Pass, a yellow pass) the other card |
| 95 | Factory Pass | a pass or ticket, though players know it as the Factory Pass |
| 161 | Prison Key | a generic "Key", if one is needed (the other keys have odd shapes) |
| 111 | Rusty Key | looks like a sword |
| 160 | Lab Card | an ID or pass (reads differently from a plain card) |
| 138 | Shady Note | a paper, note or code |
| 159 | Big Gear | a cog: fixing something broken |
| 187 | (no name; the queen's face) | something enemy related |
| 171 | Big Mistake | a pixelated Mistake: a bad or negative item, or a trap |
| 42 | Magic Ice | an ice cube: an ice trap |
| 181 | Danger Dish | eating something bad: a poison trap |
| 180 | Plumpling Pie | a derpy face: a trap |
| 150, 155 | Crystal Feather, Aphid Shake | jump or other moves, if moves are split into items |
| 29 | Coal Crystal | a plain block: something neutral |
| 98 | Crimson Ore | a stone, ore or gem |
| 110 | Game Tokens | currency or tokens (e.g. medal shops locked behind tokens) |
| 140 | Red Paint | looks like soup |
| 149 | Package | a box or mail |

**Combined icons**: the mod can layer game sprites into a new icon, still the game's own art. Tried
as mockups for the ticket (2026-09-26): emblems in the card's corner looked stuck on; centred and tilted with the card
they read better, but grey or brown on silver has no contrast. The ticket uses the plain card; a combined icon needs an
emblem that contrasts with its base.

## 10. Silent failures: things that go wrong quietly

Most connection mistakes don't crash; they just silently do nothing, or look like they worked. The full
list we keep is in [client-requirements.md](client-requirements.md), "Known failure modes". The ones that
matter first:

- The wrong game name, or a missing `ws://` for a local server, and the login fails or times out.
- Treating "the socket is open" as "we're in a seed": after a disconnect, rules must stay in force.
- Not saving the received-item count: every reload hands out every item again.
- An uncompressed connection works today, but the server warns that one day it may not. Turning compression
  on in websocket-sharp without accepting `server_max_window_bits` breaks every connection (build step 5).
- A lost connection that is only "disconnected" politely can keep a reading loop spinning in the background.
  The game just gets slower and uses more memory, with no error. Close the socket itself (build step 4).

## 11. The logic explained: regions, exits, rules, and this world's layout

### In short

The logic is how the generator knows where it may put an item. It is made of three things:

- A **region** is a place: a part of the game you can walk around freely once you're in it.
- An **exit** is a one-way path from one region into another, with a **rule**: what you need to go through.
- A **location** (a check) sits in one region, and may have a rule of its own: what that one spot needs once you're
  there.

The generator starts in one region, the **origin** ("Menu"), and walks: through every exit whose rule the items it has
so far meet, into every region that opens. Every location it reaches whose own rule is met is somewhere it may place
the next item. That walk, repeated as items are placed, is how Archipelago proves a seed can be finished.

```text
 Menu ──> BugariaOutskirtsOutsideCity ──[LoadZoneGoldenPath: Snakemouth Den Cleared]──> BOGoldenPath
                   │ DoorSnakemouth                                                      • Golden Path, Grass by the Dirt Spot
                   ▼
   BugariaOutskitsSnakemouthCorridor1 ──> … ──> SnakemouthLake ──> … ──> SnakemouthMushroomPit
                                                                          • Mushroom Pit, Mushroom by the Droplets [Freeze]
```

**Is a region a collection of rules?** Not quite. A region is a place; the rules sit on its ways in (the exits) and on
its spots (the locations). What a region does is *share*: every location inside gets "you got here" for free, and any
one of its ways in will do.

### In depth

- **Reaching a region:** some chain of exits leads to it from the origin, and every rule on that chain is met. Along
  one chain the rules add up (an *and*); between chains, any one will do (an *or*). So two exits into one region are
  an "or" nobody has to write.
- **Reaching a location** takes its region plus its own rule. Archipelago checks the region by itself (`world api.md`:
  entrances and locations "implicitly check for the accessibility of their parent region").
- **The origin:** Archipelago assumes the player can always get back to the region the logic starts from. The rule
  for that is `room-logic.md` rule 4: a one-way counts only together with what it takes to get back, and the Warp never
  counts (rule 9), except as the way back with Points of No Return on (build step 37). Today no one-way in the logic
  carries its way back yet: the room mapping writes each with `one_way` (build step 37).
- **Why a rule may only get easier:** the generator places items one at a time, each where the items placed so far can
  reach. That works only if having more items never shuts anything, so a rule can say "has" but never "hasn't"
  (rule 3).
- **Events:** some needs aren't items: a story step, a boss beaten. Each is an event, a location with no id holding a
  logic-only item (*First Boss Beaten* holds "Snakemouth Den Cleared"). It's reached like any location, and from then
  on its item counts for every rule, so a rule names a story step with `Has`, as it would an item. The goal is one
  too: "has enough Artifacts".
- **The rules are Archipelago's Rule Builder** (build step 29): `Has("Explorer Permit")`, `a & b` for "and", `a | b`
  for "or". Needs that depend on the seed's options are Bug Fables' own registered rules: `CanUse("Horn Slash")` (the
  ability's item when it's an item, its member when members are), `Member("Vi")`, `MoveItem("Freeze")`, `Boat(1)` (the
  Boat Ticket, or the Progressive Boat's first copy) and `WayBack(...)` (a one-way's way back, dropped with Points of
  No Return). When a seed
  is made, each becomes Archipelago's plain item checks, so an option that makes something free drops it from the rule.

### When to make a region, and why

When this world makes a new region inside a room, and when a spot's own rule is enough: `room-logic.md`, "The model"
(moved there 2026-09-30).

Why regions at all, instead of a full rule on every spot:

- **Written once:** what a place needs sits on its ways in, not copied onto every spot inside.
- **The "or" for free:** two ways in are two exits.
- **Doors can move:** the entrance randomizer rewires exits, and each rule moves with its exit.
- **The start can move:** a random start only changes where the walk begins.

### How this world does it

- **All of the logic is in the apworld; none is in the mod.** The mod only does what the generator decided
  (`slot_data`; `room-logic.md`, rule 7), so the game's side has no logic and no file per room.
- **Every map is a region, every door an entrance** (build step 12, 2026-09-30): `regions.py` makes them from the door
  table, named `"<map>: <door>"`, so Archipelago's entrance randomizer can shuffle them.
- **One Python module per game area** (`logic/`, build step 29): one for each of the game's 25 areas, from
  `outskirts.py` to `upper_snakemouth.py`. Each lists its locations and story events (each in its
  map, with its own rule), its door gates (`DOOR_RULES`) and ways between maps that aren't doors (`TRANSFERS`). Per
  area, not per room: a room's logic often reaches into its neighbours, and an area is tested in one sitting. Menu is
  made in `regions.py`.
- **Today** (2026-09-30; entrances 2026-10-02): 244 regions (Menu included), 584 entrances, 75 locations, 4 story events
  and 1 artifact event. Until the rooms are mapped (build step 24), what the old large areas needed is kept on each spot
  as its `reach`, beside its own `rule`. Adding a room is adding lines to its area's module, not code.
- **What a module looks like** (shortened):

  ```python
  # Every Snakemouth room with water droplets, or reached only through one: Leif freezes the droplets.
  UNDERGROUND = DEN & CanUse("Freeze")
  LOCATIONS = (
      Location("Snakemouth Den: Mushroom Pit, Mushroom by the Droplets", 9, "SnakemouthMushroomPit",
               Source(flag=724, pickup=Pickup(map="SnakemouthMushroomPit", type=0, item=144)),
               rule=CanUse("Freeze"), reach=UNDERGROUND),
  )
  ```

  A spot with two ways to it inside its area writes both: `rule=Has("Key A") | Has("Key B")`.
- **Checking it:** a test for each measured need, which fails without the rule; `test_areas.py` for how the modules
  fit together; the Logic Test apworld, to play a seed's logic without the game (Next 42); and Archipelago's
  `Utils.visualize_regions`, which draws the whole region graph as a PlantUML diagram.

## 12. Universal Tracker: how it's implemented

### In short

Universal Tracker is a tracker for any Archipelago game: it shows which of the player's locations are in logic right
now. It has no logic of its own for Bug Fables. It runs our apworld instead: it builds the player's world inside
itself, with the same regions, doors and rules the generator used, and asks those rules what the items the player has
received can reach (the walk in How it works 11).

The hard part is building the *same* world. The generator made choices at random (which door leads where, which member
starts, which fights are swapped), and Universal Tracker doesn't have the seed's random. So the world sends every
choice in slot_data, and when Universal Tracker rebuilds the world, the world takes each one from there instead of
rolling it:

```text
generation:         yaml ──> the world rolls doors, party, fights ──> slot_data ──> server
Universal Tracker:  server ──> slot_data ──> the world takes the rolls ──> the same regions, doors and rules
                                                       + the items received ──> the locations in logic
```

No yaml is needed: everything that shapes the world is in slot_data.

### In depth

- **What Universal Tracker runs** (its `TrackerCore.py`, v0.3.4): the world's own steps, `generate_early` through
  `generate_basic`, never fill or `fill_slot_data`; then Archipelago's `exclusion_rules`. It drops the start inventory
  (items with an id), since the server sends those as received items. Each time items arrive, it sweeps the regions
  from the origin as the generator does and lists the reachable locations the server says are still missing.
- **Telling it no yaml is needed:** `ut_can_gen_without_yaml = True`, and a static `interpret_slot_data` that hands the
  slot_data back. On connecting, Universal Tracker then builds the world from an empty yaml (every option at its
  default), with `multiworld.re_gen_passthrough["Bug Fables"]` set to the slot_data (`universal_tracker.passthrough`).
- **What slot_data carries for it** (How it works 7):
  - `options`, the options as the seed applied them (build step 39). `generate_early` builds the world's options from
    them first (`universal_tracker.apply_options`), so every location, rule and exclusion comes out the same.
  - the seed's rolls: `starting_member`, `enemy_swaps` and `start` in `generate_early`, before anything depends on them;
    `door_targets` in `connect_entrances`; the music and shop inventories in `generate_basic`. With them taken, the
    rebuilt world's slot_data is the seed's, key for key.
- **The doors** (build step 40): `door_targets` is the table the mod rewrites doors from. `entrances.replay` reads it
  back into the pairings it was written from and connects each door as Archipelago's entrance randomizer connected it.
  So the tracker follows exactly what the game does, and slot_data carries nothing extra for it. With the doors
  shuffled, each door stays unconnected until the player has gone through it, by default (its deferred entrances,
  build step 55); every location in logic is still shown.
- **What it can't recompute is sent as applied:** Shop Contents' Filler Only fallback runs in `pre_fill` and counts the
  whole room's items, which Universal Tracker never sees, so `options` sends No Progression when it fell back.
- **Refused:** a seed from another world version, or with no `options` (no support for older versions, How it works
  7). Universal Tracker then shows that the world couldn't be generated, the reason in its log.
- **Explaining:** `/explain` prints each rule's `explain_json`, which Archipelago's Rule Builder gives every resolved
  rule; our custom rules resolve to built-in rules, so they explain themselves as item lists (build step 41).
  `/get_logical_path` walks the doors from the origin to a spot. `custom_ut_sort` orders the list by area as the story
  reaches them, then by name.
- **How it's checked:**
  - `test_tracker.py` rebuilds seeds the way Universal Tracker does, without it, over 18 option sets: the rebuilt world
    must match the seed's slot_data, entrances, locations and exclusions, and reach the same locations with the same
    items. With the passthrough ignored, every case fails.
  - Universal Tracker's own fuzzer hook, run by `test-apworld.ps1` and CI over 10000 random seeds, regenerates each one
    with its own code and compares every sphere with the real generation (`development.md`, "Fuzzing the apworld").
- **Not built yet:** the map tab, which loads the PopTracker pack's maps (build step 41); the mod's keys it would
  follow are written since build step 57.
