using System;
using LastStand.Arena;
using LastStand.Combat;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Loot;
using LastStand.Mode;
using LastStand.Text;
using LastStand.Waves;
using LastStand.Well;
using Mirror;

namespace LastStand.Session;

internal sealed class LastStandSession
{
	private readonly EnemyRegistry _enemies = new EnemyRegistry();
	private readonly ArenaArrival _arrival = new ArenaArrival();
	private readonly CampRift _campRift = new CampRift();
	private readonly LootService _loot = new LootService();
	private readonly WaveDirector _waves;
	private readonly PainService _pain;

	private SessionSettings _settings;
	private SessionPhase _phase = SessionPhase.Inactive;
	private int _arenaNode = ArenaLocator.NoNode;
	private double _now;

	public PlainArena PlainArena { get; } = new PlainArena();

	public DreamWell Well { get; } = new DreamWell();

	public SessionSettings Settings => _settings;

	public LastStandSession(SessionSettings settings)
	{
		_settings = settings;
		_waves = new WaveDirector(_enemies, OnEnemySpawned);
		_pain = new PainService(_enemies, _waves.HasPending);

		_waves.WaveStarted += OnWaveStarted;
		_waves.WaveCleared += OnWaveCleared;
		_enemies.Died += OnEnemyDied;
		_pain.GroupFell += _waves.RestoreHuntedZone;
	}

	public bool OwnsPainZone(RoomModifierBase modifier)
	{
		return _pain.OwnsZone(modifier);
	}

	public void UseSettings(SessionSettings settings)
	{
		_settings = settings;
	}

	public void Tick(double now)
	{
		_now = now;

		if (!LastStandMode.IsHosting())
		{
			if (_phase != SessionPhase.Inactive)
			{
				Reset();
			}

			return;
		}

		if (!ArenaLocator.IsStandingInArena())
		{
			if (_phase == SessionPhase.Fighting || _phase == SessionPhase.Arriving)
			{
				StopFight();
			}

			_phase = SessionPhase.Camp;
			_campRift.Tick(now);
			return;
		}

		int node = ArenaLocator.CurrentNode();
		if (_phase != SessionPhase.Fighting || node != _arenaNode)
		{
			Arrive(now, node);
			return;
		}

		_waves.Tick(now, _settings.Waves);
		_pain.Tick(now, _settings.Pain);
		_loot.Tick(now);
	}

	public void StartNextWaveNow()
	{
		if (_phase == SessionPhase.Fighting)
		{
			_waves.StartNextWaveNow(_now);
		}
	}

	public string Describe()
	{
		return LogText.Status(LastStandMode.IsSelected(), NetworkServer.active, _phase.ToString(),
			_waves.CurrentWave, _waves.WaveInProgress, _waves.SecondsUntilNextWave(_now), _enemies.Occupied(_now),
			_waves.PendingSpawns, _loot.TrackedItems);
	}

	public void Reset()
	{
		StopFight();
		PlainArena.Reset();
		_campRift.Reset();
		_phase = SessionPhase.Inactive;
	}

	private void Arrive(double now, int node)
	{
		_phase = SessionPhase.Arriving;
		if (!_arrival.TryArrive(now, out ArenaPoints arena))
		{
			return;
		}

		StopFight();
		_arenaNode = node;
		_phase = SessionPhase.Fighting;

		Well.Place(arena.Center);
		_waves.Begin(now + _settings.FirstWaveDelaySeconds);

		Log.Info(LogText.ArenaReady(_arrival.StagedHeroes, arena.Center));
		ChatNotices.Broadcast(Messages.ArenaReady((int)Math.Ceiling(_settings.FirstWaveDelaySeconds)));
	}

	private void StopFight()
	{
		_waves.Reset();
		_pain.Reset();
		_loot.Reset();
		_enemies.Clear();
		_arrival.Reset();
		Well.Forget();
		_arenaNode = ArenaLocator.NoNode;
	}

	private void OnEnemySpawned(Entity entity, EnemyTier tier)
	{
		_enemies.Track(entity, tier);
		if (tier == EnemyTier.Pain)
		{
			_pain.OnSpawned(entity, _settings.Pain);
		}
	}

	private void OnWaveStarted(WavePlan plan)
	{
		_loot.OnWaveStarted();
	}

	private void OnWaveCleared(int wave)
	{
		_loot.OnWaveCleared(_now, _settings.Loot);
	}

	private void OnEnemyDied(EnemyDeath death)
	{
		_loot.OnEnemyDied(death, _waves.CurrentWave, _now, _settings.Loot);
		_pain.OnEnemyDied(death);
	}
}
