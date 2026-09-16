# Dream Arena

Host-only PvP proof of concept for Shape of Dreams.

**Other players do not need this mod.** Everything is enforced as server-side
state that Mirror replicates.

## How PvP is possible

`DewPlayer` carries three SyncLists that vanilla builds but never uses:

```csharp
public readonly SyncList<DewPlayer> allies   = new SyncList<DewPlayer>();
public readonly SyncList<DewPlayer> enemies  = new SyncList<DewPlayer>();
public readonly SyncList<DewPlayer> neutrals = new SyncList<DewPlayer>();
```

`GetTeamRelation` consults them *before* its final "both are human players, so
Ally" default, so an explicit entry wins:

```csharp
if (allies.Contains(other))   return TeamRelation.Ally;    // 1
if (enemies.Contains(other))  return TeamRelation.Enemy;   // 2  <- override
...
if (isHumanPlayer && other.isHumanPlayer) return TeamRelation.Ally;  // 6
```

Nothing in the shipped game ever adds to these lists. `DewConsoleCommands` even
contains orphaned helpers (`ClearRelationship`, `ResolveTwoPlayersAndLog`) that
nothing calls — unshipped relation commands.

Three properties make it work without client installs:

1. **SyncLists are server-authoritative.** The host writes; Mirror replicates.
2. **All damage routes through it.** `AbilityTargetValidator` defaults to
   `Neutral | Enemy`, so a hostile player becomes a legal target for every
   damaging ability. There is no separate friendly-fire flag anywhere.
3. **The UI already reacts.** `TeamRelationChanged` is registered as a SyncList
   callback and refreshes healthbars, so clients recolor nameplates themselves.

Relations are **directional** — `a.GetTeamRelation(b)` only reads a's lists — so
every change is written to both sides.

## What it does

| setting | effect | mechanism |
|---|---|---|
| Arena mode | master switch | — |
| Team mode | FreeForAll / TwoTeams / Peaceful | `enemies` / `allies` SyncLists |
| Never end the run | players can die, run continues | prefix blocking `GameManager.WrapUpAndShowResult` |
| No monsters | empty arena, no boss | `maxAndSpawnedPopulationMultiplier = 0` |
| Camp and arena | safe camp, PvP past the rift | `SetRoomOverride` on every non-Start node |
| No PvE | no monsters, props, boss, chests or loose items | blocked `RoomProps`, plus an actor sweep |
| Clean HUD | no Goal, no room modifiers | hidden quests, stripped node modifiers |
| Direct rift | camp exits straight into the arena | `Rift_RoomExit.nextNodeIndex` |
| Auto revive | knocked-out players get back up | `Se_HeroKnockedOut.Revive(mult)` |

`WrapUpAndShowResult` is the single funnel every run-ending path goes through
(`CheckGameOver`, `ConcludePureWhiteDream`, `ConcludeUnknownFate`), which is why
blocking it there is stronger than clearing `isGameOverEnabled` — that only
stops the all-knocked-out countdown. Both are applied.

Win conditions are deliberately absent. This build just holds the game open.

### The arena

Default is **the Lone King's Zenith** — `Room_Primus_Boss_0`. Confirmed from the
scene file `level149`, which contains `Special_TheLoneKingsZenith` (the room
announcer string), `Room_Primus_Boss_0` (the room), `Mon_Primus_BossPrimusAeron`
(the Lone King himself) and `Room_BossArena` (the arena-area marker).

`WorldNodeData` carries both a `room` and a `roomOverride`, and travel loads the
override when set:

```csharp
string sceneName = string.IsNullOrEmpty(nodes[s.to].roomOverride)
    ? nodes[s.to].room
    : nodes[s.to].roomOverride;
```

`SetRoomOverride(nodeIndex, room)` is a public server method that writes back
into the `nodes` SyncList, so the change replicates and unmodded clients load
the same room.

