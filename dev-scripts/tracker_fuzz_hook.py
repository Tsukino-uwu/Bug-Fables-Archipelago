"""Universal Tracker's yaml-less fuzzer hook, for test-apworld.ps1's tracker pass:

    python fuzz.py -r <runs> -n 1 -g bug_fables --hook tracker_fuzz_hook:Hook   (dev-scripts on PYTHONPATH)

Universal Tracker's own YamllessHook regenerates each fuzzed seed from its slot_data and checks, sphere by sphere, that
it puts the same locations in logic as the real generation. TrackerCore keeps every world it regenerates in class-level
lists, and fuzz.py's workers live for the whole run, so this empties them before each run.
"""
from __future__ import annotations

from worlds.tracker.fuzzer_hook import YamllessHook
from worlds.tracker.TrackerCore import TrackerCore


class Hook(YamllessHook):
    def before_generate(self, args) -> None:
        TrackerCore.cached_multiworlds.clear()
        TrackerCore.cached_slot_data.clear()
        super().before_generate(args)
