# Releasing

## Versions

`about/metadata.json`'s `modVer` is display-only (the mod manager and the
`loaded:` log line show it). Before 1.0, a release with a new feature bumps
the minor version (0.2.0 to 0.3.0), a fix-only release the patch version.
Commit the bump as its own `vX.Y.Z` commit.

Separately, `ConfigSchema.Current` versions the config file (see
[Settings](settings.md#config-file-and-schema)).

## Packing

The game's mod manager uploads the whole mod folder
(`SteamUGC.SetItemContent(item.path)`), so never upload from the repo: it
would ship `.git`, `src/`, `obj/` and the docs. Build Release, then run
`./pack.sh`. It writes `dist/DreamArena/` with only `DreamArena.dll` and
`about/`, with `metadata.json` pointed at `DreamArena.dll`.

Only one folder with the mod's id may be in `Mods/` at a time
(`IdConflict`), so for the upload point the `dreamarena` incus device at
`dist/DreamArena`, upload, then point it back at the repo.

The first upload creates the item (Private) and writes
`about/publishedfileid.txt` into the uploaded folder; the game reads it to
update the same item later. `pack.sh` copies it back to `about/` (commit it)
and into every new pack. Every upload sets the item to Hidden until it's made
Public again.

## Change notes

Write the release's note in `about/changenote.txt` (Workshop BBCode) before
packing, and commit it with the release. `pack.sh` copies it into the pack
and prints it. On upload, `WorkshopChangeNotePatches` (a prefix and
finalizer on `DewMod.UpdateItem`, and a prefix on
`SteamUGC.SubmitItemUpdate`) swap it in as the update's change note, only
for this mod's id. The log says `workshop: attached about/changenote.txt
(N characters) ...`, or that there was none.

## Updates reach players how

The game never asks Steam to download; it reads only what's installed.
Players get an update when Steam updates the subscribed item, which usually
means restarting the game. Only the host needs the update.
