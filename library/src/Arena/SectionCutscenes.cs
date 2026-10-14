using System;
using UnityEngine;
using UnityEngine.Events;

namespace SodLib.Arena;

internal static class SectionCutscenes
{
	private static readonly ErrorThrottle Errors = new ErrorThrottle(SharedTuning.ErrorLogIntervalSeconds);

	public static void SwitchOff(Room room)
	{
		if (room == null || !ArenaLocator.IsInArenaNode() || !SkipsCutscenes())
		{
			return;
		}

		try
		{
			Log.Info(SharedLogText.SectionCutscenesOff(room.name, SwitchOffAll()));
		}
		catch (Exception e)
		{
			Errors.Report(Time.time, SharedLogText.SectionCutscenesFailed, e);
		}
	}

	private static bool SkipsCutscenes()
	{
		return ModEntry.Active != null && ModEntry.Active.SkipsCutscene();
	}

	private static int SwitchOffAll()
	{
		int count = 0;
		foreach (RoomSection section in UnityEngine.Object.FindObjectsByType<RoomSection>(
			         FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			count += SwitchOff(section.onEnterFirstTime)
			         + SwitchOff(section.onEveryonePresent)
			         + SwitchOff(section.onEntityEnter)
			         + SwitchOff(section.onEntityExit)
			         + SwitchOff(section.onEntitiesChanged);
		}

		return count;
	}

	private static int SwitchOff(UnityEventBase sectionEvent)
	{
		if (sectionEvent == null)
		{
			return 0;
		}

		int count = 0;
		for (int i = 0; i < sectionEvent.GetPersistentEventCount(); i++)
		{
			if (sectionEvent.GetPersistentTarget(i) is DewCutsceneDirector)
			{
				sectionEvent.SetPersistentListenerState(i, UnityEventCallState.Off);
				count++;
			}
		}

		return count;
	}
}
