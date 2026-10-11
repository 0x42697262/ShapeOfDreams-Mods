# Fast kill follows the players

The [heroic fast kill](heroic-bosses.md) raises the difficulty (world) of
every monster when the party kills a wave's heroic bosses too fast. It's
tracked per player, so the raise follows the players who earned it: when
one of them leaves, it shrinks to what the players still here would have
earned; when that player comes back, it returns. Nothing else lowers it:
deaths, knock-outs, a weak build or a slow wave don't count. Turning the
setting off (0 s) removes it. Fast clear, which also followed the players,
was removed in schema 53; only heroic bosses are measured.

**What's recorded** (`PaceLedger`, one `HeroicFastKillRecord` per heroic
fast kill): the projected seconds to kill all of that wave's heroic
bosses, the extra worlds those bosses fought at (`FromExtraWorlds`: the
wave's scheduled extra worlds plus the raise already in force), and each
player's damage in the best burst window.

**From record to worlds.** For the players here, the projection is divided
by their share of that burst, then turned into a step as usual
(`HeroicFastKillRule.StepFor`: target ÷ projection, at most the largest
step, nothing at or over the target). `HeroicFastKillRule.RaiseFor` turns
that step into worlds from the record's `FromExtraWorlds`: the world where
the game's boss health curve is the step times the curve there, minus
`FromExtraWorlds` (`WorldScaling.ExtraWorldsForBossHealth`). The raise for
the players here is the sum over every record (`PaceLedger.RaisedWorlds`).

A player who dealt 99% of a ×100 burst takes almost all of that record's
raise with them; one who dealt half doubles the projection, which halves
the step unless it's still past the cap (×6 becomes ×3). A record whose
step falls to ×1 or below for the players here adds nothing. Because a
record keeps the world it was measured at, a smaller step is still a gap
measured from that world, not from today's.

Because each record is re-evaluated against the current settings, changing
`Heroic fast kill: seconds for all bosses` or `largest step` also changes
the raise from past fast kills. A record measured at ×100 (the cap) can
grow past ×100 later only if the cap is raised. At 0 seconds the raise is
0 while the records stay, so turning it back on restores it.

**Who is who.** A player is keyed by Steam ID (`DewPlayer.steamId`, set by
the server from the connection's auth), else by profile GUID, else by
network id (`PlayerKeys`). The key survives leaving and rejoining.
Damage by a player's summons counts for that player (the hit's actor's
`firstEntity.owner`). Damage with no player behind it (environments,
monsters, falling) is kept under a "no player" key that always counts.
Every player in `DewPlayer.gamePlayers` counts as here, dead or alive.

**When it applies.** The raise is worked out when a wave starts
(`WaveDirector.ApplyPlan`), so a leave mid-wave takes effect from the next
wave; monsters already out keep their stats (the game also adds its own
per-player health bonus only once, when a monster spawns). The log shows
`pace: wave N raises every monster +A of +B world(s) for the players here`
whenever the result changes, where +B is what the whole record would
give.

**Saved with the progress.** The record is saved with the wave progress
(`ProgressStore`, format `pace3`: per record
`projected:fromExtraWorlds:player=damage,...`, records split by `;`), so a mod reload keeps it,
players included. Progress saved by an older version (format `pace`,
which also held fast clears, or `pace2`, which had no world per record)
loads with no fast kills on record.
