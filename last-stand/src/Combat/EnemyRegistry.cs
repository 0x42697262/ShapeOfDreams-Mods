using System;
using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Combat;

internal sealed class EnemyRegistry
{
	private readonly Dictionary<Entity, EnemyTier> _alive = new Dictionary<Entity, EnemyTier>();
	private readonly List<Entity> _stale = new List<Entity>();
	private readonly List<Entity> _ofTier = new List<Entity>();
	private readonly List<double> _reservations = new List<double>();

	public event Action<EnemyDeath> Died;

	public int Occupied(double now)
	{
		Prune(now);
		return _alive.Count + _reservations.Count;
	}

	public void Reserve(double now)
	{
		_reservations.Add(now + Tuning.SpawnReservationSeconds);
	}

	public void Track(Entity entity, EnemyTier tier)
	{
		if (_reservations.Count > 0)
		{
			_reservations.RemoveAt(0);
		}

		Adopt(entity, tier);
	}

	public void Adopt(Entity entity, EnemyTier tier)
	{
		if (entity == null || _alive.ContainsKey(entity))
		{
			return;
		}

		_alive[entity] = tier;
		entity.EntityEvent_OnDeath += (Action<EventInfoKill>)(kill => OnDeath(entity, tier, kill));
	}

	public int CountAlive(EnemyTier tier)
	{
		return AliveOf(tier).Count;
	}

	public IReadOnlyList<Entity> AliveOf(EnemyTier tier)
	{
		_ofTier.Clear();
		foreach (KeyValuePair<Entity, EnemyTier> entry in _alive)
		{
			if (entry.Value == tier && !entry.Key.IsNullInactiveDeadOrKnockedOut())
			{
				_ofTier.Add(entry.Key);
			}
		}

		return _ofTier;
	}

	public void Clear()
	{
		_alive.Clear();
		_stale.Clear();
		_ofTier.Clear();
		_reservations.Clear();
	}

	private void OnDeath(Entity entity, EnemyTier tier, EventInfoKill kill)
	{
		if (!_alive.Remove(entity))
		{
			return;
		}

		DewPlayer killer = kill.actor != null && kill.actor.firstEntity != null
			? kill.actor.firstEntity.owner
			: null;

		Died?.Invoke(new EnemyDeath(tier, entity.agentPosition, killer));
	}

	private void Prune(double now)
	{
		_reservations.RemoveAll(expiresAt => expiresAt <= now);

		_stale.Clear();
		foreach (Entity entity in _alive.Keys)
		{
			if (entity.IsNullInactiveDeadOrKnockedOut())
			{
				_stale.Add(entity);
			}
		}

		foreach (Entity entity in _stale)
		{
			_alive.Remove(entity);
		}
	}
}
