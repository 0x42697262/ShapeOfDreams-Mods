using System;
using System.Collections.Generic;
using LastStand.Combat;

namespace LastStand.Waves;

internal sealed class SpawnQueue
{
	private readonly Queue<EnemyTier> _pending = new Queue<EnemyTier>();
	private readonly List<EnemyTier> _released = new List<EnemyTier>();
	private double _nextReleaseAt;

	public int PendingCount => _pending.Count;

	public void Enqueue(WavePlan plan)
	{
		Add(EnemyTier.Pain, plan.PainCount);
		Add(EnemyTier.MiniBoss, plan.MiniBossCount);
		Add(EnemyTier.Hunter, plan.HunterCount);
		Add(EnemyTier.Regular, plan.RegularCount);
	}

	public bool HasPending(EnemyTier tier)
	{
		foreach (EnemyTier pending in _pending)
		{
			if (pending == tier)
			{
				return true;
			}
		}

		return false;
	}

	public IReadOnlyList<EnemyTier> Release(double now, int alive, int maxAlive, double intervalSeconds,
		int perRelease)
	{
		_released.Clear();
		if (_pending.Count == 0 || now < _nextReleaseAt)
		{
			return _released;
		}

		int openings = Math.Min(perRelease, maxAlive - alive);
		while (openings > 0 && _pending.Count > 0)
		{
			_released.Add(_pending.Dequeue());
			openings--;
		}

		if (_released.Count > 0)
		{
			_nextReleaseAt = now + intervalSeconds;
		}

		return _released;
	}

	public void Clear()
	{
		_pending.Clear();
		_released.Clear();
		_nextReleaseAt = 0;
	}

	private void Add(EnemyTier tier, int count)
	{
		for (int i = 0; i < count; i++)
		{
			_pending.Enqueue(tier);
		}
	}
}
