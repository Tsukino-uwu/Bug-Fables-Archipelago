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

On the game's main menu, choose **Archipelago** and fill in the room's address, port and your slot name (and the
password if the room has one). With Archipelago enabled, the mod connects on its own.
