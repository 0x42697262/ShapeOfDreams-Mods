using System.Collections.Generic;
using LastStand.Config;
using UnityEngine;

namespace LastStand.Loot;

internal static class LootDropper
{
	public static Actor DropMemory(Vector3 position, DewPlayer recipient, Rarity? rarity, out Rarity dropped)
	{
		dropped = Rarity.Common;
		LootManager loot = NetworkedManagerBase<LootManager>.softInstance;
		if (loot == null)
		{
			return null;
		}

		Rarity? chosen = LootRules.HighestAvailable(rarity ?? loot.SelectSkillRarity(),
			candidate => HasItems(loot.poolSkillsByRarity, candidate));
		if (!chosen.HasValue)
		{
			return null;
		}

		dropped = chosen.Value;
		loot.SelectSkillAndLevel(dropped, out SkillTrigger skill, out int level);
		return skill == null ? null : Dew.CreateSkillTrigger(skill, RewardPosition(position), level, recipient);
	}

	public static Actor DropEssence(Vector3 position, DewPlayer recipient, Rarity? rarity, out Rarity dropped)
	{
		dropped = Rarity.Common;
		LootManager loot = NetworkedManagerBase<LootManager>.softInstance;
		if (loot == null)
		{
			return null;
		}

		Rarity? chosen = LootRules.HighestAvailable(rarity ?? loot.SelectGemRarity(),
			candidate => HasItems(loot.poolGemsByRarity, candidate));
		if (!chosen.HasValue)
		{
			return null;
		}

		dropped = chosen.Value;
		loot.SelectGemAndQuality(dropped, out Gem gem, out int quality);
		return gem == null ? null : Dew.CreateGem(gem, RewardPosition(position), quality, recipient);
	}

	public static void DropDreamDust(Vector3 position, int amount)
	{
		PickupManager pickups = NetworkedManagerBase<PickupManager>.softInstance;
		if (pickups == null || amount <= 0)
		{
			return;
		}

		pickups.DropDreamDust(false, amount, position);
	}

	public static void DropGold(Vector3 position, int amount)
	{
		PickupManager pickups = NetworkedManagerBase<PickupManager>.softInstance;
		if (pickups == null || amount <= 0)
		{
			return;
		}

		pickups.DropGold(true, false, amount, position);
	}

	private static bool HasItems(Dictionary<Rarity, List<string>> pool, Rarity rarity)
	{
		return pool != null && pool.TryGetValue(rarity, out List<string> items) && items.Count > 0;
	}

	private static Vector3 RewardPosition(Vector3 position)
	{
		return Dew.GetGoodRewardPosition(position, Tuning.RewardScatter);
	}
}
