# The Archipelago side: how it works, and how it was built

This is the Archipelago half of the Bug Fables randomizer: the apworld, seeds, the server, and how the mod
connects, sends what the player finds and receives items. It has two parts: **how we built it**, step by
step, and **how it works**, a plain explainer of how any game talks to Archipelago. The game side (the mod
itself, probing the game) has its own guide: [documentation.md](documentation.md).

The explainer follows Archipelago's own [network protocol doc](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/network%20protocol.md)
(read at version 0.6.7). Where this file and that doc disagree, that doc is right.

## Where it stands

**Done so far:** a tiny apworld that generates seeds and passes its tests, with the goal "collect N
artifacts"; a local server; and the mod connecting to it on its own, retrying when the server is unreachable.

**Next:**

1. **Receive an item:** the server sends a key item and the mod gives it in the game.
2. **Send a check:** finishing a location tells the server.
3. **Survive a reload:** the received-item count lives in the save.
4. **Goal:** the mod counts the game's artifact flags and sends "goal reached" at the required number.
5. **Compressed connection** (see known issues).

**Known issues:**

- The server warns that our connection isn't compressed. Everything works today; it's a thing to fix
  before the server stops accepting uncompressed clients.

## Contents

**How we built it**

1. [Build step 1: a first, tiny apworld](#build-step-1-a-first-tiny-apworld)
2. [Build step 2: connect the mod to a real server](#build-step-2-connect-the-mod-to-a-real-server)
3. [Build step 3: the goal, counted in artifacts](#build-step-3-the-goal-counted-in-artifacts)
4. [Build step 4: connecting on its own, and staying connected](#build-step-4-connecting-on-its-own-and-staying-connected)

**How it works**

1. [The big picture](#1-the-big-picture)
2. [Opening the connection](#2-opening-the-connection)
3. [Logging in](#3-logging-in)
4. [Sending what the player found](#4-sending-what-the-player-found)
5. [Receiving items](#5-receiving-items)
6. [Finishing the game](#6-finishing-the-game)
7. [Settings from the seed: slot_data](#7-settings-from-the-seed-slot_data)
8. [Use a library](#8-use-a-library)
9. [How this mod does it](#9-how-this-mod-does-it)
10. [Things that go wrong quietly](#10-things-that-go-wrong-quietly)

---

# How we built it

## Build step 1: a first, tiny apworld

The apworld started deliberately tiny: two early locations, one key item (the Explorer Permit), the gate
it opens, and "open that gate" as a temporary goal. Items and locations live in simple JSON files, so
growing the world is mostly adding data. It follows the layout of `worlds/apquest`, Archipelago's own
teaching example, and writes its rules with Archipelago's Rule Builder.

We wrote **tests**, including one that proves the gate really needs the permit. To make sure that test
could fail, we removed the rule on purpose, watched the test fail, and put the rule back. Archipelago's
own test suite passes for it too.

To try it, the world folder is linked into a local copy of Archipelago (run from source), and seeds are
generated with `Generate.py`.

## Build step 2: connect the mod to a real server

We generated a seed with the tiny world, started a local Archipelago server (`MultiServer.py`), and had the
mod log in from inside the running game, using the official .NET client library
(Archipelago.MultiClient.Net).

The first try timed out. The server's own log showed what happened: the library first tried a secure
connection, which the plain local server rejected. Giving the address as `ws://…` fixed it, and the mod
logged in. This also proved the game's runtime can run the client library, which had been an open risk.

**Lesson:** when two programs talk, read the logs on *both* ends. The mod's default address became
`ws://127.0.0.1:38281` for the same reason, so a local setup works out of the box.

## Build step 3: the goal, counted in artifacts

The game shows up to 7 artifacts on the pause menu and on each save file. Reading how it draws them showed
they aren't items at all: the game counts how many of 7 story milestones you've reached. That makes a good
goal. It's cheap for the mod to check, it's real progress, and **"any N of 7"** doesn't care about order, so
it keeps working with options like a random start.

The apworld has an option, *Artifacts Required* (1 to 7). Each artifact is an **event** in the region where
the game grants it, and the goal is "have N of them". An event holds no real item; it exists so the
generator can prove the goal is reachable. The world only includes the first artifact so far, so a request
for more is lowered, with a warning, instead of producing a seed that can't be won. That rule has a test,
and so does the permit gate: remove the permit rule and two tests fail.

One rule came out of this for every later option: **every seed can be completed from wherever it starts.**
Whatever an area or the goal needs is written into the logic, and the mod never hands things out to patch
a gap.

## Build step 4: connecting on its own, and staying connected

Players enter the room's address, port and slot in an Archipelago panel on the main menu. While the
Archipelago mod is enabled and those are filled in, **the mod connects by itself**, with no Connect button.
Failures are sorted into two kinds, using the refusal codes the client library reports:

- **Refused** (a wrong slot or password): the reason is shown, and nothing is retried until a detail changes.
- **Unreachable, or the connection dropped**: it retries on its own, waiting 2, 4, 8, 15, then 30 seconds.

A dropped server turned out to be invisible: an idle connection doesn't notice the other side is gone. So
while connected, the mod asks the server something tiny every 5 seconds (the documented read-only value
`_read_race_mode`), and treats 15 seconds of silence, a failed send or a socket error as a lost connection.

Tested against a local server: a wrong slot was refused and left alone; with the server stopped the mod kept
retrying, and when the server came back it connected by itself.

**A dropped connection must be closed by force.** Stopping the server while connected made the game lag and
eat memory (2026-09-24: five threads spinning, memory growing about 2.5 MB a second). The client library
keeps reading "while the socket is open", and in this game's version of .NET a dead socket still reports
itself open, so the read failed and retried forever. Asking the library to disconnect politely doesn't help,
because the goodbye can't reach a dead server. The mod now aborts the socket itself whenever a connection
is lost or replaced, which ends the loop. It also gives every connect attempt 12 seconds: the library's
login step can wait forever, and a stuck attempt had stopped all further retries. **Not yet confirmed in
the game:** stop the server while connected, then check that the log shows `socket closed: Open -> Aborted`
and the game stays smooth.

---

# How it works

## 1. The big picture

```
 generator + apworld  ──(makes)──>  seed file  ──(loaded by)──>  server
                                                                   ▲
                                                        websocket  │  JSON messages
                                                                   ▼
                                                     your game + its client (the mod)
```

- The **apworld** runs once, when a seed is generated, and decides where every item goes. It never runs
  while anyone plays.
- The **server** hosts the seed. It knows which item sits at every location.
- The **client** lives in or next to the game. It only ever talks to the server.

## 2. Opening the connection

The client opens a **websocket** to the server's address, for example `archipelago.gg:38281` or a local
`127.0.0.1:38281`. All messages are JSON, called "packets", each with a `cmd` naming its kind.

- `wss://` is the encrypted kind, `ws://` the plain kind. A local server you started yourself is plain, so
  write `ws://127.0.0.1:38281`. Without the prefix, a library may try the encrypted one first and time out.
- Rooms on the website can change port, so a client must let the player edit the port.

## 3. Logging in

The server speaks first. The order, from the protocol doc:

1. The client opens the websocket.
2. The server sends **RoomInfo**: the room's details.
3. Optionally, the client asks for the **DataPackage**, the tables that turn item and location numbers into
   names.
4. The client sends **Connect**: the game's name, the player's slot name, the password if any, and how it
   wants items delivered.
5. The server answers **Connected** (you're in) or **ConnectionRefused** (with the reason).
6. The server sends **ReceivedItems** with anything the player is owed.

**The game name in Connect must match the apworld's `game` exactly** (here, `Bug Fables`).

**How items are delivered** is chosen in Connect with three switches (`items_handling`):

| Switch | Meaning |
|---|---|
| items from other worlds | always wanted by a normal client |
| items from your own world | on: even items you find yourself come from the server |
| your starting inventory | on: the server sends it on connect |

This mod turns all three on, which makes it a **"remote items"** client: picking something up never gives
it directly, and everything arrives from the server.

## 4. Sending what the player found

When the player completes a location, the client sends **LocationChecks** with that location's number.

- **Duplicates are harmless.** The doc says the server ignores repeats. So after a reconnect a client can
  simply send every location it knows is done, which is also how checks made while offline get delivered.
- The server then sends the item at that spot to whoever it belongs to, you included in a remote-items
  game.

## 5. Receiving items

Items arrive in **ReceivedItems** packets. Each carries an `index`: the item's position in that player's
list of everything ever received.

- **After every connect, the server sends the whole list again**, including items from past sessions. So
  the client must remember **how many items it has already given the player**, and skip those.
- **Keep that number in the player's save**, per the doc. Then loading an older save gives back exactly
  what that save hadn't had yet, and a brand-new save gets everything.
- An `index` of **0** means "this is the full list"; anything else continues from where the last packet
  stopped. If the numbers don't line up, the client sends **Sync** and gets the full list again.
- A client must cope with **any item arriving any number of times**. Admins can send items, and starting
  inventory repeats.

## 6. Finishing the game

When the player reaches the goal, the client sends **StatusUpdate** with status **30** (goal reached).
Nothing else marks a slot as finished.

## 7. Settings from the seed: slot_data

The apworld can hand the client a small dictionary, **slot_data**, which arrives inside Connected. It's the
only way a setting chosen at generation (an option, a version number) reaches the game. This mod puts the
world's version in it, so a mismatched mod and apworld can be caught.

## 8. Use a library

Writing all of the above by hand is possible, but libraries exist for most languages; the protocol doc
lists them. For C# (Unity, BepInEx) it's **Archipelago.MultiClient.Net**. It handles:

- the handshake and every packet;
- turning numbers into names;
- a single call to log in.

What it can't do for you:

- deciding **when** it's safe to hand the player an item in your game;
- **saving** the received-item count in your game's save;
- knowing **which spot** in your game is which location.

Those are the parts that make each game's client different.

## 9. How this mod does it

- The login call can take several seconds, so it runs **off the game's own thread**, and the result is
  passed back to the game thread through a queue. The game never freezes on connect.
- Connection settings (address, slot, password) live in the mod's BepInEx config file.
- The client library and its JSON library sit in `BepInEx/plugins`, loaded once, and the mod itself
  reloads on its own during development.

## 10. Things that go wrong quietly

Most connection mistakes don't crash; they just silently do nothing, or look like they worked. The full
list we keep is in [client-requirements.md](client-requirements.md), "Known failure modes". The ones that
matter first:

- The wrong game name, or a missing `ws://` for a local server, and the login fails or times out.
- Treating "the socket is open" as "we're in a seed": after a disconnect, rules must stay in force.
- Not saving the received-item count: every reload hands out every item again.
- An uncompressed connection works today, but the server warns that one day it may not.
- A lost connection that is only "disconnected" politely can keep a reading loop spinning in the background.
  The game just gets slower and uses more memory, with no error. Abort the socket.
