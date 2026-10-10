# Edge shrines

Every shrine Last Stand places (Dream Well, Altar of Cleansing, Shrine of
Ascension, Maw of Doom, Shrine of Destiny, Hatred Remnant, Reminiscence of
Luck) stands by the edge of the room, so
the center stays clear for the fight and the shrines are far enough apart
that nobody uses the wrong one by mistake. They're spaced at even angles
around the arena center (`EdgePlacement.PickRing`): two face each other,
three make a triangle, seven (the default) a heptagon. Only the turned-on ones
take a place, so the spacing stays even. Each shrine sits at the edge in its
own direction, so in a long or uneven room the shape stretches with the room.

- **Reach:** the host measures 72 directions (every 5°) from the center. In
  each it samples the navmesh every 1 m from 2.5 m out to 60 m
  (`NavMesh.SamplePosition`, 2 m), and takes the farthest floor point that
  is walkable from the center (`NavMesh.CalculatePath` complete, and the path
  at most 1.6× the straight line, so floor behind a wall or down a cliff
  doesn't count). Pillars and rocks in the way don't cut the reach short.
  Only the 6 farthest points are path-checked; if none passes, the old
  first-boundary trace (`NavMesh.Raycast`) is used.
- **Lava ends the reach:** the march stops at the first floor point that
  `Dew.IsNotPlayableOnTop` reports (the ground under it is an
  `INotPlayableOnTop` collider, such as `LavaLand_Lava`, the lava sea in
  the Lava Land boss room). The navmesh there runs on over the lava, so
  without this the reach went out about 59 m and shrines stood in the lava,
  far from the fight. When the march hits lava, the reach is the farthest
  walkable point before it (or 0), never the old trace, which doesn't see
  lava.
- **Cap:** a reach is capped at 1.3× the median reach of all 72, so a
  corridor or the entry path doesn't pull a shrine far away.
- **Directions:** every start of the 72 is tried, one direction every
  360° / n, and the start with the largest total capped reach wins.
- **Spot:** each shrine stands at its direction's capped reach minus 1.5 m
  (never under 6 m), snapped with `Dew.GetValidAgentDestination_LinearSweep`
  from the center, so it's on the navmesh and reachable on foot. If that
  spot is unplayable ground, it moves 1 m at a time toward the center until
  it isn't.
- Log: `edge ring: 7 shrine(s), median reach X m, capped at Y m; N
  direction(s) end at unplayable ground (lava).`
- **Why not one ring:** the first version used one radius for all, the
  shortest reach of the set, and traced only to the first navmesh boundary.
  In the Snow Mountain boss room one short direction (or a rock) kept every
  shrine about 14.5 m from the center, far inside the room.
- All of these values are in `Tuning`. Edge shrines already standing are
  reused, wherever they are. One turned on mid-run gets a fresh ring, so it may
  not line up with the others until the next run.
