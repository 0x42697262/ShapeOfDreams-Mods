# Code layout

Same architecture as Last Stand (the sister project): a thin entry point,
one session that owns every subsystem, thin patches, plain rules, game
adapters, and every literal in one named place.

```
src/
  DreamArenaMod.cs        ModEntry subclass: the config, the session, five overrides
  DreamArenaConsole.cs    console commands
  ModHooks.cs             dropdown choices and the lobby name, for the library
  Session/
    DreamArenaSession.cs  owns every subsystem; start, tick, stop, reset
  Config/
    DreamArenaConfig.cs   the settings (pages, groups, labels)
    SessionSettings.cs    validated snapshot every subsystem reads
    Tuning.cs             fixed internal constants
    ConfigChoices.cs      dropdown lists
    ConfigSchema.cs       the config schema version (Current)
    TeamMode.cs, SettingsPage.cs, ChoiceSet.cs
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
  Patches/                thin Harmony patches, one concern per file
  Text/                   ConfigText, Messages, LogText, CommandText, ModText
  Identity/ModIdentity.cs our names, tags and keys
  Diagnostics/            ArenaScan
```

## Shared library

The plumbing both mods need lives in `library/src` (namespace `SodLib`) and
is compiled into `DreamArena.dll`; see the
[library README](../../../library/README.md). That covers the lifecycle
(`ModEntry`: load, tick with throttled errors, unload cleanup, the config
reset on a schema change, Apply and the settings notices), the mode check
(`ModMode`) and selecting the mode, the settings window and panel with its
dropdown fields, the lobby dropdown option and settings button, UI cleanup,
`Log`, `ErrorThrottle`, `ChatNotices`, `RunGoal`, `LocalizedNames`, the
Workshop change note, and shared text, game names and tuning. A fix there
reaches both mods; build both (`./check.sh`).

Dream Arena plugs in through the contract files: `Identity/ModIdentity.cs`,
`Text/ModText.cs`, `ModHooks.cs`, `DreamArenaMod.cs` (the `ModEntry`
subclass) and `Config/ConfigSchema.cs` (`Current` only; the file check is
`ConfigFile.ResetIfOutdated` in the library).

## Where new code goes

- A setting: a field in `DreamArenaConfig` (with `[LabelText]` and
  `[Description]` from `ConfigText`), validated in `SessionSettings.From`,
  read only from `SessionSettings`.
- A constant: `Tuning`. A game identifier: `GameNames`. Text: `src/Text`.
- A hook into the game: a patch that checks `ModMode` and calls the
  session or a service. No rules or state in patches.
- Anything that runs per frame: a subsystem ticked by the session, on a
  timer, with `.softInstance`.
