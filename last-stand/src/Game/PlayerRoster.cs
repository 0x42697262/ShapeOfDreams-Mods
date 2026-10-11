using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace LastStand.Game;

internal static class PlayerRoster
{
	private const int AtLeastOnePlayer = 1;

	private const int MinimumHeroLevel = 1;

	public static int ReadyConnections()
	{
		int ready = 0;
		foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
		{
			if (connection != null && connection.isReady)
			{
				ready++;
			}
		}

		return ready;
	}

	public static int PlayerCount()
	{
		int count = 0;
		if (DewPlayer.gamePlayers != null)
		{
			foreach (DewPlayer player in DewPlayer.gamePlayers)
			{
				if (player != null && player.isHumanPlayer)
				{
					count++;
				}
			}
		}

		return count < AtLeastOnePlayer ? AtLeastOnePlayer : count;
	}

	public static int GoldSharers()
	{
		int players = DewPlayer.gamePlayers != null ? DewPlayer.gamePlayers.Count : 0;
		return players < AtLeastOnePlayer ? AtLeastOnePlayer : players;
	}

	public static int GiveGold(int amount)
	{
		int players = 0;
		if (amount <= 0 || DewPlayer.gamePlayers == null)
		{
			return players;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null)
			{
				player.EarnGold(amount);
				players++;
			}
		}

		return players;
	}

	public static int GiveDreamDust(int amount)
	{
		int players = 0;
		if (amount <= 0 || DewPlayer.gamePlayers == null)
		{
			return players;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null)
			{
				player.EarnDreamDust(amount);
				players++;
			}
		}

		return players;
	}

	public static int SetHeroLevel(int level)
	{
		return ForEachHero(hero => hero.Status.level = Mathf.Clamp(level, MinimumHeroLevel, hero.maxLevel));
	}

	public static int ForEachHero(Action<Hero> action)
	{
		int heroes = 0;
		if (DewPlayer.gamePlayers == null)
		{
			return heroes;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			Hero hero = player != null ? player.hero : null;
			if (hero.IsNullOrInactive())
			{
				continue;
			}

			action(hero);
			heroes++;
		}

		return heroes;
	}

	public static void FillPlayersWithLiveHeroes(List<DewPlayer> buffer)
	{
		buffer.Clear();
		if (DewPlayer.gamePlayers == null)
		{
			return;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null && player.isHumanPlayer && !player.hero.IsNullInactiveDeadOrKnockedOut())
			{
				buffer.Add(player);
			}
		}
	}

}
