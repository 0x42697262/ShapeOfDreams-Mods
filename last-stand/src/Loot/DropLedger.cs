using System.Collections.Generic;

namespace LastStand.Loot;

internal sealed class DropLedger
{
	private readonly Dictionary<string, int> _items = new Dictionary<string, int>();
	private readonly Dictionary<string, int> _mobItems = new Dictionary<string, int>();

	public int MobMemories { get; private set; }

	public int MobEssences { get; private set; }

	public void StartWave()
	{
		_items.Clear();
		_mobItems.Clear();
		MobMemories = 0;
		MobEssences = 0;
	}

	public void Record(string playerGuid, bool fromMob, bool memory)
	{
		Increment(_items, playerGuid);
		if (!fromMob)
		{
			return;
		}

		Increment(_mobItems, playerGuid);
		if (memory)
		{
			MobMemories++;
		}
		else
		{
			MobEssences++;
		}
	}

	public bool HasDropThisWave(string playerGuid)
	{
		return _items.ContainsKey(playerGuid);
	}

	public string ChooseMobRecipient(IReadOnlyList<string> candidates, string preferred, int perPlayerCap)
	{
		return Fewest(_mobItems, candidates, preferred, perPlayerCap);
	}

	public string ChooseRecipient(IReadOnlyList<string> candidates, string preferred)
	{
		return Fewest(_items, candidates, preferred, int.MaxValue);
	}

	private static string Fewest(Dictionary<string, int> counts, IReadOnlyList<string> candidates,
		string preferred, int cap)
	{
		string best = null;
		int bestCount = int.MaxValue;

		foreach (string guid in candidates)
		{
			counts.TryGetValue(guid, out int count);
			if (count >= cap)
			{
				continue;
			}

			if (count < bestCount || (count == bestCount && guid == preferred))
			{
				best = guid;
				bestCount = count;
			}
		}

		return best;
	}

	private static void Increment(Dictionary<string, int> counts, string key)
	{
		counts.TryGetValue(key, out int count);
		counts[key] = count + 1;
	}
}
