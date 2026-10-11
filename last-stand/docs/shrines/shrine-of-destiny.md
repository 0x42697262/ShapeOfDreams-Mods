# Shrine of Destiny

The host can also place the game's `Shrine_Destiny`, which takes essence
quality instead of gold.

- **Sacrifice** (the alternate interact) opens the edit window on the
  player's essences. The chosen essence loses the reroll cost in quality, or
  is destroyed if it would fall under 10. The shrine then offers that player
  a random essence of the same rarity or better (`currentItems`, a synced
  `SyncDictionary` keyed by player). Sacrificing again rerolls the offer.
- **Reroll cost:** `max(average rare essence quality for the zone × 0.35, 20)
  × (1 + 0.25 × rerolls)`, rounded to 10 and at least 20. The per-player
  reroll count is synced, so a vanilla client's tooltip shows the exact cost
  and the "will be destroyed" warning. It keeps climbing all run.
- **Claim** (the normal interact) drops the offered essence reserved for the
  player, and runs `DoPostUseRoutines`. Sacrifices don't count as uses.
- **2 claims per player per wave** (`Shrine of Destiny claims per wave`).
  Unlimited claims let one high-quality essence be sacrificed into dozens of
  new ones, since each sacrifice only takes 20 or so quality. The prefab
  allows one claim per player, and that limit isn't synced, so a vanilla
  client blocks itself once its synced use count reaches 1. The host counts
  each player's claims (from the `DoPostUseRoutines` postfix, which now gets
  the user) and, while a player is under the limit, writes their synced use
  count back to 0. The last claim leaves it at 1, which also blocks further
  sacrifices. When a wave is cleared every count is cleared
  (`ResetUseCounts`; log: `destiny: claims reset after wave N`). A mod reload
  forgets the host's tally, so a player who had one claim left gets two.
- **Placement:** by the edge of the room, see [Edge shrines](edge-shrines.md).
- Setting: `Shrine of Destiny`, default on. After a mod reload or a revisit,
  the shrine already standing anywhere in the arena is reused.
