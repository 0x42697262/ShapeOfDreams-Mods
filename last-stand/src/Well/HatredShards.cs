namespace LastStand.Well;

internal static class HatredShards
{
	private const float NoGold = 0f;

	private static float? _dreamDustBase;
	private static int? _gold;

	public static float Open(Shrine_Hatred_Reward shard, int wave, int dreamDustPerWave, int gold)
	{
		if (gold <= 0)
		{
			shard.goldWeight = NoGold;
		}

		float dreamDust = (float)dreamDustPerWave * wave;
		_dreamDustBase = dreamDust;
		_gold = gold;
		return dreamDust;
	}

	public static bool TryGetDreamDust(out float amount)
	{
		amount = _dreamDustBase ?? 0f;
		return _dreamDustBase.HasValue;
	}

	public static bool TryGetGold(out int amount)
	{
		amount = _gold ?? 0;
		return _gold.HasValue;
	}

	public static void Close()
	{
		_dreamDustBase = null;
		_gold = null;
	}
}
