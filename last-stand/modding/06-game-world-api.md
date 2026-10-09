# 06. The game's object model and how to drive it

This chapter maps the runtime objects of Shape of Dreams (managers, actors,
rooms, nodes, shrines, quests) and shows the calls that create, change and
watch them from a host-side mod. It then teaches the one method that found
almost every API used in Last Stand: **to do X, find where the game already
does X and call the same thing**. The last section lists the traps that cost
the most time.

Everything below was checked against the decompiled `Dew.Core.dll` and, for
content types (shrines, room modifiers, bosses), single types decompiled from
`Dew.Contents.dll`. How to get and search that decompile is in
`02-reading-the-game.md`. Paths like `Dew.Core/ZoneManager.cs` refer to that
output, one `.cs` file per type. What reaches vanilla clients and why is in
`04-host-only-networking.md`. This chapter only points at it.

---

## 1. The object model

### 1.1 Managers: global services with a static accessor

Most game systems are singletons that derive from one of two generic bases:

| Base | Networked | Examples |
|---|---|---|
| `NetworkedManagerBase<T> : DewNetworkBehaviour` | yes (Mirror `NetworkBehaviour`) | `GameManager`, `ZoneManager`, `ActorManager`, `PickupManager`, `LootManager`, `ChatManager`, `QuestManager`, `GameSettingsManager`, `ClientEventManager`, `ConsoleManager` |
| `ManagerBase<T> : LogicBehaviour` | no, local to each game process | `MusicManager`, `CameraManager`, `SpawnManager`, `TransitionManager`, `EditSkillManager`, `MessageManager`, `LobbyManager`, `TitleManager` |

The networked managers exist on the host and on every client, with the same
SyncVars. A `ManagerBase<T>` lives only in the local process, so a host-only
mod can't change it for anyone else. `MusicManager` is the textbook case: each
client picks its own track, so music can't be driven from the host (see
`04-host-only-networking.md`). There is no type called `NetworkedManagers`.
"Networked managers" just means the `NetworkedManagerBase<T>` subclasses.

Both bases expose two static accessors. The difference matters
(`Dew.Core/NetworkedManagerBase.cs`):

```csharp
public static T instance
{
    get
    {
        if (softInstance == null)
            softInstance = Object.FindObjectOfType<T>();
        if (softInstance == null)
            softInstance = Object.FindObjectOfType<T>(includeInactive: true);
        return softInstance;
    }
}

public static T softInstance { get; private set; }
```

- `softInstance` is a cached field set in `Awake`/`OnEnable`. It costs nothing
  and returns `null` when the manager doesn't exist (title screen, lobby,
  between scenes).
- `instance` falls back to **two full scene scans** when the manager is
  missing. Calling it every frame while out of a game scans the scene twice
  per frame. Use `softInstance` in anything that runs often, and treat `null`
  as "not now, try later".

`SingletonDewNetworkBehaviour<T>` (used by `Room`) and `SingletonBehaviour<T>`
(used by scene markers such as `Room_BossArena`) follow the same pattern.
Some actors have their own accessor: `Rift.instance` goes through
`Dew.Helper_GetInstanceOfActor`, which walks `ActorManager.allActors` and then
calls `FindAnyObjectByType` whenever the cached rift is gone. In a room with
no rift, every call pays for that search and still returns `null`.

**Lifecycle hook for mods.** `ModBehaviour` has `CallOnNetworkedManager<T>`
and `CallOnManager<T>` (`Dew.Core/ModBehaviour.cs`). They register a callback
that runs at once if the manager exists, and again every time one starts.
`NetworkedManagerBase<T>.OnStartClient` calls
`DewMod.NotifyManagerLifecycle(GetType(), enable: true)`, and `OnStopClient`
calls it with `false`. The `Action onStart, Action onCleanup` overload and the
`Func<T, Action>` overload give a cleanup that runs when the manager stops.
Last Stand resets its whole session per run this way:

```csharp
CallOnNetworkedManager<GameManager>(OnGameManagerStarted);

private void OnGameManagerStarted(GameManager game)
{
    Session?.Reset();
    Session?.UseSettings(SessionSettings.From(config));
}
```

### 1.2 Actors: everything that lives in the run

Almost every gameplay object is an `Actor` (`Actor : DewNetworkBehaviour`).
The hierarchy, from the decompiled class declarations:

```
Actor
├── Entity                      units with Status/Control/AI/Visual
│   ├── Monster                 type: Lesser | Normal | MiniBoss | Boss
│   │   └── BossMonster         "heroic bosses" (Mon_*_Boss* in Dew.Contents)
│   ├── Hero                    a player's character
│   └── Summon
├── AbilityInstance             ability effects (Ai_*)
│   ├── StatusEffect            buffs and debuffs (Se_*)
│   │   ├── MiniBossEffect
│   │   └── MirageSkinEffect
│   └── PickupInstance          gold, dream dust and exp orbs
├── AbilityTrigger
│   └── SkillTrigger            a memory (skill item)
├── Gem                         an essence
├── Shrine (abstract)           every interactable shrine
│   └── EditSkillShrine         shrines that open the item edit window
├── Rift → Rift_RoomExit        the exit portal
├── RoomModifierBase            room modifiers (RoomMod_*)
├── GameEffect
│   ├── DewQuest                quests in the tracker (Quest_*)
│   └── GameModifierBase        run modifiers (GameMod_*)
└── ServerActor                 a neutral parent for server-made things
```

`ActorManager` indexes the live ones: `allActors` (`HashSet<Actor>`),
`allEntities` (`HashSet<Entity>`), `allHeroes` (`List<Hero>`) and
`serverActor`. Useful members on every actor:

| Member | Meaning |
|---|---|
| `isActive` | true from `OnStartServer` until destruction starts. Check it before using a reference you kept. |
| `isNewInstance` | false when the actor was restored from a room save (a revisit) |
| `parentActor`, `firstEntity` | who created it; the nearest `Entity` up the chain |
| `IsDescendantOf(Actor)` | walks `parentActor` (Last Stand uses it to find a boss's pillars) |
| `persistentData` | `Dictionary<string,string>` saved with the room |
| `Destroy()` | server only; on a client it logs a warning and does nothing |

Extension helpers: `actor.IsNullOrInactive()` (`ActorCheck.cs`) and
`entity.IsNullInactiveDeadOrKnockedOut()` (`EntityCheck.cs`). Use them on
every stored reference.

**Actors are pooled.** `SpawnManager.Create` reuses instances, and
`DewPool.ClearEventsAndProcessors` calls `ClearPooledEventsAndProcessors` on
an actor that goes back to the pool, which empties its `EntityEvent_*`
handlers. Two consequences:

- Handlers you add to an entity never leak into its next life.
- A C# reference you kept can later point at a reused object. The game even
  warns about it in `Dew.InstantiateActor_Imp`: *"You must not store actors
  for later use; instead fetch them from the database whenever they're
  needed"*. Keep references only while `isActive`, and look prefabs up
  through `DewResources` each time.

### 1.3 Entities: Status, Control, AI, Visual

An `Entity` is split into components exposed as properties:

| Property | Type | What you use it for |
|---|---|---|
| `Status` | `EntityStatus` | `level`, health, `AddStatBonus`, `HasStatusEffect<T>`, `TryGetStatusEffect<T>`, `SetHealth` |
| `Control` | `EntityControl` | `MoveToDestination(dest, immediately)`, `Teleport(pos)`, `agentPosition` |
| `AI` | `EntityAI` | `Aggro(target, doPropagation)`, `disableAI` |
| `Visual` | `EntityVisual` | `NetworkskipSpawning` (skip the spawn animation) |

Plus `owner` (a `DewPlayer`) and `agentPosition` (the navmesh position).
Monsters belong to `DewPlayer.creep`. `Monster.type` is a synced
`MonsterType` (`Lesser`, `Normal`, `MiniBoss`, `Boss`). `Monster` also has
`populationCost`, `disableLoot` and `isHunter`. `Hero` has `isKnockedOut` and
`maxLevel`.

`EntityAI.Aggro(Entity target, bool doPropagation = false)` forces a target.
It refuses (with a warning) unless `entity.CheckEnemyOrNeutral(target)`, and
it does nothing when `CanAggroTo` is false. With `doPropagation` it also
alerts allies within `AggroPropagationRange` (17 m).

### 1.4 Status effects (`Se_*`)

A status effect is an actor attached to a `victim`. Create one from any actor
with `actor.CreateStatusEffect<T>(victim, new CastInfo(actor))` or the
`Type` overload `CreateStatusEffect(Type, victim, info, beforePrepare)`. Query
with `victim.Status.HasStatusEffect<T>()` and `TryGetStatusEffect<T>(out T)`.
`Dew.CreateStatusEffect` returns `null` without creating anything when the
prefab is `isKilledByCrowdControlImmunity` and the victim is unstoppable or
invulnerable.

Last Stand makes a monster a hunter with exactly what `RoomMonsters` does for
random hunters:

```csharp
if (!entity.Status.HasStatusEffect<Se_HunterBuff>())
{
    entity.CreateStatusEffect<Se_HunterBuff>(entity, new CastInfo(entity));
}
```

### 1.5 Players: `DewPlayer`

| Member | Meaning |
|---|---|
| `DewPlayer.gamePlayers` | static `List<DewPlayer>` of players in the `Playing` state, sorted by `netId` |
| `DewPlayer.local` | this process's player (the host's, on the host) |
| `DewPlayer.creep`, `DewPlayer.environment` | non-human owners. `isHumanPlayer` is false for both. |
| `hero` | the player's `Hero` (null before spawn) |
| `guid` | stable id; keys `Shrine._useCountByPlayer` and `RoomMod_HeroSoul.clientData` |
| `gold`, `dreamDust` | synced currency |
| `EarnGold(int)`, `EarnDreamDust(int)` | `[Server]`: add currency and play the earn RPC |

