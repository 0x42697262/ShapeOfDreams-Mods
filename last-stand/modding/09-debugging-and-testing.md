# 9. Debugging and testing

A host-only mod has no debugger attached, no unit-test harness inside the
game, and a second audience (vanilla clients) whose screens you can't see
from the host. What you do have is a text log that the engine, the game and
your mod all write to, a developer console, a hot-reload loop, and the
decompiled game code to check numbers against. This chapter turns those into
a method:

1. read the log well enough to go from one stack-trace line to the exact
   instruction that failed;
2. write your own log lines as measurements, then check them against the
   game's math;
3. make every game state reachable in under a minute with test commands;
4. test what a vanilla client actually sees;
5. turn every change into a numbered, observable check;
6. prove that the DLL you ship is the code you think it is.

Prerequisites: a mod that loads (`01-project-setup.md`), a decompiled
`Dew.Core` you can grep (`02-reading-the-game.md`), and a basic idea of what
is synced (`04-host-only-networking.md`).

---

## 1. Player.log: the one place everything ends up

**Technique: know where the log is, follow it live, and filter by prefix.**
Unity's player writes every `Debug.Log`, `Debug.LogWarning`,
`Debug.LogError` and uncaught exception to `Player.log`. The game, the mod
loader (`DewMod`), Harmony, Mirror and your mod all share this one file.

### Where it is

| Platform | Path |
|---|---|
| Windows | `%USERPROFILE%\AppData\LocalLow\Lizard Smoothie\Shape of Dreams\Player.log` |
| Linux (Proton) | `<SteamLibrary>/steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log` |

Next to it is `Player-prev.log`. Unity rotates the file at startup: the
previous session's log becomes `Player-prev.log` and a fresh `Player.log`
starts. **Copy the log before you relaunch** if a crash or a bug happened,
or the evidence is gone two launches later. The same folder holds
`QuickSave/` (profiles, `r_platform.json`, mod config files under
`QuickSave/Mods/<id>/`).

### Following it live

Linux, or any shell with coreutils:

```sh
LOG=".../Shape of Dreams/Player.log"
tail -F "$LOG"
```

Use `-F`, not `-f`: `-F` reopens the file by name, so it survives the
rotation when the game restarts.

Windows PowerShell:

```powershell
$log = "$env:USERPROFILE\AppData\LocalLow\Lizard Smoothie\Shape of Dreams\Player.log"
Get-Content $log -Wait -Tail 50
```

### Filtering

A busy run writes thousands of engine lines (asset unloads, EOS, transport,
input). Give every line your mod writes one fixed prefix and filter on it.
Last Stand's whole logging layer is three methods:

```csharp
internal static class Log
{
	public static void Info(string message)
	{
		Debug.Log(ModIdentity.LogPrefix + message);
	}

	public static void Warn(string message)
	{
		Debug.LogWarning(ModIdentity.LogPrefix + message);
	}

	public static void Error(string message, Exception exception)
	{
		Debug.LogError(ModIdentity.LogPrefix + message + Environment.NewLine + exception);
	}
}
```

with `LogPrefix = "[" + Tag + "] "`, so every line starts with `[LastStand] `.
`Error` appends `exception.ToString()`, which carries the type, message and
full stack trace into the same log entry.

A useful live filter keeps your lines, the loader, Harmony, and every
exception together with its stack frames (frames start with `  at `):

```sh
tail -F "$LOG" | grep --line-buffered -E \
  '^\[MyMod\]|\[DewMod|Harmony|Exception|^  at |Runtime (addition|removal) failed'
```

```powershell
Get-Content $log -Wait -Tail 0 |
  Select-String -Pattern '^\[MyMod\]','\[DewMod','Harmony','Exception','^  at '
```

Keep an unfiltered `tail -F` in a second terminal. Filters hide context, and
the line before an exception (`Current scene: ...`, `Transport shut down.`)
often explains it.

### What the log does and doesn't contain

Observed in the shipped build:

- `Debug.Log` lines are written **without** a stack trace. A plain
  `[LastStand] unloaded.` is one line. Only exceptions and anything you
  append yourself carry frames.
- Engine and game lines have their own tags you can anchor on:

| Line | Meaning |
|---|---|
| `[LaunchOptions] modDir: ...` | Command-line options parsed at boot (`DewLaunchOptions`) |
| `Current scene: Title` / `PlayLobby` / ... | Scene changes |
| `[DewMod] Initialized com.example.mymod::MyModBehaviour` | One per `ModBehaviour` type added to the mod's container |
| `[DewMod] Activating com.example.mymod` | Container activated; `Awake`/`Start` run after this |
| `[DewMod] Failed to load com.example.mymod::X due to exception: ...` | A type failed to construct or register |
| `Warning: Loading mods while in the lobby or in-game may cause unexpected bugs.` | A load or reload happened with a network session active |
| `[DewMod-Override] ...` | JSON override registration problems |
| `Runtime removal failed; Modifier instance with id N not found` | `RoomModifiers.HandleRuntimeRemoval` was asked to remove an unknown modifier |
| `Transport shut down.` / `Transport shut down - was server.` | Mirror transport stopped (game over, leaving, restart) |

A mod load in the middle of a lobby looks like this (id replaced):

```
[MyMod] unloaded.
[DewMod] Initialized com.example.mymod::MyModConsole
[DewMod] Initialized com.example.mymod::MyMod
[DewMod] Activating com.example.mymod
Warning: Loading mods while in the lobby or in-game may cause unexpected bugs.
[MyMod] loaded: My Mod v0.1.0.
```

Print a `loaded: <name> v<version>` line from your `Start` (Last Stand uses
`mod.metadata.name` and `mod.metadata.modVer`). If it's missing after the
`Activating` line, `Start` threw before reaching it.

**Where else this applies:** the client's own `Player.log` (section 7);
any Unity game's log; spotting a second copy of your mod (two `Activating`
lines for different ids).

---

## 2. Reading exceptions

**Technique: read a Mono stack trace frame by frame, decode the compiler's
generated names, and map the IL offset to one instruction.**

### The shape of a frame

A real trace from the shipped game (paths shortened):

