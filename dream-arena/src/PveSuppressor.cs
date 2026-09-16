using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Strips the PvE layer out of the arena.
/// </summary>
/// <remarks>
/// <para>
/// Zeroing the monster population budget stops waves, but it does not touch
/// everything: room props are spawned by <c>RoomProps.OnRoomStartServer</c> on
/// a separate path, which is where wells, gold and dream-dust stones,
/// merchants and shrines come from. A boss room also places its boss and the
/// hazards it owns as part of the room itself, so those keep appearing even
/// with the population at zero.
/// </para>
/// <para>
/// Prop spawning is blocked at the source; anything that still makes it into
/// the arena is swept up here. Heroes, their summons and rifts are left alone.
/// </para>
/// </remarks>
internal static class PveSuppressor
{
	private const float SweepInterval = 1f;

	private static float _timer;

	private static readonly List<Entity> Doomed = new List<Entity>();

	/// <summary>
	/// True when PvE content should be kept out of the room the players are in.
	/// </summary>
	/// <remarks>
	/// The camp is left completely alone — it should still look and behave like
	/// a normal starting room.
	/// </remarks>
	public static bool ShouldSuppress(DreamArenaConfig config, PhaseKind phase)
	{
		return config != null && config.arenaEnabled && config.suppressMonsters
		       && phase == PhaseKind.Arena;
	}

	public static void Tick(float deltaTime, DreamArenaConfig config, PhaseKind phase)
	{
		if (!NetworkServer.active || !ShouldSuppress(config, phase))
		{
			return;
		}

		_timer += deltaTime;
		if (_timer < SweepInterval)
		{
			return;
		}

		_timer = 0f;
		Sweep();
	}

	/// <summary>
	/// Removes every monster, prop and loose pickup currently in the arena.
	/// </summary>
	public static int Sweep()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.instance;
		if (actors == null)
		{
			return 0;
		}

		Doomed.Clear();

		foreach (Entity entity in actors.allEntities)
		{
			if (entity.IsNullOrInactive() || !IsPve(entity))
			{
				continue;
			}

			Doomed.Add(entity);
		}

		foreach (Entity entity in Doomed)
		{
			// Destroy, not Kill: a kill would drop rewards and, for a boss, run
			// the defeat path and open the exit rift.
			entity.Destroy();
		}

		if (Doomed.Count > 0)
		{
			Debug.Log($"{ModIdentity.LogPrefix}swept {Doomed.Count} PvE object(s) from the arena.");
		}

		return Doomed.Count;
	}

	/// <summary>
	/// PvE content is monsters (bosses included, since <c>BossMonster</c>
	/// derives from <c>Monster</c>) and props such as stones, wells and
	/// merchants.
	/// </summary>
	private static bool IsPve(Entity entity)
	{
		if (entity is Hero || entity is Summon)
		{
			return false;
		}

		return entity is Monster || entity is PropEntity;
	}

	public static void Reset()
	{
		_timer = 0f;
	}
}
