# Gold prices

Every gold price in the game goes through the synced
`GameManager.goldCostMultiplier`: buy prices (`SkillTrigger.GetBuyGold`,
`Gem.GetBuyGold`) through `GetAdjustedGoldAmount_Cost`, and service prices
through `GetAdjustedGoldAmount_Cost_Service`. The game itself leaves it at 1
for the whole run (nothing in `Dew.Core` or `Dew.Contents` writes it). It's a
SyncVar, so the host and vanilla clients show and charge the same price.

Last Stand sets it on arrival, at the start of every wave and when the
settings are applied, and puts it back to 1 when the session stops or the
mod unloads (log: `gold prices: multiplier set to X at wave N`,
`gold prices: multiplier restored to 1`). The value comes from
`GoldPriceRule`, plain logic in `src/Loot`.

## Prices grow with the waves

By default prices go up **5% every wave, compounding, until wave 100**
(`Gold price growth (%)`, `Gold prices grow every N waves`, `Gold prices
stop growing at wave`): the multiplier is `1.05 ^ min(wave, 100)`, and stays
at 131× after wave 100.

| Wave | 1 | 10 | 50 | 75 | 100 and after |
|---|---|---|---|---|---|
| Multiplier | 1.05× | 1.6× | 11.5× | 39× | 131× |
| Maw of Doom (before the zone curve) | 21 | 33 | 229 | 777 | 2,630 |

Until wave 100 this outgrows gold income on purpose: bonus gold grows 1% of
its base a wave in a straight line (about 2× at wave 100), vanilla kill gold
follows the world, not the wave, and Hatred Shard gold is flat. After wave
100 prices hold while bonus gold keeps growing, so shrines slowly get
affordable again. Without the stop, wave 200 would be 17,293× (a 345,852
gold Maw). Growth at 0% keeps vanilla prices all run; 4% a wave gives 50× at
wave 100.

What it raises:

- the Maw of Doom (20 gold × the multiplier, still 8% risk),
- the Shrine of Ascension,
- the Reminiscence of Luck's prays.

The Altar of Cleansing doesn't use the multiplier (`GetCleanseGoldCost` goes
through `GetAdjustedGoldAmount` only), so it keeps its vanilla price.

Side effect: special gold rewards that the game prices from a buy price
(`GameManager.GetSpecialRewardAmount_Gold`, used by gold boxes, hidden
stashes, Pot of Greed and the like) grow by the same multiplier. The Hatred
Shard isn't affected: its gold is set to a flat amount (see
[Hatred Remnant](../shrines/hatred-remnant.md)).

## Free gold shrines

With `Free gold shrines` on (off by default) the multiplier is 0 instead, and
growth is ignored. The Maw of Doom and the Shrine of Ascension cost 0 gold, and
a Luck pray costs 0 or 1 (each item's buy price is raised to at least 1
before the pray fraction is taken). The Maw keeps its 8% risk, since that
comes from the pinned use count, not the price. The Altar keeps its price.

None of this is a JSON override, so vanilla runs are untouched.
