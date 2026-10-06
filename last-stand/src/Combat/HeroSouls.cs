using System;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Combat;

internal static class HeroSouls
{
	public static void Suppress(Se_HeroKnockedOut knockedOut)
	{
		if (knockedOut == null)
		{
			return;
		}

		knockedOut.disableQuest = true;

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		DewPlayer owner = knockedOut.victim != null ? knockedOut.victim.owner : null;
		if (zones == null || owner == null)
		{
			return;
		}

		for (int node = 0; node < zones.nodes.Count; node++)
		{
			foreach (ModifierData modifier in zones.nodes[node].modifiers.ToArray())
			{
				if (IsSoulOf(modifier, owner))
				{
					zones.RemoveModifier(node, modifier.id);
					Log.Info(LogText.HeroSoulRemoved(node));
				}
			}
		}
	}

	public static int ReviveKnockedOut()
	{
		if (Dew.GetAliveHeroCount() == 0)
		{
			return 0;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int revived = 0;
		foreach (Hero hero in actors.allHeroes.ToArray())
		{
			if (hero == null || !hero.isActive || !hero.isKnockedOut
			    || !hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
			{
				continue;
			}

			knockedOut.Revive();
			revived++;
		}

		return revived;
	}

	private static bool IsSoulOf(ModifierData modifier, DewPlayer owner)
	{
		return string.Equals(modifier.type, GameNames.HeroSoulModifier, StringComparison.Ordinal)
		       && string.Equals(modifier.clientData, owner.guid, StringComparison.Ordinal);
	}
}
