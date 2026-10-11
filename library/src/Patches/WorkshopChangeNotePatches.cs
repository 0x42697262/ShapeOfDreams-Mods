using HarmonyLib;
using Steamworks;

namespace SodLib.Patches;

[HarmonyPatch(typeof(DewMod), nameof(DewMod.UpdateItem))]
internal static class WorkshopUploadPatch
{
	[HarmonyPrefix]
	private static void Prefix(ModItem item)
	{
		WorkshopChangeNote.Begin(item);
	}

	[HarmonyFinalizer]
	private static void Finalizer()
	{
		WorkshopChangeNote.End();
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
