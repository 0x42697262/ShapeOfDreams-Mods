# Heroic bosses

"Heroic boss" is the game's name for a zone boss (`BossMonster`). Last Stand
brings them in as the last enemy of a wave: from wave 25 (`First heroic boss
wave` 25), every 25 waves (`Heroic boss every N waves` 25): 25, 50, 75,
100, 125…

| Wave | 25 | 50 | 75 | 100 | 125 | 150 | 200 | 250 |
|---|---|---|---|---|---|---|---|---|
| Heroic bosses | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 |

Each boss wave brings one more boss than the last (`Extra heroic bosses
per boss wave`, 1): the count is `1 + occurrence × extra`, where
`occurrence` is the boss wave's index (0 for wave 25), capped at 10
(`Max heroic bosses per wave`) from wave 250. The schedule is a
`ShiftingCadence`: the early cadence up to the switch wave (`Heroic bosses
more often from wave`, 100), then the later interval (`Heroic boss every N
waves, later`, 25) continuing from the last early boss wave, and its
`Occurrence` counts boss waves across both. With both intervals at 25 the
switch changes nothing; it's there for a host who wants a slow start
(until schema 53 the defaults were 50, 100, then every 25).

**Entrance.** The boss waits until every monster and miniboss of its wave has
spawned. Then, after 3 more seconds, it enters with a chat notice and plays its
own spawn animation (its `Spawn` ability). It spawns at a random spot like any
monster, heads for the nearest hero, and counts toward clearing the wave.

**Mid-wave entrances from wave 125.** From `Heroic bosses enter mid-wave
from wave` (125), each heroic boss gets a random entrance time when the
wave starts: 0 to `Heroic boss entrance window (s)` (60) seconds after the
wave's first spawn, rolled separately per boss. When its time comes it
enters with the usual chat notice and spawn animation, while the wave's
monsters are still arriving. If the monsters all finish spawning before a
boss's time, that boss enters after the last one instead, 3 s later, as
before wave 125. So a boss always enters between the wave's first and last
spawn (or right after the last). A window of 0 keeps every boss last.

Why seconds and not a share of the wave: how long spawning takes isn't
known when the wave starts, since the alive cap (35) makes new monsters
wait for kills. A fast party finishes spawning sooner, so more bosses fall
through to the end; a slow one meets them mid-wave.

`WaveSchedule.HeroicBossEntranceSeconds` rolls the times (one roll per
boss, passed in by the director, sorted), and `SpawnQueue` holds them: it
notes the time of the first release and lets each boss out once its offset
has passed, outside the alive cap, like the finale. When the last monster
has been released, any boss still waiting joins the finale. The log's
`wave N:` line shows `heroicWindow=60s` (0 before wave 125), and the chat
wave line says "N heroic boss(es) at any moment".

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

**Strength: the wave's world plus a heroic raise, faster skills.** It
spawns at the current monster level and gets the game's own boss scaling
for the run's world, difficulty and player count. Then, like every other
monster, it gets the difference up to a higher world (see [Waves](waves.md),
*Strength, by world*), on the game's boss curve: the wave's world plus
the heroic raise from fast kills below (0 until the party kills them too
fast). So its health and damage are exactly a vanilla heroic boss's at
that world. There are no flat boss multipliers any more (×5 health and ×8
damage until schema 53), and no merged-wave health floor (20× the
toughest monster until schema 53).

With the defaults, from world 1 and before any fast kill: wave 25 fights
at about world 2, 50 at 3, 100 at 5, 150 at 7, 200 at 9 and 250 at 34.
Against a world 1 boss that's about ×1.7 health at wave 25, ×9.6 at 100
and ×74 at world 10 (boss health table in [Waves](waves.md)). Base health
(prefabs; bosses gain nothing per level): Belphomet 8,000, Seeker 8,800,
Skoll 9,200, Infernus 9,600, Nyx 10,400, Azurak 13,200. Each extra player
adds 85% of the boss's health (the game's
`bossBonusHealthPercentagePerPlayer`) and +11% damage. The numbers measured
before schema 53 (about 2M health at wave 50 on Nightmare world 4) came
from the ×5 multiplier and the old per-wave bonus and no longer apply.

The log gives the real stats: `heroic boss: wave N <type> maxHealth=…
attackDamage=…` one frame after the spawn, and `biggest hit on a hero …`
(after armor, shields included) with the ability's type when the boss
dies. The chat wave line says "heroic bosses like world Y.Y", and the
`wave N:` log line shows `heroicWorld=`.
It also gets ability haste, so it uses its patterns more often: +200 at
wave 25 (`Heroic boss ability haste`), then +50 more on each later boss wave
(`Heroic boss haste per later boss wave`): 250 at 50, 300 at 75, 350 at
100, 550 at 200.
The wave's haste is worked out with the rest of the wave plan and shows in the
log as `heroicHaste=`. The game turns haste into cooldown
(`AbilityTrigger.GetCooldownTimeMultiplierByAbilityHaste`) as
`1 / (1 + haste / 100)`, down to its own floor. This applies to every boss
ability that allows cooldown reduction. Haste is a normal synced stat. It's
applied one frame after the spawn, once the game's difficulty bonus is in.

**Fast kills raise the next boss.** Heroic bosses have a check of their
own, `Heroic fast kill: seconds for all bosses`, 90 by default: killing
all of a wave's heroic bosses together shouldn't take less than 90
seconds of the party's best damage. Mobs aren't watched any more (fast
clear was removed in schema 53); they just follow the wave's world. A
host whose party wants to one-shot bosses sets it to 0 (off). It was 0 by
default until schema 45, 15 until schema 48, and counted per boss until
schema 53.

