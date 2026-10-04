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
| Plain arena | lit, no entry trigger, barriers open | forced `Room.isRevisit`, the synced "already cleared" state |
| No PvE | no monsters, props, boss, chests or loose items | blocked `RoomProps`, plus an actor sweep |
| Clean HUD | no Goal, no room modifiers, mode always shown | hidden quests, stripped node modifiers, replaced location text |
| No lucid dreams | PvE progression layer stripped | blocked `AddLucidDream`, cleared available list |
| No game modifiers | Obliviax, Mirage Skin, Golden Lizard… | cleared `addedGameMods`, swept `GameModifierBase` |
| Direct rift | camp exits straight into the arena | `Rift_RoomExit.nextNodeIndex` |
| Auto revive | knocked-out players get back up | `Se_HeroKnockedOut.Revive(mult)` |
| Camp remnants | ten free pick-anything remnants | spawned `ChoiceShrine`s, offering overwritten |
| Max player level | everyone at the cap in camp | `Hero.maxLevel` |
| Starting memory level | every equipped memory, identity included, at least level 10 | `SkillTrigger.level`, as the Upgrade Well sets it |
| Hero health multiplier | every hero's max health x100 by default | a `finalStatsProcessors` entry, applied after all bonuses |

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

### Camp remnants

Ten remnants ring the camp rift, with an opening left toward the camp to walk in
through — **memories down one half, essences down the other** — one per rarity,
running Common to Unique, with the two Commons flanking the opening and the
Uniques meeting at the back. Everything is free and never runs out, so a build is
chosen rather than farmed and two players can take the same thing.

They are the game's own Retrospection and Enlightenment remnants
(`Shrine_Retrospection`, `Shrine_Enlightenment`) rather than merchants. A
remnant opens a pick-one window whose contents are per player and written by
the server, which is exactly the shape this needs; a merchant's window is a
purchase flow built around gold and stock counts that mean nothing here.

A postfix on each `OnPopulateChoices` replaces the usual three random picks
with every item of that remnant's bracket, at a fixed level or quality. They
are configured on spawn to be permanent fixtures:

| field | value | why |
|---|---|---|
| `maxTotalUseCount` | `-1` | the game's own no-limit value |
| `maxUseCountPerPlayer` | `-1` | same, per player |
| `useCooldown` | `0` | the cooldown is shared, so one pick would lock everyone out |
| `baseGoldCost` | `0` | gold is PvE progression |

`choices` is a `SyncDictionary` keyed by player guid and is never cleared after
a pick, so a remnant can be used again immediately.

**Why split by rarity rather than one remnant per kind.** The choice window is
a `GridLayoutGroup` with no scrolling, so a remnant holding every memory in the
game would spill off screen. A single rarity is a few dozen entries. Remnants
the world generates are matched by instance id and left completely alone.

**Placement is rigid.** The circle is centred on the rift. Slot positions come
from exact circle maths and are never nudged sideways; only their height is taken
from the ground. The solver tries a radius and rotation and accepts it only when
*every* slot lands on walkable ground, so the camp gets a clean ring or none at
all. Radii are tried widest-first, and for each the whole ring is turned by small
amounts to dodge a blocked slot while keeping the opening near the camp. Each
remnant faces inward, so a player standing inside the ring sees their fronts.

The ring is centred on the rift and never moved, rather than placed off to one
side: `Rift_RoomExit` is a scene object with no position syncing, so relocating
it host-side would leave clients interacting with thin air. The remnants go
around it instead.

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

### Plain arena

A boss room loads in its pre-fight state, and its intro cutscene brings the
lights up. `Room_DarkCave_Boss_0` shows this most clearly:
`DarkCaveCutSceneFogFadeOut` sets the boss fog to full black on load, and
`BossZoneLights` starts off. Only the cutscene's timeline signals clear them.
With the cutscene suppressed, the room stayed pitch black.

Lighting, fog and post-processing are drawn locally from the scene on each
client, so the host cannot restyle them for unmodded players. But the game has
a synced state for "this boss room is done": `Room.isRevisit`. Rooms read it on
every client to present themselves as cleared. Dark Cave fades its fog
instantly, and the server fires `BossSeekerKillBehavior`, which brings the
sunlight in. Barriers open, and props, monsters and rewards are skipped.

`ZoneManager` sets the flag in the scene-load callback, just before
`NetworkServer.SpawnObjects` runs `Room.OnStartServer`. A postfix there
overrides it before any client receives the room. The same postfix switches
off the server-only entry trigger (`DewAllHeroesPresentZone`), and a postfix on
`StartRoom` opens any barriers. A cleared room never opens its own exit rift,
and that is left alone on purpose: there is nothing to leave the arena to.

How lit a room ends up depends on how that room scripts its revisit, so rooms
can still differ. Room by room, from each scene's own scripts:

