# 02. Reading the game: decompiling and navigating the game's code

Shape of Dreams ships no source code and its official modding docs are a few
pages long. Everything a mod relies on (which method computes a price, which
field reaches clients, which call every shrine use goes through) is learned
by reading the game's compiled code. This chapter teaches that reading:
how to get the code out of the DLLs, how to find your way through a few
thousand types, how to read what the compiler and Mirror's weaver did to the
source, and how to write down what you learned so it stays true.

Setting up the mod project itself is in `01-project-setup.md`. Turning a
finding into a patch is in `03-hooking-with-harmony.md`. Deciding whether a
field reaches vanilla clients is in `04-host-only-networking.md`. Values that
live in prefabs, scenes and localization (not in code) are in
`05-assets-and-data.md`.

---

## 1. What you are reading

### 1.1 Mono build, IL in DLLs

Unity games are built with one of two scripting backends. **IL2CPP** compiles
C# to native code (you'd see `GameAssembly.dll`); **Mono** keeps the
compiled C# as **IL** (Intermediate Language, .NET's bytecode) in ordinary
.NET DLLs and runs it with a bundled Mono runtime. Shape of Dreams is a
Mono build: `<GameFolder>` holds `MonoBleedingEdge/`, there is no
`GameAssembly.dll`, and the game's code sits in

```
<GameFolder>/Shape of Dreams_Data/Managed/*.dll
```

`Player.log` says the same on startup:

```
Mono path[0] = '.../Shape of Dreams/Shape of Dreams_Data/Managed'
Initialize engine version: 6000.0.77f1 (88f89d0d8b31)
```

IL keeps nearly everything the source had: type, method, field and property
names, signatures, attributes, string literals, inheritance. It loses
comments, local variable names (unless a matching `.pdb` is present, see
1.4), `const` names at their use sites, and the shape of some high-level
constructs (iterators, `async`, lambdas), which the compiler lowered into
extra classes. A **decompiler** (ILSpy, dnSpy) turns IL back into C#. The
result is very close to the original, but it is a reconstruction, and
section 7 lists where it lies.

Record the game build next to every decompile. `<GameFolder>/version.txt`
holds it (`r.1.4.0.13_s` at the time of writing). A fact read from one
build can be false in the next.

### 1.2 Which assembly holds what

Only a handful of the ~150 DLLs in `Managed` are the game's own. Listing
classes per assembly (`ilspycmd -l c`, section 2.3) gives the scale:

| Assembly | Classes (incl. nested) | What lives there |
|---|---|---|
| `Dew.Core.dll` | 1417 | The engine of the game: managers (`GameManager`, `ZoneManager`, `PickupManager`, `LootManager`, `ChatManager`, `EditSkillManager`…), base classes (`Actor`, `Entity`, `Monster`, `BossMonster`, `Hero`, `Shrine`, `EditSkillShrine`, `ChoiceShrine`, `StatusEffect`, `RoomModifierBase`, `DewQuest`), rooms (`Room`, `RoomMonsters`, `RoomSection`, `Section_Monsters`), the `Dew` static helper class, the mod API (`DewMod`, `ModBehaviour`, `ModConfig`, `DewGUI`, `DewConsoleCommands`), Mirror's generated code (`Mirror/GeneratedNetworkCode.cs`), and some content that the core needs (`Shrine_Chaos`, `Shrine_UpgradeWell`, `Shrine_AltarOfCleansing`, `RoomMod_Hunted`, `Se_HunterBuff`, `Se_HeroKnockedOut`, `GameMod_Limbo`, `GameMod_MirageSkin`, `Quest_ShapeOfDreams`…) |
| `Dew.Contents.dll` | 3601 | The content: `Ai_*` (725, ability instances: projectiles, explosions), `Se_*` (610, status effects), `At_*` (267, ability triggers), `St_*` (177, skill triggers = memories), `Gem_*` (105, essences), `ACH_*` (94, achievements), `Mon_*` (66, monsters, `Mon_*_Boss*` = heroic bosses), `RoomMod_*` (60, room modifiers), `Shrine_*` (55), `Star_*` (51), `Treasure_*`, `Artifact_*`, `LucidDream_*`, `Rev_*` (reveries), `Quest_*`, `GameMod_*`, `Ge_*` (game effects) |
| `Dew.UI.dll` | 687 | Everything drawn: `UI_InGame_*`, `UI_Lobby_*`, `UI_Title_*`, `UI_Tooltip_*`. Client-local by nature |
| `Dew.External.dll` | 1177 | Bundled third-party code: DOTween (`DG.Tweening`), `CI.QuickSave`, `DuloGames.UI`, Epic Online Services, effects packages. Rarely relevant |
| `Mirror.dll` | 91 | The networking library (SyncVars, Commands, RPCs) |
| `FizzySteamworks.dll` | 17 | Mirror transport over Steam networking (`Mirror.FizzySteam`) |
| `MultiplexTransport.dll` | small | Mirror transport that combines several transports |
| `Assembly-CSharp.dll` | 55 | Leftovers: render features, a few small types |
| `0Harmony.dll`, `Mono.CecilX.dll` | | Harmony (built into the game for mods) and Mirror's renamed copy of Mono.Cecil (used in section 4) |

The prefix tells you the base class, which tells you the lifecycle and the
networking. Checked in the decompile:

| Prefix | Base class | Example |
|---|---|---|
| `Mon_` | `Monster` or `BossMonster` (both `: Entity : Actor`) | `public class Mon_Sky_BossNyx : BossMonster` |
| `Se_` | `StatusEffect : AbilityInstance : Actor` (curses: `CurseStatusEffect`) | `public class Se_Curse_DarkUrge : CurseStatusEffect` |
| `Ai_` | `AbilityInstance` or a subclass | `public class Ai_Gem_E_Crimson : InstantDamageInstance` |
| `At_` | `AbilityTrigger : Actor` | `At_MorasDomain_MorasCreation_Boss_Move` |
| `St_` | `SkillTrigger : AbilityTrigger` | `St_D_BeautifulThreat` |
| `Gem_` | `Gem : Actor` | `public class Gem_L_Culinary : Gem` |
| `RoomMod_` | `RoomModifierBase : Actor` | `public class RoomMod_PureDream : RoomModifierBase, ...` |
| `Shrine_` | `Shrine : Actor` (or `EditSkillShrine`, `ChoiceShrine`) | `public class Shrine_Ascension : EditSkillShrine` |
| `GameMod_` | `GameModifierBase` | `GameMod_Obliviax` |
| `Ge_` | `GameEffect : Actor` | `Ge_Shrine_PotOfGreed` |
| `Quest_` | `DewQuest : GameEffect` | `Quest_LostSoul` |

Almost everything that exists in a run is an `Actor`, which is a Mirror
`NetworkBehaviour` (`Actor : DewNetworkBehaviour : NetworkBehaviour`). That
one fact explains why most game objects can carry synced state
(`04-host-only-networking.md`).

`strings` on a DLL also shows where each type lived in the developer's
project, because Unity embeds a type-to-file table
(`UnitySourceGeneratedAssemblyMonoScriptTypes_v1`):

```sh
strings "$M/Dew.Contents.dll" | grep -i ascension
# \Assets\Dew\RoomMods\Common\SpawnAscension\RoomMod_SpawnAscension.cs
# \Assets\Dew\Shrines\Shrine_Ascension\Shrine_Ascension.cs
```

Counting those folders is a quick content census (`Assets\Dew\Monsters`
803 entries, `Stars` 386, `Skills` 274, `RoomMods` 93, `Shrines` 29…).

**Where else this applies:** guessing where an unknown class lives before
searching; telling content (`Dew.Contents`) from engine (`Dew.Core`) when
choosing what to patch; knowing that anything in `Dew.UI` runs per client
and can't be changed for vanilla players.

---

## 2. Getting the code out

### 2.1 GUI decompilers

**ILSpy** (and **dnSpy**/dnSpyEx) open a DLL and show a tree of namespaces,
types and members, with decompiled C# on the right. Use them for browsing,
and above all for **Analyze** (right-click a member): "Used By", "Read By",
"Assigned By", "Overridden By" list every reference in the loaded
assemblies, from the IL, so compiler-generated classes are included. Load
`Dew.Core.dll`, `Dew.Contents.dll`, `Dew.UI.dll` and `Mirror.dll` together
so cross-assembly references resolve.

The GUI is good for one question at a time. For "every place that does X",
an exported project plus `grep` (section 3) and an IL scan (section 4) are
faster and repeatable.

