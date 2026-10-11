using System;
using DreamArena.Config;

namespace SodLib.Text;

internal static partial class ModText
{
	public const string ModeName = "PvP Arena";

	public const string ModeDescription =
		"Fight each other in a boss arena. No monsters and no ending: knocked-out players get back up. " +
		"Only the host needs this mod.";

	public const string WindowNote = "Settings apply to this lobby. Only the host needs the mod.";

	public const string TickFailed = "session tick failed.";

	public const string NoticeColor = "#f5c542";

	public static partial string PageTitle(Enum page)
	{
		switch ((SettingsPage)page)
		{
			case SettingsPage.Arena:
				return "Arena";
			case SettingsPage.Camp:
				return "Camp";
			case SettingsPage.Heroes:
				return "Heroes";
			case SettingsPage.Rules:
				return "Rules";
			default:
				return "Match";
		}
	}

	public static partial string PageSubtitle(Enum page)
	{
		switch ((SettingsPage)page)
		{
			case SettingsPage.Arena:
				return "Where the fights happen and how players arrive.";
			case SettingsPage.Camp:
				return "Free remnants and a training dummy between rounds.";
			case SettingsPage.Heroes:
				return "Levels, health and revives.";
			case SettingsPage.Rules:
				return "What PvE content is kept out.";
			default:
				return "Teams, the run's end and restarts.";
		}
	}
}
