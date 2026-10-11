using HarmonyLib;
using Steamworks;

namespace SodLib.Patches;

[HarmonyPatch(typeof(SteamUGC), nameof(SteamUGC.StartItemUpdate))]
internal static class WorkshopStartPatch
{
	[HarmonyPrefix]
	private static void Prefix(PublishedFileId_t __1)
	{
		WorkshopChangeNote.Begin(__1);
	}
}

[HarmonyPatch(typeof(SteamUGC), nameof(SteamUGC.SubmitItemUpdate))]
internal static class WorkshopSubmitPatch
{
	[HarmonyPrefix]
	private static void Prefix(ref string pchChangeNote)
	{
		WorkshopChangeNote.Apply(ref pchChangeNote);
	}
}
