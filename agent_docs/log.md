# Session log

Newest last. What was tried, what happened, what the user said.

## 2026-09-24: the project starts

- **The user's decisions:** a separate project from their other work, built on its stricter rules. The first
  version shuffles **key items only**. **Remote items only, permanently**, after comparing it with local
  items: for a mod, remote is simpler, recovers a lost save and allows co-op on one slot, at the cost of
  needing the connection up. Local git on `main`, GitHub later.
- **Measured:** the game build facts in `MEASURED.md`. A web search found no existing Bug Fables Archipelago
  world.
- **Read:** the TEVI randomizer and Emerald's `remote_items` (`references.md`), and Archipelago's `docs/` at
  `0.6.7` (`client-requirements.md`). Licences in `licensing.md`.
- **Decompiled** `Assembly-CSharp.dll` with ilspycmd 10.1.1 into the gitignored `decompiled/`, which holds
  91 files. How items are granted is recorded in `MEASURED.md`.
- **Installed, with the user's yes:** BepInEx `v5.4.23.5` win_x64 (zip sha256 `82f98785…32c4`) and
  BepInEx.Debug ScriptEngine `r11.1` (zip sha256 `7f4a385f…b339b`), from their GitHub releases, into the
  Bug Fables install. Nothing was overwritten; the install had no BepInEx before. To undo, remove
  `BepInEx/`, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt` from the game
  folder. **The game keeps its save (`save0.dat`) in the same folder**, and nothing we do may touch it.
- **Carried over from the author's other project:** ScriptEngine **refuses a plugin in `BepInEx/scripts/`
  with no `.pdb` beside it**, and the failure is silent. Not yet confirmed here. The config keys are
  confirmed here (below).
- **The user launched and closed the game once.** BepInEx's log (`BepInEx/LogOutput.log`) shows it working;
  the lines are in `MEASURED.md`. The generated `com.bepis.bepinex.scriptengine.cfg` has `[AutoReload]`
  `EnableFileSystemWatcher = false`, `AutoReloadDelay = 3`, `DumpAssemblies = false`, and `[General]`
  `LoadOnStart = false`, `ReloadKey = F6`, `QuietMode = false`, `IncludeSubdirectories = false`. Those are
  the defaults, so hot reload still needs the watcher turned on and a `BepInEx/scripts/` folder, which
  doesn't exist yet.
- **The first in-game session of the plugin** (the agent launched the game, the user played a new game
  until quitting without saving):
  - **ScriptEngine loaded the plugin at startup** (`LoadOnStart`). The log shows
    `Loading bugfables.archipelago`, `Reloaded all plugins!` and our `loaded. GrantProbe=True`.
  - **ScriptEngine's FileSystemWatcher never fires in this game:** its handler's `File <name> changed` line
    never appeared after a redeploy. **F6 didn't reload either**, pressed in the focused window during play.
    Both are unexplained. `DevReload.cs` now polls our DLL and sets ScriptEngine's `shouldReload`; it's
    untested in the game so far.
  - **The log was a false lead:** it stopped at 00:56:53, which looked like a buffer. BepInEx flushes its
    disk log every 2 s (`DiskLogListener`, read with ilspycmd), and on close one more line appeared at frame
    25478 (about 58 fps since launch). So the plugin ran the whole time. **The probe's safety check at the
    top returned early silently through the entire session.** It now logs why it's waiting each time the
    reason changes. Which check it was is the next measurement.
  - **Focus:** the game has its own pause-when-unfocused option (`MainManager.pauseonfocus` drives
    `Application.runInBackground`, `MainManager.cs:16737`; set in `PauseMenu.cs:1626`). Use that rather
    than the mod overriding it.
  - **The user asked for spoiler-free chat:** the docs and the apworld hold everything, but chat refers to
    late content by id or chapter only. They haven't finished the game.
- **The hot-reload loop works (the third launch, 01:13):** the Unity log is copied into BepInEx's
  (`WriteUnityLog = true` in the dev config) and the console is on. That showed ScriptEngine's
  `new FileSystemWatcher` throwing `NotImplementedException` in this game's Mono, which aborts
  `ScriptEngine.Awake` and explains why F6 did nothing. The watcher is now off, and the plugin's
  `DevReload` polls the DLL instead. **Redeploying with the game running now reloads with no input:**
  `Unloading old plugin instances` → `Reloaded all plugins!` → our `unloaded` / `loaded`. The deploy
  script stamps the DLL's time, because `Copy-Item` keeps the source's and an unchanged build looked like
  no change.
- **A game freeze explained (the user, 2026-09-24):** clicking inside the BepInEx console, even while
  dragging the window, starts a Windows QuickEdit selection. The game then waits on its next console write
  until Enter or Esc. The user turned QuickEdit off in the console's Properties. The same cause explains
  an earlier freeze seen in another BepInEx game.
- **The main menu and the Archipelago panel, confirmed on screen by the user (2026-09-24).** How it got there,
  all from the user's screenshots:
  - A fourth line overlapped the credits: tightened the spacing and moved the cursor to match.
  - The panel was drawn behind the logo: hid the title screen, then rebuilt the panel from the Settings
    screen's own pieces, hung off the GUI camera like PauseMenu, with its dimmer.
  - Backing out crashed (`SetMenuText` indexes three labels): hand the game back three entries before each
    rebuild.
  - The Settings leaf broke (the title screen keeps updating under it): touch the cursor only while the main
    menu itself shows.
  - The gamepad couldn't leave a text field: `joykeys` are raw buttons, so A/B are `joykeys[0]/[1]`, not
    `[4]/[5]` (the user saw Start work).
  - A long centred "Archipelago (Enabled)" ran under the leaf: "Archipelago" centred like the others, the state
    as a small tag to its right, placed by measuring the screenshot (1 unit ≈ 69 px at 1280×720).
  **Reach first next time:** the game's own screen code for positions and draw orders, and a screenshot per
  change.
- **Connecting, 2026-09-24 (late):** an Archipelago panel on the main menu (address and port separate,
  archipelago.gg by default; typing, paste and copy; the gamepad works). The mod connects by itself while the
  Archipelago mod is enabled. Refusals wait; an unreachable or dropped server retries with backoff. Measured:
  killing the local server wasn't noticed by the idle socket, so a watchdog now pings `_read_race_mode` every
  5 s and calls the connection lost after 15 s of silence. **Tested:** the refusal path (InvalidSlot) and the
  unreachable path (server stopped, retries, then connected by itself when it came back). **Not yet tested:**
  the watchdog noticing a server killed *while connected*. Stop the server with the mod connected and look for
  `[ap] connection lost` within about 15 s.
- **The user ended the session here** to continue in a new chat.
- **Next:** the watchdog test above, then receiving items (the mod gives a received key item the game's own
  way, at a safe moment), then sending checks.
- **Earlier next:** read the TextProbe output from the user's play: which dialogue script carries each item
  command, and whether a key item's grant and its completion flag (15 for the first one) sit in the same
  script. That decides how locations are identified.
- **Lag and a memory leak after a server drop, 2026-09-24 (04:45):** the user said the game felt laggy and
  asked whether it was the probes. Measured with the game running: 3.7 cores busy, private memory 3.2 GB and
  growing about 2.5 MB/s, 95 threads. Five thread-pool threads started together at 04:28:01–02 (when the
  server was stopped while connected) were each spinning at about 75% of a core. The game's own threads were
  normal, and the probes run on the game thread, so **the probes were not the cause.** GrantProbe allocates a
  little garbage each frame, but it doesn't leak. Cause, read from MultiClient.Net 6.7.1 and the game's
  `System.dll` with ilspycmd: the library's `PollingLoop` loops `while (State == Open)`. Mono's
  `ManagedWebSocket.ReceiveAsyncPrivate` throws `ConnectionClosedPrematurely` without leaving `Open`, and our
  `DisconnectAsync` couldn't close a dead stream. The log also showed a second bug: the connect attempt after
  that never reported back (`LoginAsync` → `SendPacket(...).Wait()` has no timeout), so retries stopped.
  Fix: abort the `ClientWebSocket` by reflection on every loss or replace, and give each attempt a 12 s
  deadline. Built; **not yet confirmed in the game.** The old spinning threads belong to the previous load,
  and a hot reload can't reach them, so the game must restart once.
  The watchdog test from the entry above half-happened: the drop was caught by the socket error, not by the
  15 s silence.
- **The socket fix, measured in the game (04:55):** restarted, deployed, fresh seed, local server. The mod
  logged in on its own. Before the drop: 107% of a core, 69 threads, 737 MB. After the server was stopped:
  `socket closed: Open -> Aborted`, `connection lost: socket error ...`. Then 26% of a core (menu), 65 threads,
  memory flat around 720–790 MB with a save loaded. Before the fix it was 5 spinning threads and +2.5 MB/s. No retry
  followed, correctly: the user switched the Archipelago mod off right after and loaded a normal save.
  **Still open:** the user's on-screen check that the game stays smooth, and the retry after a drop on this build.
- **Retry after a drop, on the fixed build (04:58):** the user switched the mod back on with the server down.
  It retried with backoff (`could not reach the server`, no stuck attempt). The server was restarted at
  04:58:13 and the mod logged in by itself at 04:58:19. Across the retries: 64 threads, memory flat at about
  793 MB. The user saw it retrying on screen ("its trying to reconnect").
- **Compression, 2026-09-24 (05:00–05:13):** the user wanted it even though it's optional, "as an example of
  how it's done". Research first. The game's Mono `ClientWebSocket` has no permessage-deflate. MultiClient.Net's
  net40 build runs on websocket-sharp, which has it but never turns it on. Upstream issue #141 warned, and
  both codebases confirmed, that websocket-sharp refuses MultiServer's `server_max_window_bits=11`. Built:
  net40 references, a Harmony postfix to switch compression on, a prefix to strip that parameter, and sends
  and closes moved off the game thread (websocket-sharp pings before every send). **First run:** the
  handshake passed but the login timed out silently. With socket errors logged during the connect, it was
  `PlatformNotSupportedException` in the net40 Newtonsoft.Json's `DynamicMethod` (no Reflection.Emit in this
  Mono). Fixed by shipping the netstandard2.0 Newtonsoft.Json (same 11.0.0.0 identity). **Second run:**
  logged in compressed (read back from `WebSocket.Extensions`), no server warning. Switching the mod off
  closed cleanly. Server stopped: `connection lost` via "connection reset by peer", ~20% of a core, flat
  memory, 65–66 threads. Server back: reconnected compressed in 9 s. **Still open:** archipelago.gg over
  `wss://` (TLS 1.3 flag on old Mono).
  The user also reported that backing out of Start Game or Settings puts the leaf on Start Game, while backing
  out of Archipelago keeps it on Archipelago. The game's `SetMenuText` resets `option = 0`
  (`StartMenu.cs:319`); our panel doesn't call it. Asked which the user wants.
- **A hosted room, 2026-09-24 (05:17):** the user generated a seed with the installed Archipelago (slot
  `Player1`), uploaded it to archipelago.gg (port 52073), and set the panel on screen. The old slot `BugTester`
  was refused (`InvalidSlot`, the room log and ours agree); after the slot changed, the mod logged in by itself.
  With a bare `archipelago.gg` address it connected **over wss, compressed** (logged from the socket's own
  `Url` and `Extensions` after a hot reload). The room log showed no compression warning. The user saw
  "Connected as Player1." on screen. The TLS 1.3 worry didn't come true. The local server was stopped.
  The generator warned that our hand-zipped apworld lacks manifest fields (`compatible_version`), which will
  break with 0.7.0: package it with "Build APWorlds" (asked the user first, since it runs in the checkout).
