using System;
using LastStand.Text;

namespace LastStand.Combat;

internal sealed class HeroicBossReport
{
	private readonly Entity _boss;
	private readonly int _wave;
	private float _biggestHit;
	private string _biggestHitSource;

	private HeroicBossReport(Entity boss, int wave)
	{
		_boss = boss;
		_wave = wave;
	}

	public static void Watch(Entity boss, int wave)
	{
		if (boss.IsNullInactiveDeadOrKnockedOut())
		{
			return;
		}

		HeroicBossReport report = new HeroicBossReport(boss, wave);
		EntityStatus status = boss.Status;
		Log.Info(LogText.HeroicBossStats(wave, Name(boss), status.maxHealth, status.attackDamage,
			status.abilityPower, status.abilityHaste));
		boss.ActorEvent_OnDealDamage += (Action<EventInfoDamage>)report.OnDealDamage;
		boss.EntityEvent_OnDeath += (Action<EventInfoKill>)report.OnDeath;
	}

	private void OnDealDamage(EventInfoDamage info)
	{
		float hit = info.damage.amount + info.negatedAmountByShield;
		if (!(info.victim is Hero) || hit <= _biggestHit)
		{
			return;
		}

		_biggestHit = hit;
		_biggestHitSource = info.actor != null ? info.actor.GetType().Name : null;
	}

	private void OnDeath(EventInfoKill kill)
	{
		Log.Info(LogText.HeroicBossBiggestHit(_wave, Name(_boss), _biggestHit, _biggestHitSource));
	}

	private static string Name(Entity boss)
	{
		return boss != null ? boss.GetType().Name : string.Empty;
	}
}
