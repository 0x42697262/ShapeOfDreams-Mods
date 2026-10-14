# Wave vote

After a wave is cleared, the game's own vote window (the one a travel vote
uses) opens for the break. Each player presses the vote key (`Tab` by
default, `Select` on a gamepad) to mark themselves ready. Once every live
player is ready, the next wave starts at once. Otherwise it starts when the
break ends, as before. It's always on; there's no setting.

- **Who counts.** Every human player with a live hero
  (`PlayerRoster.FillPlayersWithLiveHeroes`). Knocked-out and dead heroes
  don't block it, and AI allies aren't asked. Solo, one press starts the
  wave. A player who joins during a break doesn't see the window (RPCs
  aren't replayed) but counts, and can still vote with the key.
- **When it opens.** In a break on a timer after a cleared wave, with more
  than 5 s left (`Tuning.WaveVoteMinSeconds`), so it doesn't flash up for
  the last seconds or the 3 s after a curse. Not before wave 1 (the first
  wave's 10 s arrival delay has no vote), and not while
  [Curse to start](curse-to-start.md) holds the wave.
- **One vote per break.** Any player with a live hero can cancel it for
  everyone (the cancel key, `Escape` by default). The game's cancel runs:
  chat says the player canceled the travel vote and the window closes on
  every screen. Last Stand then posts `Vote canceled. Wave N arrives in S
  s.` The wave starts on its timer, and the vote comes back after the next
  wave. If several players press it, the first cancel wins: the cancel key
  only works while a vote is open, so the others do nothing.
- **It closes** when the wave starts, by vote or by timer, and when the
  fight stops (camp, game over, mod unload).

Chat: the cleared-wave line is unchanged (`Wave N cleared. The next wave
arrives in S s.`), and a passed vote says `Everyone is ready. Wave N starts now.` Log:
`wave vote: open for wave N, S s left; …`, `wave vote: every live player
voted; wave N starts now.`, or `wave vote: canceled by a player; wave N starts
on its timer in S s.`

## How it works

The vote is the game's, so vanilla clients see the window and can vote. It
lives on `ZoneManager`: `isVoting`, `voteType`, `voteData` and
`voteRemainingSeconds` are SyncVars, and each player's `DewPlayer.isReady`
is one too. The vote key sends `DewPlayer.CmdSetIsReady` while `isVoting` is
true, and the cancel key `ZoneManager.CmdCancelVote`. Clients show the
window on the `RpcInvokeVoteStarted` RPC and hide it on
`RpcInvokeVoteCompleted` or `RpcInvokeVoteCanceled`.

Last Stand doesn't start the vote with the game's `StartVote_Imp`, for two
reasons:

- Its coroutine counts down from 6 s and then completes the vote through
  `WaitAndCompleteVote`, and every ready press (`UpdateVoteStatus`) does
  the same once all are ready. That runs `GetCannotTravelReason` first,
  which waits, showing "can't travel" errors every 4 s, while any player
  holds an item, talks to a shrine or reads an artifact story, or a dropped
  memory or essence lies within reach of the rift. Between waves that's
  common.
- It marks the player who started it as ready.

So `WaveVote` (a game adapter) sets the SyncVars itself: every player's
`isReady` to false, `voteType` to `None`, `voteData` to -1,
`voteRemainingSeconds` to the seconds left in the break, `isVoting` to true,
and sends `RpcInvokeVoteStarted` with the host's player as the starter
(`DewPlayer.local`, whatever the player count; the window's first line names
them, but they aren't marked ready) (the private
setters and RPCs are reached with `AccessTools`). Each tick it updates
`voteRemainingSeconds`, so the window's "traveling in N seconds" counts down
with the break, and checks whether every live player is ready. A prefix on
`ZoneManager.UpdateVoteStatus` (`WaveVotePatch`) skips the game's check
while Last Stand's vote is open, so `WaitAndCompleteVote` never runs for it.
To close, it sets `isVoting` to false and `voteData` to -1 and sends
`RpcInvokeVoteCompleted`. A cancel goes through the game's own
`CmdCancelVote`, which clears `isVoting`; `WaveVote` sees that on its next
tick.

The window's text is the game's travel-vote text ("<host> started a travel
vote", "traveling in N seconds"), since vanilla clients can only show what
the game has. With `voteType` `None`, none of the next-node, next-zone or
sidetrack panels show.

If any step throws, the error is logged (at most once every 5 s), the vote
is given up for that break, and the wave starts on its timer.