```
System.NullReferenceException: Object reference not set to an instance of an object
  at Cysharp.Threading.Tasks.PlayerLoopHelper.AddAction (...) [0x00000] in ...\PlayerLoopHelper.cs:490
  at Cysharp.Threading.Tasks.UniTask.Delay (...) [0x00062] in ...\UniTask.Delay.cs:180
  ...
  at DewSteam.GetTicketForWebApi (System.String pchIdentity) [0x000ac] in ...\DewSteam.cs:172
  at DewSave.UpsertProfile () [0x0005b] in ...\DewSave_Profile.cs:630
```

Each frame is `at <Namespace.Type>[+Nested].<Method> (<parameter types>)
[0x<IL offset>] in <file>:<line>`.

- The **first frame** is where it threw. The frames under it are callers.
  Read top-down until you hit the first frame in code you own or can change.
- The **hex in brackets** is the IL offset inside that method. It is there
  even when there is no line number.
- The **file and line** appear only when the runtime has debug symbols for
  the assembly. The game's own assemblies print source paths from the
  developer's machine. Mods are loaded from a byte array
  (`Assembly.Load(byte[])`, see section 4), so expect your own frames to
  carry the IL offset but no usable line. Plan to work from the offset.

### Compiler-generated names

C# features compile into hidden types and methods with names that can't be
written in C#. They show up verbatim in traces and in IL listings.

| In a trace | What it is | Find it in decompiled C# |
|---|---|---|
| `Type+<Routine>d__31.MoveNext ()` | Iterator (coroutine) or `async` state machine for method `Routine` | The method `Routine` itself; the body of `MoveNext` *is* the method body |
| `Type+<>c__DisplayClass31_0.<Routine>b__1` | Lambda that captures locals of method 31 | The `delegate` / `=>` inside `Routine` |
| `Type+<>c.<Method>b__29_0` | Lambda that captures nothing (cached singleton `<>c`) | Same, a non-capturing lambda |
| `Type+<<Outer>g__Routine\|17_0>d.MoveNext ()` | Local function `Routine` inside `Outer`, itself an iterator | `static IEnumerator Routine()` declared inside `Outer` |
| `<Name>k__BackingField` | Auto-property storage | The property `Name` |
| `Mirror.NetworkBehaviour+...<InitSyncObject>b__0` | A lambda inside Mirror's `InitSyncObject` | Mirror's `NetworkBehaviour` |

The number after `d__` / `DisplayClass` is the method's index in the type's
metadata. It changes when the game is rebuilt, so search for the method
name inside the angle brackets, not the number.

### From an IL offset to the failing instruction

`ilspycmd -il` prints IL with `IL_xxxx` labels that match the trace's
`[0x0xxxx]` offsets:

```sh
M="<GameFolder>/Shape of Dreams_Data/Managed"
ilspycmd -r "$M" -il "$M/Dew.Core.dll" > /tmp/Dew.Core.il
grep -n "'<SmallArtilleryRoutine>d__31'" /tmp/Dew.Core.il | head
```

Gotcha (ilspycmd 8.2): with `-il`, the `-t <type>` filter is ignored and the
whole assembly is printed. Dump once, then search the file for the nested
class name. Section 3 does this for a real bug.

### Harmony errors

Harmony is the patching library the game ships (`0Harmony.dll`); see
`03-hooking-with-harmony.md`. Two kinds of failure:

1. **At patch time** (`harmony.PatchAll()` in your `Start`). Harmony throws,
   and Unity logs the exception from `Start`. Everything after `PatchAll` in
   `Start` doesn't run, so your `loaded` line is missing. Messages compiled
   into `0Harmony.dll`:
   - `Patching exception in method ...`: the target was found but the patch
     couldn't be applied (wrong parameter names in the patch method,
     injected argument of the wrong type, a broken transpiler).
   - `Undefined target method for patch method ...`: `[HarmonyPatch(typeof(X),
     "Name")]` names a method that doesn't exist with that signature. Usually
     a private method you misspelled, an overload you didn't disambiguate,
     or a game update renamed it.
   - `AccessTools.Field: Could not find field for type ...` (and the
     `TypeByName`, `Property`, `Method` variants): a reflection lookup by
     name failed.
2. **At run time**, an exception inside your prefix or postfix propagates
   into the game method that was patched, as if the game threw it. The
   frame for the patched method is a generated replacement method, not the
   original. Look for your patch class in the frames under it.

Patches should never throw. Keep them thin and hand off to code that
catches (`07-mod-architecture.md`). The only `throw` in Last Stand's
patches is the body of a `[HarmonyReversePatch]` stub
(`throw new NotImplementedException();`), which Harmony replaces with the
original method's code, so it never runs.

### Throttle repeated errors

An exception in per-frame code prints once per frame: 60 traces a second
bury everything else and slow the host. Last Stand wraps its per-frame tick
and reports through a throttle:

```csharp
private void Update()
{
	if (Session == null)
	{
		return;
	}

	try
	{
		Session.Tick(Time.time);
	}
	catch (Exception e)
	{
		_tickErrors.Report(Time.time, LogText.TickFailed, e);
	}
}
```

`ErrorThrottle.Report` logs at most once every 5 s
(`Tuning.ErrorLogIntervalSeconds`) and counts what it dropped:

```csharp
if (now < _nextAt)
{
	_suppressed++;
	return;
}

Log.Error(_suppressed > 0 ? message + " " + Text.LogText.Suppressed(_suppressed) : message, exception);
_suppressed = 0;
_nextAt = now + _intervalSeconds;
```

The next report then reads `session tick failed; ... (37 more like it were
not logged.)`, so you still learn the rate.

**Where else this applies:** `[DewMod] Failed to load` traces (your
constructor or field initializers threw); `Debug.LogException` lines from
`DewMod.ReloadFromActiveMods`; Mirror exceptions such as `InitSyncObject`
(section 10).

---

## 3. Worked example: the artillery that never fired

The goal: from wave 101 on, Last Stand adds the game's hunted-room modifier
`RoomMod_Hunted` to the arena for the length of each wave, through
`ZoneManager.AddModifier`. The modifier should rain small and big artillery
on the heroes. In play, hunters spawned and the hunt-level HUD showed, but
no artillery ever fell, on any wave from 101 to 155.

### Step 1: find the evidence

Filtering the log for exceptions at a hunter wave gave two traces each wave,
one per routine, of this shape:

```
NullReferenceException: Object reference not set to an instance of an object
  at RoomMod_Hunted+<SmallArtilleryRoutine>d__31.MoveNext () [0x001cb] ...
  at UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) ...
```

and the same with `<BigArtilleryRoutine>d__32`. (The offsets here are the
`callvirt` instructions found in step 3 for the current build.)

