"""Metal Island, over the sea from the pier. No spots yet."""
from __future__ import annotations

from ..custom_rules import BOAT_TICKET
from ..data_types import Transfer

TRANSFERS = (
    # The sailor sails only for the Boat Ticket (with Progressive Boat on, its first copy).
    Transfer("boat", "BugariaPier", "MetalIsland1", BOAT_TICKET),
)
