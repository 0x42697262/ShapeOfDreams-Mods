# Settings

The host can change them in two places: the **Last Stand** button in the
lobby's settings panel (or the `laststand_config` console command), and the
mod manager's config screen. The lobby window edits a copy, and **Apply**
saves it and applies it live; **Cancel** or Escape drops the edits. The
button shows only for the host (`NetworkServer.active`, checked each time the
panel opens): a client who also has the mod loaded doesn't get it, since its
settings would only edit their own file and never reach the host's run. For
the same reason `laststand_config` refuses on a connected client and logs
`only the host's settings apply`. The mod manager's own config screen
belongs to the game and can't be hidden; it only edits that player's file.

**Change notices.** Every applied change is announced in chat, so players
in the lobby (or mid-run) see what the host changed:
`[Last Stand] Host changed Boss music: on -> off`. Both places end in
`ModBehaviour.OnConfigChanged()` after the new values are in `config` (the
lobby window through `LastStandMod.ApplyConfig`, the mod manager after
copying its working copy back), so `SettingsAnnouncer` keeps a clone of the
last applied config (taken in `LoadConfigsToDisk`) and diffs it there:
- `SettingChange.Between` compares every field the settings panel shows
  (`ConfigFields.Visible`: public or `[SerializeField]`, not
  `[HideInInspector]`, so `schemaVersion` never shows).