Read the frames:

- `UnityEngine.SetupCoroutine.InvokeMoveNext` at the bottom means a
  **coroutine** threw. Unity logs the exception and **stops that coroutine
  for good**. Nothing else is printed after it. This is why the bug looked
  like "nothing happens" instead of a crash, and why there were only two
  lines per wave.
- `<SmallArtilleryRoutine>d__31.MoveNext` is the state machine of the
  iterator method `RoomMod_Hunted.SmallArtilleryRoutine`.

### Step 2: read the method

`RoomMod_Hunted` is in `Dew.Core`, so it's in the decompiled tree:

```sh
grep -n "endWhenRiftOpen\|Rift.instance\|IEnumerator\|OnStartServer" Dew.Core/RoomMod_Hunted.cs
```

```csharp
public bool endWhenRiftOpen;
...
private IEnumerator SmallArtilleryRoutine()
{
	...
	yield return new WaitForSeconds(UnityEngine.Random.Range(initDelay.x, initDelay.y));
	while (true)
	{
		Hero hero = Dew.SelectRandomAliveHero(fallbackToDead: true, skipStealthed: true);
		...
		for (int i = 0; i < count; i++)
		{
			if (endWhenRiftOpen && Rift.instance.isOpen && !Rift.instance.isLocked)
			{
				yield break;
			}
			...
```

`BigArtilleryRoutine` has the same test in its loop condition:

```csharp
if (hero == null || (endWhenRiftOpen && Rift.instance.isOpen && !Rift.instance.isLocked))
```

Two candidates for null: `hero` (checked just before) and `Rift.instance`.

### Step 3: confirm with the IL offset

Dump the IL and look inside the state machine's `MoveNext`:

```
IL_01be: ldloc.1
IL_01bf: ldfld bool RoomMod_Hunted::endWhenRiftOpen
IL_01c4: brfalse.s IL_01e0

IL_01c6: call class Rift Rift::get_instance()
IL_01cb: callvirt instance bool Rift::get_isOpen()
IL_01d0: brfalse.s IL_01e0
```

`IL_01cb` calls `get_isOpen` on whatever `get_instance` returned. A
`NullReferenceException` there means `Rift.instance` was null. In
`<BigArtilleryRoutine>d__32` the same pattern sits at `IL_010a`/`IL_010f`.

### Step 4: why is it null?

```csharp
public static Rift instance => Dew.Helper_GetInstanceOfActor(ref softInstance);
```

```csharp
public static T Helper_GetInstanceOfActor<T>(ref T softInstance) where T : Actor
{
	...
	if (softInstance == null)
	{
		softInstance = FindActorOfType<T>();
	}
	if (softInstance == null)
	{
		softInstance = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
	}
	return softInstance;
}
```

The arena loads already cleared and has no exit rift, so there's no `Rift`
actor to find. In a normal hunted room there's always one. The routine was
written for rooms that have a rift. Note also that every call with no rift
does two scene searches: one more reason to keep it from being called.

### Step 5: choose the smallest fix

| Option | Verdict |
|---|---|
| Patch `MoveNext` with a transpiler | Works, but it rewrites a compiler-generated state machine whose name and offsets change with every game build |
| Spawn a dummy `Rift` | A networked actor with interaction, objective arrow and room-exit behavior; side effects on every client |
| Clear `endWhenRiftOpen` on this instance | `&&` short-circuits, so `Rift.instance` is never evaluated. One assignment |

The third wins, if two conditions hold.

- **Timing:** the field must be false before the routines start.
  `OnStartServer` starts them (`GameManager.CallOnReady(...)
  StartCoroutine(SmallArtilleryRoutine())`). `ZoneManager.AddModifier(int,
  ModifierData, Action<RoomModifierBase> beforePrepare)` on the live room
  goes to `RoomModifiers.HandleRuntimeAddition`, which creates the actor with
  `Dew.CreateActor(..., delegate(RoomModifierBase mod) { ...
  beforePrepare?.Invoke(mod); ... })`, before the actor is prepared and
  started. So `beforePrepare` runs early enough.
- **Sync:** `public bool endWhenRiftOpen;` has no `[SyncVar]`, and only the
  server's coroutines read it. Changing it on the host affects nothing a
  client computes. Clients see the artillery because the host spawns it.

Last Stand's code:

```csharp
if (!_modifier.Add(nameof(RoomMod_Hunted), KeepArtilleryWithoutRift))
...
private static void KeepArtilleryWithoutRift(RoomModifierBase modifier)
{
	if (modifier is RoomMod_Hunted hunted)
	{
		hunted.endWhenRiftOpen = false;
	}
}
```

where `_modifier.Add` ends in
`zones.AddModifier(nodeIndex, new ModifierData { type = type }, beforePrepare)`.

### Step 6: write down the check

The fix became checklist item 15's last sentence (section 8):

> The log has no `RoomMod_Hunted+<SmallArtilleryRoutine>` or
> `<BigArtilleryRoutine>` `NullReferenceException`, small and big artillery
> land around heroes all wave, and none falls in the break.

It contains a negative log check (the exact trace that must not appear), a
positive visual check, and a boundary check (the break).

### Caveat: the fix lives on the instance

`beforePrepare` is not stored in the node data. When a room starts,
`RoomModifiers.OnRoomStartServer` re-creates every modifier listed on the
node with `HandleRuntimeAddition(modifiers[i].id)`, with **no**
`beforePrepare`. A `RoomMod_Hunted` left on the node across a room reload
would come back with the prefab's `endWhenRiftOpen = 1` and the same
exception. Last Stand removes the modifier when the wave is cleared, and its
`Restore` path re-adds it through the same `Add` with the same callback.
See section 10, "Modifiers that come back".

**Where else this applies:** any game coroutine that assumes a room object
(rift, boss, entry trigger) your custom room lacks; any stock component with
a public, unsynced tuning field you can set from `beforePrepare` or a
`CreateActor` callback; any "nothing happens" bug, which is often a
coroutine that died once.

---

## 4. In-game tools

**Technique: turn on the game's developer tooling and know what hot reload
really does to a running session.**

### Developer Mode, F1 and the console

Developer Mode is a gameplay setting, saved as
`gameplay.enableDeveloperMode` in `QuickSave/r_platform.json`. With it on:

- **F1** cycles `DevCanvas.state` through three states (it starts at
  `ShownWithoutPopup`):

  | State | Console window object | Log popup (`DebugLogPopup`) |
  |---|---|---|
  | `ShownWithoutPopup` | available | hidden |
  | `Hidden` | hidden | hidden |
  | `Shown` | available | shown |

  The popup is the on-screen counter of new log entries, handy for seeing
  errors appear mid-fight without alt-tabbing to the log.
- **`` ` ``** (backquote, `DebugLogManager.toggleKey = KeyCode.BackQuote`)
  opens the console. It lists the same log entries as `Player.log`, stack
  traces included, and runs commands.
- **Manage Mods** in the console opens the built-in mod manager anywhere
  (official docs), with the auto-reload toggle.

### Game commands worth knowing

The game registers its own debug commands from `DewConsoleCommands` with
`[DewConsoleMethod]`. The command name is the C# method name, matched
case-insensitively. Useful ones for mod work:

| Command | Does |
|---|---|
| `nodeinfo` | Current zone, node index, type, status, hunted flag, room, room area, and **every modifier on the node** with its `clientData` |
| `netstat` | Server, client or no network; connecting/connected; round-trip time |
| `plist` | Lists players (server) |
| `netid <id>` | Name of the networked object with that `netId` |
| `showfps` | Toggles the FPS counter |
| `listgems [substring]`, `listskills [substring]` | Essence and memory type names |
| `testexception` | Throws `This is a test exception`; proves your log filter catches exceptions |
| `usefulactorname true` | Renames actors every logic update to readable names (slow) |

### Auto reload

The mod manager has an auto-reload toggle, saved as `enableAutoReloadMods`
in `QuickSave/r_platform.json`. It only takes effect with Developer Mode on
(`DewMod`: `enableDeveloperMode && enableAutoReloadMods`). How it works,
from `DewMod.EnableAutoReload`:

1. A `FileSystemWatcher` is created for **every local mod directory**
   (`Mods/` and `../Mods`, or only the `-moddir <dir>` launch option's
   directory if it exists), `IncludeSubdirectories = true`, filter `*.*`,
   watching create, delete, rename, size and last-write.
2. Any event records the path. A coroutine reloads once 0.5 s passed since
   the last event, no mod is loading, the transition manager is idle, **and
   the game window has focus** (`Application.isFocused`).
3. `ReloadFromActiveMods` reloads **all** active mods, not just the changed
   one.

Consequences: building in place (`obj/`, `bin/`) triggers it, editing a doc
file inside the mod folder triggers it, building a different mod triggers
it, and nothing happens until you click back into the game.

### What a reload does to a running session

`ReloadFromActiveMods` calls `UnloadAll`, then `Load(id)` for each active
mod:

- **Unload:** `Object.Destroy(container)`, so every `ModBehaviour`'s
  `OnDestroy` runs; the mod's console commands are removed; manager-patch
  cleanups run. Harmony patches are **not** removed by the loader. Your
  `OnDestroy` must do it (`harmony.UnpatchAll(harmony.Id)`).
- **Load:** the DLL is read into bytes, Mono.Cecil renames the assembly to
  `<Name>_<DateTime.Now.Ticks>`, and `Assembly.Load(byte[])` loads it. Every
  reload is a *new, differently named* assembly. The old one is never
  unloaded (Mono can't unload assemblies from the default domain).

| Thing | After a reload |
|---|---|
| Your static fields | Fresh: the new assembly has its own types. The old statics still exist in the old assembly, unreachable from new code |
| Harmony patches | The old ones stay unless the old `OnDestroy` removed them. `ModBehaviour.harmony` ids are `mod.metadata.id + Guid.NewGuid()`, so old and new instances never share an id |
| Delegates you attached to game objects (`entity.EntityEvent_OnDeath += ...`) | Still attached and still fire, into the **old** assembly's code and its reset state |
| Networked objects you spawned (shrines, quests, monsters, modifiers) | Still in the world. The new instance doesn't know about them |
| Config | Re-read from disk (`LoadConfigsToDisk`) |
| `AppDomain.CurrentDomain.SetData` | Survives: one domain, shared by every assembly version |

Treat a reload as a test, not just a convenience. Last Stand has checks for
it: the wave count carries on (`progress restored: wave N ...`), one tracker
(`wave tracker: took over the quest left from before.`), one of each shrine,
and a heroic boss alive during a reload still drops its rewards. Designing
for that is `07-mod-architecture.md`. Seeing it fail is section 10.

**Where else this applies:** `-moddir <dir>` to start the game with an
empty mod directory (a quick vanilla session on the same install);
`nodeinfo` after any room-modifier work; `testexception` after changing your
log filter.

---

## 5. Log lines as measurements

**Technique: log one line per decision, with every input and output as
`key=value`, at the moment the value is final. Then compare the numbers to
the formula you read in the decompile.**

### Rules for a measurement line

1. **One line per decision.** When the mod decides something (a wave's
   makeup, a reward, a price), print the whole decision in one line. Partial
   lines across frames can't be correlated.
2. **Inputs and outputs.** Print the numbers that went into the decision,
   not only the result, so a wrong result points at the wrong input.
3. **`key=value`, stable wording, fixed prefix.** The line is grepped, diffed
   between runs and quoted in checks. Last Stand keeps every log string in
   one file (`src/Text/LogText.cs`), so the docs and the checklist quote the
   exact text.
4. **At the moment the value is final.** Stats change after spawn as the
   game adds its own bonuses. Log after the last writer.
5. **Bounded.** Per decision, not per frame. Repeated errors are throttled
   (section 2).

### Last Stand's wave line

```csharp
public static string WaveStarted(WavePlan plan)
{
	return $"wave {plan.Number}: regular={plan.RegularCount} surge={plan.IsSurge} hunter={plan.HunterCount} " +
	       $"miniboss={plan.MiniBossCount} pain={plan.PainCount} heroic={plan.HeroicBossCount} " +
	       $"heroicHaste={plan.HeroicBossHaste:0} " +
	       $"ambient={plan.AmbientLevel} " +
	       $"health=+{plan.HealthBonusPercent:0}% damage=+{plan.DamageBonusPercent:0}% " +
	       $"fastClear=x{plan.FastClearMultiplier:0.##} heroicFastClear=x{plan.HeroicFastClearMultiplier:0.##} " +
	       $"foreign={plan.ForeignTypesUnlocked} mirage={plan.MirageSkinsUnlocked} " +
	       $"mirageChance={plan.MirageChance * Tuning.PercentScale:0}% " +
	       $"huntLevel={plan.HuntLevel} cap={plan.MaxAlive}.";
}
```

The `WavePlan` is computed by plain rules from the wave number and settings,
then logged before anything spawns. Every scheduling rule shows up in it,
which is what makes checks like these one grep:

- `health=+1480% damage=+1480%` at wave 149, `+1530%` at 150 (steeper
  scaling from wave 150);
- `heroic=1` at 49, `heroic=2` at 99 and 124;
- `huntLevel=0` on an environment wave, `hunter=` still nonzero.

A status command gives the same kind of snapshot on demand:
`laststand_status` logs `mode=... server=... phase=... wave=... inProgress=...
next=...s alive=... pending=...` plus items dropped per rarity.

### The heroic boss lines

Two lines bracket a heroic boss's life. The first is logged when its stats
are final:

```csharp
Dew.CallDelayed(() =>
{
	MonsterEnhancer.Multiply(entity, health, damage);
	MonsterEnhancer.AddAbilityHaste(entity, haste);
	HeroicBossReport.Watch(entity, wave);
});
```

`Dew.CallDelayed(func, frameCount = 1)` runs it one frame later, after the
game's `GameManager` entity-add handler has called
`DewDifficultySettings.ApplyDifficultyModifiers`. `Watch` logs:

```
heroic boss: wave N <Type> maxHealth=... attackDamage=... abilityPower=... abilityHaste=...
```

and subscribes to two events on the boss:

```csharp
boss.ActorEvent_OnDealDamage += (Action<EventInfoDamage>)report.OnDealDamage;
boss.EntityEvent_OnDeath += (Action<EventInfoKill>)report.OnDeath;
...
private void OnDealDamage(EventInfoDamage info)
{
	float hit = info.damage.amount + info.negatedAmountByShield;
	if (!(info.victim is Hero) || hit <= _biggestHit)
	{
		return;
	}

	_biggestHit = hit;
	_biggestHitSource = info.actor != null ? info.actor.GetType().Name : null;
}
```

At death it logs `heroic boss: wave N <Type> biggest hit on a hero <n>
(after armor, shields included) from <AbilityInstanceType>`. It records the
maximum, not every hit, so a five-minute fight costs one line. It names the
ability type, which is what you need to find the ability's ratios in the
decompile. Adding the shield-absorbed amount makes the number the real hit
size, not what was left after shields.

### Worked example: proving the boss health formula exactly

Goal: confirm that a heroic boss's health is exactly "vanilla boss scaling ×
Last Stand's wave bonus × the health multiplier", with nothing missing or
counted twice.

**Step 1: measure.** A solo Nightmare run, starting world 4, wave 124, logged
two bosses:

```
heroic boss: wave 124 <Skoll type> maxHealth=4306783 ...
heroic boss: wave 124 <Nyx type>   maxHealth=4868537 ...
```

**Step 2: divide out what differs.** The bosses differ in base health
(prefab values: Skoll 9,200, Nyx 10,400; bosses gain no health per level).
Divide:

```
4306783 / 9200  = 468.1286
4868537 / 10400 = 468.1286
```

Equal to four digits: everything else is one type-independent factor. If a
term depended on the boss type, the two ratios would differ.

**Step 3: remove the terms you own.** Last Stand's wave bonus at wave 124 is
+10% per wave after the first: +1230%, ×13.3 (the `health=` field of the
`wave 124:` line). The health multiplier setting is ×5. What's left belongs
to the game:

```
468.1286 / (13.3 × 5) = 7.0395
```

**Step 4: find the game's terms in the decompile.**
`DewDifficultySettings.ApplyDifficultyModifiers` adds two stat bonuses to a
`Monster`. First a flat difficulty bonus,
`maxHealthPercentage = enemyHealthPercentage`. Then a zone-scaling bonus:

```csharp
if (monster2 is BossMonster)
{
	num = NetworkedManagerBase<GameManager>.instance.GetBossMonsterHealthMultiplierByScaling();
	...
}
entity.Status.AddStatBonus(new StatBonus
{
	maxHealthPercentage = (num - 1f) * 100f,
	...
});
```

Follow the call:

```csharp
public float GetBossMonsterHealthMultiplierByScaling(float customZoneIndex = float.NaN)
{
	return GetStrongHealthScalingMultiplier_Imp(difficulty.GetScaledZoneIndexForHealth(customZoneIndex));
}