It measures the party's best burst, not the fight's length. A timer from
the first hit to the death can be beaten by stalling (hit once, wait,
then burst) and runs on through a boss's immune phases and its walks out
of reach. Instead, every damage event on a heroic boss
(`EntityEvent_OnTakeDamage`, which `EnemyRegistry` hooks and reports as
`EnemyRegistry.Hit`) adds its amount to a sliding window of `Heroic fast
kill: burst window (s)` (5). `HeroicFastKillWatch` keeps the heaviest
window. In `Actor.cs` the event's `damage.amount` is the health removed
plus what a shield absorbed, and a hit blocked by damage immunity fires
`EntityEvent_OnDamageNegatedByImmunity` and returns before it, so immune
time adds nothing. Each hit counts at most the boss's max health, so one
huge overkill hit can't count for more than a whole boss.

When the wave is cleared, the best window becomes a projected kill time:
`window × sum of the wave's heroic bosses' max health ÷ best window
damage`, the seconds all of them together would last at that rate. Bosses
with very different health count by their real health, with no average.
A fight shorter than the window uses its own length (first heroic hit to
last), so a 2 s kill reads as 2 s, not 5. If the projection is under the
target, the step is `target ÷ projection`, at most `Heroic fast kill:
largest step` (×100; ×50 until schema 53, ×10 before schema 51): at 90, a
30 s projection gives ×3. Steps compound (×3 then ×2 is ×6), a projection
at or over the target changes nothing, so it settles at the target, and
it goes back down only when a player who earned it leaves ([Fast kill
follows the players](pace-by-player.md)). Holding back doesn't help: the
only way to keep the burst low is to deal low damage the whole fight.

**The multiplier becomes a world.** The step product M is a health
multiplier for later heroic bosses, but it's applied as a higher world,
not a flat bonus. Each wave, when the plan is made, `WaveDirector`
asks `WorldScaling.ExtraWorldsForBossHealth` for the world where the
game's boss health curve is M times the curve at the wave's world: it
doubles a search span from the wave's world until the curve passes the
target, then bisects 40 times. The heroic bosses spawn at that world
(`WavePlan.HeroicBossWorldRaise` on top of the wave's extra worlds), so
their damage rises by the game's own boss damage curve too, which grows
more slowly than health late. M stays relative to the wave's world, so
the same ×3 is about 2 worlds up at world 3, 4 at world 10 and about 60
at world 50, where the boss curve is linear. Mobs aren't raised.

Only Last Stand's own heroic spawns count: the boss's helpers and a boss
it summons (Erebos) aren't heroic tier. It's measured at the wave's clear
rather than at the boss's death, so the killing hit is always in. A
skipped wave and a wave resumed after a mod reload aren't measured; the
record behind the multiplier is saved with the wave progress, so a reload
keeps it. With the setting back at 0 the earned multiplier stops applying.

Logs: `heroic fast kill: wave 50's heroic bosses took 41,203,118 damage in
their best 5 s, all of them dead in 2.1 s at that rate (target 90 s); this
step x42.86 (largest x100), heroic bosses now have x42.86 health.`, or the
same with `no change.`; every `wave N:` line shows `heroicKill=xM`
and `heroicWorld=`, above `world=` once M is over 1. Chat says it in red:
"Your best burst would kill all of this wave's heroic bosses in Xs, faster
than 90s. Heroic bosses now return from a stronger world, with xM health."

**Full kit.** It's the real `BossMonster` with its own AI: the boss AI always
targets the nearest hero, picks its ability pool (the hard pool where the zone
says so), and runs its patterns, combos, phase changes at health thresholds,
and the helpers it spawns itself: Dark Cave's hallucinations, Azurak's
rolling pillars, Infernus's pillars, Nyx's phases. None of the default bosses
needs objects from its own boss room. The entrance plays the boss's own spawn
ability, which for some bosses also sets up its first skills.

**Rewards never depend on chance.** Each boss drops 2 Corrupted Chaos
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

**Reward.** 2 `Shrine_CorruptedChaos` (`Heroic boss Corrupted Chaos
rewards`; 3 until schema 53). Each is one shrine that every player picks from separately, as with
Pain. An earlier version dropped 5 legendary `Shrine_Chaos` instead. The boss
also drops 1000 dream dust and 200 bonus gold, and rolls the item table at ×2
like a Pain miniboss.

**Stardust** (`Heroic boss stardust`, on). A vanilla boss kill gives stardust
from the boss soul (`Shrine_BossSoul`): `ges.stardustBossSoulAmount` (16 in
the asset) plus `difficulty.bossKillBonusStardust` (Easy 0, Normal 2, Hard 4,
Nightmare 7) plus 3 per evil lucid dream in play. Last Stand skips the boss
soul flow, so its heroic bosses gave none (the game's per-kill stardust
chance is 0 for the `Boss` monster type). Now `BossStardust` drops the same
amount at the boss with `PickupManager.DropStarDust`, the game's own call:
orbs of `Shrine_Stardust`, and each orb's `OnUse` gives its amount to every
player through `DewPlayer.GiveStardust` (a `ClientRpc` into each client's
own profile), so a vanilla client gets it too. Each heroic boss gives
what one vanilla boss kill gives, no more. The modding rules forbid granting exclusive items,
the ones you can't buy with stardust; stardust itself isn't one, and keeping
it at the game's own boss-kill amount keeps to the reason the rule gives.
Regular monsters and minibosses keep the game's tiny per-kill chance (0.1%
and 0.2% for 5 stardust; hunters drop none). The log line reads `heroic
boss: died, dropped 2 Corrupted Chaos reward(s) and N stardust.`

Still drawn by every game: the boss health bar, and the kill counting toward
each player's heroic boss stats.
