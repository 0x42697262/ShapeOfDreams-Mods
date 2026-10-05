using UnityEngine;

namespace LastStand.Loot;

internal static class ChaosRarity
{
	private const double Certain = 1;

	public static void Reroll(Shrine_Chaos shrine, LootSettings settings)
	{
		if (shrine == null || settings == null)
		{
			return;
		}

		shrine.Networkrarity = Roll(settings, Random.value);
	}

	public static Rarity Roll(LootSettings settings, double roll)
	{
		return LootRules.RollRarity(settings.ChaosRarestFirst, Certain, roll) ?? Rarity.Common;
	}
}
