namespace LastStand.Loot;

internal readonly struct LootDecision
{
	public Rarity? ItemRarity { get; }

	public int DreamDust { get; }

	public int Gold { get; }

	public bool DropsItem => ItemRarity.HasValue;

	public LootDecision(Rarity? itemRarity, int dreamDust, int gold)
	{
		ItemRarity = itemRarity;
		DreamDust = dreamDust;
		Gold = gold;
	}
}
