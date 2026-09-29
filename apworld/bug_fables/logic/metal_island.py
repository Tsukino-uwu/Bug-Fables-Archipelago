"""Metal Island, over the sea from the pier. No spots yet."""
from __future__ import annotations

from rule_builder.rules import Has

from ..data_types import Transfer

TRANSFERS = (
    # The sailor sails only for the Boat Ticket.
    Transfer("boat", "BugariaPier", "MetalIsland1", Has("Boat Ticket")),
)
