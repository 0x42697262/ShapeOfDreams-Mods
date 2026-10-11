# 07. Structuring a mod that survives real play

A mod that works in a five-minute test can still fail in real play. Real play
means a game update that renames a private method, a co-op lobby where clients
run the vanilla game, a restart after game over, a mod reload in the middle of
a fight, 300 monsters alive at once, and an unload that has to leave the game
as it found it. This chapter teaches one design method for all of that: list
the forces that can break the mod, then give every piece of code a layer
chosen by which forces reach it.

Last Stand is the only worked example. It is a host-only wave mode of about
140 C# files, and every rule below comes from a failure it met or avoided.
Hooks are covered in `03-hooking-with-harmony.md` and sync in
`04-host-only-networking.md`. Settings UI and console commands are in
`08-config-ui-and-commands.md`, and log reading is in
`09-debugging-and-testing.md`. This chapter is about how the pieces fit
together.

---

## 1. Start from the forces, not from the features

Before writing a folder layout, list what can break the mod at runtime. Each
force puts one requirement on the code. Last Stand's list:

| Force | What breaks | Architectural answer | Section |
|---|---|---|---|
| Game updates | Patched signatures and private names change | Thin patches, one file per hook, game names in one file | 3.1, 9 |
| Host-only play | Plain fields set on the host never reach clients | Adapters that write synced state; one source of truth | 3.3, 8 |
| Restarts and new runs | Mod state leaks into the next run, or the mode is forgotten | A mode flag the game itself carries; reset on a new `GameManager` | 4, 2 |
| Per-frame cost | Scene scans or allocations every frame lag the host | One tick, throttled retries, `softInstance`, caps | 5 |
| Exceptions | A throw in `Update` floods the log; a throw in a patch breaks the game's call | Error throttle, fail-safe returns, try/catch at fragile edges | 6 |
| Unload | Patches, multipliers, modifiers and UI left behind | An undo for every write, called from `OnDestroy` | 6.3 |
| Mod reload | The old assembly stays alive; in-memory state is lost | Cross-reload slots and rebuilding ownership from the world | 7 |
| Growth | Every new feature touches control flow | Rules as data, features added through config and plans | 11 |

Every later section answers one row. When you design a new mod, write this
table first. A row with no answer is a bug you will hit in play.

---

## 2. The entry point is a wiring board

### 2.1 What the loader does with your assembly

Read `DewMod` in the decompiled `Dew.Core` (see `02-reading-the-game.md` for
how to decompile). The load loop, shortened:

```csharp
byte[] array = File.ReadAllBytes(text2);
using (AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(array)))
{
    AssemblyNameDefinition name = assemblyDefinition.Name;
    name.Name = name.Name + "_" + DateTime.Now.Ticks;
    ...
}
foreach (Type item in from t in Assembly.Load(array).GetTypes()
    where t != typeof(ModBehaviour) && typeof(ModBehaviour).IsAssignableFrom(t)
    select t)
{
    ModBehaviour modBehaviour = (ModBehaviour)gameObject.AddComponent(item);
    modBehaviour.instance = loadedModInstance;
    if (modBehaviour.modConfigFields.Length != 0)
        modBehaviour.LoadConfigsToDisk();
    ...  // then registers every method marked [ConsoleCommand]
}
```

This snippet sets four architectural facts:

1. Every concrete `ModBehaviour` in the DLL becomes a component on one
   container `GameObject`. Last Stand has two: `LastStandMod` (lifecycle) and
   `LastStandConsole` (commands). They talk through a static
   `LastStandMod.Current`.
2. `LoadConfigsToDisk()` runs right after `AddComponent`, before Unity calls
   `Start`. So the config is already loaded when `Start` builds anything from
   it.
3. The assembly is loaded **from bytes, under a new name every time**. Section 7
   covers what that does to a reload.
4. The `harmony` property builds its id as
   `mod.metadata.id + Guid.NewGuid().ToString()`, so each loaded instance has
   its own id. Unpatch with `harmony.UnpatchAll(harmony.Id)`. Calling
   `UnpatchAll()` with no id removes every mod's patches.

### 2.2 Last Stand's `ModBehaviour`

`src/LastStandMod.cs` holds no rules. It builds the session, hooks the game
and tears everything down. The lifecycle is the same for Dream Arena, so it
lives in the shared library's abstract `ModEntry`
(`library/src/ModEntry.cs`), which `LastStandMod` subclasses:

```csharp
protected void Start()
{
    instance.isAlteringGameplay = true;
    OnLoaded();
    ChoiceFields.Register();
    harmony.PatchAll();
    CallOnNetworkedManager<GameManager>(_ => OnGameStarted());
    Log.Info(SharedLogText.Loaded(mod.metadata.name, mod.metadata.modVer));
}

protected void Update()
{
    try { OnTick(Time.time); }
    catch (Exception e) { _tickErrors.Report(Time.time, ModText.TickFailed, e); }
}

protected void OnDestroy()
{
    OnUnloading();
    ConfigWindow.Close();
    InjectedUi.RemoveAll();
    LobbyModeOption.Detach();
    ChoiceFields.Unregister();
    harmony.UnpatchAll(harmony.Id);
    if (Active == this) Active = null;
    Log.Info(SharedLogText.Unloaded);
}
```

