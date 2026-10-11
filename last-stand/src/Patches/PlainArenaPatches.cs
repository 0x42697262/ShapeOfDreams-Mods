using HarmonyLib;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Room), nameof(Room.OnStartServer))]
internal static class PlainArenaStartServerPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		if (ModMode.IsHosting())
		{
			LastStandMod.Current?.Session?.PlainArena.Prepare(__instance);
		}
	}
}

[HarmonyPatch(typeof(Room), nameof(Room.StartRoom))]
internal static class PlainArenaStartRoomPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		if (ModMode.IsHosting())
		{
			LastStandMod.Current?.Session?.PlainArena.AfterStart(__instance);
		}
	}
}
