using HarmonyLib;
using LastStand.Session;

namespace LastStand.Patches;

[HarmonyPatch(typeof(DewPersistence), nameof(DewPersistence.SerializeGameData))]
internal static class ContinueSaveWritePatch
{
	[HarmonyPostfix]
	private static void Postfix(DewPersistence.GameData __result)
	{
		if (ModMode.IsHosting())
		{
			ContinueSave.Write(__result);
		}
	}
}

[HarmonyPatch(typeof(DewPersistence), nameof(DewPersistence.ApplyGameData))]
internal static class ContinueSaveReadPatch
{
	[HarmonyPrefix]
	private static void Prefix(DewPersistence.GameData data)
	{
		ContinueSave.Read(data);
	}
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveContinueDataMidRun))]
internal static class ContinueSaveGatePatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !ModMode.IsHosting() || LastStandMod.Current?.Session?.AllowsContinueSave() != false;
	}
}
