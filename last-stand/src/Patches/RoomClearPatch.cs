using HarmonyLib;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Room), nameof(Room.ClearRoom))]
internal static class RoomClearPatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !LastStandMode.IsHosting() || LastStandMod.Current?.Session?.AllowsRoomClear() != false;
	}
}
