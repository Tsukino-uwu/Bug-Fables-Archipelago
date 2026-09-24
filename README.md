# Bug Fables Archipelago

An [Archipelago](https://archipelago.gg) randomizer for *Bug Fables: The Everlasting Sapling*: a BepInEx mod for
the game plus an apworld for the Archipelago generator.

**Status: not playable yet.** A tiny apworld generates seeds, and the mod logs in to an Archipelago server
from inside the game. Receiving items and sending checks come next. The first version will shuffle key
items only.

## How it will work

- **The apworld** (`apworld/bug_fables/`) tells the generator which items and locations exist and which key
  items gate which areas. Today it covers two early locations.
- **The mod** (`mod/`) runs inside the game. Picking up a key item will send a check to the server, and
  every item, including your own, will arrive from the server and be given to you through the game's own
  item system. Because items are remote only, a new save can recover everything the server has sent.

You need your own copy of the game. Nothing from the game is included in this repo.

## Documentation

- **[How the mod was made](agent_docs/documentation.md)**: the game side, step by step. Finding out the
  game could be modded, the mod loader, reloading while the game runs, and watching the game to learn
  how it hands out items.
- **[Archipelago implementation](agent_docs/apimplementation.md)**: the Archipelago side. How the apworld
  and the server connection were built, and how a game talks to Archipelago in general.

## License

MIT, see `LICENSE`.
