# 03. Hooking with Harmony: choosing and writing patches

A mod changes the game's behavior in one of two ways: it calls the game's
API (spawn this, set that SyncVar; see `06-game-world-api.md`), or it
intercepts code the game already runs. This chapter is about the second.
It covers:

1. what Harmony does to a running game, and the instance every mod gets;
2. the patch kinds and their special arguments;
3. how to target any method: public, private, overloaded, property,
   constructor, generic, coroutine, Mirror command, virtual;
4. **the method for choosing a hook**, applied to ten real hooks from Last
   Stand;
5. keeping patches safe, and proving that they applied.

Finding the code to hook (decompiling, searching for callers, scanning
`Dew.Contents.dll` IL) is in `02-reading-the-game.md`. What a host-side
change reaches on vanilla clients is in `04-host-only-networking.md`. How
patches fit into a mod's layers is in `07-mod-architecture.md`.

---

## 1. What Harmony does

### 1.1 Why patching works in this game at all

Shape of Dreams is a Unity game on the **Mono** scripting backend. The
evidence is the game folder: `Shape of Dreams_Data/Managed/` holds real .NET
assemblies (`Dew.Core.dll`, `Dew.Contents.dll`, `Dew.UI.dll`, Mirror,
`0Harmony.dll`), and `MonoBleedingEdge/` holds the runtime. Under Mono, the
game's methods exist as IL (the .NET intermediate bytecode) and are compiled
to machine code by a JIT when first called. That is what lets Harmony
rewrite them at runtime. A game built with IL2CPP compiles IL ahead of time
to C++, and this whole chapter would not apply.

### 1.2 Runtime IL patching

Harmony (the `HarmonyLib` namespace in `0Harmony.dll`) never edits files.
For each method you patch (the **original**) it:

1. reads the original's IL;
2. builds a **replacement** method: your prefixes, then the original IL
   (unless a prefix skipped it), then your postfixes, all wrapped by any
   finalizers, with any transpilers applied to the original IL first;
3. redirects the original to the replacement, so every caller, in every
   assembly, now runs the replacement.

Consequences that matter when you design a hook:

- **A patch is process-wide.** It runs for every instance of the type and
  every caller, on the host and on any client that has your mod loaded. A
  patch on `Room.ClearRoom` runs in the camp, in normal runs, on the title
  screen if it gets called there. Every patch must decide for itself whether
  it applies (section 7.2).
- **All mods share one Harmony.** The game ships one `0Harmony.dll`
  (version 2.3.6.0 at the time of writing). Reference it from `Managed`, do
  not ship your own copy. Two mods patching the same method stack their
  patches on the same replacement.
- **Patch methods are compiled into the replacement.** An exception thrown
  by your prefix or postfix is thrown *by the game's method*, into the
  game's caller (section 7.3).

### 1.3 The mod's built-in `harmony` instance

`ModBehaviour` (in `Dew.Core`) gives every mod a lazily created instance:

```csharp
private Harmony _harmony;

public Harmony harmony
{
    get
    {
        if (_harmony == null)
        {
            _harmony = new Harmony(mod.metadata.id + Guid.NewGuid().ToString());
        }
        return _harmony;
    }
}
```

Two facts follow from that getter:

- **The id is not your mod id.** It is `com.example.mymod` followed by a
  fresh GUID, so every load (and every reload) gets a different id. Anything
  that takes a Harmony id must use `harmony.Id`, never the literal mod id.
- **`mod` must be set before first use.** `mod` is `instance.mod`, and the
  loader sets `instance` right after `AddComponent`. That is safe in `Awake`
  only because `DewMod.Load` creates the container inactive and activates it
  after all components are added:

  ```csharp
  GameObject gameObject = new GameObject(modItem.metadata.id);
  gameObject.SetActive(value: false);
  ...
  ModBehaviour modBehaviour = (ModBehaviour)gameObject.AddComponent(item);
  modBehaviour.instance = loadedModInstance;
  ...
  Debug.Log("[DewMod] Activating " + modItem.metadata.id);
  gameObject.SetActive(value: true);
  ```

  So `Awake` and `Start` both see a ready `harmony`. A field initializer or
  constructor would not.

### 1.4 `PatchAll` versus manual patching

`harmony.PatchAll()` finds every class in your assembly that carries
`[HarmonyPatch]` and applies it. Harmony 2.3.6's implementation:

```csharp
public void PatchAll()
{
    MethodBase method = new StackTrace().GetFrame(1).GetMethod();
    Assembly assembly = method.ReflectedType.Assembly;
    PatchAll(assembly);
}

public void PatchAll(Assembly assembly)
{
    AccessTools.GetTypesFromAssembly(assembly).Do(delegate(Type type)
    {
        CreateClassProcessor(type).Patch();
    });
}
```

- The assembly is taken from the **caller's stack frame**. Call `PatchAll()`
  from your own mod's code (it is), not through a helper in another library.
- **The first failing class aborts the rest.** `Do` is a plain loop and
  `Patch()` throws `HarmonyException` on any error. Classes enumerated
  before the bad one stay patched, the ones after are never patched, and
  the exception leaves your `Awake`/`Start`, so the rest of that method
  does not run either. One typo in one target name can silently disable
  half of a mod. Read `Player.log` after every build (section 8).

Manual patching does the same work for one method, with no attributes:

```csharp
MethodInfo original = AccessTools.Method(typeof(Room), nameof(Room.ClearRoom));
MethodInfo prefix = AccessTools.Method(typeof(MyPatches), nameof(MyPatches.ClearRoomPrefix));
harmony.Patch(original, prefix: new HarmonyMethod(prefix));
```

Use manual patching when the target is only known at runtime (a type found
by name, a coroutine's `MoveNext`, an optional patch that should not abort
the others). Otherwise attributes plus `PatchAll` keep each hook in its own
small file, which is how Last Stand does it. Its entry point's `Start`,
now in the shared library's `ModEntry` (`library/src/ModEntry.cs`), where
`OnLoaded` is Last Stand's override that creates the session:

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
```

The session exists before any patch can fire, and the `loaded:` log line
comes after `PatchAll`, so a missing `loaded:` line in `Player.log` points
straight at a patching failure.

### 1.5 Unpatching in `OnDestroy`

Mods can be unloaded and reloaded at runtime (the mod manager, or the
auto-reload watcher in developer mode). The loader destroys your container
and your `OnDestroy` runs. **The loader does not unpatch for you.**
`DewMod.UnloadAll` destroys containers, removes console commands and runs
manager cleanups. It has no Harmony call. So `OnDestroy` must:

```csharp
protected void OnDestroy()
{
    OnUnloading();
    ...
    harmony.UnpatchAll(harmony.Id);
}
```

Pass the id. Harmony's `UnpatchAll`:

```csharp
public void UnpatchAll(string harmonyID = null)
{
    ...
    bool IDCheck(Patch patchInfo)
    {
        return harmonyID == null || patchInfo.owner == harmonyID;
    }
}
```

With no argument, `IDCheck` is true for **every** patch from every mod. The
official `Mods/ModTemplate/TestMod.cs` calls `harmony.UnpatchAll()` with no
argument. Today's loader unloads all mods together (`ReloadFromActiveMods`
calls `UnloadAll`), which hides the damage, but `UnpatchAll(harmony.Id)` is
correct in every case and costs nothing.

Why unpatching matters so much here: the loader loads your DLL from bytes
after renaming the assembly (`name.Name + "_" + DateTime.Now.Ticks`), so a
reload brings a **second copy** of every type. The old copy stays in memory.
If the old patches stay applied, the game calls into the old assembly's
statics (an old, reset session) next to the new one. See
`07-mod-architecture.md`, section 7, for what else survives a reload.

---

## 2. Anatomy of a patch class

```csharp
[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.ShouldVoteOnTravel))]
internal static class SkipTravelVotePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ref bool __result)
    {
        if (!ModMode.IsHosting())
        {
            return true;
        }

        __result = false;
        return false;
    }
}
```

- The **class** attribute names the original: declaring type plus method
  name (plus argument types or a `MethodType` when needed, section 4).
- Patch methods are `static`. Visibility doesn't matter; `private` is fine.
- The kind comes from an attribute (`[HarmonyPrefix]`, `[HarmonyPostfix]`,
  `[HarmonyFinalizer]`, `[HarmonyTranspiler]`, `[HarmonyReversePatch]`) or
  from the method's name (`Prefix`, `Postfix`, ...). Last Stand uses both:
  the attribute and the matching name.
- The **parameters** are matched by name against the original's parameters
  and a set of special names (section 3.6). The order does not matter.
- One class can patch several methods of one type: put the type on the
  class and the method name on each patch method. `CreateLobbyModePatch`
  (in the shared library, `library/src/Patches/`):

  ```csharp
  [HarmonyPatch(typeof(UI_Title_CreateLobbyView))]
  internal static class CreateLobbyModePatch
  {
      [HarmonyPostfix]
      [HarmonyPatch(SharedGameNames.CreateLobbyShow)]
      private static void OnShow_Postfix(UI_Title_CreateLobbyView __instance) { ... }

      [HarmonyPostfix]
      [HarmonyPatch(SharedGameNames.CreateLobbyUpdateState)]
      private static void UpdateUIState_Postfix(UI_Title_CreateLobbyView __instance) { ... }
  }
  ```

---

## 3. Patch kinds

### 3.1 Prefix: before the original, optionally instead of it

A prefix runs before the original. Return `void` to only observe, or `bool`
to decide: `true` runs the original, `false` skips it. Postfixes still run
after a skipped original.

**Skip and supply the answer.** When you skip a method that returns a value,
set `__result`, or the caller gets the type's default. `SkipTravelVotePatch`
above makes `ShouldVoteOnTravel` return `false` without running it.

**Skip a void method.** `RoomClearPatch` lets the original run unless the
session says no:

```csharp
[HarmonyPrefix]
private static bool Prefix()
{
    return !ModMode.IsHosting() || LastStandMod.Current?.Session?.AllowsRoomClear() != false;
}
```

**Change an argument.** Declare it `ref`, with the original's parameter
name. `HatredShardGoldPatch` rewrites the gold amount of
`PickupManager.DropGold(bool isKillGold, bool isGivenByOtherPlayer, int
amount, Vector3 position, Hero target)`:

```csharp
[HarmonyPatch(typeof(PickupManager), nameof(PickupManager.DropGold))]
internal static class HatredShardGoldPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref int amount)
    {
        if (HatredShards.TryGetGold(out int gold))
        {
            amount = gold;
        }
    }
}
```

**Supply `out` values.** An `out` parameter is declared `ref` in the patch.
`MiniBossSelectPatch` fills both outs of `RoomMonsters.SelectMiniBoss(out
Entity entity, out MiniBossEffect effect, DewRandom random)` and skips the
original (section 6.6).

**Carry data to the postfix.** A prefix can declare `out T __state`; the
postfix of the same patch class declares `T __state` and receives it. Use
it for before/after comparisons. An illustration (not Last Stand code):

```csharp
[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]
internal static class UseCountProbe
{
    private static void Prefix(Shrine __instance, out int __state)
    {
        __state = __instance.totalUseCount;
    }

