using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Which part of the run the players are currently in.
/// </summary>
internal enum PhaseKind
{
	/// <summary>Not in a run, or PvP mode is off.</summary>
	None,

	/// <summary>The starting camp. A safe area; everyone is friendly here.</summary>
	Camp,

	/// <summary>Through the rift. Hostilities apply.</summary>
	Arena,
}

/// <summary>
/// Switches hostility on and off according to where the players are.
/// </summary>
/// <remarks>
/// <para>
/// The run begins at the camp — world generation always makes node 0
/// <c>WorldNodeType.Start</c> and gives it a room from the zone's
/// <c>startRooms</c>, which is the campfire you normally open a run in. Every
/// other node is redirected to the arena, so taking the rift out of camp is
/// what starts a match.
/// </para>
/// <para>
/// Players are allies in camp regardless of the configured team mode, so it
/// works as a lobby between rounds: pick builds, regroup, then head through
/// the rift. Coming back to camp disarms PvP again.
/// </para>
/// <para>
/// This is polled rather than hooked to a transition event: the node index is
/// a single synced int, so watching it costs nothing and avoids depending on
/// the exact ordering of the travel pipeline.
/// </para>
/// </remarks>
internal sealed class ArenaPhase
{
	private PhaseKind _current = PhaseKind.None;
	private int _lastNodeIndex = int.MinValue;

	public PhaseKind Current => _current;

	/// <summary>Re-evaluates the phase, applying relations when it changes.</summary>
	public void Tick(DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null)
		{
			return;
		}

		if (!config.arenaEnabled)
		{
			if (_current != PhaseKind.None)
			{
				_current = PhaseKind.None;
				_lastNodeIndex = int.MinValue;
			}

			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.instance;
		if (zones == null)
		{
			return;
		}

		int index = zones.currentNodeIndex;
		if (index == _lastNodeIndex)
		{
			return;
		}

		_lastNodeIndex = index;
		Apply(Resolve(zones, index), config);
	}

	private static PhaseKind Resolve(ZoneManager zones, int index)
	{
		// -1 means the world exists but nobody has entered a room yet.
		if (index < 0 || index >= zones.nodes.Count)
		{
			return PhaseKind.Camp;
		}

		return zones.nodes[index].type == WorldNodeType.Start
			? PhaseKind.Camp
			: PhaseKind.Arena;
	}

	private void Apply(PhaseKind phase, DreamArenaConfig config)
	{
		_current = phase;

		TeamMode mode = phase == PhaseKind.Arena ? config.teamMode : TeamMode.Peaceful;
		RelationController.Apply(mode);

		Debug.Log($"{ModIdentity.LogPrefix}phase -> {phase} " +
		          $"({(phase == PhaseKind.Camp ? "safe, everyone allied" : mode.ToString())}).");
	}

	/// <summary>
	/// Re-applies relations for the phase already in effect. Used when the
	/// roster or the config changes without anyone moving.
	/// </summary>
	public void Reapply(DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null || !config.arenaEnabled)
		{
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.instance;
		PhaseKind phase = zones == null
			? PhaseKind.Camp
			: Resolve(zones, zones.currentNodeIndex);

		Apply(phase, config);
	}

	public void Reset()
	{
		_current = PhaseKind.None;
		_lastNodeIndex = int.MinValue;
	}
}
