using LastStand.Config;
using UnityEngine;

namespace LastStand.Combat;

internal static class PainRewards
{
	public static void Grant(Vector3 position)
	{
		Dew.CreateActor<Shrine_CorruptedChaos>(Dew.GetGoodRewardPosition(position, Tuning.RewardScatter), null);

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player == null || player.hero.IsNullInactiveDeadOrKnockedOut())
			{
				continue;
			}

			if (Random.value > player.doubleChaosChance)
			{
				continue;
			}

			string guid = player.guid;
			Dew.CreateActor<Shrine_CorruptedChaos>(
				Dew.GetGoodRewardPosition(position, Tuning.RewardScatter), null, null,
				chaos => chaos.playersOverride = new[] { guid });
		}
	}
}
