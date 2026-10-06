# Last Stand

A host-only wave-survival mode for **Shape of Dreams**, for solo and co-op. The
party holds a boss arena against endless waves that get harder every wave.
Memories and essences drop from kills, scarce but better as the waves go on. A single
Dream Well in the middle of the arena turns what you collect into upgrades.

> **Status: first build, not yet tested in-game.** The code compiles and covers
> the whole loop below. [`AGENTS.md`](AGENTS.md) holds the working notes and
> the list of things that still need an in-game check.

## The pitch

- One arena, no travel. The fight happens in a boss room with the boss
  removed.
- A new wave starts **once the last one is cleared**, after a short break.
  Each wave is bigger and stronger than the last.
- Every monster gets **more health and damage every wave**.
- **Minibosses** come every 10 waves, then several at once. No real bosses
  for now.
- **Heroic bosses**, the game's real zone bosses, enter right before every
  50th wave: 49, 99, 149… One more joins every 100 waves. Each drops 3
  Corrupted Chaos rewards, the kind Pain minibosses leave.
- **Pain minibosses** change the rules while they live: every hero takes
  constant damage until they die. Killing them drops **Corrupted Chaos**
  rewards.
- **Monsters from other worlds** join every 10 waves, one type at a time,
  weakest worlds first. Once all have joined, **hunters** start coming, and
  later the arena becomes a **hunter zone** with falling artillery.
- **Mirage skins** appear from wave 30, weakest first, a stronger one every
  10 waves.
- **Loot is scarce but improves.** Monsters drop at most one memory or
  essence per player per wave, by chance. Minibosses roll their own chance.
  Rare, epic and legendary drops get more likely every wave, chaos rewards
  included, reaching no commons at all by wave 100 (wave 50 for chaos).
- **No goal.** The run's "Shape of Dreams" goal is removed, since there is
  nowhere to go.
- **One Dream Well**, unlimited uses, at the center of the arena.
- **The lobby stays vanilla.** Lucid dreams, Corrupted Chaos and every other
  game modifier can be picked as normal, unlike Dream Arena, which strips them.
- **Host only.** Friends join with an unmodded game.

The run ends the way a normal run does, when the whole party is down. The score
is the wave you reached.

## Picking the mode

Last Stand is a game mode next to Shape of Dreams and Limbo, in both places the
game offers one:

- **Solo:** the Singleplayer button opens the game mode list (Shape of Dreams, Limbo). A
  **Last Stand** button is added under Limbo. It is a clone of the Limbo button,
  so it looks and navigates the same, with its click replaced to start a solo
  run in Last Stand. If a build ships without Limbo, the game normally skips
  that list and starts Shape of Dreams at once. In that case the list is shown
  anyway, with Limbo hidden, so Last Stand can still be picked.
- **Multiplayer:** Create Lobby has a Game Mode dropdown. **Last Stand** is
  added as a third option, with its own description.

### How the choice is remembered

Choosing Last Stand sets the run's `customGameSettingsSaveKey` to
`laststand`, the same mechanism Limbo uses with `limbo`. That one value is the
mode's **single source of truth**:

- `GameSettingsManager` copies it into `gameSettingsSaveKey`, a SyncVar, when
  the session starts. The mod asks that field whether Last Stand is on. No
  separate "enabled" flag exists to drift out of sync.
- `DewNetworkManager.RestartAsync` copies it back into the start settings, so
  a restart after game over stays in Last Stand.
- Lobby preferences (difficulty, lucid dreams, banned items) are saved per key,
  so Last Stand's lobby setup doesn't overwrite a normal run's.

## Arena

The run opens in the normal camp (node 0, `WorldNodeType.Start`). Every other
node is redirected to one boss room with `ZoneManager.SetRoomOverride`, applied
right after `GenerateWorld_Imp` fills the node list. Taking the rift out of camp
starts the fight. The override writes into the synced node list, so vanilla
clients load the same room.

### Leaving camp

The camp rift goes straight to the arena: one "move to the next location"
prompt, no world map and no node to pick. `Rift_RoomExit` checks its
`nextNodeIndex` before opening the map, and that field is a SyncVar, so the
host points it at an arena node (one connected to camp, skipping side nodes)
and vanilla clients get the same single prompt. This is retried once a second
while in camp, until the rift exists.

