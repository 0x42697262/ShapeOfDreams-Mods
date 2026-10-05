using System;
using HarmonyLib;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.AddModifier),
	new[] { typeof(int), typeof(ModifierData), typeof(Action<RoomModifierBase>) })]
internal static class ModifierGatePatch
{
	[HarmonyPrefix]
	private static bool Prefix(ZoneManager __instance, int nodeIndex, ModifierData mod, ref int __result)
	{
		if (!LastStandMode.IsHosting() || LastStandMod.Current?.Session?.AllowsModifier(__instance, nodeIndex, mod.type) != false)
		{
			return true;
		}

		__result = mod.id;
		return false;
	}
}
