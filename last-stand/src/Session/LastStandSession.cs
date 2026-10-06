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
	private readonly RunGoal _goal = new RunGoal();
	private readonly HeroicBosses _heroicBosses = new HeroicBosses();
	private readonly TestBoss _testBoss;
	private readonly LootService _loot = new LootService();
	private readonly WaveDirector _waves;
	private readonly PainService _pain;

	private SessionSettings _settings;
	private SessionPhase _phase = SessionPhase.Inactive;
	private int _arenaNode = ArenaLocator.NoNode;
	private double _now;

	public PlainArena PlainArena { get; } = new PlainArena();

	private readonly ArenaMusic _music = new ArenaMusic();

	public ArenaShrines Shrines { get; } = new ArenaShrines();

	public SessionSettings Settings => _settings;

	public LastStandSession(SessionSettings settings)
	{
		_settings = settings;
		_testBoss = new TestBoss(_heroicBosses);
		_waves = new WaveDirector(_enemies, _heroicBosses, OnEnemySpawned);
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

	public void OnRoomMusicStarted(Room room)
	{
		_music.OnRoomStarted(room, _settings.RandomBossMusic);
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

		_goal.Tick(now);

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
	}

	public void SpawnTestBoss(string typeName)
	{
		if (_phase != SessionPhase.Fighting)
		{
			Log.Warn(LogText.TestBossNotInArena);
			return;
		}

		_testBoss.Spawn(typeName);
	}

	public bool OwnsHeroicBoss(Entity entity)
	{
		return _heroicBosses.Owns(entity);
	}

	public void OnHeroicBossDied(BossMonster boss)
	{
		_heroicBosses.OnDied(boss, _settings.Loot.HeroicBossChaosRewards);
	}

	public void ApplyArenaRoom()
	{
		ArenaRoomOverride.Apply(_settings.ArenaRoom, _settings.StripRoomModifiers);
	}

	public bool AllowsModifier(ZoneManager zones, int nodeIndex, string modifierType)
	{
		if (!_settings.StripRoomModifiers || ModifierGate.Allows(zones, nodeIndex, modifierType))
		{
			return true;
		}

		Log.Info(LogText.ModifierBlocked(modifierType, nodeIndex));
		return false;
	}

	public void RerollChaos(Shrine_Chaos shrine)
	{
		ChaosRarity.Reroll(shrine, _settings.Loot, Math.Max(Tuning.FirstWave, _waves.CurrentWave));
	}

	public void StartNextWaveNow()
	{
		if (_phase == SessionPhase.Fighting)
		{
			_waves.StartNextWaveNow(_now);
		}
	}

	public void JumpToWave(int wave)
	{
		if (_phase != SessionPhase.Fighting || wave < Tuning.FirstWave)
		{
			Log.Warn(LogText.JumpNotPossible);
			return;
		}

		int previous = wave - 1;
		_waves.Resume(previous, false, _now);
		ProgressStore.Save(_arenaNode, previous, false);
		Log.Info(LogText.JumpedToWave(wave));
	}

	public string HudLabel(double now)
	{
		if (!_settings.ShowWaveInHud)
		{
			return null;
		}

		switch (_phase)
		{
			case SessionPhase.Inactive:
				return null;
			case SessionPhase.Camp:
				return HudText.Camp;
			case SessionPhase.Arriving:
				return HudText.Arriving;
		}

		if (_waves.WaveInProgress)
		{
			return HudText.WaveRunning(_waves.CurrentWave, _enemies.Occupied(now) + _waves.PendingSpawns);
		}

		return HudText.NextWave(_waves.CurrentWave + 1, (int)Math.Ceiling(_waves.SecondsUntilNextWave(now)));
	}

	public string Describe()
	{
		return LogText.Status(LastStandMode.IsSelected(), NetworkServer.active, _phase.ToString(),
			_waves.CurrentWave, _waves.WaveInProgress, _waves.SecondsUntilNextWave(_now), _enemies.Occupied(_now),
			_waves.PendingSpawns, _loot.DescribeTally());
	}

	public void Reset()
	{
		StopFight();
		PlainArena.Reset();
		_campRift.Reset();
		_goal.Reset();
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

		int hidden = WorldMapNodes.HideAllBut(NetworkedManagerBase<ZoneManager>.softInstance, node);
		if (hidden > 0)
		{
			Log.Info(LogText.MapNodesHidden(hidden, node));
		}

		Shrines.Place(arena, _settings.AltarOfCleansing);

		if (ProgressStore.TryLoad(node, out int wave, out bool inProgress))
		{
			int adopted = inProgress ? EnemyAdoption.AdoptSurvivors(_enemies) : 0;
			_waves.Resume(wave, inProgress, now + _settings.Waves.IntermissionSeconds);
			Log.Info(LogText.ProgressRestored(wave, inProgress, adopted));
			return;
		}

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
		_testBoss.Forget();
		_heroicBosses.Clear();
		Shrines.Forget();
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
		ProgressStore.Save(_arenaNode, plan.Number, true);
		_loot.OnWaveStarted();
	}

	private void OnWaveCleared(int wave)
	{
		ProgressStore.Save(_arenaNode, wave, false);
		_loot.OnWaveCleared(wave, _settings.Loot);

		int revived = HeroSouls.ReviveKnockedOut();
		if (revived > 0)
		{
			Log.Info(LogText.HeroesRevived(wave, revived));
			ChatNotices.Broadcast(Messages.HeroesRevived(revived));
		}
	}

	private void OnEnemyDied(EnemyDeath death)
	{
		_loot.OnEnemyDied(death, _waves.CurrentWave, _settings.Loot);
		_pain.OnEnemyDied(death);
	}
}