In multiplayer, a confirmed travel normally still goes to a vote, which opens
the world map for everyone with a 6-second countdown. The host decides that
in `ZoneManager.ShouldVoteOnTravel()`, which is forced to false in Last Stand,
so the party travels at once. The synced `GameSettingsManager.enableVotes` is
left alone on purpose: the lobby saves it as the host's preference, so
clearing it would carry into normal runs.

Both are taken from Dream Arena.

### The arena room

The arena loads already cleared: `Room.isRevisit` (a SyncVar) is set in
`Room.OnStartServer`, before clients receive the room. The room is lit, its
barriers are open, and its own monsters, props, rewards and boss are skipped.
The entry trigger (`DewAllHeroesPresentZone`) is switched off and intro
cutscenes are skipped. The exit rift stays shut, so there is nowhere to travel.

**A plain arena.** World generation rolls room modifiers onto every node, and
the arena node keeps them. In testing, the arena arrived with
`RoomMod_SpawnGuidance` (an extra shrine standing in the arena),
`RoomMod_StrayMemory_StartProp` and `RoomMod_HarderFightBetterReward`. The game
also adds modifiers later: Corrupted Chaos's Aura of Pain, a miniboss on
travel (`RoomMod_SpawnMiniBoss`), and the hunters' spread. So, with
`Plain arena` on (the default):

- after `GenerateWorld_Imp`, every arena node's modifiers are removed with
  `ZoneManager.RemoveModifier`, which writes the node back so it syncs;
- a prefix on `ZoneManager.AddModifier` refuses any later modifier on an arena
  node. Two kinds still get through. One is Last Stand's own Pain environment
  and hunter zone, which are added through `ModifierGate.AddOwn`. The other is
  `RoomMod_HeroSoul`, which the game adds when a hero is knocked out and which
  co-op revives depend on.

The camp keeps its modifiers.

**Knocked-out players come back when the wave is cleared.** In the stock game a
knocked-out hero's soul, `RoomMod_HeroSoul`, goes on another node a few steps
away (`lostSoulDistance`), and the others revive them by traveling there. The
arena's rift is shut, so Last Stand has no souls at all. A prefix on
`Se_HeroKnockedOut.CheckAndAddHeroSoul` (which runs on the knock-out and on
every room load) skips the game's placement, sets the knock-out's
`disableQuest` so no "lost soul" quest starts, and removes any soul of that
hero left on any node. When a wave is cleared, every knocked-out hero is
revived through the game's own `Se_HeroKnockedOut.Revive` (half health, the
game's default), as long as at least one hero is still standing, so they
have the break to get ready for the next wave. If everyone
is down, the game's own game over follows as usual. Chat announces the revive.

**The world map shows only the camp and the arena.** The map (M) draws every
node except sidetrack nodes, and `WorldNodeData.IsSidetrackNode` is just
`position.x < 0`. So once the camp rift has its arena node, and again on
arrival, every other node's map position is mirrored to a negative x
(`-1 - x`) and written back to the synced `ZoneManager.nodes`. Clients' maps
then skip those nodes. Nothing else needs them: the arena never travels on,
and the game only uses the sidetrack test for hunter progress and for picking
quest nodes.

**Random boss music (host only).** Each time the arena loads, the host hears a
random zone boss fight track: Forest, Dark Cave, Despair, Sky, Snow Mountain,
Lava Land, both Ink tracks, or Primus. It never repeats the previous pick.
Normally a boss room's `BossMusicStart` effect (an `FxChangeMusic` in the
scene) plays the track when the boss section's combat starts. The arena loads
cleared, so that never happens, and the room plays its zone's normal music
from `Room.OnLateStart`. A postfix there switches the host's `MusicManager` to
the chosen track. Music items are linked by GUID only and aren't in the
resource database's name maps (a lookup by name found nothing in testing). So
`GameNames` lists the nine tracks' GUIDs, read from the boss room scenes, and
`DewResources.GetByGuid` loads them through Addressables, where all nine are
in the catalog.

Other players hear their usual music. Music is local to each game: every
client picks its own in `Room.OnLateStart` from the room and zone, and no
synced state or networked effect can name a track. The only networked music
effects are scene objects (`BossMusicStart`) that exist only in their own boss
room, and Mora's Domain's shrine. Setting: `Random boss music`, default on.

The arena's center is `Room_BossArena` when the room has one, otherwise the
`pathablePivot` of the largest `RoomSection`. On arrival, players are placed in
a ring around it and the **Dream Well** is placed at the center. Arrival
retries on a timer until the room and heroes are ready.

