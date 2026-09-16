using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Replaces a camp shop's stock with its whole rarity bracket, free.
/// </summary>
/// <remarks>
/// <para>
/// A merchant normally rolls a handful of random items and prices them. These
/// stalls instead list everything of their kind, every rarity, at no cost and
/// in effectively unlimited stock, so a specific loadout is a matter of
/// browsing rather than luck.
/// </para>
/// <para>
/// The stock is written after the original runs rather than instead of it: the
/// base class does bookkeeping around population, and overwriting
/// <c>merchandises[player.guid]</c> afterwards is the same thing every
/// merchant implementation does as its last step.
/// </para>
/// <para>
/// Merchants that are not ours are left completely alone.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(PropEnt_Merchant_Jonas), "OnPopulateMerchandises")]
internal static class CampShopStockPatch
{
	[HarmonyPostfix]
	private static void Postfix(PropEnt_Merchant_Jonas __instance, DewPlayer player)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled || player == null)
		{
			return;
		}

		if (!CampShops.TryGetAssignment(__instance, out CampShops.ShopAssignment shop))
		{
			return;
		}

		List<string> items = ShopCatalog.GetItems(shop.Kind);
		if (items.Count == 0)
		{
			return;
		}

		MerchandiseData[] stock = new MerchandiseData[items.Count];

		for (int i = 0; i < items.Count; i++)
		{
			stock[i] = new MerchandiseData
			{
				type = shop.Kind == ShopKind.Memory ? MerchandiseType.Skill : MerchandiseType.Gem,
				itemName = items[i],

				// Memories use this as a skill level, essences as a quality.
				level = shop.Kind == ShopKind.Memory
					? Mathf.Max(1, mod.config.shopMemoryLevel)
					: Mathf.Max(1, mod.config.shopEssenceQuality),

				// Gold is PvE progression and means nothing here.
				price = default,

				// Purchase is blocked at count <= 0 and decrements each time, so
				// there is no unlimited sentinel; a large stock is the same thing
				// in practice. Two players wanting the same build is normal.
				count = Mathf.Max(1, mod.config.shopStock),
			};
		}

		__instance.merchandises[player.guid] = stock;
	}
}