Two reasons no boss appears: the node keeps its original **type** (only the room
asset changes, so no `ExitBoss` encounter is triggered), and the population
multiplier is zero anyway. Travel to a normal node rather than the zone's own
boss node.

The current node is skipped deliberately — the game itself warns that
overriding the room you are standing in is "prone to bugs". So players start in
the normal first room and reach the arena on their first travel.

### Choosing PvP

PvP is a game mode, not a checkbox. `UI_Title_CreateLobbyView.OnShow` rebuilds
the Game Mode dropdown each time the screen opens, adding `(label, value)` pairs
for `"ShapeOfDreams"` and `"Limbo"`; a postfix appends a third.

PvP layers on top of a normal run, so it leaves `addedGameMods` empty — unlike
Limbo, which adds `GameMod_Limbo`. The view's own handler already clears that
list for any non-Limbo value, so selecting PvP resets cleanly. A postfix on
`UpdateUIState` keeps arena mode and the description text in step, writing only
on an actual change since that method runs constantly.

### Camp and arena

A run opens in the camp and PvP begins past the rift.

World generation always makes node 0 `WorldNodeType.Start` with a room from the
zone's `startRooms` — the campfire a run normally opens in. That node is left
untouched; **every other node** is redirected to the arena. So taking the rift
out of camp is what starts a match, whichever node you pick.

`ArenaPhase` watches `ZoneManager.currentNodeIndex` and flips relations to match:

| where | relations |
|---|---|
| camp (`WorldNodeType.Start`) | everyone allied, regardless of team mode |
| anywhere else | the configured team mode |

So camp works as a between-rounds lobby: regroup, sort builds out, head back
through the rift. Returning to camp disarms PvP again. Late joiners are placed
on the correct side of that line too.

The index is a single synced int, so it is polled rather than hooked to a
transition event — no dependency on the exact ordering of the travel pipeline.

### Leaving camp

Stock behaviour opens the world map and asks which node to travel to, which is
pointless when every node is the same arena. `Rift_RoomExit` already supports a
direct route — its interaction checks `nextNodeIndex` before anything else:

```csharp
if (nextNodeIndex >= 0) { confirm -> CmdTravelToNode(nextNodeIndex); return; }
if (currentNode.type != ExitBoss && !forceTravelToNextZone) { show world map; return; }
```

Setting it gives the boss-room feel: one prompt, then straight into the fight.
The prompt text itself is swapped for a PvP-themed one by intercepting the
`InGame_Message_RiftMoveToNextLocation` lookup in `DewLocalization.GetUIValue` —
far less invasive than reimplementing the rift's interaction, and it leaves
every other string alone. It is a SyncVar, so the host setting it is enough.
A connected non-camp node is preferred, with any non-camp node as a fallback.

### Removing PvE

Zeroing the monster population budget stops waves, but not everything:

- **Props** — wells, gold and dream-dust stones, merchants and shrines come from
  `RoomProps.OnRoomStartServer`, an entirely separate path that the population
  budget never touched. A prefix skips it in the arena.
- **The boss and its hazards** — a boss room places these as part of the room
  itself, so they appear regardless of the spawn budget.

Anything that still reaches the arena is swept once a second, in two passes:

- `ActorManager.allEntities` — every `Monster` (bosses included, since
  `BossMonster` derives from it) and every `PropEntity`.
- `ActorManager.allActors` — chests (`Treasure`) and loose items, which are
  Actors rather than Entities so the first pass misses them.

Items are matched through `IItem`, which also covers a hero's own equipped
skills — so only genuinely unclaimed ones are taken: `handOwner`, `owner` and
`tempOwner` must all be empty. Getting that wrong would strip the players' kits.

Heroes, summons and rifts are left alone throughout, and `Destroy()` is used
rather than `Kill()` so nothing drops rewards or opens the exit rift.

**Room modifiers** are stripped from arena nodes as the room override is
applied. Rolls like Accelerated Time change everyone's movement and attack
speed, which has no place in a match meant to be even. `RemoveModifier` is used
rather than editing the list in place, since `WorldNodeData` is a struct inside
a SyncList and must be written back to replicate.

