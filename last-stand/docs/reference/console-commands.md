# Console commands

With Developer Mode on (F1, then `` ` ``):

| Command | Does |
|---|---|
| `laststand_status` | Logs mode, phase, wave, time to next wave, alive, queued, and items dropped per rarity |
| `laststand_next` | Starts the next wave now, even if monsters remain (host only) |
| `laststand_wave <n>` | Starts wave n now, e.g. after testing (host only). Monsters already out stay |
| `laststand_gold <amount>` | Cheat: grants that gold to every player (host; needs `Allow cheat commands`) |
| `laststand_dust <amount>` | Cheat: grants that dream dust to every player |
| `laststand_level <n>` | Cheat: sets every hero to level n, capped at the game's max |
| `laststand_essence <rarity or name> [quality]` | Cheat: drops an essence near the host, reserved for the host. A rarity (`common`, `rare`, `epic`, `legendary`) picks a random one from the run's pool; otherwise a type name or part of a type or display name (`listgems` lists them). Quality defaults to the game's roll for that rarity and world |
| `laststand_memory <rarity or name> [level]` | Cheat: the same for a memory (`listskills` lists them); level defaults to the game's roll |
| `laststand_secondwind <charges> [seconds]` | Cheat: gives every hero the Second Wind star's effect (`Se_FatalHitProtection_Interrupt`, what `Se_Star_L_FatalHitProtection` grants) with that many charges. Each charge cancels a fatal hit, sets health to 25% and makes the hero invulnerable for `seconds` (default 3.5, the old global star's value). Running it again replaces the charges; 0 removes it |
| `laststand_skip [n]` | Cheat: ends the current wave now (its monsters and remaining spawns removed, no drops, Pain zone lifted) and starts the next wave at once, at that wave's own difficulty. Each skipped wave still counts as cleared (shrine resets, revives, progress save). With n, repeats n times, one wave per frame. In a break, skips the break |
| `laststand_testboss [type]` | Cheat: spawns a weak level 1 heroic boss (host, in the arena; needs `Allow cheat commands`) |
| `laststand_config` | Opens the Last Stand settings window. Refused while connected to someone else's lobby or run (`NetworkClient.active` without `NetworkServer.active`): only the host's settings apply. Works on the title screen and when hosting |
| `laststand_solo` | Starts a solo Last Stand run |
