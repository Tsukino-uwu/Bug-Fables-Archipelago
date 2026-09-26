# Bug Fables Archipelago

An [Archipelago](https://archipelago.gg) randomizer for *Bug Fables: The Everlasting Sapling*: a BepInEx mod for
the game plus an apworld for the Archipelago generator.

**Status: early work in progress, released as pre-releases.** It covers the start of the game (59 locations by
default). The mod doesn't report the goal yet, so a seed can't be marked finished from the game.

## Playing

- **[Setup guide](apworld/bug_fables/docs/setup_en.md)**: install, your options, connecting and the in-game settings.
- **[Game page](apworld/bug_fables/docs/en_Bug%20Fables.md)**: what's randomized, the options and the goal.
- **[Downloads](https://github.com/Tsukino-uwu/Bug-Fables-Archipelago/releases)**: the mod, the apworld and a yaml.

You need your own copy of the game. Nothing from the game is included in this repo.

## How it works

- **The apworld** (`apworld/bug_fables/`) tells the generator which items and locations exist and which key
  items gate which areas.
- **The mod** (`mod/`) runs inside the game. Finishing a location sends a check to the server, and every
  item, including your own, arrives from the server and is given to you through the game's own item
  system. Because items are remote only, a new save can recover everything the server has sent.

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
