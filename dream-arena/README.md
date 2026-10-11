# Dream Arena

A host-only PvP mode for **Shape of Dreams**. Fight your friends in a boss
arena: free for all or two teams, no monsters, and no ending. Pick a build in
camp, test it on a dummy, then take the rift and fight.

> **Status: 0.2.0, a full refactor.** It builds clean but hasn't been played
> since the refactor. [`AGENTS.md`](AGENTS.md) holds the working notes and
> the list of in-game checks.

## Features

- **PvP with vanilla friends.** Only the host needs the mod. Friends join on
  an unmodded game and see hostile nameplates, damage and everything else.
- **Free For All, Two Teams or Peaceful.** Teams split by join order.
- **Camp and arena.** Camp is safe; the arena past the rift is live. Chat
  tells everyone when the arena goes live and when they're back in camp.
- **A real arena.** Every room past camp is one boss room (the Lone King's
  Zenith by default, any of 8), loaded cleared and lit, with no boss,
  monsters, props or room modifiers. Players arrive in a ring inside it.
- **Free builds.** Ten remnants around the camp rift offer every memory and
  essence, one remnant per rarity, at level 10 and Flawless quality. Each
  player takes each item once per round.
- **A training dummy** in camp that never moves, fights back or dies.
- **Long fights.** Heroes start at max level with x100 health, memories at
  level 10, and get back up 10 s after a knock-out.
- **No ending.** The run never ends. Type `-restart` in chat to vote for a
  new round: back to camp, builds wiped, remnants full again.
- **Clean run.** No lucid dreams, game modifiers, rescue quests or run goal.
- **Settings in the lobby.** The host's **Dream Arena** button opens every
  setting, and changes are announced in chat.

## Install

Only the host needs the mod.

- **Steam Workshop:** subscribe to Dream Arena, then enable it in the game's
  mod manager.
- **By hand:** put this folder in `<Shape of Dreams>/Mods/DreamArena`.

## Play

1. Create Lobby, then pick **PvP Arena** in the Game Mode dropdown.
2. Optional: change settings with the **Dream Arena** button in the lobby's
   settings panel.
3. Start. In camp, take what you want from the remnants and try it on the
   dummy.
4. Take the rift to fight. Type `-restart` in chat for a new round.

## Docs

The design docs are in [`docs/`](docs/README.md), one topic per file, with a
table of contents.

## Build

From the repository root, run `./build.sh dream-arena` (Debug, which
`about/metadata.json` loads) or `./build.sh dream-arena Release`. The script
runs `dotnet build` in the build container; plain `dotnet build` in this
folder works too. The project finds the game's assemblies through `GameDir`
in the root `Directory.Build.props` (the Steam install on Linux by default;
override it with `-p:GameDir="<Shape of Dreams>"`) and compiles the shared
[`library/`](../library/README.md) into `DreamArena.dll`. To load the build,
this folder must be the game's `<Shape of Dreams>/Mods/DreamArena` (copied
or mounted there).

For the Workshop, build Release and run `./pack.sh dream-arena` from the
repository root. It writes `dream-arena/dist/DreamArena/` with only
`DreamArena.dll` and `about/`, ready to upload from the game's mod manager
(see [Releasing](docs/reference/releasing.md)).
