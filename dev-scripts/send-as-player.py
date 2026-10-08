"""Acts as another slot in a local test room: logs in as it and checks the named locations, so the items there go out as
found by that player. Run from your Archipelago checkout, with it on the path:
    PYTHONPATH=. python <this repo>/dev-scripts/send-as-player.py <slot> <game> "<location>" [...]
e.g. Other APQuest "Bottom Left Chest". Location names are the game's own, from its world's location table."""
import asyncio
import json
import sys
import uuid

import websockets

import worlds  # noqa: F401  loads every world into the register
from worlds.AutoWorld import AutoWorldRegister


async def main(slot: str, game: str, names: list[str]) -> None:
    table = AutoWorldRegister.world_types[game].location_name_to_id
    ids = [table[name] for name in names]
    async with websockets.connect("ws://127.0.0.1:38281", max_size=None) as socket:
        print("<-", json.loads(await socket.recv())[0]["cmd"])
        await socket.send(json.dumps([{
            "cmd": "Connect", "game": game, "name": slot, "password": "", "uuid": str(uuid.uuid4()),
            "version": {"major": 0, "minor": 6, "build": 8, "class": "Version"}, "items_handling": 0, "tags": [],
            "slot_data": False,
        }]))
        while True:
            packets = json.loads(await socket.recv())
            commands = [p["cmd"] for p in packets]
            print("<-", commands)
            if "ConnectionRefused" in commands:
                print(packets)
                return
            if "Connected" in commands:
                break
        await socket.send(json.dumps([{"cmd": "LocationChecks", "locations": ids}]))
        print("-> LocationChecks", dict(zip(names, ids)))
        # The server answers nothing to LocationChecks; a moment lets it send the items before the socket closes.
        await asyncio.sleep(1.5)


asyncio.run(main(sys.argv[1], sys.argv[2], sys.argv[3:]))
