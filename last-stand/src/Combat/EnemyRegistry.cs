using System;
using System.Collections.Generic;
using LastStand.Config;
using UnityEngine;

namespace LastStand.Combat;

internal sealed class EnemyRegistry
{
	private readonly Dictionary<Entity, EnemyTier> _alive = new Dictionary<Entity, EnemyTier>();
	private readonly Dictionary<Entity, float> _firstHitAt = new Dictionary<Entity, float>();
	private readonly List<Entity> _stale = new List<Entity>();
	private readonly List<Entity> _ofTier = new List<Entity>();
	private readonly List<double> _reservations = new List<double>();

	public event Action<EnemyDeath> Died;

	public event Action<EnemyHit> Hit;

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
		entity.EntityEvent_OnTakeDamage += (Action<EventInfoDamage>)(info => OnDamaged(entity, tier, info));
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

	public int RemoveAll()
	{
		int removed = 0;
		foreach (Entity entity in new List<Entity>(_alive.Keys))
		{
			if (!entity.IsNullOrInactive())
			{
				entity.Destroy();
				removed++;
			}
		}

		Clear();
		return removed;
	}

	public void Clear()
	{
		_alive.Clear();
		_firstHitAt.Clear();
		_stale.Clear();
		_ofTier.Clear();
		_reservations.Clear();
	}

	private void OnDamaged(Entity entity, EnemyTier tier, EventInfoDamage info)
	{
		float now = Time.time;
		if (_alive.ContainsKey(entity) && !_firstHitAt.ContainsKey(entity))
		{
			_firstHitAt[entity] = now;
		}

		Hit?.Invoke(new EnemyHit(tier, entity.netId, info.damage.amount, entity.maxHealth, now, OwnerOf(info.actor)));
	}

	private void OnDeath(Entity entity, EnemyTier tier, EventInfoKill kill)
	{
		if (!_alive.Remove(entity))
		{
			return;
		}

		double sinceFirstHit = _firstHitAt.TryGetValue(entity, out float firstHitAt)
			? Math.Max(0, Time.time - firstHitAt)
			: 0;
		_firstHitAt.Remove(entity);

		Died?.Invoke(new EnemyDeath(tier, entity.agentPosition, OwnerOf(kill.actor), sinceFirstHit));
	}

	private static DewPlayer OwnerOf(Actor actor)
	{
		return actor != null && actor.firstEntity != null ? actor.firstEntity.owner : null;
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
			_firstHitAt.Remove(entity);
		}
	}
}
