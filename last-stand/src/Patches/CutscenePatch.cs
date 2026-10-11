using HarmonyLib;
using LastStand.Arena;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(DewCutsceneDirector), nameof(DewCutsceneDirector.PlayNetworked))]
internal static class CutscenePatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(LastStandMode.IsHosting() && ArenaLocator.IsInArenaNode());
	}
}
