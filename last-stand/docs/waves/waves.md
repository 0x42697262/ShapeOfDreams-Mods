# Waves

The first wave starts 10 s after arrival. A wave is **cleared** when everything
in it has spawned and died. The next wave starts 20 s after that, or 40 s after
a wave that had a miniboss, a Pain miniboss or a heroic boss (10, 20, 30, 40,
49, 50, 60…), so the party can use the well and pick up rewards. The pace is
set by how fast the party kills, not by a timer. The chat notice for a cleared
wave gives the break that applies. After a mod reload between waves, the
normal break is used, because the cleared wave's plan didn't survive. The score is the wave reached.

| From wave | What changes | Every |
|---|---|---|
| 1 | Regular monsters, 6 growing by 2 a wave | wave |
| 5 | Surge: twice the regular monsters (hunters included) and alive cap | 5 waves |
| 48 | Merged waves: at most 100 monsters and hunters, carrying the whole wave's strength; surges add 2 minibosses and 2 Pain minibosses | wave |
| 2 | +10% health and +10% damage on every monster, stacking | wave |
| 150 | The stacking step becomes +50% health and +50% damage | wave |
| 10 | 3 monster types from other worlds join the pool | 5 waves |
| 10 | Minibosses (1, 1, 2, 2… up to 10) | 10 waves |
| 20 | Pain minibosses (1, 1, 2, 2… up to 3) | 20 waves |
| 30 | The next stronger Mirage skin is unlocked | 10 waves |
| 130 | Mirage chance grows from 20% to 100% at wave 149 | wave |
| 49 | Heroic boss, last in the wave (1 at 49, 2 at 99 and 124, 3 at 149 and 174, 4 at 199…, up to 10 from 499) | 50 waves, 25 from wave 100 |
| 101 | Hunters: 1/30 of the regular monsters, 2/30 at 102… all of them from 130 on | wave |
| 101 | Hunter zone on every wave: falling artillery, hunt level 1 for 101–110, 2 for 111–120… up to 6 from 151 | wave |

How many monster types the other worlds add is only known at runtime. The log
reports it when the arena is ready: `catalog: N monster type(s) from other
worlds, M Mirage skin(s), …; hunters start at wave W`. With the 29 types seen
in testing, the last three join at wave 55. Hunters used to wait until every
type had joined, which put them at wave 295 with one type every 10 waves, so
now they have their own start wave.

Each wave is planned by `WaveSchedule`, plain logic with no game calls. It
works through these knobs, all using systems the game already syncs:

- **Count:** regular monsters start at 6 and grow by 2 a wave with no end
  (`2 × wave + 4` for one player: 224 at wave 110, 264 at 130). Each player
  past the first adds half a wave again (×1.5 for two, ×2 for three). Every
  5th wave is a **surge** that doubles the regular count (wave 5: 28 instead
  of 14) and the hunter count, and doubles the alive cap for that wave
  only, so the extra monsters are on the field together instead of
  queuing. Minibosses, Pain minibosses and heroic
  bosses are added on top and are not doubled. Wave 110 itself is a surge
  with 6 minibosses: 454 monsters for one player (448 + 6), 678 for two.
  An earlier version stopped the growth at wave 110 (`Monster count stops
  growing at wave`); merged waves replaced that, and the setting is gone.
