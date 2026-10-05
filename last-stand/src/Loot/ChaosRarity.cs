using UnityEngine;

namespace LastStand.Loot;

internal static class ChaosRarity
{
	private const double Certain = 1;

	public static void Reroll(Shrine_Chaos shrine, LootSettings settings, int wave)
	{
		if (shrine == null || settings == null)
		{
			return;
		}

		shrine.Networkrarity = Roll(settings, wave, Random.value);
	}

	public static Rarity Roll(LootSettings settings, int wave, double roll)
	{
		return LootRules.RollRarity(settings.Chaos.At(wave), Certain, roll) ?? Rarity.Common;
	}
}
