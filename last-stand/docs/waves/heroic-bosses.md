# Heroic bosses

"Heroic boss" is the game's name for a zone boss (`BossMonster`). Last Stand
brings them in as the last enemy of a wave: at waves 49 and 99, then, from
wave 100 (`Heroic bosses more often from wave`), every 25 waves counted on
from 99 (`Heroic boss every N waves, later`): 124, 149, 174, 199…

| Wave | 49 | 99 | 124 | 149 | 174 | 199 | 249 | 499 |
|---|---|---|---|---|---|---|---|---|
| Heroic bosses | 1 | 2 | 2 | 3 | 3 | 4 | 5 | 10 |

The count is `1 + (wave − 49) / 50` (`Heroic boss step`), capped at 10, so it
still grows every 50 waves; the shorter interval only adds boss waves in
between. The schedule is a `ShiftingCadence`: the early cadence up to the
switch wave, then the later interval continuing from the last early boss
wave.

**Entrance.** The boss waits until every monster and miniboss of its wave has
spawned. Then, after 3 more seconds, it enters with a chat notice and plays its
own spawn animation (its `Spawn` ability). It spawns at a random spot like any
monster, heads for the nearest hero, and counts toward clearing the wave.

**Which boss.** A random one from `Heroic boss types`, by default the zone
bosses of Forest, Dark Cave, Despair (Azurak), Sky, Snow Mountain and Lava
Land. The Ink bosses are left out. They're a paired fight: White Night spawns
Dark Moon itself, and Dark Moon runs its own death logic. `Mon_Despair_BossCrawler`
doesn't exist as a boss type. A boss is skipped if it's hidden (`isHiddenBoss`) or if it, or any
class between it and `BossMonster`, overrides `OnDeath`. Such bosses run their
own death logic: `Mon_Primus_BossPrimusAeron` starts the game's ending, for
one. The log says which types were skipped, and the catalog line gives how
many are left. There is always at least one: if no box is checked, or none
of the checked types can be used, the boss is picked from the whole list of
six, and the log says so.

**Strength: vanilla boss scaling times a multiplier, faster skills.** It
spawns at the current monster level and gets the game's own boss scaling for
the [starting world](../start/starting-world.md), difficulty and player count, the same
per-wave bonus as everything else, and then ×5 health (`Heroic boss health
multiplier`) and ×8 attack damage and ability power (`Heroic boss damage
multiplier`). The game multiplies stat bonuses together
(`DewMath.MultiplyPercentageBonuses`), so a `+(M − 1) × 100%` bonus is
exactly ×M whatever else the boss has.

