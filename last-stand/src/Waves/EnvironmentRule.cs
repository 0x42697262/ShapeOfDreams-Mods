using System;

namespace LastStand.Waves;

internal readonly struct EnvironmentRule
{
	public const int NoLastWave = 0;

	public const int NoBlock = -1;

	private const int NextWave = 1;

	public double Chance { get; }

	public int HighFromWave { get; }

	public double HighChance { get; }

	public int HuntedFromWave { get; }

	public double HuntedChance { get; }

	public int MaxPerBlock { get; }

	public int BlockWaves { get; }

	public int HeroicFromWave { get; }

	public int UntilWave { get; }

	public EnvironmentRule(double chance, int highFromWave, double highChance, int huntedFromWave,
		double huntedChance, int maxPerBlock, int blockWaves, int heroicFromWave, int untilWave)
	{
		Chance = chance;
		HighFromWave = highFromWave;
		HighChance = highChance;
		HuntedFromWave = huntedFromWave;
		HuntedChance = huntedChance;
		MaxPerBlock = Math.Max(0, maxPerBlock);
		BlockWaves = Math.Max(NextWave, blockWaves);
		HeroicFromWave = heroicFromWave;
		UntilWave = Math.Max(NoLastWave, untilWave);
	}

	public int Block(int wave)
	{
		return wave < HuntedFromWave ? NoBlock : (wave - HuntedFromWave) / BlockWaves;
	}

	public bool AllowsHeroic(int wave)
	{
		return wave >= HeroicFromWave;
	}

	public double ChanceFor(int wave, int environmentsInBlock)
	{
		if (UntilWave != NoLastWave && wave > UntilWave)
		{
			return 0;
		}

		if (wave < HuntedFromWave)
		{
			return wave >= HighFromWave ? HighChance : Chance;
		}

		return environmentsInBlock < MaxPerBlock ? HuntedChance : 0;
	}
}
