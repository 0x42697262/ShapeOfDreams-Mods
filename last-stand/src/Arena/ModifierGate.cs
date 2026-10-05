using System;

namespace LastStand.Arena;

internal static class ModifierGate
{
	private static int _ownAdds;

	public static int AddOwn<T>(ZoneManager zones, int nodeIndex, Action<T> beforePrepare) where T : RoomModifierBase
	{
		_ownAdds++;
		try
		{
			return zones.AddModifier(nodeIndex, beforePrepare);
		}
		finally
		{
			_ownAdds--;
		}
	}

	public static bool Allows(ZoneManager zones, int nodeIndex, string modifierType)
	{
		return _ownAdds > 0
		       || !ArenaLocator.IsArenaNode(zones, nodeIndex)
		       || modifierType == typeof(RoomMod_HeroSoul).Name;
	}

	public static int Strip(ZoneManager zones, int nodeIndex)
	{
		if (!ArenaLocator.IsArenaNode(zones, nodeIndex) || zones.nodes[nodeIndex].modifiers == null)
		{
			return 0;
		}

		int[] ids = zones.nodes[nodeIndex].modifiers.ConvertAll(modifier => modifier.id).ToArray();
		foreach (int id in ids)
		{
			zones.RemoveModifier(nodeIndex, id);
		}

		return ids.Length;
	}
}
