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
)
STORY_EVENTS = (
    # The border cave's two gates, each opened for good by its lever on the far side (Event136 sets the lever's
    # activationflag, which hides that side's SnekGate): a basic attack (the user, 2026-10-07).
    StoryEvent("Far Grasslands: Border Cave, Left Gate Opened", "Border Cave Left Gate Open", "FGCave",
               Source(flag=362), rule=ANY_ATTACK, area="Left"),
    StoryEvent("Far Grasslands: Border Cave, Right Gate Opened", "Border Cave Right Gate Open", "FGCave",
               Source(flag=361), rule=ANY_ATTACK, area="Right"),
    # The wizard in the tower's attic, first talked to (his line from flag 450 on): 450 hides the front door outside
    # and the lock on the stairs' side (the user, 2026-10-08). The attic's own needs come with its mapping.
    StoryEvent("Far Grasslands: Wizard's Tower, Door Unlocked", "Wizard Tower Door Unlocked", "WizardTowerAttic",
               Source(flag=450)),
)
# The tower's front door, shut from both sides until the wizard unlocks it; the hole beside it, ringed with grass, the
# horn (the user, 2026-10-08). Before flag 449 the hole is the fall's trigger (Event166), after it a door: one way in.
DOOR_RULES = (
    DoorRule("FarGrasslandsWizard", "loadzonetower", Has("Wizard Tower Door Unlocked")),
    DoorRule("WizardTowerStairs", "loadzoneoutside", Has("Wizard Tower Door Unlocked")),
    DoorRule("FarGrasslandsWizard", "loadzonebasement", CanUse("Horn Slash")),
    # The Wasp Kingdom's front gate between the lake and outside the hive: in the game its door (both ends) is there
    # only from peace with the wasps (flag 555, after the story), a grate and a gate shutting it until then. Kept shut
    # (the user, 2026-10-08: opened, its far side lands inside the wasps' patrol, which catches the party in a loop).
    DoorRule("FarGrasslandsLake", "loadzonewasp", False_()),
    DoorRule("WaspKingdomOutside", "loadzonesouth", False_()),
)
TRANSFERS = (
    # East of the crossroads: from the right door back to the left part, Jump up rocks, then a drop (the user).
    Transfer("drop", "FarGrasslands3", "FarGrasslands3", CanUse("Jump"), two_way=False,
             way_back=CanUse("Horn Slash") & CanUse("Jump"), from_area="Right"),
)
MAP_AREAS = (
    # The border cave (the user, 2026-10-07): its bottom door the map's own region; the ant tunnel's miner at the top past
    # grass, the horn both ways; its left and right doors (to the Broodmother's lair and the Wasp Kingdom) each behind a
    # gate its own side's lever opens.
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
)
# Maki's turn-back before the Wasp Kingdom's front gate at the lake (Event12, spoken by the follower), there until the
# swamp bridge falls (336), which the seed never lets happen: kept away (the user, 2026-10-08). Past it there is only
# the gate's grate.
KEPT_OPEN = (
    EntityRef("FarGrasslandsLake", "blocker"),
)