private void OnGameManagerStarted(GameManager game)
{
    Session?.Reset();
    Session?.UseSettings(SessionSettings.From(config));
}
```

(Shown compressed; the real files use one statement per line.)

`isAlteringGameplay = true` is required for any server-side gameplay mod by
the official modding rules. It shows the MOD icon on the lobby.

### 2.3 Finding a "new run started" hook

A mod needs one moment where it can drop everything from the previous run.
Look at `ModBehaviour.CallOnNetworkedManager<T>(Action<T>)` in
`Dew.Core/ModBehaviour.cs`:

```csharp
public void CallOnNetworkedManager<T>(Action<T> onStart) where T : NetworkedManagerBase<T>
{
    ManagerPatchItem item = new ManagerPatchItem { type = typeof(T), onStart = ... };
    InitManagerPatchItem(item, NetworkedManagerBase<T>.instance);
}
```

`InitManagerPatchItem` runs `onStart` at once if the manager exists. Then find
who else calls the item. `NetworkedManagerBase<T>.OnStartClient` calls
`DewMod.NotifyManagerLifecycle(GetType(), enable: true)`, which runs `onStart`
again for every registered item of that type. The host is also a client, so
`OnStartClient` fires on the host for every new `GameManager`: every new run,
every restart. That makes `CallOnNetworkedManager<GameManager>` the "new run"
hook. Last Stand resets its session there and takes a fresh settings snapshot.

**Where else this applies:** `CallOnNetworkedManager<ZoneManager>` for
per-run world setup; `CallOnManager<T>` for local (non-networked) managers;
the optional `onCleanup` argument for work that has to be undone when the
manager stops (`NotifyManagerLifecycle(..., enable: false)` runs it).

---

## 3. Layers: the dependency question

Last Stand's `src/` is split by concern (`docs/reference/code-layout.md`):

| Folder | Concern |
|---|---|
| `LastStandMod.cs`, `LastStandConsole.cs` | Entry point (a `ModEntry` subclass) and console commands |
| `ModHooks.cs`, `Identity/` | What the shared library needs from Last Stand: dropdown choices, lobby name, tag, save key, object names |
| `Session/` | Run phases (camp, arriving, fighting), arrival, cross-reload progress |
| `Waves/` | Wave schedule, cycle, spawn queue (plain logic) and the director |
| `Combat/`, `Loot/`, `Well/`, `Arena/` | Feature services, their rules and their adapters |
| `Game/` | Thin wrappers over game APIs, and `GameNames` |
| `Config/` | Raw config, validated snapshot, schema version, `Tuning` |
| `Text/` | Every string: chat, tracker, config screen, log, commands, and `ModText` for the library |
| `UI/` | The solo button |
| `Patches/` | Harmony hooks, each only handing off |

What both mods need (`Log` and `ErrorThrottle`, the mode check and selecting
the mode, the settings window and field builders, the lobby buttons, the
Workshop change note) lives in the shared library, `library/src` (see
`library/README.md`), and is compiled into the same DLL.

The folders are by feature. The rules that matter are by **layer**. To place
a piece of code, ask four questions in order:

1. **Does it intercept the game?** Then it is a patch. It holds no rules and
   no state.
2. **Does it decide something from numbers alone** (how many, how much,
   when)? Then it is a rule. It must not touch Unity, Mirror or live game
   objects.
3. **Does it read or write the game** (spawn, drop, set a SyncVar, send
   chat)? Then it is an adapter. It does what it is told and decides nothing.
4. **Does it hold state over time and call rules and adapters?** Then it is
   a service, owned by the session.

Config, lifecycle and UI each sit beside these four.

### 3.1 Patches: five lines and a hand-off

Every file in `src/Patches/` has the same shape: check the mode, find the
session, call one method. `KillGoldPatch`:

```csharp
[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetKillGoldAmount))]
internal static class KillGoldPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref int __result)
    {
        if (__result <= 0 || !ModMode.IsHosting()) return;
        LastStandSession session = LastStandMod.Current?.Session;
        if (session != null) __result = session.CapKillGold(__result);
    }
}
```

`RoomClearPatch` is one line of logic:

```csharp
[HarmonyPatch(typeof(Room), nameof(Room.ClearRoom))]
internal static class RoomClearPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !ModMode.IsHosting() || LastStandMod.Current?.Session?.AllowsRoomClear() != false;
    }
}
```

Why this shape:

- **Game updates hit patches first.** The patch target is the most
  version-fragile point in a mod: a renamed method or a changed parameter
  breaks it. When the patch is five lines, an update breaks one small file,
  and the fix never touches a rule.
- **The fallback is vanilla.** Every patch passes through when the mode is
  off (`!IsHosting()` returns `true` from a prefix, or leaves `__result`
  alone). `?.` plus `!= false` means "no session" also falls back to vanilla.
  That covers the frames during a reload when `Current` is null.
- **The decision is testable elsewhere.** `CapKillGold` calls
  `KillGoldRule.Cap(amount, max, sharers)`, a pure function.

A patch that calls into fragile code wraps it, so a failure costs one
feature and not the game's call (section 6.1). `HeroSoulPatch` is the
pattern:

```csharp
[HarmonyPrefix]
private static bool Prefix(Se_HeroKnockedOut __instance)
{
    if (!ModMode.IsHosting()) return true;
    try { HeroSouls.Suppress(__instance); }
    catch (Exception e) { Log.Error(LogText.HeroSoulFailed, e); }
    return false;
}
```

### 3.2 Rules: plain functions of numbers

A rule takes plain inputs and returns a value. It doesn't read the scene, the
network or the clock. Last Stand's rules:

| Rule | Signature (shortened) | Decides |
|---|---|---|
| `WaveSchedule.Plan` | `(int wave, int players, WaveSettings, WaveContent) → WavePlan` | Monster counts per tier, caps, stat bonus, hunt level, environment |
| `WaveCycle` | `TryStartWave(now)`, `TryFinishWave(now, fieldClear, breakSeconds)` | When a wave starts and ends |
| `SpawnQueue` | `Release(now, alive, maxAlive, interval, perRelease, finaleDelay)` | Which tiers to spawn this frame |
| `FastClearRule` | `MultiplierFor(stacks)`, `KillsNeeded(monsters)` | Penalty for clearing too fast |
| `GoldPriceRule` | `MultiplierAt(wave) → float` | The shared gold price multiplier |
| `LootRules.Decide` | `(TierLoot, LootSettings, wave, itemRoll) → LootDecision` | Item rarity, dust and gold per kill |
| `CurseGoalRule.Kills` | `(waveMonsters, players, wavesOfKills) → int` | A curse's kill goal |
| `Cadence` | `IsOn(wave)`, `OccurrencesSoFar(wave)` | "Every N waves from wave F" |

Two complete examples:

```csharp
internal static class CurseGoalRule
{
    public static int Kills(int waveMonsters, int players, double wavesOfKills)
    {
        double perPlayer = waveMonsters / (double)Math.Max(1, players);
        return Math.Max(Tuning.MinimumCurseKills, (int)Math.Round(perPlayer * wavesOfKills));
    }
}
```

```csharp
public float MultiplierAt(int wave)
{
    if (FreeShrines) return Free;
    int lastGrowingWave = UntilWave == NeverStops ? wave : Math.Min(wave, UntilWave);
    int steps = Math.Max(0, Steps.OccurrencesSoFar(lastGrowingWave));
    return (float)Math.Pow(1 + GrowthPerStep, steps);
}
```

The method for turning a feature into a rule:

1. Write the decision as one sentence with numbers: "a curse ends after the
   chooser's share of the wave's monsters, times a strength factor".
2. Every value that comes from the game becomes a parameter. The player count
   comes from `PlayerRoster.PlayerCount()` and the wave size from the plan.
   The **caller** reads them and passes them in.
3. Randomness becomes a parameter too. `LootRules.Decide` takes
   `double itemRoll` and doesn't call `Random`. The service rolls and passes
   the roll in.
4. Time becomes a parameter. `WaveCycle` and `SpawnQueue` take `double now`
   and never read `Time.time`.
5. Settings come in as an immutable snapshot (`WaveSettings`, `LootSettings`),
   never as the raw config.

What you get:

- **You can check it by arithmetic.** With 5% growth per wave, wave 10 gives
  `1.05^10 = 1.63`, and the log line `gold prices: multiplier set to 1.63 at
  wave 10` confirms the live value matches the rule.
- **It can be tested alone.** The rule files import only `System` and other
  rule types. Linking them into a plain `dotnet test` project compiles without
  the game. Last Stand ships no test project; it checks its rules through
  logged plans (section 10).
- **It survives game updates.** A rule never breaks because the game moved a
  field.

Check the layer with a grep. It should print nothing:

```sh
grep -ln "using UnityEngine\|using Mirror" \
  src/Waves/WaveSchedule.cs src/Waves/WaveCycle.cs src/Waves/SpawnQueue.cs \
  src/Waves/Cadence.cs src/Waves/FastClearRule.cs src/Loot/GoldPriceRule.cs \
  src/Loot/LootRules.cs src/Combat/CurseGoalRule.cs
