using System;
using System.Collections.Generic;
using LastStand.Arena;
using LastStand.Combat;
using LastStand.Config;
using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Waves;

internal sealed class WaveDirector
{
	private readonly SpawnQueue _queue = new SpawnQueue();

	private readonly MonsterCatalog _catalog = new MonsterCatalog();

	private readonly HuntedZone _huntedZone = new HuntedZone();

	private readonly ArenaEnvironment _environment = new ArenaEnvironment();

	private readonly EnvironmentTally _environmentTally = new EnvironmentTally();

	private readonly FriendlyFire _friendlyFire = new FriendlyFire();

	private readonly SpawnPoints _spawnPoints = new SpawnPoints();

	private readonly HeroicFastKillWatch _heroicFastKill = new HeroicFastKillWatch();
	private readonly PlayerKeys _playerKeys = new PlayerKeys();
	private PaceLedger _pace = new PaceLedger();
	private double _loggedRaisedWorlds;

	private readonly BossHealthFloor _bossHealth = new BossHealthFloor();

	private readonly ErrorThrottle _spawnErrors = new ErrorThrottle(SharedTuning.ErrorLogIntervalSeconds);

	private readonly MonsterSpawner _spawner;

	private readonly EnemyRegistry _enemies;

	private readonly Action<Entity, EnemyTier> _onSpawned;

	private WaveCycle _cycle;

	private WavePlan _plan;

	private WaveSettings _settings;

	private double _now;

	private int _skipsLeft;

	public PaceLedger Pace => _pace;

	public double MergeMultiplier => _plan != null ? _plan.MergeMultiplier : MergeRule.NoChange;

	public int CurrentWave => _cycle?.CurrentWave ?? 0;

	public bool WaveInProgress => _cycle?.InProgress ?? false;

	public int ClearedWaves => WaveInProgress ? CurrentWave - 1 : CurrentWave;

	public bool HasArenaModifier => _huntedZone.IsActive || _environment.IsActive;

	public bool WaitingForCurse => _cycle?.IsHeld ?? false;

	public int PendingSpawns => _queue.PendingCount;

	public event Action<WavePlan> WaveStarted;

	public event Action<int> WaveCleared;

	public WaveDirector(EnemyRegistry enemies, HeroicBosses heroicBosses, Action<Entity, EnemyTier> onSpawned)
	{
		_enemies = enemies;
		_onSpawned = onSpawned;
		_spawner = new MonsterSpawner(_catalog, heroicBosses);
		_enemies.Hit += OnEnemyHit;
	}

	public void Begin(double firstWaveAt)
	{
		_cycle = new WaveCycle(firstWaveAt);
		_plan = null;
		ResetPace(new PaceLedger());
		_queue.Clear();
	}

	public void Resume(int wave, bool inProgress, double nextWaveAt, PaceLedger pace)
	{
		_cycle = WaveCycle.Resume(wave, inProgress, nextWaveAt);
		_plan = null;
		_skipsLeft = 0;
		ResetPace(pace ?? new PaceLedger());
		_queue.Clear();
	}

	private double RaisedWorlds(HeroicFastKillRule rule, ICollection<string> present)
	{
		return _pace.RaisedWorlds(rule, present, WorldScaling.ExtraWorldsForBossHealth);
	}

	private void ResetPace(PaceLedger pace)
	{
		_heroicFastKill.Cancel();
		_playerKeys.Clear();
		_pace = pace;
		_loggedRaisedWorlds = 0;
	}

	public bool HasPending(EnemyTier tier)
	{
		return _queue.HasPending(tier);
	}

	public double SecondsUntilNextWave(double now)
	{
		return _cycle?.SecondsUntilNextWave(now) ?? 0;
	}

	public void Skip(int waves)
	{
		if (_cycle != null && waves > 0)
		{
			_skipsLeft += waves;
		}
	}

	public void StartNextWaveNow(double now)
	{
		_cycle?.StartNextWaveNow(now);
	}

	public void HoldForCurse()
	{
		_cycle?.Hold();
	}

	public void ReleaseCurseHold(double nextWaveAt)
	{
		_cycle?.Release(nextWaveAt);
	}

