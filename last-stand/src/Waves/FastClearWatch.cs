using System;

namespace LastStand.Waves;

internal sealed class FastClearWatch
{
	private FastClearRule _rule;
	private bool _watching;
	private bool _triggered;
	private int _needed;

	public int InstantKills { get; private set; }

	public int Deaths { get; private set; }

	public int Needed => _needed;

	public bool Missed => _watching && !_triggered;

	public int Stacks { get; private set; }

	public void Start(int wave, int monsters, FastClearRule rule)
	{
		_rule = rule;
		_watching = rule.WatchesWave(wave) && monsters > 0;
		_triggered = false;
		_needed = rule.KillsNeeded(monsters);
		InstantKills = 0;
		Deaths = 0;
	}

	public bool OnKill(double secondsSinceFirstHit)
	{
		if (!_watching)
		{
			return false;
		}

		Deaths++;
		if (!_rule.IsInstantKill(secondsSinceFirstHit))
		{
			return false;
		}

		InstantKills++;
		if (_triggered || InstantKills < _needed)
		{
			return false;
		}

		_triggered = true;
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
