using System;

namespace LastStand.Waves;

internal readonly struct Cadence
{
	public const int NotYet = -1;

	public int FirstWave { get; }

	public int IntervalWaves { get; }

	public Cadence(int firstWave, int intervalWaves)
	{
		FirstWave = firstWave;
		IntervalWaves = Math.Max(1, intervalWaves);
	}

	public bool IsOn(int wave)
	{
		return wave >= FirstWave && (wave - FirstWave) % IntervalWaves == 0;
	}

	public int Occurrence(int wave)
	{
		return wave < FirstWave ? NotYet : (wave - FirstWave) / IntervalWaves;
	}

	public int OccurrencesSoFar(int wave)
	{
		return Occurrence(wave) + 1;
	}
}
