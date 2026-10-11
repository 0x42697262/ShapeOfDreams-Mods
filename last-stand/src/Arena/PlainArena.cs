using LastStand.Text;
using UnityEngine;

namespace LastStand.Arena;

internal sealed class PlainArena
{
	private Room _prepared;

	public void Prepare(Room room)
	{
		_prepared = null;
		if (room == null || !ArenaLocator.IsInArenaNode())
		{
			return;
		}

		room.isRevisit = true;
		_prepared = room;

		int triggers = 0;
		foreach (DewAllHeroesPresentZone zone in Object.FindObjectsByType<DewAllHeroesPresentZone>(
			         FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			zone.enabled = false;
			triggers++;
		}

		Log.Info(LogText.PlainArenaPrepared(room.name, triggers));
	}

	public void AfterStart(Room room)
	{
		if (room != null && room == _prepared)
		{
			room.RemoveObstacles();
		}
	}

	public void Reset()
	{
		_prepared = null;
	}
}
