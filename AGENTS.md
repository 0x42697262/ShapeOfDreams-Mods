# AGENTS.md: Shape of Dreams mods

Host-only mods for *Shape of Dreams*, in one repository. Read this file
first, then the `AGENTS.md` of the mod you're changing. The rules here apply
to every mod and to the shared library.

## Layout

```
AGENTS.md, README.md     this file, the overview
build.sh                 ./build.sh <mod|all> [Debug|Release]
pack.sh                  ./pack.sh <mod>  -> <mod>/dist/<Name>/
watch-log.sh             ./watch-log.sh [<mod>|all]
check.sh                 builds every mod and checks the code rules
Directory.Build.props    game path, game DLL references, the library include
tools/                   env.sh (shared shell setup), dupscan.py, duplicates.txt
library/                 shared source, compiled into every mod (library/README.md)
last-stand/              Last Stand: LastStand.csproj, src/, docs/, about/, AGENTS.md
dream-arena/             Dream Arena: same shape
modding/                 the guide to modding the game (Last Stand is the example)
```

## Architecture

**One DLL per mod, shared source.** Each mod's project compiles its own
`src/` plus every file in `library/src/` into one DLL, `<Name>.dll`, and
`pack.sh` ships only that DLL. There is no library DLL, on purpose: the
game's loader (`DewMod`) renames every assembly it loads
(`<name>_<ticks>`) and loads it from bytes with no assembly resolver, so a
second DLL wouldn't link; and every Workshop item must work alone, since a
player may subscribe to one mod only. Each mod's copy of a library type is
its own type in its own assembly, so static state (the open config window,
the settings panel's last page) never mixes between mods.

`Directory.Build.props` does the wiring once for every project: the game's
`Managed` folder (`$(GameDir)`, the Steam install in `chen` by default), the
63 game DLL references, and `<Compile Include="$(LibraryDir)**/*.cs" />`.
A mod's `.csproj` is one line. `library/src/GlobalUsings.cs` imports the
library's namespaces into every file of every mod.

**The contract.** The library never references a mod's types. What differs
per mod comes in through a few `partial` classes that the library declares
and each mod completes (`library/README.md` lists every member):

| Mod file | Fills in |
|---|---|
| `src/Identity/ModIdentity.cs` | `Tag`, `DisplayName`, `SaveKey`, `LobbyModeValue`, `PreselectInLobby` (plus its own object names) |
| `src/Text/ModText.cs` | `ModeName`, `ModeDescription`, `WindowNote`, `NoticeColor`, `TickFailed`, `PageTitle`, `PageSubtitle` |
| `src/ModHooks.cs` | `Choices(set)` for the settings dropdowns, `LobbyName()` |
| `src/<Name>Mod.cs` | a `ModEntry` subclass: the config field, the session, seven small overrides |
| `src/Config/ConfigSchema.cs` | `Current`, the config schema version |

A missing member is a compile error, so a new mod can't half-implement it.

**What the library gives** (see `library/README.md` for the full list): the
mod's lifecycle (`ModEntry`: load, tick with throttled errors, unload
cleanup, config reset on a schema change, Apply and settings notices), the
settings window and sidebar panel with dropdown and checklist fields, the
lobby game-mode option and settings button, mode checks (`ModMode`),
settings change notices in chat, chat and log helpers, run-goal removal,
Workshop change notes, the arena basics (node lookups, center and radius,
hero ring, plain arena, room override, camp rift, map hiding, revives,
cutscene skip), and shared text, game names and tuning.

**Shared Harmony patches.** Library patches are compiled into each DLL and
applied by each mod's own `harmony.PatchAll()`, so with two mods loaded the
game method is patched twice, once per mod. Each must act only for its own
mod: check `ModMode.IsHosting()`/`IsSelected()` or the mod's own
`ModIdentity`, as `LobbyConfigButton` and `WorkshopChangeNote` do.

**Adding a mod.**

1. Copy a mod folder's shape: `<mod>/<Name>.csproj` (one line), `about/`
   with `metadata.json` (`"assemblies": [ "bin/Debug/netstandard2.1/<Name>.dll" ]`).
2. Fill in the five contract files above. Start from Dream Arena's; it's
   the smaller mod.
3. Give the config class a hidden `schemaVersion` field
   (`[HideInInspector] public int schemaVersion = ConfigSchema.Current;`)
   and a `[Page(...)]` on its first field; pages are the mod's own enum.
4. `./build.sh <mod>`, add an incus disk device for `Mods/<Name>`, and an
   `AGENTS.md` and `docs/` like the other mods.

