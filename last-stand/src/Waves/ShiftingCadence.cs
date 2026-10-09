using System;

namespace LastStand.Waves;

internal readonly struct ShiftingCadence
{
	private readonly Cadence _early;
	private readonly Cadence _late;
	private readonly int _switchWave;
	private readonly int _earlyCount;

	public ShiftingCadence(Cadence early, int switchWave, int lateIntervalWaves)
	{
		_early = early;
		_switchWave = Math.Max(early.FirstWave, switchWave);
		_earlyCount = early.OccurrencesSoFar(_switchWave - 1);
		int lastEarly = _earlyCount > 0 ? early.FirstWave + (_earlyCount - 1) * early.IntervalWaves : Cadence.NotYet;
		_late = new Cadence(FirstOnOrAfter(lastEarly, Math.Max(1, lateIntervalWaves), _switchWave),
			lateIntervalWaves);
	}

	public int FirstWave => _early.FirstWave;

	public bool IsOn(int wave)
	{
		return wave < _switchWave ? _early.IsOn(wave) : _late.IsOn(wave);
	}

	public int Occurrence(int wave)
	{
		if (wave < _switchWave)
		{
			return _early.Occurrence(wave);
		}

		int late = _late.Occurrence(wave);
		return late == Cadence.NotYet ? _earlyCount - 1 : _earlyCount + late;
	}

	private static int FirstOnOrAfter(int lastEarly, int interval, int switchWave)
	{
		if (lastEarly == Cadence.NotYet)
		{
			return switchWave;
		}

		int steps = (switchWave - lastEarly + interval - 1) / interval;
		return lastEarly + Math.Max(1, steps) * interval;
	}
}
