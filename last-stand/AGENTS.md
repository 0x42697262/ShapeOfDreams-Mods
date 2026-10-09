# AGENTS.md: Last Stand

Working notes for anyone, human or agent, building this mod. The design lives
in [`docs/`](docs/README.md), one topic per file. Open the table of contents
and read only the files for the area you're changing.

## Status

Playable and played. Many items under "Needs an in-game check" have been
seen working; the newest ones (the highest numbers) are still untested.

`about/icon.png` (512×512) and `about/preview.png` (1024×576, under Steam's
1 MB limit) are Last Stand's own. `about/description.txt` is the Workshop
description (BBCode). Their sources are in `art/`: `art/build.sh` runs
`art/icon/icon.py` and `art/preview/preview.py` (SVG from code, with the
cropped screenshots beside it) and renders both into `about/` with
`rsvg-convert` and ImageMagick. `art/preview/crop.sh` cuts the tiles from the
full screenshots in `art/preview/raw/`. The output matches the shipped files pixel
for pixel.

**Workshop upload.** The game's mod manager uploads the whole mod folder
(`SteamUGC.SetItemContent(item.path)`), so never upload from the repo: it
would ship `.git`, `src/`, `obj/` and the docs. `./pack.sh` builds
`dist/LastStand/` from the Release build: `LastStand.dll` plus `about/`,
with `metadata.json` pointing at `LastStand.dll`. The first upload writes
`about/publishedfileid.txt` into the uploaded folder, and the game reads it
to update the same item later; `pack.sh` copies it back to `about/` (commit
it) and into every new pack. Only one folder with the mod's id may be in
`Mods/` at a time (`IdConflict`), so the `laststand` incus device is pointed
at `dist/LastStand` for the upload and back at the repo afterwards. A new
item is created Private.

**Change notes.** The game submits every upload with no change note
(`SteamUGC.SubmitItemUpdate(handle, null)` in `DewMod.UpdateItem`). Last
Stand fills it in: a prefix on `DewMod.UpdateItem` reads
`about/changenote.txt` from the uploaded folder when the item is Last
Stand's own (same id as the loaded mod), and a prefix on
`SteamUGC.SubmitItemUpdate` puts it in place of the `null`
(`WorkshopChangeNotePatches`, `WorkshopChangeNote`). `SubmitItemUpdate`
runs before the method's first `await`, so it happens inside the same
call; a finalizer clears the note afterwards. These patches work outside
Last Stand mode, since uploads happen from the title screen. `pack.sh`
copies the file into the pack and prints it. Write the next update's note
in `about/changenote.txt` (Steam BBCode) before packing, and commit it. It
appears on the item's Change Notes page.

## Sister project

[Dream Arena](../dream-arena) is a host-only PvP mode for the same game, built
the same way. Much of Last Stand's plumbing exists there and should be adapted,
not reinvented:

| Need | Dream Arena source |
|---|---|
| One boss room for every node | `src/ArenaRules.cs`, `src/Patches/WorldGenPatch.cs` |
| Room loads cleared, lit, no entry trigger | `src/Patches/PlainArenaPatch.cs` |
| Boss removal, moving players into the arena | `src/ArenaStage.cs` |
| Arena center (`pathablePivot` of the biggest section) | `src/ArenaSpawn.cs` |
| Skipping intro cutscenes | `src/Patches/RoundStartPatch.cs` |
| Spawning memories and essences | `src/CampShops.cs`, `src/ShopCatalog.cs` |
| Fixing unsynced shrine limits | `src/Patches/CampRemnantPatch.cs` |
| Chat notices and chat commands | `src/MatchRestart.cs`, `src/Patches/ChatRestartPatch.cs` |
| Lobby settings window | `src/ArenaConfigWindow.cs` |
| Config | `src/DreamArenaConfig.cs` |

**Do not copy** Dream Arena's lucid-dream, game-modifier or PvE suppression.
Last Stand keeps all of those vanilla. The one exception is *room* modifiers
on arena nodes. Like Dream Arena, Last Stand strips them, and it also blocks
new ones (`ModifierGate`), because they put shrines, props, rifts and
minibosses into the arena.

**Hidden test command.** `LastStandConsole.Cmd_TestBoss` (spawns a weak heroic
boss for testing) is kept but not registered. `DewMod` only registers methods
that carry `[ConsoleCommand]`, so to use it again, put back
`[ConsoleCommand(CommandText.TestBossDescription, CommandText.TestBoss)]` on
both overloads.

## Hard constraints

- **Host-only.** Clients run the unmodded game. Only server-side logic and synced
  state reach them. Anything drawn per client, such as HUD text, lighting, fog
  or post-processing, cannot be changed for them.
- **Check whether a field is synced before relying on it.** Setting a plain
  field on the host does nothing for clients. Known traps:
  - `Shrine.maxUseCountPerPlayer` and `Shrine.useCooldown` are not synced.
    Reset the synced use count and `_lastUseTime` after each use instead.
  - `DewQuest` titles and descriptions are not synced, only set by key
    through the `SetLocalizedTitle`/`SetLocalizedDescription` RPCs.
    `currentProgress` and `step` are synced strings.
- **Use `.softInstance`, never `.instance`, in per-frame code.**
  `NetworkedManagerBase<T>.instance` runs `FindObjectOfType` twice when the
  manager is missing. That caused full scene scans every frame and host lag in
  Dream Arena.
- **Throttle retries.** Anything that waits for a room to settle retries on a
  timer with a cap, never every frame.
- **Mark it as gameplay-altering** (`instance.isAlteringGameplay = true`), as
  the official modding rules require.
- **Don't bypass the game's cross-play block** for modded lobbies.

## Environment

- The game and .NET SDK live in the incus container **`chen`**. Run commands
  with `incus exec chen -- su chicken -s /bin/bash -c '...'`.
- Game dir in `chen`: `~/.local/share/Steam/steamapps/common/Shape of Dreams`.
  Mods go in `Mods/<Name>`.
- Dream Arena is bind-mounted into `Mods/DreamArena` as incus device
  `dreamarena`. Last Stand is bind-mounted into `Mods/LastStand` as incus
  device `laststand`.
- `build.sh` and `watch-log.sh` are local to this machine and git-ignored.
  If they're missing, run `dotnet build` in the mounted directory inside
  `chen` directly.
- Build with `./build.sh` (Debug) or `./build.sh Release`. It runs
  `dotnet build` inside `chen`, in the mounted directory.
  `about/metadata.json` points at the Debug DLL.
- `./watch-log.sh` follows the game log filtered to this mod, Harmony and
  errors. `./watch-log.sh all` shows everything.
- There is no C# language server here. A clean build (0 warnings, 0 errors)
  is the check.
- Game log: `~/.local/share/Steam/steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log`
  (in `chen`).

## Reading the game's code

- Decompiled `Dew.Core` and friends: `~/sod-modding/decomp-new/` in `chen`.
- `Dew.Contents.dll` (monsters, room modifiers, skills, gems) won't export
  in full. Decompile one type at a time:
  ```sh
  export DOTNET_ROLL_FORWARD=Major PATH=$PATH:~/.dotnet/tools
  M="$HOME/.local/share/Steam/steamapps/common/Shape of Dreams/Shape of Dreams_Data/Managed"
  ilspycmd -r "$M" -t RoomMod_CorruptedChaos_AuraOfPain "$M/Dew.Contents.dll"
  ```
  Results go in `~/sod-modding/decomp-new/types/`. To find a type name, run
  `strings "$M/Dew.Contents.dll" | grep -i <word>`.
- Scene inspection: UnityPy venv at `~/sod-modding/venv`, with scripts
  `scan_scene.py` and `roomscripts.py`.

## Official modding docs

From the developer, Lizard Smoothie. The docs are early and brief, so the
decompiled code stays the source of truth.

| Resource | Link |
|---|---|
| Mod docs and starter guide | https://github.com/LizardSmoothie/ShapeOfDreamsModDocs |
| API reference (`Dew.Core`) | https://lizardsmoothie.com/sod/moddoc/api/Global.html |
| Modding Terms of Service | https://shape-of-dreams.gitbook.io/shape-of-dreams-modding-terms-of-service/ |
| Official example mods (with source) | https://steamcommunity.com/id/lizardsmoothie/myworkshopfiles/?appid=2444750 |
| Official server-side cheat console mod | https://steamcommunity.com/sharedfiles/filedetails/?id=3618192903 |
| JSON overrides example (SUPER LACERTA) | https://steamcommunity.com/sharedfiles/filedetails/?id=3618193300 |
| Shape of Dreams Workshop | https://steamcommunity.com/app/2444750/workshop/ |

What the docs say that matters here:

