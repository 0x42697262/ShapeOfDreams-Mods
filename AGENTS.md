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

Each mod still builds inside its own `Mods/<Name>` mount in the `chen`
container (incus devices `laststand` and `dreamarena`), so point the device
at the mod's folder in this repo (or at its `dist/` pack for an upload).
