# Altar of Cleansing

Players aren't meant to buy items, so there are no merchants. Instead gold
goes into the shrines by the edge of the room ([Edge shrines](edge-shrines.md)):
the Altar of Cleansing, the Shrine of Ascension, the Maw of Doom and the
Reminiscence of Luck's prays, all on by default. The Shrine of Destiny (on)
stands there too and costs no gold. With
[free gold shrines](../rewards/gold-prices.md) (off by default) everything but
the Altar costs nothing.

The host places the game's `Shrine_AltarOfCleansing`.

- **What it does:** for gold, it resets a memory to its lowest level or an
  essence to its lowest quality, and drops the dream dust that was spent
  upgrading it. That dust can go into another item at the well, so upgrades
  can be moved between items. It doesn't remove the item.
- **Price:** the game's own (`GameManager.GetCleanseGoldCost`, by memory level
  or essence quality), so it rises as items get upgraded. Every game works it
  out the same way, so vanilla clients see the price they're charged. A price
  that also rises with each use isn't possible that way, because none of the
  synced price settings affect it.
- **Unlimited uses:** the same reset as the well, through the same patch, so
  vanilla clients can use it again and again.
- Setting: `Altar of Cleansing`, default on. After a mod reload or a
  revisit, the altar already standing anywhere in the arena is reused.
