# Maw of Doom

The host can also place the game's `Shrine_MawOfDoom`.

- **What it does:** for gold, it raises a memory's level or an essence's
  quality by one step, or destroys the item instead. Vanilla's formulas, by the
  player's synced use count `x`: price `20 × x` gold (times the zone's service
  curves and `goldCostMultiplier`), destroy chance `min(0.08 × x, 0.5)`, and
  3 points less for an essence while that's under 20%.
- **Fixed price and risk: 20 gold, 8%.** The host writes every player's
  synced use count (`Shrine._useCountByPlayer`, a `SyncDictionary`) back to 1
  after each use, on arrival, when a wave is cleared, and once a second
  (`Tuning`) so a player who joins mid-run doesn't start at 0. At 1 the price is
  `20 × 1 = 20` gold (times the [gold price](../rewards/gold-prices.md)
  multiplier, which grows with the waves) and the destroy chance is 8% for a memory and 5% for an
  essence (vanilla takes 3 points off an essence's chance while it's under
  20%). Every client's tooltip shows exactly that. A higher pinned count
  raises both: 7 gives 140 gold and vanilla's 50% cap. Price and risk come from the same count,
  so a lower count would mean a lower risk; at 0 a use would be free and
  risk-free. The price is made free instead through the multiplier (see
  [free gold shrines](../rewards/gold-prices.md)), off by default: 0 gold, still 8%.
- Setting: `Maw of Doom`, default on. The prefab already has no use limit and
  no cooldown. After a mod reload or a revisit, the Maw already standing
  anywhere in the arena is reused.
