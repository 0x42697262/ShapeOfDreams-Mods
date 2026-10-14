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
| Progress (kill icon) | `Wave 12: 34 monsters left` during a wave, `Wave 13` before it |

Between waves, and before wave 1, it shows the next wave's number, not a
countdown: the break's countdown is in the [wave vote](../waves/wave-vote.md)
window.
With [Curse to start](../waves/curse-to-start.md) holding the wave, it reads
`Wave N: choose a curse`. The countdown (`Wave 13 in 5s`) was removed in
v0.14.0.

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

Three things used to send a run back to wave 1, and all are fixed:

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

- **Quitting and continuing.** Leaving a run for the lobby or the main
  menu and continuing it later ("resume the dream") used to restart at
  wave 1, with the fast-kill raise gone: the progress lived only in memory,
  keyed by the `GameManager`'s instance ID, and a continued run gets a new
  `GameManager`. The key is now `GameManager.runId`, a GUID the game makes
  once per run and keeps in its save (`[SaveVar]`), and the progress is
  also written into the game's continue save.

  **The continue save is an end-of-wave checkpoint.** A first version let
  the game save whenever it wanted, which made quitting an exploit: quit
  during a heroic boss wave and continue, and the boss came back next to
  the replayed wave's new one; gold and dream dust picked up during the
  wave were kept and earned again. Now:

  - When a wave is cleared (after its rewards drop and souls revive), the
    session asks the game for a save: `GameManager.SaveContinueDataMidRun`,
    the same call the game makes when the host quits.
    `ContinueCheckpoint` retries once a second through the break until the
    game allows it (`IsEligibleForMidRunSave`: the room's `didClearRoom`,
    which an environment holds open until it ends). Log: `continue save:
    checkpoint after wave N saved.`
  - While a wave is being fought, a prefix on `SaveContinueDataMidRun`
    (`ContinueSaveGatePatch`) skips every save the game tries: on quitting,
    on a boss kill (`BossMonster` saves when the last boss dies), on
    `LockMidRunSave`. Log: `continue save: the game's save during wave N
    was skipped; the last checkpoint stays.` So quitting mid-wave leaves the
    checkpoint from the end of the last wave in place, with everyone's
    items, levels, gold and dream dust as they were then. Monsters aren't
    in the save, so the wave replays in full.
  - Quitting during a break saves as the game normally does, so what was
    bought or upgraded in the break is kept.

  The only thing left to repeat is a wave itself, which the game allows
  too.

  **How the progress rides along.** Every continue save goes through
  `DewPersistence.SerializeGameData`. A postfix there (`ContinueSaveWritePatch`,
  host in the mode only) adds one entry to its `managerPersistentData`:
  key `LastStand`, field `progress`, the same string as in memory (run ID,
  arena node, wave, in progress or not, fast-kill raise). The game only
  reads entries named after its own managers, so it ignores ours, and the
  save stays loadable without the mod. On continue, `PlayGameManager`
  passes the save to `DewPersistence.ApplyGameData`; a prefix there
  (`ContinueSaveReadPatch`) puts the entry back in memory under the key
  `continued` instead of its saved run key, since the game is loading that
  very save and `runId` isn't restored yet at that point. The first arrival
  in the arena takes a `continued` entry whatever the run, and stores it
  again under the run's `runId`, so later mod reloads find it as usual.
  When a game starts without continue data
  (`DewNetworkManager.startSettings.continueData` is null), an unclaimed
  `continued` entry is dropped, so a new run can't pick it up. Keying by the
  saved run would break on saves written before the key changed: in
  testing, a save keyed `instance:seed` resumed wave 31 into memory, the
  arrival didn't match it against `runId`, and the run started at wave 1. A save without the entry clears what's in memory, so a stale
  mid-wave state can't be resumed. The read doesn't check the game mode,
  since the lobby settings may not be restored yet, but it only reads Last
  Stand's own entry. A wave saved as in progress (it shouldn't happen any
  more) is restored as the wave before it, cleared.

  The difficulty comes back with it: the monster world is the run's world
  (saved by the game), plus the wave's scheduled rise, plus the saved
  fast-kill raise. Log on continuing: `continue save: resuming after wave
  N.` A save from before this fix logs `no Last Stand progress in this
  save.` and starts at wave 1.

The crash behind the reset at wave 10 came from the game's
`RoomMonsters.SpawnMiniBoss`, which always calls `SelectMiniBoss` first. That
picks from the *room's* own monster pool and throws when the pool has no
miniboss-capable monster, as some boss rooms don't, even though Last Stand
passes its own pick. A prefix on `SelectMiniBoss` hands back Last Stand's pick
while one is pending, so the room's pool is never read.
