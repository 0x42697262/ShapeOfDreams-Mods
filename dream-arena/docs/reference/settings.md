# Settings

The host changes them in two places: the **Dream Arena** button in the
lobby's settings panel (or the `arena` console command), and the mod
manager's config screen. The lobby window edits a copy; **Apply** saves it
and applies it live, **Cancel** or Escape drops the edits. The button shows
only for the host of a Dream Arena lobby (`ModMode.IsHosting()`:
`NetworkServer.active` and the synced `GameSettingsManager.gameSettingsSaveKey`
is Dream Arena's), checked each time the panel opens (the game shows the
panel with `SetActive`, so its `OnEnable` runs every time). A client with
the mod loaded doesn't get it, since their settings would only edit their
own file and never reach the host's run. A Shape of Dreams or Last Stand
lobby doesn't get it either: with both mods loaded, only the selected
mode's button shows. An earlier version checked `NetworkServer.active`
alone, so both buttons appeared in every lobby. For the same
reason `arena` refuses on a connected client. The mod manager's own screen
belongs to the game and can't be hidden; it only edits that player's file.

## Layout

Both places show the same panel (from the shared library, `library/src/UI`,
also used by Last Stand): a sidebar of pages on the left and the selected
page on the right. A page has a title and a
one-line summary, then its settings in grouped cards. Each row has a label
and a short gray description on the left and the control on the right.
`DreamArenaConfig` overrides `ModConfig.BuildWidgets`, which the mod
manager's screen calls, and builds `SettingsPanel` there. Pages come from
`[Page]` on the first field of each page, groups from `[Header]`.

**Names are picked, not typed.** `Arena room` and `Dummy monster` are
dropdowns. Fields marked `[PickOne]` are claimed by an entry put at the
front of the game's public `DewGUI.fieldBuilders` list (`ChoiceFields`,
lists in `ConfigChoices`, with the arena rooms from the library's
`SharedChoices.ArenaRooms`). Labels use the game's own `<type>_Name` text
with the world in brackets. The value is still the plain asset name, and a
saved name that isn't in the list shows as an extra option rather than
being changed.

## Every setting

| Page | Group | Setting | Default |
|---|---|---|---|
| Match | Teams | Team mode | Free For All |
| | | Announce in chat | on |
| | Match flow | Never end the run | on |
| | | Chat -restart | on |
| | | Skip travel vote | on |
| Arena | Room | Arena for every room | on |
| | | Arena room | Primus (the Lone King's Zenith) |
| | | Plain arena | on |
| | | No room modifiers | on |
| | Arrival | Skip intro cutscenes | on |
| Camp | Remnants | Camp remnants | on |
| | | Memory level | 10 |
| | | Essence quality | 400 |
| | Training dummy | Training dummy | on |
| | | Dummy monster | Treant |
| Heroes | Strength | Player level (0 = off, capped at the hero's level cap) | 20 |
| | | Starting memory level | 10 |
| | | Hero health multiplier | 100 |
| | Revive | Revive after (seconds) | 10 |
| | | Revive health (%) | 100 |
| Rules | PvE | No monsters | on |
| | | No game modifiers | on |
| | | No lucid dreams | on |
| | | No rescue quests | on |
| | | No run goal | on |

`SessionSettings.From` validates them once per change (levels at least 1,
health multiplier at least 1, revive health 1 to 100%, empty names back to
the defaults), and every subsystem reads only that snapshot.

There's no on/off switch for the mode itself: picking **PvP Arena** as the
lobby's game mode is the switch (see [Networking](networking.md)).

## Change notices

Every applied change is announced in chat, so the players see what the host
changed: `[Dream Arena] Host changed Team mode: FreeForAll -> TwoTeams`. Both
places end in `ModBehaviour.OnConfigChanged()` with the new values in
`config` (the library's `ModEntry` overrides it), so `SettingsAnnouncer`
keeps a clone of the last applied config (taken in `LoadConfigsToDisk`) and
diffs it there. Bools read on/off, floats up to 3 decimals, rooms and
monsters by their game name. Each change is logged (`settings: ...`); chat
gets the first 6 (`SharedTuning.MaxSettingChangesInChat`), then `...and N
more setting(s) changed.`. Chat goes out only while hosting a PvP Arena
session.

## Config file and schema

Settings persist to
`<persistentDataPath>/QuickSave/Mods/com.0x42697262.dreamarena/DreamArenaMod.config.json`.
The game loads the saved JSON over the defaults, so a changed default would
never reach a player who has the file. `ConfigFile.ResetIfOutdated` (shared
library, called from `ModEntry.LoadConfigsToDisk`) compares the file's
`schemaVersion` with `ConfigSchema.Current` before loading, and if it's
older (or missing) moves it to `.old` and lets the game write fresh
defaults, logging `saved config is from an older version ...`.

Bump `ConfigSchema.Current` whenever a default changes or a field is renamed
or removed. Schema 1 is the 0.2.0 refactor: files from 0.1.0 have no
`schemaVersion` and are reset once.

**Renamed and removed in 0.2.0:**

| 0.1.0 | 0.2.0 |
|---|---|
| `arenaEnabled` | removed; the lobby game mode is the switch |
| `suppressMonsters` | `noMonsters` |
| `suppressKnockoutQuests` | `noRescueQuests` |
| `campShops`, `shopMemoryLevel`, `shopEssenceQuality` | `campRemnants`, `remnantMemoryLevel`, `remnantEssenceQuality` |
| `hideObjectives` | `removeRunGoal` (now removed for everyone) |
| `showModeInHud` | removed; replaced by `announcePhases` (chat) |
| `spreadSpawns` | removed; the arrival ring is always on |
| `removeBoss` | removed; it was never read |