    private static void Postfix(Shrine __instance, int __state)
    {
        Debug.Log($"[MyMod] {__instance.name} uses {__state} -> {__instance.totalUseCount}");
    }
}
```

**Cooperate with other prefixes.** A prefix can declare `bool
__runOriginal` to read whether an earlier prefix (another mod's, say)
already decided to skip the original. If you only observe, return `void`:
a `void` prefix can never skip anything by accident.

### 3.2 Postfix: after the original

A postfix runs after the original (or after the skip), and sees:

- `__result`: the return value. Declare it `ref` to change it.
- `__instance`: the object the method ran on.
- the original's arguments, by name, with the values *after* the original
  ran (for `ref`/`out` parameters, the final ones).

`KillGoldPatch` adjusts a return value:

```csharp
[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetKillGoldAmount))]
internal static class KillGoldPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref int __result)
    {
        if (__result <= 0 || !ModMode.IsHosting())
        {
            return;
        }

        LastStandSession session = LastStandMod.Current?.Session;
        if (session != null)
        {
            __result = session.CapKillGold(__result);
        }
    }
}
```

`HatredCursePatch` reads arguments of a private method by name:

```csharp
[HarmonyPatch(typeof(Shrine_Hatred), GameNames.HatredDoCurse)]
internal static class HatredCursePatch
{
    [HarmonyPostfix]
    private static void Postfix(HatredStrengthType strength, Hero hero)
    { ... }
}
```

The original is `private void DoCurse(HatredStrengthType strength, Hero
hero)`. The names `strength` and `hero` must match exactly.

### 3.3 Finalizer: always runs, sees exceptions

A finalizer runs after everything else, like a `finally` block around the
original and its patches. It can declare `Exception __exception` to see what
was thrown. A finalizer returning `Exception` replaces the exception
(`null` swallows it); a `void` finalizer lets it through unchanged.

Last Stand uses one to close a scope that a prefix opened, so the scope
cannot leak even when the original throws:

```csharp
[HarmonyPatch(typeof(Shrine_Hatred_Reward), GameNames.ShrineOnUse)]
internal static class HatredShardUsePatch
{
    [HarmonyPrefix]
    private static void Prefix(Shrine_Hatred_Reward __instance)
    {
        if (ModMode.IsHosting())
        {
            LastStandMod.Current?.Session?.OnHatredShardOpening(__instance);
        }
    }

    [HarmonyFinalizer]
    private static void Finalizer()
    {
        HatredShards.Close();
    }
}
```

`Shrine_Hatred_Reward.OnUse` calls `GetSpecialRewardAmount_DreamDust` and
`PickupManager.DropGold` synchronously inside its local functions
`DropDreamDustReward` and `DropGoldReward`. While the scope is open, the
`DropGold` prefix (3.1) and a `GetSpecialRewardAmount_DreamDust` postfix
replace the amounts. Outside it, both pass through. Section 6.11 generalizes
this.

### 3.4 Reverse patch: call the original's own body

A reverse patch copies an original's IL into a stub method of yours, so you
can call that exact body. Last Stand needs this to call a **base-class
implementation non-virtually**:

```csharp
[HarmonyPatch]
internal static class EntityBaseCalls
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Entity), GameNames.EntityOnDeath)]
    public static void OnDeath(Entity instance, EventInfoKill info)
    {
        throw new NotImplementedException();
    }
}
```

After patching, the stub's body *is* `Entity.OnDeath`'s body; the
`throw` is never reached. For an instance method, the first stub parameter
is the instance. Why not reflection: `Entity.OnDeath` is `protected
virtual`. `MethodInfo.Invoke` on a virtual method dispatches to the most
derived override, so invoking `Entity.OnDeath` on a `BossMonster` runs
`BossMonster.OnDeath`, which runs your prefix again. The reverse patch is the
C# `base.OnDeath(info)` call that outside code cannot write. Section 6.10
shows why it is needed. `PainZonePatches` uses the same trick with
`RoomModifierBase.OnCreate` and `OnDestroyActor`.

### 3.5 Transpiler: rewriting the IL itself

A transpiler receives the original's instructions and returns new ones:

```csharp
private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    MethodInfo kill = AccessTools.Method(typeof(Entity), nameof(Entity.Kill));
    MethodInfo filtered = AccessTools.Method(typeof(MyPatches), nameof(MyPatches.KillUnlessProtected));
    foreach (CodeInstruction instruction in instructions)
    {
        if (instruction.Calls(kill))
        {
            instruction.opcode = OpCodes.Call;
            instruction.operand = filtered;
        }
        yield return instruction;
    }
}
```

That sketch would swap the `item.Kill()` call in `BossMonster.OnDeath`'s
kill-all loop for a filter (`KillUnlessProtected(Entity)` taking the same
instance argument). Last Stand does **not** do this. It uses a prefix plus a
reverse patch instead (6.10). Reasons to avoid transpilers:

- **They break on game updates without warning.** A changed loop or an extra
  call changes the IL. A transpiler that matches nothing usually doesn't
  throw: the hook silently stops working.
- **They are unreadable** next to a prefix.
- **They conflict** with other mods' transpilers on the same method in ways
  neither author can see.

Use one only when the behavior sits in the middle of a long method, there is
no narrower method to hook (section 5), and a prefix that re-implements the
method would copy dozens of lines.

### 3.6 Special arguments

| Parameter | Meaning |
|---|---|
| `T __instance` | The object the original runs on (absent for static methods). |
| `T __result` / `ref T __result` | Return value. In a prefix, set it when skipping. In a postfix, read or change it. |
| `out T __state` / `T __state` | Value passed from a prefix to the postfix/finalizer of the same class. |
| `T ___fieldName` | A field of the instance, by name, with three underscores in front. `ref` to write it. |
| `object[] __args` | All arguments, boxed. |
| `MethodBase __originalMethod` | The patched method; useful when one patch method serves several targets. |
| `bool __runOriginal` | Prefix: whether the original will run so far. |
| `Exception __exception` | Finalizer: the exception thrown, or null. |
| `T name` / `ref T name` | An argument of the original, by its exact name. |
| `T __0`, `T __1` ... | An argument by position, when the name is awkward. |

The triple underscore is added to the field name as it is. Shrine's private
use-count table is `_useCountByPlayer`, so the patch parameter is
`SyncDictionary<string, int> ____useCountByPlayer`: four underscores.
Outside a patch, Last Stand reaches the same field with
`AccessTools.Field(typeof(Shrine), GameNames.ShrineUseCounts)`
(`src/Well/UseCountPin.cs`).

`[HarmonyArgument("name")]` on a patch parameter maps it to a differently
named original parameter, if a name would clash with something in your code.

---

## 4. Targeting any method

### 4.1 Public members: `typeof` plus `nameof`

```csharp
[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]
```

`nameof` is checked by the compiler. When a game update renames or removes
the method, the build fails, instead of `PatchAll` failing on the player's
machine.

### 4.2 Private and protected members: one table of strings

`nameof` does not compile on a member you can't access (`CS0122`), so
`private` and `protected` targets need string names. Last Stand keeps its
own in one file, `src/Game/GameNames.cs`:

```csharp
internal static class GameNames
{
    public const string CurseOnCreate = "OnCreate";
    public const string HatredDoCurse = "DoCurse";
    public const string EntityOnDeath = "OnDeath";
    public const string ShrineUseCounts = "_useCountByPlayer";
    public const string ShrineOnUse = "OnUse";
    public const string ActorOnCreate = "OnCreate";
    public const string ActorOnDestroy = "OnDestroyActor";
    ...
}
```

Names both mods use (`GenerateWorld_Imp`, the Create Lobby and lobby
settings methods) are in the shared library's
`library/src/Game/SharedGameNames.cs`. After a game update, those two files
are the checklist: each string is a name the compiler can't verify, so grep
the new decompile for each one. Where a member is public, prefer `nameof`
even inside these files (`nameof(RoomMod_AcceleratedTime)`).

### 4.3 Overloads: give the argument types

`ZoneManager` has three `AddModifier` methods (two generic, one not; see
6.7). A name alone is ambiguous, and Harmony throws `Ambiguous match for
HarmonyMethod[...]`. Add the parameter types:

```csharp
[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.AddModifier),
    new[] { typeof(int), typeof(ModifierData), typeof(Action<RoomModifierBase>) })]
