from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld

from .options import option_groups


class BugFablesWebWorld(WebWorld):
    theme = "grassFlowers"
    tutorials = [  # noqa: RUF012 (WebWorld declares it a list)
        Tutorial(
            "Multiworld Setup Guide",
            "Setting up the Bug Fables randomizer mod and connecting to a room.",
            "English",
            "setup_en.md",
            "setup/en",
            ["Tsukino"],
        )
    ]
    option_groups = option_groups
