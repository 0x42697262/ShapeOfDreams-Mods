using System.Collections.Generic;

namespace LastStand.Waves;

internal sealed class FastClearWatch
{
	private FastClearRule _rule;
	private bool _watching;
	private bool _triggered;
	private int _needed;
	private Dictionary<string, int> _instantKillsByPlayer = new Dictionary<string, int>();

	public int InstantKills { get; private set; }

	public int Deaths { get; private set; }

	public int Needed => _needed;

	public bool Missed => _watching && !_triggered;

	public void Start(int wave, int monsters, FastClearRule rule)
	{
		_rule = rule;
		_watching = rule.WatchesWave(wave) && monsters > 0;
		_triggered = false;
		_needed = rule.KillsNeeded(monsters);
		InstantKills = 0;
		Deaths = 0;
		_instantKillsByPlayer = new Dictionary<string, int>();
	}

	public FastClearRecord Record()
	{
		return new FastClearRecord(_needed, _instantKillsByPlayer);
	}

	public bool OnKill(double secondsSinceFirstHit, string killer)
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
		_instantKillsByPlayer[killer] = _instantKillsByPlayer.TryGetValue(killer, out int kills) ? kills + 1 : 1;
		if (_triggered || InstantKills < _needed)
		{
			return false;
		}

		_triggered = true;
		return true;
	}

	public void Cancel()
	{
		_watching = false;
	}
}
