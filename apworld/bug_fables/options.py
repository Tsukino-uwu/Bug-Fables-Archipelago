from dataclasses import dataclass

from Options import Choice, DefaultOnToggle, OptionGroup, PerGameCommonOptions, PlandoConnections, Range, Toggle

from .data_tables import DOOR_NAMES, DOORS, ENCOUNTERS, LOCATIONS, ONE_WAY_LANDINGS, ONE_WAY_NAMES, ROOM_STARTS
from .shop_inventories import SPOTS


class ArtifactsRequired(Range):
    """
    How many artifacts you need to collect to win. The game has 7, one per chapter's milestone.

    Asking for more than this version of the world includes lowers the goal to what it includes, with a
    warning, so a seed can always be finished.
    """

    display_name = "Artifacts Required"
    range_start = 1
    range_end = 7
    default = 1


class ShuffleQuests(DefaultOnToggle):
    """
    Quest rewards are locations: quests from the quest board, and side quests such as helping a lost kid.

    Turned off, quests give their usual rewards and aren't part of the seed.

    Checks added in this version: {count}.
    """

    display_name = "Shuffle Quests"


class ShuffleCrystalBerries(DefaultOnToggle):
    """
    Crystal berry spots are locations, and the berries are items. Some are well hidden.

    Turned off, crystal berries stay where they are and the crystal berry shop works as usual.

    Checks added in this version: {count}.
    """

    display_name = "Shuffle Crystal Berries"


class ShuffleDiscoveries(Toggle):
    """
    Journal discoveries are locations: recording a discovery (examining a statue, a hidden spot, arriving somewhere
    new) sends a check. They give no item of their own. Off by default.

    Checks added in this version: {count}.
    """

    display_name = "Shuffle Discoveries"


class ShuffleMedalShops(DefaultOnToggle):
    """
    Medals sold in shops are locations: the shelf shows what's really there, and buying it sends the check.

    Turned off, shops sell their own medals as usual.

    Checks added in this version: {count}.
    """

    display_name = "Shuffle Medal Shops"


class ShuffleItemShops(DefaultOnToggle):
    """
    The first purchase of each item in an item shop is a location: the shelf shows what's really there, and buying it
    sends the check. After that, the shop restocks it as often as you like: its own item, or with Shuffle Shop
    Inventories on (the default) the one the seed gives that slot.

    Turned off, item shops sell their own items as usual (or the seed's, with Shuffle Shop Inventories).

    Checks added in this version: {count}.
    """

    display_name = "Shuffle Item Shops"


class ShopContents(Choice):
    """
    What shop locations may hold, when shops are shuffled. Shops put many easy checks in one place, which can soak up
    the important items; this keeps them spread over the world.

    Anything: any item, progression included.
    No Progression: no item that unlocks something (useful, filler and trap items only); those are out in the world.
    Filler Only: small items only. If the whole room has too few small items to fill every shop, this seed's shops use
    No Progression instead, and the generator says so.
    """

    display_name = "Shop Contents"
    option_anything = 0
    option_no_progression = 1
    option_filler_only = 2
    default = 1


class ShuffleShopInventories(DefaultOnToggle):
    """
    What item shops restock and what respawning floor items come back with, shuffled among themselves and fixed by the
    seed. It never touches an Archipelago check or location: the first purchase of each item in an item shop (Shuffle
    Item Shops) and the first pickup of a respawning floor item are still checks. This only changes what they sell and
    give after that, or from the start in a shop that isn't a location.

    Only those items take part (food and other consumables): a shop may sell what another shop or a floor item had,
    each item is still sold or found as often as before, and no shop sells the same item twice. The price is the
    game's own for the item sold. No item, check or rule depends on it. On by default.

    Shop slots and floor items in this version: {count}.
    """

    display_name = "Shuffle Shop Inventories"


class EntranceRandomizer(Choice):
    """
    EXPERIMENTAL. Doors between areas lead somewhere else, and every room stays reachable.

    Room Swap: whole rooms trade places with rooms that have as many doors, in the same part of the world. The map
    keeps the game's shape; only which room sits where changes. Turning round takes you back where you came from.
    Coupled: any door may lead to any other; a door and its way back stay a pair, so turning round takes you back.
    Decoupled: any door may lead to any other, and the way back is shuffled too: turning round can take you somewhere
    else.

    The logic follows the doors, but not yet what each room needs inside: a room can ask for more than the logic knows,
    so a seed with this on may not be finishable (the pause menu's Warp button gets you out of a dead end). Off by
    default.

    Doors in this version: {count}.
    """

    display_name = "Entrance Randomizer (experimental)"
    option_off = 0
    option_coupled = 1
    option_room_swap = 2
    option_decoupled = 3
    default = 0


