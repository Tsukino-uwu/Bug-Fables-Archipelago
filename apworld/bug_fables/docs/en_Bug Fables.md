# Bug Fables: The Everlasting Sapling

## What does randomization do to this game?

Key items, medals and other items are shuffled across the multiworld. Picking one up, being handed one, buying one
in a shuffled shop or finishing a quest sends a check instead, and the item there shows what the seed put in its
place. Every item, your own included, arrives from the server and is given to you through the game's own item system.

This is an early version. It covers the start of the game: the Bugaria Outskirts, Snakemouth Den, the open parts of
Bugaria with Merab's medal shop and Madame Butterfly's item shop, the caravan's shop outside the city, and the Golden Path, plus the seven scenes in later chapters where the game teaches a field ability and the one where the Termite King hands over the submarine. More chapters come later.

## Options

- **Artifacts Required** (1 to 7): the goal, see below.
- **Shuffle Quests** (on): quest rewards are locations.
- **Shuffle Crystal Berries** (on): crystal berry spots are locations and crystal berries are items.
- **Shuffle Discoveries** (off): recording a journal discovery sends a check.
- **Shuffle Medal Shops** (on): medals sold in shops are locations.
- **Shuffle Item Shops** (on): the first purchase of each item in an item shop is a location.
- **Shop Contents** (No Progression): what shop locations may hold.
- **Shuffle Shop Inventories** (on): what item shops restock and what respawning floor items come back with is
  shuffled among themselves (food and other consumables only, each as often as before). Never a check or location:
  the first purchase or pickup is still the check, and this changes only what comes after. New in this version.
- **Entrance Randomizer** (off, experimental): doors between areas lead somewhere else. Coupled: a door and its way
  back stay a pair. Decoupled (new in this version): the way back is shuffled too, so turning round can take you
  somewhere else. Or Room Swap, whole rooms trading places (see below). The spoiler log lists where each door leads.
- **Enemy Shuffle** (off): ordinary enemies on each map are swapped for others of the same group size.
- **Starting Location** (off, experimental): a new file begins in any room in the game.
- **Starting Party Member** (all three): a new file starts with the whole party; or with Vi, Kabbu or Leif alone (or one
  picked by the seed), and the other two are items; or Off, the story's party, with Leif joining in Snakemouth Den. With any setting but Off, the opening and the fall
  room after the spider become locations. New in this version.
- **Filler Starting Checks** (on): the checks a new file sends by itself when the game begins (Maki and Eetl's gift,
  the tutorial battle, and the opening spot when members are items) hold filler only, so a seed doesn't open with its
  good items. Nothing else changes; turn it off to plando an item there. With the Entrance Randomizer on Coupled or
  Room Swap it doesn't apply, since the doors can leave the start too small. New in this version.
- **Shuffle Field Moves** (off): Vi's Beemerang Toss, Kabbu's Horn Slash and Leif's Freeze are items too (the Toss and
  the Freeze as the first copy of their progressive item); until one arrives, that attack only buzzes, and each shows in
  your key items once it does.
- **Shuffle Jump** (off): Jump is an item for the whole party; until it arrives, the jump button only buzzes, and the
  pause menu's Warp is always there. New in this version.
- **Points of No Return** (off): the logic may send you somewhere only the pause menu's Warp to Start gets you out of,
  a drop or a one-way door, so items can land in more places and you're expected to warp back. Off, it always leaves
  you a way to walk back. Few one-ways are in the logic yet, so for now it changes little. New in this version.
- **Progressive Boat** (on): the Boat Ticket and the submarine are one item found twice, the ticket first; off, two
  items in any order (see below). New in this version.
- **Music Shuffle** (off, under Aesthetic Options): every song plays in place of another, the same every time you play
  the seed, and the jingles (victory, game over, chapter titles) swap among themselves. The title screen, the wind,
  water and machine sounds, and the factory elevator's music stay. Samira plays the song you pick. It changes nothing
  else. New in this version.

Each option's description in the yaml says what it does in full and how many checks it adds.

## Room Swap

Set *Entrance Randomizer* to `room_swap` (experimental, new in this version). Whole rooms trade places with rooms that
have as many doors, in the same part of the world, so the map keeps the game's shape: only which room sits where
changes. Turning round always takes you back where you came from. It is gentler than Coupled, where any door may lead
to any other. As with Coupled, the logic follows the doors but not yet what each room needs inside, so a seed may not
be finishable; the pause menu's Warp gets you out of a dead end.

## Plando: choosing where doors lead

With *Entrance Randomizer* on Coupled or Decoupled, `plando_connections` pins doors (new in this version). Each door is
named by its map and its door, `MapName: DoorName`, as the spoiler log's Entrances section lists them. `entrance` is the door you go through, `exit`
the door you arrive next to:

```yaml
plando_connections:
  - entrance: "BugariaOutskirtsOutsideCity: DoorBugaria"
    exit: "SnakemouthLake: WarpMap5"
    direction: both
```

`direction` is `both` (the default), `entrance` (only the first door leads to the second) or `exit` (only the second
leads back to the first); Coupled always joins both ways. The rest of the doors are shuffled around them. Plando's
"connections" must be turned on where the seed is generated (Archipelago's plando guide). It's ignored with the
Entrance Randomizer off or on Room Swap.

## Field abilities

Every ability the story teaches is an item, in every seed, and the scene where the game teaches it is a check instead:
the Beemerang Halt, Bee Fly, the Dash, the Horn Dash, Beetle Dig, the Icicle and the Shield. Three come as progressive
items, in the game's own order: **Progressive Beemerang** (the Toss, then the Halt), **Progressive Dash** (the Dash,
then the Horn Dash) and **Progressive Freeze** (the Freeze, then the Icicle). An ability works once its item arrives,
wherever you are in the story, and its battle skill comes with it. The logic for chapters 2 to 7 is cautious for now:
each teaching scene counts as reachable only once every ability taught before it is yours.

## The boat and the submarine

The Boat Ticket and the submarine, the Termite Kingdom's **Subaquatic Maritime Neotransport**, are items in every
seed. With the option *Progressive Boat* on (the default) they are one item, **Progressive Boat**, found twice: the first
copy is the Boat Ticket, the second the submarine. Off, they are two items found in any order. The pier's sailor takes
you to Metal Island only with the ticket. The submarine's docks are there only once it is yours, wherever you are in
the story, and it sails to every one of them, Metal Island's included. The scene where the Termite King hands it over
is a check instead.

To hint it, `!hint Submarine` (or `!hint Boat` for the ticket) works either way. With the progressive item it shows
where both copies are, since a hint can't tell the two apart.

## Good to know

- **We Owe Ya!** calls a helper into battle only from those you have unlocked in the story or a side quest.
  Received early, it does nothing until then.

## What is the goal?

Collect a number of artifacts (the option *Artifacts Required*). The game has 7, one per chapter milestone. This
version includes only the first, so the goal is capped at 1. Once you have that many, the mod tells the server,
which marks your game finished.
