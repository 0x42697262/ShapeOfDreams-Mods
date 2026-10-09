# Last Stand

A host-only wave-survival mode for **Shape of Dreams**, for solo and co-op. The
party holds a boss arena against endless waves that get harder every wave.
A ring of shrines around the edge of the arena hands out memories and
essences and turns what you collect into upgrades.

> **Status: early.** The code compiles and covers the whole loop below.
> [`AGENTS.md`](AGENTS.md) holds the working notes and the list of things that
> still need an in-game check.

## Features

- One arena, no travel. The fight happens in a boss room with the boss
  removed.
- A new wave starts **once the last one is cleared**, after a short break.
  Each wave is bigger and stronger than the last.
- Every monster gets **more health and damage every wave**.
- **Curse to start** (difficulty option, off by default): no wave timer.
  Each wave waits until a player takes a curse at the Hatred Remnant, which
  curses every player, each with a different curse.
- **Clear too fast and it hits back:** from wave 30, kill 80% of a wave within 10 s and
  every later wave's monsters get double health and damage, stacking with
  each fast clear (heroic bosses take half the stacks).
- **Minibosses** come every 10 waves, then several at once. No real bosses
  for now.
- **Heroic bosses**, the game's real zone bosses, enter right before every
  50th wave: 49, 99, 149… One more joins every 100 waves. Each drops 3
  Corrupted Chaos rewards, the kind Pain minibosses leave.
- **Pain minibosses** change the rules while they live: every hero takes
  constant damage until they die. Killing them drops **Corrupted Chaos**
  rewards.
- **Monsters from other worlds** join every 5 waves, three types at a time,
  weakest worlds first. From wave 101, a growing share of every wave's
  monsters are **hunters**, until every monster is one from wave 130 on, and
  every wave is a **hunter zone** with falling artillery that grows stronger
  every 10 waves, except the environment waves (106, 116 …), where an
  environment takes its place.
- **Mirage skins** appear from wave 30, weakest first, a stronger one every
  10 waves. From wave 130 their chance grows until every monster wears one
  from wave 149.
- **Loot is scarce but improves.** Monster item drops are off by default;
  the Reminiscence of Luck gives one free pick per player each wave instead.
  Turned on, monsters drop at most one memory or essence per player per
  wave, by chance. Minibosses roll their own chance.
  Rare, epic and legendary drops get more likely every wave, chaos rewards
  included, reaching no commons at all by wave 100 (wave 50 for chaos).
- **No goal.** The run's "Shape of Dreams" goal is removed, since there is
  nowhere to go.
- **Shrines on a ring by the edge of the room**, each one optional and on
  by default: a **Dream Well** and an **Altar of Cleansing** (both
  unlimited), a **Shrine of Ascension** (opens after wave 1, four uses per player,
  given back after every wave), a **Maw of Doom** (fixed 8% risk), a **Shrine of Destiny** (trade
  essence quality for new essences, 2 claims per player each wave), a
  **Hatred Remnant** (a curse for a reward, once per player each wave) and a
  **Reminiscence of Luck** (a free memory or essence, once per player each
  wave, with gold prays for more choices). The Altar, the Ascension, the
  Maw and Luck's prays cost gold, and every price but the Altar's goes up
  5% every wave, compounding, until wave 100 (`Free gold shrines` makes them free, off
  by default). Kill gold is vanilla.
- **The lobby stays vanilla.** Lucid dreams, Corrupted Chaos and every other
  game modifier can be picked as normal, unlike Dream Arena, which strips them.
- **Host only.** Friends join with an unmodded game.

The run ends the way a normal run does, when the whole party is down. The score
is the wave you reached.

## Install

Only the host needs the mod. Friends join with an unmodded game.

- **Steam Workshop:** subscribe to Last Stand, then enable it in the game's
  mod manager.
- **By hand:** put this folder in `<Shape of Dreams>/Mods/LastStand`.

## Play

- **Solo:** Singleplayer, then **Last Stand** under Limbo.
- **Co-op:** Create Lobby, then pick **Last Stand** in the Game Mode dropdown.
  It's selected by default.

Take the rift out of camp to start. Change settings with the **Last Stand**
button in the lobby's settings panel, or in the mod manager.

## Docs

The design docs are in [`docs/`](docs/README.md), one topic per file, with a
table of contents.

## Build

Put this folder in `<Shape of Dreams>/Mods/LastStand` so the project finds the
game's assemblies, then run `dotnet build` there (Debug, which
`about/metadata.json` loads) or `dotnet build -c Release`.

For the Workshop, build Release and run `./pack.sh`. It writes
`dist/LastStand/` with only `LastStand.dll` and `about/`, ready to upload
from the game's mod manager. Keep `about/publishedfileid.txt` once the first
upload creates it: it's how later uploads update the same item.
