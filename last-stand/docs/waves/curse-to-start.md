# Curse to start

A difficulty option, off by default: `Curse to start each wave`. With it on,
there's no wave timer. Each wave, the first one included, waits until a
player takes a curse at the [Hatred Remnant](../shrines/hatred-remnant.md).

- **The Hatred Remnant is forced on.** `SessionSettings` turns the shrine
  on whenever this option is, whatever `Hatred Remnant` says.
- **Every choice is a curse.** The shrine rolls three of None, Mild, Potent
  and Powerful once (`Shrine_Hatred.choices`, a synced `SyncList`). A None
  pick would spend the player's one choice for the wave without starting
  it, so solo the run would be stuck. With the option on, the host swaps
  None for the strength that's missing, on its shrine pin timer. The list is
  synced, so a vanilla client's window shows the new choice. The swap stays
  for the rest of the run (the list is saved with the run).
  Log: `hatred: choice None replaced with Mild so every choice is a curse.`
- **The hold.** When a wave is cleared, or on arrival, the host sets the
  next wave's start time to never (`WaveCycle.Hold`) and gives everyone
  their Hatred choice back. Chat: `Wave N cleared. Choose a curse at the
  Hatred Remnant to start wave N+1.` The quest tracker reads `Wave N+1:
  choose a curse`. Log: `curse to start: wave N waits for a curse`.
- **One player curses everyone.** A postfix on `Shrine_Hatred.DoCurse`
  (host only; it runs 0.75 s after the pick) sees the chooser's strength.
  If the wave is on hold, every other live hero gets a curse of the same
  strength, each a different type: the pick skips curses any player already
  has, and falls back to any viable one when they run out. It uses the
  game's own pick (`availableStrengths`, `IsViable`, `chanceWeight`). Their
  goals become kills, sized like every curse ([Curses](../shrines/curses.md)).
  The chooser gets their own curse and Hatred Shard as usual; the others
  get no shard.
- **The wave starts 3 s later** (`Curse to start: delay (s)`). Chat: `A
  Mild curse falls on everyone. Wave N starts in 3 s.`
- A curse taken during a wave works as vanilla: only the chooser is cursed,
  and nothing starts.
- If the shrine isn't standing, the wave runs on the normal timer, and the
  log says `wave N started without a curse`. `laststand_next` and
  `laststand_skip` skip the hold the same way.
- After a mod reload in a break, the hold starts again and the choices are
  given back, so nobody is locked out.
- There's no [wave vote](wave-vote.md) during the hold. After a curse,
  the 3 s delay is too short for one (votes open with more than 5 s left).
- Turning the option off in a break lets the next wave start after the
  normal break.
