"""Wasp Kingdom Hive (the game's area 19, MapControl.areaid): its spots and what the seed changes there."""
from __future__ import annotations

from rule_builder.rules import False_, Has, True_

from ..custom_rules import ANY_ATTACK, CanUse, Member
from ..data_types import (Area, EntityMove, EntityRef, Give, ItemShop, Location, Pickup, SceneryCopy, SceneryLayer,
                          Source)

LOCATIONS = (
    # WaspKingdom3 (2026-10-11): the Wasp Key (key item 119, flag 364), which lay free under the left ledge once the
    # stealth wasps were away, moved up where it takes the Beemerang Halt and Bee Fly (ENTITIES_MOVED); may go back to
    # its own spot, free, later (2026-10-11).
    Location("Wasp Kingdom Hive: Fountain Room, On the Roof", 286, "WaspKingdom3",
             Source(flag=364, pickup=Pickup(map="WaspKingdom3", type=1, item=119)),
             rule=CanUse("Beemerang Halt") & CanUse("Bee Fly")),
    # The Prison (WaspKingdomPrison; named 2026-10-11): free between its two doors; Dark Cherries (item 121, flag 369)
    # dug up in its bottom right cell, past a boulder (Horn Dash) and burrowed into (Beetle Dig).
    Location("Wasp Kingdom Hive: Prison, Dig Spot", 287, "WaspKingdomPrison",
             Source(flag=369, pickup=Pickup(map="WaspKingdomPrison", type=0, item=121)),
             rule=CanUse("Horn Dash") & CanUse("Beetle Dig"), category="dig_spot", no_jump=True),
    # Jayde's Room (WaspKingdomJayde; 2026-10-11): everything free. Her meeting, the room's own scene on entering
    # (Event139, while 366 is off), ends with the game's second Wasp Key (the one for the Food Storage) and sets 366.
    Location("Wasp Kingdom Hive: Jayde's Room, Arrival", 291, "WaspKingdomJayde",
             Source(event=139, flag=366, give=Give(map="WaspKingdomJayde", type=1, item=119)), no_jump=True),
    # Her shop from 366 on (Jayde's Stew, Magic Seed, Mushroom Salad): first purchase a check, then its own item.
    *(Location(f"Wasp Kingdom Hive: Jayde's Room, Shop {slot}", 287 + slot, "WaspKingdomJayde",
               Source(item_shop=ItemShop(map="WaspKingdomJayde", keeper="jayde - Duplicate", item=item)),
               category="item_shop", no_jump=True)
      for slot, item in enumerate((120, 11, 15), start=1)),
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
    # Food Storage (WaspKingdom5; named 2026-10-11 from its sign): its bottom (the Barrier Room's door, the save crystal,
    # the right door) the map's own region; the top through a key door on the ground, the Wasp Key either way (gone
    # from 365); the vent to the main hall on a small ledge of its own, dropped from free, up by Jump.
    Area("WaspKingdom5", "Top", (), Has("Wasp Key"), out=Has("Wasp Key")),
    Area("WaspKingdom5", "Vent", ("loadzone vent",), CanUse("Jump"), out=True_(), to="WaspKingdom5 (Top)"),
    # The Main Hall (WaspKingdomMainHall; 2026-10-11): the vent from the Food Storage opens on a ledge of its own, a
    # drop into the hall with no way back up.
    Area("WaspKingdomMainHall", "Vent", ("loadzonevent",), False_(), out=True_()),
    # Its front door to Outside the Hive, made in a seed with its barrier here away (KEPT_PRESENT, SCENERY_OFF): a way
    # out of the hive, never in (Outside the Hive's barrier stays; arriving there puts the party past it).
    Area("WaspKingdomMainHall", "Front Door", ("loadzoneoutside",), True_(), out=True_()),
    # The throne room's door up top: Jump or Bee Fly from the hall floor, a free drop back down.
    Area("WaspKingdomMainHall", "Top", ("loadzonethrone",), CanUse("Jump") | CanUse("Bee Fly"), out=True_()),
    # The Throne Room (WaspKingdomThrone; named 2026-10-11): its bottom door free; up a ledge (Jump or Bee Fly) General
    # Ultimax, a Wasp Driller and a Wasp Bomber are fought (Event140, until 370): Kabbu for the Driller, nothing for the
    # others; the Queen's door beyond, Jump or Bee Fly both ways.
    Area("WaspKingdomThrone", "Top", ("loadzonequeen",), (CanUse("Jump") | CanUse("Bee Fly")) & Member("Kabbu"),
         out=CanUse("Jump") | CanUse("Bee Fly")),
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
# The hive's front door, made only from the ending (555) at both ends, kept present so the hive always has a way out;
# the Main Hall's barrier (Base/gate) off, Outside the Hive's kept, so it is one way out (2026-10-11).
KEPT_PRESENT = (
    EntityRef("WaspKingdomMainHall", "loadzoneoutside"),
    EntityRef("WaspKingdomOutside", "loadzoneinside"),
)
SCENERY_OFF = (
    EntityRef("WaspKingdomMainHall", "Base/gate"),
)
