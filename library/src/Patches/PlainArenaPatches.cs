using HarmonyLib;

namespace SodLib.Patches;

[HarmonyPatch(typeof(Room), nameof(Room.OnStartServer))]
internal static class PlainArenaStartServerPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		if (ModMode.IsHosting())
		{
			PlainArena.Prepare(__instance);
		}
		else
		{
			PlainArena.Reset();
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
			PlainArena.AfterStart(__instance);
		}
	}
}
