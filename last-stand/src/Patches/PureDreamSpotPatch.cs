using System;
using HarmonyLib;
using LastStand.Arena;
using LastStand.Game;
using LastStand.Mode;
using UnityEngine;

namespace LastStand.Patches;

[HarmonyPatch(typeof(RoomMod_PureDream), GameNames.PureDreamSpawnPosition)]
internal static class PureDreamSpotPatch
{
	[HarmonyPrefix]
	private static bool Prefix(RoomMod_PureDream __instance, ref Vector3 __result)
	{
		if (!LastStandMode.IsHosting() || !ArenaLocator.IsInArenaNode())
		{
			return true;
		}

		return !PureDreamSpots.TryPick(() => PureDreamCalls.CalSpawnPosition(__instance), out __result);
	}
}

[HarmonyPatch]
internal static class PureDreamCalls
{
	[HarmonyReversePatch]
	[HarmonyPatch(typeof(RoomMod_PureDream), GameNames.PureDreamSpawnPosition)]
	public static Vector3 CalSpawnPosition(RoomMod_PureDream instance)
	{
		throw new NotImplementedException();
	}
}
