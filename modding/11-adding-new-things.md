# 11. Adding new things: the end-to-end method

This chapter is the procedure that ties the other ten together. It takes a
feature from "I want X in the game" to "X ships, survives real play, and
the next person knows why it works". It has four parts:

1. **The procedure**: eight steps, each with the concrete commands and
   questions that drive it.
2. **Worked example A**: a shrine with a per-wave claim limit (the game's
   `Shrine_Destiny` placed in Last Stand's arena).
3. **Worked example B**: reusing the game's room modifiers (`RoomMod_*`) as
   one-wave events in a room that is already cleared. This one broke in
   four different ways, and how each break was found is the most useful
   part.
4. **Worked example C** (short): a pure rule plus a kill watch plus stat
   math that has to respect how the game adds bonuses.

It ends with a **field guide**: for ten kinds of feature, where to search,
which chapter has the technique, and the trap that usually bites.

The techniques themselves live in earlier chapters. This chapter links to
them instead of repeating them:

| Need | Chapter |
|---|---|
| Decompiling, navigating, grepping the game | `02-reading-the-game.md` |
| Picking and writing Harmony patches | `03-hooking-with-harmony.md` |
| SyncVars, RPCs, what a vanilla client sees | `04-host-only-networking.md` |
| Prefab values, localization tables, scenes | `05-assets-and-data.md` |
| Actors, entities, rooms, managers | `06-game-world-api.md` |
| Layers, reload survival, teardown | `07-mod-architecture.md` |
| Config files, settings UI, console, chat | `08-config-ui-and-commands.md` |
| Logs and test commands | `09-debugging-and-testing.md` |

---

## Part 1. The procedure

```
idea
 └─ 1. find the game's precedent ─── by visible name, by type name, by mechanic, by member use
     └─ 2. read its lifecycle ─────── create → OnCreate → OnStartServer → use → destroy, every input
         └─ 3. classify inputs ────── synced / prefab / host-only / client-local
             └─ 4. choose hooks ──── one funnel method, server side, once per event
                 └─ 5. write ──────── rule (pure) · adapter (game API) · patch (thin) · config · text · constants
                     └─ 6. survive ── reload · revisit · game over · late join · unload
                         └─ 7. measure ── one log line with numbers, an in-game check written first
                             └─ 8. document the why
```

Steps 1 to 4 are reading. Most of the time goes there, and most mistakes
come from cutting them short. A feature that "should just work" after
step 1 usually fails at step 3 (a field that isn't synced) or step 6 (a
state that doesn't come back after a reload).

### Step 1. Find the game's precedent

Never invent a mechanism when the game already has one. The game's own
code has been played thousands of hours. Your version hasn't. Something in
the game almost always already places, rewards, limits, scales or shows
the thing you want. Find that first, then bend it.

There are four entry points. Use whichever matches what you know.

#### 1a. You know what it looks like in game: visible name → localization key → type

The game's English strings and their keys are stored in a localization
table inside `<GameFolder>/Shape of Dreams_Data/resources.assets`, as
UTF-16 text (see `05-assets-and-data.md` for the format). Keys are built
from type names: `<TypeName>_Name`, `<TypeName>_Description`,
`<TypeName>_Tooltip`, and others. So the visible name leads straight to
the type.

```sh
cd "<GameFolder>/Shape of Dreams_Data"
strings -el resources.assets | grep -n -B1 '^Remnants of Destiny$'
```
```
104721-Shrine_Destiny_Name
104722:Remnants of Destiny
```

`strings -el` reads 16-bit little-endian strings. `-B1` prints the line
before each match, which is the key. Strip the suffix and you have the type:
`Shrine_Destiny`.

Two traps:

- **The English name is not what players call it.** Last Stand's settings
  say "Shrine of Destiny". The game says "Remnants of Destiny". Search for
  a distinctive word (`grep -i destiny`) when the exact name misses, and
  read the hits.
- **Other languages come first in the file.** The same key appears once
  per language (`Relikt des Schicksals`, `Vestigios del Destino`...). Search
  for the English value, not the key, to land in the English block.

The same search works for room modifiers (`Accelerated Time` →
`RoomMod_AcceleratedTime_Name`), status effects, memories and essences.

#### 1b. You know (part of) the type name: the DLLs

```sh
M="<GameFolder>/Shape of Dreams_Data/Managed"
strings "$M/Dew.Contents.dll" | grep -i destiny
ilspycmd -r "$M" -t Shrine_Destiny "$M/Dew.Contents.dll" > Shrine_Destiny.cs
```

`Dew.Core.dll` (managers, base classes, most systems) decompiles as a
whole. `Dew.Contents.dll` (monsters, room modifiers, shrines, skills,
gems) does not export in one go, so decompile one type at a time with
`-t`. `02-reading-the-game.md` covers the setup.

While you are there, look for a **spawner of the thing**. Content types
often come with a small type whose only job is to create them. For
`Shrine_Destiny` that is `RoomMod_SpawnDestiny`:

```csharp
public class RoomMod_SpawnDestiny : RoomModifierBase
{
	public override void OnStartServer()
	{
		base.OnStartServer();
		PlaceShrine<Shrine_Destiny>(new PlaceShrineSettings
		{
			spawnOnLastSection = true,
			lockedUntilCleared = true,
			removeModifierOnUse = false
		});
	}
}
```

That is the game's own answer to "how do I put this shrine in a room".
Reading `RoomModifierBase.PlaceShrine<T>` shows the actual call is
`Dew.CreateActor<T>(position, null, this)`, and that on a revisit
(`!isNewInstance`) it finds the existing shrine instead of making a new
one. Both facts reappear in example A.

#### 1c. You know the mechanic: mechanic → manager

Global systems live in singleton managers. `NetworkedManagerBase<T>`
managers exist on the server and every client and carry synced state.
`ManagerBase<T>` managers are local to one machine. Get the list once:

```sh
grep -rhoE "class \w+ : (NetworkedManagerBase|ManagerBase)<\w+>" Dew.Core Dew.UI | sort
```

The ones a gameplay feature usually starts from:

| Mechanic | Manager | Start reading at |
|---|---|---|
| Gold, dream dust, pickups, kill drops | `PickupManager` (networked) | `DropGold`, `DropDreamDust`, `HandleDeathDrop` |
| Item rarity and level rolls | `LootManager` (networked) | `SelectSkillRarity`, `SelectGemAndQuality` |
| Prices, ambient level, run state | `GameManager` (networked) | `GetAdjustedGoldAmount_*`, `SetAmbientLevel`, `goldCostMultiplier` |
| World map, nodes, room modifiers, travel | `ZoneManager` (networked) | `AddModifier`, `RemoveModifier`, `currentZoneIndex` |
| Every actor and entity alive | `ActorManager` (networked) | `allEntities`, `ClientEvent_OnActorAdd` |
| Game-wide events | `ClientEventManager` (networked) | `OnHeroKnockedOut`, `OnHeroRevive` |
| Quests (the tracker on the right) | `QuestManager` (networked) | `DewQuest.currentProgress` |
| Chat | `ChatManager` (networked) | `BroadcastMessage`, `MessageType.Raw` |
| Lobby / mode settings | `GameSettingsManager` (networked) | `gameSettingsSaveKey` |
| Music | `MusicManager` (local only) | nothing to sync |

Per-room systems hang off `Room` (`SingletonDewNetworkBehaviour<Room>`)
as components: `room.monsters` (`RoomMonsters`), `room.modifiers`
(`RoomModifiers`), `room.rewards`, `room.props`, `room.rifts`,
`room.sections`.

#### 1d. You know a member: find every reader in the IL

When the question is "who reads this field?" or "who calls this method?",
grepping decompiled `.cs` files misses everything you haven't decompiled,
which for `Dew.Contents.dll` is most of it. Scan the IL instead. Mirror
ships its own copy of Mono.Cecil in the game's `Managed` folder as
`Mono.CecilX.dll`, so a ten-line console program can read every method
body of any game assembly:

```xml
<!-- ilscan.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Mono.CecilX">
      <HintPath>PATH/TO/Managed/Mono.CecilX.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```
```csharp
using Mono.CecilX;
using Mono.CecilX.Cil;

string dll = args[0];
string member = args[1];
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(dll));
var module = ModuleDefinition.ReadModule(dll, new ReaderParameters { AssemblyResolver = resolver });
int unreadable = 0;
foreach (TypeDefinition type in module.GetTypes())
foreach (MethodDefinition method in type.Methods)
{
    MethodBody body;
    try { body = method.HasBody ? method.Body : null; }
    catch { unreadable++; continue; }
    if (body == null) continue;
    foreach (Instruction ins in body.Instructions)
    {
        if (ins.Operand is MemberReference m && m.FullName.Contains(member))
        {
            Console.WriteLine($"{type.FullName}::{method.Name}  {ins.OpCode} {m.Name}");
        }
    }
}
Console.Error.WriteLine($"{unreadable} method bodies could not be read");
```
```sh
dotnet run -- "$M/Dew.Contents.dll" "Room::get_didClearRoom"
```

Notes on reading the output:

- A property read is a call to its getter, so search `Room::get_didClearRoom`,
  not `didClearRoom`. A write is `set_…`. A field is `ldfld`/`stfld` on
  `Type::field`.
- `module.GetTypes()` includes nested compiler-generated types. Coroutines
  and local functions show up as names like
  `RoomMod_FireDevil/<<OnCreate>g__Routine|0_1>d::MoveNext`. That hit means
  "the `Routine` local function inside `RoomMod_FireDevil.OnCreate`".
- A handful of method bodies in `Dew.Contents.dll` don't parse (23 when
  this was written; the same reason the whole-assembly export fails). The
  `try` keeps the scan going. The count tells you how big the blind spot is.

The output of this exact scan is the backbone of example B.

**Where else this applies.** Which shrines call `DoPostUseRoutines`
outside `Shrine.OnInteract` (search `Shrine::DoPostUseRoutines`). Every
writer of `GameManager.goldCostMultiplier` (`set_Network` setter). Every
monster that overrides death (`::OnDeath`). Every content type that calls
`RoomMonsters::OverrideMonsterType`.

### Step 2. Read the lifecycle and every input

Once you have the type, read it top to bottom with one question per
stage: what happens, on which machine, and what does it read?

The stages of any `Actor` (shrines, room modifiers, monsters, quests,
status effects are all actors; see `06-game-world-api.md`):

| Stage | Where | What to look for |
|---|---|---|
| `Dew.CreateActor<T>(pos, rot, parent, beforePrepare)` | server | `beforePrepare` runs before `PrepareAndSpawn`, so fields set there are in place for everything below |
| `OnPrepare`, `Awake` | server (and client for `Awake`) | defaults, prefab copies |
| `OnCreate` | server and every client | look for `if (base.isServer)` blocks: the part inside is server-only |
| `OnStartServer` / `OnStartClient` | one side each | Mirror's start callbacks; room modifiers do most of their work in `OnStartServer` |
| `ActiveLogicUpdate(dt)` / coroutines | usually server | per-tick behavior; every field read here is an input |
| use (`OnInteract`, `Cmd…`, `OnUse`) | server | the gate checks (affordability, limits, availability) |
| `OnDestroyActor` | server and clients | cleanup: what comes back when the actor goes |

For each stage, list **every input**: fields, properties, other managers,
static singletons (`Rift.instance`, `Room.instance`). The full input list is
what step 3 classifies, and the singletons are where crashes come from
when you move a thing into a context its author didn't imagine.

Example, the gate of every shrine use, `Shrine.CanInteract`:

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
```

Inputs: `isAvailable`, `creationTime`, `interactableDelay`, `isOnCooldown`
(which reads `_lastUseTime` and `useCooldown`), `maxUseCountPerPlayer`,
`_useCountByPlayer`. Six inputs; step 3 decides which of them you can
actually change for a vanilla client.

Also find **who calls the gate**. `CanInteract` is called by
`ActionInteract.Tick` (the hero walking up to the shrine, which runs on
the server because it ends in the `RpcInvokeInteract` ClientRpc), by
`EditSkillShrine`'s `CmdActivateEditSkillAction` handler on the server,
and on each client by local code such as `ControlManager` and
`EditSkillManager` (`grep -rln '\.CanInteract(' Dew.Core` lists them).
So the same gate runs on both sides with each side's own inputs.

### Step 3. Classify every input: synced, prefab, host-only, client-local

The host runs the modded game. Clients run the vanilla game. Whatever a
client computes for itself, it computes from *its* copy of each input.
Classify each input from the declaration (details in
`04-host-only-networking.md`):

| Declaration | Class | Host can change it for clients? |
|---|---|---|
| `[SyncVar]` field, or a `Network<Name>` setter property | synced | yes, write through the `Network…` setter |
| `SyncList` / `SyncDictionary` / `SyncHashSet` field | synced | yes, mutate the collection on the server |
| sent by `[ClientRpc]` / `[TargetRpc]` | event | only at the moment it is sent; late joiners don't get replays |
| plain public field with no sync attribute | prefab | no: each client reads its own prefab value |
| `[NonSerialized]` with no sync attribute | runtime local | no: exists only on the machine that set it |

`[NonSerialized]` is about Unity's prefab serialization, not about the
network. `GameManager.goldCostMultiplier` is `[NonSerialized]` **and**
`[SyncVar]`, so it is synced. `Shrine_MawOfDoom.priceMultipliersByPlayer`
is `[NonSerialized]` with no sync attribute, so it is host-only. Read both
attributes.

Prefab values are read from the asset bundles, not from the code (the
initializer in code is often overridden by the prefab). The method is in
`05-assets-and-data.md`: UnityPy with a type-tree generator built from
`Managed`, then `read_typetree()` on the prefab's `MonoBehaviour`. For
`Shrine_Destiny`:

```
Shrine_Destiny {'maxUseCountPerPlayer': 1, 'maxTotalUseCount': -1, 'useCooldown': 0.0,
                'baseGoldCost': 0, 'unavailableByDefault': 0}
```

Then decide, input by input, what a vanilla client will see. Write it down
as a sentence: "the client thinks the limit is 1, because
`maxUseCountPerPlayer` is a prefab field". That sentence decides the
design in step 5.

### Step 4. Choose hooks

A hook is where your code learns that something happened, or gets to
change it. Rules (patch mechanics in `03-hooking-with-harmony.md`):

1. **Prefer a funnel.** Find the one method every path goes through.
   List the callers (`grep -rn 'DoPostUseRoutines(' Dew.Core`, plus the
   IL scan for `Dew.Contents`). If three paths lead to one method, patch
   that method, not the three paths.
2. **Server side.** The host's patches only run on the host. Pick a method
   that executes on the server for the event you care about. A method
   that only runs on clients (UI, `ClientRpc` bodies) can't be observed
   for remote players.
3. **Once per event.** Per-frame methods are a last resort. If you must
   poll, poll on a throttled timer (`07-mod-architecture.md`, "the tick
   loop").
4. **Prefer events over patches** when the game raises one:
   `entity.EntityEvent_OnDeath`, `ClientEventManager.OnHeroKnockedOut`,
   `ActorManager.ClientEvent_OnActorAdd`, `RoomMonsters.onAfterSpawn`. They
   cost nothing to undo.
5. **Postfix to correct, prefix to block.** A postfix sees the state after
   the game's own bookkeeping and can overwrite it. A prefix returning
   `false` skips the original; use it only to block, and only under your
   mode's condition.
6. **Gate every patch** on "my mode is on and I am the server". In Last
   Stand that is `ModMode.IsHosting()` (shared library), which is
   `NetworkServer.active && settings.gameSettingsSaveKey == SaveKey`.

### Step 5. Write it in its layers

`07-mod-architecture.md` explains the layering. The short version, as a
checklist for one feature:

| Piece | Holds | Touches the game? | Last Stand example |
|---|---|---|---|
| Rule | the math and the decisions | no | `FastClearRule`, `WaveSchedule.HasEnvironment` |
| Service / owner | state for one feature, calls rule and adapters | via adapters | `DestinyShrine`, `ArenaEnvironment` |
| Adapter | one game API, wrapped, no decisions | yes | `UseCountPin`, `RoomCombat`, `ArenaModifier` |
| Patch | hook → hand off | yes, minimally | `ArenaShrineUsePatch` |
| Config field | the tunable, its default | no | `destinyClaimsPerWave = 2` |
| Validation | clamp to sane values once | no | `SessionSettings.From` |
| Text | every string, chat, log, label | no | `LogText.DestinyClaimsReset` |
| Constants | private member names, prefab names | no | `GameNames.ShrineUseCounts = "_useCountByPlayer"` |

Use `typeof`/`nameof`/generic APIs for every public game member, and a
single named constant for private names you reach by reflection.

### Step 6. Make it survive

A feature works when it works after each of these. Go through the list
for every piece of state the feature creates or changes:

| Event | What happens | Question to answer |
|---|---|---|
| **Mod reload** (dev auto-reload, or the mod manager) | new assembly, new statics; actors in the world stay | Does the new instance *find* what the old one placed, or place a second one? What in-memory state is lost, and is that acceptable? |
| **Room revisit / reload** | room state saved to `ZoneManager.visitedNodesSaveData` and restored; room modifiers re-created with `isNewInstance == false` | Does the thing re-create itself, or vanish, or double? |
| **Game over / restart** | transport shuts down; synced collections become read-only | Does any cleanup write synced state after `NetworkServer.active` went false? |
| **Late join** | the joiner receives current SyncVars and Sync collections, but no past RPCs | Is everything the joiner needs in synced state? |
| **Unload** | `OnDestroy` on your `ModBehaviour` | Is every global change (multipliers, patches, injected UI) put back? |
| **Setting changed mid-run** | new snapshot of the config | Does turning it on create it, and turning it off remove it? |

Reload survival patterns (an AppDomain slot for small state, adopting
existing actors instead of tracking them in memory) are in
`07-mod-architecture.md`, section 7.

### Step 7. Measure

Two artifacts, both written **before** the first test:

1. **A log line with numbers**, with the mod's fixed prefix, at the moment
   the feature decides something. Not "destiny reset" but
   `destiny: claims reset after wave 12.` Not "fast clear" but
   `fast clear: wave 31 had 40 of 50 monster(s) die within 0.5 s of their first hit; from wave 32
   monsters have x2 health and damage, heroic bosses x1.41.` The numbers are
   what let you compare the log to what you saw.
2. **An in-game check**: a numbered paragraph that says exactly what to
   do, what you should see on the host and on a vanilla client, and the
   log line to look for. Writing it first forces you to decide what
   "working" means, and it is the list you walk after every later change.

Then test with commands that jump to the situation (`laststand_wave 49`
style; see `09-debugging-and-testing.md`) rather than playing an hour to
reach it.

### Step 8. Document the why

The code says *what*. The doc for the feature says *why this way*: which
game method it relies on, which field is or isn't synced, what the first
version did and why it failed. In Last Stand that is one file per topic
under `docs/`, a line in `docs/README.md`, an entry in the in-game check
list, and a config schema bump when a default changes (old config files
keep old values otherwise; `07-mod-architecture.md`, section 9.1).

---

## Part 2. Worked example A: a shrine with a per-wave claim limit

**Goal.** Place the game's Shrine of Destiny at the edge of the arena.
Each player may claim its essence **twice per wave**, and the limit
resets when a wave is cleared. Vanilla clients must see the right prompt
and must not be able to claim a third time.

### A1. Precedent

Visible name to type, as in step 1a: "Remnants of Destiny" →
`Shrine_Destiny_Name` → `Shrine_Destiny` in `Dew.Contents.dll`.
Decompile it with `ilspycmd -t Shrine_Destiny`. Its spawner,
`RoomMod_SpawnDestiny`, shows the placement call
(`Dew.CreateActor<T>`) and the revisit behavior (find the existing one).

Hierarchy: `Shrine_Destiny : EditSkillShrine, ICustomInteractable`, and
`EditSkillShrine : Shrine`. So three files to read: `Shrine.cs`,
`EditSkillShrine.cs` (both `Dew.Core`), `Shrine_Destiny.cs`.

### A2. Lifecycle and inputs

`Shrine_Destiny` has two interactions:

- **Alt interact (sacrifice)** opens the edit window. The server handler
  `CmdActivateEditSkillAction` checks `CanInteract(player.hero)`, then
  calls `OnActivateEditSkill`. Destiny's override lowers the essence's
  quality by `GetRerollCost` (or destroys it under 10), bumps the synced
  `rerollCount[guid]`, rerolls the synced offer `currentItems[guid]`, and
  returns `false`. `EditSkillShrine` only calls `DoPostUseRoutines` when
  that returns `true`, so **a sacrifice is not a use**.
- **Normal interact (claim)**:

```csharp
IEnumerator Routine()
{
	currentItems.Remove(player.guid);
	Vector3 pos = GetRandomSpawnPosition(entity.position);
	DoPostUseRoutines(entity);
	yield return new WaitForSeconds(0.5f);
	Dew.CreateGem(prefab, pos, NetworkedManagerBase<LootManager>.instance.SelectGemQuality(prefab.rarity), player);
}
```

The claim is a use, and the gem is created with `tempOwner = player`
(reserved for that player; see the "Reserved items" fact in
`06-game-world-api.md`).

Then the funnel, `Shrine.DoPostUseRoutines`:

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

Note the last two lines before the RPC: after counting the use, the
shrine recomputes `isAvailable`. `UpdateIsAvailable` sets it to `false`
when no living player has a use left. In solo, the first claim makes the
shrine unavailable for everyone.

And the reset the game already provides:

```csharp
public void ResetUseCounts()
{
	Network_003CtotalUseCount_003Ek__BackingField = 0;
	_useCountByPlayer.Clear();
	UpdateIsAvailable();
}
```

### A3. Classify

From `Shrine.cs`:

```csharp
[SyncVar] public int maxTotalUseCount = -1;
public int maxUseCountPerPlayer = -1;
public float useCooldown = 1f;
[SyncVar] private double _lastUseTime = double.NegativeInfinity;
private readonly SyncDictionary<string, int> _useCountByPlayer = new SyncDictionary<string, int>();
[SyncVar(hook = "OnIsAvailableChanged")] private bool <isAvailable>k__BackingField;
```

(attributes condensed; `maxTotalUseCount` also carries `[SaveVar]`)

| Input of `CanInteract` | Class | Prefab value |
|---|---|---|
| `maxUseCountPerPlayer` | **prefab, not synced** | 1 |
| `_useCountByPlayer` | synced (`SyncDictionary`, private) | — |
| `isAvailable` | synced | — |
| `useCooldown` | prefab | 0 |
| `_lastUseTime` | synced | — |
| `maxTotalUseCount` | synced | −1 |

The sentence for step 3: *a vanilla client always believes the limit is
1, and blocks itself as soon as its synced use count reaches 1.*

That kills the obvious design. Setting `maxUseCountPerPlayer = 2` on the
host does nothing for clients: they keep 1 and refuse the second claim
locally. The only lever that reaches them is the **synced count**.

Design that follows from the table: keep the prefab limit of 1
everywhere, keep a per-player tally on the host, and after each claim,
while the player is under the real limit, write their synced count back
to 0. After the last allowed claim, leave it at 1: the client and the
server both refuse, and because `CmdActivateEditSkillAction` also checks
`CanInteract`, sacrifices are blocked too, which is what you want (a
player out of claims has no reason to reroll). When the wave is cleared,
call `ResetUseCounts()`.

Why a limit at all: with no limit, sacrifice and claim loop forever: each
sacrifice only costs about 20 quality, so one high-quality essence turns
into dozens.

### A4. Hooks

- **Use**: a postfix on `Shrine.DoPostUseRoutines`. It is the funnel for
  every shrine's successful use (base `OnInteract`, `EditSkillShrine`
  edits, Destiny's claim routine). A postfix runs after the increment and
  after `UpdateIsAvailable`, so it can overwrite both. One patch serves
  every shrine feature in the mod; it filters by instance.

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

  The parameter is named `entity` because Harmony binds by the original
  parameter name; `__instance` is the shrine.
- **Wave cleared**: the mod's own wave director raises it; no patch.

### A5. Write

The adapter that writes the private synced dictionary (reflection by a
named constant, `GameNames.ShrineUseCounts = "_useCountByPlayer"`):

```csharp
internal static class UseCountPin
{
	private static readonly FieldInfo UseCounts = AccessTools.Field(typeof(Shrine), GameNames.ShrineUseCounts);

	public static void Set(Shrine shrine, DewPlayer player, int uses)
	{
		if (!shrine.IsNullOrInactive() && player != null && player.guid != null
		    && UseCounts?.GetValue(shrine) is SyncDictionary<string, int> counts)
		{
			counts[player.guid] = uses;
		}
	}
}
```

Writing a `SyncDictionary` on the server marks it dirty and Mirror sends
the change; clients see the new count on the next sync. That is why it
works where writing `maxUseCountPerPlayer` doesn't.

The owner of the feature:

```csharp
public void OnUsed(Shrine shrine, Entity user, int claimsPerWave)
{
	DewPlayer player = user != null ? user.owner : null;
	if (!_standing.Owns(shrine) || player == null || player.guid == null)
	{
		return;
	}

	_claims.TryGetValue(player.guid, out int claims);
	claims++;
	_claims[player.guid] = claims;
	if (claims >= claimsPerWave)
	{
		return;
	}

	UseCountPin.Set(shrine, player, ClaimsLeftOpen);
	shrine.MakeAvailable();
}

public void OnWaveCleared(int wave)
{
	_claims.Clear();
	Shrine_Destiny shrine = _standing.Current;
	if (shrine.IsNullOrInactive())
	{
		return;
	}

	shrine.ResetUseCounts();
	Log.Info(LogText.DestinyClaimsReset(wave));
}
```

`MakeAvailable()` undoes what `UpdateIsAvailable` did inside
`DoPostUseRoutines` (in solo, `isAvailable` went false on the first claim).
Both writes are synced, so the client's prompt comes back.

Placement goes through a small generic owner, `StandingShrine<T>`, shared
by all of Last Stand's shrines:

```csharp
_shrine = FindExisting(position);
if (_shrine != null)
{
	Configure(_shrine);
	Log.Info(LogText.ShrineReused(_name, position));
	return;
}

_shrine = Dew.CreateActor<T>(position, null, null, Configure);
Log.Info(LogText.ShrinePlaced(_name, position));
```

`Configure` runs as `beforePrepare`, so the limits are set before the
shrine spawns on clients. For a shrine kept at vanilla limits it copies
them from the prefab (`DewResources.GetByType<T>()`). The position comes
from the edge ring (navmesh sampling; see `06-game-world-api.md`), and
the turned-on shrines share the ring at even angles.

The rest is bookkeeping in its single places:

| Piece | Where |
|---|---|
| `public bool shrineOfDestiny = true;` `public int destinyClaimsPerWave = 2;` | `LastStandConfig` |
| `Math.Max(Tuning.MinimumDestinyClaims, config.destinyClaimsPerWave)` | `SessionSettings.From` |
| Label and description for the settings window | `ConfigText` |
| `destiny: claims reset after wave {wave}.` | `LogText` |
| `"_useCountByPlayer"` | `GameNames` |

### A6. Survive

| Event | Behavior | How |
|---|---|---|
| Mod reload | the shrine in the world is adopted, not duplicated | `FindExisting` scans `Object.FindObjectsByType<Shrine_Destiny>` |
| Mod reload | the host's tally is lost; a player with one claim left gets two | accepted and documented: rebuilding it would need a second copy of state |
| Late join | the joiner sees correct counts and offers | `_useCountByPlayer`, `currentItems`, `rerollCount` are all synced collections |
| Leaving the arena / game over | the owner drops its reference and its tally | `Forget()` from the session's single teardown path |
| Setting turned on mid-run | the shrine gets a fresh ring position, which may not line up with the others until the next run | documented in the edge-shrine doc |

### A7. Measure

Log lines: `Shrine of Destiny placed at (x, y, z).` on arrival,
`Shrine of Destiny already standing at …; reusing it.` after a reload,
`destiny: claims reset after wave N.` after each wave.

The in-game check, written before testing:

> **Shrine of Destiny:** on by default, on the edge ring. On the host and
> a vanilla client: the alternate interact sacrifices an essence, shows
> the right quality cost (and the destroy warning under 10), and the
> shrine shows a new offer; sacrificing again rerolls it at a higher
> cost. Claiming drops the essence for that player only. Each player can
> claim twice per wave; after the second claim the shrine can't be used
> (no sacrifice, no claim) until the wave is cleared (log: `destiny:
> claims reset after wave N`), then two more. After a mod reload there's
> still just one shrine.

Every clause maps to one input from A3 or one row from A6.

### A8. Document

The doc records the four non-obvious facts: the limit field isn't synced,
the count is; sacrifices don't call `DoPostUseRoutines`; the last claim
also blocks sacrifices; a reload forgets the tally. Without the first
one, the next person "fixes" the feature by setting
`maxUseCountPerPlayer`.

**Where else this applies.** Any vanilla limit you want to change for
clients: find the synced counter the limit is compared against and
rewrite the counter, not the limit. Last Stand uses the same pin for the
Maw of Doom (its price and destroy chance are formulas of the synced use
count, so pinning the count pins both) and resets for the Hatred Remnant
and Shrine of Ascension. The trap to check each time: a count at or
above the client's limit makes `CanInteract` false, and for edit-window
shrines `EditSkillManager` then closes the window every frame, so a count
can never be used to carry a price past what the limit allows.

---

## Part 3. Worked example B: room modifiers as one-wave events

**Goal.** Every N waves, turn one of the game's room modifiers
("environments": Accelerated Time, Hazardous Weather, Dream of Wealth…)
on for one wave, in the arena, and off when the wave is cleared. Vanilla
clients must see the effects.

The arena is a boss room loaded as already cleared. That one fact is the
source of most of what follows.

### B1. Precedent: how the game picks and adds modifiers

Room modifiers are `RoomModifierBase` actors named `RoomMod_*`. World
generation collects the candidates in
`ZoneManager.LoadModifierLightPrefabsOfCurrentZone`:

```csharp
foreach (RoomModifierBase item in DewResources.FindAllByTypeSubstring<RoomModifierBase>("RoomMod_", ResourceLoadSettings.Light))
{
	if (Dew.IsRoomModifierIncludedInGame(item.GetType().Name) && !item.excludeFromPool
	    && (item.allowedZones == null || item.allowedZones.Length == 0 || item.allowedZones.Contains(currentZone.name))
	    && item.IsAvailableInGame() && currentZoneIndex >= item.zoneIndexRange.x
	    && currentZoneIndex <= item.zoneIndexRange.y && !bannedRoomModifiersForCurrentLoop.Contains(item.GetType().Name))
	{
		list.Add(item);
	}
}
```

and writes the chosen ones straight into node data
(`WorldNodeData.modifiers`, a list of `ModifierData { id, type,
clientData }`). That is world gen, not something you can call mid-room.

The mid-room path is `ZoneManager.AddModifier`. The game uses it for
Corrupted Chaos, `RoomMod_SpawnMiniBoss`, the hunter status updates on
travel, and `RoomMod_HeroSoul` on a knock-out. Read it whole:

```csharp
public int AddModifier(int nodeIndex, ModifierData mod, Action<RoomModifierBase> beforePrepare = null)
{
	if (mod.id == 0) mod.id = _nextModifierId++;
	...
	if (DewResources.GetByShortTypeName<RoomModifierBase>(mod.type).isMain)
	{
		List<ModifierData> modifiers = nodes[nodeIndex].modifiers;
		for (int num = modifiers.Count - 1; num >= 0; num--)
		{
			if (DewResources.GetByShortTypeName<RoomModifierBase>(modifiers[num].type).isMain)
			{
				RemoveModifier(nodeIndex, modifiers[num].id);
				break;
			}
		}
	}
	... nodes[nodeIndex].modifiers.Add(mod) ...
	if (nodeIndex == currentNodeIndex && Room.instance != null && Room.instance.modifiers != null
	    && Room.instance.modifiers.isRoomActive)
	{
		modifierServerData[mod.id] = new ModifierServerData { didCreateInstance = false };
		Room.instance.modifiers.HandleRuntimeAddition(mod.id, beforePrepare);
		...
	}
	...
}
```

(condensed). Three facts fall out:

1. **It returns an id.** `RemoveModifier(nodeIndex, id)` takes it back out.
2. **One main modifier per node.** Adding a main modifier silently removes
   the newest other main one.
3. **On the current node it goes live at once**, through
   `RoomModifiers.HandleRuntimeAddition`, which calls
   `Dew.CreateActor(prefab, …, beforePrepare)` and sets
   `mod.isNewInstance = !didCreateInstance`, which is `true` for a fresh
   add.

The arena strips and blocks room modifiers on its node (they would put
shrines, props and minibosses in it), so the mod's own adds go through a
gate that lets them past. That gate counts "own adds in progress" around
the call:

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

### B2. Lifecycle and inputs of a modifier

`RoomModifierBase` (in `Dew.Core`):

- `OnCreate`: plays `fxLoop`, hooks events.
- `OnStart`: places decorations; if `disableOtherEnvParticles`, hides the
  room's `EnvParticle`s.
- `OnStartServer` (per subclass): the gameplay.
- `ModifyEntities(onStart, onStop)`: runs `onStart` on every entity now
  and on every entity added later, and queues `onStop` for every entity on
  destroy:

```csharp
public void ModifyEntities(Action<Entity> onStart, Action<Entity> onStop)
{
	foreach (Entity allEntity in NetworkedManagerBase<ActorManager>.instance.allEntities)
		onStart?.Invoke(allEntity);
	_onEntityAdd = (Action<Entity>)Delegate.Combine(_onEntityAdd, onStart);
	_onDestroyActor.Add(delegate
	{
		foreach (Entity allEntity2 in NetworkedManagerBase<ActorManager>.instance.allEntities)
			onStop?.Invoke(allEntity2);
	});
}
```

  (try/catch removed). So for status-effect environments (Accelerated
  Time, Gravity Training, Black Rain…), **destroying the modifier is the
  whole cleanup**.
- `OnDestroyActor`: stops `fxLoop`, destroys decorations, re-enables env
  particles, runs the queued `onStop`s.

Inputs to collect per subclass: `isNewInstance`, `Room.didClearRoom`,
`Rift.instance`, `room.monsters.defaultRule`, the prefab flags.

Prefab flags, read from the bundle (UnityPy, as in A3), columns `isMain,
hiddenOnVisitedNode, spawnOncePerLoop, disableOtherEnvParticles`:

```
RoomMod_AcceleratedTime                    (1, 0, 0, 1)
RoomMod_FireDevil                          (1, 0, 0, 1)
RoomMod_GoldEverywhere                     (1, 0, 0, 1)
RoomMod_LingeringAuraOfGuidance            (1, 0, 0, 1)
RoomMod_StarCookie                         (1, 0, 0, 0)
RoomMod_Hunted                             (1, 0, 0, 1)
RoomMod_CorruptedChaos_AuraOfPain          (1, 0, 0, 1)
RoomMod_FragmentOfRadiance_MysteriousPlace (1, 0, 0, 0)
RoomMod_HarderFightBetterReward            (1, 1, 0, 0)
RoomMod_SpawnMiniBoss                      (1, 1, 0, 0)
```

Every environment is main. So is the hunter zone, and so is the Aura of
Pain. Fact 2 from B1 now matters: an environment and a hunter zone can't
coexist on one node.

### B3. Classify

| Input | Class | Consequence |
|---|---|---|
| the modifier actor itself | networked actor | clients get its creation, status effects, hazards, visuals |
| `isNewInstance` | SyncVar on `Actor` | true for a runtime add, so loot environments place their props |
| `Room.didClearRoom` | `{ get; private set; }`, **not synced** | the host can change it; only server code reads it for these modifiers |
| `isRevisit` | SyncVar | true in the arena; `didClearRoom = isRevisit` at room start |
| `room.monsters.defaultRule` | server field | host-only |
| the modifier panel (`UI_InGame_RoomModDisplay`) | client-local UI | shown on `ClientEvent_OnActorAdd`, hidden only on room load |

### B4. The naive version and what breaks

The naive version does the obvious thing: pick a type, `AddModifier` on
the arena node at wave start, `RemoveModifier` at wave end. Status-effect
environments work. Four other things don't.

#### Break 1: hazards never fire

Hazardous Weather, Crimson Flame Demon, Gaze of the Abyss: the modifier
is added, chat announces it, and nothing falls from the sky. Giant Ink
Drop removes itself at once.

Hypothesis: something in them checks whether the room is cleared. Reading
`RoomMod_InkStrikeWarning` confirmed it for one type:

```csharp
public override void OnStartServer()
{
	base.OnStartServer();
	if (SingletonDewNetworkBehaviour<Room>.instance.didClearRoom)
	{
		DestroyIfActive();
		return;
	}
	...
```

and `RoomMod_FireDevil`'s routine:

```csharp
yield return new WaitForSeconds(Random.Range(0.5f, 2f));
if (SingletonDewNetworkBehaviour<Room>.instance.didClearRoom
    || NetworkedManagerBase<ZoneManager>.instance.isInAnyTransition || this.IsNullOrInactive())
{
	break;
}
```

One file at a time doesn't tell you how far the problem reaches. The IL
scan from step 1d does:

```sh
dotnet run -- "$M/Dew.Contents.dll" "Room::get_didClearRoom"
```
```
RoomMod_CorruptedChaos_AuraOfPain::ActiveLogicUpdate  callvirt get_didClearRoom
Mon_Sky_LittleBaam/<<OnCreate>g__Routine|5_0>d::MoveNext  callvirt get_didClearRoom
RoomMod_StrayMemory_StartProp::OnStartServer  callvirt get_didClearRoom
RoomMod_StardustEverywhere::ActiveLogicUpdate  callvirt get_didClearRoom
Se_RoomMod_WarpingField_Unstable::ActiveLogicUpdate  callvirt get_didClearRoom
Ai_UnstableRatSwarm_Projectile::OnComplete  callvirt get_didClearRoom
Se_LingeringAuraOfGuidance::ActiveLogicUpdate  callvirt get_didClearRoom
RoomMod_InkStrikeWarning::OnStartServer  callvirt get_didClearRoom
RoomMod_FireDevil::<OnCreate>g__GetStartEndPos|0_2  callvirt get_didClearRoom
RoomMod_FireDevil/<<OnCreate>g__Routine|0_1>d::MoveNext  callvirt get_didClearRoom
RoomMod_RiskOfMeteors::ActiveLogicUpdate  callvirt get_didClearRoom
RoomMod_RiskOfMeteors/<<ActiveLogicUpdate>g__Routine|11_0>d::MoveNext  callvirt get_didClearRoom
RoomMod_GazeOfErebos/<>c__DisplayClass4_0/<<OnStartServer>g__Routine|1>d::MoveNext  callvirt get_didClearRoom
Ge_Shrine_PotOfGreed::ClientEventOnRoomLoaded  callvirt get_didClearRoom
```

and on `Dew.Core.dll`:

```
GameManager::IsEligibleForMidRunSave  callvirt get_didClearRoom
Room/<<ClearRoom>g__Routine|92_0>d::MoveNext  call get_didClearRoom
RoomMonsters/<<OnRoomStartServerRoutine>g__Routine|32_1>d::MoveNext  callvirt get_didClearRoom
RoomSection::<Awake>b__39_0  callvirt get_didClearRoom
Section_Monsters/<ActivateCombatRoutine>d__35::MoveNext  callvirt get_didClearRoom
RoomModifierBase::PlaceShrine  callvirt get_didClearRoom
```

Now the whole picture is on one screen: five environments stop, one
destroys itself, Warping Field's teleports stop, the rat projectiles
change, Little Baam stops summoning, and the game skips its mid-run save
while the flag is false.

Fix, in two pieces:

1. **Hold the room "in combat" while an environment lives.** The setter
   is private but the flag isn't synced, so flipping it on the host is
   invisible to clients and exactly what every server-side reader sees:

```csharp
internal static class RoomCombat
{
	private static readonly MethodInfo SetCleared = AccessTools.PropertySetter(typeof(Room), nameof(Room.didClearRoom));
	private static Room _opened;

	public static bool IsHeldOpen => _opened != null;

	public static void Open()
	{
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || !room.didClearRoom) return;
		SetCleared.Invoke(room, new object[] { false });
		_opened = room;
	}

	public static void Close()
	{
		Room room = _opened;
		_opened = null;
		if (room != null) SetCleared.Invoke(room, new object[] { true });
	}
}
```

   `Open()` runs **before** `AddModifier`, because `InkStrikeWarning`
   checks the flag in `OnStartServer`, which runs during the add.
   `Close()` runs after removal, and in every failure path.
2. **Block the game's clear routine in that window.** With the flag false,
   something may call `Room.ClearRoom`, whose routine sets the flag,
   fires `onRoomClear`, and destroys idle creep monsters:

```csharp
foreach (Entity item in new List<Entity>(NetworkedManagerBase<ActorManager>.instance.allEntities))
{
	if (!item.IsNullInactiveDeadOrKnockedOut() && item is Monster monster && !(monster.owner != DewPlayer.creep)
	    && !monster.AI.disableAI && !NetworkedManagerBase<ZoneManager>.instance.isCurrentNodeHunted
	    && !(Time.time - monster.creationTime < 3f) && (monster.isSleeping || monster.AI.context.targetEnemy == null))
	{
		Debug.Log("Destroying leftover monster: " + item.GetActorReadableName());
		item.Destroy();
	}
}
```

   That would delete wave monsters. A prefix that returns `false` while
   `RoomCombat.IsHeldOpen` blocks it, and logs
   `environment: blocked the room's clear routine; …` so it shows up in
   `Player.log` if it ever fires.

Side effects accepted and documented: no mid-run save during that wave
(`IsEligibleForMidRunSave`), and monsters whose summons stop in cleared
rooms summon again, as they would in a normal fight.

#### Break 2: monster environments changed the room permanently

Leaf Hound Den and Meteoric Life resize monsters. The IL scan for
`RoomMonsters::OverrideMonsterType` finds exactly those two
(`RoomMod_LeafPuppies::OnStartServer`, `RoomMod_MeteoricLife::OnStartServer`).
The method:

```csharp
public void OverrideMonsterType(Monster m)
{
	...
	if (defaultRule == null)
	{
		defaultRule = NetworkedManagerBase<ZoneManager>.instance.currentZone.defaultMonsters;
	}
	defaultRule = UnityEngine.Object.Instantiate(defaultRule);
	defaultRule.pool = UnityEngine.Object.Instantiate(defaultRule.pool);
	...
```

It replaces `room.monsters.defaultRule`, and nothing ever puts it back.
Last Stand doesn't spawn from `defaultRule`, but other code (the miniboss
selection, which indexes the default rule's pool) does. The size and
health change come through `RoomMonsters.onAfterSpawn`, which fires for
every `SpawnMonsters` spawn, Last Stand's included, so the visible part
of the environment still works. Fix: save `defaultRule` before adding an
environment and restore it after removal:

```csharp
_monsters = room.monsters;
_defaultRule = _monsters.defaultRule;
RoomCombat.Open();
...
private void RestoreRoom()
{
	if (_monsters != null) _monsters.defaultRule = _defaultRule;
	_monsters = null;
	_defaultRule = null;
	RoomCombat.Close();
}
```

#### Break 3: some environments leave things behind

`RoomMod_LingeringAuraOfGuidance.OnStartServer`:

```csharp
if (!base.isNewInstance) return;
foreach (RoomSection section in SingletonDewNetworkBehaviour<Room>.instance.sections)
{
	section.TryGetGoodNodePosition(out var pos);
	CreateActor<Shrine_Guidance>(pos, null);
}
ModifyEntities(...);
```

`RoomMod_StarCookie` does the same with `Shrine_StarCookie`. Those
shrines outlive the modifier. They are kept in the list but off by
default. Others are left out of the list entirely, each for a reason
found by reading it: Ambush spawns tower actors; Dream of the Star Seeker
hands out stardust far faster than normal play; Distant Memories,
Challenging Fight and Fallen Star depend on the exit rift and room
rewards that the arena doesn't have.

The loot environments (`GoldEverywhere`, `PureDream`…) only place their
breakable stones when `isNewInstance`. A runtime add is new, so they
work.

#### Break 4: the main-modifier slot

Two consequences of "one main modifier per node":

- **Environment vs hunter zone.** From wave 101 most waves are hunter
  zones (`RoomMod_Hunted`, also main). Adding an environment would
  remove it. Instead the *rule* decides: on an environment wave the
  schedule sets the hunt level to 0
  (`int huntLevel = environment ? NoHuntLevel : HuntLevel(...)`), so the
  two never collide, and a Pain wave never gets an environment.
- **Stale ids and the panel.** When the game removes a main modifier
  because another was added, the mod's record of the first one's id is
  stale. Calling `RemoveModifier` with it reaches
  `RoomModifiers.HandleRuntimeRemoval`, which logs `Runtime removal
  failed; Modifier instance with id N not found`. So `ArenaModifier.Remove`
  checks the node's data first:

```csharp
if (NetworkServer.active && zones != null && node >= 0 && node < zones.nodes.Count && IsOnNode(zones, node, id))
{
	try { zones.RemoveModifier(node, id); }
	catch (Exception e) { Log.Error(LogText.ModifierRemoveFailed(_type), e); }
}
```

  The modifier panel (`UI_InGame_RoomModDisplay`) is client-local: it
  shows the newest main modifier on `ClientEvent_OnActorAdd` and is hidden
  only on `ClientEvent_OnRoomLoadStarted`. The arena never loads a new
  room, so vanilla clients kept showing an environment long after it
  ended. The host can't hide a client's panel, but a new main modifier
  always replaces it. So whenever none of the mod's own main modifiers is
  active, it keeps a harmless one with suitable text on the node
  (`RoomMod_FragmentOfRadiance_MysteriousPlace`, "Mysterious Space"),
  added through the same `ArenaModifier`. Replace instead of hide.

#### A related break in the same family: a singleton that isn't there

The hunter zone goes through the same `ArenaModifier`. Its artillery
routines died at once with a `NullReferenceException` in `Player.log`:

```csharp
if (endWhenRiftOpen && Rift.instance.isOpen && !Rift.instance.isLocked)
{
	yield break;
}
```

`Rift.instance` is null in the arena, and `endWhenRiftOpen` is 1 on the
prefab. It's a plain public field, so the host can set it per instance
before the actor starts, through the `beforePrepare` callback that
`AddModifier` forwards:

```csharp
_modifier.Add(nameof(RoomMod_Hunted), KeepArtilleryWithoutRift);
...
hunted.endWhenRiftOpen = false;
```

The routines run on the server only, so the client's prefab value doesn't
matter. The lesson for step 2: list the static singletons a type reads
(`Rift.instance`, `Room.instance`), then ask whether each exists in your
context.

### B5. Survive

| Event | Risk | Handling |
|---|---|---|
| Game over during an environment wave | `RemoveModifier` writes the node's `SyncList` after the transport shut down: `InitSyncObject: IsWritable: neither NetworkServer nor NetworkClient are active`. Seen in play; it aborted the session reset halfway | `Remove()` clears its own state **first**, then calls the game only while `NetworkServer.active` |
| Another main modifier takes the slot mid-wave (a Pain group's aura replaces the hunter zone) | the first one is gone from the node | when the Pain group falls, `ArenaModifier.Restore()` re-adds the hunter zone, but only if its id is no longer on the node |
| Mod reload | the new assembly has fresh statics | the old instance's `OnDestroy` → `Session.Reset()` → `WaveDirector.Reset()` ends the environment and closes `RoomCombat`; the new instance resumes the wave from the AppDomain slot and `ResumePlan` → `BeginEnvironment` starts a fresh one |
| Late join | modifier actor and its status effects are networked | nothing to do for the gameplay |
| Exception while adding | half-added modifier, room held in combat | `Begin` catches, calls `Remove()` and `RestoreRoom()`, logs `environment: adding X failed; this wave has none.` |

### B6. Measure and document

Log lines: `environment: wave 16 brings RoomMod_FireDevil; the arena
counts as in combat until it's cleared.` and `environment:
RoomMod_FireDevil removed at the end of wave 16.` The checks: one for
"each environment shows on host and client and is gone next wave", one
for "hazards actually fire, no monsters removed by the clear routine, the
mid-run save still happens after the wave", one for monster
environments, one for the hunter zone interaction. The doc lists every
environment, which are off and why, and the `didClearRoom` readers.

**Where else this applies.** Any content type that "only works in a
normal room": scan for the room-state members it reads (`didClearRoom`,
`isRevisit`, `Rift.instance`, `rewards`) before reusing it elsewhere. Any
runtime `AddModifier` (Corrupted Chaos for the Pain aura, the hunter zone,
the default panel). Any state the game mutates and never restores
(`OverrideMonsterType`): save before, restore after.

---

## Part 4. Worked example C (short): a rule, a watch, and additive stats

**Goal.** If players kill nearly everything instantly, every later
monster gets ×2 health and damage, stacking. Concretely: if 80% of a
wave's monsters die within 0.5 s of the first hit they take.

### C1. The rule is plain math

```csharp
public bool WatchesWave(int wave) => IsOn && wave >= FromWave;
public int KillsNeeded(int monsters) => Math.Max(1, (int)Math.Ceiling(monsters * KillShare));
public double MultiplierFor(int stacks) => IsOn ? Math.Pow(StatMultiplier, stacks) : NoChange;
```

No Unity, no networking, so it can be read and reasoned about alone.

### C2. The watch hangs off events, not a patch

Deaths come from `entity.EntityEvent_OnDeath` and first hits from
`entity.EntityEvent_OnTakeDamage`, both subscribed per monster when the
mod spawns it (`EnemyRegistry`), so only the mod's own wave monsters
count. Reading `Actor` showed the order on a lethal hit: the damage event
fires first, then `target.Kill()`, so a one-shot monster records its first
hit in the same frame and reports 0 s. The watch is a small state machine
(`Start`, `OnKill(secondsSinceFirstHit)`, `Cancel`, `Reset(stacks)`) that
the wave director feeds. The skip command cancels it, so a cheat can't
trigger it.

### C2b. The first metric measured the wrong thing

The first version counted kills within 10 s of the **wave's start**. In
play it almost never fired, even for teams that deleted everything. The
reason was in the mod's own numbers: spawning releases 6 monsters every
0.5 s, so at most about 120 exist after 10 s, and the walk in from the
spawn point eats into the window too. On any wave much larger than that,
80% can't even be alive in time. The metric measured the spawn rate, not
the players.

The method: before trusting a threshold, list every factor that can hold
the measured number down, and check that only the thing you mean to
measure is left. Time from a monster's first hit to its death depends on
nothing but how hard it's being hit. It also corrects itself: each ×2
makes monsters live longer, until waves stop counting. And when a watched
wave misses, the log says by how much (`K of D death(s) within 0.5 s of
the first hit; X needed`), so the threshold can be tuned from data.

### C3. The stat math has to match how the game combines bonuses

Before writing `AddStatBonus(new StatBonus { maxHealthPercentage = 100 })`
for ×2, check how the game combines it with the bonuses the monster already
has. `EntityStatus.CalculateStats` folds every granted bonus with `+`, then
multiplies once:

```csharp
BonusStats @default = BonusStats.Default;
for (int i = 0; i < _grantedStatBonuses.Count; i++)
{
	StatBonus statBonus = _grantedStatBonuses[i];
	@default += statBonus;
	...
}
NetworkbonusStats = @default;
...
data.maxHealth *= 1f + bonusStats.maxHealthPercentage / 100f;
```

That `+` looks like a sum, and it's easy to stop reading there and conclude
that +100% on top of a wave's +500% gives (1 + 6) / (1 + 5) ≈ ×1.17. Last
Stand's docs said exactly that for a while. But `@default` is a
`BonusStats`, and `BonusStats` overloads `operator +`:

```csharp
result.maxHealthFlat = x.maxHealthFlat + y.maxHealthFlat;
result.maxHealthPercentage = DewMath.MultiplyPercentageBonuses(x.maxHealthPercentage, y.maxHealthPercentage);
...
public static float MultiplyPercentageBonuses(float a, float b)
{
	return ((1f + a * 0.01f) * (1f + b * 0.01f) - 1f) * 100f;
}
```

Flat values add; percentages compound. A separate +100% bonus is exactly
×2 on top of everything else. Last Stand raises the wave bonus itself
instead, which gives the same total in one bonus:

```csharp
public static double Raise(double bonusPercent, double multiplier)
{
	return (Tuning.PercentScale + bonusPercent) * multiplier - Tuning.PercentScale;
}
```

Wave 21's +200% (3× base) with one stack becomes +500% (6× base), exactly
double. The method: when a game type is combined with `+`, `*` or `==`,
open the type and look for an operator overload before you assume the
arithmetic (step 2: read every input, here the operator itself). The same
mistake once made a health floor overshoot by the boss's whole existing
bonus, because its formula divided out a sum that was really a product.

### C4. Survive and measure

The stack count goes into the same AppDomain slot as the wave number
(`ProgressStore`), so a reload logs `progress restored: … N fast clear(s)
so far` and keeps the multiplier. Every `wave N:` log line carries
`fastClear=xM heroicFastClear=xK`, and the trigger logs
`fast clear: wave W had K of M monster(s) die within 0.5 s of their first hit; …`.

---

## Part 5. Field guide

For each kind of feature: where to start searching, where the technique
is explained, and the trap that usually bites first.

### Overview

| You want to add… | Search first | Technique chapter | Usual trap |
|---|---|---|---|
| Monster behavior or strength | `Monster`, `EntityAI`, `RoomMonsters`, `StatBonus` | 06 | percentage bonuses compound through an overloaded `+` |
| A reward or drop | `PickupManager`, `LootManager`, `Dew.Create*` | 06 | server-only APIs; exclusive items; stardust only at vanilla rates |
| A shrine or interactable | `Shrine_*`, `RoomMod_Spawn*`, `EditSkillShrine` | 04, 05, 06 | per-player limit isn't synced |
| HUD information | `DewQuest`, `UI_InGame_*` | 04, 08 | most UI is built locally by each client |
| A chat feature | `ChatManager` | 08 | client chat reaches the server through a Mirror command, not a public method |
| A setting | `ModBehaviour.LoadConfigsToDisk`, `DewGUI.fieldBuilders` | 08 | saved JSON overrides new defaults |
| A game mode entry | `UI_Title_*`, `GameSettingsManager` | 07, 08 | a flag that doesn't survive a restart |
| Price changes | `GameManager.GetAdjustedGoldAmount_*`, `Shrine.GetCost` | 04 | clients compute the price themselves |
| Difficulty scaling | `GameManager.SetAmbientLevel`, `currentZoneIndex`, `DewDifficultySettings` | 06 | zone index drives prices and rewards too |
| A console command | `ModBehaviour.ConsoleCommandAttribute` | 08 | runs on whoever types it, host or not |

### New monster behavior or strength

- **Search:** `RoomMonsters.SpawnMonsters(SpawnMonsterSettings)` (with
  `spawnPosGetter`, `afterSpawn`), `RoomMonsters.SpawnMiniBoss`,
  `RoomMonsters.onAfterSpawn`, `AI.Aggro(Entity)`, `Entity.CreateStatusEffect<T>`,
  `EntityStatus.AddStatBonus`, `Se_HunterBuff`, `MirageSkinEffect`
  subclasses.
- **Read:** the monster type's `OnCreate` and `OnDeath` overrides; boss
  types spawn helpers with themselves as parent.
- **Traps:** percentage bonuses compound through `BonusStats operator +` (example C). An `isBossSpawn` rule forces one
  spawn with no hunter buff. `SpawnMiniBoss` always calls `SelectMiniBoss`,
  which throws when the room's default pool has no miniboss type, even if
  you pass the entity. `BossMonster.OnDeath` pauses game time and runs the
  boss-soul flow unless `skipBossSoulFlow`.

### New reward or drop

- **Search:** `PickupManager.DropGold(isKillGold, isGivenByOtherPlayer,
  amount, position, target)`, `DropDreamDust(...)`,
  `Dew.CreateGem(prefab, pos, quality, tempOwner)`,
  `Dew.CreateSkillTrigger(prefab, pos, level, tempOwner)`,
  `LootManager.SelectGemRarity` / `SelectGemAndQuality` /
  `SelectSkillRarity` / `SelectSkillAndLevel`.
- **Read:** `PickupManager.HandleDeathDrop` for how kill gold is split and
  multiplied, `Shrine_Hatred_Reward.OnUse` for a weighted reward roll.
- **Traps:** all server-only. `tempOwner` reserves the item and hides it
  from everyone else's camera; a leaving player's reserved items are
  destroyed. Never grant exclusive items that can't be bought with
  stardust (modding terms), and keep stardust at vanilla rates. Gold orbs split by player count, so "give N" needs the math.

### New shrine or interactable

- **Search:** visible name → localization → `Shrine_*`; its `RoomMod_Spawn*`
  spawner; `Shrine`, `EditSkillShrine`, `ICustomInteractable`.
- **Read:** `CanInteract`, `OnInteract`, `OnUse`, `OnActivateEditSkill`,
  `DoPostUseRoutines`, `GetCost`, `UpdateIsAvailable`.
- **Traps:** `maxUseCountPerPlayer` and `useCooldown` are prefab fields;
  rewrite the synced `_useCountByPlayer` and `_lastUseTime` instead
  (example A). `isAvailable` is recomputed after every use. Reuse an
  existing shrine after a reload (`FindObjectsByType<T>`) instead of
  creating a second one.

### New HUD information

- **Search:** `DewQuest` (`currentProgress` is a synced string shown as-is),
  `SetLocalizedTitle` / `SetLocalizedDescription` (ClientRpcs by key),
  `UI_InGame_*` for what each element reads.
- **Read:** the UI element's refresh trigger and inputs. A label is only
  changeable if every input is synced *and* the client redraws when it
  changes.
- **Traps:** RPCs aren't replayed to late joiners, so re-send titles to
  each newly ready connection. Unknown keys render as `ui.!<key>`. Client
  UI (`UI_InGame_Location`, the modifier panel) can't be changed directly;
  only through synced state it already reads.

### New chat feature

- **Search:** `ChatManager.BroadcastMessage(new ChatManager.Message { type =
  MessageType.Raw, content = … })` to send;
  `ChatManager.UserCode_CmdSendChatMessage__String__NetworkConnectionToClient`
  to receive (the server-side body of the client's command).
- **Read:** the `MessageType` enum; `Raw` renders as-is (rich-text color
  tags included) on vanilla clients.
- **Traps:** the receive hook is a protected, Mirror-generated `UserCode_`
  method, so it is patched by its generated name (keep it in one named
  constant). Its `sender` parameter tells you who typed it; check that
  you're the host before acting.

### New setting

- **Search:** `ModBehaviour.LoadConfigsToDisk`, `GetModConfigFilePath`,
  `DewGUI.CreateWidgetsForObject`, `DewGUI.fieldBuilders`.
- **Read:** how missing keys keep initializer values, and how
  `[HideInInspector]` hides a field.
- **Traps:** an existing config file keeps old defaults forever; version
  the file and reset outdated ones. Validate once into a snapshot rather
  than reading raw fields everywhere.

### New game mode entry

- **Search:** `UI_Title_TitleView.EnterSingleplayerOptions`,
  `UI_Title_CreateLobbyView` (`OnShow`, private `_state`),
  `DewNetworkStartSettings.customGameSettingsSaveKey`,
  `GameSettingsManager.gameSettingsSaveKey`.
- **Read:** `DewNetworkManager.RestartAsync`, which copies the key back, so
  a restart stays in the mode.
- **Traps:** a mode flag in a static field dies with a reload and isn't
  visible after restart; carry it in a synced field the game already
  round-trips. Don't bypass the cross-play block for modded lobbies.

### Price changes

- **Search:** `GameManager.goldCostMultiplier` (SyncVar),
  `GetAdjustedGoldAmount_Cost`, `GetAdjustedGoldAmount_Cost_Service`,
  each shrine's `GetCost`.
- **Read:** whether the price formula's inputs are synced. Clients compute
  the tooltip price and a local `CanAfford` from their own copy.
- **Traps:** a host-only multiplier (`priceMultipliersByPlayer`,
  `[NonSerialized]` and not synced) makes the tooltip lie. Some prices
  (the Altar of Cleansing's `GameManager.GetCleanseGoldCost`) don't use the
  synced multiplier at all. JSON overrides reach clients but apply to every run
  and only to allowed field types.

### Difficulty scaling

- **Search:** `GameManager.SetAmbientLevel` (monster level),
  `ZoneManager.currentZoneIndex` (curves everywhere),
  `DewDifficultySettings.ApplyDifficultyModifiers`, `StatBonus`.
- **Read:** every use of `currentZoneIndex` before changing it (the IL scan
  counts them; prices, rewards, XP and the location label all read it).
- **Traps:** the ambient level is reset to `currentZoneIndex + 1` on zone
  change; additive bonuses (example C); `abilityHasteFlat` only shortens
  cooldowns whose config has `canReceiveCooldownReduction`.

### New console command

- **Search:** `ModBehaviour.ConsoleCommandAttribute(description,
  customName)`; any public method on a `ModBehaviour` carrying it is
  registered.
- **Read:** `ModBehaviour.cs`, where the attribute is declared (nested in
  `ModBehaviour`, so subclasses write `[ConsoleCommand(...)]`).
- **Traps:** a command runs on the machine that typed it. Refuse when not
  the host (`NetworkServer.active` plus your mode check) and log why.
  Removing the attribute is the way to keep a command in code but
  unregistered.

---

## Summary of the method

1. Find the precedent: visible name → `strings -el resources.assets` →
   key → type; type → `ilspycmd -t`; mechanic → manager; member → IL scan.
2. Read the lifecycle and list every input, including static singletons.
3. Classify each input (synced, prefab, host-only, client-local) and
   write down what a vanilla client will compute.
4. Hook the funnel, on the server, once per event; prefer events.
5. Write rule, owner, adapter, thin patch; config, text and names in
   their single places.
6. Walk reload, revisit, game over, late join, unload, setting change.
7. Write the log line and the in-game check before testing.
8. Write down why, including what the first version got wrong.
