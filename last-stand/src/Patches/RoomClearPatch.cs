using HarmonyLib;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Room), nameof(Room.ClearRoom))]
internal static class RoomClearPatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !ModMode.IsHosting() || LastStandMod.Current?.Session?.AllowsRoomClear() != false;
	}
}
