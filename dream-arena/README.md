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
| No monsters | empty arena | `maxAndSpawnedPopulationMultiplier = 0` |
| Pure white dream | white arena look, no boss | `RoomMod_PureDream` on the current node |

`WrapUpAndShowResult` is the single funnel every run-ending path goes through
(`CheckGameOver`, `ConcludePureWhiteDream`, `ConcludeUnknownFate`), which is why
blocking it there is stronger than clearing `isGameOverEnabled` — that only
stops the all-knocked-out countdown. Both are applied.

Win conditions are deliberately absent. This build just holds the game open.

## Configuring it

Host only, three ways, all the same settings:

- **In lobby** — a "Dream Arena" button is injected into the lobby settings panel.
- **Console** (Developer Mode, F1): `arena`, `arena_on`, `arena_off`,
  `arena_mode <FreeForAll|TwoTeams|Peaceful>`, `arena_status`.
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
  ArenaRules.cs           no game over, no monsters, pure white dream
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
- **Pure white dream** — least certain. `RoomMod_PureDream` is a
  `RoomModifierBase` prefab with a blank `roomOverride`, so it should decorate
  the room without swapping it or spawning a boss. But applying a modifier to a
  node that already loaded may not decorate retroactively. Failures are caught
  and logged, never thrown.

Known gaps, for the next pass:

- **No revive.** A knocked-out player stays down. `Se_HeroKnockedOut.Revive(float)`
  is the entry point; the open question is locating that effect on a hero.
- **Quests untouched.** Not wired up, per the brief. Quest code does react to
  knockouts, so odd log entries are possible.
- **Teams are join-order based** (even/odd index). No picker yet.
- Zone transitions, revives and rewards all still assume co-op.