| room | revisit lighting | hazards | notes |
|---|---|---|---|
| DarkCave_Boss_0 | fog clears, sunlight in | – | verified in play |
| LavaLand_Boss_0 | none scripted | lava | entry trigger disabled |
| Sky_Boss_0 | none scripted | damaging light fog | entry trigger disabled |
| Ink_Boss_0 | none scripted | boss-spawned only | entry trigger disabled |
| Forest_Boss_0 | none; day/night is per-client | – | barrier opened |
| SnowMountain_Boss_0 | none | – | barrier opened |
| Despair_Boss_0 | none; fog follows the camera | – | – |
| Primus_Boss_0 | none | – | ending scripts only fire on the boss's death |

Floor hazards are left on on purpose, as a bit of environmental challenge. Both
run on the server (`LavaLand_Lava`, `Sky_LightFogDamager`), so they could be
switched off host-side if that is ever wanted.

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
- `ActorManager.allActors` — shrines, chests (`Treasure`), artifacts, traps and
  loose items. These derive from `Actor` rather than `Entity`, so the first
  pass misses them entirely.

  `Rift` is deliberately spared — it is the way out of the room. So are
  `AbilityTrigger`, `AbilityInstance`, `GameEffect` and `ServerActor`, which
  are the players' own kit and the machinery running the match.

Only five things derive from `Entity` — `Hero`, `Summon`, `Monster`,
`PropEntity` and `MockEntity` — so the entity pass covers everything a hero
could legally auto-attack except other players. Summons are judged by ownership
rather than skipped outright: a monster's summon outlives the monster being
destroyed, stays hostile, and is a perfectly good auto-attack target with
nothing obvious on screen. Players' own summons survive.

Items are matched through `IItem`, which also covers a hero's own equipped
skills — so only genuinely unclaimed ones are taken: `handOwner`, `owner` and
`tempOwner` must all be empty. Getting that wrong would strip the players' kits.

### Diagnosing phantom targets

`arena_scan` reports three things: the arena anchor it would use, every entity
with the facts that decide targeting — type, owner, relation, active state, and
any untargetable/invisible/invulnerable flags, split into attackable and not —
and finally a breakdown of every actor in the room by type.

That last part matters. Most room furniture derives from `Actor` and not
`Entity`, so an entity-only listing will happily report an empty room while
shrines are plainly still standing in it. The entity list answers "what is
drawing fire"; the actor list answers "what is still here".

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
| Lucid dreams | **removed** — never added, and the offer list is emptied |
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

### Game modifiers

`GameSettingsManager.OnStartServer` does more than copy the lobby's choices. It
walks every `GameModifierBase` in the game and adds any that is not excluded
from the pool:

```csharp
foreach (GameModifierBase item in DewResources.FindAllByType<GameModifierBase>(...)) {
    if (Dew.IsGameModifierIncludedInGame(text) && !Dew.IsExcludedFromPool(text)
        && !addedGameMods.Contains(text))
        addedGameMods.Add(text);
}
```

So every run silently carries Obliviax, Mirage Skin, Golden Lizard and Corrupted
Chaos — PvE content generators, and exactly the sort of thing that keeps
producing entities in a supposedly empty arena. The list is cleared in a
postfix so the actors are never created, and `PveSuppressor` also sweeps
`GameModifierBase` in case manager start order puts creation first.

`DewQuest` shares the `GameEffect` base, so the sweep matches the modifier type
specifically rather than the base — otherwise it would take the run's own goal
with it.

### Lucid dreams

A PvE progression layer: run-wide modifiers players bring in or pick up from
shrines. `GameManager.GetLucidDreams()` is the single place they become actors,
so anything left in `activeLucidDreams` is genuinely in effect — hiding them
would not do.

`AddLucidDream` is refused outright, whatever offered it, and
`UpdateAvailableLucidDreams` is emptied as it is rebuilt so nothing is
selectable in the lobby. The game recomputes that offer list whenever the roster
changes, so clearing it once would not hold.

PvP also gets **its own settings save slot**, `dreamarena_pvp`.
`PlayLobbyManager.StartGame_Imp` writes the lobby's settings back to whichever
slot is active — difficulty, banned items and lucid dreams included. Sharing the
Shape of Dreams slot would mean one PvP match overwriting the host's normal-run
lucid dream selections with an empty list. Limbo already solves this with its
own `"limbo"` key; PvP does the same.

### HUD

The location readout, normally "1. Forest of Turbulence", is replaced with the
match type — "PVP: Free For All", or "PVP: Camp (safe)" while nobody can be
hurt. `UI_InGame_Location` only rewrites its text on room load, so the label is
also refreshed whenever the phase or the configured mode changes.

### Round start

Three things happen as players enter the arena, all hung off one postfix on
`DewHeroesTeleporter.TeleportAllHeroes` so they cover both routes in — walking
onto the entry trigger and the `arena_start` command:

- **No intro cutscene.** A prefix on `DewCutsceneDirector.PlayNetworked`
  suppresses the RPC, removing the "press space to skip" wait between rounds.
  The entry routine still runs its fade and teleport, so the flow is unchanged.
