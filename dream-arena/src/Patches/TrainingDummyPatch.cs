using HarmonyLib;
using DreamArena.Camp;
using Mirror;

namespace DreamArena.Patches;

[HarmonyPatch(typeof(EntityControl), nameof(EntityControl.StartDisplacement))]
internal static class TrainingDummyPatch
{
	[HarmonyPrefix]
	private static bool Prefix(EntityControl __instance, Displacement disp)
	{
		if (!NetworkServer.active || disp == null || disp.isFriendly || !TrainingDummy.IsDummy(__instance.entity))
		{
			return true;
		}

		Traverse.Create(disp).Property(nameof(Displacement.hasStarted)).SetValue(true);
		Traverse.Create(disp).Property(nameof(Displacement.isAlive)).SetValue(false);
		disp.onCancel?.Invoke();
		__instance.ClientEvent_OnDisplacementCanceled?.Invoke(disp);
		return false;
	}
}
