# Bug Fables Randomizer Setup Guide

## Requirements

- Bug Fables: The Everlasting Sapling (PC)
- [BepInEx 5.4](https://github.com/BepInEx/BepInEx/releases) (the `win_x64` download)
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.7 or newer
- From the [latest release](https://github.com/Tsukino-uwu/Bug-Fables-Archipelago/releases):
  `bugfables-archipelago.zip`, `bug_fables.apworld` and `bug_fables.yaml`

## Installing

1. Extract BepInEx into your Bug Fables folder, so `winhttp.dll` sits next to `Bug Fables.exe`. Start the game
   once, then close it.
2. Extract `bugfables-archipelago.zip` into your Bug Fables folder. The mod ends up in
   `BepInEx/plugins/BugFablesAP`.
3. Put `bug_fables.apworld` in your Archipelago's `custom_worlds` folder.

## Your options

Edit `bug_fables.yaml` (at least `name`, your slot name) and give it to whoever generates the room.

## Connecting

On the game's main menu, choose **Archipelago**. The panel has:

- **Address**: `archipelago.gg` by default, which is right for rooms hosted there. A server on your own
  computer needs `ws://` in front: `ws://127.0.0.1`.
- **Port**: the room's port, e.g. `38281`; usually the only thing to change. Pasting a whole
  `archipelago.gg:38281` into Address fills in the port too.
- **Slot**: your slot name in the room.
- **Password**: only if the room has one; leave it empty otherwise.
- **Archipelago**: Enabled keeps randomizer saves in their own folder, apart from your normal saves.
  The main menu shows it as "Archipelago (Enabled)" or "(Disabled)".
- **DeathLink**: Off (the default). On, when your party is defeated in a battle, everyone in the room with DeathLink
  on is too, and their deaths reach you: in a battle, the game's Game Over menu; on the map, a Game Over and back to
  your last save. A death waits for a free moment, never striking in a cutscene or a text box.
- **Achievements**: Off (the default) holds Steam achievements back while the Archipelago mod is enabled, as
  normal saves are kept apart. It only concerns Steam, never Archipelago.
- **Use on normal saves**: Off (the default) keeps normal saves vanilla. On, the Quality of life and Gameplay settings
  also apply with the Archipelago mod disabled. Nothing tied to a seed does.
- Under the rows, a line explaining the highlighted one, and a line showing the connection's state. Cancel
  (X, or B on a gamepad) backs out of the panel.

**While the Archipelago mod is enabled and the address, port and slot are filled in, the mod connects on its
own**: when the game starts, when you enable it, and after you change a detail. If the room refuses (a wrong
slot or password), the reason is shown until you change it. If the server can't be reached, or the
connection drops mid-game, it keeps retrying on its own, waiting a bit longer each time. Disabling it
disconnects.

**A randomizer save needs one connection each time the game starts.** Until the mod has logged in once, it
doesn't know the seed, so choosing a file (or a new game) on the file select plays a buzzer and says to connect
first. After that, a dropped connection doesn't stop play: pickups still hold the seed's items, and their
checks are sent when the connection comes back.

**Quality of life and Gameplay** are two more pages, at the top of the game's own **Settings** (from the pause
menu, and from the main menu), shown while the Archipelago mod is enabled or *Use on normal saves* is on. Each has
**Reset to defaults** and **Disable all** on top, each asking Yes / No first.
Enabling the Archipelago mod (or *Use on normal saves*) also stops a short stutter the game itself has every 5
seconds (it tidies its memory on a timer).

- **Quality of life**: **Fast text** (dialogue is instant, and holding skip races through it; On), **Travel** (Off,
  Warp, Map or Both, the default: the Warp is a pause-menu button back to where the game began, or to the seed's
  start; Map is fast travel from the pause menu's map to areas you've visited; both ask Yes / No; in a seed the Warp
  is always there, whatever this says), **Skip confirm**
  (Off, the default, Warp, Map or Both: which travel buttons go at once, without the Yes / No), **Skip cutscenes**
  (On: scenes you don't need to watch are skipped or pass by fast. The new game's intro, tutorial battle included, is
  always skipped with the Archipelago mod enabled: Vi joins and the first check is sent), **Item
  animation** (which received items are shown held up: All, the default, Progression or Off. Items replayed on a new
  file or a reconnect are shown too. Your own finds that the game already shows in its own scene aren't shown twice), **Item colors** (how an item's importance is coloured, in the text and the starburst behind it: Rarity, the
  default, like loot in other games: filler green, useful blue, progression purple, trap red; Archipelago, as its own
  client colours them; Off, the game's own colours), **Archipelago icon** (Other games, the default: another
  game's item shows the Archipelago icon on the ground, on shelves and when found; All players: every item that isn't
  yours; Off: they look like the game's own item there), **Item backgrounds** (On, the default: a check's item,
  yours included, has a starburst behind it in its Archipelago class colour, so you can tell from afar whether it matters; Off
  keeps it a surprise), **Detector** (On, the default, acts as if the Detector medal were equipped. With the Archipelago mod
  enabled, the Detector (row or medal) also beeps on entering a room that still has a check of any kind, and stays
  quiet in a room with none left), **Spy Specs** (Off, the default; On acts as if the Spy Specs medal were equipped:
  every enemy's HP shows, and Spy needs no aiming and doesn't use the turn) and **Uncap FPS** (experimental; ten pips like the volume rows. The first, Off, is the
  default: the game's own 30 or 60 FPS. The last, Monitor, is your display's own refresh rate, with VSync, so no
  tearing; on a 60 Hz display it keeps the game's own setting. The ones between cap it at 90, 100, 120, 144, 165,
  180, 240 or 360, with VSync when the number divides your monitor's refresh rate or reaches it, otherwise as a limit. Motion is
  drawn smoothly between the game's steps, and the game still plays as it does at 60. Switching it on the first time in a
  session takes a few seconds).
- **Gameplay**: **Difficulty** (Normal, the default, leaves it to the game; Hard plays as if the Hard Mode medal were
  equipped, Hardest as if the save had the HARDEST code, without writing it into the save; in a seed, boss prize
  medals are handed out on every setting; on a normal save, as in the game: on Hard from Artis, a missed one at the
  caravan), **Enemy scaling** (Party level, the default, scales every enemy to your level;
  Artifacts to the artifacts found; Off keeps each enemy's own stats; Difficulty applies on top), **Attack boost** (Off, the default, or +1: each hit
  your party lands does 1 more damage), **Healing crystals** (Off, the default; On: the blue save crystals turn
  yellow, so they heal HP and TP as well as saving, from the next room on), **Auto-save** (Off, the default; On:
  walking into a new room saves at its door once you can move, at most every 15 seconds, so a defeat costs one
  room), **Medal prices**
  (medals in any shop, a bar in tenths of the price: full, the default, is normal, half is half price, empty is free), and **EXP multiplier** and **Berry multiplier** (1x, the default,
  to 10x, a bar like the volume rows; EXP from every defeated enemy, berries picked up in the world; a battle still
  gives at most a level's worth, and a check's berries are never multiplied).

Select a row and press confirm to type into it: Backspace deletes, **Ctrl+V pastes**, Ctrl+C copies, Enter
keeps it, Escape undoes. The same settings are saved in `BepInEx/config/bugfables.archipelago.cfg`. One setting is
only there: `Compression` under `[Connection]` (on, as the server asks); turn it off only if connecting keeps failing.
