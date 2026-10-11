namespace LastStand.Combat;

internal readonly struct EnemyHit
{
	public EnemyTier Tier { get; }

	public uint EnemyId { get; }

	public double Amount { get; }

	public double MaxHealth { get; }

	public double At { get; }

	public EnemyHit(EnemyTier tier, uint enemyId, double amount, double maxHealth, double at)
	{
		Tier = tier;
		EnemyId = enemyId;
		Amount = amount;
		MaxHealth = maxHealth;
		At = at;
	}
}