### Hidden vs actually removed

Worth being precise about, since a hidden effect is still an effect:

| thing | treatment |
|---|---|
| Room modifiers (Accelerated Time, …) | **removed** — stripped from the node before the room ever loads, so they never apply |
| Monsters, boss, props, chests, items | **removed** — blocked at spawn and swept every second |
| Rescue quests from knockouts | **removed** — `Se_HeroKnockedOut.disableQuest` set before it can fire |
| The run's Goal quest | **hidden only** — still tracking, see below |

The Goal is the one thing suppressed purely visually, through a postfix on
`DewQuest.IsVisibleLocally`. `DewQuest` is wired into run progression, and
completing or removing the main goal could end the run — which is exactly what
this mod exists to prevent. It tracks "move deeper into The Rapids" quietly and
has no effect on a match.

Knockout quests are a different matter and are genuinely prevented. A second
after any hero goes down, `Se_HeroKnockedOut` starts a `Quest_LostSoul` and
drops a `RoomMod_HeroSoul` modifier onto a node. With constant knockouts that
would mean a stream of rescue quests, plus modifiers pushed back onto nodes this
mod had already stripped. Both are gated behind the effect's own public
`disableQuest` flag, so setting it stops them outright.

The camp is deliberately untouched — it should still look and behave like a
normal starting room.

### Round start

Three things happen as players enter the arena, all hung off one postfix on
`DewHeroesTeleporter.TeleportAllHeroes` so they cover both routes in — walking
onto the entry trigger and the `arena_start` command:

- **No intro cutscene.** A prefix on `DewCutsceneDirector.PlayNetworked`
  suppresses the RPC, removing the "press space to skip" wait between rounds.
  The entry routine still runs its fade and teleport, so the flow is unchanged.
- **Spread spawns.** Stock teleport drops everyone in a tight line. Players are
  redistributed evenly around a ring, sized from `Room_BossArena`'s own centre
  and radius and snapped to walkable ground with
  `Dew.GetValidAgentDestination_LinearSweep`.

No spawn immunity is granted: players arrive from the safety of camp, so nothing
is hostile until they are already through the rift.

### Starting a match

A boss room has two states. You arrive in an antechamber, and when every hero
stands in the entry zone the fight *triggers*: a cutscene plays and
`DewHeroesTeleporter.TeleportAllHeroes()` drops everyone into the sealed arena
proper. That triggered state is the PvP arena.

The wiring is `DewBossRoomEntry`, which requires a `DewAllHeroesPresentZone` and
a `DewHeroesTeleporter`. Walking onto the trigger together still works normally;
`arena_start` calls the same public teleport directly so you can start without
herding everyone onto the zone.

The boss is swept for once a second and removed with `Destroy()` rather than
`Kill()` — killing it would run the defeat path, drop rewards and open the exit
rift.

### Revive

Knocked-out heroes never recover on their own, and this mod blocks the run from
ending, so without revives a free-for-all stops after one death each.
`Se_HeroKnockedOut.Revive(float)` sets health and destroys the effect; it is
found via `hero.Status.GetStatusEffect<Se_HeroKnockedOut>()`. Default is 10
seconds at 100% health, both configurable.

## Configuring it

Host only, three ways, all the same settings:

- **Create Lobby → Game Mode** — "PvP Arena" sits beside Shape of Dreams and
  Limbo. This is the intended way in.
- **In lobby** — a "Dream Arena" button is injected into the lobby settings panel
  for the rest of the options.
- **Console** (Developer Mode, F1): `arena`, `arena_on`, `arena_off`,
  `arena_mode <FreeForAll|TwoTeams|Peaceful>`, `arena_room <Room_...>`,
  `arena_revive <seconds>`, `arena_start`, `arena_purge`, `arena_status`.
