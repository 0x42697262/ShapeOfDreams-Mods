using UnityEngine;

namespace LastStand.Combat;

internal readonly struct EnemyDeath
{
	public EnemyTier Tier { get; }

	public Vector3 Position { get; }

	public DewPlayer Killer { get; }

	public EnemyDeath(EnemyTier tier, Vector3 position, DewPlayer killer)
	{
		Tier = tier;
		Position = position;
		Killer = killer;
	}
}
