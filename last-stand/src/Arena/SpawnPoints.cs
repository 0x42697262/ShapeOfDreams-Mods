using System.Collections.Generic;
using LastStand.Game;
using UnityEngine;

namespace LastStand.Arena;

internal sealed class SpawnPoints
{
	private readonly List<RoomSection> _sections = new List<RoomSection>();
	private readonly List<Hero> _heroes = new List<Hero>();
	private float _totalArea;
	private float _minHeroDistance;

	public bool IsReady => _sections.Count > 0;

	public bool TryPrepare()
	{
		_sections.Clear();
		_totalArea = 0f;

		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || room.sections == null)
		{
			return false;
		}

		foreach (RoomSection section in room.sections)
		{
			if (section != null && section.monsters != null && section.area > 0f)
			{
				_sections.Add(section);
				_totalArea += section.area;
			}
		}

		return IsReady;
	}

	public Vector3 Next(float minHeroDistance)
	{
		_minHeroDistance = minHeroDistance;
		PlayerRoster.FillLiveHeroes(_heroes);
		return PickSection().monsters.GetSpawnPositionInSection(IsFarFromHeroes);
	}

	public void Clear()
	{
		_sections.Clear();
		_heroes.Clear();
		_totalArea = 0f;
	}

	private RoomSection PickSection()
	{
		float pick = Random.value * _totalArea;
		foreach (RoomSection section in _sections)
		{
			pick -= section.area;
			if (pick <= 0f)
			{
				return section;
			}
		}

		return _sections[_sections.Count - 1];
	}

	private bool IsFarFromHeroes(Vector3 position)
	{
		foreach (Hero hero in _heroes)
		{
			if (Vector2.Distance(hero.agentPosition.ToXY(), position.ToXY()) < _minHeroDistance)
			{
				return false;
			}
		}

		return true;
	}
}
