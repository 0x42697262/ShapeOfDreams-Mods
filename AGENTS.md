# AGENTS.md: Shape of Dreams mods

Two host-only mods for *Shape of Dreams* live here, each in its own folder
with its own `AGENTS.md`, `docs/`, project and Workshop item. Read the
`AGENTS.md` of the mod you're changing before you change it:

- [`last-stand/AGENTS.md`](last-stand/AGENTS.md): Last Stand, the wave mode.
  Its *Conventions* (code rules and working rules) apply to both mods.
- [`dream-arena/AGENTS.md`](dream-arena/AGENTS.md): Dream Arena, the PvP mode.

[`modding/`](modding/README.md) is the general guide to modding the game.
[`library/DISCREPANCIES.md`](library/DISCREPANCIES.md) lists the code both
mods carry in their own copies and how each difference should be settled
before it moves into a shared library.

`Directory.Build.props` holds the game path and the game DLL references for
every mod, so each `.csproj` is one line.

## Environment

- The game and the .NET SDK live in the incus container **`chen`**. The
  scripts run commands there with `incus exec chen -- su chicken -s /bin/bash -c '...'`.
  `SOD_CONTAINER` and `SOD_USER` override the names (`tools/env.sh`).
- **This repo is mounted into `chen` at the same path** as on the host (incus
  device `sodmods`), so builds run there. Builds never run inside `Mods/`,
  so an upload pack in `Mods/` doesn't break them.
- **Each mod is mounted into the game's `Mods/<Name>`** (devices `laststand`
  and `dreamarena`), pointing at `<repo>/<mod>` while developing. The game
  loads the Debug DLL from there (`about/metadata.json`). With developer
  mode and auto-reload on, a new build reloads the mod when the game window
  gets focus.
- `./build.sh <mod|all> [Debug|Release]` builds. `./check.sh` (every mod's
  build plus the comment, literal and duplicate checks) must pass before
  reporting done.
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
