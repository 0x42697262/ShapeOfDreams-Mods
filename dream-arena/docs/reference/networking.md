# Networking

Everything runs on the host, so **only the host installs the mod**. Clients
run the unmodded game and see only synced state and chat.

The host's lobby shows the MOD icon and sets `isAlteringGameplay`, as the
official rules require. A modded lobby turns off cross-play, so only Steam
players can join; that's the game's own rule and isn't bypassed.

## What reaches vanilla clients

| Feature | Carried by |
|---|---|
| PvP relations, nameplate colors | `DewPlayer.enemies` / `allies` SyncLists |
| The arena room | `ZoneManager.SetRoomOverride` writes the synced `nodes` list |
| Room modifiers removed | `ZoneManager.RemoveModifier` writes the same list |
| Cleared, lit arena | `Room.isRevisit`, set before the room is sent to clients |
| No game over | server-only decision; `isGameOverEnabled` is a SyncVar |
| Direct camp rift | `Rift_RoomExit.nextNodeIndex` SyncVar |
| Remnant offers | `ChoiceShrine.choices` SyncDictionary, per player |
| Remnants usable again | synced use counts and `_lastUseTime` rolled back per pick |
| Health multiplier, levels, memory levels | `finalStats` and level SyncVars, computed on the server |
| Revives, no rescue quest | server-side status effects |
| No run goal | the goal quest destroyed (actors sync their destruction) |
| Phase, restart and settings notices | raw chat messages, which vanilla clients render as-is |
| The restart vote | the game's own travel vote |

## What can't reach them, and was removed

Anything a client draws for itself can't be changed by the host. Three 0.1.0
features did exactly that, so only the host saw them, a mismatch between
what the host and everyone else saw:

| 0.1.0 | Why it was host-only | 0.2.0 |
|---|---|---|
| "PVP: Free For All" in the top-right location label | `UI_InGame_Location` builds its text on each client, on room load | chat announcements on crossing the rift (`Announce in chat`) |
| Hidden run goal | the postfix on `DewQuest.IsVisibleLocally` runs on each client | the goal quest is destroyed for everyone (`No run goal`) |
| PvP-themed rift prompt | the `DewLocalization.GetUIValue` intercept runs on each client | removed; the stock prompt shows |

Rule for new features: before relying on a field, check that it's a
SyncVar, SyncList or SyncDictionary, or that the effect is a server-side
actor. Setting a plain field on the host does nothing for clients (the
remnants' `maxUseCountPerPlayer` is the example in this mod).

## A client who has the mod too

The mode is decided by the host. `ArenaMode.IsHosting()` is true only with
`NetworkServer.active` and the lobby's settings save key equal to
`dreamarena_pvp`; every patch and the whole session check it (or
`NetworkServer.active`) first. A client with the mod loaded therefore runs
nothing: no relations, sweeps, remnants or chat. Their lobby panel has no
Dream Arena button, `arena` refuses, and their settings changes only reach
the log. Different mod versions between host and client don't matter for the
same reason.

If the host doesn't run the mod, nobody gets the mode, even clients who have
it: the game-mode option only exists on the host's Create Lobby screen.

## Shutting down cleanly

Game over, a restart into a new game, leaving the lobby and unloading the mod
all stop the session. Writing a SyncList or SyncVar after the transport has
shut down throws `InitSyncObject: IsWritable: neither NetworkServer nor
NetworkClient are active` (seen in Last Stand). So `DreamArenaSession.Stop`
clears relations and restores rules only while `NetworkServer.active`, and
always resets its own state in a `finally`. The training dummy is likewise
only destroyed while the server runs.

## Host performance

The host is the server, so a slow host frame stalls every client and can
trip EOS peer-inactivity kicks during joins:

- Per-frame code reads managers through `NetworkedManagerBase<T>.softInstance`,
  never `.instance`. When a manager is missing, `.instance` runs two
  whole-scene `FindObjectOfType` searches; `ZoneManager` and `ActorManager`
  don't exist in the lobby, so `.instance` there meant full scene scans every
  frame. That was the 0.1.0 host-lag fix and stays the rule.
- Everything that waits for a room retries on a timer with a cap: remnants
  once a second (5 tries), staging every 0.25 s, the PvE sweep and the camp
  rift once a second, hero checks every 0.5 s, the dummy every 0.2 s.
- A failing session tick is caught and logged at most every 5 s
  (`ErrorThrottle`), never thrown into the game loop.

## Lobby and transport facts

Since the Sep 22 game patch (still labeled `r.1.4.0.13`):

- `DewMod.isAnyModActive` forces the host's cross-play off and publishes the
  EOS lobby with `crossPlayGate = "STEAM"`, so other platforms get
  `CrossPlayMismatch`.
- A host with Steam running uses `MultiplexTransport` (Steam and EOS).
  Clients connect over Steam, then fall back to EOS.
- The lobby `version` check compares only the part before `_`, so a friend
  whose game hasn't finished updating fails it.