	public void Tick(double now, WaveSettings settings)
	{
		_now = now;
		_settings = settings;
		if (_cycle == null)
		{
			return;
		}

		if (!_catalog.IsReady && !TryBuildCatalog(settings))
		{
			return;
		}

		if (!_spawnPoints.IsReady && !_spawnPoints.TryPrepare())
		{
			return;
		}

		if (_cycle.InProgress && _plan == null)
		{
			ResumePlan(settings, now);
		}

		if (_cycle.TryStartWave(now))
		{
			StartWave(settings, now);
		}

		if (_skipsLeft > 0 && _cycle.InProgress)
		{
			_heroicFastKill.Cancel();
			_queue.Clear();
			int removed = _enemies.RemoveAll();
			Log.Info(LogText.WaveSkipped(_cycle.CurrentWave, removed));
		}

		ReleaseSpawns(now, settings);
		if (_cycle.InProgress)
		{
			_bossHealth.Tick(now, _plan, _enemies);
		}

		_friendlyFire.Tick(now);

		bool fieldClear = _queue.PendingCount == 0 && _enemies.Occupied(now) == 0;
		double breakSeconds = WaveSchedule.BreakAfter(_plan, settings);
		if (_cycle.TryFinishWave(now, fieldClear, breakSeconds))
		{
			FinishWave(breakSeconds, settings.CurseToStart);
			ContinueSkipping(now);
		}
		else if (!_cycle.InProgress)
		{
			ContinueSkipping(now);
		}
	}

	private void ContinueSkipping(double now)
	{
		if (_skipsLeft <= 0)
		{
			return;
		}

		_skipsLeft--;
		_cycle.StartNextWaveNow(now);
	}

	public void RestoreHuntedZone()
	{
		_huntedZone.Restore();
	}

	public void Reset()
	{
		_huntedZone.End();
		_environment.End();
		_environmentTally.Reset();
		_friendlyFire.End();
		_cycle = null;
		_plan = null;
		_skipsLeft = 0;
		ResetPace(new PaceLedger());
		_bossHealth.StartWave();
		_queue.Clear();
		_spawner.Reset();
		_catalog.Clear();
		_spawnPoints.Clear();
	}

	private bool TryBuildCatalog(WaveSettings settings)
	{
		if (!_catalog.TryBuild(settings.HeroicBossTypes, settings.AllHeroicBossTypes))
		{
			return false;
		}

		Log.Info(LogText.CatalogBuilt(_catalog.ForeignTypeCount, _catalog.MirageSkinCount,
			settings.HunterFromWave, settings.AllHuntersFromWave, _catalog.HeroicBossCount));
		if (settings.Merge.IsOn)
		{
			Log.Info(LogText.MergeStarts(settings.Merge.FromWave, settings.Merge.Cap));
		}

		return true;
	}

	private void StartWave(WaveSettings settings, double now)
	{
		ApplyPlan(settings);
		_queue.Enqueue(_plan, WaveSchedule.HeroicBossEntranceSeconds(_plan.HeroicBossCount,
			_plan.HeroicBossWindowSeconds, () => UnityEngine.Random.value));
		_bossHealth.StartWave();
		_heroicFastKill.Start(settings.HeroicFastKill.IsOn && _plan.HeroicBossCount > 0,
			settings.HeroicFastKill.BurstSeconds);

		Log.Info(LogText.WaveStarted(_plan));
		ChatNotices.Broadcast(Messages.WaveStarted(_plan));
		AnnounceMilestones(settings);
		BeginEnvironment(settings);
		BeginFriendlyFire(settings, now);
		WaveStarted?.Invoke(_plan);
	}

	private void AnnounceMilestones(WaveSettings settings)
	{
		List<Milestone> starting = Milestones.StartingAt(_plan.Number, settings);
		if (starting.Count == 0)
		{
			return;
		}

		Log.Info(LogText.MilestonesAnnounced(_plan.Number, starting));
		ChatNotices.Broadcast(Messages.Milestones(starting, settings));
	}

	private void ResumePlan(WaveSettings settings, double now)
	{
		ApplyPlan(settings);
		Log.Info(LogText.WaveResumed(_plan.Number));
		BeginEnvironment(settings);
		BeginFriendlyFire(settings, now);
	}

	private void BeginEnvironment(WaveSettings settings)
	{
		EndEnvironment();
		if (!_plan.HasEnvironment || !_environment.Begin(settings.EnvironmentTypes))
		{
			return;
		}

		_environmentTally.Record(_plan.Number, settings.Environment.Block(_plan.Number));
		string type = _environment.Current;
		Log.Info(LogText.EnvironmentStarted(_plan.Number, type));
		ChatNotices.Broadcast(Messages.EnvironmentStarted(LocalizedNames.Find(type) ?? type));
	}

