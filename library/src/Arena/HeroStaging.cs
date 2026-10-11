using System.Collections.Generic;
using UnityEngine;

namespace SodLib.Arena;

internal static class HeroStaging
{
	public static float PlaceAround(ArenaPoints arena, IReadOnlyList<Hero> heroes, float ringShare, float minRing)
	{
		float ring = ringShare > 0f ? Mathf.Max(minRing, arena.Radius * ringShare) : 0f;
		if (heroes.Count == 0)
		{
			return ring;
		}

		float step = SharedTuning.FullCircleDegrees / heroes.Count;
		for (int i = 0; i < heroes.Count; i++)
		{
			Vector3 offset = Quaternion.Euler(0f, step * i, 0f) * Vector3.forward * ring;
			Move(heroes[i], Dew.GetValidAgentDestination_LinearSweep(arena.Center, arena.Center + offset));
		}

		return ring;
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
