using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Levels every hero to the cap, and their memories to a common floor.
/// </summary>
/// <remarks>
/// <para>
/// Experience is a PvE progression curve, and in a match it only rewards
/// whoever farmed most recently. Starting everyone at the cap means builds are
/// compared at full strength.
/// </para>
/// <para>
/// <c>EntityStatus.level</c> is server-only by design — it throws outright on a
/// client — and <c>Hero.maxLevel</c> resolves either the hero's own override or
/// the global <c>ges.maxHeroLevel</c>, so this respects whatever the game
/// considers the ceiling.
/// </para>
/// <para>
/// Memories are levelled the same way the Upgrade Well does it: writing
/// <c>SkillTrigger.level</c> on the server, which is a SyncVar, and skipping
/// any skill whose <c>isLevelUpEnabled</c> is off. Each traveler starts with
/// their own kit, identity included, at level 1; that is raised to the
/// configured floor. It is a floor, not a set value: nothing is ever lowered,
/// so memories taken from the camp remnants or upgraded higher keep their level.
/// Late joiners and swapped-in memories are caught on the next pass.
/// </para>
/// </remarks>
internal static class HeroLeveller
{
	/// <summary>Seconds between passes. Levels only ever need to catch up once.</summary>
	private const float Interval = 0.5f;

	private static readonly HeroSkillLocation[] SkillSlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.W,
		HeroSkillLocation.E,
		HeroSkillLocation.R,
		HeroSkillLocation.Identity,
		HeroSkillLocation.Movement,
	};

	private static float _timer;

	/// <summary>Skills already reported as not upgradable, so each is logged once.</summary>
	private static readonly HashSet<int> ReportedLocked = new HashSet<int>();

	public static void Tick(float deltaTime, DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null || !config.arenaEnabled)
		{
			return;
		}

		if (!config.maxPlayerLevel && config.startingMemoryLevel <= 1)
		{
			return;
		}

		_timer += deltaTime;
		if (_timer < Interval)
		{
			return;
		}

		_timer = 0f;

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		foreach (Hero hero in actors.allHeroes)
		{
			if (hero.IsNullOrInactive() || hero.Status == null)
			{
				continue;
			}

			if (config.maxPlayerLevel)
			{
				RaiseHeroLevel(hero);
			}

			if (config.startingMemoryLevel > 1)
			{
				RaiseMemories(hero, config.startingMemoryLevel);
			}
		}
	}

	private static void RaiseHeroLevel(Hero hero)
	{
		int cap = hero.maxLevel;
		if (cap <= 0 || hero.Status.level >= cap)
		{
			return;
		}

		hero.Status.level = cap;
		Debug.Log($"{ModIdentity.LogPrefix}levelled {hero.name} to {cap}.");
	}

	private static void RaiseMemories(Hero hero, int floor)
	{
		HeroSkill skills = hero.Skill;
		if (skills == null)
		{
			return;
		}

		foreach (HeroSkillLocation slot in SkillSlots)
		{
			SkillTrigger skill = skills.GetSkill(slot);
			if (skill == null || skill.level >= floor)
			{
				continue;
			}

			if (!skill.isLevelUpEnabled)
			{
				if (ReportedLocked.Add(skill.GetInstanceID()))
				{
					Debug.Log($"{ModIdentity.LogPrefix}left {hero.name}'s {slot} memory " +
					          $"{skill.GetType().Name} at level {skill.level}: the game " +
					          "marks it as not upgradable.");
				}

				continue;
			}

			int before = skill.level;
			skill.level = floor;

			Debug.Log($"{ModIdentity.LogPrefix}raised {hero.name}'s {slot} memory " +
			          $"{skill.GetType().Name} from level {before} to {floor}.");
		}
	}

	public static void Reset()
	{
		_timer = 0f;
		ReportedLocked.Clear();
	}
}
