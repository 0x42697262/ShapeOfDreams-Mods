using System.Collections.Generic;

namespace DreamArena.Camp;

internal static class RemnantChoices
{
	public static void Offer(ChoiceShrine remnant, DewPlayer player, CampRemnants remnants, int memoryLevel,
		int essenceQuality)
	{
		if (player == null || !RemnantTag.TryRead(remnant, out (RemnantKind Kind, Rarity Rarity) tag))
		{
			return;
		}

		List<string> items = RemnantStock.Items(tag.Kind, tag.Rarity);
		if (items.Count == 0)
		{
			return;
		}

		int level = tag.Kind == RemnantKind.Memory ? memoryLevel : essenceQuality;
		List<ChoiceShrineItem> offering = new List<ChoiceShrineItem>(items.Count);
		foreach (string item in items)
		{
			if (!remnants.HasTaken(player, item))
			{
				offering.Add(new ChoiceShrineItem { typeName = item, level = level });
			}
		}

		remnant.choices[player.guid] = offering.ToArray();
	}

	public static bool TryReadChoice(ChoiceShrine remnant, int index, DewPlayer player, out string item)
	{
		item = null;
		if (player == null || !RemnantTag.TryRead(remnant, out _)
		    || !remnant.choices.TryGetValue(player.guid, out ChoiceShrineItem[] items)
		    || index < 0 || index >= items.Length)
		{
			return false;
		}

		item = items[index].typeName;
		return !string.IsNullOrEmpty(item);
	}

	public static void AfterChoice(ChoiceShrine remnant, DewPlayer player, string item, CampRemnants remnants)
	{
		remnants.RecordTaken(player, item);
		remnant.PopulatePlayerChoices(player);
		remnant.ResetUseCounts();
		remnant.Network_lastUseTime = double.NegativeInfinity;
	}

	public static bool IsEmptyFor(ChoiceShrine remnant, Entity entity)
	{
		return entity != null && entity.owner != null && RemnantTag.TryRead(remnant, out _)
		       && remnant.choices.TryGetValue(entity.owner.guid, out ChoiceShrineItem[] items) && items.Length == 0;
	}
}