class EnemyShuffle(Choice):
    """
    Changes which enemies you fight at each place, fixed by the seed. Each enemy on each map gets another fight of
    the same size: a lone enemy stays a lone enemy, a group of three stays three. The enemy you see walking around
    still looks like the original one for now; the fight is the shuffled one.

    Off: every fight is the game's own.
    Enemies Only: ordinary enemies on maps are shuffled among themselves. Bosses stay where they are.

    Bosses (Bosses Only, Both, Chaos) come in a later version. Map fights shuffled in this version: {count}.
    """

    display_name = "Enemy Shuffle"
    option_off = 0
    option_enemies_only = 1
    default = 0


class StartingLocation(Choice):
    """
    EXPERIMENTAL. Where a new file begins. Off: where the game begins, outside Bugaria. Anywhere: any room in the game,
    even in the middle of a dungeon, arriving as if through one of its doors. The pause menu's Warp takes you back to
    it.

    The logic doesn't know the start yet: items are placed as if you began outside Bugaria, so a seed started anywhere
    may not be finishable, and a room with no free way out can strand you (a new seed then). Off by default.

    Room arrivals to start at in this version: {count}.
    """

    display_name = "Starting Location (experimental)"
    option_off = 0
    # Not "random": Archipelago reserves it (any Choice can be set to random).
    option_anywhere = 1
    default = 0


class StartingPartyMember(Choice):
    """
    Off: the story's party (Vi and Kabbu from the start, Leif after the spider in Snakemouth Den).

    Vi, Kabbu or Leif: a new file starts with that one member, and the other two are items, found like any other.
    Random Member: one of the three, picked by the seed. All Three: a new file starts with the whole party, and no
    member is an item. With any of these, the two places where the story adds a member (the opening outside the city,
    and the fall room after the spider) become locations, whoever starts.

    The logic is cautious for now: with one member, everything past the Outskirts gate needs all three members, and a
    few spots before it need Kabbu's horn or Leif's ice. All Three by default.
    """

    display_name = "Starting Party Member"
    option_off = 0
    option_vi = 1
    option_kabbu = 2
    option_leif = 3
    # Not "random": Archipelago reserves it (any Choice can be set to random, Off included).
    option_random_member = 4
    option_all_three = 5
    default = 5


class FillerStartingChecks(DefaultOnToggle):
    """
    The checks a new file sends by itself when the game begins (Maki and Eetl's gift, the tutorial battle, and the
    opening spot where the story's second member joins, when party members are items) hold filler only: no
    progression, useful or trap item, so a seed doesn't open with its good items before you've played. Nothing else
    changes, the spot in the fall room after the spider included. Turn it off to plando an item there.

    With the Entrance Randomizer on Coupled or Room Swap it doesn't apply, and the generator says so: the doors can
    leave the start with only these spots, too few to begin a seed. On by default.
    """

    display_name = "Filler Starting Checks"


class ShuffleFieldMoves(Toggle):
    """
    The three field attacks are items too: Vi's Beemerang Toss (the first Progressive Beemerang), Kabbu's Horn Slash and
    Leif's Freeze (the first Progressive Freeze). Until one arrives, that attack does nothing but a short "can't" sound,
    and each shows in the key items once it does. A move works only with its member in the party too. The abilities the
    story teaches later (the Beemerang Halt, Bee Fly, the Dash and Horn Dash, Beetle Dig, the Icicle, the Shield) are
    items in every seed.

    The logic is cautious for now: everything past the Outskirts gate needs all three moves, and a few spots before it
    need the one they were seen to need. Off by default.
    """

    display_name = "Shuffle Field Moves"


class ShuffleJump(Toggle):
    """
    Jump is an item, for the whole party: until it arrives, the jump button does nothing but a short "can't" sound.
    The pause menu's Warp is always there with it on, the way out of a spot you can't jump out of.

    The logic is cautious for now: every check needs Jump except the few seen reachable without it (the opening, the
    ladybug siblings' house, the caravan and the town's shops). Off by default.
    """

    display_name = "Shuffle Jump"


class PointsOfNoReturn(Toggle):
    """
    Off: wherever the logic sends you, it leaves you a way to walk back.

    On: the logic may send you somewhere only the pause menu's Warp gets you out of (a drop, a one-way door, a
    transfer with no way back), so items can land in more places, and you're expected to warp back to the start. The
    Warp is always there with it on.

    No one-way in the logic has a way back for it to drop until its rooms are mapped, so for now it changes nothing.
    Off by default.
    """

    display_name = "Points of No Return"


