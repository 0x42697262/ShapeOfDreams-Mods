# Restart

Any player can type `-restart` in chat during a fight (`Chat -restart`, on by
default). That calls a vote. If it passes, everyone goes back to camp and
starts over. The host's `arena_restart` console command skips the vote.

## Chat

Every chat line, the host's included, reaches the server through
`ChatManager.CmdSendChatMessage`. `ChatRestartPatch` is a postfix on its body
(`UserCode_CmdSendChatMessage__String__NetworkConnectionToClient`) that
matches `-restart`, so vanilla clients only have to type it. The line still
shows in chat. A refusal (typed in camp, during another vote, while
traveling, or with the setting off) is sent only to the player who asked,
through `ChatNotices.Tell`, as a raw red message.

## The vote

`RestartVote` reuses the game's own travel vote, so vanilla clients get the
usual panel, countdown and keys. The requester counts as agreeing. Tab agrees,
any player pressing Esc cancels, everyone agreeing passes it early, and the
countdown running out passes it. A lone standing hero restarts at once.

It's started as a next-zone vote (`StartVoteNextZone`), because the next-node
panel draws the mini world map with every unused node; the next-zone panel
has none. While a restart vote is pending, three patches read the session's
flag:

- A passed next-zone vote calls `GameManager.LoadNextZone`.
  `RestartVotePassedPatch` runs the restart instead and skips the zone
  change. `PlayGameManager` overrides that method, so every `GameManager`
  type that declares it is patched.
- `ZoneManager.ClearVoteState` runs on cancel and after completion, and
  clears the flag.
- `ZoneManager.GetCannotTravelReason` is waived. It holds a vote until nobody
  carries an item and nothing lies on the floor, both of which the restart
  clears anyway.

## What a restart does

`MatchRestart.Execute`, in order:

1. Revives every knocked-out hero at full health (room travel doesn't),
   through the shared library's `HeroRevival.Revive`.
2. `BuildWipe.Wipe` destroys every equipped essence and the Q, W, E and R
   memories, plus whatever a hero holds in hand. Identity and movement can't
   be removed, so only their essences go.
3. `BuildWipe.SweepLooseItems` destroys every unowned memory and essence on
   the floor.
4. `BuildWipe.RestoreLoadout` recreates the starting Q and R memories from
   each hero's loadout, the way the game equips them at the start of a run.
   `HeroBoost` raises them to the memory floor afterward.
5. The camp remnants are restocked (taken items cleared, ring rebuilt).
6. Every `ZoneManager.visitedNodesSaveData` entry is cleared. Leaving a room
   saves its state and revisiting restores it, so without this the camp
   would come back with its old, half-picked remnants. Loose items are
   destroyed rather than dropped for the same reason: the arena is saved
   again during the trip.
7. `ZoneManager.TravelToNode(camp, advanceTurn: false, ignoreInterrupts:
   true)`. Unlike the client's `CmdTravelToNode`, the server call doesn't
   need the nodes to be connected; the arena has no link back to camp.

Chat says `[Dream Arena] Restarting. Back to camp: builds wiped, remnants
restocked.`, and the log `restart: by <guid>; wiped N build(s) ...`.
