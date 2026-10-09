# Top-right label

The top-right location label is left to the game, so the host and every
client see the same text: `{world}. {zone name}`, for example
`1. Forest of Turbulence`, or the room's own name in a room that brings one
(the Primus room shows `3. The Lone King's Zenith` at world 1). The world
number comes from [Starting world](../start/starting-world.md).

It can't show the wave. `UI_InGame_Location` builds the text on each client
from the synced zone index and the localized zone name, and only when a room
loads, which the arena does once. The name can only be a real zone's. So the
wave goes in the [quest tracker](wave-tracker.md) instead, which everyone sees.
Earlier builds overwrote the label on the host only; that was dropped so all
players see the same thing.
