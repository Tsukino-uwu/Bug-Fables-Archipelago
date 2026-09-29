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

## Apworld: reflection by name

The apworld runs on whichever machine generates a seed. It imports only the names listed in the patterns file
(nothing that reads or writes files, runs programs or opens connections); it reads its own data files, which ship
inside it, through `pkgutil.get_data`. A few places look an attribute up by a name held in a variable rather than
written in the code. Each is listed here with where the name comes from, since a name in a variable is where a
reviewer can't see at a glance what is reached.

| Where (file and function) | What it looks up, and where the name comes from |
|---|---|
| `apworld/bug_fables/abilities.py item_count` | The yaml option an ability's item count depends on; the option names are written in `ABILITIES`, in the same file |
| `apworld/bug_fables/locations.py category_on` | The yaml option that switches a location category on or off; the names are written in `CATEGORY_OPTIONS`, in `options.py` |
| `apworld/bug_fables/data_types.py present` | A record's own fields, as `dataclasses.fields` lists them |
| `apworld/bug_fables/slot_data.py _by_location` | One named field of each location's source record; each name is written at the calls in the same file |
