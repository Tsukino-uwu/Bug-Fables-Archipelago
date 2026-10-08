<!-- line-cap: 150 -->
# Working notes for Claude

An Archipelago randomizer for Bug Fables: a BepInEx 5 mod (C#, `mod/`) and an apworld (Python,
`apworld/bug_fables/`). `agent_docs/README.md` indexes every internal doc; this file holds only rules.

## RULE 0: the 150-line cap

**Run `wc -l CLAUDE.md` before adding a line. Going over 150 is a regression, and preflight refuses it.** To add a
rule, remove one in the same edit. The rule lives here and its reasoning in `agent_docs/`, behind a one-line pointer.

## What never changes

- **Archipelago's way, always; never reinvent it** (the user, 2026-09-29; again 2026-10-08). The whole project follows
  every standard AND recommendation ("should", "recommended", "encouraged") in Archipelago's `docs/` at the targeted
  tag, and supports every optional feature, plando included. Read every doc, guide and reference world, never assuming
  one doesn't apply. **Before writing anything that talks to the server or a tracker, read the source of what it uses**
  (Archipelago, MultiClient.Net, not just its DLL, PopTracker); use what it provides; custom only if it has none (§8).
- **The two process guides NEVER go stale** (the user, 2026-09-24: a step-by-step guide that misses steps is
  worthless). `agent_docs/documentation.md` is how the MOD was made; `agent_docs/apimplementation.md` is the
  Archipelago side (apworld, server, connecting, items, checks), built step by step, plus a stable explainer. Both
  user-facing: easy to read AND browse, process only, no game facts, the index first. Status lives in each step's last
  line (**Status:**), changed in the same commit as the work, never in a summary; Next and Known issues live once, in
  apimplementation.md's "Where it stands". **A step is not done until it is written in the right one, in the same
  commit** (`.githooks/commit-msg` refuses a commit touching `mod/`, `apworld/` or `dev-scripts/` without either,
  unless a line says `docs: no process change`: a typo, never a step). Re-read both when a session starts. **A feature
  gets its own step** when it adds a yaml option or panel setting, a new kind of location, or changes how the game
  plays in a seed; everything else joins the step it belongs to, or one the user merges it into. **In doubt, its own
  step:** the user can merge it later, while an unwanted merge goes unnoticed. Say so in the commit message. The
  pre-commit `doc-coverage.py` refuses an option, setting, `slot_data` key or source file the docs don't name.
