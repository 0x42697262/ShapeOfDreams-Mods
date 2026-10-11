# Library

Source shared by every mod in this repository, namespace `SodLib`. It is not
built on its own: each mod's project compiles `library/src/**/*.cs` into
that mod's own DLL (`Directory.Build.props`), so a mod ships one DLL and
works alone on the Workshop. See the root [`AGENTS.md`](../AGENTS.md),
*Architecture*, for why there's no separate library DLL.

## Using it from a mod

`library/src/GlobalUsings.cs` imports every `SodLib` namespace into every
file of every mod, so a mod uses `Log`, `ModMode`, `ChatNotices`,
`SharedMessages` and the rest without `using` lines.

A mod plugs in by completing the library's `partial` classes and
subclassing `ModEntry`. Every member below is required; a missing one is a
compile error.

### `src/Identity/ModIdentity.cs` (`partial class SodLib.Identity.ModIdentity`)

| Member | Meaning | Last Stand | Dream Arena |
|---|---|---|---|
| `Tag` | Log prefix, object-name suffix, AppDomain slot prefix | `LastStand` | `DreamArena` |
| `DisplayName` | Window title, lobby button, chat prefix | `Last Stand` | `Dream Arena` |
| `SaveKey` | The mode flag: `customGameSettingsSaveKey`, synced as `GameSettingsManager.gameSettingsSaveKey` | `laststand` | `dreamarena_pvp` |
| `LobbyModeValue` | The option's value in Create Lobby's game-mode dropdown | `LastStand_Mode` | `DreamArena_PvP` |
| `PreselectInLobby` | Select this mode every time Create Lobby opens (`static readonly bool`) | `true` | `false` |

The library adds `LogPrefix`, `OwnedSuffix`, `LobbyButtonName`,
`ConfigWindowName`, `ChangeNoteFile`, `ConfigVersionField` and `IsOurs`.
A mod adds its own names (Last Stand's `SoloButtonName`, Dream Arena's
`DummyName`) in the same file. Only one loaded mod should preselect its
mode, or the last one to patch wins.

### `src/Text/ModText.cs` (`partial class SodLib.Text.ModText`)

| Member | Meaning |
|---|---|
| `ModeName`, `ModeDescription` | The lobby dropdown's option and the description under it |
| `WindowNote` | The line under the settings window's title |
| `NoticeColor` | The mod's chat color for `SharedMessages.Notice` |
| `TickFailed` | Logged (throttled) when the session's per-frame tick throws |
| `PageTitle(Enum)`, `PageSubtitle(Enum)` | Sidebar name and subtitle of a settings page; cast to the mod's page enum |

### `src/ModHooks.cs` (`partial class SodLib.ModHooks`)

| Member | Meaning |
|---|---|
| `Choices(Enum set)` | The choices for a `[PickOne]`/`[PickMany]` field's set; cast to the mod's set enum |
| `LobbyName()` | The lobby name set when the mode is picked in Create Lobby |

### `src/<Name>Mod.cs`: a `ModEntry` subclass

```csharp
public class LastStandMod : ModEntry
{
    public LastStandConfig config = new LastStandConfig();
    internal static LastStandMod Current => Active as LastStandMod;
    internal LastStandSession Session { get; private set; }
    internal override ModConfig Config => config;
    protected override int ConfigSchemaVersion => ConfigSchema.Current;
    protected override void OnLoaded() { /* create the session */ }
    protected override void OnTick(double now) { /* Session?.Tick(now) */ }
    protected override void OnSettingsChanged() { /* push new settings */ }
    protected override void OnGameStarted() { /* reset for a new GameManager */ }
    protected override void OnUnloading() { /* undo the session's effects */ }
    protected override void OnWorldGenerated() { /* e.g. ArenaRooms.Apply(...) */ }
    protected override bool SkipCutscenes() { /* e.g. ArenaLocator.IsInArenaNode() */ }
    protected override bool SkipTravelVote() { /* true: travel at once; false: the game's vote rule */ }
}
```

The config must be a public field named `config` in the subclass: the game
finds config fields by reflection (`ModBehaviour.modConfigFields`) and names
the file `<class>.<field>.json` (`GetModConfigFilePath`), so the name keeps
existing players' settings files. The config class needs a hidden
`schemaVersion` field (the name is `ModIdentity.ConfigVersionField`) and a
`[Page(...)]` on its first visible field. `ModEntry` is `public abstract`;
the loader skips abstract types and attaches the concrete subclass.

## What's in it