## Environment

- The game and the .NET SDK live in the incus container **`chen`**. The
  scripts run commands there with `incus exec chen -- su chicken -s /bin/bash -c '...'`.
  `SOD_CONTAINER` and `SOD_USER` override the names (`tools/env.sh`).
- **This repo is mounted into `chen` at the same path** as on the host (incus
  device `sodmods`), so builds run there against the real `library/`. Builds
  never run inside `Mods/`, so an upload pack in `Mods/` doesn't break them.
- **Each mod is mounted into the game's `Mods/<Name>`** (devices `laststand`
  and `dreamarena`), pointing at `<repo>/<mod>` while developing. The game
  loads the Debug DLL from there (`about/metadata.json`). With developer
  mode and auto-reload on, a new build reloads the mod when the game window
  gets focus.
- `./build.sh <mod|all> [Debug|Release]` builds. `./check.sh` must pass
  before reporting done (see *Working rules*).
- `./watch-log.sh <mod>` follows the game log filtered to that mod, Harmony
  and errors; `./watch-log.sh` shows everything. Log in `chen`:
  `~/.local/share/Steam/steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log`.
- There is no C# language server. A clean build is the type check.
- Decompiled game code: `~/sod-modding/decomp-new/` in `chen`. Last Stand's
  `AGENTS.md` (*Reading the game's code*, *Confirmed API facts*) has the
  tools and most confirmed game facts; check there before decompiling again.

**Uploading a mod to the Workshop.** The game's mod manager uploads the
whole mounted folder, so never upload from the repo.

1. `./build.sh <mod> Release && ./pack.sh <mod>` writes `<mod>/dist/<Name>/`
   (the DLL, `about/`, `metadata.json` pointing at the DLL) and prints the
   change note.
2. `incus config device set chen <device> source=$PWD/<mod>/dist/<Name>`
3. Upload from the mod manager (developer mode). The first upload writes
   `about/publishedfileid.txt` into the pack; the next `pack.sh` copies it
   back to `<mod>/about/` (commit it).
4. `incus config device set chen <device> source=$PWD/<mod>`

Only one folder with a mod's id may be in `Mods/` at a time (`IdConflict`).

## Conventions

### Code rules

Hard rules for all code here, the library included.

- **No comments.** No `//`, `/* */` or XML doc comments. Code explains
  itself through names, small types and small methods. The *why* (which
  game system a choice relies on, why it reaches clients) goes in the
  topic's file under the mod's `docs/`, in `library/README.md`, or in an
  `AGENTS.md`.
- **No magic strings or hard-coded values.** Every number, duration, limit,
  type or prefab name, room name, chat command and chat text lives in
  exactly one named place:
  - Tunable values go in the mod's config class, with defaults set there.
    Fixed internal constants go in `Tuning` (`SharedTuning` in the library).
  - Fixed game identifiers that can't be a `typeof` or `nameof` (private
    method names, room names, modifier names) go in `GameNames`
    (`SharedGameNames` in the library). Prefer `typeof`, `nameof` and
    generic APIs such as `Dew.CreateActor<T>` when the member is public.
  - Our own identifiers (tags, save keys, object names) go in `ModIdentity`.
  - Text goes in `src/Text` (`library/src/Text` for shared text).
  - Plain `0`, `1`, `null` and `string.Empty` are fine where their meaning
    is obvious.
- **Don't repeat yourself.** One implementation per job, one place per
  fact.
  - Inside a mod, reuse the existing helper instead of writing the loop
    again (`PlayerRoster.ForEachHero`, not another `gamePlayers` loop).
  - Across mods, code that two mods need, or that a new mod would need,
    lives in `library/src` and is used from there. Don't copy a file from
    one mod into another. When a mod's copy of something drifts from the
    library's, fix the library and delete the copy.
  - Mod-specific values reach the library through the contract (partial
    classes, `ModEntry` overrides), never through a mod type referenced
    from the library.
  - `check.sh` fails on new duplicated code (six or more identical lines
    in two files). `library/DISCREPANCIES.md` lists how earlier copies were
    settled and what one mod has that the other could use.
- **Separation of concerns.** Each type has one job. Keep these apart:
  - **Harmony patches** are thin. They hook the game and hand off to a
    service. They hold no rules or state.
  - **Game rules** (wave schedule, scaling, loot rolls) are plain logic
    that doesn't touch Unity or networking, so they can be read and tested
    alone.
  - **Game adapters** wrap the game's API (spawning, pickups, shrines,
    quests, chat). Rules call adapters and never the game directly.
  - **Config, lifecycle and UI** each have their own types. The mod's
    `ModEntry` subclass only wires things together.
