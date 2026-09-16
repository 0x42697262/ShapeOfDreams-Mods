using TMPro;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Replaces the in-game location readout with the match type.
/// </summary>
/// <remarks>
/// The HUD normally shows something like "1. Forest of Turbulence", which means
/// nothing in an arena match — the zone is incidental. It reads "PVP: Free For
/// All" instead, so the rules in force are always on screen.
///
/// <c>UI_InGame_Location</c> only rewrites its text when a room loads, so this
/// is also called whenever the phase or the configured mode changes.
/// </remarks>
internal static class LocationLabel
{
	public static void Refresh(DreamArenaConfig config, PhaseKind phase)
	{
		if (config == null)
		{
			return;
		}

		UI_InGame_Location location = Object.FindAnyObjectByType<UI_InGame_Location>();
		if (location == null)
		{
			return;
		}

		TMP_Text text = location.GetComponent<TMP_Text>();
		if (text == null)
		{
			return;
		}

		if (!config.arenaEnabled || !config.showModeInHud)
		{
			return;
		}

		text.text = Describe(config, phase);
	}

	private static string Describe(DreamArenaConfig config, PhaseKind phase)
	{
		// In camp nobody can be hurt, so say so rather than naming a mode that
		// is not in force yet.
		if (phase == PhaseKind.Camp)
		{
			return "PVP: Camp";
		}

		return "PVP: " + ModeName(config.teamMode);
	}

	private static string ModeName(TeamMode mode)
	{
		switch (mode)
		{
			case TeamMode.FreeForAll: return "Free For All";
			case TeamMode.TwoTeams: return "Teams";
			case TeamMode.Peaceful: return "Truce";
			default: return mode.ToString();
		}
	}
}
