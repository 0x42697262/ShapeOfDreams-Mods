using HarmonyLib;
using DreamArena.Arena;
using DreamArena.Mode;

namespace DreamArena.Patches;

[HarmonyPatch(typeof(Room), nameof(Room.OnStartServer))]
internal static class PlainArenaStartServerPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		if (__instance != null && ArenaMode.IsHosting() && DreamArenaMod.Current?.Session?.Settings?.PlainArena == true
		    && ArenaLocator.CurrentPhase() == Phase.Arena)
		{
			PlainArena.Prepare(__instance);
		}
		else
		{
			PlainArena.Forget();
		}
	}
}

[HarmonyPatch(typeof(Room), nameof(Room.StartRoom))]
internal static class PlainArenaStartRoomPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		PlainArena.AfterStart(__instance);
	}
}
