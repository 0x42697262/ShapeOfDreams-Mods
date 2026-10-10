using DreamArena.Diagnostics;
using DreamArena.Text;
using UnityEngine;

namespace DreamArena.Arena;

internal static class PlainArena
{
	private static Room _prepared;

	public static void Prepare(Room room)
	{
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

	public static void Forget()
	{
		_prepared = null;
	}

	public static void AfterStart(Room room)
	{
		if (room != null && room == _prepared)
		{
			room.RemoveObstacles();
		}
	}
}
