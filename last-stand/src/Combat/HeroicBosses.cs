using System;
using System.Collections.Generic;
using System.Reflection;
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

		return false;
	}

	public BossMonster Spawn(BossMonster prefab, Vector3 position, int level, bool skipEntrance)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (prefab == null || actors == null)
		{
			return null;
		}

		GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
		MidJoinWaitType midJoin = settings != null ? settings.midJoinWaitType : default(MidJoinWaitType);

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

		if (settings != null)
		{
			settings.midJoinWaitType = midJoin;
		}

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
		if (!_spawned.Contains(boss) || !_rewarded.Add(boss))
		{
			return;
		}

		Vector3 position = boss.agentPosition;

		for (int i = 0; i < chaosRewards; i++)
		{
			Dew.CreateActor<Shrine_Chaos>(Dew.GetGoodRewardPosition(position, Tuning.RewardScatter), null, null,
				chaos => chaos.Networkrarity = Rarity.Legendary);
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
