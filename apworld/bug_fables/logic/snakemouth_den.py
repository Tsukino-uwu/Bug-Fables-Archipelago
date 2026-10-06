"""Snakemouth Den: its spots and story events, what reaching them needs until the rooms are mapped, and what the seed
changes there."""
from __future__ import annotations

from rule_builder.rules import False_, Has

from ..custom_rules import ANY_ATTACK, CanUse, Member, one_way
from ..data_types import Area, Artifact, EntityRef, FlagEntity, Give, Location, Pickup, Source, StoryEvent
from .outskirts import PAST_GATE

# Grass in the second corridor and outside the cave, then the door room's horn puzzle down the trapdoor.
DEN = PAST_GATE & CanUse("Horn Slash")
# Every Snakemouth room with water droplets, or reached only through one: Leif freezes the droplets.
UNDERGROUND = DEN & CanUse("Freeze")
# The door room's puzzle: grass cut, Jump onto the stone hung on a vine, hit it down, and the horn knocks both stones
# onto the plates; the trapdoor Mushroom appears.
TRAPDOOR = CanUse("Jump") & CanUse("Horn Slash")
# Its high door to SnakemouthTop: grass cut, then Bee Fly across; dropped down from freely.
TOP_LEDGE = CanUse("Horn Slash") & CanUse("Bee Fly")
# The lake's way up to its top: the switch on a pillar (the Beemerang), then platforms (Jump).
LAKE_UP = CanUse("Jump") & CanUse("Beemerang Toss")
# The underground door room's droplets: frozen (Freeze) and jumped on (Jump), up to its middle and its left.
DROPLETS = CanUse("Jump") & CanUse("Freeze")
LOCATIONS = (
    Location("Snakemouth Den: Underground Door Room, Behind the Wall", 5, "SnakemouthUndergrondDoor",
             Source(flag=60, pickup=Pickup(map="SnakemouthUndergrondDoor", type=2, item=9)), no_jump=True,
             area="Middle"),
    # On a pillar by the bounce pad (no Jump needed), grabbed with the Beemerang.
    Location("Snakemouth Den: Bridge Room, Pillar", 6, "SnakemouthBridgeRoom",
             Source(flag=651, pickup=Pickup(map="SnakemouthBridgeRoom", type=0, item=13)),
             rule=CanUse("Beemerang Toss"), no_jump=True),
    # Alone on a pillar, too far for anything but the Beemerang.
    Location("Snakemouth Den: Lake, Pillar", 7, "SnakemouthLake",
             Source(flag=23, pickup=Pickup(map="SnakemouthLake", type=2, item=0)),
             rule=CanUse("Beemerang Toss"), no_jump=True),
    Location("Snakemouth Den: Mushroom Pit, Mushroom by the Ledge", 8, "SnakemouthMushroomPit",
             Source(flag=42, pickup=Pickup(map="SnakemouthMushroomPit", type=2, item=7)), no_jump=True),
    Location("Snakemouth Den: Mushroom Pit, Mushroom by the Droplets", 9, "SnakemouthMushroomPit",
             Source(flag=724, pickup=Pickup(map="SnakemouthMushroomPit", type=0, item=144)), rule=DROPLETS),
    Location("Snakemouth Den: Lake, Ladybug Kid's Reward", 10, "SnakemouthLake",
             Source(event=31, flag=55, give=Give(map="SnakemouthLake", type=1, item=52)),
             rule=Has("Leif") & Has("Snakemouth Den Cleared"),
             category="quest", reach=DEN, pending=True),
    Location("Snakemouth Den: Door Room, Trapdoor", 11, "SnakemouthDoorRoom",
             Source(event=5, flag=14, pickup=Pickup(map="SnakemouthDoorRoom", type=0, item=13, story=True)),
             rule=TRAPDOOR),
    # Crystal berry #2: from the room's upper-left entrance it needs nothing, from below it needs Leif.
    Location("Snakemouth Den: Underground Door Room, Left under the Glowing Cap", 20, "SnakemouthUndergrondDoor",
             Source(berry=2, pickup=Pickup(map="SnakemouthUndergrondDoor", type=3, item=0)), category="crystal_berry",
             no_jump=True, area="Left"),
    # Crystal berry #1, from the bush by the tablet on the lake room's far left.
    Location("Snakemouth Den: Lake, Bush by the Tablet", 21, "SnakemouthLake",
             Source(berry=1, pickup=Pickup(map="SnakemouthLake", type=3, item=0)), category="crystal_berry",
             rule=CanUse("Jump") & CanUse("Horn Slash")),
    # A respawning pickup: hidden only by a regional flag, so it comes back; the game's own again once checked.
    Location("Snakemouth Den: Underground Door Room, Pillar by the Droplets", 22, "SnakemouthUndergrondDoor",
             Source(regional=24, pickup=Pickup(map="SnakemouthUndergrondDoor", type=0, item=1)), rule=DROPLETS),
    # A respawning pickup; its entity is named 'CrunchyLeaf - Duplicate' but holds a Mushroom.
    Location("Snakemouth Den: Underground Door Room, Right under the Glowing Cap", 23, "SnakemouthUndergrondDoor",
             Source(regional=29, pickup=Pickup(map="SnakemouthUndergrondDoor", type=0, item=13)), no_jump=True,
             area="Left"),
    # A respawning pickup, out of sight behind a pillar.
    Location("Snakemouth Den: Underground Bridge Room, Behind Pillar", 24, "SnakemouthUndergroundRightB",
             Source(regional=28, pickup=Pickup(map="SnakemouthUndergroundRightB", type=0, item=0)), reach=UNDERGROUND),
    # Recorded at the end of the spider fights after the trapdoor, before Leif joins.
    Location("Snakemouth Den: Fall Room, Spider Fight", 29, "SnakemouthFallRoom",
             Source(discovery=1), category="discovery", no_jump=True),
    # Recorded by examining the warning sign hidden behind the bridge room's bushes.
    Location("Snakemouth Den: Bridge Room, Sign behind the Bushes", 30, "SnakemouthBridgeRoom",
             Source(discovery=2),
             rule=CanUse("Horn Slash"),
             category="discovery", no_jump=True, area="Left"),
    # Crystal berry #32, on the vine above the pillars: hovered to with Bee Fly, knocked down with the Beemerang.
    Location("Snakemouth Den: Bridge Room, Vine above the Pillars", 94, "SnakemouthBridgeRoom",
             Source(berry=32, pickup=Pickup(map="SnakemouthBridgeRoom", type=3, item=0)), category="crystal_berry",
             rule=CanUse("Bee Fly") & CanUse("Beemerang Toss"), no_jump=True),
    # Recorded by examining the old statue the first time.
    Location("Snakemouth Den: Underground Door Room, Statue", 31, "SnakemouthUndergrondDoor",
             Source(discovery=3), category="discovery", no_jump=True),
    # Where the mod has Leif join (the spider scene over, flag 27), whoever starts.
    Location("Snakemouth Den: Fall Room, After the Spider", 67, "SnakemouthFallRoom",
             Source(event=6, flag=27), category="party_member", no_jump=True),
)
STORY_EVENTS = (
    # The bridge's rope, hit from either bank (up a ledge, Jump): only the Beemerang reaches it from the right, any
    # attack from the left. It stays down (flag 7).
    StoryEvent("Snakemouth Den: Bridge Room, Bridge Lowered from the Right", "Snakemouth Bridge Lowered",
               "SnakemouthBridgeRoom", Source(event=1, flag=7), rule=CanUse("Jump") & CanUse("Beemerang Toss")),
    StoryEvent("Snakemouth Den: Bridge Room, Bridge Lowered from the Left", "Snakemouth Bridge Lowered",
               "SnakemouthBridgeRoom", Source(event=1, flag=7), rule=CanUse("Jump") & ANY_ATTACK, area="Left"),
    # The two big switches up the underground's side rooms (flags 33, 34), cautious until those rooms are mapped; with
    # both, the underground door room's big door opens (Event24, flag 35).
    StoryEvent("Snakemouth Den: Upper Left Room, Switch Hit", "Snakemouth Left Switch", "SnakemouthUndergroundLeftB",
               Source(flag=33), reach=UNDERGROUND),
    StoryEvent("Snakemouth Den: Upper Right Room, Switch Hit", "Snakemouth Right Switch",
               "SnakemouthUndergroundRightB", Source(flag=34), reach=UNDERGROUND),
    StoryEvent("Snakemouth Den: Underground Door Room, Big Door Opened", "Snakemouth Big Door Opened",
               "SnakemouthUndergrondDoor", Source(event=24, flag=35),
               rule=Has("Snakemouth Left Switch") & Has("Snakemouth Right Switch"), no_jump=True, area="Middle"),
    # Taking the trapdoor Mushroom (flag 14) opens the hole down to the fall room.
    StoryEvent("Snakemouth Den: Door Room, Trapdoor Opened", "Snakemouth Trapdoor Opened", "SnakemouthDoorRoom",
               Source(event=5, flag=14), rule=TRAPDOOR),
    # At the lake, reachable without passing any water droplet, so Leif comes before everything that needs him.
    StoryEvent("Leif Joins", "Leif", "SnakemouthLake",
               Source(event=14, flag=16), category="story_party", area="Top"),
    # The Spider fight, walking into the treasure room: Vi's attack hits it in the air. Bosses aren't shuffled yet.
    StoryEvent("First Boss Beaten", "Snakemouth Den Cleared", "SnakemouthTreasureRoom",
               Source(event=26, flag=41), rule=Member("Vi")),
)
ARTIFACTS = (
    Artifact(1, "Artifact 1", "SnakemouthTreasureRoom",
             Source(event=26, flag=41), reach=Member("Vi")),
)
MAP_AREAS = (
    # The bridge room's left bank, with the door room's door: across the river on the lowered bridge (Jump), both ways.
    Area("SnakemouthBridgeRoom", "Left", ("LoadingZoneDoorRoom",),
         CanUse("Jump") & Has("Snakemouth Bridge Lowered")),
    # The door room's hole down: shut until the trapdoor; walked in from freely.
    Area("SnakemouthDoorRoom", "Trapdoor", ("LoadZoneFallRoom",), Has("Snakemouth Trapdoor Opened"),
         out=one_way(None, Has("Snakemouth Trapdoor Opened"))),
    Area("SnakemouthDoorRoom", "High Door", ("New Entity16",), TOP_LEDGE, out=one_way(None, TOP_LEDGE)),
    # The fall room's door up to the door room, by the bounce mushroom: up a ledge (Jump), dropped down from.
    Area("SnakemouthFallRoom", "Mushroom Ledge", ("LoadingZoneDoorRoom",), CanUse("Jump"),
         out=one_way(None, CanUse("Jump"))),
    # The lake's top, with Leif's scene and the door to the underground door room: up platforms (Jump) past a pillar the
    # switch below lowers (the Beemerang); jumped down from (Jump), back up only that way.
    Area("SnakemouthLake", "Top", ("WarpMap5",), LAKE_UP, out=one_way(CanUse("Jump"), LAKE_UP)),
    # The underground door room: from the lake door's bottom, droplets up to its middle (the right door, the broken
    # house, the big door); dropped down from.
    Area("SnakemouthUndergrondDoor", "Middle", ("LoadingRightDown",), DROPLETS, out=one_way(None, DROPLETS)),
    # Its left, under the glowing cap: droplets up from the middle, dropped down to it.
    Area("SnakemouthUndergrondDoor", "Left", ("WarpLeft",), DROPLETS, out=one_way(None, DROPLETS),
         to="SnakemouthUndergrondDoor (Middle)"),
    # Through the big door, the mushroom pit's door; arriving through it while the door is shut, the game pushes the
    # party past it.
    Area("SnakemouthUndergrondDoor", "Pit Door", ("WarpPit",), Has("Snakemouth Big Door Opened"),
         out=one_way(None, Has("Snakemouth Big Door Opened")), to="SnakemouthUndergrondDoor (Middle)"),
    # The two top doors: no way up from inside the room, only a drop, the left one to the left, the right one to the
    # middle.
    Area("SnakemouthUndergrondDoor", "Top Left", ("WarpLeftUp",), False_(), out=one_way(None, False_()),
         to="SnakemouthUndergrondDoor (Left)"),
    Area("SnakemouthUndergrondDoor", "Top Right", ("WarpRightUp",), False_(), out=one_way(None, False_()),
         to="SnakemouthUndergrondDoor (Middle)"),
    # The mushroom pit's bottom door: up the bounce mushrooms (Jump), dropped down to from the top freely.
    Area("SnakemouthMushroomPit", "Bottom", ("WarpDoorRoom",), one_way(None, CanUse("Jump")), out=CanUse("Jump")),
)
KEPT_OPEN = (
    # Turns the party back until the first boss; with the way up kept present it has nothing left to guard.
    EntityRef("SnakemouthFallRoom", "blocker"),
)
KEPT_PRESENT = (
    # Crystal berry #32 and the vine it hangs in, made only after the first boss.
    EntityRef("SnakemouthBridgeRoom", "coilyvine"),
    EntityRef("SnakemouthBridgeRoom", "VinedItem"),
    # The big door to Upper Snakemouth; the Peculiar Gem slot right behind it still locks the way.
    EntityRef("SnakemouthDoorRoom", "DoorLoadZone"),
    # The bounce mushroom back up to the pitfall room, so the trapdoor is never a dead end.
    EntityRef("SnakemouthFallRoom", "JumpShroom"),
    # The door at the top of the bounce mushroom.
    EntityRef("SnakemouthFallRoom", "LoadingZoneDoorRoom"),
)
SCENERY_HIDDEN = (
    # The door room's big door, shut until the trapdoor (flag 14): open from the start, since arriving through it left
    # the party stuck behind it.
    EntityRef("SnakemouthDoorRoom", "Base/Door"),
    EntityRef("SnakemouthDoorRoom", "Base/Door (1)"),
)
SCENERY_PRESENT = (
    EntityRef("SnakemouthDoorRoom", "Base/Door (2)"),
    EntityRef("SnakemouthDoorRoom", "Base/Door (3)"),
)
PRESENT_FROM = (
    # The way back down to the fall room, made from the trapdoor instead of the first boss; not from the start, since
    # that would skip the scene where Leif's joining begins.
    FlagEntity("SnakemouthDoorRoom", "LoadZoneFallRoom", 14),
)