```

Game enums such as `Rarity` are allowed in a rule (`LootRules` returns a
`Rarity?`). They are plain values. Game objects and managers are not.

### 3.3 Adapters: wrap one game API, decide nothing

An adapter turns "set the price multiplier to x" into the game's API, safely.
`src/Game/GoldPrices.cs`:

```csharp
public void Set(float multiplier, int wave)
{
    GameManager game = NetworkedManagerBase<GameManager>.softInstance;
    if (game == null || !NetworkServer.active) return;
    _applied = true;
    if (game.goldCostMultiplier == multiplier) return;
    game.NetworkgoldCostMultiplier = multiplier;
    Log.Info(LogText.GoldPricesSet(multiplier, wave));
}

public void Restore()
{
    if (!_applied) return;
    _applied = false;
    GameManager game = NetworkedManagerBase<GameManager>.softInstance;
    if (game != null && NetworkServer.active)
    {
        game.NetworkgoldCostMultiplier = Tuning.VanillaGoldCostMultiplier;
        Log.Info(LogText.GoldPricesRestored(Tuning.VanillaGoldCostMultiplier));
    }
}
```

Every adapter in Last Stand follows the same checklist:

1. Get managers through `softInstance`, and do nothing when one is null.
2. Write only on the server (`NetworkServer.active`).
3. Write synced state through the Mirror-generated `Network...` setter, so
   clients receive it (`04-host-only-networking.md`).
4. Compare before writing, and log only on change.
5. Have an undo, and track whether there is anything to undo (`_applied`).

Other adapters: `ChatNotices.Broadcast` (shared library; one
`ChatManager.Message` of type `Raw`), `PlayerRoster` (gold, dust, levels,
live heroes), `MonsterStrength` (ambient level), `WaveTracker` (the quest
tracker line), `ArenaModifier` (add and remove one room modifier by id),
`UseCountPin` (the shrine's private synced use counts).

### 3.4 Services and the session

Services hold state over time. `WaveDirector` owns the cycle, the queue, the
catalog and the per-wave modifiers. `PainService`, `LootService` and
`ArenaShrines` own their features. Each has `Tick` (or event handlers), a
`Reset`/`Forget`, and calls rules and adapters.

`LastStandSession` owns the services and wires them with C# events in its
constructor:

```csharp
_waves = new WaveDirector(_enemies, _heroicBosses, OnEnemySpawned);
_pain = new PainService(_enemies, _waves.HasPending);
_waves.WaveStarted += OnWaveStarted;
_waves.WaveCleared += OnWaveCleared;
_waves.FastCleared += OnFastCleared;
_enemies.Died += OnEnemyDied;
_pain.GroupFell += _waves.RestoreHuntedZone;
```

The services don't know about each other. When wave 12 is cleared, the
director raises `WaveCleared(12)`. The session then saves progress, lets loot
and shrines react, and revives knocked-out heroes. Adding a reaction to "wave
cleared" means one line in `OnWaveCleared`, not a change to the director.

### 3.5 Config as a validated snapshot

The raw config (`LastStandConfig : ModConfig`) is whatever the player typed,
loaded from JSON. Nothing reads it directly except `SessionSettings.From`,
which clamps every value once and builds immutable settings objects:

```csharp
IntermissionSeconds = Math.Max(0f, config.intermissionSeconds),
MaxAliveBase = Math.Max(MinimumStep, config.maxAliveBase),
SpawnsPerInterval = Math.Max(MinimumStep, config.spawnsPerInterval),
WavesPerAmbientLevel = Math.Max(MinimumStep, config.wavesPerAmbientLevel),
```

A negative interval or a zero divisor can't reach a rule. A setting change
calls `OnConfigChanged()` (the mod manager calls it after Apply, see
`DewMod`'s `onAfterApply`). That builds a new snapshot and swaps it in with
`Session.UseSettings`. Mid-run edits take effect on the next tick, and no
service sees a half-edited config.

**Where else this applies:** any mod with more than one feature. A
damage-tweak mod puts its formula in a rule and the `DataProcessorGroup` hookup
in an adapter. A UI mod keeps "what to show" (rule) apart from "which
`TextMeshProUGUI` to write" (adapter).

---

## 4. Mode gating: find a flag the game carries for you

A host-only mode must answer "am I active?" from every patch, every command
and every frame. The answer has to be right after a restart, wrong in a
normal run, and the same on every code path.

### 4.1 The method

You want a value that:

1. is set by the game's own start path, from something your UI controls;
2. lives on the host (synced is a bonus);
3. survives the game's **restart** path (game over, then "restart");
4. is empty in any run your mod didn't start.

Start from the restart path, because it is the strictest test. Search the
decompile for what a restart copies forward:

```sh
grep -n "RestartAsync" Dew.Core/DewNetworkManager.cs
```

Inside `private async UniTaskVoid RestartAsync()`:

```csharp
startSettings.continueData = null;
if (NetworkedManagerBase<GameSettingsManager>.instance != null)
{
    startSettings.addedGameMods = NetworkedManagerBase<GameSettingsManager>.instance.addedGameMods.ToList();
    startSettings.customGameSettingsSaveKey = NetworkedManagerBase<GameSettingsManager>.instance.gameSettingsSaveKey;
}
...
NetworkServer.Shutdown();
NetworkClient.Shutdown();
```

Two values cross a restart: `addedGameMods` and `customGameSettingsSaveKey`.
Next, find where the second one goes on the way in:

```sh
grep -rn "customGameSettingsSaveKey" Dew.Core
```

`GameSettingsManager.Awake`:

```csharp
if (!string.IsNullOrEmpty(DewNetworkManager.startSettings.customGameSettingsSaveKey))
{
    Network_003CgameSettingsSaveKey_003Ek__BackingField = DewNetworkManager.startSettings.customGameSettingsSaveKey;
}
```

It becomes `gameSettingsSaveKey`, a SyncVar. The same grep finds the
precedent in `TitleManager`: Limbo starts with
`customGameSettingsSaveKey = "limbo"`. The game already uses this key as a
mode flag. A third hit, `DewSave.profileMain.GetPreferredGameSettings(gameSettingsSaveKey)`,
shows that lobby preferences are stored per key. So a custom key also keeps
the mode's lobby setup apart from normal runs.

All four requirements hold: the mod sets it on the start settings, the game
copies it into a host-side SyncVar, `RestartAsync` carries it, and a vanilla
start leaves it null.

Other options fail at least one requirement:

- **A static `bool` in the mod.** It fails 3 and 4. It is lost on a mod
  reload (new assembly, new statics), and nothing clears it when the player
  leaves for the title screen and starts a normal run.
- **`addedGameMods`.** It also survives restarts, but `GameManager` resolves
  each entry with `DewResources.GetByShortTypeName<GameModifierBase>(name)`
  and creates that actor. A mod has no game modifier prefab of its own to
  register there. Last Stand clears it
  (`settings.addedGameMods = new List<string>()`).

### 4.2 The implementation

Selecting the mode (`ModeSelection`, now in the shared library,
`library/src/Mode/ModeSelection.cs`) writes the key into the start settings
that both the solo button and the lobby's `PlayGame` use:

```csharp
public static void Select(DewNetworkStartSettings settings)
{
    settings.addedGameMods = new List<string>();
    settings.customGameSettingsSaveKey = ModIdentity.SaveKey;
}
```

Checking it (`ModMode`, `library/src/Mode/ModMode.cs`; each mod's DLL has its
own copy, comparing with its own `ModIdentity.SaveKey`):

```csharp
public static bool IsSelected()
{
    GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
    return settings != null && settings.gameSettingsSaveKey == ModIdentity.SaveKey;
}

