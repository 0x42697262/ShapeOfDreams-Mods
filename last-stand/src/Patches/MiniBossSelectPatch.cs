using HarmonyLib;
using LastStand.Combat;

namespace LastStand.Patches;

[HarmonyPatch(typeof(RoomMonsters), nameof(RoomMonsters.SelectMiniBoss))]
internal static class MiniBossSelectPatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref Entity entity, ref MiniBossEffect effect)
	{
		if (!MiniBossPick.TryTake(out Entity picked, out MiniBossEffect pickedEffect))
		{
			return true;
		}

		entity = picked;
		effect = pickedEffect;
		return false;
	}
}
