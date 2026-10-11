using System;

namespace LastStand.Waves;

internal readonly struct WorldRule
{
	public const int NoFasterRise = 0;

	private const int FirstWave = 1;

	private const int MinimumStepWaves = 1;

	public double RisePerStep { get; }

	public int StepWaves { get; }

	public int FasterFromWave { get; }

	public double FasterRisePerStep { get; }

	public int FasterStepWaves { get; }

	public bool HasFasterRise => FasterFromWave != NoFasterRise;

	public WorldRule(double risePerStep, int stepWaves, int fasterFromWave, double fasterRisePerStep,
		int fasterStepWaves)
	{
		RisePerStep = Math.Max(0, risePerStep);
		StepWaves = Math.Max(MinimumStepWaves, stepWaves);
		FasterFromWave = Math.Max(NoFasterRise, fasterFromWave);
		FasterRisePerStep = Math.Max(0, fasterRisePerStep);
		FasterStepWaves = Math.Max(MinimumStepWaves, fasterStepWaves);
	}

	public double ExtraWorlds(int wave)
	{
		int lastNormalWave = HasFasterRise ? Math.Min(wave, FasterFromWave) : wave;
		double normal = RisePerStep * Math.Max(0, lastNormalWave - FirstWave) / StepWaves;
		double faster = HasFasterRise
			? FasterRisePerStep * Math.Max(0, wave - FasterFromWave) / FasterStepWaves
			: 0;
		return normal + faster;
	}
}