public static bool IsHosting() => NetworkServer.active && IsSelected();
```

`IsHosting()` is the first check of every patch, every gameplay console
command and `Session.Tick`. It costs one static property read and one string
compare, so it is safe to call per frame and per patched call. There is no
"enabled" field anywhere in the mod that could disagree with it.

**Where else this applies:** any value you need to outlive a scene change or
restart. Search the restart and continue paths (`RestartAsync`,
`continueData`, `DewNetworkStartSettings`) for fields the game already copies
forward before you invent your own persistence. The same search tells you
what a restart *wipes*: `continueData = null` is why a restart never resumes
a save.

---

## 5. The tick loop

### 5.1 One `Update`, one `Tick(now)`

Last Stand has exactly one `Update`, in `ModEntry` (which `LastStandMod`
subclasses), and it calls `Session.Tick(Time.time)` through `OnTick`. Every
service receives `now` as a parameter. Benefits:

- One place controls order: the tracker updates after the wave state, and
  shrines update after the director.
- One try/catch covers everything (section 6.1).
- Rules and services never read the clock, so they can be driven with any
  `now`.

### 5.2 The phase state machine

`SessionPhase` is `Inactive`, `Camp`, `Arriving`, `Fighting`. The transitions
live in `LastStandSession.Tick`:

```csharp
if (!ModMode.IsHosting())
{
    if (_phase != SessionPhase.Inactive) Reset();
    return;
}

_goal.Tick(now);
_tracker.Tick(now, TrackerProgress(now));
_startingBonus.Tick(_settings.StartingGold, _settings.StartingDreamDust);

if (!ArenaLocator.IsStandingInArena())
{
    if (_phase == SessionPhase.Fighting || _phase == SessionPhase.Arriving) StopFight();
    _phase = SessionPhase.Camp;
    _world.Tick(now, _settings.StartingWorld);
    _campRift.Tick(now);
    return;
}

int node = ArenaLocator.CurrentNode();
if (_phase != SessionPhase.Fighting || node != _arenaNode)
{
    Arrive(now, node);
    return;
}

_waves.Tick(now, _settings.Waves);
TickCurseStart(now);
_pain.Tick(now, _settings.Pain);
_panel.Tick(_waves.HasArenaModifier || _pain.ZoneActive);
Shrines.Tick(now, _waves.ClearedWaves, _settings.AscensionResetWaves, _settings.Waves.CurseToStart);
```

| From | Condition | To | Side effect |
|---|---|---|---|
| any | not hosting the mode | `Inactive` | `Reset()` once |
| any | hosting, not in the arena node or in a transition | `Camp` | `StopFight()` if a fight was on |
| `Camp` | in the arena node, transition over | `Arriving` | `Arrive` attempts |
| `Arriving` | `ArenaArrival.TryArrive` succeeds | `Fighting` | shrines placed, waves begun or resumed |
| `Fighting` | node index changes | `Arriving` | re-arrive |

Every state is **recomputed from the game each frame**, not remembered and
trusted. "In the arena" is
`zones != null && !zones.isInAnyTransition && IsArenaNode(zones, zones.currentNodeIndex)`.
If the game moves the party, the machine follows on the next frame. Game
over, the title screen and a non-host client all fall into the first `if`
and reset.

### 5.3 Waiting for a room to settle: the retry gate

On arrival, the room may exist but not be active yet. Heroes may not be
placed, and section data may be missing. Don't hook a "room ready" event and
hope it fires once. Poll with a retry gate:

```csharp
internal sealed class ArenaArrival
{
    private double _nextAttemptAt;

    public bool TryArrive(double now, out ArenaPoints arena)
    {
        arena = default;
        if (now < _nextAttemptAt) return false;
        _nextAttemptAt = now + Tuning.ArrivalRetrySeconds;

        PlayerRoster.FillLiveHeroes(_heroes);
        if (_heroes.Count == 0 || !ArenaLayout.TryFind(out arena)) return false;

        HeroStaging.PlaceAround(arena, _heroes);
        return true;
    }

    public void Reset() { _nextAttemptAt = 0; _heroes.Clear(); }
}
```

`ArenaLayout.TryFind` starts with
`Room room = SingletonDewNetworkBehaviour<Room>.softInstance; if (room == null || !room.isActive) return false;`.
It is a `Try…` method that returns false while the world isn't ready, and the
gate makes sure the expensive part runs at most twice a second.

Every timed check in Last Stand uses the same gate, each with its interval in
`Tuning`:

| Gate | Interval | What it retries |
|---|---|---|
| `ArenaArrival` | `ArrivalRetrySeconds` 0.5 s | Find the arena layout, stage heroes |
| `CampRift` | `CampRiftRetrySeconds` 1 s | Point the camp rift at the arena node |
| `RunGoal` (shared library) | `SharedTuning.GoalCheckSeconds` 1 s | Remove the vanilla run goal quest |
| `StartingWorld` | `WorldCheckSeconds` 1 s | Apply the starting world index |
| `WaveTracker` | `TrackerRefreshSeconds` 1 s | Write the tracker's progress string |
| `ArenaShrines` | `ShrinePinSeconds` 1 s | Re-pin shrine use counts and locks |
| `FriendlyFire` | `FriendlyFireRefreshSeconds` 1 s | Refresh the co-op curse |

Where to draw the line: a check made of a few `softInstance` reads and null
tests may run every frame. `WaveDirector.Tick` does this for
`_catalog.IsReady` and `_spawnPoints.IsReady`, which fail fast on a null
`Zone` or `Room`. Anything that scans the scene (`FindObjectsByType`),
samples the navmesh or walks all entities goes behind a gate.

### 5.4 `softInstance`, never `instance`, in per-frame code

The decompiled `NetworkedManagerBase<T>`:

```csharp
public static T instance
{
    get
    {
        if (softInstance == null) softInstance = Object.FindObjectOfType<T>();
        if (softInstance == null) softInstance = Object.FindObjectOfType<T>(includeInactive: true);
        return softInstance;
    }
}

