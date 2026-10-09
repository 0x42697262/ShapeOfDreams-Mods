# How monsters are spawned

Through the room's own spawner, `RoomMonsters`, so everything that hooks into
it still applies: room modifiers, Corrupted Chaos, the vanilla Mirage Skin
modifier and the like.

- **Regular and hunter:** `RoomMonsters.SpawnMonsters` with a one-monster rule
  per monster type, marked `isBossSpawn` so the game spawns exactly one and
  doesn't add waves, random hunters or wait on population. The wave's stat
  bonus, the hunter buff and any Mirage skin are added right after it spawns.
- **Miniboss:** `RoomMonsters.SpawnMiniBoss`, with the monster picked from the
  unlocked `ISpawnableAsMiniBoss` types and a random `MiniBossEffect`. This is
  the game's own promotion: bigger, tougher, unstoppable, and it drops the
  stock miniboss chaos reward on death. On top of that, one frame after it
  spawns, its health is multiplied by 1.5 and its attack and ability damage
  by 3 (`Tuning.BaselineMiniBossHealthMultiplier` and `...DamageMultiplier`,
  no longer settings), the same true ×N as heroic bosses:
  `(N − 1) × (100% + the bonus it already has)`. Pain minibosses don't get
  this multiplier.
