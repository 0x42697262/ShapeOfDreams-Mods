using HarmonyLib;
using LastStand.Game;

namespace LastStand.Patches;

[HarmonyPatch(typeof(UI_InGame_Location), GameNames.LocationRoomLoaded)]
internal static class LocationLabelPatch
{
	[HarmonyPostfix]
	private static void Postfix(UI_InGame_Location __instance)
	{
		LastStandMod.Current?.Hud.Capture(__instance);
	}
}
