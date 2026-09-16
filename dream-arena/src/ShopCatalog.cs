using System.Collections.Generic;
using UnityEngine;

namespace DreamArena;

/// <summary>What a camp remnant offers.</summary>
public enum ShopKind
{
	/// <summary>Memories, which are skills (<c>St_</c>).</summary>
	Memory,

	/// <summary>Essences, which are gems (<c>Gem_</c>).</summary>
	Essence,
}

/// <summary>
/// Builds the item lists the camp remnants offer.
/// </summary>
/// <remarks>
/// One remnant per kind and rarity, so a build is assembled by picking a
/// bracket and choosing from it rather than rolled for.
///
/// Splitting by rarity is not only for readability. A remnant's choices are
/// drawn as a <c>GridLayoutGroup</c> with no scrolling, so one remnant holding
/// every memory in the game would spill off the window. A single rarity is a
/// few dozen entries, which the grid can lay out.
///
/// <c>Rarity.Character</c> and <c>Rarity.Identity</c> are skipped: those are a
/// hero's built-in kit, not lootable items, and the game excludes them from
/// every pool it builds.
/// </remarks>
internal static class ShopCatalog
{
	/// <summary>Rarities worth giving a remnant, in display order.</summary>
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
	/// Everything of a kind and rarity that may legally be given out.
	/// </summary>
	public static List<string> GetItems(ShopKind kind, Rarity rarity)
	{
		string key = kind + ":" + rarity;
		if (Cache.TryGetValue(key, out List<string> cached))
		{
			return cached;
		}

		List<string> names = kind == ShopKind.Memory
			? CollectMemories(rarity)
			: CollectEssences(rarity);

		Cache[key] = names;

		Debug.Log($"{ModIdentity.LogPrefix}catalog {Describe(kind, rarity)}: {names.Count} item(s).");
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

	/// <summary>Remnant label.</summary>
	public static string Describe(ShopKind kind, Rarity rarity)
	{
		return rarity + " " + (kind == ShopKind.Memory ? "Memories" : "Essences");
	}
}
