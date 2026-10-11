# Shared code: discrepancies between Last Stand and Dream Arena

This file lists the code and build plumbing that both mods carry in their own
copies, how each pair differs, and how to settle it before it moves into a
shared `library/`. It was made at commit `4eb24ac` by diffing every same-named
file in `last-stand/src` and `dream-arena/src` after replacing namespaces and
mod names (LastStand/DreamArena, `LastStandMode`/`ArenaMode`, "Last Stand"/"Dream
Arena"), then comparing same-purpose files with different names by hand.

Two bugs were fixed in both copies after this was written: the lobby button
now shows only in the mod's own mode, and Dream Arena's on/off `Max player
level` became a `Player level` number. The entries below reflect the fixes.

## Decisions that come first

- **Compile the library into each mod's DLL** (`<Compile Include="..\library\src\**\*.cs" />`).
  Don't ship it as a separate DLL. `DewMod` renames every loaded assembly to
  `<name>_<ticks>` before `Assembly.Load(byte[])` (`DewMod.cs`, mod loading), so
  a mod DLL's reference to a library DLL would not resolve. When the library is
  compiled into each mod, each mod keeps its own static state
  (`ConfigWindow._current`, `SettingsPanel._lastPage`,
  `WorkshopChangeNote._pending`, `ChoiceFields` conditions), and that is what we want.
- **No `ModBehaviour` and no `[ConsoleCommand]` in the library.** `DewMod`
  attaches every non-abstract `ModBehaviour` in the assembly and registers its
  commands. A shared console class would register the same command names
  twice when both mods are loaded. It skips abstract types, so an abstract
  base class is possible.
- **Mod context.** Library code needs these from the mod: identity (tag,
  save key, lobby mode value, display name, mod id), the "own mode is hosting"
  check, the config type and its apply callback, and mod-named text.
  Constants are simplest as a small source contract: each mod defines the
  types the library expects, so they stay `const` and work in attributes.
- **Every library Harmony patch must no-op unless its own mod is hosting its
  own mode** (or, for Workshop uploads, the item is its own). Both mods patch
  the same game methods (see the arena and lobby sections). The patches stay
  safe when both mods are loaded only if each one checks its own mode.
- **Strings.** Neutral strings go in `library/src/Text`. Anything that names
  the mod, its commands or its colors stays in the mod's `src/Text` and is
  passed in.

## Summary

| Area | Files | Status | Recommended handling |
|---|---|---|---|
| Config core | `Choice`, `ConfigFields`, `SettingChange`, `ConfigFile`, `PageAttribute`, `PickOneAttribute` | identical | Move. Make the attributes mod-neutral. |
| Config schema | `ConfigSchema` | cosmetic | Share `IsCurrent(json, current)`. `Current` stays per mod. |
| Choice lists | `ChoiceSet`, `ConfigChoices`, `ChoiceList`*, `PickManyAttribute`* | drifted (LS superset) | Share `ArenaRooms` and PickMany. Each mod registers its own sets. |
| Settings UI | `SettingsPanel`, `ChoiceFields`, `SettingChangeText`, `ConfigWindow`, `ConfigWindowLayout`, `SettingsLayout`, `SettingsAnnouncer` | identical / drifted | Take LS. Parametrize the config type, pages, apply callback and mode check. |
| Mod config | `<Mod>Config`, `SessionSettings`, `SettingsPage` | mod-specific | Keep per mod. Adopt DA's fallback for a blank arena room. |
| Lobby and mode | `LobbyConfigButton`, `LobbySettingsPatch`, `InjectedUi`, `CreateLobbyModePatch`, `LobbyModeOption`, `ModeSelection`, `LastStandMode`/`ArenaMode` | identical / drifted | Share. Parametrize preselect, lobby name and mode text. |
| Entry point and console | `LastStandMod`/`DreamArenaMod`, `LastStandConsole`/`DreamArenaConsole` | lightly drifted / mod-specific | Keep per mod. Share a lifecycle helper and console guards. |
| Chat, logging, errors | `Log`, `ErrorThrottle`, `ChatNotices`, chat formatting in `Messages` | identical / drifted | Move. Merge DA's `Tell`. Prefix and colors come from the mod. |
| Workshop change notes | `WorkshopChangeNote`, `WorkshopChangeNotePatches` | identical | Move first. Mod id comes from the context. |
| Arena basics | `ArenaLocator`, `ArenaLayout`, `HeroStaging`, `PlainArena`(+patches), `WorldGenPatch`, `ArenaRoomOverride`/`ArenaRooms`, `CampRift`, `RunGoal`, `LocalizedNames`, revive helpers | identical / drifted | Merge one at a time (details below). |
| Identity and text | `ModIdentity`, `GameNames`, `Tuning`, `LogText`, `Messages`, `ConfigText`, `CommandText` | drifted | Split: neutral parts go to the library, mod parts stay. |
| Build and packaging | `.csproj`, `pack.sh`, `.gitignore`, `about/metadata.json`, local `build.sh`/`watch-log.sh` | cosmetic | Shared props file and script. Fix the container mount first. |
| Conventions | the two `AGENTS.md` files | drifted | Move the conventions to the root `AGENTS.md` and add library rules. |

\* Last Stand only.

## Config and settings UI

| File | What it does | Status | Differences | Handling |
|---|---|---|---|---|
| `Config/Choice.cs` | Value and detail pair for a dropdown entry | identical | none | Move as is. |
| `Config/ConfigFields.cs` | Lists the visible config fields by reflection | identical | none | Move as is. |
| `Config/SettingChange.cs` | Diffs two configs field by field | identical | none | Move as is. |
| `Config/ConfigFile.cs` | Moves an outdated config file to `.old` | identical | Uses `ConfigSchema.Current` and `LogText.ConfigReset` | Move. Take the current version as a parameter. |
| `Config/ConfigSchema.cs` | Reads `schemaVersion` from the saved JSON | cosmetic | `Current` is 45 (LS) vs 1 (DA). Uses `nameof(<Config>.schemaVersion)` | Share `IsCurrent(json, current)` with a fixed field-name constant. `Current` stays in each mod. |
| `Config/PageAttribute.cs`, `PickOneAttribute.cs` | Tag a field with its page or choice set | identical text | Typed to each mod's own `SettingsPage`/`ChoiceSet` enums | Move. Key them by `Enum`/`int` (or make them generic) so each mod keeps its enums. |
| `Config/SettingsPage.cs` | Page enum | mod-specific | LS has 7 pages (General to Rewards), DA has 5 (Match to Rules) | Keep per mod. `SettingsPanel` hard-codes the default page (`General`/`Match`). Use the enum's first value instead. |
| `Config/ChoiceSet.cs`, `ConfigChoices.cs` | Named choice lists, picked by a `For(set)` switch | drifted | The `ArenaRooms` list is identical. LS adds `HeroicBosses`, `Environments`, `CurseStrengths` and `Values()`. DA adds `DummyMonsters` | Move `ArenaRooms` and the world names. Replace the switch with a lookup each mod registers its sets in. |
| `Config/ChoiceList.cs`, `PickManyAttribute.cs` | Comma-separated multi-pick fields | LS only | DA has no checkbox lists | Move them with the UI. DA gets them for free. |
| `UI/ChoiceFields.cs` | Inserts the dropdown builder (LS: also the checklist builder) at the front of `DewGUI.fieldBuilders` | drifted | LS adds `PickManyCondition`, `BuildChecklist`, `Selected` (lines 77–149) | Take LS. `fieldBuilders` is a game-wide list. Each mod must only remove its own conditions, which is safe only while the library is compiled into each mod. |
| `UI/SettingsPanel.cs` | Sidebar of pages with grouped cards | drifted | LS stacks PickMany rows vertically (lines 213–232). The default page literal differs | Take LS. Page titles and subtitles come from the mod (`ConfigText.PageTitle/PageSubtitle` differ). |
| `UI/SettingChangeText.cs` | Turns one change into a chat line | drifted | LS adds `DescribeList` (added and removed items) for PickMany | Take LS. |
| `UI/ConfigWindow.cs`, `ConfigWindowLayout.cs`, `SettingsLayout.cs` | Settings window for the lobby and the console | identical | Cast to the mod's config type and call `<Mod>.Current.ApplyConfig` | Move. Make the config type generic and pass in the apply callback. |
| `UI/SettingsAnnouncer.cs` | Logs setting changes and broadcasts them while hosting | identical | Uses the mode check and `Messages.SettingChanged` | Move. The mode check and chat text come from the mod. |
| `Config/LastStandConfig.cs` / `DreamArenaConfig.cs`, `Config/SessionSettings.cs` | The mod's settings and their validated copy | mod-specific | LS's `SessionSettings` uses one long constructor, DA's uses an object initializer. DA trims a blank `arenaRoom` and falls back to the default. LS passes it raw, and `ArenaRoomOverride` then silently skips the override | Keep per mod. Share small validation helpers such as "blank string → default". |

## Lobby and mode selection

- **`UI/LobbyConfigButton.cs`, `Patches/LobbySettingsPatch.cs`, `UI/InjectedUi.cs`**:
  add and remove the **Last Stand**/**Dream Arena** button in the lobby
  settings panel. They are identical apart from the mode check: the button
  shows only when the mod's own `IsHosting()` holds (it used to check
  `NetworkServer.active` alone, so a host with both mods loaded saw both
  buttons in every lobby). *Handling:* share them, with the mode check
  passed in from the mod context.
  `InjectedUi.RemoveAll` finds buttons by the mod's `OwnedSuffix`, which is
  already per mod.
- **`Patches/CreateLobbyModePatch.cs`**: hooks `UI_Title_CreateLobbyView.OnShow`/`UpdateUIState`.
  Identical. *Handling:* move as is.
- **`UI/LobbyModeOption.cs`**: adds the dropdown option and writes the mode
  into the start settings. Drifted, on purpose: LS preselects its option
  (`Add` sets `gameModeDropdown.value`, LS check 4) and builds the lobby name
  from the starting world (`Messages.LobbyName(int)`). DA doesn't preselect
  (DA check 1) and uses a fixed `LobbyName`. *Handling:* take LS. Pass in
  `preselect`, a lobby-name function, the mode name and the description.
  *Risk:* with both mods loaded, whichever preselects last wins. Only one
  mod should preselect.
- **`Mode/ModeSelection.cs`**: drifted. Both have `Select`/`Deselect` (clear
  `addedGameMods`, set or clear the save key). LS adds `CreateSoloSettings`
  and `StartSolo` for its solo button. *Handling:* share `Select`/`Deselect`.
  The solo pieces can move as an optional part or stay in LS.
- **`Mode/LastStandMode.cs` ↔ `Mode/ArenaMode.cs`**: `IsSelected` (save key
  matches) and `IsHosting`. Identical apart from the class name. *Handling:* one mode check keyed by the mod's save key.
- LS only: `UI/SoloModeButton.cs`, `Patches/SoloModePatch.cs`. Keep them in
  LS (PvP has no solo).

## Entry point, session and console

- **`LastStandMod.cs` ↔ `DreamArenaMod.cs`** (`ModBehaviour` entry point).
  Lightly drifted. The lifecycle is the same: `Awake` sets `Current`, `Start`
  sets `isAlteringGameplay`, registers `ChoiceFields`, patches and hooks
  `GameManager`, `Update` runs a throttled `Session.Tick`, and
  `LoadConfigsToDisk` resets outdated files. `ApplyConfig` and
  `OnConfigChanged` are the same too. Differences: LS builds its session
  with the settings (`new LastStandSession(settings)`), while DA calls
  `UseSettings` afterward. `OnDestroy` calls `Session.Reset()` in LS and
  `Session.Shutdown()` in DA (which restores relations and rules only while
  active). *Handling:* keep a `ModBehaviour` per mod. Share the
  schema-reset, apply and announce sequence as a helper or an abstract
  generic base, with a small session interface (`UseSettings`, `Tick`,
  `Reset`, `Shutdown`).
- **`LastStandConsole.cs` ↔ `DreamArenaConsole.cs`**: mod-specific. Both
  have config, status and revive commands:
  - Config: the host-only check is the same, but LS calls `ConfigWindow.Open()` and DA calls `Toggle()`.
  - Revive: LS gates it behind `CheatsAllowed()` and revives through `HeroSouls.ReviveAll(Tuning.CheatReviveHealth)`. DA gates it behind `Hosting()` and uses `Revives.ReviveAll()`.
  - Command names: `laststand_*` in LS. DA uses `arena` and `arena_*`, which don't match its id `dreamarena`.
  - *Handling:* keep the commands per mod. Share the host and cheat guards and the config-command body, and pick one of `Open`/`Toggle`.
- **`LastStandSession` ↔ `DreamArenaSession`**: mod-specific. The shape is
  shared: `Tick` stops and resets when the mod isn't hosting, plus
  `Describe` and `Reset`. The phases differ: LS has `SessionPhase` (Inactive,
  Camp, Arriving, Fighting), DA has `Phase` (None, Camp, Arena from
  `ArenaLocator.CurrentPhase`). Keep them per mod.

## Chat, logging and errors

- **`Diagnostics/Log.cs`**: identical. It prefixes `ModIdentity.LogPrefix`.
  *Handling:* move. The prefix comes from the mod's identity.
- **`Diagnostics/ErrorThrottle.cs`**: identical. It uses `LogText.Suppressed`.
  *Handling:* move it with the library's text.
- **`Game/ChatNotices.cs`**: drifted. DA adds `Tell(player, content)` (lines
  20–33) for a message to one player (restart refusals). *Handling:* merge,
  keeping both `Broadcast` and `Tell`.
- **Chat formatting in `Text/Messages.cs`**:
  - LS: `Notice`/`Success`/`Warning` write `<color=…>[{ModeName}] …`, with notices orange `#e8a33d` and success green `#5ad45a`.
  - DA: `Notice`/`Warning` with notices yellow `#f5c542`. The prefix is the literal `[Dream Arena] `, while DA's `ModeName` is "PvP Arena".
  - Both use red `#ff4d4d` for warnings.
  - *Handling:* share a formatter. The prefix and colors come from the mod.
- **Log lines in `Text/LogText.cs`** (21 shared names):
  - Identical: `ChangeNote*`, `ConfigApplied`, `ConfigReset`, `ConfigSaveFailed`, `Loaded`, `Unloaded`, `SettingChanged`, `SettingChangeFailed`, `Suppressed`.
  - Worded differently: `TickFailed`, `SessionStopped`, `NotHosting`, `ModeOptionFailed`, `LobbyButtonFailed`, `ConfigHostOnly`, `GoalRemoved`, `PlainArenaPrepared` (DA adds a `plain arena:` prefix) and `ArenaRoomApplied` (DA logs one line with modifiers, LS logs two).
  - `Status` is mod-specific.
  - *Handling:* identical lines go to the library. Lines that name the mod or a command take it as a parameter.

## Workshop change notes

- **`Workshop/WorkshopChangeNote.cs`, `Patches/WorkshopChangeNotePatches.cs`**:
  attach `about/changenote.txt` to an upload. Identical. Both mods patch
  `DewMod.UpdateItem` and `SteamUGC.SubmitItemUpdate`. This is already safe
  with both loaded: `IsOurs` only acts on an item whose id matches the
  mod's own `metadata.id`, and `_pending` is per assembly. *Handling:* move
  first. Read the mod and its id from the context instead of
  `<Mod>.Current`. `ChangeNoteFile` and `ModAboutFolder` go to the library.

## Arena basics

| File(s) | What it does | Status | Differences | Handling |
|---|---|---|---|---|
| `Arena/ArenaLocator.cs` | Node and phase lookups | drifted | Shared: `NoNode`. LS: `IsArenaNode` (bounds + not Start), `IsStandingInArena` (also not in transition), `IsInArenaNode`, `CurrentNode`. DA: `CurrentPhase` (None/Camp/Arena; an out-of-range index counts as Camp), `CampNode`, `TryFindArenaNode`. LS has the same search privately in `Arena/CampRift.cs` `TryFindArenaNode` (lines 50–76) | Merge into one locator with all of them. |
| `Arena/ArenaLayout.cs` | Finds the arena center and radius | drifted | LS requires an active `Room` and reads `SingletonBehaviour<Room_BossArena>.instance`, which runs `FindObjectOfType` twice when the marker is missing (against the `.softInstance` rule). It returns `ArenaPoints`. DA uses `.softInstance`, adds a `DewHeroesTeleporter.destination` fallback before the largest section (lands players in the arena, not the antechamber), returns the source name, and makes `TryFindLargestSection` public (used by `Camp/CampLayout.cs`) | Merge: LS's active-room guard and `ArenaPoints`, with DA's `softInstance`, teleporter fallback and source. Check the fallback in LS rooms, since it can move LS's center and edge ring. |
| `Arena/HeroStaging.cs` | Teleports heroes into a ring | drifted | Same placement body (cancel channels, teleport, move summons). LS: `PlaceAround(points, heroes)`, where the caller supplies the heroes (`PlayerRoster.FillLiveHeroes`), ring = max(2 m, 0.5 × radius). DA: `TryPlaceInRing()` finds the layout and heroes itself, logs, ring = max(1 m, 0.75 × radius) | Take LS's pure signature. Pass the ring share and minimum as parameters. DA keeps a thin wrapper. |
| `Arena/PlainArena.cs`, `Patches/PlainArenaPatches.cs` | Loads the room cleared, disables entry triggers, removes obstacles | drifted | LS: an instance owned by the session, the arena-node check inside `Prepare`, `Reset` with the session, both patches gated on `IsHosting`. DA: a static class, gated in the patch (`IsHosting` + `PlainArena` setting + `Phase.Arena`), `Forget()` otherwise. Its `StartRoom` postfix is ungated (safe, since `_prepared` is null) | Take LS's session-owned shape and add an "enabled" input for DA's setting. Keep every patch gated on the mod's own mode. |
| `Patches/WorldGenPatch.cs` + `Arena/ArenaRoomOverride.cs` ↔ `Arena/ArenaRooms.cs` | Sets the boss room on nodes and strips world-gen modifiers | drifted | Same target, `ZoneManager.GenerateWorld_Imp`. LS gates in the patch, DA in the session (`_active`). LS: arena nodes only, strips through `ModifierGate.Strip`, no try/catch, two log lines, world gen only. DA: `ForceArenaRoom` toggle, try/catch with `ArenaRoomFailed`, one log line, also re-applies on session start and on a settings change | Merge: DA's try/catch, toggle and blank-room fallback, with LS's `IsArenaNode`. LS's `ModifierGate` (blocks later additions) stays in LS or becomes an optional part. |
| `Arena/CampRift.cs` (LS) ↔ `Camp/CampRift.cs` (DA) | Points the camp rift at the arena node (`NetworknextNodeIndex`), checked every 1 s | drifted | LS also hides the other world-map nodes (`WorldMapNodes.HideAllBut`, LS check 28) and remembers its target. DA doesn't hide the map | Take LS on top of the shared locator. Make map hiding an option. |
| `Game/RunGoal.cs` | Destroys `Quest_ShapeOfDreams` | identical | LS always runs it. DA puts it behind `RemoveRunGoal` | Move as is. |
| `Game/LocalizedNames.cs` | `<Type>_Name` lookup for choice labels | identical | none | Move as is. |
| Cutscene skip: `Patches/CutscenePatch.cs` (LS) ↔ `CutscenePatch` in `Patches/ArrivalPatches.cs` (DA) | Prefix on `DewCutsceneDirector.PlayNetworked` | drifted | LS skips while hosting in an arena node. DA skips while hosting with `SkipCutscenes` on, in any node | Keep a patch per mod, or share one with a mod predicate. |
| Travel vote: `Patches/SkipTravelVotePatch.cs` (LS) ↔ `SkipTravelVotePatch` in `Patches/RunRulePatches.cs` (DA) | Prefix on `ZoneManager.ShouldVoteOnTravel` | drifted | LS always skips while hosting. DA skips only with the setting on and no restart vote pending | Keep per mod. |
| Revive helpers: `Combat/HeroSouls.cs` (LS) ↔ `Heroes/Revives.cs` (DA) | Revive every knocked-out hero | drifted | LS: `ReviveAll(ratio)` iterates `allHeroes.ToArray()` and checks `isActive`. DA: `ReviveAll()` iterates the live collection with no `isActive` check, at full health. LS blocks souls in a `CheckAndAddHeroSoul` prefix and removes soul modifiers. DA only sets `disableQuest` from its tick, after `CheckAndAddHeroSoul` has already run at knock-out | Share LS's `ReviveAll`. Check in game whether DA leaves a soul shrine. |

## Identity and text

- **`Identity/ModIdentity.cs`**: drifted.
  - Shared members: `Tag`, `SaveKey`, `ChangeNoteFile`, `LobbyModeValue`, `LogPrefix`, `OwnedSuffix`, `LobbyButtonName`, `ConfigWindowName`, `IsOurs`.
  - LS adds `SoloButtonName`. DA adds `DummyName`, `DummyLifeEffectId` and `RemnantName`.
  - Naming: LS is consistent (`LastStand`, `laststand`, `LastStand_Mode`, "Last Stand"). DA mixes `DreamArena`, `dreamarena_pvp`, `DreamArena_PvP`, the mode name "PvP Arena" and `arena*` commands.
  - *Handling:* the library derives `LogPrefix`, `OwnedSuffix` and the button and window names from the tag and display name. Each mod keeps its values. Don't rename `SaveKey` or `LobbyModeValue` during the move: they are the mode flag.
- **`Game/GameNames.cs`**: drifted.
  - 17 constants are shared with the same values: 8 arena room names, `DefaultArenaRoom`, `CreateLobbyShow`, `CreateLobbyState`, `CreateLobbyUpdateState`, `LobbySettingsStart`, `LobbySettingsEnable`, `GenerateWorld`, `LocalizedNameSuffix` and `ModAboutFolder`. No other value is shared under a different name.
  - *Handling:* those 17 go to a library `GameNames`. The rest stay per mod.
- **`Config/Tuning.cs`**: drifted.
  - Identical: `ErrorLogIntervalSeconds`, `MaxSettingChangesInChat`, `GoalCheckSeconds`, `FallbackArenaRadius`, `FullCircleDegrees`, `PercentScale`.
  - Same value, different name: `MinArenaRadius`/`MinSectionRadius` (4), `MaxArenaRadius`/`MaxSectionRadius` (18), `CampRiftRetrySeconds`/`CampRiftCheckSeconds` (1).
  - Different values: hero ring share 0.5 vs 0.75, ring minimum 2 vs 1, arrival retry 0.5 s vs staging retry 0.25 s.
  - LS only: `ListSeparator`/`ListJoiner` (PickMany).
  - *Handling:* shared values go to a library `Tuning` under one name each. Ring and retry values stay per mod as parameters.
- **Text classes.** Shared names that are identical are neutral and go to `library/src/Text`. The rest stay per mod.

  | Class | Shared names | Identical | Differs |
  |---|---|---|---|
  | `LogText` | 21 | 11 | Listed in the chat and logging section. |
  | `Messages` | 9 | 6 (setting-change lines) | `ModeName`, `ModeDescription`, `LobbyName`. |
  | `ConfigText` | 21 | 15 (Apply, Cancel, world names, `ChoiceLabel`, `UnknownChoiceLabel`, `ArenaRoomLabel`, and `WindowTitle`/`LobbyButton`, which are just the mod name) | `WindowNote`, `ArenaRoomDescription`, `StripRoomModifiers*`, page titles. |
  | `CommandText` | 6 | 1 (`StatusDescription`) | Command names. |

## Build and packaging

- **`LastStand.csproj` ↔ `DreamArena.csproj`**: identical apart from names,
  except that LS also references `Sirenix.Serialization` (no LS source uses
  it) and lists the Steamworks reference in another place. Both target
  `netstandard2.1` with `GameDir` set to `..\..`. *Handling:* a shared props
  file in `library/` with the game references, imported by both projects,
  plus the `Compile Include` of the library sources. Drop Sirenix from LS
  unless it's needed.
- **Build risk: the container mount.** Each mod builds in its own
  `Mods/<Name>` bind mount in `chen`, so `../library` isn't visible there.
  Add an incus device for `library/` at a path the projects can reach, or
  mount the repo root and set `GameDir` explicitly. Check whether a folder
  without `metadata.json` under `Mods/` upsets the mod scanner before
  mounting it there. DA's build-from-a-copy recipe (`_dabuild`) must copy
  the library too.
- **`pack.sh`**: identical apart from `name=`. *Handling:* one
  `library/pack.sh <mod dir> <Name>`, called by thin per-mod wrappers. The
  library is compiled into the mod's DLL, so the pack still ships one DLL.
- **`.gitignore`**: identical (`bin/`, `obj/`, `*.user`, `/build.sh`,
  `/watch-log.sh`, `dist/`). *Handling:* move it to the repo root.
- **`about/metadata.json`**: same shape (`id`, `name`, `author`, `modVer`,
  `supportedGameVer`, `assemblies` pointing at the Debug DLL). DA has no
  `publishedfileid.txt` yet. *Handling:* nothing to share. `pack.sh`
  rewrites `assemblies`.
- **Local `build.sh`/`watch-log.sh`** (git-ignored): the same apart from the
  mount path and the log filter. *Handling:* one script that takes the mod
  name.

## Conventions and docs

- **LS `AGENTS.md` Conventions** has the full code rules and working rules:
  version bumps as their own commit, the change-note format, schema bumps,
  docs and README. **DA's** is a short summary that points at LS. It adds
  "add a numbered in-game check for each change". It leaves out the
  version-bump and change-note rules, though DA ships a `changenote.txt`.
- **Hard constraints:** DA adds "never Harmony-patch `DewLocalization`" and
  "never write synced state after the server stops". LS has the second one
  only as an API fact (modifier removal at game over).
- **Environment:** DA documents building from a copy while the device points
  at `dist/`. LS doesn't.
- **Stale cross-reference:** LS's "Sister project" table still lists Dream
  Arena 0.1.0 files that no longer exist (`src/ArenaRules.cs`,
  `src/ArenaStage.cs`, `src/ArenaConfigWindow.cs`, …). DA's table is current.
- *Handling:* move the conventions and the shared hard constraints to the
  root `AGENTS.md`, which already says LS's conventions apply to both mods.
  Add library rules: no mod names or mod types in library code, library
  strings in `library/src/Text`, every library patch gated on its own mode,
  and each move followed by both mods' related in-game checks. Replace both
  sister-project tables with a pointer to `library/`.

## Candidates not shared yet

- `Arena/WorldMapNodes.cs` (LS): would give DA a map with only camp and arena.
- `Arena/ModifierGate.cs` (LS): DA strips modifiers but doesn't block later ones.
- Hero level: LS `PlayerRoster.SetHeroLevel` (cheat, clamped to `maxLevel`)
  and DA `Heroes/HeroBoost` `Player level` (raise only, clamped to
  `maxLevel`). A shared "raise hero level" helper could serve both.
- Diagnostics: LS `BossFinder.LogAll` (in `laststand_status`) and DA
  `Diagnostics/ArenaScan.cs` (`arena_scan`) do different jobs. Keep both.

## Suggested order

Move one file or one tight pair at a time. After each step, both mods must
build clean (0 warnings, 0 errors), `grep -rn "//\|/\*" src library` must
print nothing, and the related in-game checks of both mods must pass.

1. **Build setup:** props file, `Compile Include`, container mount. Both mods
   build with an empty `library/src`.
2. **Identical, no mod types:** `Log`, `ErrorThrottle`, `Choice`,
   `ConfigFields`, `SettingChange`, `LocalizedNames`, `RunGoal`, plus the
   library `Text`, `GameNames` and `Tuning` entries they need.
3. **Workshop change note** pair (identical; LS check 83).
4. **Config core:** `ConfigSchema` (version as a parameter), `ConfigFile`,
   `PageAttribute`, `PickOneAttribute`, then LS's `ChoiceList`,
   `PickManyAttribute` and the shared `ArenaRooms` choices.
5. **Settings UI** (LS versions): `ChoiceFields`, `SettingChangeText`,
   `SettingsPanel`, then `ConfigWindow` with its layouts, then
   `SettingsAnnouncer` (LS checks 25 and 91, DA checks 3 and 4).
6. **Lobby and mode**: mode check, `ModeSelection`, `CreateLobbyModePatch`, `LobbyModeOption`
   (preselect parameter), then `LobbyConfigButton`, `LobbySettingsPatch` and
   `InjectedUi` (LS checks 4 and 79, DA check 1).
7. **Arena basics**, drifted, one at a time: `ArenaLocator`, then
   `ArenaLayout`, `HeroStaging`, `PlainArena`, the room override, `CampRift`
   and the revive helper (LS checks 6 and 28, DA checks 5, 6 and 14).
8. **Last:** chat formatting, `ChatNotices` (with `Tell`), and the
   entry-point lifecycle helper.