- **Staged into the arena.** A boss room is two spaces: an antechamber you land
  in, and the arena proper, which the game only puts you in once every hero
  stands on the entry trigger. Left alone, players arrive with PvP already live
  and fight in the corridor — `Room_SnowMountain_Boss_0` has a notably long
  approach. So arrival is staged directly: everyone is placed inside the arena
  on entry, then ringed evenly and snapped to walkable ground with
  `Dew.GetValidAgentDestination_LinearSweep`.

  Boss rooms are inconsistent about marking their arena, so three anchors are
  tried in order:

  | anchor | notes |
  |---|---|
  | `Room_BossArena` — real centre and radius | only LavaLand, Primus and Despair name one |
  | `DewHeroesTeleporter.destination` | where the game itself drops heroes to start a fight |
  | largest `RoomSection` | boss rooms are an approach plus a big open space, so the widest section is the arena; `pathablePivot` is a known-walkable centre and the radius follows from its area |
  | heroes' own midpoint | last resort, spread only |

  Staging waits for one of the first three: the midpoint fallback would just
  shuffle people around the antechamber they are trying to leave.

  The chosen anchor is logged on every placement, and `arena_scan` reports it,
  so a room that still fails can be identified rather than guessed at.

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

### Hero health

Max health is multiplied, 100x by default, for longer fights that reward play
over the first burst. Health is calculated on the server
(`EntityStatus.CalculateStats`) and replicated as the `finalStats` SyncVar, so
unmodded clients and their health bars see it too.

The multiplier is a `finalStatsProcessors` entry, which runs at the end of the
stat pipeline, after level scaling and every item bonus, so it scales the
finished total, a true multiply. A percentage stat bonus would not be: bonus
percentages are summed and applied once, so the mod's share would dilute what
the player built. Each hero gets the processor once; it is taken back off when
the multiplier changes, when arena mode is turned off, or when the mod unloads.
`CalculateStats` preserves each hero's current/max health fraction across a
recalculation, so the change scales current health with the maximum instead of
leaving someone at a sliver or overfull.

## Configuring it

Host only, three ways, all the same settings:

- **Create Lobby → Game Mode** — "PvP Arena" sits beside Shape of Dreams and
  Limbo. This is the intended way in.
- **In lobby** — a "Dream Arena" button is injected into the lobby settings panel
  for the rest of the options.
- **Console** (Developer Mode, F1): `arena`, `arena_on`, `arena_off`,
  `arena_mode <FreeForAll|TwoTeams|Peaceful>`, `arena_room <Room_...>`,
  `arena_revive <seconds>`, `arena_start`, `arena_purge`, `arena_scan`,
  `arena_status`.
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
  HeroLeveller.cs         hero level cap and memory level floor
  HeroHealth.cs           max-health multiplier for every hero
  ArenaConfigWindow.cs    host's in-lobby settings window
  InjectedUi.cs           teardown for live reload
  ModIdentity.cs          names stamped on created objects
  Patches/
    CreateLobbyModePatch.cs  PvP Arena in the Create Lobby game mode dropdown
    RoundStartPatch.cs    skips cutscenes, spreads spawns
    PveSuppressionPatch.cs  blocks room prop spawning
    HudPatches.cs         PvP rift prompt, hides objectives
    WorldGenPatch.cs      applies the arena room once the world exists
    PlainArenaPatch.cs    loads the arena in its cleared, lit state
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

### Networking after the Sep 22 game patch

The patch (still labeled `r.1.4.0.13`) reworked lobbies and transports. The
mod's own compiled references are unchanged against the new assemblies, but
the game now treats any modded session differently:

- `DewMod.isAnyModActive` forces the host's **Cross-play off** (and saves that
  setting). The EOS lobby is published with `crossPlayGate = "STEAM"` and
  `CrossplayOptOut`, so only Steam players can find or join it. Anyone on
  another platform gets `CrossPlayMismatch`. This is intended by the game and
  cannot be fixed host-side.
- A host with Steam running uses `MultiplexTransport` (Steam + EOS). Clients
  connect over Steam to the published `steamHost`, then fall back to EOS if
  that fails. Its packet limit is the smaller of the two transports (EOS:
  1170 bytes × fragments).
- The lobby `version` check is only the part before `_`, so any friend whose
  Steam copy has not finished updating will fail it.

Host-side lag matters more than it used to, because the host is the server:
a slow host frame stalls every client and can trip EOS peer-inactivity kicks
during joins. For that reason:

- Per-frame code reads managers through `NetworkedManagerBase<T>.softInstance`,
  never `.instance`. When the manager is missing, `.instance` runs two
  whole-scene `FindObjectOfType` searches (the second includes inactive
  objects). `ZoneManager` and `ActorManager` do not exist in the lobby, so
  `.instance` there meant several full scene scans every frame for as long as
  the lobby was open.
- Camp remnant builds and arena staging retry on a timer instead of every
  frame.

Known gaps, for the next pass:

- **Travelling to the zone's own boss node** would still stage that boss fight.
  Use a normal node.
- **Quests untouched.** Not wired up, per the brief. Quest code does react to
  knockouts, so odd log entries are possible.
- **Teams are join-order based** (even/odd index). No picker yet.
- Zone transitions, revives and rewards all still assume co-op.
