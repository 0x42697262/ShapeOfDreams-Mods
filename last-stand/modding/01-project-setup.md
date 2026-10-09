# 1. From nothing to a loaded mod

This chapter takes you from an empty folder to a DLL the game loads, logs
from, and unloads cleanly. Every fact about the loader comes from reading the
game's own code, and each section shows where that code is and how to find
it. The point is not the list of facts. The point is the habit: whenever the
documentation is silent (and for this game it usually is), open the code path
that does the work and read it.

Prerequisites: the .NET SDK (any version that can build `netstandard2.1`),
a decompiler (ILSpy, dnSpyEx, or the `ilspycmd` command-line tool), and the
game installed through Steam. How to decompile the game into searchable files
is covered in `02-reading-the-game.md`; this chapter only uses `grep` over
that output.

Paths used below:

| Name | Meaning |
|---|---|
| `<GameFolder>` | The install folder, e.g. `steamapps/common/Shape of Dreams` |
| `<Managed>` | `<GameFolder>/Shape of Dreams_Data/Managed` |
| `<DataDir>` | Unity's `Application.persistentDataPath`: `%USERPROFILE%\AppData\LocalLow\Lizard Smoothie\Shape of Dreams` on Windows; under Proton, `steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams` |

---

## 1. Identify what kind of game you are modding

**Technique: read the install folder before reading any code.** A Unity game
is built with one of two scripting backends, and the backend decides whether
C# modding is even possible in the usual way.

- **Mono**: game code ships as .NET IL assemblies in `<Managed>`, run by a
  Mono runtime (a `MonoBleedingEdge` folder next to the exe). IL can be
  decompiled back to near-original C#, a new assembly can be loaded at run
  time, and Harmony can patch methods.
- **IL2CPP**: game code is compiled to native code in `GameAssembly.dll`;
  there is no IL to decompile and loading a C# DLL needs a special loader.

Procedure:

1. List `<GameFolder>`. Shape of Dreams has `MonoBleedingEdge/`,
   `Shape of Dreams_Data/Managed/` (about 350 DLLs), `Mods/`, `RawData/` and
   `version.txt`. No `GameAssembly.dll`. So: Mono.
2. Find where the game's own code is. Unity's default assembly is
   `Assembly-CSharp.dll`, but here it is only about 90 KB. List the
   non-Unity, non-system DLLs in `<Managed>` and sort by size; the game code is
   in `Dew.Core.dll`, `Dew.Contents.dll`, `Dew.UI.dll` and `Dew.External.dll`
   (about 2.5 MB each). `Shape of Dreams_Data/ScriptingAssemblies.json` lists
   every assembly Unity loads, which confirms the names.
3. Read the engine version from the log (section 6 says where it is):
   ```
   Initialize engine version: 6000.0.77f1 (88f89d0d8b31)
   ```
   That is Unity 6. It matters when you look up Unity API behavior.
4. Read the game version. Four places agree:
   - `<GameFolder>/version.txt` (e.g. `r.1.4.0.13_s`);
   - the `== System Report START ==` block at the top of `Player.log`,
     written by `GlobalLogicPackage.Awake` as `Version: {Application.version}`;
   - the label on the title screen (`UI_ApplicationVersion` sets its text to
     `Application.version`);
   - the console command `build` (`DewConsoleCommands.Build`, prints
     `Showing build profile used for '<version>'`).

The game turns that string into a multiplayer compatibility version:

```csharp
public static string GetMultiplayerCompatibilityVersion(string bundleVersion)
{
	if (bundleVersion.Contains("_"))
	{
		return bundleVersion.Split("_")[0];
	}
	return bundleVersion;
}
```

`LobbyServiceProvider.SetBasicAttributes` writes it into the lobby as
`"version"`, and joining compares it with the client's own. Mods do not take
part in that check: a vanilla client on `r.1.4.0.13` can join a modded host on
`r.1.4.0.13`. That fact is the foundation of host-only mods
(`04-host-only-networking.md`).

**Where else this applies.**

- Before using any Unity API, check it exists in Unity 6 and in the
  `UnityEngine.*Module.dll` files present in `<Managed>`.
- After every game update, compare `version.txt` with the version you last
  tested; the loader never refuses a mod for being old (section 2.3).
- A `RawData/` folder ships with this game (localization, sprites, and the
  JSON override reference in `RawData/!ModResources/overrides`). Look for
  such folders in any game before extracting assets yourself.

---

## 2. What a mod is to this game

### 2.1 The folder

The official template (`<GameFolder>/Mods/ModTemplate`) shows the shape:

```
Mods/
  MyMod/
    about/
      metadata.json      required
      description.txt    optional, BBCode, shown in the mod manager and on the Workshop
      icon.png           optional
      preview.png        optional, Workshop preview image
      publishedfileid.txt  written by the first Workshop upload
    MyMod.csproj
    MyMod.cs
    bin/Debug/netstandard2.1/MyMod.dll
```

A sample folder only shows one valid layout. The rules are in the code that
reads it, so read that next.

### 2.2 Find the reader, not the sample

**Technique: start from a fixed string you already know and search the
decompile for it.** The file name `metadata.json` cannot be renamed by an
obfuscator or a refactor; it has to appear as a literal in whatever reads it.

```sh
grep -rln '"metadata.json"' Dew.Core Dew.UI Dew.External
```

One hit: `Dew.Core/DewMod.cs`, method `AddMod`. Trimmed:

```csharp
private static void AddMod(string modPath, ModSourceType source, ulong publishedFileId = 0uL)
{
	string path2 = Path.Join(modPath, "about", "metadata.json");
	if (!File.Exists(path2) || Directory.Exists(Path.Join(modPath, ".template.config")))
	{
		return;
	}
	...
	item.metadata = JsonUtility.FromJson<ModMetadata>(File.ReadAllText(path2));
	item.assemblyPaths = (from rel in (from p in Directory.GetFiles(modPath, "*.*", SearchOption.AllDirectories)
			select ConvertToRelativeForwardSlashPath(p, modPath) into path
			where item.metadata.assemblies.Any((string pattern) => path.EqualsWildcard(pattern))
			select path).Distinct()
		select Path.GetFullPath(Path.Join(modPath, rel))).ToArray();
	AssertRequiredFieldMetadata("id");
	AssertRequiredFieldMetadata("name");
	AssertRequiredFieldMetadata("author");
	if (item.state == ModItemState.Ok && allMods.Find((ModItem m) => m.metadata.id == item.metadata.id && m != item) != null)
	{
		item.state = ModItemState.IdConflict;
	}
	...
	catch (Exception exception)
	{
		Debug.LogException(exception);
		item.state = ModItemState.InvalidMetadata;
	}
```

