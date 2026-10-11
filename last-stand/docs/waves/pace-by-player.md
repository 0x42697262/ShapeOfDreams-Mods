# Fast clear and fast kill follow the players

[Fast clear](waves.md) (mobs) and [heroic fast kill](heroic-bosses.md)
(heroic bosses) raise monster health when the party kills too fast. Both
are tracked per player, so the raise follows the players who earned it:
when one of them leaves, it shrinks to what the players still here would
have earned; when that player comes back, it returns. Nothing else lowers
it: deaths, knock-outs, a weak build or a slow wave don't count.

**What's recorded** (`PaceLedger`, one record per event):

- **A fast clear** (`FastClearRecord`): the instant kills needed (80% of
  the wave) and each player's instant kills that wave, counted until the
  wave ends, not just up to the trigger. It counts as a stack for the
  players here when their instant kills alone reach the number needed.
  So if the leaver made 30 of 80 needed kills and the rest made 60, the
  stack goes; if the rest made 85, it stays.
- **A heroic fast kill** (`HeroicFastKillRecord`): the projected seconds
  per boss and each player's damage in the best burst window. For the
  players here, the projection is divided by their share of that burst,
  then turned into a step as usual (`HeroicFastKillRule.StepFor`: target ÷
  projection, at most the largest step, nothing at or over the target).
  A player who dealt 99% of a ×50 burst takes almost all of it with them;
  one who dealt half halves the projection's speed. The multiplier is the
  product of every record's step.

Because each record is re-evaluated against the current settings, changing
`Heroic fast kill: seconds per boss` or `largest step` also changes the
steps of past fast kills. A record measured at ×50 (the cap) can grow
past ×50 later only if the cap is raised.

**Who is who.** A player is keyed by Steam ID (`DewPlayer.steamId`, set by
the server from the connection's auth), else by profile GUID, else by
network id (`PlayerKeys`). The key survives leaving and rejoining.
Damage and kills by a player's summons count for that player (the hit's
actor's `firstEntity.owner`). Kills and damage with no player behind
them (environments, monsters, falling) are kept under a "no player" key
that always counts. Every player in `DewPlayer.gamePlayers` counts as
here, dead or alive.

**When it applies.** The multipliers are worked out when a wave starts
(`WaveDirector.ApplyPlan`), so a leave mid-wave takes effect from the next
wave; monsters already out keep their stats (the game also adds its own
per-player health bonus only once, when a monster spawns). The log shows
`pace: wave N counts S of F fast clear(s) and heroic bosses xA of xB
health for the players here` whenever the result changes, where F and xB
are what the whole record would give.

**Saved with the progress.** The record is saved with the wave progress
(`ProgressStore`, format `pace`), so a mod reload keeps it, players
included. Progress saved by an older version loads with an empty record.
