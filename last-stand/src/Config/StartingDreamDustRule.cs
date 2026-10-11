using System;

namespace LastStand.Config;

internal static class StartingDreamDustRule
{
	public static int Amount(int baseAmount, int startingWorld)
	{
		double amount = baseAmount * Math.Pow(Tuning.StartingDreamDustGrowthPerWorld, startingWorld);
		return amount >= int.MaxValue ? int.MaxValue : (int)amount;
	}
}
