# Curses

A curse (`CurseStatusEffect`) ends when its goal is met. The Hatred Remnant
rolls the goal 50/50: a number of kills, or a number of new rooms entered.
Others use reaching the next world. Nobody travels in Last Stand, so a travel
goal would never end, and vanilla's kill goals (28 to 50) would be over in
seconds with Last Stand's wave sizes.

So a postfix on `CurseStatusEffect.OnCreate` (host only, during the fight)
turns every hero curse whose goal is kills, rooms or worlds into a kill goal
sized to the waves (`CurseGoalRule`): one player's share of the current wave
(or the next one, during a break), times the strength's setting. Mild is 1
wave's worth, Potent 1.25, Powerful 1.75. Solo at wave 101 (309 monsters), a
Mild curse ends after 309 kills; with two players each needs half of that.
Kills and assists count, and a heroic boss counts as 10, as in vanilla.

- The goal type, amount and progress are SyncVars, so a vanilla client's
  curse quest shows the new goal. The quest (`Quest_KillToLiftCurse`) copies
  them on progress changes, so the host fires the curse's progress event
  right after the change.
- Curses that end some other way (`Hidden`, removed by whatever added them)
  are left alone.
- A knock-out still ends a curse, as in vanilla.
- Log: `curse: <type> was Travel x3; now ends after N kill(s).`
