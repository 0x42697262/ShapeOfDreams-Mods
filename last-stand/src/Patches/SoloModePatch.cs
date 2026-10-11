using System;
using HarmonyLib;
using LastStand.Game;
using LastStand.Text;
using LastStand.UI;

namespace LastStand.Patches;

[HarmonyPatch(typeof(UI_Title_TitleView))]
internal static class SoloModePatch
{
	[HarmonyPrefix]
	[HarmonyPatch(nameof(UI_Title_TitleView.EnterSingleplayerOptions))]
	private static bool EnterSingleplayerOptions_Prefix(UI_Title_TitleView __instance)
	{
		if (SoloModeButton.IsLimboAvailable())
		{
			return true;
		}

		TitleManager title = ManagerBase<TitleManager>.softInstance;
		if (title == null)
		{
			return true;
		}

		title.CheckTutorial(() => __instance.SetState(UI_Title_TitleView.StateType.GameMode));
		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(GameNames.TitleViewStateChanged)]
	private static void OnStateChanged_Postfix(UI_Title_TitleView __instance)
	{
		if (__instance == null || __instance.state != UI_Title_TitleView.StateType.GameMode)
		{
			return;
		}

		try
		{
			SoloModeButton.Ensure(__instance);
		}
		catch (Exception e)
		{
			Log.Error(LogText.SoloButtonFailed, e);
		}
	}
}
