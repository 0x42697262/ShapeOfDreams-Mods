# Heroes

On the host, in camp and arena alike, `HeroBoost` checks every hero every
0.5 s and `Revives` every frame (a handful of heroes, so both are cheap).

## Player level

`Player level` (default 20) raises every hero to that level, capped at the
hero's own `Hero.maxLevel` (`ges.maxHeroLevel`, 20, or more once a Chaos
reward's level-cap bonus sets `maxLevelOverride`). It only raises: a hero
already above it keeps their level. 0 turns it off. `Status.level` is server
state, so clients see it. Log: `heroes: <hero> raised to level N.` It
replaced the on/off `Max player level` (schema 2), which could only raise
heroes to the cap.

## Memory floor

`Starting memory level` (default 10; 1 turns it off) raises every equipped
memory, identity and movement included, to at least that level by setting
`SkillTrigger.level`, as the Upgrade Well does. A memory the game marks not
upgradable (`isLevelUpEnabled` false) is left alone and logged once. After a
restart, the recreated starting memories are raised the same way.

## Health multiplier

`Hero health multiplier` (default 100, at least 1) multiplies every hero's
max health, for longer fights that reward play over the first burst. Health
is calculated on the server (`EntityStatus.CalculateStats`) and replicated as
the `finalStats` SyncVar, so unmodded clients and their health bars see it.

The multiplier is a `finalStatsProcessors` entry, which runs at the end of
the stat pipeline, after level scaling and every item bonus, so it scales the
finished total. A percentage stat bonus wouldn't: bonus percentages combine
with the player's own, so the mod's share would dilute what the player built.
Each hero gets the processor once. It's removed when the multiplier changes,
when the session stops, and on unload. `CalculateStats` keeps the
current/max health fraction, so current health scales with the maximum.

## Revives

Knocked-out heroes never recover on their own, and the run never ends, so
without revives a free-for-all stops after one death each. `Revives` notes
when each hero went down and, after `Revive after (seconds)` (default 10; 0
turns it off), calls `Se_HeroKnockedOut.Revive(ratio)` with `Revive health
(%)` (default 100). `arena_revive` revives everyone at once.

## Rescue quests

A second after a hero goes down, `Se_HeroKnockedOut` starts a
`Quest_LostSoul` and drops a `RoomMod_HeroSoul` modifier on a node. With
constant knock-outs that's a stream of rescue quests, plus modifiers pushed
onto nodes whose modifiers were stripped. With `No rescue quests` on,
`Revives` sets the effect's public `disableQuest` on the first tick, which
stops both.
