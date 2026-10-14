# Arena

## The arena room

`WorldNodeData` has both `room` and `roomOverride`, and travel loads the
override when it's set. `ZoneManager.SetRoomOverride(node, room)` is a public
server method that writes back into the synced `nodes` list, so unmodded
clients load the same room. With `Arena for every room` on,
`ArenaRooms.Apply(arenaRoom, stripModifiers)` (shared with Last Stand,
`library/src/Arena/ArenaRooms.cs`) points every arena node
(`ArenaLocator.IsArenaNode`: any node but the camp, `WorldNodeType.Start`)
except the current one at `Arena room` (default `Room_Primus_Boss_0`, the
Lone King's Zenith, also used for a blank name). The current node is
skipped because the game itself warns that overriding the room you stand
in is prone to bugs.

It runs when the world is generated, when the session starts, and when
settings change, so a new arena room applies to the next travel. The world
gen hook is the library's `WorldGenPatch` (a postfix on
`ZoneManager.GenerateWorld_Imp`), which calls Dream Arena's
`OnWorldGenerated` override; that applies the room only while the session
is active. Log: `arena room '<room>' applied to N node(s); M room
modifier(s) removed.`, or `could not apply arena room '<room>'.` if it
throws.

No boss appears because the node keeps its original type (only the room
asset changes, so no `ExitBoss` encounter starts) and the monster population
is zero.

**Room modifiers.** With `No room modifiers` on, every arena node's modifiers
are removed with `ZoneManager.RemoveModifier`, one id at a time
(`ArenaRooms.Strip`, in the same `Apply` call). Rolls like
Accelerated Time change everyone's speed, which has no place in an even match.
`WorldNodeData` is a struct inside a synced list, so editing its list in place
wouldn't replicate; `RemoveModifier` writes it back.

## Plain arena

A boss room loads in its pre-fight state, and its intro cutscene brings the
lights up. In `Room_DarkCave_Boss_0`, `DarkCaveCutSceneFogFadeOut` sets the
boss fog to full black on load and only the cutscene clears it. Lighting, fog
and post-processing are drawn locally on each client, so the host can't
restyle them for unmodded players.

But the game has a synced "this room is done" state: `Room.isRevisit`. Rooms
read it on every client to present themselves cleared: Dark Cave fades its
fog at once and the server fires `BossSeekerKillBehavior`, which brings the
sunlight in. Barriers open, and props, monsters and rewards are skipped.

