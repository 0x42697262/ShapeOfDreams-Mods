using HarmonyLib;
using LastStand.Arena;

namespace LastStand.Patches;

[HarmonyPatch(typeof(DewCutsceneDirector), nameof(DewCutsceneDirector.PlayNetworked))]
internal static class CutscenePatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(ModMode.IsHosting() && ArenaLocator.IsInArenaNode());
	}
}
