# PvP

## How players become enemies

`DewPlayer` carries three synced lists that the shipped game builds but never
fills:

```csharp
public readonly SyncList<DewPlayer> allies   = new SyncList<DewPlayer>();
public readonly SyncList<DewPlayer> enemies  = new SyncList<DewPlayer>();
public readonly SyncList<DewPlayer> neutrals = new SyncList<DewPlayer>();
```

`DewPlayer.GetTeamRelation` reads them before its last rule ("both are human
players, so Ally"), so an entry wins:

```csharp
if (allies.Contains(other))   return TeamRelation.Ally;
if (enemies.Contains(other))  return TeamRelation.Enemy;
...
if (isHumanPlayer && other.isHumanPlayer) return TeamRelation.Ally;
```

Three things make this work with no mod on the clients:

1. **The lists are server-authoritative.** The host writes, Mirror replicates.
2. **All damage goes through relations.** `AbilityTargetValidator` defaults to
   `Neutral | Enemy`, so a hostile player is a legal target for every damaging
   ability. There is no separate friendly-fire flag.
3. **The UI already reacts.** `TeamRelationChanged` is registered as a list
   callback and refreshes health bars, so clients recolor nameplates
   themselves.

Relations are directional (`a.GetTeamRelation(b)` reads only `a`'s lists), so
`Relations` writes every pair both ways.

## Team modes

`TeamRule` is plain logic, no Unity or networking:

| Mode | Pairs |
|---|---|
| Free For All | everyone is an enemy of everyone |
| Two Teams | even join index vs odd join index; same-team pairs are written as allies |
| Peaceful | nothing is written, so the game's own "humans are allies" rule applies |

Join order is the order of `DewPlayer.allHumanPlayers`. There's no team
picker yet.

`Relations.Apply` first clears every human pair's `enemies`, `allies` and
`neutrals`, then writes the new pairs, so switching modes never leaves a stale
entry. `Relations.ClearAll` is used when the session stops.

## Camp and arena

A run opens in camp and PvP starts past the rift. World generation always
makes one `WorldNodeType.Start` node (the camp); every other node becomes the
arena (see [Arena](../arena/arena.md)). `ArenaLocator.CurrentPhase` reads the
synced `ZoneManager.currentNodeIndex`:

| Where | Relations |
|---|---|
| Camp | Peaceful, whatever the team mode |
| Anywhere else | the configured team mode |

`PhaseWatch` polls the node index and the number of human players. When
either changes, it applies the matching relations (so a player who joins
mid-match lands on the right side), and logs `phase: Arena (FreeForAll).`.
Polling a single synced int avoids depending on the order of the travel
pipeline. A settings change applies the relations again at once.

## Chat announcements

With `Announce in chat` on (default), crossing the line is announced to
everyone as a raw chat message, which vanilla clients render as-is:

- into the arena: `[Dream Arena] The arena is live. Free for all: everyone is
  your enemy.` (red), the two-teams or peaceful version for those modes;
- back to camp: `[Dream Arena] Back in camp. Everyone is friendly here.`
  (yellow).

This replaces the old HUD label ("PVP: Free For All" in the top-right
location text), which only the host could see: `UI_InGame_Location` builds
its text on each client, so a host-side rewrite never reaches anyone else
(see [Networking](../reference/networking.md)). Chat reaches everyone.
