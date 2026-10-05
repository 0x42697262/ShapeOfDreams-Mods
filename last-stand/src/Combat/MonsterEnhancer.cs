using LastStand.Waves;

namespace LastStand.Combat;

internal static class MonsterEnhancer
{
	public static void ApplyWaveScaling(Entity entity, WavePlan plan)
	{
		if (plan.HealthBonusPercent <= 0 && plan.DamageBonusPercent <= 0)
		{
			return;
		}

		entity.Status.AddStatBonus(new StatBonus
		{
			maxHealthPercentage = (float)plan.HealthBonusPercent,
			attackDamagePercentage = (float)plan.DamageBonusPercent,
			abilityPowerPercentage = (float)plan.DamageBonusPercent,
		});
	}

	public static void MakeHunter(Entity entity)
	{
		if (!entity.Status.HasStatusEffect<Se_HunterBuff>())
		{
			entity.CreateStatusEffect<Se_HunterBuff>(entity, new CastInfo(entity));
		}
	}

	public static void ApplyMirage(Entity entity, MirageSkinEffect skin)
	{
		if (skin == null || !(entity is Monster monster) || monster.type == Monster.MonsterType.Lesser
		    || entity.Status.HasStatusEffect<MirageSkinEffect>())
		{
			return;
		}

		entity.CreateStatusEffect(skin.GetType(), entity, new CastInfo(entity));
	}
}
