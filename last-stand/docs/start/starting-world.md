# Starting world

`Starting world` is the difficulty setting. It sets the world number the run
counts as by writing `ZoneManager.currentZoneIndex` (a SyncVar, set to the
value minus 1) once in camp, once the first world has loaded and before the
arena loads. Last Stand never travels to another world, so it stays there for
the run.

**The default is world 1**, the same as vanilla's first world (Forest of
Turbulence). The game adds its own world bonus to every monster in
`DewDifficultySettings.ApplyDifficultyModifiers`, with a curve per kind
(regular, miniboss, boss); Last Stand doesn't touch it. The only multiplier
Last Stand adds is the per-wave bonus, and stat bonuses add up, so a monster
at world `N`, wave `W` has the game's world `N` bonus plus the wave bonus.

Raising the world makes monsters stronger by the game's curves (at
difficulty factor 1):

| World | Regular health | Regular damage | Heroic boss health |
|---|---|---|---|
| 1 | ×1 | ×0.9 | ×1 |
| 2 | ×1.7 | ×1.7 | ×1.7 |
| 3 | ×3.3 | ×3.4 | ×3.1 |
| 5 | ×9.5 | ×10 | ×9.6 |
| 10 | ×45 | ×44 | ×63 |

**Where the waves sit.** At world 1, the per-wave bonus alone (+10% a wave,
+50% from wave 150) makes a wave as tough as these vanilla worlds:

| Wave | Monster health and damage | Like vanilla world |
|---|---|---|
| 10 | ×1.9 | 2 |
| 30 | ×3.9 | 3 |
| 50 | ×5.9 | 4 |
| 100 | ×10.9 | 5 |
| 150 | ×15.9 | 6 |
| 200 | ×40.9 | 9 to 10 |
| 300 | ×90.9 | 14 |

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
