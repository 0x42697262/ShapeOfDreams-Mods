using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Puts the players inside the boss arena and keeps the boss out of it.
/// </summary>
/// <remarks>
/// <para>
/// A boss room has two states. You arrive in an antechamber, and when every
/// hero stands in the entry zone the fight "triggers": a cutscene plays and
/// <c>DewHeroesTeleporter.TeleportAllHeroes()</c> drops everyone into the
/// sealed arena proper. That triggered state is the PvP arena we want.
/// </para>
/// <para>
/// The wiring is <c>DewBossRoomEntry</c>, which requires a
/// <c>DewAllHeroesPresentZone</c> and a <c>DewHeroesTeleporter</c>. Walking
/// into the entry zone together still works normally; <see cref="StartFight"/>
/// just calls the same public teleport directly so a match can be started
/// without herding everyone onto the trigger.
/// </para>
/// <para>
/// Server-side only. The teleport and the despawn both replicate, so clients
/// need no mod.
/// </para>
/// </remarks>
internal static class ArenaStage
{
	/// <summary>How often to look for a boss that needs removing.</summary>
	private const float SweepInterval = 1f;

	private static float _sweepTimer;

	/// <summary>
	/// Removes the room's boss if one exists.
	/// </summary>
	/// <remarks>
	/// Suppressing the monster population budget should stop most spawns, but a
	/// boss room may place its boss through room logic rather than the
	/// population system, so this sweeps for it explicitly.
	///
	/// <c>Destroy()</c> is used rather than <c>Kill()</c>: killing a boss would
	/// run the defeat path, spawn rewards and open the exit rift.
	/// </remarks>
	public static bool RemoveBoss()
	{
		if (!NetworkServer.active)
		{
			return false;
		}

		BossMonster boss = Dew.FindActorOfType<BossMonster>();
		if (boss.IsNullOrInactive())
		{
			return false;
		}

		string name = boss.name;
		boss.Destroy();

		Debug.Log($"{ModIdentity.LogPrefix}removed boss '{name}' from the arena.");
		return true;
	}

	/// <summary>Periodic boss sweep, driven from the mod's Update.</summary>
	public static void Tick(float deltaTime, DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null
		    || !config.arenaEnabled || !config.removeBoss)
		{
			return;
		}

		_sweepTimer += deltaTime;
		if (_sweepTimer < SweepInterval)
		{
			return;
		}

		_sweepTimer = 0f;
		RemoveBoss();
	}

	/// <summary>
	/// Teleports every hero into the arena, the same move the boss-fight
	/// trigger performs.
	/// </summary>
	public static bool StartFight()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}only the host can start the arena.");
			return false;
		}

		DewBossRoomEntry entry = Object.FindAnyObjectByType<DewBossRoomEntry>();
		if (entry == null)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no boss room entry here. " +
			                 "Travel to the arena room first.");
			return false;
		}

		DewHeroesTeleporter teleporter = entry.GetComponent<DewHeroesTeleporter>();
		if (teleporter == null)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}boss room entry has no teleporter.");
			return false;
		}

		RemoveBoss();
		teleporter.TeleportAllHeroes();

		Debug.Log($"{ModIdentity.LogPrefix}teleported all heroes into the arena.");
		return true;
	}

	public static void Reset()
	{
		_sweepTimer = 0f;
	}
}
