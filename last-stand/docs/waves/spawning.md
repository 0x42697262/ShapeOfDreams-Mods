# How monsters are spawned

Through the room's own spawner, `RoomMonsters`, so everything that hooks into
it still applies: room modifiers, Corrupted Chaos, the vanilla Mirage Skin
modifier and the like.

- **Regular and hunter:** `RoomMonsters.SpawnMonsters` with a one-monster rule
  per monster type, marked `isBossSpawn` so the game spawns exactly one and
  doesn't add waves, random hunters or wait on population. The wave's world
  bonus (see [Waves](waves.md)), the hunter buff and any Mirage skin are
  added right after it spawns.
- **Miniboss:** `RoomMonsters.SpawnMiniBoss`, with the monster picked from the
  unlocked `ISpawnableAsMiniBoss` types and a random `MiniBossEffect`. This is
  the game's own promotion: bigger, tougher, unstoppable, and it drops the
  stock miniboss chaos reward on death. It gets the game's miniboss scaling
  up to the wave's world, with no extra multiplier.
