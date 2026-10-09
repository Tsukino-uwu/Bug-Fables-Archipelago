# How the Bug Fables mod is being made

This is the story of building the **game side** of an [Archipelago](https://archipelago.gg) randomizer
for Bug Fables: the mod that runs inside the game, step by step, in the order it happened. It's meant for
anyone curious about the process, or thinking of doing the same for another game.

- **The Archipelago side** (the apworld, seeds, the server, connecting, items and checks) has its own
  guide: [apimplementation.md](apimplementation.md).
- **Facts about how Bug Fables works inside** live in `MEASURED.md`.
- **The programming ideas underneath** (DLLs, Harmony, reflection, frames, coroutines), in plain words:
  [programming-concepts.md](https://github.com/Tsukino-uwu/MeshGhost/blob/master/agent_docs/programming-concepts.md)
  from [MeshGhost](https://github.com/Tsukino-uwu/MeshGhost), another project by Tsukino (an online layer for
  singleplayer games).

## The steps

**By topic** (the steps are numbered in the order they were built; the Archipelago side of each is in
[apimplementation.md](apimplementation.md#contents)):

- **Getting into the game's code:** [1](#1-can-the-game-be-modded-unity-mono-a-readable-dll),
  [3](#3-reading-the-games-code-decompiling-it), [4](#4-bepinex-the-mod-loader-and-the-mods-layout); the dev tools,
  [5](#5-hot-reload-copying-into-the-game-the-dev-console), [6](#6-probes-logging-what-the-game-does-while-you-play),
  [7](#7-dumps-every-map-script-entity-and-sprite-listed).
- **Items and checks in the game:** [9](#9-item-swap-a-pickup-sends-its-check-instead-of-its-vanilla-item),
  [12](#12-shops-in-the-game-shelves-show-the-seeds-items),
  [14](#14-the-detector-medal-beeps-for-every-check-left-in-a-room),
  [26](#26-field-abilities-as-items-in-the-game-ability-checks-read-the-bag),
  [29](#29-save-crystals-used-with-the-confirm-button-no-move-needed),
  [35](#35-shuffle-shop-inventories-in-the-game-shelf-slots-and-pickups-swapped), the submarine,
  [37](#37-the-submarines-docks-follow-its-key-item).
- **The party you have:** [11](#11-missing-party-members-stand-ins-in-scenes-and-followers),
  [36](#36-scripted-fights-cast-from-the-members-you-have).
- **The entrance randomizer in the game:** [13](#13-the-entrance-randomizer-in-the-game-doors-rewritten-at-map-load).
- **The Archipelago panel and its settings:** the panel, [8](#8-the-archipelago-panel-on-the-main-menu); Quality of
  life, [10](#10-the-quality-of-life-page-fast-text-skip-cutscenes-warp-and-more),
  [38](#38-the-warp-forced-on-with-points-of-no-return),
  [39](#39-spy-specs-the-medals-effects-as-a-quality-of-life-row),
  [20](#20-item-colors-archipelagos-colours-in-the-you-got-box),
  [21](#21-archipelago-icon-other-players-items-on-the-ground-and-on-shelves),
  [22](#22-item-backgrounds-a-checks-item-class-shown-before-pickup),
  [24](#24-uncap-fps-frame-rates-above-60-without-speeding-the-game-up); Gameplay,
  [15](#15-difficulty-and-detector-rows-and-what-goes-in-the-panel-or-the-yaml),
  [17](#17-enemy-scaling-each-areas-enemies-fit-when-you-reach-it), [19](#19-exp-and-berry-multipliers),
  [27](#27-attack-boost-1-damage-on-every-hit), [30](#30-healing-crystals-every-save-crystal-heals),
  [31](#31-auto-save-between-rooms-a-death-costs-one-room); with Archipelago off,
  [18](#18-use-on-normal-saves-the-panels-settings-with-archipelago-off).
- **Saves:** [16](#16-randomizer-saves-in-their-own-folder), [31](#31-auto-save-between-rooms-a-death-costs-one-room).
- **Builds and safety:** [32](#32-reproducible-builds-the-release-dll-rebuilt-byte-for-byte),
  [33](#33-server-text-cleaned-before-the-game-shows-it), [34](#34-multiclientnets-cache-kept-in-its-own-folder); speed,
  [25](#25-hitches-fixed-the-mods-garbage-and-the-games-5-second-collection); never stuck for good,
  [40](#40-no-respawn-loop-a-fall-that-only-leads-back-into-itself-ends-with-the-warp).
- **Reading the seed:** the options from slot_data's `options`,
  [41](#41-the-seeds-options-read-from-slot_datas-options).

1. [Can the game be modded? Unity, Mono, a readable DLL](#1-can-the-game-be-modded-unity-mono-a-readable-dll)
2. [The design, decided first: remote items, what the player sees](#2-the-design-decided-first-remote-items-what-the-player-sees)
3. [Reading the game's code: decompiling it](#3-reading-the-games-code-decompiling-it)
4. [BepInEx, the mod loader, and the mod's layout](#4-bepinex-the-mod-loader-and-the-mods-layout)
5. [Hot reload, copying into the game, the dev console](#5-hot-reload-copying-into-the-game-the-dev-console)
6. [Probes: logging what the game does while you play](#6-probes-logging-what-the-game-does-while-you-play)
7. [Dumps: every map, script, entity and sprite listed](#7-dumps-every-map-script-entity-and-sprite-listed)
8. [The Archipelago panel on the main menu](#8-the-archipelago-panel-on-the-main-menu)
9. [Item swap: a pickup sends its check instead of its vanilla item](#9-item-swap-a-pickup-sends-its-check-instead-of-its-vanilla-item)
10. [The Quality of life page: Fast text, Skip cutscenes, Warp and more](#10-the-quality-of-life-page-fast-text-skip-cutscenes-warp-and-more)
11. [Missing party members: stand-ins in scenes, and followers](#11-missing-party-members-stand-ins-in-scenes-and-followers)
12. [Shops in the game: shelves show the seed's items](#12-shops-in-the-game-shelves-show-the-seeds-items)
13. [The entrance randomizer in the game: doors rewritten at map load](#13-the-entrance-randomizer-in-the-game-doors-rewritten-at-map-load)
14. [The Detector medal beeps for every check left in a room](#14-the-detector-medal-beeps-for-every-check-left-in-a-room)
15. [Difficulty and Detector rows, and what goes in the panel or the yaml](#15-difficulty-and-detector-rows-and-what-goes-in-the-panel-or-the-yaml)
16. [Randomizer saves in their own folder](#16-randomizer-saves-in-their-own-folder)
17. [Enemy scaling: each area's enemies fit when you reach it](#17-enemy-scaling-each-areas-enemies-fit-when-you-reach-it)
18. [Use on normal saves: the panel's settings with Archipelago off](#18-use-on-normal-saves-the-panels-settings-with-archipelago-off)
19. [EXP and berry multipliers](#19-exp-and-berry-multipliers)
20. [Item colors: Archipelago's colours in the "You got" box](#20-item-colors-archipelagos-colours-in-the-you-got-box)
21. [Archipelago icon: other players' items on the ground and on shelves](#21-archipelago-icon-other-players-items-on-the-ground-and-on-shelves)
22. [Item backgrounds: a check's item class shown before pickup](#22-item-backgrounds-a-checks-item-class-shown-before-pickup)
23. [The Archipelago logo, drawn in code in the game's style](#23-the-archipelago-logo-drawn-in-code-in-the-games-style)
24. [Uncap FPS: frame rates above 60 without speeding the game up](#24-uncap-fps-frame-rates-above-60-without-speeding-the-game-up)
25. [Hitches fixed: the mod's garbage and the game's 5-second collection](#25-hitches-fixed-the-mods-garbage-and-the-games-5-second-collection)
26. [Field abilities as items in the game: ability checks read the bag](#26-field-abilities-as-items-in-the-game-ability-checks-read-the-bag)
27. [Attack boost: +1 damage on every hit](#27-attack-boost-1-damage-on-every-hit)
28. [A Graphics page, tried and removed: render scale and MSAA](#28-a-graphics-page-tried-and-removed-render-scale-and-msaa)
29. [Save crystals used with the confirm button, no move needed](#29-save-crystals-used-with-the-confirm-button-no-move-needed)
30. [Healing crystals: every save crystal heals](#30-healing-crystals-every-save-crystal-heals)
31. [Auto-save between rooms: a death costs one room](#31-auto-save-between-rooms-a-death-costs-one-room)
32. [Reproducible builds: the release DLL rebuilt byte for byte](#32-reproducible-builds-the-release-dll-rebuilt-byte-for-byte)
33. [Server text cleaned before the game shows it](#33-server-text-cleaned-before-the-game-shows-it)
34. [MultiClient.Net's cache kept in its own folder](#34-multiclientnets-cache-kept-in-its-own-folder)
35. [Shuffle Shop Inventories in the game: shelf slots and pickups swapped](#35-shuffle-shop-inventories-in-the-game-shelf-slots-and-pickups-swapped)
36. [Scripted fights cast from the members you have](#36-scripted-fights-cast-from-the-members-you-have)
37. [The submarine's docks follow its key item](#37-the-submarines-docks-follow-its-key-item)
38. [The Warp forced on with Points of No Return](#38-the-warp-forced-on-with-points-of-no-return)
39. [Spy Specs: the medal's effects as a Quality of life row](#39-spy-specs-the-medals-effects-as-a-quality-of-life-row)
40. [No respawn loop: a fall that only leads back into itself ends with the Warp](#40-no-respawn-loop-a-fall-that-only-leads-back-into-itself-ends-with-the-warp)
41. [The seed's options read from slot_data's `options`](#41-the-seeds-options-read-from-slot_datas-options)
42. [The ant tunnels' miners dig for free](#42-the-ant-tunnels-miners-dig-for-free)
43. [A free seller: the price in their lines made 0](#43-a-free-seller-the-price-in-their-lines-made-0)
44. [Enemysanity: an enemy's won fight drops its check](#44-enemysanity-an-enemys-won-fight-drops-its-check)
45. [The Termacade: tokens, the gift and the prize stand](#45-the-termacade-tokens-the-gift-and-the-prize-stand)
46. [The Platinum Card carries the bank's doubled interest](#46-the-platinum-card-carries-the-banks-doubled-interest)
47. [A chapter's main quest filed without freezing its scene](#47-a-chapters-main-quest-filed-without-freezing-its-scene)
48. [The pause menu's artifacts: each one's own icon](#48-the-pause-menus-artifacts-each-ones-own-icon)
49. [Doors a seed adds or sends elsewhere](#49-doors-a-seed-adds-or-sends-elsewhere)
50. [The Bee Kingdom's scan sets flag 160 too](#50-the-bee-kingdoms-scan-sets-flag-160-too)

## Where it stands

Each step's status is its last line (**Status:**), before its *Code:* line. What's next and the known issues
are in [apimplementation.md, "Where it stands"](apimplementation.md#where-it-stands).

## Keeping this guide honest

A step-by-step guide is only useful if no step is missing, so the project enforces it: any commit that
changes the mod, the apworld or the dev scripts is refused unless it also updates this file or
[apimplementation.md](apimplementation.md) (or says, explicitly, that nothing about the process changed).
That check is a small git hook, `.githooks/commit-msg`, which also keeps each subject to 72 characters with no
attribution (its neighbour `.githooks/pre-commit` runs the preflight, which refuses anything unpublishable:
[apimplementation.md, build step
28](apimplementation.md#build-step-28-the-preflight-nothing-unpublishable-in-the-repo-or-a-release)). Each step that
built code ends with a short *Code:* line naming the files and methods to read, just after its **Status:** line. Each
new step also gets a line in the index above.

**Titles say what a step does, and links can't break** (2026-09-30, the user: titles like "the Boat Ticket" or "six
questions" didn't tell a newcomer where the logic was done, or which step covers the entrance randomizer). A step's
title names what it adds or decides, and the yaml option or panel row by its own name; its number never changes, as
prose everywhere cites "build step 16" or "the mod guide, step 11". So a title can be reworded at any time, the
pre-commit's `doc-coverage.py` checks both guides' indexes against their headings, as it does the log's, and resolves
every link into a Markdown heading anywhere in the repo, refusing one that leads nowhere. Since 2026-09-30 it resolves
every plain link to a file or folder too, as MeshGhost's preflight does: all 213 led somewhere on the first run.

**The session log's index** (2026-09-29, the user: the log will become the longest file, and an index makes it
"easy to read, and also easier to search/grep"): `log.md` opens with a Contents list, one line per entry with its
link. The pre-commit's `doc-coverage.py` works out each heading's link the way GitHub does and refuses a commit whose
list doesn't match the entries one to one, in order, printing the line to add. **Headings are the date and a title,
nothing else** (the user, 2026-09-29: "later, morning, end of session… there is already a date"): the check refuses
anything between the date and the colon, and a date earlier than the entry above it (newest last).

---

## 1. Can the game be modded? Unity, Mono, a readable DLL

Before writing anything, we looked at what the game is made of. Bug Fables is a **Unity game built with
Mono**, which means its code ships as a normal .NET file (`Assembly-CSharp.dll`) that can be turned back
into readable code. That's the easiest case there is. We didn't check whether anyone had already started or
made a Bug Fables randomizer: we assumed nobody had started on an apworld. Other Bug Fables projects were
never read, looked at or used while making this one, not even their public docs.

*How to tell for your own game:* an `Assembly-CSharp.dll` in the game's `_Data/Managed` folder means
Unity with Mono. A `GameAssembly.dll` means Unity with IL2CPP, which is harder.

**Status:** done.

## 2. The design, decided first: remote items, what the player sees

A few decisions made first, because they shape everything after:

- **Start small: key items only.** More pools, like medals and crystal berries, come later.
- **"Remote items" only.** Picking something up never gives it to you directly. It tells the server,
  and every item, even your own, comes back from the server. That's simpler to build, a lost save can get
  everything back, and two people can share one slot. The cost: with the server down, nothing arrives.
- **What the player sees** (2026-09-24):
  - **At your own find** (a pickup, or an NPC handing something over), the game shows **what's really
    there**: a Bug Fables item (yours, or another Bug Fables player's in the same room) with the game's own
    sprite, and any other game's item as the **Archipelago icon**. To know what's there before it's found,
    the mod asks the server first, a "scout", without creating hints (`create_as_hint` 0, see
    `client-requirements.md`). **Seen items are never sent as hints** (the user, 2026-10-06: "way too many hints").
    Archipelago allows it: a scout with `create_as_hint` set tells the server of a location the player has seen but not
    checked, a free hint like ALttP's ledge items (`network protocol.md`, LocationScouts, 0.6.8), and Archipelago's
    own `UndertaleClient.py` and `MMBN3Client.py` send `create_as_hint: 2` when their game shows an item. Here every
    shelf and pickup shows its item, so it would hint most of the seed. Hints come only from the player asking
    (`!hint`, `!hint_location`).
  - **A small Archipelago chat feed in the bottom-left corner.** It never stops play. It shows the items you
    send and receive, in Archipelago's own wording ("Player1 found their Hammer (Location)", "Player1 sent
    Hammer to Player2 (Location)"), and players connecting and disconnecting. It's **on by default**, with an
    on/off switch in the Archipelago panel. Items arriving from the server show up only there, never as a
    popup. **Later, it becomes a real text client** (2026-09-24): a key opens a text line over
    the feed, so server commands like `!hint` work in game. The game's controls pause while typing, and the
    server's replies show in the feed. **Also** (2026-09-29, the user: so "people won't have to use the archipelago
    launcher text client"): DeathLinks show in the feed too, and each kind of message can be hidden (a filter per
    kind), with hints and the server's other commands typed from the text line. Built the Archipelago way: the client
    library's message log (the server's `PrintJSON`), its kinds as the filter's categories (item sent, hint, join,
    leave, chat, goal, release, collect, countdown and the rest), its message parts for the colours, and `Say` for
    chat and commands. **The key and the menu** (2026-09-29, the user): Enter opens the text line in the field (the
    overworld) and in battles; Enter again sends and closes it, and Enter on an empty line just closes it. No
    gamepad button: typing needs a keyboard anyway. In the field this replaces the game's own use of Enter there
    (action 9, the "help": a party member talks about what's in front; `MEASURED.md`, Input); in the pause and start
    menus Enter keeps the game's uses. A **Chat menu** in the Archipelago panel holds the chat's on/off switch, the
    filters and its other options; with the chat off, Enter is the game's everywhere, and with Archipelago off
    nothing changes (vanilla stays vanilla). **While it's open, nothing reaches the game** (2026-09-29, the user): no
    key or gamepad button acts in the game until you leave, by Enter on an empty line, Enter to send, or Esc (which
    drops what was typed, and never opens the pause menu while the chat is open). In the field the dev console already
    holds the game this way (`player.lockkeys` and `minipause`, as an item-get does, so the world waits too); battles
    read their input their own way, to be read in the code before building. **The look** (2026-09-29, the user: "like
    a twitch chat where old msgs eventually become invisible unless you press enter"): closed, the newest few lines
    stack in the bottom-left corner with no box, in the game's font with a shadow, and each fades out a while after it
    arrives; open, a semi-transparent dark panel shows the history (scrolled with the wheel or Page Up and Down) with
    the typing line under it. Names, items (by class) and locations in Archipelago's colours. The Chat menu sets how
    long lines stay (or never fade), how many show, and how dark the panel is. The chat draws its own letters in the
    game's font (`MainManager.fonts`), never from the game's 500-letter pool, which its dialogue shares
    (`MEASURED.md`, the text letter pool). The font is borrowed from the running game, never copied into the mod.
    Other games' names can hold characters the game's fonts lack: each is checked as the game checks its own
    (`GetCharacterInfo`), and a missing one is drawn in a fallback font made from the computer's own fonts
    (`Font.CreateDynamicFontFromOSFont`), scaled to the game's letters: it looks different but stays readable (the
    user, 2026-09-29: better than "gf798?? recieved from play????"). Only a character no font has becomes "?".
  - **Item names are coloured the way Archipelago's clients colour them** (`NetUtils.py`): progression plum
    `#AF99EF`, useful slateblue `#6D8BE8`, trap salmon `#FA8072`, filler cyan `#00EEEE` (later the *Item colors*
    row, Rarity by default, with Archipelago's colours darkened for the box: step 20).
- **Read what others already solved.** We read the TEVI randomizer (another Unity mod), Pokémon Emerald's
  apworld, Archipelago's own docs, and notes from an earlier Archipelago project, all for ideas only,
  with each one's licence checked first, except Emerald's own: read 2026-09-30, after its code (`licensing.md`).

**Status:** done (decided 2026-09-24); the chat feed and the text client it describes aren't built yet
([apimplementation.md](apimplementation.md#where-it-stands), Next).

## 3. Reading the game's code: decompiling it

We used **ILSpy** to turn the game's `Assembly-CSharp.dll` back into C# source, kept on our own machine
and never shared. Reading it answered the first big question: *how does this game hand out items?*
The answer was a small set of places every item goes through, which is exactly where a randomizer
needs to hook in.

**Status:** done.

## 4. BepInEx, the mod loader, and the mod's layout

Unity games don't load mods by themselves, so we installed **BepInEx 5**, the usual mod loader for
Unity games. One launch of the game confirmed it worked, and showed its log file.

**Where the code lives** (2026-09-27, a refactor that changed nothing the plugin does): one project, its sources in
folders by job under `mod/BugFablesAP/`: `Core` (the plugin, the connection), `Items` (checks sent, items received,
what a location shows), `World` (doors, enemies, the open world, the party), `Ui`, `Gameplay` (panel settings that
change play), `Guards` (quiet fixes for the game's own warnings, Steam achievements held back, its 5-second
collection skipped) and `Dev` (the console, probes and dumps). The namespace stays `BugFablesAP` everywhere, so a move
never touches code. **How "changed nothing" is proven:** build before and after, decompile both DLLs with ILSpy, and
diff the output; a pure move comes out identical.

**Each system runs on its own** (an outside review, 2026-09-28). Every frame the plugin runs about 20 systems in turn
(the connection, checks, received items, shops, the party...). They used to share one error guard, so a system that
threw on every frame silently stopped every system after it. Now each has its own guard, and its errors are logged
under its name (`[recv] threw: ...`), once per distinct message. Built, not yet seen in game.

**Errors are checked for, not swallowed** (a comparison with other randomizers, 2026-09-28). A few places caught an
error and carried on without a word: an id outside the game's item or medal tables, a save point's coordinates that
didn't parse, a method whose code couldn't be read. Each now checks first (the table's size; `TryParse` with the
game's own number format, as `Convert.ToSingle` reads it under the en-US culture the game sets), and a failed code
read is logged once. The dev console's pickup guard now gets its logger before its first use: before, a missing hook
target would have thrown inside `Awake` and stopped the plugin loading. The code style is in `.editorconfig`.

**Hooks marked with attributes, the way BepInEx mods do** (2026-09-28). Each hook used to be wired by hand, with the
same setup copied into every feature. Now each is marked with Harmony's attributes (`[HarmonyPatch]`,
`[HarmonyPrefix]`...), and `Core/Hooks.cs` installs them:
- **A group at a time.** A group is a class of attributed hooks with its own Harmony instance. A group with a missing
  target installs nothing rather than half, and logs what the player loses.
  - A feature whose hooks stand or fall together is its own group, annotated in place.
  - An optional hook gets a nested group of its own.
  - Hooks that depend on each other are separate groups installed in order, each only if the one before went in.
  - SaveRedirect's group is `required`: without every redirect a randomizer save could land beside the normal ones,
    so a missing target still stops the plugin loading. CachePaths' group is too (step 34).
- **Order.** Where one target carries several of our hooks of a kind, their run order can matter (ItemSwap's pickup
  prefix before its berry prefix): such hooks are separate groups installed in that order. A hook that must run last
  says so with `[HarmonyPriority(Priority.Last)]`.
- **Targets an attribute can't name.**
  - A coroutine's step is reached with `MethodType.Enumerator`.
  - An overload taking a private nested type (DevConsole's `DoDamage`) is reached through the group's `TargetMethod`.
  - Where the methods to patch are found by reading the game's code at install time (FrameRate's lists, FrameSites,
    Abilities' scan), they stay patched by hand, on a `Hooks.Create` instance.
- **Transpilers** go through `Hooks.Safe` and find everything they need before they change anything: a transpiler that
  throws would break its method for every later patch until the game restarts.
- **Unloading:** `Hooks.UninstallAll` takes every group off. Only `Hooks.cs` makes a Harmony instance.

**How "changed nothing" was proven:**
- The Debug setting `PatchDump` writes every patch the mod made (target, kind, patch method, priority) once per load,
  and logs the run order wherever one target has several of ours.
- The HarmonyX members it reads (`GetAllPatchedMethods`, `GetPatchInfo`, `Patch.owner`/`priority`/`PatchMethod`) were
  read at both tags, 2.7.0 (compiled against) and 2.9.0 (the game's), and match.
- The baseline was taken in game at the main menu (HarmonyX 2.9.0.0): 167 patches.
- After each batch of the 33 features, the list and the run order were identical.
- Two hot reloads in a row gave the same 167 patches each time, with none left from the load before.

**Unloading guarded too, and nothing put back as the game closes** (2026-10-04). A game log ended in a
`NullReferenceException` in the game's `MainManager.ApplySettings` as the game closed with Uncap FPS on: Uncap FPS
puts the game's frame settings back through it (step 24), and the audio sources it also sets were already gone. The
throw stopped the plugin's unloading at that step, so the hooks never came off and no "unloaded" line was written.
- **Each unload step runs through the frame's guard** (`Guarded`): one that throws is logged with its stack, and the
  rest still run, the hooks above all, which a hot reload must not leave beside the new instance's.
- **A closing game puts nothing back.** Unity's `Application.quitting` (in the game's Unity 2018.4, read in its
  `UnityEngine.CoreModule`) sets `Plugin.Quitting` and logs `[quit] the game is closing`. Uncap FPS then skips
  putting back the camera, the characters' interpolation and the game's settings; a hot reload still puts them back.
- **To see:** close the game with Uncap FPS on; I read the log for the `[quit]` line, then "unloaded.", with no error.

**The dev tools in their own half** (2026-09-28). `Plugin` is a partial class. `Plugin.cs` holds what every build
runs. `Dev/Plugin.Dev.cs` holds the [Debug] settings, the console, probes and dumps, reached through partial methods
(`DevAwakeEarly`, `DevAfterTick`...). FrameRate's measurements work the same way. A build without `Dev/` still compiles,
and the calls into it vanish.

**Lines kept to 120 characters** (2026-09-29, the limit `.editorconfig` states). A pass of line breaks and indentation
only, proven by the compiled code: a Release and a Debug build without debug info came out byte-identical before and
after (the builds are deterministic, so any difference would show). 903 lines were over 120; 144 were left that day,
each a single string that only splitting would shorten, which changes the compiled code.

**The seed's data in one record** (2026-09-28). What a login reads from `slot_data` moves from about 30
separate fields on the connection into one immutable `SeedData`. **How "changed nothing" is proven:** the Debug setting
`SeedDump` writes everything the mod read from the seed, one sorted line per entry, once a login brings it. It is taken
before and after the change on the same local seed. `copy-dev.ps1 -ConfigSet Archipelago.RandomizerEnabled=true` lets
the game log in at the main menu for it, with no save in play. The same login showed compression on
(`permessage-deflate`), with its hooks moved.
- **Built so far:** `Core/SeedData.cs` parses the seed whole before anything is published, so a malformed `slot_data`
  changes nothing. Before, a throw halfway left a mix of two seeds. The connection keeps one reference to it, and its
  old properties forward to it. The seed dump was identical (1,073 entries, on a local seed, 2026-09-28).
- **The connection no longer writes into features.** It used to set PartyMembers', FieldMoves' and Abilities' seed
  flags at login. Those three now read the seed themselves, through a `Func<SeedData>` their `Enable` takes, like the
  connection other features are handed. The seed dump was identical again.
- **The connection's table properties stay** as one-line forwards to `SeedData`: about 80 readers use them, and
  rewriting them would change nothing a player sees.

**Status:** done; separate guards per system built 2026-09-28, not yet seen in game; errors checked for instead of
swallowed, built 2026-09-28 (both builds pass), not yet seen in game; hooks to attributes built 2026-09-28 (all 33
features; the patch list and run order identical to before, 167 patches, in game). Seen in a play-test (2026-09-29):
a new file with its starting items, a pickup gone once checked, shops, Uncap FPS and saving all as before. Unloading
guarded and nothing put back at closing, built 2026-10-04 (both builds pass), not yet seen in the log.

*Code: `mod/BugFablesAP/Core/Plugin.cs` (`Plugin`, a BepInEx plugin: `Awake` sets everything up, `Tick` runs
every frame); `Core/Hooks.cs` (`Install`, `Create`, `Safe`, `UninstallAll`); `Core/SeedData.cs`; the dev build's
half in `Dev/Plugin.Dev.cs`, `PatchDump.cs` and `SeedDump.cs`; the project file is `BugFablesAP.csproj`.*

## 5. Hot reload, copying into the game, the dev console

Restarting the game for every change is slow, so before any real feature we set up **hot reload**:
change the mod, and it swaps itself into the running game in a second or two.

This took some detective work. The standard tool for it (ScriptEngine) relies on a feature this game's
runtime doesn't have, and it failed silently. Turning on more logging showed the real error. The fix was
small: the mod checks its own file once a second and asks for a reload when it changes.

**Lesson:** when something silently does nothing, make the invisible errors visible before guessing.

**Build, then copy.** `dev-scripts/stage-dev.ps1` builds the mod and stages it inside the repo, in `stage/`,
laid out like the game folder. Copying it into the game is a separate step (2026-09-24): the build
never writes to the game install. After each build, copy
`stage/every-build/BepInEx` onto the game folder, and the running game reloads it. Copy `stage/setup/BepInEx`
(the client libraries and ScriptEngine's config) once, with the game closed, and again only when the script
says the libraries changed.

**The copy keeps a backup.** `dev-scripts/copy-dev.ps1` does that copy, and can switch Debug settings in the
mod's config in the same run. Before replacing anything it copies the old file to `stage/backup/`, and
`-Restore` puts it back. It exists because an agent's copy into the game was refused twice as irreversible
(2026-09-24): a copy that can be undone is the safe kind.

*Code: `DevReload.cs` (`TryCreate`, `Tick`); `dev-scripts/stage-dev.ps1`, `dev-scripts/copy-dev.ps1`.*

**Getting to a spot without playing there.** Testing a location shouldn't mean playing the story up to it
(2026-09-24). A dev console, off by default, on F9, warps to a map with the game's own door warp and
then stands the party next to the entity with a given flag. So `loc 5` goes straight to location 5's pickup,
because the seed already says which map and flag that is. It can also drop a pickup next to you with the
game's own dig-spot function, and show or set a flag. Before building it we checked the game's leftover test
room: it has debug helpers, but they run unknown old scripts and can't put you by a location. See
`development.md` for the commands.

The agent was then asked to drive it, so the console also reads a **command file** (`DevCommandFile`):
each line written there runs as if typed. The agent writes the file in its own scratch folder, never in the
game's, and reads the answers in the log (`[dev]` lines). `copy-dev.ps1 -DebugSet DevCommandFile=<path>` sets
it.

**It found a logic bug on its first run.** `loc` put the party by a pickup the apworld had as open from the
start, and on screen it was inside a house that opens later in the story. The entity dump hadn't kept which
*inside* (a building's interior) an entity belongs to. It does now, and that location was retired. The
console can't enter an inside yet: it stands you by the item, but only the door's own step lets you pick it up.

**And on its second run it froze the game.** The target room starts a cutscene by itself the first time you
walk in (a map's auto-start list, which the map dump had shown). Arriving by warp, out of the story's order,
the cutscene crashed half-way, and the game stayed "in a cutscene", so the player couldn't move. The fixes:
warps now mark the target map's auto-start cutscenes as seen before arriving, and an `unstick` command runs
the game's own end-of-cutscene cleanup. The log showed the cause straight away (the game's exception, with the
event's name), which is why catching the game's own errors in the log was worth setting up.

**Warping onto water taught one more thing.** The game's own map transfer ends by *walking* the party to its
target spot and waits for that walk to finish. A target beside the item turned out to be over the lake, so the
walk never finished, the transition never ended, and the game kept respawning the party there (the game
had to be restarted to get out). So a warp now aims at the item's own spot, which is standable since the item
rests on it, guards the item from being taken the moment the map exists, and only after the transition steps
to a side with room and safe ground (no wall, no water, spikes or pits), else to the save point. `unstick` also
ends a stuck walk now.

**Which build is loaded, at a glance** (2026-09-27: checking a reload took minutes): the plugin writes one
line to `BepInEx/bugfablesap-reload.txt`, the loaded build's hash (as `copy-dev.ps1` prints it) or what a new copy
waits for; `copy-dev.ps1 -Status` prints it. One read instead of watching the log.

**An apworld change, live** (2026-10-04, the user: "i want to hotswap/live edit things, for faster dev"). Hot
reload swaps the mod, but what the apworld decides (blockers, flags, gives) reaches the game only through a seed's
`slot_data`, so every apworld fix meant a new seed and a new file. Now `dev-scripts/live-slot-data.py` generates the
player file twice with the apworld on disk and keeps the keys both seeds agree on: what the apworld decides, never what
a seed rolls. The console's `liveslot` lays that file over the login's `slot_data`, rebuilds the seed's tables and
re-enters the room through a door. A dev tool for test files: the server still holds the seed's items and locations.

**A warp beside an entity the story hasn't made yet** (2026-10-04): `warp <map> @<name>` matched the inactive entity
and stepped the party into the void beside it. It now matches only an entity that's present, and otherwise lands by
the save point or a door, saying `(<name> isn't present yet)`.

**A queued warp waits out a transfer** (2026-10-04): a `liveslot` sent while the party was still walking in through a
door re-entered the room mid-transfer, and the game's own transfer threw in its fade (`NullReferenceException` in
`TransferMap`, twice). Queued `loc`, `warp` and `liveslot` now also wait while `MainManager.roomtransition` is set.

**Items one at a time** (2026-10-09): testing what each learned ability does alone needs them to come and go one by
one, which a seed starting with all of them can't do. `take` already removed one; `give` adds it back as a received
ability's key is added (only if not held; an item only if the bag has room). The user: "probly nice to have
something for giving/taking the abilities away for testing anyway". First used to hand the Desert Key over for a look
inside the Defiant Root storage.

**Status:** done: hot reload, the build-and-copy scripts and the dev console are in use; `liveslot` seen working
(2026-10-04): three apworld changes in a row (the Golden Path door, its blocker, its tunnel) shown in the running game
with no new seed or file.

*Code: `DevConsole.cs` (the console, the command file, `liveslot`, `take`, `give`), `LiveSlotData.cs`,
`DevConsole.Warp.cs` (`loc`, `warp`, `unstick`), `DevConsole.Party.cs` (`spawn`), `DevConsole.Inspect.cs` (`flag`,
`tree`).*

## 6. Probes: logging what the game does while you play

Reading code tells you what *can* happen. Watching the game tells you what *does*. We added small,
read-only **probes** to the mod. They never change the game, and they write to a log:

- one logs every **key item** that arrives, and every **flag** the game sets (flags are how the game
  remembers what you've done);
- one logs every **item script** the game runs when it hands something out.

Then the tester simply played, and told us when they found something. Each find showed which flag the game
sets for it. That flag is how the randomizer will recognise "this spot is done". It also sorted pickups
into kinds: ones that come back after you leave an area can't be randomizer locations, and one-time ones
can.

**Lesson:** the live log caught a mistake in our reading of the code (every item number was off by one),
which is why things are measured, not just read.

When something left no trace in the log at all, we compared two of the tester's saves instead: one from before
and one from after. The mod decodes both inside the game, using the game's own routine (so the game's key
never leaves it), and lists which values changed. It's read-only and never writes a save.

**Status:** done: the probes and the two-save comparison are in use, off by default.

*Code: `GrantProbe.cs` (key items and flags), `TextProbe.cs` (item scripts, read as the game's
`MainManager.SetText` runs them), `SaveDiff.cs` (the two-save comparison). All off by default, switched on
in the Debug section of the config.*

## 7. Dumps: every map, script, entity and sprite listed

Playing the whole game to find every item would take days, so we also asked the running game directly:
a one-off dump loaded every map's dialogue data and kept only the item and flag commands. Together with
the code, that gave a full list of where key items come from, raw material for the apworld.

*Code: `ScriptDump.cs` (`TryRun`).*

**A second dump for what lies on the ground, and what gates it.** Items lying in the world aren't in any
dialogue. They are *entities*, rows in each map's entity table, which the game parses in
`MapControl.CreateEntities`. Each row also carries two lists of story flags: those the entity needs before it
appears (`requires`) and those that hide it (`limit`). Every pickup, door and blocker is gated by those two
lists, so the dump gives both what each pickup is and much of what the logic has to know about it. The
parser puts every count at a fixed position followed by a fixed-size block, so the dump reads fields at the
same positions the parser does. A second file names every item and medal id. Both files stay in the BepInEx
folder; the facts drawn from them go into `MEASURED.md`.

*Code: `EntityDump.cs` (`TryRun`), switched on by `EntityDump` in the config's Debug section.*

**Each enemy's encounter, for enemy shuffle** (2026-09-26): a map enemy's fight is the list of enemy ids in its
entity row (`battleids`: a count, then up to four ids), which the dump didn't write. It now writes it as a last
column, `battleids`, so the scripts that read columns by name are unchanged. Run at the title screen
(2026-09-26): 327 map enemies, each with its encounter (`MEASURED.md`, "Battles, for enemy shuffle"). The same run
now also writes the enemy table's columns the shuffle needs (`bugfablesap-enemies.tsv`: stats, start position,
can't fall, event on death, and from 2026-09-27 weaknesses, so Kabbu's flip can be read), since that table is game data
the code doesn't hold. Run at the title screen (2026-09-26): 117 enemies.

**Scenery switched by flags** (2026-09-25): a door can be two things, a load-zone entity and a model in the map's
scenery that opens by a flag of its own. The scenery isn't an entity, so the entity dump can't see it. The map dump
now also lists every `ConditionChecker` (hidden or moved by flags) and `FlagAnimation` (animated by flags) in each
map prefab, into `bugfablesap-mapflags.tsv`, read from the prefab without instantiating it.

**Making an entity exist early** (2026-09-25): entities whose flags aren't met still exist in the map, switched
off. Doors are switched on and off every other frame (`MapControl`, by distance, while `CheckIfCanExist` says
"exists"); other objects only once, in their own `Start` (`NPCControl.cs:438`). So `KeptOpen` marks the entity
straight after `CreateEntities`, before `Start`, with a `requires` array of its own that its prefix on
`CheckIfCanExist` answers with "exists", the mirror of how a kept-open blocker gets a `limit` answered "hide".
Seen in play (2026-09-25): the town's door and the plaza's district doors, kept present (the Archipelago guide, build
step 9).

**A kept-away shopkeeper's goods go too** (2026-10-07): a shop's goods (`Fixedshop0`, ...) are made in a second pass
of `CreateEntities`, each pointing back to its keeper (`shopkeeper`), and their own check asks nothing; keeping only
the keeper away left the goods laid out (seen: the snail's goods on top of the caravan's stall). So `KeptOpen` hides
the goods of a kept-away keeper twice: right after `CreateEntities`, for a room the seed's lists reach already loaded,
and in each good's own `Start`, for a room loaded fresh (the first alone missed that case; seen in the log). Seen: the
settlement entrance's caravan stall with its three goods only.

**Item and medal sprites too** (2026-09-26): `SpriteDump` also writes every item and medal sprite with its id and name
(`bugfablesap-itemsprites.tsv`) and their sheets, so a labelled contact sheet can be made from them, to pick an icon
(the Boat Ticket's) by pointing at it. Game art stays local, never in the repo.

**Status:** done: the script, entity and map dumps are in use; making an entity exist early seen (2026-09-25).

*Code: `Dev/ScriptDump.cs`, `EntityDump.cs`, `MapDump.cs` and `SpriteDump.cs`; making an entity exist early,
`World/KeptOpen.cs` (`BeforeCreate`, `AfterNewEntity`, `AfterCreate`, its `CheckIfCanExist` prefix, `ShopGoods`).*

## 8. The Archipelago panel on the main menu

Players need to type a room address, a slot name and maybe a password, so the mod adds **"Archipelago"** to the
game's main menu. It opens a panel drawn with the game's own box and font, so it looks like part of the game,
but it takes real typing: the game itself never reads typed text (its name screen is a letter grid), so the
mod reads the keyboard itself. Backspace, Ctrl+V to paste and Ctrl+C to copy all work. The same panel switches
**the Archipelago mod** (enabled or disabled), which keeps randomizer saves in their own folder so normal saves are
never touched. Its rows, top to bottom (order chosen 2026-09-24): Address, Port, Slot, Password, Difficulty, Detector,
**Archipelago** (the mod on/off, just "Archipelago"; the config's `RandomizerEnabled`). No Back row: cancel backs out,
as the hint box says. Today (the moves are below): Address, Port, Slot, Password, Archipelago, DeathLink (the
Archipelago guide, build step 25), Achievements and *Use on normal saves* (step 18).
Under them, one line explains the highlighted row (2026-09-24: "Detector" alone didn't say it means
the medal), then the connection's state. The game's text colour 5 draws light blue here, not grey, and a long
coloured line looked tilted, so both lines are plain black. The choice rows use the settings screen's own
pieces: its arrow sprite on either side of the value, made once when the panel opens (button prompts rebuilt
on every cursor move replayed their pop-in, so they seemed to shift), and its value-change sound, `Confirm0`
on channel 10, instead of the cursor's scroll sound (2026-09-24).

Several things went wrong on the way, each found on screen by the tester:

- **A fourth menu line landed on top of the credits.** Fixed by spacing the four lines a little tighter and
  moving the game's cursor to match.
- **The panel was drawn behind the logo**, with the main menu showing through it. Fixed by drawing it in front
  and hiding the title screen while it's open, as the game does for its own file select.
- **Backing out crashed the menu.** The game rebuilds its main menu every time you return to it, and its own
  code only knows three entries; our extra fourth one made it read past the end of its list. Fixed by handing
  the game back its three entries before it rebuilds, and adding ours again afterwards.

- **The game's own Settings screen broke**: its leaf cursor no longer lined up. The title screen keeps running
  underneath Settings, and our fix for the main menu's spacing kept moving whatever cursor was active, which
  there was the Settings one. Fixed by only touching the cursor while the main menu itself is showing.
- **Backing out left the leaf on "Archipelago"**, while backing out of Start Game or Settings puts it on
  Start Game (noticed on screen). The game rebuilds its menu on the way back, and the rebuild resets the
  cursor to the top. Our panel doesn't rebuild the menu, so it now resets the cursor itself, a choice made
  to match the game.
- **The leaf sat far left of the labels**, next to the game's Settings screen. The first two nudges were
  measured from single cropped screenshots and went the wrong way ("Address" ended up on the vine border). What
  worked was the tester's **side-by-side screenshots** of both screens: Settings starts its labels ~88 px in from
  the vine, with the leaf's tip right before them, so the labels moved to that distance with the leaf beside them.
  **Lesson:** compare against the real thing in one view, not against a number read off another picture.
- **The leaf didn't wiggle** like the game's (noticed on screen once it sat beside its label). The game gives its
  menu cursor a `SpriteBounce` component when it creates it; the panel's leaf now gets the same one.
- **No sound opening or closing the panel**, where Start Game and Settings have one (noticed on screen). The
  game plays "Confirm" for every main-menu choice before acting on it, and our entry takes the press first,
  so it skipped the sound. The mod now plays the same "Confirm" on opening, and "Cancel" when backing out with
  the cancel button, the sound the game uses leaving the file select.

**Lesson:** when adding to a game's own screen, find every time the game rebuilds that screen, and everything
else that keeps running while another screen is on top of it.

**The file select waits for the first login** (2026-09-24, chosen over keeping a copy of the
seed on disk, which would spoil every location to anyone opening the file). The mod learns the seed only when it
logs in, so a save played before that would hand out vanilla items. How it was built:

1. Find where the file select acts on a file: `StartMenu.Update`, when the file-select screen is up
   (`menuid` 2, `submenu` 0), the cursor is on one of the three files and confirm is pressed. That branch loads
   the save (Event22) or starts a new game (Event8). A second branch starts the secret codes' new game (Event8) with
   no confirm: key 9 on an empty file, once two or more secret codes are unlocked.
2. A prefix on `StartMenu.Update` checks the same conditions first, for confirm and for key 9 (since 2026-10-08, the
   Archipelago guide's build step 60). With the Archipelago mod enabled and no login
   yet this run, it plays the game's buzzer (`PlayBuzzer`), opens a popup and skips the game's `Update`, so the
   game never sees the press. The popup is a dimmer over the whole screen and the game's orange box in the
   middle, sorted above the save slots (their boxes sort at -20 to -60, their text at 10): "Not connected to
   Archipelago", what to do, the connection's live state and OK / Close hints (confirm and cancel; a button's label
   carries its own sort, or it draws behind the box). It sits 0.9 units above the middle, over the save slots. The file
   select stays frozen under it until confirm or cancel closes it. A first try, one line at the top of the screen for
   four seconds, ran over the save slots and was hard to read (seen in a screenshot, 2026-09-24).
3. "Logged in" is a flag the connection sets when `slot_data` arrives. It stays set after a drop, so the rules stay
   in force offline once the seed is known.

*Seen on screen (2026-09-24): with the server down, a save and a new game were both held back with
the popup (keyboard and gamepad hints); once the server was back and the mod logged in on its own, the same save
loaded normally.*

Next it had to feel like the game's settings screen, so the mod rebuilds that screen's look
from the game's own pieces, read from how the pause menu builds it: the same orange box, the controls box
above it with the game's button hints, the game's leaf cursor, labels on the left and values on the right, and
arrows around the On/Off value.

**The help line follows the value (2026-09-26):** on a row whose values mean different things (Difficulty,
Item animation, Medal prices, Enemy scaling), the line under the rows describes the value now chosen, and changes as
left/right steps through them; every step redraws the screen. Since then most rows do; only Fast text, Skip cutscenes,
Detector and the main page's Archipelago on/off keep one line.

**Three pages (2026-09-26; seen on screen):** the main page keeps the connection and the Archipelago
on/off, plus two links, *Quality of life* and *Gameplay*. Gameplay holds how the game plays: Difficulty, Enemy scaling
(moved from Quality of life; its config key stays under `[QualityOfLife]`, so a saved choice carries over) and
Detector. Quality of life keeps the speed-ups, with Disable all / Reset to defaults on top (step 10). Cancel backs out
of a Yes / No first, then out of a page. `ApMenu` tracks the page as an enum. **Rows moved (2026-09-26):**
Shop prices (now Medal prices) to Gameplay, Detector to Quality of life, so Gameplay is Difficulty, Enemy scaling, Medal
prices and Quality of life is Fast text, Travel, Skip cutscenes, Item animation, Detector; each config key stays where
it was, so a saved choice carries over, and each page's two buttons cover its own rows. **The two links left the main
page (2026-09-26: "so AP looks clean"):** the pages are reached only from Settings (below), and *Use on normal saves*
(step 18) took their place under Achievements. A third page, Graphics, came and went on 2026-09-28 (step 28).
Medal prices left the Gameplay page for the dev cheats on 2026-10-09 (step 10, item 7).

**The two pages in game too (2026-09-26; seen on screen, in game and on the main menu).** While Archipelago is enabled,
the pause menu's Settings list gets *Quality of life* and *Gameplay* at the top (with it disabled, only under *Use on
normal saves*, step 18), above Music Volume (first between Key Bindings and Return to Main Menu), opening the same
pages. Neither touches a check or the logic, so changing them mid-save is safe (Hardest is already kept out of the save;
a boss prize reads Hard Mode as the boss falls, and missed prizes are paid anyway). The connection page stays on the
main menu. How (`InGameSettings.cs`): the Settings list is `MainManager.GetSettings()`, a list of ids; an id's label
is `menutext[settingsindex[id]]`. A postfix adds ids 26 and 27 at the top of the list, after two labels appended
to `menutext` and two entries to `settingsindex` (re-added if the game reloads its text). The game draws left/right
arrows on every row but a named few, so a postfix on `ShowItemList` (type 17) removes the new rows' (`Bar<index>`
rows, `slider0/1` children). A prefix on `PauseMenu.Update` catches confirm on them and opens the page
(`ApMenu.ShowInGame`). **Only the Settings screen's two boxes are hidden** (`PauseMenu.boxes`; its list lives inside
them), and the pause menu's `Update` is skipped while the page is open. Switching the whole pause menu off at first also
took its darkened background away: the game view flashed before the page appeared (seen on screen). Now the background
stays, as going from the pause menu to Settings does, and the page skips its own dimmer in game. Cancel shows the boxes
again, on Settings. In the main menu's Settings too (one Settings screen, not two); since 2026-09-26 that is the only
way to them from the main menu.

**Letters going missing (2026-09-26):** the Reset to defaults Yes / No box drew whole, then lost letters
("Ye", no "No") after left / right. The game draws text from a pool of 500 letters, and its own `DestroyText` frees only
every other one until the frame ends (`MEASURED.md`, the text letter pool); behind a page opened from Settings the
Settings list holds many, so a redraw ran the pool dry. Disable all's shorter question just fitted. `TextPool.Free`
frees every letter and replaces `DestroyText` everywhere in the mod, and left / right in the box redraws only the box.

**The status line cut short (2026-09-30):** on the Quality of life page opened from the main menu's Settings, the
bottom line lost its end, "Cancel goes back to Sett", "Setti" or "Settin", only with Fast text, Detector or Uncap FPS
highlighted (the user's screenshots; the Gameplay page never). Those three have the longest help lines on the page
with the most rows. The console's `letters`, sent with the page open, counted **500 of 500 taken**: about 300 by the
page, 135 by the Settings screen's hidden boxes and bars, 46 by the main menu's four options still drawn. A text that
asks for more than are free just ends, and the status line is drawn last. Freeing everything first (`TextPool.Free`)
can't help: the rest belongs to the screens underneath. **The fix, read in the game's code first:** `GetEmptyLetter`
makes a letter with the game's own `NewLetter` for any empty slot of the pool array (`MEASURED.md`, the text letter
pool), so `TextPool.Reserve` lengthens the array to 1000 when the panel builds, and the game fills the new slots
itself, only as letters are needed. Nothing changes until a screen asks for more than 500, which only the panel's
pages do. The log says it once: `[text] letter pool 500 -> 1000 slots (the game fills the new ones)`. **Seen on screen
(2026-10-04):** opened from the title screen's Settings, the line whole with Fast text, Detector and Uncap FPS each
highlighted, and the log's line once.

**Nine rows at a time, scrolled the game's way (2026-09-30):** the Spy Specs row (step 39) made the Quality of life
page twelve rows, and spreading them ever closer stops being readable; the user asked for "the up/down scroll that the
games normal "settings" menu have". **How the game does it, read first:** its Settings screen (pause window 4) shows
`listammount` 9 rows, 0.7 apart (`PauseMenu.cs:2168`), and `MainManager.UpdateList` moves the view only when the
cursor steps past its top or bottom row. The pause menu draws `guisprites[1]` at 1.25 as the list's arrows, turned for
up, 0.3 over the first row while rows are hidden above and 0.2 under the last while rows are hidden below
(`MainManager.cs:15583-15604`, `:16364-16385`). **Ours:** a settings page showed nine rows (seven since, the next
paragraph) between the same top and bottom row as before (the Gameplay page's look, which already had nine), `Scroll`
keeps the cursor's row in view by the game's rule, rows out of view aren't drawn, and each row's value arrows follow
it. **The list arrows:** first placed right of the value arrows and scaled to the panel's spacing; the user, with
screenshots of the game's Settings screen: "the normal settings menu have them more to the side". The panel's box is the
Settings screen's own box (same type, place and size, `PauseMenu.cs:2707`), so the game's spots carry over unchanged:
the box's top-right and bottom-right corners, over the border, at 1.25 (`MEASURED.md`, the Settings list's arrows). Up
from the top row still wraps to the bottom, as the panel always did. The main page is unchanged. **Seen on screen
(2026-09-30):** "yee the scroll works", and the arrows in the corners: "yes this worked correctly".

**The rows at the game's own size (2026-09-30):** the user, "we have scaled down the left/right arrows & the sound bar
things in qol & gameplay as well compared to how the normal settings menu does it". They were (0.75 and 0.68 of the
game's), to fit the panel's closer rows. In the shared box a Settings row is: rows 0.7 apart, arrows at 1 on x 0.4 and
5.4, the value centred at 2.9, ten pips from 1.1 at the game's size (`MEASURED.md`, a Settings row). At that spacing
nine rows fill the whole box, where the panel also keeps its help and status lines; asked, the user chose **seven rows
at the game's size**, both pages scrolling (Gameplay by two rows), the help and status lines staying under them. The
list arrows keep the corners just seen. A value now fits about 11 letters before it shrinks. The main page keeps its
own narrower row. **Seen on screen (2026-09-30):** "yee it works, feels a bit cramped but its how the game does it so
fits in better": kept as the game has it.

**Achievements (2026-09-26; seen held back 2026-10-04, the first boss on Seed A, achievement 5):** an
*Achievements* row on the main page, off by default. While Archipelago is enabled and it's off, Steam achievements
aren't unlocked, as normal saves are kept apart; the help line says it only concerns Steam, never Archipelago. Every
achievement goes through one function, `InputIO.Achivement(id)` (the game's spelling), which asks Steam and sets it;
`AchievementGuard.cs` skips it and logs each id held back once. With Archipelago disabled the game unlocks as usual
(vanilla stays vanilla).

**Text gone inside shops (2026-09-26):** in the shop building the Quality of life and Gameplay pages showed
their arrows and no text, while the game's Settings list was fine. Wrong theories first: the letter pool running dry
(132 of 500 taken), text cleared every frame, depth against the camera. The fix came from putting one of the panel's
letters beside one of the game's in the running game (dev `menuinfo`): **the GUI camera is turned 90° in the shop**, the
game's letters turned with it, and the panel's were not. Its text object was attached with `.parent =` and never had
its rotation reset, so it kept an unturned world rotation and was seen edge-on. Every attached box and text now resets
`localEulerAngles`, as the arrows already did; the Warp button's Yes / No box too. **Seen on screen (2026-09-26):**
the Quality of life page with all its text inside the shop. It follows whatever turn the camera has, so any room that
turns it is covered. The lesson went into CLAUDE.md: read how the game does a thing first.

**Status:** works, seen on screen (2026-09-24): the menu entry, the panel, and the file select held back until the first
login; the Quality of life and Gameplay pages seen (2026-09-26); the Achievements row built (2026-09-26), not yet seen;
the letter pool grown for a long page (2026-09-30), seen (2026-10-04); the game's scroll, its list arrows and its row
sizes on the settings pages seen (2026-09-30); the leaf level with the main page's last row and its help line level,
seen (2026-10-04); the secret codes' new game (key 9) held back too (2026-10-08), not yet seen.

*Code: `MenuToggle.cs` (the menu entry: `BeforeSetMenuText` and `AfterSetMenuText` around the game's rebuild,
`AfterUpdate` for the cursor, `SetMode` for the switch, `HoldBackFile` and `ShowPopup` for the file select);
`ApMenu.cs` (the panel: `Build`, `Redraw`, `Navigate`, `Scroll`, `Close`), `ApMenu.Rows.cs` (`Describe`, `Step`),
`ApMenu.TextEntry.cs` (`TypeInto`, typing); `InGameSettings.cs` (the pages in game); `TextPool.cs` (`Free`,
`Reserve`); `AchievementGuard.cs`.*

## 9. Item swap: a pickup sends its check instead of its vanilla item

With items remote only, finding a location must not give you the game's item there. The game still has to
mark the location done, though, because that flag is how the check gets sent. So the mod leaves the scene
alone and changes just two things inside it: **the item never reaches your inventory, and the item-get
shows what the seed actually put there.**

- **Where to change it:** every item the game hands out in dialogue goes through one command, `giveitem`,
  inside one very long routine. Reading its compiled code (the IL, the instructions the game really runs)
  showed a fixed order: pick the item's sprite, write its name, add it to the inventory, play the item sound.
  The mod rewrites just those calls with a Harmony *transpiler*. It swaps the sprite for the real item's,
  skips the inventory add, and puts the real name in the "You got" box.
- **Knowing it's a location:** the apworld records, for each location, the `giveitem` that hands out its
  vanilla item (map, kind, item number), and sends that in `slot_data`. Only an exact match is swapped;
  every other item the game gives is left alone.
- **Knowing what's there:** at each login the mod asks the server what's at its locations (a "scout", without
  creating hints) and keeps the answer.
- **Safety:** before changing anything, the patch checks the shape it expects. It needs exactly one sprite
  call, one description-box call before it, one inventory add for items and one for medals between the
  sprite and the item sound, and one read of the first-medal flag after the sound. If the game's code ever
  differs, it installs nothing and says so in the log, instead of patching the wrong spot. (The name
  and the starburst aren't among the rewritten calls: the name is swapped in the text the box reads, and the
  starburst is found on screen and recoloured.)

**Lesson:** reading the game's source told us *what* happens; reading its compiled code told us *where* it
can safely be changed. A misread from earlier also surfaced here: the numbers after an item in `giveitem`
had been taken as "who holds it up", and are really "which line of dialogue comes next".

**Tested on screen (2026-09-24):** a seed with the Explorer Permit placed on Artis's medal (Archipelago's
item plando). Talking to Artis showed "You got the Explorer Permit!" with the permit's sprite, and on screen
no medal was added and no second permit appeared. Three leftovers of the medal were visible, so the patch
grew to cover them: **the description box** (the game opens it just before the sprite, now with the real
item's description, or none for another game's item), **the starburst colour** behind the sprite (now the
real item's kind, or its Archipelago colour for another game's item), and **the first-medal tutorial** that
followed. That one is skipped, because no medal was given, and flag 31 stays unset for the real first medal.
Then all three were confirmed on screen with the G-Bug Ranger Plushie placed there instead: its sprite,
its description and the key-item colour, with no tutorial.

**Items lying in the world take another road.** Picking one up doesn't go through `giveitem`. The pickup's own
code sets up the item-get (the name, the sprite held overhead, the starburst, the description box), then starts
a text that ends by marking the pickup taken, `|flag,<its flag>,true|`, and adding it, `|additemtoss,<kind>,…|`.
Two facts made this one easy:

- **Each pickup already carries a unique flag**, set when it's taken (the entity dump showed every one-time
  pickup has one). That flag is the location's, so the mod knows a pickup by its map and flag. Items buried in
  dig spots pop out as the same kind of pickup, with the flag copied over, so they're covered too.
- **The add command has a kind that adds nothing.** Kind 3, the crystal berry's, adds to no list but closes
  the box and ends the text exactly like the others.

So the mod looks at that text just before it starts. When the pickup is a location, it shows the real item
(name, sprite, starburst, description) and turns the add into kind 3. The flag command stays, so the game
still marks the pickup taken and the check is sent. A medal's first-medal tutorial is dropped, as for gifts.
The list of pickup locations comes from the seed (`location_pickups` in `slot_data`), and every location is
now scouted at login, not only the gifts.

**Tested on screen (2026-09-24):** a medal pickup in Snakemouth Den with the G-Bug Ranger Plushie placed on
it (plando). On screen: "You found a Bug Ranger Plushie!", its sprite held up, its description, the key-item
starburst. The log: the medal kept out, the game set the pickup's flag, the check was sent, and the Plushie
came back from the server into key items; flag 31 never flipped, so no first-medal tutorial.

**On the ground too** (seen on screen: the pickup still looked like its vanilla medal before it was touched).
A few times a second the mod gives each pickup location on the current map the sprite of what's really there,
placed the way the game places an item's sprite. The game redraws an item's sprite only when its item id
changes, so the swap holds. Another game's item kept the vanilla look until the Archipelago icon came (steps 21 and
23).
**Confirmed on screen (2026-09-24, screenshots):** the Snakemouth medal pickup lay on the ground as the G-Bug
Ranger Plushie, and picking it up showed the Plushie too.

**Berries** (2026-09-24): the game's berry reward is the same `giveitem` command, but its money branch never reaches
the calls the swap replaces. So at a berry location the mod rewrites the command, just before the text runs, into
a hand-over of an ordinary item it then swaps as usual (`BerryPrefix`), and received berries go through the game's
own money reward. The berry sprite follows the game's own choice by amount. Built, not yet seen in game.

**Story pickups have no flag of their own** (2026-09-24): a pickup the story makes appear and hides for good (the
trapdoor Mushroom) carries no "taken" flag. A first try knew it by its entity name, but the new event log
(`[event]` lines, from the dev console) showed the scene creates its own copy (`tempitem`), so the mod knows it by
the story event picking it up starts instead (`IsPickup`). Built, not yet seen in game.

**Respawning pickups send their own check** (2026-09-24): a floor item hidden only by a regional flag comes back
after every area change, and nothing in the save marks it taken. So the pickup prefix recognises it by map plus
regional flag (the game writes `|regionalflag,N,true|` into the pickup's own text), queues the check itself
(`ApConnection.QueueRespawnCheck`, sent by `LocationChecks`), and swaps the item as usual. Once the check is done
(the server's list, its updates, or queued here), the prefix and the ground sprites leave the pickup alone, so it
gives its vanilla item. The dev console's `loc` refuses a location with no flag (a berry or a respawning pickup) and
points to `warp <map> @<entity>`. Seen in play (2026-09-24, `MEASURED.md`, "Respawning pickups, seen in play").

**A crystal berry spot showing another item** (2026-09-25): the game sets such a spot up for its 3D berry model, with
its sprite centred on the ground and the entity spinning (`NPCControl.cs:937-952`). Showing the seed's item there as
a flat sprite left it half in the ground (seen in a screenshot of the berry outside the cave). The swap now lifts the
sprite by half its height, as the game does for items, stops the spin and squares the sprite to the camera. On screen it
then held still, but the berry model still stood on top, and a second guess (that the model is added twice) failed
the same way. So, measure instead of a third guess: a new dev console command, `tree`, logs the nearest pickup's
whole object tree (each object, active or not, and its renderers, on or off). It showed one berry model, active, with
the item sprite set. The reason is one line in the game: every frame, the first child of the sprite (the model) is
made active exactly when the sprite is enabled (`EntityControl.cs:2781-2786`), and showing the item needs the
sprite enabled. The game toggles the object, never its renderers, so the swap now switches off the model's
renderers, on every pass. `tree` then showed both berry renderers disabled with the item sprite on. **Confirmed on
screen (2026-09-25, screenshot):** the seed's Mistake standing on the ground outside the cave, no berry, no spin.

**Another player's item says whose it is (built 2026-09-26).** Seen in play: Artis's gift held another player's
Sword and the box read "You got the QuestTester's Sword!", which the tester took for their own item. The sentence is the
game's menu text 106, read in the running game (dev `articles`): `You got |string,1| |color,1||string,0||color,0|!`,
the article (`flagstring[1]`) then the name (`flagstring[0]`), both of which the mod already swaps. For that one box the
mod changes line 106 and puts it back the next frame: **"You found QuestTester's Sword!"** (the chosen wording), the
article and its space gone. A party member, who has no article, loses just the article: "You got Vi!". Seen with a
second player (2026-09-26): "You found Other's Key!".
**Pickups have their own line** (seen on screen, 2026-09-26: a picked-up item read "You found a ..."): menu text 2,
`You found |string,1| |color,1||string,0||color,1|!`, built into the text the mod already rewrites for a pickup
location. It kept the vanilla item's article (a Bad Book spot holding the Explorer Permit would say "a Explorer
Permit"). Now the same rules as the gift line: the seed item's own article, none for a party member ("You found Vi!"),
and "You found Player's Sword!" for another player's item, article gone, in the Item colors. **Seen on screen
(2026-09-26):** "You found an Ambusher Medal!" (the medal's own article, at the Residential rooftop) and "You found
Vi!" at the Fountain Rooftop, Vi's icon in a yellow starburst, no description; then another player's items, a gift
and three pickups, "You found Other's Key!" and the rest, in their colours (the Archipelago guide, build step 19).
The pickup line's own "!" is red (it ends `|color,1|!`); after another player's coloured name it looked stray on screen,
so with Item colors on it ends in black there, as the gift line does. Your own finds keep the game's all-red name.

**Archipelago's colours in the line (2026-09-26, three rounds on screen).** The game colours text only from
its own palette (`|color,n|`), which in its scene is 10 colours, not the 7 in the code (dev `palette`; a first try that
assumed 7 showed the game's gray and green, and indices past the end stopped the line). The mod adds Archipelago's text
client colours after them (`HoldUps.AddApColors`, while Archipelago is on), darkened, since the client's are made for a
dark window and fade on the near-white box: another player dark yellow `B8860B` (Archipelago's yellow `FAFAD2` was near
invisible), progression dark plum `8A63D2`, useful dark slate blue `4A6BD8`, filler dark cyan `008B8B`, trap `E9573F`
(Archipelago's salmon was pale, its red would look like the game's red for every find). "from" and "'s" are black. So:
"You got Kabbu (plum) from TestPlayer (dark yellow)!" for an item received from another player, seen; "You found
Player's Sword!" in the same colours for one found here. The player's own finds keep the game's red.
Dev `colortry <hex...>` shows a trap line per colour.

**Planned (2026-09-26): another game's item shows its type before you take it.** On the ground and on a
shop shelf, another game's item still shows the vanilla item's sprite today, which reads as the vanilla item. It
will show an Archipelago icon on a backdrop in its type's colour (Archipelago's: progression plum, useful blue,
trap salmon, filler cyan), the same colours the starburst already uses at pickup. It's a Quality of life row, on
by default, for players who'd rather be surprised. Bug Fables items that belong to another Bug Fables player keep
their real sprite, and the owner's name is in the text. **Built as the Archipelago icon (step 23), used by the
Archipelago icon row (step 21).**

**The description box read the wrong field (fixed 2026-09-26):** the swap showed `itemdata[0, id, 1]`, but the game's
box shows field 2 (`MEASURED.md`, "The item table's fields"); field 1 is "Desc" for every key item. Found while reading
key items' descriptions for the Boat Ticket; not yet seen fixed in game.

**Long names fitted to the box (2026-09-30).** Seen in play: "You got a Poison Resistance Medal from BugTester!" ran
past both edges of the box. The game never wraps that box: it draws the line centred, at full size, however long
(`MEASURED.md`, how a line is laid out). The pickup's box does wrap, but before the name is filled in, so a name is
never measured there either. The user chose **two lines, squashed only if one is still too wide**:

- The line is measured the way the game lays it out, letter by letter with its own `GetLetterOffset`, against the
  box's own sprite (the pickup's: the game's wrap width).
- If it fits, nothing changes. If not, it breaks before "from" ("You got a Poison Resistance Medal" / "from
  BugTester!"), or after "'s" for another player's item found here. The line composers mark that one space; the mark is
  a control character, which no server string can hold (`ServerText` drops them), and it never reaches a save.
- The game centres the block by its widest line, so the shorter line is moved in by spaces to look centred.
- A line still too wide on its own (a name can be 100 characters) is narrowed sideways to fit, the way the game's own
  `clamp` does. The same goes for a long line with no place to break.
- The log says what it decided: `[fit] <width> wide, room <room>: two lines <first> / <second>`, and `squashed` when
  it narrowed one.

Dev `holdup long` shows four such lines: the one seen, a longer one, and the longest name with a player and alone.

**Status:** works for gifts, pickups and their ground sprites, and respawning pickups seen on screen
(2026-09-24, `MEASURED.md`), crystal berry spots (2026-09-25), and a berry reward and a story pickup (the trapdoor
Mushroom, 2026-10-04); the description box's field fix (2026-09-26) seen on a key item's pickup (2026-10-04); long
names fitted to the box (2026-09-30), seen with `holdup long`'s four lines (2026-10-04: broken before "from", the
shorter line centred, every one inside the box) and on a real find ("You found OtherPlayerQuest's / Health Upgrade!",
broken after "'s", 2026-10-04).

*Code: `ItemSwap.cs` (`Enable` finds the routine, `Transpile` rewrites it; `Decide`, `DescWindow`,
`Recolour` and `FirstMedalSeen` do the swapping); `ItemSwap.Pickups.cs` (`PickupPrefix`, `FindPickup`, `TickGround`,
`BerryPrefix`) handles pickups; `ItemSwap.Looks.cs` (`LookOf`, the articles) and `HoldUps.AddApColors`; long names,
`TextFit.cs` (`Fit`, `HoldUpRoom`); the scout is `ApConnection.Scout`.*

## 10. The Quality of life page: Fast text, Skip cutscenes, Warp and more

The goal: a way to skip the intro, the tutorials and other slow parts, as a sub-menu of on/off rows
(2026-09-25). The page isn't only for skips: a later row was planned that changes play, a pause-menu button
back to the seed's start. Such a row is fine in the panel as long as it never changes where items are, and the
logic never counts on it (one exception since 2026-09-30: the Warp with *Points of No Return*, step 38).

The first job was finding out what a "skip" can safely do, so a search through the decompiled game
came before any code. Two findings shaped everything:

- **The game has no cutscene skip**, only its own text fast-forward: holding cancel sets `skiptext`, which drops the
  wait between letters and the `|wait|` pauses, unless a line is marked `|noskip|`.
- **Cutscenes change the world, not just the screen.** One scene hands over the Explorer Permit and a Crunchy Leaf,
  others change the party, load maps or make objects that a later scene uses. So "replace a scene with its end
  flags" would lose items and checks. Skips are built in layers instead, the safest first, and each row must leave
  the game exactly as playing it would. Anything that changes what's reachable is a yaml option, never a panel row.

The rows, all On by default (2026-09-25) and active only while the Archipelago mod is enabled:

1. **Fast text.** Each frame a dialogue box is typing, the mod sets the game's own `skiptext`, under the same
   conditions holding the button needs (a box open, no prompt or list, not `|noskip|`, on the newest line). Each box
   still waits for a press. **Confirmed on screen (2026-09-25):** "text appears instantly".
   With the text instant, holding the skip button still felt slow: the game's hold advances a box, then
   waits out a cooldown of 16 frames (10 when a new dialogue opens). So while the button is held on a skippable box,
   the mod cuts that cooldown to 4, the game's own value while a box is typing; the game's hold code still does the
   advancing. It was a row of its own, "Turbo skip", until the tester felt the difference; it was folded
   into Fast text (2026-09-25).
2. **Skip intro.** The four story slides at the start of a new game run inside the new-game event, so they can't be
   cut out. While they're on screen (the event is running and its black backdrop exists), the mod answers each
   line's wait and runs the game at 8 times speed. The game's own end-of-event resets the speed, and the mod does
   too once the backdrop is gone. **Confirmed on screen (2026-09-25):** on a new file the slides "skipped past
   really fast on its own"; the log showed `[qol] intro slides: passing them by`, then `over: normal speed` (the lines
read `[qol] Event<n>: passing it by at speed` and `[qol] scene over: normal speed` since).
   **Replaced (2026-09-25):** the slides are now cut out after all (item 5, the opening), and the row was folded into
   *Skip cutscenes* ("can probably just be bundled"). The speed-up stays as a fallback if the cut misses.
3. **Free boat** (2026-09-25: nobody should have to farm berries in Archipelago). The Metal Island boat
   costs 300 berries (90 in a later state). The fare isn't in the boat scene but in the sailor's dialogue lines, so
   ScriptDump got a money column (`checkmoney`, `money`), which found both fares on the pier, lines 16 and 19, each
   `|checkmoney,N,20||money,-N|`, and a free trip back. The line a prompt jumps to is read inside the running
   dialogue through `MainManager.GetDialogueText(id)`, not through a new `SetText`, so a postfix there drops the two
   money commands from those two lines. It changes no reachability: with it off, the fare can always be earned in
   battle. Built; seen (2026-09-26), then removed for the Boat Ticket (the Archipelago guide, build step 16).
4. **Warp button** (2026-09-25: a fifth pause-menu button, "warp to start", with a yes/no before it
   acts). The pause menu's row is window 0: `maxoptions` icons (4, or 2 in battle, so the button never shows there),
   made as `sprites[13 + n]`; confirm opens window `option + 1`, and the labels are `menutext[10 + option]` and
   `[50 + option]`. So the mod respaces the four icons, adds a fifth, raises `maxoptions` to 5, catches confirm on it
   before the game would open a "window 5", and writes its own labels. **First try threw every frame:** the game's
   `IconAnim` is handed exactly the four icons and indexes them by option, so the fifth option ran off the end
   ("got a lot of errors"); a prefix now hands it five, and the game animates the fifth like the others. On Yes
   (No is preselected), the menu closes the game's way (`PrepareExit`) and the game's own `TransferMap` takes the
   party to the Outskirts, beside the save point where a new game begins (since *Starting Location*, to the seed's
   start when it has one; since 2026-10-02 through a door, as map travel below). **The icon** ("look at how the
   other menu buttons do things, and do the same"): a tinted Settings icon with the map item on top looked wrong, so
   a new dev dump, `SpriteDump`, saved the game's GUI sheets and a table of `guisprites` indexes (into the BepInEx
   folder, never the repo), and a contact sheet of them showed a round icon in the same style, `guisprites[34]`
   (a blue map). The button is now made exactly like the other four: one sprite, no tint, no overlay. A plugin
   reload with the menu open had left the old icon behind the new one; unloading now removes it. Title "Warp".
   **Seen on screen (2026-09-25, screenshot):** five matching icons, "Warp" above them, the description line,
   and the Yes / No box with No preselected ("this looks good"). The warp itself was seen 2026-09-26. **A second
   IndexOutOfRange, the mod's own this time:** coming back to the main page from another, the menu briefly still holds
   that page's shorter sprite array (8 to 12 long; window 0's is 19), and the button's per-frame check read slot 16 of
   it. It now checks the length first. Lesson: a prefix on a menu's `Update` sees every page's state, not just the one
   it was written for.
   **The logic never counts on the warp** (2026-09-25): it's fast travel and a way out when stuck, but a
   seed must not assume players teleport out, so every one-way drop still needs a real way back in the logic. The
   one exception, since 2026-09-30: with *Points of No Return* the Warp is that way back (step 38).
5. **Skip cutscenes** (2026-09-25: scenes and fluff that give no checks, starting with the two at the
   Snakemouth bridge). **The intro is no longer part of it (2026-09-26):** with Archipelago enabled the
   opening is always skipped, since a random start and a starting party member both need it gone, and the row is what
   players expect it to be, for scenes later in the game. Every scene starts through `EventControl.StartEvent`, so a
   prefix there sees each one by its event number and map. Each scene is read in full before it goes on the list, and it
   gets one of two treatments: *skipped* when it only moves the camera and party, talks and sets flags (the mod sets
   those flags and the scene never starts: the bridge message, Event0, flag 11), or *fast-forwarded* when it also
   changes the world in ways its flags don't cover (the game runs it at 8 times speed with its lines answered, as for
   the intro slides: the rope, Event1, which plays the bridge's Fall animation and fixes it fallen before setting flags
   7 and 11; setting the flags alone would leave the bridge standing until the room reloads). Never a scene that gives
   an item, sends a check, changes the party or starts a battle, **unless the skip does that one thing itself through
   the game's own call.** **The arrival outside Snakemouth Den** (Event11, 2026-09-26: "can we skip this cutscene?"): a
   walk, one line, then journal discovery 0, which is a location. Its autostart (map `autoevent` 22:11) sets flag 22
   itself on starting it (`MapControl.cs:874-882`), so the skip only records the discovery
   with `MainManager.UpdateJounal`, the scene's own call, which also shows the game's discovery pop-up; the check then
   goes as it would. **Seen (2026-09-26):** flag 22 and discovery 0 reset (dev), walked in from the cave's side: no
   scene, the pop-up. **The Tattle tutorial in the bridge room** (Event2, 2026-09-26): Vi and Kabbu walk, one line (map
   line 1, no item, flag, event or transfer command in the script dump), then flag 10, which also hides its trigger
   (`TattleTutorial`); skipped like the bridge message. Built. **Seen (2026-09-26/27, the log).**
   **The door room's puzzle solved** (Event4, 2026-09-26: "can we speed up this cutscene?"): it places the
   two rocks, destroys entities 0 and 1, sets flag 13 and drops the Mushroom whose pickup starts the trapdoor scene
   (`EntityControl.CreateItem`), so it is fast-forwarded, like the rope. Built. **Seen at speed (2026-09-26/27, the
   log).** The trapdoor scene (Event5) itself stays
   at normal speed until it has been seen with three members.
   **The trapdoor scene** (Event5, 2026-09-27, once it had been seen with three): skipped, since the mod's
   trapdoor landing (step 11) now does its other half. What it leaves is flag 14 (location 11's check) and the party in
   the fall room; the skip sets 14, ends it the game's way, and the landing's `TransferMap` goes from the door room
   (flag 14 set, no scene running) through the trapdoor's door. **Seen (2026-09-27): it worked but looked
   wrong:** the party stood idle, a pause, then a teleport, and the trapdoor never opened. So it is fast-forwarded
   instead (the opening and the fall at speed), and the landing places the party after it. The landing itself, after
   the full scene, seen on screen: arrived where the trapdoor leads in.
   **At speed the camera swung far left before the landing** (2026-09-27): the fast-forward drops once the
   scene loads the fall room (the scene list matches by map), and the scene's own landing then placed the party at a
   door-room position (x -22.8, logged by PartyFit), the camera following. So the scene now ends on the black screen
   right after the fall: its coroutine stopped, the party's bodies made normal (the game's `LockRigid(false)`,
   gravity, animations), the camera limits restored, `EndEvent`, the cave music, then the landing's door arrival.
   **Seen (2026-09-27): works correctly:** the opening and the fall at speed, black, the arrival.
   **The spider scene** (Event6, 2026-09-27): two battles, party changes, flag 27 and discovery 1, no
   choice prompt; fast-forwarded. A battle a scene starts now plays at normal speed (the fast-forward checks
   `MainManager.battle` and `inbattle`) and the scene speeds up again after it. Built. **Seen with three members**
   (the same day, at normal speed): Leif stood idle through the scenes; the fights were Kabbu alone, then Kabbu and
   Vi, as the story has them.
   **The first spider fight ends at once** (2026-09-27: a scripted fight, three turns of waiting): the
   game makes it unwinnable (the spider at 999 HP, 99 defence) and ends it itself on turn 3 with `ExitBattle`
   (`BattleControl.CheckEvent`, while flag 15 is set and 27 isn't). With *Skip cutscenes* a prefix on `CheckEvent`
   calls the same `ExitBattle` at the first moment the player could act, only for that fight: during Event6, the
   spider alone, `flagvar[11]` at 0 or 1. The second fight (the spider and Leif in the web, enemy 12, `flagvar[11]` 2)
   is a real one and untouched. **Seen (2026-09-27, flags 27 and 16 reset):** the first fight ended at
   once and the real one began.
   **Leif out of sight in the spider scene** (2026-09-27: with all three, a Leif standing idle next to the
   Leif stuck in the web looked wrong): while Event6 runs in the fall room, a Leif who is a party character
   (`playerentity`, set by the game for the `Player` and `PFollower` tags) is hidden like a stand-in (`PartyFit`'s
   `LateUpdate` pass), so only the map's own Moth shows, in the web; the scene's party changes already take him out
   and the mod adds him back after. **Seen (2026-09-27):** worked fine.
   **First skip froze the player** (seen at the bridge, 2026-09-25): a trigger
   freezes the player (`minipause`) before starting its scene (`NPCControl.cs:5512-5525`), and the scene's own
   `EndEvent` unfreezes. A skipped scene never ends, so the skip now calls the game's `EndEvent()` itself, which is all
   resets (`EventControl.cs:146-187`). Not yet seen.
   **The opening is the one exception, on purpose** (2026-09-25: "just start playing the game"). After the
   slides you play Kabbu alone inside the starting building, and one scene, Event16, stands between you and the door
   (its trigger, entity 9, is hidden by flag 15). It holds Maki's talk, Vi joining, the tutorial battle, the Explorer
   Permit (location 1) and Kina's and Eetl's talk: an item, a battle and a party change, so no flag list could skip it.
   So the scene never starts: as soon as the player is free on that map with flag 15 unset (not only at the trigger,
   which the tester stood clear of, taking it for the scene), the mod leaves what its end leaves
   (`EventControl.cs:3598-3822`), through the game's own calls: `ChangeParty({0, 1})` and `SetPlayers` (the `addleif`
   method), the tutorial's Crunchy Leaf, Vi's stand-in and the `blockingbox` destroyed, the exit (entity 2) active again
   with the default camera, flag 15 and quest 11 on the board. Flag 15 sends location 1's check, and a hold-up shows the
   seed's item. The logic needs no change: Vi is in the party either way, and location 1 was already reachable from the
   start. **First tries (2026-09-25):** (1) the opening waited for its trigger, and the tester stood clear of it, taking
   it for the scene; now it runs as soon as the player is free. (2) The trigger then stayed in the room (flag 15 hides
   it only on a map load), the scene started anyway, since the block only covered "flag 15 unset", and crashed looking
   for Vi's stand-in the mod had removed (freed with `unstick`). Now Event16 is refused on that map whenever the skip is
   on, and the trigger is hidden. (3) The talk after the slides is still Event8, which only the slides' speed-up
   covered; that part (talk and party moves, no prompt) is now fast-forwarded too.
   (4) Still seen: the building and the sped-up talk, since Event8 loads the building and plays there, and the opening
   (or a test start's warp, `TestStart`, which worked: the party arrived in the plaza by its save point) can only follow
   it. A black screen until the start was tried and dropped (hiding it "looks dumb"). (5) So Event8 is cut
   right after its slides: its first step after the slides' backdrop goes is `ChangeParty({1})` (Kabbu alone), while Vi
   is still in the party. A prefix refuses that one call and stops the scene (`StopCoroutine("Event8")`, since scenes
   run as `StartCoroutine("Event" + id)`); the next frame the mod ends it as its own end does (HUD, camera, the
   building's music, `EndEvent`, the fade-in), and the opening and any warp follow before a single line of talk.
   Seen (2026-09-25): straight into the town. (6) The slides still showed, and the test start's warp stepped
   to the save crystal afterwards (the console's warp looks for a spot beside a save point). With *Skip intro* on, the
   cut now comes before the slides, at their first step, the backdrop `NewSolidColor("back")` made after the building's
   map has loaded (`EventControl.cs:2655`); the later talk cut stays as a fallback. The test start uses the game's
   `TransferMap` alone.
   (7) Seen: no slides, but the bottom of the building showed before the warp: the mod removed the slides' black
   backdrop when ending the scene, and the transfer only started after the opening. Now, with a test start, the backdrop
   stays up, the transfer starts as the scene ends, the backdrop goes once the start map has loaded behind the
   transfer's own fade, and the opening runs there. Its building-only steps (the exit, entity 11, the trigger) run only
   in the building, since the same entity numbers are other things on other maps.
   Seen (2026-09-25): the spawn in the town looks right; only the building's music played briefly, so with a
   test start the scene's end no longer starts it.
   (8) The test start put the party behind the plaza's statue: `TransferMap` with position zero is the map's origin.
   **Decided (2026-09-25): a start arrives as if through a door**, the way random starts will work. A door
   holds its target (`data[0]` the map, `vectordata[1]` where the party appears, `vectordata[2]` where it walks,
   `NPCControl.cs:5461`), and it lies on the map left behind, so the mod reads it from that map's entity table at the
   positions the game's parser uses, and hands those spots to `TransferMap`.
   **Seen (2026-09-25): "looks perfect"**: a new file goes from the main menu straight to the town's gate
   from the Outskirts, with Vi and Kabbu and the first check's item, no slides, talk, fight or building on the way.
   (9) **Maki stayed in the building** at his start spot (2026-09-26, on a file with no seed start): flag 15,
   his limit, only hides him on a map load, and the scene itself walks him out and destroys him (`EventControl.cs`,
   Event16). The opening now removes him the same way (entity 4, checked by name, in the building only). **Seen
   (2026-09-26):** a new file starts in the building with Maki gone; the log says `Maki removed`.
   **The rule since (2026-09-25):** a scene that gives an item may be skipped *as long as the item can still
   be received*, and fewer cutscenes are preferred, as an option at least. So a skip now has to keep every check the
   scene holds (sent by the mod, or moved to something the player still does). Next candidate: the
   spider fight with Leif and its scenes, with the discovery granted on entering or leaving the room instead. Not read
   yet: that fight is also the first boss (its prize medal, flag 41 and what gates on it), so each of those needs a
   home.
   **The Termite gate's first opening from outside** (Event149, 2026-10-07: "could we remove/skip this gate cutscene
   without it affecting anything else in the game?"): its first run talks outside, walks the queen off with the
   plaza's guards and lets the escort follower (96) go, then sets flag 384; with 384 already set the same scene only
   rumbles, fades and loads the other side. Its world must still change, so neither skipped nor fast-forwarded: the
   gate's hook from inside (`TermiteGate.cs`, build step 36) now also marks 384 and lets 96 go as the scene starts
   outside, with *Skip cutscenes* on. Setting 384 from the start was weighed and left: the flag also turns
   `BarrenLandsCD`'s left edge into its shortcut, clears a mushroom on an unmapped room and opens Patton's services,
   all earlier than the game does (the user: "option 1 is easier, and breaks less things"). **Seen (2026-10-07):**
   384 cleared, the gate walked up to: "it just faded and put me inside". **Then its trigger** (the user: "can we stop
   the cutscene from warping you inside?"): the trigger in front of the gate (`event`, limit 384) that starts the
   first opening as the party walks up is kept away in a seed (`kept_open`, `logic/forsaken_lands.py`), so the gate
   opens only when talked to, as every later time. **Seen (2026-10-07):** "had to walk up to and use the gate. didn't
   get warped". **And the plaza's half:** the queen and her guard (`elizant`, `eventguard`, limit 384), whom the first
   opening walks off, are kept away too (`logic/termite_capitol.py`), so a party reaching the plaza from inside the city
   before the gate never finds them waiting. **Seen (2026-10-07):** 384 cleared, in from `TermiteIndustrial`: "the
   queen is gone".
   **Talks that start by themselves** (2026-10-08, the swamp's first room: "can we skip the cutscene? it played from
   both entreances properly, but don't think it does or give anything"). A `DialogueTrigger` with `data[2]` 1 opens
   its line from its own per-frame check (`NPCControl.Update`) as soon as nothing else runs, with no `StartEvent`, then
   destroys itself, so the scene list never sees it. `SwamplandsEntrance`'s (`initialmessage`, until 357): line 1
   sets 357 and shows a bubble, nothing else; nothing else reads 357. The user's pick, over keeping it away in every
   seed: with *Skip cutscenes*. So a second list, `TalkScenes` (map, entity, flags), is applied as the room's entities
   are made (a postfix on `MapControl.CreateEntities`): the flags set and the trigger kept away the mod's usual way
   (`KeptOpen.KeepAway`), before its first frame. The rule the user set with it: a scene that changes the logic goes in
   the seed (the wizard's tower, the Archipelago guide's build step 59); one that is only story goes here.
   **The Defiant Root's first visit** (2026-10-09, mapping `DefiantRoot1`: "think we should just add it to the skip
   cutscene panel"): `first visit auto dialogue trigger` (until 170), seen playing from two of its doors; line 52 is a
   party talk that sets 170 and gives nothing (read with the console's `line`). The game also sets 170 on entering
   `DesertDRSouthEntrance` (`MapControl`); nothing else reads it. A second `TalkScenes` row.
6. **Item animation** (2026-09-25): a discovery showed nothing of what it found, and items from other
   players arrive silently. Your own finds always get the hold-up (pickups already did; a discovery recorded in play
   now does too); the row, *Item animation: All / Progression / Off* decides which items from other players do
   (default All, chosen once bursts were fast with the skip button held). Since 2026-09-28 it decides for every
   received item, replays of your own included; only what a scene already showed is never shown twice (step 9). The
   hold-up is the game's own `giveitem`, run on a key item stand-in (an ordinary item's `giveitem` does nothing when the
   bag is full, `MainManager.cs:11499`), held up by the leader; the item swap shows the chosen item and keeps the
   stand-in out, as for a location's gift, in a new display-only mode. The follow-up line `giveitem` always shows is an
   empty one the mod answers for a reserved number. Hold-ups wait in a queue for the same free moment the receiver waits
   for (no battle, scene, dialogue, menu or map change), one at a time; the item itself is always given by the receiver,
   never by the hold-up. A discovery already recorded when the save loads shows nothing. **A discovery's hold-up in play
   (2026-09-25, log):** the spider fight recorded discovery 1, the check went out, and after the fight chain the swap
   held up the seed's Mushroom for that location and kept the stand-in out. **Seen (2026-09-25):** a test hold-up from
   the new console command `holdup` ("Explorer Permit from TestPlayer") waited for a cutscene to end, then played; the
   item probe saw nothing added. The box read "You got a Explorer Permit": `giveitem`, like a pickup, sets the article
   of the item it gives (`itemdata[0, id, 3]`, a medal's `badgedata[id, 6]`; `MainManager.cs:11545`, `:11554`), and a
   hold-up gives the stand-in, item 0, whose article is "a". Hold-ups and location swaps
   now set the shown item's own article. Not yet seen. A hold-up now waits for 30 free frames in a row (half a second at
   60 FPS; under Uncap FPS counted in sixtieths of a second, step 24), not one free frame: a chain of scenes and fights
   (the spider fights) can leave a free frame between links. **Seen (2026-09-25):** three queued test hold-ups waited
   through the spider fights' chain, then played one after another, reading "You got the Explorer Permit from
   TestPlayer!" (the game's own article for it). Each was followed by an empty box: an empty follow-up line is still
   shown as a box waiting for a press. The follow-up is now the game's `|end|`, which skips that wait. Confirmed on
   screen the same day: no empty box. **Bursts** (50 were queued to see what *All* feels like when a multiworld sends
   many at once: about a minute of boxes). Only the first of a burst waits for the settled half second; the rest follow
   as soon as the previous box closes. A summary box ("...and N more items from other players!") past three was tried
   and dropped: it felt off to the tester, so every item gets its own box. Instead, **holding the skip button runs the
   game at 4 times speed while one of the mod's hold-ups is on screen**, since the item-get's own pauses are fixed waits
   that fast text doesn't shorten; only a speed-up the hold-up made is undone. **Seen (2026-09-25): "better"**,
   and *All* became the default. **Replays are shown too** (2026-09-28: "all items appear, even on a reconnect"). A new
   save starts at 0 received and the mod gives it everything the server has for the slot, oldest first, which is what
   makes a lost save recoverable; each of those items now gets its hold-up, per the setting, as items arriving during
   play do. At first replays were silent (only items past the count the server had at
   login, `ApConnection.ReceivedAtLogin`, were held up), so a new file showed nothing for a check with no scene of its
   own. The one exception left: an item whose check's own scene just showed it on screen (`ItemSwap.ShownInScene`,
   filled when a pickup or gift shows the seed's item, used up by that item's arrival). An item shop purchase and a
   journal discovery queue a hold-up of their own for the seed's item, so they fill it too: one's own item bought
   showed twice, the purchase's box then "received from" (parked 2026-10-04, reported again 2026-10-07); **seen
   (2026-10-07):** three purchases with one box each. A give whose line sets its flag first (`flag,91,true||break||
   giveitem`, the farmer's reward) had its check sent before the give, so the give counted as a replay and the item
   came in two boxes; a check found in play in the last 15 seconds now counts as that find
   (`LocationChecks.FoundJustNow`).
   **Seen (2026-10-07):** Chubee's gift, the same order, in one box. "Arrived after login" was tried
   first and missed a replay in a second new file of the same session: Meditation, found in the file before, came in
   with no box (2026-09-28: "I expected it to be remote"). **Seen (2026-09-28):** on the next new file Meditation
   arrived with its box, and the opening's items stayed quiet. On *All*, a new file late in a seed plays a
   hold-up for every item; *Progression* or *Off* shortens that. **A quiet start** (the same day: six boxes in a
   row on a new file was "a bit much"): starting items (sent by the server itself, slot 0) and the items of the
   opening's three checks (`quiet_locations` in `slot_data`, marked `quiet` in the apworld's `logic/`) arrive with no
   hold-up. A party member placed at any other location still gets its box. The opening skip used to queue its own box
   for Maki and Eetl's Gift, standing in for the gift scene it skips; it now skips that too when the check is quiet.
   **An item from the server console is held up (2026-10-04, the user's choice):** a `/send` also comes from slot 0,
   so the quiet start kept it silent too (seen: a Crystal Berry arrived with no box). Archipelago's protocol tells
   them apart (`network protocol.md`, 0.6.7: location `-1` is `Cheat Console`, `-2` is `Server`, "typically Remote
   Start Inventory"), so only `-2` stays quiet now. Seen the same day: three Mushrooms from `/send_multiple`, a box
   each.
7. **Shop prices** (2026-09-25: Normal by default, Half or Free). The medal table's price columns (5 for
   berries, 7 for crystal berries) are scaled in memory, from a kept copy, and put back when the row is Normal or the
   mod is off. The logic never counts on it. **Now a bar on the Gameplay page (2026-09-26):** 0 to 10 pips
   like the multipliers (step 19), each a tenth of the price: a full bar normal (the default), 5 half, an empty bar free
   (1 = free was asked first; an empty bar keeps 5 = half and 10 = normal exact). Any price above free is at least
   1, rounded up, so small crystal-berry prices stay 1 on the low settings. A new key (`[Gameplay] MedalPrices`, a
   number), as the old one held a word. **Renamed *Medal prices*** (2026-09-26: more accurate): it scales
   the medal table, so every medal on sale anywhere, never an item shop's consumables. If item shops are ever scaled,
   they get their own row (*Item prices*), as their prices sit on another scale. **Moved to the dev cheats
   (2026-10-09, the user: "that one feels a bit to cheaty"):** off the Gameplay page and out of the player guide; the
   dev build's `[Debug] MedalPrices` (the same 0 to 10, 10 by default) and the console's `medalprices [0-10]`. The
   release build binds no such key, so its shops charge the full price; a value set under `[Gameplay]` before is
   left unread.
8. **Skip battle tutorials: read, and Leif's line skipped (2026-09-27).** A battle's scripted moments are
   `BattleControl.EventDialogue` cases, started by `CheckEvent` or by an enemy's own action. The only real tutorial is
   the fight against Maki in the opening (cases 0, 1 and 2, enemy `MakiTutorial`, while flag 15 is unset), which the
   opening skip already removes. The spider's first fight ends at once (item 5). What's left: case 3, Leif's one line in
   the first battle after he joins (flag 16 set, 24 not; it sets 24; `SetMaxOptions` reads 15 and 16, not 24), and
   story lines inside boss fights (cases 7, 8 in the first boss; others later), which stay. **Leif's line is always
   skipped with Archipelago on** (2026-09-27, even with Skip cutscenes off): once flag 16 is set and 24 isn't,
   the mod sets 24, outside a battle. Flag 24 also lets enemy 1 appear on the ground instead of always in the air
   (`MainManager.cs:6299-6309`); the game sets it with that same line, so only the moments between Leif joining and his
   first battle change, and only toward easier. **Seen (2026-09-27):** flag 16 set by the dev console at the
   lake (the lake scene hadn't triggered before the spider), the mod logged the line marked said, and the next fight
   started without it. *Code: `PartyMembers.cs`.*
   **Travel: Off / Warp / Map / Both (2026-09-26; seen on screen).** The Warp button's on/off became one
   *Travel* row (config `Travel`, default Both). `WarpButton.cs` now places the travel buttons after the game's four:
   Warp, then Map (left from the first button wraps to Map; Warp sits between, harder to hit by accident). Five fit
   two apart as before; six sit 1.7 apart (seen in screenshots: 1.8 pushed the first off the panel, 1.6 made them
   touch). The game's four are placed at their final spots as the game makes them (a postfix on `NewUIObject` for the
   `menuicon` objects): moving them a few frames later made them jump and overlap while the menu opened. The map
   shortcut already holds sprite 18, so a second button takes sprite 19 in a grown `sprites` array, and `IconAnim` is
   handed one entry per button. **Icons:** Map gets the round blue map (`guisprites[34]`, Warp's icon until now: "fits a
   map more"), Warp gets the map item's scroll (`itemsprites[0, 41]`, "like a return scroll"), so the two differ without
   a tint. The blue map is one finished sprite with its round backdrop painted in; the scroll is an item sprite with
   none ("don't have a background thing"), so it gets one. The game's white circle (`guisprites[59]`) came with its own
   dark outline and shading and looked off (the others are one flat ring and one flat inner colour, and brown and pale
   was dull). Now the mod draws the backdrop itself: a texture the size of the blue map icon, a flat ring round a flat
   fill, in the game's own colour recipe, measured from its icons (`MEASURED.md`, "The round pause-menu icons'
   colours"): ring at full saturation and 0.51 brightness, fill at 0.34 saturation and full brightness, one hue. Guessed
   colours kept looking off (pale brown, a pale orange, teal that blended into the green and blue beside it, a vivid
   orange that stuck out, a ring and fill that read as two colours); the map icon was the one that "nailed the color
   scheme", and measuring it gave the recipe. **Lime, hue 0.28 (chosen)**: colour-wheel spacing put it in the row's
   biggest gap (gold 50° to green 155°), after orange at the recipe turned salmon (0.05) or brown (0.08, a dark orange
   is brown) and pink (0.9) sat too close to purple and red. `warpcolor` (dev) still tries a hue. **The scroll stays the
   game's own art:** softening a copy (black outline to dark lime, colours lifted) was tried and looked worse each time
   (first the copy took another sprite from the atlas, as a render texture's rows run the other way on Direct3D; then
   dark reds were caught as outline; then it read flat and washed out), and none of the game's other round icons (key,
   leaf, the library tabs) means "warp". A drawn icon would be art work, not code. **Then the leaf:** of the game's
   premade round icons (the key and leaf of the item categories, `guisprites[23]` / `[22]`; the Library's tabs, where
   the map icon comes from), the leaf "looks more as the game intended" than the scroll on a drawn backdrop, so Warp
   uses the leaf, unchanged ("leaf" as in leave). **Design rule (2026-09-26): the game's own art whenever it
   fits**, over adapted or drawn assets, as the panel's pages are built to look and feel like the game's Settings
   screen. The drawn backdrop stays for the dev console's `warpicon scroll`; it is the button's own sprite (so the
   game's outline and wiggle apply), the scroll on top. **Map** opens the game's own map window (6) the way its map
   shortcut does (`windowid = 6`, `BuildWindow`), in a travel mode: confirm on a visited area (`librarystuff[4, area]`)
   opens "Travel to \<area>?" (No first) instead of flipping the description's pages; the map opened any other way keeps
   vanilla controls. On Yes the menu closes the game's way and `TransferMap` lands the party beside the area's travel
   spot: a save point at its entrance or hub (starting choices, `AreaSpots`; the Outskirts use Warp's start spot), from
   the entity dump and each map's area (the map dump's new `area` column, `MapControl.areaid`). **The first try threw
   every frame** ("a lot of errors"; a one-time diagnostic finalizer on `PauseMenu.Update` logged the state): the map
   read the pause menu's leftover `option` (5, the Map button) as an area and drew toward a marker that didn't exist,
   and no area was marked visited at all, as a new file never marks its starting area (`MEASURED.md`, "Visited areas and
   the pause-menu map"). Opening the travel map now sets `option` to -1, the map's own "none yet", and marks the area
   the party stands in as visited (the one field `UpdateArea` writes). **Then no box showed:** it opened (the log said
   so) but behind the map, a 3D object at depth 5 on the GUI camera; the map's box now hangs off the GUI camera at
   depth 2. **Seen on screen (2026-09-26):** the map opened without errors, "Travel to Bugaria Outskirts?" showed over
   it, and Yes landed the party beside the start's save point ("a good location"); then back and forth between the
   Outskirts and Defiant Root (after a dev warp there), both working. The other areas' spots are starting choices,
   checked as they're reached.
   **Through a door, since 2026-10-02.** The swamp's spot looped (the user):
   - map travel there, then a jump into the water by the crystal: the party came back over the water, again and again,
     with the pause menu out of reach.
   - The cause, read in the code (`MEASURED.md`, "Where the party comes back"): the travel used `TransferMap(map,
     spot)`. Its spot, beside a save point, was the mod's guess, and the game took it as the place to put the party
     back after a fall. The swamp's crystal sits low by the water.

   The user's fix: "any warp/map/teleport, always acts as if you are coming in from an entrance".
   - Map travel and Warp to Start now arrive the way a door does, `TransferMap(map, here, appear, walk to)` read from a
     real door's entity data (`QualityOfLife.DoorInto`, which gained a door name). The game itself then sets where a
     fall comes back, where the walk-in ends.
   - Each destination's door is in `AreaDoors`, the Outskirts (area 0) also Warp's. `dev-scripts/door-graph.py
     --travel` picks it from the entity dump, and `--travel --check` checks the table. It's a mode of the door tool,
     not a script of its own: a new script loading another by path is a new row in `docs/capabilities.md`, the user's
     call. The rule: of the doors into the map, the one whose walk-in ends nearest
     the old save point, among doors with no camera change or jump on arrival (a transfer without the door can't copy
     those) and no flags of their own (a door that exists only in some story state may land on scenery that does
     too).
   - Its first version also asked the way back to have no flags. That sent Warp to the far side of the Outskirts (the
     city gate's way back needs flag 107) and changed nothing about the ground, so it went.
   - A seed's room start arrives with its walk too (it used to land on the walk's end), and the save-point start
     (designed, not built) logs an error instead of landing beside a save point.
   - The dev console's `travel <area>` runs the same path to any area, visited or not, to check every spot.
   - The respawn-loop guard (step 40) catches any loop left.
   - **Seen (the user, 2026-10-03):** every map travel destination, one by one, from the pause menu's map with its
     Yes / No box: "All map fast travel locations work
     properly now", nothing odd since arriving through a door "instead of random spawn next to crystals".
   **Skip confirm: Off / Warp / Map / Both (2026-09-26; seen on screen the same day: on Map, map travel went at once and
   Warp still asked),** an add-on to *Travel*, so its row sits right below it (the two belong together, not split
   apart). Warp: picking the Warp button warps at once; Map: confirm on a visited area in the travel map goes there at
   once; Off (the default) keeps both boxes. Disable all sets it Off (asking is the safe value). The press that would
   open the box goes straight to what Yes did, through the one method both share (`Go`), so the two can't drift apart;
   its log line says when no box was shown. An area not visited still gets the buzzer. Config `[QualityOfLife]
   SkipConfirm`.

The panel got an eighth row, "Quality of life", which opens a second page in the same box; cancel comes back (the
pages are reached from Settings since 2026-09-26, step 8).

**Disable all and Reset to defaults (2026-09-26; both boxes seen on screen the same day; the Reset box's lost letters
fixed, step 8, the fix seen 2026-10-04: "Yes" and "No" whole through left / right).** Two buttons side by side at the
top of the Quality of life page (not rows in the list); left/right picks one. **Reset to defaults is on the left, where
the cursor lands** (entering the page shouldn't put you on Disable all), Disable all on the right, its label about 2.3
wide at size 0.8 (measured on screen), so the leaf sits 1.15 left of its centre. Confirming one opens a
**Yes / No box** over the page (a box, not the choice inside the menu), built with the game's own box
(`MainManager.Create9Box`, the controls type the help box uses), the question on top and the leaf on the answer. No is
picked first, so a stray press never wipes the settings; cancel closes the box. The question isn't repeated in the help
line below. **The Gameplay page has the same two buttons** (`ApMenu.GameplayAll`): Disable all there sets Difficulty
Normal, Enemy scaling, Attack boost, Healing crystals and Auto-save Off, and both multipliers 1; Reset
puts each back to its default. Both pages open on Reset to defaults. On the Quality of life page, Disable all turns
every row off (a choice row to its off value: Item animation Off, Travel Off, Detector Off) and Reset to defaults puts
every row back to its default (`QualityOfLife.DisableAll` / `ResetAll`, the defaults from each setting's own config
definition).

**Status:** in progress: Fast text, the opening skip, the Warp button's menu and Item animation seen on screen
(2026-09-25); the bridge skips seen (2026-10-04: the message skipped, the rope at speed, the bridge still down on
coming back); Medal prices not yet seen, a dev cheat since 2026-10-09; replays held up and the quiet start seen on
screen (2026-09-28); Free boat seen (the fare waived with no berries, the boat left, 2026-09-26) and then removed for
the Boat Ticket (the Archipelago guide, build step 16), the warp itself, map travel and Skip confirm seen (2026-09-26);
Skip cutscenes' Den arrival seen (2026-09-26), the Tattle tutorial and the door room's puzzle (Event4) at speed
(2026-09-26/27), the trapdoor and spider scene (2026-09-27); Skip battle tutorials: Leif's first-battle line skipped,
seen (2026-09-27); map travel and Warp to Start through a door built (2026-10-02), map travel's seen at every
destination (2026-10-03); the Warp button since the hooks moved, Warp to Start through the city gate and through a
seed start's door seen (2026-10-04); the swamp's water jump not yet seen; the Reset box's letters and Item animation's
reworded help lines seen (2026-10-04); the swamp entrance's talk skip seen (2026-10-08: 357 cleared, in by the left
door, "it was skipped, nothing happened").

*Code: `QualityOfLife.cs` (the settings and the per-frame speed-ups), `QualityOfLife.Opening.cs` (the opening),
`QualityOfLife.Scenes.cs` (the scene skips, the first spider fight, the trapdoor), `ApMenu.cs` (the second page),
`WarpButton.cs` (the travel buttons, `AreaDoors`), `HoldUps.cs` (item animation's hold-ups);
`dev-scripts/door-graph.py` (`--travel`).*

## 11. Missing party members: stand-ins in scenes, and followers

Bug Fables' scenes are written for a party of two or three, but a seed can start with one member and add the
others as items. So every scene, talk and follower has to cope with a member who isn't there: the mod fills the
gaps with invisible stand-ins, and the problems below were found in play, one at a time.

**Scenes with a member missing get a stand-in** (2026-09-25: make scenes work with one or two members;
the barkeeper's first talk, Event83, crashed twice on `p[2]` with Vi and Kabbu). Scenes take the party as a list and
use fixed slots (about 110 lookups). While a scene runs, `GetPartyEntities` returns three: each missing member is
an invisible, collision-free stand-in with that member's `animid`, made the way the game makes scene characters,
in the member's own slot (id order) or after the party, and removed when the scene ends. Outside scenes nothing
changes, and no scene tests a member with `GetEntity(-6) != null` (grep). A member asked for by name during a scene
(`GetEntity(-4)` to `(-6)`) gets the same stand-in. Why not the leader, as for followers? Because a scene
moves every member at once, so the leader would be pulled to two spots and play another character's animations. Limits:
a scene that changes the party, or needs a member's ability, still needs the member (a logic rule, as for the boat);
some scenes will look odd, and each one that used a stand-in is logged, to skip or hold back one by one. **Seen
(2026-09-25):** the barkeeper's first talk played through with Leif's stand-in (naming Leif, as expected), so it went on
the skip list, only while its flag 158 is unset: the same scene later takes bounties and gives their rewards.

1. **Without a test start, the party stood under the house** (2026-09-25: "the weird broken location" seen
   briefly before step 10's (7) fix). Event8 places the party only after its slides (Kabbu 2.5 left of entity 4,
   `EventControl.cs:2770`), so cut before them the party kept a new game's raw spawn point; every test since step 10's
   (6) had a test start, whose warp moved it away. The opening now stands the party where the scene would have. Seen on
   screen: right spot, but the fade-in first showed the spawn point, then a jump: the opening runs a few frames after
   the scene's end, and the fade-in starts at that end. So the scene's end (the mod's) moves the party there and snaps
   the camera before the fade-in. Seen with only the first half loaded: the right spot, then a snap back once the
   opening ran, since it waits for the fade-in to end and the player can walk during it. So the opening no longer places
   anyone: it uses where the player stands. Seen on screen: right spot, no snap, but outside the house the camera was
   broken with Leif alone. `ResetCamera` at the scene's end aims the camera at the leader, the opening's `ChangeParty`
   then destroyed that character (with Vi and Kabbu, Kabbu's was reused), and leaving the house hands the camera back to
   the player only for insides that centre on themselves (`MapControl.cs:1373`). Event16 itself ends
   with `ResetCamera()` after its party change (`EventControl.cs:3795`); the opening now does too. **Didn't help**
   (seen: still low outside, and stuck after the gift). Two guesses failed, so measured: a console command `cam` logs
   what the camera follows. It read `target DESTROYED` with the leader (`Player 0`) fine. Unity destroys an object at
   the end of the frame, so the opening's `ResetCamera`, aiming at `MainManager.player`, still found the old leader's
   character and followed it as it vanished, leaving the camera where it last stood, under the house. The opening now
   aims the camera at the new leader's character itself (`playerdata[0].entity`). Seen on screen: the camera fine from
   the gift on, but Vi and Kabbu showed for a moment and the camera was odd outside until then: the opening swaps the
   party only once the fade-in is over. Moving the swap into the scene's end, before the game's `EndEvent`, crashed it
   (`FixEntities`, a NullReferenceException, a black screen; freed with `unstick`). Now the scene ends as before behind
   the black screen, and on the next frame the party is swapped, placed and the camera set, then the fade-in
   starts. **Seen (2026-09-25):** Leif alone from the first frame, in the room, the camera right inside, outside and
   after the gift.
2. **Stand-ins in conversations too** (2026-09-25, Leif alone): Artis's talk hands lines to Vi and Kabbu
   (`|next,-4|`, `|next,-5|`), and `SetText` resolves a speaker through `GetEntity` (`MainManager.cs:12385-12398`,
   `:18418-18440`). The stand-ins only answered while a scene ran (`inevent`), and a talk isn't one, so the lookup came
   back empty and `SetText` threw a NullReferenceException at the end of the talk. They now answer while a scene or a
   conversation runs (`inevent` or `message`) and go when both are over. Kept that narrow on purpose: an invisible
   member around all the time could be counted as real by battles, followers or menus. **Seen on screen:** Artis's talk
   played to the end and the permit's check went out (the crashed one had left a dead dialogue: `unstick`).
3. **A stand-in arrives at once** (2026-09-25): the horn tutorial (Event10, near Snakemouth) walks Vi to a
   spot and waits until she's there (`while (entities[0].forcemove)`, `EventControl.cs:2935`); the stand-in, without
   collision, never arrived, and the scene never reached its first line. Every `MoveTowards` overload ends in the
   five-argument one (`EntityControl.cs:4911-4960`); a postfix puts a stand-in straight on the spot and ends the walk.
   The scene's cut itself is done by the scene (`CutGrass()` after the action button), not by Kabbu. **Seen on screen:**
   the tutorial played through and its reward was sent.
4. **Stand-ins stay where they're put** (2026-09-25): the trapdoor scene ran with stand-ins, but Leif
   landed "down/left at a rock" instead of on the mushroom. Its end places member m on the m-th scene character's spot
   (`EventControl.cs:1476-1484`), so Leif took stand-in Vi's; a stand-in had its collision switched off but not its
   gravity, so it would sink through the floor. Stand-ins are now kinematic, without gravity. The likely cause, not
   measured. **Wrong** (seen: now all the way left). Read in the code instead: just before placing, the scene runs
   `PartyMover`, which walks every party member, stand-ins included, to `MainManager.player`, and with one member the
   player (Leif) isn't in the scene: he stood where the fall room put him on loading, far left. Item 3's instant arrival
   then put stand-in Vi on him, and the end put Leif on her spot. So a stand-in sent toward the real player stays where
   the scene last put it, and the positions the scene hands `SetPlayers` are logged, with the stand-ins' and the
   player's. **Seen (2026-09-25):** Leif landed at the right spot; the log: placed at (12.7, 6.5, 0.3), stand-in
   Vi's landing point, with the player at (-21.9, 0, 0) before.
5. **A stand-in has its physics body at once** (2026-09-25): the spider fight's lead-in (Event6) made a
   stand-in `Jump()` in the frame it was made, and `Jump`'s `Unfix` needs the body (`rigid`), which a new character only
   gets in its `Start`, a frame later (`EntityControl.cs:524-528`): NullReferenceException, the scene dead (`unstick`).
   The stand-in now gets its body when made, weightless; `Start` adds one only when there is none. Not yet seen.
6. **Stand-ins hidden at the last moment** (2026-09-25: their sprites flashed now and then): the
   per-frame hiding ran before the scene's step and the character's own updates, which could switch a sprite back on
   for a frame. A postfix on `EntityControl.LateUpdate` (`EntityControl.cs:3672`) hides a stand-in after both, just
   before drawing. First reload with the guard: it waited for the running scene. Not yet seen.
7. **A scene's end hands over to the real party** (2026-09-25: after the spider fight's end, Leif stood to
   the right instead of where the scene leaves the party). The end of Event6 (`EventControl.cs:2272-2289`) walks Vi and
   Kabbu to the spot and never the player, whom it takes to be one of them, and sets the fall room's character to follow
   Kabbu (`entities[2].following = entities[1]`, then `extrafollowers.Add(2)`). Now, before the stand-ins go: if the
   story's leader (the first member of the party the story last asked for, remembered by the member guard before it
   filters) was a stand-in, the real party moves to where it was left; anyone following a stand-in follows the real
   party's last member. Both logged. Not yet seen.
8. **A party member isn't also a follower** (2026-09-25: three Leifs after the spider fight, one standing,
   one trailing the player). In the story, Leif meets the party after that fight and follows until he joins at the lake:
   the scene's third character is the room's Leif (entity 1), set to follow Kabbu, and `extrafollowers.Add(2)` (ids are
   characters: 0 Vi, 1 Kabbu, 2 Leif; removed when he joins, `EventControl.cs:3533`), from which every map load makes a
   follower (`MapControl.cs:826-829`, `AddFollower`, kept in `map.tempfollowers`). With Leif the one member, that's the
   player plus two copies. Now the member guard, each frame, takes any party member off `extrafollowers` with their
   follower copies, and a scene's end removes a party member's character that followed a stand-in. On first load it took
   Leif off the list but found no copy in `tempfollowers`: the copies were the scene's own character, not made by
   `AddFollower`. **Still a copy** (seen: a second Leif copying every move). Measured with a new console command,
   `who` (every character drawn as a party member): two "Player 0", both player characters. The spider scene calls
   `ChangeParty({0, 1})` then the no-argument `SetPlayers()` (`EventControl.cs:1711-1712`), which makes new player
   characters without removing the old ones; in the story those become the scene's actors, but with stand-ins the old
   Leif stayed, controls and all. Now, outside scenes, twice a second, any character with player controls other than
   the leader's is removed (logged). On loading it removed the stray at once; `who` then listed one player. **Seen
   (2026-09-25):** no extra Leif any more.
   **The whole sequence replayed (2026-09-25): "worked perfectly"**, no extra Leif, the right spot after the
   ending. The log: the fall placed at (12.7, 6.5, 0.3); Leif taken off the follower list; the scene's end moved the
   party to stand-in Vi's spot (-44, 0, 1.2); the story's Leif ("Moth") removed as a copy; the stray player removed.
   Items 7 and 8 seen with it.
9. **The leader acts the story leader's part** (as wanted in `apimplementation.md`): at a scene's or talk's
   first stand-in, if the story's leader isn't in the party, the real leader plays that member (walks, faces, is placed
   where they would be) and only other missing members stay invisible; chosen once per scene; in a party list by member
   the leader's own slot gets an invisible stand-in so he's never moved twice. **Seen (2026-09-25):** Leif
   acting the story leader's part in the scenes ("doing the funny animation things"), a screenshot of the
   treasure room scene with Leif speaking the party's line. **Then cast twice** (2026-09-25: Leif didn't move in the
   briefing, where Kabbu and Leif stand back while Vi gives the artifact to the Queen): Event45 asks for Vi, Kabbu and
   Leif by name; Leif acted Vi but the request for Leif still found him, so he followed whichever order came last and
   stayed back. Now a by-name request for the acting leader's own member gets an invisible stand-in, as his slot in a
   party list already did. **Replayed and seen:** Leif acted Vi in the palace entrance, but the scene then reloads into
   the throne room with the party remade, the actor was lost, and Leif played himself there. The verdict: that's
   right once the story has Leif ("wrong to force one member to do the others' part"). So: **a leader the story's party
   already holds plays himself** (Vi from the opening, flag 15; Kabbu always; Leif from flag 16); he acts the lead only
   in scenes whose party doesn't have him (chapter 1 before Leif joins). And after a scene remakes the party characters,
   the new leader takes the acting part on again (for chapter 1 scenes that change maps). **Seen
   (2026-09-25):** the briefing replayed with Leif playing himself, "working as intended".
   The briefing's hold moved from 114 to 66 (the bridge swap: a shuffled door or a random start inside the
   palace could reach it with no follower or the wrong one), `apimplementation.md`.
10. **Leif's joining scene skipped when Leif is already in the party** (2026-09-25): Event14 at the lake
    takes its Leif from the follower list (`map.tempfollowers[0]`, `EventControl.cs:3339`), empty since item 8, and
    threw `ArgumentOutOfRange` at its start (predicted from the code a moment before the tester reached it; freed with
    `unstick`). A prefix on `EventControl.StartEvent` doesn't start it and leaves what it leaves: flag 16, the regional
    flag of the creature it removes (entity 5) with the creature gone, Leif off the follower list. **Then always
    skipped** with Archipelago on ("it's not a check"): it's no location, only the logic's *Leif Joins* event at the
    lake (flag 16), and without its fight the lake no longer quietly needs Vi. When Leif isn't in the party yet, he
    joins right there, as the scene's own `ChangeParty({0, 1, 2})` would have him (then `SetPlayers`, the camera on the
    leader); with one starting member the guard still decides whether he may. **Moved earlier** ("it could just happen
    after the spider, when Leif first starts to follow"): once the spider scene is over (flag 27, not yet 16), Leif
    joins for real, flag 16 goes on, and the story's follower Leif is removed with his follower entry. The logic is
    unchanged: *Leif Joins* is in the same region (*Snakemouth Den*) as the lake. With one starting member, only once
    Leif is allowed (received). On loading, the tester's Leif-alone file got flag 16 ("Leif was already in the
    party"); **seen on screen:** the lake walked past with no scene. With a two-member start, not yet seen.
11. **Position lookups beyond the party** (2026-09-25): the droplet scene (Event21) ends by walking the
    second and third members by position
    (`GetEntity(-2)`, `(-3)`, `EventControl.cs:4112-4114`; `MainManager.cs:18526-18537` answer only inside the party)
    and threw on nothing. In a scene, slot k beyond the party now gets the k-th member in the story's order (the acting
    role first, then the others by id) as a stand-in. The acting leader also has a fallback when a reload forgot the
    story's party: the first missing member by id.
12. **Every way the code reaches for a party member, listed** (2026-09-25: "dump fully what a party member
    or follower is, so we know everything they could ask for"): `dev-scripts/party-access.py` counts 29 ways across the
    decompiled code, with the methods and events using each (`--where <way>`). Covered: lookups by position and
    character, the party as a list, `PartyMover`, `SetPlayers()`, `ChangeParty`, `.following`, `extrafollowers`, the
    leader. Open, since a direct index can't be intercepted: `playerdata[1]`/`[2]` (Events 52, 122, 130, 137, 138, 182,
    all past chapter 1, and `BattleControl.DoAction`/`EventDialogue`, to confirm they check the party's
    size), `tempfollowers[..]` (11 events; they read story companions, and break only for a removed party member, so far
    only Event14, now skipped), `partyorder` (Events 6, 54, 138) and `GetExtraFollower` (Event223).
    The `playerdata[1]`/`[2]` reads are patched where they're read since 2026-09-30 (below, and step 36).
    **Seen (2026-09-25):** the droplet scene replayed to its end with no crash, and the log shows item 9 at work in
    it and in the switch scene (Event23): "the leader (Player 0, member 2) acts member 0's part".
13. **Every member present acts, not only the leader** (2026-09-26, with Vi and Leif in the spider scene:
    Vi led as herself, Kabbu's part went to an invisible stand-in, and Leif stood idle). A member the story doesn't have
    yet (by the story's flags, as for the leader) now takes a missing member's part, in party order, after the leader
    picks his: every lookup by name, by list or by id order hands out that member where a stand-in would have gone,
    and his own part, if asked for, goes to a stand-in so he never gets two sets of orders. This was chosen over
    hiding him, knowing it shows two Leifs in this scene (one acting Kabbu, the story's own in the web). Logged:
    "[party] EventN: Moth (member 2) acts member 1's part". **Seen (2026-09-26):** in the spider scene with
    Vi and Leif, Leif did Kabbu's part (his moves and actions) before the first fight.
    **`unstick` now stops the dead scene too:** the first try got stuck, and after `unstick` the scene's coroutine
    kept running and threw once its stand-ins were cleared (a NullReferenceException in Event6, Vi left tilted).
14. **The story's party changes use the same stand-in** (2026-09-26: "Leif is Kabbu, so Leif fights alone in
    the first fight, then when Vi comes back for the 2nd fight it's Leif + Vi, similar to how it works in vanilla for
    Kabbu"). The first try ran with the guard off (a plugin reload resets it, and no yaml option sets it yet), so the
    spider scene's `ChangeParty({1})` brought Kabbu back for real. The guard now substitutes instead of only dropping:
    each member the story asks for who isn't allowed is replaced by an allowed member the story doesn't have yet (by
    its flags, as the scenes pick), so `{1}` becomes `{2}` and `{0, 1}` becomes `{0, 2}`; with no one to stand in, it
    keeps who is here as before. **Seen in the log (2026-09-26):** "asked for party 1; given 2" before the first fight,
    "asked for party 0,1; given 0,2" before the second. **Then Leif lost his part** (seen: invisible after the first
    fight): the scene deletes its characters and remakes the party (`destroyoldentity`), and the actor was kept as a
    character, so the remake rule took the new leader (Vi) and Kabbu's part went to an invisible stand-in. Actors and
    spares are now kept by member number and looked up again when asked for, so a remade Leif stays Kabbu's actor.
    **Seen (2026-09-26):** Leif visible after the first fight, then Vi and Leif in the second (a screenshot).
    One retry went idle for a reason of mine: resetting the scene, flag 16 was cleared before 27, and the mod's "Leif
    joins after the spider" rule set 16 again in between, so Leif counted as joined. Clear 27 first.
15. **A chosen few stay a chosen few** (2026-10-07). When every asked member is allowed, the guard also keeps a member
    already in the party whom the story hasn't reached yet (Leif before the lake), so the opening's Vi and Kabbu keep
    him. The Wacka Worm game asks for Vi alone (`Event54`, `ChangeParty({0})`), and Leif was kept too: the scene's
    `EndEvent` then threw on his sprite (`UpdateSpriteMat`), the player frozen, and `unstick` threw the same way. Now a
    party that leaves out a member the story already has is taken as chosen, and nobody is added (`[members] ... leaving
    out 1,2: nobody added`). **Seen (2026-10-07):** the game played as Vi alone, no freeze. *Code: `PartyMembers.cs`.*

**No warnings for missing animations (2026-09-26: "dumb to leave bug/errors laying around, even if its
harmless").** A character asked for a state its controller lacks (a lone Leif acting another member's part, a swapped
enemy look with another enemy's movement) made Unity warn twice every time ("State could not be found", "Invalid
Layer Index '-1'"), 68 times in one session, and play nothing. Every character's animation goes through
`EntityControl.SetAnim`, which calls `Animator.CrossFadeInFixedTime(name, time)` with no layer. `AnimGuard.cs`
swaps those two calls for a check: a state found on any layer plays as before; a missing one is skipped (what the
game did anyway) and logged once per controller (`[anim] BeeBoss(Clone) (BeeBoss) has no state 'Walk'`), which is
also the list for mapping a missing animation to the closest one later. Only while Archipelago is enabled, or with
*Use on normal saves* (step 18). **Tested
(2026-09-26):** with a Bee Boss look walking like an Underling, the count stayed at 68 and one `[anim]` line appeared
instead. **The game's direct `anim.Play("name")` calls** (about 30: battles, events, the map, menus) all end in
Unity's `Animator.Play(string, int, float)`, so a prefix there gives them the same check (the asked layer, or any
layer for -1), installed and logged at load. A layer warning that still shows comes from another path; the log names
it.

**Unity's glow-colour error, handled like the animation warnings (2026-09-26):** a light in Rubber Prison's cell
block has a material without the glow colour the game's `GlowTrigger` reads, so Unity logged an error on arrival
(harmless: the value is only written back to the same missing property). `GlowGuard.cs` swaps the three reads for one
that checks first and reads black, logging each material once; only while Archipelago is enabled, or with *Use on
normal saves* (step 18).

**A member added mid-map meets the enemy-only walls (2026-09-26):** after adding Kabbu on Outskirts East,
walls only enemies should bump into blocked the party. A map load tells those walls to ignore each character, and the
added member comes with new characters (`MEASURED.md`, enemy-only walls). Adding a member now redoes that step the
game's way (`SetPlayerColliders`, 0.2 s later), as receiving a party member will need. Seen on screen
(2026-09-26): Vi then Kabbu added mid-map, and the party walked through where the wall had been.

**The seed decides the start, not the config (2026-09-26):** with *Starting Party Member* on (the Archipelago guide,
build step 18), `slot_data`'s `starting_member` takes the place of the dev `TestStartMember`, and party members arrive
as items. A received member joins at once, the way `addmember` does. Which members the guard lets in is worked out
from the items the save has counted, every frame, and forgotten on the title screen: kept only in memory, a member
from one file would let the story add him early in the next. Seen in play (2026-09-26, the Archipelago guide's build
step 18): a Kabbu start and a Vi start from the seed, each received member joining at once.

**Built (asked for again, 2026-09-27, after landing by the rock once more):** after the pitfall scene (the trapdoor
into `SnakemouthFallRoom`, Event5), place the party as if it had just entered the fall room through one of its doors,
the same arrival a random start uses (build step 15 of the Archipelago guide; the arrival jump from the door's entity,
step 13 here). It is expected to line the landing up better than the scene's own spot, as the random start into that
room did. How: Event5 starting on `SnakemouthDoorRoom` marks a landing due; on the first free frame
in `SnakemouthFallRoom` after the scene, the game's `TransferMap` into the same room with the spots of the door room's
door into it (`DoorInto`, the way down the opened trapdoor), as a random start arrives. Only with Archipelago on. First
seen with three members: the scene ended without error (the patched list, `[party] Event5 placed 2 members; member slot
2 stands behind`); the landing seen on screen (2026-09-27, step 10: the arrival where the trapdoor leads in). *Code:
`QualityOfLife.Scenes.cs` (`TickTrapdoorLanding`), `QualityOfLife.Opening.cs` (`DoorInto`).*

**Battles: a member's number used as a slot** (2026-09-30, found while planning stand-ins for scripted fights; read in
the code, not yet seen). In vanilla the party is Vi, Kabbu and Leif in that order, so the game's code sometimes uses
a member's number where it means a party slot. With members as items the party can be any of them in any order (a
received member joins at the end), and those places pick the wrong member or none.
- **The leader at a battle's start** (`BattleControl.StartBattle`, `:1282-1289`). The start turns the party until
  slot `partypointer[0]` equals `partyorder[0]`, but `partyorder` holds member numbers: the field switch writes each
  slot's `animid` into it (`PlayerControl.SwitchOrder`). With Vi and Leif and Leif in front it waits for slot 2 in a
  party of two and never stops; with Kabbu and Leif and Kabbu in front it puts Leif in front.
- **The dizzy enemy's first strike** (`:1336`): it gives the free turn to the member whose number equals the front
  slot.
- **The fix** (`PartySlots.cs`): a transpiler on `StartBattle`'s coroutine. Both reads go through
  `PartySlots.SlotOfMember`, which returns the slot holding that member. Without him, the number itself while it is a
  slot (the story's own small parties, such as the spider fight's Kabbu-first order, where the member playing his part
  stands there), else the leader's slot. Each site is found by its exact instructions, once each; otherwise the
  method keeps its own code and the log says so. Logged: `[party] battle start: the field leader, member 2, is slot 1
  of 2; that slot goes in front`. Only while Archipelago is enabled.
- **To see:** remove Kabbu (`removemember 1`), switch until Leif leads, touch an enemy: the fight opens with Leif in
  front (before the fix, expected: the two swap places without end, which costs a restart).
- **A member being eaten** (`BattleControl.AdvanceTurnEntity`, `:3259`, `:3270`): each turn the Pitcher drains the
  member it swallowed, reading `playerdata[t.trueid].hp`, his number as a slot. With Kabbu and Leif, Leif eaten reads
  slot 2 of two and throws, and the fight stops. Only the Pitcher eats (enemy 98: its bounty and the rematch machine;
  enemy shuffle never moves a boss). Both reads go through `SlotOfMember` (the other two `trueid` reads there name a
  medal's wearer and stay). To see: `removemember 0`, `enemyfight 98`, let Leif be eaten; the drain ticks on.
- **Skills that name their member** (`DoAction`): Heavy Strike reads Kabbu as `GetPlayerData(1, frombattleentity:
  true)` (`:11545`, `:11566`), which finds slot 1, and the Vi and Leif team attack reads `GetPlayerData(0/2, true)`
  (`:12029-12030`). The five constants go through `SlotOfMember`; `GetPlayerAttack` already finds a member by number.
  To see: Kabbu first in the party (a Kabbu start that received Leif): Heavy Strike's damage follows Kabbu's attack.
- The scripted fights that name a slot are step 36.
- **Scenes that place the party by slot** (item 12's open reads). Several scenes read a character straight from
  `playerdata[k].entity`, the k-th in line, since the field switch rotates who stands where:
  - Event52 places slots 0-2 (`EventControl.cs:8491-8494`);
  - Event122 does the same (`:20696-20698`);
  - Event130 has Chompy follow slot 2 (`:22252`);
  - Event138 anchors Kabbu's line on slot 1 (`:23283`).

  A shorter party throws there. Past the party's end the read now finds a record whose character is the stand-in
  scenes already give that place (`GetEntity(-1 - k)`, item 11), so the scene places or anchors the stand-in. Event138
  also lines the party up by `playerdata[partyorder[k]]`, a member's number used as a slot (`:23259-23262`); those four
  go through `SlotOfMember`. The Beast's and Zommoth's scenes (Events 137 and 182) are cast with their fights (step 36).
  Logged: `[party] Event52: place 2 is beyond a party of 2: its stand-in (…) placed`. All four are past the Outskirts
  gate, where the logic still needs all three members.

**Status:** works with Leif alone, seen on screen through chapter 1 into chapter 2 (2026-09-25); Leif joining after the
spider with the story's two (Vi and Kabbu) seen (2026-09-26: he followed, could lead, and showed in the pause menu);
items 5 and 6, and item 10's lake walked past with a two-member start, not yet seen; item 12's direct reads of slots 1
and 2 (Events 52, 122, 130, 137, 138, 182, `DoAction`, `EventDialogue`), the battle start's leader, the eaten tick and
the skills' named members built (2026-09-30), not yet seen; `tempfollowers[..]`, `partyorder` elsewhere
and `GetExtraFollower` still open.

*Code: `PartyFit.cs` (the stand-ins and the acting leader), `PartyMembers.cs` (the member guard, followers,
Leif's joining), `PartySlots.cs` (a member's slot in fights), `AnimGuard.cs` and `GlowGuard.cs` (the guards);
`dev-scripts/party-access.py` (item 12).*

## 12. Shops in the game: shelves show the seed's items

Shops needed their own approach because buying isn't a pickup or a gift: a medal shop is a shelf of item entities
and a script, an item shop adds its item silently, and nothing in the save marks a purchase. So each kind of
shop had to show the seed's items and tell when one was bought.

**Medal shops as locations** (2026-09-25). A medal shop turned out not to be a menu: each shelf slot is an
item entity on the counter (`NPCControl.SetBadgeShop`), looking at one opens its description box, and buying runs the
shopkeeper's dialogue, whose script checks the money, pays, removes the medal from the stock and gives it
(`giveitem`). So: the shelf shows the seed's item's sprite; while the description box and the buy prompt are built,
the medal table briefly holds the seed item's name and description; the `giveitem` is swapped like a gift; and the
check was first the medal leaving the shop's stock, which the save keeps (no "bought" flag exists; replaced by a bit
per copy, below). **Seen
(2026-09-25):** Merab's shelf showed the seed's items (two books, a leaf, a mushroom); buying the Mushroom Gummies at
medal 12's slot held them up, kept Sleep Resistance out, sent *Medal Shop 4* when medal 12 left her stock, and the
server's Mushroom Gummies arrived. On joining the seed, two medals bought earlier (before shops were locations) sent
their checks from the stock alone. **More on show**: Merab's shelf holds 5 instead of 3, spread evenly across her own
first-to-last spots (a 6th past the end was hard to reach), and Shades's 4 instead of 2. The slot count is the
shopkeeper's `data` length and each slot sits at `vectordata[j]`, so both are lengthened before the shelf is built,
once per shopkeeper: the game rebuilds the shelf on the same shopkeeper after a purchase, and a second stretch drifted
it right. Seen: 5 on Merab's counter (screenshot, 2026-09-25).

**Item shops** (2026-09-25: the first purchase of each item in each shop is a check, then the shop's own
item again). An item shop isn't a shelf the shopkeeper builds: the map makes one `Fixedshop<n>` slot per entry of the
shopkeeper's `data` (`MapControl.cs:1715-1745`), and the buy line pays and then adds the item with `additem`, silently,
with no item-get box to swap (`BugariaCommercial` line 16). So the slot shows the seed's item (sprite, name and
description, as for medals, from `itemdata[0, id, 0]` and `[.., 2]`); when the buy line is read (`GetDialogueText`) its
`additem` is taken out, so nothing local is given; once the dialogue is over, berries down by the price mean it was
bought, and the check goes out through the respawning pickups' queue with a hold-up. Nothing in the save marks it,
as for respawning pickups. After the check, the slot is the shop's own item again. **Seen (2026-09-25):**
the first try showed the shop's own items and a hold-up of "an Archipelago item", since the item shop locations were
never scouted (the scout list is built table by table); after adding them, the shelf showed the seed's medals, each
purchase held up the seed's item and sent its check, and the bought slots became the shop's own items again.

**A shopkeeper kept present, and scenery shown** (the caravan, 2026-09-25). Making an entity present works
by a marker on its `requires`, set right after the map builds its entities. A shopkeeper's slots are built *during*
that build, right after the keeper is read, and only if the keeper exists by then (`MapControl.cs:1708-1745`), so the
marker came too late. While a map builds, the mod now remembers the entity just made (every one starts as
`CreateNewEntity(name)`), and a check made with that entity's own `requires` array answers "exists" when it's listed.
Scenery (a `ConditionChecker`) gets the mirror of the rocks' treatment: a marker `requires` before its `Start`, which
answers "exists" (`scenery_present`, the caravan's stall). Seen (2026-09-25): the stall and Crickerly with
her three slots, the seed's items in them, each first purchase a check, then her own items.

**The reshuffle choice first** (2026-09-25: faster to reset a shelf). A shopkeeper's greeting ends in a
`prompt` whose choices are listed as N targets then N texts (`MainManager.cs:12213-12222`); the reshuffle is the one
with target `-199` and text `-195` (Shades's line 1, Merab's line 34, read with the console's `script`). With
Archipelago on, that pair moves to the front, in the map's dialogue table in memory, once per map load. Seen
(2026-09-25): at Shades's, reshuffling is now a matter of tapping the confirm button.

**Full stock from the start, the mod owning it** (2026-09-25; built, then seen: see this step's Status). Every medal a
shop will ever stock is on its shelf from a new game, and a medal the story stocks twice is two locations. That broke
"the check is the medal leaving the stock" twice over: a fresh file holds 10 of Merab's 22 copies, which looks like 12
purchases, and two copies of one medal can't be told apart in a list of medal ids. So:

1. **The stock is set, not read.** The game rebuilds a shop's shelf pool from its stock in one method, `UpdateShops`,
   on every map start and after each purchase. A prefix there sets the stock to the shop's copies not yet done, as the
   game's own `shoppool` script command writes it. Medals the story adds later are trimmed there too.
2. **What was bought is a bit per copy in the save:** a free number slot per shop (`flagvar[7]` Merab's, `[8]`
   Shades's; both unused by the game's code and text). A shop's copies are its locations in id order. A copy is done
   once its bit is set or the server has its check, so an offline purchase survives a save and quit and is sent on
   reconnecting.
3. **The bit is set by the purchase itself:** the swapped `giveitem` of a shop medal is the moment of buying, the same
   moment the vanilla medal is kept out, so "check marked" and "item withheld" can't come apart.
4. **Each shelf slot stands for one copy:** the k-th slot showing a medal is that medal's k-th copy not yet done, and
   opening a slot's buy prompt remembers that copy, so buying the second TP Plus slot gives what that slot showed.
5. **Leave the stock alone mid-purchase.** The shelf is rebuilt by the buy line's `kill,caller`, which in Shades's line
   runs after `removebadgeshop` but *before* `giveitem`, so the bit isn't set yet and step 1 would put the bought medal
   straight back on the shelf. While a buy prompt's dialogue runs, the game's own removal stands; the next rebuild
   happens after the bit is set. (Caught reading the code before the first test, 2026-09-25.)
   **Seen (2026-09-25):** 18 of Merab's 22 copies bought on a new file, each check sent for the copy bought
   (both TP Plus copies and both Ambusher copies separately), the guard in step 5 firing every time; after saving,
   the title screen and loading, exactly the 4 unbought copies stayed, reshuffles included. One flash fixed after: a
   rebuilt shelf showed the game's own medal sprites for a moment (the swap ran every 15 frames; now every frame for a
   second after a rebuild; seen gone, 2026-09-25).
   **The same flash on the ground, in houses** (2026-09-25): Madeleine's table items looked right from outside
   and once inside, but showed their own items for a moment on the way in. A house on the same map is an "inside", and
   going in switches its entities on (`MapControl.RefreshInsides`). **First guess, wrong:** swap right after
   `RefreshInsides` and every frame for a second; the flash stayed. **Measured instead:** a log line in a patch on
   `EntityControl.UpdateItem` (`EntityControl.cs:3218`), the one place the game draws an item entity's own sprite, run
   whenever its animation state changes. Every way in, the game redrew the house's pickups there, and the ground pass
   (logged in its one-second window) never found anything to fix. So the fix is that patch: when the game draws a
   location pickup's own item, the seed's item goes back on in the same call. **Seen (2026-09-25):** nothing
   odd going in or out any more. The first guess was taken back out.

**A bought slot shows the shop's own item again, asked of the game (2026-09-26):** the item shops remembered each slot's
sprite before swapping it and put that back once the check was done. After a hot reload the memory was empty while the
shelf still showed the seed's look, so that look was remembered as the original, and the Caravan's slot 1 kept the
Archipelago icon after it was bought (seen on screen). The slot's own item now comes from `MainManager.GetItemSprite`.

**Status:** works, seen on screen (2026-09-25): Merab's medal shop with its full stock, the reshuffle choice, Madame
Butterfly's item shop, the caravan, and pickups in houses; Shades's shop not yet built as locations.

*Code: `ShopSwap.cs` (medal shops and their stock), `ItemShops.cs` (item shops), `KeptOpen.cs` (the shopkeeper
and scenery kept present), `QualityOfLife.Opening.cs` (`RerollFirst`, the reshuffle choice first), `ItemSwap.cs`
(`UpdateItem`, pickups in houses).*

## 13. The entrance randomizer in the game: doors rewritten at map load

The entrance randomizer changes where doors lead. Each door in the game carries its own target, so the mod
rewrites doors as a map loads, and a shuffled door needs its own way back.

**Doors rewritten: the entrance randomizer's proof of concept** (2026-09-25). A door to another map calls
`TransferMap(data[0], vectordata[0], vectordata[1], vectordata[2])` when walked into (`NPCControl.cs:5458-5461`): the
target map, the walk on this side, where the party appears, where it then walks. So "door A leads where door B leads"
is: after the map builds its entities, A's `data` and its `vectordata` from `[1]` on are replaced by B's, read from B's
own map's entity table and names table (`Data/EntityData/Names/<map>names`); A's own `vectordata[0]` stays. The pairs
come from `slot_data` (`door_targets`) or, for a test, the dev setting `TestDoors`. First test: the Outskirts' east exit
leading where the plaza's door to the Commercial District leads. **Seen (2026-09-25):** from the Outskirts'
bottom-right exit they appeared on the right side of the Commercial District, exactly as when coming in from the plaza.
**Both ways, seen (2026-09-25):** four rewrites swapped two connections as a coupled shuffle would (the
Outskirts' east exit with the plaza's Commercial door, and their ways back). The log showed every trip landing on the
right map: the plaza's door to the Outskirts' east area and back, the Outskirts' exit to the Commercial District and
back, each several times. The tester found it confusing to keep track by eye, so from here the log is the record of each
trip.

**What a door carries, side by side** (2026-09-25, reading the rest of `TransferMap`, `MainManager.cs:17467-17620`). A
door's `data` is more than its target: `[1..3]` switch the camera's offset, angle and limits on arrival (from
`vectordata[3..6]`), and `[4] == 1` means the party isn't walked into the door first (nine doors: holes, wells, ladders,
the fall room's). The arrival jump is read off the door's own entity, `emoticonoffset.x` (entity table field 175). So a
rewritten door takes the target, the camera and the jump from the other door, and keeps its own `[4]`
and `vectordata[0]`: whatever happens on the side you leave stays, whatever happens on the side you arrive at comes
along.

**Pairing each door with its way back** (2026-09-25). Two maps can be joined by several doors, so "the door on the
other map that leads back" can be more than one. The one that belongs to a door is the one the party arrives next to:
the door on the target map whose start position is nearest (on the ground plane) to where the door places the party,
`vectordata[1]`. EntityDump now writes each entity's start position (fields 6-8) and the jump (field 175), and
`dev-scripts/door-graph.py` pairs every door that way, marking pairs that don't point at each other ("not mutual").
The first run found three things to fix in the script itself: Rubber Prison's pier stacks doors floor above floor, so
the distance is 3D; a map can hold two doors of one name, so doors are told apart by entity index; and story variants
of one door (day and night copies at one spot) count as one. After that, 531 of 567 doors paired both ways; the rest
are listed in `MEASURED.md` to check in play. The
coupled entrance randomizer needs those pairs: going through a shuffled door and turning round must bring you back.

**Status:** works, seen on screen (2026-09-25): a rewritten door, and a coupled swap both ways; the 36 doors that don't
pair both ways are still to check in play.

*Code: `DoorShuffle.cs`; `dev-scripts/door-graph.py` (the pairs).*

## 14. The Detector medal beeps for every check left in a room

The Detector medal beeps when a room hides something. In a seed it should beep for what matters instead: any
of the seed's checks left in the room. That meant replacing the game's answer, not adding to it.

**The Detector for every check** (2026-09-25: beep for any kind of check left in the room: shops, quests,
someone to help, not only hidden items). Read first how the medal works: one second after a map loads, the game asks its
objects (`NPCControl.CheckHidden`: buried crystal berries, grass hiding one, a dig spot with a medal) and the map
(`MapControl.CheckDisc`: an unrecorded discovery) whether something is hidden; any yes sets one value,
`map.hiddenitem = 100`, and the map's update turns that into the "!" over the leader and the beep
(`MapControl.cs:885-896`). So nothing about the medal needs changing: a prefix on `CheckDisc` (run a second after every
map load, discoveries or not) answers in its place in a seed, and sets the same value when one of the seed's locations
on this map isn't done. Every location type has a map: pickups, gifts (quest rewards included, where the reward is
handed over), shop copies and item shops from `slot_data`, discoveries from the map's own `discoveryids`. Done means the
server has the check, or offline the save says so (its flag, crystal berry, journal entry, a shop copy's bought bit).
Only while the Detector counts as equipped (the medal, or the panel's Detector row). **In a seed the mod's answer is the
only one** (beep with one check or more left, quiet when the room is done): the game's own checks would still beep for
hidden things the seed doesn't have, so `NPCControl.CheckHidden` doesn't run, `CheckDisc` is replaced by the mod's
answer (a beep or silence, logged either way), and a music record's `Start`, which sets the value as the map builds and
is used on the first free frame (`MusicSpinner.cs:54-57`), has it cleared right after. Outside a seed, all
vanilla. **Seen (2026-09-25):** in the Residential District it beeped for the rooftop item, then, with both items taken,
for the quest reward still handed out there (location 16, the delivery quest), as intended; quiet in the plaza with none
left.

**Status:** works, seen on screen (2026-09-25).

*Code: `CheckDetector.cs`.*

## 15. Difficulty and Detector rows, and what goes in the panel or the yaml

Two rows in the Archipelago panel change how the game plays, never where items are. Both work by answering the
game's own "is this medal equipped?" question, so the save stays clean and the logic never changes.

**A row "Difficulty: Normal / Hard / Hardest" in the Archipelago panel** (2026-09-24). Hard and
Hardest add the game's two Hard Mode levels; Normal leaves it to the game (the medal equipped, or the
HARDEST code). **Default: Normal.** Boss prize medals are paid out on every setting (apimplementation.md,
build step 10).

**What belongs in the panel** (2026-09-24): only on/off preferences that never change what's where
(Difficulty, Detector, later DeathLink, which only adds a tag to the connection). Anything that decides the
seed (entrance rando, shuffles, goals) is a player-file (yaml) option, applied from `slot_data`.

**Every panel setting applies only while Archipelago is enabled** (2026-09-24; or with *Use on normal saves*, step
18): vanilla saves play exactly as vanilla. Difficulty, Detector and every item swap check the switch; a Hardest flag
the mod set is cleared the moment it's switched off.

**A row "Detector: On / Off" in the Archipelago panel** (2026-09-24), a help for finding items.
On acts as if the Detector medal (#2) were equipped; Off leaves it to the game (the medal equipped or not).
**Default: On.**
All three of its effects ask one question, `BadgeIsEquipped(2)` (objects `NPCControl.cs:1344`, discoveries
`MapControl.cs:408`, music `MusicSpinner.cs:54`), and Hard is the same question for medal #11, so one patch
on `BadgeIsEquipped` serves both rows (and Spy Specs, medal #17, since step 39). It changes no save data and no logic.

**Built (2026-09-24), not yet seen on screen:** the panel has eight rows now (spaced tighter so the status
line still fits). Difficulty offers Normal, Hard and Hardest. `MedalAssist.cs`
answers "equipped" for medal 11 (Hard) or 2 (Detector) on party-wide checks (since step 39 also 17, Spy Specs), on
randomizer saves only (since step 18, also with *Use on normal saves*). The
medals menu equips from the medal list itself, never through that check, so it's unaffected.

**Hardest** (chosen: switchable, the save stays clean): its extras read the save's HARDEST flag (614)
directly, and the game keeps no other trace of a typed code. So the mod turns 614 on in play and remembers
that it did; each time the game saves, the flag is cleared just for the write and put back after, and
switching down clears only a 614 the mod set. Loading a save or starting a new one forgets the mark, since
those flags are the save's own. If any of the three hooks (save, load, new game) is missing, Hardest does
nothing rather than risk a save.

**Status:** the Detector row's effect seen on screen through step 14 (the Detector beeping for checks left in a room,
2026-09-25); Difficulty (Hard, Hardest) built (2026-09-24), not yet seen on screen, waived by the user (2026-10-04:
assumed fine until an issue shows); the Detector's reworded help line seen whole (2026-10-04).

*Code: `MedalAssist.cs`.*

## 16. Randomizer saves in their own folder

With the Archipelago mod enabled, the game reads and writes its saves in a separate folder, so a randomizer
run never touches a normal save. It had to exist before the mod granted its first item.

Through a whole session played with the Archipelago mod enabled, the game
saved only into the `archipelago` folder (last write 05:06), and the normal save files kept their earlier
times (03:42 and 2023), checked on disk on 2026-09-24. The redirect covers all five places the game touches
a save file, and normal saves are only reachable with the mod disabled.

**Status:** works, checked on disk (2026-09-24).

*Code: `SaveRedirect.cs` (the separate save folder, patching the game's five save-file functions in `InputIO`).*

## 17. Enemy scaling: each area's enemies fit when you reach it

The world is always open, so a chapter 6 area can be reached during chapter 1 (and the entrance randomizer and
a random start make that more likely). In vanilla that never happens, so its enemies would be far too strong,
and a chapter 1 area met late far too weak. Enemy scaling makes each area about as hard as it would be at the
right point in the story. It is a balance setting, not a challenge setting.

**Decided (2026-09-26):**
- A row on the panel's Quality of life page (now the Gameplay page, step 8), *Enemy scaling*: **Off / Party level /
  Artifacts**, on by default (Party level). It isn't in the yaml: it ties to no check and no logic, so the player can
  change it from the main menu. Only while Archipelago is enabled (vanilla stays vanilla), or with *Use on normal
  saves* (step 18).
  - **Off:** vanilla, each enemy's own stats.
  - **Party level:** every enemy scaled to the party's level, so every area plays fair in any order. No cheese: a hard
    area early is scaled down, an easy one late scaled up, and EXP follows (below).
  - **Artifacts:** scaled to the artifacts found, as vanilla's difficulty follows the story; levelling ahead makes it
    easier, rushing harder. A `chapter` mode was dropped: the chapter ends *are* the artifact flags, so it would be
    the same number.
- Both up and down. **Normal / Hard / Hardest** (step 15) stays the challenge setting, on top.
- The **bestiary** shows the scaled numbers, as the enemy would be if met now. Spy in a fight already shows the live
  ones.

**Where the numbers come from** (the facts in `MEASURED.md`, "Battles, for enemy shuffle"). The tester hasn't finished
the game, so nothing is from memory; everything is from the game's own data:
1. **An ordinary enemy's home level comes from its EXP.** The game's EXP rule (`base - (level - 1) * 2.5`) makes each
   enemy "outgrown" at `base / 2.5 + 1`. Home level is **3 below outgrown**: that fits both ends of the game at once
   (a new game at level 1 meets enemies outgrown near 4; the last area's enemies are outgrown near 30, the cap is 27).
   Grouped by area, the home levels climb with the story (Snakemouth ~1, Golden Hills ~6, the desert ~8-9, the
   factory and hideout ~9-12, the sand castle ~13.6, the grasslands ~15, the swamps ~16.6, Upper Snakemouth ~18.4,
   the Barren Lands ~20, Rubber Prison ~23.6), so the game's own tuning is the curve.
2. **A boss's home level comes from its chapter** (the story event that starts its fight; bosses have flat EXP), one
   level per chapter from that curve: 3, 8, 11, 14, 17, 21, 26 for chapters 1-7. A summoned part takes its boss's.
   The intro spider (can't be won), tutorial and test fights, and the Everlasting King's keys and tablet (parts of his
   fight) are left alone. **Corrected (2026-09-30):** this list first held the ids the game leaves out of a x1.5 read
   as Hard Mode's; that x1.5 is the hologram machine's hard rematch (flag 166, `MainManager.cs:6282`), and it also
   kept the Wasp General (72) unscaled. The user: "the Wasp General is more like a mini boss, its not as hard as the
   actual boss but it was a harder fight than just normal enemies"; the game's own `minibosslist` holds it, and every
   other mini-boss there scales from its chapter, so it does too (chapter 5).
3. **Artifacts' target:** the level of the areas vanilla opens after that many artifacts: 1, 6, 9, 13, 16, 19, 23, 27.

**How it works** (built 2026-09-26): `EnemyScaling.cs`, a postfix on `MainManager.GetEnemyData` when a fight builds
its enemies (`createentity`), after the game has applied Hard/Hardest:
- **HP** times `(target + 5) / (home + 5)` (and `maxhp`, which the game copies from HP).
- **Attack, per hit, by the same ratio as HP** (changed 2026-09-26). First it was a flat step through
  `hardatk` (+1 per 4 levels, held to -3...+6). But the game adds `hardatk` to every hit (`CalculateBaseDamage`,
  `BattleControl.cs:6364`, per hit), so a flat -3 barely touched one big hit and floored many small ones. A Dead Lander
  G at level 1 still hit hard through its string of attacks (seen in play). Now a prefix on `CalculateBaseDamage` scales
  the move's own damage by the ratio, before `hardatk` (so Hard/Hardest still add on top), when an enemy is the
  attacker. The game's floor of 1 per hit stays.
- **Defence** +1 per 6 levels, never below 0; a defence of -1 (shown as "?") is left alone.
- **EXP** by the game's own rule at the level the enemy is matched to, `GetEXP(base, level - difference)`: in Party
  level mode that's the enemy's home level, so levelling keeps vanilla's pace. Left alone where the game fixes it
  (fixed EXP, no EXP, the level cap, hologram fights).
- Some ids read another row's data (column 25); the row the game read is used.
- **The bestiary** (built 2026-09-26, seen on screen): the page builds its text in `PauseMenu.UpdateText` from its
  own copy of the raw table (`PauseMenu.enemydata`), so a prefix swaps the shown enemy's row for a scaled one (HP and
  its Hard bonus by the ratio, defence by the step) and a postfix puts it back; the page's own Hard/Hardest maths runs
  on top. The field holds other text on other pages, so only a real enemy row is touched. The Dead Lander G showed
  HP 7, Defense 0 there, as in the fight.
- Every scaled enemy is logged (`[scale] Seedling (9): home 1, target 10: hp 4 -> 10, hits x2.50, def … -> …, exp …
  -> …`).
All the constants are starting values, tuned by play.

**Seen on screen (2026-09-26):** at level 1 on Party level, a map Underling shuffled into a Dead Lander G was
logged `home 27, target 1: hp 35 -> 7, attack -3, def 1 -> 0, exp 74 -> 9`, and Spy in the fight showed HP 7, Defense 0.
The panel row stepped through its three values (the log followed each). **How it played:** tough but fair. Leif alone at
7 HP (healing 1+ a turn) went to 4 HP after its first hits, then to 2. The tester judged 35 -> 7 HP and no defence
balanced, "fair/hard" for anyone who shuffles enemies. Its attack then sat at the -3 floor; per-hit scaling replaced
that floor the same day (below).

**Per-hit attack felt fair (2026-09-26):** the same Dead Lander G at level 1 (hits x0.19, the dev cheat
`onehit` off) landed 1-2 attacks at "fair damage" and died in two hits, like any other enemy there.

**A scripted end at 10 HP keeps the whole fight** (2026-09-30). **The user asked** whether the Beast's scripted end
is a percentage or an amount of HP, "so we don't start the fight and instantly get the scripted end", and wanted "a
fight + end with the scripted thing even when scaling is enabled".
- **Read in the code:** it's an amount. `SurviveWith10` floors every hit at 10 HP (`BattleControl.cs:7491-7494`), and
  the Beast's own turn plays its script at `hp <= 10` (`:18361`); the Everlasting King's phases start the same way
  (`:20826`, the floor in its data). Scaling alone could never start it at once (the Beast has 76 HP, at least 21 after
  scaling), but at level 1 the real fight shrank from 66 HP to 11.
- **The user's choice:** only the HP above the 10 scales, `10 + round((hp − 10) × ratio)`, and the game's 10 isn't
  patched. The Beast at level 1 has 28 HP, 18 of them before its script, as other enemies' HP scales; then the game's
  own finale. At its home level (17) it keeps 76; at 27, 106.
- **Which fights:** an enemy whose data has `SurviveWith10` (the King), and the Beast while its scene runs (`Event137`
  adds the floor after the enemy is made; a hologram rematch has none). The bestiary shows the same numbers (the
  Beast always, the King by its data's column 23). Logged: `[scale] Centipede (69): home 17, target 1: hp 76 -> 28
  (only the HP above its scripted end at 10 scaled), …`.
- **The other fixed numbers in enemy scripts, surveyed** (2026-09-30; the user: "can we scale the scripted things in
  fights as well, so they actually work/are fun"). `dev-scripts/enemy-numbers.py` lists every fixed HP number in
  `BattleControl.cs` with its enemy: 39 fixed, 30 relative (a share of `maxhp`), which scale by themselves. Each was
  read and classified in `MEASURED.md` ("Fixed numbers in the enemies' scripts").
  - **Scale:** fixed heals (an ally's 4 to 15), an HP set outright (a revived partner at 7, a summoned ally at 10), the
    holo party's thresholds, and the battle start's flat HP adjustments (Spuder −15, Zasp and Mothiva +15, Maki's team
    +10, fire areas +3). Each by the ratio of the enemy whose HP it measures.
  - **Keep:** 1 (survive at 1), 999 (an invulnerable marker), the two 10-HP scripted ends (above), and heals of 1.
- **Built** (2026-09-30; `EnemyScaling.ScriptNumbers`). Each number becomes `max(1, round(N × ratio))` with the ratio
  scaling gives the enemy it measures, and stays N when scaling is off, the enemy untouched, or the ratio 1:
  - **Heals** (`DoAction`, 19 literal amounts in 18 calls). Each literal marks itself on its way, and the call goes to
    the mod's `ScaledHeal`, which scales only an enemy's heal and only that literal, then calls the game's `Heal`. So
    the other branch of a `? :` (the Weevil's full heal with Heavy Strike) passes untouched. The call is replaced
    rather than `Heal` patched: it's a one-line wrapper the runtime may inline into its callers, where a patch never
    runs (as `BerryBounce` was, step 19, and the field attacks' tap, the Archipelago guide's build step 21).
  - **HP set outright** (3): the store becomes a call that scales by the enemy set. Stratos's and Delilah's "7" counter
    (2) is scaled by the reviver's ratio (a pair share their chapter).
  - **The battle start's adjustments** (`StartBattle`, 8: HP and max HP for four cases), by the enemy the start's loop
    is on (its hoisted `<i>`).
  - **The holo party's AI** (`HoloVi`, 2) and `EnemyHeavyThrow` (1), by `currentEnemy`.

  Each is found by its exact instructions and counted; otherwise the method keeps its code and the log says so. Logged
  once per fight: `[scale] Stratos (111) heals 15 -> 9 (x0.58)`.
  **To see:** a fight whose script heals, far from its home level with Party level scaling (for example `enemyfight`
  with Kali and Kenny's ids at a low level): the heal's number is the scaled one, and the log line says so.
- **Maki's hits** (2026-10-08, the user: "maki always hits really hard, unrelated to the party level"): as a follower
  in the Far Grasslands and the swamp (`BattleControl.AddAI(46, …)` in those areas) he attacks for a fixed 6, piercing,
  plus one per medal 90 (`AIAttack`), so scaled-down enemies fell to him in a hit or two. His hit is now scaled by the
  ratio its target gets, as its HP was (`FollowerHits`): a prefix on the `DoDamage` overload every hit ends in, the
  shorter one his call goes through being a one-line wrapper Mono inlines (a patch there never ran). **Seen
  (2026-10-08):** with the dev `onehit` off, against Riz at level 1, Maki hit for 2 (6 x 0.27).
- **Scripted knockouts left alone** (2026-10-08): at its 10-HP end the swamp boss (the Centipede, `BattleControl`'s
  scripted turn) hits Vi and Leif for their whole HP (`NoExceptions`) so Kabbu fights alone; scaled, they stood (seen:
  4/8 and 5/8 after it). An enemy's `NoExceptions` hit, the only one there is, now passes unscaled. **Seen
  (2026-10-08):** brought to 11 HP (`killall 11`), the boss held at 10 through the party's turns, then its scripted
  turn knocked out Vi and Leif and Kabbu fell to 1, healed and gained his attack boost; the fight ended as intended.

**Status:** works, seen on screen (2026-09-26): scaled HP, defence and per-hit damage in a fight, and the bestiary;
the constants still to tune by play. The 10-HP scripted end and the fixed numbers in enemy scripts built (2026-09-30),
not yet seen; the Wasp General scaled as a mini-boss (2026-09-30), not yet seen. Maki's hits scaled (2026-10-08), seen.
The swamp boss's 10-HP end and its scripted knockout seen (2026-10-08).

*Code: `EnemyScaling.cs` (`AfterGetEnemyData`, `Damage`, `Bestiary`, `ScriptNumbers`, `FollowerHits`); the row in
`ApMenu.cs` and `ApMenu.Rows.cs` (`ScalingRow`), its config in `QualityOfLife.cs` (`EnemyScalingMode`).*

## 18. Use on normal saves: the panel's settings with Archipelago off

Quality of life and Gameplay are useful without a seed too. **Decided (2026-09-26):** an opt-in row, a
deliberate exception to "vanilla stays vanilla" that only the project owner could make.

- **The row:** *Use on normal saves*, ON / OFF, **off by default**, on the panel's main page under Achievements.
  Help lines: "Quality of life and Gameplay also apply with Archipelago off." (on) and "Quality of life and Gameplay
  apply only with Archipelago on." (off). A label longer than 15 letters now shrinks to fit before the arrows, as a
  long value already did.
- **What it turns on, with Archipelago off:** the Settings rows to both pages (step 8), Fast text, the scenes Skip
  cutscenes skips or speeds by, Travel (Warp to Start goes to the game's own start), Medal prices, Difficulty,
  Detector, Spy Specs (step 39), Enemy scaling, the EXP and berry multipliers (step 19), Uncap FPS (step 24), skipping
  the game's 5-second forced collection (step 25), Attack boost (step 27), Healing crystals (step 30), Auto-save
  (step 31), and the guards against missing animations and glow colours (step 11).
  **Added (2026-09-29):** the user saw the animation warnings on a normal save with the row on, in the Barren Lands,
  where the game warns on its own; asked, they chose the guards follow the row. Nothing changes on screen.
- **What it never turns on:** anything tied to a seed. The intro skip (its end sends the first check and makes the
  seed's start), items, checks, the shuffles, the Detector's check beeps, boss prizes paid on any difficulty (they are
  checks), Item animation (only items from the server), and the achievement guard.
- **How:** one `settingsOn` in `Plugin.cs` (Archipelago enabled, or this row) goes to the modules behind the two pages
  in place of the Archipelago switch: `MedalAssist` (which keeps the Archipelago switch for boss prizes),
  `EnemyScaling`, `InGameSettings`, `Multipliers`, `FrameRate`, `ClockCleanup`, `AnimGuard`, `GlowGuard`,
  `AttackBoost`, `SaveCrystals` (Healing crystals only; the confirm press stays on the Archipelago switch), `AutoSave`,
  the Travel buttons, and `QualityOfLife.SettingsOn` (fast text, the scene list, and
  `ShopSwap`'s prices). The seed's start and the entrance randomizer's forced Warp answer only with Archipelago
  enabled, so a normal save never warps to a seed's start.

**Status:** built (2026-09-26); in use on a normal save with the row on (2026-09-29, chapter 5: Uncap FPS at 240
applied with Archipelago off). The rest of what it turns on, and the guards that joined (2026-09-29), not yet seen.

*Code: `Plugin.cs` (`settingsOn`, handed to each module above); the row in `ApMenu.cs` and `ApMenu.Rows.cs`
(`NormalSavesRow`).*

## 19. EXP and berry multipliers

An opt-in for a faster, easier game (Next 16 and 17 in `apimplementation.md`).

**Decided (2026-09-26):**
- Two rows on the **Gameplay** page, *EXP multiplier* and *Berry multiplier*, **1x to 10x, default 1x**
  ("just 1-10x to make it simple"). First planned as 1x-5x on Quality of life.
- **They look like the game's volume rows** (asked for with a screenshot of Music Volume): ten pips between the
  two arrows, one per step, the lit ones yellow; left / right lights or clears one.
- Only while Archipelago is enabled, or with *Use on normal saves* (step 18). No check and no logic depend on them.

**How it works** (`Multipliers.cs`, the facts in `MEASURED.md`, "EXP and berries picked up"):
- **EXP:** a postfix on the battle's own `BattleControl.GetEXP(amount, fixedexp, enemy)`, the one call that turns each
  defeated enemy into its EXP share, after the game's Hard Mode bonus and its per-enemy caps. It multiplies that share,
  so it stacks on top of enemy scaling. The game then adds it to the battle's total, which it caps at one level's worth
  (`neededexp`); that cap stays, so a high multiplier early mostly means a level per battle. A hard rematch at the
  hologram machine (flag 166) keeps the game's 5.
- **Berries:** a prefix on the first step of `NPCControl.BerryBounce`, the coroutine the game starts only right after a
  berry lying in the world (1, 5 or 20, from the map or dropped after a fight) has been added, and before it clamps
  money at 999. **First hooked on `BerryBounce()` itself, which never ran** (2026-09-26: 10x berries gave the
  plain amount, and the log had no berry line while the EXP lines were there): that method only builds the coroutine
  object, and a stub that small is inlined into its caller, so a patch on it is skipped. The patch is now on the
  coroutine's `MoveNext` (`MethodType.Enumerator`), reached only through the interface, and acts on state 0. It adds the
  rest (value x (multiplier - 1)) and clamps the same way. A check's berries come from the server through the item
  grant, never this pickup, so they aren't multiplied.
- **The bar** (`ApMenu.DrawPips`): the game draws a volume row's ten pips with `guisprites[59]` (empty, a quarter
  size) and `guisprites[42]` coloured yellow (lit, a third), 0.4 apart from 0.7 past the left arrow
  (`MainManager.ShowItemList`, type 17). First scaled by 0.68 to fit the panel's closer rows; since 2026-09-30 drawn
  at the game's own place and size, as the rows are (step 8).
- Each page's two buttons: Reset puts both back to 1x, Disable all sets 1x.

**Seen on screen (2026-09-26):** EXP at 10x: a Pseudoscorpion and a Cactus logged 5 -> 50 and 7 -> 70, and the
battle gave 100, the game's cap of a level's worth. A berry picked up at 10x, after the move to `MoveNext`.

**Status:** works, seen on screen (2026-09-26): EXP at 10x, and a berry picked up at 10x.

*Code: `Multipliers.cs` (`AfterGetExp`, `BeforeBerryStep`); the rows in `ApMenu.cs` (`DrawPips`) and
`ApMenu.Rows.cs`.*

## 20. Item colors: Archipelago's colours in the "You got" box

**Asked (2026-09-26):** after picking the colours on screen (step 9, "Archipelago's colours in the line"),
a Quality of life row to turn them off and keep the game's look: *Item colors: Archipelago / Off*, Archipelago by
default. **Later the same day: RARITY / ARCHIPELAGO / OFF, Rarity by default, for the starbursts too** (step 22, the
end). Right below Item animation, since both are about another player's items.

**How it works** (`QualityOfLife.cs` binds it, `ApMenu.cs` draws the row, `ItemSwap.cs` reads it):
- **Archipelago:** another player's name in dark yellow, the item by its kind (progression, useful, filler, trap),
  "from" and "'s" in black.
- **Off:** no colour commands at all, so the whole name stays in the game's red, as it was before. The wording ("You
  found ...", "from ...") is the same either way; only the colours change. Your own finds are always the game's red.
- The row joins Reset to defaults (back to its default: Archipelago then, Rarity since step 22) and Disable all (Off).
  Only while Archipelago is enabled, or with *Use on normal saves* (step 18), like the page's other rows.
- The Quality of life page grew to eight rows then (the last at the panel's lowest row spot, above the help line);
  step 21 on added more.

**Status:** built (2026-09-26); the row seen on the ten-row page (2026-09-26, step 21); a hold-up with it off seen
(2026-10-04): the whole "Explorer Permit from TestPlayer" in the game's red, the wording unchanged.

*Code: `QualityOfLife.cs` (`ItemColors`, `ApColors`), `ApMenu.cs` (`ColorsRow`), `ItemSwap.Looks.cs`
(`PlayerText`, `ClassText`).*

## 21. Archipelago icon: other players' items on the ground and on shelves

**Asked (2026-09-26):** once the icon was drawn (step 23, `ApIcon.cs`), use it for every other world's item,
with a Quality of life row. The design from 2026-09-24 and 2026-09-26 (another game's item as the icon, another Bug
Fables player's with its real sprite, a row on by default, off for a surprise) plus a mode added on top: every other
player's item as the icon. **Archipelago icon: OTHER GAMES / ALL PLAYERS / OFF**, Other games by default, below Item
colors.

**How it works** (`ItemSwap.Describe`, the one place every look comes from: the ground, a shop shelf, a pickup, a gift):
- **Other games:** another game's item is the icon, its starburst in its class colour; another Bug Fables player's item
  keeps its real sprite; yours too.
- **All players:** any item that isn't yours is the icon, in its class colour.
- **Off:** another game's item keeps the vanilla item's look (as before the icon), a surprise until found; Bug Fables
  items show their real sprite. The text always names whose it is.
- With more rows, the Quality of life page's rows sit closer (the first and last where they were); the Gameplay page
  followed in step 30. Since step 39 a page shows seven rows at the game's size and scrolls instead (step 8).
- **Shops name it too (2026-09-26):** a shop's box names another player's item in its class colour, and its
  description says whose: "A useful item for Other (APQuest).", or for another Bug Fables player's item "For
  BugTester2: " before the item's own description. First the name was "\<player>'s \<item>", but a shopkeeper pastes the
  name into a line the game has already wrapped, so it ran off the bubble ("Interested in that BugTester2's Crunchy
  Leaf?", seen on screen): the name is now the item alone. The shelf's own description box shows every item's name in
  plain black, the game's own included; kept so (2026-09-26): the backdrop and the bubble already carry the
  colour.

**Seen (2026-09-26):** the icon on the ground (the Ladybugs' Sword) and on the Caravan's shelf beside another Bug
Fables player's and your own items; the page with ten rows, every row and both help lines fitting (after a fresh
launch: late in a day of about forty hot reloads the settings pages showed their arrows but no text, and a restart
brought it back; no error was logged, and the letter pool had 487 of 500 free).

**Status:** works, seen on screen (2026-09-26): Other games on the ground and on a shelf; All players seen on a shelf
(2026-10-04); Off seen keeping a Bug Fables player's sprite, the text naming them, and another game's item with the
vanilla look on a shelf and the ground (2026-10-04).

*Code: `QualityOfLife.cs` (`ItemIcons`, `IconMode`), `ApMenu.cs` (`IconsRow`, `RowAt`), `ItemSwap.cs`
(`Describe`), `ApIcon.cs`.*

## 22. Item backgrounds: a check's item class shown before pickup

**Asked (2026-09-26):** the sprite (or the Archipelago icon) says what an item is, not whether it matters; the
starburst a pickup grows when taken already has the class colour. So show it before: a check's item, on the ground
or on a shop shelf, has that starburst behind it (yours included: "include the players own things"), and a Quality of
life row turns it off for a surprise: **Item backgrounds: ON / OFF**, On by default, apart from the icon row.

**How it works** (`ItemSwap.Mark`, called where a location's look is kept: `TickGround`, and the item and medal shops'
shelf ticks): a child sprite `apback` on the item's sprite, the game's starburst (`guisprites[85]`, what a pickup's own
"back" uses), in Archipelago's class colour. Taking the item removes it, so the game's own starburst grows as usual.
Every check's item, yours included.

**Getting it to sit right (on the Caravan's shelf, 2026-09-26):** at 70% and the hold-up's 0.2 behind, it
hung low, its bottom hidden by the counter, and from the side it slid away from its item. A readout (dev `iteminfo`)
settled why: every slot stands at the same height, every item sprite is pivoted at its centre and lifted half its
height (0.5), and the starburst too is centred (`MEASURED.md`); at 60% it reaches 0.84 below the item's centre, below
its base. At 45% the item hid it. So: 60%, only 0.05 behind, and **the item and its starburst raised 0.3 together**
(move the items up so they sit in the centre), back to the game's height when backgrounds are off. Items
still differ in look along a shelf: their pictures differ in shape and margin, as in vanilla. Dev `mark <size> <raise>`
tunes it live.

**Seen on the ground too (2026-09-26):** the Ladybugs' Sword, the icon on plum, raised clear of the stump it
used to sit half inside: "more visually clear than it clipping inside terrain".

**Rarity colours (2026-09-26), in Item colors (step 20).** Side by side on a shelf (dev `markclass`, four
slots forced to the four classes), Archipelago's plum and slate blue read alike, and so did slate blue and cyan: "blue,
red, blue, purple". A ladder from loot games (common green, rare blue, epic purple) told all four apart at
once: filler green `4CC94C`, useful blue `4A90E8`, progression purple `B36BE8`, trap red `E03C3C` (salmon read clearly
but was disliked). So *Item colors* became **RARITY / ARCHIPELAGO / OFF**, Rarity by default, one setting for
the text and the starbursts together: Rarity's text shades are darker for the white box (`8A45C8`, `2F6FD8`, `C62828`,
`2E9E3E`); Off gives the game's red text and its own starburst colours by kind (items teal, key items pink, medals
orange). **Seen (2026-09-26):** Artis's gift with Rarity, "You found Other's Key!" in dark yellow and purple,
the Archipelago icon held up on a purple starburst ("looks really good now"); the Caravan's shelf, blue behind Other's
useful item, green behind BugTester2's filler and the player's own. **Then bought, they turned teal** (seen): a Bug
Fables item's pickup starburst still took the game's colour for its kind. With Item colors on, the starburst at pickup
and on a received item's hold-up is now the class colour too, matching the backdrop; seen on screen (2026-09-26):
BugTester2's Lore Book bought at Madame Butterfly's stayed blue. A bought slot, its check done, goes back to the shop's
own item at the game's height with no backdrop, so a shelf shows at a glance which slots are still checks
("really visible that they are not AP checks anymore"). Off not yet seen.

**Crystal berries stay flat (2026-09-26):** the game draws a crystal berry as a spinning 3D model, which would
cut through the flat starburst close behind it, and every other item is a flat sprite; so a crystal berry is its flat
icon everywhere, crystal berry spots included, always with its backdrop, for the clarity the backdrop gives.

**Then crystal berries lost it (found 2026-10-06, Snakemouth Den's vine berry):** a berry's flat icon is put back on
every pass (the game shows its model again), and that put-back switched off every renderer under the sprite but the
sprite itself, the backdrop included; `Mark` doesn't switch one back on. Not the vine, as first thought: dev `iteminfo`
(now with each backdrop's drawn state) showed the berry's backdrop there but not drawn, the Mushroom's drawn. Fixed by
sparing the backdrop; seen on the vine berry after a fresh room load, the same day.

**Lost behind a faded wall (found 2026-10-06, Snakemouth Den's broken house):** walls that fade as the party comes
near (the game's `FaderRange`) showed the item through them but not its backdrop, depending on distance. Dev
`iteminfo` (now with each one's shader and render queue) showed why: the item draws in the game's sprite queue (2450,
before anything see-through), the backdrop in Unity's default sprite queue (3000), the same queue as the faded wall,
where draw order goes by distance. So the backdrop now takes its item's queue, and shows and hides with it. Seen
inside the house, the same day.

**Scenery cutting into a backdrop (2026-10-04):** at Madame Butterfly's shelf (`BugariaCommercial`,
`ButterflyShopkeeper`) a flower behind the shelf cut into the second slot's starburst (the user's screenshot). The game
draws no backdrop behind a shelf item (its own starburst only appears in the hold-up, `NPCControl.cs:5646`), so there
was nothing of its to copy. Pulling the backdrop from 0.05 to 0.01 behind its item still clipped. The user's idea,
kept: every slot of that shelf a small step toward the camera together, so no one item sticks out: 0.1, through each
slot's sprite holder depth (which the backdrop rides on), for the shop's own items and restocks too. Seen clear the
same day, the row even on the shelf. `ItemShops.ShelvesForward` lists such shelves; dev `shelfforward` tunes a step
live, and dev `mark` takes the backdrop's depth as well.

**Lost against the sky (found 2026-10-08, the Badlands' rock ledge):** the user's screenshot showed the item over the
sky but only a sliver of its backdrop, where ground was behind it: "the starburst goes away with some layers". The
game sorts every entity's sprite by its distance from the camera every other frame (`EntityControl.UpdateGeneralAnim`:
its viewport depth times 1000), so the item's sort order is in the thousands, while the backdrop kept Unity's default,
0. In the same queue, scenery sorted between the two (the sky there) draws over the backdrop but under the item. So
the backdrop now takes its item's sort order less one, right after the game sets it (a postfix on
`UpdateGeneralAnim`, the `MarkSort` group), and stays just behind its item whatever the camera does.

**Status:** works, seen on the Caravan's shelf and on the ground (2026-09-26); with Item colors Off, the game's own
colours by kind seen (2026-10-04): medals orange on the Caravan's shelf and Madeleine's table, an item teal beside
them, and a key item pink (a dev pickup drawn from the pier's checked spot, its Progressive Boat, through `spawn key 27
@7720026`). The sort-order fix built 2026-10-08, not yet seen: the rock ledge's backdrop whole against the sky.

*Code: `ItemSwap.Looks.cs` (`Mark`, `MarkColorOf`, `MarkSort`), `QualityOfLife.cs` (`ItemBackgrounds`), `ApMenu.cs`
(`BackgroundsRow`).*

## 23. The Archipelago logo, drawn in code in the game's style

Another game's item needs a picture in Bug Fables, and today it showed the vanilla item's sprite, which read as the
vanilla item (the tester took another player's Sword for their own). Archipelago has a logo; the question was which
image, and whether it's fine in a public repo forever.

**Drawn in code, so nothing is copied (2026-09-26):** "use art from within the game itself ... something
that looks good but also the same style as the game". The game's round pause-menu icons are flat, one hue as a dark ring
round a pale fill (`MEASURED.md`, the round icons' colours), and the mod already drew circles that way for the Warp
button. So `ApIcon.cs` draws the logo itself at runtime: six overlapping circles in the logo's colours (sampled from
your Archipelago checkout's `data/icon.png`), placed round a circle with the middle open, each later one cutting a gap
into those below, as in the logo. Item-sized, like the party members' icons (the Archipelago guide, build step 18).

**How the look was picked (on screen, eight looks):**
1. The game's orb recipe exactly (dark ring, pale fill): pastel, with outlines too heavy at item size ("the outlines /
   shading is a bit weird").
2. The logo's own colours with a thinner ring, flat with no ring, and the recipe thinner and stronger: better, but on
   the red starburst of a hold-up the red circle vanished, and see-through gaps let any backdrop wash the colours out.
3. As a sticker, the gaps and a rim round the flower filled: white (odd), white thinner, black thin (too sharp), black
   as thick as the first white. Compared as hold-ups on two class backdrops, plum and cyan (dev `holdup ap`), then side
   by side on the Caravan's shelf, close up and at a distance (dev `shelflook`). **Black, the fuller rim**, won: it
   reads on any backdrop, keeps six separate circles at a distance, and matches the game's outlined item sprites.

**Where it's used:** another game's item on the ground, on a shelf, at a pickup and a gift (step 21's row decides whose
items), on the class-coloured backdrop of step 22; and the Jump item's own look, as it belongs to no member
(`CustomItems.cs`).

**Next: thicker outlines** (the user, 2026-09-30, with screenshots of a shop shelf, the icon next to a vanilla leaf and
egg: "it has pretty thin outlines while the vanilla items have really thick and visible ones"). Plan only; the look is
picked on screen. Measured on the game's sprite sheet (`MEASURED.md`, item sprites' outline): the game's outline is
about 4 to 5 sheet pixels, the icon's rim about 2.1 and its lines between circles about 1.5, so both are half as thick.
A rim share of about 0.14 to 0.19 matches (today 0.07); a thicker rim also shrinks the circles a little, since the
drawing is fitted to the same size. How it will be picked:

1. Dev `shelflook` takes a gap share as well as a rim share (today only the rim).
2. On a shop shelf next to vanilla items (the screenshot's leaf and egg), rims 0.14, 0.16 and 0.18 against gaps 0.05,
   0.08 and 0.10, close up and at a distance, then as hold-ups; `holdup ap` first learns the four current class
   colours (today it shows two fixed ones).
3. The user picks; `Rim` and `Gap` in `ApIcon.cs` change, and this step's status says what was seen.

**Thicker outlines, picked on screen (2026-10-04).** `shelflook` first gained a gap share, then every share (rim,
gap, the middle's outline, circle radius and spread) and `#n` for a medal shelf's slots, so each look was a console
line with no rebuild (the user asked why every change needed one: only code changes do). On the Caravan's shelf beside
a vanilla leaf, then on Bugaria's medal shelf beside two medals and the leaf, each on its class backdrop:

1. Rim 0.16, gaps 0.08: about the leaf's outline, but the flower shrank and the open middle closed to a pinhole.
2. The middle carved as a fixed round hole: "the middle circle looks unnatural now". Dropped: the middle's edge must
   be the circles' arcs. So the outline round the middle became its own share, kept at 0.07, and the circles spread
   wider (0.64 to 0.70) to keep the middle as open as the first look's while the gaps thickened. The user, comparing:
   "i like the middle one, where it actually has a proper gap/hole in the middle now".
3. Thicker "to match other items", then "a bit more like the medal": rim 0.20 / gaps 0.14 / spread 0.68 against rim
   **0.22 / gaps 0.16 / spread 0.70**, the user's pick "for now".
4. Tried after the pick and dropped (2026-10-04): thinner lines between the circles (0.10, 0.12, 0.14 with the circles
   pulled in to keep the middle): "they just look off". Then those lines in a dark shade of each circle's colour, as
   the game draws a sprite's inner lines (the mushroom's spots, the leaf's veins), the edge and the middle still
   black: "it just looks weird/transparent ish", the overlaps reading as see-through. The black lines stay.
5. Size and colour (2026-10-04, two new numbers on a look: `scale`, the drawn size, and `sat`, the circles' colour
   strength in HSV). 15% bigger: "i like the current size more than the large ones". Colours ×1.5: "looks to vibrant";
   **×1.2**, at today's size, the user's pick "for now". Then 7% bigger, since the thick lines leave the circles
   smaller than the first look's: "still looks/feels a bit to big". The size stays at 1.

`ApIcon.Look` holds a look's five shares, its size and its colour strength: `Current` (rim 0.22, gap 0.16, middle
0.07, radius 0.39, distance 0.70, scale 1, colours ×1.2) is the icon everywhere; `First` (0.07, 0.05, 0.07, 0.39,
0.60, scale 1, colours ×1, the look since 2026-09-26) is kept to go back to or test against (`shelflook #n black
first`), as the user asked. A thicker rim shrinks the circles, since the flower is fitted
to an item's size; making it bigger is the next knob if wanted.

**Status:** works, seen on screen (2026-09-26) on hold-ups, on the Caravan's shelf and on the ground. Thicker outlines:
picked on a shop shelf (2026-10-04); the new icon seen on the ground and in hold-ups (2026-10-04), as Jump's key item
not yet.

*Code: `ApIcon.cs`; used by `ItemSwap.Describe`.*

## 24. Uncap FPS: frame rates above 60 without speeding the game up

The game's settings offer 30 or 60 fps. More was wanted on a 240 Hz monitor, as a Quality of life row
(first Off, 120, 144, 240; now ten pips, Off the default, below; `UncapFps` in the config), overriding the game's own
frame rate and VSync while Archipelago is on (or with *Use on normal saves*, step 18), and done "properly so things
don't break" (2026-09-27).

**First, read how the game ties itself to frames** (`MEASURED.md`, frame rate). Most motion is scaled by frame time
(`TieFramerate`), but about 30 checks count frames (`Time.frameCount % N`: AI ticks, fishing, fades, shadows), the
tapping-key action command and `FrameDifference` read the frame rate or refresh rate, and an unused "uncapped" setting
would break the tap bar outright. A plain higher cap would make those run faster.

**Then look before building: does a higher cap even look better?** The console's `display` read the monitor (240 Hz)
and the game's settings; `fps <cap>` and `interp on|off` let the tester compare on screen, one change at a time:
1. 240 fps as is: hard to tell apart. The measurement said why: physics steps 50 times a second
   (`fixedDeltaTime` 0.02) and the camera follows in `FixedUpdate`, so most frames repeat the last picture.
2. Characters interpolated (Unity's rigidbody interpolation): worse, "like motion blur", because the camera still jumped
   50 times a second under smoothly moving characters.
3. The camera drawn between its last two physics steps as well (`FrameRate.cs`: placed just before drawing, put back
   after, so the game's camera code never sees it): "better/sharper", and against plain 60, "a really big difference".

**How the row works** (`FrameRate.cs`). Four read-only audits of the game's code, one per share of files, listed every
place it counts frames instead of time first.
- **The cap.** A cap that divides the monitor's refresh rate, or reaches it, is met with VSync (240 on 240 Hz: every
  refresh; 120: every second one; 240 on 180 Hz: every refresh); any other is a limit with VSync off. Without VSync at
  240 on 240 Hz the frame times wobbled from 2.9 to 5.3 ms. Re-applied after the game's own `ApplySettings`; Off
  calls `ApplySettings` to put the game's settings back, except while the game is closing (step 4, 2026-10-04). The
  game's own settings file is never written.
- **Motion drawn between physics steps.** The camera is placed between its last two steps before drawing, and put
  back after. Characters at first got Unity's rigidbody interpolation; since 2026-09-30 they are drawn the camera's
  way instead (the pitfall "drawn at physics steps", below). **Pitfall, found on screen:**
  the main camera has two child cameras, 3DGUI (emoticons, the "!" over NPCs) and the HUD's GUICamera, which draw after
  it. Putting the camera back straight after its own draw left the "!" jittering against the world, on sideways walking
  only. Found by subtraction (on screen, one piece off at a time: interpolation off, still there; camera smoothing off,
  gone), then the console's `cams`. The camera now goes back after the frame's last camera.
- **Pitfall, interpolation against a moving parent: mud on platforms.** Seen at 240: on bridges and moving
  platforms the party moved in slow motion, nearly stuck unless jumping; at 30 and 60 (the game's own, no
  interpolation) normal. The game carries whoever stands on a platform by making them its child (`GroundDetector`,
  `MEASURED.md`), while walking sets the body's velocity; interpolation redraws the body from its own last physics
  poses every frame and so pulled it back against the platform's carrying. **Isolated with one switch:** at 240 on the
  platform, the console's `interp off` (sent through the command file); on screen: "I can move around freely now". **The
  fix:** after the game's ground check (`GroundDetector.OnTriggerStay` / `OnTriggerExit`), a body standing on a
  platform isn't interpolated, and is again once off it. Seen on screen (2026-09-27): normal speed on the platform,
  with a slight shimmer there only (drawn at physics steps); sharp again on the ground. Replaced 2026-09-30: no
  character is interpolated any more (the pitfall "drawn at physics steps", below).
- **Pitfall, a knocked frozen enemy: two faults stacked.** Seen at 240: knocking an enemy in ice looked slow,
  then "stops short". **First fault, frame order:** the knock (`NPCControl.Dizzy`) sets the block's speed flat and hops
  it a frame later; in between, the frozen enemy's own check reads no vertical speed as landed and cancels the slide
  (`icevel`). At 60 a physics step (gravity) nearly always comes between; at 240 usually not. Fixed: a cancel in a frame
  no physics step came before is undone (`Time.fixedTime` unchanged since that enemy's last `Update`); one right after a
  step, a real landing, stands. It covers pushed rocks too (the same check). **Second fault, hidden by the first:** a
  frozen enemy's position is written back every frame (`LimitRadius`), from the drawn pose, which trails the physics one
  under interpolation, so it dragged. The first test with `interp off` showed nothing because the slide was cancelled
  anyway; after the first fix: "worked for 1 hit, then it became slow", and with `interp off` it moved properly. Fixed
  as the platforms: not interpolated while frozen, one decision for both cases so neither undoes the other. **Seen on
  screen (2026-09-27):** knocked around properly, every time. The first fix stays; the second was replaced 2026-09-30,
  when interpolation went for every character (below).
- **Pitfall, Vi's flight in slow motion** (the user, 2026-09-29, at 240). While Vi flies, `PlayerControl.LateUpdate`
  lifts her by writing her whole position every frame (the rise), read from the drawn pose, which trails the physics
  one under interpolation: the frozen enemy's second fault again. Her flying speed itself is a velocity, the same at
  any frame rate. The console's `interp off` can no longer isolate it: since the platform fix, the ground check
  re-decides interpolation every physics step, so the switch is undone before a flight starts. So the fix is the
  test, one change: the leader isn't interpolated while flying, in the same one decision (checked before the
  player's `LateUpdate`). If the slow motion stays, the cause is elsewhere and the change comes out. **Seen on screen
  (2026-09-29, Monitor at 240 Hz):** "fly works now"; Kabbu and Leif following her, "they look the same as Vi".
  Replaced 2026-09-30 with the rest (below).
- **Pitfall, drawn at physics steps: blurry at 240** (the user, 2026-09-30). Everywhere interpolation had been
  turned off (platforms, flight, frozen enemies) the leader looked "really blurry/bad" while moving; the party
  following looked fine. The camera is smoothed, so a body drawn only 50 times a second jumps against it. The first
  fix smoothed only those bodies and changed nothing on screen. The console's `bodytrace` (the leader's place on screen
  each frame) showed why: the user was on a **conveyor**, with interpolation *on*, and the leader still jumped about
  22 px every physics step (spread 11.3 px), because the conveyor moves her by writing her position inside the step
  (`PlayerControl.OnTriggerStay`), which Unity's interpolation doesn't smooth. **The fix, the user's idea: split
  movement from drawing.** Unity's interpolation is no longer used; every character has `None` (the player's in the
  game, which never sets it) and is drawn as the camera is. Right after each physics step (a `WaitForFixedUpdate`
  coroutine, which Unity runs after the step's trigger messages) its pose is read. Before drawing, it is set back by the
  share of that step's move not yet played, and put back after the last camera, so the game never reads a drawn pose and
  the slow motion can't return. The move is measured from its pose at the last draw (nothing runs between a draw and the
  next step), so what the game writes every frame (a platform carrying it, Vi's rise) is drawn as it is. The move is
  kept in the parent's space, for turning platforms. The same trace on the conveyor afterwards: drawn spread 4.3 px
  against 12.2 px for the true pose (the leader speeding up). Seen on screen: "the belt looks good now", flying "looks
  good now", but Kabbu "looks weird when Vi is using fly". In flight the game puts Kabbu at Vi's true position every
  frame (`EntityControl.Follow`), so he ran ahead of her drawn pose. A character the game copies another into every
  frame (Kabbu in flight; a temporary follower in flight or while digging) now takes that one's offset: "kabbu looks
  good during flight now". A safety net puts poses back before a physics step, should the last camera not draw.
- **Pitfall, scenery swung inside physics steps: the Rubber Prison's swinging platforms** (the user, 2026-10-01, at
  240: "the platform itself + the chains get a bit blurred when its moving"). Standing on one, the console's `solids`
  named it: `swingingplatform`, a `StaticModelAnim`, holding `CranePlatform`, which the party stands on as its
  children. `StaticModelAnim` writes its swing and bob in `FixedUpdate`, 50 times a second, so against the smoothed
  camera the platform jumped at each step. The party on it looked smooth only because a character's step move
  included the swing's carry. **The fix** (`FrameRate.Scenery.cs`): every `StaticModelAnim` swinging or bobbing by the
  game's own test is read after each physics step, in its parent's space, drawn between its last two poses as the
  camera is, and put back after the last camera. A `KeepAngle` under it (the crane platform hanging level) is held level
  again for the draw. A character's step move is now measured in its parent's space when the parent stayed the same,
  so the swing carries it once, not twice; for a parent moved outside the step it measures the same as before.
  **Pitfall inside the fix:** first seen "a bit better/sharper, but now it looks as if its stuttering a bit". Tracing
  each frame showed the platform unsmoothed in 163 of 240 frames, snapping about 10 px at a step: an object whose last
  two poses were equal was skipped, and Unity's `Quaternion ==` counts rotations under 0.162 degrees apart as equal
  (read in its assembly), about what this swing turns in a step. The skip now compares exactly. **Measured after**
  (`bodytrace`, now also tracing what the leader stands on; the console's `scenerylerp off|on` twice, standing on the
  platform): smoothed in 237 and 238 of 240 frames; its change of on-screen speed per frame 0.07 to 0.08 px/ms drawn,
  against 1.2 to 2.2 for its true pose; the party on it the same. **Seen on screen (2026-10-01):** "the platform & the
  chain look good now".
- **Random shakes re-rolled once per 1/60 s.** Some effects jump to a new random offset every frame, a blur at 240
  (seen: shaky text in conversations sharp at 60, blurry at 240). Their timing was already right; only the re-roll
  was per frame. Now, while the row is on, the offset holds between ticks: `FontEffects` (shaky and glitchy letters;
  a shaky letter's position also overrides wavy, so wavy holds with it), `MainManager.ShakeObject` (the bushes before
  the leaf gang's ambush, Event128, and many scenes) and `EntityControl.ShakeSprite` (a character's shake), the last two
  run as the game's own loop with the offset kept. The camera's screen shake needs nothing: it's rolled in
  `FixedUpdate`, 50 times a second at any frame rate (seen: the swamp bridge's collapse, Event130, looked normal
  at 240). **Seen on screen (2026-09-27):** the text sharp at 240. Not
  yet seen: the bushes, a character's shake.
- **What the game counts in frames runs 60 times a second.** Every method that reads `Time.frameCount` (24, from a
  fixed list the console's `fpsscan` checks; each body is read again at load) sees a 60 Hz count instead: on a frame
  that starts a new 1/60 s, the count; on the frames between, 1, which no `% n` check divides. `FrameDifference` ("once
  every 1/60 s") answers the same way. **Pitfall, the opposite test** (2026-09-30, found with `bodytrace`): a follower's
  walk-or-brake decision, `EntityControl.DoFollow`, *skips* its work when the count divides (`if (Time.frameCount %
  2 == 0) return;`). Given 1 in between, it ran on every frame there: about 210 times a second at 240 against 30 at 60.
  The party following the leader on a conveyor then started and stopped at once: "a bit choppy", the user said, once the
  leader was sharp. The trace showed the follower's speed changing every frame, 13.2 to 3.1 to 7.7 to 0.7 within a
  second and a half. Such a method gets 0 in between instead, which every n divides, so it skips there too. Read in
  context, every other site does its work on the divided count or returns on `!= 0`, so only `DoFollow` has the opposite
  test. The site that had scaled its braking (`StopForceMove`, whose only smooth brake is `DoFollow`'s) was compensating
  for the same bug and came out. After both: walk, brake by half every 1/30 s, walk, a steady rhythm about every 0.1 s,
  as at 60.
- **The mod's own frame counts, counted the same way** (2026-10-01; the user: "don't we match the game when doing
  uncap fps?"). The row had patched only the game's counts. The hold-ups' waits (30 free frames before the first of a
  burst, 3 before each of the rest, 5 after each one shows; step 10) and the auto-save's 20 free frames (step 31)
  still counted rendered frames, so at 240 they waited a quarter as long: the half second between chained scenes an
  eighth. Each now counts only on a frame that starts a new 1/60 s (`FrameRate.OnTick`), as a game counter's 1 does;
  with the row off that is every frame, as before. Nothing wrong had been seen on screen.
- **Frame time inside a physics step reads as it does at 60.** Code in `FixedUpdate` and trigger or collision messages
  scales by `framestep`/`TieFramerate`, which follow the render frame: at 240 fps conveyor belts, wind and the
  safe-respawn point would have run at a quarter strength. There, `TieFramerate(x)` returns `x` and `framestep` 1.
- **The tapping-key action command** reads the target frame rate, which the row sets to the rate that results.
- **Pitfall, the mod half-loaded:** the first build read method bodies with a Harmony call that needs
  `System.Reflection.Emit.ILGeneration`, which this game's Mono lacks; the exception aborted the plugin's `Awake` and
  every feature after it, hot reload included (a restart was needed). `PatchProcessor.ReadMethodBody` needs no such
  assembly, and the row's setup now catches its own failure and stays off.

- **The rest, one site at a time** (`FrameSites.cs`). The audits' list: a counter that ticks once a frame, a fixed
  amount added each frame, a smoothing step with a fixed factor. Each site is patched on its exact instructions, written
  against the method's IL (the console's `il`), with the count of matches it expects; a site that doesn't match exactly
  is left alone and logged. Three kinds of fix: a counter's 1 counts only on a frame that starts a new 1/60 s (and a
  check made right after it sees the counter only on that frame: the disguised enemy turns at 80 and 40, once each); an
  amount is scaled by the frame's worth in sixtieths; a factor f becomes 1 - (1 - f)^k. Per-frame blinks (`enabled =
  !enabled`) flip at most once per renderer each 1/60 s. A cutscene's `FloorToInt(a) % n == 0` on a time-driven `a`
  counts once per whole value. Text waits round up to whole sixtieths, as a 60 fps frame does.
  **Gameplay:** fishing's fish approach and nibble, the screw platform, the Wacka Worm, disguised enemies, wandering
  enemies' retries, enemies settling to their height, dizzy enemies dropping, gate slides, the dig skill's aim in
  battle, Vi's hover, the map's culling grace. **Scenes:** the battle drop, return from digging, two scenes' turns
  (26, 99) and a fade, text waits. **Looks:** spins, sprite turning, the dig spin, followers catching up, the Watcher's
  eye, the battle EXP counter, damage numbers, the enemy beemerang, particles, blinking. **Left as they are**
  (cosmetic): random jitter, some battle skills' spin effects, HUD numbers counting up, fleeing losing a berry a frame
  sooner.
- **How the logic is checked without the game on screen.** Every fix rests on two measures: what a frame is worth in
  sixtieths, and whether it starts a new sixtieth. The console's `rates` sums both over a few seconds: at 240 fps,
  59.88 sixtieths and 59.78 new-sixtieth frames a second (2026-09-27), so everything built on them runs as at 60 (a
  long frame counts as at most one, as at 60 fps). What each site does on screen still needs the tester.
- **Installed only when the row is on:** with it off nothing of the game is patched. Installing takes about 4 s, 3 of
  them for the battle's action coroutine (one enormous method). The methods to patch come from a fixed list; the
  console's `fpsscan` reads all 4111 of the game's methods and compares (2026-09-27: nothing missing, nothing stale).
- **Pitfall, a transpiler that throws poisons its method.** The first site build used `CodeInstruction.labels`, which
  this game's HarmonyX lacks. The failed transpiler stayed registered on its methods, so the next feature to patch one
  of them (the pause menu's Settings rows, on `PauseMenu.Update`) failed with it, aborting the plugin's `Awake`, hot
  reload included: every later build sat unloaded, which looked like fixes that changed nothing. Found by reading the
  error at the end of the log, after two guessed fixes failed the same way. Only a game restart clears it; the sites'
  transpiler now never throws (it returns the method unchanged and logs why).

**Ten pips, and Monitor by default (2026-09-28).** A tester played on a 180 Hz monitor, where 120 and 144 divide
nothing (a limit without VSync, so tearing) and only 240 synced (at 180), with nothing on screen saying so. Other
common rates (165, 170, 200, 360, 480) had the same gap. So the row was made to work like the volume rows:
ten pips, the first Off, then 90, 100, 120, 144, 165, 180, 240, 360, and **Monitor** last, which is the display's own
refresh rate (`Screen.currentResolution.refreshRate`) met with VSync, so any display is smooth without tearing. At 60 Hz
or less Monitor keeps the game's own setting, and the row's line says so. The row stops at its ends, as the volume rows
do. Monitor was the default from 2026-09-28 (once shaky text was fixed and no odd combat had been seen) to 2026-09-29.
The first frame with the row on installs the frame sites (a few seconds). Rates above 240 are untested. **Seen on screen
(2026-09-28):** the ten pips look and work fine.

**Off by default again (2026-09-29, the user).** A fresh config and the panel's reset give Off, the game's own frame
rate; Monitor stays the last pip. As before, a config that already stores a value keeps it (no migration), so a
config that says Monitor stays at Monitor until the row is changed. **Seen (2026-10-04):** the panel's Reset to
defaults puts the row at OFF.

**Status:** in progress, experimental (the row says so). Seen on screen (2026-09-27) at 240: smooth, the "!" steady and
sharp. The logic measured (`rates`); each site patched as expected (the log's `[fps] frame sites`). Not yet seen on
screen: every site above, most of all fishing, the screw platform, the Wacka Worm, a disguised enemy and the dig skill;
waived by the user (2026-10-04: assumed fine until an issue shows).
Platforms and bridges: the slow motion fixed and seen (2026-09-27). Vi's flight: fixed and seen (2026-09-29). Every
character drawn smoothed (2026-09-30): seen sharp on a conveyor and in Vi's flight, Kabbu with her, and on the
Rubber Prison's swinging platform (2026-10-01); bridges, a knocked frozen enemy, the "!" over NPCs and shadows during
jumps not yet seen with it. Followers deciding walk or brake 30 times a second, as at 60 (2026-09-30): measured with
`bodytrace`; on the conveyor, "I think it looks fine", hard to tell next to the leader. Off by default again
(2026-09-29): seen after the panel's Reset to defaults (2026-10-04). Swinging and bobbing scenery drawn smoothed
(2026-10-01): measured and seen on the Rubber Prison's swinging platform, the party standing on it; other bobbing
scenery (boats, floating things) not yet seen. The mod's own frame counts (hold-ups, auto-save) counted in sixtieths
(2026-10-01): built, not yet seen.

*Code: `FrameRate.cs`, `FrameRate.Scenery.cs`, `FrameSites.cs`, the row in `ApMenu.cs` and `QualityOfLife.cs`, the
after-physics hooks in `Plugin.cs`, the waits in `HoldUps.cs` and `AutoSave.cs`; the
console's `display`, `fps`, `interp`, `camlerp`, `bodylerp`, `scenerylerp`, `bodytrace`, `frames`, `trace`, `cams`,
`il`, `rates` and `fpsscan` (`DevConsole.cs`, the traces and scans in `Dev/FrameRate.Dev.cs`).*

## 25. Hitches fixed: the mod's garbage and the game's 5-second collection

Found while measuring step 24, at 60 fps as well as at 240: an FPS counter dipping (246 to 220 at 240 fps) every few
seconds. The ask: "I just want the fps fixed and the dips removed, and yes always on with archipelago" (2026-09-27).

**Measure, don't guess.** The console's `frames <seconds>` logs every frame over twice the median with its time and
whether a garbage collection ran. Two clocks showed up:
1. **Every ~1.8 s, 42 ms: the mod's own garbage.** Timing each of the plugin's per-frame jobs in turn and counting what
   each allocated put 68 KB a frame on the check tick: it asked, every frame, whether each shop copy was bought, and
   each answer rebuilt and sorted every shop's list. Built once per `slot_data` now (build step 6): 3.6 KB a frame, the
   collections from 7 to 2 in 12 s. At 60 fps it cost the same, a quarter as often.
2. **Every 5.00 s, 45 + 66 ms: the game.** Its play-time clock (`MainManager.DoClock`) unloads unused assets and forces
   a collection every fifth second. Pinging the server every 30 s instead of 5 left it in place, which ruled the mod's
   connection out; the game's code showed the rest. With Archipelago on, or with Use on normal saves (step 18,
   2026-09-27: for vanilla chapters at 240), the mod skips those two calls there (`ClockCleanup.cs`); leaving a
   map still does both, and the runtime collects when memory needs it. 20 s of play
   afterwards: one collection (47 ms) instead of four double stalls.

**Status:** works, measured (2026-09-27). The tester's FPS counter dipping to 220 was that stall; confirmation on screen
that it's gone waived by the user (2026-10-04: assumed fine until an issue shows).

*Code: `ClockCleanup.cs`; `LocationChecks.cs` and `ShopSwap.cs` (`Copies`); the console's `frames`
(`DevConsole.cs`, `Dev/FrameRate.Dev.cs`).*

## 26. Field abilities as items in the game: ability checks read the bag

Every ability the story teaches became an item (the Archipelago side, build step 23). The game remembers a learned
ability as a flag, and that flag does three jobs: it lets the party *use* the ability, it gives a battle skill, and it
is story state (a scene checks it; a rock or a miniboss is gone once it's set). Only the first two may follow the item.

- **Read the game's reads in its IL.** The decompiled C# shows where each flag is read, but a patch matches IL: an
  ILSpy IL dump counted the exact pattern (`ldfld flags; ldc.i4 n; ldelem.u1`): 8 in `PlayerControl`, 2 in
  `NPCControl`'s Beemerang (flag 21), 15 in `MainManager.RefreshSkills` (the C# suggested 14). Every other read is
  story and stays.
- **Swap only the read.** That one instruction becomes a call taking the same two values and returning the same bool:
  the game's flag, or, in a seed with ability items, whether the key item is in the bag. Nothing is written to the save
  but the key item, which the receiver gives like any other.
- **Log what the patch decided:** the counts found against the measured 8, 2 and 15, an error when they differ.
- **Seven key items** (205-211) with the game's own names and field descriptions from `skilldata`; the battle skill
  rides on the same key item (fourteen would be bloat).

**Status:** built (2026-09-27); seen (2026-10-04): Bee Fly, received from another slot, worked on the field and its
battle skill showed; Dash and Bee Fly in Key Items with the game's names and text.

*Code: `Abilities.cs`; the receiver in `ItemReceiver.cs`, the key items in `CustomItems.cs`, the looks in
`ItemSwap.Looks.cs`, `slot_data` `ability_items` in `SeedData.cs`, the Warp in `QualityOfLife.cs`.*

## 27. Attack boost: +1 damage on every hit

An opt-in for a faster, easier game, asked for after a hard boss (2026-09-28). Enemy scaling (step 17) balances
an area met early or late; this is for a fight that's hard at the right level.

**Decided (2026-09-28):**
- A row on the **Gameplay** page, *Attack boost*: **Off / +1, off by default** ("a kinda cheaty gameplay
  setting"). No +2 or more.
- **+1 attack, not +1 per hit, was the ask.** Read first: nearly every attack and skill reads the attack stat on each
  hit, so the two are the same thing; a three-hit skill gains 3. At attack 2-3 that is +33-50%, pointed out before
  building.
- Only while Archipelago is enabled, or with *Use on normal saves* (step 18). No check and no logic depend on it.

**How it works** (`AttackBoost.cs`, the facts in `MEASURED.md`, "The game's own per-hit bonuses"):
- **Read how the game does it first.** `BattleControl.CalculateBaseDamage` already adds the party's own per-hit
  bonuses (+1 for the member in front, two medals) when the attacker is a party member, after it returns Raw hits
  as they are, and never in the demo battle.
- **A prefix on that function adds 1 under the same conditions.** The save's attack stat is never written; switching
  the row off ends it at the next hit.
- **Shown on the medals screen** (2026-09-28: "nice to visually see/know about it", when setting up medals).
  The game rewrites that screen's stats every frame (`PauseMenu.UpdateDynamicText`, window 2, the second line the chosen
  member's attack); a postfix writes attack + 1 into that line only. The stat itself stays untouched.
- Each page's two buttons: Reset and Disable all both set Off.

**Status:** the medals screen's +1 seen on screen (2026-09-28): Vi's attack 3 (with Power Exchange) shown as 04, and
right as medals go on and off. The +1 in a fight's damage not yet seen.

*Code: `AttackBoost.cs`; the row in `ApMenu.cs` and `ApMenu.Rows.cs`.*

## 28. A Graphics page, tried and removed: render scale and MSAA

Asked for after the frame-rate work (2026-09-28): the game, mostly its paper sprites, looking sharper. Built,
seen working, and removed the same day because it cost too much for what it showed. Kept here as a record of the
process.

- **Ruled out first:** DLSS / FSR (they render *lower* and upscale, for speed; the game is light and already draws at
  the screen's resolution) and upscaled art (a texture pack of the game's own art can't be published).
- **Measured first, in the running game** (the console's `cams`, extended for this; `MEASURED.md`, "How the game draws
  a frame"): MSAA was 0 and never set by the game; three forward-rendering cameras; the game's FXAA on the world camera;
  the game's own render scale goes only down (100% to 40%), drawing into a texture a quad shows on screen; the
  sprites' textures already at full size.
- **Built** (a third settings page was chosen, Graphics, with Uncap FPS moved onto it): Render scale 100 / 150 /
  200% (the world and 3DGUI cameras drawn into a larger texture) and Anti-aliasing Off / 2x / 4x / 8x MSAA.
- **The game's own quad looked like an old TV** (seen at 200%: curved, inset, vertical colour stripes, darker):
  its shader is `Custom/CRT`, the minigames' look. A plain copy to the screen before the HUD camera (a command buffer
  `Blit`) replaced it, and 150% and 200% then looked normal (seen on screen).
- **Removed (2026-09-28): "bad result/useless for the fps impact".** 200% with 8x MSAA took a 1920x1080
  window from 240 to about 95 fps, and on screen either row made little or no difference: the art's thick outlines
  and the game's own FXAA leave few jagged edges, and the result is shrunk back into the window. Uncap FPS went back to
  Quality of life; the page, its Settings row and `RenderQuality.cs` are gone.
- **What would sharpen the sprites instead:** the game's own 3840x2160 resolution, when the screen has it (the tester's
  game ran a 1920x1080 window on a 3840x2160 screen).

**Status:** removed (2026-09-28), once it had been seen working.

## 29. Save crystals used with the confirm button, no move needed

With Shuffle Field Moves (the Archipelago side, build step 21), the party may go far into a seed with no field attack,
and the game only starts a save crystal when an attack hits it: no save and no heal until a move item arrives (a gap
spotted 2026-09-28).

**Decided (2026-09-28):** confirm next to a crystal uses it, "like talking to an NPC", not touching it (a
prompt each time you brush past one); always, in a seed, whether or not a move is in hand. Hitting it still works.

- **Read how the game does it first** (`MEASURED.md`, "Save crystals, saving, Game Over and room transfers"): the hit
  bounces the crystal, plays its sound, heals if it's yellow, shows the hit sparkle, then opens the save prompt. The
  heal is in the hit, not in the prompt.
- **The confirm button jumps next to a crystal**, because the game only talks to NPCs, and the crystal drops in and out
  of the player's talk list. So the mod finds the nearest crystal itself, on the same inside/outside view, and only when
  no one to talk to is in front. **Its reach is an NPC's talking range**: the game's own test (distance under the
  entity's radius), with 1.6, the radius nearly every NPC has, since a crystal's own is 0.
  - The first version used the hit's reach (squared distance 30, about 5.5): a jump anywhere near a crystal became a
    save prompt.
  - The console's `radii` measured it (`MEASURED.md`).
  - Seen (2026-09-29): the right distance, and out of the way of jumping.
- **The jump's prefix** (already there for Shuffle Jump, `FieldMoves.cs`) asks first: a crystal in reach takes the
  press and runs the game's hit steps in order, the prompt last; the save is the game's own. No jump, no buzzer.
- **The "?" over the player** while in reach, the one the game shows next to something to check, such as a discovery
  (seen 2026-10-01).
- Red DeadLander crystals are left alone (their hit turns a DeadLander). Only while Archipelago is enabled.

**Status:** works: the range seen right on screen (2026-09-29), the "?" over the player (2026-10-01).

*Code: `SaveCrystals.cs` (`InReach`, `TryUse`, `Tick`); the call in `FieldMoves.BeforeJump`.*

## 30. Healing crystals: every save crystal heals

Asked for with step 29 (2026-09-28), and suggested on Discord before (the Archipelago side, "Where it
stands", Next 22): blue crystals only save, yellow ones save and heal.

**Decided (2026-09-28):** a row on the **Gameplay** page, *Healing crystals*, **ON / OFF, off by default**
(off: as the game has them; on: every crystal yellow). The game names no "save crystal" (it says "ancient crystal"),
so the row's name is ours. Only while Archipelago is enabled, or with *Use on normal saves* (step 18). No check and
no logic depend on it.

- **One value decides both the look and the heal:** `data[2] == 0` makes `SetUp` tint the crystal yellow and the hit
  heal. A prefix on `NPCControl.SetUp` sets it for every save crystal except the red ones, so the game's own code does
  the rest: the colour, the heal on a hit, and step 29's confirm. `data` is the map's entity data, never saved.
- **Switching it takes effect in the next room**, when the map's crystals are built again (said in the row's help).
- **The Gameplay page grew a row:** its rows then spread between the same top and bottom row as Quality of life's
  did (since 2026-09-30 both pages show seven rows at the game's spacing and scroll, step 8).
  Each row with a value gets its left/right arrows from one list per page; *Healing crystals* and *Auto-save* were
  first missing from it (caught 2026-09-28, before anyone saw it).
- Reset sets Off; Disable all sets Off.

**Status:** built (2026-09-28), not yet seen in game.

*Code: `SaveCrystals.cs` (`BeforeSetUp`); the row in `ApMenu.cs` and `ApMenu.Rows.cs`.*

## 31. Auto-save between rooms: a death costs one room

Asked for with DeathLink (the Archipelago side, build step 25; 2026-09-28): a received death goes back to the
last save, and story progress lives only in the save (items and checks come back from the server), so without saves
along the way a death can mean a long walk back and scenes played again.

**Decided (2026-09-28):** a row on the **Gameplay** page, *Auto-save*, **ON / OFF, off by default**; its own
row, not tied to DeathLink ("an on/off thing in gameplay, not forced"). Saves go to the room you walk into, not more
than once every 15 seconds, so going back and forth through a door doesn't save over and over. Only while
Archipelago is enabled, or with *Use on normal saves* (step 18). Save crystals work as before.

- **Read how the game saves first** (`MEASURED.md`, "Save crystals, saving, Game Over and room transfers"): the
  crystal's Yes calls `MainManager.Save(position)`, which writes the whole save (the map, the position given, HP as it
  is, flags) through `InputIO.Save`, keeping the old file as a backup. No UI, no precondition. The mod's
  randomizer-save redirect catches it like every save.
- **When:** a door's transfer holds `roomtransition` until the party has walked in, then sets `lastloadzone`, where
  the walk ended. When it clears in a new map, the save waits until the player has been free for 20 frames in a row
  (a third of a second at 60 FPS, counted in sixtieths under Uncap FPS, step 24; no scene, text box, menu, battle or
  transition, on the ground). A map's auto-event starts as soon as the player is
  free and sets its flag as it starts, so saving on the first free frame could store that flag and skip the scene on a
  reload; if a scene starts, the save waits until it ends. Cutscene map changes aren't doors and don't save.
- **Where:** at the room's entrance (`lastloadzone`), where a reload puts the party.
- **Within 15 seconds of the last auto-save** the room waits rather than being skipped: it saves once the time is up,
  if you're still there and free, so the save is always the room you're in.
- **Never while a DeathLink death is waiting or under way**, so a reload can't come back to a state saved after the
  death.
- Reset sets Off; Disable all sets Off. No sound or text on screen yet; the log says `[autosave] saved`.

**Status:** built (2026-09-28), not yet seen in game.

*Code: `AutoSave.cs`; `DeathLinkGame.Busy`; the row in `ApMenu.cs` and `ApMenu.Rows.cs`.*

## 32. Reproducible builds: the release DLL rebuilt byte for byte

The mod's DLL is the one file in a release that GitHub can't build: compiling it needs the game's own
`Assembly-CSharp.dll`, which never enters the repo. So it's built on the maintainer's machine and committed. That
leaves a question a player or a reviewer is right to ask: **is the committed DLL really what the committed source
makes?** A hash written next to it can't answer that, because whoever built the DLL also wrote the hash. What can
answer it is a build anyone who owns the game can repeat and get the same bytes.

**Measured first (2026-09-29):** each published DLL rebuilt from its own commit, in a fresh folder:

| Release | Rebuilt | Result |
|---|---|---|
| v0.1.0 | from its tag | identical to the published DLL |
| v0.2.0 | from its tag, on two different .NET SDKs (10.0.401 and 10.0.302) | identical |
| the DLL committed on 2026-09-28 | from the commit that added it (`a43d9da`) | identical |

One thing it showed: that last DLL's record named the commit *before* it, because the build ran on a working copy
with its changes not yet committed. The bytes were right; the record pointed at the wrong commit.

**Then the build was pinned,** so the same inputs stay the same:
- **Every package at one exact version, with its content hash:** `BepInEx.Core` was `5.4.*` (any 5.4 release), now
  5.4.21. A lock file, `packages.lock.json`, holds every package's hash, and a restore that would change anything
  fails.
- **Each package from one feed only** (`nuget.config`): BepInEx's packages and `UnityEngine.Modules` from BepInEx's
  feed, everything else from nuget.org, so a same-named package on the other feed can never be picked.
- **One SDK** (`global.json`), recorded in each build.
- **No build files from outside the repo** (`Directory.Build.props`): MSBuild otherwise picks up build files from
  every folder above the project and from per-user folders.

With the pins the DLL came out byte-identical to the build before them, so they changed nothing in it.

**How a release DLL is built now** (`dev-scripts/build-release.ps1`). It refuses to stage one unless every check
passes:
1. **Only committed code.** Tracked files must have no uncommitted changes, and the build runs in fresh clones of
   HEAD, so nothing that isn't committed can reach the compiler.
2. **Built twice, in two clean clones at different paths, and both come out byte-identical.** No path, time or
   machine state goes into it.
3. **Only the two known packages run code during the build:** packages with build scripts or analyzers must be
   exactly `BepInEx.Core` and `NETStandard.Library`.
4. **The three libraries don't change silently.** They must match the committed copies, unless the change is
   deliberate (`-NewLibraries`).
5. **Unchanged sources rebuild the committed DLL exactly.** If none of the build's inputs changed since the committed
   DLL was built, the new DLL must be byte-identical to it, or the committed one isn't what these sources make.
6. **Preflight's DLL checks pass on the fresh build** before anything is staged: a plain compiled library, no denied
   call, and nothing its source doesn't say (apimplementation.md, build step 28). The `[Debug]` settings stay in
   the dev build, and exactly the shipped files are staged.

`release/built-from.txt` then records the full commit, the SDK and the game's `Assembly-CSharp.dll` hash (which game
build it was compiled against), next to each input's hash and each DLL's.

**What this proves, and to whom.** It makes the maintainer sure. Someone else can't see the build machine, so for
them it proves nothing until they rebuild from the tag with their own copy of the game and compare the hash (the
recipe is in `docs/reviewing.md`), or decompile the DLL and read it.

**Status:** built (2026-09-29). The three rebuilds and the unchanged pinned build were measured on this machine.

*Code: `dev-scripts/build-release.ps1`; `mod/BugFablesAP/BugFablesAP.csproj`, `packages.lock.json`; `nuget.config`,
`global.json`, `Directory.Build.props`.*

## 33. Server text cleaned before the game shows it

**Found by the review (2026-09-29, apimplementation.md build step 28):** the names that reach the game from outside
went into its text as they came. They are other players' names, other games' item names, and the seed's name, which
the save keeps. Two things in the game's own code make that unsafe:
- **The text engine runs commands written between `|` marks.** Colours, but also setting story flags, changing money,
  giving items, moving the party. It reads a substituted string as more text to run: the `string` command puts
  `flagstring[n]` into the line and steps back to parse it (`MainManager.SetText`, `MEASURED.md`). So a player called
  `|flag,500,true|` would have set flag 500 in the game of anyone who received an item from them.
- **A save splits on `|SPLIT|`, the not sign and line breaks.** The seed's name and the last item name shown are
  saved (`flagstring[5]` and `[0]`), so either of those inside a name would shift the save's fields on the next load.

**The fix (2026-09-29):** one class, `ServerText`. Every string the server or another game decides is read through it
before the game shows or saves it:
- player names, item and location names, game names, the seed;
- `|`, the not sign, and control and format characters are dropped;
- a string is kept to 100 characters.

A normal name is unchanged, and so is every seed name Archipelago makes, so saves already tied to a seed still match.
Comparisons go through it too (whether an item is Bug Fables'), so there is one reading of each value. The preflight
refuses a raw read anywhere else in the mod, so a new text path can't go around it (apimplementation.md, build
step 28).

**What to check in the game:**
- another player's item on the ground, on a shelf, and in its "You got" box;
- an item received from another player;
- a shop slot holding another player's item.

Each still shows the right names, colours and descriptions, as before.

**Status:** built (2026-09-29); seen (2026-10-04): another Bug Fables player's name and items as before on the ground,
on a shelf (its description naming them), in the "You got" box and in the hold-ups.

*Code: `Core/ServerText.cs`; its callers in `ItemSwap.cs`, `ItemSwap.Looks.cs`, `ItemReceiver.cs`, `LocationChecks.cs`,
`ApConnection.cs`.*

## 34. MultiClient.Net's cache kept in its own folder

**Found by the review (2026-09-29, apimplementation.md build step 28):** MultiClient.Net, the library the mod connects
with, caches each game's item and location names. The file goes
in `Archipelago\Cache\datapackage\<game>\<checksum>.json` in the user's local application data, and both names come from
the server:
- **The "safe name" function returns its input unchanged** in the version the mod ships (6.7.1). It cleans a copy and
  hands back the original (`MEASURED.md`).
- **The checksum used for reading isn't cleaned at all.**

So a server could have named a path outside the cache: `..`, a folder separator, or a whole drive path. The file is
always `.json` and holds only the game's names, but it could have replaced another program's file of the same name. A
character Windows refuses in a name would also have thrown inside the library, while it handled the server's first
message.

**The fix (2026-09-29):** two patches on the library's cache class (`Core/CachePaths.cs`):
- **Its safe-name function does what it was meant to.** It drops every character a file name can't hold (Windows'
  set, on every system) and control characters, and keeps 100 characters. It never gives an empty name, dots alone,
  or a device name like `CON`.
- **The checksum is cleaned the same way** before the cache is read.

A real game name and checksum come through unchanged, so the cache works as before; a `:` is dropped, as the library
meant. The class is internal to the library, so the patches name it as text (`[HarmonyPatch("Type, Assembly",
"Method")]`). HarmonyX looks it up when the plugin loads; if it's missing, the plugin stops rather than run with the
cache unguarded, as the save redirect does. Both targets are rows in `docs/capabilities.md`, and preflight reads them
back out of the built DLL (apimplementation.md, build step 28).

**Tested outside the game (2026-09-29):**
- **The cleaning, on 24 names in a scratch console run**, hostile and normal alike. Every hostile one came out as a
  single name inside the folder. `Bug Fables`, `Pokémon Red and Blue` and a real checksum stayed as they were.
- **A Release build of this source through preflight's DLL sections.** It found 5 patch targets outside the game, all
  listed. With the two new rows removed, it failed and named exactly these two.

**What to check in the game:**
- the BepInEx log at start shows `[cache] data package cache names made safe`, and no `NOT installed`;
- connecting to a room still works, and other games' item names still show;
- the cache folder holds a `Bug Fables` folder with a `<checksum>.json` in it.

**Status:** built (2026-09-29); the log line, connecting and the cache folder checked (2026-10-04: no `NOT installed`,
a new `Bug Fables\<checksum>.json` at the seed's first login); another game's item name seen on a shelf (2026-10-04,
"A filler item for OtherPlayerQuest (APQuest)").

*Code: `Core/CachePaths.cs`, installed from `Core/Plugin.cs`.*

## 35. Shuffle Shop Inventories in the game: shelf slots and pickups swapped

**The idea (the user, 2026-09-30):** once an item shop slot's check is bought, or a respawning pickup's check is found,
the spot sells or gives another spot's item, as the seed says (apimplementation.md, build step 34). It's never a check.

**How the game already does it:** a medal pickup under the random-medal cheat becomes another medal as it's taken
(`NPCControl.CheckItem`). The game sets three fields on the entity, `basestate`, `itemstate` and `animstate`, to the
new id and calls `UpdateItem()`. Everything after that reads those fields:
- a shop slot's price, name and the item its buy line adds (`Interact`);
- the description box (`CreateDescWindow`);
- the sprite (`UpdateItem`);
- the item a pickup adds (`CheckItem`).

Both `Interact` and `CreateDescWindow` first copy `itemstate` back into `animstate`, so setting only `animstate` would
not hold. The mod does the same three fields and the same call, so the price is the game's own for that item.

**Which slot is which:** the map makes shop slot n from its keeper's list (`Fixedshop<n>`, from the keeper's
`data[n]`), and the name stays. The mod reads a slot's stocked item from there, never from the entity, because the swap
changes the entity. The item shop checks match slots the same way now, so a swapped slot can't pass for another
location in the same shop. A respawning pickup is known by its map and regional flag, as before.

**When:**
- after `MapControl.CreateEntities`, before the entities start, so no frame shows the game's own item;
- every 15 frames, for a check done during the visit or a login after the map was built;
- a slot whose check isn't done keeps its own item, so the seed's item on the shelf and the check work as before;
- an undone respawning pickup is left alone, since it gives nothing and shows the seed's item;
- only with Archipelago enabled and a seed whose list isn't empty.

**The guards:**
- A slot whose name can't be read is never swapped, and its check falls back to matching by the item it holds.
- A pickup holding something other than the seed's item for it is left alone, with a warning.
- Every swap is logged once: `[inventories] BugariaCommercial ButterflyShopkeeper Fixedshop0: Crunchy Leaf (0)
  restocks as Honey Drop (1) (on arrival)`.

**What to check in the game:**
- the BepInEx log at start shows `[inventories] installed on MapControl.CreateEntities`;
- at Madame Butterfly's, after a check is bought, that slot soon shows another consumable, with its own name,
  description and price, and buying it gives that item;
- a slot whose check isn't done still shows the seed's item, and buying it still sends the check;
- the Snakemouth pillar Honey Drop, after its check and an area change, shows and gives its new item;
- with the option off, both are the game's own.

**Status:** built (2026-09-30), the build succeeds; not yet seen in game.

*Code: `Items/ShopInventories.cs`, `Items/ItemShops.cs` (`LocationOf`), `Core/SeedData.cs`, `Core/ApConnection.cs`,
`Dev/SeedDump.cs`, installed from `Core/Plugin.cs`.*

## 36. Scripted fights cast from the members you have

**The ask (2026-09-30):** fights written for the whole party must not freeze or crash when the seed's party lacks a
member, as scenes and talks already manage with stand-ins (step 11); the example, the Beast at the end of chapter 5,
where Vi and Leif are knocked out and Kabbu fights on powered up. **The user's choice:** the members you have play
the parts, never a temporary member you don't own yet ("if you do the beast fight with only Vi, or just with Leif,
it's more fun to show it that way").

**Why battles need their own way:** a fight's scripted parts read `playerdata[k]` directly, k meaning member k, since
in vanilla the party is Vi, Kabbu and Leif in that order. An array index can't be answered by a lookup patch, as
scenes' lookups are. With members as items the party can be any of them in any order (a received member joins at the
end), so slot k may hold someone else, or not exist, and a read past the party's end throws inside the fight's
coroutine: the fight stops for good.

**How a part is cast** (`PartySlots.cs`). Each fixed read goes through the mod, which answers with the member the part
belongs to:
- **Member** (the default): member k if he is in the party; else whoever stands in slot k (a scene's own small party
  puts the member playing the part there); else nobody.
- **Speakers** (a line the fight gives a member): member k, else a member the story doesn't have yet (as scenes cast
  one, step 11), else the leader. Never nobody while the party has anyone, so the line is always said.
- **Nobody:** reads find a member with no HP and no body, and writes go nowhere.

Logged once per fight: `[party] EventDialogue 5: member 1's part played by member 0 (slot 0)`.

**The installs** are transpilers on the fights' coroutines. Each finds its reads by their exact instructions,
`ldfld playerdata; ldc.i4.k; ldelema BattleData`, and expects a count. The `ldelema` becomes the enumerator (`ldarg.0`)
and a call returning a reference into the party, or to the nobody record. If the count differs, the method keeps its
own code and the log says so. Only while Archipelago is enabled.

1. **The spider's second fight** (`BattleControl.EventDialogue`, 20 fixed reads). On every second turn, case 5 gives
   Kabbu a line if `playerdata[1].hp > 0`. The member guard makes the story's "Vi and Kabbu" a party of one with a
   one-member start, so slot 1 doesn't exist and the fight stopped if the Web was still up on turn 2 (read in the
   code, not seen; Known issues). Case 5 now uses Speakers: the lone member says Kabbu's line. The other cases are
   the tutorials, and Leif's first line, which never plays in a seed; they use Member, as vanilla does with the story's
   party. **To see:** a one-member start, the second spider fight, the Web left up for two turns: the line, then the
   fight goes on.

2. **The Beast** (enemy 69, the game's `Centipede`, end of chapter 5). The scene (`Event137`) adds a 10-HP floor, and
   at 10 HP the Beast's own turn plays the script (`DoAction`, `case Centipede`, `BattleControl.cs:18358-18472`):
   - it revives slot 1;
   - it hits the slots `{0, 2, 1}` in turn;
   - it knocks out slots 0 and 2;
   - slot 1 says Kabbu's three lines (`commondialogue[166-168]`), is left at 1 HP, then gets Attack Up for good, is
     healed and can't use items.

   The scene reads and restores slot 1's `lockitems` around the fight. **The cast** (Survivor): Kabbu's part goes to
   Kabbu, else the leader; Vi's and Leif's to their own member if he's there and not already cast, else nobody. So with
   Vi alone, Vi is revived, boosted and says the lines, and nobody is knocked out; with Vi and Leif, Vi fights on and
   Leif falls; with all three in any order, each plays himself. The scene uses the same cast, so the fight and the
   scene agree on who survived. What changes in the fight's code:
   - the revive's slot;
   - the one `GetSingleTarget(int)` call: a part nobody plays is no target, and the hit's own `hp > 0` check skips it;
   - `ClearStatus(ref playerdata[hits])`;
   - the two knock-outs, a null-safe `StartDeath`;
   - the survivor's animation stores.
3. **Zommoth** (enemy 96, Upper Snakemouth). The scene (`Event182`) freezes slot 2 for the fight (`EventStop`), and
   the fight wakes it with the beam: `RemoveCondition`, animation 116, then `cantmove` and animation 13. **The cast**:
   Leif's part is Leif, or nobody, and never the party's only member (no one sits out rather than the only fighter
   frozen). Freezing nobody only adds to a list the fight never reads; its four animation stores are null-safe.
4. **The Everlasting King** (enemy 91). At 10 HP it heals, revives the party and plays an exchange: lines 199 and 200 by
   slots 0 and 1, later 201 and 202 by slots 1 and 2 (`BattleControl.cs:20826-20895`). **The cast** (Speakers): a
   missing member's line goes to someone other than the one he answers, so an exchange stays two voices when it can.

**The install** (`ScriptedFights`, a transpiler on `DoAction`'s coroutine) counts everything it changes before it
changes anything:
- 29 fixed `ldelema` reads and 2 by value (`ldelem`), all cast by the acting enemy (read from the enumerator's `entity`
  and `actionid`);
- every other enemy uses Member, which for three members in any order is each member playing himself;
- the Beast's revive, target, status and knock-outs;
- 6 animation stores.

`ScriptedScenes` does the two scenes (3 reads of slot 1 in `Event137`, 1 of slot 2 in `Event182`). Logged once per
fight: `[party] the Beast: member 1's part played by member 0 (slot 0)`.

**To see** (warps only when the user asks; `onehit` is a dev cheat, so ask before turning it on): at the Beast's
map, `flag 359 off` replays the scene. Try all three in arrival order, Vi alone, Vi and Leif, Kabbu and Leif. Bring it
to 10 HP: who is revived, who says the lines, who falls (nobody when alone), the boost, and the scene ending. Zommoth:
Leif frozen until the beam, or with Leif missing nobody frozen. The King: who speaks each line.

**Status:** items 1 to 4 built (2026-09-30), the build succeeds; not yet seen in game.

*Code: `World/PartySlots.cs` (`FightEvents`, `ScriptedFights`, `ScriptedScenes`), installed from `Core/Plugin.cs`.*

## 37. The submarine's docks follow its key item

The game side of the Archipelago guide's build step 36: the submarine, the Subaquatic Maritime Neotransport, is an
item. **The user's ask (2026-09-30):** "you need the item for it to appear/be useable". The docks appear and sail only
with it, wherever the story is, and the story's own scenes keep playing.

**What the game does** (read first, `MEASURED.md`, "The submarine"): no item, only flags. Six docks, one scene
(`Event153`). A dock exists by its entity's `requires` (flag 379 at the Termite pier, 448 elsewhere, none on Mystery
Island). Boarding first reads flag 447, and without it plays the Termite pier's introduction (`Event165`) instead.
Landing reads 448, and without it every dock but the Termite and Bugaria piers refuses.

**How the mod does it:**
1. **The key items** (`CustomItems.cs`), written into the game's item table as the Boat Ticket is:
   - 212, the Subaquatic Maritime Neotransport, the description "It is impossible for it to sink! ...Probably." (the
     king's line, then the team's doubt), and the Big Gear's look (item 159), lent until the user picks one on screen;
   - 213, the Progressive Boat, the ticket's look: never in the bag, only a row so a hold-up, a shelf or a spot on the
     ground can name and draw it, one look for both copies, as the progressive abilities have one each.
2. **Receiving** (`ItemReceiver.cs`): a Progressive Boat copy adds the next of the Boat Ticket (200) and the submarine
   (212) not yet in the bag (`CustomItems.NextBoat`), so a new file's replay gives them in the same order. With the yaml
   option off, the two arrive as their own key items, as any key item does.
3. **The docks exist by the item** (`KeptOpen.cs`, slot_data `present_with_item`): each dock's `requires` becomes a
   marker array. The game's own existence check (`MainManager.CheckIfCanExist`, the one place it decides) answers
   "hidden" without key item 212 in the bag. With it, the check runs on no requirement, so the dock is there whatever
   the story's flags. The Termite pier's scientist and queen (`held_until_item`) keep their own requirement, the throne
   room's flag, and also wait for the item, so their introduction never shows off a dock that isn't there. Logged per
   entity: `[open] BugariaPier: Fixedsub - Duplicate present with key item 212 (in the bag: present)`.
4. **Boarding and landing without the story** (`Submarine.cs`): a transpiler on `Event153`'s coroutine turns its two
   story reads, `flags[447]` and `flags[448]`, into "the flag, or key item 212 in the bag", the same instruction swap
   as the abilities' (step 26). It counts the reads first and logs `[submarine] installed in Event153: 2 of 2 story
   reads answered from the bag`; any other count is an error in the log. No flag is written: the scenes that set them
   still do, as the game does.

Only while Archipelago is enabled, and only in a seed whose `slot_data` says `submarine_item`: an older seed's docks
follow the story.

**Out of the story's order** (read in the code; each to be seen):
- **The item before the story:** the Bugaria pier's dock is there, boarding asks as usual, and every dock lands. The
  first landing at the Rubber Prison plays its arrival scene (`Event185`, which sets no flag). The first landing at
  the Bugaria pier sets 448 and plays Elizant's welcome, as in the story: 100 berries, and flag 350, which opens the
  Termacade (nothing in the logic uses it).
- **The story before the item:** the throne-room scene plays and sends its check (location 76). The Termite pier's
  dock, scientist and queen wait for the item; they appear at the next load of the map once it arrives.

**To see** (the user; warps only when asked):
- the log's install lines (`2 of 2`, the docks' `[open]` lines) and the item's look in the Key Items menu;
- both Progressive Boat copies from `send-as-player.py`, one at a time: the Boat Ticket, then the submarine;
- the Bugaria pier's dock absent before the submarine and there after (`take key 212` to remove it again);
- a crossing to each dock, the Rubber Prison's and the Bugaria pier's first landings included;
- the Termite pier, and the plaza's gate from inside before it was opened from outside (kept away);
- the throne-room scene sending its check.

**The name in the Key Items list (2026-10-04):** "Subaquatic Maritime Neotransport" ran past the list's right edge
(the user's screenshot). The game draws a list's rows with no fitting (`MainManager.ShowItemList`, its row's
`SetText`); its only fitting is `|sizemulti,0.7,1|` on the rows of long-worded languages. Asked, the user chose to
keep the full name everywhere and narrow it in lists only: a prefix on `ShowItemList` swaps the name for one between
`|sizemulti,0.7,1|` and `|sizemulti,1.4286,1|` (the command multiplies the current size, so the selected item's
"name - Worth..." header after it keeps its size), and a finalizer puts it back. Seen fitting the same day.

**The boat stays (the user, 2026-10-04):** with the submarine the boat still runs, both docked side by side; never a
boat flag cleared for the submarine (the Archipelago guide, Next 40).

**Status:** built (2026-09-30); seen (2026-10-04): the item's look and text in Key Items, its name fitted, the Bugaria
pier's dock absent before the submarine and there after (from the next load of the map), a crossing to Metal Island.
Seen 2026-10-04: the Progressive Boat's copies in order (the Boat Ticket, then the submarine), the throne-room scene
sending its check, and the Termite pier: its dock, scientist and queen away before the submarine and there after (the
next load of the map), the introduction (Event165) then boarding (Event153); crossings from the Termite pier by Metal
Lake to the Bugaria pier, whose first landing played Elizant's welcome (flags 448 and 350 read back set), and to the
Rubber Prison, whose first landing played its arrival (Event185, started by the dock scene directly, so not in the
event log; seen on screen); the plaza's gate from inside, first kept away (seen closed), then opened from inside
(`TermiteGate.cs`, the Archipelago guide's build step 36, item 6): seen both ways; the fishing village's and Mystery
Island's docks, nothing special on leaving and coming back. Every dock seen.

*Code: `Items/CustomItems.cs` (`Submarine`, `ProgressiveBoat`, `NextBoat`, the list narrowing),
`Items/ItemReceiver.cs` (`Give`), `World/KeptOpen.cs` (`TieToItem`, `BeforeCheck`), `World/Submarine.cs`,
`Core/SeedData.cs`, installed from `Core/Plugin.cs`.*

## 38. The Warp forced on with Points of No Return

The game side of the Archipelago guide's build step 37. With *Points of No Return* on, the logic may leave the player
where only the pause menu's Warp to Start gets them out, so the Warp must always be there, whatever the Travel setting
says (the user, 2026-09-30: forced "similar to how it is for entrance, spawn etc").

**How the mod does it:** `SeedData` reads `slot_data` `points_of_no_return` (inside `options` since step 41);
`Plugin.cs` hands it to `QualityOfLife` as `PointsOfNoReturn`, only while Archipelago is enabled;
`QualityOfLife.WarpOn` adds it to the cases that force the Warp on (a seed start, shuffled doors, Shuffle Jump, the
abilities as items). A seed with no `options` reads it as off, and the status line says the seed comes from an older
apworld (step 41: no support for older versions). The dev seed dump lists it.

In today's seeds the abilities are always items, which already forces the Warp on; this case keeps it on by its own
reason, so it holds whatever the other options become.

**To see** (the user): with a seed made with it on and Travel set to Off, the Warp is in the pause menu.

**Status:** built (2026-09-30); seen (2026-10-04): with it on and Travel OFF, the Warp is in the pause menu. That
can't single out this option yet: abilities as items force the Warp on in every seed today (`WarpOn`).

*Code: `Core/SeedData.cs` (`PointsOfNoReturn`), `Core/Plugin.cs`, `Gameplay/QualityOfLife.cs` (`WarpOn`),
`Dev/SeedDump.cs`.*

## 39. Spy Specs: the medal's effects as a Quality of life row

**Asked (the user, 2026-09-30):** "similar to detector in qol, spy spec could maybe be a on/off thing (the automatic
spy, it does not take up a turn to spy)". **Decided:** built now, off by default (an opt-in that makes fights easier,
as Attack boost is).

**What the medal does, read in the game's code first** (`MEASURED.md`, Spy Specs): the Spy Specs medal (17) is asked
for in three places, each the party-wide `BadgeIsEquipped(17)`:

- a battle's start sets `scopeequipped`, and every enemy's HP bar then shows, spied or not;
- the Spy action skips its crosshair aim and always succeeds, and then skips `EndPlayerTurn`, so spying is free;
- the battle menu draws a small icon beside Spy, the game's own sign that it is free.

**How:** as the Detector row (step 15): `MedalAssist`'s `BadgeIsEquipped` postfix answers yes for medal 17 on
party-wide checks while the row is on. So all three follow at once, exactly as the medal would, with nothing of the
game's battle code copied or changed. The row, *Spy Specs: ON / OFF* (four values since 2026-10-04, below), sits
under Detector on the Quality of life page (config `[QualityOfLife] SpySpecs`, off by default), and joins that page's
Reset to defaults and Disable all. Its help
line follows the value: "As if Spy Specs were on: enemy HP shows, Spy is free." or "Spy as the game has it: aim, and
it uses the turn." Only while Archipelago is enabled, or with *Use on normal saves* (step 18). No check and no logic
depend on it. The HP bars follow from the next battle, since the game reads the medal as a battle starts.

The page grew to twelve rows (eleven settings under the two buttons). Squeezing them closer was the first plan; the
user asked for the game's own way instead ("not better to just add the up/down scroll that the games normal
"settings" menu have ?"): a settings page now scrolls, seven rows at the game's own size (step 8).

**Split in four (2026-10-04, the user):** "split up the spy specs into off/hp/free(free turn+no aim)/both". The row
now reads *Spy Specs: OFF / HP / FREE / BOTH* (config `[QualityOfLife] SpySpecs` holds `Off`, `HP`, `Free` or
`Both`), and its help line follows the value. HP is every enemy's HP bar; Free is Spy with no aim and the turn kept,
which the game ties to one answer (`Tattle` asks once and both hang on it), so they stay together. The icon beside Spy
goes with Free, the game's sign that spying is free. Both is the medal, as the row's ON was.

**How the halves are told apart:** still the one `BadgeIsEquipped` postfix, which now asks who is asking. Three small
hooks mark it for their own run: `StartBattle`'s step (its `MoveNext`) for HP, `Tattle`'s step for Free, and
`ShowItemList` (the battle menu's icon) for Free; each puts the outer mark back as it ends, so a nested call can't
clear it. An ask from anywhere else gets yes only with Both. If those hooks can't go in, the log says so and only Both
works. The log names each yes: `[medals] Spy Specs: every enemy's HP bar shows this battle` and `[medals] Spy Specs:
Spy with no aim, keeping the turn`. A config that stored the old ON or OFF reads Off now (BepInEx falls back to a
list's first value), so ON has to be picked again as BOTH.

**To see** (the user): in a battle after picking each value, HP shows every enemy's HP and Spy as the game has it;
Free has Spy with no aiming, the icon beside it, and the same member able to act after spying, while unspied enemies
show no HP; Both is all of it; Off is as the game has it.

**Status:** built (2026-09-30), split in four (2026-10-04), the build succeeds; the row seen on its page while the
scrolling was checked (2026-09-30); its four values and their help lines seen (2026-10-04, the old ON read as OFF);
HP's, Free's and Off's battle effects seen (2026-10-04, HP and Free each with its log line); Both waived by the user
("'both' should just work"), not seen.

*Code: `Gameplay/MedalAssist.cs` (`SpySpecsMedal`, the postfix, `SpyAsks`), `Gameplay/QualityOfLife.cs` (`SpySpecs`,
`SpyHp`, `SpyFree`), `Ui/ApMenu.cs` and `Ui/ApMenu.Rows.cs` (`SpyRow`), `Core/Plugin.cs`.*

## 40. No respawn loop: a fall that only leads back into itself ends with the Warp

**Seen (the user, 2026-10-02):**

- Map travel to the swamp, then a jump into the water next to the crystal: the party came back over the water, fell
  in again, and again, forever.
- A shuffled door, probably in chapter 2, did the same with a hazard.
- Both times the pause menu wouldn't open, so the Warp, the safety net for every dead end (the Archipelago guide's
  build step 12), couldn't be reached: a hard softlock.

**How the game puts the party back, read first** (`MEASURED.md`, "Save crystals, saving, Game Over and room
transfers"):

- Water, a hole or spikes run `Hazards.HazardAction`, which puts the party at `player.lastpos`. Falling below the
  map's floor does the same, in `PlayerControl.LateUpdate`.
- A door's transfer sets `lastpos` and `lastloadzone` to where its walk-in ends.
- Walking in a `Respawn` zone moves `lastpos` along.
- The game's own way out of a bad `lastpos`: from the 5th respawn in quick succession it uses `lastloadzone` instead,
  but only while a direction is held.

So when both spots are over water, or nobody holds a direction, nothing ends the loop. A respawn holds the game paused
(`minipause`) while it runs, so the pause menu stays out of reach.

**Decided (2026-10-02, the user):** fix the landings at their source (map travel arriving through a door, step 10),
and make sure nothing can lock the game hard again.

**How the mod does it** (`Guards/RespawnLoop.cs`, with Archipelago on or *Use on normal saves*, step 18):

1. **What counts as a loop.** Every respawn is counted: a prefix on `Hazards.HazardAction`, and on
   `PlayerControl.LateUpdate` the frame the party is below the floor.
   - **Play between two respawns starts the count again:** the party touched ground and was free (no respawn or
     transfer running) for half a second, or walked a quarter unit on the ground from where the respawn left it. A
     loop over water never touches ground; one on spikes is hit again where it lands. (Built first as half a second
     *standing*; replaced 2026-10-04, and the walk added 2026-10-08, both below.)
   - A respawn while a room transfer runs isn't counted: the transfer puts the party at its door, and the guard's own
     warp is one.
   - The game's own counter (`respawntries`) isn't used: it resets itself when its fallback fires.
   - Each respawn logs the decision: how long the party was on ground at most, how long free, play or counted.
2. **Six in a row is a loop**, one past the game's own fallback, so the game's way is tried first. The guard waits for
   that respawn to finish, then runs Warp to Start's own path (`WarpButton.WarpToStart`), without the pause menu.
   - The Warp arrives through a door, so `lastpos` and `lastloadzone` are set where the game itself sets them.
   - A second loop within 15 seconds only logs an error, so a bad landing can never warp back and forth.
3. **A walk-in that never ends.** A transfer waits for the party's walk-in, and a walk toward a spot over water never
   ends: the screen stays dark, the game paused (the console's `unstick` was written for it).
   - One still going after 8 seconds is stopped with the game's own `StopForceMove`, and the transfer then finishes
     as usual.
   - A door's walk-in takes a second or two.
4. **The log names the cause:**
   - from the third respawn in a row, each one;
   - on a loop: the map, `lastpos`, `lastloadzone`, whether a transfer was running;
   - the last transfer: from where, through which door, and whether `door_targets` had rewritten it
     (`DoorShuffle.Rewrote`).

   The next loop the user meets says which door caused it.

**Testing it:** once map travel lands through doors, the swamp no longer loops. So the dev console's `hazardloop`
sets `lastpos` and `lastloadzone` above the middle of the nearest water or hole (`development.md`, the dev console),
and the next fall there loops on purpose.

**To see** (the user):

- after `hazardloop`, a fall into the water ends with the warp to the start, after the 6th respawn;
- falling in a few times with a moment on ground between still respawns as the game does, with no warp;
- in a shuffled seed, the chapter-2 area again: any loop ends in a warp, and the log names the door.

**Normal falls warped, found and fixed (2026-10-04).** The user jumped into the river in Snakemouth Den's bridge room a
few times, and the sixth fall warped them to the start. Twice: the first time the guard logged only "6 in a row
without standing on ground", so its log line gained what it measured, and the user fell in again. The numbers: free
1.03 to 1.57 s between falls, but at most 0.14 to 0.39 s on ground, since the user jumped straight back in. Half a
second of *standing* was the wrong test; touching ground at all, with half a second free, is the right one: the same
user's falls then logged "play" twelve times in a row (on ground 0.14 to 0.34 s, free 1.05 to 1.23 s).

**The loop tests, made faithful (2026-10-04):**

- `hazardloop` first put the spot over the middle of the water's box, and walking to the water undid it: standing
  15 frames in a `Respawn` zone moves `lastpos` to where you stand (`MEASURED.md`, save crystals and room transfers),
  and a box's middle can be over land. It now picks the nearest point inside the water with no ground above it (the
  game's own ground layers, 8 and 13), and holds it until the fall's respawn starts.
- At 1 above the water the party touched it while the respawn still ran, and the game repeated the respawn at once;
  the guard caught it and warped, but kept counting the respawns during its own warp's transfer (an error line, "a
  loop again 1 s after the last warp", until the walk-in ended). Those are no longer counted, and the spot is 3 above.
- `oldtravel <map> <entity>` replays map travel's landing from before 2026-10-02 (beside a save point, through the
  2-argument `TransferMap`). At the swamp's crystal (`oldtravel SwamplandsBridge 6`) it gives the original loop: the
  user jumped in, came back over the water at once (free 0.00 s) five times more, and was warped to the start; the
  respawns during the warp's transfer weren't counted.

**When the start is the loop (2026-10-04).** A seed's random start landed behind the desert border's shut gate (the
Archipelago guide, build step 15): the guard warped the party to the start, which looped again, and the guard then only
logged "a loop again ... not warping again" while the party kept falling. Now a loop back within 15 s of the guard's
warp warps once more, to the **game's own start** outside the city (`WarpToStart(..., gameStart: true)`, which passes
over the seed's start); only a loop after that is left to the log. The start itself is fixed at its source too (no
start lands at a door the game hasn't made yet).

**Walking back into thorns warped, found and fixed (2026-10-08).** While mapping `FarGrasslands4`, the user walked
into its thorns over and over, and the sixth time warped them to the start: "nothing the guard should catch as its not
a softlock". The log: each time on ground 0.09 to 0.15 s and free 0.10 to 0.16 s, so under the half second. Free time
can't tell the two apart here, since the thorns sat right beside where each respawn put the party. How the game
respawns tells them apart (`Hazards.HazardAction`, read first): it holds `minipause` until the fade back in ends, and
a party put back onto the hazard is hit again during that fade, where it lands, before it can walk. The user's party
walked about half a unit each time (the walk is 5 a second). So walking a quarter unit on the ground from where the
respawn left it now counts as play too, and the log line says how far the party walked.

**Status:** built (2026-10-02); seen (2026-10-04): the guard ends the swamp's original loop and a `hazardloop` loop with
the warp after the 6th respawn, and normal falls never warp (after the fix above). The thorns fix built 2026-10-08, not
yet seen. Still to see: walking into thorns again and again never warps; a loop met in a shuffled seed, naming its door;
the fallback to the game's start (built 2026-10-04).

*Code: `Guards/RespawnLoop.cs`, `Ui/WarpButton.cs` (`WarpToStart`), `World/DoorShuffle.cs` (`Rewrote`),
`Core/Plugin.cs`, `Dev/DevConsole.Warp.cs` (`hazardloop`, `oldtravel`).*

## 41. The seed's options read from slot_data's `options`

The game side of the Archipelago guide's build step 39. The apworld now sends the options as the seed applied them in
one dict, `options`, which Universal Tracker also rebuilds the seed from, and no longer writes the four top-level copies
the mod read.

**How the mod does it:** `SeedData` takes `options` (a JSON object, as every nested slot_data value arrives) and reads
four values from it with two small helpers in `SlotData.cs`: `On` for a JSON boolean, `Number` for a JSON integer.
`shuffle_field_moves` and `shuffle_jump` (the field moves and Jump as items, the Archipelago guide's build steps 21
and 22), `points_of_no_return` (the Warp forced on, step 38) and `artifacts_required` (the goal). Everything
that used them reads them through `SeedData` as before, so nothing else changed.

**No support for older versions** (the user, 2026-10-03: "people are expected to use the latest release for all of
them"): the old keys are never read. A seed with no `options` still connects, and the main menu's status line says
"this seed comes from an older apworld: use the latest release of everything and generate a new seed"; the log adds
that its goal and option rules won't apply.

**Proof the move changed nothing:** the dev seed dump keeps its labels (`shuffle_moves`, `shuffle_jump`,
`points_of_no_return`, `artifacts_required`), so a dump of a new seed reads exactly as one of an old seed with the same
options did.

**To see** (the user), on a fresh seed with Shuffle Field Moves, Shuffle Jump and Points of No Return on, Travel set
to Off:

- each attack refused with the short "can't" sound until its item arrives, and the jump refused until Jump arrives;
- the Warp in the pause menu;
- the goal sent once the artifact is in hand (`[goal]` in the log).

**Status:** built (2026-10-03); seen (2026-10-04): the attacks and the jump refused until each item arrived, read
from `options`; the Warp with Travel Off (Points of No Return's seed). The goal from `options` not yet seen.

*Code: `Core/SeedData.cs` (`OptionsMissing`, the four values), `Core/SlotData.cs` (`On`, `Number`),
`Core/ApConnection.cs` (the status line), `World/FieldMoves.cs`, `Items/LocationChecks.cs`.*

## 42. The ant tunnels' miners dig for free

The game side of the Archipelago guide's build step 45. Every far end's miner runs one scene, `Event48`, which stores
its area's price in `flagvar[0]`, then shows it, checks the berries and charges from there. With `free_ant_tunnels` in
the seed and Archipelago on, a transpiler sends each of the six stored prices through `AntTunnels.Price`, which answers
0, so the dig costs nothing and still opens the tunnel the game's way (its flag, its scene). The log says which way each
price went (`[tunnels] the miner's price N: free in this seed`), and the install line counts the prices found (6 of 6).
Off Archipelago the prices are the game's.

**Status:** works, seen on screen (2026-10-04): the Golden Way's miner asked "0 berries for this tunnel!" with 0
berries in the bag.

*Code: `World/AntTunnels.cs`, `Core/SeedData.cs` (`FreeAntTunnels`).*

## 43. A free seller: the price in their lines made 0

The game side of the Archipelago guide's build step 46. Beette's price isn't a variable like the miners': her lines
write it out, the offer as text ("150 berries for the house") and the sale as commands (`|checkmoney,150,22|`,
`|money,-150|`). With `free_sales` in the seed and Archipelago on, the listed lines are rewritten as their map's lines
load, just before `MapControl.CreateEntities`: every `checkmoney` and `money` price to 0, and each of those prices
followed by "berr" in the same lines to 0, so what she says matches what she charges. The log names the lines and the
price (`[sales] BeehiveBalcony: lines 20, 21 free (price 150 to 0)`).

**The hook named no overload** (found 2026-10-04 in the log: "NOT installed (EnemyDrops): Ambiguous match found"): the
game has `Death()`, which only starts `Death(true)`, and `Death(bool)`, which does the work and whose state the drop
reads; both patches now name `Death(bool)`. After the hot reload: "Enemysanity installed".

**Status:** built (2026-10-04), installing since the overload fix (the log, 2026-10-04); not yet seen in game.

*Code: `World/FreeSales.cs`, `Core/SeedData.cs` (`FreeSales`).*

## 44. Enemysanity: an enemy's won fight drops its check

The game side of the Archipelago guide's build step 47. The game already drops a held key from two map enemies, a
guaranteed pickup made by the public `EntityControl.CreateItem` (`MEASURED.md`, a map enemy's drops). For each enemy in
`location_enemies` (keyed `map:entity`, the entity being `NPCControl.mapid`, as Enemy Shuffle keys it):

1. **Kept present:** as the map builds its entities, a listed enemy with a `requires` or `limit` gets the kept-present
   marker (`KeptOpen.KeepPresent`), so no story flag removes it.
2. **The drop:** `EntityControl.Death`'s coroutine is watched (its first step notes the enemy and where it stood, its
   last makes the drop), and while the location isn't done on the server a key-item pickup with no timer is made there
   the game's way. No save flag: only 37 story flags are free (`MEASURED.md`).
3. **The pickup:** `ItemSwap` finds the drop as a location (`EnemyDrops.LocationOf`), shows the seed's item on the
   ground and in its line, and sends the check at once, as a respawning pickup's is (`QueueRespawnCheck`); the game's
   own item is never given.

Not yet: the held item drawn on the enemy before the fight (the game draws it for two kinds only).

**The hook named no overload** (found 2026-10-04 in the log: "NOT installed (EnemyDrops): Ambiguous match found"): the
game has `Death()`, which only starts `Death(true)`, and `Death(bool)`, which does the work and whose state the drop
reads; both patches now name `Death(bool)`. After the hot reload: "Enemysanity installed".

**Status:** built (2026-10-04), installing since the overload fix (the log, 2026-10-04); not yet seen in game.

*Code: `World/EnemyDrops.cs`, `World/KeptOpen.cs` (`KeepPresent`), `Items/ItemSwap.Pickups.cs`, `Core/SeedData.cs`
(`LocationEnemies`).*

## 45. The Termacade: tokens, the gift and the prize stand

The game side of the Archipelago guide's build step 50.

1. **Tokens from the server:** the receiver adds a token item's amount to `flagvar[27]`, where the game keeps them
   (it caps them at 9999 itself); the hold-up shows the Game Tokens sprite.
2. **The gift:** a give in `location_gives` may name its `npc`; `ItemSwap` then swaps that `giveitem` only when the
   description box it opens belongs to that character. The greeter's does; the arcade's score rewards open none inside
   the arcade, so they stay the game's own.
3. **A prize bought:** Event121 runs `giveitem` only after taking the tokens, so the give swap itself is the purchase.
   A prize with a flag is sent when the flag is set; one sold again and again has none, so its check is queued at the
   swap (`QueueRespawnCheck`), and once done the stand sells its own prize again (`ItemSwap.SellsItsOwnAgain`).
4. **The list:** `Termacade.cs` puts the seed's name, description and sprite into the game's own item and medal tables
   for each undone prize while `ShowItemList` builds list 26, and puts the game's back after it (a finalizer). Every
   look is read before any is swapped.

Not changed: with a full bag, Event121 refuses a prize in an item slot whatever the seed put there (the game's own
check); the "buy it?" question names the stand's own prize.

**Status:** built (2026-10-06); the list, a flag-less and a once-only purchase seen working (2026-10-06).

*Code: `Items/Termacade.cs`, `Items/ItemSwap.cs` (`FindLocation`, `IsFlaglessPrize`), `Items/ItemReceiver.cs`,
`Items/ItemSwap.Looks.cs`, `Core/ItemIds.cs` (`TokenKind`), `Core/GameSlots.cs` (`GameVars.Tokens`),
`Core/SeedData.cs` (`LocationPrizes`, `Give.Npc`), `Dev/QuestDump.cs` (the prize dump).*

## 46. The Platinum Card carries the bank's doubled interest

The game side of the Archipelago guide's Banker location (`apimplementation.md`, the residential district's recheck).
The card's one effect in the game is flag 630, which doubles the bank's interest (`MainManager.DoClock`: 4% instead of
2%), and the banker sets that flag as he hands the card over (`MEASURED.md`, the Bank of Bugaria). In a seed the flag
is his check's, and the card is an item that can arrive from anywhere, so the effect would stay with whoever talked to
him. **Items are remote only**, so with Archipelago on, `DoClock`'s read of the flag becomes "is the Platinum Card
(key item 176) in the bag", the way the learned abilities' reads are answered (step 26's `Abilities.cs` pattern: the
read `ldfld flags; ldc.i4 630; ldelem.u1` becomes a call taking the same array and index). The banker's own reads of
the flag (his lines 70 and 82) stay the game's, so his check still happens once. With Archipelago off, nothing changes.
The log says `[card] installed in MainManager.DoClock`, or an error when the read isn't found exactly once.

**Status:** built (2026-10-08); not yet seen in game.

*Code: `Items/PlatinumCard.cs`, `Core/Plugin.cs`.*

## 47. A chapter's main quest filed without freezing its scene

Found mapping the Ancient Castle's treasure room (2026-10-09): taking the artifact froze the party. Each chapter's
scene files that chapter's main quest (11 to 17) at a fixed place in the quest lists, `boardquests[2].Insert(3, 14)`
in the castle's, and `List.Insert` throws when the list is shorter than that place. In story order it never is, since
the chapters before have filed theirs; in a seed, any chapter's scene can come first. Nine such inserts in seven scenes
(`Event45`, 73, 99, 118, 142, 194, 203; `MEASURED.md`, the treasure room), and a throw leaves the party frozen with
the rest of its scene unrun: the castle's after its artifact flag, `Event142` before its own (347), so that chapter's
artifact would never be set. The last chapter's (`Event203`) already wraps its insert and appends the quest when it
throws, the same fallback as here; patching it too changes nothing.

With Archipelago on, a transpiler on each of those scenes' coroutines swaps every `List<int>.Insert` for a call that
takes the same list, place and quest and, when the place is past the end, files the quest at the end instead. The
list is the game's own field, written by the game's own `Insert`; only the order of the done list can differ from a
story-order save, and the game reads those lists only by what they contain, so a quest filed at the end just shows
lower on the quests page (a review of the game's reads, 2026-10-09). With Archipelago off, the call inserts exactly as
the game does. The log names each insert it
guards (`[chapters] installed: quest 14 at 3`) and each quest it moves (`[chapters] quest 14 filed at 1, not 3`).

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633`: the castle's artifact scene
ran to its save menu with no freeze, the log saying quest 14 filed at 1, not 3 (the done list held 1).

*Code: `World/ChapterQuests.cs`, `Core/Plugin.cs`.*

## 48. The pause menu's artifacts: each one's own icon

The pause menu counts the artifact flags (`SaveProgressIcons`) and draws that many icons from the first chapter's on
(`PauseMenu.BuildWindow`), and the quests page blacks out a chapter's artifact until the count reaches that chapter
(`UpdateText`). Right only in story order: with only the castle's artifact (flag 345), the menu showed the first
chapter's icon (seen 2026-10-09, `MEASURED.md`). Asked and decided (the user, 2026-10-09): in a seed, the pause menu
shows each artifact you have; the file select keeps the game's count, since a save stores only the number.

With Archipelago on, `BuildWindow`'s read of `StartMenu.psprite` returns the same seven icons with the set flags'
first, so the game's loop draws the icons of the artifacts held; and the quests page's count becomes the chapter's own
flag (all seven if set, none if not). With Archipelago off, both read the game's own values. Next 28's *Artifact
Shuffle* will draw the Artifacts received instead (`apimplementation.md`). The log says `[artifacts] installed in
PauseMenu.BuildWindow` and `in PauseMenu.UpdateText`, or what it didn't find. The file select's icons: Next 65.

**Status:** built (2026-10-09); the row seen the same day with only flag 345 set: the castle's own icon. The quests
page not yet seen.

*Code: `Ui/ArtifactIcons.cs`, `Gameplay/EnemyScaling.cs` (its `ArtifactFlags`), `Core/Plugin.cs`.*

## 49. Doors a seed adds or sends elsewhere

Found mapping the Bee Kingdom's Scanner Room (2026-10-09): the room has one door, and its way on is a scene that warps
the party into the hive. The user wanted it kept as a room between the outside and the inside: "copy the bottom
entrance/door how it works, place it where the gate is, and redirect how you come in/out of it". A door in this game is
an entity row: its target map (`data[0]`), where the party walks into it (`vectordata[0]`), and where it appears and
walks to on the other side (`[1]`, `[2]`).

With Archipelago on, the seed's `door_rows` are written into a map's entity rows before `MapControl.CreateEntities`
parses them: a transpiler after its two `Split` calls (the rows, then the names), as the day/night switches' copies are
added (`DayNight`). A copy (`copy`: a map and entity) is that door's row with the seed's spot (`at`), its requires and
limit dropped (always there), and its data and vectors (the walk in, the appear and walk-to spots, then the camera's
four, which the game reads when the data has more than one entry); it is appended, and its name added to the names in
the same order. A re-point rewrites the map's own door's row in place, since scenes find entities by their row. The game
then builds and runs both like its own doors. A row the game can't run is refused: when a door's data has more than one
entry, its transfer reads data 1 to 3 and vectordata 3 to 6, so such a row needs at least 4 data and 7 vectors (a
review's find, 2026-10-09). `DoorInto` (the dev warp, the Warp button, a seed's start) asks the seed's rows first and
skips a game row the seed sends elsewhere. With Archipelago off, the rows are the game's. The log says `[doorrows]
installed`, and for each map what it copied or re-pointed, or why not.

Found while testing: the copied top door's walk in, mirrored from the bottom door's, pointed past the corridor's shut
end; the game's transfer waits for that walk to end, so the screen stayed black 3.8 s more (the dev load timer). It now
ends on the door's own spot: 1.8 s from the door to control, as any door.

Not yet: the entrance randomizer reading these rows (`DoorShuffle` and the door data), for the door pass
(`apimplementation.md`, Next 2).

**Status:** built (2026-10-09); seen the same day in seed `AP_70580691250444408633` through the dev `liveslot`: the
Scanner Room's top door into the main area and the main area's bottom exit back into the room's top, inside the gate,
walked both ways several times.

*Code: `World/DoorRows.cs`, `Core/SeedData.cs`, `Gameplay/QualityOfLife.Opening.cs` (`DoorInto`), `Core/Plugin.cs`.*

## 50. The Bee Kingdom's scan sets flag 160 too

`Event84`'s first part, the scan, writes flag 159 once; its second part, which warps the party to the main area and HB's
Lab, wrote 160. In a seed the second part is kept away (`apimplementation.md`, build step 73), and the user decided 160
comes with the scan ("lets just give 160 alongside the scan in the scanner room, so its not missable"). With Archipelago
on, a transpiler after the 159 write calls a method that sets every flag the seed's `flags_with` names for event 84 and
flag 159: 160. That teaches Leif Bubble Shield Lite at the game's next skill refresh (the game's own, as Pep Talk is at
the farm), takes HB from beside his lab door, and switches Outside the Beehive's main door, which the kept lists hold on
the Scanner Room. The log says `[scan] installed in Event84`, and at each scan what it set, or why nothing.

With **Skip cutscenes** the scan is fast-forwarded (the game's own fast-forward, in the setting's scene list): it
destroys its scanner and its trigger, so a skip by flags alone would leave them, and it ends exactly as it would.

**Status:** built (2026-10-09); seen the same day: flags 159 and 160 set by the scan, the check for location 208 sent,
the scan from either end of the room, and sped up ("passing it by at speed").

*Code: `World/HiveScan.cs`, `Core/ApConnection.cs` (`FlagWith`), `Core/SeedData.cs`, `Gameplay/QualityOfLife.cs` (the
scene list), `Core/Plugin.cs`.*
