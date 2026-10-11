using System;
using HarmonyLib;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;
using LastStand.UI;

namespace LastStand.Patches;

[HarmonyPatch(typeof(UI_Lobby_SettingsWindow))]
internal static class LobbySettingsPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(GameNames.LobbySettingsStart)]
	private static void Start_Postfix(UI_Lobby_SettingsWindow __instance)
	{
		TryInject(__instance);
	}

	[HarmonyPostfix]
	[HarmonyPatch(GameNames.LobbySettingsEnable)]
	private static void OnEnable_Postfix(UI_Lobby_SettingsWindow __instance)
	{
		TryInject(__instance);
	}

	private static void TryInject(UI_Lobby_SettingsWindow window)
	{
		if (window == null)
		{
			return;
		}

		try
		{
			LobbyConfigButton.Ensure(window.transform);
		}
		catch (Exception e)
		{
			Log.Error(LogText.LobbyButtonFailed, e);
		}
	}
}
