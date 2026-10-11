using System;
using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Loot;

internal sealed class RarityTable
{
	private readonly IReadOnlyList<RarityChance> _rarestFirst;
	private readonly int _targetWave;
	private readonly List<RarityChance> _atWave = new List<RarityChance>();
	private int _cachedWave = -1;

	public RarityTable(IReadOnlyList<RarityChance> rarestFirst, int targetWave)
	{
		_rarestFirst = rarestFirst;
		_targetWave = Math.Max(Tuning.FirstWave + 1, targetWave);
	}

	public IReadOnlyList<RarityChance> At(int wave)
	{
		if (wave == _cachedWave)
		{
			return _atWave;
		}

		_cachedWave = wave;
		_atWave.Clear();

		double progress = Math.Min(1, Math.Max(0, (double)(wave - Tuning.FirstWave) / (_targetWave - Tuning.FirstWave)));
		foreach (RarityChance entry in _rarestFirst)
		{
			double chance = entry.Chance + (entry.TargetChance - entry.Chance) * progress;
			_atWave.Add(new RarityChance(entry.Rarity, chance, entry.TargetChance));
		}

		return _atWave;
	}
}
