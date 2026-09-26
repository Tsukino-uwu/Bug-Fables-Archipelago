from dataclasses import dataclass

from Options import Choice, DefaultOnToggle, PerGameCommonOptions, Range, Toggle

from .data_tables import DOORS, ENCOUNTERS, LOCATIONS, STARTS


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
    EXPERIMENTAL. Doors between areas lead somewhere else. Coupled: a door and its way back stay a pair, so turning
    round takes you back where you came from. Every area stays reachable through doors.

    The logic doesn't follow the doors yet: items are placed as if the doors were where the game has them, so a seed
    with this on may not be finishable (the pause menu's Warp button gets you out of a dead end). Off by default.

    Doors in this version: {count}.
    """

    display_name = "Entrance Randomizer (experimental)"
    option_off = 0
    option_coupled = 1
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
    EXPERIMENTAL. Where a new file begins. Off: where the game begins, outside Bugaria. Anywhere: beside any save point in
    the game, even in the middle of a dungeon. The pause menu's Warp takes you back to it.

    The logic doesn't know the start yet: items are placed as if you began outside Bugaria, so a seed started anywhere
    may not be finishable (the map's fast travel and the Warp get you around). Off by default.

    Save points to start at in this version: {count}.
    """

    display_name = "Starting Location (experimental)"
    option_off = 0
    # Not "random": Archipelago reserves it (any Choice can be set to random).
    option_anywhere = 1
    default = 0


def category_count(category: str) -> int:
    """How many locations an option's category adds, straight from the location data."""
    return sum(1 for location in LOCATIONS if location.get("category") == category)


# Counted from the data so the numbers players see never go stale.
ShuffleQuests.__doc__ = ShuffleQuests.__doc__.replace("{count}", str(category_count("quest")))
ShuffleCrystalBerries.__doc__ = ShuffleCrystalBerries.__doc__.replace("{count}", str(category_count("crystal_berry")))
ShuffleDiscoveries.__doc__ = ShuffleDiscoveries.__doc__.replace("{count}", str(category_count("discovery")))
ShuffleMedalShops.__doc__ = ShuffleMedalShops.__doc__.replace("{count}", str(category_count("shop")))
ShuffleItemShops.__doc__ = ShuffleItemShops.__doc__.replace("{count}", str(category_count("item_shop")))
EntranceRandomizer.__doc__ = EntranceRandomizer.__doc__.replace("{count}", str(2 * len(DOORS["connections"])))
EnemyShuffle.__doc__ = EnemyShuffle.__doc__.replace("{count}", str(len(ENCOUNTERS)))
StartingLocation.__doc__ = StartingLocation.__doc__.replace("{count}", str(len(STARTS)))


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