**Enemies spawn at random spots across the whole room**, antechamber included,
so they come from every side. Each spawn:

- picks one of the room's `RoomSection`s, weighted by its `area`, so a big
  section gets more spawns than a small one;
- asks that section for a point with the game's own
  `Section_Monsters.GetSpawnPositionInSection`, which samples random points in
  the section and keeps one that `RoomMonsters.FilterSpawnPosition` accepts as
  on the walkable floor (navmesh) and reachable. It's how the game places its
  own section spawns;
- rejects points closer than 8 m to any living hero for the first 10 tries,
  so nothing spawns on top of someone. After that the game takes any valid
  point, so a spawn never fails just because the heroes are spread out.

The game sends each new monster toward the nearest hero, so monsters far away
walk in.

## Waves

The first wave starts 15 s after arrival. A wave is **cleared** when everything
in it has spawned and died. The next wave starts 5 s after that, so the pace is
set by how fast the party kills, not by a timer. The score is the wave reached.

| From wave | What changes | Every |
|---|---|---|
| 1 | Regular monsters, 6 growing by 2 a wave | wave |
| 5 | Surge: twice the regular monsters, hunters and alive cap | 5 waves |
| 2 | +10% health and +10% damage on every monster, stacking | wave |
| 10 | A monster type from another world joins the pool | 10 waves |
| 10 | Minibosses (1, 1, 2, 2… up to 10) | 10 waves |
| 20 | Pain minibosses (1, 1, 2, 2… up to 3) | 20 waves |
| 30 | The next stronger Mirage skin is unlocked | 10 waves |
| 49 | Heroic boss, last in the wave (1 at 49 and 99, 2 at 149 and 199, 3 at 249…, up to 10) | 50 waves |
| all types seen + 5 | Hunters, one more each time (up to 10) | 5 waves |
| first hunter wave + 10 | Hunter zone: falling artillery for the wave (hunt level 1, 2, 3) | 10 waves |

The waves for hunters depend on how many monster types the other worlds add,
which is only known at runtime. The log reports it when the arena is ready:
`catalog: N monster type(s) from other worlds, M Mirage skin(s); hunters start
at wave W`. With 5 new types, the last joins at wave 50, hunters start at
wave 55 and hunter zones come at 65, 75 and 85.

Each wave is planned by `WaveSchedule`, plain logic with no game calls. It
works through these knobs, all using systems the game already syncs:

- **Count:** regular monsters start at 6 and grow by 2 a wave. Each player past
  the first adds half a wave again. Every 5th wave is a **surge** that doubles
  the regular count (wave 5: 28 instead of 14) and the hunter count, and
  doubles the alive cap for that wave only, so the extra monsters are on the
  field together instead of queuing. Minibosses, Pain minibosses and heroic
  bosses are added on top and are not doubled. A multiplier below 1 shrinks
  the counts but never the cap.
- **Strength, per wave:** every spawned monster gets a stat bonus of +10% max
  health and +10% attack and ability damage for each wave after the first. So
  wave 11 is +100% health and +100% damage. This is a server-side
  `StatBonus`, so clients see it through the synced stats.
- **Strength, slowly:** the game's ambient level, `GameManager.SetAmbientLevel`,
  sets the level monsters spawn at. It rises by one every 10 waves.
- **Cap:** at most 100 monsters alive at once, whatever the player count. Spawns
  that would go over wait in a queue for the next opening, and are released
  in small batches. The host is the server, so if it lags, everyone lags.

Pain minibosses are queued first, then minibosses, then hunters, then regular
monsters, so the specials always arrive early in their wave. Heroic bosses
come last: see below.

### Monsters from other worlds

The pool starts as the run's current zone (`Zone.defaultMonsters`). Every 10
waves, one more monster type from another zone joins. The other zones are the
game's own list (`Zone_` assets, filtered by the build's content settings, as
`ZoneManager` does) ordered by `zoneTier`, so weaker worlds come first. Each new
type is a fair share of the pool: with 8 local types and 2 new ones, a regular
spawn is a new type 2 times in 10. Minibosses can be any unlocked type that
the game marks `ISpawnableAsMiniBoss`.

### Hunters and hunter zones

- **Hunters** are monsters with the game's `Se_HunterBuff`, the same buff the
  game's own hunter spawns get: faster, shadow-walking, no gold or exp. They
  start 5 waves after the last new monster type joins. The first hunter wave
  has 1, and one more joins every 5 waves after that, up to 10.
