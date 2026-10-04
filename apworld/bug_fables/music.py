"""Music Shuffle: which track plays in place of each, rolled at generation and sent in slot_data."""
from __future__ import annotations

from random import Random

# Every track, as the game's MainManager.Musics names them (a clip must carry one of these names).
TRACKS: tuple[str, ...] = (
    "Field0", "Battle0", "Cave0", "Battle1", "Calm", "Inside0", "LevelUp", "Theater", "Tension", "Chef0", "Moth",
    "Beetle", "Title", "Field1", "Inside1", "Inside2", "Dungeon0", "Mothiva", "Festival", "Battle2", "Venus", "Field2",
    "Dungeon1", "Miniboss", "Field3", "Battle3", "Wind", "Water", "Dungeon2", "Chef1", "Chef2", "Bee", "Battle4",
    "Dungeon2b", "Sad", "MothivaCalm", "BeeQ", "Tension2", "MachineHum", "Battle5", "Dungeon3", "Dungeon4", "Field4",
    "Battle6", "Cave1", "Sad2", "Battle7", "Field5", "Submarine", "Termite", "Breathing", "TermiteLoop", "WaspHive",
    "Dungeon5", "Invasion", "MetalIsland", "Bounty", "Centipede", "Lab", "FlyingBee", "Battle8", "Battle9", "Alert",
    "Giant1", "Giant2", "Giant3", "Final1", "Final2", "MiteKnight", "Field6", "Pier", "Credits", "Tension3", "Field7",
    "TeamSnek",
)

# Title plays before the client connects; the four ambience beds are sound, not songs (Samira leaves them out too).
# The factory's Dungeon2 and Dungeon2b are shuffled: the mod makes the elevator's crossfade between them a plain fade.
KEPT: frozenset[str] = frozenset({"Title", "Wind", "Water", "MachineHum", "Breathing"})

# Names in the game's list with no clip behind them: one played in a track's place would leave it playing as itself.
NO_CLIP: frozenset[str] = frozenset({"Beetle", "Giant2", "Giant3"})

POOL: tuple[str, ...] = tuple(track for track in TRACKS if track not in KEPT and track not in NO_CLIP)

# The short pieces the game plays as sounds at music volume: the victory fanfare, the game over, the chapter titles
# (ch1-ch7) and two arrivals. They swap among themselves.
JINGLES: tuple[str, ...] = (
    "BattleWon", "Gameover", "Snakemouth", "SandCastleRise", "ch1", "ch2", "ch3", "ch4", "ch5", "ch6", "ch7",
)


def shuffle(names: tuple[str, ...], random: Random) -> dict[str, str]:
    """{track: the track played in its place}: a permutation, so every track still plays somewhere."""
    played = list(names)
    random.shuffle(played)
    return dict(zip(names, played))
