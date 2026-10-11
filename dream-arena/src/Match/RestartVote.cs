using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.Match;

internal sealed class RestartVote
{
	private string _requester;

	public bool Pending { get; private set; }

	public string TryBegin(DewPlayer player, ArenaPhase phase, System.Action restock)
	{
		string reason = MatchRestart.CannotRestartReason(phase);
		if (reason != null)
		{
			return reason;
		}

		string who = player != null ? player.guid : Messages.HostName;
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || !zones.ShouldVoteOnTravel())
		{
			return MatchRestart.Execute(who, restock) ? null : Messages.RestartNotReady;
		}

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

}
