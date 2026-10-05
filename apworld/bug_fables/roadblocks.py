"""The Extra Roadblocks option, apart from options.py so the logic's rules can filter on it: options.py reads the
data tables, which read the logic."""
from __future__ import annotations

from Options import OptionSet


class ExtraRoadblocks(OptionSet):
    """
    Obstacles the game puts up later in the story, there from the start instead, each needing its ability to cross,
    both ways. Without one, the way it would block stays open all game.

    None yet: the Snakemouth Barrier was one until the horn tutorial was seen to move the party past it.
    """

    display_name = "Extra Roadblocks"
    valid_keys: list[str] = []
