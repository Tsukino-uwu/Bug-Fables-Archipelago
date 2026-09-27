# Bug Fables: The Everlasting Sapling

## What does randomization do to this game?

Key items, medals and other items are shuffled across the multiworld. Picking one up, being handed one, buying one
in a shuffled shop or finishing a quest sends a check instead, and the item there shows what the seed put in its
place. Every item, your own included, arrives from the server and is given to you through the game's own item system.

This is an early version. It covers the start of the game: the Bugaria Outskirts, Snakemouth Den, the open parts of
Bugaria with Merab's medal shop and Madame Butterfly's item shop, the caravan's shop outside the city, and the Golden Path, plus the seven scenes in later chapters where the game teaches a field ability. More chapters come later.

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
- **Shuffle Field Moves** (off): Vi's Beemerang Toss, Kabbu's Horn Slash and Leif's Freeze are items too (the Toss and
  the Freeze as the first copy of their progressive item); until one arrives, that attack only buzzes, and each shows in
  your key items once it does.
- **Shuffle Jump** (off): Jump is an item for the whole party; until it arrives, the jump button only buzzes, and the
  pause menu's Warp is always there. New in this version.

Each option's description in the yaml says what it does in full and how many checks it adds.

## Field abilities

Every ability the story teaches is an item, in every seed, and the scene where the game teaches it is a check instead:
the Beemerang Halt, Bee Fly, the Dash, the Horn Dash, Beetle Dig, the Icicle and the Shield. Three come as progressive
items, in the game's own order: **Progressive Beemerang** (the Toss, then the Halt), **Progressive Dash** (the Dash,
then the Horn Dash) and **Progressive Freeze** (the Freeze, then the Icicle). An ability works once its item arrives,
wherever you are in the story, and its battle skill comes with it. The logic for chapters 2 to 7 is cautious for now:
each teaching scene counts as reachable only once every ability taught before it is yours.

## Good to know

- **We Owe Ya!** calls a helper into battle only from those you have unlocked in the story or a side quest.
  Received early, it does nothing until then.

## What is the goal?

Collect a number of artifacts (the option *Artifacts Required*). The game has 7, one per chapter milestone. This
version includes only the first, so the goal is capped at 1. Once you have that many, the mod tells the server,
which marks your game finished.