- **Hunter zones** start 10 waves after the first hunter wave and repeat every
  10 waves. For that wave, the arena gets the game's `RoomMod_Hunted` modifier
  through `ZoneManager.AddModifier`, which adds it to the live room and syncs it
  to clients. Its artillery, small and big meteors aimed at the heroes, comes
  from the modifier itself, not from any skill, memory or essence. It also
  makes hunters tougher. The hunt level (`ZoneManager.currentHuntLevel`) is set
  to 1, 2 and then 3 for successive zones, which scales the artillery. When the
  wave is cleared the modifier is removed and the old hunt level is restored.

### Mirage skins

From wave 30, regular monsters and hunters have a 20% chance to spawn with a
Mirage skin (`MirageSkinEffect`), the game's own variant that adds a shield,
more health and special attacks. The first skin unlocked is Delusion, the one
the game uses in its early zones. The rest follow by the game's skin `tier`,
weakest first, one more every 10 waves. A monster that already has a skin,
for example from the vanilla Mirage Skin modifier, is left alone.

### Heroic bosses

"Heroic boss" is the game's name for a zone boss (`BossMonster`). Last Stand
brings them in as the last enemy of a wave, right before every 50th wave:

| Wave | 49 | 99 | 149 | 199 | 249 | 349 |
|---|---|---|---|---|---|---|
| Heroic bosses | 1 | 1 | 2 | 2 | 3 | 4 |

The count is `1 + (wave − 49) / 100`, capped at 10.

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
many are left. With none left, there are no heroic boss waves.

**Strength: 10× health, 3× damage, faster skills.** It spawns at the current
monster level and gets the game's own boss scaling and the same per-wave bonus
as everything else. Then its health is multiplied by 10 and its attack and
ability damage by 3 (settings `Heroic boss health multiplier` and
`Heroic boss damage multiplier`). Stat bonuses in the game add up rather than
multiply, so a true ×N adds `(N − 1) × (100% + the bonus it already has)`.
It also gets ability haste, so it uses its patterns more often: +200 at
wave 49 (`Heroic boss ability haste`), then +50 more on each later boss wave
(`Heroic boss haste per later boss wave`): 250 at 99, 300 at 149, 350 at 199.
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

### How monsters are spawned

Through the room's own spawner, `RoomMonsters`, so everything that hooks into
it still applies: room modifiers, Corrupted Chaos, the vanilla Mirage Skin
modifier and the like.

- **Regular and hunter:** `RoomMonsters.SpawnMonsters` with a one-monster rule
  per monster type, marked `isBossSpawn` so the game spawns exactly one and
  doesn't add waves, random hunters or wait on population. The wave's stat
  bonus, the hunter buff and any Mirage skin are added right after it spawns.
- **Miniboss:** `RoomMonsters.SpawnMiniBoss`, with the monster picked from the
  unlocked `ISpawnableAsMiniBoss` types and a random `MiniBossEffect`. This is
  the game's own promotion: bigger, tougher, unstoppable, and it drops the
  stock miniboss chaos reward on death. On top of that, one frame after it
  spawns, its health and its attack and ability damage are multiplied by 2.5
  (`Miniboss health multiplier` and `Miniboss damage multiplier`), the same
  true ×N as heroic bosses: `(N − 1) × (100% + the bonus it already has)`. The
  game's stock miniboss scaling and the wave bonus alone left them weak, and
  5× was too strong. Pain minibosses don't get this multiplier.

## Pain minibosses

These are modeled on the game's own **Aura of Pain** (`RoomMod_CorruptedChaos_AuraOfPain`
in `Dew.Contents`), part of the Corrupted Chaos modifier. In the stock game:

- Two minibosses spawn with `Se_LimboBossDecorator`, plus +20% health and +30%
  damage.
- Every 0.1 s, every living hero takes **1% of max health + shield per
  second** as pure damage that ignores armor. It never brings a hero below 1
  HP.
- When both die, a `Shrine_CorruptedChaos` spawns, plus a second one per player
  based on their `doubleChaosChance`.

Last Stand keeps the effect but ties it to the bosses instead of the room:

- A Pain miniboss is a miniboss with the decorator and the stat bonus added
  when it spawns.
- The pain is on **while any Pain miniboss is alive**, and stops when the last
  one dies. The stock version stops only when the room is cleared, which never
  happens here.
