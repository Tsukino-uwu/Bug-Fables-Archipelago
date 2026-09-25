# How the Bug Fables mod is being made

This is the story of building the **game side** of an [Archipelago](https://archipelago.gg) randomizer
for Bug Fables: the mod that runs inside the game, step by step, in the order it happened. It's meant for
anyone curious about the process, or thinking of doing the same for another game.

- **The Archipelago side** (the apworld, seeds, the server, connecting, items and checks) has its own
  guide: [apimplementation.md](apimplementation.md).
- **Facts about how Bug Fables works inside** live in `MEASURED.md`.

## Where it stands

**Done so far:** the mod loads through BepInEx, reloads itself while the game runs, watches the game with
read-only probes, and has a full list of where key items come from. The main menu has an Archipelago panel
for connecting, randomizer saves are kept in their own folder, and finished locations are reported to the
server, and items from the server arrive in the game once each.

**Next:**

1. **Separate randomizer saves:** done. Through a whole session played with the Archipelago mod enabled, the game
   saved only into the `archipelago` folder (last write 05:06), and the normal save files kept their earlier
   times (03:42 and 2023), checked on disk on 2026-09-24. The redirect covers all five places the game touches
   a save file, and normal saves are only reachable with the mod disabled.
2. **Give an item the game's own way**, when the server sends one: works (apimplementation.md, build step 7).
3. **Spot a location being done** (the flag the game sets) and report it: works (apimplementation.md, build
   step 6). The game's own item there is kept out, and the seed's item shown instead (step 9).
4. **Keep the received-item count in the save**, so loading never hands items out twice: works, in a save slot
   the game never uses (`MEASURED.md`, "Free save slots for the mod").
5. **The chat feed**, then the in-game text client (see the design list in step 2).
6. **A row "Difficulty: Normal / Hard / Hardest" in the Archipelago panel** (the user, 2026-09-24). Hard and
   Hardest add the game's two Hard Mode levels; Normal leaves it to the game (the medal equipped, or the
   HARDEST code). **Default: Normal.** Boss prize medals are paid out on every setting (apimplementation.md,
   "Where it stands").
   **What belongs in the panel** (the user, 2026-09-24): only on/off preferences that never change what's where
   (Difficulty, Detector, later DeathLink, which only adds a tag to the connection). Anything that decides the
   seed (entrance rando, shuffles, goals) is a player-file (yaml) option, applied from `slot_data`.
   **Every panel setting applies only while Archipelago is enabled** (the user, 2026-09-24): vanilla saves play
   exactly as vanilla. Difficulty, Detector and every item swap check the switch; a Hardest flag the mod set is
   cleared the moment it's switched off.
7. **A row "Detector: On / Off" in the Archipelago panel** (the user, 2026-09-24), a help for finding items.
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
8. **A "Quality of life" page in the Archipelago panel** (the user, 2026-09-25): on/off rows that speed the game
   up and make it smoother: skips first, others later (a pause-menu warp back to the seed's start, say). Skip
   intro and Fast text (with a faster hold-to-skip) are confirmed on screen; battle tutorials next (step 10).

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

## Keeping this guide honest

A step-by-step guide is only useful if no step is missing, so the project enforces it: any commit that
changes the mod, the apworld or the dev scripts is refused unless it also updates this file or
[apimplementation.md](apimplementation.md) (or says, explicitly, that nothing about the process changed).
That check is a small git hook, `.githooks/commit-msg` (its neighbour `.githooks/pre-commit` refuses
personal paths and names). Each step below ends with a short *Code:* line naming the files and methods to
read. Each new step also gets a line in the index above,
and "Where it stands" is updated with it.

---

## 1. Check whether the game can be modded at all

Before writing anything, we looked at what the game is made of. Bug Fables is a **Unity game built with
Mono**, which means its code ships as a normal .NET file (`Assembly-CSharp.dll`) that can be turned back
into readable code. That's the easiest case there is. We also checked that nobody had already made a
Bug Fables randomizer.

*How to tell for your own game:* an `Assembly-CSharp.dll` in the game's `_Data/Managed` folder means
Unity with Mono. A `GameAssembly.dll` means Unity with IL2CPP, which is harder.

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

## 3. Read the game's code

We used **ILSpy** to turn the game's `Assembly-CSharp.dll` back into C# source, kept on our own machine
and never shared. Reading it answered the first big question: *how does this game hand out items?*
The answer was a small set of places every item goes through, which is exactly where a randomizer
needs to hook in.

## 4. Get a mod loader running

Unity games don't load mods by themselves, so we installed **BepInEx 5**, the usual mod loader for
Unity games. One launch of the game confirmed it worked, and showed its log file.

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

## 8. An Archipelago menu inside the game

Players need to type a room address, a slot name and maybe a password, so the mod adds **"Archipelago"** to the
game's main menu. It opens a panel drawn with the game's own box and font, so it looks like part of the game,
but it takes real typing: the game itself never reads typed text (its name screen is a letter grid), so the
mod reads the keyboard itself. Backspace, Ctrl+V to paste and Ctrl+C to copy all work. The same panel switches
**the Archipelago mod** (enabled or disabled), which keeps randomizer saves in their own folder so normal saves are never touched.
Its rows, top to bottom (the user's order, 2026-09-24): Address, Port, Slot, Password, Difficulty, Detector,
**Archipelago** (the mod on/off, just "Archipelago"). No Back row: cancel backs out, as the hint box says.
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

*Code: `MenuToggle.cs` (the menu entry: `BeforeSetMenuText` and `AfterSetMenuText` around the game's rebuild,
`AfterUpdate` for the cursor, `SetMode` for the switch); `ApMenu.cs` (the panel: `Build`, `Redraw`,
`Navigate`, `TypeInto` for typing, `Close`); `SaveRedirect.cs` (the separate save folder, patching the
game's five save-file functions in `InputIO`).*

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
   and the Yes / No box with No preselected ("this looks good"). The warp itself is still to see.
   **The logic never counts on the warp** (the user, 2026-09-25): it's fast travel and a way out when stuck, but a
   seed must not assume players teleport out, so every one-way drop still needs a real way back in the logic.
5. **Skip battle tutorials:** next. The tutorial battles end on fixed turns and read story flags, so each one is
   read in full before anything is skipped.

The panel got an eighth row, "Quality of life", which opens a second page in the same box; cancel comes back.

*Code: `QualityOfLife.cs` (the settings and the per-frame speed-ups), `ApMenu.cs` (the second page).*