Health is `base × world × difficulty × (1 + 0.85 × (players − 1)) × wave ×
multiplier`. Base health (prefabs; bosses gain nothing per level): Belphomet
8,000, Seeker 8,800, Skoll 9,200, Infernus 9,600, Nyx 10,400, Azurak
13,200. World 4 is ×3.96 health on Normal, ×6.19 on Hard and ×7.04 on
Nightmare (difficulty's world factor and its flat health bonus), and wave 49
is ×5.8. So wave 49, Nightmare, world 4, solo, with the default ×5: Belphomet
1.63M, Infernus 1.96M, Azurak 2.70M, about 2M on average. Each extra player
adds 85% of that (the game's `bossBonusHealthPercentagePerPlayer`): Infernus
3.6M with 2, 5.3M with 3, 7.0M with 4. The wave factor is the same per-wave
bonus as every monster's, +10% a wave and +50% from wave 150: ×10.8 at 99,
×13.3 at 124, ×15.8 at 149, ×16.3 at 150, ×28.3 at 174, ×40.8 at 199.
Measured at wave 124: Skoll 4,306,783 and Nyx 4,868,537, both exactly
468.13 × base, as the formula gives.

Damage is the ability's AD/AP ratio × the boss's attack damage or ability
power. Players add only the game's +11% per extra player. Nightmare world 4
at wave 49 is ×43.9 on that (×5.74 world, +32% Nightmare, ×5.8 wave), times
the multiplier. Reading the prefab ratios undercounts: at wave 124 with ×8,
Skoll's `AuraBlade` hit for 302k (8.3 × its ability power) and Nyx's
`PillarOfStars` for 161k (4.3 ×), about 132k and 70k at wave 49.

The log measures both: `heroic boss: wave N <type> maxHealth=… attackDamage=…`
when the multiplier is applied, and `biggest hit on a hero …` (after armor,
shields included) with the ability's type when the boss dies.
It also gets ability haste, so it uses its patterns more often: +200 at
wave 49 (`Heroic boss ability haste`), then +50 more on each later boss wave
(`Heroic boss haste per later boss wave`): 250 at 99, 300 at 124, 350 at
149, 450 at 199. Boss waves come twice as often after wave 100, so haste
grows twice as fast there too.
The wave's haste is worked out with the rest of the wave plan and shows in the
log as `heroicHaste=`. The game turns haste into cooldown
(`AbilityTrigger.GetCooldownTimeMultiplierByAbilityHaste`) as
`1 / (1 + haste / 100)`, down to its own floor. This applies to every boss
ability that allows cooldown reduction. Haste is a normal synced stat. That's applied one frame after the
spawn, once the game's difficulty bonus is in, and it keeps the boss at full
health. 50× was tried first: a wave 49 boss had about 23 million health and
lost only a quarter of it in five minutes of nonstop fighting. 10× damage was
also too much.

**Full kit.** It's the real `BossMonster` with its own AI: the boss AI always
targets the nearest hero, picks its ability pool (the hard pool where the zone
says so), and runs its patterns, combos, phase changes at health thresholds,
and the helpers it spawns itself: Dark Cave's hallucinations, Azurak's
rolling pillars, Infernus's pillars, Nyx's phases. None of the default bosses
needs objects from its own boss room. The entrance plays the boss's own spawn
ability, which for some bosses also sets up its first skills.

**Rewards never depend on chance.** Each boss drops 3 Corrupted Chaos
shrines, the same `Shrine_CorruptedChaos` Pain minibosses leave. They have no
rarity, so the chaos reroll never touches them. A heroic boss is recognized even after the mod reloads mid-fight (auto-reload
in developer mode): any supported `BossMonster` standing in the arena counts
as Last Stand's, because a plain arena never spawns its own boss. In testing a
reload during the wave 49 fight lost the record of the boss, and it died
through the game's own boss flow with no rewards. A boss spawned by another
boss (Erebos) still drops nothing.

**Its helpers die with it.** In the stock game the boss's death kills every
other monster, and that's what clears away its pillars and hallucinations.
Last Stand skips that kill-all so the wave isn't wiped. Instead, when a heroic
boss dies, every monster descended from it (`Actor.IsDescendantOf`) is killed.
A boss that a heroic boss spawns, such as Erebos from Nyx under The Consort of
Night, counts as Last Stand's too. Its death skips the vanilla boss-kill flow,
and it drops no extra reward.

**None of the boss-room machinery.** Boss rooms spawn their boss through the
entry cutscene (`DewBossRoomEntry`), which the arena never fires. Last Stand
spawns it directly with `Dew.SpawnEntity`. When it dies, a Harmony prefix on
`BossMonster.OnDeath` runs only the base `Entity.OnDeath` for bosses Last Stand
spawned. That skips everything the game does on a boss kill:

- pausing the game timer, which only a room load would undo;
- killing every other monster, which would end the wave early;
- locking the rifts, the boss soul shrine (`Shrine_BossSoul`) and the mid-run
  save;
- the knocked-out revive.

`skipBossSoulFlow` is set too, and the big kill feedback
(`playBossKillFeedback`) is turned off on the server. No rift opens, because
the arena is loaded already cleared.

**Mid-join.** `BossMonster.OnCreate` sets the synced
`GameSettingsManager.midJoinWaitType` to `FightingBoss`, and
`OnDestroyActor` of the last boss sets `AfterBossFight`. While it isn't
`None`, `DewPlayer.LogicUpdate_MidJoin` holds a joining player on "The
expedition is preparing to move after fighting a Heroic Boss". Only travel
to a non-boss node sets it back to `None`, and the arena never travels, so
after the first heroic boss nobody could join for the rest of the run.
Writing the old value back after `Dew.SpawnEntity` didn't help: on the host,
`OnCreate` runs later, in Mirror's spawn flow. `MidJoinGate` checks the flag
once a second during the fight and clears the three boss values
(`BeforeBossFight` to `AfterBossFight`), leaving `HostLoadingGame` alone. A
player already waiting is let in on the next `LogicUpdate_MidJoin`.

**Reward.** 3 `Shrine_CorruptedChaos` (`Heroic boss Corrupted Chaos
rewards`). Each is one shrine that every player picks from separately, as with
Pain. An earlier version dropped 5 legendary `Shrine_Chaos` instead. The boss
also drops 1000 dream dust and 200 bonus gold, and rolls the item table at ×2
like a Pain miniboss.

Still drawn by every game: the boss health bar, and the kill counting toward
each player's heroic boss stats.
