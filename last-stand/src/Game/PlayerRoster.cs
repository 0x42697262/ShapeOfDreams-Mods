using System.Collections.Generic;

namespace LastStand.Game;

internal static class PlayerRoster
{
	private const int AtLeastOnePlayer = 1;

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

	public static void FillLiveHeroes(List<Hero> buffer)
	{
		buffer.Clear();
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		foreach (Hero hero in actors.allHeroes)
		{
			if (!hero.IsNullInactiveDeadOrKnockedOut())
			{
				buffer.Add(hero);
			}
		}
	}
}
