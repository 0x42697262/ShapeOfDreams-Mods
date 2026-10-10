using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Text;

namespace DreamArena.Match;

internal static class MatchRules
{
	public static void Apply(SessionSettings settings)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null)
		{
			return;
		}

		game.isGameOverEnabled = !settings.NeverEndRun;
		game.maxAndSpawnedPopulationMultiplier = settings.NoMonsters ? Tuning.NoMonsters : Tuning.NormalMonsters;
		int cleared = ClearLucidDreams(settings);
		Log.Info(LogText.RulesApplied(game.isGameOverEnabled, game.maxAndSpawnedPopulationMultiplier, cleared));
	}

	public static void Restore()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null)
		{
			return;
		}

		game.isGameOverEnabled = true;
		game.maxAndSpawnedPopulationMultiplier = Tuning.NormalMonsters;
		Log.Info(LogText.RulesRestored);
	}

	private static int ClearLucidDreams(SessionSettings settings)
	{
		GameSettingsManager game = NetworkedManagerBase<GameSettingsManager>.softInstance;
		if (!settings.RemoveLucidDreams || game == null || game.activeLucidDreams.Count == 0)
		{
			return 0;
		}

		int count = game.activeLucidDreams.Count;
		game.ClearLucidDreams();
		return count;
	}
}
