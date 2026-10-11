# Reminiscence of Luck

The host can also place the game's `Shrine_Luck`, a `ChoiceShrine`.

- **What it does:** every player has their own list of 3 choices (a synced
  `choices` dictionary by player guid), each a random memory or essence of one
  rarity rolled from the run's normal chances (`LootManager.SelectSkillRarity`)
  at the zone's level, plus the room's bonus level. Picking one is free
  (`baseGoldCost` 0) and drops it reserved for that player.
- **Praying** (the alternate interact) costs gold and adds one more choice of
  the same rarity. The price is the choices' average buy price × 0.22 ×
  (1 + 0.3 × prays so far), at most 3 prays (`maxPrayCount`, checked on
  every game). Buy prices go through `goldCostMultiplier`, so prays grow
  with the [gold price](../rewards/gold-prices.md) multiplier, and with free
  gold shrines on a pray costs 0 or 1 gold.
- **Once per player each wave.** The prefab allows one pick per player
  (`maxUseCountPerPlayer` 1). When a wave is cleared the host clears the
  synced use counts and pray counts and rolls new choices for every player
  (`PopulatePlayerChoices`), so everyone gets a new free pick and three new
  prays (log: `luck: picks, prays and choices renewed after wave N`). Choices
  not picked are replaced. A player who joins mid-run gets choices from the
  game's own `onGamePlayerAdded` hook.
- Setting: `Reminiscence of Luck`, default on. After a mod reload or a
  revisit, the shrine already standing anywhere in the arena is reused.
