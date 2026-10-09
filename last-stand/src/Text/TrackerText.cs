

namespace LastStand.Text;

internal static class TrackerText
{
	public static string NextWave(int wave, int seconds)
	{
		return $"Wave {wave} in {seconds}s";
	}

	public static string AwaitingCurse(int wave)
	{
		return $"Wave {wave}: choose a curse";
	}

	public static string WaveRunning(int wave, int monstersLeft)
	{
		return monstersLeft == 1
			? $"Wave {wave}: 1 monster left"
			: $"Wave {wave}: {monstersLeft} monsters left";
	}
}