- **Code of conduct.** Mods may heavily alter gameplay, but must not hurt
  others' experience without consent, and must not grant illicit access to
  exclusive, server-generated items ("things you cannot buy in-game using
  stardusts"). Stardust itself isn't one; Last Stand gives it only at the
  vanilla boss-kill rate (heroic bosses). Every server-side mod that
  alters gameplay must set `instance.isAlteringGameplay = true`. This shows the
  MOD icon on the lobby and a notice to joining clients.
- **Loading.** The DLLs listed under `"assemblies"` in the mod's
  `metadata.json` are injected. Every `ModBehaviour` in the assembly is attached
  to one container GameObject per mod instance. `ModBehaviour` is the entry
  point and owns the lifecycle.
- **Unloading.** Mods can be loaded and unloaded at runtime, so clean up
  everything in `OnDestroy`. Loading or unloading mid-run can cause bugs.
- **Harmony** is built in. Use the `harmony` property of a `ModBehaviour`, and
  unpatch in `OnDestroy`.
- **Project template:** `<GameFolder>/Mods/ModTemplate`. Create the project in
  `Mods` with "Place solution and project in the same folder" so it references
  the game's assemblies.
- **Developer Mode** (gameplay settings) enables the debug console. **F1**
  cycles the log viewer mode, and **`` ` ``** opens the console. Mods can add
  console commands through a `ModBehaviour`. The console also has
  "Manage Mods", which opens the built-in mod manager anywhere.
- **JSON overrides** (`<GameFolder>/RawData/!ModResources/overrides`) change
  static parameters such as stats, damage and cooldowns. They sync from host to
  clients automatically and set `isAlteringGameplay` on their own. They cannot
  react to settings or game state, so they suit fixed tuning only.
- **Workshop upload** is in the mod manager with Developer Mode on. The first
  upload creates the item, later uploads update it. Every upload sets the item
  to Hidden until it is made Public again.

## Confirmed API facts

These were checked in the decompiled code.

- `RoomMod_CorruptedChaos_AuraOfPain`: 2 minibosses with
  `Se_LimboBossDecorator`, +20% health and +30% damage. Every 0.1 s it deals
  `PureDamage` (DamageOverTime and IgnoreArmor) of 1%/s of max health +
  shield, never below 1 HP. The damage stops on `room.didClearRoom`. When both
  minibosses die it spawns `Shrine_CorruptedChaos`, plus one per player on
  `doubleChaosChance`, which uses `playersOverride`.
- `PickupManager.DropDreamDust(isGivenByOtherPlayer, amount, position, target)`
  and `DropGold(isKillGold, isGivenByOtherPlayer, amount, position, target)`.
  Both are server-only.
- `Dew.CreateSkillTrigger(prefab, pos, level, tempOwner)` and
  `Dew.CreateGem(prefab, pos, quality, tempOwner)`.
- `Shrine_UpgradeWell : EditSkillShrine : Shrine`.
- **Reserved items.** `SkillTrigger` and `Gem` have a synced `tempOwner`.
  `IsLockedFor(player)` is true when `tempOwner` is someone else whose hero
  isn't dead or knocked out. `ItemWorldModel.IsVisible` hides locked items
  from the camera's `focusedEntity` owner, and the model refreshes on
  knock-out, revive, focus change and (`SkillWorldModel`, `GemWorldModel`) a
  `tempOwner` change. `DewPlayer.OnStopServer` destroys a leaving player's
  reserved items.
- `Shrine_AltarOfCleansing : EditSkillShrine`. Cleansing doesn't remove the
  item: it sets the memory's level to `GetCleanseSkillMinLevel` or the
  essence's quality to `GetCleanseGemMinQuality`, and drops the refund from
  `GetCleanseReturnedDreamDust`. The gold price, `GetCleanseGoldCost`, comes
  from `ges` curves times `GetAdjustedGoldAmount` (zone index only). It
  doesn't use the synced `goldCostMultiplier`, so it can't be raised in a way
  clients see.
- `Shrine_Ascension : EditSkillShrine`. For gold it raises an item one rarity
  (`GetNextRarity`: common to rare to epic to legendary to unique), swapping it for a
  random one of that rarity from `LootManager.poolSkillsByRarity` /
  `poolGemsByRarity` at the same level or quality, re-equipped in the slot.
  Prefab (read from the asset bundle): `maxUseCountPerPlayer = 4`,
  `maxTotalUseCount = -1`, `useCooldown = 0`, `addedCostMultiplierPerUse =
  0.1`, `baseGoldCostByStartRarity` common 30, rare 60, epic 120, legendary
  150, unique 30 (character and identity 0, rejected). Price:
  `RoundToInt(GetAdjustedGoldAmount_Cost_Service(base[rarity] × (1 + 0.1 ×
  GetUseCountByPlayer)))`, which includes the synced
  `GameManager.goldCostMultiplier`. Clients compute it themselves (tooltip and
  a local `CanAfford` check before sending the command); the server computes it
  again to charge. A use count at or above the client's unsynced
  `maxUseCountPerPlayer` makes `CanInteract` false, and `EditSkillManager`
  then closes the edit window every frame, so the count can't carry a price
  past +30%. Last Stand keeps the prefab limits and vanilla's price, and
  clears the counts every 5th wave. `goldCostMultiplier` is shared by
  every gold price (the Maw of Doom and Luck's prays included); with `Free
  gold shrines` on (off by default since schema 29) Last Stand sets it to 0
  in the arena (`GoldPrices`) to make them free, and the Altar of Cleansing,
  which doesn't use it, keeps its price.
- **Kill gold.** `PickupManager.HandleDeathDrop` drops
  `GameManager.GetKillGoldAmount(victim)` (unless a hunter or `disableLoot`):
  zone curve × `ges.killGold[type]`, divided by the spawn population factor for
  `Lesser`/`Normal`, times `gamePlayers.Count`, through
  `GetAdjustedGoldAmount_Income`. It doesn't scale with the wave or ambient
  level. An untargeted gold orb splits its amount by `gamePlayers.Count` and
  random-rounds each share, then applies `killGoldMultiplier` and the player's
  `monsterKillGoldMultiplier`. Last Stand's bonus gold uses `DropGold`
  directly, not `GetKillGoldAmount`.
- `Shrine_Destiny : EditSkillShrine, ICustomInteractable` (Dew.Contents).
  Essences only, no gold. Alt-interact opens the edit window; the chosen
  essence loses `GetRerollCost` quality (destroyed under 10) and the synced
  `currentItems[guid]` is rerolled to a random essence of at least its rarity.
  `rerollCount[guid]` is synced and raises the cost 25% each. Normal interact
  spawns the offered essence with `tempOwner` set and calls
  `DoPostUseRoutines`; sacrifices don't. Prefab (read from the bundle):
  `maxUseCountPerPlayer = 1`, `maxTotalUseCount = -1`, no cooldown, no gold.
- **Curses.** `CurseStatusEffect` ends on `progressType`: `Kills` (the
  hero's kills and assists within 8 s, a `BossMonster` counts 10), `Travel`
  (non-revisit traveling room loads), `TravelZone` (next world), or
  `Hidden`/`Collect` (removed by their source). `requiredAmount`,
  `currentProgress` and `progressType` are SyncVars. A knock-out destroys
  the curse unless `dontRemoveOnKnockOut`. `Shrine_Hatred.DoCurse` sets the
  type 50/50 Kills/Travel in the create callback, before `OnCreate`, which
  starts `Quest_KillToLiftCurse`; the quest copies type and remaining count
  only on step start and on `CurseEvent_OnProgressChanged`. Last Stand
  rewrites Kills/Travel/TravelZone goals to wave-sized kills in a postfix on
  `OnCreate` and fires that event.
- `Shrine_Hatred`: prefab 1 choice per player. `choices` (a synced
  `SyncList`) is rolled once in `OnCreate`. `CmdChoose` checks
  `CanInteract`, then `DoPostUseRoutines`, the curse, and a
  `Shrine_Hatred_Reward` for that player (gold, dream dust, memory, essence
  or Chaos; never platinum). Platinum only comes from the Seed of Torment and
  the mid-join catch-up (`DewPlayer`, + zone index).
- **Free shrines in the game.** `Shrine_ScaredMaw` gives +3 levels (or 3
  upgrades of essence quality) for nothing, with no risk.
  `Shrine_Entanglement` merges every other equipped memory or essence into
  the chosen one and destroys them, for nothing.
- `Shrine_MawOfDoom : EditSkillShrine`. Prefab: `maxUseCountPerPlayer = -1`,
  `maxTotalUseCount = -1`, `useCooldown = 0`, `goldCostByUseCount = "0+20*x"`,
  `destroyChanceByUseCount = "Min(0+0.08*x, 0.5)"` (`Formula`, a struct, not
  an allowed JSON override type), `x` = `GetUseCountByPlayer`. Price
  `RoundToInt(GetAdjustedGoldAmount_Cost_Service(20x) ×
  priceMultipliersByPlayer)`; that dictionary is `NonSerialized` and host-only.
  The server rolls the destroy chance in `OnActivateEditSkill` (essences get
  -0.03 under 0.2) and both memories and essences can be destroyed.
  `Shrine._useCountByPlayer` is a private `SyncDictionary<string, int>`; the
  host can write it by reflection and clients see the new count at once.
- **Modifier removal at game over.** `ZoneManager.RemoveModifier` writes a
  node's `SyncList`. Once the transport has shut down (game over, restart),
  that throws `InitSyncObject: IsWritable: neither NetworkServer nor
  NetworkClient are active`. `ArenaModifier.Remove` clears its own state first
  and only calls the game while `NetworkServer.active`; `HuntedZone.End` only
  puts the hunt level back then too. Seen in play at wave 170: the throw
  aborted the session reset halfway.
- **Mirage drops.** Neither `GameMod_MirageSkin` nor `MirageSkinEffect` drops
  anything. In Last Stand a Mirage monster rolls loot as its tier (regular or
  hunter).
- **JSON overrides** (`DewMod`): a mod's `overrides` folder is registered on
  load (and sets `isAlteringGameplay`), and the host sends its list to joining
  clients (`DewPlayer.TpcSendOverrides`). Targets must start with an allowed
  prefix (`Shrine_` is one), and a key is accepted only if the field's type is
  in `AllowedValueTypesForJsonOverride` (int, float, bool, arrays, lists and a
  few game structs). `PerRarityData<T>` isn't, so
  `Shrine_Ascension.baseGoldCostByStartRarity` can't be overridden;
  `addedCostMultiplierPerUse` and `maxUseCountPerPlayer` could be. Overrides
  apply to every run while the mod is loaded, not just Last Stand.
- `EntityAI` attacks any entity whose relation is `Enemy`, and
  `AI.Aggro(Entity)` forces a target.
- `GameManager.SetAmbientLevel(int)` is how the game scales monster strength
  over a run.
- Room state is saved into `ZoneManager.visitedNodesSaveData` on leaving and
  restored on revisit.
- Chat reaches the server through
  `ChatManager.UserCode_CmdSendChatMessage__String__NetworkConnectionToClient`.
  `MessageType.Raw` notices render as-is for vanilla clients.
- **Mode selection.** Solo: `UI_Title_TitleView.EnterSingleplayerOptions`
  shows the `GameMode` state (a `VerticalLayoutGroup` with "Shape of Dreams",
  "Limbo", "Go Back" buttons in scene `level2`) only when
  `Dew.IsGameModifierIncludedInGame("GameMod_Limbo")`. Otherwise it calls
  `TitleManager.EnterSingleplayerShapeOfDreams`. The Limbo button's label is
  `limboModeText`, set by code, with no `DewLocalizedText`. Its tooltip child
  has one. Multiplayer: `UI_Title_CreateLobbyView` rebuilds its dropdown in
  `OnShow`, and its private `_state` (`DewNetworkStartSettings`) is what
  `PlayGame` receives.
- **`customGameSettingsSaveKey`** goes from the start settings into
  `GameSettingsManager.gameSettingsSaveKey` (a SyncVar) on `Awake`, and
  `DewNetworkManager.RestartAsync` copies it back. Last Stand uses it as the
  mode flag.
- **Spawning.** `RoomMonsters.SpawnMonsters(SpawnMonsterSettings)` runs a
  coroutine. `spawnPosGetter` picks the point, and `afterSpawn` gets the
  entity. A rule with `isBossSpawn` forces one wave, one spawn, no hunter
  buff and no population wait. `RoomMonsters.SpawnMiniBoss(settings, ent, se)`
  sets `Monster.type = MiniBoss`, calls the `ISpawnableAsMiniBoss` hooks, adds
  the `MiniBossEffect` and drops `RoomRewards.DropChaosReward` on death unless
  `disableMiniBossRewards`. Monsters spawn at `GameManager.ambientLevel`.
- **Spawn points.** `RoomSection.monsters` is a `Section_Monsters`.
  `GetSpawnPositionInSection(Func<Vector3, bool> evaluator, random)` tries 30
  random points in the section. It applies the evaluator to the first 10, and
  takes the first that passes `RoomMonsters.FilterSpawnPosition` (on the
  navmesh and reachable from the section's `pathablePivot`). `SpawnMonsterImp`
  sends each new monster toward the nearest hero.
- **Loot.** `LootManager.SelectSkillRarity(isHigh)`,
  `SelectSkillAndLevel(rarity, out skill, out level)`,
  `SelectGemRarity(isHigh)` and `SelectGemAndQuality(rarity, out gem, out quality)`
  use the run's pools. Items on the ground have `owner == null` and
  `handOwner == null`.
- **Mod config files.** `DewMod` calls `ModBehaviour.LoadConfigsToDisk()` (virtual)
  once on load. It reads `GetModConfigFilePath(field)`
  (`persistentDataPath/QuickSave/Mods/<id>/<Type>.<field>.json`), or writes
  defaults if the file is missing, using Newtonsoft through
  `DewPersistence.FromJson`. Missing keys keep their initializer value. The
  config screen (`DewGUI.CreateWidgetsForObject`) skips fields marked
  `[HideInInspector]`. For every other field it uses the first entry of the
  public `DewGUI.fieldBuilders` list (condition on type and `FieldInfo`, then a
  builder returning `FieldBuildResult`) that accepts it. Inserting at index 0
  overrides the stock widget. The list is cleared only at
  `SubsystemRegistration`, before mods load. `ModConfig.Clone()` and
  `CopyTo()` copy every field by reflection. `UI_Toggle.isChecked` raises
  `onIsCheckedChanged` even when set from code.
- **Shrines.** `Shrine.maxTotalUseCount` is a SyncVar. Every successful use ends
  in `Shrine.DoPostUseRoutines`, including `EditSkillShrine` edits.
  `Shrine_UpgradeWell` charges `GameManager.GetSkillUpgradeDreamDustCost` and
  `GetGemUpgradeDreamDustCost` in dream dust.
- **Camp rift.** `Rift_RoomExit.nextNodeIndex` is a SyncVar. When it is set, the
  rift's interaction confirms and calls `CmdTravelToNode` instead of opening
  the world map. `ZoneManager.ShouldVoteOnTravel()` is the host's only test for
  turning a travel into a world-map vote.
- **Hunters.** `Se_HunterBuff` sets `Monster.isHunter`, doubles population
  cost, adds speed, attack speed and haste, enables shadow walk and turns off
  gold and exp drops. `RoomMonsters` gives it to normal spawns by
  `addedHunterChance`, but not to `isBossSpawn` rules.
- **Hunter zone.** `RoomMod_Hunted` (Dew.Core) is the hunted-room modifier. On
  `OnStartServer` it reads `ZoneManager.currentHuntLevel` (a public, synced
  property) to scale its formulas, adds hunter and Mirage chance to the room,
  buffs `Se_HunterBuff` spawns and runs the small and big artillery coroutines
  (`Ai_HunterArtillery_Small` and `_Big`). `ZoneManager.AddModifier<T>(node)`
  on the current node calls `RoomModifiers.HandleRuntimeAddition`, so it goes
  live mid-room. It returns the modifier id, and `RemoveModifier(node, id)`
  takes it back out. If the added modifier `isMain`, `AddModifier` first
  removes the node's other main modifier.
- **Hunter zone artillery.** `RoomMod_Hunted.endWhenRiftOpen` is 1 on the
  prefab, and both artillery routines dereference `Rift.instance` (null in
  the arena) when it's set, so they die with a `NullReferenceException` on
  the first check (seen in `Player.log`, waves 101–155). Last Stand clears the
  field in `beforePrepare`.
- **Hunt level and threat level.** `ZoneManager.currentHuntLevel` only rises by
  itself on travel into a hunted node. At 5 or more, `GameMod_Obliviax` starts
  `Quest_HuntedByObliviax` on a traveling room load, and the quest ends in
  `Ge_Obliviax_InterruptTravelAndKidnap`. Both check `isTraveling`, so raising
  the level inside the arena never triggers them. The HUD and the room-mod
  label clamp the level to 5. The "threat level" HUD
  (`UI_InGame_AmbientLevel`) is `GameManager.ambientLevel`, which has a
  minimum of 1, no maximum, and nothing reacting to its value.
- **Mirage skins.** `MirageSkinEffect` subclasses (`Se_MirageSkin_Delusion`,
  `_Armor`, `_Oblivion`, `_Oppression`, `_Pulverization`, `_Sanctification`)
  carry a `tier`. `GameMod_MirageSkin.RefreshPool` gives early zones Delusion
  only, then adds by tier. It skips bosses, `Lesser` monsters and monsters that
  already have a skin.
- **Chaos reward rarity.** `Shrine_Chaos.SetRandomRarity(isHighQuality, random)`
  sets the synced `rarity` from the prefab's `chance` or `highChance` table,
  with everything past common, rare and epic going to legendary. It runs in
  `CreateActor`'s `beforePrepare`, before `OnLateStartServer` builds `rewards`
  from `poolByRarity.Get(rarity)`. In play, miniboss chaos was legendary nearly
  every time. `Shrine_CorruptedChaos` is a separate `EditSkillShrine` with no
  rarity.
- **Main room modifiers.** Read from the asset bundle: both `RoomMod_Hunted`
  and `RoomMod_CorruptedChaos_AuraOfPain` have `isMain = 1` and
  `disableOtherEnvParticles = 1`. The Aura of Pain has `enableOuterDecorations`
  and a `fxLoop`. `RoomModifierBase.OnCreate` plays `fxLoop` and hooks events.
  `OnStart` places decorations and hides `EnvParticle`s. The Aura of Pain's
  own `OnCreate` adds the gameplay (extra minibosses, pop multiplier, stat
  bonus, shrines). Its damage tick stops on `room.didClearRoom`.
  `HandleRuntimeAddition` passes `beforePrepare` into `Dew.CreateActor`, so it
  runs before `OnCreate`. `HandleRuntimeRemoval` of an unknown id only logs.
- **Location label.** `UI_InGame_Location` (Dew.UI) sets its `TextMeshProUGUI`
  to `"{currentZoneIndex + 1}. {localized zone name}"` in the private
  `OnRoomLoaded`, called on `ClientEvent_OnRoomLoaded` and once at start
  through `GameManager.CallOnReady`. Its inputs are synced (`currentZoneIndex`,
  `currentZone`), but the text is built by each client and only redrawn on a
  room load, and only a real zone's name can appear. The redraw comes from the
  `InvokeOnRoomLoaded` RPC, which 42 other client listeners also react to, and
  `currentZoneIndex` drives about 86 game uses (curves, prices, rewards). So
  free text such as the wave can't be shown there for vanilla clients.
- **Quest tracker.** `DewQuest.currentProgress` is a synced string that
  `UI_InGame_QuestDisplay_Item` shows as-is beside the icon picked by the
  synced `progressType`. The title and short description are local, set from
  `<Type>_Title`/`_Description` in `OnCreate`, or by key through the
  `SetLocalizedTitle`/`SetLocalizedDescription` `ClientRpc`s, which fall back
  to `ui.!<key>` for an unknown key and aren't replayed to late joiners.
  `QuestType` is a plain prefab field. `Quest_Limbo` is an empty subclass,
  only started by `GameMod_Limbo`. Every update (`ClientQuestEvent_OnQuestUpdated`)
  replays the item's update flash. Destroying a quest that's still in
  progress removes the item with no complete or fail animation. UI strings
  live in `Resources/MainLocalization` (`DewLocalizationBuildData`, Odin
  binary, UTF-16 keys and values).
- **Modifier panel.** `UI_InGame_RoomModDisplay` (Dew.UI,
  `SingletonBehaviour`) shows the newest main modifier's name and description
  on `ClientEvent_OnActorAdd`. It is hidden only on
  `ClientEvent_OnRoomLoadStarted`, so it outlives a removed modifier. It's
  client-local.
- **Run goal.** `PlayGameManager.OnStartClient` starts `Quest_ShapeOfDreams` on
  the server for new runs. `GameMod_Limbo` simply `Destroy()`s it, which is
  the precedent for removing it. Quests are actors, so destruction syncs.
- **Boss kits.** Checked in Dew.Contents: Seeker spawns
  `Mon_DarkCave_SeekerHallucination`, Azurak `Mon_Despair_AzurakRollPillar`,
  Infernus `Mon_LavaLand_InfernusPillar` (all plain `Monster`s, spawned with the
  boss as parent). Nyx has phases and may spawn Erebos (`Ge_TheConsortOfNight`).
  `Mon_Ink_BossWhiteNight` spawns `Mon_Ink_BossDarkMoon`, which overrides
  `OnDeath`. `Mon_Despair_BossCrawler` does not exist. `EntityStatus.AddStatBonus`
  recalculates at once (`CalculateStats`) and keeps the health ratio.
  `DewDifficultySettings.ApplyDifficultyModifiers` runs from `ActorManager`'s
  entity-add event. Ability cooldowns use the owner's `Status.abilityHaste`
  when the config's `canReceiveCooldownReduction` is set: multiplier
  `lerp(ges.cooldownFloorRatioByAbilityHaste, 1, 1 / (1 + haste / 100))`.
- **World map.** `UI_InGame_WorldMap.RefreshNodes` skips nodes where
  `WorldNodeData.IsSidetrackNode()` (`position.x < 0`). Other uses of that
  test: `ZoneManager.GetHunterProgress` and the scoring in
  `TryGetNodeIndexForNextGoal`. Map items are indexed by node index with null
  gaps (`InGameUIManager.fullWorldMapNodeItems`), so hiding middle nodes is
  safe.
- **Mod reloads.** With developer mode and `enableAutoReloadMods` (in
  `QuickSave/r_platform.json`), any file change under `Mods/` reloads the mod
  mid-run (`DewMod` watcher; it waits for the window to have focus). `DewMod`
  loads the DLL from bytes (`Assembly.Load(byte[])`), so the old assembly stays
  in the AppDomain and `AppDomain.SetData` survives. Event handlers the old
  assembly attached to entities still fire into the old, reset session.
  Ownership that matters at death (heroic bosses) must not rely on in-memory
  sets alone.
- **Minibosses.** `RoomMonsters.SpawnMiniBoss(settings, ent, se)` always calls
  `SelectMiniBoss` first, which indexes the room's `defaultRule.pool` entries
  that are `ISpawnableAsMiniBoss` and throws `ArgumentOutOfRangeException`
  when there are none, even when `ent` and `se` are given.
- **Music.** `MusicManager` is a plain local `ManagerBase`. `Room.OnLateStart`
  plays `Room.music`, or the zone's `defaultMusicRef` when that's null and the
  node isn't `ExitBoss`. Boss tracks are played by scene `FxChangeMusic`
  objects (`BossMusicStart`) from `Section_Monsters.onStartCombatArea`. Their
  `DewMusicItem`s, read from the scenes with UnityPy: `musBossForestDemon`,
  `musBossSeeker`, `musDespairBoss`, `musBossNyx`, `musBossSkoll`,
  `musBossInfernus`, `musInkBoss`, `musInkBossSecond`, `musBossPrimus`.
  `DewMusicItem` is `[DewResourceLink(Guid)]`, so `GetByName` doesn't apply,
  and the database's `nameToGuid`/`guidToName` don't list them (checked in
  game). Their `resourceId` GUIDs (read with UnityPy from the scenes'
  sharedassets) are all in the Addressables `catalog.bin`.
  Clients can't be given a track: networked effects are addressed by path
  under a `NetworkIdentity` that already holds them.
- **Arena node modifiers.** World gen writes modifiers straight into node
  data. Seen on an arena node: `RoomMod_SpawnGuidance`,
  `RoomMod_StrayMemory_StartProp`, `RoomMod_HarderFightBetterReward`. Later
  additions all go through `ZoneManager.AddModifier(int, ModifierData,
  Action<RoomModifierBase>)`: Corrupted Chaos, `RoomMod_SpawnMiniBoss` on
  travel, `UpdateModifiersByHunterStatus` on travel, and
  `Se_HeroKnockedOut` → `RoomMod_HeroSoul`.
- **Hatred Shard.** `Shrine_Hatred_Reward.OnUse` (server) picks gold, dust,
  memory, essence or chaos by the instance's plain weight fields (`goldWeight`
  1, `dreamDustWeight` 1, `skillWeight` 1, `gemWeight` 1.25, `chaosWeight` 1;
  `Dew.SelectRandomWeightedIndexInParams` never picks a 0 weight). Dust is
  `GetSpecialRewardAmount_DreamDust()` × `dreamDustMultipliers[strength]`
  (0.8, 1.5, 2.4, 3.3) × ±`currencyDeviation` 0.15; gold uses
  `goldMultipliers` (0.5, 0.8, 1.2, 1.6). The special amounts come from the
  zone index only.
- **Dark Urge.** `Se_Curse_DarkUrge` (server) adds every other game player
  to `victim.owner.neutrals` and back, and scales damage between two human
  players' heroes by `damageMultipliers` (0.15, 0.25, 0.4 by strength) and
  `lowHpMultiplier` 0.35 under `lowHpRatio` 0.5, through `dealtDamageProcessor`
  and `takenDamageProcessor` (`DataProcessorGroup.Add/Remove`).
  `LucidDream_TheDarkestUrge` does the same relation change for the whole
  run. `Entity` treats `EntityRelation.Neutral` like an enemy for hits.
  Each curse's processor only skips damage it marked itself
  (`DamageData.IsAmountModifiedBy(this)`), so two cursed players cut each
  other's hits twice. `Actor.ProcessDealtDamage` runs before the target's
  `ProcessReceivedDamage`; `DataProcessorGroup` runs lower priorities first.
  `CurseStatusEffect.OnCreate` shows the center message and chat line on
  every client (unless `disableStartNotification`) and starts
  `Quest_KillToLiftCurse`, visible to the victim's owner, with the curse's
  localized name and text. `progressType` `Hidden` never counts kills or
  rooms. `OnDestroyActor` completes the quest unless the victim is down.
- **Environment modifiers.** Read from the asset bundle: every pool
  environment (`RoomMod_AcceleratedTime`, `_GoldEverywhere`, `_PureDream`,
  `_UnstableVeilOfTime`, the hazards…) has `isMain = 1`, like `RoomMod_Hunted`
  and the Aura of Pain. Most work through `ModifyEntities(onStart, onStop)`
  (status effects on every entity, new ones included, removed in
  `OnDestroyActor`). Loot ones (`GoldEverywhere`, `PureDream`,
  `LingeringAuraOfGuidance`, `StarCookie`, `InkStrikeWarning`,
  `UnstableRatSwarm`) only spawn their props when `isNewInstance`.
  `Room.didClearRoom` (`{ get; private set; }`, not synced) is set from
  `isRevisit` at room start, so it's true in the arena. Readers (scanned the
  IL of Dew.Contents with `Mono.CecilX`): Aura of Pain, StrayMemory,
  StardustEverywhere, `Se_RoomMod_WarpingField_Unstable`,
  `Ai_UnstableRatSwarm_Projectile`, `Se_LingeringAuraOfGuidance` (heals more
  when cleared), InkStrikeWarning (destroys itself when cleared), FireDevil,
  RiskOfMeteors, GazeOfErebos, `Mon_Sky_LittleBaam` (stops summoning),
  `Ge_Shrine_PotOfGreed`. In Dew.Core: `GameManager.IsEligibleForMidRunSave`,
  `RoomMonsters`, `RoomSection` (first entry with `clearRoomOnEnterFirstTime`
  calls `ClearRoom`), `Section_Monsters`, `RoomModifierBase.PlaceShrine`.
  `Room.ClearRoom` fires `onRoomClear` and destroys idle creep monsters.
  `RoomMonsters.OverrideMonsterType` (Leaf Hound Den, Meteoric Life) replaces
  `defaultRule` and is never undone. Population multipliers don't change an
  `isBossSpawn` rule's single spawn. `onAfterSpawn` runs for every
  `SpawnMonsters` spawn.
- **Knock-outs.** `Se_HeroKnockedOut.disableQuest` (public, NonSerialized)
  stops both the soul and `Quest_LostSoul` (started after 1 s in
  `ActiveLogicUpdate`). `Se_HeroKnockedOut.Revive(mult = -1)` sets health
  (default `reviveHealthMultiplier` 0.5) and destroys the effect, whose
  `OnDestroyActor` clears `isKnockedOut` and removes the soul.
  `Se_HeroKnockedOut.CheckAndAddHeroSoul` (public, server) runs
  on knock-out and on every room load. It skips `ExitBoss` nodes, then adds
  `RoomMod_HeroSoul` (clientData = owner guid) to the node from
  `TryGetNodeIndexForNextGoal(lostSoulDistance)`. `RoomMod_HeroSoul.OnStartServer`
  spawns `Shrine_HeroSoul` at `props.TryGetGoodNodePosition`. `AddModifier`
  on the current node with an active room creates the modifier at once
  (`HandleRuntimeAddition`). The knock-out's `OnDestroyActor` removes the soul
  from every node.
- **Heroic bosses.** "Heroic boss" is the game's name for `BossMonster`
  (`Mon_*_Boss*` in Dew.Contents). Boss rooms spawn them through the entry
  cutscene (`DewBossRoomEntry` → `DewCutsceneDirector`). `BossMonster.OnCreate`
  sets `midJoinWaitType = FightingBoss`. `OnDeath` pauses game time
  (`isGameTimePausedByGame`, which is only cleared by a room load), kills every
  non-boss `Monster` that isn't `IDontKillOnBossMonsterDeath`, and starts a
  reward routine unless `skipBossSoulFlow` is set. That routine locks all rifts
  and spawns `Shrine_BossSoul`, then a mid-run save. A revive routine also runs.
  `Monster` doesn't override `OnDeath`, so the base is `Entity.OnDeath`.
  `Mon_Primus_BossPrimusAeron.OnDeath` calls `Primus_Ending.StartPrimusDeath`.
- **Zones.** `ZoneManager.LoadNextZoneByContentSettings` lists zones with
  `DewResources.FindAllByNameSubstring<Zone>("Zone_")` and
  `DewBuildProfile.current.content.FilterZones`. `Zone.zoneTier` orders them.
- `ZoneManager` sets the ambient level to `currentZoneIndex + 1` on zone
  change, and `GameManager` sets it to 1 at the start of a run.

## Needs an in-game check

The first build answered the old open questions from the decompiled code.
Each answer still needs to be seen working:

1. **Camp rift:** one prompt, no world map, solo and in a lobby with a vanilla
   client. No travel vote in multiplayer.
2. **Wave clearing:** the next wave starts only once every monster is dead.
   Watch for a monster stuck out of reach, which would hold the wave open
   forever (`laststand_next` forces the next wave). If it happens, add a stall
   rule.
3. **Solo button:** appears under Limbo, reads "Last Stand", starts a solo run,
   and gamepad focus reaches it.
4. **Lobby option:** "Last Stand" in the dropdown shows its description, and the
   run starts in Last Stand (`laststand_status` reports `mode=True`). It's
   preselected every time Create Lobby opens, with the name `Last Stand (Wave
   Mode) Z<starting world>`. A renamed lobby keeps the host's name, switching
   to Shape of Dreams brings back the vanilla name, and switching back sets
   the Last Stand name again. After a mod unload the window opens on Shape
   of Dreams as vanilla.
5. **Restart** after game over stays in Last Stand.
6. **Arena:** loads lit and cleared with no boss, players land in a ring at the
   center, and the well is on the edge ring.
7. **Spawn points:** monsters appear across the whole room, never on top of a
   hero, never inside walls or off the floor, and the ones far away walk in.
   With 100 alive, check the host's frame rate and the network load on clients.
8. **One-monster rule:** each regular spawn makes exactly one monster, at the
   current ambient level.
9. **Minibosses** spawn and drop the chaos reward. **Pain** bosses show the
   decorator, the aura hurts and stacks, stops at the last kill, and the
   Corrupted Chaos shrines appear. Check whether the health bonus fills current
   health.
10. **Loot** (with `Monster item drops` on) drops and stays until picked up. Check what `tempOwner` means for
   other players picking it up, and that chances visibly improve by late
   waves.
11. **Dream Well** upgrades for the host and for a vanilla client, many times
   in a row.
12. **Per-wave scaling:** the stat bonus shows on monsters, and whether it fills
    current health or only raises max health.
13. **Other worlds:** the `catalog:` log line's type count, and that monsters
    from other zones spawn and behave (abilities prewarmed, no missing assets)
    in a different zone's arena.
14. **Hunters:** the buff applies to our one-monster spawns, they shadow-walk,
    and they count toward clearing the wave.
15. **Hunter zone:** `AddModifier<RoomMod_Hunted>` goes live in a cleared
    (revisit) room, artillery falls, vanilla clients see it, removal stops it,
    and whether `RoomMod_Hunted.isMain` strips another main modifier (such as
    Corrupted Chaos's) from the arena node. Also check the hunt-level HUD.
    The log has no `RoomMod_Hunted+<SmallArtilleryRoutine>` or
    `<BigArtilleryRoutine>` `NullReferenceException`, small and big
    artillery land around heroes all wave, and none falls in the break.
16. **Mirage:** skins apply and show on vanilla clients, in Delusion-first
    order.
17. **Monster item cap** (with `Monster item drops` on): solo gets at most one item a wave. Two players get
    one memory and one essence. Minibosses still drop on their own chance.
18. **Chaos rarity:** miniboss chaos rewards now vary in rarity, and the reward
    choices and color match it on a vanilla client.
19. **Goal and panel:** the Shape of Dreams goal disappears for the host and a
    vanilla client, and nothing else breaks without it (rift, game end, save).
    The "Omen of Pain" and "Hunted" panels are replaced by "Mysterious
    Space" when they end (see check 77).
20. **Pain environment:** the Aura of Pain look (fx loop, outer decorations,
    hidden env particles) shows for the host and a vanilla client while a
    Pain group lives, and goes away when it falls. Check that no extra
    minibosses or Corrupted Chaos shrines come from the stock modifier, and
    that the hunter zone comes back after a Pain group falls mid-zone.
21. **Top-right label:** the host and a vanilla client see the same stock
    label (`1. <first world>`, or the room's own name), never the wave.
22. **Heroic bosses:** at wave 49 the boss enters 3 s after the last monster
    spawns, plays its spawn animation, counts toward the wave, and drops 3
    Corrupted Chaos rewards (the Pain kind). There's no time pause, no rift and no boss soul.
    Its health and damage are vanilla boss scaling plus the wave bonus, it uses its skills about three times as often, and every default boss uses its patterns and helpers. Pillars and
    hallucinations vanish when it dies. Nyx with The Consort of Night (Erebos)
    causes no boss-kill flow. Check what clients see.
23. **Plain arena:** no extra shrines or props in the arena. The log shows
    modifiers stripped at world gen and any blocked later. Co-op knock-out
    souls still appear.
24. **Wave tracker:** in the arena the quest tracker shows "Threat Level
    Rising / Powerful Monsters" with a kill icon and `Wave 12: 34 monsters
    left` or `Wave 13 in 5s`, on the host and a vanilla client, including
    one who joins mid-run (log: `wave tracker: title sent to N ready
    connection(s)`). It never reads "Limbo" or `ui.!`. Check that the
    once-a-second update flash isn't distracting, that it's gone in camp and
    after game over, and that a mod reload leaves one tracker
    (`took over the quest left from before`). Old config files are moved to
    `.old` (schema 23, when `Show wave in HUD` was removed).
25. **Settings window:** the lobby's settings panel has a **Last Stand** button
    that opens the settings window: a sidebar of seven pages on the left,
    the page's grouped cards on the right, the same in the mod manager's
    gear screen. Clicking a page switches it and scrolls to the top, long
    descriptions wrap, number fields and dropdowns line up on the right,
    and gamepad focus reaches the sidebar. Reset and Undo in the mod
    manager refresh every page. `Arena room` is a dropdown and
    `Heroic boss types` is a checkbox list, in both that window and the mod
    manager's screen. Apply saves the file, Cancel and Escape drop the edits,
    and the dropdown list isn't clipped by the scroll area. Same dropdowns for
    Dream Arena's `Arena room` and `Dummy monster`.
26. **No random boss music** (removed, schema 37): the arena plays the
    zone's normal music, and nothing in the log mentions boss music.
27. **Revive after the wave:** no soul shrine or "lost soul" quest appears
    when a hero is knocked out. They're revived when the wave is cleared
    (log: `revive: N knocked-out hero(es) revived after wave W was cleared`),
    only if someone is still alive.
28. **World map:** M shows only the camp and the arena node, on clients too.
29. **Heroic boss after a reload:** a heroic boss alive during a mod reload
    still drops its 3 Corrupted Chaos rewards when killed, and the game's
    boss-kill flow (`Rev_KillHeroicBosses`, timer pause) doesn't run.
30. **Wave count survives:** reload the mod mid-wave (touch a file). The log
    shows `progress restored: wave N ...` and the wave count carries on.
    `laststand_wave 10` jumps to wave 10.
31. **Minibosses in any arena room:** wave 10 spawns its miniboss with no
    `SelectMiniBoss` exception, also in rooms whose pool has no miniboss type.
32. **Tracker after a reload:** within a second of a mod reload, the quest
    tracker shows the wave again, with one tracker, not two.
33. **Minibosses:** a 10th-wave miniboss has the game's miniboss scaling
    plus the wave bonus, no extra multiplier.
34. **Heroic boss rewards:** a heroic boss leaves 3 Corrupted Chaos shrines
    (the Pain kind), and every player can pick from each one.
35. **Rarity floor** (with `Monster item drops` on): no rarity falls under 25% of drops. By wave 100 the
    `loot:` log lines show common, rare, epic and legendary about equally,
    and Pain minibosses and heroic bosses get there by wave 47.
36. **Heroic boss haste grows:** the `wave N:` log line shows `heroicHaste=200`
    at wave 49, 250 at 99, 300 at 124 and 350 at 149, and later bosses use their skills
    visibly more often.
37. **Hunters from wave 101:** the catalog log line says `hunters start at
    wave 101, every monster is a hunter from wave 130`. Wave 101 logs about
    1/30 of its monsters as `hunter=` (10 of 309 solo) and a hunter zone at
    hunt level 1; the share grows every wave, wave 115 is about half, and from
    130 the line shows `regular=0`. Every wave from 101 except 106, 116 …
    shows a hunt level (`huntLevel=1` for 101–110, 2 for 111–120… 6 from 151)
    and has artillery,
    and the breaks between waves have none. Hunters arrive mixed through the wave, not
    all at the start. Check the host's frame rate with 300+ hunters.
38. **Altar of Cleansing:** on by default, on the edge ring. A vanilla client
    can cleanse several items in a row, pays the shown gold price and gets the
    dream dust back. After a mod reload there's still just one altar.
39. **Breaks:** "wave N cleared" says 20 s after a normal wave and 40 s after
    waves 10, 20, 49, 50 and so on.
40. **Leftover items:** in co-op, let an item reserved for someone else lie
    until the wave is cleared. It becomes visible and can be picked up by
    anyone, chat says so, and it stays visible after the revive.
41. **No heroic boss types checked:** uncheck every box. The log says
    `picking from the whole list`, and wave 49 still has a heroic boss.
42. **Shrine of Ascension:** It stands by the edge of the arena
    (a new spot each run), reachable on foot, not inside a wall or off the
    floor. Each player gets four uses ("uses left" counts down on the host
    and on a vanilla client, never `-1`), given back every 5th wave (see
    check 53).
    After a mod reload there's still just one.
43. **Cheat commands off by default:** with `Allow cheat commands` off,
    `laststand_gold 5000` and `laststand_testboss` log that cheats are off and
    do nothing. Turn it on and both work: gold reaches every player, and a
    weak heroic boss spawns.
44. **Ascension price is vanilla and exact:** with `Free gold shrines` off, a
    vanilla client's tooltip shows exactly the gold the host takes, 30 to 150
    by rarity (times the zone curves), +10% per use, back to the base each
    time the uses come back.
45. **Maw of Doom:** it stands by the edge, opposite the Shrine of Ascension
    (with both on). On the host and a vanilla client every use shows and
    costs 20 gold (times the zone curves, never more; 0 with free gold
    shrines) and an 8% chance for a memory (5% for an essence), from the
    first use on, after a
    wave and for a player who joins mid-run. A destroyed memory or essence is
    really gone, and a vanilla client's tooltip never shows 0 gold or 0%.
46. **Edge ring:** with the defaults all seven shrines (Dream Well, Altar,
    Ascension, Maw, Destiny, Hatred, Luck) stand at even angles, each near
    the room's edge in its own direction (the log's `edge ring:` line gives
    the median reach), none in a wall, off the floor, on top of another or
    down an entry corridor; with two turned on they face each other. In the
    Snow Mountain boss room they used to sit about 14.5 m out; now they
    should be near the walls. Check a few rooms, including ones with
    pillars or narrow ends, and that the well is still unlimited for a
    vanilla client. With `Dream Well` off there's no well.
47. **Kill gold cap** (off by default since schema 33; set `Max kill gold
    per player` to 1 to check): solo, each normal kill gives 1 gold (more
    only with gold-gain bonuses). With 3 players each gets about 1 per kill.
    Miniboss, Pain and heroic boss bonus gold still drops in full. With the
    default 0, kills drop vanilla kill gold. Old config files are moved to
    `.old` (schema 33).
    Regular bonus gold (schema 34): solo, a wave 1 kill drops vanilla kill
    gold (about 3) plus a 3 gold bonus orb, a wave 100 kill about 6 bonus,
    and a hunter's kill only the bonus.
48. **Steeper scaling from wave 150:** the `wave N:` log line shows
    `health=+1480% damage=+1480%` at wave 149, `+1530%` at 150 and `+1580%`
    at 151, and monsters visibly get tougher faster.
49. **Game over late in a run:** lose during a wave from 101 on (a hunter
    zone wave). The log shows no `session tick failed` and no
    `InitSyncObject` exception, and the restart into the next run is clean.
50. **Heroic boss waves:** the `wave N:` log line shows `heroic=1` at wave
    49, `heroic=2` at 99 and 124, `heroic=3` at 149 and 174 and `heroic=4`
    at 199, and that many bosses enter. Waves 74, 100, 125 and 150 have
    none. `laststand_wave 124` is the quickest check of the switch.
51. **Mirage fills up from wave 130:** the `wave N:` log line shows
    `mirageChance=20%` at 129, `24%` at 130, `60%` at 139 and `100%` from 149.
    From 149 every hunter wears a skin (Lesser monsters excepted), the host's
    frame rate holds, and vanilla clients see the skins.
52. **Shrine of Destiny:** on by default, on the edge ring.
    On the host and a vanilla client: the alternate interact sacrifices an
    essence, shows the right quality cost (and the destroy warning under 10),
    and the shrine shows a new offer; sacrificing again rerolls it at a
    higher cost. Claiming drops the essence for that player only. Each player
    can claim twice per wave; after the second claim the shrine can't be used
    (no sacrifice, no claim) until the wave is cleared (log: `destiny: claims
    reset after wave N`), then two more. After a mod reload there's still
    just one shrine.
53. **Ascension opens after wave 10, uses back every 10th wave** (set
    `Ascension uses come back every N waves` to 10; the default is 1): the
    shrine is locked through wave 10 (the lock effect shows, and using it
    does nothing on the host or a vanilla client; log `ascension: locked
    until wave 10 is cleared`). It unlocks when wave 10 is cleared
    (`ascension: unlocked after wave 10 was cleared`) with four uses per
    player. Use some on wave 12; when wave 20 is cleared everyone has four
    again on the host and a vanilla client (log: `ascension: uses reset
    after wave 20 was cleared`), and nothing resets after waves 11–19.
    `laststand_wave 3` after that locks it again. Old config files are moved
    to `.old` (schema 14).
54. **Free gold shrines** (off by default since schema 29; turn it on to
    check): the log shows `gold prices: multiplier set to 0`.
    On the host and a vanilla client the Maw of Doom shows and takes 0 gold
    at 8%, and the Ascension 0 gold, with any amount of gold, even none. The
    Altar of Cleansing keeps its price. After a mod unload the log shows
    `multiplier restored to 1`.
55. **Hatred Remnant:** on by default, on the edge ring. A vanilla client
    can choose a strength once, gets the curse and a Hatred Shard, and can
    choose again after the wave is cleared (log: `hatred: choices reset after
    wave N`). The shard's reward never includes platinum, its gold is
    exactly 500 at every strength (with free gold shrines on, not 1), and its
    dream dust is about 50 × wave × the strength multiplier (0.8 / 1.5 / 2.4 /
    3.3), with the log line `hatred: shard opened at wave N; dream dust base
    X, gold 500`. Old config files are moved to `.old` (schema 18).
56. **Curse goals:** every curse taken in the arena logs `curse: ... now
    ends after N kill(s)`, with N about one player's share of the wave (×1,
    ×1.25, ×1.75 by strength). The curse quest on the host and on a vanilla
    client shows a kill count (never rooms) from the start, counts down with
    kills, and the curse ends at 0.
57. **Environments:** waves 6, 16 … 96, 106, 116 … each log `environment:
    wave N brings RoomMod_…` and chat names it; wave 104 has none. The effect shows for the
    host and a vanilla client, and stops when the wave is cleared (log
    `environment: … removed`, chat "… fades"). The next wave has no leftover
    status effects, hazards or slowed music (Gravity Training). Try
    `laststand_wave 4` several times to see different ones.
58. **Hazards in a cleared arena:** Crimson Flame Demon, Gaze of the Abyss,
    Hazardous Weather, Giant Ink Drop and Warped Space actually fire during
    the wave (the room is held in combat), and stop after it. No `blocked the
    room's clear routine` line removes monsters, and the run's mid-run save
    still happens after the wave.
59. **Gold and dust environments:** Dream of Wealth and Pure Dream drop gold
    or dust per kill and place breakable stones. Kill gold from the
    environment skips the kill gold cap (it isn't `GetKillGoldAmount`).
60. **Monster environments:** Leaf Hound Den and Meteoric Life resize Last
    Stand's spawns (Meteoric Life corpses explode). Monster types don't
    change, and wave 10's miniboss still spawns after one. Unstable Rat Swarm
    rats don't hold the wave open.

61. **Monster count keeps growing** (schema 42, the stop at wave 110 is
    gone): solo, the `wave N:` log line shows 222 regular monsters (hunters
    included) at wave 109, 224 at 111 and 264 at 130 (a surge: 528), and
    wave 110 totals 454 with its 6 minibosses.

62. **Dark urge:** in co-op from wave 176, about 1 in 5 waves without a
    heroic boss log `dark urge: wave N curses every player with Potent Dark
    Urge`. Every player, on the host and on a vanilla client, gets the
    "cursed" message and a Dark Urge entry in the quest tracker on the right
    with no kill count. Heroes hurt each other at about 25% (less under half
    health), not 6%. A knock-out doesn't lift it. When the wave is cleared
    the curse is lifted for everyone (`curses lifted at the end of wave N`),
    and nobody can hurt a teammate in the break. Heroic boss waves (199,
    224…) and solo runs never get it. Old config files are moved to `.old`
    (schema 21, when the default chance went from 50% to 20%).

63. **Starting gold:** a new run, solo and in a lobby, logs `starting gold:
    granted 100 to N player(s)` once in camp, and every player, a vanilla
    client included, has 100 more gold than the game's start. A mod reload
    doesn't grant it again, a restart after game over does, and a continued
    save doesn't. Old config files are moved to `.old` (schema 16).
    Starting dream dust: also `starting dream dust: granted 20 to N
    player(s)` at starting world 1, and 80 at starting world 3, on the host
    and a vanilla client. Old config files are moved to `.old` (schema 28).

64. **Environments replace the hunter zone:** `laststand_wave 116` logs
    `huntLevel=0` and an environment, with no artillery and no "hunted"
    chat line, while hunters still spawn (`hunter=` in the `wave N:` line).
    Wave 117 is a hunter zone again at hunt level 2. Late environments with
    300+ hunters (Meteoric Life corpses, Dream of Wealth stones) keep the
    host's frame rate. Old config files are moved to `.old` (schema 17).

65. **Reminiscence of Luck:** on by default, on the edge ring. On the host
    and a vanilla client: each player sees their own 3 choices, picks one for
    free and gets it (reserved for them), and can't pick again that wave.
    Praying adds a choice of the same rarity, shows and costs the same gold on
    both (0 or 1 with free gold shrines), up to 3 times. When the wave is
    cleared (log: `luck: picks, prays and choices renewed after wave N`)
    everyone has new choices, a new pick and 3 prays, including an open
    choice window updating. A mid-run joiner gets choices. After a mod reload
    there's still just one shrine. Old config files are moved to `.old`
    (schema 19).

66. **No monster items by default:** with `Monster item drops` off, no kill
    of any tier drops a memory or essence and the log has no `loot:` item
    lines, while dream dust, bonus gold and miniboss, Pain and heroic boss
    chaos rewards still drop. Turning it on in the lobby window brings item
    drops back at the next kill. Old config files are moved to `.old`
    (schema 20).

67. **Starting world:** at the default 1 nothing is logged and the
    top-right label reads `1. <first world>`. At 5 the camp log shows
    `starting world: zone index 0 -> 4`, a vanilla client's label reads
    `5. <first world>` (`7. The Lone King's Zenith` in the Primus room),
    wave 1 monsters have about 9.5× the health of world 1, and memories,
    essences, XP and Dream Well prices move like vanilla world 5. A run
    continued from a save keeps its own index. Old config files are moved
    to `.old` (schema 25, when the default went back to 1 and the boss
    multipliers were removed).

68. **Cheat commands:** with `Allow cheat commands` off, `laststand_dust`,
    `laststand_level`, `laststand_essence`, `laststand_memory` and
    `laststand_skip` log that cheats are off. On: `laststand_dust 5000`
    reaches a vanilla client too; `laststand_level 20` levels every hero
    (stats update, level-up effect); `laststand_essence legendary` and
    `laststand_essence 300` style name searches drop a reserved item near
    the host, an ambiguous name lists matches; `laststand_skip` mid-wave
    removes the monsters, logs `wave N ended early`, chat says it was
    cleared and the next wave starts at once with its own `wave N:` line;
    `laststand_skip 10` lands on the 10th next wave with the right
    scaling, and the Pain aura is gone after a skipped Pain wave.
    `laststand_secondwind 3` shows a Second Wind icon with 3 on every
    hero (a vanilla client too); a fatal hit leaves the hero at 25% health,
    invulnerable for 3.5 s, and the count drops to 2. `laststand_secondwind
    3 8` makes it 8 s, `laststand_secondwind 0` removes it.

69. **Heroic boss multipliers:** at wave 49 (Nightmare, world 4, solo) the
    log line `heroic boss: wave 49 <type> maxHealth=` shows about 1.6M
    (Belphomet) to 2.7M (Azurak), Infernus about 1.96M; with 2 players
    ×1.85. The health bar starts full. The boss's `biggest hit on a hero`
    line at death is in the tens of thousands (Skoll's AuraBlade about 132k).
    Old config files are moved to `.old` (schema 27).
    Pain minibosses and regular minibosses are unchanged.

70. **Gold shrines cost gold by default:** a new config logs no `gold
    prices: multiplier set to 0`. On the host and a vanilla client the Maw
    of Doom shows and takes 20 gold (times the zone curves) at 8% for a
    memory and 5% for an essence on every use, the Ascension its vanilla
    price, and a Luck pray its vanilla price, rising with each pray. Old
    config files are moved to `.old` (schema 29).

71. **Gold prices grow 5% a wave:** `laststand_wave 10` logs `gold
    prices: multiplier set to 1.63 at wave 10`, `laststand_wave 50` 11.47,
    `laststand_wave 100` 131.5. On the host and a vanilla client the Maw
    shows 33 gold at wave 10 (20 × 1.63, times the zone curve) and the
    Ascension and Luck prays rise the same way; the Altar of Cleansing
    doesn't. Game over or a mod unload logs `multiplier restored to 1`.
    Old config files are moved to `.old` (schema 32). From schema 35
    growth stops at wave 100: `laststand_wave 150` from an early wave logs
    131.5, the same
    as wave 100.

72. **Fast clear carries over** (set `Fast clear: from wave` to 1 first;
    the default is 30): one-shot 5 of wave 1's 6 monsters (see check 82
    for the trigger).
    Chat says, in red (`#ff4d4d`) instead of the usual orange, `5 of wave
    1's 6 monsters died within 0.5s of their first hit. From wave 2 on,
    monsters hit back with x2 health and damage.` and the log `fast clear:
    wave 1 had 5 of 6 monster(s) die within 0.5 s of their first hit`. The `wave 2:` and `wave 3:` lines show `fastClear=x2`
    and both waves' monsters take about twice the hits; a second fast clear
    makes it `x4`. A heroic boss (`laststand_wave 49`) after a fast clear
    has normal health. A slow wave and `laststand_skip` never trigger it.
    A mod reload logs `... 1 fast clear(s) so far` and keeps `x2`. Old
    config files are moved to `.old` (schema 32).

73. **Defaults (schema 32):** the Ascension unlocks after wave 1 and gives
    four uses back after every wave; monsters spawn 6 per batch.
74. **Curse to start** (schema 36, off by default): turn it on with
    `Hatred Remnant` off. The shrine still stands, and its window offers
    no None (log `hatred: choice None replaced`), on the host and a vanilla
    client. Wave 1 never starts until someone picks; the tracker reads
    `Wave 1: choose a curse`. In co-op one pick curses every player, each
    with a different curse and a kill goal; wave 1 starts 3 s later. After
    the wave every player can pick again and the next wave waits. A reload
    in a break holds again with choices given back. A pick mid-wave curses
    only the chooser.
75. **Fast clear from wave** (schema 39, default 30): with the defaults,
    clear waves 1 to 29 fast: no `fast clear:` log line and `fastClear=x1`
    stays. `laststand_wave 30`, clear it fast: wave 31 shows
    `fastClear=x2`.
76. **Fast clear on heroic bosses** (schema 40): with `Fast clear: from
    wave` at 1, fast-clear waves 1 to 4 (x16), then `laststand_wave 49`.
    The `wave 49:` line shows `fastClear=x16 heroicFastClear=x4`, and the
    `heroic boss:` maxHealth line is about 4× a run without fast clears.
    With the toggle off, `heroicFastClear=x1`.
77. **Default modifier panel:** on arrival the panel reads "Mysterious
    Space" on the host and a vanilla client (log `panel: ... shown`), with
    no center animation, no rift and no new props. At wave 6 the
    environment replaces it; when wave 6 is cleared "Mysterious Space" is
    back on both. Same after a hunter zone (wave 101+) and a Pain group.
    The game log has no `HandleRuntimeRemoval` unknown-id lines.
78. **Chat colors:** "Wave N cleared. The next wave arrives in …" (and the
    curse-to-start version) is green (`#5ad45a`), the fast-clear line red,
    every other Last Stand notice orange, on the host and a vanilla client.
79. **Lobby button host-only:** host a lobby with the mod; the settings panel
    has the **Last Stand** button. A friend with the mod loaded joins: their
    settings panel has no Last Stand button, and nothing else of the mod
    shows or runs on their side.
80. **Config command host-only:** the friend from check 79 runs
    `laststand_config` in the lobby and in the run: no window opens, and
    their log shows `only the host's settings apply`. On the title screen
    and as host it opens the window as before.
81. **Mid-join after a heroic boss:** `laststand_wave 49`, kill the boss,
    then have a friend join mid-run. The log shows `mid-join: cleared the
    game's AfterBossFight wait` (and `FightingBoss` while it lived), and
    the friend joins within a few seconds instead of waiting on "The
    expedition is preparing to move after fighting a Heroic Boss". A
    friend who joins while the boss is alive gets in too.
82. **Fast clear by time to kill** (schema 41): with `Fast clear: from
    wave` at 1, one-shot most of wave 1: the log shows `fast clear: wave 1
    had K of 6 monster(s) die within 0.5 s of their first hit` as soon as
    the 5th instant kill lands, even on a big wave that takes far longer
    than 10 s to spawn. Hit monsters a few times each on a later wave: no
    trigger, and at the wave's end `fast clear: wave N had K of D death(s)
    within 0.5 s of the first hit; X needed`. Old config files are moved
    to `.old` (schema 41, `Fast clear: within (s)` replaced by `Fast clear:
    instant within (s)`).
83. **Workshop change note:** with `about/changenote.txt` written,
    `./pack.sh` prints it. Upload the pack: the log shows `workshop:
    attached about/changenote.txt (N characters)`, and the item's Change
    Notes page shows the note with its formatting. Without the file the
    log says the update goes up without a change note, and uploading
    another mod attaches nothing.
84. **Merged waves** (schema 42): arriving logs `merge: from wave 48,
    waves hold at 100 monsters and hunters`. Solo, `laststand_wave 47`
    logs `merged=98/x1`, wave 48 `merged=100/x1`, and `laststand_wave 50`
    `merged=208/x2.08` with 2 more minibosses than usual and `pain=2`; the
    Pain aura shows and the break is 40 s. `laststand_wave 131` logs
    `merged=266/x2.66` with `hunter=100 regular=0`, chat says `They fight
    with the strength of 266: x2.66 health and damage for every monster.`,
    and only 100 monsters spawn, each taking about 2.66× the hits, with
    2.66× bigger bonus gold and dream dust orbs. On a merged wave with
    minibosses (`laststand_wave 140`) the log shows `boss health: wave 140
    … (5x the toughest monster's …)` for any miniboss below 5×, within a
    second of the first monsters, and each miniboss's bar shows the new
    maximum. At `laststand_wave 149` a heroic boss below 20× logs the
    same with `20x`. Set `Monsters in wave 1` to 20:
    the merge line says wave 41. `Merged wave size` 0: nothing merges.
85. **Alive cap 35** (schema 42): on a big wave (`laststand_wave 101`)
    never more than 35 monsters are alive at once (`cap=35` in the `wave
    N:` line), 70 on a surge wave (`laststand_wave 100`, `cap=70`), new
    ones arrive as others die, and the host's frame rate is higher than
    with the old cap of 200.
86. **Heroic boss stardust:** kill a heroic boss (`laststand_wave 49`) on
    Nightmare: stardust orbs pop out at the boss, the log says `heroic
    boss: died, dropped 3 Corrupted Chaos reward(s) and 23 stardust.` (16
    + 7; 18 on Normal), chat says so, and the host and a vanilla client
    each gain that much stardust (title screen total). With `Heroic boss
    stardust` off: `0 stardust` and no orbs.

## Conventions

### Code rules

These are hard rules for all code in this repo.

- **No comments.** No `//`, `/* */` or XML doc comments in the codebase. Code
  explains itself through names, small types and small methods. The *why*
  (which game system a choice relies on, why it reaches clients) goes in the
  topic's file under `docs/` or this file instead. Dream Arena's code is commented. Strip its
  comments when adapting it.
- **No magic strings or hard-coded values.** Every number, duration, limit,
  type or prefab name, room name, chat command and chat text lives in exactly
  one named place:
  - Tunable values (wave length, caps, drop chances, scaling curves) go in
    config (`LastStandConfig`), with defaults set there. Fixed internal
    constants go in `Tuning`.
  - Fixed game identifiers that can't be a `typeof` or `nameof` (private
    method names, room names, modifier names) go in `GameNames`. Prefer
    `typeof`, `nameof` and generic APIs such as `Dew.CreateActor<T>` when
    the member is public.
  - Text goes in `src/Text`: `Messages` for chat, `TrackerText` for the quest
    tracker's progress, `ConfigText` for the config screen, `LogText` for the log and
    `CommandText` for console commands.
  - Plain `0`, `1`, `null` and `string.Empty` are fine where their meaning is
    obvious.
- **Separation of concerns.** Each type has one job. Keep these apart:
  - **Harmony patches** are thin. They hook the game and hand off to a
    service. They hold no rules or state.
  - **Game rules** (wave schedule, scaling, loot rolls, pain stacking) are
    plain logic that doesn't touch Unity or networking, so they can be read and
    tested alone.
  - **Game adapters** wrap the game's API (spawning, pickups, shrines, quests,
    chat). Rules call adapters and never the game directly.
  - **Config, lifecycle and UI** each have their own types. The `ModBehaviour`
    only wires things together and tears them down.
- **DDIA design.** Follow the principles of *Designing Data-Intensive
  Applications*:
  - **Reliability.** The host is the server, so a fault there breaks the run
    for everyone. Fail safe: if a room isn't ready or a lookup fails, retry on a
    throttled timer or skip, and never throw from per-frame code or a patch.
    Every effect is undone on unload.
  - **Scalability.** Bound per-frame work. Cap enemies and pickups alive, use
    `.softInstance`, cache lookups, and scale load by player count.
  - **Maintainability**, the main focus:
    - *Operability:* clear, throttled log lines with a fixed prefix, so a
      problem can be found in `Player.log`.
    - *Simplicity:* the smallest design that works. No speculative
      abstraction.
    - *Evolvability:* new waves, enemy tiers, drops or settings are added
      through config and data, not by editing control flow.
  - **One source of truth** for state. The server's synced state is the record.
    Don't keep a second copy that can drift from it.

### Working rules

- **American English** in code and docs.
- **Don't commit** unless the user asks.
- **Check changed C# files** with a clean build before reporting done, and
  with LSP diagnostics if a C# language server is ever set up. Also check that
  the diff has no comments and no new literals outside `src/Config`,
  `src/Text`, `src/Identity` and `src/Game/GameNames.cs`. For example:
  `grep -rn "//\|/\*" src` should print nothing.
- **Bump `ConfigSchema.Current`** whenever a config default changes or a field
  is renamed or removed. The game loads the saved JSON over the defaults, so
  without a bump, players keep the old values. This happened once already:
  the first build's file kept minibosses at wave 4, Pain every 3 waves and the
  24-monster cap through several later changes.
- **Where things go:** new tunables go in `LastStandConfig` and are validated in
  `SessionSettings.From`. Internal constants go in `Tuning`. Game identifiers
  go in `GameNames`. Every string goes in `src/Text`. Patches only check
  `LastStandMode` and call into the session.
- **Keep `docs/` in step** with the code. With no comments in the code, the
  docs are where the design and its reasons live. They describe how things
  actually work, not just what they do. Update the topic's file, and add a
  new file plus a line in `docs/README.md` for a new topic. The root
  `README.md` is the player-facing overview; change it only when a feature
  or default players see changes.
