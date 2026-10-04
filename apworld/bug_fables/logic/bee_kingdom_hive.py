"""Bee Kingdom Hive (the game's area 12, MapControl.areaid): its spots, and what the seed changes there. Its rooms aren't
mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS
from ..data_types import EntityRef, FreeSale, Give, Location, Source

LOCATIONS = (
    # Beette sells the Flower Key (the plaza's red house) on the balcony, free in a seed; her next line sets flag 228.
    Location("Bee Kingdom Hive: Balcony, Beette's Sale", 78, "BeehiveBalcony",
             Source(flag=228, give=Give(map="BeehiveBalcony", type=1, item=54)), reach=LATER_CHAPTERS),
)
KEPT_PRESENT = (
    # Beette herself, made only after chapter 3 (flag 299) (the user, 2026-10-04: "make it appear always").
    EntityRef("BeehiveBalcony", "smug bee"),
)
FREE_SALES = (
    # Her offer ("150 berries for the house") and the sale's price commands.
    FreeSale("BeehiveBalcony", (20, 21)),
)
