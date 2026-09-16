using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Places players around the arena so a round does not open with everyone
/// stacked in one spot.
/// </summary>
/// <remarks>
/// No spawn immunity is granted: players arrive from the safety of the camp,
/// so the round only becomes hostile once they are already through the rift.
/// </remarks>
internal static class ArenaSpawn
{
	/// <summary>Fallback ring size when the arena does not declare one.</summary>
	private const float FallbackRadius = 10f;

	/// <summary>Keeps players off the arena's very edge.</summary>
	private const float RadiusUsage = 0.75f;

	public static void PlaceHeroes(DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null)
		{
			return;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.instance;
		if (actors == null)
		{
			return;
		}

		List<Hero> heroes = new List<Hero>();
		foreach (Hero hero in actors.allHeroes)
		{
			if (!hero.IsNullInactiveDeadOrKnockedOut())
			{
				heroes.Add(hero);
			}
		}

		if (heroes.Count == 0)
		{
			return;
		}

		if (config.spreadSpawns)
		{
			Spread(heroes);
		}
	}

	/// <summary>
	/// Distributes heroes evenly around a ring so nobody starts in anyone's
	/// attack range.
	/// </summary>
	private static void Spread(List<Hero> heroes)
	{
		if (!TryGetArena(heroes, out Vector3 center, out float radius))
		{
			return;
		}

		// One player has no one to be spread away from.
		if (heroes.Count < 2)
		{
			return;
		}

		float ring = Mathf.Max(1f, radius * RadiusUsage);
		float step = 360f / heroes.Count;

		for (int i = 0; i < heroes.Count; i++)
		{
			Hero hero = heroes[i];

			Vector3 offset = Quaternion.Euler(0f, step * i, 0f) * Vector3.forward * ring;

			// Sweep from the centre so the point always lands on walkable ground.
			Vector3 target = Dew.GetValidAgentDestination_LinearSweep(center, center + offset);

			hero.Control.CancelOngoingChannels();
			hero.Control.CancelOngoingDisplacement();
			hero.Control.Teleport(target);

			foreach (Summon summon in hero.summons)
			{
				summon.Control.Teleport(target + Random.onUnitSphere.Flattened());
			}
		}

		Debug.Log($"{ModIdentity.LogPrefix}spread {heroes.Count} players around the arena " +
		          $"(radius {ring:0.#}).");
	}

	/// <summary>
	/// Finds the arena's centre and size.
	/// </summary>
	/// <remarks>
	/// <c>Room_BossArena</c> is the marker the boss rooms carry, exposing the
	/// centre and radius of the fighting area. Without it, fall back to the
	/// heroes' own midpoint so the spread still does something sensible.
	/// </remarks>
	private static bool TryGetArena(List<Hero> heroes, out Vector3 center, out float radius)
	{
		Room_BossArena arena = SingletonBehaviour<Room_BossArena>.instance;
		if (arena != null)
		{
			center = arena.center;
			radius = arena.radius;
			return true;
		}

		center = Vector3.zero;
		foreach (Hero hero in heroes)
		{
			center += hero.agentPosition;
		}

		center /= heroes.Count;
		radius = FallbackRadius;
		return true;
	}

}
