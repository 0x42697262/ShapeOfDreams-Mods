using System;
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

	private const string PureDreamModifier = "RoomMod_PureDream";

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

		GameManager game = NetworkedManagerBase<GameManager>.instance;
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
		// multiply by this, so zero empties the arena.
		game.maxAndSpawnedPopulationMultiplier =
			(active && config.suppressMonsters) ? NoMonsters : NormalMonsters;

		Debug.Log($"{ModIdentity.LogPrefix}rules applied: " +
		          $"gameOverEnabled={game.isGameOverEnabled}, " +
		          $"populationMultiplier={game.maxAndSpawnedPopulationMultiplier}.");
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

		GameManager game = NetworkedManagerBase<GameManager>.instance;
		if (game == null)
		{
			return;
		}

		game.isGameOverEnabled = true;
		game.maxAndSpawnedPopulationMultiplier = NormalMonsters;

		Debug.Log($"{ModIdentity.LogPrefix}stock game rules restored.");
	}

	/// <summary>
	/// Tags the current room with the Pure White Dream modifier for the white
	/// arena look.
	/// </summary>
	/// <remarks>
	/// <c>RoomMod_PureDream</c> is a <c>RoomModifierBase</c> prefab resolved by
	/// short type name from <c>ModifierData.type</c>. Its <c>roomOverride</c>
	/// is blank, so it decorates the room rather than swapping it, and it
	/// spawns no boss.
	///
	/// This is the least certain part of the mod: world generation is
	/// involved and applying a modifier after a node already loaded may not
	/// retroactively decorate it. Failures are logged, never thrown.
	/// </remarks>
	public static void TryApplyPureWhiteDream()
	{
		if (!NetworkServer.active)
		{
			return;
		}

		try
		{
			ZoneManager zones = NetworkedManagerBase<ZoneManager>.instance;
			if (zones == null)
			{
				return;
			}

			// WorldNodeData is a struct, so this is a copy — but `modifiers` is a
			// List reference, so adding to it still reaches the real node.
			WorldNodeData node = zones.currentNode;
			if (node.modifiers == null)
			{
				Debug.LogWarning($"{ModIdentity.LogPrefix}current node has no modifier list; " +
				                 "skipping pure white dream.");
				return;
			}

			if (node.HasModifier(PureDreamModifier))
			{
				return;
			}

			node.modifiers.Add(new ModifierData(PureDreamModifier));
			Debug.Log($"{ModIdentity.LogPrefix}applied {PureDreamModifier} to the current room.");
		}
		catch (Exception e)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}could not apply {PureDreamModifier}: {e.Message}");
		}
	}
}
