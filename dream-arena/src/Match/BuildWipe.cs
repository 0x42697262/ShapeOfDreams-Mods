using System.Collections.Generic;
using DreamArena.Config;
using UnityEngine;

namespace DreamArena.Match;

internal static class BuildWipe
{
	private static readonly List<GemLocation> GemSlots = new List<GemLocation>();
	private static readonly List<Actor> Loose = new List<Actor>();

	public static void Wipe(Hero hero)
	{
		HeroSkill skills = hero.Skill;
		Vector3 at = hero.position;
		GemSlots.Clear();
		foreach (HeroSkillLocation slot in Tuning.AllSkillSlots)
		{
			foreach (KeyValuePair<GemLocation, Gem> pair in skills.GetGemsPairInSkill(slot))
			{
				GemSlots.Add(pair.Key);
			}
		}

		foreach (GemLocation location in GemSlots)
		{
			Gem gem = skills.UnequipGem(location, at);
			if (!gem.IsNullOrInactive())
			{
				gem.Destroy();
			}
		}

		GemSlots.Clear();
		foreach (HeroSkillLocation slot in Tuning.MemorySlots)
		{
			if (!skills.TryGetSkill(slot, out SkillTrigger _))
			{
				continue;
			}

			SkillTrigger removed = skills.UnequipSkill(slot, at, ignoreCanReplace: true);
			if (!removed.IsNullOrInactive())
			{
				removed.Destroy();
			}
		}
	}

	public static void RestoreLoadout(Hero hero)
	{
		if (hero.loadout == null)
		{
			return;
		}

		foreach (HeroSkillLocation slot in Tuning.LoadoutMemorySlots)
		{
			int choice = hero.loadout.GetSkill(slot);
			SkillTrigger[] options = hero.Skill.GetLoadoutSkills(slot);
			if (choice < 0 || options == null || options.Length == 0)
			{
				continue;
			}

			SkillTrigger prefab = options[Mathf.Clamp(choice, 0, options.Length - 1)];
			SkillTrigger created = Dew.CreateSkillTrigger(prefab, hero.position, Tuning.StarterMemoryLevel);
			hero.Skill.EquipSkill(slot, created, ignoreCanReplace: true);
		}
	}

	public static int SweepLooseItems()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		Loose.Clear();
		foreach (Actor actor in actors.allActors)
		{
			bool loose = (actor is SkillTrigger skill && skill.owner == null) || (actor is Gem gem && gem.owner == null);
			if (loose && !actor.IsNullOrInactive())
			{
				Loose.Add(actor);
			}
		}

		foreach (Actor actor in Loose)
		{
			actor.Destroy();
		}

		int count = Loose.Count;
		Loose.Clear();
		return count;
	}
}
