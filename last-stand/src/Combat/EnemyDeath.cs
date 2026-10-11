using UnityEngine;

namespace LastStand.Combat;

internal readonly struct EnemyDeath
{
	public EnemyTier Tier { get; }

	public Vector3 Position { get; }

	public DewPlayer Killer { get; }

	public double SecondsSinceFirstHit { get; }

	public EnemyDeath(EnemyTier tier, Vector3 position, DewPlayer killer, double secondsSinceFirstHit)
	{
		Tier = tier;
		Position = position;
		Killer = killer;
		SecondsSinceFirstHit = secondsSinceFirstHit;
	}
}
