using System;

namespace LastStand.Waves;

internal readonly struct HeroicFastKillRule
{
	public const double NoChange = 1;

	public double TargetSeconds { get; }

	public double BurstSeconds { get; }

	public double MaxStep { get; }

	public bool IsOn => TargetSeconds > 0 && BurstSeconds > 0 && MaxStep > NoChange;

	public HeroicFastKillRule(double targetSeconds, double burstSeconds, double maxStep)
	{
		TargetSeconds = targetSeconds;
		BurstSeconds = burstSeconds;
		MaxStep = maxStep;
	}

	public double StepFor(double projectedSeconds)
	{
		if (!IsOn || projectedSeconds >= TargetSeconds)
		{
			return NoChange;
		}

		return projectedSeconds <= 0 ? MaxStep : Math.Min(MaxStep, TargetSeconds / projectedSeconds);
	}

	public double MultiplierFor(double earned)
	{
		return IsOn ? earned : NoChange;
	}
}
