using HarmonyLib;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// A monster in camp that stands still, never fights back and never dies,
/// for trying out memories and essences.
/// </summary>
/// <remarks>
/// <para>
/// The game has no training dummy, so this is an ordinary monster
/// (<c>dummyMonster</c>, a Treant by default) made harmless on the host.
/// Everything below is either server-side logic or a stock networked effect,
/// so vanilla clients see a normal monster with a normal health bar and damage
/// numbers:
/// </para>
/// <list type="bullet">
/// <item><description><b>No AI.</b> <c>EntityAI.disableAI</c>: the AI runs only on
/// the server, so it never walks, turns to a target or attacks.</description></item>
/// <item><description><b>Never dies.</b> A <c>DeathInterruptEffect</c> in the stock
/// <c>Se_GenericEffectContainer</c> (<c>Actor.CreateBasicEffect</c>), the same
/// mechanism that knocks heroes out instead of killing them. A killing blow
/// sets it back to full health. Damage that skips death interrupts still kills
/// it, and it is respawned within a second.</description></item>
/// <item><description><b>Never moves.</b> Knockbacks and pulls are displacements,
/// canceled by <c>Patches.TrainingDummyDisplacementPatch</c>. That is used
/// rather than the unstoppable effect because unstoppable also strips stuns,
/// slows and roots, which are worth testing too. Anything else that shifts it
/// (body blocking, a swap) is undone by teleporting it home.</description></item>
/// <item><description><b>Resets.</b> Like a practice dummy, it heals to full after
/// a few seconds without being hit.</description></item>
/// </list>
/// <para>
/// It stands in camp just outside the remnant ring, off to one side of the
/// opening so it does not block the way to the rift. Leaving camp destroys it
/// with the rest of the room; coming back spawns a new one.
/// </para>
/// </remarks>
internal static class TrainingDummy
{
	private const string LifeEffectId = ModIdentity.Tag + "_DummyLife";

	/// <summary>How far from the rift the dummy stands; the ring is at most 7 m.</summary>
	private static readonly float[] Distances = { 11f, 12.5f, 10f, 14f };

	/// <summary>Degrees off the opening, so it stands beside the way in, not in it.</summary>
	private static readonly float[] Angles = { 40f, -40f, 60f, -60f, 25f, -25f, 0f };

	private const float SpawnRetryInterval = 1f;
	private const float MaintainInterval = 0.2f;
	private const float HomeTolerance = 0.5f;
	private const float ResetAfterSeconds = 3f;

	private static Entity _dummy;
	private static Vector3 _home;
	private static float _nextSpawnTime;
	private static float _nextMaintainTime;
	private static float _lastHitTime;
	private static float _lastHealth;
	private static string _warnedMonster;

	public static bool IsDummy(Entity entity)
	{
		return entity != null && _dummy != null && entity == _dummy;
	}

	public static void Tick(DreamArenaConfig config, PhaseKind phase)
	{
		if (!NetworkServer.active || config == null || !config.arenaEnabled)
		{
			return;
		}

		if (phase != PhaseKind.Camp || !config.trainingDummy)
		{
			// Leaving camp destroys it with the room; turning it off should too.
			Despawn();
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.isInAnyTransition)
		{
			return;
		}

		if (_dummy.IsNullOrInactive() || _dummy.Status.isDead)
		{
			_dummy = null;
			if (Time.unscaledTime >= _nextSpawnTime)
			{
				_nextSpawnTime = Time.unscaledTime + SpawnRetryInterval;
				Spawn(config);
			}

			return;
		}

		if (Time.unscaledTime >= _nextMaintainTime)
		{
			_nextMaintainTime = Time.unscaledTime + MaintainInterval;
			Maintain();
		}
	}

	private static void Spawn(DreamArenaConfig config)
	{
		Entity prefab = DewResources.GetByShortTypeName<Entity>(config.dummyMonster);
		if (prefab == null)
		{
			if (_warnedMonster != config.dummyMonster)
			{
				_warnedMonster = config.dummyMonster;
				Debug.LogWarning($"{ModIdentity.LogPrefix}training dummy '{config.dummyMonster}' " +
				                 "not found; no dummy.");
			}

			return;
		}

		if (!TryFindHome(out Vector3 home, out Quaternion facing))
		{
			return;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		int level = game != null ? Mathf.Max(1, game.ambientLevel) : 1;

		Entity dummy = Dew.SpawnEntity(prefab, home, facing,
			actors != null ? actors.serverActor : null, DewPlayer.creep, level,
			entity =>
			{
				// No rise-from-the-ground intro; it should just be standing there.
				entity.Visual.NetworkskipSpawning = true;
				if (entity is Monster monster)
				{
					monster.disableLoot = true;
				}

				if (entity.AI != null)
				{
					entity.AI.disableAI = true;
				}
			});

		if (dummy == null)
		{
			return;
		}

		dummy.name = "Training Dummy" + ModIdentity.OwnedSuffix;
		_dummy = dummy;
		_home = home;
		_lastHealth = dummy.currentHealth;
		_lastHitTime = Time.unscaledTime;
		Maintain();

		Debug.Log($"{ModIdentity.LogPrefix}training dummy ({config.dummyMonster}) placed at {home}.");
	}

	/// <summary>
	/// Beside the ring's opening, on walkable ground. Faces the rift, so it
	/// looks toward players coming in.
	/// </summary>
	private static bool TryFindHome(out Vector3 home, out Quaternion facing)
	{
		home = default;
		facing = Quaternion.identity;

		if (!CampShops.TryGetCampLayout(out Vector3 center, out Vector3 openingDir))
		{
			return false;
		}

		float baseAngle = Mathf.Atan2(openingDir.x, openingDir.z) * Mathf.Rad2Deg;
		foreach (float distance in Distances)
		{
			foreach (float offset in Angles)
			{
				float a = (baseAngle + offset) * Mathf.Deg2Rad;
				Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
				if (!CampShops.TryGroundSlot(center + dir * distance, out Vector3 spot))
				{
					continue;
				}

				home = spot;
				facing = Quaternion.LookRotation(-dir, Vector3.up);
				return true;
			}
		}

		return false;
	}

	private static void Maintain()
	{
		Entity dummy = _dummy;
		if (dummy.IsNullOrInactive())
		{
			return;
		}

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
				// Last in line, after anything the monster brings with it.
				priority = int.MaxValue,
			}, float.PositiveInfinity, LifeEffectId);
		}

		Vector3 offset = dummy.agentPosition - _home;
		offset.y = 0f;
		if (!dummy.Control.isDisplacing && offset.sqrMagnitude > HomeTolerance * HomeTolerance)
		{
			dummy.Control.Teleport(_home);
		}

		float health = dummy.currentHealth;
		if (health < _lastHealth - 0.01f)
		{
			_lastHitTime = Time.unscaledTime;
		}
		else if (health < dummy.maxHealth
		         && Time.unscaledTime - _lastHitTime > ResetAfterSeconds)
		{
			dummy.Status.SetHealth(dummy.maxHealth);
			health = dummy.maxHealth;
		}

		_lastHealth = health;
	}

	private static void OnKillingBlow(EventInfoKill kill)
	{
		Entity dummy = kill.victim;
		if (dummy.IsNullOrInactive())
		{
			return;
		}

		dummy.Status.SetHealth(dummy.maxHealth);
		_lastHealth = dummy.maxHealth;
		_lastHitTime = Time.unscaledTime;
	}

	private static void Despawn()
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

	public static void Reset()
	{
		Despawn();
		_nextSpawnTime = 0f;
		_nextMaintainTime = 0f;
		_warnedMonster = null;
	}
}
