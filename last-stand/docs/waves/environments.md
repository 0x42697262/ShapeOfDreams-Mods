# Environments

An **environment** is one of the game's own room modifiers, picked at random
from `Environment types`. Which waves get one:

- **Waves 1-49, by chance.** Every wave rolls `Environment chance (%)`
  (10% by default) when it starts, wave 1 included.
- **Waves 50-99, every wave.** From `Higher environment chance from wave`
  (50) the chance is `Higher environment chance (%)` (100), so every wave
  without a heroic boss or Pain gets one.
- **Hunter zone waves, half the time with a cap.** From the first hunter
  wave (`First hunter wave`, 100) every wave is a hunter zone and rolls
  `Hunter zone environment chance (%)` (50%), but a block of 10 waves
  (100-109, 110-119 …; `Hunter zone: block size` 10) has at most 2
  environment waves (`Hunter zone: max environments per block`), because
  each one takes that wave's hunter zone. Once a block has 2, its other
  waves roll nothing, so at least 8 waves of every 10 keep their hunter
  zone. This stage starts with the hunter zones, so moving `First hunter
  wave` moves it too.
- **Heroic boss waves** get none before wave 100 (`Environments on heroic
  boss waves from wave`), so waves 25, 50 and 75 have none; from 100 on (100, 125, 150 …)
  they roll like any other wave.
- **Never** on a Pain wave (20, 40 … 100, 120 …): the Aura of Pain is a main
  modifier too, and only one fits. Never after `Last environment wave` (0
  by default, never stop).

There are no sure environment waves any more (106, 116 … until schema 48).

The rule is plain logic: `EnvironmentRule.ChanceFor` gives the wave's
chance (0 or a setting) from the block's count so far,
`WaveSchedule.EnvironmentChance` adds the heroic and Pain exclusions, and
`WaveSchedule.Plan` compares it with a roll the director passes in
(`UnityEngine.Random.value`). The director keeps the count
(`EnvironmentTally`): it counts a wave once, when its environment begins,
and starts over in a new block or a new run. The count lives only in
memory, so a mod reload mid-block starts it at 0, and a wave resumed after
a reload rolls again.

The environment goes
live when the wave starts and is removed when the wave is cleared, so the break
after it is plain. Chat says which one came ("The arena changes: …") and
when it fades. All of these are main modifiers, like the hunter zone and the
Aura of Pain, and the game keeps only one main modifier per room, so adding
one would replace the other:

- **From wave 100 the environment replaces the hunter zone.** A wave that
  rolls one gets an environment and no
  hunter zone (`WaveSchedule.Plan` sets their hunt level to 0), so no artillery and no hunter stat bonus. Hunters
  still make up their usual share of the wave, and the hunt level of the next
  hunter zone wave is the same as without the environment.
- **A wave with Pain minibosses never gets one,** since the Aura of Pain
  is a main modifier too.

It's added the same way as the hunter zone: `ZoneManager.AddModifier` on the
arena node, through `ModifierGate.AddOwn` so the plain-arena gate lets it
through. The modifier is a networked actor, so vanilla clients get its status
effects, hazards and visuals. Removing it destroys the actor, whose
`OnDestroyActor` takes the status effects off everyone and stops its routines.

| On by default | What it does |
|---|---|
| Accelerated Time | Everyone moves, attacks and casts faster, monsters too |
| Veil of Time | One random memory per character gets −85% cooldown, the rest +400% |
| Dream of Wealth | Monsters drop gold on death, and gold stones appear |
| Pure Dream | Monsters drop dream dust on death, and dust stones and breakable dream statues appear |
| Black Rain | Elemental damage and duration down, other damage up |
| Symbiote Habitat | More attack speed, less armor |
| Inversion Sigil | Elements flipped; plain damage becomes a random element |
| Toxic Area | Healing and shields much weaker |
| Gravity Training | Everyone slower |
| Extreme Cold | Patches of icy mist |
| Burning Forest | Fire damage near monsters |
| Dark Condensation | Vision halved |
| Shroud of Darkness | Patches of darkness that block vision |
| Warped Space | Random teleports |
| Hazardous Weather | Meteor showers |
| Crimson Flame Demon | Fire tornadoes |
| Giant Ink Drop Warning | Slowing ink drops aimed at heroes |
| Gaze of the Abyss | Damaging gazes sweep the room |
| Unstable Rat Swarm | Exploding rats keep coming |
| Leaf Hound Den | Monsters spawn at random sizes, bigger ones tougher |
| Meteoric Life | Random sizes, and dead monsters explode |

