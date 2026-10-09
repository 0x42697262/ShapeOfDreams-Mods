# Picking the mode

Last Stand is a game mode next to Shape of Dreams and Limbo, in both places the
game offers one:

- **Solo:** the Singleplayer button opens the game mode list (Shape of Dreams, Limbo). A
  **Last Stand** button is added under Limbo. It is a clone of the Limbo button,
  so it looks and navigates the same, with its click replaced to start a solo
  run in Last Stand. If a build ships without Limbo, the game normally skips
  that list and starts Shape of Dreams at once. In that case the list is shown
  anyway, with Limbo hidden, so Last Stand can still be picked.
- **Multiplayer:** Create Lobby has a Game Mode dropdown. **Last Stand** is
  added as a third option, with its own description, and is selected every
  time the window opens. Picking it (on open or later) fills the lobby name
  with `Last Stand (Wave Mode) Z<starting world>`, such as `Z4`. The host can
  rename it or pick another mode before creating; the name is only reset the
  next time the window opens or Last Stand is picked again. The name goes into
  the game's preferred settings under the `laststand` key, so the vanilla lobby
  name stays as it was.

## How the choice is remembered

Choosing Last Stand sets the run's `customGameSettingsSaveKey` to
`laststand`, the same mechanism Limbo uses with `limbo`. That one value is the
mode's **single source of truth**:

- `GameSettingsManager` copies it into `gameSettingsSaveKey`, a SyncVar, when
  the session starts. The mod asks that field whether Last Stand is on. No
  separate "enabled" flag exists to drift out of sync.
- `DewNetworkManager.RestartAsync` copies it back into the start settings, so
  a restart after game over stays in Last Stand.
- Lobby preferences (difficulty, lucid dreams, banned items) are saved per key,
  so Last Stand's lobby setup doesn't overwrite a normal run's.
