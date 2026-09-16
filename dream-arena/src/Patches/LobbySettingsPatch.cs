using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DreamArena.Patches;

/// <summary>
/// Adds a "Dream Arena" button to the host's in-lobby settings panel.
/// </summary>
/// <remarks>
/// Only the host has this mod, so only the host sees the button. The console
/// command <c>arena</c> opens the same window and is the fallback if the
/// lobby layout changes and injection misses.
/// </remarks>
[HarmonyPatch(typeof(UI_Lobby_SettingsWindow))]
internal static class LobbySettingsPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("Start")]
	private static void Start_Postfix(UI_Lobby_SettingsWindow __instance)
	{
		TryInject(__instance);
	}

	/// <summary>Also runs on reopen, which catches a panel that already existed
	/// when the mod was loaded mid-session.</summary>
	[HarmonyPostfix]
	[HarmonyPatch("OnEnable")]
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

		// Idempotent: bail if our button is already there.
		foreach (Transform child in window.transform)
		{
			if (ModIdentity.IsOurs(child.name))
			{
				return;
			}
		}

		try
		{
			Button button = Object.Instantiate(DewGUI.widgetButton, window.transform);
			button.name = ModIdentity.LobbyButtonName;
			button.SetHeight(54f);
			button.GetComponentInChildren<TMPro.TMP_Text>(includeInactive: true).text = "Dream Arena";
			button.onClick.AddListener(ArenaConfigWindow.Toggle);

			Debug.Log($"{ModIdentity.LogPrefix}lobby settings button installed.");
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}could not inject the lobby button " +
			                 $"(use the 'arena' console command instead): {e.Message}");
		}
	}
}
