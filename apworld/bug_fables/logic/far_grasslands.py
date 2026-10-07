"""Far Grasslands (the game's area 8, MapControl.areaid): its spots, and what the seed changes there. Its rooms are
being mapped."""
from __future__ import annotations

from rule_builder.rules import CanReachRegion, Has

from ..custom_rules import ANY_ATTACK, CanUse
from ..data_types import Area, DoorRule, Location, Pickup, Source, StoryEvent

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
)
