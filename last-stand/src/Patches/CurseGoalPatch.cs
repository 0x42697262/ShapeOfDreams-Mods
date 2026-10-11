using HarmonyLib;
using LastStand.Game;
using Mirror;

namespace LastStand.Patches;

[HarmonyPatch(typeof(CurseStatusEffect), GameNames.CurseOnCreate)]
internal static class CurseGoalPatch
{
	[HarmonyPostfix]
	private static void Postfix(CurseStatusEffect __instance)
	{
		if (NetworkServer.active && ModMode.IsHosting())
		{
			LastStandMod.Current?.Session?.OnCurseCreated(__instance);
		}
	}
}
