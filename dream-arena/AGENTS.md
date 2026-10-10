# AGENTS.md: Dream Arena

Working notes for anyone, human or agent, building this mod. The design lives
in [`docs/`](docs/README.md), one topic per file. Open the table of contents
and read only the files for the area you're changing.

## Status

0.2.0 is a full refactor onto Last Stand's architecture. It builds clean but
hasn't been played yet; every item under "Needs an in-game check" is open.
0.1.0's mechanisms (relations, plain arena, remnants, dummy, restart vote)
were seen working before the refactor, and their logic was kept.

## Sister project

[Last Stand](../last-stand) is a host-only wave mode for the same game, built
the same way, and the model for this repo's design. Shared code that should
stay in step:

| Need | Last Stand source |
|---|---|
| Settings panel, window, dropdowns | `src/UI/SettingsPanel.cs`, `ConfigWindow.cs`, `ChoiceFields.cs` |
| Lobby button, game-mode option | `src/UI/LobbyConfigButton.cs`, `LobbyModeOption.cs` |
| Settings change notices | `src/UI/SettingsAnnouncer.cs`, `SettingChangeText.cs` |
| Config schema reset | `src/Config/ConfigSchema.cs`, `ConfigFile.cs` |
| Workshop change note | `src/Workshop/`, `src/Patches/WorkshopChangeNotePatches.cs` |
| Packing | `pack.sh` |

Last Stand's `AGENTS.md` holds most confirmed game API facts. Check there
before decompiling something again.

## Hard constraints

- **Host-only.** Clients run the unmodded game. Only server-side logic and
  synced state reach them. Anything drawn per client, such as HUD text,
  localized strings, lighting, fog or post-processing, can't be changed for
  them. Don't add a feature only the host can see.
- **Check whether a field is synced before relying on it.** Setting a plain
  field on the host does nothing for clients. Known trap here:
  `Shrine.maxUseCountPerPlayer` and `useCooldown` aren't synced; reset the
  synced use count and `_lastUseTime` after each use instead.
- **Use `.softInstance`, never `.instance`, in per-frame code.**
  `NetworkedManagerBase<T>.instance` runs `FindObjectOfType` twice when the
  manager is missing. That caused full scene scans every frame and host lag
  in 0.1.0.
- **Throttle retries.** Anything that waits for a room retries on a timer
  with a cap, never every frame.
- **Never write synced state after the server stops.** Game over and restarts
  shut the transport down first; check `NetworkServer.active`.
- **Mark it as gameplay-altering** (`instance.isAlteringGameplay = true`).
- **Don't bypass the game's cross-play block** for modded lobbies.

## Environment

- The game and .NET SDK live in the incus container **`chen`**. Run commands
  with `incus exec chen -- su chicken -s /bin/bash -c '...'`.
- Game dir in `chen`: `~/.local/share/Steam/steamapps/common/Shape of Dreams`.
- This repo is bind-mounted into `Mods/DreamArena` as incus device
  `dreamarena`. `about/metadata.json` points at the Debug DLL.
- `build.sh` and `watch-log.sh` are local and git-ignored. Without them, run
  `dotnet build` in the mounted directory inside `chen`.
- When the device points at `dist/DreamArena` (for an upload), build from a
  copy outside `Mods/`: tar `DreamArena.csproj` and `src`, unpack under
  `<Game>/_dabuild/DreamArena` (same depth, so `<GameDir>..\..</GameDir>`
  resolves, and outside `Mods/`, so no `IdConflict`), build, delete it.
- There's no C# language server. A clean build (0 warnings, 0 errors) is the
  check.
- Game log in `chen`: `~/.local/share/Steam/steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/Player.log`.
- Decompiled game code: `~/sod-modding/decomp-new/` in `chen`.

## Needs an in-game check

1. **Lobby option:** Create Lobby lists "PvP Arena" (not preselected),
   picking it renames the lobby `Dream Arena (PvP)` and shows the
   description, and the run starts in the mode (`arena_status`:
   `mode=True active=True`). Picking Shape of Dreams starts a normal run with
   no Dream Arena log lines.
2. **Old config reset:** with a 0.1.0 config file present, the log says
   `saved config is from an older version` once, the old file is kept as
   `.old`, and the defaults load.
3. **Settings window:** the lobby panel's **Dream Arena** button opens the
   window with five pages (Match, Arena, Camp, Heroes, Rules); Apply saves,
   Cancel and Escape drop the edits; the same panel shows in the mod
   manager's gear screen. A friend with the mod gets no button.
4. **Change notices:** changing `Team mode` and `Revive after` in a lobby
   with a vanilla client shows `[Dream Arena] Host changed ...` lines to
   both; nothing on the title screen.
5. **Camp:** everyone friendly, the stock rift prompt leads straight to the
   arena with no world map and no vote, ten remnants ring the rift, the
   dummy stands beside them.
6. **Arena live:** crossing the rift says `The arena is live. Free for all...`
   in red to everyone, nameplates turn hostile on a vanilla client, players
   land in a ring inside the arena (not the antechamber), and there are no
   monsters, props or shrines. Returning to camp says `Back in camp...`.
7. **Two teams:** with 3+ players, even and odd joiners can hurt only the
   other team.
8. **No goal:** the Shape of Dreams goal is gone from the quest tracker for
   the host and a vanilla client (log `removed the run's Shape of Dreams
   goal`).
9. **Remnants:** a vanilla client can take several items in a row from the
   same remnant, a taken item disappears from that player's list only, and a
   remnant whose list is empty can't be opened. After a mod reload there's
   still one ring.
10. **Dummy:** never moves, attacks or dies, heals 3 s after the last hit,
    and after a mod reload there's still one.
11. **Heroes:** levels at the cap, memories at 10, max health x100 on a
    vanilla client's health bar; a knock-out gets up after 10 s at full
    health with no Lost Soul quest.
12. **Restart:** `-restart` from a vanilla client in the arena starts the
    vote panel for everyone; passing it revives everyone, wipes builds,
    returns the starting Q and R memories, clears the floor, and the camp
    remnants are full again. Typing it in camp gets a red refusal only for
    that player.
13. **Shutdown:** game over is impossible; leaving to the title screen and
    unloading the mod log `PvP Arena session ended` with no `InitSyncObject`
    exception.
14. **Plain arena per room:** each of the 8 arena rooms loads cleared, with
    the barriers open, and the ring lands inside the arena.

## Conventions

Same as Last Stand's: read its `AGENTS.md` "Conventions" section. In short:

- **No comments** in code. The why goes in `docs/`.
- **No magic values.** Tunables in `DreamArenaConfig` (validated in
  `SessionSettings.From`), constants in `Tuning`, game identifiers in
  `GameNames`, our own names in `ModIdentity`, every string in `src/Text`.
  Plain `0`, `1`, `null` and `string.Empty` are fine.
- **Thin patches** that check `ArenaMode` and call the session. Rules in
  plain classes, game access in adapters, the `ModBehaviour` only wires.
- **Fail safe** on the host: retry on a timer or skip, never throw from a
  patch or per-frame code, undo every effect on unload.
- **American English.** **Don't commit** unless asked.
- **Bump `ConfigSchema.Current`** on any default change, rename or removal.
- **Keep `docs/` in step**, and add a numbered in-game check here for each
  change.
- Check before reporting done: a clean build, `grep -rn "//\|/\*" src`
  prints nothing, and no new literals outside `src/Config`, `src/Text`,
  `src/Identity` and `src/Game/GameNames.cs`.
