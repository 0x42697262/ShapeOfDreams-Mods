# Chaos rewards

Every miniboss, Pain included, drops the game's stock chaos reward on death
(`RoomRewards.DropChaosReward`, a `Shrine_Chaos`), plus a second one for players
who roll their `doubleChaosChance`. The game rolls its rarity in
`Shrine_Chaos.SetRandomRarity` from odds stored on the prefab. Those odds gave a
legendary nearly every time, which made the game much easier with a miniboss
every 10 waves.

In Last Stand, a postfix on `SetRandomRarity` replaces that roll with its own:

| Rarity | Wave 1 | Wave 10 | Wave 20 | Wave 30 | Wave 40 | Wave 50 on |
|---|---|---|---|---|---|---|
| Legendary | 5% | 7.3% | 9.8% | 12.4% | 14.9% | 17.5% |
| Epic | 15% | 18.7% | 22.8% | 27% | 31.1% | 35.2% |
| Rare | 35% | 37.3% | 39.8% | 42.3% | 44.8% | 47.3% |
| Common | 45% | 36.7% | 27.6% | 18.4% | 9.2% | 0% |

The chances move in a straight line to their wave 50 target, then hold. The
targets are the old wave 50 rates scaled up so common is gone.

The rarity is set before the shrine is spawned, and the shrine builds its
reward choices from it afterwards (`OnLateStartServer`), so the choices match.
`rarity` is a SyncVar, so vanilla clients see the same rarity and color. The
Corrupted Chaos shrines from a Pain group are a different type with no rarity,
and are left alone.
