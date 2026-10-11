using DreamArena.Config;
using DreamArena.Text;
using Mirror;
using UnityEngine;

namespace DreamArena.Camp;

internal sealed class TrainingDummy
{
	private Entity _dummy;
	private Vector3 _home;
	private double _nextSpawnAt;
	private double _nextMaintainAt;
	private double _lastHitAt;
	private float _lastHealth;
	private string _warnedMonster;

	public static bool IsDummy(Entity entity)
	{
		return entity != null && entity.name == ModIdentity.DummyName;
	}

	public void Tick(double now, SessionSettings settings)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.isInAnyTransition)
		{
			return;
		}

		if (_dummy.IsNullOrInactive() || _dummy.Status.isDead)
		{
			_dummy = null;
			if (now >= _nextSpawnAt)
			{
				_nextSpawnAt = now + Tuning.DummySpawnRetrySeconds;
				_dummy = FindLeftover() ?? Spawn(now, settings.DummyMonster);
				_lastHitAt = now;
			}

			return;
		}

		if (now >= _nextMaintainAt)
		{
			_nextMaintainAt = now + Tuning.DummyMaintainSeconds;
			Maintain(now);
		}
	}

	public void Despawn()
	{
		if (_dummy == null)
		{
			return;
		}

		if (!_dummy.IsNullOrInactive())
		{
			_dummy.Destroy();
		}

		_dummy = null;
	}

	public void Reset()
	{
		if (NetworkServer.active)
		{
			_dummy = _dummy ?? FindLeftover();
			Despawn();
		}

		_dummy = null;
		_nextSpawnAt = 0;
		_nextMaintainAt = 0;
		_warnedMonster = null;
	}

	private static Entity FindLeftover()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return null;
		}

		foreach (Entity entity in actors.allEntities)
		{
			if (!entity.IsNullOrInactive() && IsDummy(entity))
			{
				return entity;
			}
		}

		return null;
	}

	private Entity Spawn(double now, string monster)
	{
		Entity prefab = DewResources.GetByShortTypeName<Entity>(monster);
		if (prefab == null)
		{
			if (_warnedMonster != monster)
			{
				_warnedMonster = monster;
				Log.Warn(LogText.DummyMonsterMissing(monster));
			}

			return null;
		}

		if (!TryFindHome(out Vector3 home, out Quaternion facing))
		{
			return null;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		int level = game != null ? Mathf.Max(Tuning.MinDummyLevel, game.ambientLevel) : Tuning.MinDummyLevel;
		Entity dummy = Dew.SpawnEntity(prefab, home, facing, actors != null ? actors.serverActor : null, DewPlayer.creep,
			level, Prepare);
		if (dummy == null)
		{
			return null;
		}

		dummy.name = ModIdentity.DummyName;
		_home = home;
		_lastHealth = dummy.currentHealth;
		_lastHitAt = now;
		Log.Info(LogText.DummyPlaced(monster, home));
		return dummy;
	}

	private static void Prepare(Entity entity)
	{
		entity.Visual.NetworkskipSpawning = true;
		if (entity is Monster monster)
		{
			monster.disableLoot = true;
		}

		if (entity.AI != null)
		{
			entity.AI.disableAI = true;
		}
	}

	private static bool TryFindHome(out Vector3 home, out Quaternion facing)
	{
		home = default;
		facing = Quaternion.identity;
		if (!CampLayout.TryFind(out Vector3 center, out Vector3 opening))
		{
			return false;
		}

		float baseAngle = CampLayout.Angle(opening);
		foreach (float distance in Tuning.DummyDistances)
		{
			foreach (float offset in Tuning.DummyAngles)
			{
				Vector3 direction = CampLayout.Direction(baseAngle + offset);
				if (CampLayout.TryGroundSlot(center + direction * distance, out home))
				{
					facing = Quaternion.LookRotation(-direction, Vector3.up);
					return true;
				}
			}
		}

		return false;
	}

	private void Maintain(double now)
	{
		Entity dummy = _dummy;
		if (dummy.AI != null && !dummy.AI.disableAI)
		{
			dummy.AI.disableAI = true;
		}

		if (!dummy.Status.hasDeathInterrupt)
		{
			Actor source = NetworkedManagerBase<ActorManager>.softInstance?.serverActor;
			(source != null ? source : dummy).CreateBasicEffect(dummy, new DeathInterruptEffect
			{
				onInterrupt = OnKillingBlow,
				priority = int.MaxValue,
			}, float.PositiveInfinity, ModIdentity.DummyLifeEffectId);
		}

		if (_home == default)
		{
			_home = dummy.agentPosition;
		}

		Vector3 offset = dummy.agentPosition - _home;
		offset.y = 0f;
		if (!dummy.Control.isDisplacing && offset.sqrMagnitude > Tuning.DummyHomeTolerance * Tuning.DummyHomeTolerance)
		{
			dummy.Control.Teleport(_home);
		}

		float health = dummy.currentHealth;
		if (health < _lastHealth - Tuning.HealthChangeEpsilon)
		{
			_lastHitAt = now;
		}
		else if (health < dummy.maxHealth && now - _lastHitAt > Tuning.DummyResetSeconds)
		{
			dummy.Status.SetHealth(dummy.maxHealth);
			health = dummy.maxHealth;
		}

		_lastHealth = health;
	}

	private static void OnKillingBlow(EventInfoKill kill)
	{
		Entity dummy = kill.victim;
		if (!dummy.IsNullOrInactive())
		{
			dummy.Status.SetHealth(dummy.maxHealth);
		}
	}
}
