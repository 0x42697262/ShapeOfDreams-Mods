# Waves

The first wave starts 10 s after arrival. A wave is **cleared** when everything
in it has spawned and died. The next wave starts 20 s after that, or 40 s after
a wave that had a miniboss, a Pain miniboss or a heroic boss (10, 20, 30, 40,
50, 60… and the heroic boss waves 25, 75, 125…), so the party can use the well and pick up rewards.
If every live player votes in the break's [wave vote](wave-vote.md) (`Tab`),
the next wave starts at once. The pace is
set by how fast the party kills, not by a timer. The chat notice for a cleared
wave gives the break that applies. After a mod reload between waves, the
normal break is used, because the cleared wave's plan didn't survive. The score is the wave reached.

| From wave | What changes | Every |
|---|---|---|
| 1 | Regular monsters, 6 growing by 2 a wave | wave |
| 5 | Surge: twice the regular monsters (hunters included) and alive cap | 5 waves |
| 48 | Merged waves: at most 100 monsters and hunters, carrying the whole wave's strength; surges add 2 minibosses and 2 Pain minibosses | wave |
| 1 | Monster stats follow the game's world scaling, rising 1 world every 25 waves, a little each wave | wave |
| 200 | Faster world rise: 1 world every 5 waves | wave |
| 10 | 3 monster types from other worlds join the pool | 5 waves |
| 10 | Minibosses (1, 1, 2, 2… up to 10) | 10 waves |
| 20 | Pain minibosses (1, 1, 2, 2… up to 3) | 20 waves |
| 30 | The next stronger Mirage skin is unlocked | 10 waves |
| 130 | Mirage chance grows from 20% to 100% at wave 149 | wave |
| 25 | Heroic boss, last in the wave (1 at 25, 2 at 50, 3 at 75, 4 at 100… one more each boss wave, up to 10 from 250) | 25 waves |
| 100 | Hunters: 1/31 of the regular monsters, 2/31 at 101… all of them from 130 on | wave |
| 100 | Hunter zone on every wave: falling artillery, hunt level 1 for 100–104, 2 for 105–109… no cap | wave |

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
- **Milestone announcements:** the wave where a new rule starts gets one
  extra chat line in light blue (`SharedMessages.Announcement`, `#8fd3ff`,
  neither the usual orange nor the red of warnings), right after the
  wave's own line. With the defaults: wave 100 announces the hunt
  (hunters, artillery, the hunt level's step) and the hunter zone
  environment chance and cap, wave 200 the faster world rise ("The
  monsters grow stronger, faster! From now on, they climb 1 world(s)
  every 5 wave(s) instead of 1 every 25."), wave 300 the dark urge and
  its chance. The waves come from the settings, not fixed numbers:
  `Milestones.StartingAt` compares the wave with `First hunter wave`,
  `Faster world rise from wave` and `First dark urge wave`, and skips a
  rule that's off (chance 0, faster rise from wave 0). The values in the
  text are the current settings. It's sent only when a wave starts, not when one is resumed
  after a reload; `laststand_wave 200` sends it. Log: `milestone: wave N
  announces …`.
- **Merged waves:** merging starts at the wave where one player's normal
  count reaches 100 (`Merged wave size`): `6 + 2 × (wave − 1) ≥ 100`, wave
  48 with the defaults. Change the wave 1 count or the growth and the start
  wave moves with them (`MergeRule.StartWave`; the log says `merge: from
  wave N, waves hold at 100 …` when the arena is ready). From then on a
  wave with more than 100 regular monsters and hunters spawns only 100,
  whatever the player count, and they carry the strength of the whole
  count: health and damage × count / 100, on top of the world scaling
  (multiplied, through `MergeRule.RaiseBonus`, so the game's additive stat
  bonuses don't dilute it). Wave 48 solo is exactly 100 (×1),
  for two players 150 → ×1.5; wave 50 solo is a surge of 208 → ×2.08; wave
  131 solo is 266 → ×2.66; wave 135 for two players is a surge of 822 →
  ×8.22. Before the start wave nothing merges, even when co-op or a surge
  goes past 100. The hunter share applies to the 100. Bonus gold and dream
  dust per kill of these monsters are multiplied the same way
  (`LootService`), so a merged wave pays what the full count would have;
  vanilla kill gold isn't. Minibosses, Pain minibosses and heroic bosses
  aren't merged and don't get the merge multiplier. Instead, once waves
  merge, minibosses and Pain minibosses have a health floor: at least 5×
  the max health of the toughest regular monster or hunter in the wave
  (`Merged waves: miniboss health`). Without it, ×8 monsters would leave
  them weaker than the mobs around them. Heroic bosses have no floor any
  more (20× until schema 53): their world, like every monster's raised by
  the heroic fast kill, sets their health (see [Heroic bosses](heroic-bosses.md)). `BossHealthFloor`
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
  lives, and the wave's break is the 40 s boss break. Curse kill goals count the full
  wave, so they keep growing (see [Curses](../shrines/curses.md)). The chat line for
  the wave says `They fight with the strength of N: xM health and damage
  each.` and the `wave N:` log line shows `merged=N/xM` (`merged=N/x1` when
  nothing merged). The rule is `MergeRule`, plain logic used by
  `WaveSchedule`.
- **Strength, by world:** monster stats follow the game's own world (zone)
  scaling, evaluated at a higher world than the run's, a little more each
  wave. The run's real world (`ZoneManager.currentZoneIndex`, set once by
  [Starting world](../start/starting-world.md)) never changes, so prices,
  loot, gold, XP and Mirage tiers stay at the run's world; only monster
  health and damage climb. `WorldRule.ExtraWorlds(wave)` gives the extra
  worlds:

  `Worlds per step × (min(wave, F) − 1) / Waves per step` +
  `Faster: worlds per step × (wave − F) / Faster: waves per step`,

  where F is `Faster world rise from wave` (the second part counts only
  from F on; 0 turns it off). It's smooth, not a jump every step: with the
  defaults (1 world per 25 waves, then from wave 200 1 world per 5 waves (2 before schema 56);
  `Waves per step` was 10 in v0.12.0, schema 54) wave 2 is 0.04 worlds up
  and wave 26 exactly 1.

  | Wave | 1 | 26 | 51 | 101 | 150 | 200 | 210 | 220 | 250 | 300 |
  |---|---|---|---|---|---|---|---|---|---|---|
  | Monsters fight like world (from world 1) | 1 | 2 | 3 | 5 | 7 | 9 | 11 | 13 | 19 | 29 |

  What a world means, from the game's curves (Nightmare, vs world 1):

  | World | Regular health | Regular damage | Boss health |
  |---|---|---|---|
  | 2 | ×1.7 | ×1.6 | ×1.7 |
  | 3 | ×3.3 | ×3.2 | ×3.1 |
  | 5 | ×9.5 | ×9.2 | ×9.6 |
  | 8 | ×29 | ×27 | ×36 |
  | 10 | ×51 | ×48 | ×74 |
  | 15 | ×144 | ×104 | ×273 |
  | 20 | ×297 | ×159 | ×678 |
  | 30 | ×751 | ×270 | ×2347 |
  | 50 | ×1774 | ×492 | ×6457 |
  | 100 | ×4332 | ×1046 | ×16731 |

  The curves are cubic, then linear: damage goes linear past scaled index
  8, regular health past 20 and boss health past 30. So one world more is
  ×1.7 to ×2.0 early, about ×1.3 around world 10, ×1.1 around world 20 to
  25 and ×1.03 around world 50. This is why the rise speeds up at wave 200:
  at 1 world per 25 waves, late waves would barely get stronger. At 1 world
  per 5 waves from world 9, each wave adds about +5% health and +4.5%
  damage at first, and about +2% and +1.5% by world 20 (wave about 255).
  Players who survive keep going; the climb outpaces them slowly.

  How it's applied (`WorldScaling`, the game adapter): the game already
  gives every monster its real world's scaling when it spawns. For each
  of Last Stand's spawns, `WaveDirector.Enhance` asks the game's curve for
  that monster's kind at the real world and at the real world plus the
  extra worlds (`GameManager.GetRegular/MiniBoss/BossMonsterHealthMultiplierByScaling`
  and the matching damage functions; minibosses and Pain minibosses use
  the miniboss curve, heroic bosses the boss curve, everything else the
  regular one), and adds the difference as one server-side `StatBonus` of
  `(raised ÷ real − 1) × 100%` to max health and to attack damage and
  ability power. The game multiplies percentage stat bonuses together
  (`BonusStats` `+` uses `DewMath.MultiplyPercentageBonuses`), so the
  monster ends up exactly at the raised world's scaling, and clients see
  it through the synced stats. Merged-wave strength multiplies mobs on
  top (`MergeRule.RaiseBonus`).

  **Fast-kill raise on top.** A heroic fast kill adds a fixed number of
  worlds for every monster, mobs to heroic bosses, from the next wave on
  (see [Heroic bosses](heroic-bosses.md) and [Fast kill follows the
  players](pace-by-player.md)). Each wave the raise for the players here
  (`WavePlan.RaisedWorlds`) is added to the scheduled extra worlds for
  every tier in `WaveDirector.Enhance`, so the monster world is the real
  world + the schedule's extra worlds + the raise. Like the schedule, it
  changes stats only.

  Chat's wave line ends with "Difficulty: X.X", the monster world, and the
  `wave N:` log line shows `world=X raised=+R`, where `world=` includes the
  raise R (`+0` before any fast kill). It counts from the run's real
  world, so with `Starting world` 5, wave 26 is world 6.

  This replaced, in schema 53, a flat bonus of +10% health and damage per
  wave (+50% from wave 150), the empowered waves (×1.5 every 10 waves from
  200) and fast clear (×2 health and damage on every later wave when 80%
  of a wave died within 1 s of its first hit).
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
