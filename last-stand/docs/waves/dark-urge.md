# Dark urge

From wave 176 (`First dark urge wave`), every wave without a heroic boss has
a 20% chance (`Dark urge chance`) to give **every player the game's Dark Urge
curse** (`Se_Curse_DarkUrge`) until it's cleared, so players can hurt each
other. It never happens solo.

**It's the real curse, so vanilla clients see it like any curse:** the "You
have been cursed" message, the chat line, the curse icon, and the curse's
quest in the tracker on the right (`Quest_KillToLiftCurse`, shown to the
cursed player), with the curse's own name and text. The curse also does the
work: it adds the other players to its owner's synced `DewPlayer.neutrals`
(the game treats neutrals as hittable) and scales damage between players by
its strength: Mild 15%, Potent 25% (`Dark urge curse strength`, the default),
Powerful 40%, and ×0.35 more against a player under half health. The setting
is a strength rather than a free percent so the curse's text on every client
matches what happens.

**How Last Stand runs it.** At the wave start the host gives each human hero
the curse, unless they already have one (from the Hatred Remnant, say), with
the progress type `Hidden`, so no kill or travel count shows and it can't lift
itself, and `dontRemoveOnKnockOut`, so a knock-out doesn't lift it either. It
re-checks once a second for a player who joins mid-wave. When the wave is
cleared it destroys the curses it gave, which completes their quests ("curse
lifted") and takes the neutral relations back. Any leftover Hidden Dark Urge
with `dontRemoveOnKnockOut` (only Last Stand makes those, e.g. one restored
from a save) is removed when the arena loads. Chat also says when it starts,
in red with its chance ("A dark urge takes hold (20% chance per wave): …"),
and when it fades.

**Counted once.** The curse applies its multiplier in both the attacker's
`dealtDamageProcessor` and the target's `takenDamageProcessor`, guarded only
against its own instance. With both players cursed, the hit would be cut
twice (25% × 25%). A `takenDamageProcessor` on each hero, running before the
curse's (priority −1), sees that the attacker's curse already cut the hit and
marks it as cut by the target's curse too, so it's 25% once, as the text says.

Side effect, as with the curse in a normal run: while it lasts, players are
neutral to each other, so ally-only effects (heals, shields, buffs on
teammates) don't reach them. A player who had their own Dark Urge from the
Hatred Remnant keeps it, but the cleared wave also removes their neutral
relations with the others until their curse's next refresh.