internal static class ModifierGatePatch
```

The list must match the declared parameter types exactly, including
optional parameters (`Action<RoomModifierBase> beforePrepare = null` counts).
For `ref`/`out` parameters, use the `ArgumentType[]` overload of
`[HarmonyPatch]` or `typeof(T).MakeByRefType()` with `AccessTools.Method`.

### 4.4 Properties

A property is two methods, `get_X` and `set_X`. Pick one with `MethodType`:

```csharp
[HarmonyPatch(typeof(Room), nameof(Room.didClearRoom), MethodType.Getter)]
```

`Room.didClearRoom` is `public bool didClearRoom { get; private set; }`.
The getter is public; the setter is private, so patching or calling it uses
the property name plus `MethodType.Setter`, or `AccessTools.PropertySetter`.
Last Stand only *calls* the private setter (`src/Game/RoomCombat.cs`):

```csharp
private static readonly MethodInfo SetCleared =
    AccessTools.PropertySetter(typeof(Room), nameof(Room.didClearRoom));
...
SetCleared.Invoke(room, new object[] { false });
```

Auto-property getters are tiny. See 8.3 on inlining before relying on a
getter patch.

### 4.5 Constructors

`MethodType.Constructor` (plus argument types) targets a constructor;
`MethodType.StaticConstructor` the type initializer. For anything that is a
Unity object (every `Actor`, `Shrine`, `Monster`), the constructor is the
wrong hook: Unity clones prefabs, and Mirror-generated constructors do
plumbing only (`public Shrine_Hatred() { InitSyncObject(choices); }`).
Hook the lifecycle method where the object becomes real: `Awake`/`Start`
for UI, `Actor.OnCreate`, `OnStartServer` or `OnLateStartServer` for
networked actors.

### 4.6 Generic methods: hook the non-generic funnel

Harmony patches a concrete method body. A generic method has no single body:
each instantiation is compiled separately, and instantiations over reference
types can share one compiled body, so a patch on `AddModifier<RoomMod_Hunted>`
may cover other instantiations too, or the wrong one. Avoid it. Generic APIs
in this game usually forward to a non-generic method, and that is the one to
patch:

```csharp
public int AddModifier<T>(int nodeIndex, Action<T> beforePrepare = null) where T : RoomModifierBase
{
    ...
    return AddModifier(nodeIndex, new ModifierData
    {
        id = _nextModifierId++,
        ...
```

Both generic overloads end in `AddModifier(int, ModifierData,
Action<RoomModifierBase>)`, which is the target of `ModifierGatePatch`.

### 4.7 Coroutines and local iterators

The game starts much of its logic as coroutines, often through a local
function:

```csharp
public void ClearRoom()
{
    if (!NetworkServer.active) { ... }
    else
    {
        StartCoroutine(Routine());
    }
    IEnumerator Routine()
    {
        if (!didClearRoom)
        {
            didClearRoom = true;
            ...
```

The C# compiler turns `Routine` into a state machine class whose
`MoveNext()` holds the body. List the generated names with `ilspycmd -l c
Dew.Core.dll` (see `02-reading-the-game.md`):

```text
Class Room.<<ClearRoom>g__Routine|92_0>d
Class BossMonster.<>c__DisplayClass9_0.<<OnDeath>g__RewardRoutine|1>d
Class BossMonster.<>c__DisplayClass9_0.<<OnDeath>g__ReviveRoutine|2>d
Class Shrine_Hatred.<>c__DisplayClass8_0.<<CmdChoose>g__Routine|0>d
```

Read the names carefully:

- `92_0` and `9_0` are compiler ordinals. They change whenever the game's
  source file changes. Never hard-code them.
- A routine that captures locals lives inside a `<>c__DisplayClass` type,
  one level deeper.
- The routine in `Shrine_Hatred` is named after `CmdChoose`, not after the
  method that contains it in the decompile
  (`UserCode_CmdChoose__Int32__NetworkConnectionToClient`). The compiler
  named it from the source; Mirror's weaver renamed the method afterwards
  (4.8).

To patch a `MoveNext`, find the type by a stable part of its name and
target it from a `TargetMethod`:

```csharp
[HarmonyPatch]
internal static class ClearRoutineProbe
{
    private static MethodBase TargetMethod()
    {
        Type routine = AccessTools.FirstInner(typeof(Room),
            t => t.Name.Contains("ClearRoom") && t.Name.Contains("Routine"));
        return AccessTools.Method(routine, "MoveNext");
    }

    private static void Prefix(object __instance) { ... }
}
```

`AccessTools.EnumeratorMoveNext(method)` does the lookup for you, but it
needs the *iterator method itself* (the one that contains exactly one
`newobj`): `<ClearRoom>g__Routine|92_0`, not `ClearRoom`. The attribute form
`[HarmonyPatch(typeof(T), "Method", MethodType.Enumerator)]` does the same
for a method that *is* an iterator (returns `IEnumerator` and contains
`yield`). It doesn't help for `ClearRoom`, which only starts one. Inside `MoveNext`,
`__instance` is the state machine, not the `Room`; the `Room` is the
compiler field `<>4__this`, which you have to read with
`AccessTools.Field`. Every one of these details breaks on updates, so treat
coroutine patches as a last resort and hook something the coroutine
*calls* (6.5 does exactly that with `DoCurse`).

### 4.8 Mirror commands and RPCs: the `UserCode_` methods

Mirror (the game's networking library; see `04-host-only-networking.md`)
uses a post-compile **weaver**. For a method marked `[Command]`, the weaver
moves the original body into a new method and turns the original into a
sender stub. In `ChatManager`:

```csharp
[Command(requiresAuthority = false)]
public void CmdSendChatMessage(string content, NetworkConnectionToClient sender = null)
{
    NetworkWriterPooled writer = NetworkWriterPool.Get();
    writer.WriteString(content);
    SendCommandInternal("System.Void ChatManager::CmdSendChatMessage(System.String,Mirror.NetworkConnectionToClient)", 973844096, writer, 0, requiresAuthority: false);
    NetworkWriterPool.Return(writer);
}

protected void UserCode_CmdSendChatMessage__String__NetworkConnectionToClient(string content, NetworkConnectionToClient sender)
{
    if (content == null) { return; }
    content = content.Trim();
    ...
    BroadcastMessage(message2);
}

protected static void InvokeUserCode_CmdSendChatMessage__String__NetworkConnectionToClient(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
{
    if (!NetworkServer.active)
    {
        Debug.LogError("Command CmdSendChatMessage called on client.");
    }
    else
    {
        ((ChatManager)obj).UserCode_CmdSendChatMessage__String__NetworkConnectionToClient(reader.ReadString(), senderConnection);
    }
}
```

| Method | Runs on | Patch it to... |
|---|---|---|
| `CmdSendChatMessage` | the **sending** client (the host only for its own messages) | change what *this* machine sends |
| `UserCode_CmdSendChatMessage__String__NetworkConnectionToClient` | the **server**, once per message from any player, vanilla clients included | see or handle every player's chat on the host |
| `InvokeUserCode_...` | the server, as the dispatch glue | nothing; it only forwards |

So a host-only mod that reacts to chat (for example, chat commands typed by
vanilla clients) patches the `UserCode_` method. Its name encodes the
parameter types (`__String__NetworkConnectionToClient`) and it is
`protected`, so it is a string in your names table. `[ClientRpc]` and
`[TargetRpc]` follow the same pattern in the other direction: the
`UserCode_Rpc...`/`UserCode_Tpc...` body runs on each client, which a
host-only mod cannot reach on vanilla machines.

Mirror's `[Server]` attribute leaves a similar fingerprint. A method whose
decompiled body starts with

```csharp
if (!NetworkServer.active)
{
    Debug.LogWarning("[Server] function 'System.Void Room::ClearRoom()' called when server was not active");
}
```

was marked `[Server]` and does its work only on the server.

### 4.9 Virtual methods: patch the declaring type

A patch attaches to one method body. For a virtual method that body belongs
to the type that **declares** it (or overrides it):

- `[HarmonyPatch(typeof(BossMonster), "OnDeath")]` patches
  `BossMonster.OnDeath`, the override declared in `BossMonster`.
- **Attribute targets are declared-only.** Harmony resolves a
  `[HarmonyPatch]` target with `AccessTools.DeclaredMethod`.
  `Mon_Forest_BossDemon` doesn't override `OnDeath`, so
  `[HarmonyPatch(typeof(Mon_Forest_BossDemon), "OnDeath")]` fails with
  `Undefined target method`. Name the type that declares the body.
- **Manual lookups walk up.** `AccessTools.Method(typeof(Mon_Forest_BossDemon),
  "OnDeath")` searches base types and returns `BossMonster.OnDeath`, so a
  manual patch through it changes the death of **every** boss. Either way,
  filter on `__instance` if you want one subtype.
- **An override that doesn't call `base` never reaches your patch.** Most
  curses do `base.OnCreate()` first, but not this one:

  ```csharp
  public class Se_Curse_DreamAfflictionDissociative : CurseStatusEffect
  {
      protected override void OnCreate()
      {
      }
  ```

  A patch on `CurseStatusEffect.OnCreate` never runs for it.
- **An override that does call `base` runs your patch in the middle of its
  own work.** `Mon_Primus_BossPrimusAeron.OnDeath` calls `base.OnDeath(info)`
  and then starts the game's ending. A prefix on `BossMonster.OnDeath` that
  returns `false` skips only `BossMonster`'s part; Primus's code still runs.

Before patching a virtual method, grep for every `override` of it (in
`Dew.Contents` too) and read whether each one calls `base`, and where.

---

## 5. The method: choosing a hook

Most bad patches hook the first method found that seems related. The
procedure below finds the one method where a change is both complete
(covers every case) and contained (changes nothing else).

1. **State the behavior as an observable effect**, not as code: "a vanilla
   client is locked out of the shrine after one use", "kill gold is too high
   with many players", "the miniboss spawn throws". You'll check the final
   hook against this sentence.
2. **Find the code that produces the effect.** Search the decompile for the
   field, the log string, the localization key, or the type in the effect
   (`02-reading-the-game.md`). For a value, find every *write* to it.
3. **List every path that reaches it.** Search for callers of that code,
   then callers of those, until you reach entry points (a command, an RPC,
   an `Update`, an event). Include `Dew.Contents`, which doesn't decompile
   as a whole (`02-reading-the-game.md` shows the IL scan). Write the list
   down. It is the coverage you need.
4. **Find the narrowest funnel.** Pick the method that *every* path in your
   list goes through and as few other paths as possible go through. Wider
   than that, and you change unrelated behavior and must filter. Narrower,
   and some path escapes you. If no single method covers all paths, use two
   hooks (6.7), not one wide hook.
5. **Check where it runs.** Host-only mods only change what runs on the
   server. Look for `NetworkServer.active`/`isServer` checks, `[Server]`
   warnings, `UserCode_Cmd...` callers, and client-only calls
   (`DewPlayer.local`, `InGameUIManager`). If the method runs on both,
   guard on `NetworkServer.active`.
6. **Check how often it runs per event.** Exactly once per event is ideal.
   More often (every frame, once per client, twice for a double reward)
   means your handler must be idempotent or count. Note what "event" means
   for your effect: per shrine created, per use, per death.
7. **Check the order of side effects.** List what the method and its callers
   do before and after the point you hook: which values exist yet, which
   SyncVars are already written, which callbacks already ran. A prefix sees
   the world before all of it, a postfix after. Pick the side where the data
   you need exists and your change still matters.
8. **Choose the kind, preferring observation.** In order of preference:
   a `void` postfix that observes and calls your code; a postfix that adjusts
   `__result` or a field; a prefix that adjusts arguments; a prefix that
   skips the original. A skip throws away everything the original does,
   including what a game update later adds to it.
9. **Write the hand-off.** The patch checks the mode and calls one method of
   your own (`07-mod-architecture.md`, section 3.1).

The checklist, compressed:

| Question | Bad sign |
|---|---|
| Does every path reach it? | A caller you found bypasses it |
| Does anything else reach it? | It's a shared utility (`DropGold`, `CreateActor`) |
| Does it run on the host? | Only `DewPlayer.local` or UI code calls it |
| How often per event? | Per frame, per client, or "sometimes twice" |
| What's already done when it runs? | The value you need is set later, in a callback or a coroutine |
| Do you need to skip anything? | You're copying the original's body into your prefix |

---

## 6. Worked examples

All ten hooks below are in `src/Patches/` and were seen working in play.

### 6.1 `ArenaShrineUsePatch`: postfix on `Shrine.DoPostUseRoutines`

**Problem.** Last Stand's Dream Well has to be usable without limit by
vanilla clients, and its Shrine of Destiny has to allow a set number of
claims per wave. `Shrine.maxUseCountPerPlayer` and `useCooldown` are not
SyncVars, so changing them on the host changes nothing for clients: each
client's own `CanInteract` locks it out after one use.

**Paths.** The client check reads the synced use count:

```csharp
public virtual bool CanInteract(Entity entity)
{
    if (isAvailable && Time.time - base.creationTime > interactableDelay && !isOnCooldown)
    {
        if (maxUseCountPerPlayer >= 0)
        {
            return GetUseCountByPlayer(entity.owner) < maxUseCountPerPlayer;
```

So the fix is to reset the synced count after each use, and the hook is
"after each successful use". Searching for writes to `_useCountByPlayer`
gives one line in the whole game, in `Shrine.DoPostUseRoutines`:

```csharp
public void DoPostUseRoutines(Entity entity)
{
    Cost? cost = GetCost(entity);
    totalUseCount++;
    _useCountByPlayer[entity.owner.guid] = GetUseCountByPlayer(entity.owner) + 1;
    FxPlayNewNetworked(useEffect, entity);
    TpcShowLocalUseEffect((NetworkConnectionToClient)entity.owner, entity);
    if (cost.HasValue)
    {
        entity.owner.Spend(cost.Value);
    }
    Network_lastUseTime = NetworkTime.time;
    UpdateIsAvailable();
    RpcInvokeOnSuccessfulUse(entity);
}
```

Its callers are every kind of use: `Shrine.OnInteract` (plain shrines),
`EditSkillShrine` (two calls: the well, the altar, the Maw of Doom, the
Ascension), `ChoiceShrine`, `Shrine_Chaos`, and in `Dew.Contents`
`Shrine_Destiny`, `Shrine_Hatred`, `Shrine_Entanglement`,
`Shrine_MirrorOfRemorse`, `Shrine_CorruptedChaos`,
`Shrine_MarshOfDestiny_SeedOfTorment`.

**Why this method.** It is the funnel for "a use happened" across every
shrine type. The per-type alternatives (`OnUse`, `abstract` and different in
each subclass, or `OnActivateEditSkill`) would each need a patch, and some
(`Shrine_Hatred`'s `CmdChoose`) call it from a coroutine. It is non-virtual,
so no override can bypass it. It runs only on the server (every caller is a
server path). It runs once per successful use. And by the end of it, the
count, the cost and `_lastUseTime` are all written, which is exactly what
the handler resets.

**Kind.** `void` postfix: the use must happen as normal; the mod only acts
after it.

```csharp
[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]
internal static class ArenaShrineUsePatch
{
    [HarmonyPostfix]
    private static void Postfix(Shrine __instance, Entity entity)
    {
        if (ModMode.IsHosting())
        {
            LastStandMod.Current?.Session?.OnShrineUsed(__instance, entity);
        }
    }
}
```

**Hand-off.** `Session.OnShrineUsed` passes the shrine to each Last Stand
shrine service (`ArenaShrines.OnUsed`), and each one ignores shrines it
doesn't own. The Destiny service, for one, counts claims and writes the
synced count back with `UseCountPin.Set`.

### 6.2 `KillGoldPatch`: postfix on `GameManager.GetKillGoldAmount`

**Problem.** An optional setting caps the gold a regular kill drops per
player. Last Stand's own bonus gold and environment gold must stay uncapped.

**Paths.** The death drop is `PickupManager.HandleDeathDrop`:

```csharp
int killGoldAmount = NetworkedManagerBase<GameManager>.instance.GetKillGoldAmount(obj.victim);
if (flag)
{
    Pickup_BaseGoldOrb.GrantGold(killGoldAmount, null, isKillGold: true, isGivenByOtherPlayer: false);
}
else
{
    DropGold(isKillGold: true, isGivenByOtherPlayer: false, killGoldAmount, obj.victim.position);
}
```

Two delivery paths: an orb (`DropGold`), or, when the host is under
performance pressure (`IsUnderPerfPressure()`), a direct grant that never
calls `DropGold`. A search for `GetKillGoldAmount` finds this one caller.

**Why this method.** `DropGold` is the first idea and the wrong one: it
misses the perf-pressure path, and it is shared by every gold source (shard
gold, Last Stand's bonus gold, `RoomMod_GoldEverywhere`), so it would need
filtering. `GetKillGoldAmount` is the computation both delivery paths
consume, and nothing else calls it, so the cap reaches kill gold and only
kill gold. It runs on the server (`HandleDeathDrop` is server-side) once per
death.

**Kind.** Postfix that adjusts `__result` (3.2). The original runs in full,
including its random rounding, and the cap is applied to what it produced.
The `__result <= 0` early exit skips non-monsters (the original returns 0
for them) cheaply. Hunters never get here: `HandleDeathDrop` returns before
the call when `Se_HunterBuff` disables gold.

**Hand-off.** `Session.CapKillGold` calls the pure rule
`KillGoldRule.Cap(amount, perPlayer, players)`, which returns `amount`
untouched when the setting is 0.

### 6.3 `ChaosRarityPatch`: postfix on `Shrine_Chaos.SetRandomRarity`

**Problem.** Miniboss chaos rewards came out legendary nearly every time.
Their rarity should follow a per-wave table instead.

**Paths.** The rarity is the SyncVar `Shrine_Chaos.rarity`, written through
the weaver-generated setter `Networkrarity`. Its writers: `SetRandomRarity`
(the roll from the prefab's `chance`/`highChance` tables) and one direct
write in `Ai_RandomGemUpgrade`. `SetRandomRarity`'s callers are both in
`RoomRewards.DropChaosReward`, inside the `beforePrepare` callback of
`Dew.CreateActor`:

```csharp
Dew.CreateActor(Dew.GetGoodRewardPosition(pos), null, null, delegate(Shrine_Chaos chaos)
{
    chaos.SetRandomRarity(isHighQuality);
});
```

and once more for each player who rolls `doubleChaosChance`.

**Order of side effects.** `beforePrepare` runs before the actor is
prepared and spawned (`Dew.PrepareAndSpawnActor_Imp` calls it, then
`PrepareAndSpawn`). The reward list is built later, in
`OnLateStartServer`, from `poolByRarity.Get(rarity)`. So any rarity written
during `SetRandomRarity` is in place before the choices are built, and the
choices match the color clients see.

**Why this method.** It runs once per chaos shrine, on the server, at the
one moment the rarity is decided. Dead ends: a postfix on `DropChaosReward`
runs after the shrines exist and can't reach the double-chaos one cleanly;
a JSON override of the odds is impossible (`PerRarityData<T>` is not an
allowed override type, see `05-assets-and-data.md`) and would apply to every
run, not just Last Stand. The `Ai_RandomGemUpgrade` path sets its own
rarity and is deliberately left alone.

**Kind.** `void` postfix that overwrites what the original wrote. The
original's roll still consumes its random number, so the game's random
sequence is unchanged for everything after it.

```csharp
[HarmonyPostfix]
private static void Postfix(Shrine_Chaos __instance)
{
    if (ModMode.IsHosting())
    {
        LastStandMod.Current?.Session?.RerollChaos(__instance);
    }
}
```

**Hand-off.** `ChaosRarity.Reroll` sets `shrine.Networkrarity`, the
generated SyncVar setter, so clients receive the new value. Writing the
plain field `rarity` would change the host only (`04-host-only-networking.md`).

### 6.4 `CurseGoalPatch`: postfix on `CurseStatusEffect.OnCreate`

**Problem.** A curse ends after N kills, N rooms entered, or the next world.
Nobody travels in Last Stand, and vanilla kill goals (28 to 50) are seconds
of play. Every hero curse should become a kill goal sized to the wave.

**Paths.** Curses are created in more than one place: `Shrine_Hatred.DoCurse`
and `Shrine_MarshOfDestiny_SeedOfTorment` (its `CurseThisZone` penalty) both
call `CreateStatusEffect` with a curse. They all create a `CurseStatusEffect` subclass, and every actor runs
`OnCreate` once, guarded by `Actor.InvokeOnCreateIfDidnt`.

**Order of side effects.** `DoCurse` sets the goal in the create callback:

```csharp
hero.CreateStatusEffect(asset, hero, new CastInfo(hero, hero), delegate(CurseStatusEffect curse)
{
    curse.currentStrength = strength;
    curse.skillLevel = (int)(strength - 1);
    if (UnityEngine.Random.value < 0.5f)
    {
        curse.progressType = QuestProgressType.Kills;
        ...
```

That callback runs before `OnCreate`. So a prefix on `DoCurse` or on
`CreateStatusEffect` would be overwritten by the callback. By the end of
`CurseStatusEffect.OnCreate`, the goal is final and the quest exists:

```csharp
if (Networkquest.IsNullOrInactive())
{
    Networkquest = NetworkedManagerBase<QuestManager>.instance.StartQuest(delegate(Quest_KillToLiftCurse q)
    {
        q.NetworktargetEffect = this;
    });
}
```

The quest copies the goal only when it starts and on
`CurseEvent_OnProgressChanged`. So the handler changes the goal and then
fires that event.

**Where it runs.** `OnCreate` runs on every machine (it shows the center
message and chat line on clients). Only the server part matters, hence the
extra `NetworkServer.active` guard.

**Why this method.** It is the one funnel for "a curse now exists with its
goal set", whatever created it. The miss is `Se_Curse_DreamAfflictionDissociative`,
whose `OnCreate` doesn't call `base` (4.9), which is acceptable because it
never starts a goal quest. The target is `protected`, so its name lives in
`GameNames.CurseOnCreate`.

```csharp
[HarmonyPatch(typeof(CurseStatusEffect), GameNames.CurseOnCreate)]
internal static class CurseGoalPatch
{
    [HarmonyPostfix]
    private static void Postfix(CurseStatusEffect __instance)
    {
        if (NetworkServer.active && ModMode.IsHosting())
        {
            LastStandMod.Current?.Session?.OnCurseCreated(__instance);
        }
    }
}
```

Note what the postfix on the base method means for subclasses: a curse
whose override calls `base.OnCreate()` first reaches the postfix *before*
its own server code runs. The handler only touches base-class SyncVars, so
that order is fine.

**Hand-off.** `Session.OnCurseCreated` filters (`CurseGoals.EndsByKillsOrTravel`:
hero victim, goal type Kills, Travel or TravelZone), sizes the goal with the
pure `CurseGoalRule.Kills`, and `CurseGoals.SetKills` writes the synced
properties and invokes `CurseEvent_OnProgressChanged`.

### 6.5 `HatredCursePatch`: postfix on `Shrine_Hatred.DoCurse`

**Problem.** In the "curse to start" option, one player's pick at the
Hatred Remnant curses everyone with the same strength. The hook must know
who chose and which strength.

**Paths.** The pick arrives as a Mirror command, and the body is a
coroutine:

```csharp
protected void UserCode_CmdChoose__Int32__NetworkConnectionToClient(int index, NetworkConnectionToClient sender)
{
    StartCoroutine(Routine());
    IEnumerator Routine()
    {
        ...
                DoPostUseRoutines(player.hero);
                if (choices[index] != 0)
                {
                    yield return new WaitForSeconds(0.75f);
                    DoCurse(choices[index], player.hero);
                }
```

**Dead ends.** A postfix on `UserCode_CmdChoose...` runs when the command
returns, which is at the first `yield`, 0.75 s before the curse exists, and
it sees an index, not a strength. A patch on the iterator's `MoveNext`
(4.7) would work but depends on compiler names. A postfix on
`DoPostUseRoutines` runs for every shrine and also before the curse.

**Why this method.** `DoCurse(HatredStrengthType strength, Hero hero)` is
the one call where the chooser and the strength are both arguments, and its
postfix runs after `CreateStatusEffect` for the chooser has returned. It runs once per pick, on
the server (the command runs only there). It skips `None` itself. It is
`private`, so its name is `GameNames.HatredDoCurse`.

**Kind.** `void` postfix reading arguments by name (3.2). The chooser's own
curse needs nothing from this patch: its goal is fixed by `CurseGoalPatch`
when its `OnCreate` runs, like every other curse's.

**Hand-off.** `Session.OnHatredCurse(hero, strength)` checks that the wave
is on hold, curses the other heroes with `SharedCurses.CurseOthers`, and
schedules the wave.

### 6.6 `MiniBossSelectPatch`: prefix on `RoomMonsters.SelectMiniBoss`

**Problem.** Wave 10 reset the run in some arena rooms. The log showed an
`ArgumentOutOfRangeException` from the game's miniboss spawn, even though
Last Stand passes its own monster and effect.

**Paths.** `SpawnMiniBoss` always selects first, then uses the arguments:

```csharp
public void SpawnMiniBoss(SpawnMonsterSettings settings, Entity ent = null, StatusEffect se = null)
{
    SelectMiniBoss(out var entity, out var effect, settings.random);
    if (ent == null)
    {
        ent = entity;
    }
    ...
```

and `SelectMiniBoss` indexes a list built from the *room's* monster pool:

```csharp
foreach (MonsterPool.SpawnRuleEntry filteredEntry in defaultRule.pool.GetFilteredEntries())
{
    if (filteredEntry.monster.asset is ISpawnableAsMiniBoss && !list2.Contains(filteredEntry.monster.asset))
    {
        list2.Add(filteredEntry.monster.asset);
    }
}
...
entity = list2[random.Range(0, list2.Count)];
```

Boss rooms whose pool has no miniboss-capable monster leave `list2` empty,
and `list2[0]` throws.

**Why this method.** The bug is in `SelectMiniBoss`, and only its result is
wrong for Last Stand. Replacing `SpawnMiniBoss` instead would mean copying
about forty lines (the spawn rule, the miniboss effect, the reward on death)
into the mod and keeping them in step with updates. Skipping just the
selection keeps all of that vanilla.

**Kind.** Prefix that fills the `out` parameters (declared `ref`) and skips
the original, but **only inside Last Stand's own call**:

```csharp
[HarmonyPrefix]
private static bool Prefix(ref Entity entity, ref MiniBossEffect effect)
{
    if (!MiniBossPick.TryTake(out Entity picked, out MiniBossEffect pickedEffect))
    {
        return true;
    }

    entity = picked;
    effect = pickedEffect;
    return false;
}
```

The scope is set around the call in `MonsterSpawner`:

```csharp
MiniBossPick.Set(boss, effect);
try
{
    room.monsters.SpawnMiniBoss(settings, boss, effect);
}
finally
{
    MiniBossPick.Clear();
}
```

The patch has no mode check: outside that `try`, no pick is pending, so it
returns `true` and every other miniboss spawn in the game is vanilla.

### 6.7 `ModifierGatePatch`: prefix on `ZoneManager.AddModifier`

**Problem.** Room modifiers put shrines, props, rifts and extra minibosses
into the arena. Last Stand wants a plain arena, but must still add its own
modifiers (Pain environment, hunter zone, wave environments) and keep the
game's `RoomMod_HeroSoul`, which co-op revives depend on.

**Paths.** Two kinds of writes put modifiers on a node:

1. **World generation** writes them straight into node data, without
   `AddModifier`.
2. **Everything later** goes through `AddModifier`: Corrupted Chaos,
   `RoomMod_SpawnMiniBoss` on travel, `UpdateModifiersByHunterStatus`,
   `Se_HeroKnockedOut` adding `RoomMod_HeroSoul`, and Last Stand's own.

No single method covers both, so there are two hooks:
`WorldGenPatch` (a postfix on the private `ZoneManager.GenerateWorld_Imp`
that strips the arena node once) and this gate for everything after.

**Why this overload.** The two generic `AddModifier<T>` overloads forward to
`AddModifier(int nodeIndex, ModifierData mod, Action<RoomModifierBase>
beforePrepare = null)` (4.6). That body starts by removing the node's other
main modifier, then writes the node's synced list, then may create the
modifier live. A prefix is the only point before all of those side effects.

**Kind.** Prefix that skips and supplies `__result`:

```csharp
[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.AddModifier),
    new[] { typeof(int), typeof(ModifierData), typeof(Action<RoomModifierBase>) })]
internal static class ModifierGatePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ZoneManager __instance, int nodeIndex, ModifierData mod, ref int __result)
    {
        if (!ModMode.IsHosting() || LastStandMod.Current?.Session?.AllowsModifier(__instance, nodeIndex, mod.type) != false)
        {
            return true;
        }

        __result = mod.id;
        return false;
    }
}
```

Callers expect an id back (`RemoveModifier` takes it later), so the skip
returns the id the generic wrapper already assigned. A later
`RemoveModifier` with that id finds nothing in the node's list and removes
nothing (`FindIndex` returns -1), and the room's runtime removal of an
unknown id only logs.

**Hand-off.** `ModifierGate.Allows` lets through anything not on an arena
node, `RoomMod_HeroSoul`, and anything added inside `ModifierGate.AddOwn`,
which raises a counter around Last Stand's own call (the same scoped-context
idea as 6.6):

```csharp
public static int AddOwn(ZoneManager zones, int nodeIndex, string type, Action<RoomModifierBase> beforePrepare)
{
    _ownAdds++;
    try
    {
        return zones.AddModifier(nodeIndex, new ModifierData { type = type }, beforePrepare);
    }
    finally
    {
        _ownAdds--;
    }
}
```

Blocked adds are logged: `[LastStand] blocked room modifier <type> on arena
node <n>.`

### 6.8 `RoomClearPatch`: prefix on `Room.ClearRoom`

**Problem.** The arena loads already cleared (`didClearRoom` true), and
several environments stop working in a cleared room. While an environment
is live, Last Stand sets `didClearRoom` back to false through its private
setter (4.4). But then the game's own "room is done" logic would fire
whenever the wave thins out: destroy idle monsters, fire `onRoomClear`.

**Paths.** Callers of `ClearRoom`: `RoomMonsters` and `RoomSection` (both
from coroutines that wait for aggroed enemies, then call it), plus in
`Dew.Contents` `Shrine_TheConsortOfNight`, `Shrine_CallOfTheRavenous`,
`RoomMod_Ambush`, `RoomMod_DistantMemories`. All of them call the one public
method.

**Why this method.** `ClearRoom` is the funnel for "the room becomes
cleared", is server-only (the `[Server]` warning in 4.8), and its side
effects are all inside the coroutine it starts. Blocking each caller would
mean six patches, two of them in coroutines.

**Kind.** Prefix that skips (3.1), active only while Last Stand holds the
room open; otherwise it returns `true` and the room clears as normal. It
logs `environment: blocked the room's clear routine; the arena stays in
combat until the wave ends.`

**Hand-off.** `Session.AllowsRoomClear` reads `RoomCombat.IsHeldOpen`.

### 6.9 `SkipTravelVotePatch`: prefix on `ZoneManager.ShouldVoteOnTravel`

**Problem.** In multiplayer, the camp's rift should take the party straight
to the arena, with no world-map vote.

**Paths.** The decision is a predicate:

```csharp
public bool ShouldVoteOnTravel()
{
    if (forceVoteForDebug.HasValue)
    {
        return forceVoteForDebug.Value;
    }
    if (NetworkedManagerBase<GameSettingsManager>.instance.enableVotes)
    {
        return DewPlayer.gamePlayers.Count((DewPlayer p) => !p.hero.IsNullInactiveDeadOrKnockedOut()) > 1;
    }
    return false;
}
```

Its callers: `UserCode_CmdTravelToNode...` and `UserCode_CmdTravelToNextZone...`
(server commands that start a vote or travel), `TravelWithValidationAndConfirmation`
(client UI, uses `DewPlayer.local`), `Rift_Sidetrack` and
`Shrine_TheConsortOfNight_Rift`.

**Where it runs.** The decision that matters is on the server:

```csharp
if (ShouldVoteOnTravel())
{
    StartVoteNextNode(player, index);
}
else
{
    TravelToNode(index);
}
```

The client-side caller only decides whether to run a local pre-check
before sending the command, and a host-only patch can't change it on
vanilla clients anyway. Patching the predicate on the host makes every
server path travel at once.

**Dead end.** `GameSettingsManager.enableVotes` is synced and would also
work, but the lobby saves it as the host's preference, so clearing it would
leak into the host's normal runs. Patching the predicate changes behavior
without touching saved state.

**Kind.** Prefix that skips and supplies `__result = false` (section 2).
There is no hand-off: the rule is the mode itself.

### 6.10 `HeroicBossDeathPatch`: prefix on `BossMonster.OnDeath`, plus a reverse patch

**Problem.** Last Stand spawns zone bosses as wave monsters. A boss death
must not run the game's boss-kill flow:

```csharp
protected override void OnDeath(EventInfoKill info)
{
    base.OnDeath(info);
    if (!base.isServer) { return; }
    if (!NetworkedManagerBase<ActorManager>.instance.allEntities.Any((Entity e) => e != this && e is BossMonster))
    {
        NetworkedManagerBase<GameManager>.instance.isGameTimePausedByGame = true;
    }
    foreach (Entity item in new List<Entity>(NetworkedManagerBase<ActorManager>.instance.allEntities))
    {
        if (item.isActive && !item.Status.isDead && item is Monster { type: not MonsterType.Boss } monster && !(monster is IDontKillOnBossMonsterDeath))
        {
            item.Kill();
        }
    }
    ...
    if (!skipBossSoulFlow)
    {
        NetworkedManagerBase<ActorManager>.instance.StartCoroutine(RewardRoutine());
    }
    NetworkedManagerBase<ActorManager>.instance.StartCoroutine(ReviveRoutine());
```

That pauses the game timer (only a room load unpauses it), kills the whole
wave, and starts reward and revive coroutines.

**Options weighed.**

- `skipBossSoulFlow` (a SyncVar, set at spawn) stops only the reward
  routine. The pause, the kill-all and the revive remain.
- A transpiler removing the pause and kill-all (3.5) is fragile.
- A prefix that skips `BossMonster.OnDeath` drops all of it, but also
  `base.OnDeath(info)`, the `Entity` part every entity's death runs.

**Chosen.** A prefix that skips `BossMonster.OnDeath`, runs the base body
through a reverse patch (3.4), then hands off:

```csharp
[HarmonyPatch(typeof(BossMonster), GameNames.EntityOnDeath)]
internal static class HeroicBossDeathPatch
{
    [HarmonyPrefix]
    private static bool Prefix(BossMonster __instance, EventInfoKill info)
    {
        if (!ModMode.IsHosting() || LastStandMod.Current?.Session?.OwnsHeroicBoss(__instance) != true)
        {
            return true;
        }

        EntityBaseCalls.OnDeath(__instance, info);
        LastStandMod.Current.Session.OnHeroicBossDied(__instance);
        return false;
    }
}
```

`Entity.OnDeath` is empty today (`protected virtual void OnDeath(EventInfoKill
info) { }`). The reverse patch still calls it, so whatever a game update
adds there keeps running.

**Virtual-method caveats (4.9).** Bosses whose class overrides `OnDeath`
(`Mon_Primus_BossPrimusAeron`, `Mon_Ink_BossDarkMoon`) would reach this
prefix only through their own `base.OnDeath(info)`, and then run their own
death logic anyway. Last Stand does not spawn them: the catalog skips any
boss type that overrides `OnDeath` anywhere between itself and
`BossMonster`.

**Reload caveat.** `OwnsHeroicBoss` must still say yes after a mod reload,
when the new session's spawn list is empty. It falls back to a check from
the world: a supported `BossMonster` type, in the arena node. See
`07-mod-architecture.md`, section 7.3.

**Hand-off.** `Session.OnHeroicBossDied` kills the boss's descendants
(pillars, hallucinations) and drops the reward.

### 6.11 The pattern behind 3.3, 6.6 and 6.7: a scoped context

Three hooks above use the same trick to change a **shared** method only
when Last Stand's own call is on the stack:

| Scope opened by | Shared method changed | Closed by |
|---|---|---|
| prefix on `Shrine_Hatred_Reward.OnUse` | `DropGold` amount, `GetSpecialRewardAmount_DreamDust` result | finalizer on `OnUse` |
| `MiniBossPick.Set` before `SpawnMiniBoss` | `SelectMiniBoss` | `finally` → `Clear` |
| `ModifierGate.AddOwn` counter | `AddModifier` gate | `finally` → decrement |

Rules for it: the inner call must be synchronous (no coroutine between open
and close), the close must be in a `finally` or finalizer, and the scope
must hold no state that outlives the call. Unity runs game code on one
thread, so a static field is enough.

### 6.12 Summary

| Patch | Target | Kind | Why there |
|---|---|---|---|
| `ArenaShrineUsePatch` | `Shrine.DoPostUseRoutines` | postfix | only writer of use counts; every shrine type funnels here |
| `KillGoldPatch` | `GameManager.GetKillGoldAmount` | postfix, `__result` | feeds both delivery paths, nothing else uses it |
| `ChaosRarityPatch` | `Shrine_Chaos.SetRandomRarity` | postfix | runs before rewards are built from the rarity |
| `CurseGoalPatch` | `CurseStatusEffect.OnCreate` | postfix | after the create callback, for every curse source |
| `HatredCursePatch` | `Shrine_Hatred.DoCurse` | postfix | chooser and strength as arguments, after the delay |
| `MiniBossSelectPatch` | `RoomMonsters.SelectMiniBoss` | prefix, skip in scope | the throwing step only |
| `ModifierGatePatch` | `ZoneManager.AddModifier(int, ModifierData, Action)` | prefix, skip | non-generic funnel, before any side effect |
| `RoomClearPatch` | `Room.ClearRoom` | prefix, skip | one funnel for six callers |
| `SkipTravelVotePatch` | `ZoneManager.ShouldVoteOnTravel` | prefix, `__result` | the server's only vote decision |
| `HeroicBossDeathPatch` | `BossMonster.OnDeath` | prefix, skip + reverse patch | skip the boss flow, keep the base death |

Seven of ten are observers or result adjusters. The three that skip do so
for a stated reason (a bug, a gate, a whole flow to drop) and only under a
narrow condition.

---

## 7. Patch hygiene

### 7.1 Thin patches

A patch hooks and hands off: guard, find the service, call one method. Rules
and state live elsewhere. The full argument is in `07-mod-architecture.md`,
section 3.1. Harmony-specific reasons: the patch file is what a game update
breaks, so it should hold nothing else; and patch methods can't hold
instance state anyway.

### 7.2 Guard: "am I the server, and is my mode on?"

Every patch runs for every call in every context (1.2). Last Stand's guard,
`ModMode`, now in the shared library (`library/src/Mode/ModMode.cs`); each
mod's DLL compiles its own copy, which compares with that mod's
`ModIdentity.SaveKey`:

```csharp
internal static class ModMode
{
    public static bool IsSelected()
    {
        GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
        return settings != null && settings.gameSettingsSaveKey == ModIdentity.SaveKey;
    }

    public static bool IsHosting()
    {
        return NetworkServer.active && IsSelected();
    }
}
```

- `NetworkServer.active` first: a client that has the mod loaded too gets
  the same synced save key, so the mode alone would make its patches act.
- `softInstance`, not `instance`: the guard runs on hot paths, and
  `instance` searches the scene when the manager is missing.
- Methods that run on every machine (`OnCreate`, 6.4) keep their own
  `NetworkServer.active` check even inside a mode that implies hosting.
- The fallback is always vanilla: return `true` from a prefix, leave
  `__result` alone, return from a postfix. With `?.` and `!= false`/`!= true`,
  a missing session (during a reload) also means vanilla.

Patches that are scoped (6.11) are guarded by the scope instead.

### 7.3 Never throw from a patch

An exception from a patch leaves the game's method halfway. Some callers
catch and log (`Actor.InvokeOnCreateIfDidnt` logs `Exception occured on
<path>::OnCreate` and carries on), most don't. Wrap anything that can fail,
and log with a throttle if it can repeat:

```csharp
[HarmonyPrefix]
private static bool Prefix(Se_HeroKnockedOut __instance)
{
    if (!ModMode.IsHosting())
    {
        return true;
    }

    try
    {
        HeroSouls.Suppress(__instance);
    }
    catch (Exception e)
    {
        Log.Error(LogText.HeroSoulFailed, e);
    }

    return false;
}
```

For a patch on a hot method (per death, per spawn), report through a rate
limiter like Last Stand's `ErrorThrottle` (`07-mod-architecture.md`,
section 6.1).

### 7.4 Idempotence

A hook can fire more often than the event you care about.
`LobbySettingsPatch` (shared library) patches both `Start` and `OnEnable` of
`UI_Lobby_SettingsWindow`, so the injection runs on every opening;
`LobbyConfigButton.Ensure` checks for its own button (named with a fixed
suffix) before creating one. A death handler
can see the same entity twice (a kill and a cleanup). Write handlers so a
second call changes nothing.

### 7.5 Reloads

On a reload, the old assembly stays loaded (1.5). With
`UnpatchAll(harmony.Id)` in `OnDestroy`, its patches are gone. What stays:

- C# event handlers your old code added to game objects
  (`entity.EntityEvent_OnDeath += ...`) still call into the old assembly.
- Coroutines your old code started on game objects keep running.
- Game objects your old code spawned are still in the world.

So a patch's handler must recognize "ours" from the world, not only from a
list in memory (6.10). `07-mod-architecture.md`, section 7, covers the
rest.

---

## 8. Proving a patch applied and fires

### 8.1 Read `Player.log` after every build

The log is at
`%USERPROFILE%\AppData\LocalLow\Lizard Smoothie\Shape of Dreams\Player.log`
on Windows, and under
`steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log`
with Proton on Linux. A healthy load shows the loader's lines, then your
own:

```text
[DewMod] Initialized com.example.mymod::MyMod
[DewMod] Activating com.example.mymod
[MyMod] loaded: My Mod v1.0.0.
```

If your "loaded" line is missing, look above it for a `HarmonyException`.

### 8.2 Failure messages and their causes

| Message (Harmony 2.3.6) | Cause | Fix |
|---|---|---|
| `Patching exception in method null` with inner `Undefined target method for patch method ...` | No method with that name on that type: typo, renamed in an update, wrong declaring type | Grep the current decompile; for public members switch to `nameof` |
| `Ambiguous match for HarmonyMethod[...]` / `Ambiguous match in Harmony patch for ...` | Overloads share the name | Add argument types (4.3) |
| `Parameter "x" not found in method ...` | A patch parameter name matches no original parameter and no special name | Copy the exact parameter name from the decompile, or use `__0`/`[HarmonyArgument]` |
| `... has not "bool" or "void" return type: ...` | A prefix returns something else | Return `bool` or `void` |
| `... must be static` | Patch method is an instance method | Make it `static` |
| `Cannot assign method return type ...` | `__result` declared with the wrong type | Use the original's return type |
| `Method ... returned an unexpected result: null` | Your `TargetMethod` returned null (an `AccessTools` lookup missed; `AccessTools` itself only writes its reason to `harmony.log.txt`) | Same as the first row; enable `HARMONY_DEBUG` (8.4) to see the lookup's message |

Since `PatchAll` stops at the first failure (1.4), fix the first error and
reload; later errors only show up then.

### 8.3 Applied but silent

The patch applied and never runs. Causes, in the order to check them:

1. **The path you exercised doesn't go through it.** Re-do step 3 of
   section 5 for the path you tested.
2. **An override doesn't call `base`**, or you patched the base while the
   subtype overrides it (4.9).
3. **The guard returns early.** Log the guard's inputs once
   (`NetworkServer.active`, the mode check).
4. **It runs on the other machine.** A client-side method on the host's
   patch sees only the host's own UI.
5. **Inlining.** Harmony's documentation warns that the JIT can inline very
   small methods into their callers; call sites that were inlined never
   enter the patched method. Suspect this for one-line getters, and prefer
   a bigger caller.

### 8.4 Inspect patch state at runtime

Harmony can tell you what is patched and by whom. A debug console command
(`08-config-ui-and-commands.md`) that dumps it:

```csharp
foreach (MethodBase original in harmony.GetPatchedMethods())
{
    Patches info = Harmony.GetPatchInfo(original);
    Debug.Log($"[MyMod] {original.FullDescription()} prefixes={info.Prefixes.Count} " +
              $"postfixes={info.Postfixes.Count} finalizers={info.Finalizers.Count} " +
              $"owners={string.Join(",", info.Owners)}");
}
```

`GetPatchedMethods()` lists only your instance's targets;
`Harmony.GetAllPatchedMethods()` lists every mod's. More than one owner
that starts with your mod id means an old instance was never unpatched
(1.5). Another mod's id on a method you patch means you share it, and the
order of prefixes matters (`[HarmonyPriority]`, `[HarmonyBefore]`,
`[HarmonyAfter]` control it).

For the generated IL, set the environment variable `HARMONY_DEBUG=1` before
starting the game (or put `[HarmonyDebug]` on one patch class). Harmony then
writes every replacement method and every patching exception to
`harmony.log.txt` on the desktop, or to the path in `HARMONY_LOG_FILE`.

### 8.5 Prove it fires

A patch is done when the log shows it doing its job in play. Last Stand's
handlers log the decision, not the call: `blocked room modifier ...`,
`curse: <type> was Travel x3; now ends after N kill(s).`, `hatred: shard
opened at wave N; ...`. One line per decision, with the prefix, makes a
`grep` of `Player.log` the test. `09-debugging-and-testing.md` builds on
that.

---

## 9. Where else this applies

- **Any "after X happened" hook**: find the routine every variant of X ends
  in, as `DoPostUseRoutines` is for shrine uses. Candidates in `Dew.Core`
  follow the same shape: a public, non-virtual method that writes the
  synced state and fires an RPC.
- **Any number the game computes and then delivers in several ways** (gold,
  experience, dream dust): patch the computation, not the delivery, as
  with `GetKillGoldAmount`.
- **Any value set in a `beforePrepare` callback**: hook after the callback
  and before the value is consumed, as with `SetRandomRarity` and
  `CurseStatusEffect.OnCreate`.
- **Any server-side reaction to player input** (chat, shrine choices,
  travel): the `UserCode_Cmd...` body or a method it calls synchronously,
  never the `Cmd...` stub.
- **Any game routine that breaks in your context**: skip only the broken
  step under a scope, as `SelectMiniBoss` is skipped.
- **Any decision predicate** (`ShouldVoteOnTravel`, `CanInteract`-style
  methods): a prefix with `__result` changes a decision without touching
  saved settings.
- **Any "skip the subclass, keep the base" need**: prefix plus reverse
  patch, as with boss deaths and the Aura of Pain's `OnCreate`.
