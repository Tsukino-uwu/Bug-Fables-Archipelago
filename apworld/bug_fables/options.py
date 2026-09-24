from dataclasses import dataclass

from Options import DefaultOnToggle, PerGameCommonOptions, Range


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
    """

    display_name = "Shuffle Quests"


class ShuffleCrystalBerries(DefaultOnToggle):
    """
    Crystal berry spots are locations, and the berries are items. Some are well hidden.

    Turned off, crystal berries stay where they are and the crystal berry shop works as usual.
    """

    display_name = "Shuffle Crystal Berries"


@dataclass
class BugFablesOptions(PerGameCommonOptions):
    artifacts_required: ArtifactsRequired
    shuffle_quests: ShuffleQuests
    shuffle_crystal_berries: ShuffleCrystalBerries
