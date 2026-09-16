using HarmonyLib;

namespace DreamArena.Patches;

/// <summary>
/// Records every déjà vu pick a player makes, on the host.
/// </summary>
/// <remarks>
/// <c>CmdSetDejavuItem</c> is sent by the client with authority over its own
/// player, and its body runs server-side. Patching after the body means the
/// item has already passed the game's own validation, so only legal picks are
/// banked.
///
/// The player's UI still shows one slot. Their machine is unchanged; the host
/// is simply keeping the history.
/// </remarks>
[HarmonyPatch(typeof(DewPlayer), "UserCode_CmdSetDejavuItem__String")]
internal static class DejavuPickPatch
{
	[HarmonyPostfix]
	private static void Postfix(DewPlayer __instance)
	{
		if (!DreamArenaMod.IsLoadoutActive)
		{
			return;
		}

		// Read the field rather than the argument: if validation rejected the
		// pick this still holds the previous value, and an empty value is the
		// player clearing their slot.
		LoadoutRegistry.Record(__instance, __instance.selectedDejavuItem);
	}
}

/// <summary>
/// Expands stock déjà vu from one item into the player's whole banked build.
/// </summary>
/// <remarks>
/// <c>DoDejavuSpawn</c> runs on the server for each player once the first zone
/// is ready, and spawns their single selected item with
/// <c>Dew.CreateSkillTrigger</c> / <c>Dew.CreateGem</c>. Those are networked
/// spawns, so adding more here replicates to unmodded clients as ordinary
/// drops.
///
/// A postfix is used so the stock item still spawns exactly as the game
/// intends; only the remainder of the build is added.
/// </remarks>
[HarmonyPatch(typeof(PlayGameManager), "DoDejavuSpawn")]
internal static class DejavuSpawnPatch
{
	[HarmonyPostfix]
	private static void Postfix(DewPlayer h)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !DreamArenaMod.IsLoadoutActive)
		{
			return;
		}

		LoadoutGranter.GrantExtras(h, mod.config);
	}
}
