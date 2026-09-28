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

1. [Check whether the game can be modded at all](#1-check-whether-the-game-can-be-modded-at-all)
2. [Pick the design before the code](#2-pick-the-design-before-the-code)
3. [Read the game's code](#3-read-the-games-code)
4. [Get a mod loader running](#4-get-a-mod-loader-running)
5. [Make changes load without restarting the game](#5-make-changes-load-without-restarting-the-game)
6. [Watch the game while you play ("probing")](#6-watch-the-game-while-you-play-probing)
7. [List everything, without playing everything](#7-list-everything-without-playing-everything)
8. [An Archipelago menu inside the game](#8-an-archipelago-menu-inside-the-game)
9. [Keep the game's own item, show the seed's](#9-keep-the-games-own-item-show-the-seeds)
10. [Quality of life: a quicker, smoother game](#10-quality-of-life-a-quicker-smoother-game)
11. [Playing with fewer party members: stand-ins and followers](#11-playing-with-fewer-party-members-stand-ins-and-followers)
12. [Shops in the game](#12-shops-in-the-game)
13. [Doors rewritten: the entrance randomizer in the game](#13-doors-rewritten-the-entrance-randomizer-in-the-game)
14. [The Detector for every check](#14-the-detector-for-every-check)
15. [Difficulty and the Detector: the panel's two game settings](#15-difficulty-and-the-detector-the-panels-two-game-settings)
16. [Randomizer saves kept apart from normal saves](#16-randomizer-saves-kept-apart-from-normal-saves)
17. [Enemy scaling: every area fair whenever you reach it](#17-enemy-scaling-every-area-fair-whenever-you-reach-it)
18. [Use on normal saves: the settings without Archipelago](#18-use-on-normal-saves-the-settings-without-archipelago)
19. [EXP and berry multipliers: bars like the volume rows](#19-exp-and-berry-multipliers-bars-like-the-volume-rows)
20. [Item colors: Archipelago's colours in the "You got" box](#20-item-colors-archipelagos-colours-in-the-you-got-box)
21. [Archipelago icon: other players' items on the ground and on shelves](#21-archipelago-icon-other-players-items-on-the-ground-and-on-shelves)
22. [Item backgrounds: how much an item matters, before you take it](#22-item-backgrounds-how-much-an-item-matters-before-you-take-it)
23. [The Archipelago icon, drawn in the game's style](#23-the-archipelago-icon-drawn-in-the-games-style)
24. [Frame rates above 60: smoother, and the same game](#24-frame-rates-above-60-smoother-and-the-same-game)
25. [Hitches: the mod's garbage and the game's 5-second collection](#25-hitches-the-mods-garbage-and-the-games-5-second-collection)
26. [Field abilities as items: the game asks the bag](#26-field-abilities-as-items-the-game-asks-the-bag)
27. [Attack boost: +1 on every hit, the way the game adds its own](#27-attack-boost-1-on-every-hit-the-way-the-game-adds-its-own)
28. [A Graphics page, tried and removed: render scale and MSAA](#28-a-graphics-page-tried-and-removed-render-scale-and-msaa)
29. [Save crystals by the confirm button, as an NPC is talked to](#29-save-crystals-by-the-confirm-button-as-an-npc-is-talked-to)
30. [Healing crystals: every save crystal yellow](#30-healing-crystals-every-save-crystal-yellow)
31. [Auto-save between rooms: a death costs one room](#31-auto-save-between-rooms-a-death-costs-one-room)

## Where it stands

Each step's status is its last line (**Status:**), before its *Code:* line. What's next and the known issues
are in [apimplementation.md, "Where it stands"](apimplementation.md#where-it-stands).

## Keeping this guide honest

A step-by-step guide is only useful if no step is missing, so the project enforces it: any commit that
changes the mod, the apworld or the dev scripts is refused unless it also updates this file or
[apimplementation.md](apimplementation.md) (or says, explicitly, that nothing about the process changed).
That check is a small git hook, `.githooks/commit-msg`, which also keeps each subject to 72 characters with no
attribution (its neighbour `.githooks/pre-commit` refuses personal paths and names). Each step below ends with a short *Code:* line naming the files and methods to
read, just after its **Status:** line. Each new step also gets a line in the index above.

---

## 1. Check whether the game can be modded at all

Before writing anything, we looked at what the game is made of. Bug Fables is a **Unity game built with
Mono**, which means its code ships as a normal .NET file (`Assembly-CSharp.dll`) that can be turned back
into readable code. That's the easiest case there is. We also checked that nobody had already made a
Bug Fables randomizer.

*How to tell for your own game:* an `Assembly-CSharp.dll` in the game's `_Data/Managed` folder means
Unity with Mono. A `GameAssembly.dll` means Unity with IL2CPP, which is harder.

**Status:** done.

## 2. Pick the design before the code

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
    `client-requirements.md`).
  - **A small Archipelago chat feed in the bottom-left corner.** It never stops play. It shows the items you
    send and receive, in Archipelago's own wording ("Player1 found their Hammer (Location)", "Player1 sent
    Hammer to Player2 (Location)"), and players connecting and disconnecting. It's **on by default**, with an
    on/off switch in the Archipelago panel. Items arriving from the server show up only there, never as a
    popup. **Later, it becomes a real text client** (2026-09-24): a key opens a text line over
    the feed, so server commands like `!hint` work in game. The game's controls pause while typing, and the
    server's replies show in the feed.
  - **Item names are coloured the way Archipelago's clients colour them** (`NetUtils.py`): progression plum
    `#AF99EF`, useful slateblue `#6D8BE8`, trap salmon `#FA8072`, filler cyan `#00EEEE`.
- **Read what others already solved.** We read the TEVI randomizer (another Unity mod), Pokémon Emerald's
  apworld, Archipelago's own docs, and notes from an earlier Archipelago project, all for ideas only,
  with each one's licence checked first.

**Status:** done (decided 2026-09-24); the chat feed and the text client it describes aren't built yet ([apimplementation.md](apimplementation.md#where-it-stands), Next).

## 3. Read the game's code

We used **ILSpy** to turn the game's `Assembly-CSharp.dll` back into C# source, kept on our own machine
and never shared. Reading it answered the first big question: *how does this game hand out items?*
The answer was a small set of places every item goes through, which is exactly where a randomizer
needs to hook in.

**Status:** done.

## 4. Get a mod loader running

Unity games don't load mods by themselves, so we installed **BepInEx 5**, the usual mod loader for
Unity games. One launch of the game confirmed it worked, and showed its log file.

**Where the code lives** (2026-09-27, a refactor that changed nothing the plugin does): one project, its sources in
folders by job under `mod/BugFablesAP/`: `Core` (the plugin, the connection), `Items` (checks sent, items received,
what a location shows), `World` (doors, enemies, the open world, the party), `Ui`, `Gameplay` (panel settings that
change play), `Guards` (quiet fixes for the game's own warnings) and `Dev` (the console, probes and dumps). The
namespace stays `BugFablesAP` everywhere, so a move never touches code. **How "changed nothing" is proven:** build
before and after, decompile both DLLs with ILSpy, and diff the output; a pure move comes out identical.

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

**Status:** done; separate guards per system built 2026-09-28, not yet seen in game; errors checked for instead of
swallowed, built 2026-09-28 (both builds pass), not yet seen in game.

*Code: `mod/BugFablesAP/Core/Plugin.cs` (`Plugin`, a BepInEx plugin: `Awake` sets everything up, `Tick` runs
every frame); the project file is `BugFablesAP.csproj`.*

## 5. Make changes load without restarting the game

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

**Status:** done: hot reload, the build-and-copy scripts and the dev console are in use.

*Code: `DevConsole.cs`.*

## 6. Watch the game while you play ("probing")

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

## 7. List everything, without playing everything

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
can't fall, event on death, and from 2026-09-27 weaknesses, so Kabbu's flip can be read), since that table is game data the code doesn't hold. Run at the title screen
(2026-09-26): 117 enemies.


**Scenery switched by flags** (2026-09-25): a door can be two things, a load-zone entity and a model in the map's
scenery that opens by a flag of its own. The scenery isn't an entity, so the entity dump can't see it. The map dump
now also lists every `ConditionChecker` (hidden or moved by flags) and `FlagAnimation` (animated by flags) in each
map prefab, into `bugfablesap-mapflags.tsv`, read from the prefab without instantiating it.

**Making an entity exist early** (2026-09-25): entities whose flags aren't met still exist in the map, switched
off. Doors are switched on and off every other frame (`MapControl`, by distance, while `CheckIfCanExist` says
"exists"); other objects only once, in their own `Start` (`NPCControl.cs:438`). So `KeptOpen` marks the entity
straight after `CreateEntities`, before `Start`, with a `requires` array of its own that its prefix on
`CheckIfCanExist` answers with "exists", the mirror of how a kept-open blocker gets a `limit` answered "hide".
Built, not yet seen in game.

**Item and medal sprites too** (2026-09-26): `SpriteDump` also writes every item and medal sprite with its id and name
(`bugfablesap-itemsprites.tsv`) and their sheets, so a labelled contact sheet can be made from them, to pick an icon
(the Boat Ticket's) by pointing at it. Game art stays local, never in the repo.

**Status:** done: the script, entity and map dumps are in use; making an entity exist early built, not yet seen in game.

## 8. An Archipelago menu inside the game

Players need to type a room address, a slot name and maybe a password, so the mod adds **"Archipelago"** to the
game's main menu. It opens a panel drawn with the game's own box and font, so it looks like part of the game,
but it takes real typing: the game itself never reads typed text (its name screen is a letter grid), so the
mod reads the keyboard itself. Backspace, Ctrl+V to paste and Ctrl+C to copy all work. The same panel switches
**the Archipelago mod** (enabled or disabled), which keeps randomizer saves in their own folder so normal saves are never touched.
Its rows, top to bottom (order chosen 2026-09-24): Address, Port, Slot, Password, Difficulty, Detector,
**Archipelago** (the mod on/off, just "Archipelago"; the config's `RandomizerEnabled`). No Back row: cancel backs out, as the hint box says.
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
  the cancel button, the sound the game uses leaving the file select. Choosing the panel's "Back" line plays
  "Confirm", like any other menu choice.

**Lesson:** when adding to a game's own screen, find every time the game rebuilds that screen, and everything
else that keeps running while another screen is on top of it.

**The file select waits for the first login** (2026-09-24, chosen over keeping a copy of the
seed on disk, which would spoil every location to anyone opening the file). The mod learns the seed only when it
logs in, so a save played before that would hand out vanilla items. How it was built:

1. Find where the file select acts on a file: `StartMenu.Update`, when the file-select screen is up
   (`menuid` 2, `submenu` 0), the cursor is on one of the three files and confirm is pressed. That branch loads
   the save (Event22) or starts a new game (Event8).
2. A prefix on `StartMenu.Update` checks the same conditions first. With the Archipelago mod enabled and no login
   yet this run, it plays the game's buzzer (`PlayBuzzer`), opens a popup and skips the game's `Update`, so the
   game never sees the press. The popup is a dimmer over the whole screen and the game's orange box in the
   middle, sorted above the save slots (their boxes sort at -20 to -60, their text at 10): "Not connected to
   Archipelago", what to do, the connection's live state and OK / Close hints (confirm and cancel; a button's label carries its own sort, or it draws behind the box). It sits 0.9 units above the middle, over the save slots. The file select stays frozen under it
   until confirm or cancel closes it. A first try, one line at the top of the screen for four seconds, ran over
   the save slots and was hard to read (seen in a screenshot, 2026-09-24).
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
left/right steps through them; every step redraws the screen. On/off rows keep one line.

**Three pages (2026-09-26; built, not yet seen):** the main page keeps the connection and the Archipelago
on/off, plus two links, *Quality of life* and *Gameplay*. Gameplay holds how the game plays: Difficulty, Enemy scaling
(moved from Quality of life; its config key stays under `[QualityOfLife]`, so a saved choice carries over) and
Detector. Quality of life keeps the speed-ups, with Disable all / Reset to defaults on top (step 10). Cancel backs out
of a Yes / No first, then out of a page. `ApMenu` tracks the page as an enum. **Rows moved (2026-09-26):**
Shop prices (now Medal prices) to Gameplay, Detector to Quality of life, so Gameplay is Difficulty, Enemy scaling, Medal prices and Quality
of life is Fast text, Travel, Skip cutscenes, Item animation, Detector; each config key stays where it was, so a saved
choice carries over, and each page's two buttons cover its own rows. **The two links left the main page
(2026-09-26: "so AP looks clean"):** the pages are reached only from Settings (below), and *Use on normal saves*
(step 18) took their place under Achievements. A third page, Graphics, came and went on 2026-09-28 (step 28).

**The two pages in game too (2026-09-26; seen on screen, in game and on the main menu).** While Archipelago is enabled, the pause
menu's Settings list gets *Quality of life* and *Gameplay* at the top (with it disabled, only under *Use on normal saves*, step 18), above Music Volume (first between
Key Bindings and Return to Main Menu), opening the same pages. Neither touches a check or the logic, so changing them mid-save is safe (Hardest is already kept out of the
save; a boss prize reads Hard Mode as the boss falls, and missed prizes are paid anyway). The connection page stays on
the main menu. How (`InGameSettings.cs`): the Settings list is `MainManager.GetSettings()`, a list of ids; an id's
label is `menutext[settingsindex[id]]`. A postfix adds ids 26 and 27 at the top of the list, after two
labels appended to `menutext` and two entries to `settingsindex` (re-added if the game reloads its text). The game
draws left/right arrows on every row but a named few, so a postfix on `ShowItemList` (type 17) removes the new rows'
(`Bar<index>` rows, `slider0/1` children). A prefix on `PauseMenu.Update` catches confirm on them and opens the page
(`ApMenu.ShowInGame`). **Only the Settings screen's two boxes are hidden** (`PauseMenu.boxes`; its list lives inside
them), and the pause menu's `Update` is skipped while the page is open. Switching the whole pause menu off at first
also took its darkened background away: the game view flashed before the page appeared (seen on screen). Now the
background stays, as going from the pause menu to Settings does, and the page skips its own dimmer in game. Cancel
shows the boxes again, on Settings. In the main menu's Settings too
(one Settings screen, not two); since 2026-09-26 that is the only way to them from the main menu.

**Letters going missing (2026-09-26):** the Reset to defaults Yes / No box drew whole, then lost letters
("Ye", no "No") after left / right. The game draws text from a pool of 500 letters, and its own `DestroyText` frees only
every other one until the frame ends (`MEASURED.md`, the text letter pool); behind a page opened from Settings the
Settings list holds many, so a redraw ran the pool dry. Disable all's shorter question just fitted. `TextPool.Free`
frees every letter and replaces `DestroyText` everywhere in the mod, and left / right in the box redraws only the box.

**Achievements (2026-09-26; built, not yet seen):** an *Achievements* row on the main page, off by default.
While Archipelago is enabled and it's off, Steam achievements aren't unlocked, as normal saves are kept apart; the help
line says it only concerns Steam, never Archipelago. Every achievement goes through one function,
`InputIO.Achivement(id)` (the game's spelling), which asks Steam and sets it; `AchievementGuard.cs` skips it and logs
each id held back once. With Archipelago disabled the game unlocks as usual (vanilla stays vanilla).

**Text gone inside shops (2026-09-26):** in the shop building the Quality of life and Gameplay pages showed
their arrows and no text, while the game's Settings list was fine. Wrong theories first: the letter pool running dry
(132 of 500 taken), text cleared every frame, depth against the camera. The fix came from putting one of the panel's
letters beside one of the game's in the running game (dev `menuinfo`): **the GUI camera is turned 90° in the shop**, the
game's letters turned with it, and the panel's were not. Its text object was attached with `.parent =` and never had
its rotation reset, so it kept an unturned world rotation and was seen edge-on. Every attached box and text now resets
`localEulerAngles`, as the arrows already did; the Warp button's Yes / No box too. **Seen on screen (2026-09-26):**
the Quality of life page with all its text inside the shop. It follows whatever turn the camera has, so any room that
turns it is covered. The lesson went into CLAUDE.md: read how the game does a thing first.

**Status:** works, seen on screen (2026-09-24): the menu entry, the panel, and the file select held back until the first login.

*Code: `MenuToggle.cs` (the menu entry: `BeforeSetMenuText` and `AfterSetMenuText` around the game's rebuild,
`AfterUpdate` for the cursor, `SetMode` for the switch); `ApMenu.cs` (the panel: `Build`, `Redraw`,
`Navigate`, `TypeInto` for typing, `Close`).*

## 9. Keep the game's own item, show the seed's

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
changes, so the swap holds. Another game's item keeps the vanilla look until the Archipelago icon is in the mod.
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
points to `warp <map> @<entity>`. Built, not yet seen in game.

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
Player's Sword!" in the same colours for one found here. The player's own finds keep the game's red. Dev `colortry <hex...>` shows a trap line per colour.

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

**Status:** works for gifts, pickups and their ground sprites, and respawning pickups seen on screen (2026-09-24, `MEASURED.md`), and crystal berry spots (2026-09-25); berry rewards and story pickups built, not yet seen in game.

*Code: `ItemSwap.cs` (`Enable` finds the routine, `Transpile` rewrites it; `Decide`, `DescWindow`,
`Recolour` and `FirstMedalSeen` do the swapping; `PickupPrefix`, `FindPickup` and `TickGround` handle pickups); the
scout is `ApConnection.Scout`.*

## 10. Quality of life: a quicker, smoother game

The goal: a way to skip the intro, the tutorials and other slow parts, as a sub-menu of on/off rows
(2026-09-25). The page isn't only for skips: a later row was planned that changes play, a pause-menu button
back to the seed's start. Such a row is fine in the panel as long as it never changes where items are, and the
logic never counts on it.

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
   really fast on its own"; the log shows `[qol] intro slides: passing them by`, then `over: normal speed`.
   **Replaced (2026-09-25):** the slides are now cut out after all (item 5, the opening), and the row was folded into
   *Skip cutscenes* ("can probably just be bundled"). The speed-up stays as a fallback if the cut misses.
3. **Free boat** (2026-09-25: nobody should have to farm berries in Archipelago). The Metal Island boat
   costs 300 berries (90 in a later state). The fare isn't in the boat scene but in the sailor's dialogue lines, so
   ScriptDump got a money column (`checkmoney`, `money`), which found both fares on the pier, lines 16 and 19, each
   `|checkmoney,N,20||money,-N|`, and a free trip back. The line a prompt jumps to is read inside the running
   dialogue through `MainManager.GetDialogueText(id)`, not through a new `SetText`, so a postfix there drops the two
   money commands from those two lines. It changes no reachability: with it off, the fare can always be earned in
   battle. Built, not yet seen.
4. **Warp button** (2026-09-25: a fifth pause-menu button, "warp to start", with a yes/no before it
   acts). The pause menu's row is window 0: `maxoptions` icons (4, or 2 in battle, so the button never shows there),
   made as `sprites[13 + n]`; confirm opens window `option + 1`, and the labels are `menutext[10 + option]` and
   `[50 + option]`. So the mod respaces the four icons, adds a fifth, raises `maxoptions` to 5, catches confirm on it
   before the game would open a "window 5", and writes its own labels. **First try threw every frame:** the game's
   `IconAnim` is handed exactly the four icons and indexes them by option, so the fifth option ran off the end
   ("got a lot of errors"); a prefix now hands it five, and the game animates the fifth like the others. On Yes
   (No is preselected), the menu closes the game's way (`PrepareExit`) and the game's own `TransferMap` takes the
   party to the Outskirts, beside the save point where a new game begins. **The icon** ("look at how the
   other menu buttons do things, and do the same"): a tinted Settings icon with the map item on top looked wrong, so
   a new dev dump, `SpriteDump`, saved the game's GUI sheets and a table of `guisprites` indexes (into the BepInEx
   folder, never the repo), and a contact sheet of them showed a round icon in the same style, `guisprites[34]`
   (a blue map). The button is now made exactly like the other four: one sprite, no tint, no overlay. A plugin
   reload with the menu open had left the old icon behind the new one; unloading now removes it. Title "Warp".
   **Seen on screen (2026-09-25, screenshot):** five matching icons, "Warp" above them, the description line,
   and the Yes / No box with No preselected ("this looks good"). The warp itself is still to see. **A second
   IndexOutOfRange, the mod's own this time:** coming back to the main page from another, the menu briefly still holds
   that page's shorter sprite array (8 to 12 long; window 0's is 19), and the button's per-frame check read slot 16 of
   it. It now checks the length first. Lesson: a prefix on a menu's `Update` sees every page's state, not just the one
   it was written for.
   **The logic never counts on the warp** (2026-09-25): it's fast travel and a way out when stuck, but a
   seed must not assume players teleport out, so every one-way drop still needs a real way back in the logic.
5. **Skip cutscenes** (2026-09-25: scenes and fluff that give no checks, starting with the two at the
   Snakemouth bridge). **The intro is no longer part of it (2026-09-26):** with Archipelago enabled the
   opening is always skipped, since a random start and a starting party member both need it gone, and the row is what
   players expect it to be, for scenes later in the game. Every scene starts through `EventControl.StartEvent`, so a prefix there sees each one by its
   event number and map. Each scene is read in full before it goes on the list, and it gets one of two treatments:
   *skipped* when it only moves the camera and party, talks and sets flags (the mod sets those flags and the scene
   never starts: the bridge message, Event0, flag 11), or *fast-forwarded* when it also changes the world in ways its
   flags don't cover (the game runs it at 8 times speed with its lines answered, as for the intro slides: the rope,
   Event1, which plays the bridge's Fall animation and fixes it fallen before setting flags 7 and 11; setting the flags
   alone would leave the bridge standing until the room reloads). Never a scene that gives an item, sends a check,
   changes the party or starts a battle, **unless the skip does that one thing itself through the game's own call.**
   **The arrival outside Snakemouth Den** (Event11, 2026-09-26: "can we skip this cutscene?"): a walk, one
   line, then journal discovery 0, which is a location. Its autostart (map `autoevent` 22:11) sets flag 22 itself on
   starting it (`MapControl.cs:874-882`), so the skip only records the discovery with `MainManager.UpdateJounal`, the
   scene's own call, which also shows the game's discovery pop-up; the check then goes as it would. **Seen
   (2026-09-26):** flag 22 and discovery 0 reset (dev), walked in from the cave's side: no scene, the pop-up.
   **The Tattle tutorial in the bridge room** (Event2, 2026-09-26): Vi and Kabbu walk, one line (map line 1,
   no item, flag, event or transfer command in the script dump), then flag 10, which also hides its trigger
   (`TattleTutorial`); skipped like the bridge message. Built.
   **The door room's puzzle solved** (Event4, 2026-09-26: "can we speed up this cutscene?"): it places the
   two rocks, destroys entities 0 and 1, sets flag 13 and drops the Mushroom whose pickup starts the trapdoor scene
   (`EntityControl.CreateItem`), so it is fast-forwarded, like the rope. Built. The trapdoor scene (Event5) itself stays
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
   which the tester stood clear of, taking it for the scene), the mod leaves what its end leaves (`EventControl.cs:3598-3822`),
   through the game's own calls: `ChangeParty({0, 1})` and `SetPlayers` (the `addleif` method), the tutorial's Crunchy
   Leaf, Vi's stand-in and the `blockingbox` destroyed, the exit (entity 2) active again with the default camera, flag 15
   and quest 11 on the board. Flag 15 sends location 1's check, and a hold-up shows the seed's item. The logic needs no
   change: Vi is in the party either way, and location 1 was already reachable from the start.
   **First tries (2026-09-25):** (1) the opening waited for its trigger, and the tester stood clear of it, taking it for
   the scene; now it runs as soon as the player is free. (2) The trigger then stayed in the room (flag 15 hides it only
   on a map load), the scene started anyway, since the block only covered "flag 15 unset", and crashed looking for Vi's
   stand-in the mod had removed (freed with `unstick`). Now Event16 is refused on that map whenever the skip is on, and
   the trigger is hidden. (3) The talk after the slides is still Event8, which only the slides' speed-up covered; that
   part (talk and party moves, no prompt) is now fast-forwarded too.
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
   (7) Seen: no slides, but the bottom of the building showed before the warp: the mod removed the slides' black backdrop
   when ending the scene, and the transfer only started after the opening. Now, with a test start, the backdrop stays up,
   the transfer starts as the scene ends, the backdrop goes once the start map has loaded behind the transfer's own
   fade, and the opening runs there. Its building-only steps (the exit, entity 11, the trigger) run only in the
   building, since the same entity numbers are other things on other maps.
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
   yet: that fight is also the first boss (its prize medal, flag 41 and what gates on it), so each of those needs a home.
6. **Item animation** (2026-09-25): a discovery showed nothing of what it found, and items from other
   players arrive silently. Your own finds always get the hold-up (pickups already did; a discovery recorded in play
   now does too); the row, *Item animation: All / Progression / Off* decides which items from other players do
   (default All, chosen once bursts were fast with the skip button held). The hold-up is the game's own `giveitem`, run on a key item stand-in (an ordinary item's
   `giveitem` does nothing when the bag is full, `MainManager.cs:11499`), held up by the leader; the item swap shows
   the chosen item and keeps the stand-in out, as for a location's gift, in a new display-only mode. The follow-up line
   `giveitem` always shows is an empty one the mod answers for a reserved number. Hold-ups wait in a queue for the
   same free moment the receiver waits for (no battle, scene, dialogue, menu or map change), one at a time; the item
   itself is always given by the receiver, never by the hold-up. A discovery already recorded when the save loads
   shows nothing. **A discovery's hold-up in play (2026-09-25, log):** the spider fight recorded discovery 1, the check
   went out, and after the fight chain the swap held up the seed's Mushroom for that location and kept the stand-in
   out. **Seen (2026-09-25):** a test hold-up from the new console command `holdup` ("Explorer Permit from
   TestPlayer") waited for a cutscene to end, then played; the item probe saw nothing added. The box read "You got a
   Explorer Permit": `giveitem` always uses the game's default article (`menutext[125]`), while a picked-up item uses
   its own (`itemdata[0, id, 3]`, a medal's `badgedata[id, 6]`, `NPCControl.cs:5670-5690`). Hold-ups and location swaps
   now set the item's own article. Not yet seen. A hold-up now waits for half a second of free time in a row, not one free
   frame: a chain of scenes and fights (the spider fights) can leave a free frame between links.
   **Seen (2026-09-25):** three queued test hold-ups waited through the spider fights' chain, then played one after
   another, reading "You got the Explorer Permit from TestPlayer!" (the game's own article for it). Each was followed
   by an empty box: an empty follow-up line is still shown as a box waiting for a press. The follow-up is now the
   game's `|end|`, which skips that wait. Confirmed on screen the same day: no empty box.
   **Bursts** (50 were queued to see what *All* feels like when a multiworld sends many at once: about a minute of
   boxes). Only the first of a burst waits for the settled half second; the rest follow as soon as the previous box
   closes. A summary box ("...and N more items from other players!") past three was tried and dropped: it felt off to
   the tester, so every item gets its own box. Instead, **holding the skip button runs the game at 4 times speed while one
   of the mod's hold-ups is on screen**, since the item-get's own pauses are fixed waits that fast text doesn't shorten;
   only a speed-up the hold-up made is undone. **Seen (2026-09-25): "better"**, and *All* became the default.
   **Replays are shown too** (2026-09-28: "all items appear, even on a reconnect"). A new save starts at 0
   received and the mod gives it everything the server has for the slot, oldest first, which is what makes a lost save
   recoverable; each of those items now gets its hold-up, per the setting, as items arriving during play do. At first
   replays were silent (only items past the count the server had at login, `ApConnection.ReceivedAtLogin`, were held
   up), so a new file showed nothing for a check with no scene of its own. The one exception left: an item whose
   check's own scene just showed it on screen (`ItemSwap.ShownInScene`, filled when a pickup or gift shows the seed's
   item, used up by that item's arrival). "Arrived after login" was tried first and missed a replay in a second new
   file of the same session: Meditation, found in the file before, came in with no box (2026-09-28: "I
   expected it to be remote"). **Seen (2026-09-28):** on the next new file Meditation arrived with its box,
   and the opening's items stayed quiet. On *All*, a new file late in a seed plays a
   hold-up for every item; *Progression* or *Off* shortens that. **A quiet start** (the same day: six boxes in a
   row on a new file was "a bit much"): starting items (sent by the server itself, slot 0) and the items of the
   opening's three checks (`quiet_locations` in `slot_data`, marked `quiet` in `locations.json`) arrive with no
   hold-up. A party member placed at any other location still gets its box. The opening skip used to queue its own box for Maki
   and Eetl's Gift, standing in for the gift scene it skips; it now skips that too when the check is quiet.
7. **Shop prices** (2026-09-25: Normal by default, Half or Free). The medal table's price columns (5 for
   berries, 7 for crystal berries) are scaled in memory, from a kept copy, and put back when the row is Normal or the
   mod is off. The logic never counts on it. **Now a bar on the Gameplay page (2026-09-26):** 0 to 10 pips
   like the multipliers (step 19), each a tenth of the price: a full bar normal (the default), 5 half, an empty bar free
   (1 = free was asked first; an empty bar keeps 5 = half and 10 = normal exact). Any price above free is at least
   1, rounded up, so small crystal-berry prices stay 1 on the low settings. A new key (`[Gameplay] MedalPrices`, a
   number), as the old one held a word. **Renamed *Medal prices*** (2026-09-26: more accurate): it scales
   the medal table, so every medal on sale anywhere, never an item shop's consumables. If item shops are ever scaled,
   they get their own row (*Item prices*), as their prices sit on another scale.
8. **Skip battle tutorials: read, and Leif's line skipped (2026-09-27).** A battle's scripted moments are
   `BattleControl.EventDialogue` cases, started by `CheckEvent` or by an enemy's own action. The only real tutorial is
   the fight against Maki in the opening (case 0 and 1, enemy `MakiTutorial`, while flag 15 is unset), which the
   opening skip already removes. The spider's first fight ends at once (item 5). What's left: case 3, Leif's one line in
   the first battle after he joins (flag 16 set, 24 not; it sets 24; `SetMaxOptions` reads 15 and 16, not 24), and
   story lines inside boss fights (cases 7, 8 in the first boss; others later), which stay. **Leif's line is always
   skipped with Archipelago on** (2026-09-27, even with Skip cutscenes off): once flag 16 is set and 24 isn't,
   the mod sets 24, outside a battle. Flag 24 also lets enemy 1 appear on the ground instead of always in the air
   (`MainManager.cs:6299-6309`); the game sets it with that same line, so only the moments between Leif joining and his
   first battle change, and only toward easier. **Seen (2026-09-27):** flag 16 set by the dev console at the
   lake (the lake scene hadn't triggered before the spider), the mod logged the line marked said, and the next fight
   started without it. *Code: `PartyMembers.cs`.*
   **Travel: Off / Warp / Map / Both (2026-09-26; built, not yet seen).** The Warp button's on/off became one
   *Travel* row (config `Travel`, default Both). `WarpButton.cs` now places the travel buttons after the game's four:
   Warp, then Map (left from the first button wraps to Map; Warp sits between, harder to hit by accident). Five fit
   two apart as before; six sit 1.7 apart (seen in screenshots: 1.8 pushed the first off the panel, 1.6 made them
   touch). The game's four are placed at their final spots as the game makes them (a postfix on `NewUIObject` for the
   `menuicon` objects): moving them a few frames later made them jump and overlap while the menu opened. The map shortcut already holds sprite 18, so a second button takes sprite 19
   in a grown `sprites` array, and `IconAnim` is handed one entry per button. **Icons:** Map gets the round
   blue map (`guisprites[34]`, Warp's icon until now: "fits a map more"), Warp gets the map item's scroll
   (`itemsprites[0, 41]`, "like a return scroll"), so the two differ without a tint. The blue map is one finished sprite with its round
   backdrop painted in; the scroll is an item sprite with none ("don't have a background thing"), so it gets
   one. The game's white circle (`guisprites[59]`) came with its own dark outline and shading and looked off
   (the others are one flat ring and one flat inner colour, and brown and pale was dull). Now the mod draws the
   backdrop itself: a texture the size of the blue map icon, a flat ring round a flat fill, in the game's own colour recipe,
   measured from its icons (`MEASURED.md`, "The round pause-menu icons' colours"): ring at full saturation and 0.51
   brightness, fill at 0.34 saturation and full brightness, one hue. Guessed colours kept looking off (pale brown, a
   pale orange, teal that blended into the green and blue beside it, a vivid orange that stuck out, a ring and fill
   that read as two colours); the map icon was the one that "nailed the color scheme", and measuring it gave the
   recipe. **Lime, hue 0.28 (chosen)**: colour-wheel spacing put it in the row's biggest gap (gold 50° to green 155°),
   after orange at the recipe turned salmon (0.05) or brown (0.08, a dark orange is brown) and pink (0.9) sat too close
   to purple and red. `warpcolor` (dev) still tries a hue. **The scroll stays the game's own art:** softening a copy
   (black outline to dark lime, colours lifted) was tried and looked worse each time (first the copy took another
   sprite from the atlas, as a render texture's rows run the other way on Direct3D; then dark reds were caught as
   outline; then it read flat and washed out), and none of the game's other round icons (key, leaf, the library tabs)
   means "warp". A drawn icon would be art work, not code. **Then the leaf:** of the game's premade round
   icons (the key and leaf of the item categories, `guisprites[23]` / `[22]`; the Library's tabs, where the map icon
   comes from), the leaf "looks more as the game intended" than the scroll on a drawn backdrop, so Warp uses the leaf,
   unchanged ("leaf" as in leave). **Design rule (2026-09-26): the game's own art whenever it
   fits**, over adapted or drawn assets, as the panel's pages are built to look and feel like the game's Settings
   screen. The drawn backdrop stays for the dev console's `warpicon scroll`; it is the button's own sprite (so the game's outline and wiggle apply), the scroll on top.
   **Map** opens the game's own map window (6) the way its map shortcut does (`windowid = 6`, `BuildWindow`), in a
   travel mode: confirm on a visited area (`librarystuff[4, area]`) opens "Travel to <area>?" (No first) instead of
   flipping the description's pages; the map opened any other way keeps vanilla controls. On Yes the menu closes the
   game's way and `TransferMap` lands the party beside the area's travel spot: a save point at its entrance or hub
   (starting choices, `AreaSpots`; the Outskirts use Warp's start spot), from the entity dump and each map's area
   (the map dump's new `area` column, `MapControl.areaid`). **The first try threw every frame** ("a lot of errors"; a one-time
   diagnostic finalizer on `PauseMenu.Update` logged the state): the map read the pause menu's leftover `option` (5,
   the Map button) as an area and drew toward a marker that didn't exist, and no area was marked visited at all, as
   a new file never marks its starting area (`MEASURED.md`, "Visited areas and the pause-menu map"). Opening the
   travel map now sets `option` to -1, the map's own "none yet", and marks the area the party stands in as visited
   (the one field `UpdateArea` writes). **Then no box showed:** it opened (the log said so) but behind the map, a 3D
   object at depth 5 on the GUI camera; the map's box now hangs off the GUI camera at depth 2. **Seen on screen
   (2026-09-26):** the map opened without errors, "Travel to Bugaria Outskirts?" showed over it, and Yes landed the
   party beside the start's save point ("a good location"); then back and forth between the Outskirts and Defiant Root
   (after a dev warp there), both working. The other areas' spots are starting choices, checked as they're
   reached.
   **Skip confirm: Off / Warp / Map / Both (2026-09-26; seen on screen the same day: on Map, map travel went at once and Warp still asked),** an add-on to *Travel*, so its
   row sits right below it (the two belong together, not split apart). Warp: picking the Warp button warps at
   once; Map: confirm on a visited area in the travel map goes there at once; Off (the default) keeps both boxes.
   Disable all sets it Off (asking is the safe value). The press that would open the box goes straight to what Yes
   did, through the one method both share (`Go`), so the two can't drift apart; its log line says when no box was
   shown. An area not visited still gets the buzzer. Config `[QualityOfLife] SkipConfirm`.

The panel got an eighth row, "Quality of life", which opens a second page in the same box; cancel comes back.

**Disable all and Reset to defaults (2026-09-26; built, not yet seen).** Two buttons side by side at the top
of the Quality of life page (not rows in the list); left/right picks one. **Reset to defaults is on the left, where the
cursor lands** (entering the page shouldn't put you on Disable all), Disable all on the right. Confirming one opens a
**Yes / No box** over the page (a box, not the choice inside the menu), built with the game's own box
(`MainManager.Create9Box`, the controls type the help box uses), the question on top and the leaf on the answer. No is
picked first, so a stray press never wipes the settings; cancel closes the box. The question isn't repeated in the help line below.
**The Gameplay page has the same two buttons**: Disable all there sets Difficulty Normal, Enemy
scaling Off and Detector Off; Reset puts each back to its default. Both pages open on Reset to defaults. Disable all turns every row off (a choice row
to its off value: Enemy scaling Off, Item animation Off, Medal prices full); Reset to defaults puts every row back to
its default (`QualityOfLife.DisableAll` / `ResetAll`, the defaults from each setting's own config definition).

**Status:** in progress: Fast text, the opening skip, the Warp button's menu and Item animation seen on screen (2026-09-25); the bridge skips and Medal prices not yet seen; replays held up and the quiet start seen on screen (2026-09-28); Free boat seen (the fare waived with no berries, the boat left, 2026-09-26) and then removed for the Boat Ticket (the Archipelago guide, build step 16), the warp itself, map travel and Skip confirm seen (2026-09-26); Skip cutscenes' Den arrival, trapdoor and spider scene seen (2026-09-27); Skip battle tutorials: Leif's first-battle line skipped, seen (2026-09-27).

*Code: `QualityOfLife.cs` (the settings and the per-frame speed-ups), `ApMenu.cs` (the second page),
`WarpButton.cs` (the Warp button), `HoldUps.cs` (item animation's hold-ups).*

## 11. Playing with fewer party members: stand-ins and followers

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
moves every member at once, so the leader would be pulled to two spots and play another character's animations. Limits: a scene that changes the party, or
needs a member's ability, still needs the member (a logic rule, as for the boat); some scenes will look odd, and each
one that used a stand-in is logged, to skip or hold back one by one. **Seen (2026-09-25):** the barkeeper's
first talk played through with Leif's stand-in (naming Leif, as expected), so it went on the skip list, only while
its flag 158 is unset: the same scene later takes bounties and gives their rewards.

1. **Without a test start, the party stood under the house** (2026-09-25: "the weird broken location" seen
   briefly before step 10's (7) fix). Event8 places the party only after its slides (Kabbu 2.5 left of entity 4,
   `EventControl.cs:2770`), so cut before them the party kept a new game's raw spawn point; every test since step 10's (6) had a
   test start, whose warp moved it away. The opening now stands the party where the scene would have. Seen on screen:
   right spot, but the fade-in first showed the spawn point, then a jump: the opening runs a few frames after the scene's
   end, and the fade-in starts at that end. So the scene's end (the mod's) moves the party there and snaps the camera
   before the fade-in. Seen with only the first half loaded: the right spot, then a snap back once the opening
   ran, since it waits for the fade-in to end and the player can walk during it. So the opening no longer places anyone:
   it uses where the player stands. Seen on screen: right spot, no snap, but outside the house the camera was broken
   with Leif alone. `ResetCamera` at the scene's end aims the camera at the leader, the opening's `ChangeParty` then
   destroyed that character (with Vi and Kabbu, Kabbu's was reused), and leaving the house hands the camera back to the
   player only for insides that centre on themselves (`MapControl.cs:1373`). Event16 itself ends with `ResetCamera()`
   after its party change (`EventControl.cs:3795`); the opening now does too. **Didn't help** (seen: still low outside,
   and stuck after the gift). Two guesses failed, so measured: a console command `cam` logs what the camera follows. It
   read `target DESTROYED` with the leader (`Player 0`) fine. Unity destroys an object at the end of the frame, so the
   opening's `ResetCamera`, aiming at `MainManager.player`, still found the old leader's character and followed it as it
   vanished, leaving the camera where it last stood, under the house. The opening now aims the camera at the new
   leader's character itself (`playerdata[0].entity`). Seen on screen: the camera fine from the gift on, but Vi and
   Kabbu showed for a moment and the camera was odd outside until then: the opening swaps the party only once the
   fade-in is over. Moving the swap into the scene's end, before the game's `EndEvent`, crashed it (`FixEntities`,
   a NullReferenceException, a black screen; freed with `unstick`). Now the scene ends as before behind the black
   screen, and on the next frame the party is swapped, placed and the camera set, then the fade-in starts. **Seen
   (2026-09-25):** Leif alone from the first frame, in the room, the camera right inside, outside and after the gift.
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
   takes its Leif from the follower list (`map.tempfollowers[0]`, `EventControl.cs:3339`), empty since item 8, and threw
   `ArgumentOutOfRange` at its start (predicted from the code a moment before the tester reached it; freed with
   `unstick`). A prefix on `EventControl.StartEvent` doesn't start it and leaves what it leaves: flag 16, the regional
   flag of the creature it removes (entity 5) with the creature gone, Leif off the follower list. **Then always skipped**
   with Archipelago on ("it's not a check"): it's no location, only the logic's *Leif Joins* event at the lake
   (flag 16), and without its fight the lake no longer quietly needs Vi. When Leif isn't in the party yet, he joins right
   there, as the scene's own `ChangeParty({0, 1, 2})` would have him (then `SetPlayers`, the camera on the leader); with
   one starting member the guard still decides whether he may. **Moved earlier** ("it could just happen after
   the spider, when Leif first starts to follow"): once the spider scene is over (flag 27, not yet 16), Leif joins for
   real, flag 16 goes on, and the story's follower Leif is removed with his follower entry. The logic is unchanged: *Leif
   Joins* is in the same region (*Snakemouth Den*) as the lake. With one starting member, only once Leif is allowed
   (received). On loading, the tester's Leif-alone file got flag 16 ("Leif was already in the party"); **seen on screen:**
   the lake walked past with no scene. With a two-member start, not yet seen.
11. **Position lookups beyond the party** (2026-09-25): the droplet scene (Event21) ends by walking the
   second and third members by position (`GetEntity(-2)`, `(-3)`, `EventControl.cs:4112-4114`; `MainManager.cs:18526-18537`
   answer only inside the party) and threw on nothing. In a scene, slot k beyond the party now gets the k-th member in the
   story's order (the acting role first, then the others by id) as a stand-in. The acting leader also has a fallback
   when a reload forgot the story's party: the first missing member by id.
12. **Every way the code reaches for a party member, listed** (2026-09-25: "dump fully what a party member
   or follower is, so we know everything they could ask for"): `dev-scripts/party-access.py` counts 29 ways across the
   decompiled code, with the methods and events using each (`--where <way>`). Covered: lookups by position and character,
   the party as a list, `PartyMover`, `SetPlayers()`, `ChangeParty`, `.following`, `extrafollowers`, the leader. Open,
   since a direct index can't be intercepted: `playerdata[1]`/`[2]` (Events 52, 122, 130, 137, 138, 182, all past
   chapter 1, and `BattleControl.DoAction`/`EventDialogue`, to confirm they check the party's size), `tempfollowers[..]`
   (11 events; they read story companions, and break only for a removed party member, so far only Event14, now skipped),
   `partyorder` (Events 6, 54, 138) and `GetExtraFollower` (Event223).
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

**No warnings for missing animations (2026-09-26: "dumb to leave bug/errors laying around, even if its
harmless").** A character asked for a state its controller lacks (a lone Leif acting another member's part, a swapped
enemy look with another enemy's movement) made Unity warn twice every time ("State could not be found", "Invalid
Layer Index '-1'"), 68 times in one session, and play nothing. Every character's animation goes through
`EntityControl.SetAnim`, which calls `Animator.CrossFadeInFixedTime(name, time)` with no layer. `AnimGuard.cs`
swaps those two calls for a check: a state found on any layer plays as before; a missing one is skipped (what the
game did anyway) and logged once per controller (`[anim] BeeBoss(Clone) (BeeBoss) has no state 'Walk'`), which is
also the list for mapping a missing animation to the closest one later. Only while Archipelago is enabled. **Tested
(2026-09-26):** with a Bee Boss look walking like an Underling, the count stayed at 68 and one `[anim]` line appeared
instead. **The game's direct `anim.Play("name")` calls** (about 30: battles, events, the map, menus) all end in
Unity's `Animator.Play(string, int, float)`, so a prefix there gives them the same check (the asked layer, or any
layer for -1), installed and logged at load. A layer warning that still shows comes from another path; the log names
it.

**Unity's glow-colour error, handled like the animation warnings (2026-09-26):** a light in Rubber Prison's cell
block has a material without the glow colour the game's `GlowTrigger` reads, so Unity logged an error on arrival
(harmless: the value is only written back to the same missing property). `GlowGuard.cs` swaps the three reads for one
that checks first and reads black, logging each material once; only while Archipelago is enabled.

**A member added mid-map meets the enemy-only walls (2026-09-26):** after adding Kabbu on Outskirts East,
walls only enemies should bump into blocked the party. A map load tells those walls to ignore each character, and the
added member comes with new characters (`MEASURED.md`, enemy-only walls). Adding a member now redoes that step the
game's way (`SetPlayerColliders`, 0.2 s later), as receiving a party member will need. Seen on screen
(2026-09-26): Vi then Kabbu added mid-map, and the party walked through where the wall had been.

**The seed decides the start, not the config (2026-09-26):** with *Starting Party Member* on (the Archipelago guide,
build step 18), `slot_data`'s `starting_member` takes the place of the dev `TestStartMember`, and party members arrive
as items. A received member joins at once, the way `addmember` does. Which members the guard lets in is worked out
from the items the save has counted, every frame, and forgotten on the title screen: kept only in memory, a member
from one file would let the story add him early in the next. Not yet seen in play.

**Built (asked for again, 2026-09-27, after landing by the rock once more):** after the pitfall scene (the trapdoor into `SnakemouthFallRoom`,
Event5), place the party as if it had just entered the fall room through one of its doors, the same arrival a random
start uses (build step 15 of the Archipelago guide; the arrival jump from the door's entity, step 13 here). It is
expected to line the landing up better than the scene's own spot, as the random start into that room did.
How: Event5 starting on `SnakemouthDoorRoom` marks a landing due; on the first free frame in `SnakemouthFallRoom` after
the scene, the game's `TransferMap` into the same room with the spots of the door room's door into it (`DoorInto`, the
way down the opened trapdoor), as a random start arrives. Only with Archipelago on. First seen with three members: the
scene ended without error (the patched list, `[party] Event5 placed 2 members; member slot 2 stands behind`); the
landing spot itself not yet seen. *Code: `QualityOfLife.cs`.*

**Status:** works with Leif alone, seen on screen through chapter 1 into chapter 2 (2026-09-25); Leif joining after the spider with the story's two (Vi and Kabbu) seen (2026-09-26: he followed, could lead, and showed in the pause menu); items 5 and 6 not yet seen; the direct lookups in item 12 still open.

*Code: `PartyFit.cs` (the stand-ins and the acting leader), `PartyMembers.cs` (the member guard, followers,
Leif's joining).*

## 12. Shops in the game

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
`prompt` whose choices are listed as N targets then N texts (`MainManager.cs:12213-12222`); the reshuffle is the one with
target `-199` and text `-195` (Shades's line 1, Merab's line 34, read with the console's `script`). With Archipelago on,
that pair moves to the front, in the map's dialogue table in memory, once per map load. Seen (2026-09-25):
at Shades's, reshuffling is now a matter of tapping the confirm button.

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

**Status:** works, seen on screen (2026-09-25): Merab's medal shop with its full stock, the reshuffle choice, Madame Butterfly's item shop, the caravan, and pickups in houses; Shades's shop not yet built as locations.

*Code: `ShopSwap.cs` (medal shops and their stock), `ItemShops.cs` (item shops), `KeptOpen.cs` (the shopkeeper
and scenery kept present), `QualityOfLife.cs` (the reshuffle choice first), `ItemSwap.cs` (`UpdateItem`, pickups
in houses).*

## 13. Doors rewritten: the entrance randomizer in the game

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
right map: the plaza's door to the Outskirts' east area and back, the Outskirts' exit to the Commercial District and back,
each several times. The tester found it confusing to keep track by eye, so from here the log is the record of each trip.

**What a door carries, side by side** (2026-09-25, reading the rest of `TransferMap`, `MainManager.cs:17467-17620`). A
door's `data` is more than its target: `[1..3]` switch the camera's offset, angle and limits on arrival (from
`vectordata[3..6]`), and `[4] == 1` means the party isn't walked into the door first (nine doors: holes, wells, ladders,
the fall room's). The arrival jump is read off the door's own entity, `emoticonoffset.x` (entity table field 175). So a
rewritten door takes the target, the camera and the jump from the other door, and keeps its own `[4]` and `vectordata[0]`:
whatever happens on the side you leave stays, whatever happens on the side you arrive at comes along.

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

**Status:** works, seen on screen (2026-09-25): a rewritten door, and a coupled swap both ways; the 36 doors that don't pair both ways are still to check in play.

*Code: `DoorShuffle.cs`; `dev-scripts/door-graph.py` (the pairs).*

## 14. The Detector for every check

The Detector medal beeps when a room hides something. In a seed it should beep for what matters instead: any
of the seed's checks left in the room. That meant replacing the game's answer, not adding to it.

**The Detector for every check** (2026-09-25: beep for any kind of check left in the room: shops, quests,
someone to help, not only hidden items). Read first how the medal works: one second after a map loads, the game asks its
objects (`NPCControl.CheckHidden`: buried crystal berries, grass hiding one, a dig spot with a medal) and the map
(`MapControl.CheckDisc`: an unrecorded discovery) whether something is hidden; any yes sets one value,
`map.hiddenitem = 100`, and the map's update turns that into the "!" over the leader and the beep
(`MapControl.cs:885-896`). So nothing about the medal needs changing: a postfix on `CheckDisc` (run a second after every
map load, discoveries or not) sets the same value when one of the seed's locations on this map isn't done. Every location
type has a map: pickups, gifts (quest rewards included, where the reward is handed over), shop copies and item shops
from `slot_data`, discoveries from the map's own `discoveryids`. Done means the server has the check, or offline the save
says so (its flag, crystal berry, journal entry, a shop copy's bought bit). Only while the Detector counts as equipped
(the medal, or the panel's Detector row). **In a seed the mod's answer is the only one** (beep with one check
or more left, quiet when the room is done): the game's own checks would still beep for hidden things the seed doesn't
have, so `NPCControl.CheckHidden` doesn't run, `CheckDisc` is replaced by the mod's answer (a beep or silence, logged
either way), and a music record's `Start`, which sets the value as the map builds and is used on the first free frame
(`MusicSpinner.cs:54-57`), has it cleared right after. Outside a seed, all vanilla. **Seen (2026-09-25):**
in the Residential District it beeped for the rooftop item, then, with both items taken, for the quest reward still
handed out there (location 16, the delivery quest), as intended; quiet in the plaza with none left.

**Status:** works, seen on screen (2026-09-25).

*Code: `CheckDetector.cs`.*

## 15. Difficulty and the Detector: the panel's two game settings

Two rows in the Archipelago panel change how the game plays, never where items are. Both work by answering the
game's own "is this medal equipped?" question, so the save stays clean and the logic never changes.

**A row "Difficulty: Normal / Hard / Hardest" in the Archipelago panel** (2026-09-24). Hard and
Hardest add the game's two Hard Mode levels; Normal leaves it to the game (the medal equipped, or the
HARDEST code). **Default: Normal.** Boss prize medals are paid out on every setting (apimplementation.md,
build step 10).

**What belongs in the panel** (2026-09-24): only on/off preferences that never change what's where
(Difficulty, Detector, later DeathLink, which only adds a tag to the connection). Anything that decides the
seed (entrance rando, shuffles, goals) is a player-file (yaml) option, applied from `slot_data`.

**Every panel setting applies only while Archipelago is enabled** (2026-09-24): vanilla saves play
exactly as vanilla. Difficulty, Detector and every item swap check the switch; a Hardest flag the mod set is
cleared the moment it's switched off.

**A row "Detector: On / Off" in the Archipelago panel** (2026-09-24), a help for finding items.
On acts as if the Detector medal (#2) were equipped; Off leaves it to the game (the medal equipped or not).
**Default: On.**
All three of its effects ask one question, `BadgeIsEquipped(2)` (objects `NPCControl.cs:1344`, discoveries
`MapControl.cs:408`, music `MusicSpinner.cs:54`), and Hard is the same question for medal #11, so one patch
on `BadgeIsEquipped` serves both rows. It changes no save data and no logic.

**Built (2026-09-24), not yet seen on screen:** the panel has eight rows now (spaced tighter so the status
line still fits). Difficulty offers Normal, Hard and Hardest. `MedalAssist.cs`
answers "equipped" for medal 11 (Hard) or 2 (Detector) on party-wide checks, on randomizer saves only. The
medals menu equips from the medal list itself, never through that check, so it's unaffected.

**Hardest** (chosen: switchable, the save stays clean): its extras read the save's HARDEST flag (614)
directly, and the game keeps no other trace of a typed code. So the mod turns 614 on in play and remembers
that it did; each time the game saves, the flag is cleared just for the write and put back after, and
switching down clears only a 614 the mod set. Loading a save or starting a new one forgets the mark, since
those flags are the save's own. If any of the three hooks (save, load, new game) is missing, Hardest does
nothing rather than risk a save.

**Status:** the Detector row's effect seen on screen through step 14 (the Detector beeping for checks left in a room, 2026-09-25); Difficulty (Hard, Hardest) built (2026-09-24), not yet seen on screen.

*Code: `MedalAssist.cs`.*

## 16. Randomizer saves kept apart from normal saves

With the Archipelago mod enabled, the game reads and writes its saves in a separate folder, so a randomizer
run never touches a normal save. It had to exist before the mod granted its first item.

Through a whole session played with the Archipelago mod enabled, the game
saved only into the `archipelago` folder (last write 05:06), and the normal save files kept their earlier
times (03:42 and 2023), checked on disk on 2026-09-24. The redirect covers all five places the game touches
a save file, and normal saves are only reachable with the mod disabled.

**Status:** works, checked on disk (2026-09-24).

*Code: `SaveRedirect.cs` (the separate save folder, patching the game's five save-file functions in `InputIO`).*

## 17. Enemy scaling: every area fair whenever you reach it

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
   The intro spider (can't be won), tutorial and test fights, and the ids the game itself leaves out of Hard Mode's
   x1.5 are left alone.
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
- Every scaled enemy is logged (`[scale] Seedling (9): home 1, target 10: hp 4 -> 10, attack +2, ...`).
All the constants are starting values, tuned by play.

**Seen on screen (2026-09-26):** at level 1 on Party level, a map Underling shuffled into a Dead Lander G was
logged `home 27, target 1: hp 35 -> 7, attack -3, def 1 -> 0, exp 74 -> 9`, and Spy in the fight showed HP 7, Defense 0.
The panel row stepped through its three values (the log followed each). **How it played:** tough but fair. Leif alone at 7 HP
(healing 1+ a turn) went to 4 HP after its first hits, then to 2. The tester judged 35 -> 7 HP and no defence balanced,
"fair/hard" for anyone who shuffles enemies. Its attack sits at the -3 floor; if late enemies prove too harsh early,
that floor is the first knob to try.

**Per-hit attack felt fair (2026-09-26):** the same Dead Lander G at level 1 (hits x0.19, the dev cheat
`onehit` off) landed 1-2 attacks at "fair damage" and died in two hits, like any other enemy there.

**Status:** works, seen on screen (2026-09-26): scaled HP, defence and per-hit damage in a fight, and the bestiary;
the constants still to tune by play.

## 18. Use on normal saves: the settings without Archipelago

Quality of life and Gameplay are useful without a seed too. **Decided (2026-09-26):** an opt-in row, a
deliberate exception to "vanilla stays vanilla" that only the project owner could make.

- **The row:** *Use on normal saves*, ON / OFF, **off by default**, on the panel's main page under Achievements.
  Help lines: "Quality of life and Gameplay also apply with Archipelago off." (on) and "Quality of life and Gameplay
  apply only with Archipelago on." (off). A label longer than 15 letters now shrinks to fit before the arrows, as a
  long value already did.
- **What it turns on, with Archipelago off:** the Settings rows to both pages (step 8), Fast text, the scenes Skip
  cutscenes skips or speeds by, Travel (Warp to Start goes to the game's own start), Medal prices, Difficulty,
  Detector, Enemy scaling, the EXP and berry multipliers (step 19), Uncap FPS (step 24) and skipping the game's
  5-second forced collection (step 25).
- **What it never turns on:** anything tied to a seed. The intro skip (its end sends the first check and makes the
  seed's start), items, checks, the shuffles, the Detector's check beeps, boss prizes paid on any difficulty (they are
  checks), Item animation (only items from the server), and the achievement guard.
- **How:** one `settingsOn` in `Plugin.cs` (Archipelago enabled, or this row) goes to the modules behind the two pages
  in place of the Archipelago switch: `MedalAssist` (which keeps the Archipelago switch for boss prizes),
  `EnemyScaling`, `InGameSettings`, `Multipliers`, `FrameRate`, `ClockCleanup`, the Travel buttons, and `QualityOfLife.SettingsOn` (fast text, the scene list, and
  `ShopSwap`'s prices). The seed's start and the entrance randomizer's forced Warp answer only with Archipelago
  enabled, so a normal save never warps to a seed's start.

**Status:** built (2026-09-26), the build succeeds, not yet seen in game.

## 19. EXP and berry multipliers: bars like the volume rows

An opt-in for a faster, easier game (Next 16 and 17 in `apimplementation.md`).

**Decided (2026-09-26):**
- Two rows on the **Gameplay** page, *EXP multiplier* and *Berry multiplier*, **1x to 10x, default 1x**
  ("just 1-10x to make it simple"). First planned as 1x-5x on Quality of life.
- **They look like the game's volume rows** (asked for with a screenshot of Music Volume): ten pips between the
  two arrows, one per step, the lit ones yellow; left / right lights or clears one.
- Only while Archipelago is enabled, or with *Use on normal saves* (step 18). No check and no logic depend on them.

**How it works** (`Multipliers.cs`, the facts in `MEASURED.md`, "What the mod's code relies on"):
- **EXP:** a postfix on the battle's own `BattleControl.GetEXP(amount, fixedexp, enemy)`, the one call that turns each
  defeated enemy into its EXP share, after the game's Hard Mode bonus and its per-enemy caps. It multiplies that share,
  so it stacks on top of enemy scaling. The game then adds it to the battle's total, which it caps at one level's worth
  (`neededexp`); that cap stays, so a high multiplier early mostly means a level per battle. The hologram fights
  (flag 166) keep the game's 5.
- **Berries:** a prefix on the first step of `NPCControl.BerryBounce`, the coroutine the game starts only right after a
  berry lying in the world (1, 5 or 20, from the map or dropped after a fight) has been added, and before it clamps
  money at 999. **First hooked on `BerryBounce()` itself, which never ran** (2026-09-26: 10x berries gave the
  plain amount, and the log had no berry line while the EXP lines were there): that method only builds the coroutine
  object, and a stub that small is inlined into its caller, so a patch on it is skipped. The patch is now on the
  coroutine's `MoveNext` (`AccessTools.EnumeratorMoveNext`), reached only through the interface, and acts on state 0. It adds the
  rest (value x (multiplier - 1)) and clamps the same way. A check's berries come from the server through the item
  grant, never this pickup, so they aren't multiplied.
- **The bar** (`ApMenu.DrawPips`): the game draws a volume row's ten pips with `guisprites[59]` (empty, a quarter
  size) and `guisprites[42]` coloured yellow (lit, a third), 0.4 apart from 0.7 past the left arrow
  (`MainManager.ShowItemList`, type 17). The panel's arrows sit closer, so the same layout is scaled by 0.68.
- Each page's two buttons: Reset puts both back to 1x, Disable all sets 1x.

**Seen on screen (2026-09-26):** EXP at 10x: a Pseudoscorpion and a Cactus logged 5 -> 50 and 7 -> 70, and the
battle gave 100, the game's cap of a level's worth.

**Status:** works, seen on screen (2026-09-26): EXP at 10x, and a berry picked up at 10x.

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
- The row joins Reset to defaults (back to its default: Archipelago then, Rarity since step 22) and Disable all (Off). Only while Archipelago is enabled, or
  with *Use on normal saves* (step 18), like the page's other rows.
- The Quality of life page grows to eight rows (the last at the panel's lowest row spot, above the help line).

**Status:** built (2026-09-26); the row seen on the ten-row page (2026-09-26, step 21); not yet seen: a hold-up with it off.

*Code: `QualityOfLife.cs` (`ItemColors`, `ApColors`), `ApMenu.cs` (`ColorsRow`), `ItemSwap.cs` (`PlayerText`, `ClassText`).*

## 21. Archipelago icon: other players' items on the ground and on shelves

**Asked (2026-09-26):** once the icon was drawn (step 9, `ApIcon.cs`), use it for every other world's item,
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
- With more rows, the Quality of life page's rows sit closer (the first and last where they were); the other pages
  are unchanged.
- **Shops name it too (2026-09-26):** a shop's box names another player's item in its class colour, and its
  description says whose: "A useful item for Other (APQuest).", or for another Bug Fables player's item "For
  BugTester2: " before the item's own description. First the name was "<player>'s <item>", but a shopkeeper pastes the
  name into a line the game has already wrapped, so it ran off the bubble ("Interested in that BugTester2's Crunchy
  Leaf?", seen on screen): the name is now the item alone. The shelf's own description box shows every item's name in
  plain black, the game's own included; kept so (2026-09-26): the backdrop and the bubble already carry the
  colour.

**Seen (2026-09-26):** the icon on the ground (the Ladybugs' Sword) and on the Caravan's shelf beside another Bug
Fables player's and your own items; the page with ten rows, every row and both help lines fitting (after a fresh
launch: late in a day of about forty hot reloads the settings pages showed their arrows but no text, and a restart
brought it back; no error was logged, and the letter pool had 487 of 500 free).

**Status:** works, seen on screen (2026-09-26): Other games on the ground and on a shelf; All players and Off not yet
seen.

*Code: `QualityOfLife.cs` (`ItemIcons`, `IconMode`), `ApMenu.cs` (`IconsRow`, `RowAt`), `ItemSwap.cs` (`Describe`), `ApIcon.cs`.*

## 22. Item backgrounds: how much an item matters, before you take it

**Asked (2026-09-26):** the sprite (or the Archipelago icon) says what an item is, not whether it matters; the
starburst a pickup grows when taken already has the class colour. So show it before: a check's item, on the ground
or on a shop shelf, has that starburst behind it (yours included: "include the players own things"), and a Quality of life row turns it off for a surprise:
**Item backgrounds: ON / OFF**, On by default, apart from the icon row.

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

**Status:** works, seen on the Caravan's shelf and on the ground (2026-09-26).

*Code: `ItemSwap.cs` (`Mark`, `MarkColorOf`), `QualityOfLife.cs` (`ItemBackgrounds`), `ApMenu.cs` (`BackgroundsRow`).*

## 23. The Archipelago icon, drawn in the game's style

Another game's item needs a picture in Bug Fables, and today it showed the vanilla item's sprite, which read as the
vanilla item (the tester took another player's Sword for their own). Archipelago has a logo; the question was which
image, and whether it's fine in a public repo forever.

**Drawn in code, so nothing is copied (2026-09-26):** "use art from within the game itself ... something
that looks good but also the same style as the game". The game's round pause-menu icons are flat, one hue as a dark ring
round a pale fill (`MEASURED.md`, the round icons' colours), and the mod already drew circles that way for the Warp
button. So `ApIcon.cs` draws the logo itself at runtime: six overlapping circles in the logo's colours (sampled from
your Archipelago checkout's `data/icon.png`), placed round a circle with the middle open, each later one cutting a gap
into those below, as in the logo. Item-sized, like the party members' icons (the mod guide, step 11).

**How the look was picked (on screen, eight looks):**
1. The game's orb recipe exactly (dark ring, pale fill): pastel, with outlines too heavy at item size ("the outlines /
   shading is a bit weird").
2. The logo's own colours with a thinner ring, flat with no ring, and the recipe thinner and stronger: better, but on
   the red starburst of a hold-up the red circle vanished, and see-through gaps let any backdrop wash the colours out.
3. As a sticker, the gaps and a rim round the flower filled: white (odd), white thinner, black thin (too sharp), black
   as thick as the first white. Compared as hold-ups on the four class backdrops (dev `holdup ap`), then side by side
   on the Caravan's shelf, close up and at a distance (dev `shelflook`). **Black, the fuller rim**, won: it reads on any
   backdrop, keeps six separate circles at a distance, and matches the game's outlined item sprites.

**Where it's used:** another game's item on the ground, on a shelf, at a pickup and a gift (step 21's row decides whose
items), on the class-coloured backdrop of step 22.

**Status:** works, seen on screen (2026-09-26) on hold-ups, on the Caravan's shelf and on the ground.

*Code: `ApIcon.cs`; used by `ItemSwap.Describe`.*

## 24. Frame rates above 60: smoother, and the same game

The game's settings offer 30 or 60 fps. More was wanted on a 240 Hz monitor, as a Quality of life row
(first Off, 120, 144, 240; now ten pips with Monitor the default, below; `UncapFps` in the config), overriding the game's own frame rate and VSync while Archipelago is on, and done
"properly so things don't break" (2026-09-27).

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
- **The cap.** A cap that divides the monitor's refresh rate is met with VSync (240 on 240 Hz: every refresh; 120: every
  second one); any other is a limit with VSync off. Without VSync at 240 on 240 Hz the frame times wobbled from 2.9 to
  5.3 ms. Re-applied after the game's own `ApplySettings`; Off calls `ApplySettings` to put the game's settings back.
  The game's own settings file is never written.
- **Motion drawn between physics steps.** Characters get Unity's rigidbody interpolation (new ones in
  `EntityControl.Start`); the camera is placed between its last two steps before drawing. **Pitfall, found on screen:**
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
  with a slight shimmer there only (drawn at physics steps); sharp again on the ground.
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
  screen (2026-09-27):** knocked around properly, every time.
- **Random shakes re-rolled once per 1/60 s.** Some effects jump to a new random offset every frame, a blur at 240
  (seen: shaky text in conversations sharp at 60, blurry at 240). Their timing was already right; only the re-roll
  was per frame. Now, while the row is on, the offset holds between ticks: `FontEffects` (shaky and glitchy letters;
  a shaky letter's position also overrides wavy, so wavy holds with it), `MainManager.ShakeObject` (the bushes before
  the leaf gang's ambush, Event128, and many scenes) and `EntityControl.ShakeSprite` (a character's shake), the last two
  run as the game's own loop with the offset kept. The camera's screen shake needs nothing: it's rolled in
  `FixedUpdate`, 50 times a second at any frame rate (seen: the swamp bridge's collapse, Event130, looked normal at 240).
  **Seen on screen (2026-09-27):** the text sharp at 240. Not
  yet seen: the bushes, a character's shake.
- **What the game counts in frames runs 60 times a second.** Every method that reads `Time.frameCount` (24, found by
  reading each method's IL at load) sees a 60 Hz count instead: on a frame that starts a new 1/60 s, the count; on the
  frames between, 1, which no `% n` check divides. `FrameDifference` ("once every 1/60 s") answers the same way.
- **Frame time inside a physics step reads as it does at 60.** Code in `FixedUpdate` and trigger or collision messages
  scales by `framestep`/`TieFramerate`, which follow the render frame: at 240 fps conveyor belts, wind and the
  safe-respawn point would have run at a quarter strength. There, `TieFramerate(x)` returns `x` and `framestep` 1.
- **The tapping-key action command** reads the target frame rate, which the row sets to the rate that results.
- **Pitfall, the mod half-loaded:** the first build read method bodies with a Harmony call that needs
  `System.Reflection.Emit.ILGeneration`, which this game's Mono lacks; the exception aborted the plugin's `Awake` and
  every feature after it, hot reload included (a restart was needed). `PatchProcessor.ReadMethodBody` needs no such
  assembly, and the row's setup now catches its own failure and stays off.

- **The rest, one site at a time** (`FrameSites.cs`). The audits' list: a counter that ticks once a frame, a fixed amount
  added each frame, a smoothing step with a fixed factor. Each site is patched on its exact instructions, written against
  the method's IL (the console's `il`), with the count of matches it expects; a site that doesn't match exactly is left
  alone and logged. Three kinds of fix: a counter's 1 counts only on a frame that starts a new 1/60 s (and a check made
  right after it sees the counter only on that frame: the disguised enemy turns at 80 and 40, once each); an amount is
  scaled by the frame's worth in sixtieths; a factor f becomes 1 - (1 - f)^k. Per-frame blinks (`enabled = !enabled`)
  flip at most once per renderer each 1/60 s. A cutscene's `FloorToInt(a) % n == 0` on a time-driven `a` counts once
  per whole value. Text waits round up to whole sixtieths, as a 60 fps frame does.
  **Gameplay:** fishing's fish approach and nibble, the screw platform, the Wacka Worm, disguised enemies, wandering
  enemies' retries, dizzy enemies dropping, gate slides, the dig skill's aim in battle, Vi's hover, the map's culling
  grace. **Scenes:** the battle drop, return from digging, two scenes' turns (26, 99) and a fade, text waits. **Looks:**
  spins, sprite turning, the dig spin, followers catching up and braking, the Watcher's eye, the battle EXP counter,
  damage numbers, the enemy beemerang, particles, blinking. **Left as they are** (cosmetic): random jitter, some battle
  skills' spin effects, HUD numbers counting up, fleeing losing a berry a frame sooner.
- **How the logic is checked without the game on screen.** Every fix rests on two measures: what a frame is worth in
  sixtieths, and whether it starts a new sixtieth. The console's `rates` sums both over a few seconds: at 240 fps,
  59.88 sixtieths and 59.78 new-sixtieth frames a second (2026-09-27), so everything built on them runs as at 60 (a
  long frame counts as at most one, as at 60 fps). What each site does on screen still needs the tester.
- **Installed only when the row is on:** with it off nothing of the game is patched. Installing takes about 4 s, 3 of
  them for the battle's action coroutine (one enormous method). The methods to patch come from a fixed list; the
  console's `fpsscan` reads all 4111 of the game's methods and compares (2026-09-27: nothing missing, nothing stale).
- **Pitfall, a transpiler that throws poisons its method.** The first site build used `CodeInstruction.labels`, which
  this game's HarmonyX lacks. The failed transpiler stayed registered on its methods, so the next feature to patch one of
  them (the pause menu's Settings rows, on `PauseMenu.Update`) failed with it, aborting the plugin's `Awake`, hot reload
  included: every later build sat unloaded, which looked like fixes that changed nothing. Found by reading the error at
  the end of the log, after two guessed fixes failed the same way. Only a game restart clears it; the sites'
  transpiler now never throws (it returns the method unchanged and logs why).

**Ten pips, and Monitor by default (2026-09-28).** A tester played on a 180 Hz monitor, where 120 and 144 divide
nothing (a limit without VSync, so tearing) and only 240 synced (at 180), with nothing on screen saying so. Other
common rates (165, 170, 200, 360, 480) had the same gap. So the row was made to work like the volume rows:
ten pips, the first Off, then 90, 100, 120, 144, 165, 180, 240, 360, and **Monitor** last, which is the display's own
refresh rate (`Screen.currentResolution.refreshRate`) met with VSync, so any display is smooth without tearing. At 60 Hz
or less Monitor keeps the game's own setting, and the row's line says so. The row stops at its ends, as the volume rows
do. Monitor is the default (2026-09-28, once shaky text was fixed and no odd combat had been seen), so a fresh
config runs above 60 while Archipelago is on; a config that already says Off keeps it. The first frame with the row on
installs the frame sites (a few seconds), so on a fresh config that pause lands on the main menu. Rates above 240 are
untested. **Seen on screen (2026-09-28):** the ten pips look and work fine.

**Status:** in progress, experimental (the row says so). Seen on screen (2026-09-27) at 240: smooth, the "!" steady and
sharp. The logic measured (`rates`); each site patched as expected (the log's `[fps] frame sites`). Not yet seen on
screen: every site above, most of all fishing, the screw platform, the Wacka Worm, a disguised enemy and the dig skill.
Platforms and bridges: fixed and seen (2026-09-27), a slight shimmer on them left.

*Code: `FrameRate.cs`, `FrameSites.cs`, the row in `ApMenu.cs` and `QualityOfLife.cs`; the console's `display`, `fps`,
`interp`, `camlerp`, `frames`, `trace`, `cams`, `il`, `rates` and `fpsscan` (`DevConsole.cs`).*

## 25. Hitches: the mod's garbage and the game's 5-second collection

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
that it's gone is still to come.

*Code: `ClockCleanup.cs`; `LocationChecks.cs` and `ShopSwap.cs` (`Copies`); the console's `frames` (`FrameRate.cs`).*

## 26. Field abilities as items: the game asks the bag

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

**Status:** built (2026-09-27), not yet seen in game.

*Code: `Abilities.cs`; the receiver in `ItemReceiver.cs`, the key items in `CustomItems.cs`, the looks in
`ItemSwap.Looks.cs`, `slot_data` `ability_items` in `ApConnection.cs`, the Warp in `QualityOfLife.cs`.*

## 27. Attack boost: +1 on every hit, the way the game adds its own

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
seen working, and removed the same day because it cost too much for what it showed. Kept here as a record of the process.

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

## 29. Save crystals by the confirm button, as an NPC is talked to

With Shuffle Field Moves (the Archipelago side, build step 21), the party may have no field attack for a long time, and
the game only starts a save crystal when an attack hits it: no save and no heal until a move item arrives (a gap
spotted 2026-09-28).

**Decided (2026-09-28):** confirm next to a crystal uses it, "like talking to an NPC", not touching it (a
prompt each time you brush past one); always, in a seed, whether or not a move is in hand. Hitting it still works.

- **Read how the game does it first** (`MEASURED.md`, "Save crystals, saving, Game Over and room transfers"): the hit
  bounces the crystal, plays its sound, heals if it's yellow, shows the hit sparkle, then opens the save prompt. The
  heal is in the hit, not in the prompt.
- **The confirm button jumps next to a crystal**, because the game only talks to NPCs, and the crystal drops in and out
  of the player's talk list. So the mod finds the nearest crystal itself, within the game's own reach (squared distance
  30, the hit's check), on the same inside/outside view, and only when no one to talk to is in front.
- **The jump's prefix** (already there for Shuffle Jump, `FieldMoves.cs`) asks first: a crystal in reach takes the
  press and runs the game's hit steps in order, the prompt last; the save is the game's own. No jump, no buzzer.
- **The "!" over the player** while in reach, the one the game shows next to something to check.
- Red DeadLander crystals are left alone (their hit turns a DeadLander). Only while Archipelago is enabled.

**Status:** built (2026-09-28), not yet seen in game.

*Code: `SaveCrystals.cs` (`InReach`, `TryUse`, `Tick`); the call in `FieldMoves.BeforeJump`.*

## 30. Healing crystals: every save crystal yellow

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
- **The Gameplay page grew a row:** its rows now spread between the same top and bottom row as Quality of life's do.
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
  (no scene, text box, menu, battle or transition, on the ground). A map's auto-event starts as soon as the player is
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
