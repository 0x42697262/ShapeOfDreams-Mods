using System;
using HarmonyLib;
using LastStand.Game;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(RoomMod_CorruptedChaos_AuraOfPain), GameNames.ActorOnCreate)]
internal static class PainZoneCreatePatch
{
	[HarmonyPrefix]
	private static bool Prefix(RoomMod_CorruptedChaos_AuraOfPain __instance)
	{
		if (!LastStandMode.IsHosting() || LastStandMod.Current?.Session?.OwnsPainZone(__instance) != true)
		{
			return true;
		}

		RoomModifierBaseCalls.OnCreate(__instance);
		return false;
	}
}

[HarmonyPatch(typeof(RoomMod_CorruptedChaos_AuraOfPain), GameNames.ActorOnDestroy)]
internal static class PainZoneDestroyPatch
{
	[HarmonyPrefix]
	private static bool Prefix(RoomMod_CorruptedChaos_AuraOfPain __instance)
	{
		if (!LastStandMode.IsHosting() || LastStandMod.Current?.Session?.OwnsPainZone(__instance) != true)
		{
			return true;
		}

		RoomModifierBaseCalls.OnDestroyActor(__instance);
		return false;
	}
}

[HarmonyPatch]
internal static class RoomModifierBaseCalls
{
	[HarmonyReversePatch]
	[HarmonyPatch(typeof(RoomModifierBase), GameNames.ActorOnCreate)]
	public static void OnCreate(RoomModifierBase instance)
	{
		throw new NotImplementedException();
	}

	[HarmonyReversePatch]
	[HarmonyPatch(typeof(RoomModifierBase), GameNames.ActorOnDestroy)]
	public static void OnDestroyActor(RoomModifierBase instance)
	{
		throw new NotImplementedException();
	}
}
