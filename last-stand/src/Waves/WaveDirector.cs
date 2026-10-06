using System;
using LastStand.Arena;
using LastStand.Combat;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Waves;

internal sealed class WaveDirector
{
	private readonly SpawnQueue _queue = new SpawnQueue();
	private readonly MonsterCatalog _catalog = new MonsterCatalog();
	private readonly HuntedZone _huntedZone = new HuntedZone();
	private readonly SpawnPoints _spawnPoints = new SpawnPoints();
	private readonly MonsterSpawner _spawner;
	private readonly EnemyRegistry _enemies;
	private readonly Action<Entity, EnemyTier> _onSpawned;

	private WaveCycle _cycle;
	private WavePlan _plan;

	public event Action<WavePlan> WaveStarted;

	public event Action<int> WaveCleared;

	public int CurrentWave => _cycle?.CurrentWave ?? 0;

	public bool WaveInProgress => _cycle?.InProgress ?? false;

	public int PendingSpawns => _queue.PendingCount;

	public WaveDirector(EnemyRegistry enemies, HeroicBosses heroicBosses, Action<Entity, EnemyTier> onSpawned)
	{
		_enemies = enemies;
		_onSpawned = onSpawned;
		_spawner = new MonsterSpawner(_catalog, heroicBosses);
	}

	public void Begin(double firstWaveAt)
	{
		_cycle = new WaveCycle(firstWaveAt);
		_plan = null;
		_queue.Clear();
	}

	public bool HasPending(EnemyTier tier)
	{
		return _queue.HasPending(tier);
	}

	public double SecondsUntilNextWave(double now)
	{
		return _cycle?.SecondsUntilNextWave(now) ?? 0;
	}

	public void StartNextWaveNow(double now)
	{
		_cycle?.StartNextWaveNow(now);
	}

	public void Tick(double now, WaveSettings settings)
	{
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

		if (_cycle.TryStartWave(now))
		{
			StartWave(settings);
		}

		ReleaseSpawns(now, settings);

		bool fieldClear = _queue.PendingCount == 0 && _enemies.Occupied(now) == 0;
		if (_cycle.TryFinishWave(now, fieldClear, settings.IntermissionSeconds))
		{
			FinishWave(settings);
		}
	}

	public void RestoreHuntedZone()
	{
		_huntedZone.Restore();
	}

	public void Reset()
	{
		_huntedZone.End();
		_cycle = null;
		_plan = null;
		_queue.Clear();
		_spawner.Reset();
		_catalog.Clear();
		_spawnPoints.Clear();
	}

	private bool TryBuildCatalog(WaveSettings settings)
	{
		if (!_catalog.TryBuild(settings.HeroicBossTypes))
		{
			return false;
		}

		Log.Info(LogText.CatalogBuilt(_catalog.ForeignTypeCount, _catalog.MirageSkinCount,
			WaveSchedule.HunterStartWave(settings, _catalog.ForeignTypeCount), _catalog.HeroicBossCount));
		return true;
	}

	private void StartWave(WaveSettings settings)
	{
		WaveContent content = new WaveContent(_catalog.ForeignTypeCount, _catalog.MirageSkinCount,
			_catalog.HeroicBossCount);
		_plan = WaveSchedule.Plan(_cycle.CurrentWave, PlayerRoster.PlayerCount(), settings, content);

		MonsterStrength.SetAmbientLevel(_plan.AmbientLevel);
		if (_plan.IsHunted)
		{
			_huntedZone.Begin(_plan.HuntLevel);
		}

		_queue.Enqueue(_plan);

		Log.Info(LogText.WaveStarted(_plan));
		ChatNotices.Broadcast(Messages.WaveStarted(_plan));
		WaveStarted?.Invoke(_plan);
	}

	private void FinishWave(WaveSettings settings)
	{
		_huntedZone.End();

		int seconds = (int)Math.Ceiling(settings.IntermissionSeconds);
		Log.Info(LogText.WaveCleared(_cycle.CurrentWave));
		ChatNotices.Broadcast(Messages.WaveCleared(_cycle.CurrentWave, seconds));
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
		bool spawned = _spawner.Spawn(tier, plan.ForeignTypesUnlocked, position, entity =>
		{
			Enhance(entity, tier, plan, settings);
			_onSpawned(entity, tier);
		});

		if (!spawned)
		{
			Log.Warn(LogText.SpawnSkipped(tier));
		}
		else if (tier == EnemyTier.HeroicBoss)
		{
			ChatNotices.Broadcast(Messages.HeroicBossArrives());
		}
	}

	private void Enhance(Entity entity, EnemyTier tier, WavePlan plan, WaveSettings settings)
	{
		if (entity == null)
		{
			return;
		}

		MonsterEnhancer.ApplyWaveScaling(entity, plan);

		if (tier == EnemyTier.Hunter)
		{
			MonsterEnhancer.MakeHunter(entity);
		}

		if (tier == EnemyTier.HeroicBoss)
		{
			double health = settings.HeroicBossHealthMultiplier;
			double damage = settings.HeroicBossDamageMultiplier;
			double haste = settings.HeroicBossAbilityHaste;
			Dew.CallDelayed(() =>
			{
				MonsterEnhancer.MultiplyStats(entity, health, damage);
				MonsterEnhancer.AddAbilityHaste(entity, haste);
			});
		}

		if (tier.IsMob() && UnityEngine.Random.value < plan.MirageChance)
		{
			MonsterEnhancer.ApplyMirage(entity, _catalog.RandomMirageSkin(plan.MirageSkinsUnlocked));
		}
	}
}
