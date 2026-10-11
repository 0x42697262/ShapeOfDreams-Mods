namespace LastStand.Loot;

internal sealed class TierLoot
{
	public double DropMultiplier { get; }

	public int DreamDust { get; }

	public int Gold { get; }

	public TierLoot(double dropMultiplier, int dreamDust, int gold)
	{
		DropMultiplier = dropMultiplier;
		DreamDust = dreamDust;
		Gold = gold;
	}
}
