namespace LastStand.Well;

internal static class HatredShards
{
	private const float NoGold = 0f;

	private static float? _dreamDustBase;
	private static float? _goldBase;

	public static void Open(Shrine_Hatred_Reward shard, int wave, int dreamDustPerWave, int goldPerWave)
	{
		if (goldPerWave <= 0)
		{
			shard.goldWeight = NoGold;
		}

		_dreamDustBase = (float)dreamDustPerWave * wave;
		_goldBase = (float)goldPerWave * wave;
	}

	public static bool TryGetDreamDust(out float amount)
	{
		amount = _dreamDustBase ?? 0f;
		return _dreamDustBase.HasValue;
	}

	public static bool TryGetGold(out float amount)
	{
		amount = _goldBase ?? 0f;
		return _goldBase.HasValue;
	}

	public static void Close()
	{
		_dreamDustBase = null;
		_goldBase = null;
	}
}
