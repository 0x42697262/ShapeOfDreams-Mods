using System;
using LastStand.Config;

namespace SodLib.Text;

internal static partial class ModText
{
	public const string ModeName = "Last Stand";

	public const string ModeDescription =
		"Hold a boss arena against endless waves that grow stronger every wave.";

	public const string WindowNote = "Host only. Clients don't need the mod.";

	public const string TickFailed = "session tick failed; the session keeps its wave and carries on.";

	public const string NoticeColor = "#e8a33d";

	public static partial string PageTitle(Enum page)
	{
		switch ((SettingsPage)page)
		{
			case SettingsPage.Shrines:
				return "Shrines";
			case SettingsPage.Waves:
				return "Waves";
			case SettingsPage.Monsters:
				return "Monsters";
			case SettingsPage.Bosses:
				return "Bosses";
			case SettingsPage.Events:
				return "Events";
			case SettingsPage.Rewards:
				return "Rewards";
			default:
				return "General";
		}
	}

	public static partial string PageSubtitle(Enum page)
	{
		switch ((SettingsPage)page)
		{
			case SettingsPage.Shrines:
				return "What stands on the edge ring, and what it costs.";
			case SettingsPage.Waves:
				return "Pace, size and spawning.";
			case SettingsPage.Monsters:
				return "Scaling, other worlds, hunters and Mirage skins.";
			case SettingsPage.Bosses:
				return "Minibosses, Pain minibosses and heroic bosses.";
			case SettingsPage.Events:
				return "Environments and dark urge.";
			case SettingsPage.Rewards:
				return "Item drops, gold, dream dust and chaos rarity.";
			default:
				return "Arena, difficulty and the basics.";
		}
	}
}