public static float GetStrongHealthScalingMultiplier_Imp(float zi)
{
	return GetMultiplier_Imp(zi, 0.0566392339721777, 0.209036492019735, 0.407011935005335, 0.999998990190336, 30);
}
```

`GetMultiplier_Imp` is the cubic `a·x³ + b·x² + c·x + d` up to `x = 30`,
linear after. `GetScaledZoneIndex` multiplies the zone index by
`healthFactorBeforeLoop` (before the loop point). Those two difficulty
fields are asset data, so read them from the difficulty assets
(`05-assets-and-data.md`). For the `DewDifficultySettings` assets:

| Asset | `enemyHealthPercentage` | `healthFactorBeforeLoop` |
|---|---|---|
| `diffNormal` | 0 | 0.8 |
| `diffHard` | 10 | 1.0 |
| `diffNightmare` | 25 | 1.0 |

**Step 5: compute.** World 4 is zone index 3. Nightmare: `x = 3 × 1.0`,
cubic = 5.63162, flat ×1.25:

```python
f = lambda x: 0.0566392339721777*x**3 + 0.209036492019735*x**2 + 0.407011935005335*x + 0.999998990190336
world = f(3) * 1.25                    # 7.039528
print(9200  * world * 13.3 * 5)        # 4306783.3
print(10400 * world * 13.3 * 5)        # 4868537.7
```

Both match the log to the integer.

**Step 6: confirm that the factors multiply.** Five separate bonuses
(difficulty, zone scaling, wave, multiplier, plus the per-player term at
one player) only multiply if the game composes percentage bonuses
multiplicatively. `BonusStats` does:

```csharp
result.maxHealthPercentage = DewMath.MultiplyPercentageBonuses(x.maxHealthPercentage, y.maxHealthPercentage);
...
public static float MultiplyPercentageBonuses(float a, float b)
{
	return ((1f + a * 0.01f) * (1f + b * 0.01f) - 1f) * 100f;
}
```

and `EntityStatus` applies the total as `data.maxHealth *= 1f +
bonusStats.maxHealthPercentage / 100f`. So a `+(M − 1) × 100%` bonus is
exactly ×M whatever else the boss carries. That is why Last Stand expresses
its multipliers as percentage bonuses.

**Conclusion.** The formula is `base × world(difficulty) × (1 + 0.85 ×
(players − 1)) × wave × multiplier`, and the same table predicts Normal
(`f(2.4)` = ×3.96) and Hard (`f(3) × 1.1` = ×6.19). Had the numbers missed
by a factor of 1.25 or 1.85, the gap itself would have named the missing term
(difficulty flat bonus, or a second player's 85%).

**Where else this applies:** gold prices (log the multiplier, compare to the
tooltip a client shows), drop rates (count `loot:` lines per rarity over a
long run), spawn counts per wave, curse kill goals, any curve you lifted
from the decompile.

---

## 6. Test commands that make states reachable

**Technique: give yourself a command for every state that takes too long to
reach by playing. Route it through the same code path the real game uses,
and gate anything that changes the economy behind a config flag that is off
by default.**

Wave 149 is hours of play. A heroic boss appears at wave 49. Without
shortcuts those features never get tested more than once.

### Last Stand's commands

| Command | Gate | State it reaches |
|---|---|---|
| `laststand_status` | none | Logs a one-line snapshot (section 5) |
| `laststand_next` | host | Starts the next wave now, even with monsters alive. The escape hatch for a monster stuck out of reach |
| `laststand_wave <n>` | host | Wave n starts now, with that wave's own plan (scaling, hunters, bosses, environment) |
| `laststand_skip [n]` | host + cheats | Ends the current wave with no drops and counts it as cleared (shrine resets, revives, progress save fire), n times, one wave per frame |
| `laststand_gold <n>`, `laststand_dust <n>` | host + cheats | Currency for every player |
| `laststand_level <n>` | host + cheats | Every hero to level n |
| `laststand_essence <rarity or name> [quality]`, `laststand_memory <rarity or name> [level]` | host + cheats | A specific item, reserved for the host |
| `laststand_secondwind <charges> [seconds]` | host + cheats | Survive fatal hits while watching a late wave |
| `laststand_testboss [type]` | host + cheats | A weak level 1 heroic boss now |

Two design points made these trustworthy:

- **Same path as real play.** `laststand_wave n` doesn't build a special
  wave. It resumes the session as if wave n − 1 had just been cleared, the
  exact path used to restore progress after a reload:

  ```csharp
  int previous = wave - 1;
  _waves.Resume(previous, false, _now, _waves.FastClearStacks);
  ProgressStore.Save(_arenaNode, previous, false, _waves.FastClearStacks);
  Log.Info(LogText.JumpedToWave(wave));
  ```

  So a check done with `laststand_wave 124` tests the real wave 124, and it
  tests the resume path for free. `laststand_skip` counts each skipped wave
  as cleared for the same reason: wave-cleared side effects are what you
  want to observe.
- **Refuse loudly.** Every refusal logs why (`laststand_wave: only works in
  the arena, with a wave of 1 or more.`, `skip: only works while fighting in
  the arena.`). A command that silently does nothing costs a test cycle.

### Gating

```csharp
private static bool CheatsAllowed()
{
	if (!LastStandMode.IsHosting())
	{
		Log.Warn(LogText.NotHosting);
		return false;
	}

	if (LastStandMod.Current?.Session?.Settings?.CheatCommands != true)
	{
		Log.Warn(LogText.CheatCommandsDisabled);
		return false;
	}

	return true;
}
```

`cheatCommands` is a config field (label "Allow cheat commands"), default
`false`. Gate on the **host**: commands run locally, and a client with the
mod loaded could otherwise run one against nothing, or against its own
idea of the session. Gate cheats on a **setting** so a public lobby can't be
handed free gold by accident. The official code of conduct also rules out
handing out stardust-bought items (`10-shipping.md`).

### Registration facts that affect testing

From `DewMod.Load` and `ModBehaviour.RegisterConsoleCommand`:

- Only methods carrying `[ConsoleCommand(description, name)]` are
  registered. To keep a command in the code but hide it, remove the
  attribute.
- Overloads with **different parameter counts** share one name
  (`laststand_skip` and `laststand_skip 10`). A second method with the
  same name and the same count is registered as `name0`, `name1`... so a
  duplicate silently gets a different name.
- Commands are removed on unload and re-registered on load. After a reload
  that failed halfway, a missing command is a symptom.

Writing commands in general is `08-config-ui-and-commands.md`.

**Where else this applies:** a command per expensive state (a boss, a late
wave, a full inventory); a "status" command per subsystem; reuse the
game's `listgems`/`listskills` for exact type names to pass to your
commands.

---

## 7. Testing what vanilla clients see

**Technique: run a second game as a vanilla client and compare, screen by
screen, what is synced against what each client computes for itself.**

The host's screen proves nothing about clients. A client runs the unmodded
game. It receives synced fields, RPCs and spawned objects, then draws
everything locally with its own code (`04-host-only-networking.md`).

### Setup

- A **second Steam account on a second machine** joins the host's lobby. A
  friend works as well. The game's `DewNetworkStartSettings` has a
  `lanMode` flag that switches to `TelepathyTransport`, but nothing in
  `Dew.Core` or `Dew.UI` sets it, so there is no LAN shortcut.
- The client must be **vanilla**: deactivate the mod in Manage Mods (the
  `activeMods` list in `r_platform.json`). If the client's mod folder holds
  your build, the `-moddir <empty directory>` launch option makes
  `DewMod.GetAllLocalModDirectories` return that directory instead of
  `Mods/` (Workshop subscriptions are still listed).
- Also test once with the mod **loaded but not hosting** on the client.
  Last Stand has checks for that: no settings button in the joined lobby,
  and `laststand_config` refuses with `only the host's settings apply`.

