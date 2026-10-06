using HarmonyLib;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Room), nameof(Room.OnLateStart))]
internal static class ArenaMusicPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		if (LastStandMode.IsHosting())
		{
			LastStandMod.Current?.Session?.OnRoomMusicStarted(__instance);
		}
	}
}