`[Server]` is a Mirror attribute. The weaver puts a guard at the top of the
method, so a client call logs `[Server] function '...' called when server was
not active` and returns a default value. It doesn't throw. The same guard is
on `Entity.Kill`, `EntityStatus.AddStatBonus`, `PickupManager.DropGold`,
`QuestManager.StartQuest`, `GameManager.SetAmbientLevel` and most other
mutators. The warning in `Player.log` is the clue that your code ran on the
wrong side.

### 1.6 The room

`Room : SingletonDewNetworkBehaviour<Room>` is the scene's root. Its
components are properties:

| Member | Type | Role |
|---|---|---|
| `monsters` | `RoomMonsters : RoomComponent` | spawning: `SpawnMonsters`, `SpawnMiniBoss`, `defaultRule`, `onBeforeSpawn`/`onAfterSpawn`, `addedHunterChance` |
| `modifiers` | `RoomModifiers` | live `RoomModifierBase` instances (`isRoomActive`) |
| `rewards` | `RoomRewards` | `DropChaosReward(pos, isHighQuality)` |
| `props` | `RoomProps` | `TryGetGoodNodePosition(out pos, random)` |
| `rifts` | `RoomRifts` | `OpenRifts()` |
| `sections` | `IList<RoomSection>` | the room's areas, filled in `StartRoom` |
| `isActive` | bool | set in `StartRoom` |
| `isRevisit` | SyncVar | the room was restored from a save |
| `didClearRoom` | bool, private setter, not synced | combat over |
| `onRoomClear` | `UnityEvent` | fired once by `ClearRoom` |

A `RoomSection` has `area`, `pathablePivot` (a ground point every spawn path
is checked against) and `monsters` (a `Section_Monsters` with
`GetSpawnPositionInSection`). `Room.StartRoom` sets `didClearRoom = isRevisit`.
That one line explains a lot of behavior in Section 5.

### 1.7 The world: `ZoneManager` and nodes

The world map is `ZoneManager.nodes`, a Mirror `SyncList<WorldNodeData>`.
`WorldNodeData` is a **struct** with `type` (`WorldNodeType`), `status`,
`room`, `roomOverride`, `modifiers` (`List<ModifierData>`) and `position`.
Other members:

| Member | Synced | Meaning |
|---|---|---|
| `currentNodeIndex`, `currentNode` | SyncVar | where the party is |
| `currentZoneIndex`, `currentZone` | SyncVar | world number (0-based) and `Zone` asset |
| `currentHuntLevel` | SyncVar (hook `OnCurrentHuntLevelChanged`) | hunter pressure |
| `isInRoomTransition`, `isInAnyTransition` | SyncVar | loading |
| `visitedNodesSaveData` | host only | `List<DewPersistence.RoomData>`, one per node |
| `ClientEvent_OnRoomLoaded`, `ClientEvent_OnRoomLoadStarted` | RPC-driven | room events |
| `onWorldGenerated` | `SafeAction` | after world gen |

Because the element is a struct, the only way to change a node is
copy, modify, write back. Writing back is what makes the `SyncList` send it.
`ZoneManager.RemoveModifier` shows the idiom:

```csharp
WorldNodeData value = nodes[nodeIndex];
value.modifiers = new List<ModifierData>(value.modifiers);
int num = value.modifiers.FindIndex((ModifierData m) => m.id == modifierId);
if (num >= 0)
    value.modifiers.RemoveAt(num);
nodes[nodeIndex] = value;
```

The fresh `List` copy matters too: the old struct and the new one would
otherwise share one list.

**Room saves.** On leaving a node, the travel routine stores
`DewPersistence.SerializeRoomData()` in `visitedNodesSaveData[from]`. On
entering, a non-null entry sets `Room.isRevisit = true` and restores the
room's actors from it. Restored actors run `OnCreate` again with
`isNewInstance == false`. `ZoneManager.SetRoomOverride` clears the node's
entry.

**Modifiers.** `AddModifier(int nodeIndex, ModifierData mod, Action<RoomModifierBase> beforePrepare = null)`
(plus generic `AddModifier<T>(nodeIndex, beforePrepare)` and
`AddModifier<T>(nodeIndex, clientData, beforePrepare)`) does three things in
order (`Dew.Core/ZoneManager.cs`):

1. If the new modifier's prefab `isMain`, it removes the node's existing main
   modifier first. **One main modifier per node.**
2. It writes the `ModifierData` into the node (synced).
3. If `nodeIndex == currentNodeIndex` and the room's `modifiers.isRoomActive`,
   it calls `RoomModifiers.HandleRuntimeAddition`, which creates the modifier
   actor **now**, with your `beforePrepare`. Otherwise it instantiates the
   prefab off-scene, runs `beforePrepare`, serializes it into
   `modifierServerData[id].persistentData` and destroys it. The real actor
   appears on the next load of that node.

It returns the id. Pass that id to `RemoveModifier(nodeIndex, id)`, which
destroys the live instance (`HandleRuntimeRemoval`) and removes the data.

### 1.8 Shrines, items, quests, rifts

- **`Shrine`** (abstract). Plain fields `maxUseCountPerPlayer`, `useCooldown`
  (not synced). SyncVar `maxTotalUseCount` (set through
  `NetworkmaxTotalUseCount`). Per-player counts in a private
  `SyncDictionary<string,int> _useCountByPlayer`. Methods `ResetUseCounts()`,
  `GetUseCountByPlayer(player)`, `MakeAvailable()`, `CanInteract(entity)`,
  `OnInteract(entity, alt)`, and `DoPostUseRoutines(entity)`, which every
  successful use ends in (count, effect, `Spend(cost)`, `_lastUseTime`,
  `RpcInvokeOnSuccessfulUse` → `ClientEvent_OnSuccessfulUse`).
  `EditSkillShrine` adds the item edit window (Dream Well, Ascension, Maw of
  Doom, Altar of Cleansing).
- **`SkillTrigger`** (memory) and **`Gem`** (essence) on the ground. Both
  have a synced `tempOwner` (`DewPlayer`) and `IsLockedFor(player)`. A
  reserved item is hidden from and locked for everyone else while its owner
  is up.
- **`DewQuest`** (a `GameEffect`). Synced `progressType` and
  `currentProgress` (string). Local title and description, set by key through
  the `SetLocalizedTitle`/`SetLocalizedDescription` ClientRpcs.
  `QuestManager.StartQuest<T>(beforePrepare)` is `Dew.CreateActor` under the
  hood. `TryGetQuest<T>(out T)` searches `activeQuests`.
- **`Rift`** / **`Rift_RoomExit`**: `isOpen`, `isLocked`, and on the exit rift
  the SyncVar `nextNodeIndex` (set through `NetworknextNodeIndex`).

---

## 2. Creating things

### 2.1 `Dew.CreateActor` and why `beforePrepare` exists

```csharp
public static T CreateActor<T>(Vector3 position, Quaternion? rotation, Actor parent = null, Action<T> beforePrepare = null) where T : Actor
public static T CreateActor<T>(T prefab, Vector3 position, Quaternion? rotation, Actor parent = null, Action<T> beforePrepare = null) where T : Actor
```

The generic form looks the prefab up with `DewResources.GetByType<T>()`.
`InstantiateActor_Imp` throws `"You can only create actors on server"` when
`NetworkServer.active` is false. A `null` rotation means the camera's entity
angle. Then `PrepareAndSpawnActor_Imp` runs:

