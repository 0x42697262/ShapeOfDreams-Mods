using HarmonyLib;
using LastStand.Game;
using LastStand.Mode;
using Mirror;

namespace LastStand.Patches;

[HarmonyPatch(typeof(CurseStatusEffect), GameNames.CurseOnCreate)]
internal static class CurseGoalPatch
{
	[HarmonyPostfix]
	private static void Postfix(CurseStatusEffect __instance)
	{
		if (NetworkServer.active && LastStandMode.IsHosting())
		{
			LastStandMod.Current?.Session?.OnCurseCreated(__instance);
		}
	}
}
