# Dream Arena design docs

How every part of the mod works, and why. The code has no comments, so these
files are where the design and its reasons live. Each file covers one topic,
so read only the one you need.

For what the mod is and how to play it, see the [project README](../README.md).
For working notes, hard constraints and the in-game test list, see
[`AGENTS.md`](../AGENTS.md).

## The match

| File | Covers |
|---|---|
| [PvP](match/pvp.md) | How players become enemies, team modes, camp vs arena, chat announcements |
| [Run rules](match/run-rules.md) | No game over, no monsters, lucid dreams, game modifiers, the run goal, the travel vote |
| [Restart](match/restart.md) | The `-restart` vote, wiping builds, back to camp |

## Places

| File | Covers |
|---|---|
| [Arena](arena/arena.md) | The arena room, plain (cleared) loading, cutscenes, the arrival ring, removing PvE |
| [Camp](camp/camp.md) | The direct rift, the remnant ring, the training dummy |

## Heroes

| File | Covers |
|---|---|
| [Heroes](heroes/heroes.md) | Max level, the memory floor, the health multiplier, revives, rescue quests |

## Reference

| File | Covers |
|---|---|
| [Settings](reference/settings.md) | Every setting, its page and default, the settings window, change notices, the config schema |
| [Console commands](reference/console-commands.md) | Every `arena*` command |
| [Networking](reference/networking.md) | What reaches vanilla clients, what can't, and a client who has the mod too |
| [Code layout](reference/code-layout.md) | Folders, the session, and where new code goes |
| [Releasing](reference/releasing.md) | Versions, packing, the Workshop upload and change notes |
