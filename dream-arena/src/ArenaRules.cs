using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Applies the non-combat arena rules to the running game.
/// </summary>
/// <remarks>
/// All of these are server-side values that replicate, so clients need no mod.
/// </remarks>
internal static class ArenaRules
{
	/// <summary>Population budget value that produces no monsters at all.</summary>
	private const float NoMonsters = 0f;

	private const float NormalMonsters = 1f;

	/// <summary>
	/// Pushes the current config onto the live <see cref="GameManager"/>.
	/// Safe to call repeatedly; it is how the rules are re-asserted after a
	/// zone change or a config edit.
	/// </summary>
	public static void Apply(DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null)
		{
			return;
		}

		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null)
		{
			// Still in the lobby; rules get applied when the game manager starts.
			return;
		}

		bool active = config.arenaEnabled;

		// Belt and braces alongside GameEndPatch: this stops CheckGameOver from
		// even starting its countdown, the patch blocks every other exit path.
		game.isGameOverEnabled = !(active && config.neverEndRun);

		// maxSpawnedPopulation and GetAdjustedMonsterSpawnPopulation both
		// multiply by this, so zero empties the arena. It is also what keeps
		// the boss from appearing in a boss room.
		game.maxAndSpawnedPopulationMultiplier =
			(active && config.suppressMonsters) ? NoMonsters : NormalMonsters;

		ClearLucidDreams(active, config);

		Debug.Log($"{ModIdentity.LogPrefix}rules applied: " +
		          $"gameOverEnabled={game.isGameOverEnabled}, " +
		          $"populationMultiplier={game.maxAndSpawnedPopulationMultiplier}.");
	}

	/// <summary>
	/// Drops any lucid dreams already in effect.
	/// </summary>
	/// <remarks>
	/// The patch stops new ones being added, but a lobby configured before PvP
	/// was selected can already carry some, and <c>GameManager</c> reads the
	/// active list directly — so leaving them would leave them in force.
	/// <c>ClearLucidDreams</c> is the game's own server-side method for this.
	/// </remarks>
	private static void ClearLucidDreams(bool active, DreamArenaConfig config)
	{
		if (!active || !config.removeLucidDreams)
		{
			return;
		}

		GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
		if (settings == null || settings.activeLucidDreams.Count == 0)
		{
			return;
		}

		int count = settings.activeLucidDreams.Count;
		settings.ClearLucidDreams();

		Debug.Log($"{ModIdentity.LogPrefix}cleared {count} active lucid dream(s).");
	}

	/// <summary>
	/// Restores stock behavior. Called on mod unload so a live reload does not
	/// leave a run with no monsters and no ending.
	/// </summary>
	public static void Restore()
	{
		if (!NetworkServer.active)
		{
			return;
		}

		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null)
		{
			return;
		}

		game.isGameOverEnabled = true;
		game.maxAndSpawnedPopulationMultiplier = NormalMonsters;

		Debug.Log($"{ModIdentity.LogPrefix}stock game rules restored.");
	}

	/// <summary>
	/// Redirects every node the players can travel to into the arena room.
	/// </summary>
	/// <remarks>
	/// <c>WorldNodeData</c> carries both a <c>room</c> and a
	/// <c>roomOverride</c>; travel loads the override when it is set
	/// (<c>ZoneManager</c>: <c>IsNullOrEmpty(roomOverride) ? room : roomOverride</c>).
	/// <c>SetRoomOverride</c> is a public server method that writes it back
	/// into the <c>nodes</c> SyncList, so the change replicates and unmodded
	/// clients load the same room.
	///
	/// The Start node is deliberately left alone. World generation always makes
	/// node 0 <c>WorldNodeType.Start</c> with a room from the zone's
	/// <c>startRooms</c> — the campfire a run normally opens in — so players
	/// spawn in a safe camp and reach the arena by taking the rift out of it.
	///
	/// The current node is skipped too: the game warns that overriding the room
	/// you are standing in is "prone to bugs".
	/// </remarks>
	public static void ApplyArenaRoom(DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null
		    || !config.arenaEnabled || !config.forceArenaRoom)
		{
			return;
		}

		if (string.IsNullOrEmpty(config.arenaRoom))
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no arena room configured; skipping.");
			return;
		}

		try
		{
			ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
			if (zones == null)
			{
				return;
			}

			int changed = 0;

			for (int i = 0; i < zones.nodes.Count; i++)
			{
				if (i == zones.currentNodeIndex)
				{
					continue;
				}

				// The camp stays the camp.
				if (zones.nodes[i].type == WorldNodeType.Start)
				{
					continue;
				}

				if (zones.nodes[i].roomOverride == config.arenaRoom)
				{
					continue;
				}

				zones.SetRoomOverride(i, config.arenaRoom);
				StripModifiers(zones, i, config);
				changed++;
			}

			Debug.Log($"{ModIdentity.LogPrefix}arena room '{config.arenaRoom}' applied to " +
			          $"{changed} node(s). Camp stays intact; take the rift to fight.");
		}
		catch (Exception e)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}could not apply arena room " +
			                 $"'{config.arenaRoom}': {e.Message}");
		}
	}

	/// <summary>
	/// Clears a node's room modifiers.
	/// </summary>
	/// <remarks>
	/// Modifiers such as Accelerated Time are rolled onto nodes during world
	/// generation and change how everyone moves and attacks, which has no place
	/// in a match meant to be even. <c>RemoveModifier</c> is used rather than
	/// editing the list in place, because <c>WorldNodeData</c> is a struct
	/// inside a SyncList and has to be written back to replicate.
	/// </remarks>
	private static void StripModifiers(ZoneManager zones, int nodeIndex, DreamArenaConfig config)
	{
		if (!config.stripRoomModifiers)
		{
			return;
		}

		List<ModifierData> modifiers = zones.nodes[nodeIndex].modifiers;
		if (modifiers == null || modifiers.Count == 0)
		{
			return;
		}

		// Snapshot the ids: removal rewrites the list being read.
		List<int> ids = new List<int>();
		foreach (ModifierData modifier in modifiers)
		{
			ids.Add(modifier.id);
		}

		foreach (int id in ids)
		{
			zones.RemoveModifier(nodeIndex, id);
		}
	}
}