### What to compare

| Area | Synced (host decides) | Computed on each client |
|---|---|---|
| Quest tracker | `DewQuest.currentProgress` string, `progressType` | Title and description from a localization key, set by RPC |
| Shrine prices | `GameManager.goldCostMultiplier`, `_useCountByPlayer` | Tooltip price and the `CanAfford` check before the command is sent |
| Shrine limits | use counts | `maxUseCountPerPlayer` (not synced): `CanInteract` on the client |
| Modifier panel | the modifier actor | `UI_InGame_RoomModDisplay` text, hidden only on room load |
| Top-right label | `currentZoneIndex`, `currentZone` | Text built locally on room load |
| Chat | `MessageType.Raw` text | Rendering |

Rule: for every number or text you change, find which column it's in. If a
client computes it, open the client's screen and check it. Last Stand's
Shrine of Ascension check reads "a vanilla client's tooltip shows exactly
the gold the host takes". The tooltip is client math on synced inputs, and
it only matches if the mod changed the input, not the host's own copy of the
formula.

Read the **client's** `Player.log` too. A client-side exception (a UI
reading a field the host set to an unexpected value, a missing localization
key printed as `ui.!<key>`) appears only there.

### Late joiners

RPCs are not replayed to players who connect later. Anything set by RPC
must be re-sent when the set of ready connections changes. Last Stand
re-sends the tracker's title keys and logs `wave tracker: title sent to N
ready connection(s).` The test:

1. Host starts a run and reaches a wave.
2. Second account joins mid-run (mid-join is blocked while a heroic boss is
   alive: `BossMonster.OnCreate` sets `midJoinWaitType = FightingBoss`).
3. Compare the joiner's tracker title, shrine counts and prices to a player
   who was there from the start; look for the `title sent to` line in the
   host's log.

**Where else this applies:** any `ClientRpc` you send (late joiners never
get it); any prefab field you changed on the host that a client also reads;
any localized text (keys only, never free text, see `05-assets-and-data.md`).

---

## 8. The "needs an in-game check" list

**Technique: every change gets a numbered, concrete, observable check with
the exact log line, written before you test it, and kept until it has been
seen working.**

Last Stand's `AGENTS.md` keeps a numbered list ("Needs an in-game check",
80 items at the time of writing). A good item has:

1. **Setup:** the commands and settings that reach the state
   (`laststand_wave 124`, "set `Fast clear: from wave` to 1 first").
2. **Host observation:** what you see and the exact log line, quoted from
   `LogText`.
3. **Client observation:** what a vanilla client sees, when it differs.
4. **Negative checks:** what must *not* appear (an exception, a doubled
   object, an effect in the break).
5. **Boundaries:** the wave before and after a threshold.
6. **Persistence:** after a reload, after game over, for a late joiner.
7. **Migration:** for a changed default, that old config files are moved to
   `.old` (the config schema bump, `08-config-ui-and-commands.md`).

Example, item 72:

> **Fast clear carries over** (set `Fast clear: from wave` to 1 first; the
> default is 30): kill 80% of wave 1 (5 of 6) within 10 s. Chat says, in red
> (`#ff4d4d`) instead of the usual orange, `Wave cleared within 10s. From
> wave 2 on, monsters hit back with x2 health and damage.` and the log `fast
> clear: wave 1 lost 5 of 6 monster(s)`. The `wave 2:` and `wave 3:` lines
> show `fastClear=x2` and both waves' monsters take about twice the hits; a
> second fast clear makes it `x4`. A heroic boss (`laststand_wave 49`) after
> a fast clear has normal health. A slow wave and `laststand_skip` never
> trigger it. A mod reload logs `... 1 fast clear(s) so far` and keeps `x2`.
> Old config files are moved to `.old` (schema 32).

Every part of the list above appears in it: setup, exact chat and log text,
the measured field, a boundary (`x2` then `x4`), negatives (slow wave,
skip), an interaction (heroic boss), persistence (reload), migration.

Maintenance rules:

- **Never renumber.** Items cross-reference each other ("see check 77") and
  the history. Append new items at the end.
- **Update an item in place** when the behavior changes, and note the
  schema version that changed it.
- **Newest items are the untested ones.** The status section says which
  range has been seen working.
- **Log text is the contract.** Because every string lives in
  `src/Text/LogText.cs`, changing a log line means updating the checks that
  quote it. Grep the list for the old text.

**Where else this applies:** release notes (the checks of a release are its
test plan); bug reports from players (ask for the log lines a check names);
regression passes after a game update (run the list top to bottom).

---

## 9. Is the build what you think it is?

**Technique: confirm which DLL the game loaded, then compare builds at the
IL level when a change should not have altered behavior.**

### Which DLL is loaded

`about/metadata.json`'s `"assemblies"` paths decide what loads. Last Stand's
points at `bin/Debug/netstandard2.1/LastStand.dll` during development; the
packed release points at `LastStand.dll` in the release folder
(`10-shipping.md`). A Release build never reaches a game that loads the
Debug path. Symptoms: "my fix does nothing". Check:

- the `[DewMod] Initialized <id>::<Type>` lines list the `ModBehaviour`
  types you expect (a new type that's missing means an old DLL);
- your `loaded: <name> v<version>` line shows the version you just set;
- only one `Activating` line for your id. Two folders with the same id
  conflict (`IdConflict`).

### IL equivalence: did the refactor change behavior?

After a refactor meant to be behavior-neutral (reformatting, removing
comments, `var` versus explicit types, moving text into constants with the
same values), the IL should be identical. ilspycmd can print it:

```sh
M="<GameFolder>/Shape of Dreams_Data/Managed"
ilspycmd --disable-updatecheck -r "$M" -il before/MyMod.dll | grep -v "// Method begins at RVA" > before.il
ilspycmd --disable-updatecheck -r "$M" -il after/MyMod.dll  | grep -v "// Method begins at RVA" > after.il
diff before.il after.il
```

- `-r` points at the game's `Managed` folder so references resolve.
- The `// Method begins at RVA 0x....` comments shift for every method after
  the first one that changed size. Drop them, or one real change shows up as
  hundreds of diff lines.
