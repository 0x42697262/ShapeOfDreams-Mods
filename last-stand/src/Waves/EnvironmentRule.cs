using System;

namespace LastStand.Waves;

internal readonly struct EnvironmentRule
{
	public const int NoLastWave = 0;

	public const int NoBlock = -1;

	private const double Certain = 1;

	private const int NextWave = 1;

	public Cadence Sure { get; }

	public double Chance { get; }

	public int HighFromWave { get; }

	public double HighChance { get; }

	public int HuntedFromWave { get; }

	public double HuntedChance { get; }

	public int MaxPerBlock { get; }

	public int BlockWaves { get; }

	public int UntilWave { get; }

	public EnvironmentRule(Cadence sure, double chance, int highFromWave, double highChance, int huntedFromWave,
		double huntedChance, int maxPerBlock, int blockWaves, int untilWave)
	{
		Sure = sure;
		Chance = chance;
		HighFromWave = highFromWave;
		HighChance = highChance;
		HuntedFromWave = huntedFromWave;
		HuntedChance = huntedChance;
		MaxPerBlock = Math.Max(0, maxPerBlock);
		BlockWaves = Math.Max(NextWave, blockWaves);
		UntilWave = Math.Max(NoLastWave, untilWave);
	}

	public int Block(int wave)
	{
		return wave < HuntedFromWave ? NoBlock : (wave - HuntedFromWave) / BlockWaves;
	}

	public double ChanceFor(int wave, int environmentsInBlock)
	{
		if (UntilWave != NoLastWave && wave > UntilWave)
		{
			return 0;
		}

		if (Sure.IsOn(wave))
		{
			return Certain;
		}

		if (wave < HuntedFromWave)
		{
			return wave >= HighFromWave ? HighChance : Chance;
		}

		return environmentsInBlock + SureWavesLeftInBlock(wave) < MaxPerBlock ? HuntedChance : 0;
	}

	private int SureWavesLeftInBlock(int wave)
	{
		int blockEnd = HuntedFromWave + (Block(wave) + NextWave) * BlockWaves - NextWave;
		int last = UntilWave == NoLastWave ? blockEnd : Math.Min(blockEnd, UntilWave);
		int count = 0;
		for (int later = wave + NextWave; later <= last; later++)
		{
			if (Sure.IsOn(later))
			{
				count++;
			}
		}

		return count;
	}
}
