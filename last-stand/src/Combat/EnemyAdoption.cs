using System.Linq;

namespace LastStand.Combat;

internal static class EnemyAdoption
{
	public static int AdoptSurvivors(EnemyRegistry enemies)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int adopted = 0;
		foreach (Entity entity in actors.allEntities.ToArray())
		{
			if (!(entity is Monster monster) || monster.IsNullInactiveDeadOrKnockedOut()
			    || monster.owner != DewPlayer.creep || HasEntityAncestor(monster))
			{
				continue;
			}

			enemies.Adopt(monster, TierOf(monster));
			adopted++;
		}

		return adopted;
	}

	private static EnemyTier TierOf(Monster monster)
	{
		if (monster is BossMonster)
		{
			return EnemyTier.HeroicBoss;
		}

		if (monster.type == Monster.MonsterType.MiniBoss)
		{
			return EnemyTier.MiniBoss;
		}

		return monster.Status.HasStatusEffect<Se_HunterBuff>() ? EnemyTier.Hunter : EnemyTier.Regular;
	}

	private static bool HasEntityAncestor(Actor actor)
	{
		for (Actor parent = actor.parentActor; parent != null; parent = parent.parentActor)
		{
			if (parent is Entity)
			{
				return true;
			}
		}

		return false;
	}
}
