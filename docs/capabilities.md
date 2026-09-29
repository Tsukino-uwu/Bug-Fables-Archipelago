# What this project's code may do

Everything in this repository that reaches beyond its own files, listed with the reason. The list is checked, not
just written: `dev-scripts/preflight.py` refuses a commit that does something listed nowhere here, and a row that
nothing uses any more. So this page is exactly what the code does, no more and no less; `git log -p` on it is the
history of every capability the project gained or dropped.

Adding a row is the maintainer's decision, made in a commit of its own. How to check all of this yourself:
[reviewing.md](reviewing.md).

## Hosts

Every place a file in this repository points to, in code, docs or build files. Loopback (`127.0.0.1`,
`localhost`) is left out: it is the player's own machine.

| Host | Why |
|---|---|
| `github.com` | This repository and its releases, the projects cited in the docs, and the workflow actions |
| `archipelago.gg` | Archipelago's site, and the mod's default server address (`Core/Plugin.cs`), which the player can change |
| `api.nuget.org` | The NuGet feed the mod's libraries are restored from at build time (`nuget.config`) |
| `nuget.bepinex.dev` | BepInEx's own package feed, for its packages only (`nuget.config`) |
| `steam://rungameid` | Starting the game through Steam, in a dev log entry |
