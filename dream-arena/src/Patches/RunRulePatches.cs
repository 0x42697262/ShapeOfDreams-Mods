using HarmonyLib;
using Mirror;

namespace DreamArena.Patches;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.WrapUpAndShowResult))]
internal static class RunEndPatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(ModMode.IsHosting() && DreamArenaMod.Current?.Session?.Settings?.NeverEndRun == true);
	}
}

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.ShouldVoteOnTravel))]
internal static class SkipTravelVotePatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref bool __result)
	{
		if (!ModMode.IsHosting() || DreamArenaMod.Current?.Session?.Settings?.SkipTravelVote != true
		    || DreamArenaMod.Current.Session.RestartVotePending)
		{
			return true;
		}

		__result = false;
		return false;
	}
}

[HarmonyPatch(typeof(GameSettingsManager), nameof(GameSettingsManager.AddLucidDream))]
internal static class LucidDreamAddPatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(ModMode.IsHosting() && DreamArenaMod.Current?.Session?.Settings?.RemoveLucidDreams == true);
	}
}

[HarmonyPatch(typeof(GameSettingsManager), nameof(GameSettingsManager.UpdateAvailableLucidDreams))]
internal static class LucidDreamListPatch
{
	[HarmonyPostfix]
	private static void Postfix(GameSettingsManager __instance)
	{
		if (ModMode.IsHosting() && DreamArenaMod.Current?.Session?.Settings?.RemoveLucidDreams == true)
		{
			__instance.availableLucidDreams.Clear();
		}
	}
}

[HarmonyPatch(typeof(GameSettingsManager), nameof(GameSettingsManager.OnStartServer))]
internal static class GameModifierPatch
{
	[HarmonyPostfix]
	private static void Postfix(GameSettingsManager __instance)
	{
		if (NetworkServer.active && __instance.gameSettingsSaveKey == ModIdentity.SaveKey
		    && DreamArenaMod.Current?.Session?.Settings?.RemoveGameModifiers == true)
		{
			__instance.addedGameMods.Clear();
		}
	}
}
