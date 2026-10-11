using LastStand.Config;
using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Combat;

internal sealed class TestBoss
{
	private readonly HeroicBosses _bosses;
	private BossMonster _boss;

	public TestBoss(HeroicBosses bosses)
	{
		_bosses = bosses;
	}

	public void Spawn(string typeName)
	{
		if (!_boss.IsNullInactiveDeadOrKnockedOut())
		{
			Log.Warn(LogText.TestBossAlreadyAlive);
			return;
		}

		BossMonster prefab = HeroicBosses.Find(typeName);
		if (prefab == null)
		{
			Log.Warn(LogText.HeroicBossUnknown(typeName));
			return;
		}

		if (!HeroicBosses.IsSupported(prefab))
		{
			Log.Warn(LogText.HeroicBossUnsupported(typeName));
			return;
		}

		Hero host = DewPlayer.local != null ? DewPlayer.local.hero : null;
		if (host.IsNullInactiveDeadOrKnockedOut())
		{
			Log.Warn(LogText.TestBossNoHero);
			return;
		}

		Vector3 position = Dew.GetValidAgentDestination_Closest(host.agentPosition,
			host.agentPosition + Random.insideUnitSphere.Flattened().normalized * Tuning.TestBossDistance);

		_boss = _bosses.Spawn(prefab, position, Tuning.TestBossLevel, true);
		if (_boss == null)
		{
			return;
		}

		_boss.Status.AddStatBonus(new StatBonus
		{
			attackDamagePercentage = Tuning.TestBossDamagePercent,
			abilityPowerPercentage = Tuning.TestBossDamagePercent,
		});

		BossMonster spawned = _boss;
		Dew.CallDelayed(() => Weaken(spawned));

		Log.Info(LogText.TestBossSpawned(typeName, Tuning.TestBossLevel));
	}

	public void Forget()
	{
		_boss = null;
	}

	private static void Weaken(BossMonster boss)
	{
		if (!boss.IsNullInactiveDeadOrKnockedOut())
		{
			boss.Status.SetHealth(boss.maxHealth * Tuning.TestBossHealthShare);
		}
	}
}
