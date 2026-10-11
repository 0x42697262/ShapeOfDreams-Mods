# Code layout

Same architecture as Last Stand (the sister project): a thin `ModBehaviour`,
one session that owns every subsystem, thin patches, plain rules, game
adapters, and every literal in one named place.

```
src/
  DreamArenaMod.cs        ModBehaviour: config, lifecycle, settings notices
  DreamArenaConsole.cs    console commands
  Session/
    DreamArenaSession.cs  owns every subsystem; start, tick, stop, reset
  Mode/
    ArenaMode.cs          IsSelected / IsHosting (the save-key mode flag)
    ModeSelection.cs      writes the mode into the lobby's start settings
  Config/
    DreamArenaConfig.cs   the settings (pages, groups, labels)
    SessionSettings.cs    validated snapshot every subsystem reads
    Tuning.cs             fixed internal constants
    ConfigChoices.cs      dropdown lists
    ConfigSchema.cs, ConfigFile.cs  reset outdated config files
    ConfigFields.cs, SettingChange.cs  visible fields, before/after diffs
    TeamMode.cs, SettingsPage.cs, ChoiceSet.cs, Choice.cs, *Attribute.cs
  Pvp/
    TeamRule.cs           who is an enemy of whom (plain logic)
    Relations.cs          writes the relation SyncLists
    PhaseWatch.cs         camp vs arena, applies relations, announces
  Arena/
    ArenaLocator.cs       phase, camp node, arena node
    ArenaRooms.cs         room override and modifier removal
    ArenaLayout.cs        arena center and radius
    HeroStaging.cs        the arrival ring
    PlainArena.cs         cleared, lit loading
    PveSweep.cs           removes PvE from the arena
    Phase.cs
  Camp/
    CampRift.cs           direct rift
    CampRemnants.cs       builds the ring, tracks taken items
    RemnantRing.cs        ring layout solver
    RemnantChoices.cs     what a remnant offers, after a pick
    RemnantStock.cs       items per kind and rarity
    RemnantTag.cs         marks our remnants by name
    CampLayout.cs         rift center, opening, ground slots
    TrainingDummy.cs      the camp dummy
  Heroes/
    HeroBoost.cs          level, memory floor, health multiplier
    Revives.cs            auto revive, rescue quests
  Match/
    MatchRules.cs         game over, population, lucid dreams
    MatchRestart.cs       the restart itself
    RestartVote.cs        -restart through the travel vote
    BuildWipe.cs          wipes and restores builds
  Game/
    GameNames.cs          game identifiers that can't be typeof/nameof
    ChatNotices.cs        broadcast and per-player chat
    RunGoal.cs            removes the run goal
    LocalizedNames.cs     game display names
  Patches/                thin Harmony patches, one concern per file
  UI/                     settings panel and window, lobby button and mode option, change notices
  Text/                   ConfigText, Messages, LogText, CommandText
  Identity/ModIdentity.cs our names, tags and keys
  Diagnostics/            Log, ErrorThrottle, ArenaScan
  Workshop/               change note attached on upload
```

`Config/`, `Identity/`, `UI/` (shared with Last Stand: `SettingsPanel`,
`ConfigWindow`, `ChoiceFields`, `LobbyConfigButton`, `LobbyModeOption`,
`SettingsAnnouncer`), `Mode/`, `Workshop/` and `Diagnostics/Log` were adapted
from Last Stand. When fixing a bug in one of them, check the other repo.

## Where new code goes

- A setting: a field in `DreamArenaConfig` (with `[LabelText]` and
  `[Description]` from `ConfigText`), validated in `SessionSettings.From`,
  read only from `SessionSettings`.
- A constant: `Tuning`. A game identifier: `GameNames`. Text: `src/Text`.
- A hook into the game: a patch that checks `ArenaMode` and calls the
  session or a service. No rules or state in patches.
- Anything that runs per frame: a subsystem ticked by the session, on a
  timer, with `.softInstance`.
