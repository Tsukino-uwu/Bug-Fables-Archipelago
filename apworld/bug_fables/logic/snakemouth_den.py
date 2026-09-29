"""Snakemouth Den: its regions and the ways between them, its spots and story events, and what the seed changes
there."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import CanUse
from ..data_types import Artifact, EntityRef, Exit, FlagEntity, Give, Location, Pickup, Region, Source, StoryEvent

REGIONS = (
    Region("Snakemouth Den", exits=(
        # Water droplets: Leif freezes them.
        Exit("Snakemouth Den Underground", CanUse("Freeze")),
    )),
    # Every Snakemouth room with water droplets, or reached only through one: Leif freezes the droplets.
    Region("Snakemouth Den Underground"),
)
LOCATIONS = (
    Location("Snakemouth Den: Underground Door Room", 5, "Snakemouth Den Underground",
             Source(flag=60, pickup=Pickup(map="SnakemouthUndergrondDoor", type=2, item=9))),
    Location("Snakemouth Den: Bridge Room, Pillar", 6, "Snakemouth Den",
             Source(flag=651, pickup=Pickup(map="SnakemouthBridgeRoom", type=0, item=13))),
    Location("Snakemouth Den: Lake, Pillar", 7, "Snakemouth Den",
             Source(flag=23, pickup=Pickup(map="SnakemouthLake", type=2, item=0))),
    Location("Snakemouth Den: Mushroom Pit, Floor", 8, "Snakemouth Den Underground",
             Source(flag=42, pickup=Pickup(map="SnakemouthMushroomPit", type=2, item=7))),
    Location("Snakemouth Den: Mushroom Pit, Droplets", 9, "Snakemouth Den Underground",
             Source(flag=724, pickup=Pickup(map="SnakemouthMushroomPit", type=0, item=144)),
             rule=CanUse("Freeze")),
    Location("Snakemouth Den: Lake, Ladybug Kid's Reward", 10, "Snakemouth Den",
             Source(event=31, flag=55, give=Give(map="SnakemouthLake", type=1, item=52)),
             rule=Has("Leif") & Has("Snakemouth Den Cleared"),
             category="quest"),
    Location("Snakemouth Den: Door Room, Trapdoor", 11, "Snakemouth Den",
             Source(event=5, flag=14, pickup=Pickup(map="SnakemouthDoorRoom", type=0, item=13, story=True)),
             rule=CanUse("Horn Slash")),
    # Crystal berry #2: from the room's upper-left entrance it needs nothing, from below it needs Leif.
    Location("Snakemouth Den: Underground Door Room, Upper Left", 20, "Snakemouth Den Underground",
             Source(berry=2, pickup=Pickup(map="SnakemouthUndergrondDoor", type=3, item=0)), category="crystal_berry"),
    # Crystal berry #1, from the bush by the sign on the lake room's far left.
    Location("Snakemouth Den: Lake, Bush by the Sign", 21, "Snakemouth Den",
             Source(berry=1, pickup=Pickup(map="SnakemouthLake", type=3, item=0)), category="crystal_berry"),
    # A respawning pickup: hidden only by a regional flag, so it comes back; the game's own again once checked.
    Location("Snakemouth Den: Underground Door Room, Pillar", 22, "Snakemouth Den Underground",
             Source(regional=24, pickup=Pickup(map="SnakemouthUndergrondDoor", type=0, item=1))),
    # A respawning pickup; its entity is named 'CrunchyLeaf - Duplicate' but holds a Mushroom.
    Location("Snakemouth Den: Underground Door Room, Floor 2", 23, "Snakemouth Den Underground",
             Source(regional=29, pickup=Pickup(map="SnakemouthUndergrondDoor", type=0, item=13))),
    # A respawning pickup, out of sight behind a pillar.
    Location("Snakemouth Den: Underground Bridge Room, Behind Pillar", 24, "Snakemouth Den Underground",
             Source(regional=28, pickup=Pickup(map="SnakemouthUndergroundRightB", type=0, item=0))),
    # Recorded at the end of the spider fights after the trapdoor, before Leif joins.
    Location("Snakemouth Den: Fall Room, Spider Fight", 29, "Snakemouth Den",
             Source(discovery=1), category="discovery"),
    # Recorded by examining a hidden spot in the bridge room, behind grass.
    Location("Snakemouth Den: Bridge Room, Hidden Spot", 30, "Snakemouth Den",
             Source(discovery=2),
             rule=CanUse("Horn Slash"),
             category="discovery"),
    # Recorded by cutting one patch of grass with Kabbu's horn.
    Location("Snakemouth Den: Underground Door Room, Grass", 31, "Snakemouth Den Underground",
             Source(discovery=3), category="discovery"),
    # Where the mod has Leif join (the spider scene over, flag 27), whoever starts.
    Location("Snakemouth Den: Fall Room, After the Spider", 67, "Snakemouth Den",
             Source(event=6, flag=27), category="party_member"),
)
STORY_EVENTS = (
    # At the lake, reachable without passing any water droplet, so Leif comes before everything that needs him.
    StoryEvent("Leif Joins", "Leif", "Snakemouth Den",
               Source(event=14, flag=16), category="story_party"),
    StoryEvent("First Boss Beaten", "Snakemouth Den Cleared", "Snakemouth Den Underground",
               Source(event=26, flag=41)),
)
ARTIFACTS = (
    Artifact(1, "Artifact 1", "Snakemouth Den Underground",
             Source(event=26, flag=41)),
)
KEPT_OPEN = (
    # Turns the party back until the first boss; with the way up kept present it has nothing left to guard.
    EntityRef("SnakemouthFallRoom", "blocker"),
)
KEPT_PRESENT = (
    # The big door to Upper Snakemouth; the Peculiar Gem slot right behind it still locks the way.
    EntityRef("SnakemouthDoorRoom", "DoorLoadZone"),
    # The bounce mushroom back up to the pitfall room, so the trapdoor is never a dead end.
    EntityRef("SnakemouthFallRoom", "JumpShroom"),
    # The door at the top of the bounce mushroom.
    EntityRef("SnakemouthFallRoom", "LoadingZoneDoorRoom"),
)
PRESENT_FROM = (
    # The way back down to the fall room, made from the trapdoor instead of the first boss; not from the start, since
    # that would skip the scene where Leif's joining begins.
    FlagEntity("SnakemouthDoorRoom", "LoadZoneFallRoom", 14),
)
