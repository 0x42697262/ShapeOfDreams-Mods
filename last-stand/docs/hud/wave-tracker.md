# Wave tracker

Waves are announced in chat as plain-text `MessageType.Raw` notices, which
vanilla clients render as-is: the wave number and what it brings when it
starts, and when the next one comes once it's cleared.

The run's own goal, **"Goal: Shape of Dreams — Move deeper into …"**, makes
no sense here, so the server destroys `Quest_ShapeOfDreams` as soon as it
exists (checked once a second). That is what the game itself does when it
starts a Limbo run (`GameMod_Limbo`). Quests are networked actors, so the goal
disappears for vanilla clients too.

**Quest tracker (every player).** While fighting in the arena, the quest
tracker on the right shows the wave to everyone, vanilla clients included:

| Line | Text |
|---|---|
| Title | Threat Level Rising |
| Description | Powerful Monsters |
| Progress (kill icon) | `Wave 12: 34 monsters left` or `Wave 13 in 5s` |

It works because `DewQuest.currentProgress` is a synced string that every
client's tracker prints as-is, next to the icon for the synced `progressType`
(set to `Kills`). The title and description aren't synced: each client looks
them up from a localization key, so they can only be lines the game already
has. The host sends the keys with the quest's `SetLocalizedTitle` and
`SetLocalizedDescription` RPCs (`GameNames.TrackerTitleKey` and
`TrackerDescriptionKey`), and they're translated on each client. RPCs aren't
replayed to players who join later, so the host sends them again whenever the
number of ready connections changes.

The quest itself is a `Quest_Limbo`: an empty quest class with no logic of
its own, only started by Limbo mode, which never runs alongside Last Stand.
The progress is updated at most once a second (`Tuning.TrackerRefreshSeconds`)
and only when its text changes, since each update replays the tracker's
flash on every client. The quest is removed in camp, on game over and on mod
unload, and after a mod reload or a continued save the existing one is taken
over instead of starting a second. It can be turned off with `Show wave in
quest tracker`.

## The wave count never resets

Two things used to send a run back to wave 1, and both are fixed:

- **Errors.** Any exception in the session tick used to stop the whole
  session, and the next tick started over at wave 1. Now a failed tick is
  logged (at most once every 5 s) and the session carries on with its state.
  A failed spawn skips just that monster.
- **Mod reloads.** With developer mode's auto-reload, any file change in the
  mod folder reloads the mod mid-run, and a fresh session started at wave 1.
  Now the session saves the run (a key from the `GameManager` instance and the
  world seed), the arena node, the wave and whether it's being fought, every
  time a wave starts or is cleared. It saves to
  `AppDomain.CurrentDomain.SetData`, which outlives the old assembly because
  Mono never unloads it. On arrival, a matching save resumes the run. If the
  wave was being fought, the monsters already on the field (creep-owned
  `Monster`s not spawned by another monster) are taken over by tier, and the
  wave ends when they're dead, with no new spawns. Otherwise the next wave
  follows the usual break. Pain minibosses come back as plain minibosses,
  because their Pain state lived in the old session. The Dream Well, the Altar
  of Cleansing, the Shrine of Ascension, the Maw of Doom, the Shrine of
  Destiny and the Hatred Remnant already standing are reused rather than
  doubled.

The crash behind the reset at wave 10 came from the game's
`RoomMonsters.SpawnMiniBoss`, which always calls `SelectMiniBoss` first. That
picks from the *room's* own monster pool and throws when the pool has no
miniboss-capable monster, as some boss rooms don't, even though Last Stand
passes its own pick. A prefix on `SelectMiniBoss` hands back Last Stand's pick
while one is pending, so the room's pool is never read.