public static T softInstance { get; private set; }
```

When the manager exists, both are cheap. When it doesn't (title screen,
loading, between runs), every `instance` read runs **two full scene scans**
and returns null. If it sits in a per-frame path, that is two scans per frame
for as long as the manager is missing, and that lags the host. `softInstance`
is set in `Awake`/`OnEnable` and is just a field read. Every per-frame lookup
in Last Stand uses `softInstance` and treats null as "not ready, try later".

The game itself uses `instance` in one-shot code (`RestartAsync`, above).
That is fine: the rule is about frequency.

### 5.5 Bounding the work per frame

Waves reach hundreds of monsters, so no frame may grow with wave size.

- **Spawns are metered.** `SpawnQueue.Release` hands out at most
  `perRelease` tiers per `intervalSeconds`, and never more than the open
  slots under the cap:

  ```csharp
  int openings = Math.Min(perRelease, maxAlive - alive);
  while (openings > 0 && _pending.Count > 0)
  {
      _released.Add(_pending.Dequeue());
      openings--;
  }
  ```

  `maxAlive` comes from the plan (`MaxAliveBase + MaxAlivePerExtraPlayer ×
  extra players`, scaled on surge waves), so load scales with player count by
  config, not by accident.
- **In-flight spawns count.** `RoomMonsters.SpawnMonsters` runs a coroutine,
  so a monster exists a few frames after the request. `EnemyRegistry.Reserve(now)`
  books a slot that expires after `Tuning.SpawnReservationSeconds` (5 s), and
  `Track` consumes it when the entity arrives. `Occupied(now)` returns alive
  plus reserved, so the cap can't be overshot by spawning into the gap.
- **No per-frame allocation.** Lists are fields that are cleared and reused:
  `SpawnQueue._released`, `EnemyRegistry._stale` and `_ofTier`,
  `ArenaArrival._heroes`. `Release` returns `IReadOnlyList<EnemyTier>` over
  the reused list.
- **Pruning is bounded by the cap.** `EnemyRegistry.Prune` walks only the
  mod's own tracked entities (at most `maxAlive` plus bosses), never
  `ActorManager.allEntities`.

**Where else this applies:** any mod that reacts to the world over time.
Examples: a mod that tags every elite needs a gate around its scan, a
damage-number mod needs a reused buffer, and an "auto-loot" mod needs a cap
on pickups per frame.

---

## 6. Reliability: the host is everyone's server

When the host's mod faults, every player's run breaks. The rules: never throw
into Unity or into the game, fail toward vanilla, and undo every effect.

### 6.1 Never throw from `Update` or a patch

What happens when you do:

- **Throw in `Update`:** Unity catches it, logs it with a stack trace, and
  calls `Update` again next frame. If the cause persists, that is a stack
  trace per frame in `Player.log`, and the rest of that tick never runs.
- **Throw in a Harmony prefix or postfix:** the patch is compiled into the
  replacement method, so the exception goes to the **game's** caller. A room
  load, a death handler or a UI callback aborts halfway.

The fix for `Update` is one try/catch around the tick, with a throttle on the
log. `ErrorThrottle`, now in the shared library
(`library/src/Diagnostics/ErrorThrottle.cs`):

```csharp
internal sealed class ErrorThrottle
{
    private readonly double _intervalSeconds;
    private double _nextAt;
    private int _suppressed;

