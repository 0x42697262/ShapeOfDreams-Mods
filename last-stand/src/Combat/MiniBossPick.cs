namespace LastStand.Combat;

internal static class MiniBossPick
{
	private static Entity _monster;
	private static MiniBossEffect _effect;

	public static void Set(Entity monster, MiniBossEffect effect)
	{
		_monster = monster;
		_effect = effect;
	}

	public static void Clear()
	{
		_monster = null;
		_effect = null;
	}

	public static bool TryTake(out Entity monster, out MiniBossEffect effect)
	{
		monster = _monster;
		effect = _effect;
		return monster != null && effect != null;
	}
}
