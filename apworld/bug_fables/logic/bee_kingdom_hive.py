"""Bee Kingdom Hive (the game's area 12, MapControl.areaid): what the seed changes there. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..data_types import EntityRef

KEPT_PRESENT = (
    # Beette, who sells the Flower Key (the plaza's red house) on the balcony, made only after chapter 3 (flag 299)
    # (the user, 2026-10-04: "make it appear always").
    EntityRef("BeehiveBalcony", "smug bee"),
)
