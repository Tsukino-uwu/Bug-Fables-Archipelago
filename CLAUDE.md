<!-- line-cap: 150 -->
# Working notes for Claude

An Archipelago randomizer for Bug Fables: a BepInEx 5 mod (C#, `mod/`) and an apworld (Python,
`apworld/bug_fables/`). `agent_docs/README.md` indexes every internal doc; this file holds only rules.

## RULE 0: the 150-line cap

**Run `wc -l CLAUDE.md` before adding a line. Going over 150 is a regression.** To add a rule, remove one in
the same edit. The rule lives here and its reasoning in `agent_docs/`, behind a one-line pointer.

## What never changes

- **`agent_docs/documentation.md` NEVER goes stale** (the user, 2026-09-24: a step-by-step guide that misses
  steps is worthless). It is the user-facing story of how this mod and apworld were made: easy to read AND
  to browse, one section per step, process only, no game facts. Status, next steps, known issues and the
  step index sit at the top; nothing a reader needs is buried deep in the file (the user, 2026-09-24). **A step is not done until it is written there, in the same
  commit**: a new capability, a tool or method adopted, a dead end and what it taught. `.githooks/commit-msg`
  refuses a commit touching `mod/`, `apworld/` or `dev-scripts/` without it, unless the message carries a
  line `docs: no process change` (a typo or rename, never a step). Re-read the whole file when a session starts.
- **Items are remote only.** A pickup grants nothing locally; it sends its check. Every item, the player's
  own included, arrives from the server. There is no local-items mode (decided 2026-09-24).
- **Never corrupt a save.** The mod writes game state only through the game's own functions (its give-item
  path, its flag setters). No raw writes into save data, and no new save format.
- **The received-item count lives in the save**, next to the items it produced. A fresh save starts at 0 and
  the server replays everything. That makes lost-save recovery work.
- **"In a seed" and "connected" are different states.** A dropped socket keeps randomizer rules in force:
  checks queue, and no pickup falls back to its vanilla item.
- **Game logic and apworld logic move together.** A gate in the game and its rule in the apworld are the same
  fact written twice. If the game is stricter, seeds can't be finished. If it's looser, the logic is decorative.

## Who verifies what

- **The user verifies the game; you verify the code.** The apworld, the tests and the mod's build are
  deterministic: confirm them with tools yourself. For anything inside the game, **"it ran without errors"
  is not evidence**, because a wrong hook fails silently. Only the user seeing it on screen settles it.
- **Nothing in-game is "verified" until the user confirms it on screen.** Your measurements (hooks, flags,
  save fields, with their evidence and date) go to `agent_docs/MEASURED.md`.
- **Never assume what the game is MEANT to do. Ask.** Name the exact state: "the main menu", never bare
  "menu".
- **No addresses, names or APIs from memory.** Every game class, field and hook traces to the decompiled
  assembly. Every Archipelago or MultiClient.Net call traces to its docs or source. Anything suspiciously
  tidy is invented until confirmed.
- **An apworld change is done when its tests pass** in the local Archipelago checkout (at the tag the world
  targets). A behaviour change gets a test that fails without the fix. Generate a seed with a second game
  as well: some failures only show up in a room with two different games (`agent_docs/client-requirements.md`).

## What may enter the repo

- **Nothing goes in that couldn't be published. The test is "fine in a public repo forever?"**, not "does a
  licence permit it?"
- **Never commit the game's files:** no `Assembly-CSharp.dll`, decompiled output, assets or saves. The build
  references the game DLL via `HintPath` to the user's own install. Decompiled source is read for facts only
  and lives in the gitignored `decompiled/`.
- **Read a project's licence before its source**, and add a row to `agent_docs/licensing.md`. A project
  with no row hasn't been checked, so don't use it. Reading is fine; copying source is not.
- **No personal username, home path or machine detail in any tracked file, prose included.** Write
  "your Bug Fables install" or "your Archipelago checkout". `.githooks/pre-commit` refuses the obvious
  cases; run `git config core.hooksPath .githooks` once per clone, and never use `--no-verify`.
- **A dated fact is true as of its date.** Archipelago, MultiClient.Net and the game all change without this
  repo changing, so re-check before a new use. Cite dates, never durations.

## Working with the user

- **Never suggest stopping, pausing or resuming later, not even as one option among several.** Silence
  means keep going until it works. If blocked, name the blocker and the next measurement.
- **Commit freely, straight to `main`; never create a branch. Push only when told to, in that message.**
  A past yes is not a standing one.
- **Ask before touching anything outside this repo.** That includes the game install (BepInEx, deploying
  the mod) and the Archipelago checkout.
- **Small runnable steps only**, each with a visible outcome.
- **You may start the game and the Archipelago server, but ASK FIRST, in the same breath as the rest of the
  setup.** Then close every process you started and check they're gone.
- **Hot reload is the default loop; restarting the game is the last resort.** BepInEx.Debug ScriptEngine
  reloads the plugin in a running game. Rebuild and restart only when a change can't be reloaded.
- **After about 3 failed live attempts, stop and table the results (config against outcome)**, then try the
  combination you haven't tested. Each attempt costs the user a game launch.
- **Test instructions use up/down/left/right**, never compass points.
- **Agent memory is for the user, not the project.** Decisions, measurements and status go in
  `agent_docs/` or the code, where the next session and a human can see them.
- **Before a session ends, add a dated entry to `agent_docs/log.md`**: what was tried, what happened, what
  the user said.

## Method

- **A diagnostic can break what it measures.** Probes and debug logging are off by default. Re-run with
  them off before believing a result.
- **Never log the value you just wrote as proof it worked.** Read it back through a real getter. After a
  scripted edit, grep the result.
- **Two guessed fixes failing the same way is a signal:** isolate by subtraction; never try a third guess.
- **Log what a guard decided, not just what happened.** Almost every Archipelago failure fails silently or
  reports success (`agent_docs/client-requirements.md`, failure modes).
- **Anything on `PATH` may resolve to the wrong install.** Run a `.bat` via `& $env:ComSpec /c`, never bare
  `cmd`; call tools by absolute path when it matters.

## Records and docs

- **`agent_docs/client-requirements.md` is the checklist:** Archipelago's hard requirements for a client and
  a world, plus the known failure modes. Tick an item only with its evidence and date.
- **`agent_docs/MEASURED.md`** holds game facts: class, method, field and flag, each with its evidence and
  date. What a source says but we haven't measured waits in its last section.
- **When the user confirms a fix, write down HOW it was found** (wrong theories, the measurement that
  settled it) in `agent_docs/log.md` before moving on.

## Read before

- **The Archipelago docs in the local checkout, at the targeted tag:** `adding games.md` (the hard
  requirements), `world api.md`, `rule builder.md`, `apworld specification.md` (package with the
  "Build APWorlds" launcher component, never with hand-written `version` fields) and `tests.md`.
  `worlds/apquest` is the structural reference.
- **`agent_docs/references.md`** before borrowing an approach from another randomizer.