- Each Pain miniboss **stacks** the damage. Two alive hurt twice as much. The
  damage comes from a living Pain miniboss, with no proc coefficient.
- When the last Pain miniboss dies and none are still queued, the Corrupted
  Chaos shrines spawn as in the stock modifier. Each Pain miniboss also drops
  the regular miniboss chaos reward.

**The Pain environment.** While a Pain group is alive, the arena also gets the
real `RoomMod_CorruptedChaos_AuraOfPain` modifier, added with
`ZoneManager.AddModifier` when the first Pain miniboss spawns and removed when
the group falls. That is what changes the room's look the way the stock
Corrupted Chaos room does: its looping effect, outer decorations and the
hidden ambient particles all come from the modifier, and the modifier actor
syncs to vanilla clients, who draw it themselves.

Only the look is wanted, so a Harmony prefix on the modifier's own `OnCreate`
and `OnDestroyActor` runs just the `RoomModifierBase` part (through a reverse
patch) for the instance Last Stand added. That skips the stock spawning of two
extra minibosses, the population multiplier, the stat bonus on every entity
and the Corrupted Chaos shrines, which Last Stand handles itself. The stock
damage tick never runs either, because the arena is loaded already cleared
(`didClearRoom`). Instances the game adds by itself, anywhere else, are left
alone.

**The modifier panel.** Adding a main modifier also makes every game show its
panel under the clock: "Omen of Pain" for the Aura of Pain, "Hunted" for the
hunter zone. That panel is `UI_InGame_RoomModDisplay`, which each game draws
for itself. It only hides when a new room starts loading, not when the
modifier goes away. On the host, Last Stand hides it when it removes a
modifier that was still showing. A vanilla client keeps the stale panel until
the next main modifier replaces it or a room loads, and that can't be fixed
from the host.

Both the Aura of Pain and the hunter zone are *main* modifiers, and the game
keeps only one main modifier per node. If a Pain group spawns during a hunter
zone wave, the Pain environment replaces the hunter zone. When the group
falls, the hunter zone is added back if its wave is still running.

**Multiplayer:** every part of this runs on the server. The damage is a normal
dispatched damage event, the decorator is a status effect, and the shrine is a
spawned actor. All of it reaches clients through the game's own syncing.

## HUD label

The top-right location readout, normally `3. Forest of Turbulence`, shows the
fight instead, the same way Dream Arena shows `PVP: Free For All`:

| When | Label |
|---|---|
| In camp | `Last Stand: Camp` |
| Arriving in the arena | `Last Stand` |
| Waiting for a wave | `Wave 13 in 5s` |
| During a wave | `Wave 12: 34 monsters left` |

"Monsters left" counts the ones alive, the ones being spawned, and the ones
still queued for the wave, so it only goes down as monsters die and hits 0 when
the wave clears.

**Only the host sees it.** The label is `UI_InGame_Location`, which each game
fills in for itself from the zone index and the localized zone name, and only
when a room loads. Nothing about it is synced, so a vanilla client keeps the
stock text. On the host, a postfix on `UI_InGame_Location.OnRoomLoaded` grabs
the text component and the stock text. The label then refreshes 4 times a
second, writing only when the text has changed, and the stock text comes back
whenever the mode isn't running. It can be turned off with `Show wave in HUD`.

A mod reload happens without a room load, and the old instance puts the stock
text back as it unloads. So whenever there's a label to show but no text
component yet, the label looks for the existing `UI_InGame_Location`
(`FindAnyObjectByType`, at most once a second) and takes it over. The label
is back within a second of a reload.

## Loot

**Items are scarce per wave, not per kill.** Every kill rolls once for a
memory or essence, and most rolls succeed. The per-wave cap below is what
keeps items scarce. Each rarity has its own chance, lower the rarer it is:

| Rarity | Monster or hunter | Miniboss (×1.5) | Pain miniboss or heroic boss (×2) |
|---|---|---|---|
| Common | 50% | 55% | 40% |
| Rare | 20% | 30% | 40% |
| Epic | 8% | 12% | 16% |
| Legendary | 2% | 3% | 4% |
| **Nothing** | **20%** | **0%** | **0%** |

These are the wave 1 chances. A boosted roll past 100% is cut back as
described below, which is why the miniboss and ×2 columns aren't plain
multiples. **The chances improve every wave** in a straight line up to a target at
wave 100, then stay there. Common falls from 50% to 25%, and the rest rise
until all four even out at 25% at wave 100. Together that's 100%: every kill
rolls an item.

