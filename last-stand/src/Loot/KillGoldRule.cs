using System;

namespace LastStand.Loot;

internal static class KillGoldRule
{
	public static int Cap(int amount, int perPlayer, int players)
	{
		if (perPlayer <= 0)
		{
			return amount;
		}

		long cap = (long)Math.Max(0, perPlayer) * Math.Max(1, players);
		return (int)Math.Min(amount, cap);
	}
}
