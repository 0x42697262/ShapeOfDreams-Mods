using System.Collections.Generic;

namespace LastStand.Waves;

internal sealed class HeroicFastKillRecord
{
	private const double Everyone = 1;

	public HeroicFastKillRecord(double projectedSecondsPerBoss, Dictionary<string, double> burstDamageByPlayer)
	{
		ProjectedSecondsPerBoss = projectedSecondsPerBoss;
		BurstDamageByPlayer = burstDamageByPlayer;
	}

	public double ProjectedSecondsPerBoss { get; }

	public Dictionary<string, double> BurstDamageByPlayer { get; }

	public double ShareOf(ICollection<string> present)
	{
		double total = 0;
		double here = 0;
		foreach (KeyValuePair<string, double> entry in BurstDamageByPlayer)
		{
			total += entry.Value;
			if (present == null || present.Contains(entry.Key))
			{
				here += entry.Value;
			}
		}

		return total <= 0 ? Everyone : here / total;
	}
}
