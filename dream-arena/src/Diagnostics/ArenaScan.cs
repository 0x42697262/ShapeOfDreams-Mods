using System.Collections.Generic;
using DreamArena.Arena;
using DreamArena.Text;
using UnityEngine;

namespace DreamArena.Diagnostics;

internal static class ArenaScan
{
	private const EntityRelation Harmful = EntityRelation.Neutral | EntityRelation.Enemy;

	public static void Report()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		Hero reference = FirstHero(actors);
		if (reference == null)
		{
			Log.Info(LogText.ScanNoHero);
			return;
		}

		Log.Info(ArenaLayout.TryFind(out Vector3 center, out float radius, out string source)
			? LogText.ScanAnchor(source, center, radius)
			: LogText.ScanNoAnchor);

		int attackable = 0;
		foreach (Entity entity in actors.allEntities)
		{
			if (entity == null || entity == reference)
			{
				continue;
			}

			EntityRelation relation = reference.GetRelation(entity);
			bool canHit = entity.isActive && (relation & Harmful) == relation;
			if (canHit)
			{
				attackable++;
			}

			Log.Info(LogText.ScanEntity(canHit, entity.GetType().Name, entity.name,
				entity.owner != null ? entity.owner.name : null, relation, entity.isActive));
		}

		Dictionary<string, int> counts = new Dictionary<string, int>();
		foreach (Actor actor in actors.allActors)
		{
			if (actor != null)
			{
				string key = actor.GetType().Name;
				counts.TryGetValue(key, out int n);
				counts[key] = n + 1;
			}
		}

		foreach (KeyValuePair<string, int> pair in counts)
		{
			Log.Info(LogText.ScanActorCount(pair.Key, pair.Value));
		}

		Log.Info(LogText.ScanSummary(reference.name, attackable, actors.allEntities.Count, actors.allActors.Count));
	}

	private static Hero FirstHero(ActorManager actors)
	{
		if (actors == null)
		{
			return null;
		}

		foreach (Hero hero in actors.allHeroes)
		{
			if (!hero.IsNullOrInactive())
			{
				return hero;
			}
		}

		return null;
	}
}
