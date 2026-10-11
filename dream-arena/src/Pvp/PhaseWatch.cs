using DreamArena.Config;
using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.Pvp;

internal sealed class PhaseWatch
{
	private int _lastNode = int.MinValue;
	private int _lastPlayers = -1;

	public ArenaPhase Current { get; private set; } = ArenaPhase.None;

	public bool Tick(SessionSettings settings)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return false;
		}

		int node = zones.currentNodeIndex;
		int players = DewPlayer.allHumanPlayers.Count;
		if (node == _lastNode && players == _lastPlayers)
		{
			return false;
		}

		bool moved = node != _lastNode;
		_lastNode = node;
		_lastPlayers = players;
		ArenaPhase previous = Current;
		Current = ArenaLocator.CurrentPhase();
		Apply(settings);
		if (moved && previous != Current && previous != ArenaPhase.None)
		{
			Announce(settings);
		}

		return moved && previous != Current;
	}

	public void Apply(SessionSettings settings)
	{
		TeamMode mode = Current == ArenaPhase.Arena ? settings.TeamMode : TeamMode.Peaceful;
		Relations.Apply(mode);
		Log.Info(LogText.PhaseChanged(Current, mode));
	}

	public void Reset()
	{
		_lastNode = int.MinValue;
		_lastPlayers = -1;
		Current = ArenaPhase.None;
	}

	private void Announce(SessionSettings settings)
	{
		if (!settings.AnnouncePhases)
		{
			return;
		}

		ChatNotices.Broadcast(Current == ArenaPhase.Arena
			? Messages.ArenaLive(settings.TeamMode)
			: Messages.BackInCamp);
	}
}