| Monster or hunter | Common | Rare | Epic | Legendary | Any item |
|---|---|---|---|---|---|
| Wave 1 | 50% | 20% | 8% | 2% | 80% |
| Wave 25 | 43.9% | 21.2% | 12.1% | 7.6% | 84.8% |
| Wave 50 | 37.6% | 22.5% | 16.4% | 13.4% | 89.9% |
| Wave 75 | 31.3% | 23.7% | 20.7% | 19.2% | 94.9% |
| Wave 100 on | 25% | 25% | 25% | 25% | 100% |

The tier multiplier applies on top. Past 100%, the excess is cut from the
commonest end, but a rarity is never cut below 25% (`Rarity floor with item
multipliers`). Once it reaches the floor, the cut moves up to the next rarity.
So no rarity ever falls under 25%, and a boosted roll evens out at 25% each
sooner. The floor only limits cuts and never raises a rarity, so a custom
table with a target under 25% still reaches it. Chaos rarity has no floor.

| Even at 25% each from | Wave |
|---|---|
| Monster or hunter (×1) | 100 |
| Miniboss (×1.5) | 65 |
| Pain miniboss or heroic boss (×2) | 47 |

Before that, a boosted roll still leans toward the low end: at wave 25 a ×2
drop is 25% common, 35.6% rare, 24.2% epic and 15.2% legendary.

**Monsters and hunters are capped per wave**, however many die: at most one
item per player (config `Monster items per player per wave`).

- **Solo:** at most one item a wave, a memory or an essence, never both.
- **Two players:** at most two, one memory and one essence, one each.
- **More players:** one each, with memories and essences alternating to stay
  even.

Once everyone has had theirs, monster kills still drop dream dust and gold,
but no more items until the next wave. **Minibosses are not capped.** Each one
rolls its own chance whatever monsters have dropped, and its item goes to the
player with the fewest items this wave.

- The roll walks the rarities rarest first and adds up their chances, so one
  random number decides both whether an item drops and which rarity. If a
  multiplier pushes the total past 100%, the common chance is what gets cut.
- The item itself, and its level or quality, come from the game's `LootManager`
  (`SelectSkillAndLevel`, `SelectGemAndQuality`) for the rolled rarity, so
  drops follow the run's own pools and bans. If the pool has nothing of the
  rolled rarity, the next rarity down that has something is used, never one
  up. Without this, an empty pool meant no drop and no use of the player's
  cap, so the next kill rolled again, which skewed drops toward the rarities
  that do have items.
- **Every item is logged** (`loot: wave 12 Regular dropped a Rare memory.`),
  and `laststand_status` shows the run's count per rarity, so drop rates can
  be checked against the tables.
- **Who gets a monster item:** a player who hasn't had one this wave, with the
  killer first. The item is created with that player as `tempOwner`.
- The rules are plain logic (`LootRules`, `DropLedger`). The ledger is cleared
  at the start of each wave.

| Other drops | Monster or hunter | Miniboss | Pain miniboss |
|---|---|---|---|
| Dream dust | 2 | 50 | 150 |
| Bonus gold | 0 | 40 | 100 |

- Dream dust is flat: 2 per monster or hunter, 50 per miniboss, 150 per Pain
  miniboss, every wave.
  Bonus gold grows 10% a wave. Normal kill gold is untouched (hunters give
  none, as in the stock game).
- **Guarantee (off by default):** when on, anyone who got no memory or essence
  during a wave gets one at their feet when it's cleared.
- Nothing despawns. Memories and essences stay on the ground until someone
  takes them, as in the stock game.

### Chaos rewards

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

## Dream Well

- A single `Shrine_UpgradeWell` at the arena center, spawned by the host.
- **Unlimited uses** at the well's normal dream dust cost.
- `maxTotalUseCount` is a SyncVar and is set to unlimited. `maxUseCountPerPlayer`
  and `useCooldown` are **not** SyncVars, so setting them on the host doesn't
  reach clients, whose own check would lock them out after one use. Dream
  Arena hit this with its remnants. So after every use of our well
  (`Shrine.DoPostUseRoutines`), the synced use counts and last-use time are
  reset and the well is made available again, and each client's check keeps
  passing.

## Wave tracker

