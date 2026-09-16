using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Keeps lucid dreams out of a match.
/// </summary>
/// <remarks>
/// <para>
/// Lucid dreams are a PvE progression layer: run-wide modifiers players bring
/// in or pick up from shrines. They have no place in an even match, and unlike
/// the HUD clutter they are removed rather than hidden — <c>GameManager</c>
/// reads <c>activeLucidDreams</c> directly, so anything left in that list is
/// still in effect.
/// </para>
/// <para>
/// Both lists are server-authoritative SyncLists, so the host emptying them is
/// enough; clients need no mod.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(GameSettingsManager))]
internal static class LucidDreamPatch
{
	private static bool ShouldBlock()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		return mod != null && mod.config.arenaEnabled && mod.config.removeLucidDreams;
	}

	/// <summary>
	/// Refuses new lucid dreams, whatever offered them — a shrine, a player's
	/// loadout, or the lobby.
	/// </summary>
	[HarmonyPrefix]
	[HarmonyPatch("AddLucidDream")]
	private static bool AddLucidDream_Prefix(string type)
	{
		if (!ShouldBlock())
		{
			return true;
		}

		Debug.Log($"{ModIdentity.LogPrefix}blocked lucid dream '{type}'.");
		return false;
	}

	/// <summary>
	/// Empties the offer list as it is rebuilt, so nothing is selectable in the
	/// lobby to begin with.
	/// </summary>
	/// <remarks>
	/// The game recomputes this from every player's unlocks whenever the roster
	/// changes, so clearing it once would not hold — it has to be cleared each
	/// time it is rebuilt.
	/// </remarks>
	[HarmonyPostfix]
	[HarmonyPatch("UpdateAvailableLucidDreams")]
	private static void UpdateAvailableLucidDreams_Postfix(GameSettingsManager __instance)
	{
		if (!ShouldBlock() || __instance.availableLucidDreams.Count == 0)
		{
			return;
		}

		__instance.availableLucidDreams.Clear();
	}
}
