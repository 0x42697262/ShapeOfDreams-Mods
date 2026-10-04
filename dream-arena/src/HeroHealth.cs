using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Multiplies every hero's maximum health by a configurable factor.
/// </summary>
/// <remarks>
/// <para>
/// Bigger health pools mean a match is decided by play rather than by who lands
/// the first burst.
/// </para>
/// <para>
/// Max health is worked out on the server in <c>EntityStatus.CalculateStats</c>
/// and replicated as the <c>finalStats</c> SyncVar, so a host-side change shows
/// up on unmodded clients, their health bars included.
/// </para>
/// <para>
/// The change is a <c>finalStatsProcessors</c> entry, which runs at the very end
/// of the stat calculation, after level scaling and every item bonus. That
/// makes it a true multiplier. Adding a percentage stat bonus instead would not
/// be: bonus percentages are summed and applied once, so the mod's share would
/// dilute whatever the player built.
/// </para>
/// <para>
/// <c>CalculateStats</c> keeps each hero's current/max health fraction across a
/// recalculation, so applying or removing the multiplier scales current health
/// with the maximum rather than leaving someone at a sliver or overfull.
/// </para>
/// </remarks>
internal static class HeroHealth
{
	/// <summary>Seconds between passes. New heroes only need picking up occasionally.</summary>
	private const float Interval = 0.5f;

	private static float _timer;
	private static float _appliedMultiplier = 1f;

	/// <summary>Heroes currently carrying the processor, so it is added once each.</summary>
	private static readonly HashSet<EntityStatus> Boosted = new HashSet<EntityStatus>();

	private static readonly List<EntityStatus> Stale = new List<EntityStatus>();

	public static void Tick(float deltaTime, DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null)
		{
			return;
		}

		float target = Multiplier(config);

		// A changed factor means every boosted hero must be redone.
		if (!Mathf.Approximately(target, _appliedMultiplier))
		{
			RemoveAll();
			_appliedMultiplier = target;
		}

		if (target <= 1f)
		{
			// Nothing to do, and anything previously boosted was just cleared.
			return;
		}

		_timer += deltaTime;
		if (_timer < Interval)
		{
			return;
		}

		_timer = 0f;

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		// Drop heroes that have gone away, so the set does not leak or re-add
		// the processor to a destroyed status.
		Stale.Clear();
		foreach (EntityStatus status in Boosted)
		{
			if (status == null || status.entity.IsNullOrInactive())
			{
				Stale.Add(status);
			}
		}
		foreach (EntityStatus status in Stale)
		{
			Boosted.Remove(status);
		}

		foreach (Hero hero in actors.allHeroes)
		{
			if (hero.IsNullOrInactive() || hero.Status == null)
			{
				continue;
			}

			if (Boosted.Add(hero.Status))
			{
				hero.Status.finalStatsProcessors.Add(Process);
				Debug.Log($"{ModIdentity.LogPrefix}health x{target:0.##} applied to {hero.name}.");
			}
		}
	}

	/// <summary>The multiplier in effect, or 1 when the feature is off.</summary>
	private static float Multiplier(DreamArenaConfig config)
	{
		if (!config.arenaEnabled || config.heroHealthMultiplier <= 1f)
		{
			return 1f;
		}

		return config.heroHealthMultiplier;
	}

	/// <summary>Runs last in the stat pipeline, so it scales the finished total.</summary>
	private static void Process(ref FinalStats data)
	{
		data.maxHealth *= _appliedMultiplier;
	}

	/// <summary>Takes the processor back off every hero and forgets them.</summary>
	public static void RemoveAll()
	{
		foreach (EntityStatus status in Boosted)
		{
			if (status != null)
			{
				status.finalStatsProcessors.Remove(Process);
			}
		}

		Boosted.Clear();
	}

	public static void Reset()
	{
		RemoveAll();
		_timer = 0f;
		_appliedMultiplier = 1f;
	}
}
