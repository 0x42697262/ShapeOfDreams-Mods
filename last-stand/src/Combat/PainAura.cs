using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Combat;

internal sealed class PainAura
{
	private double _nextTickAt;

	public void Empower(Entity entity, PainSettings settings)
	{
		if (entity == null)
		{
			return;
		}

		entity.CreateStatusEffect<Se_LimboBossDecorator>(entity, default(CastInfo));
		entity.Status.AddStatBonus(new StatBonus
		{
			maxHealthPercentage = settings.HealthBonusPercent,
			attackDamagePercentage = settings.DamageBonusPercent,
			abilityPowerPercentage = settings.DamageBonusPercent,
		});
	}

	public void Tick(double now, IReadOnlyList<Entity> painBosses, PainSettings settings)
	{
		if (now < _nextTickAt)
		{
			return;
		}

		_nextTickAt = now + Tuning.PainTickSeconds;

		if (painBosses.Count == 0)
		{
			return;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		Entity source = painBosses[0];
		foreach (Hero hero in actors.allHeroes)
		{
			if (hero.IsNullInactiveDeadOrKnockedOut())
			{
				continue;
			}

			float damage = PainRules.TickDamage(hero.maxHealth, hero.currentHealth, hero.Status.currentShield,
				settings.DamageRatioPerSecond, Tuning.PainTickSeconds, painBosses.Count);
			if (damage <= 0f)
			{
				continue;
			}

			source.PureDamage(damage, Tuning.NoProcCoefficient)
				.SetAttr(DamageAttribute.DamageOverTime)
				.SetAttr(DamageAttribute.IgnoreArmor)
				.Dispatch(hero);
		}
	}

	public void Reset()
	{
		_nextTickAt = 0;
	}
}
