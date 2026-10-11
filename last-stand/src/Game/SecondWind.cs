using System;

namespace LastStand.Game;

internal static class SecondWind
{
	public static int Grant(int charges, float invulnerableSeconds)
	{
		int heroes = 0;
		if (DewPlayer.gamePlayers == null)
		{
			return heroes;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			Hero hero = player != null ? player.hero : null;
			if (hero.IsNullOrInactive())
			{
				continue;
			}

			Apply(hero, charges, invulnerableSeconds);
			heroes++;
		}

		return heroes;
	}

	private static void Apply(Hero hero, int charges, float invulnerableSeconds)
	{
		if (hero.Status.TryGetStatusEffect(out Se_FatalHitProtection_Interrupt existing))
		{
			if (charges <= 0)
			{
				existing.Destroy();
				return;
			}

			Configure(existing, charges, invulnerableSeconds);
			return;
		}

		if (charges > 0)
		{
			hero.CreateStatusEffect(hero, new CastInfo(hero),
				(Se_FatalHitProtection_Interrupt effect) => Configure(effect, charges, invulnerableSeconds));
		}
	}

	private static void Configure(Se_FatalHitProtection_Interrupt effect, int charges, float invulnerableSeconds)
	{
		effect.maxStack = Math.Max(effect.maxStack, charges);
		effect.invulTime = invulnerableSeconds;
		effect.SetStack(charges);
	}
}
