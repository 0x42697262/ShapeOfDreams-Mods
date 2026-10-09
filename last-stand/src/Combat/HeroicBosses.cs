using System;
using System.Collections.Generic;
using System.Reflection;
using LastStand.Arena;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Combat;

internal sealed class HeroicBosses
{
	private const BindingFlags OwnMethod = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

	private readonly HashSet<Entity> _spawned = new HashSet<Entity>();
	private readonly HashSet<Entity> _rewarded = new HashSet<Entity>();
	private readonly List<Entity> _leftovers = new List<Entity>();

	public static BossMonster Find(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			return null;
		}

		try
		{
			return DewResources.GetByShortTypeName<BossMonster>(typeName);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static bool IsSupported(BossMonster prefab)
	{
		if (prefab == null || prefab.isHiddenBoss)
		{
			return false;
		}

		for (Type type = prefab.GetType(); type != null && type != typeof(BossMonster); type = type.BaseType)
		{
			if (type.GetMethod(GameNames.EntityOnDeath, OwnMethod) != null)
			{
				return false;
			}
		}

		return true;
	}

	public bool Owns(Entity entity)
	{
		if (entity == null)
		{
			return false;
		}

		if (_spawned.Contains(entity))
		{
			return true;
		}

		foreach (Entity boss in _spawned)
		{
			if (boss != null && entity.IsDescendantOf(boss))
			{
				return true;
			}
		}

		return entity is BossMonster unclaimed && IsSupported(unclaimed) && ArenaLocator.IsInArenaNode();
	}

	public BossMonster Spawn(BossMonster prefab, Vector3 position, int level, bool skipEntrance)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (prefab == null || actors == null)
		{
			return null;
		}

		BossMonster boss = Dew.SpawnEntity(prefab, position, null, actors.serverActor, DewPlayer.creep, level,
			spawned =>
			{
				spawned.NetworkskipBossSoulFlow = true;
				spawned.playBossKillFeedback = false;
				if (skipEntrance)
				{
					spawned.Visual.NetworkskipSpawning = true;
				}
			});

		if (boss == null)
		{
			return null;
		}

		_spawned.Add(boss);

		Hero target = Dew.GetClosestAliveHero(position);
		if (target != null)
		{
			boss.Control.MoveToDestination(target.agentPosition, false);
		}

		return boss;
	}

	public void OnDied(BossMonster boss, int chaosRewards)
	{
		KillLeftovers(boss);
		if (HasBossAncestor(boss) || !_rewarded.Add(boss))
		{
			return;
		}

		Vector3 position = boss.agentPosition;

		for (int i = 0; i < chaosRewards; i++)
		{
			Dew.CreateActor<Shrine_CorruptedChaos>(Dew.GetGoodRewardPosition(position, Tuning.RewardScatter), null);
		}

		Log.Info(LogText.HeroicBossDied(chaosRewards));
		ChatNotices.Broadcast(Messages.HeroicBossDefeated(chaosRewards));
	}

	public void Clear()
	{
		_spawned.Clear();
		_rewarded.Clear();
		_leftovers.Clear();
	}

	private static bool HasBossAncestor(Actor actor)
	{
		for (Actor parent = actor.parentActor; parent != null; parent = parent.parentActor)
		{
			if (parent is BossMonster)
			{
				return true;
			}
		}

		return false;
	}

	private void KillLeftovers(BossMonster boss)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		_leftovers.Clear();
		foreach (Entity entity in actors.allEntities)
		{
			if (entity != boss && entity is Monster && !entity.IsNullInactiveDeadOrKnockedOut()
			    && entity.IsDescendantOf(boss))
			{
				_leftovers.Add(entity);
			}
		}

		foreach (Entity leftover in _leftovers)
		{
			leftover.Kill();
		}

		_leftovers.Clear();
	}
}
