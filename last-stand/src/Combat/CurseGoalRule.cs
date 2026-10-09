using System;
using LastStand.Config;

namespace LastStand.Combat;

internal static class CurseGoalRule
{
	public static int Kills(int waveMonsters, int players, double wavesOfKills)
	{
		double perPlayer = waveMonsters / (double)Math.Max(1, players);
		return Math.Max(Tuning.MinimumCurseKills, (int)Math.Round(perPlayer * wavesOfKills));
	}
}
