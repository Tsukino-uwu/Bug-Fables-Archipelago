"""Wild Swamplands (the game's area 9, MapControl.areaid): its spots. Its rooms aren't mapped yet."""
from __future__ import annotations

from ..custom_rules import LATER_CHAPTERS, CanUse, Member
from ..data_types import Area, Location, Pickup, Source

LOCATIONS = (
    # Where the game teaches the Horn Dash (flag 39); the later chapters' story-order stand-in, every ability taught
    # before it.
    Location("Wild Swamplands: Bridge, Boulder", 72, "SwamplandsBridge",
             Source(event=131, flag=39),
             rule=CanUse("Beemerang Halt") & CanUse("Dash") & CanUse("Shield") & CanUse("Beetle Dig"),
             reach=LATER_CHAPTERS),
    # The lily pad pond (Swamplands2; the user, 2026-10-08): a Poison Bomb dug up in its top part, Beetle Dig; a
    # respawning Honey Drop in grass by the bottom door (regional flag 6, which the dig spot shares), the horn.
    Location("Wild Swamplands: Lily Pad Pond, Dig Spot", 181, "Swamplands2",
             Source(flag=737, pickup=Pickup(map="Swamplands2", type=0, item=31)), rule=CanUse("Beetle Dig"),
             category="dig_spot", no_jump=True, area="Top"),
    Location("Wild Swamplands: Lily Pad Pond, Grass in the Bottom Right", 182, "Swamplands2",
             Source(regional=6, pickup=Pickup(map="Swamplands2", type=0, item=1)), rule=CanUse("Horn Slash"),
             category="hidden_item", no_jump=True),
)
MAP_AREAS = (
    # The swamp boss's room (the user, 2026-10-08): its bottom (the door, the save crystal, the healing flower) the map's
    # own region; its top door across lily pads, Jump, past the boss in the middle (Event137, until 359), fought from
    # either side: Leif, as it burrows, and it summons nothing. Both ways.
    Area("SwamplandsBoss", "Top", ("loadzonenorth",), CanUse("Jump") & Member("Leif")),
    # The swamp's second room (the user, 2026-10-08): its bottom door the map's own region; its top door up across lily
    # pads and grass-blocked ledges, Jump and the horn or Bee Fly; back down the horn or Bee Fly, no Jump. The Leafbug
    # ambush before the top door (Event128) for the enemy pass.
    Area("Swamplands2", "Top", ("loadzonenorth",), CanUse("Jump") & (CanUse("Horn Slash") | CanUse("Bee Fly")),
         out=CanUse("Horn Slash") | CanUse("Bee Fly")),
)
