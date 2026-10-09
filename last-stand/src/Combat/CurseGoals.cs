namespace LastStand.Combat;

internal static class CurseGoals
{
	public static bool EndsByKillsOrTravel(CurseStatusEffect curse)
	{
		return curse != null && curse.victim is Hero
		       && (curse.progressType == QuestProgressType.Kills
		           || curse.progressType == QuestProgressType.Travel
		           || curse.progressType == QuestProgressType.TravelZone);
	}

	public static void SetKills(CurseStatusEffect curse, int kills)
	{
		curse.progressType = QuestProgressType.Kills;
		curse.requiredAmount = kills;
		curse.CurseEvent_OnProgressChanged?.Invoke();
	}
}
