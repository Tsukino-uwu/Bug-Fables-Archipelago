"""Far Grasslands (the game's area 8, MapControl.areaid): its spots, and what the seed changes there. Its rooms are
being mapped."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion, False_, Has

from ..custom_rules import ANY_ATTACK, CanUse, one_way
from ..data_types import Area, DoorRule, EntityRef, Location, Pickup, Source, StoryEvent, Transfer

LOCATIONS = (
    # Crystal berry #31 dug up at the crossroads (the first Far Grasslands room), reached with nothing: Beetle Dig.
    Location("Far Grasslands: Crossroads, Dig Spot", 170, "FarGrasslands1",
             Source(berry=31, pickup=Pickup(map="FarGrasslands1", type=3, item=0)), rule=CanUse("Beetle Dig"),
             category="crystal_berry", no_jump=True),
    # Discovery 36, "Wizard's Tower": from the lookout rock on the cave door's side, up with Jump (its talk sets flag
    # 546, which the journal check on each room load turns into the discovery), or on entering the tower's stairs room
    # (MapControl). One check, either way (the user, 2026-10-08).
    Location("Far Grasslands: Wizard's Tower, Lookout Rock", 171, "FarGrasslandsWizard", Source(discovery=36),
             rule=CanUse("Jump") | CanReachRegion("WizardTowerStairs"), category="discovery"),
    # Crystal berry #26 on top of the big tree root west of the crossroads, walked up to with nothing (the user).
    Location("Far Grasslands: West Path, On the Tree Root", 172, "FarGrasslands2",
             Source(berry=26, pickup=Pickup(map="FarGrasslands2", type=3, item=0)), category="crystal_berry",
             no_jump=True),
    # The lake (the user, 2026-10-08): a Lore Book behind a flower and a Hot Drink beside a tree, nothing needed; Dark
    # Cherries dug up in the middle, reached with Icicle or Bee Fly.
    Location("Far Grasslands: Lake, Behind the Flower", 173, "FarGrasslandsLake",
             Source(flag=460, pickup=Pickup(map="FarGrasslandsLake", type=1, item=52)), no_jump=True),
    Location("Far Grasslands: Lake, Beside the Tree", 174, "FarGrasslandsLake",
             Source(flag=736, pickup=Pickup(map="FarGrasslandsLake", type=0, item=177)), no_jump=True),
    Location("Far Grasslands: Lake, Dig Spot", 175, "FarGrasslandsLake",
             Source(flag=632, pickup=Pickup(map="FarGrasslandsLake", type=0, item=121)),
             rule=(CanUse("Icicle") | CanUse("Bee Fly")) & CanUse("Beetle Dig"), category="dig_spot", no_jump=True),
    # The tower's basement (the user, 2026-10-08): a Burly Tea on the floor behind the stairs, nothing needed; crystal
    # berry #37 on the bookshelf, from the door's ledge up ledges to the left, Jump and Bee Fly or the Beemerang.
    Location("Far Grasslands: Wizard's Tower Basement, Behind the Stairs", 177, "WizardTowerBasement",
             Source(flag=547, pickup=Pickup(map="WizardTowerBasement", type=0, item=81)), no_jump=True),
    Location("Far Grasslands: Wizard's Tower Basement, On the Bookshelf", 178, "WizardTowerBasement",
             Source(berry=37, pickup=Pickup(map="WizardTowerBasement", type=3, item=0)),
             rule=CanUse("Jump") & (CanUse("Bee Fly") | CanUse("Beemerang Toss")), category="crystal_berry",
             area="Top Right"),
)
STORY_EVENTS = (
    # The border cave's two gates, each opened for good by its lever on the far side (Event136 sets the lever's
    # activationflag, which hides that side's SnekGate): a basic attack (the user, 2026-10-07).
    StoryEvent("Far Grasslands: Border Cave, Left Gate Opened", "Border Cave Left Gate Open", "FGCave",
               Source(flag=362), rule=ANY_ATTACK, area="Left"),
    StoryEvent("Far Grasslands: Border Cave, Right Gate Opened", "Border Cave Right Gate Open", "FGCave",
               Source(flag=361), rule=ANY_ATTACK, area="Right"),
)
# The hole beside the tower's front door, ringed with grass: the horn, one way in (the user, 2026-10-08). The front
# door itself is open both ways in a seed (below).
DOOR_RULES = (
    DoorRule("FarGrasslandsWizard", "loadzonebasement", CanUse("Horn Slash")),
    # The Wasp Kingdom's front gate between the lake and outside the hive: in the game its door (both ends) is there
    # only from peace with the wasps (flag 555, after the story), a grate and a gate shutting it until then. Kept shut
    # (the user, 2026-10-08: opened, its far side lands inside the wasps' patrol, which catches the party in a loop).
    DoorRule("FarGrasslandsLake", "loadzonewasp", False_()),
    DoorRule("WaspKingdomOutside", "loadzonesouth", False_()),
)
_SWAMP_TOP = CanUse("Jump") & CanUse("Icicle") & CanUse("Horn Dash")
# Above the west path, back up to its raised left after the drop: from the bottom right by Bee Fly, from the top right
# by Horn Dash (the user).
_UP_TO_LEFT = CanUse("Jump") & (CanUse("Bee Fly") | CanUse("Horn Dash"))
TRANSFERS = (
    # East of the crossroads: from the right door back to the left part, Jump up rocks, then a drop (the user).
    Transfer("drop", "FarGrasslands3", "FarGrasslands3", CanUse("Jump"), two_way=False,
             way_back=CanUse("Horn Slash") & CanUse("Jump"), from_area="Right"),
    # Above the west path: the thorns round its bottom right, crossed to the top right with the Shield or Bee Fly, both
    # ways (the user). An Area has one link, so this second one, inside the room, is a transfer.
    Transfer("thorns", "FarGrasslands4", "FarGrasslands4", CanUse("Shield") | CanUse("Bee Fly"),
             from_area="Top Right"),
)
MAP_AREAS = (
    # The border cave (the user, 2026-10-07): its bottom door the map's own region; the ant tunnel's miner at the top
    # past grass, the horn both ways; its left and right doors (to the Broodmother's lair and the Wasp Kingdom) each
    # behind a gate its own side's lever opens.
    Area("FGCave", "Tunnel", (), CanUse("Horn Slash")),
    Area("FGCave", "Left", ("loadzonebroodmother",), Has("Border Cave Left Gate Open")),
    Area("FGCave", "Right", ("loadzone wasp",), Has("Border Cave Right Gate Open")),
    # Outside the border cave (the user, 2026-10-08): its top door (to the cave) and save crystal the map's own region;
    # its right door behind two rocks, Horn Dash both ways (they stay broken, by regional flags, but a file need not
    # have broken them); its bottom left door (to the wizard's) by burrowing, Beetle Dig both ways.
    Area("FarGrasslandsOutsideCave", "Right", ("loadzone right",), CanUse("Horn Dash")),
    Area("FarGrasslandsOutsideCave", "Bottom Left", ("loadzonewizard",), CanUse("Beetle Dig")),
    # The wizard's tower outside (the user, 2026-10-08): its door from outside the border cave the map's own region;
    # the tower side (its front door and the hole) across with the Shield or Bee Fly.
    Area("FarGrasslandsWizard", "Tower", ("loadzonetower", "loadzonebasement"), CanUse("Shield") | CanUse("Bee Fly")),
    # The second grasslands room (west of the crossroads): its top door up a ledge, Jump; a drop down (the user,
    # 2026-10-08).
    Area("FarGrasslands2", "Top", ("loadzone north",), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
    # Outside the Fishing Village: its door down a ledge, Jump both ways (the user, 2026-10-08). Riz guards it until his
    # fight (Event176, flag 509), always offered in a seed (riz_fight_with_follower); won with plain attacks.
    Area("FarGrasslandsOutsideVillage", "Village Door", ("loadzonevillage",), CanUse("Jump")),
    # East of the crossroads (the user, 2026-10-08): its left door and most of the room the map's own region; the
    # bounce pad past grass, the horn; the right door from the pad, Jump across platforms (back to the left, below).
    Area("FarGrasslands3", "Bounce Pad", (), CanUse("Horn Slash")),
    Area("FarGrasslands3", "Right", ("loadzoneright",), CanUse("Jump"), out=False_(), to="FarGrasslands3 (Bounce Pad)"),
    # Outside the swamp (the user, 2026-10-08): its bottom (the door from the Fishing Village's outside, the save
    # crystal, the swamp's door) the map's own region; its top (the doors to the swamp's boss and the Wasp Kingdom) up
    # with Jump, Icicle and Horn Dash; down, a drop to the boulders, then Icicle and Horn Dash.
    Area("FGOutsideSwamplands", "Top", ("loadzoneswamp2", "loadzone wasp"), _SWAMP_TOP,
         out=one_way(CanUse("Icicle") & CanUse("Horn Dash"), _SWAMP_TOP)),
    # Above the west path (the user, 2026-10-08): its bottom right (the door from the west path) the map's own region,
    # ringed with thorns (the transfer above). The raised left (the door from outside the border cave): up with Jump
    # and Bee Fly; down into the middle past its path's boulder, a drop: onto the thorns with the Shield, or by Bee Fly
    # (the middle's thorns then crossed the same way). The top right, where the clearing door puts the party (past the
    # boulder before it, Horn Dash back): to and from the left over the middle's ledges and platforms, Jump and Horn
    # Dash. The middle holds nothing, so it's no part of its own.
    Area("FarGrasslands4", "Left", ("loadzone left",), CanUse("Jump") & CanUse("Bee Fly"),
         out=one_way(CanUse("Shield") | CanUse("Bee Fly"), _UP_TO_LEFT)),
    Area("FarGrasslands4", "Top Right", (), CanUse("Jump") & CanUse("Horn Dash"), to="FarGrasslands4 (Left)"),
    Area("FarGrasslands4", "Clearing Door", ("loadzoneclearing",), CanUse("Horn Dash"),
         out=one_way(None, CanUse("Horn Dash")), to="FarGrasslands4 (Top Right)"),
    # The tower's basement (the user, 2026-10-08): the floor, where the hole from outside lands, the map's own region;
    # the door to the stairs at the top right up a ledge, Jump, a drop down.
    Area("WizardTowerBasement", "Top Right", ("loadzone",), CanUse("Jump"), out=one_way(None, CanUse("Jump"))),
)
# Maki's turn-back before the Wasp Kingdom's front gate at the lake (Event12, spoken by the follower), there until the
# swamp bridge falls (336), which the seed never lets happen: kept away (the user, 2026-10-08). Past it there is only
# the gate's grate.
KEPT_OPEN = (
    EntityRef("FarGrasslandsLake", "blocker"),
    # Outside the swamp, the same turn-back before the Wasp Kingdom door (Event12, until the swamp's boss, 359).
    EntityRef("FGOutsideSwamplands", "blocker"),
    # The wizard's tower, open in a seed (the user, 2026-10-08: "just remove/skip the basement cutscene with the
    # spider and then always have the tower door be open both in/out"). The fall's scene (Event166, until 449) only
    # sets 449, which only swaps the hole's trigger for its door, makes the front door outside and removes the
    # basement's wizard (no lines of his own): the trigger and that wizard kept away, the two doors kept present.
    EntityRef("FarGrasslandsWizard", "basementevent"),
    EntityRef("WizardTowerBasement", "wizard"),
)
# Outside the swamp, the Wasp Kingdom door (to the patrols' side entrance), there in the game only from the swamp's boss
# (359): kept present from the start (the user, 2026-10-08). Nothing in the Wasp Kingdom reads 359, and the border
# cave's right door already leads there before the boss.
KEPT_PRESENT = (
    EntityRef("FGOutsideSwamplands", "loadzone wasp"),
    # The wizard's tower (above): the hole's door down and the front door outside, both made in the game from 449.
    EntityRef("FarGrasslandsWizard", "loadzonebasement"),
    EntityRef("FarGrasslandsWizard", "loadzonetower"),
)
# The wizard's tower (above): the front door's model outside and its lock on the stairs' side, hidden in the game from
# the attic wizard's first talk (450), hidden from the start.
SCENERY_HIDDEN = (
    EntityRef("FarGrasslandsWizard", "Base/Tower/Door"),
    EntityRef("WizardTowerStairs", "Base/DoorLock"),
)
