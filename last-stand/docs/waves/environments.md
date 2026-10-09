# Environments

Waves 6, 16, 26 … (`First environment wave` 6, every 10 waves, with no
`Last environment wave` by default) each bring an **environment**: one of the
game's own room modifiers, picked at random from `Environment types`. It goes
live when the wave starts and is removed when the wave is cleared, so the break
after it is plain. Chat says which one came ("The arena changes: …") and
when it fades. All of these are main modifiers, like the hunter zone and the
Aura of Pain, and the game keeps only one main modifier per room, so adding
one would replace the other:

- **From wave 101 the environment replaces the hunter zone.** Waves 106,
  116, 126 … get an environment and no hunter zone (`WaveSchedule.Plan` sets
  their hunt level to 0), so no artillery and no hunter stat bonus. Hunters
  still make up their usual share of the wave, and the hunt level of the next
  hunter zone wave is the same as without the environment.
- **A wave with Pain minibosses never gets one.** With the defaults they
  never meet (Pain every 20 waves from 20).

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
| Pure Dream | Monsters drop dream dust on death, and dust stones appear |
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
