# Arena

The run opens in the normal camp (node 0, `WorldNodeType.Start`). Every other
node is redirected to one boss room with `ZoneManager.SetRoomOverride`, applied
right after `GenerateWorld_Imp` fills the node list. Taking the rift out of camp
starts the fight. The override writes into the synced node list, so vanilla
clients load the same room.

The arena basics in this page are shared with Dream Arena and live in the
library (`library/src/Arena`, `library/src/Patches`). The override is
`ArenaRooms.Apply(arenaRoom, stripModifiers)`: the library's `WorldGenPatch`
(a postfix on `GenerateWorld_Imp`) calls Last Stand's `OnWorldGenerated`
override, which applies `Arena room` (a blank name means
`Room_Primus_Boss_0`) to every arena node except the current one. An arena
node is any node but the camp (`ArenaLocator.IsArenaNode`). Log: `arena
room '<room>' applied to N node(s); M room modifier(s) removed.`, or `could
not apply arena room '<room>'.` if it throws.

**Random arena** (off by default). With `Random arena` on, the session
passes the boss rooms ticked in `Random arena: worlds` (all 8 by default;
none ticked, or Random arena off, means just `Arena room`) to
`ArenaRooms.Apply`. It's still always a boss room, so the arena center
(`Room_BossArena`), the shut exit rift and the boss music work the same as
with a fixed room. The pick is stateless: `ArenaRooms.RoomFor` first looks
for an arena node whose `roomOverride` is already one of the allowed rooms
and keeps it; only when none is does it pick one with `UnityEngine.Random`.
So the synced node list is the record: the once-a-second camp check, a mod
reload and a continued run all keep the run's room instead of rolling a new
one, and a quest's special room on a node is replaced like before. Log on a
fresh pick from two or more rooms: `arena room: picked '<room>' at random
from N room(s) for this run.` before the usual `arena room '<room>'
applied ...` line.

