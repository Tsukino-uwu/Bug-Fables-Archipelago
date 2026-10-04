# Reviewing this yourself

A guide for anyone checking this project before running it: which code runs on whose machine, what the project
claims about it, and how to check each claim yourself. Nothing here asks you to trust the author or the tests; it
points at what to read and what to run.

## How AI is used

This project has been made with the help of AI (an LLM), for the code and its documentation. This repo will never
contain any AI art, and AI has made no design, logic or naming decisions: it suggests, I decide.

In practice:
- **The agent** writes the code and the docs. It reads the game's decompiled code for facts. That output stays on the
  maintainer's machine and never enters the repo.
- **The maintainer** decides what the randomizer does (its features, options, logic rules and names) and verifies every
  change in the game on screen before it counts as done.
- **What was seen, and when:**
  - each step in the two process guides ([how the mod was made](../agent_docs/documentation.md), [the Archipelago
    side](../agent_docs/apimplementation.md)) ends with a dated **Status** line;
  - commits whose subject starts with `Seen:` record what was confirmed on screen;
  - [the log](../agent_docs/log.md) records what was tried and what the maintainer said.
- **The one picture the mod draws**, besides a plain ringed circle behind its pause-menu buttons, is the Archipelago
  icon: Archipelago's logo rebuilt from six circles in code
  (`mod/BugFablesAP/Ui/ApIcon.cs`); its look was picked on screen.

## Checks that need only git, Python and the release

Clone the repository, then download a release's three files (from its page, or `gh release download vX.Y.Z -D rel`)
and the NuGet package its libraries come from:

```sh
curl -fsSLo rel/mcn.nupkg https://api.nuget.org/v3-flatcontainer/archipelago.multiclient.net/6.7.1/archipelago.multiclient.net.6.7.1.nupkg
python dev-scripts/verify-release.py --ref vX.Y.Z --zip rel/bugfables-archipelago.zip \
    --apworld rel/bug_fables.apworld --yaml rel/bug_fables.yaml --nupkg rel/mcn.nupkg
```

It checks:
- **The mod zip** holds exactly the files of `release/mod` at that tag, byte for byte.
- **The apworld** holds exactly `apworld/bug_fables`, byte for byte, except for the repository's licence, which CI
  copies in before building, and the two version fields Archipelago's builder adds to `archipelago.json`.
- **The three library DLLs** are the NuGet package's own files.
- **For a release made after 2026-09-29,** the mod DLL passes the preflight's DLL checks (below).

Both earlier releases pass (v0.1.0 and v0.2.0, checked 2026-09-29).

**Without that script**, with standard tools only:
- `unzip -l` the zip, and compare with `git ls-tree -r --name-only vX.Y.Z release/mod`.
- `sha256sum` each file, and compare with `git show vX.Y.Z:<path> | sha256sum`.
- Compare each library with `unzip -p rel/mcn.nupkg lib/net40/<name>.dll | sha256sum` (Newtonsoft.Json is the
  `lib/netstandard2.0` one).
- **Releases after 2026-09-29:** the apworld and the yaml carry GitHub's provenance attestation, a signed record that
  this repository's release workflow built them from the tagged commit.
  `gh attestation verify rel/bug_fables.apworld -R Tsukino-uwu/Bug-Fables-Archipelago` checks it.
