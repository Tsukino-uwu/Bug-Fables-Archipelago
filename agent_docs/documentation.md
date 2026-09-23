# How this Archipelago mod is being made

This is the story of building an [Archipelago](https://archipelago.gg) randomizer for a game that never
had one, step by step, in the order it happened. It's meant for anyone curious about the process, or
thinking of doing the same for another game. It describes **how** we worked, not how Bug Fables works
inside. The game facts live in `MEASURED.md`.

An Archipelago randomizer is two programs:

- **The apworld**: Python that runs inside Archipelago's generator. It says which items and locations
  exist and what each area needs, and the generator uses that to decide where every item goes.
- **The client**: code inside the game (here, a mod). It tells the server when you've found something,
  and gives you the items the server sends.

The two never talk directly. The server sits in between.

## Where it stands

**Done so far:** the mod loads, reloads itself while the game runs, watches the game, and logs in to an
Archipelago server. A tiny apworld generates seeds.

**Next:**

1. **Receive an item:** the server sends a key item and it appears in the game's inventory.
2. **Send a check:** finding a location tells the server instead of giving the item.
3. **Survive a reload:** saving and loading never hands out items twice.
4. **Grow the world** chapter by chapter, as the game is played.

**Known issues:**

- The server warns that our connection isn't compressed. Everything works today; it's a thing to fix
  before the server stops accepting uncompressed clients.

## The steps

1. [Check whether the game can be modded at all](#1-check-whether-the-game-can-be-modded-at-all)
2. [Pick the design before the code](#2-pick-the-design-before-the-code)
3. [Read the game's code](#3-read-the-games-code)
4. [Get a mod loader running](#4-get-a-mod-loader-running)
5. [Make changes load without restarting the game](#5-make-changes-load-without-restarting-the-game)
6. [Watch the game while you play ("probing")](#6-watch-the-game-while-you-play-probing)
7. [List everything, without playing everything](#7-list-everything-without-playing-everything)
8. [Write a first, tiny apworld](#8-write-a-first-tiny-apworld)
9. [Connect the mod to a real server](#9-connect-the-mod-to-a-real-server)

## Keeping this guide honest

A step-by-step guide is only useful if no step is missing, so the project enforces it: any commit that
changes the mod, the apworld or the dev scripts is refused unless it also updates this file (or says,
explicitly, that nothing about the process changed). That check is a small git hook, `.githooks/commit-msg`.
Each new step also gets a line in the index above, and "Where it stands" is updated with it.

---

## 1. Check whether the game can be modded at all

Before writing anything, we looked at what the game is made of. Bug Fables is a **Unity game built with
Mono**, which means its code ships as a normal .NET file (`Assembly-CSharp.dll`) that can be turned back
into readable code. That's the easiest case there is. We also checked that nobody had already made a
Bug Fables randomizer.

*How to tell for your own game:* an `Assembly-CSharp.dll` in the game's `_Data/Managed` folder means
Unity with Mono. A `GameAssembly.dll` means Unity with IL2CPP, which is harder.

## 2. Pick the design before the code

A few decisions made first, because they shape everything after:

- **Start small: key items only.** More pools, like medals and crystal berries, come later.
- **"Remote items" only.** Picking something up never gives it to you directly. It tells the server,
  and every item, even your own, comes back from the server. That's simpler to build, a lost save can get
  everything back, and two people can share one slot. The cost: with the server down, nothing arrives.
- **Read what others already solved.** We read the TEVI randomizer (another Unity mod), Pokémon Emerald's
  apworld, Archipelago's own docs, and notes from an earlier Archipelago project, all for ideas only,
  with each one's licence checked first.

## 3. Read the game's code

We used **ILSpy** to turn the game's `Assembly-CSharp.dll` back into C# source, kept on our own machine
and never shared. Reading it answered the first big question: *how does this game hand out items?*
The answer was a small set of places every item goes through, which is exactly where a randomizer
needs to hook in.

## 4. Get a mod loader running

Unity games don't load mods by themselves, so we installed **BepInEx 5**, the usual mod loader for
Unity games. One launch of the game confirmed it worked, and showed its log file.

## 5. Make changes load without restarting the game

Restarting the game for every change is slow, so before any real feature we set up **hot reload**:
change the mod, and it swaps itself into the running game in a second or two.

This took some detective work. The standard tool for it (ScriptEngine) relies on a feature this game's
runtime doesn't have, and it failed silently. Turning on more logging showed the real error. The fix was
small: the mod checks its own file once a second and asks for a reload when it changes.

**Lesson:** when something silently does nothing, make the invisible errors visible before guessing.

## 6. Watch the game while you play ("probing")

Reading code tells you what *can* happen. Watching the game tells you what *does*. We added small,
read-only **probes** to the mod. They never change the game, and they write to a log:

- one logs every **key item** that arrives, and every **flag** the game sets (flags are how the game
  remembers what you've done);
- one logs every **item script** the game runs when it hands something out.

Then the user simply played, and told us when they found something. Each find showed which flag the game
sets for it. That flag is how the randomizer will recognise "this spot is done". It also sorted pickups
into kinds: ones that come back after you leave an area can't be randomizer locations, and one-time ones
can.

**Lesson:** the live log caught a mistake in our reading of the code (every item number was off by one),
which is why things are measured, not just read.

## 7. List everything, without playing everything

Playing the whole game to find every item would take days, so we also asked the running game directly:
a one-off dump loaded every map's dialogue data and kept only the item and flag commands. Together with
the code, that gave a full list of where key items come from, raw material for the apworld.

## 8. Write a first, tiny apworld

The apworld started deliberately tiny: two early locations, one key item (the Explorer Permit), the gate
it opens, and "open that gate" as a temporary goal. Items and locations live in simple JSON files, so
growing the world is mostly adding data.

We wrote **tests**, including one that proves the gate really needs the permit. To make sure that test
could fail, we removed the rule on purpose, watched the test fail, and put the rule back. Archipelago's
own test suite passes for it too.

## 9. Connect the mod to a real server

We generated a seed with the tiny world, started a local Archipelago server, and had the mod log in from
inside the running game, using the official .NET client library (Archipelago.MultiClient.Net).

The first try timed out. The server's own log showed what happened: the library first tried a secure
connection, which the plain local server rejected. Giving the address as `ws://…` fixed it, and the mod
logged in. This also proved the game's runtime can run the client library, which had been an open risk.

**Lesson:** when two programs talk, read the logs on *both* ends.
