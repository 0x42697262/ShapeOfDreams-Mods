namespace LastStand.Combat;

internal enum EnemyTier
{
	Regular,
	Hunter,
	MiniBoss,
	Pain,
}

internal static class EnemyTiers
{
	public static bool IsMob(this EnemyTier tier)
	{
		return tier == EnemyTier.Regular || tier == EnemyTier.Hunter;
	}
}
