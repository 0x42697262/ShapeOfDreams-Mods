using System;

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

	public bool IsOn => FromWave != Never && Cap > 0;

	public MergeRule(int regularBase, int growthPerWave, int cap, int surgeMiniBosses, int surgePain)
	{
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

	public int ExtraMiniBosses(int wave, bool surge)
	{
		return surge && Covers(wave) ? SurgeMiniBosses : 0;
	}

	public int ExtraPain(int wave, bool surge)
	{
		return surge && Covers(wave) ? SurgePain : 0;
	}
}
