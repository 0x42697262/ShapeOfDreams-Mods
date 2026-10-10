using System;
using LastStand.Config;

namespace LastStand.Combat;

internal static class CurseGoalRule
{
	public static int Kills(int waveMonsters, double wavesOfKills)
	{
		return Math.Max(Tuning.MinimumCurseKills, (int)Math.Round(waveMonsters * wavesOfKills));
	}
}
