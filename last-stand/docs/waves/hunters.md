# Hunters and hunter zones

- **Hunters** are monsters with the game's `Se_HunterBuff`, the same buff the
  game's own hunter spawns get: faster, shadow-walking, no gold or exp. They
  are a share of the wave's regular monsters, not extras: the total stays the
  same and some of it spawns as hunters (`WaveSchedule.HunterShare`). The share
  starts at wave 100 (`First hunter wave`) and grows evenly each wave until
  wave 130 (`Every monster a hunter from wave`), where every regular monster is
  a hunter, and stays there: 1/31 at 100, about half at 115, 30/31 at
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
  for waves 100–104 and rises by one every 5 waves (`Waves per hunt level`):
  2 at 105, 3 at 110, 11 at 150, 21 at 200. `Max hunt level` is 0 by
  default, which means no cap; any other value caps it. The game's own top
  is 5: its HUD shows at most 5 stars and "Hunted V", but the level itself
  is a synced int with no limit.
- **What a hunt level above 5 does.** The prefab's formulas (`RoomMod_Hunted`,
  evaluated with the hunt level as `x`) mostly cap early: artillery interval
  at level 4, big meteor chance at about 5.3, hunter size at 5.4, artillery
  speed at 5.6, small meteor count at 6, smart-AI share at 7. Three keep
  growing: artillery strength (`Clamp(0.9+0.1*x, 1, 5)`, cap at 41), hunter
  damage bonus (`Clamp(0.2+0.06*x, 0, 2.5)`, cap at 38) and hunter health
  bonus (`Clamp(0.2+0.035*x, 0, 2)`, cap at 51). Past about level 51 nothing
  changes. All of it runs on the host (artillery actors, hunter stats), so
  vanilla clients feel it too. Nothing else in the game reacts to a high
  level outside travel. When the wave is cleared
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