`ZoneManager` sets the flag in the scene-load callback, just before
`NetworkServer.SpawnObjects` runs `Room.OnStartServer`. With `Plain arena` on,
`PlainArenaStartServerPatch` (a postfix there) forces it before any client
receives the room, and `PlainArena.Prepare` switches off the server-only
entry trigger (`DewAllHeroesPresentZone`). With `Skip intro cutscenes` on,
whether `Plain arena` is on or off, the same postfix also switches off
room-section cutscene calls (`SectionCutscenes`; `Room_Primus_Boss_0` starts
its cutscene from a `RoomSection` event, not the entry trigger; see Last
Stand's `docs/start/arena.md`, *Room-section cutscenes*). A postfix on
`Room.StartRoom` opens
any barriers (`RemoveObstacles`). A cleared room never opens its own exit
rift, which is left alone: there's nothing to leave the arena to.

`PlainArena` and both patches are shared with Last Stand (in the library:
`library/src/Arena/PlainArena.cs`,
`library/src/Patches/PlainArenaPatches.cs`). The patches act only while
hosting the mode (`ModMode.IsHosting()`), and `Prepare` only in an arena
node. `Plain arena` reaches the library through the static
`PlainArena.Enabled`, which the session sets in `UseSettings` (Last Stand
leaves it on). Log: `plain arena: '<room>' loads cleared; N entry
trigger(s) disabled.`

How lit a room ends up depends on how it scripts its revisit:

| Room | Revisit lighting | Hazards |
|---|---|---|
| DarkCave_Boss_0 | fog clears, sunlight in | none |
| LavaLand_Boss_0 | none scripted | lava |
| Sky_Boss_0 | none scripted | damaging light fog |
| Ink_Boss_0 | none scripted | only boss-spawned |
| Forest_Boss_0 | none; day and night are per client | none |
| SnowMountain_Boss_0 | none | none |
| Despair_Boss_0 | none; fog follows the camera | none |
| Primus_Boss_0 | none | none |

Floor hazards stay on as a bit of environmental challenge. Both run on the
server (`LavaLand_Lava`, `Sky_LightFogDamager`), so they could be switched off
host-side if that's ever wanted.

## Cutscenes

With `Skip intro cutscenes` on, `CutscenePatch` refuses
`DewCutsceneDirector.PlayNetworked`, which removes the "press space to skip"
wait between rounds. The entry routine still fades and teleports.

The patch is the library's (`library/src/Patches/CutscenePatch.cs`). It
asks the mod through its `SkipCutscenes` override: Dream Arena's returns the
setting, in any node; Last Stand's skips in the arena node only. If the
check throws, the error is logged (throttled) and the cutscene plays.

## The arrival ring

A boss room is two spaces: an antechamber you land in, and the arena proper,
which the game only puts you in once every hero stands on the entry trigger.
Left alone, players would arrive with PvP live and fight in the corridor
(`Room_SnowMountain_Boss_0` has a long approach). So the session stages
arrival: once per arena visit, as soon as the room is loaded and a hero is
standing, it sweeps PvE and `ArenaStaging.TryPlaceInRing` places every
living hero (`LiveHeroes.Fill`) evenly on a ring through the library's
`HeroStaging.PlaceAround`, snapped to walkable ground with
`Dew.GetValidAgentDestination_LinearSweep`, with summons dropped next to
their owner. It retries every 0.25 s until it works. The same placement runs
after `DewHeroesTeleporter.TeleportAllHeroes` (the game's own fight start)
and on `arena_start`.

The library's `ArenaLayout.TryFind(useEntryTeleporter, out points)`
(`library/src/Arena/ArenaLayout.cs`) waits for an active `Room`, then picks
the center from three anchors, in order:

| Anchor | Notes |
|---|---|
| `Room_BossArena`: its center and radius | only Lava Land, Primus and Despair have one |
| `DewHeroesTeleporter.destination` | where the game drops heroes to start a fight; radius 10 m (`SharedTuning.FallbackArenaRadius`); only with `useEntryTeleporter` |
| the largest `RoomSection` | boss rooms are an approach plus one big space; `pathablePivot` is known-walkable, radius from its area (4 to 18 m, `SharedTuning.MinArenaRadius`/`MaxArenaRadius`) |

Dream Arena passes `useEntryTeleporter` true. Last Stand passes false, so
its center (and its edge shrine ring) stays on the marker or the largest
section.

The ring is 75% of the radius (at least 1 m: `Tuning.ArenaRingShare` and
`MinRingRadius`, passed to `PlaceAround`; Last Stand uses 50% and 2 m). The
log names the anchor (`ArenaPoints.Source`):
`staging: placed 2 hero(es) around Room_BossArena at (...), ring 7.5 m.`

There's no spawn protection: players come from the safety of camp, and
nothing is hostile until they're through the rift.

## Removing PvE

Zeroing the population stops waves, but not everything. With `No monsters`
on:

- **Props** (wells, gold and dust stones, merchants, shrines) come from
  `RoomProps.OnRoomStartServer`, a separate path. `RoomPropsPatch` skips it in
  the arena.
- **Everything else** is swept by `PveSweep` once a second in the arena, in
  two passes, with `Destroy()` rather than `Kill()` so nothing drops rewards
  or opens the exit rift:
  - `ActorManager.allEntities`: every `Monster` (bosses included) and
    `PropEntity`, and summons not owned by a human player. A monster's summon
    outlives the monster and stays hostile, an invisible auto-attack target.
    Heroes and players' summons are kept.
  - `ActorManager.allActors`: shrines, chests (`Treasure`), artifacts,
    `Room_Trap_Toggleable`, game modifiers (with `No game modifiers`), and
    items with no `owner`, `handOwner` or `tempOwner`. `IItem` also covers a
    hero's equipped kit, so all three owners must be empty. Rifts are kept.

Only five types derive from `Entity` (`Hero`, `Summon`, `Monster`,
`PropEntity`, `MockEntity`), so the entity pass covers everything a hero
could auto-attack except other players. Most room furniture derives from
`Actor`, not `Entity`, which is why the second pass exists. `arena_scan`
lists both (see [Console commands](../reference/console-commands.md)).
