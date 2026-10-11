using System;
using LastStand.Config;

namespace LastStand.Waves;

internal readonly struct MergeRule
{
	public const double NoChange = 1;

	public const int Never = 0;

	private const int FirstWave = 1;

	public int FromWave { get; }

	public int Cap { get; }

	public int SurgeMiniBosses { get; }

	public int SurgePain { get; }

	public double MiniBossHealthMultiple { get; }

	public double HeroicBossHealthMultiple { get; }

	public bool IsOn => FromWave != Never && Cap > 0;

	public MergeRule(int regularBase, int growthPerWave, int cap, int surgeMiniBosses, int surgePain,
		double miniBossHealthMultiple, double heroicBossHealthMultiple)
	{
		MiniBossHealthMultiple = miniBossHealthMultiple;
		HeroicBossHealthMultiple = heroicBossHealthMultiple;
		Cap = cap;
		FromWave = StartWave(regularBase, growthPerWave, cap);
		SurgeMiniBosses = surgeMiniBosses;
		SurgePain = surgePain;
	}

	public static int StartWave(int regularBase, int growthPerWave, int cap)
	{
		if (cap <= 0)
		{
			return Never;
		}

		if (regularBase >= cap)
		{
			return FirstWave;
		}

		if (growthPerWave <= 0)
		{
			return Never;
		}

		return FirstWave + (int)Math.Ceiling((double)(cap - regularBase) / growthPerWave);
	}

	public bool Covers(int wave)
	{
		return IsOn && wave >= FromWave;
	}

	public int Mobs(int wave, int monsters)
	{
		return Covers(wave) && monsters > Cap ? Cap : monsters;
	}

	public double StrengthFor(int wave, int monsters)
	{
		return Covers(wave) && monsters > Cap ? (double)monsters / Cap : NoChange;
	}

	public double MiniBossHealthMultipleFor(int wave)
	{
		return Covers(wave) ? MiniBossHealthMultiple : 0;
	}

	public double HeroicBossHealthMultipleFor(int wave)
	{
		return Covers(wave) ? HeroicBossHealthMultiple : 0;
	}

	public static double ExtraPercentToReach(double health, double target)
	{
		if (health <= 0 || health >= target)
		{
			return 0;
		}

		return (target / health - 1) * Tuning.PercentScale;
	}

	public int ExtraMiniBosses(int wave, bool surge)
	{
		return surge && Covers(wave) ? SurgeMiniBosses : 0;
	}

	public int ExtraPain(int wave, bool surge)
	{
		return surge && Covers(wave) ? SurgePain : 0;
	}
}
