namespace LastStand.Loot;

internal readonly struct RarityChance
{
	public Rarity Rarity { get; }

	public double Chance { get; }

	public double GrowthPerWave { get; }

	public RarityChance(Rarity rarity, double chance, double growthPerWave)
	{
		Rarity = rarity;
		Chance = chance;
		GrowthPerWave = growthPerWave;
	}
}
