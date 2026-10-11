using System.Collections.Generic;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Arena;

internal sealed class SpawnPoints
{
	private readonly List<RoomSection> _sections = new List<RoomSection>();
	private readonly List<Vector3> _anchors = new List<Vector3>();
	private readonly List<Hero> _heroes = new List<Hero>();
	private Vector3 _anchorCenter;
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

		if (IsReady && SpawnAnchors.TryFill(_anchors, out _anchorCenter))
		{
			Log.Info(LogText.SpawnAnchors(_anchors.Count, SpawnAnchors.Source));
		}

		return IsReady;
	}

	public Vector3 Next(float minHeroDistance)
	{
		_minHeroDistance = minHeroDistance;
		PlayerRoster.FillLiveHeroes(_heroes);
		if (_anchors.Count > 0)
		{
			return NextAnchor();
		}

		return PickSection().monsters.GetSpawnPositionInSection(IsFarFromHeroes);
	}

	public void Clear()
	{
		_sections.Clear();
		_anchors.Clear();
		_heroes.Clear();
		_totalArea = 0f;
	}

	private Vector3 NextAnchor()
	{
		int start = Random.Range(0, _anchors.Count);
		for (int i = 0; i < _anchors.Count; i++)
		{
			Vector3 anchor = _anchors[(start + i) % _anchors.Count];
			if (IsFarFromHeroes(anchor))
			{
				return Inward(anchor);
			}
		}

		return Inward(_anchors[start]);
	}

	private Vector3 Inward(Vector3 anchor)
	{
		Vector3 wanted = Vector3.MoveTowards(anchor, _anchorCenter, Random.value * Tuning.SpawnAnchorDepthMeters);
		return Dew.GetValidAgentDestination_Closest(anchor, wanted);
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