```csharp
try { beforePrepare?.Invoke(newActor); } catch (Exception e) { Debug.LogException(e); }
newActor.PrepareAndSpawn();
```

and `Actor.PrepareAndSpawn` → `InvokeOnPrepareIfDidnt` (raises
`ActorManager.onActorBeforePrepare`, then `OnPrepare()`) →
`NetworkServer.Spawn` → `OnStartServer`. `OnCreate` runs from
`InvokeOnCreateIfDidnt`, which `OnStartClient` calls, and it ends in
`ActorManager.AddActor`, which raises `ClientEvent_OnActorAdd`,
`ClientEvent_OnEntityAdd` and `ClientEvent_OnHeroAdd`.

So `beforePrepare` is **the only hook that runs before everything**: before
`OnPrepare`, before the object is spawned on the network, before
`OnStartServer` and before `OnCreate`. Use it for three things:

1. **Fields that `OnStartServer`/`OnCreate` read.** `RoomMod_Hunted`'s
   artillery routines test `endWhenRiftOpen`. Clearing it afterward is too
   late, because the coroutines have already started.
2. **SyncVars you want in the initial spawn message.** Set before
   `NetworkServer.Spawn`, they are part of the spawn payload, so late joiners
   and the first frame agree. Setting them after works but sends a second
   update.
3. **Per-instance tuning** of a prefab you don't own (use limits on a shrine,
   `skipBossSoulFlow` on a boss).

Exceptions inside `beforePrepare` are caught and logged, and the actor is
still spawned. A failed configure therefore leaves a half-configured actor,
not a missing one.

**Host timing trap.** `OnStartServer` runs inside `NetworkServer.Spawn`, but
`OnCreate` doesn't. On the host, the spawn message for the host's own client
goes through Mirror's `LocalConnectionToClient.Send`, which only enqueues it.
`LocalConnectionToServer.Update`, called from `NetworkClient.NetworkLateUpdate`,
processes it later (`OnHostClientSpawn` → `OnStartClient` → `OnCreate`). So
`OnCreate` and the `ActorManager` add events run **after** `CreateActor` has
returned, in Mirror's late update. The game's difficulty scaling hangs on
that event (`GameManager.OnStartServer`):

```csharp
NetworkedManagerBase<ActorManager>.instance.ClientEvent_OnEntityAdd += (Action<Entity>)delegate(Entity e)
{
    difficulty.ApplyDifficultyModifiers(e);
};
```

Anything that must apply on top of the difficulty bonus has to wait.
Last Stand defers its heroic boss multipliers by one frame:

```csharp
Dew.CallDelayed(() =>
{
    MonsterEnhancer.Multiply(entity, health, damage);
    MonsterEnhancer.AddAbilityHaste(entity, haste);
    HeroicBossReport.Watch(entity, wave);
});
```

### 2.2 Entities: `Dew.SpawnEntity`

```csharp
public static T SpawnEntity<T>(T entity, Vector3 position, Quaternion? rotation, Actor parent, DewPlayer owner, int level, Action<T> beforeSpawn = null) where T : Entity
```

It throws `"Provided owner player is null!"` for a null owner, sets
`Status.level`, `parentActor` and `owner`, then runs the same
prepare-and-spawn path. Monsters use `DewPlayer.creep` as the owner and
`ActorManager.serverActor` as the parent. That's what `RoomMonsters` does
(Section 2.5).

### 2.3 Items: memories and essences

```csharp
public static T CreateSkillTrigger<T>(T trigger, Vector3 position, int level, DewPlayer tempOwner = null, Action<T> beforePrepare = null) where T : SkillTrigger
public static T CreateGem<T>(T gem, Vector3 position, int quality, DewPlayer tempOwner = null, Action<T> beforePrepare = null) where T : Gem
```

Both set `level`/`quality` and `tempOwner` before `beforePrepare`. Pick what
to drop from the run's pools with `LootManager`:

```csharp
Rarity rarity = loot.SelectSkillRarity();                         // or SelectSkillRarity(isHigh: true)
loot.SelectSkillAndLevel(rarity, out SkillTrigger skill, out int level);
Dew.CreateSkillTrigger(skill, Dew.GetGoodRewardPosition(pos, scatter), level, recipient);
```

`SelectGemRarity`/`SelectGemAndQuality` are the essence versions. The pools
are `poolSkillsByRarity` and `poolGemsByRarity`
(`Dictionary<Rarity, List<string>>`). A rarity can be empty, so Last Stand
checks the list before selecting (`LootDropper.HasItems`).
`Dew.GetGoodRewardPosition(pivot, randomOffset = 2f)` keeps rewards off other
items and off banned room nodes.

### 2.4 Currency

| Call | Notes |
|---|---|
| `PickupManager.DropGold(bool isKillGold, bool isGivenByOtherPlayer, int amount, Vector3 position, Hero target = null)` | `[Server]`. Splits into 3 orbs over 3 frames (a coroutine on the manager), or 1 orb when the host had low FPS in the last 5 s. Orb size by amount. |
| `PickupManager.DropDreamDust(bool isGivenByOtherPlayer, int amount, Vector3 position, Hero target = null)` | `[Server]`. Splits into orbs of `max(amount / dreamDustMaxCount, dreamDustMinAmount)`. |
| `DewPlayer.EarnGold(int)` / `EarnDreamDust(int)` | Straight to the wallet, no pickup. |

