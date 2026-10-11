using System;
using System.Collections.Generic;
using LastStand.Combat;

namespace LastStand.Waves;

internal sealed class HeroicFastKillWatch
{
	private readonly Queue<EnemyHit> _window = new Queue<EnemyHit>();
	private readonly Dictionary<uint, double> _maxHealthByBoss = new Dictionary<uint, double>();
	private double _windowSeconds;
	private double _windowDamage;
	private double _firstHitAt;
	private double _lastHitAt;

	public double Multiplier { get; private set; } = HeroicFastKillRule.NoChange;

	public bool Active { get; private set; }

	public double BestBurstDamage { get; private set; }

	public double BurstSeconds => Math.Min(_windowSeconds, Math.Max(0, _lastHitAt - _firstHitAt));

	public bool Measured => BestBurstDamage > 0 && _maxHealthByBoss.Count > 0;

	public double ProjectedSecondsPerBoss => Measured ? BurstSeconds * AverageMaxHealth() / BestBurstDamage : 0;

	public void Start(bool active, double windowSeconds)
	{
		Cancel();
		Active = active;
		_windowSeconds = windowSeconds;
	}

	public void OnHit(EnemyHit hit)
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
		_window.Enqueue(new EnemyHit(hit.Tier, hit.EnemyId, counted, hit.MaxHealth, hit.At));
		_windowDamage += counted;
		while (_window.Count > 0 && _window.Peek().At <= hit.At - _windowSeconds)
		{
			_windowDamage -= _window.Dequeue().Amount;
		}

		BestBurstDamage = Math.Max(BestBurstDamage, _windowDamage);
	}

	public void Raise(double step)
	{
		Multiplier *= step;
	}

	public void Cancel()
	{
		Active = false;
		_window.Clear();
		_maxHealthByBoss.Clear();
		_windowDamage = 0;
		BestBurstDamage = 0;
		_firstHitAt = 0;
		_lastHitAt = 0;
	}

	public void Reset(double multiplier)
	{
		Cancel();
		Multiplier = Math.Max(HeroicFastKillRule.NoChange, multiplier);
	}

	private double AverageMaxHealth()
	{
		double total = 0;
		foreach (double maxHealth in _maxHealthByBoss.Values)
		{
			total += maxHealth;
		}

		return total / _maxHealthByBoss.Count;
	}
}