    public void Report(double now, string message, Exception exception)
    {
        if (now < _nextAt) { _suppressed++; return; }
        Log.Error(SharedLogText.WithSuppressed(message, _suppressed), exception);
        _suppressed = 0;
        _nextAt = now + _intervalSeconds;
    }
}
```

The first error gets logged in full, and then at most one every
`SharedTuning.ErrorLogIntervalSeconds` (5 s), with a count of the ones skipped:
`(N more like it were not logged.)`. The tick's message says what still
works: `session tick failed; the session keeps its wave and carries on.`.
This only holds because the tick is written to be **restartable**. Each step
reads the current state and moves it forward, so a throw mid-tick leaves
state that the next tick can continue from.

The same throttle guards the other hot paths that call into the game:
`WaveDirector.Spawn` (a spawn that throws counts as skipped, logs
`SpawnSkipped`, and the wave goes on) and `WaveTracker.Tick`.

For patches, use the pattern from section 3.1: guard first, then wrap a call
into fragile code (UI construction, status effects) in try/catch, and return
the vanilla answer on failure.

### 6.2 Fail safe: `Try…` and "not now"

Every lookup that can miss returns a value meaning "not now", and the caller
retries through a gate or skips:

- `ArenaLayout.TryFind(out points)` returns false until the room is active.
- `HeroicBosses.Find(typeName)` wraps `DewResources.GetByShortTypeName<BossMonster>`
  in try/catch and returns null for an unknown name from the config.
- `MonsterSpawner.Spawn` returns `bool`, and `false` becomes a log line, not a
  stalled wave.
- Adapters return early when a manager's `softInstance` is null.

A wrong config value, a missing type in this game version, or a room that
loads slowly costs one feature for a moment. It never costs the run.

### 6.3 Undo every effect

Make an inventory: every write the mod makes to state it doesn't own, next to
its undo and the place that calls the undo. Last Stand's:

| Effect | Undo | Called from |
|---|---|---|
| Harmony patches | `harmony.UnpatchAll(harmony.Id)` | `OnDestroy` |
| `GameManager.goldCostMultiplier` (shared by every gold price) | `GoldPrices.Restore()` to `Tuning.VanillaGoldCostMultiplier` | `StopFight` ← `Reset` ← `OnUnloading` ← `OnDestroy`, and on leaving the arena |
| `ZoneManager.currentHuntLevel` | `HuntedZone.End()` puts the previous level back | wave end, `WaveDirector.Reset` |
| Room modifiers added to the arena node | `ArenaModifier.Remove()` | wave end, `WaveDirector.Reset` |
| The wave tracker quest | `WaveTracker.Remove()` destroys it | `Session.Reset` |
| Injected title and lobby buttons | `InjectedUi.RemoveAll()` finds them by name suffix | `ModEntry.OnDestroy` (library) |
| The lobby dropdown option | `LobbyModeOption.Detach()` | `ModEntry.OnDestroy` (library) |
| Entries inserted into `DewGUI.fieldBuilders` | `ChoiceFields.Unregister()` | `ModEntry.OnDestroy` (library) |
| The settings window | `ConfigWindow.Close()` | `ModEntry.OnDestroy` (library) |

Three techniques make the inventory reliable:

1. **Restore only what you changed.** `GoldPrices._applied` is set on the
   first write and checked in `Restore`. A session that never touched the
   multiplier doesn't overwrite it on unload.
2. **Name what you create.** Every injected object's name ends in
   `ModIdentity.OwnedSuffix` (`" (LastStand)"`), and
   `ModIdentity.IsOurs(name)` finds them for removal, even ones created by a
   previous assembly whose references are gone:

   ```csharp
   foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
       if (button != null && button.gameObject.scene.IsValid() && ModIdentity.IsOurs(button.name))
           Object.DestroyImmediate(button.gameObject);
   ```

3. **One reset chain.** `Session.Reset()` calls `StopFight()`, which calls
   every service's `Reset`/`Forget`/`End`. Leaving the arena, a new
   `GameManager`, losing host status and unloading all go through it. There
   is no second teardown path to forget a step in.

### 6.4 Teardown when the network is already gone

Game over and restart shut the transport down (`NetworkServer.Shutdown()`
in `RestartAsync`, above) **before** your mod notices the run ended. Any
undo that writes a SyncList or SyncVar after that throws. Last Stand saw it
at wave 170. `ZoneManager.RemoveModifier` threw
`InitSyncObject: IsWritable: neither NetworkServer nor NetworkClient are active`,
and the throw aborted the session reset halfway.

The fix in `ArenaModifier.Remove`: clear your own state **first**, then touch
the game only while the server is active, and wrap that call:

```csharp
public void Remove()
{
    if (!IsActive) return;

    int node = _node;
    int id = _id;
    _instance = null;
    IsActive = false;
    _node = ArenaLocator.NoNode;

    ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
    if (NetworkServer.active && zones != null && node >= 0 && node < zones.nodes.Count && IsOnNode(zones, node, id))
    {
        try { zones.RemoveModifier(node, id); }
        catch (Exception e) { Log.Error(LogText.ModifierRemoveFailed(_type), e); }
    }
}
```

`HuntedZone.End` and `GoldPrices.Restore` have the same
`NetworkServer.active` guard. When the network is gone, the game objects are
being destroyed anyway. The only job left is to leave the mod's own state
clean for the next run.

**Where else this applies:** anything you change on a shared manager
(`GameManager` multipliers, `ZoneManager` hunt level, `DewDifficultySettings`
values), on prefabs (a changed prefab field stays changed for every later run
until restored), or in static game lists (`DewGUI.fieldBuilders`, console
commands, event subscriptions on singletons).

---

## 7. Surviving mod reloads

With developer mode and `enableAutoReloadMods` on, any file change under
`Mods/` reloads the mod mid-run. During development a reload is routine, so
design for it.

### 7.1 What a reload actually does

`DewMod.Refresh` runs a coroutine: `UnloadAll()`, `yield return null`, then
loads every active mod again. `UnloadAll` destroys each container
`GameObject`, so Unity runs your `OnDestroy` by the end of that frame, and
runs every `CallOnNetworkedManager` cleanup. Then the load from section 2.1
runs again: the bytes are read, the assembly is **renamed with
`DateTime.Now.Ticks`**, and it is loaded with `Assembly.Load(byte[])`.

Consequences:

1. **The old assembly never leaves memory.** An assembly can't be unloaded
   from a running domain. Its code and statics stay alive.
2. **The new assembly has fresh statics and distinct types.** Because the
   name differs, an old `LastStand.Session.X` is not the same type as a new
   `LastStand.Session.X`. A cast between them fails. Anything you carry
   across must be a type from a shared assembly: `string`, `int`, BCL types.
3. **Delegates the old code attached to game objects still fire.**
   `EnemyRegistry.Adopt` does
   `entity.EntityEvent_OnDeath += kill => OnDeath(entity, tier, kill);`.
   After a reload, a monster spawned before it still calls the **old**
   registry, which `OnDestroy` cleared. The death goes nowhere useful.
4. **Old patches are gone** (unpatched in `OnDestroy`), so only the new
   assembly's patches run.

### 7.2 Carry small state in an AppDomain slot

`AppDomain.CurrentDomain.SetData(string, object)` and `GetData(string)` live
in the runtime, not in your assembly. Store a `string` there and the next
assembly can read it. `src/Session/ProgressStore.cs`:

```csharp
public static string CurrentRunKey()
{
    GameManager game = NetworkedManagerBase<GameManager>.softInstance;
    ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
    if (game == null || zones == null) return null;
    return game.GetInstanceID().ToString(CultureInfo.InvariantCulture) + ":" +
           zones.worldSeed.ToString(CultureInfo.InvariantCulture);
}

public static void Save(int node, int wave, bool inProgress, int fastClears)
{
    string run = CurrentRunKey();
    if (run == null) return;
    string data = string.Join("|", run, node, wave, inProgress ? "1" : "0", fastClears);
    AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, data);
}
```

(The real code uses named constants and `InvariantCulture` for every number.
`ModIdentity.ProgressSlot` is `"LastStand.Progress"`.)

The design choices:

- **Key the data to the run.** `GameManager.GetInstanceID()` changes every run
  and `ZoneManager.worldSeed` (a SyncVar) changes every world. `TryLoad`
  rejects data whose key or node doesn't match, so a reload in a later run
  can't resume an old wave count.
- **Save on events, not per frame.** `OnWaveStarted`, `OnWaveCleared` and
  `OnFastCleared` each call `Save`. The slot is always at most one event old.
- **Load on arrival.** `Arrive` calls `TryLoad`. On a hit it resumes the cycle
  (`WaveDirector.Resume(wave, inProgress, …)`) and logs
  `progress restored: wave N ...`.
- **Version the format by field count.** A fifth field (fast clears) was added
  later. `TryLoad` accepts both `FieldCount` (5) and
  `FieldCountWithoutFastClears` (4), so a slot written by the older build
  still loads.

`StartingBonus` uses a second slot, `ModIdentity.StartingGoldSlot`
(`"LastStand.StartingGold"`), holding the
`GameManager` instance id of the run that was already paid. A reload in camp
doesn't grant the starting gold twice. A restart creates a new `GameManager`,
so it does grant it again.

### 7.3 Rebuild ownership from the world, not from memory

In-memory sets ("these entities are mine") die with the old assembly. For
each one, decide how the new instance finds out again. Ask: *if this object
vanished now, how would the next instance know?*

| State | Answer | Implementation |
|---|---|---|
| Wave number, in-progress flag, fast-clear count | Can't be derived from the world; small | AppDomain slot (`ProgressStore`) |
| Which monsters belong to the wave | Derivable | `EnemyAdoption.AdoptSurvivors` |
| Which `BossMonster` is a heroic boss | Derivable | `HeroicBosses.Owns` fallback |
| Which shrine is ours | Derivable | `StandingShrine<T>.FindExisting` |
| The tracker quest | Derivable | `WaveTracker.TryAttach` adopts an existing `Quest_Limbo` |
| Whether the modifier is still on the node | Derivable | `ArenaModifier.IsOnNode` checks the node's list by id |

`EnemyAdoption` rebuilds the registry from `ActorManager.allEntities`. It
keeps live `Monster`s owned by `DewPlayer.creep` that have no `Entity`
ancestor (so helpers spawned by a boss stay out), and recovers the tier from
synced game state:

```csharp
if (monster is BossMonster) return EnemyTier.HeroicBoss;
if (monster.type == Monster.MonsterType.MiniBoss) return EnemyTier.MiniBoss;
return monster.Status.HasStatusEffect<Se_HunterBuff>() ? EnemyTier.Hunter : EnemyTier.Regular;
```

Adoption attaches **new** death handlers that point at the new registry. That
fixes consequence 3 from section 7.1 for everything that matters.

`HeroicBosses.Owns` shows the failure that taught this rule. In testing, a
reload during a boss fight lost the in-memory record of the boss. The boss
then died through the game's own boss flow and dropped no rewards. The fix
derives ownership from the world when the set doesn't have it:

```csharp
if (_spawned.Contains(entity)) return true;
foreach (Entity boss in _spawned)
    if (boss != null && entity.IsDescendantOf(boss)) return true;
