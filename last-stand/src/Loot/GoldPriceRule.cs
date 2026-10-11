using System;
using LastStand.Waves;

namespace LastStand.Loot;

internal readonly struct GoldPriceRule
{
	private const float Free = 0f;
	private const int NeverStops = 0;

	public bool FreeShrines { get; }

	public double GrowthPerStep { get; }

	public Cadence Steps { get; }

	public int UntilWave { get; }

	public GoldPriceRule(bool freeShrines, double growthPerStep, Cadence steps, int untilWave)
	{
		FreeShrines = freeShrines;
		GrowthPerStep = growthPerStep;
		Steps = steps;
		UntilWave = untilWave;
	}

	public float MultiplierAt(int wave)
	{
		if (FreeShrines)
		{
			return Free;
		}

		int lastGrowingWave = UntilWave == NeverStops ? wave : Math.Min(wave, UntilWave);
		int steps = Math.Max(0, Steps.OccurrencesSoFar(lastGrowingWave));
		return (float)Math.Pow(1 + GrowthPerStep, steps);
	}
}
