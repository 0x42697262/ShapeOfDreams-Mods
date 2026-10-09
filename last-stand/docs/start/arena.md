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

The game sends each new monster toward the nearest hero, so monsters far away
walk in.
