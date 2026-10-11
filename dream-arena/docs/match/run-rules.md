# Run rules

`MatchRules` sets the run-wide rules when the session starts and whenever
settings change, and `MatchRules.Restore` puts the stock values back when the
session stops (only while the server is still running; see the note in
[Networking](../reference/networking.md)).

## Never end the run

`GameManager.WrapUpAndShowResult` is the single funnel every run-ending path
goes through (`CheckGameOver`, `ConcludePureWhiteDream`,
`ConcludeUnknownFate`). `RunEndPatch` refuses it while `Never end the run`
is on. `MatchRules` also clears the synced `isGameOverEnabled`, which stops
the "everyone knocked out" countdown from starting. Both are applied.

There's no win condition on purpose: the mode holds the game open and the
players decide when a round is over (see [Restart](restart.md)).

## No monsters

`GameManager.maxAndSpawnedPopulationMultiplier` scales both
`maxSpawnedPopulation` and `GetAdjustedMonsterSpawnPopulation`, so setting it
to 0 empties every room's waves. It's also what keeps the boss out of the
boss room. Props, the boss's room hazards and anything else that still
appears are handled in [Arena](../arena/arena.md#removing-pve).

## Lucid dreams

Lucid dreams are a PvE progression layer. `GameManager.GetLucidDreams()` is
the only place they become actors, so anything left in `activeLucidDreams` is
in effect; hiding them wouldn't do. With `No lucid dreams` on:

- `LucidDreamAddPatch` refuses `GameSettingsManager.AddLucidDream`, whatever
  offered it.
- `LucidDreamListPatch` empties `availableLucidDreams` each time
  `UpdateAvailableLucidDreams` rebuilds it (on every roster change), so none
  can be picked in the lobby.
- `MatchRules` clears any already active.

**Own settings slot.** The mode flag is the lobby's settings save key,
`dreamarena_pvp` (`ModIdentity.SaveKey`). `PlayLobbyManager.StartGame_Imp`
writes the lobby's settings (difficulty, banned items, lucid dreams) back to
the active slot. Sharing the Shape of Dreams slot would let one PvP match
overwrite the host's normal lucid dream picks with an empty list. Limbo does
the same with its own `"limbo"` key.

## Game modifiers

`GameSettingsManager.OnStartServer` doesn't only copy the lobby's choices: it
adds every `GameModifierBase` in the game that isn't excluded from the pool.
So every run carries Obliviax, Mirage Skin, Golden Lizard and Corrupted Chaos,
which keep producing entities in a supposedly empty arena. With `No game
modifiers` on, `GameModifierPatch` clears `addedGameMods` in a postfix so the
actors are never created, and the arena sweep removes any `GameModifierBase`
created before it.

## The run goal

With `No run goal` on, `RunGoal` (in the shared library) destroys the
`Quest_ShapeOfDreams` that `PlayGameManager.OnStartClient` starts for new
runs. It checks once a second until it finds it. Quests are actors, so the
destruction syncs and the goal disappears for every player.
`GameMod_Limbo` does exactly this, which is the precedent that it's safe.

The old build only hid the goal with a postfix on `DewQuest.IsVisibleLocally`,
which runs on each client, so only the host lost it.

## The travel vote

Taking the camp rift is a team decision, like `-restart`, so Dream Arena
keeps the game's travel vote: `CmdTravelToNode` calls `StartVoteNextNode`,
which opens the world map for everyone with a 6 s countdown (the map shows
only the camp and the arena, see [Camp](../camp/camp.md)). The requester
counts as agreeing, Tab agrees, Esc cancels. The command runs on the host,
and its only test is `ZoneManager.ShouldVoteOnTravel()`: a vote when the
lobby's `Enable votes` is on and two or more heroes stand, otherwise
straight travel. The library's `TravelVotePatch` (a prefix on it) asks the
mod through its `SkipTravelVote` override; Dream Arena answers no, so the
game's rule applies, and `-restart` uses the same rule. Last Stand answers
yes and never votes. The synced `GameSettingsManager.enableVotes` is left
alone on purpose: the lobby saves it as the host's preference, so changing
it would carry into normal runs.
