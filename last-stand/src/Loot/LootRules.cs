using System;
using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Loot;

internal static class LootRules
{
	private const int SoloPlayers = 1;
	private const double Whole = 1;

	private static double[] _shares = new double[0];

	public static LootDecision Decide(TierLoot tier, LootSettings settings, int wave, double itemRoll)
	{
		double growth = 1 + settings.GrowthPerWave * Math.Max(0, wave - Tuning.FirstWave);

		return new LootDecision(
			itemRarity: RollRarity(settings.Items.At(wave), tier.DropMultiplier, settings.ItemRarityFloor, itemRoll),
			dreamDust: tier.DreamDust,
			gold: (int)Math.Round(tier.Gold * growth));
	}

	public static Rarity? RollRarity(IReadOnlyList<RarityChance> rarestFirst, double multiplier, double floor,
		double roll)
	{
		double[] shares = Shares(rarestFirst, multiplier, floor);
		double threshold = 0;
		for (int i = 0; i < rarestFirst.Count; i++)
		{
			threshold += shares[i];
			if (roll < threshold)
			{
				return rarestFirst[i].Rarity;
			}
		}

		return null;
	}

	public static double[] Shares(IReadOnlyList<RarityChance> rarestFirst, double multiplier, double floor)
	{
		int count = rarestFirst.Count;
		if (_shares.Length < count)
		{
			_shares = new double[count];
		}

		double total = 0;
		for (int i = 0; i < count; i++)
		{
			_shares[i] = Math.Max(0, rarestFirst[i].Chance * multiplier);
			total += _shares[i];
		}

		double overflow = total - Whole;
		for (int i = count - 1; i >= 0 && overflow > 0; i--)
		{
			double cut = Math.Min(overflow, Math.Max(0, _shares[i] - floor));
			_shares[i] -= cut;
			overflow -= cut;
		}

		for (int i = count - 1; i >= 0 && overflow > 0; i--)
		{
			double cut = Math.Min(overflow, _shares[i]);
			_shares[i] -= cut;
			overflow -= cut;
		}

		return _shares;
	}

	public static Rarity? HighestAvailable(Rarity rolled, Func<Rarity, bool> hasItems)
	{
		for (int rarity = (int)rolled; rarity >= (int)Rarity.Common; rarity--)
		{
			if (hasItems((Rarity)rarity))
			{
				return (Rarity)rarity;
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