- **The root `README.md` never goes stale either.** It has no Status line (the user, 2026-10-08: the project only grows
  more complete); its intro must agree with the code. Fact-check it against the code when a session starts, never
  against an older doc. It stays minimal, links only (like MeshGhost's); player docs live in `apworld/bug_fables/docs/`,
  same rule.
- **Items are remote only.** A pickup grants nothing locally; it sends its check. Every item, the player's
  own included, arrives from the server. No local-items mode (decided 2026-09-24). One named exception: a
  respawning pickup whose check is done is the game's own again (`apimplementation.md`, build step 10).
- **Randomizer saves are separate files** (decided 2026-09-24). With the randomizer on (a main-menu toggle), the game
  reads and writes its own save files in a separate folder. Normal saves and Steam Cloud's copies are never written.
- **An item that can unlock even one location, at any point, is progression. No ifs or maybes** (the user,
  2026-09-24): even if it only sometimes does, even if not always. `TestClassifications` enforces it.
- **Vanilla stays vanilla** (the user, 2026-09-24). Everything the mod does, every panel setting (Difficulty,
  Detector, any future one) included, applies only while Archipelago is enabled. Gate each new effect on it.
  One named exception: the opt-in row *Use on normal saves* (off by default; `documentation.md`, step 18).
- **Never corrupt a save.** The mod writes game state the way the game itself does: the same fields, the same
  caps, the game's own function where it has one (`MEASURED.md`, what `Giveitem` writes). Saves go only through
  the game's own save code. No raw writes into save files, and no new save format.
- **The received-item count lives in the save**, next to the items it produced. A fresh save starts at 0 and
  the server replays everything. That makes lost-save recovery work.
- **"In a seed" and "connected" are different states.** A dropped socket keeps randomizer rules in force:
  checks queue, and no pickup falls back to its vanilla item.
- **Every seed can be completed from wherever it starts** (the user, 2026-09-24). Whatever an area or goal needs (key
  items, party members, abilities) is a rule in the logic, never handed out by the mod to patch a gap, random start too.
  **The world is open, never done for you:** each quest available from the start, none of it begun (build step 44).
- **A generated seed is NEVER impossible** (the user, 2026-09-24): every item is reachable in logic, and the game never
  makes it harder than the logic. Two halves. **(1) The logic tells the truth:** Archipelago proves a seed only by the
  logic, so every gate the game has (a story flag, an ability check) goes into the apworld; the logic may be more
  cautious than the game, never less. **(2) The mod never departs from what the generator knew:** anything it changes
  comes from `slot_data` or from tables built from the same apworld (checked by world version on connect), decided at
  design time, never at runtime. **Named allowances (the user, 2026-09-25/26):** an unchecked "Placeholder" location
  holds filler only; the entrance randomizer and a random start are options labelled experimental until the room-by-room
  logic is done (`apimplementation.md`, build steps 12 and 15).

## Who verifies what

- **The user verifies the game; you verify the code.** The apworld, the tests and the mod's build are deterministic:
  confirm them with tools yourself. For anything inside the game, **"it ran without errors" is not evidence**, because
  a wrong hook fails silently: **nothing in-game is "verified" until the user confirms it on screen.**
- **Never assume what the game is MEANT to do. Ask.** Name the exact state: "the main menu", never bare "menu".
- **No addresses, names or APIs from memory.** Game code traces to the decompiled assembly, Archipelago and
  MultiClient.Net calls to their docs or source; anything suspiciously tidy is invented until confirmed. **ALWAYS
  read how the game does a thing before building or fixing ours** (the user, 2026-09-26): the proven example.
  **A fact about another project is read (licence first) or asked, never written from memory** (the user, 2026-09-30).
- **An apworld change is done when its tests pass** in the local Archipelago checkout (at the tag the world
  targets). A behaviour change gets a test that fails without the fix. Generate a seed with a second game
  as well: some failures only show up in a room with two different games (`agent_docs/client-requirements.md`).
  **Tests always run with the fuzzer** (the user, 2026-09-28): `dev-scripts/test-apworld.ps1`, 0 failures in 10000.

## What may enter the repo

- **Nothing goes in that couldn't be published, or could harm whoever runs it** ("fine in a public repo forever?", not
  "does a licence permit it?"). `preflight.py` enforces it: pre-commit, pre-push, CI, release (build step 28).
- **Never commit the game's files:** no `Assembly-CSharp.dll`, decompiled output, assets or saves. The build
  references the game DLL via `HintPath` to the user's own install. Decompiled source is read for facts only
  and lives in the gitignored `decompiled/`.
- **A project with no row in `agent_docs/licensing.md` is off limits, its licence included, until the user says
  yes** (the user, 2026-10-01); then its licence first, then its row. Reading is fine; copying source is not. **Never
  call another project bad or list its flaws in the repo** (the user, 2026-09-30): blunt findings go to its
  developer directly. Not covered: notes on a library behaviour our code works around, and facts about the game.
- **No personal username, home path or machine detail in any tracked file, prose included:** "your Bug Fables install".
  **`docs/capabilities.md` is what the code may do:** widening it or the preflight's patterns is the user's call, never
  a way to make a check pass; **ask them when either blocks Archipelago's standard way, never code around it** (the
  user, 2026-10-03: welcome). `git config core.hooksPath .githooks` once per clone; never `--no-verify`.
- **A dated fact is true as of its date.** Archipelago, MultiClient.Net and the game all change without this
  repo changing, so re-check before a new use. Cite dates, never durations.

## Working with the user

