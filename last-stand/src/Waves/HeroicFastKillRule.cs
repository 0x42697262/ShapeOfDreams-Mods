using System;

namespace LastStand.Waves;

internal readonly struct HeroicFastKillRule
{
	public const double NoChange = 1;

	public double SecondsPerBoss { get; }

	public double BurstSeconds { get; }

	public double MaxStep { get; }

	public bool IsOn => SecondsPerBoss > 0 && BurstSeconds > 0 && MaxStep > NoChange;

	public HeroicFastKillRule(double secondsPerBoss, double burstSeconds, double maxStep)
	{
		SecondsPerBoss = secondsPerBoss;
		BurstSeconds = burstSeconds;
		MaxStep = maxStep;
	}

	public double StepFor(double projectedSecondsPerBoss)
	{
		if (!IsOn || projectedSecondsPerBoss >= SecondsPerBoss)
		{
			return NoChange;
		}

		return projectedSecondsPerBoss <= 0 ? MaxStep : Math.Min(MaxStep, SecondsPerBoss / projectedSecondsPerBoss);
	}

	public double MultiplierFor(double earned)
	{
		return IsOn ? earned : NoChange;
	}
}
