namespace LastStand.Game;

internal static class MonsterStrength
{
	public static void SetAmbientLevel(int level)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game != null && game.ambientLevel != level)
		{
			game.SetAmbientLevel(level);
		}
	}
}
