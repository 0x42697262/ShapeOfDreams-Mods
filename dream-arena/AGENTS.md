# AGENTS.md: Dream Arena

Working notes for anyone, human or agent, building this mod. Read the root
[`AGENTS.md`](../AGENTS.md) first: the code and working rules, the build
scripts and the shared library live there. The design lives in
[`docs/`](docs/README.md), one topic per file. Open the table of contents
and read only the files for the area you're changing.

## Status

0.2.0 is a full refactor onto Last Stand's architecture. It builds clean but
hasn't been played yet; every item under "Needs an in-game check" is open.
0.1.0's mechanisms (relations, plain arena, remnants, dummy, restart vote)
were seen working before the refactor, and their logic was kept.

## Sister project

[Last Stand](../last-stand) is a host-only wave mode for the same game, in the
same repo, and the model for this mod's design. The plumbing both need
(settings window, lobby option and button, mode checks, config reset, chat
and log helpers, Workshop change notes, the mod lifecycle) is in
[`library/`](../library/README.md); use it from there.
[`library/DISCREPANCIES.md`](../library/DISCREPANCIES.md) lists what the two
mods still carry in their own copies (the arena basics).

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
- **Never Harmony-patch `DewLocalization`.** 0.1.0 patched `GetUIValue`;
  after that every call threw a `NullReferenceException`, all UI text
  failed, and the title screen showed the prefab's raw Korean text. Change
  what the caller shows instead.
- **Mark it as gameplay-altering** (`instance.isAlteringGameplay = true`).
- **Don't bypass the game's cross-play block** for modded lobbies.

## Environment

See the root [`AGENTS.md`](../AGENTS.md), *Environment*: `./build.sh
dream-arena`, `./watch-log.sh dream-arena`, `./check.sh`. Dream Arena is
mounted into `Mods/DreamArena` as incus device `dreamarena`;
`about/metadata.json` points at the Debug DLL.

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
15. **Lobby button only in its own mode:** load Dream Arena and Last Stand
    together. Host a Dream Arena lobby: the settings panel shows the
    **Dream Arena** button and no Last Stand button. Host a Last Stand or
    Shape of Dreams lobby: no Dream Arena button.
16. **Player level** (schema 2): a new config shows `Player level` at 20.
    Set it to 10: heroes reach level 10 in camp (log `heroes: <hero>
    raised to level 10.`) on the host and a vanilla client; a hero already
    above 10 keeps their level. At 0 nothing is raised. Old config files
    are moved to `.old`.
17. **Shared library build:** after the move to `library/`, the log shows
    `[DreamArena] loaded: Dream Arena v…` once, the lobby button, settings
    window (all pages, Apply, Cancel, Escape), the gear screen's dropdowns,
    settings notices in chat, and the Create Lobby option (not preselected,
    lobby name `Dream Arena (PvP)`) work as before. Unloading logs
    `[DreamArena] unloaded.`

## Conventions

The code rules and working rules are in the root [`AGENTS.md`](../AGENTS.md),
*Conventions*. Dream Arena's own places: tunables in `DreamArenaConfig`
(validated in `SessionSettings.From`), constants in `Tuning`, game
identifiers in `GameNames`, our names in `ModIdentity`, every string in
`src/Text` (`ModText` for the library's contract).
