# 04. Host-only networking: Mirror, and what reaches vanilla clients

A Shape of Dreams mod that only the host installs runs on one machine. Every
other player runs the unmodified game. This chapter teaches how to tell, for
any change you want to make, whether those players will see it, and how to
write state on the host so they do.

It covers:

1. Mirror (the networking library) as it looks in the decompiled game.
2. The game's own networking layers: managers, actors, `DewPlayer`.
3. The host-only model: what travels to clients and what never does.
4. **The method**: trace a client's pixel back to its data and classify each
   input as synced or local.
5. Ten worked examples from Last Stand, including the dead ends.
6. How to write synced state from the host, and what not to do.
7. Shutdown and late-joiner pitfalls.
8. A lookup table: kind of data, whether it reaches a vanilla client, and how
   to change it.

Chapter `02-reading-the-game.md` covers decompiling and searching.
Chapter `03-hooking-with-harmony.md` covers patches. This chapter assumes
you have a decompiled `Dew.Core/` and `Dew.UI/` (one `.cs` per type) and
can grep them.

---

## 1. The model in one picture

```
            HOST (your mod loaded)                    CLIENT (vanilla)
 ┌──────────────────────────────────────┐   ┌──────────────────────────────┐
 │ NetworkServer.active = true          │   │ NetworkServer.active = false │
 │ NetworkClient.active = true          │   │ NetworkClient.active = true  │
 │                                      │   │                              │
 │ server logic (your patches run here) │──▶│ spawn messages (by assetId)  │
 │ SyncVars / SyncList / SyncDictionary │──▶│ same fields, overwritten     │
 │ [ClientRpc] / [TargetRpc] calls      │──▶│ same method bodies, vanilla  │
 │                                      │◀──│ [Command] calls (input)      │
 │ host's own UI (also a client)        │   │ UI built from LOCAL data     │
 └──────────────────────────────────────┘   └──────────────────────────────┘
```

Three facts follow from the picture and drive the rest of the chapter:

- **The host is the server and also a client.** A patch on UI code changes
  the host's screen and nobody else's. A test that only uses the host's
  screen proves nothing about clients.
- **Clients run vanilla code on whatever data reaches them.** You can't
  change *how* a client computes something. You can only change *inputs*
  that are synced.
- **Only the channels the game already defines exist.** The client has no copy
  of your types. It can't receive a SyncVar, RPC or networked object the
  vanilla game doesn't already define (section 2.10).

---

## 2. Mirror, as it appears in the decompile

