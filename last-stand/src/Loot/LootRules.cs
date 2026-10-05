using System;
using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Loot;

internal static class LootRules
{
	private const int SoloPlayers = 1;

	public static LootDecision Decide(TierLoot tier, LootSettings settings, int wave, double itemRoll)
	{
		double growth = 1 + settings.GrowthPerWave * Math.Max(0, wave - Tuning.FirstWave);

		return new LootDecision(
			itemRarity: RollRarity(settings.RarestFirst, tier.DropMultiplier, itemRoll),
			dreamDust: tier.DreamDust,
			gold: (int)Math.Round(tier.Gold * growth));
	}

	public static Rarity? RollRarity(IReadOnlyList<RarityChance> rarestFirst, double multiplier, double roll)
	{
		double threshold = 0;
		foreach (RarityChance entry in rarestFirst)
		{
			threshold += entry.Chance * multiplier;
			if (roll < threshold)
			{
				return entry.Rarity;
			}
		}

		return null;
	}

	public static bool ChooseMemory(bool balanceKinds, int players, int memories, int essences,
		double memoryShare, double roll)
	{
		if (balanceKinds && players > SoloPlayers && memories != essences)
		{
			return memories < essences;
		}

		return roll < memoryShare;
	}
}
