namespace LastStand.Text;

internal static class HudText
{
	public const string Camp = "Last Stand: Camp";

	public const string Arriving = "Last Stand";

	public static string NextWave(int wave, int seconds)
	{
		return $"Wave {wave} in {seconds}s";
	}

	public static string WaveRunning(int wave, int monstersLeft)
	{
		return monstersLeft == 1
			? $"Wave {wave}: 1 monster left"
			: $"Wave {wave}: {monstersLeft} monsters left";
	}
}
