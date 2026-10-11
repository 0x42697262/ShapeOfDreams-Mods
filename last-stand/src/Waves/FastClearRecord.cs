using System.Collections.Generic;

namespace LastStand.Waves;

internal sealed class FastClearRecord
{
	public FastClearRecord(int needed, Dictionary<string, int> instantKillsByPlayer)
	{
		Needed = needed;
		InstantKillsByPlayer = instantKillsByPlayer;
	}

	public int Needed { get; }

	public Dictionary<string, int> InstantKillsByPlayer { get; }

	public bool HoldsFor(ICollection<string> present)
	{
		int kills = 0;
		foreach (KeyValuePair<string, int> entry in InstantKillsByPlayer)
		{
			if (present == null || present.Contains(entry.Key))
			{
				kills += entry.Value;
			}
		}

		return kills >= Needed;
	}
}
