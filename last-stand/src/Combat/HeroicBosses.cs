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

	private readonly HashSet<Entity> _owned = new HashSet<Entity>();

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
		return entity != null && _owned.Contains(entity);
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

		_owned.Add(boss);

		Hero target = Dew.GetClosestAliveHero(position);
		if (target != null)
		{
			boss.Control.MoveToDestination(target.agentPosition, false);
		}

		return boss;
	}

	public void OnDied(BossMonster boss, int chaosRewards)
	{
		_owned.Remove(boss);
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
		_owned.Clear();
	}
}
