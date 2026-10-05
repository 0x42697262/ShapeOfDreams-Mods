using System;
using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Loot;

internal sealed class RarityTable
{
	private readonly IReadOnlyList<RarityChance> _rarestFirst;
	private readonly List<RarityChance> _atWave = new List<RarityChance>();
	private int _cachedWave = -1;

	public RarityTable(IReadOnlyList<RarityChance> rarestFirst)
	{
		_rarestFirst = rarestFirst;
	}

	public IReadOnlyList<RarityChance> At(int wave)
	{
		if (wave == _cachedWave)
		{
			return _atWave;
		}

		_cachedWave = wave;
		_atWave.Clear();

		int steps = Math.Max(0, wave - Tuning.FirstWave);
		double added = 0;
		foreach (RarityChance entry in _rarestFirst)
		{
			if (entry.Rarity == Rarity.Common)
			{
				_atWave.Add(new RarityChance(entry.Rarity, Math.Max(0, entry.Chance - added), 0));
				continue;
			}

			double growth = entry.GrowthPerWave * steps;
			added += growth;
			_atWave.Add(new RarityChance(entry.Rarity, entry.Chance + growth, entry.GrowthPerWave));
		}

		return _atWave;
	}
}