- **Mod manager** — the Config button, for persisted defaults.

The in-lobby window generates its widgets from `DreamArenaConfig` via
`DewGUI.CreateWidgetsForObject` — the same call the mod manager uses. Adding a
field to that class adds a widget in both places automatically; bools become
toggles and the enum becomes a dropdown with no custom UI code.

Settings persist to
`<persistentDataPath>/QuickSave/Mods/com.0x42697262.dreamarena/DreamArenaMod.config.json`.

## Layout

```
src/
  DreamArenaMod.cs        entry point, config, console commands, lifecycle
  DreamArenaConfig.cs     settings (ModConfig; also drives the lobby window)
  TeamMode.cs             FreeForAll / TwoTeams / Peaceful
  RelationController.cs   the PvP core: writes the relation SyncLists
  ArenaRules.cs           no game over, no monsters, arena room override
  ArenaStage.cs           boss removal and teleport into the arena
  ArenaSpawn.cs           ring placement inside the arena
  ArenaPhase.cs           camp/arena tracking, switches hostility
  PveSuppressor.cs        sweeps monsters, props and hazards
  CampRift.cs             points the camp rift straight at the arena
  ReviveController.cs     auto-revive timers for knocked-out players
  ArenaConfigWindow.cs    host's in-lobby settings window
  InjectedUi.cs           teardown for live reload
  ModIdentity.cs          names stamped on created objects
  Patches/
    CreateLobbyModePatch.cs  PvP Arena in the Create Lobby game mode dropdown
    RoundStartPatch.cs    skips cutscenes, spreads spawns
    PveSuppressionPatch.cs  blocks room prop spawning
    HudPatches.cs         PvP rift prompt, hides objectives
    WorldGenPatch.cs      applies the arena room once the world exists
    GameEndPatch.cs       blocks WrapUpAndShowResult
    LobbySettingsPatch.cs injects the lobby button
```

## Building

The game and SDK live in the incus container `chen`. This directory is
bind-mounted to `Shape of Dreams/Mods/DreamArena` (incus device `dreamarena`),
so editing here is what the compiler and game both see.

```sh
./build.sh          # Debug
./watch-log.sh      # tail the game log, filtered
```

## Testing

1. Developer Mode on, load the mod from the title screen.
2. Host a lobby, open **Dream Arena** from lobby settings, tick **Arena mode**, Apply.
3. Start the run. You land in the camp — hitting each other there does nothing.
4. Take the rift out of camp — one prompt, no map. You arrive in the Zenith,
   empty of PvE, and PvP is live.

`arena_status` reports the live state. Expect this in the log:

```
[DreamArena] FreeForAll applied to N players (M hostile pairs).
[DreamArena] rules applied: gameOverEnabled=False, populationMultiplier=0
[DreamArena] arena room 'Room_Primus_Boss_0' applied to N node(s).
[DreamArena] revived <hero> at 100% health.
```

Nameplates turning hostile on an unmodded client is the signal that the
server-authoritative half is working.

## Status: proof of concept

Verified by reading the game's code, **not yet play-tested**.

Confidence, highest first:

- **Relations, no-game-over, monster suppression** — mechanisms read directly
  from the decompiled source; the values are plain server-side state.
- **Lobby button injection** — defensive and logged; if the lobby layout differs
  it logs a warning and the `arena` console command still works.
- **Arena room + revive** — both use public server APIs (`SetRoomOverride`,
  `Revive`) that the game calls itself, but the timing is unproven: the room
  override is applied one frame after the game manager starts, and revive runs
  off a per-frame scan of `ActorManager.allHeroes`.

Known gaps, for the next pass:

- **Travelling to the zone's own boss node** would still stage that boss fight.
  Use a normal node.
- **Quests untouched.** Not wired up, per the brief. Quest code does react to
  knockouts, so odd log entries are possible.
- **Teams are join-order based** (even/odd index). No picker yet.
- Zone transitions, revives and rewards all still assume co-op.
