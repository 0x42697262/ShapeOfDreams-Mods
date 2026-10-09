using System;
using LastStand.Config;

namespace LastStand.Waves;

internal readonly struct FastClearRule
{
	public const double NoChange = 1;

	public double KillShare { get; }

	public double WindowSeconds { get; }

	public double StatMultiplier { get; }

	public int FromWave { get; }

	public double HeroicBossShare { get; }

	public bool IsOn => KillShare > 0 && WindowSeconds > 0 && StatMultiplier != NoChange;

	public FastClearRule(double killShare, double windowSeconds, double statMultiplier, int fromWave,
		double heroicBossShare)
	{
		KillShare = killShare;
		WindowSeconds = windowSeconds;
		StatMultiplier = statMultiplier;
		FromWave = fromWave;
		HeroicBossShare = heroicBossShare;
	}

	public bool WatchesWave(int wave)
	{
		return IsOn && wave >= FromWave;
	}

	public int KillsNeeded(int monsters)
	{
		return Math.Max(1, (int)Math.Ceiling(monsters * KillShare));
	}

	public double MultiplierFor(int stacks)
	{
		return IsOn ? Math.Pow(StatMultiplier, stacks) : NoChange;
	}

	public double HeroicBossMultiplierFor(int stacks)
	{
		return IsOn ? Math.Pow(StatMultiplier, stacks * HeroicBossShare) : NoChange;
	}

	public static double Raise(double bonusPercent, double multiplier)
	{
		return (Tuning.PercentScale + bonusPercent) * multiplier - Tuning.PercentScale;
	}
}
