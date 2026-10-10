# Arena

The run opens in the normal camp (node 0, `WorldNodeType.Start`). Every other
node is redirected to one boss room with `ZoneManager.SetRoomOverride`, applied
right after `GenerateWorld_Imp` fills the node list. Taking the rift out of camp
starts the fight. The override writes into the synced node list, so vanilla
clients load the same room.

## Leaving camp

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

## The arena room

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
  node. Two kinds still get through. One is Last Stand's own Pain environment,
  hunter zone and wave environments, which are added through
  `ModifierGate.AddOwn`. The other is
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

**Music.** The arena plays its boss room's own boss music, for the host and
every client (`Boss music`, on). Music is local to each game: every client
picks the zone's track in `Room.OnLateStart`, and `MusicManager` isn't
networked. But each boss room scene carries its own start object
(`BossMusicStart`, `BossMusic` in the Forest room) with a `NetworkIdentity`,
an `FxNetworkedEffect` and an `FxChangeMusic` holding the room's track. In a
normal run the boss section's `onStartCombatArea` calls
`FxNetworkedEffect.PlayNetworked()` on it, which sends the game's own effect
message to every client; each finds its own copy of the object by its
`netId` and plays the track. The arena loads cleared, so that never happens
by itself. `ArenaMusic` makes the same call: once the arena is ready it finds
the room's `FxNetworkedEffect` whose `FxChangeMusic` has a track (by its
components, which skips Ink's local second-phase track and Primus's
`Silence Music`), waits 2 s, and calls `PlayNetworked()` (log `boss music:
played the room's BossMusicStart for the host and N ready connection(s).`).
It plays it again 2 s after the number of ready connections changes, so a
player who joins mid-run or reconnects hears it too; `MusicManager` only
switches when the track differs, so nobody else's track restarts. A room
without its own boss music logs that once and keeps the zone music.

An earlier version tried a random heroic boss track instead. Only the host
heard it: an arbitrary track can't be named over the network, since a
networked effect can only play objects that already exist in the client's
scene. The room's own track does exist there, which is why this works.

The arena's center is `Room_BossArena` when the room has one, otherwise the
`pathablePivot` of the largest `RoomSection`. On arrival, players are placed in
a ring around it, and the **Dream Well**, **Altar of Cleansing**, **Shrine
of Ascension**, **Maw of Doom**, **Shrine of Destiny** and **Hatred Remnant**
are spaced evenly
around the edge of the room ([Edge shrines](../shrines/edge-shrines.md)). Arrival
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

**Lava Land: fixed spawn points.** The Lava Land boss room is a platform
with eight arms in a lava sea, and the navmesh runs on over the lava
(`LavaLand_Lava`). The game's spawn filter only looks at the navmesh, so
random section points landed in the lava. Rather than retrying until a
point is off the lava (extra raycasts and path checks per spawn), the room
gets fixed spawn points: the scene's `LavaLand_InfernusPillarPosition`
(where Infernus raises its pillars) has eight child transforms, one at the
tip of each arm, about 19.5 m from the center. When `SpawnPoints` finds
that component, every spawn (regular, hunter, miniboss, Pain and heroic
boss) uses them:

- starts at a random one of the eight and takes the first that is at least
  8 m from every living hero (any, if none is);
- moves up to 3 m from the tip toward the component's center (the
  platform's middle), so monsters don't stack on one spot and stay on the
  arm, then snaps with `Dew.GetValidAgentDestination_Closest`.

That's one random pick and one snap per spawn, cheaper than the room
sampling. Log: `spawns: 8 fixed spawn point(s) from
LavaLand_InfernusPillarPosition; monsters spawn only there.` Other rooms
don't have the component and keep the room sampling above.

The game sends each new monster toward the nearest hero, so monsters far away
walk in.
