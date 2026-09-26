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

- **Quality of life**: **Fast text** (dialogue is instant, and holding skip races through it; On), **Travel** (Off,
  Warp, Map or Both, the default: the Warp is a pause-menu button back to where the game began, or to the seed's
  start; Map is fast travel from the pause menu's map to areas you've visited; both ask Yes / No), **Skip cutscenes**
  (On: the new game's intro, tutorial battle included, is skipped, Vi joining and the first check sent; other scenes
  you don't need to watch are skipped or pass by fast; with a random start the intro is always skipped), **Item
  animation** (which items from other players are shown held up: All, the default, Progression or Off; your own finds
  always are) and **Detector** (On, the default, acts as if the Detector medal were equipped. With the Archipelago mod
  enabled, the Detector (row or medal) also beeps on entering a room that still has a check of any kind, and stays
  quiet in a room with none left).
- **Gameplay**: **Difficulty** (Normal, the default, leaves it to the game; Hard plays as if the Hard Mode medal were
  equipped, Hardest as if the save had the HARDEST code, without writing it into the save; boss prize medals are
  handed out on every setting), **Enemy scaling** (Party level, the default, scales every enemy to your level;
  Artifacts to the artifacts found; Off keeps each enemy's own stats; Difficulty applies on top), **Medal prices**
  (medals in any shop, a bar in tenths of the price: full, the default, is normal, half is half price, empty is free), and **EXP multiplier** and **Berry multiplier** (1x, the default,
  to 10x, a bar like the volume rows; EXP from every defeated enemy, berries picked up in the world; a battle still
  gives at most a level's worth, and a check's berries are never multiplied).

Select a row and press confirm to type into it: Backspace deletes, **Ctrl+V pastes**, Ctrl+C copies, Enter
keeps it, Escape undoes. The same settings are saved in `BepInEx/config/bugfables.archipelago.cfg`.
