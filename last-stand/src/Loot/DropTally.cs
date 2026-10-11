using System.Collections.Generic;

namespace LastStand.Loot;

internal sealed class DropTally
{
	private readonly Dictionary<Rarity, int> _counts = new Dictionary<Rarity, int>();

	public void Add(Rarity rarity)
	{
		_counts.TryGetValue(rarity, out int count);
		_counts[rarity] = count + 1;
	}

	public int Of(Rarity rarity)
	{
		_counts.TryGetValue(rarity, out int count);
		return count;
	}

	public void Clear()
	{
		_counts.Clear();
	}
}
