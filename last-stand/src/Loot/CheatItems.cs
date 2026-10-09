using System;
using System.Collections.Generic;
using LastStand.Config;
using UnityEngine;

namespace LastStand.Loot;

internal static class CheatItems
{
	public static ItemLookup FindEssence(string query)
	{
		LootManager loot = NetworkedManagerBase<LootManager>.softInstance;
		return loot == null
			? ItemLookup.NotFound
			: Find(query, loot.poolGemsByRarity, name => DewLocalization.GetGemName(DewLocalization.GetGemKey(name)));
	}

	public static ItemLookup FindMemory(string query)
	{
		LootManager loot = NetworkedManagerBase<LootManager>.softInstance;
		return loot == null
			? ItemLookup.NotFound
			: Find(query, loot.poolSkillsByRarity,
				name => DewLocalization.GetSkillName(DewLocalization.GetSkillKey(name), 0));
	}

	public static Gem SpawnEssence(ItemLookup item, int? quality, DewPlayer owner, Vector3 position, out int spawnedQuality)
	{
		spawnedQuality = 0;
		LootManager loot = NetworkedManagerBase<LootManager>.softInstance;
		string name = item.TypeName ?? RandomName(loot?.poolGemsByRarity, item.Rarity);
		Gem prefab = name != null ? DewResources.GetByShortTypeName<Gem>(name) : null;
		if (loot == null || prefab == null)
		{
			return null;
		}

		spawnedQuality = Math.Max(Tuning.MinimumEssenceQuality, quality ?? loot.SelectGemQuality(prefab.rarity));
		return Dew.CreateGem(prefab, RewardPosition(position), spawnedQuality, owner);
	}

	public static SkillTrigger SpawnMemory(ItemLookup item, int? level, DewPlayer owner, Vector3 position, out int spawnedLevel)
	{
		spawnedLevel = 0;
		LootManager loot = NetworkedManagerBase<LootManager>.softInstance;
		string name = item.TypeName ?? RandomName(loot?.poolSkillsByRarity, item.Rarity);
		SkillTrigger prefab = name != null ? DewResources.GetByShortTypeName<SkillTrigger>(name) : null;
		if (loot == null || prefab == null)
		{
			return null;
		}

		spawnedLevel = Math.Max(Tuning.MinimumMemoryLevel, level ?? loot.SelectSkillLevel(prefab.rarity));
		return Dew.CreateSkillTrigger(prefab, RewardPosition(position), spawnedLevel, owner);
	}

	private static ItemLookup Find(string query, Dictionary<Rarity, List<string>> pool, Func<string, string> localized)
	{
		if (pool == null || string.IsNullOrWhiteSpace(query))
		{
			return ItemLookup.NotFound;
		}

		if (Enum.TryParse(query, true, out Rarity rarity) && pool.TryGetValue(rarity, out List<string> ofRarity)
		    && ofRarity.Count > 0)
		{
			return ItemLookup.OfRarity(rarity);
		}

		List<string> matches = new List<string>();
		foreach (List<string> names in pool.Values)
		{
			foreach (string name in names)
			{
				if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
				{
					return ItemLookup.Named(name);
				}

				if (Contains(name, query) || Contains(localized(name), query))
				{
					matches.Add(name);
				}
			}
		}

		return matches.Count == 1 ? ItemLookup.Named(matches[0]) : ItemLookup.Ambiguous(matches);
	}

	private static bool Contains(string text, string query)
	{
		return text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string RandomName(Dictionary<Rarity, List<string>> pool, Rarity rarity)
	{
		return pool != null && pool.TryGetValue(rarity, out List<string> names) && names.Count > 0
			? names[UnityEngine.Random.Range(0, names.Count)]
			: null;
	}

	private static Vector3 RewardPosition(Vector3 position)
	{
		return Dew.GetGoodRewardPosition(position, Tuning.RewardScatter);
	}
}
