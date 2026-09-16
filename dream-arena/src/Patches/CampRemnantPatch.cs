using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Replaces a camp remnant's three random picks with its whole rarity bracket.
/// </summary>
/// <remarks>
/// <para>
/// A remnant normally rolls a rarity, then offers a few random items of it.
/// These offer every memory or essence of their own bracket instead, at a
/// fixed level, so a specific loadout is a matter of choosing rather than
/// luck — and since the remnants never run dry, everyone can take the same
/// thing.
/// </para>
/// <para>
/// The offering is written after the original runs rather than instead of it:
/// the base class does bookkeeping around population, and assigning
/// <c>choices[player.guid]</c> as the last step is exactly what the vanilla
/// implementations do.
/// </para>
/// <para>
/// Remnants that are not ours are left completely alone, so the ones the world
/// generates still behave normally.
/// </para>
/// </remarks>
internal static class CampRemnantChoices
{
	/// <summary>
	/// Offers every item of this remnant's bracket, or does nothing if the
	/// remnant belongs to the game rather than to us.
	/// </summary>
	public static void Apply(ChoiceShrine remnant, DewPlayer player)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled || remnant == null || player == null)
		{
			return;
		}

		if (!CampShops.TryGetAssignment(remnant, out CampShops.ShopAssignment shop))
		{
			return;
		}

		List<string> items = ShopCatalog.GetItems(shop.Kind, shop.Rarity);
		if (items.Count == 0)
		{
			return;
		}

		// Memories read this as a skill level, essences as a gem quality.
		int level = shop.Kind == ShopKind.Memory
			? Mathf.Max(1, mod.config.shopMemoryLevel)
			: Mathf.Max(1, mod.config.shopEssenceQuality);

		ChoiceShrineItem[] offering = new ChoiceShrineItem[items.Count];
		for (int i = 0; i < items.Count; i++)
		{
			offering[i] = new ChoiceShrineItem
			{
				typeName = items[i],
				level = level,
			};
		}

		remnant.itemCount = offering.Length;
		remnant.choices[player.guid] = offering;
	}
}

/// <summary>Memory remnants offer a whole memory rarity.</summary>
[HarmonyPatch(typeof(Shrine_Retrospection), "OnPopulateChoices")]
internal static class CampMemoryRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Retrospection __instance, DewPlayer player)
	{
		CampRemnantChoices.Apply(__instance, player);
	}
}

/// <summary>Essence remnants offer a whole essence rarity.</summary>
[HarmonyPatch(typeof(Shrine_Enlightenment), "OnPopulateChoices")]
internal static class CampEssenceRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Enlightenment __instance, DewPlayer player)
	{
		CampRemnantChoices.Apply(__instance, player);
	}
}
