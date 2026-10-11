using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Text;
using UnityEngine;

namespace DreamArena.Arena;

internal static class HeroStaging
{
	private static readonly List<Hero> Heroes = new List<Hero>();

	public static bool TryPlaceInRing()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null || !ArenaLayout.TryFind(out Vector3 center, out float radius, out string source))
		{
			return false;
		}

		Heroes.Clear();
		foreach (Hero hero in actors.allHeroes)
		{
			if (!hero.IsNullInactiveDeadOrKnockedOut())
			{
				Heroes.Add(hero);
			}
		}

		if (Heroes.Count == 0)
		{
			return false;
		}

		float ring = Mathf.Max(Tuning.MinRingRadius, radius * Tuning.ArenaRingShare);
		float step = Tuning.FullCircleDegrees / Heroes.Count;
		for (int i = 0; i < Heroes.Count; i++)
		{
			Vector3 offset = Quaternion.Euler(0f, step * i, 0f) * Vector3.forward * ring;
			Place(Heroes[i], Dew.GetValidAgentDestination_LinearSweep(center, center + offset));
		}

		Log.Info(LogText.HeroesStaged(Heroes.Count, source, center, ring));
		Heroes.Clear();
		return true;
	}

	private static void Place(Hero hero, Vector3 target)
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
