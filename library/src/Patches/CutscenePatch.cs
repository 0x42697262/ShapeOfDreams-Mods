using HarmonyLib;

namespace SodLib.Patches;

[HarmonyPatch(typeof(DewCutsceneDirector), nameof(DewCutsceneDirector.PlayNetworked))]
internal static class CutscenePatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(ModMode.IsHosting() && ModEntry.Active != null && ModEntry.Active.SkipsCutscene());
	}
}
