# Hatred Remnant

The host can also place the game's `Shrine_Hatred`.

- **What it does:** the shrine offers three curse strengths, rolled once
  when it's created from None, Mild, Potent and Powerful (None is
  effectively always there). Choosing one gives the player a random curse
  of that strength (none for None), then a **Hatred Shard**
  (`Shrine_Hatred_Reward`) for them alone. The shard gives one random reward
  scaled by the strength: gold, dream dust, a memory, an essence, or a Chaos
  shrine (not for None). No platinum coins.
- **Dream dust by wave.** The stock shard sizes its dust from the
  zone (`GameManager.GetSpecialRewardAmount_DreamDust`), and the arena never
  leaves the first zone, so it gave next to nothing. When a shard is opened
  (a prefix on `Shrine_Hatred_Reward.OnUse`, which runs on the host), Last
  Stand, for that call only, has a
  postfix make the special dust amount `Hatred Shard dream dust per wave` (50)
  × the current wave. The game then applies its own strength multiplier
  (None ×0.8, Mild ×1.5, Potent ×2.4, Powerful ×3.3, from the prefab) and
  ±15%. At wave 20 that's about 800 for None and 3300 for Powerful.
- **Gold by wave**, the same way. The stock gold amount
  (`GetSpecialRewardAmount_Gold`) is based on a buy price (`Gem.GetBuyGold`),
  and buy prices go through `goldCostMultiplier`, which is 0 with
  [free gold shrines](../rewards/gold-prices.md) (`GetBuyGold` never goes
  under 1, so a shard gave 1 gold). So while a shard opens, a postfix makes
  the special gold amount `Hatred Shard gold per wave` (50) × the current
  wave. The game then applies its own strength multiplier (None ×0.5, Mild
  ×0.8, Potent ×1.2, Powerful ×1.6, from the prefab's `goldMultipliers`) and
  ±15%, and drops it as a targeted, non-kill orb, so the kill gold cap
  doesn't touch it. At wave 50 that's about 1250 for None and 4000 for
  Powerful. With the setting at 0 the shard's `goldWeight` is set to 0, so
  it never rolls gold. Log: `hatred: shard opened at wave N; dream dust base
  X, gold base Y`.
  An earlier version set every shard's gold to a flat 500 through a
  `PickupManager.DropGold` prefix, skipping the strength multiplier. Next to
  dream dust in the thousands and gold prices that grow 5% a wave, it was
  worth almost nothing late in a run.
- **Once per player each wave.** The prefab allows one choice per player,
  and every game checks it the same way, so the host clears the synced use
  counts when a wave is cleared (log: `hatred: choices reset after wave N`).
  The three strengths stay the same all run, since the game rolls them once.
- **Curses end by kills** ([Curses](curses.md)).
- With [Curse to start](../waves/curse-to-start.md) on, the shrine is always
  placed, offers only curses, and a curse in a break curses everyone and
  starts the wave.
- Setting: `Hatred Remnant`, default on. After a mod reload or a revisit, the
  shrine already standing anywhere in the arena is reused.
