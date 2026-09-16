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
| Force arena room | every travel lands in the arena | `ZoneManager.SetRoomOverride` |
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

### Revive

Knocked-out heroes never recover on their own, and this mod blocks the run from
ending, so without revives a free-for-all stops after one death each.
`Se_HeroKnockedOut.Revive(float)` sets health and destroys the effect; it is
found via `hero.Status.GetStatusEffect<Se_HeroKnockedOut>()`. Default is 10
seconds at 100% health, both configurable.

## Configuring it

Host only, three ways, all the same settings:

- **In lobby** — a "Dream Arena" button is injected into the lobby settings panel.
- **Console** (Developer Mode, F1): `arena`, `arena_on`, `arena_off`,
  `arena_mode <FreeForAll|TwoTeams|Peaceful>`, `arena_room <Room_...>`,
  `arena_revive <seconds>`, `arena_status`.
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
  ReviveController.cs     auto-revive timers for knocked-out players
  ArenaConfigWindow.cs    host's in-lobby settings window
  InjectedUi.cs           teardown for live reload
  ModIdentity.cs          names stamped on created objects
  Patches/
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
3. Start a run with at least two players and hit each other.

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
