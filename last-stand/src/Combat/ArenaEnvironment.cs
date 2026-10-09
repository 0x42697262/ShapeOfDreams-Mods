using System;
using System.Collections.Generic;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Combat;

internal sealed class ArenaEnvironment
{
	private readonly ArenaModifier _modifier = new ArenaModifier();
	private RoomMonsters _monsters;
	private MonsterSpawnRule _defaultRule;

	public string Current { get; private set; }

	public bool IsActive => _modifier.IsActive;

	public bool Begin(IReadOnlyList<string> types)
	{
		End();

		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (types.Count == 0 || room == null || room.monsters == null)
		{
			return false;
		}

		string type = types[UnityEngine.Random.Range(0, types.Count)];
		_monsters = room.monsters;
		_defaultRule = _monsters.defaultRule;
		RoomCombat.Open();

		bool added;
		try
		{
			added = _modifier.Add(type);
		}
		catch (Exception e)
		{
			Log.Error(LogText.EnvironmentFailed(type), e);
			added = false;
		}

		if (!added)
		{
			_modifier.Remove();
			RestoreRoom();
			return false;
		}

		Current = type;
		return true;
	}

	public void End()
	{
		if (Current == null)
		{
			return;
		}

		Current = null;
		_modifier.Remove();
		RestoreRoom();
	}

	private void RestoreRoom()
	{
		if (_monsters != null)
		{
			_monsters.defaultRule = _defaultRule;
		}

		_monsters = null;
		_defaultRule = null;
		RoomCombat.Close();
	}
}