- **Never suggest stopping, pausing or resuming later, not even as one option among several.** Silence
  means keep going until it works. If blocked, name the blocker and the next measurement.
- **Commit freely, straight to `main`; never create a branch. Push only when told to, in that message.** A past
  yes is not a standing one. Subjects: imperative, 72 characters at most, no attribution; `Seen:` = seen by the user.
  **Never wait on CI after a push** (the user, 2026-09-29): read `gh run list` when a session starts; red comes first.
- **Ask before touching anything outside this repo:** the game install (BepInEx and its setup) and the
  Archipelago checkout. One standing exception, below: copying the plugin in with `copy-dev.ps1`.
- **Small runnable steps only**, each with a visible outcome.
- **You may start the game and the Archipelago server, but ASK FIRST, in the same breath as the rest of the
  setup.** Then close every process you started and check they're gone.
- **Copying the plugin into the game is yours, no asking** (the user, 2026-09-24, as in MeshGhost): only
  through `dev-scripts/copy-dev.ps1` (plugin plus `-DebugOn`/`-DebugOff`, with backups), always called from
  the PowerShell tool; never an ad-hoc `cp`, `sed` or `Set-Content` into the install (`development.md`, step 3).
- **Hot reload is the default loop; restarting the game is the last resort.** BepInEx.Debug ScriptEngine
  reloads the plugin in a running game. Rebuild and restart only when a change can't be reloaded.
- **After about 3 failed live attempts, stop and table the results (config against outcome)**, then try the
  combination you haven't tested. Each attempt costs the user a game launch.
- **Test instructions use up/down/left/right**, never compass points.
- **Agent memory is for the user, not the project:** decisions, measurements and status go in `agent_docs/` or the code.
- **Before a session ends, add a dated `agent_docs/log.md` entry:** what was tried, what happened, what the user said.

## Method

- **A diagnostic can break what it measures:** probes and debug logs stay off; believe a result only with them off.
- **Never log the value you just wrote as proof it worked:** read it back through a real getter; grep a scripted edit.
- **Two guessed fixes failing the same way is a signal:** isolate by subtraction; never try a third guess.
- **Log what a guard decided, not just what happened.** Almost every Archipelago failure fails silently or
  reports success (`agent_docs/client-requirements.md`, failure modes).
- **Anything on `PATH` may resolve to the wrong install.** Run a `.bat` via `& $env:ComSpec /c`, never bare
  `cmd`; call tools by absolute path when it matters.

## Records and docs

- **`agent_docs/client-requirements.md` is the checklist:** Archipelago's hard requirements for a client and
  a world, plus the known failure modes. Tick an item only with its evidence and date.
- **A location is named after where it is, never after what it gives** (`apimplementation.md`, build step 1;
  `TestLocationNames`). **Every name needs the user's yes first: ask, never make one up** (the user, 2026-10-04).
- **`agent_docs/MEASURED.md`** holds game facts, your measurements among them (class, method, field, flag, hook, save
  field), each with its evidence and date. What a source says but we haven't measured waits in its last section.
- **Comments are lean** (the user, 2026-09-25). One line, only where the code can't say it (a game quirk, a
  non-obvious why). No provenance, dates or decompiled line numbers in code: those go to `agent_docs/`.
- **Before changing a file, look it up in `agent_docs/code-map.md`** and read the notes it links. A new file
  gets its row in the same commit; a fact moved out of code ends "Used by `File.cs`" so its name finds it.
- **When the user confirms a fix, write down HOW it was found** (wrong theories, the measurement that
  settled it) in `agent_docs/log.md` before moving on.

## Read before

- **Archipelago's docs, before any Archipelago work** (the rule above): `adding games.md` (the hard requirements),
  `world api.md`, `rule builder.md`, `entrance randomization.md`, `tests.md`, `style.md` and `apworld specification.md`
  (package with the "Build APWorlds" component, no handwritten `version`); `worlds/apquest` is the structural reference.
- **`agent_docs/references.md`** before borrowing from another randomizer. **Never look at another Bug Fables one.**
