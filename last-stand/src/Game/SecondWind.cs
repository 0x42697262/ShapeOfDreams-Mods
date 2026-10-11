using System;

namespace LastStand.Game;

internal static class SecondWind
{
	public static int Grant(int charges, float invulnerableSeconds)
	{
		return PlayerRoster.ForEachHero(hero => Apply(hero, charges, invulnerableSeconds));
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
