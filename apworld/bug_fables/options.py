from dataclasses import dataclass

from Options import Choice, DefaultOnToggle, PerGameCommonOptions, Range, Toggle

from .data_tables import DOORS, ENCOUNTERS, LOCATIONS, ROOM_STARTS


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
    sends the check. After that, the shop sells its own item again, as often as you like.

    Turned off, item shops sell their own items as usual.

    Checks added in this version: {count}.
    """

    display_name = "Shuffle Item Shops"


class ShopContents(Choice):
    """
    What shop locations may hold, when shops are shuffled. Shops put many easy checks in one place, which can soak up
    the important items; this keeps them spread over the world.

    Anything: any item, progression included.
    No Progression: useful and filler items only; everything that unlocks something is out in the world.
    Filler Only: small items only. If the whole room has too few small items to fill every shop, this seed's shops use
    No Progression instead, and the generator says so.
    """

    display_name = "Shop Contents"
    option_anything = 0
    option_no_progression = 1
    option_filler_only = 2
    default = 1


class EntranceRandomizer(Choice):
    """
    EXPERIMENTAL. Doors between areas lead somewhere else. In both modes a door and its way back stay a pair, so
    turning round takes you back where you came from, and every room stays reachable.

    Room Swap: whole rooms trade places with rooms that have as many doors, in the same part of the world. The map
    keeps the game's shape; only which room sits where changes.
    Coupled: any door may lead to any other.

    The logic doesn't follow the doors yet: items are placed as if the doors were where the game has them, so a seed
    with this on may not be finishable (the pause menu's Warp button gets you out of a dead end). Off by default.

    Doors in this version: {count}.
    """

    display_name = "Entrance Randomizer (experimental)"
    option_off = 0
    option_coupled = 1
    option_room_swap = 2
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
    few spots before it need Kabbu's horn. All Three by default.
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


@dataclass
class BugFablesOptions(PerGameCommonOptions):
    artifacts_required: ArtifactsRequired
    shuffle_quests: ShuffleQuests
    shuffle_crystal_berries: ShuffleCrystalBerries
    shuffle_discoveries: ShuffleDiscoveries
    shuffle_medal_shops: ShuffleMedalShops
    shuffle_item_shops: ShuffleItemShops
    shop_contents: ShopContents
    entrance_randomizer: EntranceRandomizer
    enemy_shuffle: EnemyShuffle
    starting_location: StartingLocation
    starting_party_member: StartingPartyMember
    shuffle_field_moves: ShuffleFieldMoves
    shuffle_jump: ShuffleJump


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
