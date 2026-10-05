using System;
using HarmonyLib;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;
using LastStand.UI;

namespace LastStand.Patches;

[HarmonyPatch(typeof(UI_Title_CreateLobbyView))]
internal static class CreateLobbyModePatch
{
	[HarmonyPostfix]
	[HarmonyPatch(GameNames.CreateLobbyShow)]
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
			Log.Error(LogText.ModeOptionFailed, e);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(GameNames.CreateLobbyUpdateState)]
	private static void UpdateUIState_Postfix(UI_Title_CreateLobbyView __instance)
	{
		if (__instance == null || __instance.gameModeDropdown == null)
		{
			return;
		}

		LobbyModeOption.Sync(__instance);
	}
}
