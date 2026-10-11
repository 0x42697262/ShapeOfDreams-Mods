using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Text;

namespace DreamArena.Heroes;

internal sealed class HeroBoost
{
	private readonly HashSet<EntityStatus> _boosted = new HashSet<EntityStatus>();
	private readonly List<EntityStatus> _stale = new List<EntityStatus>();
	private readonly HashSet<int> _reportedLocked = new HashSet<int>();
	private float _multiplier = Tuning.NormalHealthMultiplier;
	private double _nextCheckAt;

	public void Tick(double now, SessionSettings settings)
	{
		if (!UnityEngine.Mathf.Approximately(settings.HeroHealthMultiplier, _multiplier))
		{
			RemoveHealth();
			_multiplier = settings.HeroHealthMultiplier;
		}

		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.HeroCheckSeconds;
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		PruneHealth();
		foreach (Hero hero in actors.allHeroes)
		{
			if (hero.IsNullOrInactive() || hero.Status == null)
			{
				continue;
			}

			int level = UnityEngine.Mathf.Min(settings.PlayerLevel, hero.maxLevel);
			if (level > Tuning.PlayerLevelOff && hero.Status.level < level)
			{
				hero.Status.level = level;
				Log.Info(LogText.HeroLevelled(hero.name, level));
			}

			if (settings.StartingMemoryLevel > Tuning.MemoryFloorOff)
			{
				RaiseMemories(hero, settings.StartingMemoryLevel);
			}

			if (_multiplier > Tuning.NormalHealthMultiplier && _boosted.Add(hero.Status))
			{
				hero.Status.finalStatsProcessors.Add(ScaleHealth);
				Log.Info(LogText.HealthBoosted(hero.name, _multiplier));
			}
		}
	}

	public void Reset()
	{
		RemoveHealth();
		_reportedLocked.Clear();
		_multiplier = Tuning.NormalHealthMultiplier;
		_nextCheckAt = 0;
	}

	private void RaiseMemories(Hero hero, int floor)
	{
		HeroSkill skills = hero.Skill;
		if (skills == null)
		{
			return;
		}

		foreach (HeroSkillLocation slot in Tuning.AllSkillSlots)
		{
			SkillTrigger skill = skills.GetSkill(slot);
			if (skill == null || skill.level >= floor)
			{
				continue;
			}

			if (!skill.isLevelUpEnabled)
			{
				if (_reportedLocked.Add(skill.GetInstanceID()))
				{
					Log.Info(LogText.MemoryLocked(hero.name, slot, skill.GetType().Name, skill.level));
				}

				continue;
			}

			Log.Info(LogText.MemoryRaised(hero.name, slot, skill.GetType().Name, skill.level, floor));
			skill.level = floor;
		}
	}

	private void ScaleHealth(ref FinalStats data)
	{
		data.maxHealth *= _multiplier;
	}

	private void PruneHealth()
	{
		_stale.Clear();
		foreach (EntityStatus status in _boosted)
		{
			if (status == null || status.entity.IsNullOrInactive())
			{
				_stale.Add(status);
			}
		}

		foreach (EntityStatus status in _stale)
		{
			_boosted.Remove(status);
		}

		_stale.Clear();
	}

	private void RemoveHealth()
	{
		foreach (EntityStatus status in _boosted)
		{
			if (status != null)
			{
				status.finalStatsProcessors.Remove(ScaleHealth);
			}
		}

		_boosted.Clear();
	}
}