	private void BeginFriendlyFire(WaveSettings settings, double now)
	{
		EndFriendlyFire();
		if (_plan.FriendlyFireChance <= 0 || PlayerRoster.PlayerCount() < Tuning.FriendlyFireMinPlayers ||
		    UnityEngine.Random.value >= _plan.FriendlyFireChance)
		{
			return;
		}

		_friendlyFire.Begin(settings.DarkUrgeStrength, now);
		Log.Info(LogText.FriendlyFireStarted(_plan.Number, settings.DarkUrgeStrength.ToString()));
		ChatNotices.Broadcast(Messages.FriendlyFireStarted(_plan.FriendlyFireChance));
	}

	private void EndFriendlyFire()
	{
		if (!_friendlyFire.IsActive)
		{
			return;
		}

		_friendlyFire.End();
		Log.Info(LogText.FriendlyFireEnded(_cycle.CurrentWave));
		ChatNotices.Broadcast(Messages.FriendlyFireEnded());
	}

	private void EndEnvironment()
	{
		string type = _environment.Current;
		if (type == null)
		{
			return;
		}

		_environment.End();
		Log.Info(LogText.EnvironmentEnded(_cycle.CurrentWave, type));
		ChatNotices.Broadcast(Messages.EnvironmentEnded(LocalizedNames.Find(type) ?? type));
	}

	public int FullStrengthMonstersInWave(WaveSettings settings)
	{
		if (_plan != null)
		{
			return _plan.FullStrengthMonsters;
		}

		if (_cycle == null)
		{
			return 0;
		}

		WaveContent content = new WaveContent(_catalog.ForeignTypeCount, _catalog.MirageSkinCount,
			_catalog.HeroicBossCount);
		return WaveSchedule.Plan(_cycle.CurrentWave + 1, PlayerRoster.PlayerCount(), settings, content,
				WaveSchedule.NoEnvironmentRoll, 0)
			.FullStrengthMonsters;
	}

	private void ApplyPlan(WaveSettings settings)
	{
		WaveContent content = new WaveContent(_catalog.ForeignTypeCount, _catalog.MirageSkinCount,
			_catalog.HeroicBossCount);
		int wave = _cycle.CurrentWave;
		_plan = WaveSchedule.Plan(wave, PlayerRoster.PlayerCount(), settings, content, UnityEngine.Random.value,
			_environmentTally.CountIn(settings.Environment.Block(wave)));
		_plan.RaisedWorlds = RaisedWorlds(settings.HeroicFastKill, _playerKeys.Present());
		_plan.MonsterWorld = MonsterWorld(_plan.ExtraWorlds + _plan.RaisedWorlds);
		LogPaceForRoster(settings);

		MonsterStrength.SetAmbientLevel(_plan.AmbientLevel);
		if (_plan.IsHunted)
		{
			_huntedZone.Begin(_plan.HuntLevel);
		}
	}

	private void LogPaceForRoster(WaveSettings settings)
	{
		if (_plan.RaisedWorlds == _loggedRaisedWorlds)
		{
			return;
		}

		_loggedRaisedWorlds = _plan.RaisedWorlds;
		Log.Info(LogText.PaceForRoster(_plan.Number, _plan.RaisedWorlds, RaisedWorlds(settings.HeroicFastKill, null)));
	}

	private static double MonsterWorld(double extraWorlds)
	{
		return WorldScaling.RealZoneIndex() + Tuning.WorldOfZoneIndexZero + extraWorlds;
	}

	private void OnEnemyHit(EnemyHit hit)
	{
		if (hit.Tier == EnemyTier.HeroicBoss && _heroicFastKill.Active)
		{
			_heroicFastKill.OnHit(hit, _playerKeys.Of(hit.Attacker));
		}
	}

