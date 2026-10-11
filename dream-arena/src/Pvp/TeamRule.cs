using DreamArena.Config;

namespace DreamArena.Pvp;

internal static class TeamRule
{
	public static bool AreEnemies(TeamMode mode, int first, int second)
	{
		switch (mode)
		{
			case TeamMode.FreeForAll:
				return true;
			case TeamMode.TwoTeams:
				return TeamOf(first) != TeamOf(second);
			default:
				return false;
		}
	}

	public static int TeamOf(int joinIndex)
	{
		return joinIndex % Tuning.TeamCount;
	}
}
