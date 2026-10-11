namespace LastStand.Loot;

internal readonly struct RarityChance
{
	public Rarity Rarity { get; }

	public double Chance { get; }

	public double TargetChance { get; }

	public RarityChance(Rarity rarity, double chance, double targetChance)
	{
		Rarity = rarity;
		Chance = chance;
		TargetChance = targetChance;
	}
}