return entity is BossMonster unclaimed && IsSupported(unclaimed) && ArenaLocator.IsInArenaNode();
```

The fallback is sound because the arena is a cleared room that never spawns
its own boss. Any supported `BossMonster` standing there must be the mod's.
Find the fact that makes your fallback true, and write it down in the docs.

`StandingShrine<T>.Place` searches for an existing `T` near the spot before
it creates one, and reconfigures it if found. After a reload there is still
exactly one Dream Well, not two.

**Where else this applies:** cooldowns or counters a mod tracks per player
(store a string keyed by player guid and run), objects a mod spawns (name or
tag them, adopt on start), and status effects a mod applies (find them by type
on the entity instead of remembering them).

---

## 8. One source of truth

Before you store a value, look for where the game already keeps it. Store
your own copy only when the game has none, and then keep exactly one.

| Fact | The record | Not this |
|---|---|---|
| Is the mode on? | `GameSettingsManager.gameSettingsSaveKey` (SyncVar) | A mod `bool` |
| Current gold price multiplier | `GameManager.goldCostMultiplier` (read before writing) | A cached float assumed to be live |
| Shrine uses per player | `Shrine._useCountByPlayer`, a private `SyncDictionary<string, int>`, written by reflection in `UseCountPin` | A mod dictionary of uses |
| Is a monster alive? | `entity.IsNullInactiveDeadOrKnockedOut()`, checked in `Prune` | The registry's membership alone |
| Is our modifier on the node? | `zones.nodes[node].modifiers`, searched by id | `ArenaModifier.IsActive` alone |
| Wave number | `WaveCycle.CurrentWave`, the only owner | Counters kept separately by each service |

Two consequences matter most:

- **Clients see the truth.** Writing the game's synced fields (the use count
  dictionary, `NetworkgoldCostMultiplier`) means a vanilla client's tooltip
  and the host's charge agree. A host-side copy would make the host enforce
  rules that clients can't see (`04-host-only-networking.md`).
- **Derived values are computed.** `WaveDirector.ClearedWaves` is
  `WaveInProgress ? CurrentWave - 1 : CurrentWave`, a property, not a field.
  `TrackerProgress` builds its string from the cycle and the registry every
  refresh. Nothing can drift because nothing is stored twice.

The reload slot in section 7.2 looks like a second copy, but it isn't one.
The live cycle is the record, and the slot is a snapshot written from it on
each event and read only when there is no live cycle.

---

## 9. No magic values

Every number, name and string lives in exactly one named place. A game update
that renames a private method is then one edit, and a wording change can't
miss a copy.

| Kind | Home | Example |
|---|---|---|
| Player-tunable value | `Config/LastStandConfig.cs` field with its default | `public int startingGold = 100;` |
| Fixed internal constant | `Config/Tuning.cs` (`library/src/Config/SharedTuning.cs` for both mods) | `ArrivalRetrySeconds = 0.5f`, `VanillaGoldCostMultiplier = 1f`; shared: `ErrorLogIntervalSeconds = 5` |
| Game identifier that can't be `typeof`/`nameof` | `Game/GameNames.cs` (`library/src/Game/SharedGameNames.cs` for both mods) | `EntityOnDeath = "OnDeath"`, `ShrineUseCounts = "_useCountByPlayer"`; shared: `GenerateWorld = "GenerateWorld_Imp"`, `PrimusArenaRoom = "Room_Primus_Boss_0"` |
| The mod's own identity | `Identity/ModIdentity.cs` (the library's half adds the derived names) | `Tag = "LastStand"`, `SaveKey = "laststand"`; derived: `LogPrefix = "[LastStand] "`, `OwnedSuffix` |
| Chat text | `Text/Messages.cs` (`library/src/Text/SharedMessages.cs` for the shared formatting) | `SharedMessages.Notice("A heroic boss enters the arena!")` |
| Quest tracker text | `Text/TrackerText.cs` | `WaveRunning(wave, left)` |
| Config screen text | `Text/ConfigText.cs` | labels and descriptions used in attributes |
| Log text | `Text/LogText.cs` | `TickFailed`, `WaveStarted(plan)` |
| Console command names and help | `Text/CommandText.cs` | `Status = "laststand_status"` |

`GameNames` follows one rule: use `typeof`/`nameof` whenever the member is
public, and a string only when it isn't. `HarmonyPatch(typeof(Room), nameof(Room.ClearRoom))`
fails to compile if the game renames `ClearRoom`, which is better than failing
at runtime. `CurseStatusEffect`'s `OnCreate` is protected, so it is
`GameNames.CurseOnCreate`. Even the environment list uses
`nameof(RoomMod_AcceleratedTime)` and so on, so a removed modifier type is a
build error.

Two greps keep the rule enforced. The second must list only the allowed
folders:

```sh
grep -rn "//\|/\*" src
grep -rln '"' src | grep -v "src/Config\|src/Text\|src/Identity\|src/Game/GameNames.cs"
```

(The second is a coarse first pass; read its hits, since a file outside those
folders may legitimately use `string.Empty`.) The repository's `./check.sh`
runs both over every mod and `library/src` (where `SharedGameNames.cs` is
also allowed), plus the build.

### 9.1 The schema version: when defaults change

`ModBehaviour.LoadConfigsToDisk()` writes defaults only if the file is
missing. Otherwise it does
`fieldInfo.SetValue(this, DewPersistence.FromJson(File.ReadAllText(path), type))`.
Every key in the saved file overrides the new default. Change a default in
code, and every existing player keeps the old value forever. Last Stand hit
this: an early file kept the first build's miniboss timing and alive cap
through several releases.

The fix is a hidden version field and a reset before the base loader runs:

```csharp
public class LastStandConfig : ModConfig
{
    [HideInInspector]
    public int schemaVersion = ConfigSchema.Current;
    ...
}

