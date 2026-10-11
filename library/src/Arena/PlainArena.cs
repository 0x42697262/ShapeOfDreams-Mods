using UnityEngine;

namespace SodLib.Arena;

internal static class PlainArena
{
	private static Room _prepared;

	public static bool Enabled { get; set; } = true;

	public static void Prepare(Room room)
	{
		_prepared = null;
		if (room == null || !Enabled || !ArenaLocator.IsInArenaNode())
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

		Log.Info(SharedLogText.PlainArenaPrepared(room.name, triggers));
	}

	public static void AfterStart(Room room)
	{
		if (room != null && room == _prepared)
		{
			room.RemoveObstacles();
		}
	}

	public static void Reset()
	{
		_prepared = null;
	}
}
