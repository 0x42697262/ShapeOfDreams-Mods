using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;
using LastStand.Waves;

namespace LastStand.Combat;

internal sealed class BossHealthFloor
{
	private static readonly EnemyTier[] MobTiers = { EnemyTier.Regular, EnemyTier.Hunter };

	private static readonly EnemyTier[] BossTiers = { EnemyTier.MiniBoss, EnemyTier.Pain, EnemyTier.HeroicBoss };

	private double _toughestMob;
	private double _nextCheckAt;

	public void StartWave()
	{
		_toughestMob = 0;
		_nextCheckAt = 0;
	}

	public void Tick(double now, WavePlan plan, EnemyRegistry enemies)
	{
		if (plan == null || !plan.HasBossHealthFloor || now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.BossHealthFloorCheckSeconds;

		foreach (EnemyTier tier in MobTiers)
		{
			foreach (Entity mob in enemies.AliveOf(tier))
			{
				if (mob.Status.maxHealth > _toughestMob)
				{
					_toughestMob = mob.Status.maxHealth;
				}
			}
		}

		if (_toughestMob <= 0)
		{
			return;
		}

		foreach (EnemyTier tier in BossTiers)
		{
			double multiple = tier == EnemyTier.HeroicBoss
				? plan.HeroicBossHealthMultiple
				: plan.MiniBossHealthMultiple;
			if (multiple <= 0)
			{
				continue;
			}

			foreach (Entity boss in enemies.AliveOf(tier))
			{
				Raise(boss, multiple, plan.Number);
			}
		}
	}

	private void Raise(Entity boss, double multiple, int wave)
	{
		double target = multiple * _toughestMob;
		double before = boss.Status.maxHealth;
		double extra = MergeRule.ExtraPercentToReach(before, target);
		if (extra <= 0)
		{
			return;
		}

		boss.Status.AddStatBonus(new StatBonus { maxHealthPercentage = (float)extra });
		Log.Info(LogText.BossHealthRaised(wave, boss.GetType().Name, before, boss.Status.maxHealth, multiple,
			_toughestMob));
	}
}