- **Merged waves:** merging starts at the wave where one player's normal
  count reaches 100 (`Merged wave size`): `6 + 2 × (wave − 1) ≥ 100`, wave
  48 with the defaults. Change the wave 1 count or the growth and the start
  wave moves with them (`MergeRule.StartWave`; the log says `merge: from
  wave N, waves hold at 100 …` when the arena is ready). From then on a
  wave with more than 100 regular monsters and hunters spawns only 100,
  whatever the player count, and they carry the strength of the whole
  count: health and damage × count / 100, on top of the per-wave bonus and
  fast clear (multiplied, through `FastClearRule.Raise`, so the game's
  additive stat bonuses don't dilute it). Wave 48 solo is exactly 100 (×1),
  for two players 150 → ×1.5; wave 50 solo is a surge of 208 → ×2.08; wave
  131 solo is 266 → ×2.66; wave 135 for two players is a surge of 822 →
  ×8.22. Before the start wave nothing merges, even when co-op or a surge
  goes past 100. The hunter share applies to the 100. Bonus gold and dream
  dust per kill of these monsters are multiplied the same way
  (`LootService`), so a merged wave pays what the full count would have;
  vanilla kill gold isn't. Minibosses, Pain minibosses and heroic bosses
  aren't merged and don't get the merge multiplier. Instead, once waves
  merge, they have a health floor: at least 5× the max health of the
  toughest regular monster or hunter in the wave for minibosses and Pain
  minibosses (`Merged waves: miniboss health`), and 20× for heroic bosses
  (`Merged waves: heroic boss health`). Without it, ×8 monsters would
  leave the bosses weaker than the mobs around them. `BossHealthFloor`
  checks every 0.5 s during the wave: it remembers the highest max health
  any of the wave's monsters had, and raises any boss below its floor with
  one extra `maxHealthPercentage` bonus of `(floor / current − 1) × 100%`.
  The game multiplies percentage bonuses together (`BonusStats` `+` uses
  `DewMath.MultiplyPercentageBonuses`), so that bonus is exactly ×(floor /
  current), and `AddStatBonus` keeps the health ratio. A tougher monster later in
  the wave raises the floor again. Bosses spawn before most of the wave,
  so the first raise comes within a second of the first monsters. The log
  shows `boss health: wave N <type> maxHealth A -> B (5x the toughest
  monster's C)`. A boss already above its floor is left alone. Their
  rewards don't change. A surge still doubles the count, so it
  doubles the merged strength, and it also brings 2 extra minibosses
  (`Merged surges: extra minibosses`) and 2 extra Pain minibosses (`Merged
  surges: extra Pain minibosses`), added after the usual caps. The Pain
  group puts the Pain zone in place of that wave's hunter zone while it
  lives, and the wave's break is the 40 s boss break. Fast clear's instant
  kill share counts the merged wave. Curse kill goals count the full
  wave, so they keep growing (see [Curses](../shrines/curses.md)). The chat line for
  the wave says `They fight with the strength of N: xM health and damage
  each.` and the `wave N:` log line shows `merged=N/xM` (`merged=N/x1` when
  nothing merged). The rule is `MergeRule`, plain logic used by
  `WaveSchedule`.
- **Strength, per wave:** every spawned monster gets a stat bonus of +10% max
  health and +10% attack and ability damage for each wave after the first. So
  wave 11 is +100% health and +100% damage. From wave 150 (`Steeper scaling
  from wave`) each wave adds +50% instead (`Late health / damage per wave`),
  on top of what the earlier waves added: wave 149 is +1480%, wave 150
  +1530%, wave 160 +2030%, wave 200 +4030% (`WaveSchedule.StatBonus`). This is a server-side
  `StatBonus`, so clients see it through the synced stats.
- **Fast clear:** if 80% of a wave's monsters (`Fast clear: instant kills
  (%)`, rounded up, bosses included) are killed instantly, every monster
  from the next wave on gets ×2 health and damage (`Fast clear: stats from
  next wave (x)`). A monster is killed instantly when it dies within 1 s
  of the first hit it takes (`Fast clear: instant within (s)`; 0.5 s
  until schema 44); one that dies from its first hit counts as 0 s. It carries
  over (from wave 30 by default, see below): clear wave 54 fast and wave 55 is doubled, wave 56 keeps that ×2
  (plus its normal per-wave growth) with no further doubling, until another
  fast clear stacks a second ×2 (×4), and so on. Heroic bosses get a share
  of it (`Fast clear: heroic bosses too`, on, and `Fast clear: heroic boss
  share (%)`, 50): each fast clear counts as half a stack for them, so the
  boss multiplier is `M ^ (stacks × 0.5)`. With ×2, 10 fast clears give
  regular monsters ×1024 and heroic bosses ×32 (√1024). A team that can
  clear that fast already deals enormous damage, and a full ×1024 boss
  would be unkillable. At 0% or with the toggle off, bosses are left out.
  Chat says so at once, in red instead of the usual orange (the "Wave N
  cleared" line is green), and the log shows `fast clear: wave N had K of M
  monster(s) die within 1 s of their first hit`; every `wave N:` line
  shows `fastClear=xM`. A watched wave that misses logs how close it came
  when it's cleared: `fast clear: wave N had K of D death(s) within 1 s
  of the first hit; X needed`.
  Each monster's wave bonus is raised to `(100 + bonus) × M − 100`: with
  one fast clear, wave 21's +200% (3× base) becomes +500% (6× base),
  exactly double (`FastClearRule.Raise`, applied per monster in
  `WaveDirector.Enhance`). The game multiplies percentage stat bonuses
  together (`BonusStats` `+` uses `DewMath.MultiplyPercentageBonuses`), so
  a separate +100% bonus would also be ×2; raising the wave bonus does the
  same in one bonus. Each of Last Stand's own
  spawns records the time of its first `Entity.EntityEvent_OnTakeDamage`
  (fired on the server, before the lethal hit's `Kill`), and
  `EnemyRegistry.Died` reports the time from it to the death.
  `FastClearWatch` counts the instant ones, and `laststand_skip` doesn't
  count. The number of fast clears is saved
  with the wave progress (`ProgressStore`), so a mod reload keeps it (log:
  `progress restored: ... N fast clear(s) so far`). An earlier version
  counted kills within 10 s of the wave's start. Spawning tops out at about
  12 monsters a second, so big waves could never reach 80% in time however
  hard players hit; time from the first hit doesn't depend on the spawn
  rate, the wave size or the walk in. It also corrects itself: each ×2
  makes monsters take longer to die, until waves stop counting. Any of the
  three at 0 (or the multiplier at 1) turns it off. `Fast clear: from wave`
  (default 30) is the first wave that's watched: waves 1 to 29 never
  count, so the first possible doubling is wave 31. Stacks already earned
  stay when it's raised mid-run.
- **Strength, slowly:** the game's ambient level, `GameManager.SetAmbientLevel`,
  sets the level monsters spawn at. It rises by one every 4 waves
  (`Waves per monster level`).
- **Cap:** at most 35 monsters alive at once (`Max monsters alive`),
  whatever the player count. Spawns that would go over wait in a queue for
  the next opening, and are released in small batches (6 every 0.5 s). The
  host is the server, so if it lags, everyone lags. Every monster alive
  costs AI, pathing and network updates each frame, roughly in proportion,
  so the cap is the main lag control. It was 200. A surge doubles it for
  its wave (70).

Pain minibosses are queued first, then minibosses, so the specials always
arrive early in their wave. Hunters are spread evenly through the regular
monsters, so a wave that's one third hunters is one third hunters from start
to end. Heroic bosses
come last: see below.
