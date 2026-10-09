# Dream Well

- A single `Shrine_UpgradeWell` by the edge of the room, with the other
  shrines ([Edge shrines](edge-shrines.md)), spawned by the host.
- **Unlimited uses** at the well's normal dream dust cost.
- `maxTotalUseCount` is a SyncVar and is set to unlimited. `maxUseCountPerPlayer`
  and `useCooldown` are **not** SyncVars, so setting them on the host doesn't
  reach clients, whose own check would lock them out after one use. Dream
  Arena hit this with its remnants. So after every use of our well
  (`Shrine.DoPostUseRoutines`), the synced use counts and last-use time are
  reset and the well is made available again, and each client's check keeps
  passing.
- Setting: `Dream Well`, default on. After a mod reload or a revisit, the
  well already standing anywhere in the arena is reused.
