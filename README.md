# Bug Fables Archipelago

An [Archipelago](https://archipelago.gg) randomizer for *Bug Fables: The Everlasting Sapling*: a BepInEx mod for
the game plus an apworld for the Archipelago generator.

**Status: not playable yet.** The first version will shuffle key items only.

## How it works

- **The apworld** (`apworld/bug_fables/`) tells the generator which items and locations exist and which key
  items gate which areas.
- **The mod** (`mod/`) runs inside the game. Picking up a key item sends a check to the server. Every item,
  including your own, arrives from the server and is given to you through the game's own item system.
  Because items are remote only, a new save can recover everything the server has sent.

You need your own copy of the game. Nothing from the game is included in this repo.

## Documentation

- **[How the mod was made](agent_docs/documentation.md)**: the game side, step by step. Finding out the
  game could be modded, the mod loader, reloading while the game runs, and watching the game to learn
  how it hands out items.
- **[Archipelago implementation](agent_docs/apimplementation.md)**: the Archipelago side. How the apworld
  and the server connection were built, and how a game talks to Archipelago in general.

## License

MIT, see `LICENSE`.
