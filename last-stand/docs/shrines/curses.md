# Curses

A curse (`CurseStatusEffect`) ends when its goal is met. The Hatred Remnant
rolls the goal 50/50: a number of kills, or a number of new rooms entered.
Others use reaching the next world. Nobody travels in Last Stand, so a travel
goal would never end, and vanilla's kill goals (28 to 50) would be over in
seconds with Last Stand's wave sizes.

So a postfix on `CurseStatusEffect.OnCreate` (host only, during the fight)
turns every hero curse whose goal is kills, rooms or worlds into a kill goal
sized to the waves (`CurseGoalRule`): the whole current wave (or the next
one, during a break), times the strength's setting. Mild is 1 wave's
worth, Potent 1.25, Powerful 1.75. Solo at wave 101 (206 monsters at full
strength), a Mild curse ends after 206 kills; with two players the wave is
309, and so is the goal. A heroic boss counts as 10, as in vanilla.

**Not split by players.** The curse counts kills through the game's
`KillTracker` (started in `CurseStatusEffect.OnCreate` with an 8 s window):
every monster the cursed hero damaged in the 8 s before it died counts,
whoever landed the killing blow. So in co-op a cursed player who hits most
monsters gets credit for nearly the whole wave, not 1/N of it. An earlier
version divided the goal by the player count, which let an active cursed
player in co-op lift a curse in about half the intended time.

**Merged waves.** The goal counts the wave at full strength
(`WavePlan.FullStrengthMonsters`: the unmerged mob count plus minibosses,
Pain minibosses and heroic bosses), so the goal keeps rising with the wave.
From wave 48 a merged wave spawns at most 100 mobs, each with the strength
of `count / 100` of them (see [Waves](../waves/waves.md)), and every kill
still counts 1. So late curses outlast a wave on purpose: with two players
at wave 101 a Mild goal is 309 while a wave has 100 mobs, so even a player
who touches every one needs about three waves. Before, the goal used the
100 actually spawned (and was split): with two players the Mild goal fell
from 74 at wave 47 to about 50 at wave 48 and never grew again. Counting a merged kill as its strength (a ×8 mob as
8 kills) was tried and dropped: the goal is meant to grow with the wave.

- The goal type, amount and progress are SyncVars, so a vanilla client's
  curse quest shows the new goal. The quest (`Quest_KillToLiftCurse`) copies
  them on progress changes, so the host fires the curse's progress event
  right after the change.
- Curses that end some other way (`Hidden`, removed by whatever added them)
  are left alone.
- A knock-out still ends a curse, as in vanilla.
- Log: `curse: <type> was Travel x3; now ends after N kill(s).`
