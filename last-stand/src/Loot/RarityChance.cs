namespace LastStand.Loot;

internal readonly struct RarityChance
{
	public Rarity Rarity { get; }

	public double Chance { get; }

	public RarityChance(Rarity rarity, double chance)
	{
		Rarity = rarity;
		Chance = chance;
	}
}