### 2.2 `ilspycmd`: the command-line ILSpy

```sh
dotnet tool install -g ilspycmd
M="<GameFolder>/Shape of Dreams_Data/Managed"
```

If `ilspycmd` refuses to start because its .NET runtime version is missing,
set `DOTNET_ROLL_FORWARD=Major` so it runs on the newer runtime you have.

**Whole-assembly export (`-p`)** writes a compilable project, one `.cs` per
type, folders per namespace (`Mirror/GeneratedNetworkCode.cs`,
`DewInternal/…`):

```sh
for a in Dew.Core Dew.UI; do
  ilspycmd -p -usepdb -o decomp/$a -r "$M" "$M/$a.dll"
done
ilspycmd -p -o decomp/Dew.External -r "$M" "$M/Dew.External.dll"
```

- `-r "$M"` is the reference path, so types from other assemblies
  (`UnityEngine`, `Mirror`) resolve. Without it you get worse output
  (unresolved types, missing extension-method sugar).
- `-usepdb` reads local variable names from the `.pdb` next to the DLL
  (section 1.4).

**One type (`-t`)** prints a single type to stdout:

```sh
ilspycmd -r "$M" -t Shrine_Ascension "$M/Dew.Contents.dll" > types/Shrine_Ascension.cs
```

**List types (`-l c`)** finds names without `strings`, on any OS:

```sh
ilspycmd -l c "$M/Dew.Contents.dll" | grep -i ascension
# Class RoomMod_SpawnAscension
# Class Shrine_Ascension
# Class Shrine_Ascension.<>c__DisplayClass14_0
```

**IL (`-il`)** dumps raw IL. In ilspycmd 8.2 it ignores `-t` and writes the
whole assembly (680k lines for `Dew.Contents`), so redirect it to a file and
search it.

Version 8.2 appends an update notice to stdout ("You are not using the
latest version of the tool…"). With `-t … > file.cs` those two lines end up
at the bottom of the file. Delete them or they break any script that
compiles or parses the output.

### 2.3 Why `Dew.Contents` won't export whole, and the workaround

```sh
ilspycmd -p -o decomp/Dew.Contents -r "$M" "$M/Dew.Contents.dll"
```

aborts partway (the output folder keeps whatever files were written in
parallel before the crash):

```
System.AggregateException: One or more errors occurred.
 (Error decompiling @06000439 GameMod_SantaHat.OnCreate)
 ---> System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at ICSharpCode.Decompiler.IL.ILReader.PrepareBranchTargetsAndStacksForExceptionHandlers()
```

23 method bodies in `Dew.Contents` are malformed (count from the scanner
in section 4). The IL
dump shows what they are:

```
instance void OnCreate () cil managed
{
    // Code size: 1 (0x1)
    .locals init ( [0] class [mscorlib]...IEnumerator`1<class Se_M_Endure>, ... )
    IL_0000: ret
} // end of method Se_M_Endure::OnCreate
```

The body is a lone `ret` with leftover locals, and the exception-handler
table ILSpy tries to set up from (and Cecil reads in `ReadExceptionHandlers`)
doesn't fit it. One such body fails its whole type, and in `-p` mode the
whole export. A single-type `-t` of a type with such a method fails too
(`Error decompiling @060010F1 Se_M_Endure.OnCreate`), but every other type
decompiles fine. So:

1. Export `Dew.Core`, `Dew.UI`, `Dew.External` whole.
2. For `Dew.Contents`, decompile one type at a time with `-t` into a
   `types/` folder, as you need them. Find names with `-l c` or `strings`.
3. For the few types that fail, read the method you need in the `-il`
   dump. The broken bodies are empty anyway.
4. To find *which* `Dew.Contents` types reference something, don't guess:
   scan the IL (section 4), then `-t` each hit.

The IL scanner in section 4 reports the unreadable bodies as it goes. In
this build they include `GameMod_SantaHat::OnCreate`, `Se_M_Endure::OnCreate`,
`Shrine_DebugTeleport::OnUse`, `Ai_R_Fireball::Explode` and
`Mon_Sky_BigBaam_Buffer::GetFleeDestination`.

### 2.4 PDBs: real local names

`Managed` ships `Dew.Core.pdb`, `Dew.UI.pdb`, `Dew.External.pdb` and
`Dew.Contents.pdb`. A matching PDB gives the decompiler the original local
variable names. Without `-usepdb`, `PickupManager.HandleDeathDrop` reads:

```csharp
DewGameplayExperienceSettings ges = NetworkedManagerBase<GameManager>.instance.ges;
bool flag = IsUnderPerfPressure();
int killGoldAmount = NetworkedManagerBase<GameManager>.instance.GetKillGoldAmount(obj.victim);
if (flag)
```

With `-usepdb`:

```csharp
DewGameplayExperienceSettings gss = NetworkedManagerBase<GameManager>.instance.ges;
bool lowFps = IsUnderPerfPressure();
int goldAmount = NetworkedManagerBase<GameManager>.instance.GetKillGoldAmount(obj.victim);
if (lowFps)
```

`flag` became `lowFps`, which tells you what the branch is for. Across
`Dew.UI`'s `UI_InGame_*` files, `numN =` locals drop from 108 to 3.

`Dew.Contents.pdb` does **not** match its DLL:

```
Mono.Cecil.Cil.SymbolsNotMatchingException: Symbols were found but are not matching the assembly
```

So use `-usepdb` for `Dew.Core`, `Dew.UI` and `Dew.External`, and leave it
off for `Dew.Contents` (where it makes `-t` fail outright).

Stack traces in `Player.log` carry the original source paths and line
numbers (`in C:\...\Assets\Scripts\Global\DewSteam.cs:172`). Those line
numbers are the developer's, not your decompile's; search by method name.

**Where else this applies:** any Unity Mono game; re-exporting after every
game update and `diff -r` between the old and new export to see what
changed under your patches.

---

## 3. Text search over an exported project

`grep -rn` or ripgrep (`rg -n`) over the export answers most questions in
seconds. The patterns that matter:

| Question | Pattern |
|---|---|
| Where is `X` defined? | `rg -n "(void\|bool\|int\|float\|[A-Z]\w*) X\("` or the file `X.cs` |
| Who calls method `X`? | `rg -n "\bX\("` (also unqualified calls inside the class and subclasses) |
| Who subscribes `X` as a delegate? | `rg -n "\bX\b" \| rg -v "\bX\("` (finds `+= new Action<…>(X)`) |
| Who writes field `f`? | `rg -n "\bf\s*([-+*/]?=)[^=]"` plus `Networkf\s*=` (section 6.1) |
| Who subclasses `B`? | `rg -n ": B\b"` |
| Who overrides `M`? | `rg -n "override .* M\("` |
| Which UI text key does code use? | `rg -n '"Shrine_Ascension_'` |

Worked check: callers of `GetKillGoldAmount`.

```sh
rg -n "GetKillGoldAmount" decomp/
# Dew.Core/GameManager.cs:1085:   public int GetKillGoldAmount(Entity ent)
# Dew.Core/PickupManager.cs:284:  int killGoldAmount = ...GetKillGoldAmount(obj.victim);
```

One definition, one caller. But that only covers what you exported. A
`Dew.Contents` type that calls it would be missing from the result, and
you'd never know. That's the gap the IL scan closes.

Text search also misses:

- **const values.** `PickupManager` declares
  `public const int kTargetDropOrbCount = 3;`, but `rg kTargetDropOrbCount`
  finds only the declaration: the compiler inlined the 3 into
  `int num = (IsUnderPerfPressure() ? 1 : 3);` in `DropGold`.
- **References by reflection or by name string**, such as
  `DewResources.GetByShortTypeName<SkillTrigger>(toType)` or
  `[SyncVar(hook = "OnIsAvailableChanged")]`.
- **Values set in prefabs** (section 6.4, chapter 05).

**Where else this applies:** listing every `[SyncVar]` on a class before
deciding what to change; listing every localization key a shrine uses
(`"Shrine_Ascension_AscendAction"`, `"Shrine_Ascension_InvalidTarget"`…)
to reuse the game's own strings.

---

## 4. Scanning IL with Mono.Cecil

**Mono.Cecil** reads .NET assemblies as data: types, methods, and each
method's IL instructions with their operands (the method called, the field
loaded). Scanning every instruction of every method finds every reference,
including ones inside compiler-generated classes, ones in types you never
decompiled, and delegate creation (`ldftn`). It is exactly what the GUI's
Analyze does, but scriptable and across all assemblies at once.

The game's `Managed` folder already contains `Mono.CecilX.dll`, Mirror's
renamed copy of Cecil (namespace `Mono.CecilX`). The NuGet package
`Mono.Cecil` works the same with `using Mono.Cecil;`.

### 4.1 The scanner

`ilscan.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Mono.CecilX">
      <HintPath>$(GameManaged)/Mono.CecilX.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

