# Camp

The camp is the room a run opens in (`WorldNodeType.Start`). It's left as a
normal starting room and works as the between-rounds lobby: regroup, pick a
build, try it on the dummy, take the rift.

## Leaving camp

Stock behavior opens the world map and asks which node to travel to, which is
pointless when every node is the same arena. `Rift_RoomExit` supports a
direct route: its interaction checks `nextNodeIndex` first.

```csharp
if (nextNodeIndex >= 0) { confirm -> CmdTravelToNode(nextNodeIndex); return; }
if (currentNode.type != ExitBoss && !forceTravelToNextZone) { show world map; return; }
```

`nextNodeIndex` is a SyncVar, so `CampRift` (once a second in camp) setting it
on the host gives every player the boss-room feel: one prompt, then straight
into the fight. The node comes from `ArenaLocator.TryFindArenaNode`: it
skips the current node, the camp and sidetrack nodes, and prefers one
connected to the current node, else the first. Log: `camp rift now leads
straight to node N.`

`CampRift` is shared with Last Stand (`library/src/Arena/CampRift.cs`,
checked every `SharedTuning.CampRiftCheckSeconds`).

**The world map shows only the camp and the arena**, as in Last Stand. Once
the rift has its node, `CampRift` calls the library's
`WorldMapNodes.HideAllBut`: every other node's map position is mirrored to
a negative x (`-1 - x`), which makes it a sidetrack node, and written back to
the synced `ZoneManager.nodes`. The map (M) and the travel vote's map skip
sidetrack nodes, on vanilla clients too. Hidden nodes are also skipped by
`TryFindArenaNode`, so the rift keeps the same arena node all run, and a
restart (which travels to the camp) changes nothing. Log: `world map: hid N
node(s); only the camp and the arena (node K) are shown.`

The old build also replaced the rift's prompt text by intercepting
`DewLocalization.GetUIValue`. That lookup runs on each client, so only the
host saw the new text; it was removed (see
[Networking](../reference/networking.md)).

## Remnants

With `Camp remnants` on, ten remnants ring the camp rift, with an opening
toward the camp to walk in through: memories down one half, essences down the
other, one per rarity from Common to Unique. The two Commons flank the
opening and the Uniques meet at the back. Everything is free and never runs
out, so a build is chosen rather than farmed, and two players can take the
same thing. Each player can take each item once per round.

They're the game's own Retrospection and Enlightenment remnants
(`Shrine_Retrospection`, `Shrine_Enlightenment`). A remnant opens a pick-one
window whose contents are per player and written by the server, which fits;
a merchant's window is a purchase flow built around gold and stock counts.

**Offering.** `RemnantStock` lists every memory (`SkillTrigger`) or essence
(`Gem`) of a rarity that is in the game and not excluded from the pool,
sorted by name and cached. A postfix on each remnant's `OnPopulateChoices`
(`RemnantChoices.Offer`) replaces the usual three random picks with that
whole list, minus what the player already took, at `Memory level` (default
10) or `Essence quality` (default 400, Flawless). `itemCount` stays at the
prefab default: the window sizes its grid from the array.

**Why one remnant per rarity.** The choice window is a `GridLayoutGroup` with
no scrolling, so a remnant holding every memory would spill off screen. One
rarity is a few dozen entries.

**Which remnants are ours.** Each one is tagged in its creation callback,
before it's prepared, by its object name: `Memory|Epic (DreamArena)`
(`RemnantTag`). The name lives on the object itself, so it survives a mod
reload; remnants the world generates are never touched.

**Permanent fixtures.** On creation:

| Field | Value | Why |
|---|---|---|
| `maxTotalUseCount` | -1 | the game's own no-limit value |
| `maxUseCountPerPlayer` | -1 | same, per player |
| `useCooldown` | 0 | the cooldown is shared, so one pick would lock everyone out |
| `baseGoldCost` | 0 | gold is PvE progression |
| `unavailableByDefault`, `interactableDelay` | false, 0 | usable at once |

**The per-player limit isn't synced, so it's reset per pick.**
`maxUseCountPerPlayer` and `useCooldown` are plain fields: setting them on the
host never reaches a client, which keeps the prefab's one-pick rule and
judges it against the synced use count and last-use time. So a client could
pick only once. `RemnantChoosePatch` wraps the choose command
(`UserCode_CmdChoose__Int32__NetworkConnectionToClient`): the prefix notes
the item and the player's use count, and the postfix, only if the count went
up (the pick went through), records the item as taken, repopulates that
player's choices, and rolls both synced values back (`ResetUseCounts()`,
`_lastUseTime = -inf`). The client's own check then always passes.

**An emptied remnant.** When a player has taken everything a remnant offers,
their `choices` entry is empty. `EmptyRemnantPatch` makes `CanInteract`
false for them on the host, so the window doesn't open empty.

**Placement is rigid.** The ring is centered on the rift (a scene object with
no position syncing, so it can't be moved host-side). `RemnantRing` tries
radii 7 to 5 m, widest first, and for each turns the whole ring by up to 45°
to dodge a blocked slot while keeping the opening near the camp (the
direction to the camp's largest section, from the library's
`ArenaLayout.TryFindLargestSection`). It accepts a layout only when every
slot lands on walkable ground (within 0.65 m of the navmesh), so the camp gets
a clean ring or none. Each remnant faces inward. `CampRemnants` retries once
a second, up to 5 times per camp visit, and logs if it gives up.

**Restock.** A restart clears the taken lists and rebuilds the ring.

## Training dummy

With `Training dummy` on, a monster stands in camp beside the remnants
(`Dummy monster`, a Treant by default, picked from a list of 15) for trying
out memories and essences. The game has no dummy, so `TrainingDummy` makes an
ordinary monster harmless on the host. Clients see a normal monster with a
health bar and damage numbers.

- **No AI.** `EntityAI.disableAI`. The AI only runs on the server, so it
  never walks, turns or attacks.
- **Never dies.** A `DeathInterruptEffect` carried by the stock
  `Se_GenericEffectContainer` (`Actor.CreateBasicEffect`), the mechanism that
  knocks heroes out instead of killing them. A killing blow sets it back to
  full. Damage that ignores death interrupts still kills it; a new one
  appears within a second.
- **Never moves.** Knockbacks and pulls are displacements, which
  `TrainingDummyPatch` (a prefix on `EntityControl.StartDisplacement`)
  cancels the way the game cancels them on crowd-control-immune entities.
  The unstoppable effect would do that too, but it also strips stuns, slows
  and roots, which are worth testing. Anything else that moves it is undone
  by teleporting it home.
- **Resets.** It heals to full 3 s after the last hit.
- **No loot.** `disableLoot`, owned by `DewPlayer.creep`, spawned without
  its spawn animation.

It stands 10 to 14 m from the rift, about 40° to one side of the ring's
opening, so it never blocks the way in. It's recognized by its object name
(`Training Dummy (DreamArena)`), so after a mod reload the standing one is
reused instead of a second appearing. Leaving camp destroys it with the room.