class ProgressiveBoat(DefaultOnToggle):
    """
    The Boat Ticket and the submarine (the Subaquatic Maritime Neotransport) are one progressive item, found twice: the
    first copy is the Boat Ticket, the second the submarine, so the submarine always comes after the ticket. Each shows
    in your key items under its own name. The submarine's docks are there only once it is yours.

    Off: they are two separate items, found in any order. The Boat Ticket takes you to Metal Island on the pier's boat;
    the submarine works every dock, Metal Island's included. On by default.
    """

    display_name = "Progressive Boat"


class MusicShuffle(Toggle):
    """
    Every song plays in place of another, the same way every time the seed is played: an area's music, a battle's, a
    boss's. The short jingles (the victory fanfare, the game over, the chapter titles) swap among themselves. The title
    screen, the wind, water, machine and breathing sounds, and the factory elevator's music stay as they are. Samira plays the
    song you pick. Nothing else changes: no item, check or rule depends on it. Off by default.
    """

    display_name = "Music Shuffle"


class DoorPlando(PlandoConnections):
    """
    Which door leads where, with the Entrance Randomizer on Coupled or Decoupled (ignored when it's off or on Room
    Swap). Each door is named "<map>: <door>", as the spoiler log's Entrances section lists them. entrance is the door
    you go through, exit the door you arrive next to. direction: both (the default), entrance (only the entrance door
    leads to the exit door) or exit (only the exit door leads back to the entrance door); Coupled always joins both
    ways. A one-way door (a fog maze's wrong turn, a drop) takes another one-way's landing instead, named
    "<where it lands> as from <map>: <door>" as the spoiler lists it, and only goes one way, whatever the direction.
    The seed's host must have plando's "connections" turned on.
    """

    entrances = DOOR_NAMES | ONE_WAY_NAMES
    exits = DOOR_NAMES | ONE_WAY_LANDINGS

    @classmethod
    def can_connect(cls, entrance: str, exit: str) -> bool:
        # A one-way door goes only to a one-way's landing, and a door only next to a door.
        return ((entrance.lower() in {name.lower() for name in ONE_WAY_NAMES})
                == (exit.lower() in {name.lower() for name in ONE_WAY_LANDINGS}))


@dataclass
class BugFablesOptions(PerGameCommonOptions):
    artifacts_required: ArtifactsRequired
    shuffle_quests: ShuffleQuests
    shuffle_crystal_berries: ShuffleCrystalBerries
    shuffle_discoveries: ShuffleDiscoveries
    shuffle_medal_shops: ShuffleMedalShops
    shuffle_item_shops: ShuffleItemShops
    shop_contents: ShopContents
    shuffle_shop_inventories: ShuffleShopInventories
    entrance_randomizer: EntranceRandomizer
    plando_connections: DoorPlando
    enemy_shuffle: EnemyShuffle
    starting_location: StartingLocation
    starting_party_member: StartingPartyMember
    filler_starting_checks: FillerStartingChecks
    shuffle_field_moves: ShuffleFieldMoves
    shuffle_jump: ShuffleJump
    points_of_no_return: PointsOfNoReturn
    progressive_boat: ProgressiveBoat
    music_shuffle: MusicShuffle


# Options not in a group show under "Game Options".
option_groups = [
    OptionGroup("Aesthetic Options", [MusicShuffle]),
]


# The location categories a yaml toggle leaves out, and the toggle's field.
CATEGORY_OPTIONS: dict[str, str] = {
    "quest": "shuffle_quests",
    "crystal_berry": "shuffle_crystal_berries",
    "discovery": "shuffle_discoveries",
    "shop": "shuffle_medal_shops",
    "item_shop": "shuffle_item_shops",
}


def category_count(category: str) -> int:
    """How many locations an option's category adds, straight from the location data."""
    return sum(1 for location in LOCATIONS if location.category == category)


# Counted from the data so the numbers players see never go stale.
for _category, _field in CATEGORY_OPTIONS.items():
    _option = BugFablesOptions.type_hints[_field]
    _option.__doc__ = _option.__doc__.replace("{count}", str(category_count(_category)))
EntranceRandomizer.__doc__ = EntranceRandomizer.__doc__.replace("{count}", str(2 * len(DOORS.connections)))
EnemyShuffle.__doc__ = EnemyShuffle.__doc__.replace("{count}", str(len(ENCOUNTERS)))
StartingLocation.__doc__ = StartingLocation.__doc__.replace("{count}", str(len(ROOM_STARTS)))
ShuffleShopInventories.__doc__ = ShuffleShopInventories.__doc__.replace("{count}", str(len(SPOTS)))
