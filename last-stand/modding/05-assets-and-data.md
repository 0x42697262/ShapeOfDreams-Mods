# 05. Prefab values, scenes, localization and overrides

The decompiled code tells you what the game *does* with a number. It rarely
tells you the number. Use counts, prices, chance tables, flags such as
`isMain`, music tracks and every line of UI text live in Unity's serialized
data: prefabs, ScriptableObjects, scenes and asset bundles. This chapter
teaches how to read that data, three ways plus one shortcut, and how to change
it with the game's JSON override system when that's the right tool.

It covers:

1. Why the code is not enough, and where each kind of data lives on disk.
2. The shortcut: the developer's override dump in `RawData`.
3. The resource database and reading a prefab value from inside the mod.
4. Reading the files offline with UnityPy: type trees, `m_Script`, scenes,
   Odin blobs, `Formula` structs.
5. GUI tools (AssetRipper, AssetStudio).
6. Localization: where strings live, how keys are formed, how to find a key.
7. JSON overrides: how `DewMod` loads, validates, applies and syncs them,
   what they can't touch, and when to choose them over code.
8. Three worked examples from Last Stand: proving the Shrine of Ascension's
   prefab limits, finding which scenes play boss music and their GUIDs, and
   listing which room modifiers are main.

Chapter `02-reading-the-game.md` covers decompiling. Chapter
`04-host-only-networking.md` explains why an unsynced prefab value matters
so much for a host-only mod. This chapter assumes you can grep a decompiled
`Dew.Core/`.

---

## 1. Why code is not enough

### 1.1 Unity serialization in one paragraph

A Unity *prefab* is a saved GameObject tree with its components. A
*ScriptableObject* is a saved data object with no GameObject. A *scene* is
a saved tree of GameObjects. For each `MonoBehaviour` or `ScriptableObject`
in any of these, Unity stores the values of its **serialized fields**:
public fields, and private fields marked `[SerializeField]`, unless marked
`[NonSerialized]`, and only if the field's type is something Unity can
serialize. When the game loads the asset, Unity writes the stored values
into the new object **after** the C# field initializers ran. So the
initializer you see in the decompile is only the default the designer
started from. The stored value wins.

The decompile of the base class `Shrine` (Dew.Core):

```csharp
public abstract class Shrine : Actor, IInteractable, IProp, IBanRoomNodesNearby
{
	...
	public int maxUseCountPerPlayer = -1;

	public float useCooldown = 1f;
```

The `Shrine_Ascension` prefab, read from the asset bundle (section 4.5):

```
"maxTotalUseCount": -1,
"maxUseCountPerPlayer": 4,
"useCooldown": 0.0,
```

If you trusted the code, you'd say the Shrine of Ascension has no use limit
and a one-second cooldown. It has four uses per player and no cooldown.

### 1.2 What lives only in data

Values met while building Last Stand, none of which is in any `.cs` file:

| Value | Where | Read as |
|---|---|---|
| `Shrine_Ascension.maxUseCountPerPlayer = 4`, `useCooldown = 0` | prefab | `int`, `float` |
| `Shrine_Ascension.baseGoldCostByStartRarity` (common 30, rare 60, epic 120, legendary 150, unique 30) | prefab | `PerRarityData<float>` |
| `Shrine_MawOfDoom.goldCostByUseCount = "0+20*x"`, `destroyChanceByUseCount = "Min(0+0.08*x, 0.5)"` | prefab | `Formula` |
| `RoomMod_Hunted.endWhenRiftOpen = 1` | prefab | `bool` |
| `isMain`, `hiddenOnVisitedNode`, `disableOtherEnvParticles` on every `RoomMod_*` | prefabs | `bool` |
| `Shrine_Hatred_Reward` weights (`goldWeight` 1, `dreamDustWeight` 1, `skillWeight` 1, `gemWeight` 1.25, `chaosWeight` 1) and multipliers | prefab | `float`, `float[]` |
| Which track a boss room plays (`FxChangeMusic.music` on `BossMusicStart`) | scene | reference to a `DewMusicItem` |
| `DewMusicItem.resourceId` GUIDs | `sharedassetsN.assets` | `string` |
| Every UI string ("Threat Level Rising") | `Resources/MainLocalization` | Odin binary |
| Difficulty factors (`diffNormal.healthFactorBeforeLoop = 0.8`) | ScriptableObject | `float` |

### 1.3 Why a host-only mod cares even more

On a vanilla client, every prefab value is the shipped one. The host can
change a field on its own instance, but if the field isn't a SyncVar the
client never hears about it (chapter `04-host-only-networking.md`). Client
code then computes with the shipped value. `Shrine_Ascension` builds its
"uses left" text on each client:

```csharp
public override string GetEditSkillIndicatorRawText()
{
	return string.Format(base.GetEditSkillIndicatorRawText(), maxUseCountPerPlayer - GetUseCountByPlayer(DewPlayer.local));
}
```

`maxUseCountPerPlayer` there is the client's own copy, so it's 4 no matter
what the host sets. To keep host and client in agreement, Last Stand had to
know the shipped value and keep it. You can't design around a number you
haven't read.

### 1.4 Where things are on disk

All paths are under `<GameFolder>/Shape of Dreams_Data/` unless noted.

| File | Holds | Type trees |
|---|---|---|
| `Managed/*.dll` | code: `Dew.Core.dll`, `Dew.Contents.dll`, `Dew.UI.dll`, `Sirenix.Serialization.dll`, `Unity.Addressables.dll`, `NCalc.dll`, `Newtonsoft.Json.dll` | n/a |
| `StreamingAssets/aa/catalog.bin`, `settings.json` | the Addressables catalog: every loadable key (GUID) and where it lives | n/a |
| `StreamingAssets/aa/StandaloneWindows64/defaultlocalgroup_assets_all_<hash>.bundle` | the Addressables content: actor prefabs (shrines, monsters, room modifiers, status effects), ScriptableObjects | embedded |
| `StreamingAssets/aa/StandaloneWindows64/<hash>_monoscripts_<hash>.bundle` | the `MonoScript` objects the content bundle points at | embedded |
| `levelN` | scene N (rooms, title screen) | **stripped** |
| `sharedassetsN.assets` (+ `.resS`, `.resource`) | assets used by scene N, such as its `DewMusicItem`s | stripped |
| `globalgamemanagers` | `BuildSettings` (scene list), `ResourceManager` (the `Resources/` index) | stripped |
| `globalgamemanagers.assets` | the `MonoScript` objects scenes point at | stripped |
| `resources.assets` | everything under `Resources/`: `MainLocalization`, `MainResources` | stripped |
| `<GameFolder>/RawData/` | the developer's data dump for wikis and modders (section 2) | n/a |

A *type tree* is the field layout Unity can embed next to serialized data.
With it, a reader decodes an object without knowing the C# class. Release
builds strip it from scenes and `.assets` files. The Addressables bundles
here still carry it. Section 4.2 shows how to rebuild stripped type trees
from the `Managed` DLLs.

---

## 2. The shortcut: `RawData/!ModResources/overrides`

Before any tooling, look in `<GameFolder>/RawData/`. The developer ships it
"to support our players who wish to create fan resources, wikis, guides"
(its `Readme.txt`):

```
RawData/
  !Images/  !Sprites/  Readme.txt  gradients.json
  !ModResources/overrides/        ← every overridable prefab value
  en-US/ de-DE/ ... zh-TW/        ← memories.json, essences.json, stars.json, travelers.json, achievements.json
```

The official mod docs say the `overrides` folder lists which parameters a
JSON override can change. It also lists their **current values**, one JSON
file per prefab, grouped in folders:

```sh
cd "<GameFolder>/RawData/!ModResources/overrides"
find . -name 'Shrine_Ascension.json'
# ./Shrines/Shrine_Ascension/Shrine_Ascension.json
cat Shrines/Shrine_Ascension/Shrine_Ascension.json
```

```json
[
  {
    "target": "Shrine_Ascension",
    "overrides": {
      "addedCostMultiplierPerUse": "0.1",
      "canTargetSkill": "True",
      "canTargetGem": "True",
      "shouldExitEditModeAfterAction": "False",
      "canRepeatConfirmHold": "False",
      "mapVisibility": "0",
      "interactableDelay": "0",
      "maxTotalUseCount": "-1",
      "maxUseCountPerPlayer": "4",
      "useCooldown": "0",
      "baseGoldCost": "0",
      "unavailableByDefault": "False"
    }
  }
]
```

Other answers from the same dump, each one `grep` away:

```sh
grep -r '"endWhenRiftOpen"' RoomMods/Hunted/          # "True"
grep -rl '"isMain": "True"' . | wc -l                 # 37 files (36 under RoomMods/, RoomMod_FallenStar under Treasures/StrayMemory/)
cat Shrines/Shrine_Hatred/Shrine_Hatred_Reward.json    # goldWeight 1 ... gemWeight 1.25, dreamDustMultipliers.0..3
```

