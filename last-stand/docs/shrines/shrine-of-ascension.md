# Shrine of Ascension

The host can also place the game's `Shrine_Ascension`.

- **What it does:** for gold, it raises a memory or essence by one rarity
  (common to rare, rare to epic, epic to legendary, legendary to unique),
  keeping its level or quality. The item is swapped for a random one of the
  next rarity from the run's pool, and equipped in the same slot.
- **Locked until wave 1 is cleared** (the first reset wave). The shrine stands from the start but
  is locked (the synced `Shrine.isLocked`, the same lock the game puts on
  rewards until a room is cleared) until the first reset wave is cleared. A
  locked shrine refuses use on the host, so a vanilla client sees and gets
  exactly that, and clients play the game's unlock effect when it opens. The
  host compares the lock with the number of cleared waves once a second and
  on every wave clear, so it's right again after a mod reload or
  `laststand_wave` (log: `ascension: locked until wave 1 is cleared`, then
  `ascension: unlocked after wave 1 was cleared`).
- **Uses: vanilla's four per player, back every wave.** Once unlocked,
  players spend their uses whenever they like. It keeps the prefab's limits
  (`maxUseCountPerPlayer = 4`, no total cap, no cooldown), so every game
  shows the same "uses left". When each wave is cleared
  (`Ascension uses come back every N waves`, default 1; at 10 it would be
  waves 10, 20, 30…) the host clears the synced use
  counts (`Shrine.ResetUseCounts`; log: `ascension: uses reset after wave N
  was cleared`), so everyone has four again for the break and the waves
  after it. Unused uses don't carry over.
- **Price: vanilla's,** times the [gold price](../rewards/gold-prices.md)
  multiplier (up 5% every wave, compounding), or 0 with free gold shrines.
  Otherwise it's the base for the item's rarity (common 30, rare 60, epic 120,
  legendary 150, unique 30), times `1 + 0.1 × uses`, times the zone's service
  price curves, and a vanilla client is charged exactly what it's shown.
- **Mod reload:** the reset runs on the wave-cleared event, which fires once
  per wave even across a reload (a reload in the break doesn't replay it), so
  a reload never hands out extra uses.
- **Placement:** by the edge of the room, see [Edge shrines](edge-shrines.md).
- Setting: `Shrine of Ascension`, default on. After a mod reload or a revisit,
  the shrine already standing anywhere in the arena is reused and its prefab
  limits are put back.
