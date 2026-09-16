using System.Collections.Generic;
using UnityEngine;

namespace DreamArena;

/// <summary>What a camp shop sells.</summary>
public enum ShopKind
{
	/// <summary>Memories, which are skills (<c>St_</c>).</summary>
	Memory,

	/// <summary>Essences, which are gems (<c>Gem_</c>).</summary>
	Essence,
}

/// <summary>
/// Builds the item lists the camp shops sell.
/// </summary>
/// <remarks>
/// One shop per kind, each stocking every rarity in order, so a build is
/// assembled by browsing rather than rolled for. This was ten stalls split by
/// rarity, which was clearer to read but cost ten merchant entities and their
/// networking in a room players sit in between rounds.
///
/// <c>Rarity.Character</c> and <c>Rarity.Identity</c> are skipped: those are a
/// hero's built-in kit, not lootable items, and the game excludes them from
/// every pool it builds.
/// </remarks>
internal static class ShopCatalog
{
	/// <summary>Rarities worth giving a shop, in display order.</summary>
	public static readonly Rarity[] StockedRarities =
	{
		Rarity.Common,
		Rarity.Rare,
		Rarity.Epic,
		Rarity.Legendary,
		Rarity.Unique,
	};

	private static readonly Dictionary<string, List<string>> Cache =
		new Dictionary<string, List<string>>();

	/// <summary>
	/// Everything of a kind that may legally be given out, ordered by rarity so
	/// the stall reads Common first and Unique last.
	/// </summary>
	public static List<string> GetItems(ShopKind kind)
	{
		string key = kind.ToString();
		if (Cache.TryGetValue(key, out List<string> cached))
		{
			return cached;
		}

		List<string> names = new List<string>();
		foreach (Rarity rarity in StockedRarities)
		{
			names.AddRange(kind == ShopKind.Memory ? CollectMemories(rarity) : CollectEssences(rarity));
		}

		Cache[key] = names;

		Debug.Log($"{ModIdentity.LogPrefix}catalog {kind}: {names.Count} item(s).");
		return names;
	}

	private static List<string> CollectMemories(Rarity rarity)
	{
		List<string> names = new List<string>();

		foreach (SkillTrigger skill in DewResources.FindAllByType<SkillTrigger>(ResourceLoadSettings.Light))
		{
			if (skill == null || skill.excludeFromPool || skill.rarity != rarity)
			{
				continue;
			}

			// A hero's own kit is not merchandise.
			if (rarity == Rarity.Character || rarity == Rarity.Identity)
			{
				continue;
			}

			string name = skill.GetType().Name;
			if (Dew.IsSkillIncludedInGame(name) && !names.Contains(name))
			{
				names.Add(name);
			}
		}

		names.Sort();
		return names;
	}

	private static List<string> CollectEssences(Rarity rarity)
	{
		List<string> names = new List<string>();

		foreach (Gem gem in DewResources.FindAllByType<Gem>(ResourceLoadSettings.Light))
		{
			if (gem == null || gem.excludeFromPool || gem.rarity != rarity)
			{
				continue;
			}

			string name = gem.GetType().Name;
			if (Dew.IsGemIncludedInGame(name) && !names.Contains(name))
			{
				names.Add(name);
			}
		}

		names.Sort();
		return names;
	}

	public static void ClearCache()
	{
		Cache.Clear();
	}

	/// <summary>Shop label.</summary>
	public static string Describe(ShopKind kind)
	{
		return kind == ShopKind.Memory ? "All Memories" : "All Essences";
	}
}
