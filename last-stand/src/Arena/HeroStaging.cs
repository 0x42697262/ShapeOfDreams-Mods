using System.Collections.Generic;
using LastStand.Config;
using UnityEngine;

namespace LastStand.Arena;

internal static class HeroStaging
{
	public static void PlaceAround(ArenaPoints arena, IReadOnlyList<Hero> heroes)
	{
		if (heroes.Count == 0)
		{
			return;
		}

		float ring = Mathf.Max(Tuning.MinHeroRingRadius, arena.Radius * Tuning.HeroRingShareOfRadius);
		float step = Tuning.FullCircleDegrees / heroes.Count;

		for (int i = 0; i < heroes.Count; i++)
		{
			Vector3 offset = Quaternion.Euler(0f, step * i, 0f) * Vector3.forward * ring;
			Vector3 target = Dew.GetValidAgentDestination_LinearSweep(arena.Center, arena.Center + offset);
			Move(heroes[i], target);
		}
	}

	private static void Move(Hero hero, Vector3 target)
	{
		hero.Control.CancelOngoingChannels();
		hero.Control.CancelOngoingDisplacement();
		hero.Control.Teleport(target);

		foreach (Summon summon in hero.summons)
		{
			summon.Control.Teleport(target + Random.onUnitSphere.Flattened());
		}
	}
}
