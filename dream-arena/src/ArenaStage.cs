using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Gets everyone into the fighting area as soon as they arrive.
/// </summary>
/// <remarks>
/// <para>
/// A boss room is two spaces: an antechamber you land in, and the arena proper,
/// which the game only puts you in once every hero stands on the entry trigger.
/// Left alone, players arrive in the antechamber with PvP already live and
/// fight in the corridor instead of the arena. The distance varies by room —
/// <c>Room_SnowMountain_Boss_0</c> has a notably long approach.
/// </para>
/// <para>
/// So arrival is staged directly: on entering an arena room, everyone is placed
/// inside the arena straight away. Placement is anchored on
/// <c>Room_BossArena</c>, the marker component the boss rooms carry, rather
/// than on the entry teleporter — its destination is the arena's edge, and not
/// every room is guaranteed to wire one up.
/// </para>
/// </remarks>
internal static class ArenaStage
{
	/// <summary>Node the players were last staged into, so it happens once per arrival.</summary>
	private static int _stagedNode = -1;

	/// <summary>Seconds between staging attempts while the room is not ready.</summary>
	/// <remarks>
	/// Each attempt looks for the arena anchor with whole-scene object searches.
	/// Doing that every frame in a room that never yields one was a steady drain
	/// on the host, and so on every client.
	/// </remarks>
	private const float RetryInterval = 0.25f;

	private static float _nextAttemptTime;

	public static void Tick(DreamArenaConfig config, PhaseKind phase)
	{
		if (!NetworkServer.active || config == null || !config.arenaEnabled)
		{
			return;
		}

		if (phase != PhaseKind.Arena)
		{
			// Back in camp; stage again next time they come through.
			_stagedNode = -1;
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.isInAnyTransition)
		{
			return;
		}

		if (zones.currentNodeIndex == _stagedNode)
		{
			return;
		}

		if (Time.unscaledTime < _nextAttemptTime)
		{
			return;
		}

		_nextAttemptTime = Time.unscaledTime + RetryInterval;

		if (!TryStage(config))
		{
			// Room or heroes not ready yet; try again shortly.
			return;
		}

		_stagedNode = zones.currentNodeIndex;
	}

	/// <summary>
	/// Places everyone in the arena. Returns false while the room is still
	/// settling.
	/// </summary>
	private static bool TryStage(DreamArenaConfig config)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null || actors.allHeroes.Count == 0)
		{
			return false;
		}

		// Only stage once somewhere real to stand has been identified; the
		// hero-midpoint fallback would just shuffle people around the
		// antechamber they are trying to leave.
		if (!ArenaSpawn.HasArenaAnchor())
		{
			return false;
		}

		PveSuppressor.Sweep();
		ArenaSpawn.PlaceHeroes(config, force: true);

		Debug.Log($"{ModIdentity.LogPrefix}staged {actors.allHeroes.Count} players into the arena.");
		return true;
	}

	/// <summary>
	/// Re-stages on demand, for restarting a round without leaving the room.
	/// </summary>
	public static bool StartFight(DreamArenaConfig config)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}only the host can start the arena.");
			return false;
		}

		if (!ArenaSpawn.HasArenaAnchor())
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no arena anchor in this room. " +
			                 "Take the rift out of camp first.");
			return false;
		}

		PveSuppressor.Sweep();
		ArenaSpawn.PlaceHeroes(config, force: true);
		return true;
	}

	public static void Reset()
	{
		_stagedNode = -1;
		_nextAttemptTime = 0f;
	}
}
