using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Adds "PvP Arena" to the Game Mode dropdown on the Create Lobby screen,
/// beside Shape of Dreams and Limbo.
/// </summary>
/// <remarks>
/// <para>
/// This is where a game mode is actually chosen, so it is where PvP belongs —
/// not behind a checkbox in a settings window.
/// </para>
/// <para>
/// <c>UI_Title_CreateLobbyView.OnShow</c> rebuilds the dropdown each time the
/// screen opens, adding <c>(label, value)</c> pairs: <c>"ShapeOfDreams"</c> and
/// <c>"Limbo"</c>. A postfix appends a third.
/// </para>
/// <para>
/// PvP layers on top of a normal Shape of Dreams run, so it deliberately leaves
/// <c>addedGameMods</c> empty — unlike Limbo, which adds <c>GameMod_Limbo</c>.
/// The view's own handler already clears that list for any non-Limbo value, so
/// selecting PvP resets cleanly.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(UI_Title_CreateLobbyView))]
internal static class CreateLobbyModePatch
{
	/// <summary>Dropdown value identifying our mode.</summary>
	private const string PvpValue = "DreamArena_PvP";

	private const string PvpLabel = "PvP Arena";

	private const string PvpDescription =
		"Fight each other in the Lone King's Zenith. No monsters, no ending — " +
		"knocked-out players get back up. Only the host needs this mod.";

	[HarmonyPostfix]
	[HarmonyPatch("OnShow")]
	private static void OnShow_Postfix(UI_Title_CreateLobbyView __instance)
	{
		if (__instance == null || __instance.gameModeDropdown == null)
		{
			return;
		}

		try
		{
			// OnShow clears and rebuilds the list, so this always appends once.
			__instance.gameModeDropdown.AddOption(PvpLabel, PvpValue);

			// Leaving a previous session in PvP would silently re-arm it.
			SetArenaMode(false);
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}could not add the PvP game mode " +
			                 $"(use 'arena_on' instead): {e.Message}");
		}
	}

	/// <summary>
	/// Keeps arena mode and the description text in step with the dropdown.
	/// </summary>
	/// <remarks>
	/// The view's own <c>UpdateUIState</c> writes the description for Limbo or
	/// Shape of Dreams, so this runs after it and overwrites for PvP.
	/// </remarks>
	[HarmonyPostfix]
	[HarmonyPatch("UpdateUIState")]
	private static void UpdateUIState_Postfix(UI_Title_CreateLobbyView __instance)
	{
		if (__instance == null || __instance.gameModeDropdown == null)
		{
			return;
		}

		bool isPvp = PvpValue.Equals(__instance.gameModeDropdown.value as string);

		if (isPvp && __instance.gameModeDescriptionText != null)
		{
			__instance.gameModeDescriptionText.text = PvpDescription;
		}

		SetArenaMode(isPvp);
	}

	/// <summary>Writes the mode only when it actually changes, since
	/// UpdateUIState runs often.</summary>
	private static void SetArenaMode(bool enabled)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || mod.config.arenaEnabled == enabled)
		{
			return;
		}

		mod.config.arenaEnabled = enabled;
		mod.ApplyAndPersist();

		Debug.Log($"{ModIdentity.LogPrefix}game mode set to " +
		          $"{(enabled ? "PvP Arena" : "normal")} from the create-lobby screen.");
	}
}
