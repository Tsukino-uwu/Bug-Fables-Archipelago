"""Wasp Kingdom Hive (the game's area 19, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import False_, Has, True_

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import Area, EntityMove, EntityRef, Location, Pickup, SceneryCopy, SceneryLayer, Source

LOCATIONS = (
    # WaspKingdom3 (2026-10-11): the Wasp Key (key item 119, flag 364), which lay free under the left ledge once the
    # stealth wasps were away, moved up where it takes the Beemerang Halt and Bee Fly (ENTITIES_MOVED); may go back to
    # its own spot, free, later (2026-10-11).
    Location("Wasp Kingdom Hive: Fountain Room, On the Roof", 286, "WaspKingdom3",
             Source(flag=364, pickup=Pickup(map="WaspKingdom3", type=1, item=119)),
             rule=CanUse("Beemerang Halt") & CanUse("Bee Fly")),
)
ENTITIES_MOVED = (
    EntityMove("WaspKingdom3", "key", (-3.4, 6.0, 11.7)),
)
MAP_AREAS = (
    # Outside the hive (WaspKingdomOutside; 2026-10-11): from the cave's door the game pushes the party past a boulder
    # (rock, until 332), so that way in is free; back out to the cave, the boulder broken by Horn Dash (it stays broken).
    Area("WaspKingdomOutside", "Left", ("loadzonecave",), CanUse("Horn Dash"), out=True_()),
    # The hive's door (to the main hall): coming out, the party is put past its barrier; from outside the barrier
    # keeps it shut.
    Area("WaspKingdomOutside", "Hive Door", ("loadzoneinside#16",), False_(), out=True_()),
    # The hive's left door (to WaspKingdom1, a one-way drop there) inside grass that grows back, cut by Horn Slash both
    # ways.
    Area("WaspKingdomOutside", "Grass Door", ("loadzoneinside#14",), CanUse("Horn Slash"), out=CanUse("Horn Slash")),
    # WaspKingdom1 (2026-10-11): the drop from outside lands at its bottom left; up to its one door, to
    # WaspKingdom2, by Jump or Bee Fly.
    Area("WaspKingdom1", "Bottom", (), True_(), out=CanUse("Jump") | CanUse("Bee Fly"),
         landings=(("WaspKingdomOutside", "loadzoneinside#14"),)),
    # WaspKingdom2 (2026-10-11): its bottom (the Pantry's door and the lower left one, to 243) the map's own region; up
    # to the top (the save crystal, the upper left and right doors) on the wooden lift, started by a lever in a small
    # house dug into (Beetle Dig, then any attack) and boarded by Jump or Bee Fly; down, a free drop. The right door behind a lock that
    # takes the Wasp Key (Event59, key index 9, kept in a seed; gone from 363): arriving through it the game puts the party
    # past the lock; out through it with the key.
    Area("WaspKingdom2", "Top", ("loadzoneleft",),
         CanUse("Beetle Dig") & ANY_ATTACK & (CanUse("Jump") | CanUse("Bee Fly")), out=True_()),
    # WaspKingdom4 (2026-10-11): from its left door (the map's own region) a lever (any attack) opens a barrier to the
    # top, which shuts behind the party; a copied twig before it (SCENERY_COPIED) is dug under (Beetle Dig) both ways,
    # so the top is never a trap. Across the top under its own twig (Dig) to a lever (any attack) that opens the right
    # door's barrier, again on each visit; no way back from the right.
    Area("WaspKingdom4", "Top", (), ANY_ATTACK & CanUse("Beetle Dig"), out=ANY_ATTACK & CanUse("Beetle Dig")),
    Area("WaspKingdom4", "Right", ("loadzonenext",), ANY_ATTACK & CanUse("Beetle Dig"), out=False_(),
         to="WaspKingdom4 (Top)"),
    Area("WaspKingdom2", "Right Door", ("loadzoneright",), Has("Wasp Key"), out=True_(), to="WaspKingdom2 (Top)"),
)
# The two wasp troopers on patrol and their see area (Event138, until 347) guard only the hive's door, shut by its own
# barrier; caught, the party is put back where it came in, which from the lake's gate is inside the see area, a loop.
# Kept away (2026-10-11).
KEPT_OPEN = (
    EntityRef("WaspKingdomOutside", "trooper"),
    EntityRef("WaspKingdomOutside", "trooper - Duplicate"),
    EntityRef("WaspKingdomOutside", "see area"),
    # Inside, the stealth wasps (each NPC's battle data is the catch, Event138, and its sight): kept away like the
    # Bandit Hideout's, so only the way across a room is logic (2026-10-11). Caught in WaspKingdom3 the game also takes
    # the Wasp Key.
    *(EntityRef(m, e) for m, names in (
        ("WaspKingdom1", ("wasp", "wasp - Duplicate")),
        ("WaspKingdom2", ("wasp - Duplicate", "wasp - Duplicate - Duplicate")),
        ("WaspKingdom3", ("wasp", "waspsleep")),
        ("WaspKingdom4", ("wasp1", "wasp2", "wasp3", "wasp4", "wasp5")),
    ) for e in names),
)
# WaspKingdom4's twig (its only dig wall) copied across the way to its left barrier, the room's hard floor made ground
# so it can be dug under there: no way to the top without Beetle Dig, where the barrier shuts behind the party and only
# Dig leads on (2026-10-11, placed and seen).
SCENERY_COPIED = (
    SceneryCopy("WaspKingdom4", "Base/Twig3", (0.0, 16.8), 105.0),
)
SCENERY_LAYERS = (
    SceneryLayer("WaspKingdom4", "Base/hardfloor", 8),
)
