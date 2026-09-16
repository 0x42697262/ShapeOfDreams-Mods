using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Drops each player's banked build on the ground next to them at run start.
/// </summary>
/// <remarks>
/// <para>
/// This is entirely server-side. <c>Dew.CreateGem</c> and
/// <c>Dew.CreateSkillTrigger</c> spawn networked entities, which Mirror
/// replicates, so an unmodded client simply sees items appear — the same as
/// any normal drop. That is why the whole loadout feature needs no client
/// install, unlike anything that touches local UI.
/// </para>
/// <para>
/// Quality and level are fixed by config rather than copied from whatever the
/// player would normally get. For PvP that matters: builds should compete on
/// composition, not on who happened to bank the shiniest essence.
/// </para>
/// <para>
/// For reference, stock déjà vu grants quality 100 and level 1, and quality
/// 100 is only the <i>Regular</i> tier — the scale runs
/// Chipped &lt;50, Flawed &lt;100, Regular &lt;200, Flawless &lt;400,
/// Perfect &lt;600, Otherworldly &lt;1000, Transcendent above that.
/// </para>
/// </remarks>
internal static class LoadoutGranter
{
	/// <summary>
	/// Spawns a player's banked items, skipping the one stock déjà vu already
	/// handled.
	/// </summary>
	public static void GrantExtras(DewPlayer player, DreamArenaConfig config)
	{
		Grant(player, config, player?.selectedDejavuItem);
	}

	/// <summary>Spawns a player's whole banked build.</summary>
	public static void GrantAll(DewPlayer player, DreamArenaConfig config)
	{
		Grant(player, config, null);
	}

	private static void Grant(DewPlayer player, DreamArenaConfig config, string alreadySpawned)
	{
		if (!NetworkServer.active || config == null || !config.loadoutEnabled)
		{
			return;
		}

		if (player == null || player.hero == null)
		{
			return;
		}

		List<string> loadout = LoadoutRegistry.GetLoadout(player, config);
		if (loadout.Count == 0)
		{
			return;
		}

		int granted = 0;

		foreach (string item in loadout)
		{
			if (!string.IsNullOrEmpty(alreadySpawned) && item == alreadySpawned)
			{
				continue;
			}

			if (TrySpawn(player, item, config))
			{
				granted++;
			}
		}

		if (granted > 0)
		{
			Debug.Log($"{ModIdentity.LogPrefix}granted {granted} loadout item(s) to {player.name}.");
		}
	}

	private static bool TrySpawn(DewPlayer player, string item, DreamArenaConfig config)
	{
		// Drops land beside the hero rather than inside them.
		Vector3 position = Dew.GetGoodRewardPosition(player.hero.agentPosition);

		if (LoadoutRegistry.IsMemory(item))
		{
			return TrySpawnMemory(player, item, position, config);
		}

		if (LoadoutRegistry.IsEssence(item))
		{
			return TrySpawnEssence(player, item, position, config);
		}

		return false;
	}

	private static bool TrySpawnMemory(DewPlayer player, string item, Vector3 position,
		DreamArenaConfig config)
	{
		if (!Dew.IsSkillIncludedInGame(item))
		{
			return false;
		}

		SkillTrigger skill = DewResources.GetByShortTypeName<SkillTrigger>(item);
		if (skill == null || skill.excludeFromPool)
		{
			return false;
		}

		// Same exclusions stock déjà vu applies: these are not lootable memories.
		if (skill.rarity == Rarity.Character || skill.rarity == Rarity.Identity)
		{
			return false;
		}

		int level = Mathf.Max(1, config.memoryLevel);
		Dew.CreateSkillTrigger(skill, position, level, player);
		return true;
	}

	private static bool TrySpawnEssence(DewPlayer player, string item, Vector3 position,
		DreamArenaConfig config)
	{
		if (!Dew.IsGemIncludedInGame(item))
		{
			return false;
		}

		Gem gem = DewResources.GetByShortTypeName<Gem>(item);
		if (gem == null || gem.excludeFromPool)
		{
			return false;
		}

		int quality = Mathf.Max(1, config.essenceQuality);
		Dew.CreateGem(gem, position, quality, player);
		return true;
	}

	/// <summary>Grants every player's build immediately. Used by the console command.</summary>
	public static void GrantAllPlayers(DreamArenaConfig config)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}only the host can grant loadouts.");
			return;
		}

		foreach (DewPlayer player in RelationController.GetHumanPlayers())
		{
			GrantAll(player, config);
		}
	}
}