**What it can't show.** The dump has the same shape as what
`DewMod.BuildJsonOverrideOriginal` builds (section 7.2): only the `Actor`,
`EntityAI`, `EntityStatus` and `EntityControl` components of prefabs whose
name starts with an allowed prefix, and only fields of allowed types. It
also leaves out `syncInterval`, which `DewMod` ignores. So:

- `Shrine_Ascension.baseGoldCostByStartRarity` is missing
  (`PerRarityData<float>` isn't an allowed type).
- `Shrine_MawOfDoom` lists `baseGoldCost: "30"`, but its real price comes
  from `goldCostByUseCount`, a `Formula`, which is missing. Reading the dump
  alone would give you the wrong price.
- Scenes, ScriptableObjects, music and localization aren't there at all.

Use the dump first. Go to sections 3 and 4 when the field is absent, or
when you need proof from the actual shipped asset.

The per-language JSON files hold the **rendered** names and descriptions of
memories, essences, stars and travelers keyed by type name
(`"St_C_BackStep": {"name": "Backstep", ...}`). They help you match a
displayed name to a type. They aren't the UI string table (section 6).

**Where else this applies:** checking a monster's `EntityStatus::baseStats`
before scaling it, the full list of overridable fields for any `Se_`, `Ai_`
or `Mon_` prefab, spotting which component a value sits on (keys like
`EntityStatus::baseStats.attackDamage`).

---

## 3. The resource database: reading values from inside the mod

### 3.1 How the game finds an asset

Everything the game spawns by name or type goes through the static class
`DewResources` (Dew.Core). It loads one `DewResourceDatabase` from
`Resources/MainResources` (`Resources.Load<DewResourceDatabase>("MainResources")`).
That's an Odin `SerializedScriptableObject` full of dictionaries:

```csharp
public class DewResourceDatabase : SerializedScriptableObject
{
	public Dictionary<string, string> nameToGuid = ...;
	public Dictionary<string, string> guidToName = ...;
	public Dictionary<uint, string> netObjectAssetIdToGuid = ...;
	public List<string> sceneNames = ...;
	public Dictionary<string, string> lightToHeavyGuidMap = ...;
	public Dictionary<string, string> heavyToLightGuidMap = ...;
	[NonSerialized]
	public Dictionary<Type, string> typeToGuid = ...;
	public Dictionary<string, string> typeNameToGuid = ...;
	public Dictionary<string, Type> typeNameToType = ...;
	...
}
```

Every lookup ends in a GUID string. `DewResources.Load(guid)` hands it to
Addressables:

```csharp
value = Addressables.LoadAssetAsync<UnityEngine.Object>(guid);
...
UnityEngine.Object @object = value.WaitForCompletion();
```

So the GUID must be a key in `StreamingAssets/aa/catalog.bin`. The catalog
stores keys as plain ASCII, so `grep -a -c <guid> catalog.bin` is a quick
check that a GUID is loadable.

### 3.2 The lookup API

Checked in `Dew.Core/DewResources.cs`:

| Call | Looks up | Notes |
|---|---|---|
| `GetByType<T>()` | `typeToGuid[typeof(T)]` | one prefab per type; returns the component `T` on the prefab (`Convert` calls `TryGetComponent`) |
| `GetByShortTypeName<T>(string)` | `typeNameToType[name]` then by type | `"Shrine_Ascension"` → the prefab's `Shrine_Ascension` |
| `GetByName<T>(string)` | `nameToGuid[name]` | logs `GetByName<T> failed with name: ...` when missing |
| `GetByGuid<T>(string)` | the GUID directly | no dictionary; works for anything in the catalog |
| `FindAllByType<T>()` | every type assignable to `T` that has a prefab | loads them all |
| `FindAllByNameSubstring<T>(string)` | case-insensitive substring over `nameToGuid` keys | `ZoneManager` lists zones with `FindAllByNameSubstring<Zone>("Zone_")` |
| `FindAllByTypeSubstring<T>(string)` | substring over `typeNameToGuid` keys | |
| `FindRoomNameBySubstring(string)` | `sceneNames` starting with `Room_` | returns a scene name, not an object |
| `GetLinkId(obj)`, `GetByLinkTypeAndId<T>(by, id)` | by the type's link kind | see 3.3 |

Every call takes an optional `ResourceLoadSettings`. `ResourceLoadSettings.Light`
sets `loadLight`, and `Load` then swaps the GUID through
`heavyToLightGuidMap`. Each heavy actor prefab can have a lighter twin.
Last Stand loads curses that way, because it only reads their type and
settings:

```csharp
foreach (CurseStatusEffect curse in DewResources.FindAllByType<CurseStatusEffect>(ResourceLoadSettings.Light))
```

### 3.3 Link kinds: why `GetByName` fails for some assets

`[DewResourceLink(ResourceLinkBy.X)]` on a class decides how the game
refers to its assets. `ResourceLinkBy` is `None`, `Type`, `Guid` or `Name`.
`DewResources.GetLinkType` walks the class and its base classes for the
attribute:

| Kind | Examples (from the decompile) | Identity |
|---|---|---|
| `Type` | `Actor` and all subclasses (shrines, monsters, modifiers), `DewPlayer`, `PlayGameManager` | one prefab per C# type; use `GetByType` / `GetByShortTypeName` |
| `Name` | `Zone`, `DewDifficultySettings`, `Rift_Sidetrack`, `Skin`, `Emote`, `TravelerStoryNodeDef` | the asset name; use `GetByName` / `FindAllByNameSubstring` |
| `Guid` | `DewMusicItem`, `DewAudioClip`, `DewAnimationClip` | an `ILinkedByGuid.resourceId`; use `GetByGuid` |
| `None` | `Rift` | not linkable |

`DewMusicItem` is guid-linked:

```csharp
[DewResourceLink(ResourceLinkBy.Guid)]
[CreateAssetMenu(fileName = "mus", menuName = "Dew Music Item")]
public class DewMusicItem : ScriptableObject, ILinkedByGuid
{
	public AudioClip clip;
	...
	[field: HideInInspector]
	[field: SerializeField]
	public string resourceId { get; set; }
```

Last Stand checked in game: `nameToGuid` and `guidToName` don't list the boss
tracks, so `GetByName<DewMusicItem>("musBossSeeker")` fails. The way in is
the GUID, which you only get from the data (section 4.6).

Assets also point at each other by GUID. `AssetRef<T>` (Dew.Core) is a
serialized struct holding `guid`, `typeName` and
`typeAssemblyQualifiedName`. Its `asset` getter loads by GUID, and
`lightAsset` goes through `heavyToLightGuidMap`. `Zone.defaultMusicRef` is
an `AssetRef<DewMusicItem>`, and `MonsterSpawnRule` pool entries hold
`AssetRef<Monster>`s. Offline, an `AssetRef` shows up as a `guid` string
you can follow into the catalog.

### 3.4 Reading a prefab value at runtime

The cheapest proof is to ask the running game. A probe command (a
`ModBehaviour` with a console method; `[ConsoleCommand(description, name)]`
is `ModBehaviour.ConsoleCommandAttribute`, see `08-config-ui-and-commands.md`):

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

public class PrefabProbe : ModBehaviour
{
	private const BindingFlags Declared =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

	[ConsoleCommand("Log a prefab's serialized fields", "probe_prefab")]
	public void ProbePrefab(string typeName)
	{
		UnityEngine.Object prefab = DewResources.GetByShortTypeName(typeName);
		if (prefab == null)
		{
			Debug.Log("[probe] no prefab for " + typeName);
			return;
		}

		for (Type t = prefab.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
		{
			foreach (FieldInfo f in t.GetFields(Declared))
			{
				bool serialized = (f.IsPublic || f.IsDefined(typeof(SerializeField), false)) && !f.IsNotSerialized;
				if (serialized)
				{
					Debug.Log($"[probe] {t.Name}.{f.Name} = {Describe(f.GetValue(prefab))}");
				}
			}
		}
	}

	private static string Describe(object value)
	{
		if (value is UnityEngine.Object asset)
		{
			return asset == null ? "null" : asset.name;
		}
		try
		{
			return JsonConvert.SerializeObject(value);
		}
		catch (Exception e)
		{
			return value + " (" + e.GetType().Name + ")";
		}
	}
}
```

`probe_prefab Shrine_Ascension` should log `Shrine.maxUseCountPerPlayer = 4`,
and `Shrine_Ascension.baseGoldCostByStartRarity` as a JSON object (Newtonsoft
writes the public fields of `PerRarityData<float>`). A `Formula` should print
as `{"formula":"0+20*x"}`, because its other fields are private. Unity
structs such as `Color` or `Vector3` can make Newtonsoft throw on a
self-referencing property; the `catch` falls back to `ToString()`. The game ships
`Newtonsoft.Json.dll` in `Managed`; reference it like the other game DLLs
(`01-project-setup.md`).

Last Stand reads prefab values in production code. When it places a
Shrine of Ascension or reuses one after a mod reload, it puts the shipped
limits back (`StandingShrine.Configure`):

```csharp
T prefab = DewResources.GetByType<T>();
if (prefab != null)
{
	shrine.maxUseCountPerPlayer = prefab.maxUseCountPerPlayer;
	shrine.NetworkmaxTotalUseCount = prefab.maxTotalUseCount;
	shrine.useCooldown = prefab.useCooldown;
}
```

Reading the prefab instead of hard-coding 4 keeps the host in step with the
clients' copies even if a patch changes the number.

**Gotchas.**

1. **Never write to the prefab.** `GetByType` returns the loaded asset
   itself, not a copy. A write changes every later spawn on the host, until
   the asset is unloaded. Vanilla clients keep their own value, so you've
   created the very drift section 1.3 warns about. Write to the spawned
   instance.
2. **Overrides show up at runtime.** When any JSON override targets the
   prefab, `DewResources.GetVariant` returns a clone with the overrides
   applied (section 7.3). Your probe then shows the overridden value. The
   offline readers in section 4 always show the shipped value. Compare both
   when the numbers disagree.
3. **Loading is synchronous and real.** `Load` calls `WaitForCompletion()`.
   `FindAllByType<RoomModifierBase>()` loads every modifier prefab. Fine in a
   console command, not every frame (`07-mod-architecture.md`).
4. **A missing entry logs an error.** `GetByType` and `GetByName` print
   `[DewResources] ... failed` to the log on a miss. Probe with
   `DewResources.database.typeNameToGuid.ContainsKey(name)` first when a miss
   is expected.
5. **Odin fields are invisible to Unity but visible to you.** At runtime an
   Odin-serialized dictionary is an ordinary C# `Dictionary`, already
   deserialized. Offline it's a byte blob (section 4.8).

**Where else this applies:** `DewResources.database.sceneNames` lists every
room scene (`Room_Forest_Boss_0` ...), `FindAllByType<MirageSkinEffect>()`
and `FindAllByType<MiniBossEffect>()` give the full pools Last Stand's
monster catalog draws from, and `GetByShortTypeName<BossMonster>(name)`
checks that a configured boss name exists before using it.

---

## 4. Reading the files offline with UnityPy

The runtime probe needs the game running and a type you already know.
Offline reading answers "which prefabs have `isMain` set?" or "what does
scene 95 contain?" in seconds, for every asset at once, without starting the
game.

### 4.1 Setup

```sh
python3 -m venv sod-venv
sod-venv/bin/pip install UnityPy TypeTreeGeneratorAPI
export SOD_DATA="<GameFolder>/Shape of Dreams_Data"
```

UnityPy reads Unity's serialized files and bundles. TypeTreeGeneratorAPI
rebuilds stripped type trees from the game's `Managed` DLLs. All scripts
below were run against the shipped game with UnityPy 1.25 and Python 3.14.

### 4.2 Concepts you need

- **Serialized file.** `levelN`, `sharedassetsN.assets`,
  `globalgamemanagers.assets`, and each file inside a bundle. It holds
  objects, each with a `path_id` (a 64-bit id unique within that file) and a
  class (`GameObject`, `Transform`, `MonoBehaviour`, `MonoScript`, ...).
- **PPtr.** A reference: `{"m_FileID": f, "m_PathID": p}`. `f = 0` means
  this file, `f = n` means `externals[n - 1]` of this file. `m_PathID = 0` is
  null.
- **MonoBehaviour layout.** Every `MonoBehaviour` starts with
  `m_GameObject` (PPtr), `m_Enabled` (byte plus 3 padding bytes), `m_Script`
  (PPtr to a `MonoScript`), `m_Name`, then the script's own fields in
  declaration order, base class first.
- **MonoScript.** Holds `m_ClassName`, `m_Namespace` and `m_AssemblyName`.
  This is how you map a MonoBehaviour to its C# type: follow `m_Script`.
  For scene objects, `m_Script` is `{m_FileID: 1, ...}`, and `externals[0]`
  of every `levelN` is `globalgamemanagers.assets`, where the scenes'
  MonoScripts live. Bundle objects point into the `_monoscripts_` bundle.
- **Type trees.** Present in the bundles, stripped from `levelN` and
  `.assets`. Without one, UnityPy can still read the MonoBehaviour header
  (up to `m_Name`) but not the script's fields. A `TypeTreeGenerator` built
  from the Unity version and the `Managed` folder supplies the layout.
- **Unity version.** Read it from the files rather than guessing. At the
  time of writing it's `6000.0.77f1` (Unity 6).

### 4.3 A shared helper

Save as `sod_common.py`. Each later script imports it.

```python
import os, sys, glob, struct
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

DATA = os.environ.get("SOD_DATA") or sys.exit("set SOD_DATA to <GameFolder>/Shape of Dreams_Data")
BUNDLES = glob.glob(os.path.join(DATA, "StreamingAssets/aa/StandaloneWindows64/*.bundle"))

def unity_version():
    ggm = UnityPy.load(os.path.join(DATA, "globalgamemanagers"))
    return next(iter(ggm.objects)).assets_file.unity_version

def generator():
    gen = TypeTreeGenerator(unity_version())
    gen.load_local_dll_folder(os.path.join(DATA, "Managed"))
    return gen

def script_class(obj):
    try:
        return obj.read(check_read=False).m_Script.read().m_ClassName
    except Exception:
        return None

def game_object_name(obj, tree):
    go = obj.assets_file.objects.get(tree["m_GameObject"]["m_PathID"])
    return go.read().m_Name if go else "?"

def deref(obj, env, pptr):
    if pptr["m_PathID"] == 0:
        return None
    if pptr["m_FileID"] == 0:
        return obj.assets_file.objects.get(pptr["m_PathID"])
    ext = obj.assets_file.externals[pptr["m_FileID"] - 1].path.split("/")[-1]
    for path, f in env.files.items():
        if path.endswith(ext):
            return f.objects.get(pptr["m_PathID"])
    return None
```

`script_class` reads only the header, so it works before a generator is
attached. Attaching the generator first made `m_Script.read()` fail on scene
objects ("Failed to resolve pointer - invalid m_FileID"). The scripts below
therefore pick the objects by class first, then attach the generator, then
call `read_typetree()` on the matches only. That's also much faster.

### 4.4 Script: dump a prefab class

`sod_prefab.py PREFIX [field,field,...]` prints every MonoBehaviour in the
Addressables bundles whose class starts with `PREFIX`: all non-reference
fields, or just the listed ones.

```python
import sys, json
import UnityPy
from sod_common import BUNDLES, generator, script_class

prefix, fields = sys.argv[1], sys.argv[2].split(",") if len(sys.argv) > 2 else None
env = UnityPy.load(*BUNDLES)
env.typetree_generator = generator()
seen = set()
for obj in env.objects:
    if obj.type.name != "MonoBehaviour":
        continue
    cls = script_class(obj)
    if not cls or not cls.startswith(prefix):
        continue
    tree = obj.read_typetree()
    if fields:
        row = tuple(tree.get(f) for f in fields)
        if (cls, row) not in seen:
            seen.add((cls, row))
            print(cls, dict(zip(fields, row)))
    else:
        plain = {k: v for k, v in tree.items() if not (isinstance(v, dict) and "m_PathID" in v)}
        print(f"== {cls} (path_id {obj.path_id})")
        print(json.dumps(plain, indent=1, default=str))
```

A full pass over both bundles takes about 14 s. `bool` fields come back as
`0`/`1`, and floats as their exact binary value (`0.10000000149011612`).

### 4.5 Worked example 1: proving the Shrine of Ascension's prefab limits

**Problem.** Last Stand places the game's `Shrine_Ascension` in its arena
and gives the uses back every wave. Section 1.3 showed that each client
computes "uses left" from its own `maxUseCountPerPlayer`. Before deciding
how to reset uses, Last Stand needed the shipped values, and the price
table, to predict what a vanilla client shows and pays.

**Step 1: read the code.** `Shrine.maxUseCountPerPlayer = -1`,
`useCooldown = 1f`. `Shrine_Ascension` (from `Dew.Contents`, decompiled one
type at a time, see `02-reading-the-game.md`) declares:

```csharp
public class Shrine_Ascension : EditSkillShrine
{
	public float addedCostMultiplierPerUse = 0.1f;

	public PerRarityData<float> baseGoldCostByStartRarity;
```

No value for the price table at all, and initializers that section 1.1 says
not to trust.

**Step 2: the dump.** `RawData/.../Shrine_Ascension.json` gives
`maxUseCountPerPlayer: 4`, `useCooldown: 0`, `addedCostMultiplierPerUse:
0.1`. The price table is missing, because `PerRarityData<T>` isn't overridable.

**Step 3: the bundle.**

```sh
sod-venv/bin/python sod_prefab.py Shrine_Ascension
```

```
== Shrine_Ascension (path_id ...)
{
 ...
 "interactableDelay": 0.0,
 "maxTotalUseCount": -1,
 "maxUseCountPerPlayer": 4,
 "useCooldown": 0.0,
 "baseGoldCost": 0,
 "unavailableByDefault": 0,
 "_scaleSpawnRateWithPlayers": 1,
 "canTargetSkill": 1,
 "canTargetGem": 1,
 "shouldExitEditModeAfterAction": 0,
 "canRepeatConfirmHold": 0,
 "addedCostMultiplierPerUse": 0.10000000149011612,
 "baseGoldCostByStartRarity": {
  "common": 30.0,
  "rare": 60.0,
  "epic": 120.0,
  "legendary": 150.0,
  "character": 0.0,
  "identity": 0.0,
  "unique": 30.0
 },
 ...
}
== Shrine_Ascension (path_id ...)
{ ... same values ... }
```

**Dead end worth knowing: two objects.** The class appears twice with the
same values. In one copy every effect reference (`availableEffect`,
`useEffect`, `fxEnterEditModeLocal`, ...) is null, and in the other they
point at objects. That fits the light/heavy pairs from section 3.2: the
database maps a heavy prefab to a lighter twin. Read the one with the
effects, and check that the tuning fields agree.

**Step 4: the price formula from code.** With the table in hand, the
decompiled `GetCost` reads as:
`RoundToInt(GetAdjustedGoldAmount_Cost_Service(base[rarity] × (1 + 0.1 ×
uses)))`. `GetAdjustedGoldAmount_Cost_Service` multiplies by the synced
`GameManager.goldCostMultiplier`, which is the one lever the host has over
what a client is shown (chapter `04-host-only-networking.md`).

**Conclusion.** The prefab allows 4 uses per player, has no total cap and no
cooldown. A client compares its use count against its own 4, and once it
reaches 4, `CanInteract` is false on that client. Last Stand keeps the
prefab's limits exactly (and restores them from the prefab, section 3.4),
and gives uses back by clearing the synced use counts instead of raising
the limit. The price shown and the price charged then match on every
machine.

**Same method, other values.**

```sh
sod-venv/bin/python sod_prefab.py Shrine_MawOfDoom | grep -A1 ByUseCount
#  "destroyChanceByUseCount": {
#   "formula": "Min(0+0.08*x, 0.5)"
#  "goldCostByUseCount": {
#   "formula": "0+20*x"

sod-venv/bin/python sod_prefab.py Shrine_Hatred_Reward
#  "goldWeight": 1.0, "dreamDustWeight": 1.0, "skillWeight": 1.0,
#  "gemWeight": 1.25, "chaosWeight": 1.0,
#  "goldMultipliers": [0.5, 0.8..., 1.2..., 1.6...],
#  "dreamDustMultipliers": [0.8..., 1.5, 2.4..., 3.29...],
#  "currencyDeviation": 0.15...

sod-venv/bin/python sod_prefab.py DewDifficultySettings m_Name,enemyHealthPercentage,healthFactorBeforeLoop
# DewDifficultySettings {'m_Name': 'diffHard', 'enemyHealthPercentage': 10.0, 'healthFactorBeforeLoop': 1.0}
# DewDifficultySettings {'m_Name': 'diffNormal', 'enemyHealthPercentage': 0.0, 'healthFactorBeforeLoop': 0.800000011920929}
# ...
```

The Maw's `x` is `GetUseCountByPlayer(player)` (see 4.9), so the first use
costs `20 × 1` before the zone curves, with an 8% destroy chance. The
Hatred weights explain why an essence reward is slightly more common than
the others (`Dew.SelectRandomWeightedIndexInParams` over those five
weights).

### 4.6 Script: the scene index

Scenes are stored as `level0`, `level1`, ... in build order. The order
lives in `globalgamemanagers`' `BuildSettings` object:

```python
import os
import UnityPy
from sod_common import DATA

ggm = UnityPy.load(os.path.join(DATA, "globalgamemanagers"))
for obj in ggm.objects:
    if obj.type.name == "BuildSettings":
        for i, scene in enumerate(obj.read().scenes):
            print(f"level{i}\t{scene}")
```

```
level0   Assets/Scenes/Splash.unity
level2   Assets/Scenes/Title.unity
level14  Assets/Dew/Zones/DarkCave/Rooms/Boss_0/Room_DarkCave_Boss_0.unity
level41  Assets/Dew/Zones/Despair/Rooms/Boss_0/Room_Despair_Boss_0.unity
level68  Assets/Dew/Zones/Forest/Rooms/Boss_0/Room_Forest_Boss_0.unity
level95  Assets/Dew/Zones/Ink/Rooms/Boss_0/Room_Ink_Boss_0.unity
level125 Assets/Dew/Zones/LavaLand/Rooms/Boss_0/Room_LavaLand_Boss_0.unity
level151 Assets/Dew/Zones/Primus/Rooms/Room_Primus_Boss_0/Room_Primus_Boss_0.unity
level154 Assets/Dew/Zones/Sky/Rooms/Boss_0/Room_Sky_Boss_0.unity
level181 Assets/Dew/Zones/SnowMountain/Rooms/Boss_0/Room_SnowMountain_Boss_0.unity
```

The scene file name is the room name the game uses
(`DewResources.database.sceneNames`, `ZoneManager.SetRoomOverride`). The
level numbers can shift when the developer adds a scene, so rerun this
after a game update.

### 4.7 Script: what's in a scene

`sod_scene_scripts.py LEVEL [word,word,...]` counts every script in a
scene, then lists the GameObject path of each component whose class
contains one of the words. It reads `m_Script` straight from the raw bytes
(`struct` format `<iqB3xiq` = `m_GameObject` fileID and pathID, `m_Enabled`
plus padding, `m_Script` fileID and pathID), so it needs no type tree and
runs in about a second.

```python
import os, sys, struct, collections
import UnityPy
from sod_common import DATA

level = sys.argv[1]
wanted = [w.lower() for w in sys.argv[2].split(",")] if len(sys.argv) > 2 else []

ggm = UnityPy.load(os.path.join(DATA, "globalgamemanagers.assets"))
scripts = {o.path_id: o.read().m_ClassName for o in ggm.objects if o.type.name == "MonoScript"}

env = UnityPy.load(os.path.join(DATA, level))
objs = list(env.objects)
names, parent_of, go_of_tf = {}, {}, {}
for o in objs:
    if o.type.name == "GameObject":
        g = o.read(); names[o.path_id] = (g.m_Name, g.m_IsActive)
for o in objs:
    if o.type.name == "Transform":
        t = o.read(); go_of_tf[o.path_id] = t.m_GameObject.path_id
        parent_of[t.m_GameObject.path_id] = t.m_Father.path_id

def path(go):
    parts = []
    while go in names and len(parts) < 12:
        parts.append(names[go][0]); go = go_of_tf.get(parent_of.get(go, 0))
    return "/".join(reversed(parts))

rows = []
for o in objs:
    if o.type.name != "MonoBehaviour":
        continue
    go_file, go_path, enabled, script_file, script_path = struct.unpack_from("<iqB3xiq", o.get_raw_data(), 0)
    cls = scripts.get(script_path, f"?{script_file}/{script_path}") if script_file == 1 else f"ext{script_file}/{script_path}"
    rows.append((cls, enabled, go_path))

counts = collections.Counter(r[0] for r in rows)
print("scripts:", ", ".join(f"{n}×{c}" for n, c in sorted(counts.items())))
for cls, enabled, go in sorted(rows):
    if any(w in cls.lower() for w in wanted):
        print(f"  {cls:32s} enabled={enabled} active={names.get(go, ('?', '?'))[1]}  {path(go)}")
```

The Ink boss room:

```sh
sod-venv/bin/python sod_scene_scripts.py level95 boss,music,allheroes
```

```
scripts: CinemachineBrain×1, ..., DewAllHeroesPresentZone×1, DewAudioSource×30, DewBossRoomEntry×1, ...
  DewAllHeroesPresentZone          enabled=1 active=False  Boss Room Entry
  DewBossRoomEntry                 enabled=1 active=False  Boss Room Entry
  FxChangeMusic                    enabled=1 active=False  BossMusicEnd
  FxChangeMusic                    enabled=1 active=True  BossMusicStartSecond
  FxChangeMusic                    enabled=1 active=False  BossMusicStart
  Ink_BossRoomCenter               enabled=1 active=True  CenterPosistion
  Ink_BossRoomDamageGround         enabled=1 active=False  CenterPosistion/Ink_BossRoomDamageGround (1)
  Ink_BossSelector                 enabled=1 active=False  BossSelector
  Ink_BossTeleportPosition         enabled=1 active=True  BossTeleportPosition
```

Two variations answer "what is special about each arena room": count only
`Dew.Core`/`Dew.Contents` scripts (compare `MonoScript.m_AssemblyName`) and
print, per scene, the ones that appear in at most two of the scenes passed
in:

```
level14  ['BossSeekerKillBehavior', 'DarkCaveCutSceneFogFadeOut', 'DarkCave_BlinkPosition', 'DarkCave_BossRoomCenter']
level41  ['Despair_CliffSpawnerAdvanced', 'Despair_FogPositionController', 'Despair_RockPatternSpawner', ..., 'Shrine_Despair']
level95  [..., 'Ink_BossRoomCenter', 'Ink_BossRoomDamageGround', 'Ink_BossSelector', ..., 'Shrine_BrokenWoodenDoll']
level125 [..., 'LavaLand_InfernusPillarPosition', 'LavaLand_Lava', 'Room_SurfaceOverride']
level151 [..., 'Primus_Ending', 'Primus_Pizza0', 'Primus_Pizza1', ..., 'Special_RoomAnnouncer']
```

Or count a fixed list of structural components and `Light` objects per room:

```
level68  Room_Forest_Boss_0 lights=31 {'RoomSection': 1, 'DewCutsceneDirector': 1, 'Room_Barrier': 1}
level95  Room_Ink_Boss_0    lights=28 {'DewCutsceneDirector': 2, 'DewBossRoomEntry': 1, 'DewAllHeroesPresentZone': 1, 'DewHeroesTeleporter': 1, 'RoomSection': 1}
level151 Room_Primus_Boss_0 lights=56 {'Room_BossArena': 1, 'Primus_Ending': 1, 'DewCutsceneDirector': 2, 'Special_RoomAnnouncer': 1, 'RoomSection': 1}
```

Last Stand turned boss rooms into a plain arena (no entry trigger, no
cutscene, no boss). These lists tell you, before writing a patch, which
room scripts each arena has to neutralize, and which rooms carry their own
scripted props (`Shrine_Despair`, `Shrine_BrokenWoodenDoll`,
`Primus_Ending`) that a generic patch would miss. Decompile each unfamiliar
class from `Dew.Contents` afterwards (`02-reading-the-game.md`).

### 4.8 Worked example 2: which scenes play boss music, and their GUIDs

**Problem.** Last Stand's arena played the zone's normal music. The idea:
play a random heroic-boss track instead. That needs the tracks.

**Step 1: find how boss music starts, in code.** `grep -rn "MusicManager" Dew.Core/`
leads to `Room.OnLateStart` (plays `Room.music`, or the zone's
`defaultMusicRef` when that's null and the node isn't `ExitBoss`) and to a
small scene component:

```csharp
public class FxChangeMusic : MonoBehaviour, IEffectComponent
{
	public DewMusicItem music;

	public float delay;

	public bool disableLoop;

	public void Play()
	{
		...
		ManagerBase<MusicManager>.instance.Play(music, !disableLoop);
```

No code names a boss track. The track is a serialized reference in a
scene.

**Step 2: dead end, the resource database.** `GetByName<DewMusicItem>("musBossSeeker")`
fails, and the track names aren't in `nameToGuid`/`guidToName`. Section 3.3
explains why: `DewMusicItem` is guid-linked. `FindAllByType<DewMusicItem>()`
isn't the tool either: it walks `typeToGuid`, which maps a C# type to a
single asset. That suits actors (one prefab per type), not a family of
ScriptableObjects that share one type.

**Step 3: find the components.** Section 4.7 showed `FxChangeMusic` on
GameObjects named `BossMusicStart`, `BossMusicStartSecond` and
`BossMusicEnd`. Now resolve each `music` reference. It points into the
scene's `sharedassetsN.assets`, so load the scene, that file and
`globalgamemanagers.assets` together. `sod_scene_refs.py CLASS FIELD LEVEL...`:

```python
import os, sys
import UnityPy
from sod_common import DATA, generator, script_class, game_object_name, deref

cls_name, field, levels = sys.argv[1], sys.argv[2], sys.argv[3:]
gen = generator()
for level in levels:
    n = level.removeprefix("level")
    files = [os.path.join(DATA, f) for f in (level, f"sharedassets{n}.assets", "globalgamemanagers.assets")]
    env = UnityPy.load(*[f for f in files if os.path.exists(f)])
    hits = [o for o in env.objects if o.type.name == "MonoBehaviour" and script_class(o) == cls_name]
    env.typetree_generator = gen
    for obj in hits:
        tree = obj.read_typetree()
        target = deref(obj, env, tree[field])
        ref = target.read_typetree() if target else {}
        guid = ref.get("<resourceId>k__BackingField") or ref.get("guid")
        print(f"{level}\t{game_object_name(obj, tree)}\t{field}={ref.get('m_Name')}\tguid={guid}")
```

`resourceId` is an auto-property with `[field: SerializeField]`, so Unity
stores it under the compiler's backing-field name,
`<resourceId>k__BackingField`.

```sh
sod-venv/bin/python sod_scene_refs.py FxChangeMusic music level14 level41 level68 level95 level125 level151 level154 level181
```

```
level14   BossMusicStart        music=musBossSeeker       guid=fdbb42f02a7667d4ebb445250c947bd3
level14   BossMusicEnd          music=None                guid=None
level41   BossMusicStart        music=musDespairBoss      guid=11dd0ca2f5eb7224bb06418696194032
level68   BossMusic             music=musBossForestDemon  guid=44b4082316a67a34aa42c158823ffd18
level95   BossMusicStartSecond  music=musInkBossSecond    guid=5c6e5c46604e8f8408d039c951f4cbb0
level95   BossMusicStart        music=musInkBoss          guid=add7fcbb30744e740bd82fc26d4974b7
level125  BossMusicStart        music=musBossInfernus     guid=39aeb947b3bd006458ac51da21af0536
level151  Silence Music         music=None                guid=None
level151  BossMusicStart        music=musBossPrimus       guid=b7df9f9f9b461e847aa4f227109da88b
level154  BossMusicStart        music=musBossNyx          guid=ec9380823cabf9d4e9de7843488f768f
level181  BossMusicStart        music=musBossSkoll        guid=b9c1464d58f50954c8f3fd5c1fd704c3
```

(`BossMusicEnd` and Primus's `Silence Music` have a null track: they stop the
music.) Nine tracks in eight rooms, with Ink's second phase as its own track.

**Step 4: prove they load.** Each GUID must be an Addressables key:

```sh
cd "$SOD_DATA/StreamingAssets/aa"
grep -a -c 44b4082316a67a34aa42c158823ffd18 catalog.bin    # 1
```

All nine are there. On the host, `DewResources.GetByGuid<DewMusicItem>(guid)`
then loads the track, and `ManagerBase<MusicManager>.softInstance.Play(item)`
plays it. Last Stand shipped exactly that, with the nine GUIDs in its
`GameNames` and a postfix on `Room.OnLateStart`.

**Conclusion, and why the feature was removed later.** `MusicManager` is a
plain local `ManagerBase`, not networked. The host heard the boss track,
vanilla clients kept the zone music. There's no synced channel to hand a
client a track (networked effects are addressed under a `NetworkIdentity`
that already contains them; see `04-host-only-networking.md`). A feature
only the host can hear was dropped. The data method still did its job: it
turned "where is the boss music?" into nine checked GUIDs, and made the
limit visible before more work went in.

**Where else this applies:** any `AssetRef<T>` (follow its `guid`), sound
effects (`DewAudioClip`, also guid-linked), finding the `Special_RoomAnnouncer`
`key` a special room announces with, and which rooms carry a
`PlayableDirector` (cutscene) or a `Light` that starts disabled.

### 4.9 Worked example 3: which room modifiers are main

**Problem.** Room modifiers (`RoomModifierBase` subclasses, `RoomMod_*`)
carry flags that change how the game treats them. The modifier panel
(`UI_InGame_RoomModDisplay`, Dew.UI) shows one only under this condition:

```csharp
if (a is RoomModifierBase { isMain: not false } roomModifierBase
    && (!roomModifierBase.hiddenOnVisitedNode || !SingletonDewNetworkBehaviour<Room>.instance.isRevisit))
```

and it reads the text from `<TypeName>_Name` and `<TypeName>_Description`
(section 6). Last Stand's arena counts as a revisited room. Two questions
needed the flags of every modifier at once:

1. Which modifiers can show a panel in the arena? (`isMain` and not
   `hiddenOnVisitedNode`.) Last Stand needed a harmless default panel to
   replace stale ones on vanilla clients.
2. Which ones replace each other? `ZoneManager.AddModifier` removes the
   node's other main modifier when the new one `isMain`. Adding the hunter
   zone could strip the Aura of Pain.

**Step 1: the code says nothing.** `public bool isMain;` in
`RoomModifierBase`. No subclass sets it in code.

**Step 2: all prefabs at once.**

```sh
sod-venv/bin/python sod_prefab.py RoomMod_ isMain,hiddenOnVisitedNode,disableOtherEnvParticles | sort
```

Abridged:

| Modifier | `isMain` | `hiddenOnVisitedNode` | `disableOtherEnvParticles` |
|---|---|---|---|
| `RoomMod_AcceleratedTime` | 1 | 0 | 1 |
| `RoomMod_Ambush` | 1 | 1 | 1 |
| `RoomMod_ChallengingFight` | 1 | 1 | 0 |
| `RoomMod_CorruptedChaos_AuraOfPain` | 1 | 0 | 1 |
| `RoomMod_FallenStar` | 1 | 0 | 1 |
| `RoomMod_FragmentOfRadiance_MysteriousPlace` | 1 | 0 | 0 |
| `RoomMod_GiftMerchant` | 1 | 0 | 0 |
| `RoomMod_HarderFightBetterReward` | 1 | 1 | 0 |
| `RoomMod_Hunted` | 1 | 0 | 1 |
| `RoomMod_SpawnMiniBoss` | 1 | 1 | 0 |
| `RoomMod_StardustEverywhere` | 1 | 0 | 1 |
| `RoomMod_HeroSoul`, `RoomMod_Spawn*` shrine spawners | 0 | 0 | 0 |

The same run with `endWhenRiftOpen` (or `sod_prefab.py RoomMod_Hunted`)
shows `RoomMod_Hunted.endWhenRiftOpen = 1`, plus all of its `Formula`
fields, such as `Clamp01(0.2+0.125*x)`. Its `OnStartServer` evaluates
several of them with the hunt level as `x`, for example
`addedHunterChance.Evaluate(ZoneManager.currentHuntLevel)`.

**Conclusions.**

- Panel candidates: main and not hidden on revisit. Leaving out the
  wave environments Last Stand already used, the choice came down to
  Mysterious Space (`RoomMod_FragmentOfRadiance_MysteriousPlace`), Souvenir
  Merchant, Fallen Star and Dream of the Star Seeker. Only Mysterious Space
  has no gameplay of its own, so Last Stand keeps it on the arena node
  whenever no other main modifier is active.
- `Hunted` and the Aura of Pain are both main, so adding one can remove the
  other from the arena node. Last Stand adds the hunter zone back after a
  Pain group falls mid-zone instead of assuming both stay.
- `endWhenRiftOpen = 1` explained a `NullReferenceException` in the log:
  the artillery coroutines test `endWhenRiftOpen && Rift.instance.isOpen`,
  and the arena has no rift. Last Stand sets `endWhenRiftOpen = false` on
  its own instance in `AddModifier`'s `beforePrepare` callback, before
  `OnCreate`, never on the prefab.

The `RawData` dump gives the same 37 main modifiers, but only if you grep
all of it: it's grouped by the developer's folders, not by class, and
`RoomMod_FallenStar.json` sits under `Treasures/StrayMemory/`, so a grep of
`RoomMods/` alone finds 36. The bundle scan goes by class and can't miss
one.

### 4.10 `Formula` and `PerRarityData<T>` in data

Both are `[Serializable]` structs in Dew.Core, so Unity stores them inline:

- `PerRarityData<T>` has public fields `common`, `rare`, `epic`,
  `legendary`, `character`, `identity`, `unique` and a `Get(Rarity)`
  switch. In a type tree it's a plain object with those seven keys.
- `Formula` serializes one field, `formula` (a string, with
  `[FormerlySerializedAs("forumla")]`). Everything else is private cache.
  `Evaluate(float x)` binds `x` and evaluates the string with NCalc
  (`NCalc.dll` in `Managed`) plus the game's `NCalcExtensions.ExtraNCalcFunctions`
  (`Clamp`, `Clamp01`, `Min` appear in prefabs). Simple polynomials such as
  `0+20*x` take a fast path that skips NCalc (`TryParseSimplePolynomial`).

To know what `x` means, find the `Evaluate` call in code:
`goldCostByUseCount.Evaluate(GetUseCountByPlayer(player))` in
`Shrine_MawOfDoom.GetGoldPrice`. The formula string comes from data, the
variable from code. You need both.

### 4.11 Odin-serialized data

Some assets derive from Odin Inspector's `SerializedScriptableObject` or
`SerializedMonoBehaviour` (`Sirenix.*` DLLs in `Managed`). Unity can't
serialize dictionaries, interfaces or `Type` fields, so Odin writes the
whole object into one blob and Unity stores only that. With a type tree,
`MainLocalization` reads as:

```
m_Name               'MainLocalization'
serializationData    {'SerializedFormat': 0, 'SerializedBytes': <48,704,701 bytes>,
                      'ReferencedUnityObjects': [], 'SerializationNodes': [], ...}
```

`SerializedFormat` 0 is Odin's binary format. UnityPy stops there. What
you need to read a blob for one purpose:

- It's a stream of entries. The first byte is a `BinaryEntryType` (from
  `Sirenix.Serialization.dll`; decompile the enum to get the numbers):
  `UnnamedStartOfStructNode` = 4, `EndOfNode` = 5, `NamedString` = 39
  (`0x27`), `UnnamedNull` = 46, `TypeID` = 48, ...
- A named entry is followed by its name, then its value. A string is one
  byte for the char size (`0` = 8-bit, `1` = UTF-16), an `int32` char count,
  then the chars.
- A `Dictionary<string, string>` entry is a struct node with two named
  strings, `$k` and `$v`.

For example, the bytes around a localization entry:

```
27 01 02 00 00 00 24 00 6b 00   NamedString, name "$k" (UTF-16, 2 chars)
01 2b 00 00 00 49 00 6e 00 ...  value: UTF-16, 43 chars "InGame_ThreatLevelDisplay_ThreatLevelRising"
27 01 02 00 00 00 24 00 76 00   NamedString, name "$v"
01 16 00 00 00 42 00 65 00 ...  value: UTF-16, 22 chars "Bedrohungslevel steigt"
05 04 2e                         EndOfNode, next pair: UnnamedStartOfStructNode, UnnamedNull (no type entry)
```

A full Odin reader is a project of its own. A pattern scan for the entries
you need is enough (section 6.3). When you need the whole object, read it
at runtime instead, where Odin has already deserialized it (section 3.4,
gotcha 5).

Other Odin assets: `DewResourceDatabase` (`MainResources`), and
`TravelerStoryNodeDef`.

---

## 5. GUI tools

When you'd rather browse than script:

- **AssetRipper** exports the game into a Unity-project-like folder:
  scenes and prefabs as YAML, scripts as stubs, textures and audio as files.
  Good for reading a scene's hierarchy, or grepping YAML for a field name
  across every asset at once.
- **AssetStudio** (and its maintained forks) opens the data folder or a
  bundle, lists objects by type and container path, previews textures,
  meshes and audio, and can export a MonoBehaviour as JSON when you point it
  at the `Managed` folder for type information.

Both need a build recent enough for Unity 6 serialized files. Both struggle
with the same two things UnityPy does: stripped type trees (they need the
`Managed` folder) and Odin blobs (shown as raw bytes). For repeatable
answers ("every `RoomMod_` with `isMain`") a 20-line script is faster and
gives output you can diff after a game update.

---

## 6. Localization

### 6.1 Where strings live

```csharp
public static class DewLocalization
{
	private const string MainLocalization = "MainLocalization";
	...
	public static DewLocalizationBuildData buildData
	{
		get
		{
			if (_buildData == null)
			{
				_buildData = Resources.Load<DewLocalizationBuildData>("MainLocalization");
```

`DewLocalizationBuildData` (namespace `DewInternal`) is a
`SerializedScriptableObject` with
`Dictionary<string, PerLanguageLocalizationData> dataByLanguage`, keyed
`en-US`, `de-DE`, ... Each language holds:

```csharp
public class PerLanguageLocalizationData
{
	public Dictionary<string, string> ui;
	public Dictionary<string, SkillData> skills;
	public Dictionary<string, GemData> gems;
	public Dictionary<string, AchievementData> achievements;
	public Dictionary<string, CurseData> curses;
	public Dictionary<string, string> tips;
	public Dictionary<string, StarData> stars;
	public Dictionary<string, ConversationData> conversations;
	public Dictionary<string, ArtifactData> artifacts;
	public Dictionary<string, TreasureData> treasures;
}
```

`DewLocalization.data` picks the player's language
(`DewSave.profileMain.language`, defaulting to `en-US`). On disk, the asset
lives in `resources.assets`. `globalgamemanagers`' `ResourceManager`
container maps `mainlocalization` to `{m_FileID: 4, m_PathID: 22208}`, and
file 4 is `resources.assets`.

### 6.2 How lookups and keys work

The UI table lookup, with its fallback:

```csharp
public static string GetUIValue(string key)
{
	if (key == null)
	{
		return "ui.!null";
	}
	if (data.ui.TryGetValue(key, out var value))
	{
		return value;
	}
	return "ui.!" + key;
}
```

A wrong key never throws. It shows as `ui.!<key>` on screen. `TryGetUIValue`
is the quiet version.

Key conventions, each found at its call site:

| Text | Key | Where |
|---|---|---|
| Quest title / short description | `<QuestType>_Title`, `<QuestType>_Description` | `DewQuest.OnCreate`; also the fallback when `SetLocalizedTitle`/`SetLocalizedDescription` get an empty key |
| Room modifier panel | `<ModifierType>_Name`, `<ModifierType>_Description` | `UI_InGame_RoomModDisplay.ShowModifier` |
| Special room announcer | `<Special_RoomAnnouncer.key>_Name`, `_Description` | `UI_InGame_RoomModDisplay.Refresh` |
| Hunted panel at hunt level 5 | `RoomMod_Hunted_PrimusWarnTo<Hero>_<0..2>` | `UI_InGame_RoomModDisplay.ShowModifier` |
| Skill key | type name minus its 3-char prefix (`GetSkillKey` = `skillType.Substring(3)`) | then `data.skills[key]` |
| Many UI labels | literal keys such as `InGame_SelectItemToUpgrade`, `Shrine_Ascension_AscendAction`, `Loading_LoadingMods` | `GetUIValue("...")` calls |

Some keys aren't in any code at all. `InGame_ThreatLevelDisplay_ThreatLevelRising`
appears nowhere in the decompiled assemblies (`UI_InGame_AmbientLevel` only
uses its sibling `InGame_ThreatLevelDisplay_AmbientLevel`), so whatever shows
it names the key from data, for example a `DewLocalizedText` on a UI prefab.
Code search can't find such keys. The table can.

### 6.3 Finding a key

**At runtime** (complete and exact): dump the live table.

```csharp
[ConsoleCommand("Log UI strings whose key or text contains a word", "probe_text")]
public void ProbeText(string word)
{
	foreach (KeyValuePair<string, string> entry in DewLocalization.data.ui)
	{
		if (entry.Key.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0
		    || entry.Value.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			Debug.Log($"[probe] {entry.Key} = {entry.Value}");
		}
	}
}
```

**Offline** (no game needed): scan the Odin blob for `$k`/`$v` string pairs
(section 4.11). Language codes are themselves `$k` strings, so the scan
tracks which language it's in. `sod_loc.py LANG WORD`:

```python
import os, re, sys, struct
import UnityPy
from sod_common import DATA

lang, needle = sys.argv[1], sys.argv[2].lower()

ggm = UnityPy.load(os.path.join(DATA, "globalgamemanagers"))
gfile = next(iter(ggm.objects)).assets_file
resources = next(o for o in ggm.objects if o.type.name == "ResourceManager").read_typetree()
pptr = dict(resources["m_Container"])["mainlocalization"]
holder = gfile.externals[pptr["m_FileID"] - 1].path.split("/")[-1]
env = UnityPy.load(os.path.join(DATA, holder))
raw = next(iter(env.files.values())).objects[pptr["m_PathID"]].get_raw_data()

def odin_string(buf, pos):
    wide, length = buf[pos], struct.unpack_from("<i", buf, pos + 1)[0]
    start = pos + 5
    if wide:
        return buf[start:start + 2 * length].decode("utf-16-le")
    return buf[start:start + length].decode("latin-1")

NAMED_STRING_K_OR_V = re.compile(rb"\x27\x01\x02\x00\x00\x00\$\x00([kv])\x00")
current_lang, key = None, None
for m in NAMED_STRING_K_OR_V.finditer(raw):
    value = odin_string(raw, m.end())
    if m.group(1) == b"k":
        if re.fullmatch(r"[a-z]{2}-[A-Z]{2}", value):
            current_lang = value
        key = value
    elif key is not None:
        if current_lang == lang and (needle in key.lower() or needle in value.lower()):
            print(f"{key}\t{value}")
        key = None
```

It reads the raw object bytes (no type tree needed) and runs in about a
second. It finds the pairs of every string-to-string dictionary, `ui` and
`tips` among them. Skills, gems and the
other struct-valued tables need the runtime dump. Values can be 8-bit or
UTF-16 per string; the char-size byte says which.

### 6.4 Worked example: a quest title every vanilla client can read

**Problem.** Last Stand shows the wave in the quest tracker on every
player's screen. `DewQuest.currentProgress` is a synced string that each
client prints as-is, so `Wave 12: 34 monsters left` reaches everyone. The
title and description aren't synced. Each client sets them from a key
(chapter `04-host-only-networking.md` traces this). The host can only send a
key through the `SetLocalizedTitle(string key)` and
`SetLocalizedDescription(string key)` RPCs:

```csharp
protected void UserCode_SetLocalizedTitle__String(string key)
{
	if (string.IsNullOrEmpty(key))
	{
		key = GetType().Name + "_Title";
	}
	questTitleRaw = DewLocalization.GetUIValue(key);
}
```

So the title must be a line **the game already has**, in every language,
that reads sensibly for a wave counter. Free text would show as
`ui.!Wave`.

**Step 1: the default.** Last Stand uses `Quest_Limbo` as an empty quest
class. Its own keys:

```sh
sod-venv/bin/python sod_loc.py en-US quest_limbo
# Quest_Limbo_Description   Achieve victory on your journey.
# Quest_Limbo_Title         Limbo
```

"Limbo" is wrong for this mode. Rejected.

**Step 2: search by meaning.**

```sh
sod-venv/bin/python sod_loc.py en-US "threat level"
# InGame_Hunt_HuntThreat_Tooltip_Name                Hunt Threat Level
# InGame_ThreatLevelDisplay_ThreatLevelRising        Threat Level Rising
# InGame_Tooltip_ThreatLevel_Description             The Threat Level is rising. ...
sod-venv/bin/python sod_loc.py en-US "powerful monsters"
# RoomMod_HarderFightBetterReward_Name               Powerful Monsters
```

**Step 3: check it translates.** The same key exists in every language
section (`de-DE`: "Bedrohungslevel steigt"), so each client sees it in its
own language.

**Conclusion.** Title `InGame_ThreatLevelDisplay_ThreatLevelRising`
("Threat Level Rising"), description `RoomMod_HarderFightBetterReward_Name`
("Powerful Monsters"). Both live in Last Stand's `GameNames`. RPCs aren't
replayed to late joiners, so the host sends them again when the set of
ready connections changes (`08-config-ui-and-commands.md`).

### 6.5 Localized names for your own messages

Chat notices from a host-only mod are free text: the host builds the
string, and clients print it. They aren't translated per client. To name a
game thing in its proper words, look up its key on the host. Last Stand's
helper:

```csharp
public static string Find(string typeName)
{
	return DewLocalization.TryGetUIValue(typeName + GameNames.LocalizedNameSuffix, out string localized)
		? localized
		: null;
}
```

`LocalizedNames.Find("RoomMod_GravityTraining")` returns the panel name of
that environment in the host's language. Settings labels use the same
helper (`08-config-ui-and-commands.md`).

**Where else this applies:** finding a screen's class from its visible
text (search the text, get the key, grep the key in `Dew.UI`), turning
`ui.!Something` bugs back into the key that's missing, and choosing curse or
modifier names for logs.

---

## 7. JSON overrides

### 7.1 What they are

A declarative way to change prefab fields. No C# needed, and the official
mod docs recommend it for static tuning ("character stats, damage numbers,
cooldown times"). Mechanics, checked in `Dew.Core/DewMod.cs`:

**Loading.** In `DewMod.Load(id)`:

```csharp
string text = Path.Join(modItem.path, "overrides");
if (Directory.Exists(text))
{
	loadedModInstance.isAlteringGameplay = true;
	RegisterJsonOverridesInDirectory(text);
}
```

So the folder is `<your mod folder>/overrides/`, every `*.json` in it and
its subfolders (`SearchOption.AllDirectories`). Having the folder at all
sets `isAlteringGameplay`. `RawData/!ModResources/overrides` is the
reference dump (section 2). The game doesn't load from it.

**File format.** A JSON array of `JsonOverrideItem`:

```csharp
public class JsonOverrideItem
{
	public string target;
	public Dictionary<string, string> overrides;
}
```

```json
[
  {
    "target": "Shrine_Ascension",
    "overrides": {
      "maxUseCountPerPlayer": "6",
      "addedCostMultiplierPerUse": "0.05"
    }
  }
]
```

- `target` is a short type name (`Dew.GetTypeFromShortName`) that has a
  prefab (`database.typeNameToGuid`).
- A key is a field path: dots go into structs and objects
  (`baseStats.attackDamage`), integers index arrays and lists
  (`goldMultipliers.2`). A field on another component of the prefab takes
  the component's type as a prefix: `EntityStatus::baseStats.attackDamage`.
  Without `::`, the key targets the component named like the target type.
- Values are strings. Each is parsed with `JsonConvert.DeserializeObject`
  into the field's type, and a plain string field gets quoted for you.

### 7.2 Validation: why some fields can't be overridden

Registration (`RegisterJsonOverride`) first builds the list of valid keys
for the target (`BuildJsonOverrideOriginal`), then rejects any key not on
it:

```
[DewMod-Override] Shrine_Ascension::baseGoldCostByStartRarity.common is not a valid register override key.
```

`BuildJsonOverrideOriginal(typeName)`:

1. Requires an allowed prefix:

   ```csharp
   private static readonly HashSet<string> AllowedPrefixesForJsonOverride = new HashSet<string>
   {
   	"At_", "St_", "Ai_", "Se_", "Mon_", "Ge_", "Gem_", "Hero_", "LucidDream_", "RoomMod_",
   	"Shrine_", "Treasure_"
   };
   ```

2. Loads the prefab and serializes only the `Actor`, `EntityAI`,
   `EntityStatus` and `EntityControl` components (`TryProcess<T>()` for each).
   A `Shrine_*` is an `Actor`, so its own fields are covered. A plain
   `MonoBehaviour` on the prefab isn't.
3. Uses a contract resolver, `FieldsOnlyResolver`, that keeps a member only
   if it's a field whose type is in `AllowedValueTypesForJsonOverride`, and
   isn't `syncInterval`, `budgetCost` or `excludeFromPool`.
4. Flattens nested objects and arrays into dotted keys
   (`goldMultipliers.0`).

The allowed types: `int`, `float`, `bool`, `string`, `Vector2/3/4`, their
arrays and `List<>`s, and game types `ScalingValue`, `StarScalingValue`,
`ElementalType`, `DamageData.SourceType`, `DamageAttribute`,
`LevelScaling`, `BaseStats`, `BonusStats`, `NodeModifierVisibility`,
`ModifierSpawnType`, `ScaleWithDifficultyMode`, `MapItemVisibility`,
`TriggerConfig` (and array), `CastMethodData`,
`HeroConstellationSettings`, `Dash`, `Knockback`.

That's why:

- **`PerRarityData<T>` can't be overridden.** It isn't in the set, so the
  resolver drops `baseGoldCostByStartRarity` entirely. None of its sub-keys
  ever become valid.
- **`Formula` can't be overridden.** Same reason, even though its only
  serialized field is a `string`. The resolver never looks inside a field
  whose type isn't allowed, so `goldCostByUseCount.formula` is never a
  valid key. The setter (`JsonOverrideHelper.Apply`) could write it; the
  whitelist stops it first.
- **`[NonSerialized]` state can't be overridden.** For example
  `Shrine_MawOfDoom.priceMultipliersByPlayer`. It's runtime state, not
  prefab data.

The `RawData` dump is exactly this whitelist with current values, which is
why checking it first (section 2) tells you in seconds whether an override
is possible at all.

### 7.3 Application: variants, not prefab edits

Registration doesn't touch the prefab. It stores a callback per prefab
GUID in `DewMod.currentJsonOverrideProcessorsLocal` (or `...FromServer`,
below). When something loads that GUID, `DewResources.GetVariant` sees that
an override applies, clones the prefab under a hidden
`Resource Variants` GameObject and runs the callbacks on the clone. Every
later load gets the clone. So:

- The original asset stays clean, and unloading the mod clears the
  variants.
- A runtime read (section 3.4) shows the override, while an offline read
  shows the shipped value.

### 7.4 Sync: how clients get them

- **Host side.** Local overrides apply only when
  `NetworkServer.active || (!NetworkServer.active && !NetworkClient.active && !NetworkClient.isConnecting)`,
  meaning when hosting or offline. A client connected to someone else
  ignores its own local overrides.
- **Client side.** In `DewPlayer.OnStartClient`, a non-host client in a
  modded lobby (`currentLobby.isModded`) calls `CmdRequestOverrides()`. The
  host answers with `TpcSendOverrides(DewMod.allJsonOverrideItemsLocal)`,
  and the client registers each item with `isFromServer: true`, behind a
  loading screen (`Loading_ApplyingModsFromServer`).
- **Cleanup.** `DewNetworkManager.OnDestroy` clears the variants of all
  server-sent overrides and empties `currentJsonOverrideProcessorsFromServer`.

The client doesn't need the mod installed. The override is data the host
sends, applied by vanilla code. That makes an override the **one way** to
change an *unsynced* prefab field on a vanilla client, such as
`Shrine.maxUseCountPerPlayer`, which code can't do (section 1.3).

### 7.5 Limits

- **Every run.** Overrides are registered when the mod loads and stay until
  it unloads. Nothing ties them to a game mode, a lobby setting or a wave.
  A mod that offers its own mode next to vanilla changes vanilla runs too.
- **Static.** Values are fixed text in files. They can't react to config
  or game state (the docs' own "cons" list).
- **Join-time only.** Clients request the list once, in `OnStartClient`.
  `DewMod.StartRegisterJsonOverride`, `RegisterJsonOverride(JsonOverrideItem)`
  and `EndRegisterJsonOverride` are public, so a mod could register more at
  runtime, but clients already in the game wouldn't ask again. Last Stand
  never tried this; treat it as untested.
- **Prefab fields of allowed types on the four component kinds only.**

### 7.6 Overrides or code?

| Need | Overrides | Code (Harmony, host writes) |
|---|---|---|
| A fixed balance change for every run while the mod is loaded | best fit | possible, more work |
| A change only in your mode, or driven by a setting | no | yes |
| Values that change during a run (per wave, per player) | no | yes, through synced state |
| An unsynced prefab field that clients compute with | **yes**, reaches vanilla clients | no: the host's write stays on the host |
| `Formula`, `PerRarityData<T>`, non-actor components, `[NonSerialized]` state | no | yes, if the result reaches clients through synced state |
| Leaving vanilla runs untouched | no | yes |

**What Last Stand chose.** Every Last Stand change applies only in its own
mode, so the mod has no `overrides` folder. Its docs say so for gold prices:
"None of this is a JSON override, so vanilla runs are untouched." Where an
override was the obvious tool, there was a code route through synced
state:

- More Shrine of Ascension uses per wave: instead of overriding
  `maxUseCountPerPlayer`, clear the synced per-player use counts
  (`Shrine._useCountByPlayer`, a `SyncDictionary`) when a wave is cleared.
- Shrine prices: the Ascension's table can't be overridden anyway
  (`PerRarityData`), so Last Stand scales the synced
  `GameManager.goldCostMultiplier`, which every client's price formula
  multiplies in.
- The hunter zone's `endWhenRiftOpen`: set on the host's instance before
  `OnCreate`. Only the host's routines read it, so clients don't need to
  agree.

**Where else this applies:** retuning a monster's `EntityStatus::baseStats`
for a balance mod, a hero's `St_` cooldown, a status effect's duration, and
deciding early that a requested change belongs in code because the field
is a `Formula`.

---

## 8. The procedure, in order

For any "what is this number, and can I change it" question:

1. **Find the field in code** (`02-reading-the-game.md`). Note its type, its
   class, whether it's `[SyncVar]`, and where it's read (who computes with
   it: host, client or both).
2. **Check the dump.** `grep -r '"<field>"' "<GameFolder>/RawData/!ModResources/overrides"`.
   If it's there, you have the shipped value and you know it's overridable.
3. **Not there?** Read the bundle (`sod_prefab.py <Class>`) for prefabs and
   ScriptableObjects, or the scene (`sod_scene_scripts.py`,
   `sod_scene_refs.py`) for room content and references.
4. **Confirm in the running game** with a probe command (section 3.4),
   especially when an override or a patch could be involved.
5. **For text**, search the localization (section 6.3) and reuse an existing
   key. Never send free text where a client expects a key.
6. **Decide how to change it** with the table in 7.6. If clients compute
   with it and it isn't synced, the choices are: keep the shipped value, use
   an override for every run, or find a synced input upstream
   (`04-host-only-networking.md`).
7. **Write the value down with its source.** Last Stand's working notes
   list each prefab value with "read from the asset bundle", so nobody has
   to prove it twice. Rerun the scripts after a game update to catch
   changes.
