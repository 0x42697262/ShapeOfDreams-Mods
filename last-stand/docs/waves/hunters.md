# Hunters and hunter zones

- **Hunters** are monsters with the game's `Se_HunterBuff`, the same buff the
  game's own hunter spawns get: faster, shadow-walking, no gold or exp. They
  are a share of the wave's regular monsters, not extras: the total stays the
  same and some of it spawns as hunters (`WaveSchedule.HunterShare`). The share
  starts at wave 101 (`First hunter wave`) and grows evenly each wave until
  wave 130 (`Every monster a hunter from wave`), where every regular monster is
  a hunter, and stays there: 1/30 at 101 (10 of 309), half at 115, 29/30 at
  129. Surges double the wave, so they double the hunters too. Once waves merge
  (wave 48 by default) the share applies to the merged 100 (see
  [Waves](waves.md)).
- **What all-hunter waves mean.** Hunters drop no kill gold and no exp, so from
  wave 130 the only gold is the bonus gold from minibosses, Pain groups and
  heroic bosses, and heroes stop gaining exp from kills. Items still drop
  (hunters use the regular monsters' loot).
- **Hunter zones** run on every wave from the first hunter wave on, except
  [environment](environments.md) waves (106, 116 …, and any wave that rolls
  one), where the environment takes
  their place. For each wave, the arena gets the game's `RoomMod_Hunted` modifier
  through `ZoneManager.AddModifier`, which adds it to the live room and syncs it
  to clients. Its artillery, small and big meteors aimed at the heroes, comes
  from the modifier itself, not from any skill, memory or essence. It also
  makes hunters tougher. The hunt level (`ZoneManager.currentHuntLevel`) is 1
  for waves 101–110 and rises by one every 10 waves (`Waves per hunt level`),
  up to 6 from wave 151 (`Max hunt level`), which scales the artillery. The
  game's own top is 5: its HUD shows at most 5 stars. When the wave is cleared
  the modifier is removed and the old hunt level is restored, so the breaks
  between waves have no artillery.
- **The artillery needs a fix in the arena.** The modifier's artillery
  routines stop once the room's rift is open (`endWhenRiftOpen`), and they
  check `Rift.instance.isOpen` before every shot. The arena has no rift, so
  `Rift.instance` is null: both routines threw a `NullReferenceException` on
  their first check and never fired. Last Stand turns `endWhenRiftOpen` off
  on its own instance before it starts (in `AddModifier`'s `beforePrepare`).
  It's a plain field read only by the host's routines, so vanilla clients just
  see the artillery the host spawns.
- **No kidnapping at a high hunt level.** In a normal run, at hunt level 5 or
  more, `GameMod_Obliviax` can start `Quest_HuntedByObliviax`, which ends in
  `Ge_Obliviax_InterruptTravelAndKidnap`. Both only react to a room load while
  traveling. Last Stand raises the hunt level inside the arena, after the
  only travel, and restores it when the zone ends, so neither ever fires. The
  threat level (the game's ambient level, `GameManager.SetAmbientLevel`) has no
  maximum and nothing in the game reacts to its value.
