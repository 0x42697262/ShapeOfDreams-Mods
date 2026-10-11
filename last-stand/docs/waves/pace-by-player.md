# Fast kill follows the players

The [heroic fast kill](heroic-bosses.md) raises later heroic bosses to a
stronger world when the party kills them too fast. It's tracked per
player, so the raise follows the players who earned it: when one of them
leaves, it shrinks to what the players still here would have earned; when
that player comes back, it returns. Nothing else lowers it: deaths,
knock-outs, a weak build or a slow wave don't count. Mobs have no such
raise any more: fast clear, which also followed the players, was removed
in schema 53, and mobs just follow the wave's world.

**What's recorded** (`PaceLedger`, one `HeroicFastKillRecord` per heroic
fast kill): the projected seconds to kill all of that wave's heroic bosses
and each player's damage in the best burst window. For the players here,
the projection is divided by their share of that burst, then turned into
a step as usual (`HeroicFastKillRule.StepFor`: target ÷ projection, at
most the largest step, nothing at or over the target). A player who dealt
99% of a ×100 burst takes almost all of it with them; one who dealt half
halves the projection's speed. The health multiplier is the product of
every record's step, and each wave turns it into the heroic bosses' world
raise.

Because each record is re-evaluated against the current settings, changing
`Heroic fast kill: seconds for all bosses` or `largest step` also changes
the steps of past fast kills. A record measured at ×100 (the cap) can grow
past ×100 later only if the cap is raised.

**Who is who.** A player is keyed by Steam ID (`DewPlayer.steamId`, set by
the server from the connection's auth), else by profile GUID, else by
network id (`PlayerKeys`). The key survives leaving and rejoining.
Damage by a player's summons counts for that player (the hit's actor's
`firstEntity.owner`). Damage with no player behind it (environments,
monsters, falling) is kept under a "no player" key that always counts.
Every player in `DewPlayer.gamePlayers` counts as here, dead or alive.

**When it applies.** The multiplier is worked out when a wave starts
(`WaveDirector.ApplyPlan`), so a leave mid-wave takes effect from the next
wave; bosses already out keep their stats (the game also adds its own
per-player health bonus only once, when a monster spawns). The log shows
`pace: wave N gives heroic bosses xA of xB health for the players here`
whenever the result changes, where xB is what the whole record would
give.

**Saved with the progress.** The record is saved with the wave progress
(`ProgressStore`, format `pace2`, heroic records only), so a mod reload
keeps it, players included. Progress saved by an older version (format
`pace`, which also held fast clears) loads with an empty record.
