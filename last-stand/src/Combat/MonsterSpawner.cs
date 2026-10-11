using System;
using System.Collections.Generic;
using System.Linq;
using LastStand.Game;
using UnityEngine;

namespace LastStand.Combat;

internal sealed class MonsterSpawner
{
	private const int SingleMonster = 1;
	private const float CertainChance = 1f;
	private const float NoTimeout = 0f;

	private readonly Dictionary<Monster, MonsterSpawnRule> _singleRules = new Dictionary<Monster, MonsterSpawnRule>();
	private readonly MonsterCatalog _catalog;
	private readonly HeroicBosses _heroicBosses;

	public MonsterSpawner(MonsterCatalog catalog, HeroicBosses heroicBosses)
	{
		_catalog = catalog;
		_heroicBosses = heroicBosses;
	}

	public bool Spawn(EnemyTier tier, int foreignUnlocked, Vector3 position, Action<Entity> onSpawned)
	{
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || room.monsters == null || !_catalog.IsReady)
		{
			return false;
		}

		if (tier == EnemyTier.HeroicBoss)
		{
			BossMonster heroic = _heroicBosses.Spawn(_catalog.RandomHeroicBoss(), position,
				MonsterStrength.CurrentLevel(), false);
			if (heroic == null)
			{
				return false;
			}

			onSpawned(heroic);
			return true;
		}

		SpawnMonsterSettings settings = new SpawnMonsterSettings
		{
			spawnPosGetter = () => position,
			afterSpawn = onSpawned,
			ignoreTurnPopMultiplier = true,
			ignoreCoopPopMultiplier = true,
		};

		if (tier.IsMob())
		{
			Monster monster = _catalog.NextRegular(foreignUnlocked);
			if (monster == null)
			{
				return false;
			}

			settings.rule = SingleRule(monster);
			room.monsters.SpawnMonsters(settings);
			return true;
		}

		Monster boss = _catalog.RandomMiniBoss(foreignUnlocked);
		MiniBossEffect effect = _catalog.RandomMiniBossEffect();
		if (boss == null || effect == null)
		{
			return false;
		}

		MiniBossPick.Set(boss, effect);
		try
		{
			room.monsters.SpawnMiniBoss(settings, boss, effect);
		}
		finally
		{
			MiniBossPick.Clear();
		}

		return true;
	}

	public void Reset()
	{
		foreach (MonsterSpawnRule rule in _singleRules.Values.Where(rule => rule != null))
		{
			UnityEngine.Object.Destroy(rule.pool);
			UnityEngine.Object.Destroy(rule);
		}

		_singleRules.Clear();
	}

	private MonsterSpawnRule SingleRule(Monster monster)
	{
		if (_singleRules.TryGetValue(monster, out MonsterSpawnRule cached) && cached != null)
		{
			return cached;
		}

		MonsterSpawnRule rule = ScriptableObject.CreateInstance<MonsterSpawnRule>();
		rule.isBossSpawn = true;
		rule.initialDelay = Vector2.zero;
		rule.wavesMin = SingleMonster;
		rule.wavesMax = SingleMonster;
		rule.waveTimeoutMin = NoTimeout;
		rule.waveTimeoutMax = NoTimeout;
		rule.onOverPopulation = OverpopulationBehavior.Ignore;
		rule.pool = ScriptableObject.CreateInstance<MonsterPool>();
		rule.pool.scrambleOrder = false;
		rule.pool.entries = new List<MonsterPool.SpawnRuleEntry>
		{
			new MonsterPool.SpawnRuleEntry
			{
				monster = monster,
				chance = CertainChance,
				minCount = SingleMonster,
				maxCount = SingleMonster,
			},
		};

		_singleRules[monster] = rule;
		return rule;
	}
}
