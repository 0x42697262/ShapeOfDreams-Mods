# Starting world

`Starting world` is the difficulty setting. It sets the world number the run
counts as by writing `ZoneManager.currentZoneIndex` (a SyncVar, set to the
value minus 1) once in camp, once the first world has loaded and before the
arena loads. Last Stand never travels to another world, so it stays there for
the run.

**The default is world 1**, the same as vanilla's first world (Forest of
Turbulence). The game adds its own world bonus to every monster in
`DewDifficultySettings.ApplyDifficultyModifiers`, with a curve per kind
(regular, miniboss, boss); Last Stand doesn't touch it.

**The run's world and the monsters' world are two numbers.** The run's
world is this setting, and it stays put. Monster stats then climb from it
on the game's own curves: each wave, Last Stand works out a higher world
for the monsters (1 more every 25 waves, then 1 every 2 waves from wave
200, smooth in between) and gives each monster the difference between
the curve there and at the run's world as one stat bonus (see
[Waves](../waves/waves.md), *Strength, by world*). Only health and damage
follow the monsters' world; everything below follows the run's world. So
a higher starting world shifts the whole monster climb up by the same
number of worlds and also raises loot, gold and prices, while the wave
climb on its own leaves those alone.

What a world means (the game's curves on Nightmare, vs world 1):

| World | Regular health | Regular damage | Boss health |
|---|---|---|---|
| 2 | ×1.7 | ×1.6 | ×1.7 |
| 3 | ×3.3 | ×3.2 | ×3.1 |
| 5 | ×9.5 | ×9.2 | ×9.6 |
| 10 | ×51 | ×48 | ×74 |
| 20 | ×297 | ×159 | ×678 |
| 50 | ×1774 | ×492 | ×6457 |

**Where the waves sit.** With the default world rise, monsters fight like
this world (add `starting world − 1` for a higher start; heroic bosses
can sit higher after a [fast kill](../waves/heroic-bosses.md)):

| Wave | 1 | 26 | 51 | 101 | 150 | 200 | 210 | 220 | 250 | 300 |
|---|---|---|---|---|---|---|---|---|---|---|
| Monster world | 1 | 2 | 3 | 5 | 7 | 9 | 14 | 19 | 34 | 59 |

The chat line of every wave ends with "Difficulty: X.X" (the monster world), and the
`wave N:` log line shows `world=`.

The index also moves everything else the game ties to the world number:
- **Boss skills:** `ZoneManager` rolls some zone indexes for hard boss ability
  sets when the world loads, so a forced index may or may not hit one.
- **Mirage skins:** stronger tiers and a higher base chance in
  `GameMod_MirageSkin`.
- **Loot:** higher memory levels and essence quality (`LootManager`).
- **Gold and XP:** more kill gold and experience, and higher service prices,
  Dream Well upgrades and Altar of Cleansing costs (`GameManager` curves).
- **Platinum:** a player who joins mid-run gets platinum equal to the zone
  index (`DewPlayer`'s catch-up), as in vanilla.

It's not capped (only the floor of 1). It's also the only part of the [top-right label](../hud/top-right-label.md) the host
can change: they see `<world>. <first world's name>` (the
Primus room adds its own +2). The name stays the run's first world, because
changing `currentZone` would also change monster pools, props, music and
profile stats.
