using System.Collections.Generic;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Reports what a hero could legally attack in the current room.
/// </summary>
/// <remarks>
/// Written for the case where a hero auto-attacks with nothing visible on
/// screen. Auto-attacks pick targets through <c>AbilityTargetValidator</c>,
/// which accepts anything active whose relation is Neutral or Enemy — so an
/// invisible or untargetable-looking entity can still be drawing fire. This
/// lists every candidate with the facts that decide it, rather than guessing.
/// </remarks>
internal static class ArenaDiagnostics
{
	/// <summary>The relations a harmful ability accepts by default.</summary>
	private const EntityRelation HarmfulTargets = EntityRelation.Neutral | EntityRelation.Enemy;

	public static void ReportTargets()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.instance;
		if (actors == null)
		{
			Debug.Log($"{ModIdentity.LogPrefix}no actor manager; not in a run.");
			return;
		}

		Hero reference = null;
		foreach (Hero hero in actors.allHeroes)
		{
			if (!hero.IsNullOrInactive())
			{
				reference = hero;
				break;
			}
		}

		if (reference == null)
		{
			Debug.Log($"{ModIdentity.LogPrefix}no living hero to measure relations from.");
			return;
		}

		List<Hero> heroes = new List<Hero>(actors.allHeroes);
		if (ArenaSpawn.TryGetArena(heroes, out Vector3 anchor, out float anchorRadius, out string anchorSource))
		{
			Debug.Log($"{ModIdentity.LogPrefix}arena anchor: {anchorSource} at {anchor} " +
			          $"(radius {anchorRadius:0.#}).");
		}
		else
		{
			Debug.Log($"{ModIdentity.LogPrefix}arena anchor: NONE FOUND in this room.");
		}

		List<string> attackable = new List<string>();
		List<string> ignored = new List<string>();

		foreach (Entity entity in actors.allEntities)
		{
			if (entity == null || entity == reference)
			{
				continue;
			}

			string line = Describe(reference, entity);

			if (IsAttackable(reference, entity))
			{
				attackable.Add(line);
			}
			else
			{
				ignored.Add(line);
			}
		}

		Debug.Log($"{ModIdentity.LogPrefix}scan from {reference.name}: " +
		          $"{attackable.Count} attackable, {ignored.Count} ignored, " +
		          $"{actors.allActors.Count} actors total.");

		foreach (string line in attackable)
		{
			Debug.Log($"{ModIdentity.LogPrefix}  ATTACKABLE  {line}");
		}

		foreach (string line in ignored)
		{
			Debug.Log($"{ModIdentity.LogPrefix}  ignored     {line}");
		}
	}

	/// <summary>Mirrors what <c>AbilityTargetValidator</c> does for a harmful ability.</summary>
	private static bool IsAttackable(Hero self, Entity target)
	{
		if (!target.isActive)
		{
			return false;
		}

		EntityRelation relation = self.GetRelation(target);
		return (relation & HarmfulTargets) == relation;
	}

	private static string Describe(Hero self, Entity entity)
	{
		EntityStatus status = entity.Status;

		string flags = "";
		if (status != null)
		{
			if (status.hasUntargetable) flags += " untargetable";
			if (status.hasInvisible) flags += " invisible";
			if (status.hasInvulnerable) flags += " invulnerable";
		}

		string owner = entity.owner != null ? entity.owner.name : "<no owner>";

		return $"{entity.GetType().Name} '{entity.name}' owner={owner} " +
		       $"relation={self.GetRelation(entity)} active={entity.isActive}{flags}";
	}
}
