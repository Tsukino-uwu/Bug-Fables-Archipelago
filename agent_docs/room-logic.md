# Mapping a room for the logic

**Every plan for how the logic is written, checked and tested, room by room, lives here and only here** (gathered
2026-09-30, the user: "to have it all in 1 place"). `apimplementation.md` keeps the history of each decision (build
steps 8, 12, 15, 24, 36 and 37 point here), and `MEASURED.md` keeps the game facts (listed at the end); neither holds a plan
of its own. Written with the user, 2026-09-27, and grown since.

The logic is what Archipelago uses to prove a seed can be finished, and some options lean on it hard: **one party
member** (random party), **a random start** (any actual room in the game), **the entrance randomizer** (any door may
lead anywhere, even decoupled), **shuffled attacks** (the Horn Slash may not be there) and **no Jump** (a ledge may be a
one-way). The rules it serves are in `CLAUDE.md`: the logic may be more cautious than the game, never less, and every
seed must be completable.

1. [The rules](#the-rules)
2. [The model](#the-model)
3. [Where the party can appear](#where-the-party-can-appear)
4. [Chains: what other rooms do to this one](#chains-what-other-rooms-do-to-this-one)
5. [The questions, per room](#the-questions-per-room)
6. [How a room gets mapped](#how-a-room-gets-mapped)
7. [Not in the logic, on purpose](#not-in-the-logic-on-purpose)
8. [The facts it relies on](#the-facts-it-relies-on)

## The rules

Set 2026-09-27 ("the logic has to be precise"); 1 to 4 the user's, 2026-09-29, "to simplify things". How regions and
rules work in Archipelago: `apimplementation.md`, How it works §11.

1. **A location says what it needs; an item never says what it opens.** Every need is written on what it guards: the
   location, the exit into a region, the story event. No item, ability or member lists what it unlocks. Which items
   matter is read back from the rules (Archipelago's `item_dependencies`, in `TestClassifications`, which fails unless
   exactly the items rules use are marked progression in `data/items.json`), so the rule decides and the item's class
   follows it.
2. **A need is what the vanilla game expects:** what the game asks of a player going the intended way, with no tricks,
   skips or clever routes. The rules stay simple, players stay free to go out of logic, and every seed stays
   completable (rule 5).
3. **And, or, never not.** Everything one way needs is an *and* (`&`). When there are several ways, each is written
   and any one will do (*or*, `|`): a second way into an area is a second exit into its region (the region graph does
   the *or*); two ways to one spot inside an area are an `|` in the spot's own rule. Never a *not* on an item or a story
   event: in Archipelago, receiving something may never make anything harder to reach (the same reason the tiered shop
   rule was wrong, `apimplementation.md`, build step 11). An option may decide a rule, since it's fixed when the seed
   is made.
4. **No point of no return in the logic** (the user: never expected "to go past a point of no return, where they can't
   logically go back"). A one-way (a ledge dropped without Jump, a door with no way back, a transfer that leaves you
   somewhere) counts in the logic only together with what it takes to get back. So the player can always retrace their
   steps to the start, which is what Archipelago assumes of its origin region. The Warp is never that way back.
   **Unless the player turns on Points of No Return** (the user, 2026-09-30, off by default): then a one-way counts on
   its own need, and the Warp to Start is the way back, as Archipelago's "save and quit" is for its origin (`world
   api.md`). Every one-way is written with `one_way(rule, way_back)`, so the option drops only the way back (none has
   its way back written yet: build step 37).
5. **The logic may demand more than the game does, never less.** A rule that asks for too much only makes a seed a
   little stricter; a rule that asks for too little can place an item somewhere the player can't reach, and the seed
   is impossible. Anything not yet measured is written the cautious way.
6. **Nothing counts as known until the tester has seen it on screen.** Each need goes into `MEASURED.md` with its date.
7. **The mod never departs from what the generator knew.** Anything it changes comes from `slot_data`, decided at
   generation, never at runtime.
8. **Combat stays basic:** only each member's plain attack, never a battle skill or a medal. It keeps fights simple
   and leaves room to play out of logic for fun.
9. **The Warp is a safety net, never logic** (the user, 2026-09-29). It always takes you back to the seed's spawn, and
   it's forced on with a random start, the entrance randomizer, Shuffle Jump and the abilities as items, so a player
   who leaves the logic is never stuck. The logic never counts it, nor the map's fast travel, to reach anything, the
   start included. (Until 2026-09-29 build step 15 counted it to re-enter a random start; rule 4 does that job now.)
   With Points of No Return on, the Warp counts as the way back to the start and nothing else: never a way in, and the
   map's fast travel still never counts. The Warp is forced on with it too.

## The model

- **A room is not one region.** Split it into *areas*, each a region of its own. Make one for:
  - a part of a room you can't walk to freely from the rest (an obstacle, a ledge, water, a one-way drop);
  - where a door or a transfer puts you (each arrival is a place of its own), and where a scene moves the party;
  - a place several spots share a need in, or a place with more than one way in.

  Don't make one for a single spot that needs something extra where you can otherwise walk around: that's the spot's
  own rule. Every door, transfer, save point and location sits in one area.
- **A spot's own rule is written even when its area already implies it,** so a different way into the area (the
  entrance randomizer, a random start) can't lose it (learned in build step 8; test `TestInRoomRules`).
- **Each edge between areas is one-directional**, with its own requirement. A two-way path is two edges. A path with
  no edge back is a one-way, and it counts in the logic only together with what it takes to get back (rule 4). It's
  written `one_way(rule, way_back)` (`custom_rules.py`), never with the two joined by hand, so Points of No Return can
  drop the way back; a one-way transfer carries it as its `way_back`. Where the Warp can't be used (question 20, C9), the
  way back is part of the plain rule instead, and stays with the option on.
- **A need names what the game checks:** an ability (the game's names, `abilities.py`, written `CanUse(...)`: the
  ability names its member), a member for a fight (`Member(...)`, question 8), a basic move's item alone only as the
  blanket rule for ground not yet measured (`MoveItem(...)`, in `ALL_ATTACKS`), a key
  item, a story event (`Has(...)`; the boat's two levels `Boat(1)` and `Boat(2)`). Never what an item opens (rule 1). "Only before flag Y" is a *not*, which a rule
  can never say (rule 3): see [Chains](#chains-what-other-rooms-do-to-this-one), question C5.
- **Several ways, each written:** two ways into an area are two edges into it; two ways to one spot inside an area are
  an `|` in the spot's rule (rule 3).
- **Where it stands today** (2026-09-30): each map is one region with its doors as entrances (build step 12), and what
  the old large areas needed is kept on each spot as its `reach`. Mapping a room splits its region into areas and
  replaces its spots' `reach` with the room's own rules.
- **Unused and test maps are never part of anything** (the user, 2026-09-30): no region, no logic, never the target of
  a door, a transfer or a spawn, never reachable. They are `UNUSED_MAPS` (`data_tables.py`): `SnakemouthEmpty` and
  `TestRoom`; `TestUnusedMaps` proves no region, entrance, transfer, spot, encounter, start or shuffled door names
  either. `Blank`, the only other map named like one, isn't unused:
  a scene passes through it (Event111, `LoadMap(115)` then `DesertEastmost`), so it's a scene-only room, never a start
  (S1).

## Where the party can appear

A door's arrival is only one of the places the party can appear, and each is checked like an entrance: a spot can sit
where no entrance shows it, between obstacles, behind a roadblock, above a ledge without Jump, or on the far side of a
one-way (the user, 2026-09-30: "check the spawn location … not just all the entrances").

**The kinds of spawn:**

- **A door's arrival, as the game's own door does it:** the party appears at the door's spot, then walks or jumps in,
  with the door's camera (`MEASURED.md`, "Doors paired with their way back", and "Save crystals, saving, Game Over and
  room transfers": what the calling door adds).
- **Where a transfer that isn't a door puts the party:** a scene's `LoadMap` and the position it sets, a dialogue
  line's `|warp,map,x,y,z|`, being caught (the hideout cell) (`MEASURED.md`, "Transfers that aren't doors"), the
  trapdoor (`MEASURED.md`, "Chapters", Leif's joining chain). The game has no default spot per map: each transfer names
  its own, and one that names none leaves the party at the coordinates it had (`MEASURED.md`, "Save crystals, saving,
  Game Over and room transfers": "No map has a spawn spot of its own").
- **A save point's spot** (Starting Location's *Save Points* value, designed, build step 15).
- **The random start:** a door's arrival or a transfer's spot in any actual room (the user, 2026-09-30: both kinds, "i
  want random spawn to actually be random not just 'semi random'"). As built today it isn't yet a door's arrival (build step 15).
- **Where Warp to Start lands:** the start's own spot.
- **Where a fall or a hazard puts you back** (`lastpos`), **and where a loaded save puts you:** never a way through or
  out (question 17).
- **Past a blocked walk-in:** when a door's walk-in point is behind a barrier, the game moves the party past it after a
  timer (`MEASURED.md`, "Chapters", "A blocked walk-in ends in a teleport"). Never counted.

**The questions, per spawn:**

- **S1. Is it an actual room?** The vanilla game lets the party walk around there as themselves: never a test room, a
  minigame, the submarine's lake, or a room only a scene passes through (the user, 2026-09-30: "random spawn should
  only be for actual rooms"). Read from the scenes that load it: does control come back in that map, or does the scene
  move on? A room that fails is no start, and stays in the logic as the transfer it already is.
- **S2. Which area does it land in?** Can the walk-in itself be blocked?
- **S3. In which story state?** A random start is a new file, so every blocker the story removes later is still there.
  The Warp brings you back in any later state.
- **S4. With nothing at all** (one member, no Jump, no attacks, no abilities), which exits and locations can be reached,
  and what does each of the others need?
- **S5. Does arriving start a scene out of order** (a map's auto-start scene; one crashed when a warp arrived out of
  story order, `MEASURED.md`, "What the mod's code relies on")? The mod holds it (`held_until`), or it's a rule.
- **S6. Is it safe to leave?** A start is safe while every way out of it is free and every way back in is something the
  logic already gates (the Metal Island case, build step 15, 2026-09-26). Leaving counts in the logic only together
  with what it takes to get back (rule 4); a player who leaves anyway has the Warp.
- **S7. Can it be boxed in?** The Warp can't rescue a boxed-in spawn: it lands on the same spot. A spawn from which no
  exit and no location can be reached with nothing is denied.

**The verdict:** every room's spawns are *confirmed* or *denied*, each with its date, its reason and the user's
confirmation on screen. Denied ones leave the start pool; a room with no working spawn is denied only with the user's
word. Unchecked ones stay in while Starting Location is experimental (stuck starts are accepted until then); the label
comes off when none is left unchecked.

**The logic is to start where the spawn is** (planned, `archipelago-review.md`, item 23): Menu joined to the spawn's
area, so the start table names the exact spawn, never just a pair of maps. Today Menu always joins the game's own start
(`regions.py`).

## Chains: what other rooms do to this one

A room's state is set by flags, and most are set somewhere else: a scene in one room opens a door, moves an NPC, removes
a blocker or makes a location appear in another. So besides the rooms, every chain is checked across the game, step by
step (the user, 2026-09-30: "check every cutscene chain & quest chain, to account for other rooms affecting things";
"look across rooms, too", 2026-09-28).

**What counts as a chain:**

- **The story**, chapter by chapter: each scene and the flags it sets.
- **Board quests, and taking one:** a quest shows on a board only once its unlock is met (its `QuestChecks` row: flags,
  a visited area, or a dialogue line that adds it; `MEASURED.md`, "The quest board").
- **Requests that aren't on the board:** someone who wants something (the user's example: the kid given the G-Bug
  Ranger Plushie). Found in the dialogue data, not in the board table.
- **Followers who walk with the party from another room** (the throne room needs Maki, from two rooms away).
- **Joining scenes:** Leif's, Event4 → 5 → 6 → 18 → 14; each expects the one before, and a file that skipped part of it
  crashes entering its middle, with one exception seen (`MEASURED.md`, "Chapters").
- **Scenes that hold each other in order:** chapter 2's opening, the first boss, the follower, the swap on the palace
  bridge, the briefing (build step 9).

**The questions, per step:**

- **C1. Where is it** (room, area), **and what starts it:** talking, touching, arriving (a map's auto-start scene), a
  switch, a dialogue line?
- **C2. What does it need:** the step before, an item, a member, a follower walked in from another room? The route
  with the follower is part of its rule. When it isn't a quest (a scene that happens to want a follower), the mod may
  remove the need for good so the room works on its own: always on in a seed, never part of *Skip cutscenes*, since
  the logic counts on it (2026-09-28).
- **C3. What does it set, and what does each flag change in every other room:** a door shown or hidden, a blocker, an
  NPC, scenery, a dialogue line, a location? An area closed "until chapter N" is closed by several things at once
  (blockers, doors, scenery): list every entity and scenery piece on that flag before opening it (learned in build
  step 9).
- **C4. Can it be reached out of order** (a random start, a shuffled door, the open world)? Where the game expects the
  step before (a crash, a scene with the wrong follower), the logic needs the step before, or the mod holds the scene
  until it (`held_until`), decided per step. Opening a gate means checking every scene behind it for what the story
  guaranteed (learned in build step 9, the boat seating three).
- **C5. Does it take something away** (the first capture takes the beemerang; chapter 6 takes the boat; a follower
  leaves; a door hides)? A rule can't say "only before" (rule 3): it's kept from happening in a seed, or the logic
  never counts on what it takes.
- **C6. Does it move the party?** Then it's an edge (question 9), often a one-way.
- **C7. Is it a step in another room?** Then it's a logic event in that room's area, and the reward needs the whole
  chain (`TestOldBookChain`). An item handed out mid-chain is progression (the Quest Book; the G-Bug Ranger Plushie
  too, once its request is a location). Known gap: the lost kid's reward (location 10) doesn't need the sister's step
  yet.
- **C8. Is taking it a step of its own?** Reaching a board isn't enough when the quest's unlock needs a scene, a flag
  or a visited area: the unlock is a need of the "taken" step.
- **C9. Does warping out in the middle of it break it** (a follower left behind, a scene half done)? Then the one-ways
  along that step keep their way back as a plain rule, so Points of No Return never counts on the Warp there.

**The room side** (2026-09-28, after chapter 6 took the boat away): for each room, list every flag its objects, doors,
scenery and scenes read (a thing shown, hidden or moved, a blocker added or removed), and for each: what sets it and
when, which chain it belongs to, and whether a seed can reach that. A flag that can take a way through away (a boat, a
bridge, a door) is either kept from happening in a seed or becomes a rule; one that adds a roadblock is a rule. **No
room is done until every flag it reads names its chain.**

**The draft from the data** (planned): a flag cross-reference, each flag with who sets it (scenes in the code,
dialogue lines' flag commands, switches) and who reads it (entities' `requires` and `limit`, `mapflags` scenery,
dialogue lines, doors, map auto-start scenes, code), per room. Today `gate-table.py` does it for doors only.

## The questions, per room

**Entrances**

1. Where does each entrance put you (its arrival area)?
2. Is it a one-way? Can you leave the way you came? Does it only work in one direction (a drop, a door that opens
   from one side, a transfer made by a scene)? **A transfer that isn't a door** (decided 2026-09-25): one the player
   chooses (the bar's hatch, elevators, the boat, the submarine's docks, the user, 2026-09-30) is a door in all but
   name, shuffled like one and coupled with its way back where it has one, behind its own toggle at first; one the game
   sends you through (caught by guards, a fall, a story scene) keeps its destination and is a one-way in the logic,
   which must make sure you can leave where it puts you.
3. Does it become a one-way only from some entrances, because the way back needs something you might not have?
   **Check both directions:** keeping one direction of a way open means checking the other (learned in build step 9,
   the trapdoor's way back).
4. Is it open only in some story states (a door opening on a flag, a blocker that leaves)? See
   [Chains](#chains-what-other-rooms-do-to-this-one).

**Crossing the room**

5. Between each pair of areas: does it need an ability? Grass: Horn Slash; a gap: Bee Fly; water: Icicle platforms;
   droplets and fountains: Freeze; hazards: Shield; a roadblock: Beetle Dig or the Horn Dash; a switch across a gap:
   the Beemerang; a spinning mechanism: Beemerang Halt, not just the Toss (`MEASURED.md`, what each ability opens).
6. Is there a ledge that needs Jump? Walking down one without Jump is a one-way.
7. Does anything move you one way (wind, water, conveyors, moving or rotating platforms, a spring)?
8. Is there a fight you can't avoid? It must be winnable with the members' plain attacks (rule 8): an enemy in the air
   needs Vi, a burrowed one Leif, one that can be flipped over Kabbu, expected even where the others could win without
   him (the user, 2026-09-27; the five are in `MEASURED.md`, "Who can hit what").
9. Does a scene move you? A cutscene can put the party somewhere else in the same map or on another map (the
   trapdoor: the door room to the fall room): from which area, to which, once or every time, on what flag. It's an edge like any other, often a one-way.
10. Does anything change once and stay changed (a switch that stays down, a bridge lowered, a rock broken)? That's an
   event in the logic, reachable from wherever it can be triggered, and it may open a way in both directions.

**Each location**

11. Which area is it in, and does reaching it from there need anything?
12. Can you get back from it and leave the map with what reaching it took? If not, the way back is an edge too.
13. Does it need something only when you arrive from a particular entrance?
14. Is it there only in some story states (an NPC present from a flag, a pickup that respawns)? See
   [Chains](#chains-what-other-rooms-do-to-this-one).

**The whole room**

15. Every place the party can appear in it: see [Where the party can appear](#where-the-party-can-appear).
16. With one member only: which of the above does each member manage alone? A scene that needs a member who isn't
   there waits: held in the game, and a rule in the logic for any check it gives (build step 13).
17. Does falling (a pit, water) put you back somewhere? A respawn never counts as a way through or out.
18. The Warp is on in every randomized mode: the player's way out of a dead end, never the logic's (rule 9).
19. Every door here the door table couldn't pair, and every one on `MEASURED.md`'s "To check in play" list: which way
   can it be crossed?
20. Wherever a one-way leaves you, can the pause menu open, so the Warp works? It's hidden in battle and refused while a
   scene or dialogue runs. Where it can't be used (a scene that can't be paused), the way back is part of the one-way's
   plain rule, never `WayBack`, so Points of No Return can't count on the Warp there.

## How a room gets mapped

1. **A draft from the data.** The entity dump lists every object in the map by type: `BeetleGrass`, `PushRock`,
   `DigWall`, `DigSpot`, `BreakableRock`, `JumpSpring`, `Dropplet`, `Geizer`, `WindPusher`, `Switch`,
   `RotatingPlatform`, `PathPlatform`, `TempPlatform`, plus the doors (`door-graph.py`), the flag-gated doors
   (`gate-table.py`), the transfers scenes make (`event-transfers.py`), the flag cross-reference (planned, see
   Chains), each spawn's spot, and, still to script, the scenes that move the party within a map (story events that
   set the player's position).
   **Jump, drafted from the ground itself (planned; the user, 2026-09-27: Madeleine's rocks look walkable and aren't):**
   a dev tool loads a map's collision, samples the ground on a grid (as the console's `solids` reads colliders), and
   links neighbouring spots by height: within the walkable step height, walk; above it but within the jump height,
   jump; higher, or a wall between, no way; down always, so a jump-only way up is a one-way down. From every door,
   save point and location it then marks what a walk reaches and what needs a jump. Two numbers first: the step
   height and slope limit (between Madeleine's rock, not walkable, and the ladybug house stump's side, walkable:
   `MEASURED.md`) and the jump height (from the jump's speed and the gravity, read in code, checked on a ledge known to
   be just jumpable). It can't know invisible walls, one-sided colliders, blockers that come and go with the story,
   moving platforms or springs: a draft, confirmed on screen. A way it finds counts only once the tester says it's the
   intended one (rule 2). A script turns those into the room's checklist with a guess at each requirement.
   **Which rooms first:** those with early spots that need no Jump (the fill error with *minimal* accessibility and
   Shuffle Jump, `apimplementation.md`, Known issues: more early spots is its fix).
2. **Checked on screen by the user**, one area at a time, with the dev console to test what the draft can't know:
   warp in through each entrance and at each spawn, and try each way across without the ability or without Jump.
   Record what was seen, with the date, in `MEASURED.md`.
3. **Written into the logic** (the area's module, `logic/<area>.py`: areas as regions, their edges with their rules,
   each location in its area with its own rule), cautious where anything is unmeasured, replacing the spots' `reach`.
   A Placeholder is promoted to a normal location once its requirements and name are checked, one at a time (build
   step 10).
4. **Tested:**
   - each measured need has a test that fails without it, Archipelago's `assertAccessDependency`: the listed spots
     can't be reached without the item, and no other spot depends on it;
   - `item-gates.py`'s report, read again against the game (`development.md`, "What each item gates"): what each item
     and each pair of items gates, where a gap or a stand-in shows;
   - every area reachable from every arrival once everything is collected;
   - rule 4, from the first area mapped (2026-09-29): every one-way's rule holds what its way back needs;
   - every confirmed spawn reaches an exit or a location with nothing;
   - each chain's reward fails without each of its steps;
   - a Logic Test play-through (`development.md`, "Play-testing the logic"): *stuck* means the logic is looser than the
     game, an *early key* that it's stricter; it matters most for the experimental options.

**The safeguards already in place:** the tests generate seeds across option sets and check they're beatable;
`TestClassifications` fails unless exactly the items rules use are marked progression; one location per ability; the entrance
randomizer and a random start stay labelled experimental until their room-level logic is done and tested.

## Not in the logic, on purpose

- Battle skills and medals: combat stays basic (rule 8), which leaves room to play out of logic.
- The Warp and the map's fast travel as a way in (rule 9).
- Tricks, skips and clever routes (rule 2), a blocked walk-in's teleport among them.
- Anything seen to work only sometimes: the logic follows what always works.
- Unused and test maps (the model).

## The facts it relies on

In `MEASURED.md`, by section: "The door graph" and "Doors paired with their way back" (every door and its arrival);
"Transfers that aren't doors"; "Chapters" (Leif's chain, what each ability opens, a blocked walk-in, the flag-gated
doors); "The quest board" (every board quest, its accept flag and unlock); "Battles, for enemy shuffle" (who can hit
what); "Frame rate" (walking up versus jumping); "Save crystals, saving, Game Over and room transfers" (where a fall or a
save puts you); "The submarine".
