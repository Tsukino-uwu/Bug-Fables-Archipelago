# agent_docs index

One line per file.

- [code-map.md](code-map.md): every source file, what it does, and links to the doc sections behind it. Start here from the code.
- [documentation.md](documentation.md): user-facing guide to how the MOD was made, step by step (game side). Process only, no game facts.
- [apimplementation.md](apimplementation.md): user-facing guide to the ARCHIPELAGO side: how the apworld and connection were built, step by step, plus how a game talks to Archipelago.
- [development.md](development.md): building from source, staging and copying into the game, a local test server, the apworld's tests, fuzzing and the Logic Test, the dev console and every Debug setting.
- [client-requirements.md](client-requirements.md): Archipelago's hard requirements for the client and the world, plus known failure modes. The checklist.
- [archipelago-review.md](archipelago-review.md): the full review of the project against everything Archipelago publishes (2026-09-29): each finding with its evidence, what's kept and what doesn't apply. The order of the work is `apimplementation.md`, Next 43.
- [MEASURED.md](MEASURED.md): game facts measured by us (classes, hooks, flags, save fields), each with evidence and date.
- [licensing.md](licensing.md): every third-party project, with its licence read from the file and what we may do with it.
- [references.md](references.md): projects compared with ours, and other apworlds the user knows from playing; what was taken from each.
- [room-logic.md](room-logic.md): every plan for the room logic, in one place: the rules, the model, every place the party can appear, the story and quest chains across rooms, the questions per room, the method and the tests. Read before any room-level logic.
- [log.md](log.md): dated session log. What was tried, what happened, what the user said.