`Program.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using Mono.CecilX;
using Mono.CecilX.Cil;

string managed = args[0];
string[] assemblies = args[1].Split(',');
string owner = args[2];
string member = args[3];
string[] names = { member, "get_" + member, "set_" + member };

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(managed);
var parameters = new ReaderParameters { AssemblyResolver = resolver };

foreach (string name in assemblies)
{
    using var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(managed, name + ".dll"), parameters);
    foreach (TypeDefinition type in assembly.MainModule.GetTypes())
    foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
    {
        MethodBody body;
        try
        {
            body = method.Body;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"unreadable: {type.FullName}::{method.Name} ({e.GetType().Name})");
            continue;
        }

        foreach (Instruction instruction in body.Instructions)
        {
            MemberReference target = instruction.Operand as MemberReference;
            if (target is (MethodReference or FieldReference)
                && target.DeclaringType.Name == owner
                && names.Contains(target.Name))
            {
                Console.WriteLine($"{name}: {type.FullName}::{method.Name} {instruction.OpCode} {target.Name} @IL_{instruction.Offset:x4}");
            }
        }
    }
}
```

Build and run:

```sh
dotnet build -c Release -p:GameManaged="$M"
dotnet bin/Release/net8.0/ilscan.dll "$M" Dew.Contents,Dew.Core,Dew.UI Room didClearRoom
```

Notes on the design:

- `GetTypes()` (not `.Types`) includes nested types, which is where
  closures, iterators and async state machines live.
- Matching `get_X`/`set_X` makes the same query work for fields and
  properties (a property is two methods in IL).
- `method.Body` is read lazily and throws on the malformed bodies from
  section 2.3. The `try` turns that into an `unreadable:` line instead of
  an abort.
- The opcode column says what kind of reference it is: `call`/`callvirt`
  (call), `ldftn` (delegate creation: event subscription, `StartCoroutine`
  of a local function), `ldfld`/`ldflda` (read, or pass by `ref`), `stfld`
  (write).

### 4.2 Worked example: every reader of `Room.didClearRoom`

**Problem.** Last Stand's arena is a boss room loaded as a *revisit*, so
`Room.StartRoom` sets `didClearRoom = isRevisit` → `true` (`Room.cs`:
`public bool didClearRoom { get; private set; }`, then
`didClearRoom = isRevisit;`). The mod runs room modifiers ("environments")
for one wave at a time in that room. Any modifier that checks
`didClearRoom` would think the fight is already over. The question: which
ones?

**Dead end: grep.** `rg -l didClearRoom decomp/` finds `Room`,
`RoomMonsters`, `RoomSection`, `Section_Monsters`, `RoomModifierBase`,
`GameManager` and whichever `Dew.Contents` types happened to make it into
the partial export (here only `RoomMod_CorruptedChaos_AuraOfPain`). Every modifier in `Dew.Contents`
that wasn't exported is invisible.

**Scan.** (output shortened)

```
Dew.Contents: RoomMod_CorruptedChaos_AuraOfPain::ActiveLogicUpdate callvirt get_didClearRoom @IL_0015
Dew.Contents: Mon_Sky_LittleBaam/<<OnCreate>g__Routine|5_0>d::MoveNext callvirt get_didClearRoom @IL_008a
Dew.Contents: RoomMod_StrayMemory_StartProp::OnStartServer callvirt get_didClearRoom @IL_0026
Dew.Contents: RoomMod_StardustEverywhere::ActiveLogicUpdate callvirt get_didClearRoom @IL_0070
Dew.Contents: Se_RoomMod_WarpingField_Unstable::ActiveLogicUpdate callvirt get_didClearRoom @IL_006f
Dew.Contents: Ai_UnstableRatSwarm_Projectile::OnComplete callvirt get_didClearRoom @IL_0034
Dew.Contents: Se_LingeringAuraOfGuidance::ActiveLogicUpdate callvirt get_didClearRoom @IL_010d
Dew.Contents: RoomMod_InkStrikeWarning::OnStartServer callvirt get_didClearRoom @IL_000b
Dew.Contents: RoomMod_FireDevil::<OnCreate>g__GetStartEndPos|0_2 callvirt get_didClearRoom @IL_00ee
Dew.Contents: RoomMod_FireDevil/<<OnCreate>g__Routine|0_1>d::MoveNext callvirt get_didClearRoom @IL_0063
Dew.Contents: RoomMod_RiskOfMeteors::ActiveLogicUpdate callvirt get_didClearRoom @IL_0015
Dew.Contents: RoomMod_GazeOfErebos/<>c__DisplayClass4_0/<<OnStartServer>g__Routine|1>d::MoveNext callvirt get_didClearRoom @IL_0072
Dew.Contents: Ge_Shrine_PotOfGreed::ClientEventOnRoomLoaded callvirt get_didClearRoom @IL_001b
Dew.Core: GameManager::IsEligibleForMidRunSave callvirt get_didClearRoom @IL_0029
Dew.Core: Room::StartRoom call set_didClearRoom @IL_002e
Dew.Core: Room/<<ClearRoom>g__Routine|92_0>d::MoveNext call get_didClearRoom @IL_001b
Dew.Core: Room/<<ClearRoom>g__Routine|92_0>d::MoveNext call set_didClearRoom @IL_0026
Dew.Core: RoomMonsters/<<OnRoomStartServerRoutine>g__Routine|32_1>d::MoveNext callvirt get_didClearRoom @IL_0037
Dew.Core: RoomSection::<Awake>b__39_0 callvirt get_didClearRoom @IL_0014
Dew.Core: Section_Monsters/<ActivateCombatRoutine>d__35::MoveNext callvirt get_didClearRoom @IL_0303
Dew.Core: RoomModifierBase::PlaceShrine callvirt get_didClearRoom @IL_018a
unreadable: Se_M_Endure::OnCreate (NullReferenceException)
...
```

Thirteen readers in `Dew.Contents`, most of them inside compiler-generated
iterator classes (`<<OnCreate>g__Routine|0_1>d::MoveNext`, see 6.3), which a
text search in the C# would only find if you'd already decompiled the type.
Only two writers: `Room.StartRoom` and the `ClearRoom` routine.

