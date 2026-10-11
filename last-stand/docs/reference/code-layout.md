# Code layout

| Folder | Concern |
|---|---|
| `src/LastStandMod.cs` | Entry point. A `ModEntry` subclass: holds the config, creates the session, and overrides `OnLoaded`, `OnTick`, `OnSettingsChanged`, `OnGameStarted`, `OnUnloading`, `OnWorldGenerated` (applies the arena room), `SkipCutscenes` (true in an arena node) and `SkipTravelVote` (true) |
| `src/LastStandConsole.cs` | Console commands |
| `src/ModHooks.cs` | The choices for each settings dropdown and checklist (`Choices`), and the lobby name (`LobbyName`) |
| `src/Identity` | `ModIdentity`: tag, display name, save key, lobby mode value, AppDomain slot names and Last Stand's own object names |
| `src/UI` | The solo button |
| `src/Session` | The run's phases (camp, arriving, fighting) and arrival (`ArenaArrival`, which stages heroes through the library) |
| `src/Arena` | The modifier gate, the shrine ring by the edge (and on Lava Land's arm tips), spawn points, playable and reachable ground, Pure Dream statue spots |
| `src/Waves` | Wave schedule, wave cycle and spawn queue (plain logic), and the director |
| `src/Combat` | Monster catalog, spawning, heroic bosses, per-wave enhancements, hunter zones, wave environments, tracking enemies, Pain |
| `src/Loot` | Loot rules, rarity tables and ledger (plain logic), drops |
| `src/Well` | The Dream Well and Altar of Cleansing (kept at unlimited uses), the Shrine of Ascension (opens after wave 10, uses back every 10th wave), the Maw of Doom at a fixed use count, the Shrine of Destiny (2 claims per player each wave), and the Hatred Remnant (once per player each wave) |
| `src/Game` | Thin wrappers over game APIs, and game identifiers |
| `src/Config` | The mod config, its validated snapshot, the choice lists, the schema version (`ConfigSchema.Current`) and internal tuning |
| `src/Text` | Every player-facing, config, log and command string, and `ModText` (mode name and description, window note, notice color, tick error, page titles) |
| `src/Patches` | Harmony hooks. Each one only hands off to the code above |

## Shared library

The plumbing both mods need lives in `library/src` (namespace `SodLib`) and
is compiled into `LastStand.dll`; see the
[library README](../../../library/README.md). That covers the lifecycle
(`ModEntry`: load, tick with throttled errors, unload cleanup, the config
reset on a schema change, Apply and the settings notices), the mode check
(`ModMode`) and selecting the mode, the settings window and panel with its
dropdown and checklist fields, the lobby dropdown option and settings
button, UI cleanup, chat and log helpers, run-goal removal, the Workshop
change note, and shared text, game names and tuning.

It also holds the arena basics (`library/src/Arena`, `library/src/Game`,
`library/src/Patches`): `ArenaLocator` (arena node, camp node, standing in
the arena), the camp rift (`CampRift`, which also hides the other map nodes
through `WorldMapNodes`), the room override and modifier strip
(`ArenaRooms`, run from `WorldGenPatch`), cleared loading (`PlainArena`,
`PlainArenaPatches`), the cutscene skip (`CutscenePatch`), the arena center
(`ArenaLayout`, `ArenaPoints`), the arrival ring (`HeroStaging`), live heroes
(`LiveHeroes`) and revives (`HeroRevival`). See
[Arena](../start/arena.md) for how Last Stand uses them.

Last Stand plugs in through the contract files: `Identity/ModIdentity.cs`,
`Text/ModText.cs`, `ModHooks.cs`, `LastStandMod.cs` (the `ModEntry`
subclass) and `Config/ConfigSchema.cs` (`Current` only; the file check is
`ConfigFile.ResetIfOutdated` in the library).
