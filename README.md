# Bug Fables Archipelago

An [Archipelago](https://archipelago.gg) randomizer for *Bug Fables: The Everlasting Sapling*: a BepInEx mod for
the game plus an apworld for the Archipelago generator.

**Status: not playable yet.** A tiny apworld generates seeds, and the mod logs in to an Archipelago server
from inside the game, sends checks and receives items. Next: every key item and medal in the pool, with
field abilities shuffled as items and logic that follows the story's chapter order.

## How it will work

- **The apworld** (`apworld/bug_fables/`) tells the generator which items and locations exist and which key
  items gate which areas. Today it covers nine early locations.
- **The mod** (`mod/`) runs inside the game. Finishing a location sends a check to the server, and every
  item, including your own, arrives from the server and is given to you through the game's own item
  system. Because items are remote only, a new save can recover everything the server has sent.

You need your own copy of the game. Nothing from the game is included in this repo.

## Requirements

- *Bug Fables: The Everlasting Sapling* on PC
- [BepInEx 5.4](https://github.com/BepInEx/BepInEx/releases) (the `win_x64` download)
- An Archipelago room (Archipelago 0.6.7)

## Install

**Coming with the first release. There's nothing to install and play yet.** The steps will be:

1. Extract BepInEx into your Bug Fables folder, so `winhttp.dll` sits next to `Bug Fables.exe`.
2. Start the game once, so BepInEx creates its folders, then close it.
3. Extract the release into `BepInEx/plugins`.
4. Set your connection (below) and start the game.

## Connecting

On the game's main menu, choose **Archipelago**. The panel has:

- **Address**: `archipelago.gg` by default, which is right for rooms hosted there. A server on your own
  computer needs `ws://` in front: `ws://127.0.0.1`.
- **Port**: the room's port, e.g. `38281`; usually the only thing to change. Pasting a whole
  `archipelago.gg:38281` into Address fills in the port too.
- **Slot**: your slot name in the room.
- **Password**: only if the room has one; leave it empty otherwise.
- **Difficulty**: Normal (the default) leaves it to the game. Hard plays as if the Hard Mode medal were
  equipped, Hardest as if the save had the HARDEST code, without writing it into the save. Boss prize medals
  are handed out on every setting.
- **Detector**: On (the default) acts as if the Detector medal were equipped, to help find items. Off leaves
  it to the medal.
- **Archipelago**: Enabled keeps randomizer saves in their own folder, apart from your normal saves.
  The main menu shows it as "Archipelago (Enabled)" or "(Disabled)".
- Under the rows, a line explaining the highlighted one, and a line showing the connection's state. Cancel
  (X, or B on a gamepad) backs out of the panel.

**While the Archipelago mod is enabled and the address, port and slot are filled in, the mod connects on its
own**: when the game starts, when you enable it, and after you change a detail. If the room refuses (a wrong
slot or password), the reason is shown until you change it. If the server can't be reached, or the
connection drops mid-game, it keeps retrying on its own, waiting a bit longer each time. Disabling it
disconnects.

Select a row and press confirm to type into it: Backspace deletes, **Ctrl+V pastes**, Ctrl+C copies, Enter
keeps it, Escape undoes. The same settings are saved in `BepInEx/config/bugfables.archipelago.cfg`.

## Building from source

For developers: building the mod, trying it in the running game, a local test server and the apworld's
tests are in [agent_docs/development.md](agent_docs/development.md).

## Documentation

- **[How the mod was made](agent_docs/documentation.md)**: the game side, step by step. Finding out the
  game could be modded, the mod loader, reloading while the game runs, and watching the game to learn
  how it hands out items.
- **[Archipelago implementation](agent_docs/apimplementation.md)**: the Archipelago side. How the apworld
  and the server connection were built, and how a game talks to Archipelago in general.

## License

MIT, see `LICENSE`.
