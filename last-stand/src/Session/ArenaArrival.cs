using System.Collections.Generic;
using LastStand.Config;
using LastStand.Game;

namespace LastStand.Session;

internal sealed class ArenaArrival
{
	private readonly List<Hero> _heroes = new List<Hero>();
	private double _nextAttemptAt;

	public int StagedHeroes => _heroes.Count;

	public bool TryArrive(double now, out ArenaPoints arena)
	{
		arena = default;
		if (now < _nextAttemptAt)
		{
			return false;
		}

		_nextAttemptAt = now + Tuning.ArrivalRetrySeconds;

		LiveHeroes.Fill(_heroes);
		if (_heroes.Count == 0 || !ArenaLayout.TryFind(false, out arena))
		{
			return false;
		}

		HeroStaging.PlaceAround(arena, _heroes, Tuning.HeroRingShareOfRadius, Tuning.MinHeroRingRadius);
		return true;
	}

	public void Reset()
	{
		_nextAttemptAt = 0;
		_heroes.Clear();
	}
}