- **DDIA design.** Follow the principles of *Designing Data-Intensive
  Applications*:
  - **Reliability.** The host is the server, so a fault there breaks the
    run for everyone. Fail safe: if a room isn't ready or a lookup fails,
    retry on a throttled timer or skip, and never throw from per-frame code
    or a patch. Every effect is undone on unload.
  - **Scalability.** Bound per-frame work. Cap what's alive, use
    `.softInstance`, cache lookups, and scale load by player count.
  - **Maintainability**, the main focus:
    - *Operability:* clear, throttled log lines with a fixed prefix, so a
      problem can be found in `Player.log`.
    - *Simplicity:* the smallest design that works. No speculative
      abstraction.
    - *Evolvability:* new content and settings are added through config
      and data, not by editing control flow.
  - **One source of truth** for state. The server's synced state is the
    record. Don't keep a second copy that can drift from it.

### Working rules

- **American English** in code and docs.
- **Don't commit** unless the user asks.
- **Run `./check.sh`** before reporting done. It builds every mod (0
  warnings, 0 errors), finds comments, finds string literals outside
  `src/Config`, `src/Text`, `src/Identity` and `src/Game/GameNames.cs`
  (`SharedGameNames.cs` in the library), and fails on new duplicated code.
  A library change must build every mod, since every mod compiles it.
  When duplication goes down, `./check.sh --update-baseline` records it.
- **Bump the mod's `ConfigSchema.Current`** whenever a config default
  changes or a field is renamed or removed. The game loads the saved JSON
  over the defaults, so without a bump, players keep the old values. An
  added field alone needs no bump.
- **Version bumps are their own commit.** Code changes (with their docs and
  checks) are committed first. The release is then one commit named
  `<mod> vX.Y.Z` that only changes `modVer` in the mod's
  `about/metadata.json` and the version line (and, if needed, the rest) of
  its `about/changenote.txt`. Never put code in it. Before 1.0, every
  release with features or fixes bumps the minor version (0.7.0 to 0.8.0).
  If more changes land before the upload, commit them on their own and
  keep the same version.
- **Change note.** Rewrite the mod's `about/changenote.txt` for each
  release; Last Stand's `WorkshopChangeNote` (now in the library) sends it
  as the Workshop change note. It covers only what changed since the last
  upload, written like a dev's patch notes, not a press release. The first
  line is the version in bold, `[b]vX.Y.Z[/b]`, matching `modVer` (Last
  Stand from v0.9.0 on; v0.8.0 went up without it). Then a Steam BBCode
  `[list]` of one-line bullets, plain words, no reasons or mechanics (those
  belong in `docs/`). Give the old value when a default changes. Start a
  new feature with `New:` and, if it's on by default, say how to turn it
  off in the same line. End with `Settings are reset with this update (old
  file kept as .old).` when `ConfigSchema.Current` changed since the last
  upload, then `Only the host needs to update.` Example (Last Stand v0.8.0,
  with the version line added):
  ```
  [b]v0.8.0[/b]
  [list]
  [*]Hatred Shard gold now scales with the wave and curse strength (50 per wave) instead of a flat 500.
  [*]Curse kill goals keep growing after wave 48 and are no longer split between players.
  [*]Fast clear's instant kill window is now 1s (was 0.5s).
  [*]New: heroic fast kill. Kill a heroic boss too fast and the next ones get more health. Set "Heroic fast kill: seconds per boss" to 0 to turn it off.
  [/list]

  Settings are reset with this update (old file kept as .old). Only the host needs to update.
  ```
- **Where things go:** new tunables go in the mod's config class and are
  validated in its `SessionSettings.From`. Internal constants go in
  `Tuning`, game identifiers in `GameNames`, every string in `src/Text`.
  Patches only check `ModMode` and call into the session. Anything a second
  mod would need goes in `library/src`.
- **Keep docs in step** with the code. With no comments in the code, the
  docs are where the design and its reasons live. They describe how things
  actually work, not just what they do. Update the topic's file in the
  mod's `docs/` (or `library/README.md`), add a numbered in-game check to
  the mod's `AGENTS.md` for each change, and add a new file plus a line in
  `docs/README.md` for a new topic. A mod's `README.md` is its
  player-facing overview; change it only when a feature or default players
  see changes. A library change gets an in-game check in every mod it
  affects.
