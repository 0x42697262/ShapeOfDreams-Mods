using System;
using HarmonyLib;

namespace SodLib.Patches;

[HarmonyPatch(typeof(UI_Lobby_SettingsWindow))]
internal static class LobbySettingsPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(SharedGameNames.LobbySettingsStart)]
	private static void Start_Postfix(UI_Lobby_SettingsWindow __instance)
	{
		TryInject(__instance);
	}

	[HarmonyPostfix]
	[HarmonyPatch(SharedGameNames.LobbySettingsEnable)]
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
			Log.Error(SharedLogText.LobbyButtonFailed, e);
		}
	}
}
