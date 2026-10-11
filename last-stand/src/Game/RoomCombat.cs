using System.Reflection;
using HarmonyLib;

namespace LastStand.Game;

internal static class RoomCombat
{
	private static readonly MethodInfo SetCleared = AccessTools.PropertySetter(typeof(Room), nameof(Room.didClearRoom));

	private static Room _opened;

	public static bool IsHeldOpen => _opened != null;

	public static void Open()
	{
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || !room.didClearRoom)
		{
			return;
		}

		SetCleared.Invoke(room, new object[] { false });
		_opened = room;
	}

	public static void Close()
	{
		Room room = _opened;
		_opened = null;
		if (room != null)
		{
			SetCleared.Invoke(room, new object[] { true });
		}
	}
}
