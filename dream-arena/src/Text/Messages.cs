using DreamArena.Config;

namespace DreamArena.Text;

internal static class Messages
{
	private const string NoticeColor = "#f5c542";

	private const string WarningColor = "#ff4d4d";

	private const string Prefix = "[Dream Arena] ";

	public const string ModeName = "PvP Arena";

	public const string LobbyName = "Dream Arena (PvP)";

	public const string ModeDescription =
		"Fight each other in a boss arena. No monsters and no ending: knocked-out players get back up. " +
		"Only the host needs this mod.";

	public const string HostName = "host";

	public const string SettingOn = "on";

	public const string SettingOff = "off";

	public const string SettingNumberFormat = "0.###";

	public static readonly string BackInCamp = Notice("Back in camp. Everyone is friendly here.");

	public static readonly string Restarting = Notice("Restarting. Back to camp: builds wiped, remnants restocked.");

	public static readonly string RestartFailed = Warning("The restart could not run.");

	public const string RestartOff = "-restart is turned off in this lobby.";

	public const string RestartNotReady = "the game is not ready yet.";

	public const string RestartTraveling = "already traveling.";

	public const string RestartVoteRunning = "a vote is already running.";

	public const string RestartInCamp = "you're already in camp; -restart works from the arena.";

	public const string RestartNoCamp = "could not find the camp.";

	public const string RestartVoteFailed = "the vote could not start.";

	public static string ArenaLive(TeamMode mode)
	{
		switch (mode)
		{
			case TeamMode.TwoTeams:
				return Warning("The arena is live. Two teams, split by join order.");
			case TeamMode.Peaceful:
				return Notice("The arena is open. Peaceful: nobody can be hurt.");
			default:
				return Warning("The arena is live. Free for all: everyone is your enemy.");
		}
	}

	public static string RestartVoteStarted(string player)
	{
		return Notice($"{player} wants to restart: back to camp, builds wiped. Tab to agree, Esc to cancel.");
	}

	public static string CannotRestart(string reason)
	{
		return Warning($"Can't restart: {reason}");
	}

	public static string RemnantName(string rarity, bool memory)
	{
		return memory ? $"{rarity} Memories" : $"{rarity} Essences";
	}

	public static string SettingChangeLine(string name, string before, string after)
	{
		return $"{name}: {before} -> {after}";
	}

	public static string SettingChanged(string line)
	{
		return Notice($"Host changed {line}");
	}

	public static string MoreSettingsChanged(int count)
	{
		return Notice($"...and {count} more setting(s) changed.");
	}

	private static string Notice(string text)
	{
		return $"<color={NoticeColor}>{Prefix}{text}</color>";
	}

	private static string Warning(string text)
	{
		return $"<color={WarningColor}>{Prefix}{text}</color>";
	}
}
