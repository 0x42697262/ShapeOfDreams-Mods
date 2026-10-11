# Discrepancies between the mods

What Last Stand (LS) and Dream Arena (DA) still carry in their own copies,
how the copies differ, and how to settle each before it moves into
`library/src`. `./check.sh` finds the copies (`tools/dupscan.py`: six or
more identical lines in two files, after normalizing mod names) and fails
when they grow; `tools/duplicates.txt` is the current baseline.

## Done: moved into the library

Before the move the two mods shared 499 duplicated six-line windows across
about 45 file pairs. Now there are 11, in 3 pairs (the arena basics below
and the two entry-point subclasses). What moved, and how each difference
was settled:

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

## Arena basics (still in both mods)

The next step. Each mod has its own copy of:

- **`ArenaLocator`**: LS has `IsArenaNode`, `IsStandingInArena`,
  `IsInArenaNode` and `CurrentNode`; DA has `CurrentPhase`, `CampNode`,
  `TryFindArenaNode` and its own `Phase` enum. One locator with both sets.
- **`ArenaLayout`**: LS waits for an active room and returns `ArenaPoints`;
  DA also tries the entry teleporter's destination and names the anchor.
  Make the teleporter a parameter, off for LS, so its center and edge ring
  don't move.
- **`HeroStaging`**: the same ring with a different share and minimum (LS
  0.5 / 2 m, DA 0.75 / 1 m). Pass them in.
- **`PlainArena`** and its two patches: LS's is owned by the session and
  checks the arena node; DA's is static with a setting. One static class
  with an `Enabled` switch.
- **Room override**: LS `ArenaRoomOverride` with `ModifierGate.Strip`, DA
  `ArenaRooms`, each with its own `WorldGenPatch`. One
  `ArenaRooms.Apply(room, strip)`; the patch calls a `ModEntry` hook.
- **`CampRift`**: LS also hides the other map nodes (`WorldMapNodes`), DA
  doesn't. A constructor flag.
- **Revives** (LS `HeroSouls.ReviveAll`, DA `Revives.ReviveAll`) and the
  **cutscene skip** (LS in arena nodes, DA by setting): one helper, and one
  patch asking a `ModEntry` hook.

## Other differences

- **`Config/Tuning.cs`** still has shared values under each mod:
  - Identical: `FallbackArenaRadius`, `FullCircleDegrees`, `PercentScale`.
  - Same value, different name: `MinArenaRadius`/`MinSectionRadius` (4),
    `MaxArenaRadius`/`MaxSectionRadius` (18),
    `CampRiftRetrySeconds`/`CampRiftCheckSeconds` (1).
  - Different values: hero ring share 0.5 vs 0.75, ring minimum 2 vs 1,
    arrival retry 0.5 s vs staging retry 0.25 s.
  - *Handling:* move them to `SharedTuning` with the arena code that uses
    them, one name each. Ring and retry values become parameters.
- **Console commands** (`LastStandConsole` ↔ `DreamArenaConsole`) stay per
  mod. Shareable: the host-only check of the config command (LS calls
  `ConfigWindow.Open()`, DA `Toggle()`; pick one) and a cheat guard. DA's
  commands are `arena*`, which don't match its id `dreamarena`.

## Candidates not shared yet

- `Arena/WorldMapNodes.cs` (LS): would give DA a map with only camp and arena.
- `Arena/ModifierGate.cs` (LS): DA strips modifiers but doesn't block later ones.
- Hero level: LS `PlayerRoster.SetHeroLevel` (cheat, clamped to `maxLevel`,
  through `PlayerRoster.ForEachHero`) and DA `Heroes/HeroBoost` `Player
  level` (raise only, clamped to `maxLevel`). A shared hero loop and "raise
  hero level" helper could serve both.
- Diagnostics: LS `BossFinder.LogAll` (in `laststand_status`) and DA
  `Diagnostics/ArenaScan.cs` (`arena_scan`) do different jobs. Keep both.

## Suggested order

1. `ArenaLocator`, DA's `Phase`, `CampRift` and `WorldMapNodes`.
2. `ArenaLayout`, `HeroStaging` and `PlainArena`.
3. The room override, revives and the cutscene skip.

After each step `./check.sh` must pass (run `./check.sh --update-baseline`
when duplicates drop) and the related in-game checks of both mods must
pass.
