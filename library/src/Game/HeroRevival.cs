namespace SodLib.Game;

internal static class HeroRevival
{
	public static int ReviveAll(float healthRatio)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int revived = 0;
		foreach (Hero hero in actors.allHeroes.ToArray())
		{
			if (Revive(hero, healthRatio))
			{
				revived++;
			}
		}

		return revived;
	}

	public static bool Revive(Hero hero, float healthRatio)
	{
		if (hero == null || !hero.isActive || !hero.isKnockedOut
		    || !hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
		{
			return false;
		}

		knockedOut.Revive(healthRatio);
		return true;
	}

	public static bool AnyHeroStanding()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return false;
		}

		foreach (Hero hero in actors.allHeroes)
		{
			if (hero != null && hero.isActive && !hero.isKnockedOut)
			{
				return true;
			}
		}

		return false;
	}
}
