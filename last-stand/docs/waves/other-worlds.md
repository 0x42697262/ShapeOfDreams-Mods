# Monsters from other worlds

The pool starts as the run's current zone (`Zone.defaultMonsters`). From wave
10 and every 5 waves after, three more monster types from other zones join. The other zones are the
game's own list (`Zone_` assets, filtered by the build's content settings, as
`ZoneManager` does) ordered by `zoneTier`, so weaker worlds come first. Each new
type is a fair share of the pool: with 8 local types and 2 new ones, a regular
spawn is a new type 2 times in 10. Minibosses can be any unlocked type that
the game marks `ISpawnableAsMiniBoss`.
