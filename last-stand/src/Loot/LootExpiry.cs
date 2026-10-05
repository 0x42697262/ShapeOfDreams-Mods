using System.Collections.Generic;
using LastStand.Config;

namespace LastStand.Loot;

internal sealed class LootExpiry
{
	private readonly Dictionary<Actor, double> _expiresAt = new Dictionary<Actor, double>();
	private readonly List<Actor> _done = new List<Actor>();
	private double _nextCheckAt;

	public int TrackedCount => _expiresAt.Count;

	public void Track(Actor item, double expiresAt)
	{
		if (item != null)
		{
			_expiresAt[item] = expiresAt;
		}
	}

	public void Tick(double now)
	{
		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.LootExpiryCheckSeconds;

		_done.Clear();
		foreach (KeyValuePair<Actor, double> entry in _expiresAt)
		{
			Actor item = entry.Key;
			if (item.IsNullOrInactive() || IsClaimed(item))
			{
				_done.Add(item);
				continue;
			}

			if (now >= entry.Value)
			{
				item.Destroy();
				_done.Add(item);
			}
		}

		foreach (Actor item in _done)
		{
			_expiresAt.Remove(item);
		}
	}

	public void Clear()
	{
		_expiresAt.Clear();
		_done.Clear();
		_nextCheckAt = 0;
	}

	private static bool IsClaimed(Actor item)
	{
		switch (item)
		{
			case SkillTrigger skill:
				return skill.owner != null || skill.handOwner != null;
			case Gem gem:
				return gem.owner != null || gem.handOwner != null;
			default:
				return true;
		}
	}
}
