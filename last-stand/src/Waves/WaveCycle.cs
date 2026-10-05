using System;

namespace LastStand.Waves;

internal sealed class WaveCycle
{
	private double _nextWaveAt;

	public int CurrentWave { get; private set; }

	public bool InProgress { get; private set; }

	public WaveCycle(double firstWaveAt)
	{
		_nextWaveAt = firstWaveAt;
	}

	public bool TryStartWave(double now)
	{
		if (InProgress || now < _nextWaveAt)
		{
			return false;
		}

		CurrentWave++;
		InProgress = true;
		return true;
	}

	public bool TryFinishWave(double now, bool fieldClear, double intermissionSeconds)
	{
		if (!InProgress || !fieldClear)
		{
			return false;
		}

		InProgress = false;
		_nextWaveAt = now + intermissionSeconds;
		return true;
	}

	public double SecondsUntilNextWave(double now)
	{
		return InProgress ? 0 : Math.Max(0, _nextWaveAt - now);
	}

	public void StartNextWaveNow(double now)
	{
		InProgress = false;
		_nextWaveAt = now;
	}
}
