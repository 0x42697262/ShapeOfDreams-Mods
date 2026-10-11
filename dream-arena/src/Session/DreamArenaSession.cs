using DreamArena.Arena;
using DreamArena.Camp;
using DreamArena.Config;
using DreamArena.Game;
using DreamArena.Heroes;
using DreamArena.Match;
using DreamArena.Pvp;
using DreamArena.Text;
using Mirror;

namespace DreamArena.Session;

internal sealed class DreamArenaSession
{
	private readonly PhaseWatch _phase = new PhaseWatch();
	private readonly PveSweep _pve = new PveSweep();
	private readonly CampRift _campRift = new CampRift();
	private readonly CampRemnants _remnants = new CampRemnants();
	private readonly TrainingDummy _dummy = new TrainingDummy();
	private readonly HeroBoost _heroBoost = new HeroBoost();
	private readonly Revives _revives = new Revives();
	private readonly RunGoal _goal = new RunGoal();
	private readonly RestartVote _restartVote = new RestartVote();
	private bool _active;
	private int _stagedNode = ArenaLocator.NoNode;
	private double _nextStageAt;

	public SessionSettings Settings { get; private set; }

	public ArenaPhase Phase => _phase.Current;

	public CampRemnants Remnants => _remnants;

	public bool RestartVotePending => _restartVote.Pending;

	public void UseSettings(SessionSettings settings)
	{
		Settings = settings;
		PlainArena.Enabled = settings.PlainArena;
		if (_active)
		{
			ApplyRules();
			ApplyArenaRoom();
		}
	}

	public void Tick(double now)
	{
		if (!ModMode.IsHosting() || NetworkedManagerBase<GameManager>.softInstance == null)
		{
			if (_active)
			{
				Stop();
			}

			return;
		}

		if (!_active)
		{
			Start();
		}

		if (_phase.Tick(Settings) && _phase.Current == ArenaPhase.Camp)
		{
			_stagedNode = ArenaLocator.NoNode;
		}

		_revives.Tick(now, Settings);
		_heroBoost.Tick(now, Settings);
		if (Settings.RemoveRunGoal)
		{
			_goal.Tick(now);
		}

		if (_phase.Current == ArenaPhase.Arena)
		{
			TickArena(now);
		}
		else if (_phase.Current == ArenaPhase.Camp)
		{
			TickCamp(now);
		}
	}

	public void OnWorldGenerated()
	{
		if (_active)
		{
			ApplyArenaRoom();
		}
	}

	private void ApplyArenaRoom()
	{
		if (Settings.ForceArenaRoom)
		{
			ArenaRooms.Apply(Settings.ArenaRoom, Settings.StripRoomModifiers);
		}
	}

	public void OnHeroesTeleported()
	{
		if (_active && ArenaLocator.CurrentPhase() == ArenaPhase.Arena)
		{
			ArenaStaging.TryPlaceInRing();
		}
	}

	public string RequestRestart(DewPlayer player)
	{
		if (!Settings.ChatRestart)
		{
			return Messages.RestartOff;
		}

		return _restartVote.TryBegin(player, _phase.Current, _remnants.Restock);
	}

	public string RestartNow()
	{
		string reason = MatchRestart.CannotRestartReason(_phase.Current);
		if (reason != null)
		{
			return reason;
		}

		return MatchRestart.Execute(Messages.HostName, _remnants.Restock) ? null : Messages.RestartNotReady;
	}

	public bool TryConsumeRestartVote()
	{
		return _restartVote.TryConsumePassed(_remnants.Restock);
	}

	public void OnVoteCleared()
	{
		_restartVote.Clear();
	}

	public bool StageNow()
	{
		if (!_active || _phase.Current != ArenaPhase.Arena)
		{
			return false;
		}

		_pve.Sweep(Settings);
		return ArenaStaging.TryPlaceInRing();
	}

	public int PurgeNow()
	{
		return _pve.Sweep(Settings);
	}

	public string Describe()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		return LogText.Status(ModMode.IsSelected(), _active, _phase.Current, Settings.TeamMode,
			DewPlayer.allHumanPlayers.Count, game != null && game.isGameOverEnabled, _restartVote.Pending);
	}

	public void Reset()
	{
		_phase.Reset();
		_pve.Reset();
		_campRift.Reset();
		_remnants.Reset();
		_dummy.Reset();
		_heroBoost.Reset();
		_revives.Reset();
		_goal.Reset();
		_restartVote.Clear();
		RemnantStock.Clear();
		PlainArena.Reset();
		_stagedNode = ArenaLocator.NoNode;
		_nextStageAt = 0;
		_active = false;
	}

	public void Shutdown()
	{
		if (_active)
		{
			Stop();
		}
	}

	private void Start()
	{
		_active = true;
		ApplyRules();
		ApplyArenaRoom();
		Log.Info(LogText.SessionStarted(Settings.TeamMode));
	}

	private void Stop()
	{
		try
		{
			if (NetworkServer.active)
			{
				Relations.ClearAll();
				MatchRules.Restore();
			}
		}
		finally
		{
			Reset();
			Log.Info(LogText.SessionStopped);
		}
	}

	private void ApplyRules()
	{
		MatchRules.Apply(Settings);
		_phase.Apply(Settings);
	}

	private void TickArena(double now)
	{
		_dummy.Despawn();
		_pve.Tick(now, Settings);
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.isInAnyTransition || zones.currentNodeIndex == _stagedNode || now < _nextStageAt)
		{
			return;
		}

		_nextStageAt = now + Tuning.StagingRetrySeconds;
		if (StageNow())
		{
			_stagedNode = zones.currentNodeIndex;
		}
	}

	private void TickCamp(double now)
	{
		_campRift.Tick(now);
		if (Settings.CampRemnants)
		{
			_remnants.Tick(now);
		}

		if (Settings.TrainingDummy)
		{
			_dummy.Tick(now, Settings);
		}
		else
		{
			_dummy.Despawn();
		}
	}
}
