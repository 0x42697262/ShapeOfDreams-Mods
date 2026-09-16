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

	public static void PlaceHeroes(DreamArenaConfig config, bool force = false)
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

		if (force || config.spreadSpawns)
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

		// One player still gets moved into the arena; there is just nobody to
		// be spread away from.

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
	/// <para>
	/// Boss rooms are not consistent about this, so three anchors are tried in
	/// order of how well they describe the fighting area:
	/// </para>
	/// <list type="number">
	/// <item><c>Room_BossArena</c> — the explicit marker, giving a real centre
	/// and radius. Only some rooms carry it.</item>
	/// <item><c>DewHeroesTeleporter.destination</c> — where the game itself
	/// drops heroes when a fight starts, so it is inside the arena by
	/// definition even when the marker is absent.</item>
	/// <item>The heroes' own midpoint — last resort, which only spreads them
	/// apart rather than moving them anywhere meaningful.</item>
	/// </list>
	/// </remarks>
	public static bool TryGetArena(List<Hero> heroes, out Vector3 center, out float radius)
	{
		Room_BossArena arena = SingletonBehaviour<Room_BossArena>.instance;
		if (arena != null)
		{
			center = arena.center;
			radius = arena.radius;
			return true;
		}

		DewHeroesTeleporter teleporter = Object.FindAnyObjectByType<DewHeroesTeleporter>();
		if (teleporter != null && teleporter.destination != null)
		{
			center = Dew.GetPositionOnGround(teleporter.destination.position);
			radius = FallbackRadius;
			return true;
		}

		if (heroes == null || heroes.Count == 0)
		{
			center = Vector3.zero;
			radius = FallbackRadius;
			return false;
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

	/// <summary>True when a real arena anchor exists, as opposed to only the
	/// heroes' own midpoint.</summary>
	public static bool HasArenaAnchor()
	{
		if (SingletonBehaviour<Room_BossArena>.instance != null)
		{
			return true;
		}

		DewHeroesTeleporter teleporter = Object.FindAnyObjectByType<DewHeroesTeleporter>();
		return teleporter != null && teleporter.destination != null;
	}

}
