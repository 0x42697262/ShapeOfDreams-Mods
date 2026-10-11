# Mirage skins

From wave 30, regular monsters and hunters have a 20% chance to spawn with a
Mirage skin (`MirageSkinEffect`), the game's own variant that adds a shield,
more health and special attacks. The first skin unlocked is Delusion, the one
the game uses in its early zones. The rest follow by the game's skin `tier`,
weakest first, one more every 10 waves. A monster that already has a skin,
for example from the vanilla Mirage Skin modifier, is left alone.

From wave 130 (`Mirage chance grows from wave`) the chance grows evenly every
wave, from 20% to 100% at wave 149 (`Every monster a Mirage from wave`), and
stays at 100% after that (`WaveSchedule.MirageChance`): 24% at 130, 60% at
139, 96% at 148. Every regular monster and hunter then wears a skin. The
exceptions are the game's own: `Lesser` monsters never get one, and nor do
minibosses or bosses. The `wave N:` log line shows the chance as
`mirageChance=`.