- Compare the **same configuration**. On Last Stand, the Release DLL and the
  packed copy produce a 42,608-line listing each and an empty diff. Debug
  against Release differs by about 55,000 diff lines (no optimization, extra
  `nop`s and locals), which hides everything.
- An empty diff proves the two DLLs run the same instructions. A non-empty
  diff lists exactly the methods to review.

For a readable diff of intended changes, decompile both builds to C#
projects and diff the trees:

```sh
ilspycmd -r "$M" -p -o before_src before/MyMod.dll
ilspycmd -r "$M" -p -o after_src  after/MyMod.dll
diff -r before_src after_src
```

Expected noise: renamed private members, new lambda numbering
(`<>c__DisplayClass12_0` becoming `..._1`), member order. Literal values,
branches and calls are what matter.

The same command decompiles your own release from the Workshop folder when
a player reports a bug against a version you no longer have checked out.

**Where else this applies:** comparing the game's DLLs across a game update
(diff `Dew.Core` before and after, grep for the methods you patch);
confirming a constant moved into config kept its value; checking that a
"remove dead code" change only removed code.

---

## 10. Common failure patterns

Each pattern: how it shows up, how to confirm it, and the fix.

### Per-frame scene searches

- **Symptom:** host frame rate drops, worst in rooms or scenes where some
  manager or actor doesn't exist. Clients are fine. `showfps` on the host
  shows it.
