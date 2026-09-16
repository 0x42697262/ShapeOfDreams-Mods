using HarmonyLib;

namespace DreamArena.Patches;

/// <summary>
/// Rewrites the rift prompt so leaving camp reads as starting a match.
/// </summary>
/// <remarks>
/// The confirmation text comes from
/// <c>DewLocalization.GetUIValue("InGame_Message_RiftMoveToNextLocation")</c>.
/// Swapping the lookup result is far less invasive than reimplementing
/// <c>Rift_RoomExit</c>'s interaction, and it automatically leaves every other
/// string alone.
/// </remarks>
[HarmonyPatch(typeof(DewLocalization), "GetUIValue")]
internal static class RiftPromptPatch
{
	private const string RiftKey = "InGame_Message_RiftMoveToNextLocation";

	private const string PvpPrompt =
		"Enter the arena and begin the match?\n\nAll bets are off once you step through.";

	[HarmonyPostfix]
	private static void Postfix(string key, ref string __result)
	{
		if (key != RiftKey)
		{
			return;
		}

		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled)
		{
			return;
		}

		__result = PvpPrompt;
	}
}

/// <summary>
/// Hides the run's objectives while in PvP mode.
/// </summary>
/// <remarks>
/// "Goal: Shape of Dreams — Move deeper into The Rapids" and any side quests
/// are meaningless in an arena match, so they are dropped from the HUD.
///
/// Visibility is suppressed rather than the quests themselves being cancelled:
/// <c>DewQuest</c> is wired into run progression, and completing or removing
/// the main goal could end the run. Hiding costs nothing and cannot misfire.
/// </remarks>
[HarmonyPatch(typeof(DewQuest), "IsVisibleLocally")]
internal static class QuestDisplayPatch
{
	[HarmonyPostfix]
	private static void Postfix(ref bool __result)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled || !mod.config.hideObjectives)
		{
			return;
		}

		__result = false;
	}
}

/// <summary>
/// Overwrites the location readout with the match type.
/// </summary>
/// <remarks>
/// <c>UI_InGame_Location.OnRoomLoaded</c> is the only place that field is
/// written, so a postfix owns it cleanly. Running after the original means the
/// stock text is still produced and then replaced, which keeps normal runs
/// untouched when PvP is off.
/// </remarks>
[HarmonyPatch(typeof(UI_InGame_Location), "OnRoomLoaded")]
internal static class LocationLabelPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null)
		{
			return;
		}

		LocationLabel.Refresh(mod.config, mod.Phase);
	}
}