	private void MeasureHeroicFight(int wave)
	{
		HeroicFastKillRule rule = _settings.HeroicFastKill;
		if (!_heroicFastKill.Measured)
		{
			Log.Info(LogText.HeroicFastKillUnmeasured(wave));
			return;
		}

		double projected = _heroicFastKill.ProjectedSeconds;
		double step = rule.StepFor(projected);
		if (step <= HeroicFastKillRule.NoChange)
		{
			Log.Info(LogText.HeroicFastKillMissed(wave, _heroicFastKill.BestBurstDamage, _heroicFastKill.BurstSeconds,
				projected, rule.TargetSeconds));
			return;
		}

		_pace.Add(_heroicFastKill.Record(_plan.ExtraWorlds + _plan.RaisedWorlds));
		double raised = RaisedWorlds(rule, _playerKeys.Present());
		double world = MonsterWorld(_plan.ExtraWorlds + raised);
		Log.Info(LogText.HeroicFastKill(wave, _heroicFastKill.BestBurstDamage, _heroicFastKill.BurstSeconds, projected,
			rule.TargetSeconds, step, rule.MaxStep, raised - _plan.RaisedWorlds, raised));
		ChatNotices.Broadcast(Messages.HeroicFastKill(projected, rule.TargetSeconds, _plan.MonsterWorld, world));
	}

	private void FinishWave(double breakSeconds, bool curseToStart)
	{
		_huntedZone.End();
		EndEnvironment();
		EndFriendlyFire();

		if (_heroicFastKill.Active && _settings != null)
		{
			MeasureHeroicFight(_cycle.CurrentWave);
		}

		_heroicFastKill.Cancel();
		Log.Info(LogText.WaveCleared(_cycle.CurrentWave));
		if (curseToStart)
		{
			ChatNotices.Broadcast(Messages.WaveClearedAwaitingCurse(_cycle.CurrentWave, _cycle.CurrentWave + 1));
		}
		else
		{
			ChatNotices.Broadcast(Messages.WaveCleared(_cycle.CurrentWave, (int)Math.Ceiling(breakSeconds)));
		}

		WaveCleared?.Invoke(_cycle.CurrentWave);
	}

	private void ReleaseSpawns(double now, WaveSettings settings)
	{
		if (_plan == null)
		{
			return;
		}

		foreach (EnemyTier tier in _queue.Release(now, _enemies.Occupied(now), _plan.MaxAlive,
			         settings.SpawnIntervalSeconds, settings.SpawnsPerInterval, settings.HeroicBossEntranceDelaySeconds))
		{
			Spawn(tier, _plan, now, settings);
		}
	}

	private void Spawn(EnemyTier tier, WavePlan plan, double now, WaveSettings settings)
	{
		Vector3 position = _spawnPoints.Next(settings.MinSpawnDistanceFromHeroes);

		_enemies.Reserve(now);
		bool spawned;
		try
		{
			spawned = _spawner.Spawn(tier, plan.ForeignTypesUnlocked, position, entity =>
			{
				Enhance(entity, tier, plan);
				_onSpawned(entity, tier);
			});
		}
		catch (Exception e)
		{
			_spawnErrors.Report(now, LogText.SpawnFailed(tier), e);
			spawned = false;
		}

		if (!spawned)
		{
			Log.Warn(LogText.SpawnSkipped(tier));
		}
		else if (tier == EnemyTier.HeroicBoss)
		{
			ChatNotices.Broadcast(Messages.HeroicBossArrives());
		}
	}

	private void Enhance(Entity entity, EnemyTier tier, WavePlan plan)
	{
		if (entity == null)
		{
			return;
		}

		double extraWorlds = plan.ExtraWorlds + plan.RaisedWorlds;
		WorldScaling.BonusPercentFor(entity, extraWorlds, out double health, out double damage);
		double merge = tier.IsMob() ? plan.MergeMultiplier : MergeRule.NoChange;
		MonsterEnhancer.ApplyStatBonus(entity, MergeRule.RaiseBonus(health, merge),
			MergeRule.RaiseBonus(damage, merge));

		if (tier == EnemyTier.Hunter)
		{
			MonsterEnhancer.MakeHunter(entity);
		}

		if (tier == EnemyTier.HeroicBoss)
		{
			double haste = plan.HeroicBossHaste;
			int wave = plan.Number;
			Dew.CallDelayed(() =>
			{
				MonsterEnhancer.AddAbilityHaste(entity, haste);
				HeroicBossReport.Watch(entity, wave);
			});
		}

		if (tier.IsMob() && UnityEngine.Random.value < plan.MirageChance)
		{
			MonsterEnhancer.ApplyMirage(entity, _catalog.RandomMirageSkin(plan.MirageSkinsUnlocked));
		}
	}
}