**Off by default:** Lingering Aura of Guidance (health regen) and Star
Eater's Dream (cookies), because they place shrines in the arena that stay
after the wave. **Never in the list:** Ambush (it spawns Ambush Orb towers),
Dream of the Star Seeker (it hands out stardust far faster than normal play;
Last Stand gives stardust only at the vanilla boss-kill rate), and the modifiers that need a normal room to work: Distant Memories
(clears every monster and opens the rift), Challenging Fight and Fallen Star
(tied to the exit rift and room rewards).

**Pure Dream statues and stones stay in the arena.** Pure Dream places
dust stones (`PropEnt_Stone_DreamDust`) and a few dream statues: monsters
from the room's own `defaultRule.pool`, frozen (AI off, animator off,
dream-dust material, unstoppable, 1.2–1.3× size), owned by the
environment and outside the wave count, that drop dream dust when broken
(`Se_PureDream_Statue`). Both take their spot from the private
`RoomMod_PureDream.CalSpawnPosition`, which picks a random prop cell of
the room's whole map (`innerPropNodeIndices`). In a boss room that map
includes places the fight never reaches: the Lava Land lava sea (the
navmesh runs over it) and the raised entrance ledge where heroes land
(about 30 m up, seen with `laststand_status`). In the arena a prefix
(`PureDreamSpotPatch`) runs the game's own pick through a reverse patch
and keeps it only if the spot is playable (`Dew.IsNotPlayableOnTop` is
false) and reachable on foot from the arena center (a complete navmesh
path at most 1.6× the straight line, the same test as the edge shrines).
Otherwise it picks again, up to 10 times (each pick uses up one prop
cell, as the game's own does). After 10 bad picks the last spot is moved
to the center's height and pulled toward the center 1 m at a time until
it's playable (`PlayableGround.PullToward`). Logs: `pure dream: statue or
stone placed at ... after N tries` and `pure dream: no good statue or
stone spot in 10 tries; moved ... in to ...`. If anything throws, the
game's own pick is used.

When the wave ends and Pure Dream is removed, `Se_PureDream_Statue` goes
with it (it's the modifier's child), but the statues stay: AI off,
population cost 0, and on every client the dream-dust material and a
stopped animator, set once by RPC. So a leftover statue is recognized as a
`Monster` owned by `DewPlayer.environment` with `AI.disableAI` set
(`DreamStatues.IsStatue`), which `laststand_status` and `laststand_statues`
use.

In the Lava Land boss room the statues are Infernus (the room's pool is
the boss), and Last Stand counts a broken one as a heroic boss: 2
Corrupted Chaos and the boss stardust each (`HeroicBosses.Owns` claims any
supported `BossMonster` in the arena node, a fallback for bosses alive
across a mod reload). That's kept as a feature for now.

**The arena counts as in combat during an environment.** The arena loads
cleared (`Room.didClearRoom` is true from `isRevisit`), and many environments
check that flag and stop: Crimson Flame Demon, Gaze of the Abyss, Hazardous
Weather and Giant Ink Drop (which removes itself), Warped Space's teleports,
the rat projectiles. So while an environment is live, the host sets
`didClearRoom` back to false (`RoomCombat`, through the private setter; it's
not synced) and sets it true again after removing the modifier. A prefix on
`Room.ClearRoom` blocks the game's clear routine in that window, because it
would destroy idle monsters and fire `onRoomClear`. Side effects of the flag:
the game skips its mid-run save during that wave, and some monsters' own
summons (such as the Sky world's Little Baam) work, as they do in a normal
fight.

**Monster-based environments use Last Stand's own spawns.** Every spawn is a
one-monster boss rule, so the room's population multiplier never adds
monsters. Leaf Hound Den and Meteoric Life still apply their size and health
change (`RoomMonsters.onAfterSpawn` runs for every spawn), but their
`OverrideMonsterType` only swaps the room's `defaultRule`, which Last Stand
doesn't spawn from. The modifier never puts it back, so Last Stand saves
`defaultRule` before adding an environment and restores it afterwards. Rats
and other summons aren't tracked, so they don't hold the wave open.

**The modifier panel.** The game's modifier panel shows the environment's
name and description when it's added. When it ends, the arena's default
panel replaces it for everyone ([Modifier panel](../hud/modifier-panel.md)).
