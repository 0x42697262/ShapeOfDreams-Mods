using System;
using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Text;

namespace DreamArena.Arena;

internal static class ArenaRooms
{
	private static readonly List<int> ModifierIds = new List<int>();

	public static void Apply(SessionSettings settings)
	{
		if (!settings.ForceArenaRoom)
		{
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return;
		}

		try
		{
			int changed = 0;
			int stripped = 0;
			for (int i = 0; i < zones.nodes.Count; i++)
			{
				if (i == zones.currentNodeIndex || zones.nodes[i].type == WorldNodeType.Start)
				{
					continue;
				}

				if (zones.nodes[i].roomOverride != settings.ArenaRoom)
				{
					zones.SetRoomOverride(i, settings.ArenaRoom);
					changed++;
				}

				if (settings.StripRoomModifiers)
				{
					stripped += StripModifiers(zones, i);
				}
			}

			Log.Info(LogText.ArenaRoomApplied(settings.ArenaRoom, changed, stripped));
		}
		catch (Exception e)
		{
			Log.Error(LogText.ArenaRoomFailed(settings.ArenaRoom), e);
		}
	}

	private static int StripModifiers(ZoneManager zones, int node)
	{
		List<ModifierData> modifiers = zones.nodes[node].modifiers;
		if (modifiers == null || modifiers.Count == 0)
		{
			return 0;
		}

		ModifierIds.Clear();
		foreach (ModifierData modifier in modifiers)
		{
			ModifierIds.Add(modifier.id);
		}

		foreach (int id in ModifierIds)
		{
			zones.RemoveModifier(node, id);
		}

		return ModifierIds.Count;
	}
}
