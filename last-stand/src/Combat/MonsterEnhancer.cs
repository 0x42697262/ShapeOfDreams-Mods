using LastStand.Config;

namespace LastStand.Combat;

internal static class MonsterEnhancer
{
	public static void ApplyWaveScaling(Entity entity, double healthBonusPercent, double damageBonusPercent)
	{
		if (healthBonusPercent <= 0 && damageBonusPercent <= 0)
		{
			return;
		}

		entity.Status.AddStatBonus(new StatBonus
		{
			maxHealthPercentage = (float)healthBonusPercent,
			attackDamagePercentage = (float)damageBonusPercent,
			abilityPowerPercentage = (float)damageBonusPercent,
		});
	}

	public static void Multiply(Entity entity, double health, double damage)
	{
		if (entity.IsNullInactiveDeadOrKnockedOut())
		{
			return;
		}

		float damagePercent = ExtraPercent(damage);
		entity.Status.AddStatBonus(new StatBonus
		{
			maxHealthPercentage = ExtraPercent(health),
			attackDamagePercentage = damagePercent,
			abilityPowerPercentage = damagePercent,
		});
	}

	private static float ExtraPercent(double multiplier)
	{
		return (float)((multiplier - 1) * Tuning.PercentScale);
	}

	public static void AddAbilityHaste(Entity entity, double haste)
	{
		if (entity.IsNullInactiveDeadOrKnockedOut() || haste <= 0)
		{
			return;
		}

		entity.Status.AddStatBonus(new StatBonus { abilityHasteFlat = (float)haste });
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
