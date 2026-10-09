# Pain minibosses

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
modifier goes away, so once these end the arena's default panel replaces it
for everyone ([Modifier panel](../hud/modifier-panel.md)).

Both the Aura of Pain and the hunter zone are *main* modifiers, and the game
keeps only one main modifier per node. If a Pain group spawns during a hunter
zone wave, the Pain environment replaces the hunter zone. When the group
falls, the hunter zone is added back if its wave is still running.

**Multiplayer:** every part of this runs on the server. The damage is a normal
dispatched damage event, the decorator is a status effect, and the shrine is a
spawned actor. All of it reaches clients through the game's own syncing.