and the target type, `Dew.Core/ModMetadata.cs`, in full:

```csharp
public class ModMetadata
{
	public string id;
	public string name;
	public string author;
	public string modVer;
	public string[] assemblies;
}
```

What this proves, line by line:

- **The mod root is any direct child of a mods directory that has
  `about/metadata.json`.** `AddAllModsInDirectory` uses
  `Directory.GetDirectories(modsDir, "*", SearchOption.TopDirectoryOnly)`, so
  `Mods/MyMod/MyMod/about/metadata.json` (one level too deep) is invisible.
- **A folder containing `.template.config/` is skipped.** That is how the
  template hides itself from the mod manager. If you copy the whole
  `ModTemplate` folder by hand instead of instantiating it, your copy is
  skipped too. Delete `.template.config` from the copy.
- **The parser is Unity's `JsonUtility`, not Newtonsoft.** It fills public
  fields by name and silently drops unknown keys. Keep the file plain,
  strict JSON. A parse error lands in the `catch` and marks the mod
  `InvalidMetadata`.
- **`id`, `name` and `author` are required.** An empty or missing one sets
  `InvalidMetadata`. `assemblies` is required in practice: if it is missing,
  `item.metadata.assemblies.Any(...)` throws on `null` and the mod is marked
  `InvalidMetadata` by the same `catch`.
- **`assemblies` entries are wildcard patterns, matched against every file
  under the mod root**, after converting each path to a relative path with
  forward slashes (`ConvertToRelativeForwardSlashPath`). The matcher is
  `WildcardMatch.EqualsWildcard` in `Dew.Core/WildcardMatch.cs`: `*` and `?`
  only, case-sensitive, and `*` also matches `/`. So
  `bin/Debug/netstandard2.1/MyMod.dll` is a path relative to the mod folder,
  never to `about/`, and `bin\Debug\...` with backslashes never matches.
- **Duplicate ids.** The first folder found with an id keeps state `Ok`; every
  later one becomes `IdConflict`. Local folders are scanned before Steam
  subscriptions (`Refresh` calls `GetAllLocalModDirectories` first, then
  `AddSteamSubscribedMods`), so a local development copy wins over your own
  subscribed Workshop item, which shows an error in the mod manager.
- **Where mods are found.** `GetAllLocalModDirectories` returns only the
  `-moddir <path>` launch option if it is set and exists, otherwise
  `../Mods` and `Mods` relative to the process working directory (in a normal
  Steam launch, `Mods` is `<GameFolder>/Mods`). Then Steam adds every
  subscribed and installed Workshop item.

The other `about/` files are read in the same method: `preview.png` and
`icon.png` by path, `description.txt` converted from BBCode to TextMeshPro
rich text by `ConvertBBToRichText` (tables, images and links are flattened
or dropped in the in-game manager), and `publishedfileid.txt` parsed as a
`ulong` (see `10-shipping.md`).

### 2.3 Worked example: is `supportedGameVer` checked?

The template's `metadata.json` has a key the code above never mentioned:

```json
{
  "id": "com.yourname.testmod",
  "name": "TestMod",
  "author": "AuthorName",
  "modVer": "1.0",
  "supportedGameVer": [
    "*"
  ],
  "assemblies": [
    "bin/Debug/netstandard2.1/TestMod.dll"
  ]
}
```

A version gate would matter a lot: if the game refused mods for other
versions, every game update would need a metadata bump. Proving a negative
needs more than "I didn't see it", so check three independent things.

1. **The type.** `ModMetadata` has no `supportedGameVer` field. `JsonUtility`
   only fills fields that exist, so the key is dropped at parse time.
2. **A second parser.** Something else could read the file with Newtonsoft
   or by hand. Every reader would need the key name as a string literal.
   Search the decompile:
   ```sh
   grep -rn "supportedGameVer" Dew.Core Dew.UI Dew.External
   ```
   No hits.
