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

	public delegate double WorldSolver(double fromExtraWorlds, double healthMultiplier);

	public double RaiseFor(HeroicFastKillRecord record, double share, WorldSolver solver)
	{
		double step = share > 0 ? StepFor(record.ProjectedSeconds / share) : NoChange;
		return step <= NoChange ? 0 : Math.Max(0, solver(record.FromExtraWorlds, step) - record.FromExtraWorlds);
	}
}
