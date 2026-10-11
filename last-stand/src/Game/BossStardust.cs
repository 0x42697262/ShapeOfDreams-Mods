using LastStand.Config;
using UnityEngine;

namespace LastStand.Game;

internal static class BossStardust
{
	public static int Drop(Vector3 position)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		PickupManager pickups = NetworkedManagerBase<PickupManager>.softInstance;
		if (game == null || pickups == null || game.ges == null || game.difficulty == null)
		{
			return 0;
		}

		Vector2Int range = game.ges.stardustBossSoulAmount;
		int amount = Random.Range(range.x, range.y + 1) + game.difficulty.bossKillBonusStardust
		             + Tuning.EvilLucidDreamBossStardust * EvilLucidDreams();
		pickups.DropStarDust(amount, position);
		return amount;
	}

	private static int EvilLucidDreams()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int count = 0;
		foreach (Actor actor in actors.allActors)
		{
			if (actor is LucidDream { type: LucidDreamType.Evil })
			{
				count++;
			}
		}

		return count;
	}
}
