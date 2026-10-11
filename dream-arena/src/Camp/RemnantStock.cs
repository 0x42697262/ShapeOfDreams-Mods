using System.Collections.Generic;
using DreamArena.Diagnostics;
using DreamArena.Text;

namespace DreamArena.Camp;

internal static class RemnantStock
{
	private static readonly Dictionary<(RemnantKind, Rarity), List<string>> Cache =
		new Dictionary<(RemnantKind, Rarity), List<string>>();

	public static List<string> Items(RemnantKind kind, Rarity rarity)
	{
		if (Cache.TryGetValue((kind, rarity), out List<string> cached))
		{
			return cached;
		}

		List<string> names = kind == RemnantKind.Memory ? Memories(rarity) : Essences(rarity);
		Cache[(kind, rarity)] = names;
		Log.Info(LogText.RemnantStock(TextFor(kind, rarity), names.Count));
		return names;
	}

	public static string TextFor(RemnantKind kind, Rarity rarity)
	{
		return Messages.RemnantName(rarity.ToString(), kind == RemnantKind.Memory);
	}

	public static void Clear()
	{
		Cache.Clear();
	}

	private static List<string> Memories(Rarity rarity)
	{
		List<string> names = new List<string>();
		foreach (SkillTrigger skill in DewResources.FindAllByType<SkillTrigger>(ResourceLoadSettings.Light))
		{
			string name = skill != null ? skill.GetType().Name : null;
			if (name != null && !skill.excludeFromPool && skill.rarity == rarity && Dew.IsSkillIncludedInGame(name)
			    && !names.Contains(name))
			{
				names.Add(name);
			}
		}

		names.Sort();
		return names;
	}

	private static List<string> Essences(Rarity rarity)
	{
		List<string> names = new List<string>();
		foreach (Gem gem in DewResources.FindAllByType<Gem>(ResourceLoadSettings.Light))
		{
			string name = gem != null ? gem.GetType().Name : null;
			if (name != null && !gem.excludeFromPool && gem.rarity == rarity && Dew.IsGemIncludedInGame(name)
			    && !names.Contains(name))
			{
				names.Add(name);
			}
		}

		names.Sort();
		return names;
	}
}
