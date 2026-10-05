namespace LastStand.Game;

internal static class MonsterStrength
{
	private const int FallbackLevel = 1;

	public static int CurrentLevel()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		return game != null ? game.ambientLevel : FallbackLevel;
	}

	public static void SetAmbientLevel(int level)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game != null && game.ambientLevel != level)
		{
			game.SetAmbientLevel(level);
		}
	}
}
