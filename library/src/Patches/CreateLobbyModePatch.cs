using System;
using HarmonyLib;

namespace SodLib.Patches;

[HarmonyPatch(typeof(UI_Title_CreateLobbyView))]
internal static class CreateLobbyModePatch
{
	[HarmonyPostfix]
	[HarmonyPatch(SharedGameNames.CreateLobbyShow)]
	private static void OnShow_Postfix(UI_Title_CreateLobbyView __instance)
	{
		if (__instance == null || __instance.gameModeDropdown == null)
		{
			return;
		}

		try
		{
			LobbyModeOption.Add(__instance);
		}
		catch (Exception e)
		{
			Log.Error(SharedLogText.ModeOptionFailed, e);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(SharedGameNames.CreateLobbyUpdateState)]
	private static void UpdateUIState_Postfix(UI_Title_CreateLobbyView __instance)
	{
		if (__instance == null || __instance.gameModeDropdown == null)
		{
			return;
		}

		LobbyModeOption.Sync(__instance);
	}
}
