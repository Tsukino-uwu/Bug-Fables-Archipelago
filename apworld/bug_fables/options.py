from dataclasses import dataclass

from Options import PerGameCommonOptions, Range


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


@dataclass
class BugFablesOptions(PerGameCommonOptions):
    artifacts_required: ArtifactsRequired
