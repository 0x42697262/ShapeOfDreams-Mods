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
		if (!TryGetArena(heroes, out Vector3 center, out float radius, out string source))
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no arena anchor found in this room; " +
			                 "players left where they are.");
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

		Debug.Log($"{ModIdentity.LogPrefix}placed {heroes.Count} player(s) around the arena " +
		          $"via {source} at {center} (radius {ring:0.#}).");
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
	/// <item>The room's largest <c>RoomSection</c>. Boss rooms are built as an
	/// approach plus a big open fighting space, so the widest section is the
	/// arena. <c>pathablePivot</c> gives a centre that is known to be walkable,
	/// and the radius follows from the section's area.</item>
	/// <item>The heroes' own midpoint — last resort, which only spreads them
	/// apart rather than moving them anywhere meaningful.</item>
	/// </list>
	/// </remarks>
	public static bool TryGetArena(List<Hero> heroes, out Vector3 center, out float radius)
	{
		return TryGetArena(heroes, out center, out radius, out string _);
	}

	public static bool TryGetArena(List<Hero> heroes, out Vector3 center, out float radius,
		out string source)
	{
		source = "none";

		Room_BossArena arena = SingletonBehaviour<Room_BossArena>.instance;
		if (arena != null)
		{
			center = arena.center;
			radius = arena.radius;
			source = "Room_BossArena";
			return true;
		}

		DewHeroesTeleporter teleporter = Object.FindAnyObjectByType<DewHeroesTeleporter>();
		if (teleporter != null && teleporter.destination != null)
		{
			center = Dew.GetPositionOnGround(teleporter.destination.position);
			radius = FallbackRadius;
			source = "DewHeroesTeleporter";
			return true;
		}

		if (TryGetLargestSection(out center, out radius))
		{
			source = "largest RoomSection";
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
		source = "hero midpoint";
		return true;
	}

	/// <summary>
	/// The room's biggest open space, used when nothing marks the arena
	/// explicitly.
	/// </summary>
	/// <remarks>
	/// Most boss rooms are an approach corridor feeding a large fighting space,
	/// so the section with the greatest <c>area</c> is the arena. Its
	/// <c>pathablePivot</c> is a centre already known to be walkable, and the
	/// ring radius is derived from the area of a circle of that size, kept
	/// modest so players are not pushed into the walls.
	/// </remarks>
	private static bool TryGetLargestSection(out Vector3 center, out float radius)
	{
		center = Vector3.zero;
		radius = FallbackRadius;

		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || room.sections == null || room.sections.Count == 0)
		{
			return false;
		}

		RoomSection biggest = null;
		foreach (RoomSection section in room.sections)
		{
			if (section != null && (biggest == null || section.area > biggest.area))
			{
				biggest = section;
			}
		}

		if (biggest == null || biggest.area <= 0f)
		{
			return false;
		}

		center = Dew.GetValidAgentPosition(biggest.pathablePivot);
		radius = Mathf.Clamp(Mathf.Sqrt(biggest.area / Mathf.PI), 4f, 18f);
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
		if (teleporter != null && teleporter.destination != null)
		{
			return true;
		}

		return TryGetLargestSection(out Vector3 _, out float _);
	}

}
