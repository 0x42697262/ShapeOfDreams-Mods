# Starting world

`Starting world` is the difficulty setting. It sets the world number the run
counts as by writing `ZoneManager.currentZoneIndex` (a SyncVar, set to the
value minus 1) once in camp, once the first world has loaded and before the
arena loads. Last Stand never travels to another world, so it stays there for
the run.

**World 10 is the standard difficulty** (`Tuning.BaselineWorld`, the
default). Monster strength there matches Last Stand's tuning as it was before
this setting existed, when the run stayed at world 1. Lower is easier, down to
world 1, higher is harder, following the game's own world curves:

| World | Regular health | Regular damage | Heroic boss health |
|---|---|---|---|
| 1 | ×0.022 | ×0.021 | ×0.016 |
| 5 | ×0.21 | ×0.23 | ×0.15 |
| 10 | ×1 | ×1 | ×1 |
| 20 | ×5 | ×3 | ×7.5 |

(Relative to world 10, at difficulty factor 1; minibosses use the game's
miniboss curve.)

How it works: the game adds its world bonus to every monster in
`DewDifficultySettings.ApplyDifficultyModifiers`, from `ActorManager`'s
entity-add event, with a curve per kind (regular, miniboss, boss). A postfix
on that method (`WorldScalingPatch` → `WorldScaling.Rebase`) takes the
monster back to its world 1 stats and multiplies the result by
`curve(world) / curve(10)` for its kind. Last Stand's per-wave bonus is
multiplied by the same ratio when it's added, and the miniboss and heroic
boss multipliers multiply the whole thing, so every wave keeps its shape and
the world only scales it. Bosses' helpers (pillars, hallucinations, Erebos)
go through the same postfix.

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
