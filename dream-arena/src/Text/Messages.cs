using DreamArena.Config;

namespace DreamArena.Text;

internal static class Messages
{
	public static readonly string BackInCamp = SharedMessages.Notice("Back in camp. Everyone is friendly here.");

	public static readonly string Restarting =
		SharedMessages.Notice("Restarting. Back to camp: builds wiped, remnants restocked.");

	public static readonly string RestartFailed = SharedMessages.Warning("The restart could not run.");

	public const string LobbyName = "Dream Arena (PvP)";

	public const string HostName = "host";

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
				return SharedMessages.Warning("The arena is live. Two teams, split by join order.");
			case TeamMode.Peaceful:
				return SharedMessages.Notice("The arena is open. Peaceful: nobody can be hurt.");
			default:
				return SharedMessages.Warning("The arena is live. Free for all: everyone is your enemy.");
		}
	}

	public static string RestartVoteStarted(string player)
	{
		return SharedMessages.Notice($"{player} wants to restart: back to camp, builds wiped. Tab to agree, Esc to cancel.");
	}

	public static string CannotRestart(string reason)
	{
		return SharedMessages.Warning($"Can't restart: {reason}");
	}

	public static string RemnantName(string rarity, bool memory)
	{
		return memory ? $"{rarity} Memories" : $"{rarity} Essences";
	}

}