- **To read the mod DLL itself,** decompile it with [ILSpy](https://github.com/icsharpcode/ILSpy).

## What runs where

| You want to | What runs on your machine | The code to read |
|---|---|---|
| Generate a seed, or host one | The apworld: Python, run by Archipelago | `apworld/bug_fables/` |
| Play | The mod: a BepInEx plugin inside Bug Fables, with its three libraries | `mod/BugFablesAP/` except `Dev/` |
| Build, test or release | The dev scripts | `dev-scripts/`, `.githooks/`, `.github/workflows/` |
| Work on it with Claude Code | The agent's guard, before each command and edit the agent makes | `.claude/` |

**The apworld** runs on whichever machine generates with it in `custom_worlds` (the archipelago.gg website doesn't
have it), and Archipelago imports it on every start.
- It imports only the names the preflight lists: 38 from Archipelago and Python's standard library, plus
  `json.loads`, `logging` and `pkgutil.get_data`.
- It reads its own data files, which ship inside it, and nothing else.
- It writes no file, opens no connection, runs no program and evaluates no text as code.
- The preflight checks all of this from the syntax tree, not by searching text. Four places look an attribute up by
  a name held in a variable; [capabilities.md](capabilities.md) lists them and where each name comes from.

**The mod** runs inside the game. Beyond the game itself, it:
- **Connects to one server:** the Archipelago server address the player types into the in-game menu, through
  Archipelago.MultiClient.Net.
- **Keeps its own saves** in a folder of its own, so normal saves are never read or written.
- **Stores the connection settings** in its BepInEx config file, the room password included, in plain text on the
  player's own machine.
- **Uses the clipboard** only when the player presses paste or copy in one of the panel's text boxes.
- **Patches five things outside the game** at run time (v0.2.0 patches three; the cache's two came 2026-09-29):
  - two in the connection's libraries, to turn on compression;
  - two in MultiClient.Net's cache, to keep its file names inside its folder;
  - one Unity call, to skip a missing animation.

[capabilities.md](capabilities.md) lists each of these file by file. The preflight holds that list to exactly what
the source does and what the compiled DLL calls. Everything else the mod does is patch the game's own code: the
[code map](../agent_docs/code-map.md) says what each source file does and links the notes behind it.

**The libraries** (Archipelago.MultiClient.Net, websocket-sharp, Newtonsoft.Json) are NuGet's files, unchanged; the
preflight and CI check them against the package on nuget.org.

**The dev tools** (`Dev/`) are compiled only into the maintainer's build. The release build leaves `Dev/` out, and the
preflight checks that the shipped DLL holds none of its types.

**The dev scripts** run only when someone runs them, and the git hooks on each commit and push once a clone arms
them. What each one does beyond reading files is listed in [capabilities.md](capabilities.md).

**The CI workflows** run on GitHub's runners, not on your machine. The actions and other repositories each one uses,
the packages it downloads and what it publishes are listed in [capabilities.md](capabilities.md) too, each action and
repository by name.

**If you open this repo in Claude Code**, its `.claude/settings.json` runs `.claude/hooks/agent-guard.py` before each
shell command, file edit and page fetch the agent makes. It refuses commands that would get past the git hooks, and
asks you before any read of a GitHub project that has no committed licence row (its licence included), a commit that
adds one, a change to what the gates allow (by an edit or in a commit), or a write through `gh api`. It reads the
command it's given (and, for a GitHub read, the licence list and the patterns file's own owners), runs only `git
status`, and changes nothing. The preflight holds the settings to that one command and to rules that ask or refuse,
never ones that allow more. Other editors ignore the folder.

## Where a server's data goes

A game in an Archipelago room trusts two outsiders:
- **the server;**
- **every other player in the room:** their slot names, and their games' item names, reach your game.

This is what the mod does with what they send, what it checks, and what it doesn't. It was read from the code on
2026-09-29, the game's and the libraries' included; nothing here was tested against a hostile server.

| What arrives | Where it goes | Checked on arrival | Known gaps |
|---|---|---|---|
| **Player and item names**, and the seed's name | The "You got X's item" boxes, shop names and descriptions, and pickups. The last one shown, and the seed's name, are also kept in the save | Since 2026-09-29, all through one class (`Core/ServerText.cs`): the game's command mark `\|`, the save's separators, and control and format characters are dropped, and each string is kept to 100 characters. The preflight refuses a raw read anywhere else | See the first gap below: fixed, not yet seen in game |
| **slot_data** (the seed's settings, at login) | The mod's features: which spots are locations, doors, enemies, the starting member, the goal | Read whole before anything uses it. A value of the wrong type fails the login, which is retried, and a missing setting turns its feature off | **Values are not range-checked.** An out-of-range flag or medal number throws an error; in per-frame code it is caught and logged, inside a game hook it is not. Enemy ids, the starting member and dialogue flags go to the game unchecked |
| **Received items** | The bag, key items, medals, berries, party members | The received count in the save is bounds-checked, and unknown item ids and kinds are skipped | Item numbers have no upper bound: an out-of-range one is added, and then its "You got" box fails |
| **DeathLink** | A Game Over, only when the player turned DeathLink on (on the map, back to the last save) | Joined with any death already waiting | Who sent it isn't checked; anyone in the room with DeathLink on can send one, as DeathLink is meant to work. The waiting list has no limit |
| **Chat and server messages** | Nowhere: the mod shows none | | |

**Gaps outside that table**, found in the same reading:

1. **Names could hold game text commands (fixed 2026-09-29).** Bug Fables' text engine runs commands written
   between `|` marks (colours, but also setting story flags, money, giving items, moving the party). It reads a
   substituted name as more text to run (`MainManager.SetText`, the `string` command). So until then a player or
   item name containing such a command would have run it in the game of whoever received or saw that item. A name
   holding the save file's own separators could also have corrupted the save when it was next loaded. Every such
   string now goes through `Core/ServerText.cs` first (the mod guide, step 33); the game shows it but can't run it.
2. **wss:// accepts any certificate.** websocket-sharp's default certificate check accepts everything, and the mod
   doesn't change it. When a bare address is typed, MultiClient.Net tries wss:// first and falls back to plain ws://
   if that fails. On a network you don't trust, the room password (sent at login) could be read or the connection
   intercepted. The encrypted connection only keeps out someone passively listening.
3. **MultiClient.Net's cache file path was the server's to decide (fixed 2026-09-29).** The library saves each game's
   names to `Archipelago\Cache\datapackage\<game>\<checksum>.json` in the user's local application data, a folder
   shared with other Archipelago clients that use it. In the version the mod ships (6.7.1), its "safe file name"
   function returns the name unchanged, so the game name and checksum the server sent became part of that path
   unchecked. The mod now patches that function to do what it was meant to, and cleans the checksum the same way
   (`Core/CachePaths.cs`, the mod guide's step 34); both patches are rows in [capabilities.md](capabilities.md).

## The capability list

[capabilities.md](capabilities.md) is everything the code may do that reaches beyond its own files, each with a
reason:
- the hosts it names;
- the apworld's lookups by name;
- what each mod file, each dev script and each git hook touches;
- the mod's patches outside the game;
- what each CI workflow reaches: the actions and other repositories it uses, packages, GitHub.

It is **exact**: the preflight fails on something the code does that isn't listed, and on a row nothing matches any
more. So `git log -p docs/capabilities.md` is the full history of every capability the project gained or dropped:
between two releases, `git diff v0.2.0 v0.3.0 -- docs/capabilities.md` (the release pages list features only).

## Run the gates yourself

All with Python 3.11 or newer and git; nothing else, no game:

```sh
python dev-scripts/preflight.py                  # every section, on the checked-out commit
python dev-scripts/preflight.py --history        # every file and commit message ever pushed
python dev-scripts/negative-test-preflight.py    # proves each section can fail (39 s on 2026-10-01)
python dev-scripts/dotnet_metadata.py --selftest release/mod/BepInEx/plugins/BugFablesAP/*.dll
```

Each preflight section prints what it checked and how much. A section that finds nothing to check fails rather than
passing, since "0 files scanned" would otherwise read as "0 problems". What each section refuses is data, in
`dev-scripts/preflight-patterns.json`, and each one is explained in
[apimplementation.md, build step
28](../agent_docs/apimplementation.md#build-step-28-the-preflight-nothing-unpublishable-in-the-repo-or-a-release).

**The negative test is the check on the checks.** It plants a violation for every section, in every mode each one
runs in, in a throwaway clone:
- a hidden bidi character in a doc, a homoglyph in code;
- every credential format;
- a home path inside the DLL;
- a byte changed in a library;
- a namespace in the DLL renamed to `System.Net`;
- a workflow that runs untrusted text.

For each one it confirms the section fails. It also makes a real commit and a real push carrying a violation, and
checks that both are refused, and runs the coding agent's guard (`.claude/`) on what it must refuse, ask about and
let through.

**The apworld's own tests** (621 on 2026-09-30) and the fuzzer (10000 random seeds, 0 failures before every change) need
an Archipelago checkout; [development.md](../agent_docs/development.md) says how. CI runs the tests, the Logic Test
check and the fuzzer (10000 seeds), builds the apworld and its yaml and generates from them as a player would, and
generates seeds with a second game, on every push.

## The committed DLL: what is proven, and what isn't

CI can't build the mod: compiling it needs the game's own `Assembly-CSharp.dll`, which never enters the repo. So the
DLL is built on the maintainer's machine and committed, and a hash written next to it proves nothing on its own: the
same person wrote both. Here is what can be checked.

**Without the game,** the preflight reads the DLL itself, the way the .NET runtime does:
- **It is a plain compiled library:**
  - only `mscoree.dll!_CorDllMain` imported;
  - no native code, embedded resources or stored data;
  - nothing after its last section.
- **What it can reach:** only the 13 expected assemblies. None of 42 denied kinds of call appears among its 12,762
  references: starting programs, `System.Net`, loading code, `Marshal`, the registry, code written at run time,
  opening web pages, reading who the player is, JSON picking its own type. Every file, clipboard, connection and
  lookup-by-name call is traced to the type that makes it and must be in the capability list.
- **It says only what its source says.** Against the sources of the commit it was built from, every type, method,
  field and called member name, and every one of its 1,237 strings, must appear in that source. Code in the DLL that
  the source doesn't have shows up as names and strings the source never wrote.

**What that can't show** is how the IL wires those allowed pieces together. Two things close that gap:
- **Rebuild it with your own copy of the game.** The build is reproducible: every published DLL rebuilt byte for byte
  from its own commit (v0.1.0, v0.2.0 and the committed one, 2026-09-29). The SDK is pinned by `global.json` and every
  package by a lock file. On Windows with the .NET SDK and the game installed:

  ```powershell
  git checkout vX.Y.Z
  powershell -ExecutionPolicy Bypass -File dev-scripts\build-release.ps1 -GameDir "<your Bug Fables install>"
  ```

  It builds the DLL twice from clean clones and says whether the result is byte-identical to the committed one.
  `release/built-from.txt` names the commit, the SDK and the game build it was compiled against.
- **Decompile it and read it.**

## What the gates cannot prove

- **Intent.** A check can prove a call is absent. It can't prove that a present one is used well: the save redirect
  writes files by design, and is worth reading (`mod/BugFablesAP/Core/SaveRedirect.cs`).
- **A preflight edited to weaken itself.** Anyone who can push could change the rules and the code together. The
  project makes that visible, not impossible:
  - a change to the preflight, its patterns, the hooks or the workflows can't share a commit with mod or apworld
    code;
  - the capability list's changes between two releases are one `git diff` between their tags.

  `git log -p -- 'dev-scripts/preflight*' dev-scripts/negative-test-preflight.py dev-scripts/dotnet_metadata.py
  .githooks .github .claude` shows every change to the gate itself.
- **A copy from anywhere else.** All of this is about this repository and its releases page. A file from somewhere
  else is not covered.

**What GitHub itself enforces** (since 2026-09-29):
- A published release can't be changed: its files and its tag stay what was checked.
- Release tags can't be moved or deleted.
- `main` can't be force-pushed, so its history can't be rewritten.
- A push carrying a known token format is refused.

## How it's built

- **Two process guides record every step, and a commit hook enforces them:** a commit that changes the mod, the
  apworld or the scripts must update one of them, or say `docs: no process change` in its message. They are [how the mod
  was made](../agent_docs/documentation.md) and [the Archipelago side](../agent_docs/apimplementation.md).
- **[MEASURED.md](../agent_docs/MEASURED.md)** holds the facts about the game, each with its evidence and date.
- **[room-logic.md](../agent_docs/room-logic.md)** is how the logic is written, checked and tested, room by room:
  - the rules it follows (the logic may ask more of the player than the game does, never less; no point of no return,
    unless the player turns on Points of No Return);
  - the questions asked of every room, every place the party can appear, and every story and quest chain that reaches
    across rooms;
  - how each answer is confirmed on screen and then tested.

  Each rule carries who decided it and when. Until every room has been through it, the entrance randomizer and a random
  start stay labelled experimental.
- **[licensing.md](../agent_docs/licensing.md)** lists every outside project the code uses or was read for, with its
  licence and the date it was checked; a file linking someone else's GitHub project with no row there fails the
  preflight. **[references.md](../agent_docs/references.md)** says what was taken from the randomizers compared with
  this one, and from other apworlds the maintainer knows from playing: approach and facts, never code.
- **Tests and generation:** the apworld's tests and the fuzzer, above, both in CI on every push, with seeds generated
  next to a second game; the release builds the apworld the way a player installs it.
- **[The log](../agent_docs/log.md)** records each session: what was tried, what happened, what was decided.

## Reporting a problem

Something sensitive: report it privately with the *Report a vulnerability* button on the repository's Security tab.
Anything else: an ordinary issue, or a ping on the Bug Fables thread in the Archipelago Discord. See
[SECURITY.md](../.github/SECURITY.md).