public override void LoadConfigsToDisk()
{
    foreach (FieldInfo field in modConfigFields)
        ConfigFile.ResetIfOutdated(GetModConfigFilePath(field), ConfigSchemaVersion);
    base.LoadConfigsToDisk();
}
```

The override is in the shared library's `ModEntry`, and `ConfigFile` is in
`library/src/Config/ConfigFile.cs`. `LastStandMod` supplies
`ConfigSchemaVersion => ConfigSchema.Current`; `ConfigSchema` in
`src/Config/` holds only that number. `ConfigFile.ResetIfOutdated` parses
the file with `JObject`. If `schemaVersion` isn't an integer equal to
`ConfigSchema.Current`, or the file doesn't parse, it copies the file to
`<path>.old`, deletes it, and logs `SharedLogText.ConfigReset`. The base
loader then writes fresh defaults. `[HideInInspector]` keeps the field off
the settings screen.

The rule: **bump `ConfigSchema.Current` whenever a default changes or a field
is renamed or removed.** The player's custom values are kept in `.old`.
Mention the bump in the docs, so players know why their settings reset.
`08-config-ui-and-commands.md` covers the settings UI built on this config.

---

## 10. Logging for operability

`Player.log` is the only window into a host's run (Windows:
`%USERPROFILE%\AppData\LocalLow\Lizard Smoothie\Shape of Dreams\Player.log`;
Linux/Proton:
`steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log`).
Make it searchable:

- **One prefix.** `Log.Info/Warn/Error` prepend `ModIdentity.LogPrefix`
  (`"[LastStand] "`), and `Error` appends the exception. `grep "\[LastStand\]"`
  gives the mod's whole story.
- **Log decisions as key=value.** `LogText.WaveStarted(plan)` prints
  `wave N: regular=… surge=… hunter=… miniboss=… pain=… heroic=…` and more.
  The plan comes from a pure rule, so you can check a logged line against the
  arithmetic by hand. This is how Last Stand tests its rules without a test
  project.
- **Log on change, not per frame.** `GoldPrices.Set` returns early when the
  value is already set. `WaveTracker` logs `title sent to N ready
  connection(s)` only when the count changes.
- **Throttle errors** (section 6.1) and say what still works.
- **Expose state on demand.** `laststand_status` prints `Session.Describe()`:
  mode, server, phase, wave, in progress, seconds to next wave, alive,
  pending, loot tally.

`09-debugging-and-testing.md` turns these lines into a test procedure.

---

## 11. Growing it: which layer each piece goes in

A new feature should arrive as data plus small new types, never as edits
scattered through control flow. The procedures below are the order Last
Stand uses.

### 11.1 A per-wave feature

Example: a room environment that lasts one wave, every 10 waves from wave 6.

1. **Config.** Add the fields (`environmentFromWave`,
   `environmentIntervalWaves`, `environmentUntilWave`, `environmentTypes`) to
   `LastStandConfig` with defaults and `ConfigText` labels. Bump
   `ConfigSchema.Current`.
2. **Snapshot.** Validate them in `SessionSettings.From` into `WaveSettings`
   (a `Cadence` for "every N waves from F", a list of type names).
3. **Rule.** Add the decision to `WaveSchedule.Plan` and a field to
   `WavePlan` (`HasEnvironment`). Keep it a function of `wave` and settings.
4. **Adapter.** Wrap the game API in its own type (`ArenaEnvironment`, built on
   `ArenaModifier`), with `Begin` and an `End` that is safe after network
   shutdown.
5. **Service.** Start it from `WaveDirector.StartWave` *and*
   `ResumePlan` (or a reload mid-wave loses it). End it in `FinishWave` and
   `Reset`.
6. **Text.** Log and chat lines go in `LogText` and `Messages`.
7. **Docs.** One topic file plus a line in the docs index.

### 11.2 A shrine

1. A class in `Well/`. A shrine that only needs placement and unlimited uses
   uses `StandingShrine<T>` as is. One with its own rules (uses per wave, a
   lock) wraps it, like `AscensionShrine` or `DestinyShrine`.
2. Register it in `ArenaShrines.Place`, gated by its setting, so it takes a
   spot on the edge ring.
3. Hook its reactions: `OnUsed` (fed by the existing `ArenaShrineUsePatch` on
   `Shrine.DoPostUseRoutines`), `OnWaveCleared`, and `Forget`.
4. Write per-player limits to the game's synced counts (`UseCountPin`), not to
   a mod dictionary.
5. Reload check: `FindExisting` must adopt the old one.

### 11.3 A setting

1. Field plus attributes in `LastStandConfig`, with its strings in
   `ConfigText`.
2. Clamp it in `SessionSettings.From`.
3. Read it from the snapshot where it is used.
4. Bump the schema if it changes an existing default.

### 11.4 A new hook into the game

1. One file in `Patches/`. Use `typeof`/`nameof` for public members and
   `GameNames` for the rest.
2. First line: `ModMode.IsHosting()`, falling back to vanilla.
3. Call one session method. Put the decision in a rule and the effect in an
   adapter.
4. If the call can throw into fragile code, wrap it and log.

### 11.5 Placement checklist

| Question about the new code | Layer | Folder |
|---|---|---|
| Does it run because the game called a method? | Patch | `Patches/` |
| Can it be computed from numbers and settings? | Rule | next to its feature (`Waves/`, `Loot/`, …) |
| Does it read or write a game object or manager? | Adapter | `Game/` or the feature folder |
| Does it remember something across frames? | Service, reset in `StopFight` | feature folder |
| Is it a number the player might tune? | Config + snapshot | `Config/` |
| Is it a fixed number? | `Tuning` | `Config/` |
| Is it a game name that `nameof` can't reach? | `GameNames` | `Game/` |
| Is it text? | `Text/*` | `Text/` |
| Does it change state the mod doesn't own? | Add a row to the undo inventory (6.3) | wherever its adapter lives |
| Must it survive a reload? | Derive from the world, or an AppDomain string slot | `Session/` |

**Where else this applies:** the same checklist fits a mod with one feature.
A single patch with its rule and adapter in three small files is no more code
than one large patch, and it is the version that still works after the next
game update.
