# Code layout

| Folder | Concern |
|---|---|
| `src/LastStandMod.cs` | Entry point. Wires the session, Harmony and lifecycle |
| `src/LastStandConsole.cs` | Console commands |
| `src/Mode` | Whether Last Stand is selected, and selecting it |
| `src/UI` | The solo button, the lobby dropdown option, the settings window and its lobby button, the dropdown and checkbox fields, UI cleanup |
| `src/Session` | The run's phases (camp, arriving, fighting) and arrival |
| `src/Arena` | Room override, modifier gate, camp rift, cleared-room prep, arena layout, staging heroes |
| `src/Waves` | Wave schedule, wave cycle and spawn queue (plain logic), and the director |
| `src/Combat` | Monster catalog, spawning, heroic bosses, per-wave enhancements, hunter zones, wave environments, tracking enemies, Pain |
| `src/Loot` | Loot rules, rarity tables and ledger (plain logic), drops |
| `src/Well` | The Dream Well and Altar of Cleansing (kept at unlimited uses), the Shrine of Ascension (opens after wave 10, uses back every 10th wave), the Maw of Doom at a fixed use count, the Shrine of Destiny (2 claims per player each wave), and the Hatred Remnant (once per player each wave) |
| `src/Game` | Thin wrappers over game APIs, and game identifiers |
| `src/Config` | The mod config, its validated snapshot, and internal tuning |
| `src/Text` | Every player-facing, config, log and command string |
| `src/Patches` | Harmony hooks. Each one only hands off to the code above |
