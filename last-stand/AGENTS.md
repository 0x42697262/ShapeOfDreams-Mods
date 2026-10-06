# AGENTS.md: Last Stand

Working notes for anyone, human or agent, building this mod. Read
[`README.md`](README.md) first for the design.

## Status

First build. The whole loop is in code and builds clean, but **nothing has
been tested in-game yet**. Work through "Needs an in-game check" below before
building more on top. The git repo has no commits yet.

`about/icon.png` and `about/preview.png` are Dream Arena's images, copied as
placeholders. Replace them before uploading to the Workshop.

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
  - `DewQuest` titles and descriptions are not synced. `currentProgress` and
    `step` are.
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
  others' experience without consent, and must not grant server-generated
  exclusive items (anything bought with stardust). Every server-side mod that
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
  through `GameManager.CallOnReady`. It is client-local, so it can't be changed
  for vanilla clients.
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
   run starts in Last Stand (`laststand_status` reports `mode=True`).
5. **Restart** after game over stays in Last Stand.
6. **Arena:** loads lit and cleared with no boss, players land in a ring at the
   center, and the well is there.
7. **Spawn points:** monsters appear across the whole room, never on top of a
   hero, never inside walls or off the floor, and the ones far away walk in.
   With 100 alive, check the host's frame rate and the network load on clients.
8. **One-monster rule:** each regular spawn makes exactly one monster, at the
   current ambient level.
9. **Minibosses** spawn and drop the chaos reward. **Pain** bosses show the
   decorator, the aura hurts and stacks, stops at the last kill, and the
   Corrupted Chaos shrines appear. Check whether the health bonus fills current
   health.
10. **Loot** drops and stays until picked up. Check what `tempOwner` means for
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
16. **Mirage:** skins apply and show on vanilla clients, in Delusion-first
    order.
17. **Monster item cap:** solo gets at most one item a wave. Two players get
    one memory and one essence. Minibosses still drop on their own chance.
18. **Chaos rarity:** miniboss chaos rewards now vary in rarity, and the reward
    choices and color match it on a vanilla client.
19. **Goal and panel:** the Shape of Dreams goal disappears for the host and a
    vanilla client, and nothing else breaks without it (rift, game end, save).
    The "Omen of Pain" and "Hunted" panels hide on the host when they end.
20. **Pain environment:** the Aura of Pain look (fx loop, outer decorations,
    hidden env particles) shows for the host and a vanilla client while a
    Pain group lives, and goes away when it falls. Check that no extra
    minibosses or Corrupted Chaos shrines come from the stock modifier, and
    that the hunter zone comes back after a Pain group falls mid-zone.
21. **HUD label:** the host sees the wave and monsters-left label, it survives a
    room load, and the stock label returns when leaving Last Stand.
22. **Heroic bosses:** at wave 49 the boss enters 3 s after the last monster
    spawns, plays its spawn animation, counts toward the wave, and drops 3
    Corrupted Chaos rewards (the Pain kind). There's no time pause, no rift and no boss soul.
    Its health bar shows about 10× the health, its hits land about 3× as
    hard, it uses its skills about three times as often, and every default boss uses its patterns and helpers. Pillars and
    hallucinations vanish when it dies. Nyx with The Consort of Night (Erebos)
    causes no boss-kill flow. Check what clients see.
23. **Plain arena:** no extra shrines or props in the arena. The log shows
    modifiers stripped at world gen and any blocked later. Co-op knock-out
    souls still appear.
24. **Wave tracker:** which existing quest type to use, given that only progress
    and step text sync. Chat notices stand in until then.
25. **Settings window:** the lobby's settings panel has a **Last Stand** button
    that opens a three-column window. `Arena room` is a dropdown and
    `Heroic boss types` is a checkbox list, in both that window and the mod
    manager's screen. Apply saves the file, Cancel and Escape drop the edits,
    and the dropdown list isn't clipped by the scroll area. Same dropdowns for
    Dream Arena's `Arena room` and `Dummy monster`.
26. **Random boss music:** the host hears a different boss track each time
    the arena loads. The log shows `boss music: playing mus...`. Camp music is
    unchanged.
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
32. **HUD label after a reload:** within a second of a mod reload, the
    top-right label shows the wave again instead of the room name.
33. **Stronger minibosses:** a 10th-wave miniboss has about 2.5× the health
    and hits about 2.5× as hard as before; Pain minibosses are unchanged.
34. **Heroic boss rewards:** a heroic boss leaves 3 Corrupted Chaos shrines
    (the Pain kind), and every player can pick from each one.
35. **Rarity floor:** no rarity falls under 25% of drops. By wave 100 the
    `loot:` log lines show common, rare, epic and legendary about equally,
    and Pain minibosses and heroic bosses get there by wave 47.
36. **Heroic boss haste grows:** the `wave N:` log line shows `heroicHaste=200`
    at wave 49, 250 at 99 and 300 at 149, and later bosses use their skills
    visibly more often.
37. **Hunters from wave 101:** the catalog log line says `hunters start at
    wave 101`, and wave 101 has 1 hunter and a hunter zone at hunt level 1.

## Conventions

### Code rules

These are hard rules for all code in this repo.

- **No comments.** No `//`, `/* */` or XML doc comments in the codebase. Code
  explains itself through names, small types and small methods. The *why*
  (which game system a choice relies on, why it reaches clients) goes in the
  README or this file instead. Dream Arena's code is commented. Strip its
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
  - Text goes in `src/Text`: `Messages` for chat, `HudText` for the host's
    HUD label, `ConfigText` for the config screen, `LogText` for the log and
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
- **Keep the README in step** with the code. With no comments in the code, the
  README is where the design and its reasons live. It describes how things
  actually work, not just what they do.
