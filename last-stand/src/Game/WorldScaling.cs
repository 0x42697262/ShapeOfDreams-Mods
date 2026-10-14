using LastStand.Config;

namespace LastStand.Game;

internal static class WorldScaling
{
	private const double ZoneIndexOfWorldOne = 0;

	private const double FirstSearchSpan = 1;

	private const int SearchSteps = 40;

	private const int MaxSpanDoublings = 20;

	private const double NoBonus = 0;

	public static double RealZoneIndex()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		return zones != null ? zones.currentZoneIndex : ZoneIndexOfWorldOne;
	}

	public static int RealWorld()
	{
		return (int)RealZoneIndex() + Tuning.WorldOfZoneIndexZero;
	}

	public static void BonusPercentFor(Entity entity, double extraWorlds, out double healthPercent,
		out double damagePercent)
	{
		healthPercent = NoBonus;
		damagePercent = NoBonus;
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null || !(entity is Monster monster) || extraWorlds <= 0)
		{
			return;
		}

		float real = (float)RealZoneIndex();
		float raised = (float)(real + extraWorlds);
		healthPercent = ExtraPercent(Health(game, monster, raised), Health(game, monster, real));
		damagePercent = ExtraPercent(Damage(game, monster, raised), Damage(game, monster, real));
	}

	public static double ExtraWorldsForBossHealth(double baseExtraWorlds, double healthMultiplier)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null || healthMultiplier <= 1)
		{
			return baseExtraWorlds;
		}

		double real = RealZoneIndex();
		double from = real + baseExtraWorlds;
		double target = game.GetBossMonsterHealthMultiplierByScaling((float)from) * healthMultiplier;
		double low = from;
		double high = from + FirstSearchSpan;
		for (int i = 0; i < MaxSpanDoublings && game.GetBossMonsterHealthMultiplierByScaling((float)high) < target; i++)
		{
			low = high;
			high = from + (high - from) * 2;
		}

		for (int i = 0; i < SearchSteps; i++)
		{
			double middle = (low + high) / 2;
			if (game.GetBossMonsterHealthMultiplierByScaling((float)middle) < target)
			{
				low = middle;
			}
			else
			{
				high = middle;
			}
		}

		return high - real;
	}

	private static float Health(GameManager game, Monster monster, float zoneIndex)
	{
		if (monster is BossMonster)
		{
			return game.GetBossMonsterHealthMultiplierByScaling(zoneIndex);
		}

		return monster.type == Monster.MonsterType.MiniBoss
			? game.GetMiniBossMonsterHealthMultiplierByScaling(zoneIndex)
			: game.GetRegularMonsterHealthMultiplierByScaling(zoneIndex);
	}

	private static float Damage(GameManager game, Monster monster, float zoneIndex)
	{
		if (monster is BossMonster)
		{
			return game.GetBossMonsterDamageMultiplierByScaling(zoneIndex);
		}

		return monster.type == Monster.MonsterType.MiniBoss
			? game.GetMiniBossMonsterDamageMultiplierByScaling(zoneIndex)
			: game.GetRegularMonsterDamageMultiplierByScaling(zoneIndex);
	}

	private static double ExtraPercent(float raised, float real)
	{
		return real <= 0 ? NoBonus : (raised / real - 1) * SharedTuning.PercentScale;
	}
}
