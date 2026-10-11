using System;
using System.Collections.Generic;
using LastStand.Combat;

namespace LastStand.Waves;

internal sealed class HeroicFastKillWatch
{
	private readonly Queue<WindowHit> _window = new Queue<WindowHit>();
	private readonly Dictionary<uint, double> _maxHealthByBoss = new Dictionary<uint, double>();
	private readonly Dictionary<string, double> _windowByPlayer = new Dictionary<string, double>();
	private Dictionary<string, double> _bestByPlayer = new Dictionary<string, double>();
	private double _windowSeconds;
	private double _windowDamage;
	private double _firstHitAt;
	private double _lastHitAt;

	public bool Active { get; private set; }

	public double BestBurstDamage { get; private set; }

	public double BurstSeconds => Math.Min(_windowSeconds, Math.Max(0, _lastHitAt - _firstHitAt));

	public bool Measured => BestBurstDamage > 0 && _maxHealthByBoss.Count > 0;

	public double ProjectedSeconds => Measured ? BurstSeconds * TotalMaxHealth() / BestBurstDamage : 0;

	public void Start(bool active, double windowSeconds)
	{
		Cancel();
		Active = active;
		_windowSeconds = windowSeconds;
	}

	public HeroicFastKillRecord Record()
	{
		return new HeroicFastKillRecord(ProjectedSeconds, new Dictionary<string, double>(_bestByPlayer));
	}

	public void OnHit(EnemyHit hit, string attacker)
	{
		if (!Active || hit.Tier != EnemyTier.HeroicBoss || hit.Amount <= 0)
		{
			return;
		}

		if (_maxHealthByBoss.Count == 0 && _window.Count == 0)
		{
			_firstHitAt = hit.At;
		}

		_lastHitAt = Math.Max(_lastHitAt, hit.At);
		_maxHealthByBoss[hit.EnemyId] = _maxHealthByBoss.TryGetValue(hit.EnemyId, out double known)
			? Math.Max(known, hit.MaxHealth)
			: hit.MaxHealth;

		double counted = Math.Min(hit.Amount, hit.MaxHealth);
		_window.Enqueue(new WindowHit(attacker, counted, hit.At));
		Count(attacker, counted);
		while (_window.Count > 0 && _window.Peek().At <= hit.At - _windowSeconds)
		{
			WindowHit old = _window.Dequeue();
			Count(old.Attacker, -old.Amount);
		}

		if (_windowDamage > BestBurstDamage)
		{
			BestBurstDamage = _windowDamage;
			_bestByPlayer = new Dictionary<string, double>(_windowByPlayer);
		}
	}

	public void Cancel()
	{
		Active = false;
		_window.Clear();
		_maxHealthByBoss.Clear();
		_windowByPlayer.Clear();
		_bestByPlayer = new Dictionary<string, double>();
		_windowDamage = 0;
		BestBurstDamage = 0;
		_firstHitAt = 0;
		_lastHitAt = 0;
	}

	private void Count(string attacker, double amount)
	{
		_windowDamage += amount;
		_windowByPlayer[attacker] = (_windowByPlayer.TryGetValue(attacker, out double sum) ? sum : 0) + amount;
	}

	private double TotalMaxHealth()
	{
		double total = 0;
		foreach (double maxHealth in _maxHealthByBoss.Values)
		{
			total += maxHealth;
		}

		return total;
	}

	private readonly struct WindowHit
	{
		public WindowHit(string attacker, double amount, double at)
		{
			Attacker = attacker;
			Amount = amount;
			At = at;
		}

		public string Attacker { get; }

		public double Amount { get; }

		public double At { get; }
	}
}