3. **The binaries themselves**, in case a type did not decompile (one of the
   game's assemblies does not export as a whole; see `02-reading-the-game.md`).
   .NET stores string literals as UTF-16 (the `#US` heap) and member names as
   UTF-8 (the `#Strings` heap), so search both encodings in every assembly:
   ```sh
   cd "<Managed>"
   for f in *.dll; do
     grep -q -a -F "supportedGameVer" "$f" && echo "utf8: $f"
     strings -el "$f" | grep -q "supportedGameVer" && echo "utf16: $f"
   done
   ```
   No output for any of the roughly 350 DLLs.

Conclusion: `supportedGameVer` is ignored by this game version. Last Stand
keeps `[ "*" ]` because the template has it and a future loader might read
it, but nothing stops an outdated mod from loading. Your mod has to survive
game updates on its own: see `09-debugging-and-testing.md` for re-checking
after an update.

The same three-step method (type, string search in the decompile, string
search in the binaries) answers "does anything use X?" for any key, setting
or file name.

### 2.4 The metadata, field by field

| Key | Read by | Notes |
|---|---|---|
| `id` | `AddMod`, `Load`, `ModBehaviour.harmony`, config paths, `activeMods` | Required. Unique. Use a reverse-domain id such as `com.example.mymod`. It names the container GameObject, the config folder and the Harmony id prefix, so never change it after release: players' saved config and active-mod list are keyed by it |
| `name` | mod manager, Workshop title (`SteamUGC.SetItemTitle`) | Required |
| `author` | mod manager | Required |
| `modVer` | nothing in the loader | Free text; read it back from `mod.metadata.modVer` for your own log line |
| `assemblies` | `AddMod` → `ModItem.assemblyPaths` → `Load` | Wildcard patterns relative to the mod root, forward slashes |
| `supportedGameVer` | nothing | Ignored (section 2.3) |

Last Stand's file differs from the template only in its own `id`, `name`,
`author`, `modVer` and its DLL path,
`"assemblies": [ "bin/Debug/netstandard2.1/LastStand.dll" ]`.

**Where else this applies.**

- `about/description.txt` formatting: read `ConvertBBToRichText` to see which
  BBCode tags survive in the game (headings, bold, italic, lists) and which
  are stripped (tables, images, URLs keep only their text).
- Any file the game reads from your folder: search for the literal file name
  (`"overrides"`, `"publishedfileid.txt"`) the same way.
- Error states in the mod manager: `UI_ModManager_Item` shows the tooltip
  `ModManager_ModState_{mod.state}`, so the three values of `ModItemState`
  (`Ok`, `InvalidMetadata`, `IdConflict`) are all there is.

---

## 3. The project

### 3.1 Instantiate the official template

`<GameFolder>/Mods/ModTemplate/.template.config/template.json` makes the
folder a `dotnet new` template:

```json
"shortName": "sodmod",
"sourceName": "TestMod",
"symbols": {
  "ModId":     { "type": "parameter", "replaces": "com.yourname.testmod", "defaultValue": "com.yourname.yourmod" },
  "ModAuthor": { "type": "parameter", "replaces": "AuthorName", "defaultValue": "Your Name" }
}
```

`sourceName` means every `TestMod` in file names and contents is replaced by
the project name. From a shell:

```sh
dotnet new install "<GameFolder>/Mods/ModTemplate"
cd "<GameFolder>/Mods"
dotnet new sodmod -n MyMod --ModId com.example.mymod --ModAuthor "Example"
```

In Visual Studio or Rider, install the template the same way and create the
project in `Mods/` with **"Place solution and project in the same folder"**
checked. That checkbox matters because of one line in the project file:

```xml
<GameDir>..\..</GameDir>
<GameManagedDir>$(GameDir)\Shape of Dreams_Data\Managed</GameManagedDir>
```

`..\..` from `<GameFolder>/Mods/MyMod/` is `<GameFolder>`. With the solution
in its own folder, the project sits one level deeper, `..\..` resolves to
`Mods/`, and every reference is missing. If you keep the project elsewhere,
override the property instead: `dotnet build -p:GameDir="<GameFolder>"`.
MSBuild accepts the backslashes on Linux too.

### 3.2 Read the project file

The template's `TestMod.csproj`, condensed:

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.1</TargetFramework>
  <LangVersion>9.0</LangVersion>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <NoStdLib>true</NoStdLib>
  <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  ...
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <OutputPath>bin\Debug\</OutputPath>
  ...
</PropertyGroup>
<ItemGroup>
  <Reference Include="mscorlib">
    <HintPath>$(GameManagedDir)\mscorlib.dll</HintPath>
    <Private>False</Private>
  </Reference>
  ...
</ItemGroup>
```

What each part does:

| Setting | Effect |
|---|---|
| `TargetFramework netstandard2.1` | The API surface Unity's Mono profile implements. The SDK appends the framework to `OutputPath`, which gives `bin/Debug/netstandard2.1/` |
| `NoStdLib` + `DisableImplicitFrameworkReferences` | Do not compile against the SDK's reference assemblies; compile against the game's own `mscorlib.dll`, `netstandard.dll`, `System.dll`, `System.Core.dll` from `<Managed>` instead. What compiles is then what exists at run time |
| `HintPath` into `<Managed>` | Every game and Unity assembly is referenced straight from the install. No NuGet packages, no copies |
| `Private False` | Do not copy the referenced DLL into `bin/`. The game already has it loaded; a second copy next to your DLL is at best dead weight. The build output is your DLL, its `.pdb` and a `.deps.json` |
| `LangVersion` | C# version. The template says `9.0` |

**C# version gotcha.** Last Stand sets `<LangVersion>latest</LangVersion>`
because it uses file-scoped namespaces (`namespace LastStand;`), which need
C# 10. Raising the language version is safe for syntax-only features. Features
that need runtime types fail on `netstandard2.1`: for example `init`
accessors and `record` types need
`System.Runtime.CompilerServices.IsExternalInit`, which is not in the game's
`mscorlib`. The usual fix is to declare that empty class yourself in your
assembly, or to avoid the feature.

### 3.3 Which references you need

The template references about 60 assemblies. You need a reference whenever
your code names a type from that assembly, or a type you use exposes one in
its signature. The compiler tells you which: error **CS0012** ("The type 'X'
is defined in an assembly that is not referenced. You must add a reference to
assembly 'Y'") names the missing DLL. Add a `Reference` with a `HintPath` into
`$(GameManagedDir)` and `Private False`.

To find which DLL defines a type you saw in a decompile, search the decompile
folders by file name, since the export writes one file per type:

```sh
find . -name "ModBehaviour.cs"                          # ./Dew.Core/ModBehaviour.cs
grep -rl "class DebugLogManager" Dew.Core Dew.UI Dew.External   # Dew.External/IngameDebugConsole/
```

The ones that matter for almost every mod:

| Assembly | What is in it | Example types |
|---|---|---|
| `Dew.Core` | Mod loader, managers, entities, actors, shrines, networking base classes | `ModBehaviour`, `DewMod`, `GameManager`, `ZoneManager`, `Entity`, `Shrine`, `DewPlayer` |
| `Dew.Contents` | Concrete content: monsters, skills, essences, room modifiers, most shrines | `Mon_*`, `St_*`, `Gem_*`, `RoomMod_*`, `Shrine_Destiny` |
| `Dew.UI` | Every `UI_*` component | `UI_ModManager`, `UI_Title_CreateLobbyView` |
| `Dew.External` | Third-party code the studio vendored | `IngameDebugConsole.DebugLogConsole`, `DebugLogManager` |
| `Mirror`, `Mirror.Components` | Networking | `NetworkServer`, `NetworkBehaviour`, `SyncVar` |
| `0Harmony` | Harmony 2 (`HarmonyLib` namespace) | `Harmony`, `HarmonyPatch`, `AccessTools` |
| `UnityEngine.CoreModule` | Core Unity API | `MonoBehaviour`, `GameObject`, `Debug`, `Time` |
| `UnityEngine.UI`, `Unity.TextMeshPro` | uGUI and TextMeshPro | `Button`, `ScrollRect`, `TextMeshProUGUI` |
| `Newtonsoft.Json` | JSON (the game's `DewPersistence` uses it for saves and mod config) | `JObject`, `JsonConvert` |
| `UniTask` | Async used by the game | `UniTask` |

Game types live in the **global namespace** (the decompiled `ModBehaviour.cs`
has no `namespace` line), so no `using` is needed for them.

Last Stand's `LastStand.csproj` is the template plus four changes, found by
diffing the two files (strip `\r` first; the template has Windows line
endings):

```sh
diff <(tr -d '\r' < Mods/ModTemplate/TestMod.csproj) <(tr -d '\r' < LastStand.csproj)
```

- `LangVersion latest` (file-scoped namespaces);
- `RootNamespace LastStand`;
- `UnityEngine.AIModule`, for `NavMesh.SamplePosition` and `NavMeshPath`
  when it places shrines on the arena floor (`src/Arena/EdgePlacement.cs`);
- `UnityEngine.UIModule` and `UnityUIExtensions`, added for the settings
  window's UI code.

**Where else this applies.**

- Any new compiler error about a missing type: read the assembly name in
  CS0012 and add exactly that reference.
- Keeping one mod building across several machines: `GameDir` is the only
  machine-specific value, so pass it with `-p:GameDir=...` instead of
  editing every `HintPath`.

---

## 4. How the game loads a mod: reading the loader

The official documentation says DLLs are "injected" and that "ModBehaviours
will be your main entry point". To write code that behaves, you need the
exact order of events. That order is in one class.

### 4.1 Find the loader from the type you inherit

**Technique: a base class you must inherit is a thread back to the code
that instantiates it.** Something has to create your subclass, and it can
only do so by reflection, which means `typeof(ModBehaviour)` appears in it.

```sh
grep -rn "typeof(ModBehaviour)" Dew.Core Dew.UI Dew.External
```

Hit: `Dew.Core/DewMod.cs`, in `public static void Load(string id)`. Then walk
outward with the same search on each method name:

```sh
grep -rn "DewMod.Load(\|[^.]Load(id)\|ReloadFromActiveMods" Dew.Core Dew.UI
```

That gives the call graph:

```
[RuntimeInitializeOnLoadMethod(AfterSceneLoad)] DewMod.OnInit
  └─ Refresh()                      scan folders and Workshop items into allMods
  └─ coroutine Routine
       wait until the scene isn't "Splash"/"Intro" and no transition is loading
       if any mod is active: force enableCrossPlay = false
       if activeMods not empty: 2 s prompt (cancel: hold Q or gamepad North)
       └─ ReloadFromActiveMods()
UI_ModManager.Apply  (Apply button, only when the active list changed)
  └─ DewMod.ReloadFromActiveMods()
auto-reload CheckRoutine (section 7.2)
  └─ Refresh(); ReloadFromActiveMods()
ReloadFromActiveMods → coroutine:
  StartRegisterJsonOverride
  UnloadAll()                       every loaded mod goes away first
  for each id in DewSave.platformSettings.activeMods (in order):
      yield one frame; Load(id)
  EndRegisterJsonOverride
```

`[RuntimeInitializeOnLoadMethod]` is a Unity attribute: the static method
runs automatically at a given point of engine startup. Search for it whenever
you need to know what a game class does before any scene logic runs.

Two consequences already:

- Only ids in `activeMods` load. A mod that shows in the manager but was
  never marked active and applied does nothing. The list lives in
  `<DataDir>/QuickSave/r_platform.json` (`DewPlatformSettings.activeMods`;
  the `r_` prefix is `DewBuildProfile.current.savePrefix`).
- Every reload unloads **all** mods and loads all active ones again, one per
  frame, in list order.

### 4.2 `Load`, step by step

Trimmed from `DewMod.Load`:

```csharp
ModItem modItem = allMods.Find((ModItem item) => item.metadata.id == id && item.state == ModItemState.Ok);
...
GameObject gameObject = new GameObject(modItem.metadata.id);
gameObject.SetActive(value: false);
UnityEngine.Object.DontDestroyOnLoad(gameObject);
LoadedModInstance loadedModInstance = new LoadedModInstance { mod = modItem, container = gameObject };
loadedInstances.Add(loadedModInstance);
string text = Path.Join(modItem.path, "overrides");
if (Directory.Exists(text))
{
	loadedModInstance.isAlteringGameplay = true;
	RegisterJsonOverridesInDirectory(text);
}
foreach (string text2 in modItem.assemblyPaths)
{
	if (text2.EndsWith(".dll"))
	{
		byte[] array = File.ReadAllBytes(text2);
		using (AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(array)))
		{
			AssemblyNameDefinition name = assemblyDefinition.Name;
			name.Name = name.Name + "_" + DateTime.Now.Ticks;
			...
			array = memoryStream.ToArray();
		}
		foreach (Type item in from t in Assembly.Load(array).GetTypes()
			where t != typeof(ModBehaviour) && typeof(ModBehaviour).IsAssignableFrom(t)
			select t)
		{
			if (item.IsAbstract) continue;
			ModBehaviour modBehaviour = (ModBehaviour)gameObject.AddComponent(item);
			modBehaviour.instance = loadedModInstance;
			if (modBehaviour.modConfigFields.Length != 0)
			{
				modBehaviour.LoadConfigsToDisk();
			}
			// every method with [ConsoleCommand] → RegisterConsoleCommand
			Debug.Log("[DewMod] Initialized " + modItem.metadata.id + "::" + item.Name);
		}
	}
	else
	{
		Debug.LogError("[DewMod] Unknown assembly type: " + modItem.metadata.id + "::" + Path.GetFileName(text2));
	}
}
Debug.Log("[DewMod] Activating " + modItem.metadata.id);
gameObject.SetActive(value: true);
```

Read it with Unity's rules in mind. A `MonoBehaviour` is a component on a
`GameObject`; Unity calls its message methods by name: `Awake` once when the
component first becomes active, `OnEnable` each time it is enabled, `Start`
once before its first `Update`, `Update` every frame, `OnDisable` and
`OnDestroy` when it goes away. A component added to an **inactive**
GameObject gets no `Awake` until the object is activated.

### 4.3 When does my code run?

Putting the excerpt and those rules together:

| Step | What runs | What is available |
|---|---|---|
| 1 | Your constructor and field initializers, inside `AddComponent` | Nothing from the loader yet: `instance` is still null, so `mod`, `harmony` and config paths throw |
| 2 | `instance` assigned | `mod.metadata`, `harmony` |
| 3 | `LoadConfigsToDisk()` (virtual), only if the class has public `ModConfig` fields | Your config fields are replaced by the saved file, or the defaults are written to disk |
| 4 | `[ConsoleCommand]` methods registered; `[DewMod] Initialized <id>::<Type>` logged | Repeat 1 to 4 for every `ModBehaviour` type in every listed DLL |
| 5 | `[DewMod] Activating <id>`, then `SetActive(true)` | `Awake`, then `OnEnable`, of each component, in the order they were added |
| 6 | Next frame | `Start`, then `Update` every frame |

So in `Awake` you already have `instance`, `mod.metadata`, the loaded config
and the `harmony` property. The template uses `Awake` for `harmony.PatchAll()`;
Last Stand uses `Start`. Both work.

Other facts the excerpt settles:

- **One container per mod, all `ModBehaviour`s on it.** The GameObject is
  named after the mod id and marked `DontDestroyOnLoad`, so it survives scene
  changes. Last Stand has two `ModBehaviour`s, `LastStandMod` (lifecycle) and
  `LastStandConsole` (console commands); the log shows them in `GetTypes()`
  order, `LastStandConsole` first. Don't rely on the order between your own
  behaviors.
- **Abstract subclasses are skipped**, so a shared abstract base
  `ModBehaviour` is fine.
- **The assembly is renamed every load** (`LastStand` becomes
  `LastStand_638...`) and loaded from bytes. Mono never unloads an assembly,
  so every reload adds a new copy with its own **static fields, starting
  fresh**. Anything keyed on the assembly name breaks across reloads.
  `AppDomain.CurrentDomain.SetData` is shared by all copies, and that is the
  one place Last Stand keeps state that must survive a reload
  (`src/Session/ProgressStore.cs`, section 5.4).
- **The DLL is read with `File.ReadAllBytes`, not loaded from its path**, so
  the file is not locked: you can rebuild while the game runs. Only the DLL
  bytes are passed to `Assembly.Load`; the `.pdb` beside it is not.
- **Ship one DLL.** Every listed DLL is renamed the same way. A second DLL of
  yours that the first references by its original name would not match the
  renamed copy (inferred from the rename; not tested). Last Stand ships a
  single assembly and uses only libraries the game already loads (Harmony,
  Newtonsoft, Mirror, UniTask).
- **A pattern that matches non-DLL files logs an error.** `bin/Debug/*` also
  matches the `.pdb` and `.deps.json`, and each produces
  `[DewMod] Unknown assembly type`. List the exact DLL path.
- **Errors are contained per type.** An exception in `AddComponent`,
  `LoadConfigsToDisk` or command registration is logged as
  `[DewMod] Failed to load <id>::<Type> due to exception: ...` and the next
  type is tried. An exception in `Awake` or `Start` is caught by Unity, logged,
  and the mod stays "loaded" in a half-initialized state.

### 4.4 Unloading

`UnloadAll`, trimmed:

```csharp
UnityEngine.Object.Destroy(loadedModInstance.container);
foreach (string registeredCommand in loadedModInstance.registeredCommands)
{
	DebugLogConsole.RemoveCommand(registeredCommand);
}
foreach (ModBehaviour.ManagerPatchItem managerPatch in components[i]._managerPatches)
{
	if (managerPatch.hasStarted)
	{
		managerPatch.hasStarted = false;
		managerPatch.onCleanup?.Invoke();
		managerPatch.onCleanup = null;
	}
}
...
loadedInstances = new List<LoadedModInstance>();
```

`Object.Destroy` is deferred to the end of the frame, so console commands and
manager cleanups (section 4.5) are removed first, then your `OnDisable` and
`OnDestroy` run. `ReloadFromActiveMods` yields a frame before loading again,
so the old instance's `OnDestroy` always finishes before the new instance's
`Awake`. On quit, `Application.quitting` calls `UnloadAll` too.

What the loader does **not** undo, because it never knew about it:

- Harmony patches. Nothing in `UnloadAll` touches Harmony. You unpatch in
  `OnDestroy` (section 5.2).
- GameObjects you created outside your container (UI under the game's
  canvases, objects in the scene, networked actors).
- Entries you added to the game's static lists and events
  (`DewGUI.fieldBuilders`, `SafeAction` events, handlers on entities).
- Game state you changed (prices, synced fields, settings).

### 4.5 Manager hooks: `CallOnManager` and `CallOnNetworkedManager`

`ModBehaviour.cs` has more than the lifecycle; read its public members once.
The most useful are the manager hooks. The game's singletons derive from
`ManagerBase<T>` (local) or `NetworkedManagerBase<T>` (networked). Both call
`DewMod.NotifyManagerLifecycle`: `ManagerBase` from `OnEnable`/`OnDisable`,
`NetworkedManagerBase` from Mirror's `OnStartClient`/`OnStopClient`. A
`ModBehaviour` registers interest with:

```csharp
public void CallOnNetworkedManager<T>(Action<T> onStart) where T : NetworkedManagerBase<T>
```

`InitManagerPatchItem` runs `onStart` at once if the manager already exists,
and `NotifyManagerLifecycle` runs it again every time a manager of that type
starts. Last Stand uses it to reset its session at the start of every game:

```csharp
CallOnNetworkedManager<GameManager>(OnGameManagerStarted);
```

`OnStartClient` runs on every client, the host included, so the callback
fires on a pure client too. A host-only mod must check that it is the server
inside the callback (`04-host-only-networking.md`).

The same file also gives `MakeSureServer()`, `MakeSureInGame()` and
`RegisterConsoleCommand`, covered in `08-config-ui-and-commands.md`.

**Where else this applies.**

- "When does X run relative to Y?" for any game system: find the
  `[RuntimeInitializeOnLoadMethod]`, coroutine or Unity message that drives
  it, and lay its steps beside Unity's lifecycle rules as in the table above.
- Finding every caller of a method: grep for the method name with and
  without the class prefix, because the decompiler omits the class inside its
  own file (`Load(id)` inside `DewMod.cs`, `DewMod.Load(` elsewhere).
- Reading a framework class top to bottom (`ModBehaviour.cs` is 343 lines)
  before writing against it. It is cheaper than discovering its helpers one
  bug at a time.

---

## 5. The entry point: `LastStandMod.cs`

### 5.1 Wiring only

Last Stand's `ModBehaviour` holds no game rules. It creates the session,
hooks the game, guards the per-frame tick and tears everything down. Trimmed
to the lifecycle:

```csharp
public class LastStandMod : ModBehaviour
{
	public LastStandConfig config = new LastStandConfig();

	internal static LastStandMod Current { get; private set; }

	internal LastStandSession Session { get; private set; }

	private readonly ErrorThrottle _tickErrors = new ErrorThrottle(Tuning.ErrorLogIntervalSeconds);

	private void Awake()
	{
		Current = this;
	}

	private void Start()
	{
		instance.isAlteringGameplay = true;
		Session = new LastStandSession(SessionSettings.From(config));
		ChoiceFields.Register();
		harmony.PatchAll();
		CallOnNetworkedManager<GameManager>(OnGameManagerStarted);
		Log.Info(LogText.Loaded(mod.metadata.name, mod.metadata.modVer));
	}

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

	private void OnDestroy()
	{
		Session?.Reset();
		ConfigWindow.Close();
		InjectedUi.RemoveAll();
		LobbyModeOption.Detach();
		ChoiceFields.Unregister();
		harmony.UnpatchAll(harmony.Id);

		if (Current == this)
		{
			Current = null;
		}

		Log.Info(LogText.Unloaded);
	}

	public override void LoadConfigsToDisk()
	{
		foreach (FieldInfo field in modConfigFields)
		{
			ConfigFile.ResetIfOutdated(GetModConfigFilePath(field));
		}

		base.LoadConfigsToDisk();
	}
}
```

Each line maps to a loader fact from section 4:

| Line | Why it is there |
|---|---|
| `public LastStandConfig config` | A public field of a `ModConfig` subclass. The loader finds it through `modConfigFields` and loads it in step 3, before `Awake`. File: `<DataDir>/QuickSave/Mods/<id>/LastStandMod.config.json` (`GetModConfigFilePath`: `<Type>.<field>.json`). Details in `08-config-ui-and-commands.md` |
| `LoadConfigsToDisk` override | Runs in step 3. It moves an outdated saved file aside before the base method loads it, because the base method loads the saved JSON over the defaults and would keep stale values forever |
| `static Current`, set in `Awake` | Patches are static methods and need the live instance. Static is safe here because each reload gets a fresh copy of the static (new assembly). `OnDestroy` clears it only if it still points at itself |
| `isAlteringGameplay = true` in `Start` | Code of conduct (section 8). Set at load time, long before any lobby exists |
| `harmony.PatchAll()` | Applies every `[HarmonyPatch]` class in the calling assembly (Harmony finds it from the caller's stack frame) |
| `try`/`catch` around the tick, with `ErrorThrottle` | Unity would catch an exception from `Update` itself, but it would recur and be logged once per frame. The throttle logs at most one every 5 s (`Tuning.ErrorLogIntervalSeconds`) with a count of suppressed repeats, and the session keeps its state instead of starting over |
| `OnDestroy` in reverse order of setup | Undo game state first (`Session.Reset`), then UI, then global registrations, then patches |

The second behavior, `LastStandConsole : ModBehaviour`, only holds
`[ConsoleCommand]` methods and reaches the session through
`LastStandMod.Current`. Splitting them keeps the entry point readable; the
loader treats both the same way.

### 5.2 Gotcha: the template's `UnpatchAll()` removes every mod's patches

The template's `OnDestroy` reads:

```csharp
harmony.UnpatchAll();
```

Decompile `HarmonyLib.Harmony` from `<Managed>/0Harmony.dll` and read
`UnpatchAll`:

```csharp
public void UnpatchAll(string harmonyID = null)
{
	List<MethodBase> list = GetAllPatchedMethods().ToList();
	foreach (MethodBase original in list)
	{
		...
		patchInfo2.Prefixes.DoIf(IDCheck, delegate(Patch patchInfo)
		{
			Unpatch(original, patchInfo.PatchMethod);
		});
		...
	}
	bool IDCheck(Patch patchInfo)
	{
		return harmonyID == null || patchInfo.owner == harmonyID;
	}
}
```

With no argument, `harmonyID` is `null`, `IDCheck` is true for every patch,
and every patch of **every** mod is removed (`GetAllPatchedMethods` is
process-wide). Unloading your mod would silently break all others until they
reload. Pass your own id:

```csharp
harmony.UnpatchAll(harmony.Id);
```

And the id is not your mod id. From `ModBehaviour.cs`:

```csharp
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

A new GUID per instance means each load has its own id. Unpatching by the bare
mod id (`"com.example.mymod"`) would match nothing. Use `harmony.Id`. Also: each
`ModBehaviour` on the container has its own `_harmony`, so patch and unpatch
from the same behavior.

### 5.3 Teardown checklist

Everything the loader doesn't undo (section 4.4) needs an owner. How Last
Stand handles each kind:

| You added | Undo in `OnDestroy` | Last Stand |
|---|---|---|
| Harmony patches | `harmony.UnpatchAll(harmony.Id)` | Same |
| UI objects under the game's canvases | Destroy them; find them by a name you control | Every injected object's name ends in `" (LastStand)"` (`ModIdentity.OwnedSuffix`); `InjectedUi.RemoveAll` destroys every `Button` whose name has the suffix |
| Entries in game-global lists | Remove exactly your entries | `ChoiceFields.Register` inserts two builders at the front of `DewGUI.fieldBuilders`; `Unregister` removes them with `RemoveAll` on the same delegates |
| Changes to game state | Restore the original value | `Session.Reset()` ends the run's effects, e.g. puts the shared gold price multiplier back to 1 (log: `multiplier restored to 1`) |
| Open windows | Close them | `ConfigWindow.Close()` |
| Handlers on game UI events | Unsubscribe | `LobbyModeOption.Detach()` removes its `onValueChanged` handler from the lobby window's mode dropdown |

### 5.4 Why loading and unloading mid-run is risky

The official docs warn that loading or unloading mid-game "can cause
unexpected bugs", and `ReloadFromActiveMods` shows the dev text
`ModManager_WarningInGameOrLobby` when a network session is active. Last
Stand met each of these during development, all through auto-reload
(section 7.2):

1. **Statics restart.** A reload mid-run started a fresh session at wave 1,
   because the wave count lived in the old assembly. Fix: the session writes
   `run key|node|wave|in-progress|fast clears` to
   `AppDomain.CurrentDomain.SetData("LastStand.Progress", ...)` whenever a
   wave starts or is cleared, and the new instance resumes from it if the run
   key (the `GameManager` instance id plus the world seed) matches.
2. **Old handlers keep firing.** Event handlers the old assembly attached to
   game entities stay attached after the unload and run into the old, reset
   session. A heroic boss alive during a reload lost its record and died
   through the game's own boss flow with no rewards. Fix: ownership that
   matters at death is derived from the world (any supported boss standing in
   the arena is Last Stand's), not from an in-memory set.
3. **Duplicates.** Objects placed in the world by the old instance are still
   there. On arrival the new session reuses a shrine or quest tracker that
   already stands instead of adding a second one.
4. **Teardown after the network is gone.** Game calls that write synced
   state throw once the transport has shut down (game over, restart):
   `ZoneManager.RemoveModifier` writes a node's `SyncList` and fails with
   `InitSyncObject: IsWritable: neither NetworkServer nor NetworkClient are
   active`. Seen at wave 170, it aborted the session reset halfway. Teardown
   clears its own state first and calls the game only while
   `NetworkServer.active`.

Rule of thumb: assume `OnDestroy` can run at any moment, and assume a fresh
instance can start in the middle of any game state.

**Where else this applies.**

- Before adding any static field, decide whether it must survive a reload.
  If yes, it goes through `AppDomain.SetData` as a plain string or primitive
  (a type from your old assembly is not the same type in the new one).
- Before subscribing to any game event, write the matching unsubscribe in
  `OnDestroy`, or design the handler so a stale copy is harmless.
- `07-mod-architecture.md` builds the full session design on these rules.

---

## 6. Developer Mode, the console, the log viewer and the log file

### 6.1 Developer Mode

Settings → Gameplay → **Developer Mode** sets
`DewSave.platformSettings.gameplay.enableDeveloperMode`. Search for that
field to see what it unlocks:

```sh
grep -rn "enableDeveloperMode" Dew.Core Dew.UI
```

- `DevCanvas.Update`: with it on, **F1** cycles `DevCanvasState`
  `Hidden` → `Shown` → `ShownWithoutPopup` (the initial state is
  `ShownWithoutPopup`). `Shown` puts the log viewer on the right side of the
  screen; `Hidden` also makes the console unavailable.
- `GlobalUIManager.ShowDevText`: floating developer messages (and a log line)
  only appear with it on. The loader uses these for auto-reload on/off and the
  in-game reload warning.
- `DewMod.OnSettingsChanged`: auto-reload needs it (section 7.2).
- The mod manager's Workshop upload button (`10-shipping.md`).

### 6.2 The console

The console is the vendored IngameDebugConsole
(`Dew.External/IngameDebugConsole/`). `DebugLogManager.toggleKey` is
`KeyCode.BackQuote`: the **`` ` ``** key opens and closes it, in any
`DevCanvasState` except `Hidden`. Useful built-ins:

- `help` lists every command (`DebugLogConsole` registers it), `help <text>`
  filters. Your `[ConsoleCommand]` methods appear here after a load; their
  default description is `No description provided. (<mod name>)`.
- `build` prints the build profile and `Application.version`.
- The console window has a **Manage Mods** button that opens the mod manager
  anywhere, not only on the title screen (from the official docs; the button
  is wired in the scene, not in code you can grep).

Command matching is case-insensitive (`DebugLogConsole.caseInsensitiveComparer`).
If two commands share a name and parameter count, `RegisterConsoleCommand`
appends a number (`name0`, `name1`...), so prefix your commands with your mod's
name.

### 6.3 The mod manager

Title screen → **Mods**, or **Manage Mods** in the console. Mark a mod active,
then **Apply**. `UI_ModManager.Apply` saves `activeMods`, turns cross-play off
if any mod is active, and calls `DewMod.ReloadFromActiveMods()` only if the
list changed (`IsDirty`). The gear button (config) appears only for a loaded
mod with at least one `ModConfig` field.

If a mod crashes the game during load, use the 2-second window at startup:
the loading screen shows `ModManager_AboutToLoadModCancelPrompt`; hold **Q**
(or gamepad North) to skip loading mods for this session, then deactivate it.

### 6.4 `Player.log`

Unity writes everything sent to `Debug.Log`, `Debug.LogWarning`,
`Debug.LogError` and `Debug.LogException`, plus uncaught exceptions, to:

- Windows: `%USERPROFILE%\AppData\LocalLow\Lizard Smoothie\Shape of Dreams\Player.log`
- Linux (Proton):
  `steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log`

The file starts over at each launch; the previous session is kept as
`Player-prev.log` next to it. The same folder (`<DataDir>`) holds
`QuickSave/` with `r_platform.json` (active mods, auto-reload flag) and
`QuickSave/Mods/<id>/` (mod config files).

Follow it live:

```sh
tail -F "<DataDir>/Player.log" | grep --line-buffered -E '\[MyMod\]|\[DewMod\]|Exception|Error'
```

On Windows, PowerShell: `Get-Content "<DataDir>\Player.log" -Wait`.

`[DewMod]` is the loader's own prefix. Grep for it first when a mod doesn't
show up: `Initialized`, `Activating`, `Failed to load`,
`Unknown assembly type`, and `[DewMod-Override]` for JSON overrides.

**Where else this applies.**

- Any setting the game exposes: grep its field name in the decompile to see
  every system that reacts to it, as done for `enableDeveloperMode`.
- `ShowDevText` is a quick on-screen debug channel for your own mod while
  developer mode is on (`09-debugging-and-testing.md`).

---

## 7. The build loop

### 7.1 Build

From the project folder:

```sh
dotnet build                 # Debug → bin/Debug/netstandard2.1/MyMod.dll
dotnet build -c Release      # Release → bin/Release/netstandard2.1/MyMod.dll
```

During development, `about/metadata.json` points at the **Debug** DLL, as the
template does. A Release build writes to a different folder and changes
nothing the game loads, so it is safe to build while testing. For shipping,
Last Stand's `pack.sh` copies the Release DLL into a clean folder and rewrites
`assemblies` to `[ "LastStand.dll" ]` (`10-shipping.md`).

Last Stand's check before calling a change done is a clean build:
`0 Warning(s)`, `0 Error(s)`.

### 7.2 Auto-reload

`DewMod.EnableAutoReload` runs when developer mode **and**
`platformSettings.enableAutoReloadMods` are both on (`OnSettingsChanged`).
The flag is the auto-reload toggle in the mod manager
(`UI_ModManager.ToggleAutoReload`, saved to `r_platform.json`). What it does:

```csharp
FileSystemWatcher fileSystemWatcher = new FileSystemWatcher
{
	Path = Path.GetFullPath(allLocalModDirectory),
	NotifyFilter = (NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size),
	Filter = "*.*",
	IncludeSubdirectories = true
};
```

and the check loop:

```csharp
if (_lastReqeustedAutoReloadTime != 0f && Time.unscaledTime - _lastReqeustedAutoReloadTime > 0.5f
    && !isLoadingMod && ManagerBase<TransitionManager>.instance.state == TransitionManager.StateType.Normal
    && Application.isFocused)
{
	_lastReqeustedAutoReloadTime = 0f;
	Refresh();
	ReloadFromActiveMods();
}
```

So:

- **Any** file change anywhere under the local mods directories triggers it:
  your DLL, but also `obj/` files written mid-build, a source file you saved,
  a doc you edited, another mod's folder. Workshop items are not watched.
- It waits until 0.5 s have passed since the last change, no load or scene
  transition is in progress, **and the game window has focus**. A build
  produces a burst of changes; the reload happens once you switch back to the
  game.
- It rescans (`Refresh`, so metadata edits take effect) and reloads **every**
  active mod, not only the one that changed.
- Config files live in `<DataDir>`, outside the watched folder, so editing
  them does not trigger a reload.

The loop:

1. Edit code.
2. `dotnet build`.
3. Switch to the game window.
4. Check the log for your `unloaded` line, then `[DewMod] Initialized`, then
   your `loaded` line.

Without auto-reload, reload by deactivating the mod, **Apply**, activating it,
**Apply**, or restart the game. Mid-run reloads have the costs listed in
section 5.4; reload on the title screen when the test doesn't need a run in
progress.

---

## 8. The code of conduct and where the game enforces it

The official docs' code of conduct has two requirements that touch code:

1. Every server-side mod that alters gameplay must set
   `instance.isAlteringGameplay = true`.
2. A mod must not grant exclusive, server-generated items (anything bought
   with stardust).

Plus the Modding Terms of Service
(https://shape-of-dreams.gitbook.io/shape-of-dreams-modding-terms-of-service/).

### 8.1 Follow `isAlteringGameplay` to its readers

**Technique: when a rule says "set this flag", find who reads the flag.**
That tells you what the flag does, when it is read, and therefore when you must
set it.

```sh
grep -rn "isAlteringGameplay\|isGameplayAltered" Dew.Core Dew.UI
```

Chain:

1. `LoadedModInstance.isAlteringGameplay` (your `instance` field). `Load`
   sets it to `true` by itself when the mod has an `overrides` folder.
2. `DewMod.isGameplayAltered => loadedInstances.Any(i => i.isAlteringGameplay)`.
3. `LobbyServiceProvider.SetBasicAttributes` (Steam lobbies) and the EOS
   lobby creation both write `SetLobbyAttribute("isModded", DewMod.isGameplayAltered)`.
   `SetBasicAttributes` is called from `LobbyServiceSteam` right after the
   lobby is created.
4. Readers of `isModded`:
   - `UI_Title_FindLobby_LobbyItem`: `isModdedObject.SetActive(lobby.isModded)`,
     the MOD icon in the lobby browser;
   - `UI_Title_FindLobbyView`: joining a modded lobby first shows
     `FindLobby_WarningJoinModdedLobby` with Yes/Cancel;
   - `DewPlayer.OnStartClient`: a client in a modded lobby calls
     `CmdRequestOverrides` to receive the host's JSON overrides.

Consequences:

- The flag is read **when the lobby is created**. Set it at load (`Awake` or
  `Start`), unconditionally, not when your feature first activates. Last
  Stand sets it first thing in `Start`.
- The template does not set it. Add it.
- Nothing forces it. The docs say so: "We don't have a way to force these
  rules on people". It is honor-based, and the studio monitors the Workshop.

### 8.2 Cross-play

`DewMod.OnInit` sets `enableCrossPlay = false` at startup if any mod is active,
and `UI_ModManager.Apply` does the same when activating mods (with the
`Mods_CrossPlayDisabled` message). Don't patch around it: modded lobbies
stay off cross-play by design.

### 8.3 Stardust-exclusive items

There is no code check for this rule. It applies to what your mod spawns or
grants: cosmetics and anything else bought with stardust are off limits,
even in a mod that freely grants gold, dream dust, memories or essences (Last
Stand's cheat commands, off by default, do the latter).

---

## 9. Hello world, verified

A minimal mod that proves the whole chain: folder found, metadata valid, DLL
matched, assembly loaded, behavior activated, log written, clean unload.

`Mods/HelloMod/about/metadata.json`:

```json
{
  "id": "com.example.hellomod",
  "name": "Hello Mod",
  "author": "Example",
  "modVer": "0.1.0",
  "supportedGameVer": [ "*" ],
  "assemblies": [ "bin/Debug/netstandard2.1/HelloMod.dll" ]
}
```

`Mods/HelloMod/HelloMod.cs`:

```csharp
using UnityEngine;

namespace HelloMod;

public class HelloMod : ModBehaviour
{
	private const string LogPrefix = "[HelloMod] ";

	private void Awake()
	{
		Debug.Log(LogPrefix + "awake: " + mod.metadata.id + " v" + mod.metadata.modVer);
	}

	private void Start()
	{
		instance.isAlteringGameplay = true;
		harmony.PatchAll();
		Debug.Log(LogPrefix + "started, harmony id " + harmony.Id);
	}

	private void OnDestroy()
	{
		harmony.UnpatchAll(harmony.Id);
		Debug.Log(LogPrefix + "unloaded");
	}
}
```

(File-scoped `namespace` needs `LangVersion` 10 or `latest`; with the
template's `9.0`, use a block namespace.)

Why a fixed prefix: `Player.log` mixes every system's output, and the game logs
a lot. A constant prefix in one place (Last Stand: `ModIdentity.LogPrefix =
"[LastStand] "`, used by every method of `Log`) makes your lines greppable,
and the same prefix filters the in-game log viewer.

Steps:

1. `dotnet build` in `Mods/HelloMod`.
2. Start the game (or switch to it with auto-reload on).
3. Mods → mark **Hello Mod** active → Apply.
4. Search `Player.log`:
   ```
   [DewMod] Initialized com.example.hellomod::HelloMod
   [DewMod] Activating com.example.hellomod
   [HelloMod] awake: com.example.hellomod v0.1.0
   [HelloMod] started, harmony id com.example.hellomod<guid>
   ```
5. Deactivate, Apply; expect `[HelloMod] unloaded`.

Last Stand's real lines, for comparison (`<id>` is its mod id):

```
[DewMod] Initialized <id>::LastStandConsole
[DewMod] Initialized <id>::LastStandMod
[DewMod] Activating <id>
[LastStand] loaded: Last Stand v0.1.0.
```

When it doesn't work, the symptom points at the step:

| Symptom | Cause (from the loader code) |
|---|---|
| Not in the mod manager at all | No `about/metadata.json` directly in a child of `Mods/`; folder nested one level too deep; a `.template.config` folder inside |
| Listed with an error, tooltip "invalid metadata" | JSON parse error, or `id`/`name`/`author` empty, or `assemblies` missing |
| Listed with an error, id conflict | Another folder or a subscribed Workshop item has the same `id` |
| Listed, active, but no `[DewMod] Initialized` line | `assemblies` pattern matches nothing (case, backslashes, path relative to `about/` instead of the mod root), the DLL wasn't built, or no non-abstract `ModBehaviour` subclass |
| `[DewMod] Unknown assembly type` | Pattern also matches a non-`.dll` file |
| `[DewMod] Failed to load <id> due to exception` | `Assembly.Load` or `GetTypes()` failed, typically a reference to an assembly the game doesn't have |
| `Initialized` and `Activating`, but no line of yours | Exception in `Awake` (look just below `Activating`), or your code logs before `instance` is set (constructor or field initializer) |
| Works once, then other mods break when you unload | `harmony.UnpatchAll()` without `harmony.Id` (section 5.2) |

**Where else this applies.**

- Any new hook or feature: add one prefixed log line at the point where it
  takes effect, and prove it fires before building on it
  (`09-debugging-and-testing.md`).
- Checking that teardown works: the `unloaded` line, followed by a fresh
  `loaded` line after a reload, with no exception between them, is the
  smallest test of section 5.3.
