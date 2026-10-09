# Loot

**Monster item drops are off by default.** The
[Reminiscence of Luck](../shrines/reminiscence-of-luck.md) hands out memories and essences
instead, one free pick per player each wave. With `Monster item drops` off,
`LootService` skips the item roll on every kill and the guaranteed drop at
wave end; dream dust and bonus gold from kills, chaos rewards and freeing
leftover items work as before. Everything below about item chances, caps and
guaranteed drops applies only with it on.

**Items are scarce per wave, not per kill.** Every kill rolls once for a
memory or essence, and most rolls succeed. The per-wave cap below is what
keeps items scarce. Each rarity has its own chance, lower the rarer it is:

| Rarity | Monster or hunter | Miniboss (×1.5) | Pain miniboss or heroic boss (×2) |
|---|---|---|---|
| Common | 50% | 55% | 40% |
| Rare | 20% | 30% | 40% |
| Epic | 8% | 12% | 16% |
| Legendary | 2% | 3% | 4% |
| **Nothing** | **20%** | **0%** | **0%** |

These are the wave 1 chances. A boosted roll past 100% is cut back as
described below, which is why the miniboss and ×2 columns aren't plain
multiples. **The chances improve every wave** in a straight line up to a target at
wave 100, then stay there. Common falls from 50% to 25%, and the rest rise
until all four even out at 25% at wave 100. Together that's 100%: every kill
rolls an item.

| Monster or hunter | Common | Rare | Epic | Legendary | Any item |
|---|---|---|---|---|---|
| Wave 1 | 50% | 20% | 8% | 2% | 80% |
| Wave 25 | 43.9% | 21.2% | 12.1% | 7.6% | 84.8% |
| Wave 50 | 37.6% | 22.5% | 16.4% | 13.4% | 89.9% |
| Wave 75 | 31.3% | 23.7% | 20.7% | 19.2% | 94.9% |
| Wave 100 on | 25% | 25% | 25% | 25% | 100% |

The tier multiplier applies on top. Past 100%, the excess is cut from the
commonest end, but a rarity is never cut below 25% (`Rarity floor with item
multipliers`). Once it reaches the floor, the cut moves up to the next rarity.
So no rarity ever falls under 25%, and a boosted roll evens out at 25% each
sooner. The floor only limits cuts and never raises a rarity, so a custom
table with a target under 25% still reaches it. Chaos rarity has no floor.

| Even at 25% each from | Wave |
|---|---|
| Monster or hunter (×1) | 100 |
| Miniboss (×1.5) | 65 |
| Pain miniboss or heroic boss (×2) | 47 |

Before that, a boosted roll still leans toward the low end: at wave 25 a ×2
drop is 25% common, 35.6% rare, 24.2% epic and 15.2% legendary.

**Monsters and hunters are capped per wave**, however many die: at most one
item per player (config `Monster items per player per wave`).

- **Solo:** at most one item a wave, a memory or an essence, never both.
- **Two players:** at most two, one memory and one essence, one each.
- **More players:** one each, with memories and essences alternating to stay
  even.

Once everyone has had theirs, monster kills still drop dream dust and gold,
but no more items until the next wave. **Minibosses are not capped.** Each one
rolls its own chance whatever monsters have dropped, and its item goes to the
player with the fewest items this wave.

- The roll walks the rarities rarest first and adds up their chances, so one
  random number decides both whether an item drops and which rarity. If a
  multiplier pushes the total past 100%, the common chance is what gets cut.
- The item itself, and its level or quality, come from the game's `LootManager`
  (`SelectSkillAndLevel`, `SelectGemAndQuality`) for the rolled rarity, so
  drops follow the run's own pools and bans. If the pool has nothing of the
  rolled rarity, the next rarity down that has something is used, never one
  up. Without this, an empty pool meant no drop and no use of the player's
  cap, so the next kill rolled again, which skewed drops toward the rarities
  that do have items.
- **Every item is logged** (`loot: wave 12 Regular dropped a Rare memory.`),
  and `laststand_status` shows the run's count per rarity, so drop rates can
  be checked against the tables.
- **Who gets a monster item:** a player who hasn't had one this wave, with the
  killer first. The item is created with that player as `tempOwner`.
- **Reserved items, and freeing them at wave end.** An item with a `tempOwner`
  is locked for everyone else, and every game hides it from them
  (`IsLockedFor`, `ItemWorldModel.IsVisible`), unless the owner's hero is
  knocked out. A knocked-out player also watches an ally, so they see that
  ally's items instead of their own. Last Stand revives fallen heroes when a
  wave is cleared, so in co-op, items that had become visible would suddenly
  vanish again. They weren't lost, just reserved again.
  So when a wave is cleared, every memory and essence still lying on the
  ground with a `tempOwner` gets it cleared (`LeftoverItems`). The field is a
  synced SyncVar, and both world models refresh their visibility when it
  changes, so the items become visible and free for everyone, vanilla clients
  included. Items stay reserved during the wave, so the one-per-player split
  holds while fighting. In co-op, chat says how many were freed. Setting:
  `Free leftover items at wave end`, default on. The game itself only deletes
  a reserved item when its owner leaves the game.
- The rules are plain logic (`LootRules`, `DropLedger`). The ledger is cleared
  at the start of each wave.

| Other drops | Monster or hunter | Miniboss | Pain miniboss |
|---|---|---|---|
| Dream dust | 2 | 50 | 150 |
| Bonus gold | 3 | 40 | 100 |

- Dream dust is flat: 2 per monster or hunter, 50 per miniboss, 150 per Pain
  miniboss, every wave.
  Bonus gold grows 1% a wave in a straight line (`Gold growth per wave`) and
  isn't capped: a regular monster or hunter drops 3 at wave 1, about 6 at
  wave 100, on top of
  vanilla kill gold (hunters drop no kill gold, only this). It's one
  untargeted orb, so in co-op it's split between the players like kill gold.
- **Kill gold: vanilla by default, with an optional cap.** Vanilla kill gold
  (`GameManager.GetKillGoldAmount`) is a pool shared by every player: it's
  multiplied by the player count and the gold orb divides it back out. It
  follows the world's curve, not the wave. With `Max kill gold per player`
  above 0 (default 0, no cap), a postfix on the host caps the pool at that
  many gold times the player count, so at 1 each player gets about 1 gold a
  kill (the orb rounds each share at random), before their own gold-gain
  bonuses. Kill gold is only worked out on the host, so nothing on clients is
  affected. Hunters give none, as in the stock game. The cap was there while
  prices stayed vanilla; now that [gold prices](gold-prices.md) compound every
  wave, it's off.
- **Guarantee (off by default):** when on, anyone who got no memory or essence
  during a wave gets one at their feet when it's cleared.
- Nothing despawns. Memories and essences stay on the ground until someone
  takes them, as in the stock game.