Shape of Dreams uses **Mirror**, an open-source Unity networking library
(`Mirror.dll` in the game's `Managed` folder). Mirror's **weaver** rewrites
the game's compiled IL after the C# compiler runs. ILSpy shows the woven
result, not the source the developers wrote. To read the game, learn to
recognize the woven shapes and map them back to the attributes they came
from.

### 2.1 Who am I: the activity flags

| Expression | True on | Use |
|---|---|---|
| `NetworkServer.active` | host only (the server is listening) | "may I write synced state?" |
| `NetworkClient.active` | host and every client | "is there a local game client?" |
| `NetworkServer.activeHost` | host only (`localConnection != null`) | host-mode-specific branches |
| `behaviour.isServer` / `isClient` | per spawned object, from its `NetworkIdentity` | inside a component's methods |
| `isLocalPlayer`, `isOwned` | the client that owns the object | rarely relevant in host-only mods |

From `Mirror.NetworkServer` (decompiled):

```csharp
public static bool active { get; internal set; }
public static bool activeHost => localConnection != null;
```

In a host-only mod, the general gate is `NetworkServer.active`. Last Stand
wraps it with its mode flag:

```csharp
public static bool IsHosting()
{
    return NetworkServer.active && IsSelected();
}
```

`IsSelected()` reads `GameSettingsManager.gameSettingsSaveKey`, which is a
SyncVar. If a friend who *also* has the mod installed joins, their copy reads
the same synced value and would think the mode is on. The
`NetworkServer.active` half stops their copy from acting. Last Stand uses the
same gate to hide its lobby button for non-hosts
(`existing.gameObject.SetActive(NetworkServer.active)`) and to refuse its
config console command on a client:

```csharp
if (NetworkClient.active && !NetworkServer.active)
{
    Log.Warn(LogText.ConfigHostOnly);
    return;
}
```

### 2.2 `NetworkIdentity` and spawning

A **networked object** is a GameObject with a `NetworkIdentity` component.
When the server spawns it (`NetworkServer.Spawn`), every client receives a
spawn message that carries:

- a `netId`, the object's id for the session;
- an `assetId`, which prefab to instantiate;
- the object's initial synced state.

The client then builds **its own** instance from **its own** copy of the
prefab. Shape of Dreams routes that through `SpawnManager` (a local manager):

```csharp
foreach (KeyValuePair<uint, string> item in DewResources.database.netObjectAssetIdToGuid)
{
    NetworkClient.RegisterSpawnHandler(item.Key, SpawnFromDewDatabaseHandler, CustomDespawnHandler);
}
...
gameObject = DewResources.GetNetworkedPrefab(msg.assetId);
if (gameObject == null)
{
    UnityEngine.Debug.LogError($"Failed to get networked prefab of assetId: {msg.assetId}");
```

Two consequences:

- **Only prefabs from the vanilla database can be spawned for clients.** An
  object your mod builds at runtime has no `assetId` a client knows.
- **Plain fields on the spawned object come from the client's prefab, not
  from the host's instance.** If the host changes
  `shrine.maxUseCountPerPlayer` before spawning, the client's copy still has
  the prefab's value. Section 6.1 shows the bug this causes.

### 2.3 `NetworkBehaviour` and `DewNetworkBehaviour`

A `NetworkBehaviour` is a Unity component that can hold synced state and
remote calls. Nearly everything networked in the game derives from
`DewNetworkBehaviour : NetworkBehaviour, ILogicUpdate`, which sets:

```csharp
protected virtual void Awake()
{
    syncInterval = 0.01f;
    syncMode = SyncMode.Observers;
}
```

So dirty state goes out at most every 10 ms, to every **observer** (every
client that has the object spawned).

### 2.4 `[SyncVar]` and its woven setter

A `[SyncVar]` field is copied from server to clients when it changes.
Source like:

```csharp
[SyncVar] public int maxTotalUseCount = -1;
```

decompiles (from `Dew.Core/Shrine.cs`) to the attribute, plus a **generated
property** named `Network` + field name:

```csharp
[SyncVar]
[SaveVar(SaveVarFlags.Default)]
public int maxTotalUseCount = -1;
...
public int NetworkmaxTotalUseCount
{
    get { return maxTotalUseCount; }
    [param: In]
    set { GeneratedSyncVarSetter(value, ref maxTotalUseCount, 8uL, null); }
}
```

The setter does the networking. From `Mirror.NetworkBehaviour`:

```csharp
public void GeneratedSyncVarSetter<T>(T value, ref T field, ulong dirtyBit, Action<T, T> OnChanged)
{
    if (!SyncVarEqual(value, ref field))
    {
        T arg = field;
        SetSyncVar(value, ref field, dirtyBit);
        if (OnChanged != null && NetworkServer.activeHost && !GetSyncVarHookGuard(dirtyBit))
        { ... OnChanged(arg, value); ... }
    }
}

protected void SetSyncVar<T>(T value, ref T fieldValue, ulong dirtyBit)
{
    SetSyncVarDirtyBit(dirtyBit);
    fieldValue = value;
}
```

What to take from it:

- **The dirty bit is what sends the value.** Assigning the field directly
  (`maxTotalUseCount = 5`) changes the host's copy and sets no bit. The value
  never leaves the host until something else marks that bit. Inside the
  game's own assemblies, the weaver rewrote every assignment into a call to
  the `Network…` setter. **Your mod is not woven** (the official template
  `.csproj` has no weaver step), so you must call the `Network…` property
  yourself.
- **The weaver always makes the property public**, even for a private field.
  `Shrine` has `[SyncVar] private double _lastUseTime` and a
  `public double Network_lastUseTime`.
- **Hooks.** `[SyncVar(hook = "OnIsLockedChanged")]` becomes a delegate field
  `_Mirror_SyncVarHookDelegate_…` assigned in the constructor. The hook runs
  on every client when a changed value arrives, and on the host when it
  writes through the setter (`NetworkServer.activeHost`). Writing
  `shrine.isLocked = false` on the host therefore plays the unlock effect
  (`OnIsLockedChanged` → `FxPlay(unlockEffect)`) on every screen.
- **Serialization.** Every woven class gets `SerializeSyncVars(writer,
  forceAll)` and `DeserializeSyncVars(reader, initialState)`. These list
  every SyncVar of that class in order, which makes them the quickest place
  to read the full set:

```csharp
public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
{
    base.SerializeSyncVars(writer, forceAll);
    if (forceAll)
    {
        writer.WriteInt(maxTotalUseCount);
        writer.WriteInt(totalUseCount);
        writer.WriteDouble(_lastUseTime);
        writer.WriteBool(isAvailable);
        writer.WriteBool(isLocked);
        return;
    }
    writer.WriteULong(base.syncVarDirtyBits);
    ...
```

**Auto-property SyncVars.** `[SyncVar] public bool isLocked { get; set; }`
puts the attribute on the compiler's backing field `<isLocked>k__BackingField`.
ILSpy escapes `<` and `>` as `_003C` and `_003E`, so the decompile shows:

```csharp
[CompilerGenerated]
[SyncVar(hook = "OnIsLockedChanged")]
private bool _003CisLocked_003Ek__BackingField;
...
public bool isLocked
{
    get { return _003CisLocked_003Ek__BackingField; }
    set { Network_003CisLocked_003Ek__BackingField = value; }
}
```

The real metadata names, confirmed with `strings Dew.Core.dll`, are
`set_Network<isLocked>k__BackingField` and
`_Mirror_SyncVarHookDelegate_<isLocked>k__BackingField`. You can't type
those names in C#. You don't need to: the auto-property's own setter was
woven to call the networked setter, so `shrine.isLocked = true`,
`quest.currentProgress = "…"` and `zones.currentHuntLevel = 2` all sync. If
the property's setter is private, reach the generated one by reflection:
`AccessTools.PropertySetter(typeof(T), "Network<name>k__BackingField")`.

**The `[NonSerialized]` trap.** `[NonSerialized]` is a Unity/.NET
serialization attribute (not saved in the prefab). It says nothing about
networking. `GameManager` has

```csharp
[NonSerialized]
[SyncVar]
public float goldCostMultiplier = 1f;
```

which **is** synced, while `Shrine_MawOfDoom` has

```csharp
[NonSerialized]
public Dictionary<string, float> priceMultipliersByPlayer = new Dictionary<string, float>();
```

which is not. Only `[SyncVar]` (or a sync collection, next section) decides.

### 2.5 `SyncList`, `SyncDictionary`

Collections sync through `SyncList<T>`, `SyncDictionary<K,V>` (and
`SyncSet<T>`). They are `readonly` fields registered in the constructor with
`InitSyncObject`. That call is how you spot them:

```csharp
[SaveVar(SaveVarFlags.Default)]
private readonly SyncDictionary<string, int> _useCountByPlayer = new SyncDictionary<string, int>();
...
protected Shrine()
{
    InitSyncObject(_useCountByPlayer);
    ...
}
```

`InitSyncObject` (Mirror) wires two callbacks into the collection:
`OnDirty` (mark the object dirty) and `IsWritable`:

```csharp
syncObject.IsWritable = delegate
{
    if (NetworkServer.active && NetworkClient.active)
    {
        if (syncDirection != 0) { return isOwned; }
        return true;
    }
    if (NetworkServer.active) { return syncDirection == SyncDirection.ServerToClient; }
    if (NetworkClient.active)
    {
        if (netId != 0) { ... return false; }
        return true;
    }
    throw new Exception("InitSyncObject: IsWritable: neither NetworkServer nor NetworkClient are active.");
};
```

Every mutation checks it. On a client the check fails with
`InvalidOperationException("Synclists can only be modified by the owner.")`
(`"SyncDictionaries can only be modified by the owner."` for dictionaries).
With no network at all it throws the `InitSyncObject` exception (section 8).

Two behaviors matter when you write:

- **`SyncDictionary` records every write**, even when the value doesn't
  change. The indexer always calls `AddOperation(OP_SET or OP_ADD)`. Compare
  before you write, or you send traffic for nothing.
- **`SyncList` records a set only if the element changed**:

```csharp
set
{
    if (!comparer.Equals(objects[i], value))
    {
        ...
        AddOperation(Operation.OP_SET, i, oldItem, value, checkAccess: true);
    }
}
```

  `ZoneManager.nodes` is a `SyncList<WorldNodeData>`. `WorldNodeData` is a
  struct with a `List<ModifierData> modifiers` and no `Equals` override, so
  equality compares the list *reference*. If you mutate the inner list in
  place and write the struct back, it compares equal and nothing is sent.
  The game itself copies the list first:

```csharp
WorldNodeData value = nodes[nodeIndex];
value.modifiers = new List<ModifierData>(value.modifiers);
int num = value.modifiers.FindIndex((ModifierData m) => m.id == modifierId);
if (num >= 0) { value.modifiers.RemoveAt(num); }
nodes[nodeIndex] = value;
```

### 2.6 `[Command]`: client to server

A **Command** is a method a client calls that runs on the server. The weaver
splits it into three parts. From `ChatManager`:

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
    // the original body: rate limit, sanitize, BroadcastMessage(...)
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

(The `//` line above is a summary, not decompiler output.) The static
constructor registers the handler:
`RemoteProcedureCalls.RegisterCommand(typeof(T), "<full name>", InvokeUserCode_…, requiresAuthority)`.

- `CmdX` is now only the **sender stub**. It runs on whoever calls it, which
  for a vanilla client is not your code.
- `UserCode_CmdX__ArgTypes` is the **server-side body**. It runs on the host
  for every client's call, including the host's own. **Patch this one** to
  see or change what clients ask for. (For example, `UserCode_CmdSendChatMessage__String__NetworkConnectionToClient`
  is where every client's chat line reaches the server.)
- `requiresAuthority = false` lets any client call it on any object, and the
  woven code passes `sender` in. Most game commands use this. A command
  with authority (`[Command]` alone, e.g. `EntityControl.CmdInteract`) can
  only be called on an object the client owns.

### 2.7 `[ClientRpc]` and `[TargetRpc]`: server to clients

An **RPC** (remote procedure call) here is a method the server calls that
runs on clients. It has the same three-part shape, with `SendRPCInternal`,
`InvokeUserCode_` checking `NetworkClient.active`, and
`RemoteProcedureCalls.RegisterRpc`:

```csharp
[ClientRpc]
public void BroadcastMessage(Message message)
{
    NetworkWriterPooled writer = NetworkWriterPool.Get();
    GeneratedNetworkCode._Write_ChatManager_002FMessage(writer, message);
    SendRPCInternal("System.Void ChatManager::BroadcastMessage(ChatManager/Message)", -216182835, writer, 0, includeOwner: true);
    NetworkWriterPool.Return(writer);
}
```

A `[TargetRpc]` takes a connection as its first parameter and goes to that
one client: `Shrine.TpcShowLocalUseEffect(NetworkConnection conn, Entity
entity)` → `SendTargetRPCInternal(conn, …)`. The game names these `Tpc…`
(`DewPlayer.TpcShowCenterMessage`, `Rift_RoomExit.TpcInteract`).

`SendRPCInternal` shows when an RPC silently goes nowhere:

```csharp
if (!NetworkServer.active)
{
    Debug.LogError("RPC Function " + functionFullName + " called on Client.", base.gameObject);
    return;
}
if (!isServer)
{
    Debug.LogWarning("ClientRpc " + functionFullName + " called on un-spawned object: " + base.name, base.gameObject);
    return;
}
...
if (netIdentity.observers == null || netIdentity.observers.Count == 0)
{
    return;
}
```

It reaches only **current observers**. Nobody stores it. Section 9 covers what
that means for players who join later.

Harmony note: a patch on `UserCode_RpcX` runs only on machines with your mod.
In a host-only mod that means the host's own client. It never changes what
vanilla clients do with the RPC.

### 2.8 `[Server]`: woven guard

`[Server]` on a method becomes an early return with a fixed warning. From
`Actor`:

```csharp
[Server]
internal void PrepareAndSpawn()
{
    if (!NetworkServer.active)
    {
        UnityEngine.Debug.LogWarning("[Server] function 'System.Void Actor::PrepareAndSpawn()' called when server was not active");
        return;
    }
    InvokeOnPrepareIfDidnt();
    NetworkServer.Spawn(base.gameObject, GetNetworkAuthorityConnection());
}
```

When you see that warning in `Player.log`, the code ran on a client or after
shutdown. That is a strong hint for where your guard is missing.

### 2.9 Recognition table

| You see in ILSpy | Source was | Meaning for you |
|---|---|---|
| `[SyncVar]` on a field + `public T NetworkX` with `GeneratedSyncVarSetter` | `[SyncVar] T X` | synced; write through `NetworkX` |
| `_003CX_003Ek__BackingField` with `[SyncVar]`, property `X` whose setter calls `Network_003CX…` | `[SyncVar] T X { get; set; }` | synced; write through `X` |
| `_Mirror_SyncVarHookDelegate_…` | `[SyncVar(hook = "...")]` | client-side reaction runs on change |
| `readonly SyncList<T>` / `SyncDictionary<K,V>` + `InitSyncObject(x)` in ctor | sync collection | synced; mutate on host only |
| `CmdX` calling `SendCommandInternal`, plus `UserCode_CmdX__…` | `[Command]` | patch `UserCode_` for server-side handling |
| `X` calling `SendRPCInternal` / `SendTargetRPCInternal`, plus `UserCode_X__…` | `[ClientRpc]` / `[TargetRpc]` | callable from the host; clients run vanilla body |
| `if (!NetworkServer.active) { LogWarning("[Server] function '…'") }` | `[Server]` | server-only API |
| `private void MirrorProcessed() { }` | weaver marker | the class was woven |
| `GeneratedNetworkCode._Write_…` / `_Read_…` | weaver serializers | the type can travel over the network |
| field with no `[SyncVar]` (with or without `[NonSerialized]`) | plain field | **host-only** if written at runtime |

### 2.10 Why a host-only mod can't add new channels

- Your assembly is not woven, so a `[SyncVar]` or `[ClientRpc]` you declare
  is just an attribute with no generated code behind it.
- Even if you wove it, vanilla clients don't have your type. They can't
  register its RPC hashes or deserialize its state, and they have no
  `assetId` to spawn it.

So the job is always to **find an existing channel** that carries what you
need.

---

## 3. The game's own layers

### 3.1 Managers: networked vs local, `.instance` vs `.softInstance`

The game has two manager base classes. Which one a manager derives from is
the first hint about whether its data is shared:

| Base | Derives from | Examples (decompiled `Dew.Core`) | Shared? |
|---|---|---|---|
| `NetworkedManagerBase<T>` | `DewNetworkBehaviour` | `ActorManager`, `ChatManager`, `GameManager`, `GameSettingsManager`, `LootManager`, `PickupManager`, `QuestManager`, `TimescaleManager`, `ZoneManager` (15 in total) | has SyncVars/RPCs |
| `ManagerBase<T>` | `LogicBehaviour : MonoBehaviour` | `MusicManager`, `CameraManager`, `ControlManager`, `EditSkillManager`, `MessageManager`, `SpawnManager`, `UIManager` (41 in total) | **local to each game** |

There are also singletons for single scene objects:
`SingletonDewNetworkBehaviour<T>` (networked, e.g. `Room`) and
`SingletonBehaviour<T>` (local UI, e.g. `UI_InGame_RoomModDisplay`).

All four expose `instance` and `softInstance`. `NetworkedManagerBase<T>`:

```csharp
public static T instance
{
    get
    {
        if (softInstance == null)
        {
            softInstance = Object.FindObjectOfType<T>();
        }
        if (softInstance == null)
        {
            softInstance = Object.FindObjectOfType<T>(includeInactive: true);
        }
        return softInstance;
    }
}

public static T softInstance { get; private set; }

protected override void Awake()
{
    softInstance = this as T;
    base.Awake();
}
```

When the manager exists, both return the cached reference. **When it doesn't**
(title screen, lobby, scene transitions, after game over), every `instance`
read runs `FindObjectOfType` **twice**. That is a full scan of the loaded
scenes. From per-frame code, that becomes two scene scans per frame per call
site, which shows up as host lag. Rule:

- Per-frame or per-tick code: `NetworkedManagerBase<GameManager>.softInstance`,
  then null-check and skip.
- One-shot code that needs the manager to exist (a console command, a
  Harmony postfix on a method of that manager): `instance` is fine.

The game itself calls `instance` everywhere, because its own code runs only
when the managers exist. Your tick runs before and after they do.

`ModBehaviour.CallOnNetworkedManager<T>(Action<T> onStart)` is an
alternative to polling. `NetworkedManagerBase<T>.OnStartClient` calls
`DewMod.NotifyManagerLifecycle`, which invokes your callback. It fires on
every machine with the mod, the host included (the host is a client), so
gate server work inside it with `NetworkServer.active`.

### 3.2 Actors: the game's networked objects

`Actor : DewNetworkBehaviour` is the base of almost everything in a run:
entities, monsters, heroes, shrines, status effects, room modifiers,
quests, ability instances. Creating one is server-only and replicates
automatically:

```csharp
public static T CreateActor<T>(T prefab, Vector3 position, Quaternion? rotation, Actor parent = null, Action<T> beforePrepare = null) where T : Actor
{
    return PrepareAndSpawnActor_Imp(InstantiateActor_Imp(prefab, position, rotation, parent), beforePrepare);
}
...
if (!NetworkServer.active)
{
    throw new Exception("You can only create actors on server");
}
```

`PrepareAndSpawnActor_Imp` runs `beforePrepare` first, then `OnPrepare`, then
`NetworkServer.Spawn`. Whatever SyncVars you set in `beforePrepare` are
part of the spawn message (initial state). Whatever plain fields you set
there stay on the host's instance only.

Destroying is server-only too. `Actor.Destroy()` on a client is a logged
no-op (`"Actor.Destroy() on client (no-op): …"`). On the host it destroys the
object for everyone, and Mirror unspawns it on every client. That is how the
game's Limbo mode removes the run goal (`quest_ShapeOfDreams.Destroy()` in
`GameMod_Limbo`). Last Stand removes it the same way.

Higher-level wrappers do the same thing:

- `QuestManager.StartQuest<T>(beforePrepare)` (`[Server]`) is
  `Dew.CreateActor(prefab, …)`.
- `ZoneManager.AddModifier<T>(node, beforePrepare)` writes the synced
  `nodes` list. On the current node with an active room, it also creates the
  modifier actor at once.

See `06-game-world-api.md` for the full actor lifecycle (`OnCreate`,
`OnStartServer`, `OnLateStartServer`…).

### 3.3 `DewPlayer`

One `DewPlayer` (a `DewNetworkBehaviour`) per connected player:

- `DewPlayer.gamePlayers`: static list of the run's players. `DewPlayer.local`
  is "me" on each machine. Code that reads `DewPlayer.local` is client-side
  presentation code. That is a key marker for the method in section 5.
- `guid`: a synced auto-property string. The game keys per-player synced
  data by it (`Shrine._useCountByPlayer[player.guid]`,
  `Shrine_Destiny.currentItems`).
- `gold`, `dreamDust`: changed through server methods (`EarnGold` is
  `[Server]`, `Spend(Cost)`), which sync.
- `enemies`, `neutrals`: `SyncList<DewPlayer>` (relations; the game's Dark
  Urge curse writes them).
- Targeted feedback: `TpcShowCenterMessage(CenterMessageType, string key)`,
  `TpcShowWorldPopMessage(...)`. The key is localized on the client, so it
  must be one the game has.
- Plain, host-only properties that only server logic reads, e.g.
  `monsterKillGoldMultiplier { get; set; } = 1f`, are safe to change on the
  host, because only the server reads them.

---

## 4. The host-only mod model

**What reaches a vanilla client:**

1. **Spawned networked objects** from the vanilla prefab database (actors,
   modifiers, quests, shrines, pickups, monsters, effects).
2. **Their SyncVars and sync collections**, written on the host through the
   woven setters.
3. **The game's existing RPCs**, called on the host with arguments the
   vanilla receiving code understands.
4. **Server outcomes** that are themselves synced state: damage and health,
   deaths, status effects, gold and dust, items, positions.

**What never reaches a vanilla client:**

- plain fields, including ones changed on a spawned instance (the client has
  its own prefab copy);
- `[NonSerialized]` fields without `[SyncVar]`;
- anything in a `ManagerBase<T>` (music, camera, edit-skill window, message
  boxes);
- UI text and layout (everything in `Dew.UI`), unless it is built from synced
  inputs;
- your patches on any method, since those run only where your DLL is
  loaded.

The rule from this list: **you can change what a client computes only by
changing a synced input it reads, at a time it reads it.** If no such input
exists, the change is impossible for vanilla clients, and you need a
different channel. Last Stand dropped its top-right wave label for this
reason (section 6.5).

---

## 5. THE METHOD: will a vanilla client see this change?

Run this before writing any code that should affect what other players see
or can do.

### 5.1 Procedure

1. **Name the client-side effect exactly.** It's not "the price changes". It's
   "the gold number in the Ascension tooltip on a client", and "whether the
   client's Ascend button sends the command".
2. **Find the code that produces it on the client.**
   - UI text: grep `Dew.UI/` for the widget or a distinctive localization
     key, then find where `.text =` is assigned.
   - Behavior: find the client branch. Markers are `DewPlayer.local`,
     `ManagerBase<…>.instance`, `ClientEvent_…` handlers, `UserCode_Rpc…` /
     `UserCode_Tpc…` bodies, `isClient`, and `Update`/`LateUpdate` in UI
     classes.
3. **List every input of that code** and classify each (table 5.2). Follow
   calls down until each input is a field, a property backed by a field, or
   a constant.
4. **Find when the client recomputes.** Possible triggers: every frame (UI
   `Update`), a SyncVar hook, a `ClientEvent_…` fired by an RPC, a
   one-time `Start`/`Awake`/`OnCreate`, or the window opening. A synced
   input that changes *between* recomputes doesn't show until the next
   one.
5. **Find the server's twin.** Many things are computed on both sides: the
   client to display and pre-check, the server to decide. List the server's
   inputs too. Change only one side and the client shows one thing while the
   server does another.
6. **Decide.**
   - Every input you need to change is synced and the client recomputes
     often enough: write it on the host (section 7).
   - The client's computation can't be reached, but the client's *decision*
     doesn't matter (only the server acts): change the server side only.
   - Neither: find another channel (quest tracker, chat, a different
     modifier), or drop the feature.

### 5.2 Classifying an input

| Input looks like | Synced? | How to check in the decompile |
|---|---|---|
| field with `[SyncVar]` | yes | `grep -n "Network<field>" T.cs` finds the generated property |
| auto-property with `[SyncVar]` on `_003C…_003Ek__BackingField` | yes | setter calls `Network_003C…` |
| `readonly SyncList/SyncDictionary/SyncSet` | yes | `grep -n "InitSyncObject" T.cs` |
| argument of an RPC | yes, once, to current observers | method has `SendRPCInternal` |
| plain public field set in the prefab (Inspector) | no: client uses its prefab copy | not in `SerializeSyncVars` |
| `[NonSerialized]` field without `[SyncVar]` | no | same |
| `ges.*` curves (game economy settings), `DewResources` data | same on both sides, not changeable at runtime for clients | static asset data; see `05-assets-and-data.md` |
| field of a `ManagerBase<T>` or a `MonoBehaviour` UI class | no | base class |
| derived from `DewPlayer.local`, `Time.time`, local `Random` | local | read the call |

Useful one-liners in the decompiled folder:

```sh
# every SyncVar and sync collection of a class, in wire order
grep -n "SyncVar\]\|InitSyncObject\|readonly Sync" Dew.Core/Shrine.cs

# is this field synced? (generated property exists)
grep -n "public .* Network_\?maxUseCountPerPlayer\b" Dew.Core/Shrine.cs   # nothing: not synced

# where does the client compute this? (local-player reads)
grep -n "DewPlayer.local" types/Shrine_Ascension.cs

# who renders it?
grep -rn "currentProgress" Dew.UI/
```

### 5.3 Inheritance

SyncVars are per class. `SerializeSyncVars` calls `base.SerializeSyncVars`
first, and the dirty bits continue from the base class. `Shrine` starts at
`8uL`, so `Actor` and its parents use the lower bits. To list everything
synced on a `Shrine_Ascension`, read the `SerializeSyncVars` of each class
up the chain: `Shrine_Ascension` → `EditSkillShrine` → `Shrine` → … →
`Actor`.

---

## 6. Worked examples

Each example states the client-side effect, runs the trace, and gives the
verdict and the fix Last Stand shipped.

### 6.1 Shrine limits that aren't synced: reset the synced count instead

**Goal.** A `Shrine_UpgradeWell` (Dream Well) usable without limit by
everyone, a vanilla client included.

**Obvious attempt.** Set `maxUseCountPerPlayer = -1` and `useCooldown = 0`
on the spawned shrine. The host's well works without limit. A client gets
locked out after its first use.

**Trace.** The client's interaction prompt asks `CanInteract` locally:
`ControlManager` (a local `ManagerBase`) filters focus targets with
`orAdd.CanInteract(controllingEntity)`. `Shrine.CanInteract`:

```csharp
public virtual bool CanInteract(Entity entity)
{
    if (isAvailable && Time.time - base.creationTime > interactableDelay && !isOnCooldown)
    {
        if (maxUseCountPerPlayer >= 0)
        {
            return GetUseCountByPlayer(entity.owner) < maxUseCountPerPlayer;
        }
        return true;
    }
    return false;
}

public bool isOnCooldown => NetworkTime.time - _lastUseTime < (double)useCooldown && useCooldown > 0.01f;   // simplified
```

Inputs:

| Input | Kind |
|---|---|
| `isAvailable` | SyncVar (auto-property) |
| `interactableDelay` | plain, prefab |
| `useCooldown` | **plain, prefab** (initializer `1f`) |
| `_lastUseTime` | SyncVar (`Network_lastUseTime`) |
| `maxUseCountPerPlayer` | **plain, prefab** |
| `_useCountByPlayer` | SyncDictionary |

The client keeps the prefab's `maxUseCountPerPlayer` and `useCooldown`, and
compares them with the synced count and last-use time. After one use the
synced count is 1, so the client's check fails.

**Server twin.** `Shrine.OnInteract` (server) checks the host's
`maxUseCountPerPlayer` and `totalUseCount < maxTotalUseCount`, then
`DoPostUseRoutines` increments the count, writes `Network_lastUseTime`
and recomputes `isAvailable`.

**Fix.** Keep the client's inputs at values that pass its check. After every
successful use (a Harmony postfix on `Shrine.DoPostUseRoutines`, which every
use ends in), the host undoes the synced state that use wrote:

```csharp
shrine.ResetUseCounts();
shrine.Network_lastUseTime = double.NegativeInfinity;
shrine.MakeAvailable();
```

`ResetUseCounts` clears `_useCountByPlayer` and the synced `totalUseCount`.
`MakeAvailable` (`[Server]`) sets the synced `isAvailable`. The host's own
plain-field limits are still set to unlimited, because the server's check in
`OnInteract` reads them, and `NetworkmaxTotalUseCount = -1` because that
one is synced.

**General rule.** If a client gate compares an unsynced limit with a synced
counter, keep the counter low instead of raising the limit.

### 6.2 A private `SyncDictionary` written by reflection: pinning the Maw of Doom

**Goal.** `Shrine_MawOfDoom` at a fixed price and risk on every use, for
host and clients alike.

**Trace.** Price and risk on the client:

```csharp
float adjusted = NetworkedManagerBase<GameManager>.instance.GetAdjustedGoldAmount_Cost_Service(goldCostByUseCount.Evaluate(GetUseCountByPlayer(player)));
float valueOrDefault = priceMultipliersByPlayer.GetValueOrDefault(player.guid, 1f);
...
return destroyChanceByUseCount.Evaluate(GetUseCountByPlayer(player));
```

`goldCostByUseCount` and `destroyChanceByUseCount` are prefab `Formula`s
(not overridable; see `05-assets-and-data.md`). `priceMultipliersByPlayer`
is `[NonSerialized]` with no SyncVar, so it is host-only: changing it
would charge one price and show another. The use count `x` is the only
synced input, and it drives both price and risk.

**Fix.** Write every player's count to a fixed value. The field is private,
so get the collection by reflection once and mutate it on the host:

```csharp
private static readonly FieldInfo UseCounts = AccessTools.Field(typeof(Shrine), GameNames.ShrineUseCounts); // "_useCountByPlayer"

foreach (DewPlayer player in DewPlayer.gamePlayers)
{
    if (player != null && player.guid != null && shrine.GetUseCountByPlayer(player) != uses)
    {
        counts[player.guid] = uses;
    }
}
```

- The field is `readonly`. You read the reference and call its indexer. You
  never replace the object, which would disconnect it from `InitSyncObject`.
- The write goes through Mirror's `AddOperation`, so clients see the new count
  within one sync interval. Their tooltips and checks pick it up because
  they read it every time.
- The `!= uses` check is there because `SyncDictionary` sends every write
  (section 2.5).
- When it's done: after each use (the same `DoPostUseRoutines` postfix), on
  arrival, when a wave is cleared, and once a second, which covers a player
  who joins mid-run with no entry yet (section 9).

### 6.3 A price computed on both sides: Shrine of Ascension vs Altar of Cleansing

**Goal.** Raise gold shrine prices as waves go on, with the client's tooltip
showing exactly what the host charges.

**Trace, Ascension** (`Shrine_Ascension`, decompiled from `Dew.Contents`):

```csharp
private Cost GetCost(Rarity rarity, DewPlayer player)
{
    float num = baseGoldCostByStartRarity.Get(rarity);
    num *= 1f + addedCostMultiplierPerUse * (float)GetUseCountByPlayer(player);
    return Cost.Gold(Mathf.RoundToInt(NetworkedManagerBase<GameManager>.instance.GetAdjustedGoldAmount_Cost_Service(num)));
}
```

`GetCost` → `GetTargetInfo` runs in three places:

1. **Client display.** `EditSkillManager` (local) builds the tooltip and
   greys out unaffordable items with
   `editSkillShrine.GetTargetInfo(DewPlayer.local, …).cost.CanAfford(DewPlayer.local)`.
2. **Client pre-check.** Before sending, the client refuses on its own:

   ```csharp
   AffordType affordType = targetInfo.cost.CanAfford(DewPlayer.local);
   if (affordType != 0) { InGameUIManager.instance.ShowCenterMessage(...); return; }
   editSkillShrine.CmdActivateEditSkillAction(skillType, null);
   ```

3. **Server charge.** `UserCode_CmdActivateEditSkillAction__…` recomputes
   `GetTargetInfo` on the host, checks `CanAfford` again, then
   `player.Spend(targetInfo.cost.Value)`.

All three must agree. Inputs, going down into `GameManager`:

```csharp
public float GetAdjustedGoldAmount_Cost_Service(float baseAmount)
{
    float num = baseAmount;
    num *= ges.generalServiceGoldPriceMultiplier;
    num *= ges.generalServiceGoldPriceMultiplierByZoneIndex.Evaluate(NetworkedManagerBase<ZoneManager>.instance.currentZoneIndex);
    return GetAdjustedGoldAmount_Cost(num);
}

public float GetAdjustedGoldAmount_Cost(float amount)
{
    amount *= goldCostMultiplier;
    return GetAdjustedGoldAmount(amount);
}
```

| Input | Kind |
|---|---|
| `baseGoldCostByStartRarity`, `addedCostMultiplierPerUse` | prefab |
| use count | SyncDictionary |
| `ges.*` curves | static asset data |
| `currentZoneIndex` | SyncVar (auto-property) |
| `goldCostMultiplier` | **SyncVar** (`[NonSerialized][SyncVar]`) |

**Verdict.** `goldCostMultiplier` is synced and read on every recompute.
Write it with the generated setter and all three places agree:

```csharp
game.NetworkgoldCostMultiplier = multiplier;
```

The game's own `GameMod_Limbo` does the same thing
(`gameManager.NetworkgoldCostMultiplier = gameManager.goldCostMultiplier * (1f + d3GoldPriceAmp)`),
which confirms this is a supported knob. The same multiplier feeds every
`GetAdjustedGoldAmount_Cost*` caller (Maw of Doom, buy prices, Luck
prays), so changing it changes all of them. Put it back to the vanilla value
on session end and on unload (section 8).

**Trace, Altar of Cleansing:**

```csharp
result.cost = Cost.Gold(NetworkedManagerBase<GameManager>.instance.GetCleanseGoldCost(target));
...
public int GetCleanseGoldCost(SkillTrigger skill)
{
    return Mathf.RoundToInt(GetAdjustedGoldAmount(ges.skillCleanseCostByLevel.Evaluate(skill.level)));
}
```

`GetAdjustedGoldAmount` multiplies only by the zone curve. There is no
synced input to change, so the Altar's price can't be raised in a way
clients see. Last Stand leaves it vanilla.

**What a mismatch looks like:**

- Host price higher than shown: the client sends the command, and the server
  answers with `TpcShowCenterMessage(Error, "InGame_Message_CannotAfford…")`
  or charges more than the tooltip said.
- Host price lower than shown: a client whose gold falls between the two
  prices is blocked by its own pre-check, and the command is never sent.

### 6.4 The quest tracker as a HUD text channel

**Goal.** Show the wave number to every player.

**Trace.** `UI_InGame_QuestDisplay_Item` (Dew.UI):

```csharp
titleText.text = quest.questTitleRaw;
descText.text = quest.questShortDescriptionRaw;
...
progressKillsObject.SetActive(quest.progressType == QuestProgressType.Kills);
...
progressText.text = quest.currentProgress;
```

`DewQuest`:

```csharp
[SyncVar(hook = "OnProgressTypeChanged")]
private QuestProgressType _003CprogressType_003Ek__BackingField;
[SyncVar(hook = "OnProgressChanged")]
private string _003CcurrentProgress_003Ek__BackingField;
private string _questTitleRaw;              // plain
private string _questShortDescriptionRaw;   // plain
```

Title and description are filled locally in `OnCreate` from
`GetType().Name + "_Title"` / `"_Description"`. The only way to change them
from the server is two existing `[ClientRpc]`s that take a localization
**key**:

```csharp
protected void UserCode_SetLocalizedTitle__String(string key)
{
    if (string.IsNullOrEmpty(key)) { key = GetType().Name + "_Title"; }
    questTitleRaw = DewLocalization.GetUIValue(key);
}
```

and `GetUIValue` falls back visibly for unknown keys:

```csharp
if (data.ui.TryGetValue(key, out var value)) { return value; }
return "ui.!" + key;
```

**Verdict.**

- `currentProgress` is a free-text synced string, shown as-is. It carries
  `Wave 12: 34 monsters left`.
- `progressType` is synced and picks the icon (`Kills`).
- The title and description can only be lines the game already has. Last
  Stand sends `InGame_ThreatLevelDisplay_ThreatLevelRising` and
  `RoomMod_HarderFightBetterReward_Name` ("Threat Level Rising" /
  "Powerful Monsters"). Finding usable keys is covered in
  `05-assets-and-data.md`.

**Fix (abridged from `WaveTracker`):**

```csharp
_quest = quests.StartQuest<Quest_Limbo>(quest =>
{
    quest.progressType = QuestProgressType.Kills;
    quest.currentProgress = progress;
});
...
if (progress != _shown)
{
    _quest.currentProgress = progress;
    _shown = progress;
}
```

The SyncVars are set in `beforePrepare`, so they arrive with the spawn.
`currentProgress` is written only when the text changes, at most once a
second, because each change replays the tracker's update flash on every
client. `Quest_Limbo` is used because it is an empty quest class with no
logic of its own. The title RPCs are re-sent for late joiners (section 9).

### 6.5 The top-right location label: a dead end

**Goal.** Show the wave in the top-right `1. Forest of …` label.

**Trace.** `UI_InGame_Location`:

```csharp
private void Start()
{
    _text = GetComponent<TextMeshProUGUI>();
    NetworkedManagerBase<ZoneManager>.instance.ClientEvent_OnRoomLoaded += new Action<EventInfoLoadRoom>(OnRoomLoaded);
    GameManager.CallOnReady(delegate { OnRoomLoaded(default(EventInfoLoadRoom)); });
}

private void OnRoomLoaded(EventInfoLoadRoom _)
{
    ...
    _text.text = string.Format("{0}. {1}", NetworkedManagerBase<ZoneManager>.instance.currentZoneIndex + 1,
        DewLocalization.GetUIValue(NetworkedManagerBase<ZoneManager>.instance.currentZone.name + "_Name"));
}
```

- Inputs: `currentZoneIndex` (synced) and `currentZone.name` (synced zone,
  local localization lookup). The second part can only be a real zone name.
- Recompute trigger: `ClientEvent_OnRoomLoaded`, fired by the
  `ZoneManager.InvokeOnRoomLoaded` `[ClientRpc]`, plus once at start. The
  arena loads one room, so this happens once per run.

**Dead ends, and why:**

| Attempt | Why it fails |
|---|---|
| Harmony patch on `OnRoomLoaded` / set `_text.text` | runs only on the host; clients keep the stock text, and players see different labels |
| Write `currentZoneIndex` to the wave number | about 80 lines in `Dew.Core`, `Dew.UI` and the decompiled Contents types read it: prices, curves, rewards. It would break the economy. |
| Call `InvokeOnRoomLoaded` again by reflection to force a redraw | 28 `ClientEvent_OnRoomLoaded +=` subscribers in the same code react to it as if a room loaded |

**Verdict.** No synced input carries free text into this label. Leave it
vanilla (so every player sees the same thing) and use the quest tracker
(6.4). An early Last Stand build overwrote the label on the host only. That
was removed because it only made the host's screen differ from everyone
else's.

### 6.6 The modifier panel: replace what you can't hide

**Problem.** After a hunter zone or environment modifier ended, vanilla
clients kept showing "Occupied by Hunters" in the panel under the quest
tracker.

**Trace.** `UI_InGame_RoomModDisplay` (a local `SingletonBehaviour`):

```csharp
base.gameObject.SetActive(value: false);
NetworkedManagerBase<ZoneManager>.instance.ClientEvent_OnRoomLoadStarted += ...ClientEventOnRoomLoadStarted;   // hides
NetworkedManagerBase<ActorManager>.instance.ClientEvent_OnActorAdd += (Action<Actor>)delegate(Actor a)
{
    if (a is RoomModifierBase { isMain: not false } roomModifierBase
        && (!roomModifierBase.hiddenOnVisitedNode || !SingletonDewNetworkBehaviour<Room>.instance.isRevisit))
    {
        ...
        ShowModifier(roomModifierBase, !flag);
    }
};
```

- Show trigger: a *main* modifier actor is added (`ClientEvent_OnActorAdd`,
  driven by the actor's spawn).
- Hide trigger: only a room load starting. Removing the modifier doesn't
  hide it.
- The host can't hide a client's panel (no synced input, no RPC).

**Verdict and fix.** A new main modifier *replaces* the panel's content, and
spawning an actor reaches clients. So whenever none of Last Stand's own
main modifiers is active, the host keeps a harmless main modifier on the
arena node: `RoomMod_FragmentOfRadiance_MysteriousPlace` ("Mysterious
Space"), added with `ZoneManager.AddModifier` and removed again when a real
one starts (the game allows one main modifier per node). Choosing it is the
same method applied to data: it had to be `isMain`, not
`hiddenOnVisitedNode` (the arena counts as revisited), with its own text,
and with no gameplay. Its one side effect, a sidetrack rift entry in
`Room.rifts.openedSidetrackRifts`, is server-side and is cleared by the host
every tick. Details are in Last Stand's `docs/hud/modifier-panel.md`.

### 6.7 Music: local by construction

**Goal.** Random boss music in the arena.

**Trace.** `MusicManager : ManagerBase<MusicManager>` is local. The room
picks its track on each machine:

```csharp
public override void OnLateStart()
{
    base.OnLateStart();
    if (music == null && NetworkedManagerBase<ZoneManager>.instance.currentNode.type != WorldNodeType.ExitBoss && ...)
    {
        music = NetworkedManagerBase<ZoneManager>.instance.currentZone.defaultMusicRef.asset;
    }
    ...
    ManagerBase<MusicManager>.instance.Play(music);
```

Boss tracks are played by scene `FxChangeMusic` components
(`ManagerBase<MusicManager>.instance.Play(music, !disableLoop)`), which are
local effects inside the scene. No SyncVar or RPC names a track, and a
networked effect can only play components that already exist under a
networked object.

**Verdict.** Only the host would hear it. The feature was removed.

### 6.8 Chat: `MessageType.Raw` renders as-is

**Goal.** Announce waves to everyone in custom words.

**Trace.** `ChatManager.BroadcastMessage(Message)` is a public `[ClientRpc]`.
The client renders by type (`UI_Common_ChatBox_Item.Setup`):

```csharp
case ChatManager.MessageType.Raw:
    this.text.text = DewAdvText.ConvertAdvToText(msg.content);
    break;
...
case ChatManager.MessageType.Notice:
{
    string text4 = DewLocalization.GetUIValue(msg.content);
    ...
    this.text.text = DewAdvText.ConvertAdvToText("<color=#94aab5><i>" + text4 + "</i></color>");
```

`Notice` and `WorldEvent` treat `content` as a localization key, so custom
text would show `ui.!…`. `Raw` passes the text through, and TextMeshPro
rich text such as `<color=#5ad45a>` works.

**Fix:**

```csharp
ChatManager chat = NetworkedManagerBase<ChatManager>.softInstance;
if (chat == null) { return; }
chat.BroadcastMessage(new ChatManager.Message
{
    type = ChatManager.MessageType.Raw,
    content = content,
});
```

For one player only, `ChatManager.SendChatMessage(NetworkConnection target,
Message)` is the matching `[TargetRpc]`. Like every RPC, a chat line is not
replayed to players who join later.

### 6.9 The camp rift: a SyncVar steers client UI

**Goal.** The camp exit goes straight to the arena node: one confirmation, no
world map, no vote.

**Trace.** The server answers an interaction with a TargetRpc, and the client
decides what to show from a SyncVar:

```csharp
[SyncVar]
public int nextNodeIndex = -1;

protected override bool OnInteractRift(Hero hero)
{
    TpcInteract(hero.owner);
    return true;
}

protected void UserCode_TpcInteract__NetworkConnectionToClient(NetworkConnectionToClient target)
{
    ...
    if (nextNodeIndex >= 0)
    {
        ManagerBase<MessageManager>.instance.ShowMessage(new DewMessageSettings
        {
            rawContent = DewLocalization.GetUIValue("InGame_Message_RiftMoveToNextLocation"),
            ...
            onClose = delegate(DewMessageSettings.ButtonType b)
            {
                if (b == DewMessageSettings.ButtonType.Yes)
                {
                    NetworkedManagerBase<ZoneManager>.instance.TravelWithValidationAndConfirmation(delegate
                    {
                        NetworkedManagerBase<ZoneManager>.instance.CmdTravelToNode(nextNodeIndex);
                    });
                }
            }
        });
        return;
    }
    // otherwise: open the world map
```

**Fix.** `rift.NetworknextNodeIndex = target;` on the host. Every client's
vanilla `TpcInteract` body now takes the confirm-and-travel branch.

**The vote, a function called on both sides.** `ZoneManager.ShouldVoteOnTravel()`
is called in two places:

- client-side, in `TravelWithValidationAndConfirmation`, only to decide
  whether to show a "can't travel" message;
- server-side, in `UserCode_CmdTravelToNode__Int32__NetworkConnectionToClient`,
  to choose between `StartVoteNextNode` and `TravelToNode`.

A host-only prefix that returns `false` controls the server branch, which is
the one that decides. The client's call keeps its vanilla result, which only
affects a message. Whenever a method runs on both sides, find which caller
acts.

### 6.10 When a host-only write is enough

The method also tells you when you *don't* need sync: if every reader is
server code, a plain field write on the host is complete.

- `Room.didClearRoom` is `{ get; private set; }`, not synced. Last Stand
  flips it through `AccessTools.PropertySetter(typeof(Room),
  nameof(Room.didClearRoom))` so that environment hazards (server-side
  routines that stop on a cleared room) keep firing. Clients see the
  hazards because those are spawned actors.
- `RoomMod_Hunted.endWhenRiftOpen` is a plain field, read only by the
  artillery coroutines started in `OnStartServer`. Clearing it in
  `beforePrepare` is enough.
- Plain `Shrine` limits (6.1) still need to be set on the host, because the
  server's `OnInteract` checks them. They just aren't sufficient for
  clients.

---

## 7. Writing synced state from the host

### 7.1 SyncVar fields: use the generated `Network…` property

| Last Stand write | Field |
|---|---|
| `game.NetworkgoldCostMultiplier = multiplier` | `GameManager.goldCostMultiplier` |
| `rift.NetworknextNodeIndex = target` | `Rift_RoomExit.nextNodeIndex` |
| `shrine.NetworkmaxTotalUseCount = -1` | `Shrine.maxTotalUseCount` |
| `shrine.Network_lastUseTime = double.NegativeInfinity` | private `Shrine._lastUseTime` |
| `shrine.Networkrarity = …` (in `beforePrepare`) | `Shrine_Chaos.rarity` |
| `spawned.NetworkskipBossSoulFlow = true` | boss flag |

Never assign the raw field of a SyncVar. It compiles, the host shows the
new value, and no client ever receives it. That combination is the hardest
networking bug to notice when you test alone.

### 7.2 Auto-property SyncVars: use the property

`quest.currentProgress`, `quest.progressType`, `shrine.isLocked`,
`zones.currentHuntLevel`. Each woven setter forwards to the networked one.
For a private setter, use
`AccessTools.PropertySetter(type, "Network<name>k__BackingField")`.

### 7.3 Private sync collections: reflect the field, mutate the object

```csharp
FieldInfo f = AccessTools.Field(typeof(Shrine), "_useCountByPlayer");
if (f?.GetValue(shrine) is SyncDictionary<string, int> counts) { counts[guid] = n; }
```

- Cache the `FieldInfo`. Keep the name in one place, and null-check it so a
  game update that renames the field degrades instead of throwing.
- Prefer a public method when one exists (`Shrine.ResetUseCounts()`).
- For a `SyncList` of structs: copy the element, copy any inner list you
  change, and assign it back (section 2.5). Last Stand hides world-map nodes
  this way:

```csharp
WorldNodeData node = zones.nodes[i];
node.position = new Vector2(HiddenOffset - node.position.x, node.position.y);
zones.nodes[i] = node;
```

### 7.4 Before spawn vs after spawn

| When | SyncVars you set | Plain fields you set |
|---|---|---|
| in `beforePrepare` of `Dew.CreateActor` / `StartQuest` / `AddModifier` | sent in the spawn message; client hooks see them as initial state | host instance only |
| after spawn | sent as a delta in the next sync | host instance only |

Some game logic reads values in `OnCreate`/`OnStartServer`. Those must be
set in `beforePrepare`. For example, `Shrine_Chaos` builds its rewards from
`rarity` in `OnLateStartServer`. The game rolls `rarity` in
`SetRandomRarity`, called from `beforePrepare`, so Last Stand's reroll is a
Harmony postfix on `Shrine_Chaos.SetRandomRarity` and lands before the
rewards are built.

### 7.5 Create actors, they replicate

To show something new to clients, spawn a vanilla actor that already
renders it: a modifier (environment look, panel), a quest (tracker line), a
shrine, a status effect (icon on a hero), a monster. Pick the actor with the
method: does the client's copy, built from its prefab plus the synced state
you set, look and behave the way you need?

### 7.6 Call existing RPCs

Public RPCs are ordinary method calls on the host: `quest.SetLocalizedTitle(key)`,
`chat.BroadcastMessage(msg)`, `player.TpcShowCenterMessage(type, key)`. Check
three things first:

1. What the client body does with the arguments (localization key or raw
   text?).
2. What else listens on the client. RPCs that fire a broad `ClientEvent_…`
   (like `InvokeOnRoomLoaded`) have side effects.
3. That the object is spawned and the server active. Otherwise the call
   logs `"RPC Function … called on Client."` or `"… called on un-spawned
   object"` and does nothing.

### 7.7 What not to do

- **Don't hand-build Mirror messages** (an `RpcMessage` with a copied
  function hash, a fake `SpawnMessage`). Hashes and payload layouts are
  weaver output that changes with game builds, and a malformed message can
  disconnect clients. Last Stand's rule: only the game's own channels.
- **Don't use JSON overrides for runtime state.** They change static prefab
  values, sync to clients when they join (`DewPlayer.TpcSendOverrides`), set
  `isAlteringGameplay`, and apply to every run while the mod is loaded,
  not only your mode. They suit fixed tuning only, and only fields of
  allowed types (`05-assets-and-data.md`).
- **Don't bypass the cross-play block.** `DewMod` sets
  `DewSave.platformSettings.gameplay.enableCrossPlay = false` when any mod
  is active, and `LobbyServiceEOS.IsCrossPlayEligible` requires
  `!DewMod.isAnyModActive`. A modded lobby is Steam-only by design. Leave it
  that way, and set `isAlteringGameplay = true` as the official rules
  require (`01-project-setup.md`).
- **Don't write synced state from a client.** Sync collections throw
  (`"Synclists can only be modified by the owner."`). SyncVar setters
  silently change only that client's copy (`SetSyncVar` just sets a dirty
  bit nobody sends), and its screen goes out of sync with the server.

---

## 8. Shutdown pitfalls

On game over, a restart or leaving the lobby, Mirror shuts the transport
down. Then **both** `NetworkServer.active` and `NetworkClient.active` are
false, and your session cleanup may still run (from `OnDestroy`, a
scene-change event, or one last tick). Each kind of write fails in its own
way:

| Write | Behavior after shutdown |
|---|---|
| `SyncList`/`SyncDictionary` mutation (incl. `ZoneManager.RemoveModifier`, which writes `nodes`) | **throws** `Exception: InitSyncObject: IsWritable: neither NetworkServer nor NetworkClient are active.` |
| `Dew.CreateActor` | throws `"You can only create actors on server"` |
| `[Server]` method | warning, no-op |
| RPC | `LogError("RPC Function … called on Client.")`, no-op |
| `Actor.Destroy()` | warning, no-op |
| SyncVar setter | no error, nothing sent |

Last Stand hit the first row in play: removing the hunter-zone modifier at
game over threw, and the exception aborted the session reset halfway.
The fix has a fixed order: **clear your own state first, then touch the
game only while the server is active, and catch anything the game throws**.

```csharp
public void Remove()
{
    if (!IsActive) { return; }

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

The same guard sits on every restore path: putting the hunt level back,
restoring `goldCostMultiplier` to 1, destroying the tracker quest,
destroying curses. If the server is gone, the objects are gone with it, and
skipping the write is correct. Patches get the same treatment:
`LastStandMode.IsHosting()` includes `NetworkServer.active`, so a patch that
fires during teardown does nothing. More on unload-safe lifecycles is in
`07-mod-architecture.md`.

---

## 9. Late joiners

When a player joins mid-run, Mirror spawns every object they observe and
calls `OnSerialize(writer, initialState: true)`:

```csharp
public virtual void OnSerialize(NetworkWriter writer, bool initialState)
{
    SerializeSyncObjects(writer, initialState);
    SerializeSyncVars(writer, initialState);
}
```

| State | Replayed to a late joiner? |
|---|---|
| SyncVars | **yes**: the `forceAll` branch writes every one; hooks fire for values that differ from the prefab |
| `SyncList`/`SyncDictionary` | **yes**: `OnSerializeAll` sends full contents |
| spawned actors | **yes** |
| `[ClientRpc]`/`[TargetRpc]` sent before they were an observer | **no** |
| local state those RPCs produced on other clients (quest title, chat lines, a center message) | **no** |
| per-player synced entries keyed by the newcomer's `guid` | exist only once the host writes them |

Ways to handle it, in order of preference:

1. **Carry it in synced state.** Anything readable from a SyncVar or sync
   collection needs no extra work. This is why the tracker's changing text
   lives in `currentProgress` and not in an RPC.
2. **Re-send RPCs when the audience changes.** `WaveTracker` counts ready
   connections and re-sends the title keys whenever the count changes:

   ```csharp
   int connections = ReadyConnections();
   if (connections != _labeledConnections)
   {
       _quest.SetLocalizedTitle(GameNames.TrackerTitleKey);
       _quest.SetLocalizedDescription(GameNames.TrackerDescriptionKey);
       _labeledConnections = connections;
       Log.Info(LogText.TrackerLabelSent(connections));
   }
   ...
   foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
   {
       if (connection != null && connection.isReady) { ready++; }
   }
   ```

   Count *ready* connections. A client becomes an observer (and can receive
   RPCs) only after it has loaded the scene and reported ready. Counting
   raw connections would send the RPC to someone who drops it. Re-sending to
   everyone is harmless here because the client body only sets a string.
   Check that before re-sending any RPC with visible effects.
3. **Re-apply per-player state on a timer.** The Maw pin (6.2) runs once a
   second, so a newcomer's `guid` gets its entry within a second of joining.
   Throttle these timers. Never write every frame.
4. **Know what the game does on its own.** For example, `DewPlayer.OnStopServer`
   destroys a leaving player's reserved items, and JSON overrides are pulled
   by the joining client (`CmdRequestOverrides` → `TpcSendOverrides`).

Test with a second machine or account joining *after* the change happened.
Testing only with everyone present from the start hides every row marked
"no" above (`09-debugging-and-testing.md`).

---

## 10. Checklist: kind of data → reaches a vanilla client? → how to change it

| Kind of data | Reaches vanilla client? | How to change it from the host |
|---|---|---|
| `[SyncVar]` field | yes, continuously | `obj.NetworkField = v` (never the raw field) |
| `[SyncVar]` auto-property | yes | `obj.Property = v`; private setter → reflect `Network<name>k__BackingField` |
| `SyncList` / `SyncDictionary` / `SyncSet` | yes | mutate in place on the host (reflect private fields); copy-and-reassign struct elements; compare before writing dictionaries |
| new vanilla actor (modifier, quest, shrine, status effect, monster) | yes, incl. late joiners | `Dew.CreateActor`, `QuestManager.StartQuest`, `ZoneManager.AddModifier`; set SyncVars in `beforePrepare` |
| destroying an actor | yes | `actor.Destroy()` on the host |
| existing `[ClientRpc]` / `[TargetRpc]` | yes, current observers only | call it on the host; re-send for late joiners if it set lasting local state |
| chat line | yes, as an RPC | `ChatManager.BroadcastMessage` with `MessageType.Raw` (custom text) |
| server outcomes (damage, death, gold, items, buffs) | yes, via their synced state | change server logic (patches, server methods) |
| plain/prefab field on a spawned object | **no** (client uses its prefab) | change a synced input the client compares it with (6.1), or accept host-only effect if only the server reads it (6.10) |
| `[NonSerialized]` field without `[SyncVar]` | **no** | same as above |
| `ManagerBase<T>` state (music, camera, edit window, message box) | **no** | none; spawn something that drives it on clients, or drop it |
| UI text built locally (location label, panels) | only via its synced inputs, at its recompute time | trace its inputs and triggers (6.4–6.6) |
| localized strings | only keys the client already has | pick an existing key; free text only through raw channels (`currentProgress`, `MessageType.Raw`) |
| static tuning (prefab numbers of allowed types) | yes, via JSON overrides, for every run | `overrides` folder (`05-assets-and-data.md`) |
| your own `NetworkBehaviour`, `[SyncVar]`, RPC | **no** (not woven, unknown to clients) | not possible; find an existing channel |
| anything after transport shutdown | **no**, and sync collections throw | guard with `NetworkServer.active`, clear own state first |

---

## Where else this applies

- **Shrine of Destiny resets:** `currentItems` and `rerollCount` are public
  `SyncDictionary`s keyed by player guid. Writing them is the same technique
  as 6.2 without reflection.
- **Shrine locks:** `shrine.isLocked = true/false` on the host. Clients run
  the vanilla lock/unlock effects through the SyncVar hook (Last Stand's
  Ascension unlock after a wave).
- **Hunt level HUD:** `zones.currentHuntLevel` is a synced auto-property.
  The host sets it and every client's hunt display follows.
- **Chaos reward rarity:** `Shrine_Chaos.Networkrarity` in `beforePrepare`
  changes the reward pool on the server and the color on clients at once.
- **Relations between players:** `DewPlayer.neutrals`/`enemies` are
  `SyncList<DewPlayer>`, so host writes change who can hit whom for
  everyone.
- **Hiding world-map nodes:** the `SyncList<WorldNodeData>` copy-and-reassign
  write (7.3) moves nodes to negative x, which each client's map skips.
- **Any "why does the client see X?" bug:** run section 5 in reverse. Start
  from the client's wrong pixel and look for the first input in the chain
  that is not synced.