`target` is a `Hero`, not a `DewPlayer`. `PickupManager.DropStarDust(int
amount, Vector3 position)` also exists: it spawns `Shrine_Stardust` orbs,
and each orb's `OnUse` calls `DewPlayer.GiveStardust` for every game player
(a `ClientRpc` that adds to each client's own profile). The vanilla boss soul
drops `ges.stardustBossSoulAmount` (16 in the asset) plus
`difficulty.bossKillBonusStardust` (Normal 2, Hard 4, Nightmare 7) plus 3
per evil lucid dream. Last Stand's heroic bosses drop exactly that
(`BossStardust`), because the boss soul flow is skipped. Keep stardust at
rates the game itself gives (see `01-project-setup.md`, section 8.3).

### 2.5 Monsters through the room's spawner

Spawning through `Room.monsters` keeps every hook that the room's modifiers
and run modifiers attach to `RoomMonsters` (hunter chance, Mirage skins,
`onAfterSpawn` stat changes). Spawning with `Dew.SpawnEntity` directly skips
them.

```csharp
public void SpawnMonsters(SpawnMonsterSettings settings)  // starts a coroutine, stored in ongoingSpawns
public UniTask SpawnMonstersAsync(SpawnMonsterSettings settings)
```

`SpawnMonsterSettings` fields worth knowing: `rule` (`MonsterSpawnRule`),
`section`, `hero`, `spawnPosGetter` (`Func<Vector3>`), `spawnRotGetter`,
`beforeSpawn`/`afterSpawn` (`Action<Entity>`), `onFinish`,
`earlyFinishCondition`, `spawnPopulationMultiplier`,
`ignoreTurnPopMultiplier`, `ignoreCoopPopMultiplier`, `random`.

What `SpawnMonstersRoutine` does, condensed:

1. Waits one frame, then waits out any `isInRoomTransition`.
2. Waits `rule.initialDelay`.
3. Rolls a wave count from `wavesMin..wavesMax` (adjusted by the game).
   **If `rule.isBossSpawn`: one wave, and `hunterChance = 0`.**
4. Per wave, rolls a population budget and pulls monsters from
   `rule.pool.GetMonsters`. For non-boss rules it waits on room population
   (`WaitForPopulationRoutine`) and a 0.1–0.5 s delay between spawns.
   **A boss rule spawns once and stops**, with no population wait.
5. Each spawn goes through `SpawnMonsterImp`:

```csharp
Entity entity = Dew.SpawnEntity(monster, pos, rot,
    NetworkedManagerBase<ActorManager>.instance.serverActor, DewPlayer.creep,
    NetworkedManagerBase<GameManager>.instance.ambientLevel, delegate(Entity e)
    {
        onBeforeSpawn?.Invoke(e);
        s.beforeSpawn?.Invoke(e);
        ...
    });
onAfterSpawn?.Invoke(entity);
s.afterSpawn?.Invoke(entity);
...
entity.Control.MoveToDestination(hero.GetAIPosition(entity), immediately: false);  // nearest live hero
```

So every monster spawns at `GameManager.ambientLevel`, runs the room's
`onAfterSpawn` (where modifiers like `RoomMod_Hunted` add their stat bonus),
then yours, then walks toward the nearest live hero. Exceptions inside are
caught and the spawn returns `null`.

Position comes from `GetSpawnMonsterPosRot`: the monster's own
`spawnPosOverride`, else `s.spawnPosGetter()`, else a point in `s.section`,
else a point near a hero between `rule.spawnMinDistance` and
`spawnMaxDistance`.

**Minibosses.** `SpawnMiniBoss(SpawnMonsterSettings settings, Entity ent = null, StatusEffect se = null)`
builds a one-monster `isBossSpawn` rule, sets `Monster.Networktype = MiniBoss`
in `beforeSpawn`, calls the `ISpawnableAsMiniBoss` hooks, adds the
`MiniBossEffect` and `Se_AutoDetectPresence` in `afterSpawn`, and hooks
`EntityEvent_OnDeath` to call `rewards.DropChaosReward` 1.5 s later unless
`disableMiniBossRewards`. Its **first line** is
`SelectMiniBoss(out var entity, out var effect, settings.random)`, even when
you pass `ent` and `se`. See Section 5.1.

**Ambient level.** `GameManager.ambientLevel` is a SyncVar (the "threat
level" HUD). `[Server] SetAmbientLevel(int)` clamps to at least 1. The game
sets it to 1 at run start and to `currentZoneIndex + 1` on zone change.
Nothing else reacts to the value, so a mod can raise it freely to make new
spawns stronger.

### 2.6 Finding a spot on the floor

| API | Behavior |
|---|---|
| `Section_Monsters.GetSpawnPositionInSection(Func<Vector3,bool> evaluator = null, DewRandom random = null)` | Up to 30 random points in the section. Your evaluator is applied to the first 10 only. The first point that passes `RoomMonsters.FilterSpawnPosition` wins. The 30th is returned even if it fails the filter, with a `Using fallback spawn position` warning. |
| `GetSpawnPositionInSection(float minDist, float maxDist, random)` | evaluator = not closer than `minDist` to any hero and within `maxDist` of at least one |
| `RoomMonsters.FilterSpawnPosition(pos, out filtered, Vector3? pathPivot)` | `internal`: ground raycast from 50 m up, `NavMesh.SamplePosition` within 5 m, complete path from the section's `pathablePivot`. Reach it through `GetSpawnPositionInSection`. |
| `Dew.GetValidAgentDestination_LinearSweep(start, end)` | snaps `start` to the navmesh, then steps from `end` back toward `start` (up to 50 steps) until a point samples within 1 m **and** has a complete path from `start` |
| `Dew.GetValidAgentDestination_Closest(start, end)` | closest valid navmesh point to `end` |
| `Dew.GetPositionOnGround(pos, yDiffRange = 50f)` | drops a point onto ground colliders |
| `Room.props.TryGetGoodNodePosition(out pos, random)` | a free prop node, which the game uses for shrines |

Unity's own `NavMesh.SamplePosition`, `NavMesh.CalculatePath` (check
`path.status == NavMeshPathStatus.PathComplete`) and `NavMesh.Raycast` are
available too. They are what all of the above are built from.

### 2.7 Stats

```csharp
[Server] public StatBonus AddStatBonus(StatBonus bonus)
[Server] public void RemoveStatBonus(StatBonus bonus)
```

`StatBonus` is a class with `…Flat` and `…Percentage` properties:
`attackDamage`, `abilityPower`, `maxHealth`, `maxMana`, `healthRegen`,
`manaRegen`, `attackSpeedPercentage`, `critAmp`, `critChance`,
`abilityHaste`, `tenacity`, `movementSpeedPercentage`, `armor`,
`attackRange`, the four element amps, and more. Keep the returned object if
you want to remove it later.

`AddStatBonus` calls `CalculateStats()` at once. Two details from that method
decide how you write bonuses:

- **Percentages compound.** Bonuses are summed with
  `BonusStats operator +`, which combines percentages with
  `DewMath.MultiplyPercentageBonuses(a, b) = ((1 + a/100)(1 + b/100) − 1) × 100`.
  A `+(M − 1) × 100` bonus is therefore exactly ×M on top of whatever else
  the entity has, difficulty bonus included.
- **The health ratio is kept.** `CalculateStats` stores
  `currentHealth / maxHealth` before recomputing and applies it after
  (`currentHealth = num * data.maxHealth`). A full-health monster stays full
  when you raise its max health.

The game's own percentages are ×100 values, for example
`maxHealthPercentage = num * 100f` in `RoomMod_Hunted`.

---

## 3. Events and timing

### 3.1 `SafeAction`

Most game events are `SafeAction` / `SafeAction<T…>`, not C# events. The
`+`/`-` operators create the object when it's null and add or remove a
handler. `Invoke` catches and logs an exception per handler, so one broken
handler doesn't stop the others. Inside your handler, an exception becomes a
log line, not a crash, and the game carries on. Cast lambdas when the
compiler can't infer the type, as the game does:

```csharp
entity.EntityEvent_OnDeath += (Action<EventInfoKill>)(kill => OnDeath(entity, tier, kill));
```

### 3.2 Naming tells you where it fires

| Prefix | Fires on | Example |
|---|---|---|
| `ClientEvent_*` | every client, host included | `ActorManager.ClientEvent_OnEntityAdd`, `ZoneManager.ClientEvent_OnRoomLoaded` |
| `EntityEvent_*`, `ActorEvent_*` | where the logic runs, which is the server for damage and death | `Entity.EntityEvent_OnDeath`, `Actor.ActorEvent_OnDealDamage`, `ActorEvent_OnKill` |
| `ClientActorEvent_*` | every client | `ClientActorEvent_OnDestroyed`, `ClientActorEvent_OnCreate` |
| lower-case `on*` | local field, usually server-side | `RoomMonsters.onAfterSpawn`, `ZoneManager.onWorldGenerated`, `ActorManager.onActorBeforePrepare` |

A `ClientEvent_*` handler on the host runs **once**, as the host's client.
Guard server work with `NetworkServer.active` or `isServer`.

### 3.3 The events used most

| Event | Raised by | Notes |
|---|---|---|
| `ActorManager.ClientEvent_OnActorAdd / OnEntityAdd / OnHeroAdd` (+ `Remove`) | `AddActor`, from `OnCreate` | Fires for restored actors too. Also `ClientEvent_OnLocalHeroAdd`. |
| `Entity.EntityEvent_OnDeath` (`EventInfoKill`: `actor`, `victim`) | server `Entity.Kill` | Runs after death interrupts (a `DeathInterruptEffect` can cancel the death), **before** `Destroy()`. `kill.actor.firstEntity.owner` is the killer's player. |
| `Entity.OnDeath(EventInfoKill)` (virtual) | `NotifyEntityDeathToClients` ClientRpc | Runs on every client. Overrides check `isServer`. |
| `Actor.ClientActorEvent_OnDestroyed` | destruction | `RoomMonsters` uses it to give population back |
| `RoomMonsters.onBeforeSpawn / onAfterSpawn` | `SpawnMonsterImp` | every spawn through the room |
| `Room.onRoomClear` (`UnityEvent`) | `ClearRoom` routine | once per room; `AddListener`/`RemoveListener` |
| `ZoneManager.ClientEvent_OnRoomLoaded` (`EventInfoLoadRoom`: `fromIndex`, `toIndex`, `isTraveling`, `isLoadingFromSave`, `isSidetrackTransition`) | `InvokeOnRoomLoaded` ClientRpc | end of every load |
| `ZoneManager.ClientEvent_OnRoomLoadStarted` | start of a load | |
| `ZoneManager.onWorldGenerated` | end of world gen | |
| `ClientEventManager.OnHeroKnockedOut / OnHeroRevive` (`Hero`) | knock-out and revive | `Shrine.OnCreate` listens to refresh availability |
| `Shrine.ClientEvent_OnSuccessfulUse` (`Entity`) | `RpcInvokeOnSuccessfulUse` | after `DoPostUseRoutines` |

### 3.4 "Is the game ready?" — `GameManager.CallOnReady`

```csharp
public static void CallOnReady(Action action)   // runs now if ready, else queued
public virtual bool IsLazyCallReady()
```

`IsLazyCallReady` is true when the `GameManager` is enabled, the transition
screen is `Normal`, `NetworkClient` is active and ready, no scene is loading,
`DewPlayer.local.hero` exists and the `ZoneManager` isn't in a transition. The
queue drains in `GameManager`'s logic update, and each call is wrapped in a
try/catch. `RoomMod_Hunted` starts its artillery this way.

The queue is static and cleared when the `GameManager` is destroyed, so a
queued call never runs into the next run. A one-shot "once per run when
everything exists" job can also poll it. Last Stand's starting gold does:

```csharp
GameManager game = NetworkedManagerBase<GameManager>.softInstance;
if (game == null || game == _handled || !game.IsLazyCallReady())
    return;
_handled = game;
```

### 3.5 Delays and coroutines

- `Dew.CallDelayed(Action func, int frameCount = 1)` runs on `Dew.GetCoroutiner()`,
  a `DontDestroyOnLoad` object. **It survives scene loads and your mod's
  unload.** Re-check everything the callback touches (`IsNullOrInactive`,
  `NetworkServer.active`). Last Stand's delayed boss buff starts with
  `if (entity.IsNullInactiveDeadOrKnockedOut()) return;`.
- `StartCoroutine` on your `ModBehaviour` stops when the mod is unloaded,
  which is usually what you want.
- The game runs routines that must outlive an actor on a manager.
  `BossMonster.OnDeath` uses
  `NetworkedManagerBase<ActorManager>.instance.StartCoroutine(RewardRoutine())`
  because the boss is about to be destroyed.
- A throttled tick from `Update` (`if (now < _next) return; _next = now + interval;`)
  is the simplest robust option for "retry until the room is ready". It's
  what Last Stand uses for nearly everything. See `07-mod-architecture.md`.

### 3.6 Where to subscribe and unsubscribe

Copy the game's own pairs:

| Game type | Subscribes in | Unsubscribes in |
|---|---|---|
| `PickupManager` | `OnStartServer`: `ActorManager.ClientEvent_OnEntityAdd += HandleEntityAdd`, which adds `EntityEvent_OnDeath += HandleDeathDrop` per `Monster` | per entity: never (the pool clears it) |
| `RoomMod_Hunted` | `OnStartServer`: `room.monsters.onAfterSpawn += OnAfterSpawn`, and changes `addedHunterChance` | `OnDestroyActor`: the reverse of each |
| `Se_HeroKnockedOut` | `OnCreate` (server): `ZoneManager.ClientEvent_OnRoomLoaded += …` | `OnDestroyActor`: `-=` |
| `GameMod_Limbo` | create: `ClientEvent_OnRoomLoaded`, `onWorldGenerated` | `OnDestroyActor`: `-=` |

For a mod, the equivalent rules:

1. **Long-lived events** (managers): subscribe in a `CallOnNetworkedManager<T>`
   callback and return or pass the cleanup action. Or subscribe in
   `Start`/`OnEnable` and remove in `OnDestroy` with the same delegate
   instance (keep it in a field; a new lambda won't match `-=`).
2. **Per-entity events**: add them when you create or adopt the entity. The
   pool clears them on reuse. Keep a set of entities you've hooked so you
   don't hook twice (`EnemyRegistry.Adopt` checks `_alive.ContainsKey`).
3. **Anything you changed by `+=`/`*=`** (hunter chance, population
   multiplier, gold multiplier): undo it by the inverse operation on the same
   object, and only while that object and the server still exist.

---

## 4. The method: find where the game already does X

### 4.1 Procedure

1. **Name the effect in game terms.** Not "spawn an enemy wave" but "the game
   puts a monster into a room", "the game makes a monster stronger", "the
   game revives a hero".
2. **Find the noun.** Look for the type that owns it: a manager
   (`RoomMonsters`, `PickupManager`), an actor (`Se_HeroKnockedOut`) or a
   prefab family (`RoomMod_*`, `Shrine_*`). Search type names:
   `ls Dew.Core | grep -i revive`, or `strings Dew.Contents.dll | grep -i <word>`
   for content types.
3. **Find the verb's callers.** Grep for the method that does the change and
   read **who calls it**, not only its body:
   `grep -rn "\.Revive(" Dew.Core`, `grep -rn "AddModifier<" Dew.Core`,
   `grep -rn "NetworkgoldCostMultiplier" .`. A caller is a recipe: arguments,
   order, guards, and what it does right before and after.
4. **Prefer a caller that already does what you want in a context like
   yours.** Pick a modifier that adds something at runtime over world gen,
   and an ability that revives over a debug command.
5. **Copy the recipe, not the code.** Call the same public API with the same
   arguments. Patch only where the recipe has a step you must change (see
   `03-hooking-with-harmony.md`).
6. **Read the callee once for conditions** (`isRoomActive`, `isServer`,
   `isMain`, `isBossSpawn`): those are the catches.
7. **Prove it in play** with a log line per step (`09-debugging-and-testing.md`).

Each worked example below follows the same layout: goal, precedent, API,
catch.

### 4.2 Spawn exactly one monster of a chosen type (`MonsterSpawner`)

**Goal.** Put one specific monster at a chosen point, counted by the room,
with every room hook still applied.

**Precedent.** `RoomMonsters.SpawnMiniBoss` must spawn exactly one specific
monster too. It builds a throwaway rule:

```csharp
settings.rule = ScriptableObject.CreateInstance<MonsterSpawnRule>();
settings.rule.isBossSpawn = true;
settings.rule.pool = ScriptableObject.CreateInstance<MonsterPool>();
settings.rule.pool.entries = new List<MonsterPool.SpawnRuleEntry>();
settings.rule.pool.entries.Add(new MonsterPool.SpawnRuleEntry
    { monster = (Monster)ent, chance = 1f, minCount = 1, maxCount = 1 });
settings.rule.wavesMax = 1;
settings.rule.wavesMin = 1;
settings.rule.onOverPopulation = OverpopulationBehavior.Ignore;
```

`OverrideMonsterType` (used by the Leaf Hound Den and Meteoric Life
modifiers) builds the same one-entry pool.

**API.** Last Stand builds the same rule once per monster type and caches it
(`MonsterSpawner.SingleRule`): `isBossSpawn = true`,
`initialDelay = Vector2.zero`, one wave, zero timeouts, `Ignore` on
overpopulation, one entry with `chance = 1`. Then:

```csharp
SpawnMonsterSettings settings = new SpawnMonsterSettings
{
    spawnPosGetter = () => position,
    afterSpawn = onSpawned,
    ignoreTurnPopMultiplier = true,
    ignoreCoopPopMultiplier = true,
    rule = SingleRule(monster),
};
room.monsters.SpawnMonsters(settings);
```

**Catches.**

- `isBossSpawn` is what makes "one" mean one. Without it the routine rolls
  waves, applies population multipliers and waits for population room.
- `isBossSpawn` also sets `hunterChance = 0`, so hunters must be made
  explicitly (`Se_HunterBuff`, Section 1.4).
- The rule and pool are `ScriptableObject`s you created. Destroy them on
  reset (`MonsterSpawner.Reset`), or they pile up for the whole session.
- The spawn is a coroutine. `afterSpawn` fires one frame or more later, and
  not at all if the room transitions first. Last Stand counts a pending spawn
  as a reservation that expires (`EnemyRegistry.Reserve`).
- Room modifiers that resize population (Hunted, Leaf Hound Den) don't add
  monsters to a boss rule. `onAfterSpawn` hooks still run.

**Where else this applies.** Spawning helpers for a custom event, a "summon
an elite" shrine reward, scripted ambushes at fixed points.

### 4.3 Make a monster stronger (`MonsterEnhancer`)

**Goal.** Add a per-wave health and damage bonus that clients see in
health bars and damage numbers.

**Precedent.** `grep -rn "AddStatBonus(new StatBonus" Dew.Core` leads to
`RoomMod_Hunted.ApplyHunterStatBonusAndAIPrediction`, called from its
`onAfterSpawn` handler:

```csharp
entity.Status.AddStatBonus(new StatBonus
{
    maxHealthPercentage = num * 100f,
    abilityPowerPercentage = num2 * 100f,
    attackDamagePercentage = num2 * 100f
});
```

and to `DewDifficultySettings.ApplyDifficultyModifiers`, which does the same
from `ClientEvent_OnEntityAdd`.

**API.** The same call from the spawn's `afterSpawn`:

```csharp
entity.Status.AddStatBonus(new StatBonus
{
    maxHealthPercentage = (float)healthBonusPercent,
    attackDamagePercentage = (float)damageBonusPercent,
    abilityPowerPercentage = (float)damageBonusPercent,
});
```

**Catches.**

- Damage is two stats. Abilities scale on attack damage *or* ability power,
  so raise both, as the game does.
- Percentages compound (Section 2.7). A multiplier `M` is the bonus
  `(M − 1) × 100`, independent of other bonuses (`MonsterEnhancer.ExtraPercent`).
- The health ratio is kept, so the monster spawns full.
- The resulting stats are synced (`NetworkbonusStats`, final stats), so
  vanilla clients see them. The bonus object itself is server-only.
- For bosses the difficulty bonus arrives with the entity-add event, after
  `afterSpawn`. Apply anything that must stack last one frame later
  (Section 2.1).

**Where else this applies.** Ability haste (`abilityHasteFlat`), elite
affixes, debuffing heroes for a challenge mode, temporary buffs that you
`RemoveStatBonus` later.

### 4.4 Spawn a heroic boss outside its room (`HeroicBosses.Spawn`)

**Goal.** Bring a zone boss (`BossMonster`) into any room as an ordinary,
killable wave enemy.

**Precedent.** Boss rooms bring the boss in through the entry cutscene
(`DewBossRoomEntry`), which can't run in another room. The lower-level
recipe is `SpawnMonsterImp`'s `Dew.SpawnEntity(prefab, pos, rot,
serverActor, DewPlayer.creep, ambientLevel, beforeSpawn)` followed by
`Control.MoveToDestination` toward the nearest hero.

**API.**

```csharp
BossMonster boss = Dew.SpawnEntity(prefab, position, null, actors.serverActor, DewPlayer.creep, level,
    spawned =>
    {
        spawned.NetworkskipBossSoulFlow = true;
        spawned.playBossKillFeedback = false;
    });
Hero target = Dew.GetClosestAliveHero(position);
if (target != null)
    boss.Control.MoveToDestination(target.agentPosition, false);
```

The prefab comes from `DewResources.GetByShortTypeName<BossMonster>(typeName)`.

**Catches.**

- `BossMonster.OnCreate` (server part) sets
  `GameSettingsManager.midJoinWaitType = FightingBoss` when it was `None` or
  `BeforeBossFight`. That changes how mid-run joiners are handled, and the
  boss's removal sets it to `AfterBossFight`. Only travel to a non-boss node
  resets it, so in a mode that never travels, nobody can join for the rest
  of the run (the joiner waits on "The expedition is preparing to move after
  fighting a Heroic Boss"). Last Stand first read the value before
  `SpawnEntity` and wrote it back right after the call returned. That failed
  in play: by the host timing in Section 2.1, `OnCreate` runs later than the
  write. The fix is a once-a-second check during the fight that sets the
  three boss values back to `None` (`MidJoinGate`). The lesson: a value the
  game sets in `OnCreate` can't be undone right after `CreateActor` returns
  on the host. Undo it later, or watch for it.
- `BossMonster.OnDeath` (server part) pauses game time when no other boss
  lives (`isGameTimePausedByGame = true`, cleared only by a room load), kills
  every non-boss `Monster`, starts `RewardRoutine` unless `skipBossSoulFlow`,
  and always starts a revive routine. That is fatal for a wave mode. Last
  Stand skips it with a prefix that calls only the base `Entity.OnDeath`
  through a Harmony reverse patch (`HeroicBossDeathPatch`,
  `03-hooking-with-harmony.md`), then kills only the boss's own descendants.
- Some bosses override `OnDeath` themselves (`Mon_Primus_BossPrimusAeron`
  starts the ending, `Mon_Ink_BossDarkMoon` has its own flow). Last Stand
  rejects any boss type whose class chain declares `OnDeath` below
  `BossMonster`, found by reflection with `BindingFlags.DeclaredOnly`.

### 4.5 Place a shrine (`StandingShrine<T>`)

**Goal.** A Dream Well (and six other shrines) standing in the arena, one of
each, with chosen use limits, surviving room saves and mod reloads without
duplicates.

**Precedent.** `RoomModifierBase.PlaceShrine<T>` (used by `RoomMod_SpawnAscension`,
`RoomMod_SpawnDestiny`, …):

```csharp
if (!base.isNewInstance)
{
    foreach (Actor allActor in NetworkedManagerBase<ActorManager>.instance.allActors)
        if (allActor is Shrine shrine2
            && shrine2.persistentData.TryGetData<string>("RoomModifierBase", "placeShrineId", out var value)
            && !(value != typeof(T).Name))
        { shrine = shrine2; break; }
}
else
{
    ...
    SingletonDewNetworkBehaviour<Room>.instance.props.TryGetGoodNodePosition(out value2, roomRandom);
    shrine = Dew.CreateActor<T>(value2, null, this);
    shrine.persistentData.SetData("RoomModifierBase", "placeShrineId", typeof(T).Name);
}
```

Two lessons in one method: **create with `Dew.CreateActor<T>`**, and **on a
revisit, find the restored one instead of creating another**.

**API.** Last Stand adopts first, then creates with a configure callback as
`beforePrepare`:

```csharp
_shrine = FindExisting(position);
if (_shrine != null) { Configure(_shrine); return; }
_shrine = Dew.CreateActor<T>(position, null, null, Configure);
```

`Configure` sets `maxUseCountPerPlayer`, `NetworkmaxTotalUseCount` and
`useCooldown`, either unlimited (`-1`, `-1`, `0`) or back to the prefab's
values from `DewResources.GetByType<T>()`.

**Catches.**

- `maxUseCountPerPlayer` and `useCooldown` aren't synced. A vanilla client
  still checks the prefab limit in its own `CanInteract`. For an "unlimited"
  shrine the host undoes each use after it happens: a postfix on
  `Shrine.DoPostUseRoutines` calls `ResetUseCounts()`, sets
  `Network_lastUseTime = double.NegativeInfinity` and calls `MakeAvailable()`.
  Details in `04-host-only-networking.md`.
- The arena isn't placed by a modifier, so Last Stand adopts by type and
  distance (`FindObjectsByType<T>` within an adopt radius).
  `persistentData` tagging, as above, is the game's more exact method.

### 4.6 Spread actors along the room's edge (`EdgePlacement`)

**Goal.** Seven shrines at even angles, each near the wall in its own
direction, on walkable floor, reachable from the center.

**Precedent.** The game snaps a wanted point to reachable floor with
`Dew.GetValidAgentDestination_LinearSweep`. `BossMonster`'s revive routine
uses it to drop knocked-out heroes near the boss:

```csharp
Vector3 vector = pos + UnityEngine.Random.insideUnitSphere.Flattened() * 5f;
vector = Dew.GetPositionOnGround(vector);
vector = Dew.GetValidAgentDestination_LinearSweep(pos, vector);
gamePlayer.hero.Control.Teleport(vector);
```

**API.** Measure the room first, then snap. For each of 72 directions, sample
the navmesh outward and keep the farthest point that has a reasonable path
from the center:

```csharp
if (NavMesh.SamplePosition(center + direction * distance, out NavMeshHit hit, radius, NavMesh.AllAreas))
    Floor.Add(hit.position);
...
NavMesh.CalculatePath(center, spot, NavMesh.AllAreas, Path) && Path.status == NavMeshPathStatus.PathComplete
```

The path length must stay under 1.6× the straight line, so floor behind a
wall doesn't count. Then cap each reach at a multiple of the median, choose
the rotation with the largest total, and place each shrine with
`Dew.GetValidAgentDestination_LinearSweep(center, wanted)`.

**Dead end.** The first version used one radius for every shrine (the
shortest reach) and traced only to the first navmesh boundary
(`NavMesh.Raycast`). One rock near the center pulled all shrines about 14.5 m
in. The raycast remains only as a fallback.

**Catches.** `LinearSweep` returns the snapped `start` when it finds nothing
within 50 steps, so a bad direction degrades to "at the center", never "in a
wall". Reuse one `NavMeshPath` instance, as the game does, and do this once
per arrival, not per frame.

**Where else this applies.** Hero staging rings (`HeroStaging` uses the same
`LinearSweep` from the center), reward scatter, safe teleport targets.

### 4.7 Turn on a room modifier mid-room (`HuntedZone`)

**Goal.** The hunter zone (`RoomMod_Hunted`: hunter chance, hunter stat bonus,
artillery) live in the current room for one wave, at a chosen hunt level,
seen by vanilla clients.

**Precedent.** `grep -rn "AddModifier<RoomMod_Hunted>" Dew.Core` finds
`ZoneManager.UpdateModifiersByHunterStatus`. It removes the node's other main
modifier, then calls `AddModifier<RoomMod_Hunted>(i)`. Reading `AddModifier`
(Section 1.7) shows it goes live at once on the current node with an active
room.

**API.**

```csharp
_previousHuntLevel = zones.currentHuntLevel;
zones.currentHuntLevel = huntLevel;                         // read in OnStartServer
_id = zones.AddModifier(node, new ModifierData { type = nameof(RoomMod_Hunted) },
    modifier => ((RoomMod_Hunted)modifier).endWhenRiftOpen = false);
...
zones.RemoveModifier(node, _id);                            // destroys the live instance
zones.currentHuntLevel = _previousHuntLevel;
```

**Catches.**

- **Order.** `RoomMod_Hunted.OnStartServer` evaluates all its curves from
  `currentHuntLevel` once. Set the level *before* `AddModifier`.
- **Room-shaped assumptions.** Both artillery routines test
  `endWhenRiftOpen && Rift.instance.isOpen`. The arena has no rift, so
  `Rift.instance` is `null`, and the routines died with a
  `NullReferenceException` on their first check (seen in `Player.log` for
  dozens of waves before it was found). Clearing the prefab flag in
  `beforePrepare` fixes it, and nothing else would: the routines start inside
  `OnStartServer`/`CallOnReady`.
- **One main modifier.** `RoomMod_Hunted.isMain` is true, so `AddModifier`
  silently removes any other main modifier on the node (an environment, the
  Aura of Pain). Plan which one owns the room.
- **Your own gate.** Last Stand blocks foreign modifiers on its arena with a
  prefix on `AddModifier`, and lets its own through with a re-entrancy
  counter (`ModifierGate.AddOwn`).
- **Shutdown.** After game over the transport stops, and writing
  `nodes` throws `InitSyncObject: IsWritable: neither NetworkServer nor
  NetworkClient are active`. Clear your own state first and call
  `RemoveModifier` only while `NetworkServer.active` (`ArenaModifier.Remove`).

**Where else this applies.** Any `RoomMod_*` environment for one fight, a
custom "challenge" room state, and removing world-gen modifiers from a node
(`ModifierGate.Strip` loops `RemoveModifier` over the node's ids).

### 4.8 Revive knocked-out heroes on your own schedule (`HeroSouls`)

**Goal.** No soul shrine on another node (the arena can't be left). Revive
every downed hero when a wave is cleared.

**Precedent.** `grep -rn "\.Revive(" Dew.Core` gives `Ai_ReviveHero.OnCreate`:

```csharp
if (base.info.target.Status.TryGetStatusEffect<Se_HeroKnockedOut>(out var effect))
    effect.Revive(reviveHealthMultiplier);
```

`Se_HeroKnockedOut.Revive(float reviveHealthMult = -1f)` sets health
(default `reviveHealthMultiplier` 0.5) and destroys the effect. Its
`OnDestroyActor` clears `isKnockedOut`, sends the revive RPC and removes the
hero's soul modifiers from every node.

**API.**

```csharp
foreach (Hero hero in actors.allHeroes.ToArray())
    if (hero.isActive && hero.isKnockedOut
        && hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
        knockedOut.Revive();
```

Only when `Dew.GetAliveHeroCount() > 0`. With everyone down, the game's own
game over must happen.

**Catches.**

- The knock-out calls `CheckAndAddHeroSoul()` from `OnCreate` and again on
  every `ClientEvent_OnRoomLoaded`. That method puts `RoomMod_HeroSoul`
  (`clientData` = owner `guid`) on the node from
  `TryGetNodeIndexForNextGoal`. A separate `ActiveLogicUpdate` starts
  `Quest_LostSoul` after 1 s unless `disableQuest`. Last Stand prefixes
  `CheckAndAddHeroSoul`: it sets `disableQuest = true`, removes any soul for
  that `guid` from every node, and skips the original.
- Ask for `.ToArray()` before iterating `allHeroes`. Reviving changes
  collections that other handlers walk.

### 4.9 Drop currency and items (`LootDropper`)

**Goal.** Bonus gold and dream dust per kill, and optional memory or essence
drops.

**Precedent.** `PickupManager.HandleDeathDrop`, hooked on every monster's
`EntityEvent_OnDeath`, calls `DropGold(isKillGold: true, false, amount,
victim.position)`. Chaos rewards use `RoomRewards.DropChaosReward`. Item drops
are `Dew.CreateSkillTrigger`/`Dew.CreateGem` with values from `LootManager`
(Section 2.3).

**API.**

```csharp
pickups.DropGold(true, false, amount, position);
pickups.DropDreamDust(false, amount, position);
Dew.CreateGem(gem, Dew.GetGoodRewardPosition(position, scatter), quality, recipient);
```

**Catches.** Kill gold computed by the game goes through
`GameManager.GetKillGoldAmount`. Patch that to change vanilla kill gold.
Your own `DropGold` bypasses it, so caps you put on `GetKillGoldAmount` don't
touch your bonus. An untargeted gold orb splits its amount among
`gamePlayers` when picked up. `tempOwner` reserves an item until that hero is
down. `DewPlayer.OnStopServer` destroys a leaving player's reserved items.

### 4.10 Show live text to every player (`WaveTracker`)

**Goal.** A line in the quest tracker, on vanilla clients too, updating with
the wave state.

**Precedent.** `GameMod_Limbo` removes the main goal and starts its own
quest:

```csharp
Quest_ShapeOfDreams quest_ShapeOfDreams = Dew.FindActorOfType<Quest_ShapeOfDreams>();
...
quest_ShapeOfDreams.Destroy();
...
if (base.isNewInstance && NetworkedManagerBase<QuestManager>.instance.activeQuests.Find((DewQuest q) => q is Quest_Limbo) == null)
    NetworkedManagerBase<QuestManager>.instance.StartQuest<Quest_Limbo>();
```

**API.** Reuse the empty `Quest_Limbo`. Set the synced fields in
`beforePrepare`, so the first spawn message already carries them. Adopt an
existing one first:

```csharp
if (quests.TryGetQuest(out Quest_Limbo existing) && !existing.IsNullOrInactive()) { _quest = existing; ... }
_quest = quests.StartQuest<Quest_Limbo>(quest =>
{
    quest.progressType = QuestProgressType.Kills;
    quest.currentProgress = progress;
});
```

Then update `currentProgress` (a SyncVar) only when the text changes, and set
the title and description by localization key with `SetLocalizedTitle` /
`SetLocalizedDescription`.

**Catches.** The localization RPCs aren't replayed to late joiners, so Last
Stand resends them when the count of ready connections changes. Every update
replays the tracker's flash on every client. Keep updates at most once a
second. The text side is in `08-config-ui-and-commands.md`.

### 4.11 Change every gold price at once (`GoldPrices`)

**Goal.** Make shrine and shop prices rise per wave, identically on the host
and on vanilla clients (who compute prices locally for tooltips and their own
`CanAfford` check).

**Precedent.** `grep -rn "goldCostMultiplier" .` finds the SyncVar in
`GameManager`, its use in `GetAdjustedGoldAmount_Cost…`
(`amount *= goldCostMultiplier`), and one writer, `GameMod_Limbo`:

```csharp
gameManager.NetworkgoldCostMultiplier = gameManager.goldCostMultiplier * (1f + d3GoldPriceAmp);
...                                                       // and on destroy:
gameManager.NetworkgoldCostMultiplier = gameManager.goldCostMultiplier / (1f + d3GoldPriceAmp);
```

**API.**

```csharp
if (game == null || !NetworkServer.active) return;
game.NetworkgoldCostMultiplier = multiplier;
...
game.NetworkgoldCostMultiplier = Tuning.VanillaGoldCostMultiplier;   // on stop and unload
```

**Catches.**

- Write the `Network…` property, never the plain field. The generated setter
  marks the SyncVar dirty. The field alone changes only the host.
- Every gold price shares it. Prices that don't read it (the Altar of
  Cleansing's `GetCleanseGoldCost`) can't be changed in a way clients see.
- Restore on every exit path: session stop, game over, unload. Limbo shows
  the pattern of undoing exactly what you applied.

### 4.12 Where else the method applies

- **Clear a room on demand:** `Room.ClearRoom()` (it also destroys idle creep
  monsters and may open rifts; read Section 5.3 first).
- **Teleport heroes:** `HeroStaging` copies `BossMonster`'s
  `Control.Teleport` after `CancelOngoingChannels` and
  `CancelOngoingDisplacement`, and moves summons with the hero.
- **Skip the world map:** `Rift_RoomExit.nextNodeIndex` (SyncVar) makes the
  rift travel straight to a node. The precedent is the rift's own
  `nextNodeIndex >= 0` branch.
- **Remove a run goal or quest:** `GameMod_Limbo`'s `Destroy()` (Section 4.10).
- **Change the next world's monsters:** `Zone.defaultMonsters.pool`, read the
  way `RoomMonsters.OverrideMonsterType` and `SelectMiniBoss` read it
  (`GetFilteredEntries()`, `monster.asset`).

---

## 5. Gotchas

### 5.1 Calls that throw

| Call | When | Fix |
|---|---|---|
| `RoomMonsters.SpawnMiniBoss` | always calls `SelectMiniBoss` first, which indexes `list2[random.Range(0, list2.Count)]` over the room `defaultRule.pool` entries that are `ISpawnableAsMiniBoss`. `DewRandom.Range(0, 0)` returns 0, so an empty list throws `ArgumentOutOfRangeException`, **even when you passed `ent` and `se`**. | Last Stand stores its pick (`MiniBossPick.Set`) and a prefix on `SelectMiniBoss` returns it and skips the original. `try/finally` clears the pick. |
| `Dew.CreateActor`, `Dew.SpawnEntity`, `CreateSkillTrigger`, `CreateGem` | `NetworkServer.active` false | Host only. Check before calling. |
| `Dew.SpawnEntity` | `owner == null` | Pass `DewPlayer.creep` for monsters. |
| `Dew.InstantiateActor_Imp` | prefab `null` (bad type name) | `DewResources.GetByShortTypeName<T>` logs an error and returns `null` for an unknown name, and throws `InvalidCastException` when the name belongs to another type. Wrap the lookup (`HeroicBosses.Find`). |
| `ZoneManager.AddModifier` / `RemoveModifier` | after transport shutdown (game over, restart) | `InitSyncObject: IsWritable …`. Check `NetworkServer.active`. |
| `ZoneManager.AddModifier` | unknown `mod.type` | `GetByShortTypeName<RoomModifierBase>(…).isMain` dereferences null. Use `nameof(RoomMod_X)`. |

### 5.2 Things that need an active room

- `ZoneManager.AddModifier` creates the live instance only when
  `nodeIndex == currentNodeIndex` and `Room.instance.modifiers.isRoomActive`.
  Otherwise your `beforePrepare` runs on a throwaway instance whose state is
  serialized for later. That is easy to miss, because no error is logged.
- `Room.sections` is filled in `StartRoom`. Before that, `sections` is empty
  and section-based spawn points fail. Last Stand's `SpawnPoints.TryPrepare`
  returns false until there is a section with `monsters != null` and
  `area > 0`.
- `SpawnMonstersRoutine` waits while `isInRoomTransition`. A spawn started
  during a load lands in the next room.
- `IsLazyCallReady` is false during transitions and before the local hero
  exists. Retry on a throttled timer (`ArenaArrival` every
  `ArrivalRetrySeconds`), never every frame.

### 5.3 `Room.didClearRoom`

Set from `isRevisit` in `StartRoom`. It's a plain property with a private
setter, not synced. True in any revisited room, so a room made to load
"cleared" (Last Stand sets `isRevisit = true` in a `Room.OnStartServer`
postfix to load the arena lit and empty) behaves as if combat were over.
Readers include:

- `RoomSection` (`clearRoomOnEnterFirstTime`), `RoomModifierBase.PlaceShrine`
  (locks), `GameManager.IsEligibleForMidRunSave`;
- many content modifiers that stop or remove themselves when it's true:
  Crimson Flame Demon (`RoomMod_FireDevil`), `RoomMod_RiskOfMeteors`,
  `RoomMod_GazeOfErebos`, `RoomMod_InkStrikeWarning`, the Aura of Pain's
  damage tick, `Mon_Sky_LittleBaam`'s summons.

To run hazards in a cleared room, Last Stand flips it back with reflection
for the duration (`AccessTools.PropertySetter(typeof(Room), nameof(Room.didClearRoom))`
in `RoomCombat`), and blocks `Room.ClearRoom` meanwhile with a prefix,
because `ClearRoom` would fire `onRoomClear` and destroy idle creep monsters
(AI enabled, older than 3 s, sleeping or without a target, and only while
the node isn't hunted).
A side effect: no mid-run save during that time.

### 5.4 Boss death side effects

From `BossMonster.OnDeath` (server part): the game timer pauses when no other
boss is alive (`isGameTimePausedByGame`, cleared only by a room load); every
non-boss `Monster` that isn't `IDontKillOnBossMonsterDeath` is killed;
`RewardRoutine` locks rifts, places `Shrine_BossSoul` and makes a mid-run
save unless `skipBossSoulFlow`; and a revive routine teleports and revives
knocked-out heroes. `skipBossSoulFlow` alone leaves the pause and the kill-all
in place. Section 4.4 shows the full bypass.

### 5.5 Knock-outs start their own content

`Se_HeroKnockedOut` adds a `RoomMod_HeroSoul` to a node a few steps ahead
(skipped on `ExitBoss` nodes) and, after 1 s, starts `Quest_LostSoul`. Both
assume the party will travel. If your mode doesn't travel, set
`disableQuest` and handle souls yourself (Section 4.8). A plain-arena gate
must still let `RoomMod_HeroSoul` through if you rely on the game's own flow.

### 5.6 Idempotence: revisits, reloads, continues

Anything you create may already exist:

- **Revisit:** the room's actors come back from `visitedNodesSaveData` with
  `isNewInstance == false`. The game's pattern is "find, else create"
  (`PlaceShrine`, `GameMod_Limbo`'s `isNewInstance` check).
- **Mod reload** (developer auto-reload): your in-memory sets are empty while
  the world still holds your shrines, quest, monsters and bosses. Last Stand
  adopts by type and position (`StandingShrine.FindExisting`), by type
  (`WaveTracker` via `TryGetQuest`), and by ownership
  (`EnemyAdoption.AdoptSurvivors`: creep-owned `Monster`s with no `Entity`
  ancestor). A heroic boss alive in the arena counts as its own.
- **Continued save:** `DewNetworkManager.startSettings.continueData != null`.
  Skip "once per new run" grants.

Handlers that the old assembly attached to entities still fire after a
reload, into the old, reset session. Don't make death-time behavior depend
only on a set that a reload empties.

### 5.7 Smaller traps

- `RoomMonsters.OverrideMonsterType` replaces `defaultRule` and
  `section.monsters.ruleOverride` and is never undone. Save and restore
  `defaultRule` around a modifier that calls it.
- Raising `Shrine.maxUseCountPerPlayer` on the host doesn't reach clients.
  A vanilla client whose synced use count is at or above the prefab limit
  gets `CanInteract == false` locally (`04-host-only-networking.md`).
- `Dew.GetGoodRewardPosition` uses `ActorManager.instance` and scans all
  actors. Call it per drop, not per frame.
- `ActorManager.allEntities` is a `HashSet`. Copy it (`.ToArray()`) before
  killing or destroying while iterating. Both `ClearRoom` and
  `BossMonster.OnDeath` iterate `new List<Entity>(allEntities)`.
- `GameManager.SetAmbientLevel` doesn't touch living monsters. Their level
  was fixed at spawn (`Status.level`).

---

## Summary of the calls in this chapter

| To | Call |
|---|---|
| reach a manager cheaply | `NetworkedManagerBase<T>.softInstance` |
| run code per game | `CallOnNetworkedManager<GameManager>(…)` |
| create any actor, configured | `Dew.CreateActor<T>(pos, rot, parent, beforePrepare)` |
| create a unit | `Dew.SpawnEntity(prefab, pos, rot, serverActor, owner, level, beforeSpawn)` |
| spawn through the room | `room.monsters.SpawnMonsters(new SpawnMonsterSettings { rule, spawnPosGetter, afterSpawn })` |
| spawn a miniboss | `room.monsters.SpawnMiniBoss(settings, ent, se)` (beware `SelectMiniBoss`) |
| find floor | `GetSpawnPositionInSection`, `GetValidAgentDestination_LinearSweep`, `NavMesh.*` |
| buff | `entity.Status.AddStatBonus(new StatBonus { … })` |
| add a status effect | `actor.CreateStatusEffect<T>(victim, new CastInfo(actor))` |
| drop | `PickupManager.DropGold/DropDreamDust`, `Dew.CreateSkillTrigger/CreateGem` |
| pay | `DewPlayer.EarnGold/EarnDreamDust` |
| room modifier now | `ZoneManager.AddModifier(currentNodeIndex, data, beforePrepare)` / `RemoveModifier(node, id)` |
| revive | `Se_HeroKnockedOut.Revive()` |
| quest | `QuestManager.StartQuest<T>(beforePrepare)`, `TryGetQuest<T>` |
| wait for readiness | `GameManager.CallOnReady`, `IsLazyCallReady()` |
| wait a frame | `Dew.CallDelayed(action, frames)` |
| watch | `ActorManager.ClientEvent_OnEntityAdd`, `Entity.EntityEvent_OnDeath`, `RoomMonsters.onAfterSpawn`, `ZoneManager.ClientEvent_OnRoomLoaded` |

Next: `07-mod-architecture.md` shows how to arrange these calls into a mod
that survives retries, reloads and game over.
