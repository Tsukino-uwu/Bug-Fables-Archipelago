# How the Bug Fables mod is being made

This is the story of building the **game side** of an [Archipelago](https://archipelago.gg) randomizer
for Bug Fables: the mod that runs inside the game, step by step, in the order it happened. It's meant for
anyone curious about the process, or thinking of doing the same for another game.

- **The Archipelago side** (the apworld, seeds, the server, connecting, items and checks) has its own
  guide: [apimplementation.md](apimplementation.md).
- **Facts about how Bug Fables works inside** live in `MEASURED.md`.

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

## Where it stands

Each step's status is its last line (**Status:**), before its *Code:* line. What's next and the known issues
are in [apimplementation.md, "Where it stands"](apimplementation.md#where-it-stands).

## Keeping this guide honest

A step-by-step guide is only useful if no step is missing, so the project enforces it: any commit that
changes the mod, the apworld or the dev scripts is refused unless it also updates this file or
[apimplementation.md](apimplementation.md) (or says, explicitly, that nothing about the process changed).
That check is a small git hook, `.githooks/commit-msg` (its neighbour `.githooks/pre-commit` refuses
personal paths and names). Each step below ends with a short *Code:* line naming the files and methods to
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
- **What the player sees** (the user, 2026-09-24):
  - **At your own find** (a pickup, or an NPC handing something over), the game shows **what's really
    there**: a Bug Fables item (yours, or another Bug Fables player's in the same room) with the game's own
    sprite, and any other game's item as the **Archipelago icon**. To know what's there before it's found,
    the mod asks the server first, a "scout", without creating hints (`create_as_hint` 0, see
    `client-requirements.md`).
  - **A small Archipelago chat feed in the bottom-left corner.** It never stops play. It shows the items you
    send and receive, in Archipelago's own wording ("Player1 found their Hammer (Location)", "Player1 sent
    Hammer to Player2 (Location)"), and players connecting and disconnecting. It's **on by default**, with an
    on/off switch in the Archipelago panel. Items arriving from the server show up only there, never as a
    popup. **Later, it becomes a real text client** (the user, 2026-09-24): a key opens a text line over
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

**Status:** done.

*Code: `mod/BugFablesAP/Plugin.cs` (`Plugin`, a BepInEx plugin: `Awake` sets everything up, `Tick` runs
every frame); the project file is `BugFablesAP.csproj`.*

## 5. Make changes load without restarting the game

Restarting the game for every change is slow, so before any real feature we set up **hot reload**:
change the mod, and it swaps itself into the running game in a second or two.

This took some detective work. The standard tool for it (ScriptEngine) relies on a feature this game's
runtime doesn't have, and it failed silently. Turning on more logging showed the real error. The fix was
small: the mod checks its own file once a second and asks for a reload when it changes.

**Lesson:** when something silently does nothing, make the invisible errors visible before guessing.

**Build, then copy.** `dev-scripts/stage-dev.ps1` builds the mod and stages it inside the repo, in `stage/`,
laid out like the game folder. Copying it into the game is a separate step (the user, 2026-09-24): the build
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
(the user, 2026-09-24). A dev console, off by default, on F9, warps to a map with the game's own door warp and
then stands the party next to the entity with a given flag. So `loc 5` goes straight to location 5's pickup,
because the seed already says which map and flag that is. It can also drop a pickup next to you with the
game's own dig-spot function, and show or set a flag. Before building it we checked the game's leftover test
room: it has debug helpers, but they run unknown old scripts and can't put you by a location. See
`development.md` for the commands.

The user then asked the agent to drive it, so the console also reads a **command file** (`DevCommandFile`):
each line written there runs as if typed. The agent writes the file in its own scratch folder, never in the
game's, and reads the answers in the log (`[dev]` lines). `copy-dev.ps1 -DebugSet DevCommandFile=<path>` sets
it.

**It found a logic bug on its first run.** `loc` put the party by a pickup the apworld had as open from the
start, and the user saw it was inside a house that opens later in the story. The entity dump hadn't kept which
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
walk never finished, the transition never ended, and the game kept respawning the party there (the user
restarted the game to get out). So a warp now aims at the item's own spot, which is standable since the item
rests on it, guards the item from being taken the moment the map exists, and only after the transition steps
to a side with room and safe ground (no wall, no water, spikes or pits), else to the save point. `unstick` also
ends a stuck walk now.

**Status:** done: hot reload, the build-and-copy scripts and the dev console are in use.

*Code: `DevConsole.cs`.*

## 6. Watch the game while you play ("probing")

Reading code tells you what *can* happen. Watching the game tells you what *does*. We added small,
read-only **probes** to the mod. They never change the game, and they write to a log:

- one logs every **key item** that arrives, and every **flag** the game sets (flags are how the game
  remembers what you've done);
- one logs every **item script** the game runs when it hands something out.

Then the user simply played, and told us when they found something. Each find showed which flag the game
sets for it. That flag is how the randomizer will recognise "this spot is done". It also sorted pickups
into kinds: ones that come back after you leave an area can't be randomizer locations, and one-time ones
can.

**Lesson:** the live log caught a mistake in our reading of the code (every item number was off by one),
which is why things are measured, not just read.

When something left no trace in the log at all, we compared two of the user's saves instead: one from before
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
can't fall, event on death), since that table is game data the code doesn't hold. Run at the title screen
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

**Status:** done: the script, entity and map dumps are in use; making an entity exist early built, not yet seen in game.

## 8. An Archipelago menu inside the game

Players need to type a room address, a slot name and maybe a password, so the mod adds **"Archipelago"** to the
game's main menu. It opens a panel drawn with the game's own box and font, so it looks like part of the game,
but it takes real typing: the game itself never reads typed text (its name screen is a letter grid), so the
mod reads the keyboard itself. Backspace, Ctrl+V to paste and Ctrl+C to copy all work. The same panel switches
**the Archipelago mod** (enabled or disabled), which keeps randomizer saves in their own folder so normal saves are never touched.
Its rows, top to bottom (the user's order, 2026-09-24): Address, Port, Slot, Password, Difficulty, Detector,
**Archipelago** (the mod on/off, just "Archipelago"; the config's `RandomizerEnabled`). No Back row: cancel backs out, as the hint box says.
Under them, one line explains the highlighted row (the user, 2026-09-24: "Detector" alone didn't say it means
the medal), then the connection's state. The game's text colour 5 draws light blue here, not grey, and a long
coloured line looked tilted, so both lines are plain black. The choice rows use the settings screen's own
pieces: its arrow sprite on either side of the value, made once when the panel opens (button prompts rebuilt
on every cursor move replayed their pop-in, so they seemed to shift), and its value-change sound, `Confirm0`
on channel 10, instead of the cursor's scroll sound (the user, 2026-09-24).

Several things went wrong on the way, each found on screen by the user:

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
  Start Game (the user noticed). The game rebuilds its menu on the way back, and the rebuild resets the
  cursor to the top. Our panel doesn't rebuild the menu, so it now resets the cursor itself, as the user chose
  to match the game.
- **The leaf sat far left of the labels**, next to the game's Settings screen. The first two nudges were
  measured from single cropped screenshots and went the wrong way ("Address" ended up on the vine border). What
  worked was the user's **side-by-side screenshots** of both screens: Settings starts its labels ~88 px in from
  the vine, with the leaf's tip right before them, so the labels moved to that distance with the leaf beside them.
  **Lesson:** compare against the real thing in one view, not against a number read off another picture.
- **The leaf didn't wiggle** like the game's (the user noticed once it sat beside its label). The game gives its
  menu cursor a `SpriteBounce` component when it creates it; the panel's leaf now gets the same one.
- **No sound opening or closing the panel**, where Start Game and Settings have one (the user noticed). The
  game plays "Confirm" for every main-menu choice before acting on it, and our entry takes the press first,
  so it skipped the sound. The mod now plays the same "Confirm" on opening, and "Cancel" when backing out with
  the cancel button, the sound the game uses leaving the file select. Choosing the panel's "Back" line plays
  "Confirm", like any other menu choice.

**Lesson:** when adding to a game's own screen, find every time the game rebuilds that screen, and everything
else that keeps running while another screen is on top of it.

**The file select waits for the first login** (the user, 2026-09-24, choosing this over keeping a copy of the
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
   the save slots and was hard to read (the user's screenshot, 2026-09-24).
3. "Logged in" is a flag the connection sets when `slot_data` arrives. It stays set after a drop, so the rules stay
   in force offline once the seed is known.

*Seen on screen by the user (2026-09-24): with the server down, a save and a new game were both held back with
the popup (keyboard and gamepad hints); once the server was back and the mod logged in on its own, the same save
loaded normally.*

The user then asked for it to feel like the game's settings screen, so the mod rebuilds that screen's look
from the game's own pieces, read from how the pause menu builds it: the same orange box, the controls box
above it with the game's button hints, the game's leaf cursor, labels on the left and values on the right, and
arrows around the On/Off value.

**The help line follows the value (the user, 2026-09-26):** on a row whose values mean different things (Difficulty,
Item animation, Shop prices, Enemy scaling), the line under the rows describes the value now chosen, and changes as
left/right steps through them; every step redraws the screen. On/off rows keep one line.

**Three pages (the user, 2026-09-26; built, not yet seen):** the main page keeps the connection and the Archipelago
on/off, plus two links, *Quality of life* and *Gameplay*. Gameplay holds how the game plays: Difficulty, Enemy scaling
(moved from Quality of life; its config key stays under `[QualityOfLife]`, so a saved choice carries over) and
Detector. Quality of life keeps the speed-ups, with Disable all / Reset to defaults on top (step 10). Cancel backs out
of a Yes / No first, then out of a page, landing on that page's link. `ApMenu` tracks the page as an enum.

**Status:** works, seen by the user (2026-09-24): the menu entry, the panel, and the file select held back until the first login.

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

**Tested by the user (2026-09-24):** a seed with the Explorer Permit placed on Artis's medal (Archipelago's
item plando). Talking to Artis showed "You got the Explorer Permit!" with the permit's sprite, and on screen
no medal was added and no second permit appeared. Three leftovers of the medal were visible, so the patch
grew to cover them: **the description box** (the game opens it just before the sprite, now with the real
item's description, or none for another game's item), **the starburst colour** behind the sprite (now the
real item's kind, or its Archipelago colour for another game's item), and **the first-medal tutorial** that
followed. That one is skipped, because no medal was given, and flag 31 stays unset for the real first medal.
The user then confirmed all three on screen with the G-Bug Ranger Plushie placed there instead: its sprite,
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

**Tested by the user (2026-09-24):** a medal pickup in Snakemouth Den with the G-Bug Ranger Plushie placed on
it (plando). On screen: "You found a Bug Ranger Plushie!", its sprite held up, its description, the key-item
starburst. The log: the medal kept out, the game set the pickup's flag, the check was sent, and the Plushie
came back from the server into key items; flag 31 never flipped, so no first-medal tutorial.

**On the ground too** (the user noticed the pickup still looked like its vanilla medal before it was touched).
A few times a second the mod gives each pickup location on the current map the sprite of what's really there,
placed the way the game places an item's sprite. The game redraws an item's sprite only when its item id
changes, so the swap holds. Another game's item keeps the vanilla look until the Archipelago icon is in the mod.
**Confirmed by the user (2026-09-24, screenshots):** the Snakemouth medal pickup lay on the ground as the G-Bug
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
a flat sprite left it half in the ground (the user's screenshot of the berry outside the cave). The swap now lifts the
sprite by half its height, as the game does for items, stops the spin and squares the sprite to the camera. The user
then saw it still, but the berry model still stood on top, and a second guess (that the model is added twice) failed
the same way. So, measure instead of a third guess: a new dev console command, `tree`, logs the nearest pickup's
whole object tree (each object, active or not, and its renderers, on or off). It showed one berry model, active, with
the item sprite set. The reason is one line in the game: every frame, the first child of the sprite (the model) is
made active exactly when the sprite is enabled (`EntityControl.cs:2781-2786`), and showing the item needs the
sprite enabled. The game toggles the object, never its renderers, so the swap now switches off the model's
renderers, on every pass. `tree` then showed both berry renderers disabled with the item sprite on. **Confirmed by
the user (2026-09-25, screenshot):** the seed's Mistake standing on the ground outside the cave, no berry, no spin.

**Planned (the user, 2026-09-26): another player's item says it was sent.** Seen in play: Artis's gift held
another player's Sword and the box read "You got the QuestTester's Sword!", which the user took for their own item.
The sentence is the game's menu text 106 ("You got ..."; 110 in one branch), with the article (`flagstring[1]`) and
name (`flagstring[0]`) that the mod already swaps (`MainManager.cs:11490-11564`). For another player's item, the mod
will swap in its own line for that one box and put 106 back after. **Wording (the user, 2026-09-26): "You found
QuestTester's Sword!"**; the player's own items keep the game's "You got ...". First step:
log text 106 in the running game to see its placeholder syntax, not guess it.

**Planned (the user, 2026-09-26): another game's item shows its type before you take it.** On the ground and on a
shop shelf, another game's item still shows the vanilla item's sprite today, which reads as the vanilla item. It
will show an Archipelago icon on a backdrop in its type's colour (Archipelago's: progression plum, useful blue,
trap salmon, filler cyan), the same colours the starburst already uses at pickup. It's a Quality of life row, on
by default, for players who'd rather be surprised. Bug Fables items that belong to another Bug Fables player keep
their real sprite, and the owner's name is in the text. Before adding the icon: which image file, and whether
it's fine in a public repo forever (Archipelago's licence row covers its code; check the image).

**Status:** works for gifts, pickups and their ground sprites, and respawning pickups seen by the user (2026-09-24, `MEASURED.md`), and crystal berry spots (2026-09-25); berry rewards and story pickups built, not yet seen in game.

*Code: `ItemSwap.cs` (`Enable` finds the routine, `Transpile` rewrites it; `Decide`, `DescWindow`,
`Recolour` and `FirstMedalSeen` do the swapping; `PickupPrefix`, `FindPickup` and `TickGround` handle pickups); the
scout is `ApConnection.Scout`.*

## 10. Quality of life: a quicker, smoother game

The user asked for a way to skip the intro, the tutorials and other slow parts, as a sub-menu of on/off rows
(2026-09-25). The page isn't only for skips: the user named a later row that changes play, a pause-menu button
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

The rows, all On by default (the user, 2026-09-25) and active only while the Archipelago mod is enabled:

1. **Fast text.** Each frame a dialogue box is typing, the mod sets the game's own `skiptext`, under the same
   conditions holding the button needs (a box open, no prompt or list, not `|noskip|`, on the newest line). Each box
   still waits for a press. **Confirmed by the user (2026-09-25):** "text appears instantly".
   With the text instant, holding the skip button still felt slow (the user): the game's hold advances a box, then
   waits out a cooldown of 16 frames (10 when a new dialogue opens). So while the button is held on a skippable box,
   the mod cuts that cooldown to 4, the game's own value while a box is typing; the game's hold code still does the
   advancing. It was a row of its own, "Turbo skip", until the user felt the difference and folded it into Fast text
   (2026-09-25).
2. **Skip intro.** The four story slides at the start of a new game run inside the new-game event, so they can't be
   cut out. While they're on screen (the event is running and its black backdrop exists), the mod answers each
   line's wait and runs the game at 8 times speed. The game's own end-of-event resets the speed, and the mod does
   too once the backdrop is gone. **Confirmed by the user (2026-09-25):** on a new file the slides "skipped past
   really fast on its own"; the log shows `[qol] intro slides: passing them by`, then `over: normal speed`.
   **Replaced (2026-09-25):** the slides are now cut out after all (item 5, the opening), and the row was folded into
   *Skip cutscenes* (the user: "can probably just be bundled"). The speed-up stays as a fallback if the cut misses.
3. **Free boat** (the user, 2026-09-25: nobody should have to farm berries in Archipelago). The Metal Island boat
   costs 300 berries (90 in a later state). The fare isn't in the boat scene but in the sailor's dialogue lines, so
   ScriptDump got a money column (`checkmoney`, `money`), which found both fares on the pier, lines 16 and 19, each
   `|checkmoney,N,20||money,-N|`, and a free trip back. The line a prompt jumps to is read inside the running
   dialogue through `MainManager.GetDialogueText(id)`, not through a new `SetText`, so a postfix there drops the two
   money commands from those two lines. It changes no reachability: with it off, the fare can always be earned in
   battle. Built, not yet seen.
4. **Warp button** (the user, 2026-09-25: a fifth pause-menu button, "warp to start", with a yes/no before it
   acts). The pause menu's row is window 0: `maxoptions` icons (4, or 2 in battle, so the button never shows there),
   made as `sprites[13 + n]`; confirm opens window `option + 1`, and the labels are `menutext[10 + option]` and
   `[50 + option]`. So the mod respaces the four icons, adds a fifth, raises `maxoptions` to 5, catches confirm on it
   before the game would open a "window 5", and writes its own labels. **First try threw every frame:** the game's
   `IconAnim` is handed exactly the four icons and indexes them by option, so the fifth option ran off the end (the
   user: "got a lot of errors"); a prefix now hands it five, and the game animates the fifth like the others. On Yes
   (No is preselected), the menu closes the game's way (`PrepareExit`) and the game's own `TransferMap` takes the
   party to the Outskirts, beside the save point where a new game begins. **The icon** (the user: "look at how the
   other menu buttons do things, and do the same"): a tinted Settings icon with the map item on top looked wrong, so
   a new dev dump, `SpriteDump`, saved the game's GUI sheets and a table of `guisprites` indexes (into the BepInEx
   folder, never the repo), and a contact sheet of them showed a round icon in the same style, `guisprites[34]`
   (a blue map). The button is now made exactly like the other four: one sprite, no tint, no overlay. A plugin
   reload with the menu open had left the old icon behind the new one; unloading now removes it. Title "Warp" (the
   user). **Seen by the user (2026-09-25, screenshot):** five matching icons, "Warp" above them, the description line,
   and the Yes / No box with No preselected ("this looks good"). The warp itself is still to see. **A second
   IndexOutOfRange, the mod's own this time:** coming back to the main page from another, the menu briefly still holds
   that page's shorter sprite array (8 to 12 long; window 0's is 19), and the button's per-frame check read slot 16 of
   it. It now checks the length first. Lesson: a prefix on a menu's `Update` sees every page's state, not just the one
   it was written for.
   **The logic never counts on the warp** (the user, 2026-09-25): it's fast travel and a way out when stuck, but a
   seed must not assume players teleport out, so every one-way drop still needs a real way back in the logic.
5. **Skip cutscenes** (the user, 2026-09-25: scenes and fluff that give no checks, starting with the two at the
   Snakemouth bridge). Every scene starts through `EventControl.StartEvent`, so a prefix there sees each one by its
   event number and map. Each scene is read in full before it goes on the list, and it gets one of two treatments:
   *skipped* when it only moves the camera and party, talks and sets flags (the mod sets those flags and the scene
   never starts: the bridge message, Event0, flag 11), or *fast-forwarded* when it also changes the world in ways its
   flags don't cover (the game runs it at 8 times speed with its lines answered, as for the intro slides: the rope,
   Event1, which plays the bridge's Fall animation and fixes it fallen before setting flags 7 and 11; setting the flags
   alone would leave the bridge standing until the room reloads). Never a scene that gives an item, sends a check,
   changes the party or starts a battle. **First skip froze the player** (the user at the bridge, 2026-09-25): a trigger
   freezes the player (`minipause`) before starting its scene (`NPCControl.cs:5512-5525`), and the scene's own
   `EndEvent` unfreezes. A skipped scene never ends, so the skip now calls the game's `EndEvent()` itself, which is all
   resets (`EventControl.cs:146-187`). Not yet seen.
   **The opening is the one exception, on purpose** (the user, 2026-09-25: "just start playing the game"). After the
   slides you play Kabbu alone inside the starting building, and one scene, Event16, stands between you and the door
   (its trigger, entity 9, is hidden by flag 15). It holds Maki's talk, Vi joining, the tutorial battle, the Explorer
   Permit (location 1) and Kina's and Eetl's talk: an item, a battle and a party change, so no flag list could skip it.
   So the scene never starts: as soon as the player is free on that map with flag 15 unset (not only at the trigger,
   which the user stood clear of, taking it for the scene), the mod leaves what its end leaves (`EventControl.cs:3598-3822`),
   through the game's own calls: `ChangeParty({0, 1})` and `SetPlayers` (the `addleif` method), the tutorial's Crunchy
   Leaf, Vi's stand-in and the `blockingbox` destroyed, the exit (entity 2) active again with the default camera, flag 15
   and quest 11 on the board. Flag 15 sends location 1's check, and a hold-up shows the seed's item. The logic needs no
   change: Vi is in the party either way, and location 1 was already reachable from the start.
   **First tries (2026-09-25):** (1) the opening waited for its trigger, and the user stood clear of it, taking it for
   the scene; now it runs as soon as the player is free. (2) The trigger then stayed in the room (flag 15 hides it only
   on a map load), the scene started anyway, since the block only covered "flag 15 unset", and crashed looking for Vi's
   stand-in the mod had removed (freed with `unstick`). Now Event16 is refused on that map whenever the skip is on, and
   the trigger is hidden. (3) The talk after the slides is still Event8, which only the slides' speed-up covered; that
   part (talk and party moves, no prompt) is now fast-forwarded too.
   (4) Still seen: the building and the sped-up talk, since Event8 loads the building and plays there, and the opening
   (or a test start's warp, `TestStart`, which worked: the party arrived in the plaza by its save point) can only follow
   it. A black screen until the start was tried and dropped (the user: hiding it "looks dumb"). (5) So Event8 is cut
   right after its slides: its first step after the slides' backdrop goes is `ChangeParty({1})` (Kabbu alone), while Vi
   is still in the party. A prefix refuses that one call and stops the scene (`StopCoroutine("Event8")`, since scenes
   run as `StartCoroutine("Event" + id)`); the next frame the mod ends it as its own end does (HUD, camera, the
   building's music, `EndEvent`, the fade-in), and the opening and any warp follow before a single line of talk.
   Seen (the user, 2026-09-25): straight into the town. (6) The slides still showed, and the test start's warp stepped
   to the save crystal afterwards (the console's warp looks for a spot beside a save point). With *Skip intro* on, the
   cut now comes before the slides, at their first step, the backdrop `NewSolidColor("back")` made after the building's
   map has loaded (`EventControl.cs:2655`); the later talk cut stays as a fallback. The test start uses the game's
   `TransferMap` alone.
   (7) Seen: no slides, but the bottom of the building showed before the warp: the mod removed the slides' black backdrop
   when ending the scene, and the transfer only started after the opening. Now, with a test start, the backdrop stays up,
   the transfer starts as the scene ends, the backdrop goes once the start map has loaded behind the transfer's own
   fade, and the opening runs there. Its building-only steps (the exit, entity 11, the trigger) run only in the
   building, since the same entity numbers are other things on other maps.
   Seen (the user, 2026-09-25): the spawn in the town looks right; only the building's music played briefly, so with a
   test start the scene's end no longer starts it.
   (8) The test start put the party behind the plaza's statue: `TransferMap` with position zero is the map's origin.
   **Decided (the user, 2026-09-25): a start arrives as if through a door**, the way random starts will work. A door
   holds its target (`data[0]` the map, `vectordata[1]` where the party appears, `vectordata[2]` where it walks,
   `NPCControl.cs:5461`), and it lies on the map left behind, so the mod reads it from that map's entity table at the
   positions the game's parser uses, and hands those spots to `TransferMap`.
   **Seen (the user, 2026-09-25): "looks perfect"**: a new file goes from the main menu straight to the town's gate
   from the Outskirts, with Vi and Kabbu and the first check's item, no slides, talk, fight or building on the way.
   **The rule since (the user, 2026-09-25):** a scene that gives an item may be skipped *as long as the item can still
   be received*, and fewer cutscenes are preferred, as an option at least. So a skip now has to keep every check the
   scene holds (sent by the mod, or moved to something the player still does). Next candidate, the user's idea: the
   spider fight with Leif and its scenes, with the discovery granted on entering or leaving the room instead. Not read
   yet: that fight is also the first boss (its prize medal, flag 41 and what gates on it), so each of those needs a home.
6. **Item animation** (the user, 2026-09-25): a discovery showed nothing of what it found, and items from other
   players arrive silently. Your own finds always get the hold-up (pickups already did; a discovery recorded in play
   now does too); the row, *Item animation: All / Progression / Off* decides which items from other players do
   (default All: the user's choice once bursts were fast with the skip button held). The hold-up is the game's own `giveitem`, run on a key item stand-in (an ordinary item's
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
   frame: a chain of scenes and fights (the spider fights) can leave a free frame between links (the user's point).
   **Seen (2026-09-25):** three queued test hold-ups waited through the spider fights' chain, then played one after
   another, reading "You got the Explorer Permit from TestPlayer!" (the game's own article for it). Each was followed
   by an empty box: an empty follow-up line is still shown as a box waiting for a press. The follow-up is now the
   game's `|end|`, which skips that wait. Confirmed by the user the same day: no empty box.
   **Bursts** (the user queued 50 to see what *All* feels like when a multiworld sends many at once: about a minute of
   boxes). Only the first of a burst waits for the settled half second; the rest follow as soon as the previous box
   closes. A summary box ("...and N more items from other players!") past three was tried and dropped: it felt off to
   the user, so every item gets its own box. Instead, **holding the skip button runs the game at 4 times speed while one
   of the mod's hold-ups is on screen**, since the item-get's own pauses are fixed waits that fast text doesn't shorten;
   only a speed-up the hold-up made is undone. **Seen (the user, 2026-09-25): "better"**, and *All* became the default.
   **Replays stay silent** (the user asked what a new save does): a new save starts at 0 received and the mod gives it
   everything the server has for the slot, oldest first, which is what makes a lost save recoverable. On *All* that
   would be a hold-up for every item another player ever sent. So only items past the count the server had at login
   (`ApConnection.ReceivedAtLogin`) are held up: a new save catching up, or a reconnect, is silent; items arriving
   during play are shown. Items sent while the player was offline come in silently too (the user accepted that). Not yet
   seen with a second player.
7. **Shop prices** (the user, 2026-09-25: Normal by default, Half or Free). The medal table's price columns (5 for
   berries, 7 for crystal berries) are scaled in memory, from a kept copy, and put back when the row is Normal or the
   mod is off. The logic never counts on it.
8. **Skip battle tutorials:** next. The tutorial battles end on fixed turns and read story flags, so each one is
   read in full before anything is skipped.

The panel got an eighth row, "Quality of life", which opens a second page in the same box; cancel comes back.

**Disable all and Reset to defaults (the user, 2026-09-26; built, not yet seen).** Two buttons side by side at the top
of the Quality of life page (not rows in the list); left/right picks one. Confirming turns them into Yes / No, with No
picked first so a stray press never wipes the settings. Disable all turns every row off (a choice row
to its off value: Enemy scaling Off, Item animation Off, Shop prices Normal); Reset to defaults puts every row back to
its default (`QualityOfLife.DisableAll` / `ResetAll`, the defaults from each setting's own config definition).

**Status:** in progress: Fast text, the opening skip, the Warp button's menu and Item animation seen by the user (2026-09-25); Free boat, the bridge skips, the warp itself, Shop prices and silent replays with a second player not yet seen; Skip battle tutorials planned.

*Code: `QualityOfLife.cs` (the settings and the per-frame speed-ups), `ApMenu.cs` (the second page),
`WarpButton.cs` (the Warp button), `HoldUps.cs` (item animation's hold-ups).*

## 11. Playing with fewer party members: stand-ins and followers

Bug Fables' scenes are written for a party of two or three, but a seed can start with one member and add the
others as items. So every scene, talk and follower has to cope with a member who isn't there: the mod fills the
gaps with invisible stand-ins, and the problems below were found in play, one at a time.

**Scenes with a member missing get a stand-in** (the user, 2026-09-25: make scenes work with one or two members;
the barkeeper's first talk, Event83, crashed twice on `p[2]` with Vi and Kabbu). Scenes take the party as a list and
use fixed slots (about 110 lookups). While a scene runs, `GetPartyEntities` returns three: each missing member is
an invisible, collision-free stand-in with that member's `animid`, made the way the game makes scene characters,
in the member's own slot (id order) or after the party, and removed when the scene ends. Outside scenes nothing
changes, and no scene tests a member with `GetEntity(-6) != null` (grep). A member asked for by name during a scene
(`GetEntity(-4)` to `(-6)`) gets the same stand-in. The user asked why not the leader, as for followers: a scene
moves every member at once, so the leader would be pulled to two spots and play another character's animations. Limits: a scene that changes the party, or
needs a member's ability, still needs the member (a logic rule, as for the boat); some scenes will look odd, and each
one that used a stand-in is logged, to skip or hold back one by one. **Seen (the user, 2026-09-25):** the barkeeper's
first talk played through with Leif's stand-in (naming Leif, as expected), so it went on the skip list, only while
its flag 158 is unset: the same scene later takes bounties and gives their rewards.

1. **Without a test start, the party stood under the house** (the user, 2026-09-25: "the weird broken location" seen
   briefly before step 10's (7) fix). Event8 places the party only after its slides (Kabbu 2.5 left of entity 4,
   `EventControl.cs:2770`), so cut before them the party kept a new game's raw spawn point; every test since step 10's (6) had a
   test start, whose warp moved it away. The opening now stands the party where the scene would have. Seen (the user):
   right spot, but the fade-in first showed the spawn point, then a jump: the opening runs a few frames after the scene's
   end, and the fade-in starts at that end. So the scene's end (the mod's) moves the party there and snaps the camera
   before the fade-in. Seen with only the first half loaded (the user): the right spot, then a snap back once the opening
   ran, since it waits for the fade-in to end and the player can walk during it. So the opening no longer places anyone:
   it uses where the player stands. Seen (the user): right spot, no snap, but outside the house the camera was broken
   with Leif alone. `ResetCamera` at the scene's end aims the camera at the leader, the opening's `ChangeParty` then
   destroyed that character (with Vi and Kabbu, Kabbu's was reused), and leaving the house hands the camera back to the
   player only for insides that centre on themselves (`MapControl.cs:1373`). Event16 itself ends with `ResetCamera()`
   after its party change (`EventControl.cs:3795`); the opening now does too. **Didn't help** (the user: still low outside,
   and stuck after the gift). Two guesses failed, so measured: a console command `cam` logs what the camera follows. It
   read `target DESTROYED` with the leader (`Player 0`) fine. Unity destroys an object at the end of the frame, so the
   opening's `ResetCamera`, aiming at `MainManager.player`, still found the old leader's character and followed it as it
   vanished, leaving the camera where it last stood, under the house. The opening now aims the camera at the new
   leader's character itself (`playerdata[0].entity`). Seen (the user): the camera fine from the gift on, but Vi and
   Kabbu showed for a moment and the camera was odd outside until then: the opening swaps the party only once the
   fade-in is over. Moving the swap into the scene's end, before the game's `EndEvent`, crashed it (`FixEntities`,
   a NullReferenceException, a black screen; freed with `unstick`). Now the scene ends as before behind the black
   screen, and on the next frame the party is swapped, placed and the camera set, then the fade-in starts. **Seen (the
   user, 2026-09-25):** Leif alone from the first frame, in the room, the camera right inside, outside and after the gift.
2. **Stand-ins in conversations too** (the user, 2026-09-25, Leif alone): Artis's talk hands lines to Vi and Kabbu
   (`|next,-4|`, `|next,-5|`), and `SetText` resolves a speaker through `GetEntity` (`MainManager.cs:12385-12398`,
   `:18418-18440`). The stand-ins only answered while a scene ran (`inevent`), and a talk isn't one, so the lookup came
   back empty and `SetText` threw a NullReferenceException at the end of the talk. They now answer while a scene or a
   conversation runs (`inevent` or `message`) and go when both are over. Kept that narrow on purpose: an invisible
   member around all the time could be counted as real by battles, followers or menus. **Seen (the user):** Artis's talk
   played to the end and the permit's check went out (the crashed one had left a dead dialogue: `unstick`).
3. **A stand-in arrives at once** (the user, 2026-09-25): the horn tutorial (Event10, near Snakemouth) walks Vi to a
   spot and waits until she's there (`while (entities[0].forcemove)`, `EventControl.cs:2935`); the stand-in, without
   collision, never arrived, and the scene never reached its first line. Every `MoveTowards` overload ends in the
   five-argument one (`EntityControl.cs:4911-4960`); a postfix puts a stand-in straight on the spot and ends the walk.
   The scene's cut itself is done by the scene (`CutGrass()` after the action button), not by Kabbu. **Seen (the user):**
   the tutorial played through and its reward was sent.
4. **Stand-ins stay where they're put** (the user, 2026-09-25): the trapdoor scene ran with stand-ins, but Leif
   landed "down/left at a rock" instead of on the mushroom. Its end places member m on the m-th scene character's spot
   (`EventControl.cs:1476-1484`), so Leif took stand-in Vi's; a stand-in had its collision switched off but not its
   gravity, so it would sink through the floor. Stand-ins are now kinematic, without gravity. The likely cause, not
   measured. **Wrong** (the user: now all the way left). Read in the code instead: just before placing, the scene runs
   `PartyMover`, which walks every party member, stand-ins included, to `MainManager.player`, and with one member the
   player (Leif) isn't in the scene: he stood where the fall room put him on loading, far left. Item 3's instant arrival
   then put stand-in Vi on him, and the end put Leif on her spot. So a stand-in sent toward the real player stays where
   the scene last put it, and the positions the scene hands `SetPlayers` are logged, with the stand-ins' and the
   player's. **Seen (the user, 2026-09-25):** Leif landed at the right spot; the log: placed at (12.7, 6.5, 0.3), stand-in
   Vi's landing point, with the player at (-21.9, 0, 0) before.
5. **A stand-in has its physics body at once** (the user, 2026-09-25): the spider fight's lead-in (Event6) made a
   stand-in `Jump()` in the frame it was made, and `Jump`'s `Unfix` needs the body (`rigid`), which a new character only
   gets in its `Start`, a frame later (`EntityControl.cs:524-528`): NullReferenceException, the scene dead (`unstick`).
   The stand-in now gets its body when made, weightless; `Start` adds one only when there is none. Not yet seen.
6. **Stand-ins hidden at the last moment** (the user, 2026-09-25: their sprites flashed now and then): the
   per-frame hiding ran before the scene's step and the character's own updates, which could switch a sprite back on
   for a frame. A postfix on `EntityControl.LateUpdate` (`EntityControl.cs:3672`) hides a stand-in after both, just
   before drawing. First reload with the guard: it waited for the running scene. Not yet seen.
7. **A scene's end hands over to the real party** (the user, 2026-09-25: after the spider fight's end, Leif stood to
   the right instead of where the scene leaves the party). The end of Event6 (`EventControl.cs:2272-2289`) walks Vi and
   Kabbu to the spot and never the player, whom it takes to be one of them, and sets the fall room's character to follow
   Kabbu (`entities[2].following = entities[1]`, then `extrafollowers.Add(2)`). Now, before the stand-ins go: if the
   story's leader (the first member of the party the story last asked for, remembered by the member guard before it
   filters) was a stand-in, the real party moves to where it was left; anyone following a stand-in follows the real
   party's last member. Both logged. Not yet seen.
8. **A party member isn't also a follower** (the user, 2026-09-25: three Leifs after the spider fight, one standing,
   one trailing the player). In the story, Leif meets the party after that fight and follows until he joins at the lake:
   the scene's third character is the room's Leif (entity 1), set to follow Kabbu, and `extrafollowers.Add(2)` (ids are
   characters: 0 Vi, 1 Kabbu, 2 Leif; removed when he joins, `EventControl.cs:3533`), from which every map load makes a
   follower (`MapControl.cs:826-829`, `AddFollower`, kept in `map.tempfollowers`). With Leif the one member, that's the
   player plus two copies. Now the member guard, each frame, takes any party member off `extrafollowers` with their
   follower copies, and a scene's end removes a party member's character that followed a stand-in. On first load it took
   Leif off the list but found no copy in `tempfollowers`: the copies were the scene's own character, not made by
   `AddFollower`. **Still a copy** (the user: a second Leif copying every move). Measured with a new console command,
   `who` (every character drawn as a party member): two "Player 0", both player characters. The spider scene calls
   `ChangeParty({0, 1})` then the no-argument `SetPlayers()` (`EventControl.cs:1711-1712`), which makes new player
   characters without removing the old ones; in the story those become the scene's actors, but with stand-ins the old
   Leif stayed, controls and all. Now, outside scenes, twice a second, any character with player controls other than
   the leader's is removed (logged). On loading it removed the stray at once; `who` then listed one player. **Seen
   (the user, 2026-09-25):** no extra Leif any more.
   **The whole sequence replayed (the user, 2026-09-25): "worked perfectly"**, no extra Leif, the right spot after the
   ending. The log: the fall placed at (12.7, 6.5, 0.3); Leif taken off the follower list; the scene's end moved the
   party to stand-in Vi's spot (-44, 0, 1.2); the story's Leif ("Moth") removed as a copy; the stray player removed.
   Items 7 and 8 seen with it.
9. **The leader acts the story leader's part** (the user's wish, `apimplementation.md`): at a scene's or talk's
   first stand-in, if the story's leader isn't in the party, the real leader plays that member (walks, faces, is placed
   where they would be) and only other missing members stay invisible; chosen once per scene; in a party list by member
   the leader's own slot gets an invisible stand-in so he's never moved twice. **Seen (the user, 2026-09-25):** Leif
   acting the story leader's part in the scenes ("doing the funny animation things"), the user's screenshot of the
   treasure room scene with Leif speaking the party's line. **Then cast twice** (the user, 2026-09-25: Leif didn't move in the
   briefing, where Kabbu and Leif stand back while Vi gives the artifact to the Queen): Event45 asks for Vi, Kabbu and
   Leif by name; Leif acted Vi but the request for Leif still found him, so he followed whichever order came last and
   stayed back. Now a by-name request for the acting leader's own member gets an invisible stand-in, as his slot in a
   party list already did. **Replayed (the user):** Leif acted Vi in the palace entrance, but the scene then reloads into
   the throne room with the party remade, the actor was lost, and Leif played himself there. The user's view: that's
   right once the story has Leif ("wrong to force one member to do the others' part"). So: **a leader the story's party
   already holds plays himself** (Vi from the opening, flag 15; Kabbu always; Leif from flag 16); he acts the lead only
   in scenes whose party doesn't have him (chapter 1 before Leif joins). And after a scene remakes the party characters,
   the new leader takes the acting part on again (for chapter 1 scenes that change maps). **Seen (the user,
   2026-09-25):** the briefing replayed with Leif playing himself, "working as intended".
   The briefing's hold moved from 114 to 66 (the bridge swap; the user: a shuffled door or a random start inside the
   palace could reach it with no follower or the wrong one), `apimplementation.md`.
10. **Leif's joining scene skipped when Leif is already in the party** (the user, 2026-09-25): Event14 at the lake
   takes its Leif from the follower list (`map.tempfollowers[0]`, `EventControl.cs:3339`), empty since item 8, and threw
   `ArgumentOutOfRange` at its start (predicted from the code a moment before the user reached it; freed with
   `unstick`). A prefix on `EventControl.StartEvent` doesn't start it and leaves what it leaves: flag 16, the regional
   flag of the creature it removes (entity 5) with the creature gone, Leif off the follower list. **Then always skipped**
   with Archipelago on (the user: "it's not a check"): it's no location, only the logic's *Leif Joins* event at the lake
   (flag 16), and without its fight the lake no longer quietly needs Vi. When Leif isn't in the party yet, he joins right
   there, as the scene's own `ChangeParty({0, 1, 2})` would have him (then `SetPlayers`, the camera on the leader); with
   one starting member the guard still decides whether he may. **Moved earlier** (the user: "it could just happen after
   the spider, when Leif first starts to follow"): once the spider scene is over (flag 27, not yet 16), Leif joins for
   real, flag 16 goes on, and the story's follower Leif is removed with his follower entry. The logic is unchanged: *Leif
   Joins* is in the same region (*Snakemouth Den*) as the lake. With one starting member, only once Leif is allowed
   (received). On loading, the user's Leif-alone file got flag 16 ("Leif was already in the party"); **seen (the user):**
   the lake walked past with no scene. With a two-member start, not yet seen.
11. **Position lookups beyond the party** (the user, 2026-09-25): the droplet scene (Event21) ends by walking the
   second and third members by position (`GetEntity(-2)`, `(-3)`, `EventControl.cs:4112-4114`; `MainManager.cs:18526-18537`
   answer only inside the party) and threw on nothing. In a scene, slot k beyond the party now gets the k-th member in the
   story's order (the acting role first, then the others by id) as a stand-in. The acting leader also has a fallback
   when a reload forgot the story's party: the first missing member by id.
12. **Every way the code reaches for a party member, listed** (the user, 2026-09-25: "dump fully what a party member
   or follower is, so we know everything they could ask for"): `dev-scripts/party-access.py` counts 29 ways across the
   decompiled code, with the methods and events using each (`--where <way>`). Covered: lookups by position and character,
   the party as a list, `PartyMover`, `SetPlayers()`, `ChangeParty`, `.following`, `extrafollowers`, the leader. Open,
   since a direct index can't be intercepted: `playerdata[1]`/`[2]` (Events 52, 122, 130, 137, 138, 182, all past
   chapter 1, and `BattleControl.DoAction`/`EventDialogue`, to confirm they check the party's size), `tempfollowers[..]`
   (11 events; they read story companions, and break only for a removed party member, so far only Event14, now skipped),
   `partyorder` (Events 6, 54, 138) and `GetExtraFollower` (Event223).
   **Seen (the user, 2026-09-25):** the droplet scene replayed to its end with no crash, and the log shows item 9 at work in
   it and in the switch scene (Event23): "the leader (Player 0, member 2) acts member 0's part".

**No warnings for missing animations (the user, 2026-09-26: "dumb to leave bug/errors laying around, even if its
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

**Status:** works with Leif alone, seen by the user through chapter 1 into chapter 2 (2026-09-25); items 5 and 6, and Leif joining after the spider with a two-member start, not yet seen; the direct lookups in item 12 still open.

*Code: `PartyFit.cs` (the stand-ins and the acting leader), `PartyMembers.cs` (the member guard, followers,
Leif's joining).*

## 12. Shops in the game

Shops needed their own approach because buying isn't a pickup or a gift: a medal shop is a shelf of item entities
and a script, an item shop adds its item silently, and nothing in the save marks a purchase. So each kind of
shop had to show the seed's items and tell when one was bought.

**Medal shops as locations** (the user, 2026-09-25). A medal shop turned out not to be a menu: each shelf slot is an
item entity on the counter (`NPCControl.SetBadgeShop`), looking at one opens its description box, and buying runs the
shopkeeper's dialogue, whose script checks the money, pays, removes the medal from the stock and gives it
(`giveitem`). So: the shelf shows the seed's item's sprite; while the description box and the buy prompt are built,
the medal table briefly holds the seed item's name and description; the `giveitem` is swapped like a gift; and the
check was first the medal leaving the shop's stock, which the save keeps (no "bought" flag exists; replaced by a bit
per copy, below). **Seen (the user,
2026-09-25):** Merab's shelf showed the seed's items (two books, a leaf, a mushroom); buying the Mushroom Gummies at
medal 12's slot held them up, kept Sleep Resistance out, sent *Medal Shop 4* when medal 12 left her stock, and the
server's Mushroom Gummies arrived. On joining the seed, two medals bought earlier (before shops were locations) sent
their checks from the stock alone. **More on show** (the user): Merab's shelf holds 5 instead of 3, spread evenly across her own
first-to-last spots (a 6th past the end was hard to reach), and Shades's 4 instead of 2. The slot count is the
shopkeeper's `data` length and each slot sits at `vectordata[j]`, so both are lengthened before the shelf is built,
once per shopkeeper: the game rebuilds the shelf on the same shopkeeper after a purchase, and a second stretch drifted
it right. Seen: 5 on Merab's counter (screenshot, 2026-09-25).

**Item shops** (the user, 2026-09-25: the first purchase of each item in each shop is a check, then the shop's own
item again). An item shop isn't a shelf the shopkeeper builds: the map makes one `Fixedshop<n>` slot per entry of the
shopkeeper's `data` (`MapControl.cs:1715-1745`), and the buy line pays and then adds the item with `additem`, silently,
with no item-get box to swap (`BugariaCommercial` line 16). So the slot shows the seed's item (sprite, name and
description, as for medals, from `itemdata[0, id, 0]` and `[.., 2]`); when the buy line is read (`GetDialogueText`) its
`additem` is taken out, so nothing local is given; once the dialogue is over, berries down by the price mean it was
bought, and the check goes out through the respawning pickups' queue with a hold-up. Nothing in the save marks it,
as for respawning pickups. After the check, the slot is the shop's own item again. **Seen (the user, 2026-09-25):**
the first try showed the shop's own items and a hold-up of "an Archipelago item", since the item shop locations were
never scouted (the scout list is built table by table); after adding them, the shelf showed the seed's medals, each
purchase held up the seed's item and sent its check, and the bought slots became the shop's own items again.

**A shopkeeper kept present, and scenery shown** (the caravan, the user, 2026-09-25). Making an entity present works
by a marker on its `requires`, set right after the map builds its entities. A shopkeeper's slots are built *during*
that build, right after the keeper is read, and only if the keeper exists by then (`MapControl.cs:1708-1745`), so the
marker came too late. While a map builds, the mod now remembers the entity just made (every one starts as
`CreateNewEntity(name)`), and a check made with that entity's own `requires` array answers "exists" when it's listed.
Scenery (a `ConditionChecker`) gets the mirror of the rocks' treatment: a marker `requires` before its `Start`, which
answers "exists" (`scenery_present`, the caravan's stall). Seen (the user, 2026-09-25): the stall and Crickerly with
her three slots, the seed's items in them, each first purchase a check, then her own items.

**The reshuffle choice first** (the user, 2026-09-25: faster to reset a shelf). A shopkeeper's greeting ends in a
`prompt` whose choices are listed as N targets then N texts (`MainManager.cs:12213-12222`); the reshuffle is the one with
target `-199` and text `-195` (Shades's line 1, Merab's line 34, read with the console's `script`). With Archipelago on,
that pair moves to the front, in the map's dialogue table in memory, once per map load. Seen (the user, 2026-09-25):
at Shades's, reshuffling is now a matter of tapping the confirm button.

**Full stock from the start, the mod owning it** (the user, 2026-09-25; built, then seen: see this step's Status). Every medal a
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
   **Seen (the user, 2026-09-25):** 18 of Merab's 22 copies bought on a new file, each check sent for the copy bought
   (both TP Plus copies and both Ambusher copies separately), the guard in step 5 firing every time; after saving,
   the title screen and loading, exactly the 4 unbought copies stayed, reshuffles included. One flash fixed after: a
   rebuilt shelf showed the game's own medal sprites for a moment (the swap ran every 15 frames; now every frame for a
   second after a rebuild; seen gone, the user, 2026-09-25).
   **The same flash on the ground, in houses** (the user, 2026-09-25): Madeleine's table items looked right from outside
   and once inside, but showed their own items for a moment on the way in. A house on the same map is an "inside", and
   going in switches its entities on (`MapControl.RefreshInsides`). **First guess, wrong:** swap right after
   `RefreshInsides` and every frame for a second; the flash stayed. **Measured instead:** a log line in a patch on
   `EntityControl.UpdateItem` (`EntityControl.cs:3218`), the one place the game draws an item entity's own sprite, run
   whenever its animation state changes. Every way in, the game redrew the house's pickups there, and the ground pass
   (logged in its one-second window) never found anything to fix. So the fix is that patch: when the game draws a
   location pickup's own item, the seed's item goes back on in the same call. **Seen (the user, 2026-09-25):** nothing
   odd going in or out any more. The first guess was taken back out.

**Status:** works, seen by the user (2026-09-25): Merab's medal shop with its full stock, the reshuffle choice, Madame Butterfly's item shop, the caravan, and pickups in houses; Shades's shop not yet built as locations.

*Code: `ShopSwap.cs` (medal shops and their stock), `ItemShops.cs` (item shops), `KeptOpen.cs` (the shopkeeper
and scenery kept present), `QualityOfLife.cs` (the reshuffle choice first), `ItemSwap.cs` (`UpdateItem`, pickups
in houses).*

## 13. Doors rewritten: the entrance randomizer in the game

The entrance randomizer changes where doors lead. Each door in the game carries its own target, so the mod
rewrites doors as a map loads, and a shuffled door needs its own way back.

**Doors rewritten: the entrance randomizer's proof of concept** (the user, 2026-09-25). A door to another map calls
`TransferMap(data[0], vectordata[0], vectordata[1], vectordata[2])` when walked into (`NPCControl.cs:5458-5461`): the
target map, the walk on this side, where the party appears, where it then walks. So "door A leads where door B leads"
is: after the map builds its entities, A's `data` and its `vectordata` from `[1]` on are replaced by B's, read from B's
own map's entity table and names table (`Data/EntityData/Names/<map>names`); A's own `vectordata[0]` stays. The pairs
come from `slot_data` (`door_targets`) or, for a test, the dev setting `TestDoors`. First test: the Outskirts' east exit
leading where the plaza's door to the Commercial District leads. **Seen (the user, 2026-09-25):** from the Outskirts'
bottom-right exit they appeared on the right side of the Commercial District, exactly as when coming in from the plaza.
**Both ways, seen (the user, 2026-09-25):** four rewrites swapped two connections as a coupled shuffle would (the
Outskirts' east exit with the plaza's Commercial door, and their ways back). The log showed every trip landing on the
right map: the plaza's door to the Outskirts' east area and back, the Outskirts' exit to the Commercial District and back,
each several times. The user found it confusing to keep track by eye, so from here the log is the record of each trip.

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

**Status:** works, seen by the user (2026-09-25): a rewritten door, and a coupled swap both ways; the 36 doors that don't pair both ways are still to check in play.

*Code: `DoorShuffle.cs`; `dev-scripts/door-graph.py` (the pairs).*

## 14. The Detector for every check

The Detector medal beeps when a room hides something. In a seed it should beep for what matters instead: any
of the seed's checks left in the room. That meant replacing the game's answer, not adding to it.

**The Detector for every check** (the user, 2026-09-25: beep for any kind of check left in the room: shops, quests,
someone to help, not only hidden items). Read first how the medal works: one second after a map loads, the game asks its
objects (`NPCControl.CheckHidden`: buried crystal berries, grass hiding one, a dig spot with a medal) and the map
(`MapControl.CheckDisc`: an unrecorded discovery) whether something is hidden; any yes sets one value,
`map.hiddenitem = 100`, and the map's update turns that into the "!" over the leader and the beep
(`MapControl.cs:885-896`). So nothing about the medal needs changing: a postfix on `CheckDisc` (run a second after every
map load, discoveries or not) sets the same value when one of the seed's locations on this map isn't done. Every location
type has a map: pickups, gifts (quest rewards included, where the reward is handed over), shop copies and item shops
from `slot_data`, discoveries from the map's own `discoveryids`. Done means the server has the check, or offline the save
says so (its flag, crystal berry, journal entry, a shop copy's bought bit). Only while the Detector counts as equipped
(the medal, or the panel's Detector row). **In a seed the mod's answer is the only one** (the user: beep with one check
or more left, quiet when the room is done): the game's own checks would still beep for hidden things the seed doesn't
have, so `NPCControl.CheckHidden` doesn't run, `CheckDisc` is replaced by the mod's answer (a beep or silence, logged
either way), and a music record's `Start`, which sets the value as the map builds and is used on the first free frame
(`MusicSpinner.cs:54-57`), has it cleared right after. Outside a seed, all vanilla. **Seen (the user, 2026-09-25):**
in the Residential District it beeped for the rooftop item, then, with both items taken, for the quest reward still
handed out there (location 16, the delivery quest), as intended; quiet in the plaza with none left.

**Status:** works, seen by the user (2026-09-25).

*Code: `CheckDetector.cs`.*

## 15. Difficulty and the Detector: the panel's two game settings

Two rows in the Archipelago panel change how the game plays, never where items are. Both work by answering the
game's own "is this medal equipped?" question, so the save stays clean and the logic never changes.

**A row "Difficulty: Normal / Hard / Hardest" in the Archipelago panel** (the user, 2026-09-24). Hard and
Hardest add the game's two Hard Mode levels; Normal leaves it to the game (the medal equipped, or the
HARDEST code). **Default: Normal.** Boss prize medals are paid out on every setting (apimplementation.md,
build step 10).

**What belongs in the panel** (the user, 2026-09-24): only on/off preferences that never change what's where
(Difficulty, Detector, later DeathLink, which only adds a tag to the connection). Anything that decides the
seed (entrance rando, shuffles, goals) is a player-file (yaml) option, applied from `slot_data`.

**Every panel setting applies only while Archipelago is enabled** (the user, 2026-09-24): vanilla saves play
exactly as vanilla. Difficulty, Detector and every item swap check the switch; a Hardest flag the mod set is
cleared the moment it's switched off.

**A row "Detector: On / Off" in the Archipelago panel** (the user, 2026-09-24), a help for finding items.
On acts as if the Detector medal (#2) were equipped; Off leaves it to the game (the medal equipped or not).
**Default: On.**
All three of its effects ask one question, `BadgeIsEquipped(2)` (objects `NPCControl.cs:1344`, discoveries
`MapControl.cs:408`, music `MusicSpinner.cs:54`), and Hard is the same question for medal #11, so one patch
on `BadgeIsEquipped` serves both rows. It changes no save data and no logic.

**Built (2026-09-24), not yet seen on screen:** the panel has eight rows now (spaced tighter so the status
line still fits). Difficulty offers Normal, Hard and Hardest. `MedalAssist.cs`
answers "equipped" for medal 11 (Hard) or 2 (Detector) on party-wide checks, on randomizer saves only. The
medals menu equips from the medal list itself, never through that check, so it's unaffected.

**Hardest** (the user chose: switchable, the save stays clean): its extras read the save's HARDEST flag (614)
directly, and the game keeps no other trace of a typed code. So the mod turns 614 on in play and remembers
that it did; each time the game saves, the flag is cleared just for the write and put back after, and
switching down clears only a 614 the mod set. Loading a save or starting a new one forgets the mark, since
those flags are the save's own. If any of the three hooks (save, load, new game) is missing, Hardest does
nothing rather than risk a save.

**Status:** the Detector row's effect seen on screen through step 14 (the Detector beeping for checks left in a room, the user, 2026-09-25); Difficulty (Hard, Hardest) built (2026-09-24), not yet seen on screen.

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

**Decided (the user, 2026-09-26):**
- A row on the panel's Quality of life page, *Enemy scaling*: **Off / Party level / Artifacts**, on by default
  (Party level). It isn't in the yaml: it ties to no check and no logic, so the player can change it from the main
  menu. Only while Archipelago is enabled (vanilla stays vanilla).
  - **Off:** vanilla, each enemy's own stats.
  - **Party level:** every enemy scaled to the party's level, so every area plays fair in any order. No cheese: a hard
    area early is scaled down, an easy one late scaled up, and EXP follows (below).
  - **Artifacts:** scaled to the artifacts found, as vanilla's difficulty follows the story; levelling ahead makes it
    easier, rushing harder. A `chapter` mode was dropped: the chapter ends *are* the artifact flags, so it would be
    the same number.
- Both up and down. **Normal / Hard / Hardest** (step 15) stays the challenge setting, on top.
- The **bestiary** shows the scaled numbers, as the enemy would be if met now. Spy in a fight already shows the live
  ones.

**Where the numbers come from** (the facts in `MEASURED.md`, "Battles, for enemy shuffle"). The user hasn't finished
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
- **Attack, per hit, by the same ratio as HP** (changed 2026-09-26, the user). First it was a flat step through
  `hardatk` (+1 per 4 levels, held to -3...+6). But the game adds `hardatk` to every hit (`CalculateBaseDamage`,
  `BattleControl.cs:6364`, per hit), so a flat -3 barely touched one big hit and floored many small ones. A Dead Lander
  G at level 1 still hit hard through its string of attacks (the user). Now a prefix on `CalculateBaseDamage` scales
  the move's own damage by the ratio, before `hardatk` (so Hard/Hardest still add on top), when an enemy is the
  attacker. The game's floor of 1 per hit stays.
- **Defence** +1 per 6 levels, never below 0; a defence of -1 (shown as "?") is left alone.
- **EXP** by the game's own rule at the level the enemy is matched to, `GetEXP(base, level - difference)`: in Party
  level mode that's the enemy's home level, so levelling keeps vanilla's pace. Left alone where the game fixes it
  (fixed EXP, no EXP, the level cap, hologram fights).
- Some ids read another row's data (column 25); the row the game read is used.
- **The bestiary** (built 2026-09-26, seen by the user): the page builds its text in `PauseMenu.UpdateText` from its
  own copy of the raw table (`PauseMenu.enemydata`), so a prefix swaps the shown enemy's row for a scaled one (HP and
  its Hard bonus by the ratio, defence by the step) and a postfix puts it back; the page's own Hard/Hardest maths runs
  on top. The field holds other text on other pages, so only a real enemy row is touched. The Dead Lander G showed
  HP 7, Defense 0 there, as in the fight.
- Every scaled enemy is logged (`[scale] Seedling (9): home 1, target 10: hp 4 -> 10, attack +2, ...`).
All the constants are starting values, tuned by play.

**Seen by the user (2026-09-26):** at level 1 on Party level, a map Underling shuffled into a Dead Lander G was
logged `home 27, target 1: hp 35 -> 7, attack -3, def 1 -> 0, exp 74 -> 9`, and Spy in the fight showed HP 7, Defense 0.
The panel row stepped through its three values (the log followed each). **How it played (the user):** tough but fair. Leif alone at 7 HP
(healing 1+ a turn) went to 4 HP after its first hits, then to 2. The user judged 35 -> 7 HP and no defence balanced,
"fair/hard" for anyone who shuffles enemies. Its attack sits at the -3 floor; if late enemies prove too harsh early,
that floor is the first knob to try.

**Per-hit attack felt fair (the user, 2026-09-26):** the same Dead Lander G at level 1 (hits x0.19, the dev cheat
`onehit` off) landed 1-2 attacks at "fair damage" and died in two hits, like any other enemy there.

**Status:** works, seen by the user (2026-09-26): scaled HP, defence and per-hit damage in a fight, and the bestiary;
the constants still to tune by play.
