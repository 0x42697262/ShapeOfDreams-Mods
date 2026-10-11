# Discrepancies between the mods

How Last Stand (LS) and Dream Arena (DA) code was merged into
`library/src`, what stays per mod on purpose, and what one mod has that the
other could use. `./check.sh` finds the copies (`tools/dupscan.py`: six or
more identical lines in two files, after normalizing mod names) and fails
when they grow; `tools/duplicates.txt` is the current baseline.

## Done: moved into the library

Before the move the two mods shared 499 duplicated six-line windows across
about 45 file pairs. Now there are 5, all in the two entry-point
subclasses (`LastStandMod`, `DreamArenaMod`), which are the same shape on
purpose: each fills in `ModEntry`'s overrides. What moved, and how each
difference was settled:

| Area | Moved | Settled how |
|---|---|---|
| Lifecycle | `LastStandMod`/`DreamArenaMod` bodies → `ModEntry` | One base class. LS builds its session with settings, DA calls `UseSettings` after; both now go through `OnLoaded`. LS resets on unload, DA calls `Shutdown`; each keeps its own in `OnUnloading`. |
| Config core | `Choice`, `ConfigFields`, `SettingChange`, `ConfigFile`, `ConfigSchema.IsCurrent`, `PageAttribute`, `PickOneAttribute`, `PickManyAttribute`, `ChoiceList` | Attributes take any `Enum`. `ConfigFile.ResetIfOutdated(path, version)`; `ConfigSchema.Current` stays per mod. DA gets checklists (`PickMany`) for free. |
| Choice lists | the 8 arena rooms | `SharedChoices.ArenaRooms`; each mod's `ConfigChoices` keeps its own sets. |
| Settings UI | `SettingsPanel`, `ChoiceFields`, `SettingChangeText`, `ConfigWindow` (+ layouts), `SettingsAnnouncer` | LS's versions (a superset). The panel's first page is the first `[Page]` field instead of a hard-coded `General`/`Match`. Page titles come from `ModText`. |
| Lobby and mode | `LastStandMode`/`ArenaMode` → `ModMode`; `ModeSelection`, `CreateLobbyModePatch`, `LobbyModeOption`, `LobbyConfigButton`, `LobbySettingsPatch`, `InjectedUi` | `ModIdentity.PreselectInLobby` (LS true, DA false). `ModHooks.LobbyName()` (LS adds the starting world). The button shows only for `ModMode.IsHosting()`. |
| Chat, logs, errors | `Log`, `ErrorThrottle`, `ChatNotices` (DA's, with `Tell`), `Notice`/`Success`/`Warning` and settings lines → `SharedMessages` | The chat prefix is `DisplayName`, the notice color `ModText.NoticeColor`. DA gained `Success`. |
| Workshop | `WorkshopChangeNote`, its two patches | Matches the uploaded item against the loaded mod through `ModHooks.Behaviour()`. DA gets change notes too. |
| Misc | `RunGoal`, `LocalizedNames` | As is. DA keeps its `RemoveRunGoal` switch around the call. |
| Names and text | 17 game names → `SharedGameNames`; 5 constants → `SharedTuning`; shared log, config and chat text → `SharedLogText`, `SharedConfigText`, `SharedMessages` | Per-mod values (mode name, description, window note, colors, tick failure text, page titles) went to `ModText`. Some log lines now name the mod instead of "PvP Arena"/"the arena". |
| Build | two 290-line `.csproj` files, two `pack.sh`, two `.gitignore`, local `build.sh`/`watch-log.sh` | `Directory.Build.props`, one-line projects, root `build.sh`, `pack.sh`, `watch-log.sh`, `check.sh`, one `.gitignore`. |
| Conventions | the two *Conventions* sections | The root `AGENTS.md`, with the DRY rule. |
| Arena lookups | `ArenaLocator` (both), DA's `Phase`, LS's private `TryFindArenaNode` | One locator with both sets of lookups; `ArenaPhase`. An arena node is any node but the camp (same result in both). |
| Arena layout | `ArenaLayout`, `ArenaPoints` | LS's active-room guard and `ArenaPoints` (now with `Source`), DA's `.softInstance` and anchor name. The teleporter fallback is a parameter: DA on, LS off, so LS's center and edge ring don't move. |
| Hero ring | `HeroStaging`, LS `PlayerRoster.FillLiveHeroes` | LS's pure `PlaceAround`, with the ring share and minimum as parameters (LS 0.5 / 2 m, DA 0.75 / 1 m); `LiveHeroes.Fill`. DA keeps a thin `ArenaStaging` wrapper for its log line. |
| Plain arena | `PlainArena`, its two patches | Static, with `Enabled` (DA's setting); both patches gated on `ModMode.IsHosting()` (DA's `StartRoom` postfix was ungated). |
| Room override | LS `ArenaRoomOverride` + `WorldGenPatch`, DA `ArenaRooms` + `WorldGenPatch`, LS `ModifierGate.Strip` | `ArenaRooms.Apply(room, strip)` with DA's try/catch and blank-room fallback, LS's arena-node check, one log line. The patch calls `OnWorldGenerated`; DA keeps its `Force arena room` toggle there. LS's `ModifierGate` still blocks later additions. |
| Camp rift | `CampRift` (both), LS `WorldMapNodes` | LS's version on the shared locator; map hiding is a constructor flag (LS on, DA off). |
| Revives | LS `HeroSouls.ReviveAll`/`AnyHeroStanding`, DA `Revives.ReviveAll` | `HeroRevival`: LS's safe loop (copy, skip inactive). DA's restart and console use it at full health. |
| Cutscene skip | `CutscenePatch` (both) | One patch asking the mod's `SkipCutscenes()` (LS: in an arena node; DA: its setting). |

## Other differences

- **`Config/Tuning.cs`**:
  - Moved to `SharedTuning`: `FallbackArenaRadius`, `MinArenaRadius`,
    `MaxArenaRadius`, `FullCircleDegrees`, `PercentScale`,
    `CampRiftCheckSeconds`.
  - Still per mod on purpose: hero ring share (0.5 vs 0.75) and minimum
    (2 vs 1 m), passed to `HeroStaging`; arrival retry 0.5 s vs staging
    retry 0.25 s.
- **Console commands** (`LastStandConsole` ↔ `DreamArenaConsole`) stay per
  mod. Shareable: the host-only check of the config command (LS calls
  `ConfigWindow.Open()`, DA `Toggle()`; pick one) and a cheat guard. DA's
  commands are `arena*`, which don't match its id `dreamarena`.

## Candidates not shared yet

- Map hiding: `WorldMapNodes` is in the library; DA could turn it on with
  `new CampRift(true)` (a setting) to show only camp and arena.
- `Arena/ModifierGate.cs` (LS): DA strips modifiers but doesn't block later ones.
- Hero level: LS `PlayerRoster.SetHeroLevel` (cheat, clamped to `maxLevel`,
  through `PlayerRoster.ForEachHero`) and DA `Heroes/HeroBoost` `Player
  level` (raise only, clamped to `maxLevel`). A shared hero loop and "raise
  hero level" helper could serve both.
- Diagnostics: LS `BossFinder.LogAll` (in `laststand_status`) and DA
  `Diagnostics/ArenaScan.cs` (`arena_scan`) do different jobs. Keep both.

## Suggested order

Everything both mods carried is in the library. What's left are
candidates: code only one mod has that the other, or a new mod, could use
(above). Move one at a time; after each step `./check.sh` must pass (run
`./check.sh --update-baseline` when duplicates drop) and the related
in-game checks of both mods must pass.
