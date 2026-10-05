using HarmonyLib;
using Mirror;

namespace DreamArena.Patches;

/// <summary>
/// Cancels knockbacks and pulls on the training dummy so it never leaves its
/// spot.
/// </summary>
/// <remarks>
/// <c>EntityControl.StartDisplacement</c> runs on the server and already has a
/// cancel path for entities immune to crowd control: it marks the displacement
/// finished, fires <c>onCancel</c> so the ability that caused it can wrap up,
/// and never sends the RPC, so clients do not see it move. This does the same
/// for the dummy. Friendly displacements (an entity's own dashes) are left
/// alone, though with its AI off the dummy never makes any.
/// </remarks>
[HarmonyPatch(typeof(EntityControl), nameof(EntityControl.StartDisplacement))]
internal static class TrainingDummyDisplacementPatch
{
	[HarmonyPrefix]
	private static bool Prefix(EntityControl __instance, Displacement disp)
	{
		if (!NetworkServer.active || disp == null || disp.isFriendly
		    || !TrainingDummy.IsDummy(__instance.entity))
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
