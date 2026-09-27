# Bug Fables: The Everlasting Sapling

## What does randomization do to this game?

Key items, medals and other items are shuffled across the multiworld. Picking one up, being handed one, buying one
in a shuffled shop or finishing a quest sends a check instead, and the item there shows what the seed put in its
place. Every item, your own included, arrives from the server and is given to you through the game's own item system.

This is an early version. It covers the start of the game: the Bugaria Outskirts, Snakemouth Den, the open parts of
Bugaria with Merab's medal shop and Madame Butterfly's item shop, the caravan's shop outside the city, and the Golden Path. More chapters come later.

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
- **Starting Party Member** (all three): a new file starts with the whole party; or with Vi, Kabbu or Leif alone (or one
  picked by the seed), and the other two are items; or Off, the story's party, with Leif joining in Snakemouth Den. With any setting but Off, the opening and the fall
  room after the spider become locations. New in this version.
- **Shuffle Field Moves** (off): Vi's Beemerang Toss, Kabbu's Horn Slash and Leif's Freeze are items; until one
  arrives, that attack only buzzes, and each shows in your key items once it does. New in this version.
- **Shuffle Jump** (off): Jump is an item for the whole party; until it arrives, the jump button only buzzes, and the
  pause menu's Warp is always there. New in this version.

Each option's description in the yaml says what it does in full and how many checks it adds.

## What is the goal?

Collect a number of artifacts (the option *Artifacts Required*). The game has 7, one per chapter milestone. This
version includes only the first, so the goal is capped at 1. Once you have that many, the mod tells the server,
which marks your game finished.
