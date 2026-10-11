using HarmonyLib;

namespace DreamArena.Patches;

[HarmonyPatch(typeof(DewHeroesTeleporter), nameof(DewHeroesTeleporter.TeleportAllHeroes))]
internal static class HeroesTeleportedPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		DreamArenaMod.Current?.Session?.OnHeroesTeleported();
	}
}

[HarmonyPatch(typeof(RoomProps), nameof(RoomProps.OnRoomStartServer))]
internal static class RoomPropsPatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(ModMode.IsHosting() && DreamArenaMod.Current?.Session?.Settings?.NoMonsters == true
		                                && ArenaLocator.CurrentPhase() == ArenaPhase.Arena);
	}
}
