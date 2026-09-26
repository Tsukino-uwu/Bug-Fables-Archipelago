# Bug Fables: The Everlasting Sapling

## What does randomization do to this game?

Key items, medals and other items are shuffled across the multiworld. Picking one up, being handed one, buying one
in a shuffled shop or finishing a quest sends a check instead, and the item there shows what the seed put in its
place. Every item, your own included, arrives from the server and is given to you through the game's own item system.

This is an early version. It covers the start of the game: the Bugaria Outskirts, Snakemouth Den, the open parts of
Bugaria with Merab's medal shop and Madame Butterfly's item shop, and the Golden Path. More chapters come later.

## Options

- **Artifacts Required** (1 to 7): the goal, see below.
- **Shuffle Quests** (on): quest rewards are locations.
- **Shuffle Crystal Berries** (on): crystal berry spots are locations and crystal berries are items.
- **Shuffle Discoveries** (off): recording a journal discovery sends a check.
- **Shuffle Medal Shops** (on): medals sold in shops are locations.
- **Shuffle Item Shops** (on): the first purchase of each item in an item shop is a location.
- **Shop Contents** (No Progression): what shop locations may hold.
- **Entrance Randomizer** (off, experimental): doors between areas lead somewhere else, in coupled pairs.
- **Enemy Shuffle** (off): ordinary enemies on each map are swapped for others of the same group size.
- **Starting Location** (off, experimental): a new file begins in any room in the game.

Each option's description in the yaml says what it does in full and how many checks it adds.

## What is the goal?

Collect a number of artifacts (the option *Artifacts Required*). The game has 7, one per chapter milestone. This
version includes only the first, so the goal is capped at 1. The mod doesn't report the goal to the server yet, so a
seed can't be marked finished from the game.