World generation isn't the only chance. Each time `CampRift` runs its
once-a-second camp check, `ArenaRooms.EnsureApplied` looks at every arena
node again and, if any has lost the boss room (seen once in a multiplayer
run, where the party landed in the world's normal combat room), applies the
override again. Log, as a warning: `arena room: an arena node had lost
'<room>' before the camp rift opened; applied it again.` A run that's fine
logs nothing.

**Version line.** Once per run, when the camp is ready
(`GameManager.IsLazyCallReady()`), the library's `VersionAnnouncer` posts
the mod's version from `about/metadata.json` (`modVer`) in light blue to
everyone, for example "Last Stand v0.9.0", so players can see which version
the host runs. Log: `version: announced v0.9.0 in chat.` A mod reload
during the camp posts it again.

## Leaving camp

The camp rift goes straight to the arena: one "move to the next location"
prompt, no world map and no node to pick. `Rift_RoomExit` checks its
`nextNodeIndex` before opening the map, and that field is a SyncVar, so the
host points it at an arena node and vanilla clients get the same single
prompt. The library's `CampRift` does this once a second while in camp
(`SharedTuning.CampRiftCheckSeconds`). `ArenaLocator.TryFindArenaNode` picks
the node: it skips the current node, the camp and sidetrack nodes, and
prefers one connected to the current node, else the first. Log: `camp rift
now leads straight to node N.` It also hides the other world-map nodes
(below), in both mods.

In multiplayer, a confirmed travel normally still goes to a vote, which opens
the world map for everyone with a 6-second countdown. The host decides that
in `ZoneManager.ShouldVoteOnTravel()`, whose first test is the public,
unsynced `forceVoteForDebug` field. Every frame the library's `TravelVotes`
asks the mod through its `SkipTravelVote` override; Last Stand's answers yes
(there's only the one rift, always to the arena), so on the host it sets
`forceVoteForDebug = false` and the party travels at once. It used to be a
Harmony prefix on `ShouldVoteOnTravel`, which didn't take effect in a
multiplayer run where a client's rift opened a vote; the field needs no patch.
Log, once per run: `travel votes: off on the host for this run`. Unloading
the mod sets the field back to null. Dream Arena answers no and keeps the vote. The synced
`GameSettingsManager.enableVotes` is left alone on purpose: the lobby saves
it as the host's preference, so clearing it would carry into normal runs.

## The arena room

The arena loads already cleared: `Room.isRevisit` (a SyncVar) is set in
`Room.OnStartServer`, before clients receive the room. The room is lit, its
barriers are open, and its own monsters, props, rewards and boss are skipped.
The entry trigger (`DewAllHeroesPresentZone`) is switched off and intro
cutscenes are skipped. The exit rift stays shut, so there is nowhere to travel.

This is the library's static `PlainArena`, run by `PlainArenaPatches`
(postfixes on `Room.OnStartServer` and `Room.StartRoom`, which opens the
barriers with `RemoveObstacles`). Both act only while hosting the mode
(`ModMode.IsHosting()`), and `Prepare` only in an arena node
(`ArenaLocator.IsInArenaNode()`). `PlainArena.Enabled` stays on in Last
Stand, whose `Plain arena (no room modifiers)` setting is about modifiers
(below); Dream Arena sets it from its own `Plain arena` setting. Log: `plain
arena: '<room>' loads cleared; N entry trigger(s) disabled.` The cutscene
skip is the library's `CutscenePatch` (a prefix on
`DewCutsceneDirector.PlayNetworked`), which asks the mod: Last Stand's
`SkipCutscenes` override answers yes in any arena node. If that check
throws, the error is logged (throttled) and the cutscene plays.

**A plain arena.** World generation rolls room modifiers onto every node, and
the arena node keeps them. In testing, the arena arrived with
`RoomMod_SpawnGuidance` (an extra shrine standing in the arena),
`RoomMod_StrayMemory_StartProp` and `RoomMod_HarderFightBetterReward`. The game
also adds modifiers later: Corrupted Chaos's Aura of Pain, a miniboss on
travel (`RoomMod_SpawnMiniBoss`), and the hunters' spread. So, with
`Plain arena` on (the default):

- after `GenerateWorld_Imp`, every arena node's modifiers are removed with
  `ZoneManager.RemoveModifier`, which writes the node back so it syncs
  (`ArenaRooms.Strip`, in the same `ArenaRooms.Apply` call that sets the
  room);
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
game's default), as long as at least one hero is still standing
(`HeroRevival.ReviveAll` and `AnyHeroStanding` in the library), so they
have the break to get ready for the next wave. "Standing" uses the game's
own game-over rule (`GameManager.CheckGameOver`): any `Hero` in
`ActorManager.allHeroes` that isn't knocked out. That list includes heroes
no player owns, such as the AI allies of the AllyPlayerAI Workshop mod, so
a solo player knocked out while an AI ally still fights is revived too.
(`Dew.GetAliveHeroCount` counts only `DewPlayer.gamePlayers`' heroes; the
first version used it, so a solo player with AI allies was never revived
while the run went on.) If everyone is down, the game's own game over
follows as usual. Chat announces the revive.

**The world map shows only the camp and the arena.** The map (M) draws every
node except sidetrack nodes, and `WorldNodeData.IsSidetrackNode` is just
`position.x < 0`. So once the camp rift has its arena node, and again on
arrival, every other node's map position is mirrored to a negative x
(`-1 - x`, `SharedTuning.HiddenMapNodeOffset`) and written back to the
synced `ZoneManager.nodes` (the library's `WorldMapNodes.HideAllBut`, which
logs `world map: hid N node(s); only the camp and the arena (node K) are
shown.` when it hides any). Clients' maps then skip those nodes. Nothing
else needs them: the arena never travels on, and the game only uses the
sidetrack test for hunter progress and for picking quest nodes.

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

The arena's center comes from the library's `ArenaLayout.TryFind`, once the
room is active: `Room_BossArena` when the room has one, otherwise the
`pathablePivot` of the largest `RoomSection` (radius from its area, 4 to
18 m). Last Stand passes `useEntryTeleporter` false, so Dream Arena's middle
anchor, the entry teleporter's destination, is never used: the center, and
the shrine ring measured from it, stays on the marker or the largest
section. `Room_BossArena` is read with `.softInstance`, so a room without
one costs no scene scan. On arrival, `ArenaArrival` places players with the library's
`HeroStaging.PlaceAround` in a ring at half the radius, at least 2 m
(`Tuning.HeroRingShareOfRadius`, `MinHeroRingRadius`), and the **Dream
Well**, **Altar of Cleansing**, **Shrine of Ascension**, **Maw of Doom**,
**Shrine of Destiny** and **Hatred Remnant** are spaced evenly around the
edge of the room ([Edge shrines](../shrines/edge-shrines.md)). Arrival
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