**Follow-up.** `-t` each hit into `types/` and read the condition. Then
sort them: `RoomMod_InkStrikeWarning` destroys itself when cleared,
`Mon_Sky_LittleBaam` stops summoning, `Se_LingeringAuraOfGuidance` heals
more, the hazards (`FireDevil`, `RiskOfMeteors`, `GazeOfErebos`) stop firing.
That list decided which environments Last Stand could use and that the room
has to be held "in combat" during a wave for the hazards to fire (the hook
itself is chapter 03's subject).

### 4.3 More scans from Last Stand

(`ilscan` below is short for `dotnet bin/Release/net8.0/ilscan.dll`.)

Every caller of the shrine-use funnel (section 5.4):

```
$ ilscan "$M" Dew.Contents,Dew.Core Shrine DoPostUseRoutines
Dew.Contents: Shrine_CorruptedChaos::UserCode_CmdChoose__Int32__NetworkConnectionToClient call ...
Dew.Contents: Shrine_Destiny/<>c__DisplayClass22_0/<<OnInteract>g__Routine|0>d::MoveNext call ...
Dew.Contents: Shrine_Entanglement::UserCode_CmdConfirm__Actor__NetworkConnectionToClient call ...
Dew.Contents: Shrine_Hatred/<>c__DisplayClass8_0/<<CmdChoose>g__Routine|0>d::MoveNext call ...
Dew.Contents: Shrine_MirrorOfRemorse::UserCode_CmdConfirm__NetworkConnectionToClient call ...
Dew.Contents: Shrine_MarshOfDestiny_SeedOfTorment/<>c__DisplayClass18_0/<<CmdChoose>g__Routine|0>d::MoveNext call ...
Dew.Core: ChoiceShrine/<>c__DisplayClass12_0/<<CmdChoose>g__Routine|0>d::MoveNext call ...
Dew.Core: Shrine_Chaos::UserCode_CmdChoose__Int32__NetworkConnectionToClient call ...
Dew.Core: EditSkillShrine::UserCode_CmdActivateEditSkillAction__Nullable`1__Nullable`1__NetworkConnectionToClient call ... (x2)
Dew.Core: Shrine::OnInteract call ...
```

Who writes the synced gold price multiplier, through the field and through
Mirror's generated setter (section 6.1):

```
$ ilscan "$M" Dew.Contents,Dew.Core GameManager NetworkgoldCostMultiplier
Dew.Core: GameMod_Limbo::OnCreate call set_NetworkgoldCostMultiplier @IL_005d
Dew.Core: GameMod_Limbo::OnDestroyActor call set_NetworkgoldCostMultiplier @IL_0138
```

`GameMod_Limbo` multiplies the price on create and divides it back on
destroy: a precedent inside the game for a mode that changes every gold
price, and the pattern Last Stand's gold price change follows.

Who calls `RoomMonsters.SelectMiniBoss`: only `RoomMonsters::SpawnMiniBoss`
(section 8.3). Who subscribes `PickupManager.HandleDeathDrop`:
`PickupManager::HandleEntityAdd ldftn HandleDeathDrop` (a delegate, so no
`HandleDeathDrop(` call exists anywhere).

**Where else this applies:** every reader of a field before you change
its meaning; every caller before you replace a method with a prefix;
every `stfld` of a field to prove nothing else writes it; checking after a
game update that your list of callers is still complete.

---

## 5. Navigation methods

### 5.1 Start from something visible in game

The player sees text. Text comes from localization keys. Keys lead to code.

1. **The text is a key name already.** `DewLocalization.GetUIValue` returns
   `"ui.!" + key` for a missing key:

   ```csharp
   if (data.ui.TryGetValue(key, out var value)) { return value; }
   return "ui.!" + key;
   ```

   If you ever see `ui.!Something` on screen, `Something` is the key.
2. **Reverse-look-up the text at runtime.** `DewLocalization.data` is the
   current language's `PerLanguageLocalizationData`, and its `ui` field is
   a public `Dictionary<string, string>`. A throwaway console command finds
   the key for any visible string:

   ```csharp
   public class LocaleProbe : ModBehaviour
   {
       [ConsoleCommand("Find UI keys whose text contains the query", "mymod_findtext")]
       public void Cmd_FindText(string query)
       {
           foreach (KeyValuePair<string, string> pair in DewLocalization.data.ui)
           {
               if (pair.Value != null && pair.Value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
               {
                   Debug.Log(pair.Key + " = " + pair.Value);
               }
           }
       }
   }
   ```

   (Reading the localization file offline is in `05-assets-and-data.md`.)
3. **Grep the key.** Naming conventions make most keys predictable:
   `<TypeName>_Name` (zones, modifiers, monsters), `<TypeName>_Title` and
   `<TypeName>_Description` (`DewQuest.OnCreate`:
   `GetUIValue(GetType().Name + "_Title")`), `<TypeName>_EditSkill`
   (`EditSkillShrine.GetEditSkillIndicatorRawText`). So
   "Mysterious Space" → `RoomMod_FragmentOfRadiance_MysteriousPlace_Name`
   → the type `RoomMod_FragmentOfRadiance_MysteriousPlace`.
4. **No hit in code means the key is in a prefab.** The quest title key
   `InGame_ThreatLevelDisplay_ThreatLevelRising` appears nowhere in the
   decompiled C#: it's set on a `DewLocalizedText` component in a prefab
   or scene. Its sibling `InGame_ThreatLevelDisplay_AmbientLevel` does hit:

   ```csharp
   public class UI_InGame_AmbientLevel : MonoBehaviour
   {
       private void Start()
       {
           _text = GetComponent<TextMeshProUGUI>();
           NetworkedManagerBase<GameManager>.instance.ClientEvent_OnAmbientLevelChanged += ...;
           ClientEventOnAmbientLevelChanged(0, NetworkedManagerBase<GameManager>.instance.ambientLevel);
       }
       private void ClientEventOnAmbientLevelChanged(int arg1, int arg2)
       {
           _text.text = string.Format(DewLocalization.GetUIValue("InGame_ThreatLevelDisplay_AmbientLevel"), arg2);
       }
   }
   ```

   which leads straight to `GameManager.ambientLevel`, the "threat level" on
   the HUD.
5. **Grep the format.** When the text is built, search for its shape:
   `"{0}. {1}"` in `Dew.UI` finds the location label in one hit (8.4).

### 5.2 Start from a type or prefab name

Most content is named after its type: the prefab `Shrine_Ascension` has the
component `Shrine_Ascension`. Starting there:

1. `ilspycmd -l c "$M/Dew.Contents.dll" | grep -i ascension` gives
   `Shrine_Ascension` and `RoomMod_SpawnAscension` (the room modifier that
   places it in normal runs, worth reading for how the game itself spawns
   it).
2. `-t Shrine_Ascension` (201 lines). The class line gives the parent:
   `public class Shrine_Ascension : EditSkillShrine`.
3. Note what it **overrides**: `GetTargetInfo` (twice), `OnActivateEditSkill`
   (twice), `GetEditSkillIndicatorRawText`, colors. Everything else is
   inherited, so the flow is in the parents.

### 5.3 Read up the inheritance chain

`Shrine_Ascension : EditSkillShrine : Shrine : Actor`. Reading the three
together gives the whole use flow, which no single file shows:

1. `Shrine.OnInteract(entity, alt)` runs on the server (`!alt && base.isServer`),
   checks `isLocked`, `CanAfford`, `maxTotalUseCount`, `maxUseCountPerPlayer`,
   `isAvailable`, then calls `OnUse(entity)`, and only if that returns true,
   `DoPostUseRoutines(entity)`.
2. `EditSkillShrine.OnUse` calls `EnterEditSkill(entity)` and returns
   **false**, so opening the window isn't a use. `EnterEditSkill` sends
   `TpcStartEditSkill` (a TargetRpc) to that player.
3. The player picks an item. Their client sends
   `CmdActivateEditSkillAction(skillLoc, gemLoc)`.
4. On the server, `UserCode_CmdActivateEditSkillAction__…` checks
   `CanInteract(player.hero)`, calls the subclass's `GetTargetInfo` (cost,
   reject reason), checks `cost.CanAfford`, calls the subclass's
   `OnActivateEditSkill`, then `player.Spend(cost)` and
   `DoPostUseRoutines(player.hero)`.
5. `Shrine.DoPostUseRoutines` spends `GetCost(entity)` too. That would
   double-charge, except `EditSkillShrine` overrides it:
   `public override Cost? GetCost(Entity activator) { return null; }`.

