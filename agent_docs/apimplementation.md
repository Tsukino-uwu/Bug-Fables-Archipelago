# How a game talks to Archipelago

A plain-language explainer of how a game (or a mod for one) connects to an Archipelago server, sends what
the player finds, and receives items. It doesn't change much as the project grows; for the story of what
we did and when, see [documentation.md](documentation.md).

Everything here follows Archipelago's own [network protocol doc](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/network%20protocol.md)
(read at version 0.6.7). Where this file and that doc disagree, that doc is right.

**Contents**

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
