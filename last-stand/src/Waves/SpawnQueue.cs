using System;
using System.Collections.Generic;
using LastStand.Combat;

namespace LastStand.Waves;

internal sealed class SpawnQueue
{
	private readonly Queue<EnemyTier> _pending = new Queue<EnemyTier>();
	private const double NotScheduled = -1;

	private readonly List<EnemyTier> _released = new List<EnemyTier>();
	private readonly Queue<double> _bossEntrances = new Queue<double>();
	private double _nextReleaseAt;
	private double _firstReleaseAt = NotScheduled;
	private int _finale;
	private double _finaleAt = NotScheduled;

	public int PendingCount => _pending.Count + _finale + _bossEntrances.Count;

	public void Enqueue(WavePlan plan, IReadOnlyList<double> bossEntranceSeconds)
	{
		_bossEntrances.Clear();
		foreach (double seconds in bossEntranceSeconds)
		{
			_bossEntrances.Enqueue(seconds);
		}

		_finale = plan.HeroicBossCount - _bossEntrances.Count;
		_finaleAt = NotScheduled;
		_firstReleaseAt = NotScheduled;
		Add(EnemyTier.Pain, plan.PainCount);
		Add(EnemyTier.MiniBoss, plan.MiniBossCount);
		AddMixed(EnemyTier.Hunter, plan.HunterCount, EnemyTier.Regular, plan.RegularCount);
	}

	public bool HasPending(EnemyTier tier)
	{
		if (tier == EnemyTier.HeroicBoss && (_finale > 0 || _bossEntrances.Count > 0))
		{
			return true;
		}

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
		int perRelease, double finaleDelaySeconds)
	{
		_released.Clear();
		if (_pending.Count == 0)
		{
			ReleaseFinale(now, finaleDelaySeconds);
			return _released;
		}

		ReleaseBossesDue(now);
		if (now < _nextReleaseAt)
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
			if (_firstReleaseAt == NotScheduled)
			{
				_firstReleaseAt = now;
			}
		}

		return _released;
	}

	public void Clear()
	{
		_pending.Clear();
		_released.Clear();
		_bossEntrances.Clear();
		_nextReleaseAt = 0;
		_firstReleaseAt = NotScheduled;
		_finale = 0;
		_finaleAt = NotScheduled;
	}

	private void ReleaseBossesDue(double now)
	{
		while (_firstReleaseAt != NotScheduled && _bossEntrances.Count > 0 &&
		       now >= _firstReleaseAt + _bossEntrances.Peek())
		{
			_bossEntrances.Dequeue();
			_released.Add(EnemyTier.HeroicBoss);
		}
	}

	private void ReleaseFinale(double now, double delaySeconds)
	{
		_finale += _bossEntrances.Count;
		_bossEntrances.Clear();
		if (_finale == 0)
		{
			return;
		}

		if (_finaleAt == NotScheduled)
		{
			_finaleAt = now + delaySeconds;
			return;
		}

		if (now < _finaleAt)
		{
			return;
		}

		for (; _finale > 0; _finale--)
		{
			_released.Add(EnemyTier.HeroicBoss);
		}
	}

	private void AddMixed(EnemyTier spread, int spreadCount, EnemyTier rest, int restCount)
	{
		int total = spreadCount + restCount;
		for (int i = 0; i < total; i++)
		{
			bool spreadTurn = (long)(i + 1) * spreadCount / total > (long)i * spreadCount / total;
			_pending.Enqueue(spreadTurn ? spread : rest);
		}
	}

	private void Add(EnemyTier tier, int count)
	{
		for (int i = 0; i < count; i++)
		{
			_pending.Enqueue(tier);
		}
	}
}