| Folder | Types | Job |
|---|---|---|
| (root) | `ModEntry` | The mod's lifecycle: `Awake` registers it as `Active`; `Start` marks the mod gameplay-altering, calls `OnLoaded`, registers the settings field builders, applies the DLL's Harmony patches and hooks `GameManager` start; `Update` ticks with throttled error logs; `OnDestroy` undoes the session, closes the window, removes injected buttons, detaches the lobby option, unregisters builders and unpatches. `LoadConfigsToDisk` resets an outdated config file; `ApplyConfig` copies, saves and announces. |
| (root) | `ModHooks` | Library half of the hooks: `Behaviour()`, `Config()`, `ApplyConfig()` read `ModEntry.Active`. |
| `Arena/` | `ArenaLocator`, `ArenaPhase`, `ArenaLayout`, `ArenaPoints`, `HeroStaging`, `PlainArena`, `ArenaRooms`, `CampRift`, `WorldMapNodes` | The arena both mods play in: node and phase lookups (an arena node is any node but the camp); the arena's center, radius and anchor (`Room_BossArena`, then the entry teleporter's destination if the mod asks, then the largest room section); heroes placed in a ring (ring share and minimum are the mod's); the room loaded cleared with entry triggers off (`PlainArena.Enabled`); the boss room forced on every arena node, modifiers stripped; the camp rift pointed at the arena, with the other map nodes hidden. |
| `Config/` | `ConfigFile`, `ConfigFields`, `SettingChange`, `Choice`, `ChoiceList`, `PageAttribute`, `PickOneAttribute`, `PickManyAttribute`, `SharedChoices`, `SharedTuning` | Schema reset to `.old`; visible config fields; diffs between two configs; dropdown and checklist choices (`SharedChoices.ArenaRooms`: the 8 boss rooms); shared constants. |
| `Diagnostics/` | `Log`, `ErrorThrottle` | Log lines with the mod's prefix; at most one error per interval, with a count of the rest. |
| `Game/` | `ChatNotices`, `LocalizedNames`, `RunGoal`, `SharedGameNames`, `LiveHeroes`, `HeroRevival` | Raw chat to everyone (`Broadcast`) or one player (`Tell`); localized names; removing `Quest_ShapeOfDreams`; game identifiers both mods use; the heroes still standing; reviving knocked-out heroes (`ReviveAll`, `Revive`, `AnyHeroStanding`, AI allies included). |
| `Mode/` | `ModMode`, `ModeSelection` | Whether this mod's mode is the selected one (`IsSelected`) and this machine hosts it (`IsHosting`); setting or clearing the save key on start settings. |
| `Patches/` | `CreateLobbyModePatch`, `LobbySettingsPatch`, `WorkshopUploadPatch`, `WorkshopSubmitPatch`, `PlainArenaStartServerPatch`, `PlainArenaStartRoomPatch`, `WorldGenPatch`, `CutscenePatch`, `TravelVotePatch` | The Create Lobby option, the lobby settings button, the Workshop change note, the plain arena, world gen (calls `OnWorldGenerated`), the intro cutscene skip (asks `SkipCutscenes`), the travel vote skip (asks `SkipTravelVote`). Each acts only for its own mod. |
| `Text/` | `SharedMessages`, `SharedLogText`, `SharedConfigText`, `ModText` | Chat in the mod's colors with its prefix (`Notice`, `Success`, `Warning`), settings change lines, shared log and config text. |
| `UI/` | `ConfigWindow`, `SettingsPanel`, `ChoiceFields`, `SettingChangeText`, `SettingsAnnouncer`, `LobbyConfigButton`, `LobbyModeOption`, `InjectedUi`, `SettingsLayout`, `ConfigWindowLayout` | The settings window (Apply, Cancel, Escape) and sidebar panel, also used by the mod manager's gear screen; dropdown and checklist widgets; settings notices in chat; the lobby button (host of this mode only); the game-mode option; removing injected buttons on unload. |
| `Workshop/` | `WorkshopChangeNote` | Sends `about/changenote.txt` as the change note when the uploaded item is this mod. |

## Rules for library code

- Never reference a mod's types or namespaces. A value that differs per mod
  is a contract member; add it to the partial class (and to every mod) or
  to `ModEntry` as an abstract member.
- A Harmony patch here runs once per loaded mod. It must act only when its
  own mod is selected (`ModMode`) or the target is its own (`ModIdentity`).
- Keep static state per job and reset it on unload; it's per mod already,
  since each DLL has its own copy.
- A change here builds and runs in every mod: run `./check.sh`, and add an
  in-game check to each affected mod's `AGENTS.md`.
- The code rules in the root `AGENTS.md` apply. Shared text goes in
  `library/src/Text`, shared constants in `SharedTuning`, game identifiers
  in `SharedGameNames`.

[`DISCREPANCIES.md`](DISCREPANCIES.md) lists how the two mods' copies were
merged, and what one mod has that the other could use.
