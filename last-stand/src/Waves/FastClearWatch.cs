using System;

namespace LastStand.Waves;

internal sealed class FastClearWatch
{
	private bool _watching;
	private int _needed;
	private int _kills;
	private double _deadline;

	public int Kills => _kills;

	public int Stacks { get; private set; }

	public void Start(int wave, int monsters, double now, FastClearRule rule)
	{
		_watching = rule.WatchesWave(wave) && monsters > 0;
		_needed = rule.KillsNeeded(monsters);
		_kills = 0;
		_deadline = now + rule.WindowSeconds;
	}

	public bool OnKill(double now)
	{
		if (!_watching)
		{
			return false;
		}

		if (now > _deadline)
		{
			_watching = false;
			return false;
		}

		_kills++;
		if (_kills < _needed)
		{
			return false;
		}

		_watching = false;
		Stacks++;
		return true;
	}

	public void Cancel()
	{
		_watching = false;
	}

	public void Reset(int stacks)
	{
		_watching = false;
		Stacks = Math.Max(0, stacks);
	}
}
