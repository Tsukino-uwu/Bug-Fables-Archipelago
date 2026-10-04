"""Rubber Prison (the game's area 14, MapControl.areaid): its dock. Its rooms aren't mapped yet."""
from __future__ import annotations

from rule_builder.rules import Has

from ..custom_rules import ANY_ATTACK, LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY
from ..data_types import DoorRule, EntityRef, ItemEntity, Transfer

# The checkpoint corridor's gates open and shut by switches (the user, 2026-10-04: "has to be considered a oneway").
# From the yard its gates may be shut (their switch on that side comes only from flag 79), so it never leads on; that
# needs the corridor split into two areas (room-logic.md), not yet possible: Known issues.
DOOR_RULES = (
    # Across to the yard from the far side: the switches take an attack.
    DoorRule("RubberPrisonCheckpointCorridor", "loadzoneexit", ANY_ATTACK),
    # Back to the spike room: its prison door (until flag 538) opens with the Explorer Permit.
    DoorRule("RubberPrisonCheckpointCorridor", "loadzoneforward", Has("Explorer Permit")),
)

TRANSFERS = (
    Transfer("submarine", "RubberPrisonPier", "MetalLake", LATER_CHAPTERS & SUBMARINE),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (448).
PRESENT_WITH_ITEM = (
    ItemEntity("RubberPrisonPier", "Fixedsub - Duplicate - Duplicate", SUBMARINE_KEY),
)
# The rock just inside the yard's left door, broken only by Horn Dash (until flag 589): coming in that way without it
# stranded the party (the user, 2026-10-04).
KEPT_OPEN = (
    EntityRef("RubberPrisonPier", "rock"),
)