Step 5 is the kind of thing only reading the parents shows. Also check
`virtual` vs `abstract` vs plain: `Shrine.CanInteract` is `public virtual`
(subclasses may change it), `DoPostUseRoutines` is plain `public void`
(nobody can, so it's the same code for all shrines).

To list a base class's family: `rg -l ": EditSkillShrine\b"` over Core plus
the scan or `-l c` over Contents gives `Shrine_Ascension`,
`Shrine_AltarOfCleansing`, `Shrine_BossSoul`, `Shrine_Chaos`,
`Shrine_CorruptedChaos`, `Shrine_Destiny`, `Shrine_Entanglement`,
`Shrine_MawOfDoom`, `Shrine_ScaredMaw`, `Shrine_UpgradeWell`.

### 5.4 Find the single funnel

Before patching an effect, find the one method that **every** path to it
goes through. Patch there and you cover subclasses, alternate entry points
and content you've never read.

**Shrine uses.** The scan in 4.3 shows `Shrine.DoPostUseRoutines` is called
by the base `OnInteract`, both edit-shrine paths, both choice-shrine bases
and every special shrine. Its body is the definition of "a use":

```csharp
public void DoPostUseRoutines(Entity entity)
{
    Cost? cost = GetCost(entity);
    totalUseCount++;
    _useCountByPlayer[entity.owner.guid] = GetUseCountByPlayer(entity.owner) + 1;
    FxPlayNewNetworked(useEffect, entity);
    TpcShowLocalUseEffect((NetworkConnectionToClient)entity.owner, entity);
    if (cost.HasValue) { entity.owner.Spend(cost.Value); }
    Network_lastUseTime = NetworkTime.time;
    UpdateIsAvailable();
    RpcInvokeOnSuccessfulUse(entity);
}
```

Last Stand hooks one postfix there
(`[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]`) to
learn about every shrine use in the arena, instead of one patch per shrine.

**Room modifiers.** `ZoneManager.AddModifier` has three overloads. The two
generic ones build a `ModifierData` and call the third:

```csharp
public int AddModifier<T>(int nodeIndex, Action<T> beforePrepare = null) where T : RoomModifierBase
{
    // wraps beforePrepare
    return AddModifier(nodeIndex, new ModifierData { id = _nextModifierId++, type = typeof(T).Name }, beforePrepare2);
}
public int AddModifier(int nodeIndex, ModifierData mod, Action<RoomModifierBase> beforePrepare = null)
```

So every later modifier (Corrupted Chaos from `GameMod_CorruptedChaos`,
`RoomMod_HeroSoul` from `Se_HeroKnockedOut`, `RoomMod_DreamTeller` from
`Artifact`…) passes through the non-generic overload, and Last Stand's
modifier gate patches only that one, selected by its parameter types. As a
bonus, the funnel is the non-generic method, which Harmony patches without
trouble.

How to recognize a funnel: it's non-virtual, it's called from many places
(scan), its callers do the policy and it does the bookkeeping (counters,
effects, sync).

### 5.5 Follow calls down and up

- **Down** (what does X do): read X, then each callee. Stop at engine calls
  (`UnityEngine`, `Mirror`). Write the chain as you go:
  `HandleDeathDrop → GetKillGoldAmount → GetAdjustedGoldAmount_Income →
  GetAdjustedGoldAmount`.
- **Up** (who triggers X): scan for callers, then callers of callers, until
  you reach an entry point: a Unity message (`Start`, `Update`), a game
  event subscription (`ClientEvent_*`, `EntityEvent_*`), a Mirror message
  (`UserCode_Cmd*`, `UserCode_*` RPC bodies), a console command, or a
  coroutine started from one of those.
- **Events.** The game uses `SafeAction<T>`/`Action<T>` fields named
  `ClientEvent_On…`, `EntityEvent_On…`, `ClientActorEvent_On…`. "Who
  reacts to X" is `rg -n "ClientEvent_OnRoomLoaded \+="`: 42 subscriptions
  in the exported code alone.

### 5.6 The `Dew` static class: an index of useful APIs

`Dew` (`Dew.Core/Dew.cs`, ~3900 lines, `public static class Dew`) has 223
public static members. Grepping its method names is the fastest way to
find "is there already a helper for…":

```sh
grep -oP "^\tpublic static [^(=]*\(" decomp/Dew.Core/Dew.cs \
  | sed -E 's/.* ([A-Za-z_0-9]+)(<[^>]*>)?\($/\1/' | sort -u
```

Selected results: `CreateActor`, `CreateGem`, `CreateSkillTrigger`,
`CreateStatusEffect`, `CreateAbilityInstance`, `SpawnEntity`, `FindActorOfType`,
`FindAllActorsOfType`, `FindInterfacesOfType`, `GetGoodRewardPosition`,
`GetPositionOnGround`, `GetValidAgentDestination_Closest`,
`GetValidAgentDestination_LinearSweep`, `GetClosestAliveHero`,
`SelectRandomAliveHero`, `SelectRandomWeightedInList`,
`SelectRandomWeightedIndexInParams`, `IsRoomModifierIncludedInGame`,
`IsGameModifierIncludedInGame`, `TryGetTypeFromShortName`, `GetRarityColor`,
`GetHex`, `CallDelayed`. The game's own code is the best usage example:
`rg -n "Dew\.CreateActor"` shows dozens of calls with their callbacks.

Two more index-like places: the manager singletons
(`NetworkedManagerBase<T>.instance` for networked managers,
`ManagerBase<T>.instance` for local ones, `SingletonDewNetworkBehaviour<Room>.instance`
for the current room) and `DewResources` (`GetByShortTypeName<T>`,
`FindAllByType<T>`, `FindAllByNameSubstring<T>`).

Read the accessor before using it in a loop:

```csharp
public static T instance
{
    get
    {
        if (softInstance == null) { softInstance = Object.FindObjectOfType<T>(); }
        if (softInstance == null) { softInstance = Object.FindObjectOfType<T>(includeInactive: true); }
        return softInstance;
    }
}
public static T softInstance { get; private set; }
```

When the manager doesn't exist (title screen, between scenes) every
`.instance` call scans the scene twice. Per-frame mod code uses
`.softInstance` (see `07-mod-architecture.md`).

**Where else this applies:** before writing any helper, grep `Dew`,
`DewMath`, `DewResources` and the relevant manager; most "how do I place
something on the navmesh" or "pick a random hero" questions are one line.

---

## 6. Reading what the compiler and Mirror generated

### 6.1 Mirror-weaved code

**Mirror** is the networking library. Its **weaver** rewrites the
compiled DLL after the C# compiler runs, so the decompile shows the
rewritten version. Recognize these shapes (all counts from `Dew.Core`:
670 `[Server]`, 138 `[ClientRpc]`, 81 `[Command]`, 42 `[TargetRpc]`).

**SyncVar field.** The field keeps its attribute, and the weaver adds a
`Network<field>` property whose setter marks the field dirty:

```csharp
[NonSerialized]
[SyncVar]
public float goldCostMultiplier = 1f;

public float NetworkgoldCostMultiplier
{
    get { return goldCostMultiplier; }
    [param: In]
    set { GeneratedSyncVarSetter(value, ref goldCostMultiplier, 8192uL, null); }
}
```

`8192uL` is the dirty bit (0x2000); the same bit appears in
`SerializeSyncVars` (`if ((base.syncVarDirtyBits & 0x2000L) != 0L) writer.WriteFloat(goldCostMultiplier);`)
and `DeserializeSyncVars`. Inside the game's own assemblies the weaver
replaced every assignment to the field with the property, which is why the
game's code reads `gameManager.NetworkgoldCostMultiplier = …`. Your mod is
not weaved: assigning `goldCostMultiplier` directly changes the host's value
and nothing else. Always write through `Network<field>`
(`04-host-only-networking.md` has the full rules).

**SyncVar auto-property.** Same idea, with the compiler's backing field
escaped by ILSpy (`_003C` is `<`, `_003E` is `>`):

```csharp
[SaveVar(SaveVarFlags.Default)]
public int currentZoneIndex
{
    [CompilerGenerated] get { return _003CcurrentZoneIndex_003Ek__BackingField; }
    [CompilerGenerated] set { Network_003CcurrentZoneIndex_003Ek__BackingField = value; }
}
```

Here the property's own setter already goes through the generated network
property, so `zones.currentZoneIndex = x` from a mod syncs. Check which of
the two shapes you have before writing.

**Hooked SyncVar.** `[SyncVar(hook = "OnIsAvailableChanged")]` plus a
generated `_Mirror_SyncVarHookDelegate__003CisAvailable_003Ek__BackingField`
field. The hook runs on clients when the value arrives.

**Sync collections.** `SyncDictionary<,>`/`SyncList<>` fields, registered
in the constructor with `InitSyncObject(_useCountByPlayer)`. Writes to them
sync without a property.

**Command** (client → server). Three pieces:

```csharp
[Command(requiresAuthority = false)]
public void CmdActivateEditSkillAction(HeroSkillLocation? skillLoc, GemLocation? gemLoc, NetworkConnectionToClient sender = null)
{
    NetworkWriterPooled writer = NetworkWriterPool.Get();
    writer.WriteNullableHeroSkillLocation(skillLoc);
    writer.WriteNullableGemLocation(gemLoc);
    SendCommandInternal("System.Void EditSkillShrine::CmdActivateEditSkillAction(...)", -626233092, writer, 0, requiresAuthority: false);
    NetworkWriterPool.Return(writer);
}

protected void UserCode_CmdActivateEditSkillAction__Nullable_00601__Nullable_00601__NetworkConnectionToClient(
    HeroSkillLocation? skillLoc, GemLocation? gemLoc, NetworkConnectionToClient sender)
{ /* the real body, runs on the server */ }

protected static void InvokeUserCode_CmdActivateEditSkillAction__...(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
{ /* reads arguments, calls UserCode_ */ }
```

The method with the original name is now only the **sender stub**. The
original body moved to `UserCode_<Name>__<ArgTypes>` (`Nullable_00601` is
`` Nullable`1 ``), and `InvokeUserCode_…` is the receiver, registered in the
static constructor with `RemoteProcedureCalls.RegisterCommand`. To act on
what the server does when a vanilla client sends a command, patch the
`UserCode_` method, not the stub: the stub runs on the client, which
doesn't have your mod. Chat is the classic case: messages reach the server
through `ChatManager.UserCode_CmdSendChatMessage__String__NetworkConnectionToClient`.

**ClientRpc / TargetRpc** (server → clients / one client). Same split: the
named method is a stub calling `SendRPCInternal` /
`SendTargetRPCInternal`; the body is in `UserCode_…`; the receiver checks
`NetworkClient.active` (`Debug.LogError("RPC RpcShowNotice called on server.")`).
Names don't always tell you: the game's prefixes include `Rpc…`, `Tpc…`
(`TpcShowLocalUseEffect`, `TpcStartEditSkill`), but also `InvokeOnRoomLoaded`
and `CallOnCreateAsMiniBoss`, which are `[ClientRpc]`s too. Trust the
attribute and the `Send…Internal` call, not the name.

**`[Server]`/`[Client]` guards.** The weaver prepends:

```csharp
if (!NetworkServer.active)
{
    Debug.LogWarning("[Server] function 'System.Void PickupManager::DropGold(...)' called when server was not active");
    return;
}
```

Any method that starts like this is server-only. A mod may call it only on
the host.

**Markers.** `private void MirrorProcessed() { }` in every weaved class, and
`GeneratedNetworkCode._Write_EventInfoLoadRoom`/`_Read_…` (in
`Dew.Core/Mirror/GeneratedNetworkCode.cs`) for struct serialization. The
writer shows exactly which fields of a struct cross the network.

### 6.2 Lambdas and closures

The compiler turns lambdas into methods and captured variables into fields
of a generated class. ILSpy puts them back, so you usually read normal
C#, but the names appear in the type list, in stack traces and in IL scans:

| Generated name | Source construct |
|---|---|
| `<>c` | Cache class for lambdas that capture nothing |
| `<>c__DisplayClass14_0` | Closure: holds variables captured by lambdas in method #14 |
| `<OnCreate>b__12_0` | Body of a lambda written in `OnCreate` |
| `<OnCreate>g__GetStartEndPos\|0_2` | Local function `GetStartEndPos` inside `OnCreate` |
| `<Prop>k__BackingField` (ILSpy: `_003CProp_003Ek__BackingField`) | Auto-property's field |

Two decompile shapes to recognize:

```csharp
spawnMonsterSettings.beforeSpawn = (Action<Entity>)Delegate.Combine(spawnMonsterSettings.beforeSpawn, (Action<Entity>)delegate(Entity e)
{
    ((Monster)e).Networktype = Monster.MonsterType.MiniBoss;
    ((ISpawnableAsMiniBoss)e).OnBeforeSpawnAsMiniBoss();
});
```

is the source's `settings.beforeSpawn += e => { … };` on a struct copy.
And `obj.EntityEvent_OnDeath += new Action<EventInfoKill>(HandleDeathDrop);`
is `+= HandleDeathDrop`. Lambdas passed to `Dew.CreateActor` and
`CreateAbilityInstance` (`delegate(Ai_HunterArtillery_Small ai) { ai.NetworkstrengthMultiplier = strength; … }`)
run before the actor is spawned to clients (`beforePrepare`), which is why
the game sets synced fields there.

### 6.3 Coroutines and async

A method returning `IEnumerator` with `yield` is compiled into a nested
state-machine class whose `MoveNext()` holds the body. ILSpy normally
restores the `yield` form:

```csharp
private IEnumerator SmallArtilleryRoutine()
{
    int currentHuntLevel = NetworkedManagerBase<ZoneManager>.instance.currentHuntLevel;
    ...
    yield return new WaitForSeconds(UnityEngine.Random.Range(initDelay.x, initDelay.y));
    while (true)
    {
        ...
            if (endWhenRiftOpen && Rift.instance.isOpen && !Rift.instance.isLocked)
            {
                yield break;
            }
```

but the class exists under its compiler name, and that's the name you meet
outside the decompile:

```
$ ilspycmd -l c "$M/Dew.Core.dll" | grep RoomMod_Hunted
Class RoomMod_Hunted.<BigArtilleryRoutine>d__32
Class RoomMod_Hunted.<SmallArtilleryRoutine>d__31
```

| Generated name | Source construct |
|---|---|
| `<SmallArtilleryRoutine>d__31` | Iterator method `SmallArtilleryRoutine` |
| `<<ClearRoom>g__Routine\|92_0>d` | Iterator *local function* `Routine` inside `ClearRoom` |
| `<>c__DisplayClass22_0/<<OnInteract>g__Routine\|0>d` | Iterator local function that captured locals |
| `_003CLobby_UpdateAttribute_003Ed__85 : IAsyncStateMachine` | `async` method |

Consequences:

- **Stack traces** in `Player.log` name the state machine
  (`Type+<Method>d__N.MoveNext ()`). The code that failed is the iterator
  `Method`. Last Stand's hunter-zone artillery bug showed up exactly so: a
  `NullReferenceException` in `RoomMod_Hunted`'s artillery routines, which
  the code above explains (`Rift.instance` is null in an arena with no
  rift while `endWhenRiftOpen` is set).
- **Patching** a coroutine's body means patching `MoveNext` of the nested
  class (Harmony's `AccessTools.EnumeratorMoveNext`), not the method that
  creates it. Often there's a better seam: Last Stand cleared the
  `endWhenRiftOpen` field before the modifier started instead
  (`03-hooking-with-harmony.md`).
- Local variables of a coroutine are **fields** of the state machine. The
  `currentHuntLevel` above is read once at start; changing the hunt level
  mid-routine doesn't affect it.
- **async** methods that ILSpy can't restore come out raw. In
  `GameSettingsManager`, the `[Server]`-guarded `UniTask` methods show the
  state machine struct and a `MoveNext` with `_003C_003E1__state` (the
  state) and `_003C_003E4__this` (`this`). Read the `switch` on the state
  as the sequence of `await`s.

### 6.4 Serialized fields: the initializer is not the value

Unity serializes `public` fields and `[SerializeField]` private fields of
`MonoBehaviour`s and `ScriptableObject`s into prefabs and assets. The
value in the prefab replaces the C# initializer when the object loads.

```csharp
public int maxUseCountPerPlayer = -1;     // Shrine.cs
public float useCooldown = 1f;
```

The Shrine of Ascension's prefab has `maxUseCountPerPlayer = 4` and
`useCooldown = 0`. The `-1` and `1f` in the code are only defaults for
prefabs that don't set them. Every number you read in a serialized field
is a question for the asset (`05-assets-and-data.md`), not an answer.

What isn't serialized:

- `[NonSerialized] public` fields: runtime only, start at the initializer.
  `Shrine_MawOfDoom.priceMultipliersByPlayer` is
  `[NonSerialized] public Dictionary<string, float>`; it exists only on the
  machine that fills it (the host), so clients' tooltips never see it.
- Fields of types Unity can't serialize, unless the class uses **Odin**
  (Sirenix). `SerializedScriptableObject`/`SerializedMonoBehaviour`
  subclasses serialize dictionaries and more through Odin.
  `DewGameplayExperienceSettings`, `DewLocalizationBuildData` and
  `DewResourceDatabase` are Odin-serialized.
- `static` fields.

`Formula` is a struct holding a string (`public string formula;`) with an
`Evaluate(float x)` method and an implicit conversion from `string`, which
is why you see `public Formula generalServiceGoldPriceMultiplierByZoneIndex = "1 + 0.25*x";`.
The real formula is again in the asset.

`[SaveVar]` marks fields saved into the mid-run save; `[FormerlySerializedAs("maxUseCount")]`
tells you a field was renamed (old prefabs still load).

---

## 7. Decompiler pitfalls

| Artifact | Example in this game | What it means |
|---|---|---|
| Inverted conditions | `if (!(hero == null) && hero.level < hero.maxLevel)`; `if (!(target.owner == null))` | `hero != null`. ILSpy keeps the IL's branch sense |
| Generic locals | `float num = ...; bool flag = ...;` | Names were lost; use `-usepdb` or rename while reading |
| Misleading local names | `bool num = gem != null && ...` (`EditSkillManager`) | `num` is just a slot name, here a bool |
| Inlined constants | `int num = (IsUnderPerfPressure() ? 1 : 3);` | `3` is `kTargetDropOrbCount`. Search the value too |
| Enum compared to 0 | `if (affordType != 0)` | `AffordType.Yes` is 0, so "can't afford". Check the enum's order |
| Explicit casts for overloads | `CreatePickupInstance(position, (Quaternion?)null, default(CastInfo), (Action<Pickup_LargeGoldOrb>)_applyPendingGoldOrb)` | Noise; the source likely had none |
| `(object)x != null` | `if ((object)asset != null && asset is ISpawnableAsMiniBoss)` | A raw reference check that **skips** Unity's `==`, so a destroyed object passes it |
| Pattern noise | `obj.victim is Monster { disableLoot: not false }` | `victim is Monster m && m.disableLoot` |
| Named-argument reorder | `Mathf.Lerp(t: ..., a: ..., b: ...)` | ILSpy keeps evaluation order by naming args. Same call |
| Increment of a synced property | `int num = currentZoneIndex; Network_003CcurrentZoneIndex_003Ek__BackingField = num + 1;` | `currentZoneIndex++` |
| `ref` to a property | `GeneratedSyncVarSetter(value, ref currentZoneIndex, 4uL, null);` | Doesn't compile; the IL refs the backing field |
| Properties vs fields | `public bool didClearRoom { get; private set; }` | In IL it's `get_didClearRoom`/`set_didClearRoom`; Harmony and reflection need the right kind (`AccessTools.Property` vs `Field`) |
| Empty stubs | `Mon_Despair_BossCrawler : BossMonster` has only `.ctor` and `MirrorProcessed` | A class name in `strings` or `-l c` is not proof of content. Check for behavior and a prefab |

Two more traps:

- **Optional parameters don't mean optional work.** `SpawnMiniBoss(settings, ent = null, se = null)`
  looks like passing `ent` skips selection. It doesn't (8.3).
- **Simplified notes drift from code.** A note saying kill gold is skipped
  "for hunters" is close, but the code is
  `obj.victim.Status.TryGetStatusEffect<Se_HunterBuff>(out var effect) && !effect.enableGoldAndExpDrops`:
  a hunter with `enableGoldAndExpDrops` set does drop gold. Record the
  exact condition.

---

## 8. Worked examples

Each one starts from a question Last Stand had, and ends with a fact.

### 8.1 How kill gold is computed

**Question.** The mod wanted an optional cap on gold per kill. Where is the
amount decided, and what does it depend on?

1. Start from the visible effect: gold orbs drop from monsters. `PickupManager`
   is the obvious owner. Its `OnStartServer` subscribes to new entities, and
   `HandleEntityAdd` subscribes every `Monster`'s death:

   ```csharp
   if (obj is Monster)
   {
       obj.EntityEvent_OnDeath += new Action<EventInfoKill>(HandleDeathDrop);
   }
   ```

2. `HandleDeathDrop` (server only, because it's subscribed in `OnStartServer`):

   ```csharp
   if ((obj.victim.Status.TryGetStatusEffect<Se_HunterBuff>(out var effect) && !effect.enableGoldAndExpDrops)
       || obj.victim is Monster { disableLoot: not false })
   {
       return;
   }
   ...
   int killGoldAmount = NetworkedManagerBase<GameManager>.instance.GetKillGoldAmount(obj.victim);
   if (flag)   // IsUnderPerfPressure(): low FPS in the last 5 s
   {
       Pickup_BaseGoldOrb.GrantGold(killGoldAmount, null, isKillGold: true, isGivenByOtherPlayer: false);
   }
   else
   {
       DropGold(isKillGold: true, isGivenByOtherPlayer: false, killGoldAmount, obj.victim.position);
   }
   ```

3. Down into `GameManager.GetKillGoldAmount`:

   ```csharp
   float num = ges.killGoldMultiplierByZoneIndex.Evaluate(NetworkedManagerBase<ZoneManager>.instance.currentZoneIndex);
   num *= ges.killGold.Get(monster.type);
   Monster.MonsterType type = monster.type;
   if (type == Monster.MonsterType.Lesser || type == Monster.MonsterType.Normal)
   {
       float num2 = GetAdjustedMonsterSpawnPopulation(1f, ignoreTurnMultiplier: true) * GetAdjustedMonsterWavesMultiplier();
       num = num / num2 * (float)DewPlayer.gamePlayers.Count;
   }
   else
   {
       num *= (float)DewPlayer.gamePlayers.Count;
   }
   num = GetAdjustedGoldAmount_Income(num);
   return DewMath.RandomRoundToInt(num * UnityEngine.Random.Range(1f - ges.killGoldDeviation, 1f + ges.killGoldDeviation));
   ```

   Inputs: zone index (not the wave, not `ambientLevel`), monster type,
   spawn population factor, player count, `goldIncomeMultiplier` (in
   `GetAdjustedGoldAmount_Income`), the zone economy curve (in
   `GetAdjustedGoldAmount`), ±deviation.
4. The amount is a pool for **all** players (`× gamePlayers.Count`). Where
   is it split? `DropGold` → `SpawnGoldOrbForAmount` → orbs with `target = null`
   → `Pickup_BaseGoldOrb.OnPickup` → `GrantGold`:

   ```csharp
   float num3 = (float)amount / (float)DewPlayer.gamePlayers.Count;
   foreach (DewPlayer gamePlayer in DewPlayer.gamePlayers)
   {
       float num4 = num3;
       if (isKillGold)
       {
           num4 *= DewBuildProfile.current.killGoldMultiplier;
           num4 *= gamePlayer.monsterKillGoldMultiplier;
       }
       int num5 = DewMath.RandomRoundToInt(num4);
   ```

5. Scan for other callers of `GetKillGoldAmount`: only
   `PickupManager::HandleDeathDrop`. And `DropGold` has 33 call sites
   (shrines, gems, `Mon_GoldenLizard_ElusiveLizard`…).

**Dead end avoided.** Capping in `DropGold` would have capped every gold
source in the game. `GetKillGoldAmount` is called once, only for kills,
and returns a plain `int`.

**Conclusion.** A Harmony postfix on `GameManager.GetKillGoldAmount` that
caps `__result` at `cap × player count` caps kill gold and nothing else;
the orb's split and the player's own multipliers still apply after it.
Gold Last Stand drops itself (bonus gold) uses `DropGold` directly and
skips the cap by construction.

### 8.2 How the Shrine of Ascension's price is computed, and which inputs are synced

**Question.** Can the host change the Ascension's price so that a vanilla
client's tooltip shows the same number the host charges?

1. `Shrine_Ascension.GetCost`:

   ```csharp
   private Cost GetCost(Rarity rarity, DewPlayer player)
   {
       float num = baseGoldCostByStartRarity.Get(rarity);
       num *= 1f + addedCostMultiplierPerUse * (float)GetUseCountByPlayer(player);
       return Cost.Gold(Mathf.RoundToInt(NetworkedManagerBase<GameManager>.instance.GetAdjustedGoldAmount_Cost_Service(num)));
   }
   ```

2. Down the chain in `GameManager`:

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

3. Who computes it? Up the chain: `GetCost` is called from both
   `GetTargetInfo` overloads, which are called on the **client** by
   `UI_Tooltip_EditSkillShrineTooltip` and `EditSkillManager` (with
   `DewPlayer.local`, for the tooltip and a local `CanAfford` check), and
   on the **server** by `UserCode_CmdActivateEditSkillAction` (to charge).
   Two independent computations; they agree only if every input agrees.
4. Classify every input:

   | Input | Where it lives | Same on clients? |
   |---|---|---|
   | `baseGoldCostByStartRarity` | Prefab field, `PerRarityData<float>` | Yes, from the asset. Host can't change it for clients |
   | `addedCostMultiplierPerUse` | Prefab field, plain `float` | Same |
   | `GetUseCountByPlayer` | `Shrine._useCountByPlayer`, `SyncDictionary<string,int>` | **Synced** |
   | `ges.*` | `GameManager.ges`, a local `Instantiate` of an asset in `ResetGameplayExperienceSettings` | Same asset; host edits don't sync |
   | `currentZoneIndex` | `ZoneManager`, SyncVar auto-property | **Synced** |
   | `goldCostMultiplier` | `GameManager`, `[SyncVar]` field | **Synced** |

5. Check the limits on the synced ones. Use count: `Shrine.CanInteract`
   returns `GetUseCountByPlayer(entity.owner) < maxUseCountPerPlayer`, and
   `maxUseCountPerPlayer` is a plain prefab field (4 on this prefab, client
   local). On the client, `EditSkillManager` ends editing every frame when
   `!interactable.CanInteract(DewPlayer.local.hero)`. So the count can't be
   pushed past 3 to raise the price; the window would close.
   `goldCostMultiplier` is shared by every service price (scan: `GameMod_Limbo`
   changes it for that mode).

**Conclusion.** The price is fully reproducible on vanilla clients. The
only knobs the host has are `NetworkgoldCostMultiplier` (moves every gold
price together) and the synced use counts (bounded by the client-local
cap). Last Stand keeps vanilla's formula, raises all gold prices together
through the multiplier, and resets the synced use counts with
`Shrine.ResetUseCounts()` to give uses back. The "uses left" text is
`maxUseCountPerPlayer - GetUseCountByPlayer(DewPlayer.local)`
(`GetEditSkillIndicatorRawText`), which is also why a shrine whose prefab
has `-1` there would show negative numbers.

### 8.3 Why `SpawnMiniBoss` throws in some rooms

**Question.** Last Stand spawns minibosses through
`RoomMonsters.SpawnMiniBoss(settings, ent, se)`, passing its own monster and
effect. In some arena rooms wave 10 failed with an
`ArgumentOutOfRangeException` from `RoomMonsters.SelectMiniBoss`. Why,
when the monster was given?

1. Read the body, not the signature:

   ```csharp
   public void SpawnMiniBoss(SpawnMonsterSettings settings, Entity ent = null, StatusEffect se = null)
   {
       SelectMiniBoss(out var entity, out var effect, settings.random);
       if (ent == null) { ent = entity; }
       if (se == null) { se = effect; }
       if (!(ent is ISpawnableAsMiniBoss)) { Debug.LogWarning("Invalid entity for mini boss spawn provided: " + ent.name); return; }
   ```

   Selection runs **first, unconditionally**.
2. `SelectMiniBoss`:

   ```csharp
   List<Entity> list2 = new List<Entity>();
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

3. `DewRandom.Range(int minInclusive, int maxExclusive)` returns
   `minInclusive` when `minInclusive >= maxExclusive`, so an empty list
   gives `list2[0]` → `ArgumentOutOfRangeException`. A room whose
   `defaultRule.pool` has no `ISpawnableAsMiniBoss` monster (a boss room's
   pool) always throws.
4. Scan callers of `SelectMiniBoss`: only `RoomMonsters::SpawnMiniBoss`. So
   replacing its result can't affect anything else.
5. While there, read the rest of `SpawnMiniBoss`: it builds a one-spawn
   `MonsterSpawnRule` with `isBossSpawn = true`, sets
   `Networktype = Monster.MonsterType.MiniBoss` in `beforeSpawn`, adds the
   effect and `Se_AutoDetectPresence` in `afterSpawn`, and on death starts a
   routine that drops `rewards.DropChaosReward(pos, isHighQuality: false)`
   unless `disableMiniBossRewards`. That's the miniboss "contract" the mod
   keeps by calling this method instead of building its own.

**Conclusion.** A prefix on `SelectMiniBoss` that, while Last Stand is
spawning, fills the two `out` parameters with the mod's pick and skips the
original. The mod sets the pick right before calling `SpawnMiniBoss` and
clears it in a `finally`.

### 8.4 How the top-right location label builds its text

**Question.** Can the top-right label (`1. Forest of …`) show the wave
number to every player?

1. Visible format: `<number>. <zone name>`. Grep `Dew.UI` for it:

   ```sh
   rg -l '"\{0\}\. \{1\}"' decomp/Dew.UI
   # decomp/Dew.UI/UI_InGame_Location.cs
   ```

2. The whole class:

   ```csharp
   public class UI_InGame_Location : MonoBehaviour
   {
       private TextMeshProUGUI _text;

       private void Start()
       {
           _text = GetComponent<TextMeshProUGUI>();
           NetworkedManagerBase<ZoneManager>.instance.ClientEvent_OnRoomLoaded += new Action<EventInfoLoadRoom>(OnRoomLoaded);
           GameManager.CallOnReady(delegate { OnRoomLoaded(default(EventInfoLoadRoom)); });
       }

       private void OnRoomLoaded(EventInfoLoadRoom _)
       {
           ...
           Special_RoomAnnouncer special_RoomAnnouncer = UnityEngine.Object.FindObjectOfType<Special_RoomAnnouncer>();
           if (special_RoomAnnouncer != null && special_RoomAnnouncer.setRoomName)
           {
               _text.text = (special_RoomAnnouncer.showZoneIndex ? (...currentZoneIndex + 1 + special_RoomAnnouncer.zoneIndexOffset).ToString() : "(?)")
                   + ". " + DewLocalization.GetUIValue(special_RoomAnnouncer.key + "_Name");
           }
           else
           {
               _text.text = string.Format("{0}. {1}", ...currentZoneIndex + 1, DewLocalization.GetUIValue(...currentZone.name + "_Name"));
           }
       }
   }
   ```

3. Classify: it's a `MonoBehaviour` in `Dew.UI`, so it runs on each client
   with the client's code. Inputs: `currentZoneIndex` (SyncVar),
   `currentZone.name` (synced zone), a scene object `Special_RoomAnnouncer`
   (some rooms bring their own name and index offset), localization by
   key. When: `ClientEvent_OnRoomLoaded` and once at start.
4. Up from `ClientEvent_OnRoomLoaded`: it's invoked in
   `UserCode_InvokeOnRoomLoaded__EventInfoLoadRoom`, the body of the
   `[ClientRpc] InvokeOnRoomLoaded`, sent by the server when a room loads.
5. Try each lever, and why it's a dead end:
   - **Set the text on the host.** A Harmony patch only runs where the mod
     is loaded. The host sees the wave; clients don't. Last Stand's early
     builds did this and dropped it so everyone sees the same.
   - **Change `currentZoneIndex`.** Synced, but it's the run's difficulty
     index: 86 lines of the decompile use it (curves, prices, rewards).
   - **Re-send `InvokeOnRoomLoaded`.** Redraws the label, but
     `ClientEvent_OnRoomLoaded` has 42 subscriptions in the decompiled code,
     and every one of them would react to a fake room load.
   - **Inject free text.** The only strings that reach the label are
     localized zone or room names chosen by key; there's no free-text input.

**Conclusion.** The label can't show the wave for vanilla clients. Last
Stand puts the wave in the quest tracker instead, whose progress line is a
synced string (`08-config-ui-and-commands.md`).

**Where else these apply:** any "can I change X for everyone" question
follows 8.2 and 8.4: find who computes X, on which machine, and classify
each input as synced, asset-identical or host-only. Any "why does the game
throw" question follows 8.3: read the body top to bottom, find the
unconditional step, scan its callers.

---

## 9. Recording findings

Decompiled facts are expensive to get and easy to lose. Keep a
**confirmed facts** list in the repo (Last Stand keeps it in `AGENTS.md`
under "Confirmed API facts"), one bullet per subsystem, with exact member
names so a later grep finds both the note and the code.

Pattern for an entry:

```markdown
- **Topic.** `Type.Member(signature)` does X when Y. Inputs: `a` (synced),
  `b` (prefab), `c` (host-only). Called from `Caller1`, `Caller2` (scan,
  build r.1.4.0.13). Trap: Z. Evidence: Dew.Core/Shrine.cs `DoPostUseRoutines`.
```

A real one, from Last Stand:

```markdown
- **Minibosses.** `RoomMonsters.SpawnMiniBoss(settings, ent, se)` always calls
  `SelectMiniBoss` first, which indexes the room's `defaultRule.pool` entries
  that are `ISpawnableAsMiniBoss` and throws `ArgumentOutOfRangeException`
  when there are none, even when `ent` and `se` are given.
```

Rules that keep the list trustworthy:

1. **Exact names.** `Shrine._useCountByPlayer` is a private
   `SyncDictionary<string, int>`, not "the shrine's use counter". Include
   visibility (patching a private method needs its name as a string) and
   whether it's a field or a property.
2. **Say where the fact came from**, because the kinds differ in
   strength. Last Stand's notes use: "checked in the decompiled code",
   "read from the asset bundle" (prefab values, `05-assets-and-data.md`),
   "scanned the IL of Dew.Contents", and "seen in play" (with the log line
   or wave number).
3. **Say what's synced.** For every input, note SyncVar, sync collection,
   RPC-carried, prefab-identical, or host-only. That column decides what a
   host-only mod can do (`04-host-only-networking.md`).
4. **Keep the trap.** "`maxUseCountPerPlayer` is not synced" is worth more
   than the formula it sits in.
5. **Separate "confirmed in code" from "needs an in-game check".** Code
   tells you what should happen; only the game proves it
   (`09-debugging-and-testing.md`). Last Stand keeps a numbered in-game
   checklist next to the facts.
6. **Re-verify after updates.** Note the build, re-export, `diff -r` the
   old and new decompile, and re-run the IL scans your facts depend on.