Waves are announced in chat as plain-text `MessageType.Raw` notices, which
vanilla clients render as-is: the wave number and what it brings when it
starts, and when the next one comes once it's cleared.

The run's own goal, **"Goal: Shape of Dreams — Move deeper into …"**, makes
no sense here, so the server destroys `Quest_ShapeOfDreams` as soon as it
exists (checked once a second). That is what the game itself does when it
starts a Limbo run (`GameMod_Limbo`). Quests are networked actors, so the goal
disappears for vanilla clients too.

A wave panel in the quest tracker is still an idea. `DewQuest.currentProgress`
and `DewQuest.step` are synced, but quest titles and descriptions are not, so
this needs an existing quest type with a sensible localized title. Not built
yet. The host's [HUD label](#hud-label) shows the wave meanwhile.

### The wave count never resets

Two things used to send a run back to wave 1, and both are fixed:

- **Errors.** Any exception in the session tick used to stop the whole
  session, and the next tick started over at wave 1. Now a failed tick is
  logged (at most once every 5 s) and the session carries on with its state.
  A failed spawn skips just that monster.
- **Mod reloads.** With developer mode's auto-reload, any file change in the
  mod folder reloads the mod mid-run, and a fresh session started at wave 1.
  Now the session saves the run (a key from the `GameManager` instance and the
  world seed), the arena node, the wave and whether it's being fought, every
  time a wave starts or is cleared. It saves to
  `AppDomain.CurrentDomain.SetData`, which outlives the old assembly because
  Mono never unloads it. On arrival, a matching save resumes the run. If the
  wave was being fought, the monsters already on the field (creep-owned
  `Monster`s not spawned by another monster) are taken over by tier, and the
  wave ends when they're dead, with no new spawns. Otherwise the next wave
  follows the usual break. Pain minibosses come back as plain minibosses,
  because their Pain state lived in the old session. The Dream Well already
  standing is reused rather than doubled.

The crash behind the reset at wave 10 came from the game's
`RoomMonsters.SpawnMiniBoss`, which always calls `SelectMiniBoss` first. That
picks from the *room's* own monster pool and throws when the pool has no
miniboss-capable monster, as some boss rooms don't, even though Last Stand
passes its own pick. A prefix on `SelectMiniBoss` hands back Last Stand's pick
while one is pending, so the room's pool is never read.

## Settings

The host can change them in two places: the **Last Stand** button in the
lobby's settings panel (or the `laststand_config` console command), and the
mod manager's config screen. The lobby window edits a copy, and **Apply**
saves it and applies it live; **Cancel** or Escape drops the edits. Its
sections are spread over three columns in a scrolling area, as in Dream
Arena. Only the host has the mod, so only the host sees the button.

**Nothing has to be typed except numbers.** `Arena room` is a dropdown of the
8 boss rooms, and `Heroic boss types` is a list of checkboxes, one per
supported boss (none checked means no heroic bosses). Both are still saved as
plain asset names, so old config files keep working. A saved name that isn't
in the list shows as an extra dropdown option, or is kept as-is behind the
checkboxes, so opening the window never changes it by itself. This works by
putting two entries at the front of the game's `DewGUI.fieldBuilders` list,
which `CreateWidgetsForObject` asks in order for each field. They claim only
fields marked `[PickOne]` or `[PickMany]`, so both config screens get them and
every other field keeps its stock widget. Labels use the game's own name for a
boss (`<type>_Name`) where it has one, plus the world.