- `SettingChangeText` names the field by its `[LabelText]`, writes bools as
  on/off and floats with up to 3 decimals, and names picked rooms, bosses,
  environments and strengths by their localized game name (or the
  choice's own text). Checkbox lists show counts and what moved:
  `Environment types: 21 -> 20 picked, removed Toxic Area`.
- Each change is logged (`settings: ...`). Chat gets the first 6
  (`Tuning.MaxSettingChangesInChat`), then `...and N more setting(s)
  changed.`, so a Reset to defaults doesn't flood it.
- Chat goes out only while hosting a Last Stand session
  (`LastStandMode.IsHosting()`: server active, Last Stand selected). A
  client changing their own file, or anyone on the title screen, only gets
  the log lines. Notices are raw chat messages, so vanilla clients read
  them too.

**Layout.** Both places show the same panel, laid out like a system settings
app: a sidebar of pages on the left (General, Shrines, Waves, Monsters,
Bosses, Events, Rewards) and the selected page on the right. A page has a
title and a one-line summary, then its settings in grouped cards (for
example Timing, Wave size and Spawning on Waves). Each row has the label
and a short gray description on the left and the control on the right;
checkbox lists sit under their label. The last page opened is remembered
until the game closes.

`LastStandConfig` overrides `ModConfig.BuildWidgets`, which the mod
manager's config screen calls, and builds `SettingsPanel` there; the lobby
window builds the same panel with a fixed height and its own scroll area.
Pages come from a `[Page]` attribute on the first field of each page and
groups from the usual `[Header]`, so fields are declared in page order. The
controls themselves still come from `DewGUI.fieldBuilders`, so the dropdown
and checklist builders below work in both.

**Nothing has to be typed except numbers.** `Arena room` is a dropdown of the
8 boss rooms, and `Heroic boss types` and `Environment types` are lists of
checkboxes. With no boss checked, any boss can come; with no environment
checked, none comes. A saved environment name that isn't in the list is
ignored, so the config file can't add anything else. Both are still saved as
plain asset names, so old config files keep working. A saved name that isn't
in the list shows as an extra dropdown option, or is kept as-is behind the
checkboxes, so opening the window never changes it by itself. This works by
putting two entries at the front of the game's `DewGUI.fieldBuilders` list,
which `CreateWidgetsForObject` asks in order for each field. They claim only
fields marked `[PickOne]` or `[PickMany]`, so both config screens get them and
every other field keeps its stock widget. Labels use the game's own name for a
boss (`<type>_Name`) where it has one, plus the world.

**Saved settings and new versions.** The game saves the config as JSON
(`QuickSave/Mods/com.0x42697262.laststand/LastStandMod.config.json` under the
game's persistent data folder) and loads it over the defaults on every start.
So when a new version changes a default, a saved file would quietly keep the
old value. To stop that, the file carries a hidden `schemaVersion`. Before the
game loads it (`LoadConfigsToDisk`), a file with a missing or older version,
or one that can't be read, is moved to `LastStandMod.config.json.old` and the
game writes fresh defaults. This means a version bump also resets any settings
you changed yourself. Copy them back from the `.old` file if you want them.

| Group | Setting | Default |
|---|---|---|
| Arena | Arena room | Primus (dropdown) |
| | Plain arena (no room modifiers) | on |
| | Boss music (the arena room's own) | on |
| | Show wave in quest tracker | on |
| | Dream Well | on |
| | Altar of Cleansing | on |
| | Shrine of Ascension / opens and uses come back every N waves | on / 1 |
| | Maw of Doom | on |
| | Shrine of Destiny / claims per wave | on / 2 |
| | Hatred Remnant / Hatred Shard dream dust per wave / gold | on / 50 / 500 |
| | Reminiscence of Luck | on |
| | Curse length in waves of kills: Mild / Potent / Powerful | 1 / 1.25 / 1.75 |
| | Free gold shrines | off |
| | Gold price growth / every N waves / stops at wave | 5% / 1 / 100 |
| | Starting gold | 100 |
| | Starting dream dust (× 2^starting world) | 10 |
| | Allow cheat commands | off |
| Waves | First wave delay | 10 s |
| | Break between waves / after a boss wave | 20 s / 40 s |
| | Curse to start each wave / delay ([Curse to start](../waves/curse-to-start.md)) | off / 3 s |
| | Monsters in wave 1 / extra per wave | 6 / 2 |
| | Extra player share | 0.5 |
| | Surge every N waves / monster multiplier | 5 / 2 |
| | Merged wave size (merging starts where one player's count reaches it) / merged surges: extra minibosses / extra Pain minibosses | 100 (wave 48) / 2 / 2 |
| | Merged waves: miniboss health / heroic boss health (× the toughest monster, at least) | 5 / 20 |
| | Max monsters alive / per extra player | 35 / 0 |
| | Min spawn distance from heroes | 8 m |
| | Spawn interval / spawns per batch | 0.5 s / 6 |
| Scaling | Health / damage per wave | 10% / 10% |
| | Steeper scaling from wave / late health / late damage per wave | 150 / 50% / 50% |
| | Fast clear: instant kills / instant within (from the first hit) / stats from next wave (stacking) / from wave | 80% / 0.5 s / ×2 / 30 |
| | Fast clear: heroic bosses too / heroic boss share | on / 50% |
| | Starting monster level / waves per level | 1 / 4 |
| | Starting world (vanilla world scaling) | 1 |
| Minibosses | First wave / every N waves / waves per extra / max | 10 / 10 / 2 / 10 |
| Heroic bosses | First wave / every N waves / waves per extra / max | 49 / 50 / 50 / 10 |
| | More often from wave / every N waves after that | 100 / 25 |
| | Entrance delay | 3 s |
| | Ability haste / more per later boss wave | +200 / +50 |
| | Health / damage multiplier | ×5 / ×8 |
| | Types | 6 zone bosses (checkboxes; none checked means all) |
| | Corrupted Chaos rewards / dust / gold / item multiplier | 3 / 1000 / 200 / 2 |
| | Heroic boss stardust (vanilla boss-kill amount) | on |
| Pain | First wave / every N waves / waves per extra / max | 20 / 20 / 2 / 3 |
| | Damage per second per boss | 1% |
| | Health / damage bonus | 20% / 30% |
| Other worlds | First wave / every N waves / types per step | 10 / 5 / 3 |
| Hunters | First wave / every monster a hunter from wave | 101 / 130 |
| | Waves per hunt level / max hunt level | 10 / 6 |
| Mirage | First wave / waves per new skin / chance | 30 / 10 / 20% |
| | Chance grows from wave / every monster a Mirage from wave | 130 / 149 |
| Dark urge | First wave / chance / curse strength | 176 / 20% / Potent (25% damage between players) |
| Environments | First wave / every N waves / last wave (0 = never stop) | 6 / 10 / 0 |
| | Types | 21 room modifiers (checkboxes; none checked means none) |
| Item drops | Monster item drops | off |
| | Monster items per player per wave | 1 |
| | Guaranteed drop per wave | off |
| | Free leftover items at wave end | on |
| | Common / rare / epic / legendary chance | 50% / 20% / 8% / 2% |
| | Item chances reach target at wave | 100 |
| | Common / rare / epic / legendary at target | 25% / 25% / 25% / 25% |
| | Memory share | 50% |
| | Rarity floor with item multipliers | 25% |
| Enemy rewards | Max kill gold per player (0 = no cap) | 0 |
| | Gold growth per wave | 1% |
| | Item chance multiplier: monster / miniboss / Pain | 1 / 1.5 / 2 |
| | Per-tier dust and gold | see Loot |
| Chaos rarity | Chaos reward: legendary / epic / rare chance | 5% / 15% / 35% |
| | Chaos chances reach target at wave | 50 |
| | Chaos reward: legendary / epic / rare at target | 17.5% / 35.2% / 47.3% |
