# Mapping a room for the logic

The working checklist for `apimplementation.md`, build step 24 (how we plan and build the logic).

How each room gets its logic, so it holds for **one party member** (random party), **a random start** (any room in the
game), **the entrance randomizer** (any door may lead anywhere, even decoupled), **shuffled attacks** (the Horn Slash
may not be there) and **no Jump** (a ledge may be a one-way). Written with the user, 2026-09-27. The rules it serves
are in `CLAUDE.md`: the logic may be more cautious than the game, never less, and every seed must be completable.

## The model

- **A room is not one region.** Split it into *areas*: the parts you can walk between freely with nothing. Every
  door, transfer, save point and location sits in one area.
- **Each edge between areas is one-directional**, with its own requirement. A two-way path is two edges. A path with
  no edge back is a one-way, and it counts in the logic only together with what it takes to get back: no point of no
  return (`apimplementation.md`, build step 24, rule 4).
- **Requirements name abilities** (the game's names, `abilities.py`, written `CanUse(...)`), never items or members;
  the ability names its member. Add a story condition when one applies: "needs X", "only after event Z" (a story
  event, `Has(...)`). "Only before flag Y" is a *not*, which a rule can never say: build step 12's flag survey keeps
  it from happening in a seed, or the logic never counts on that way.
- **Several ways, each written:** two ways into an area are two edges into it; two ways to one spot inside an area are
  an `|` in the spot's rule (build step 24, rule 3).
- **Every door and transfer has an arrival area**: where you stand when you come in through it. A random start and
  the entrance randomizer both start from arrivals.

## The questions, per room

**Entrances**

1. Where does each entrance put you (its arrival area)?
2. Is it a one-way? Can you leave the way you came? Does it only work in one direction (a drop, a door that opens
   from one side, a transfer made by a scene)?
3. Does it become a one-way only from some entrances, because the way back needs something you might not have?
4. Is it open only in some story states (a door opening on a flag, a blocker that leaves)?

**Crossing the room**

5. Between each pair of areas: does it need an ability (grass: Horn Slash; a gap: Bee Fly; water: Icicle platforms;
   hazards: Shield; a roadblock: Beetle Dig or the Horn Dash; a switch across a gap: the Beemerang)?
6. Is there a ledge that needs Jump? Walking down one without Jump is a one-way.
7. Does anything move you one way (wind, water, conveyors, moving or rotating platforms, a spring)?
8. Is there a fight you can't avoid? It must be winnable with the members' plain attacks (combat logic stays basic):
   an enemy in the air needs Vi, a burrowed one Leif, one that can be flipped over Kabbu, expected even where the
   others could win without him (the user, 2026-09-27; the five are in `MEASURED.md`, "Who can hit what").
9. Does a scene move you? A cutscene can put the party somewhere else in the same map (the trapdoor) or on another
   map: from which area, to which, once or every time, on what flag. It's an edge like any other, often a one-way.
10. Does anything change once and stay changed (a switch that stays down, a bridge lowered, a rock broken)? That's an
   event in the logic, reachable from wherever it can be triggered, and it may open a way in both directions.

**Each location**

11. Which area is it in, and does reaching it from there need anything?
12. Can you get back from it and leave the map with what reaching it took? If not, the way back is an edge too.
13. Does it need something only when you arrive from a particular entrance?
14. Is it there only in some story states (an NPC present from a flag, a pickup that respawns)?

**The whole room**

15. Spawning in each area (a random start lands at an arrival): can you reach every exit, or which ones, and with what?
16. With one member only: which of the above does each member manage alone?
17. Does falling (a pit, water) put you back somewhere? A respawn never counts as a way through or out.
18. The Warp is on in every randomized mode: it's the way out of a dead end, never a way *in* to anything.

## How a room gets mapped

1. **A draft from the data.** The entity dump lists every object in the map by type: `BeetleGrass`, `PushRock`,
   `DigWall`, `DigSpot`, `BreakableRock`, `JumpSpring`, `Dropplet`, `Geizer`, `WindPusher`, `Switch`,
   `RotatingPlatform`, `PathPlatform`, `TempPlatform`, plus the doors (`door-graph.py`), the flag-gated doors
   (`gate-table.py`), the transfers scenes make (`event-transfers.py`) and, still to script, the scenes that move the
   party within a map (story events that set the player's position).
   **Jump, drafted from the ground itself (planned; the user, 2026-09-27: Madeleine's rocks look walkable and aren't):**
   a dev tool loads a map's collision, samples the ground on a grid (as the console's `solids` reads colliders), and
   links neighbouring spots by height: within the walkable step height, walk; above it but within the jump height,
   jump; higher, or a wall between, no way; down always, so a jump-only way up is a one-way down. From every door,
   save point and location it then marks what a walk reaches and what needs a jump. Two numbers first: the step
   height and slope limit (between Madeleine's rock, not walkable, and the ladybug house stump's side, walkable:
   `MEASURED.md`) and the jump height (from the jump's speed and the gravity,
   read in code, checked on a ledge known to be just jumpable). It can't know invisible walls, one-sided colliders,
   blockers that come and go with the story, moving platforms or springs: a draft, confirmed on screen. A script turns those into the room's
   checklist with a guess at each requirement.
2. **Checked on screen by the user**, one area at a time, with the dev console to test what the draft can't know:
   warp in through each entrance, and try each way across without the ability or without Jump. Record what was seen,
   with the date, in `MEASURED.md`.
3. **Written into the logic** (the area's module, `logic/<area>.py`: areas as regions, their edges with their rules,
   each location in its area with its own rule), cautious where anything is unmeasured.
4. **Tested:** every area reachable from every arrival once everything is collected; no arrival strands the player
   without the Warp; each measured need has a test that fails without it.

## Not in the logic, on purpose

- Battle skills and medals: combat stays basic (a member's plain attack), which leaves room to play out of logic.
- The Warp as a way in.
- Anything seen to work only sometimes: the logic follows what always works.