**Saved settings and new versions.** The game saves the config as JSON
(`QuickSave/Mods/com.0x42697262.laststand/LastStandMod.config.json` under the
game's persistent data folder) and loads it over the defaults on every start.
So when a new version changes a default, a saved file would quietly keep the
old value. To stop that, the file carries a hidden `schemaVersion`. Before the
game loads it (`LoadConfigsToDisk`), a file with a missing or older version,
or one that can't be read, is moved to `LastStandMod.config.json.old` and the
game writes fresh defaults. This means a version bump also resets any settings
you changed yourself. Copy them back from the `.old` file if you want them.

| Group | Setting | Default |
|---|---|---|
| Arena | Arena room | Primus (dropdown) |
| | Plain arena (no room modifiers) | on |
| | Show wave in HUD | on |
| | Random boss music (host only) | on |
| Waves | First wave delay | 10 s |
| | Break between waves | 5 s |
| | Monsters in wave 1 / extra per wave | 6 / 2 |
| | Extra player share | 0.5 |
| | Surge every N waves / monster multiplier | 5 / 2 |
| | Max monsters alive / per extra player | 200 / 0 |
| | Min spawn distance from heroes | 8 m |
| | Spawn interval / spawns per batch | 0.5 s / 6 |
| Scaling | Health / damage per wave | 10% / 10% |
| | Starting monster level / waves per level | 1 / 10 |
| Minibosses | First wave / every N waves / waves per extra / max | 10 / 10 / 2 / 10 |
| | Health / damage multiplier | 2.5 / 2.5 |
| Heroic bosses | First wave / every N waves / waves per extra / max | 49 / 50 / 100 / 10 |
| | Entrance delay | 3 s |
| | Health / damage multiplier | 10 / 3 |
| | Ability haste / more per later boss wave | +200 / +50 |
| | Types | 6 zone bosses (checkboxes) |
| | Corrupted Chaos rewards / dust / gold / item multiplier | 3 / 1000 / 200 / 2 |
| Pain | First wave / every N waves / waves per extra / max | 20 / 20 / 2 / 3 |
| | Damage per second per boss | 1% |
| | Health / damage bonus | 20% / 30% |
| Other worlds | First wave / every N waves / types per step | 10 / 10 / 1 |
| Hunters | Waves per extra hunter / max per wave | 5 / 10 |
| | Waves between hunter zones / max hunt level | 10 / 6 |
| Mirage | First wave / waves per new skin / chance | 30 / 10 / 20% |
| Item drops | Monster items per player per wave | 1 |
| | Guaranteed drop per wave | off |
| | Common / rare / epic / legendary chance | 50% / 20% / 8% / 2% |
| | Item chances reach target at wave | 100 |
| | Common / rare / epic / legendary at target | 25% / 25% / 25% / 25% |
| | Memory share | 50% |
| | Rarity floor with item multipliers | 25% |
| Enemy rewards | Gold growth per wave | 10% |
| | Item chance multiplier: monster / miniboss / Pain | 1 / 1.5 / 2 |
| | Per-tier dust and gold | see Loot |
| Chaos rarity | Chaos reward: legendary / epic / rare chance | 5% / 15% / 35% |
| | Chaos chances reach target at wave | 50 |
| | Chaos reward: legendary / epic / rare at target | 17.5% / 35.2% / 47.3% |

## Console commands

With Developer Mode on (F1, then `` ` ``):

| Command | Does |
|---|---|
| `laststand_status` | Logs mode, phase, wave, time to next wave, alive, queued, and items dropped per rarity |
| `laststand_next` | Starts the next wave now, even if monsters remain (host only) |
| `laststand_wave <n>` | Starts wave n now, e.g. after testing (host only). Monsters already out stay |
| `laststand_config` | Opens the Last Stand settings window |
| `laststand_solo` | Starts a solo Last Stand run |

## Code layout

| Folder | Concern |
|---|---|
| `src/LastStandMod.cs` | Entry point. Wires the session, Harmony and lifecycle |
| `src/LastStandConsole.cs` | Console commands |
| `src/Mode` | Whether Last Stand is selected, and selecting it |
| `src/UI` | The solo button, the lobby dropdown option, the settings window and its lobby button, the dropdown and checkbox fields, UI cleanup |
| `src/Session` | The run's phases (camp, arriving, fighting) and arrival |
| `src/Arena` | Room override, modifier gate, camp rift, cleared-room prep, arena layout, staging heroes |
| `src/Waves` | Wave schedule, wave cycle and spawn queue (plain logic), and the director |
| `src/Combat` | Monster catalog, spawning, heroic bosses, per-wave enhancements, hunter zones, tracking enemies, Pain |
| `src/Loot` | Loot rules, rarity tables and ledger (plain logic), drops |
| `src/Well` | The Dream Well |
| `src/Game` | Thin wrappers over game APIs, and game identifiers |
| `src/Config` | The mod config, its validated snapshot, and internal tuning |
| `src/Text` | Every player-facing, config, log and command string |
| `src/Patches` | Harmony hooks. Each one only hands off to the code above |

## Networking

Everything is server-side, so **only the host installs the mod**. Clients see
synced state: spawned monsters, status effects, damage, pickups, the well and
chat notices.

The host's lobby shows the MOD icon and sets `isAlteringGameplay`, as the
official rules require. As with Dream Arena, a modded lobby turns off
cross-play, so only Steam players can join.
