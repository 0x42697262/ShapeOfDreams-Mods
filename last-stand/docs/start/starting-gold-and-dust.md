# Starting gold and dream dust

Every player gets `Starting gold` (100) and `Starting dream dust` once, when
a new Last Stand run starts, on top of the game's own start gold. The dream
dust is `Starting dream dust` (10) × 2^(`Starting world`), so 20 in world 1,
80 in world 3 and 320 in world 5. It's worked out from the setting rather than the live zone index, because
the grant can land before the starting world is applied in camp, and it's
capped at `int.MaxValue`. Dream dust goes through `DewPlayer.EarnDreamDust`.
The host grants it with
`DewPlayer.EarnGold`, the same call the game uses for its own start gold, as
soon as the game is ready (`GameManager.IsLazyCallReady`), in camp. It's
skipped for a continued run (`startSettings.continueData`), whose gold comes
from the save. The run it went to is kept in the AppDomain (the
`GameManager`'s instance id), so a mod reload doesn't grant it again, and a
restart, which makes a new `GameManager`, does. A player who joins mid-run
gets the game's catch-up instead (the average of everyone's gold and items).
Log: `starting gold: granted 100 to N player(s)` and `starting dream dust:
granted 20 to N player(s)`. Either is skipped at 0.
