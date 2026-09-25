from dataclasses import dataclass

from Options import DefaultOnToggle, PerGameCommonOptions, Range, Toggle

from .data_tables import LOCATIONS


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


def category_count(category: str) -> int:
    """How many locations an option's category adds, straight from the location data."""
    return sum(1 for location in LOCATIONS if location.get("category") == category)


# Each toggle that adds locations says how many (the user, 2026-09-25: so people know what they're getting into),
# counted from the data so the number never goes stale.
ShuffleQuests.__doc__ = ShuffleQuests.__doc__.replace("{count}", str(category_count("quest")))
ShuffleCrystalBerries.__doc__ = ShuffleCrystalBerries.__doc__.replace("{count}", str(category_count("crystal_berry")))
ShuffleDiscoveries.__doc__ = ShuffleDiscoveries.__doc__.replace("{count}", str(category_count("discovery")))
ShuffleMedalShops.__doc__ = ShuffleMedalShops.__doc__.replace("{count}", str(category_count("shop")))


@dataclass
class BugFablesOptions(PerGameCommonOptions):
    artifacts_required: ArtifactsRequired
    shuffle_quests: ShuffleQuests
    shuffle_crystal_berries: ShuffleCrystalBerries
    shuffle_discoveries: ShuffleDiscoveries
    shuffle_medal_shops: ShuffleMedalShops
