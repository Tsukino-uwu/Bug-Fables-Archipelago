"""Termite Capitol (the game's area 18, MapControl.areaid): its spots, dock and ways between maps that aren't doors, and
what the seed changes there. Its rooms aren't mapped yet."""
from __future__ import annotations

from rule_builder.rules import True_

from ..custom_rules import LATER_CHAPTERS, SUBMARINE, SUBMARINE_KEY, CanUse
from ..data_types import Area, EntityRef, ItemEntity, ItemShop, Location, Pickup, Source, Transfer

LOCATIONS = (
    # The Termite King hands over the submarine after the Colosseum (flag 379); never behind the sub itself. The later
    # chapters' story-order stand-in.
    Location("Termite Capitol: Termite King's Reward", 76, "TermiteRoyalChamber",
             Source(event=164, flag=379),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig")
             & CanUse("Horn Dash") & CanUse("Bee Fly"), reach=LATER_CHAPTERS),
    # The Main Plaza (TermiteMainPlaza; named 2026-10-11): everything free. Its shop computer (Squash, Shell Ointment, Mega-Rush, Aphid
    # Shake): first purchase a check, then its own item.
    *(Location(f"Termite Capitol: Main Plaza, Shop {slot}", 261 + slot, "TermiteMainPlaza",
               Source(item_shop=ItemShop(map="TermiteMainPlaza", keeper="Fixedshopcomputer", item=item)),
               category="item_shop", no_jump=True)
      for slot, item in enumerate((125, 97, 155, 130), start=1)),
    # The Royal Chamber (TermiteRoyalChamber; named 2026-10-11): its floor free, the statue in its middle discovery 24;
    # crystal berry #33 hidden behind the thrones, up by Jump or Bee Fly as the thrones' platform is.
    Location("Termite Capitol: Royal Chamber, Termite Statue", 266, "TermiteRoyalChamber", Source(discovery=24),
             category="discovery", no_jump=True),
    Location("Termite Capitol: Royal Chamber, Behind the Thrones", 267, "TermiteRoyalChamber",
             Source(berry=33, pickup=Pickup(map="TermiteRoyalChamber", type=3, item=0)),
             rule=CanUse("Jump") | CanUse("Bee Fly"), category="crystal_berry", no_jump=True),
    # The Industrial District (TermiteIndustrial; named 2026-10-11): its street, shops and both elevators free. The
    # vista (discovery 43) on the right building's roof.
    Location("Termite Capitol: Industrial District, Rooftop Vista", 269, "TermiteIndustrial", Source(discovery=43),
             category="discovery", no_jump=True),
    # A Lore Book on the window apartment's balcony, flown to from the left building's roof.
    Location("Termite Capitol: Industrial District, Apartment Balcony", 270, "TermiteIndustrial",
             Source(flag=469, pickup=Pickup(map="TermiteIndustrial", type=1, item=52)), rule=CanUse("Bee Fly"),
             no_jump=True),
    # The bomb shop (Spicy, Burly, Poison, Frost, Numb, Sleep and Clear Bomb): first purchase a check, then its own item.
    *(Location(f"Termite Capitol: Industrial District, Bomb Shop {slot}", 270 + slot, "TermiteIndustrial",
               Source(item_shop=ItemShop(map="TermiteIndustrial", keeper="bombshop", item=item)),
               category="item_shop", no_jump=True)
      for slot, item in enumerate((30, 172, 31, 44, 45, 46, 36), start=1)),
    # Bomb Master on its own stand by the shop, 200 berries (Event226, flag 719).
    Location("Termite Capitol: Industrial District, Bomb Shop Medal", 278, "TermiteIndustrial",
             Source(event=226, flag=719), no_jump=True),
    # A Lore Book between the boxes outside the Colosseum's lobby; free.
    Location("Termite Capitol: Outside the Colosseum, Between the Boxes", 268, "TermiteIndustrial",
             Source(flag=381, pickup=Pickup(map="TermiteIndustrial", type=1, item=52)), no_jump=True,
             area="Outside the Colosseum"),
)
# The street outside the Colosseum is the same map, reached through a door pair that reloads it (loadzonenearcoliseum
# and the three NEARloadzoneback); both ways free.
MAP_AREAS = (
    Area("TermiteIndustrial", "Outside the Colosseum", ("NEARloadzonecoliseum",), True_()),
)
TRANSFERS = (
    Transfer("submarine", "TermitePier", "MetalLake", SUBMARINE),
    Transfer("arena", "TermiteColiseum1", "TermiteColiseum2", LATER_CHAPTERS),
)
# The submarine's dock exists with its key item in the bag, whatever the story's flags (379 here).
PRESENT_WITH_ITEM = (
    ItemEntity("TermitePier", "Fixedsub", SUBMARINE_KEY),
)
# The scientist and the queen show the dock off (Event165), so they wait for it too.
HELD_UNTIL_ITEM = (
    ItemEntity("TermitePier", "FixedScientist", SUBMARINE_KEY),
    ItemEntity("TermitePier", "FixedQueen", SUBMARINE_KEY),
)
# The gate opens from inside too (slot_data's termite_gate_from_inside): the mod marks it opened (flag 384) as its scene
# starts there, so the scene takes the opened gate's way through instead of looking for the outside guards.
# The queen and her guard by the plaza's gate, there until its first opening (flag 384) walks them off: kept away, as
# that opening is skipped (the user, 2026-10-07), so a party reaching the plaza another way never finds them waiting.
KEPT_OPEN = (
    EntityRef("TermiteMainPlaza", "elizant"),
    EntityRef("TermiteMainPlaza", "eventguard"),
)
