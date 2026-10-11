# Modifier panel

The game's modifier panel (`UI_InGame_RoomModDisplay`, under the quest
tracker) shows the newest *main* room modifier's name and description. Each
game draws it for itself: it appears when a main modifier actor is added
(`ClientEvent_OnActorAdd`) and is hidden only when a room starts loading
(`ClientEvent_OnRoomLoadStarted`). Removing the modifier doesn't hide it, and
the arena never loads a new room, so a vanilla client would keep "Occupied
by Hunters", "Omen of Pain" or an environment on screen long after it ended.

**A default panel instead of hiding it.** The host can't hide a client's
panel, but a new main modifier always replaces it. So whenever none of Last
Stand's own main modifiers (hunter zone, Aura of Pain, environment) is
active, `ArenaPanel` keeps `RoomMod_FragmentOfRadiance_MysteriousPlace` on
the arena node: **Mysterious Space**, "The unfamiliar atmosphere sends
chills down your spine...". It shows on arrival and again after each of the
others ends, on the host and every client alike. When one of the others
starts, the game itself removes it (one main modifier per node), and it
comes back once they're all gone. The panel stays up during plain waves too,
which is why the text has to fit any moment.

- **Why this one.** A modifier can only show in the arena if it's main and
  not `hiddenOnVisitedNode` (the arena counts as a revisited room), and it
  needs its own text. Read from the asset bundle, the choices besides the
  wave environments were Mysterious Space, Souvenir Merchant (spawns a
  merchant), Fallen Star (buffs monsters, places a shrine) and Dream of the
  Star Seeker (drops stardust). Powerful Monsters, Ominous Presence, Ambush
  and the like are hidden in revisited rooms.
- **No gameplay.** Mysterious Space only adds
  `Rift_Sidetrack_FragmentOfRadiance` to the room's
  `openedSidetrackRifts`, which is read when a room is cleared. The host
  removes it again on every tick.
- **The announcement.** In a revisited room the game shows the panel quietly,
  without the big center animation.
- No JSON overrides and no faked network messages: it's a normal modifier,
  added through `ZoneManager.AddModifier` like the hunter zone.
- Log: `panel: RoomMod_FragmentOfRadiance_MysteriousPlace shown as the arena's
  default modifier panel.`
