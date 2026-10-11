using DreamArena.Arena;
using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.Match;

internal sealed class RestartVote
{
	private string _requester;

	public bool Pending { get; private set; }

	public string TryBegin(DewPlayer player, Phase phase, System.Action restock)
	{
		string reason = MatchRestart.CannotRestartReason(phase);
		if (reason != null)
		{
			return reason;
		}

		string who = player != null ? player.guid : Messages.HostName;
		if (StandingHeroes() <= 1)
		{
			return MatchRestart.Execute(who, restock) ? null : Messages.RestartNotReady;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		Pending = true;
		_requester = who;
		zones.StartVoteNextZone(player);
		if (!zones.isVoting)
		{
			Clear();
			return Messages.RestartVoteFailed;
		}

		ChatNotices.Broadcast(Messages.RestartVoteStarted(player != null
			? ChatManager.GetDescribedPlayerName(player)
			: Messages.HostName));
		Log.Info(LogText.RestartVoteStarted(who));
		return null;
	}

	public bool TryConsumePassed(System.Action restock)
	{
		if (!Pending)
		{
			return false;
		}

		string requester = _requester;
		Clear();
		if (!MatchRestart.Execute(requester, restock))
		{
			ChatNotices.Broadcast(Messages.RestartFailed);
			Log.Warn(LogText.RestartVoteFailed);
		}

		return true;
	}

	public void Clear()
	{
		Pending = false;
		_requester = null;
	}

	private static int StandingHeroes()
	{
		int standing = 0;
		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null && !player.hero.IsNullInactiveDeadOrKnockedOut())
			{
				standing++;
			}
		}

		return standing;
	}
}
