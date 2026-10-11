# Console commands

Developer Mode on, then `` ` `` opens the console. Commands that change the
match only work for the host of a PvP Arena run; anyone else gets `only the
host of a PvP Arena run can do that.` in the log.

| Command | Does |
|---|---|
| `arena` | Opens the settings window. Refused on a connected client, since only the host's settings apply |
| `arena_status` | Logs mode, session, phase, team mode, players, `gameOverEnabled` and whether a restart vote is pending |
| `arena_start` | Host: sweeps PvE from the arena and places every hero in the ring again |
| `arena_restart` | Host: restarts at once without a vote (see [Restart](../match/restart.md)) |
| `arena_revive` | Host: revives every knocked-out hero at full health |
| `arena_scan` | Logs the arena anchor, every entity with type, owner, relation to the first hero and whether a hero could attack it, then every actor in the room counted by type |
| `arena_purge` | Host: runs the arena sweep once, now |

`arena_scan` answers "what is drawing fire" (the entity list) and "what is
still here" (the actor list). Most room furniture derives from `Actor`, not
`Entity`, so an entity-only list reports an empty room while shrines still
stand.

The 0.1.0 commands that only changed a setting (`arena_on`, `arena_off`,
`arena_mode`, `arena_room`, `arena_revive <seconds>`) were removed: settings
change in one place, the window, which announces them.