- **Cause:** `NetworkedManagerBase<T>.instance` is:
  ```csharp
  if (softInstance == null)
  {
  	softInstance = Object.FindObjectOfType<T>();
  }
  if (softInstance == null)
  {
  	softInstance = Object.FindObjectOfType<T>(includeInactive: true);
  }
  return softInstance;
  ```
  When the manager is missing, every access is two full scene scans. In an
  `Update` that's two scans per frame per call site. Actor singletons such
  as `Rift.instance` do the same through `Helper_GetInstanceOfActor`.
- **Confirm:** time your tick with `System.Diagnostics.Stopwatch` and log a
  throttled summary (max and mean milliseconds over 5 s); grep your
  per-frame code for `.instance`.
- **Fix:** use `.softInstance` (the cached field, set in `Awake`/`OnEnable`,
  null when absent) in anything per-frame. Cache lookups. Retry on a
  throttled timer with a cap.

### Exceptions after the transport shut down

- **Symptom:** at game over, a restart, or leaving, the log shows
  `Transport shut down.` followed by
  `InitSyncObject: IsWritable: neither NetworkServer nor NetworkClient are
  active.` from a Mirror lambda (`<InitSyncObject>b__0`). Your cleanup stops
  halfway; the next run starts with leftover state.
- **Cause:** writing a `SyncList`/`SyncDictionary` or SyncVar after Mirror
  stopped. Last Stand hit it at wave 170: `ZoneManager.RemoveModifier`
  writes a node's modifier `SyncList`, and the exception aborted the session
  reset.
- **Confirm:** the exception's first non-Mirror frame is in your teardown,
  and the `Transport shut down` line comes before it.
- **Fix:** clear your own state first, then touch the game only while
  `NetworkServer.active`, and catch around the game call:
  ```csharp
  int node = _node;
  int id = _id;
  _instance = null;
  IsActive = false;
  _node = ArenaLocator.NoNode;

  ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
  if (NetworkServer.active && zones != null && node >= 0 && node < zones.nodes.Count && IsOnNode(zones, node, id))
  {
  	try
  	{
  		zones.RemoveModifier(node, id);
  	}
  	catch (Exception e)
  	{
  		Log.Error(LogText.ModifierRemoveFailed(_type), e);
  	}
  }
  ```
  Check 49 tests it: lose during a late wave, and the log has no `session
  tick failed` and no `InitSyncObject`.

### Doubled effects after a reload

- **Symptom:** after an auto-reload, log lines appear twice, a reward drops
  twice, two trackers or two shrines appear, or the opposite: a boss dies
  through the game's own boss flow because the new instance forgot it was
  the mod's.
- **Causes**, from section 4:
  - the old instance's Harmony patches are still applied (its `OnDestroy`
    didn't unpatch, or threw before reaching `UnpatchAll`);
  - delegates the old assembly attached to entities still fire;
  - objects the old instance spawned are still in the world, and the new
    one spawns its own.
- **Confirm:** Harmony can list owners of a patched method:
  `Harmony.GetPatchInfo(original)?.Owners`. Two owners starting with your
  mod id means a leftover patch. For objects, `nodeinfo` and the scene
  show the duplicates. Doubled log lines with the same prefix but slightly
  different state (one at wave 1, one at wave 40) mean an old handler.
- **Fix:** `OnDestroy` removes everything, patches last-resort included;
  name your own objects with a suffix (Last Stand: `" (LastStand)"`,
  `ModIdentity.IsOurs`) and adopt existing ones on start instead of
  spawning; keep ownership that matters at death in the world, not in an
  in-memory set. Last Stand treats any supported `BossMonster` in the
  arena as its own, because the arena never spawns one itself. Details in
  `07-mod-architecture.md`.

### Modifiers that come back

- **Symptom:** an effect you removed is back after re-entering a room;
  shrines or props appear in a room you meant to be plain; a fix applied in
  `beforePrepare` is gone; the log has `Runtime removal failed; Modifier
  instance with id N not found`.
- **Cause:** a room modifier is data on the node
  (`ZoneManager.nodes[i].modifiers`), not just an actor. World generation
  writes modifiers straight into node data, `AddModifier` appends to it, and
  `RoomModifiers.OnRoomStartServer` re-creates every listed modifier each
  time the room starts, without your callback. Room state saved on leaving
  (`visitedNodesSaveData`) is restored on revisit. The modifier panel
  (`UI_InGame_RoomModDisplay`) is only hidden on a room load, so it can
  outlive a removed modifier.
- **Confirm:** `nodeinfo` lists every modifier on the current node. Compare
  it with what your mod thinks is active.
- **Fix:** remove what you add before the room is left; strip unwanted
  modifiers at world generation and gate later additions; re-apply
  instance tweaks whenever an instance is re-created; remove modifiers by
  the id `AddModifier` returned, and only if that id is still on the node.

### A tick exception that resets the session

- **Symptom:** the mod's state jumps back to its initial value (Last Stand:
  wave 1) after an error.
- **Cause:** an exception in per-frame code propagated, and the next frame
  saw a half-updated or torn-down state and started over. Last Stand's
  wave-10 reset came from the game's `RoomMonsters.SpawnMiniBoss`, which
  calls `SelectMiniBoss` first and throws `ArgumentOutOfRangeException` in
  rooms whose pool has no miniboss-capable monster.
- **Confirm:** the trace just before the reset; the state line
  (`laststand_status`) before and after.
- **Fix:** catch at the tick boundary and keep state (section 2), catch per
  unit of work (a failed spawn skips one monster: `spawning a <tier>
  failed; that spawn was skipped and the wave carries on.`), and remove the
  root cause (Last Stand prefixes `SelectMiniBoss` to hand back its own
  pick).

**Where else this applies:** every one of these is a question to ask
before shipping. Does it touch `.instance` per frame? Does it write synced
state during teardown? What happens on reload? Does it add data to a node?
Can one exception stop the loop?
