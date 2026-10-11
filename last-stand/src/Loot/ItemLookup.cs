using System.Collections.Generic;

namespace LastStand.Loot;

internal sealed class ItemLookup
{
	public static readonly ItemLookup NotFound = new ItemLookup(null, Rarity.Common, new List<string>());

	private ItemLookup(string typeName, Rarity rarity, IReadOnlyList<string> candidates)
	{
		TypeName = typeName;
		Rarity = rarity;
		Candidates = candidates;
	}

	public string TypeName { get; }

	public Rarity Rarity { get; }

	public IReadOnlyList<string> Candidates { get; }

	public bool IsFound => TypeName != null || Candidates == null;

	public static ItemLookup Named(string typeName)
	{
		return new ItemLookup(typeName, Rarity.Common, null);
	}

	public static ItemLookup OfRarity(Rarity rarity)
	{
		return new ItemLookup(null, rarity, null);
	}

	public static ItemLookup Ambiguous(List<string> names)
	{
		return new ItemLookup(null, Rarity.Common, names);
	}
}
