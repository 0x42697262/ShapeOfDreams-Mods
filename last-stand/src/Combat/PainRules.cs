using System;
using LastStand.Config;

namespace LastStand.Combat;

internal static class PainRules
{
	public static float TickDamage(float maxHealth, float currentHealth, float shield, float ratioPerSecond,
		float tickSeconds, int stacks)
	{
		if (stacks <= 0)
		{
			return 0f;
		}

		float wanted = (maxHealth + shield) * ratioPerSecond * tickSeconds * stacks;
		float allowed = currentHealth + shield - Tuning.PainHealthFloor;
		float damage = Math.Min(wanted, allowed);
		return damage < Tuning.MinimumPainDamage ? 0f : damage;
	}
}
