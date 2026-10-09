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

**Strength: 2× health, 2× damage, faster skills.** It spawns at the current
monster level and gets the game's own boss scaling (rebased to the
[starting world](../start/starting-world.md)) and the same per-wave bonus as everything
else. Then its health and its attack and ability damage are multiplied by 2
(`Tuning.BaselineHeroicBossHealthMultiplier` and `...DamageMultiplier`; these
are no longer settings, `Starting world` is the knob). Stat bonuses in the
game add up rather than multiply, so a true ×N adds `(N − 1) × (100% + the
bonus it already has)`.
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
(`playBossKillFeedback`) is turned off on the server. Spawning a boss marks
the lobby as "fighting a boss", which blocks mid-join. The old value is put
back right after the spawn. No rift opens, because the arena is loaded
already cleared.

**Reward.** 3 `Shrine_CorruptedChaos` (`Heroic boss Corrupted Chaos
rewards`). Each is one shrine that every player picks from separately, as with
Pain. An earlier version dropped 5 legendary `Shrine_Chaos` instead. The boss
also drops 1000 dream dust and 200 bonus gold, and rolls the item table at ×2
like a Pain miniboss.

Still drawn by every game: the boss health bar, and the kill counting toward
each player's heroic boss stats.
