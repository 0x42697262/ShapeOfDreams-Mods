using System.Collections.Generic;
using LastStand.Arena;

namespace LastStand.Combat;

internal static class DreamStatues
{
	private static readonly List<Entity> Found = new List<Entity>();

	public static bool IsStatue(Entity entity)
	{
		return entity.Status.TryGetStatusEffect(out Se_PureDream_Statue _) || IsLeftoverStatue(entity);
	}

	private static bool IsLeftoverStatue(Entity entity)
	{
		return entity is Monster monster && monster.owner != null && monster.owner == DewPlayer.environment
		       && monster.AI != null && monster.AI.disableAI;
	}

	public static int MoveAll()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		Found.Clear();
		foreach (Entity entity in actors.allEntities)
		{
			if (!entity.IsNullInactiveDeadOrKnockedOut() && IsStatue(entity))
			{
				Found.Add(entity);
			}
		}

		int moved = 0;
		foreach (Entity statue in Found)
		{
			if (PureDreamSpots.TryPickAnywhere(out UnityEngine.Vector3 spot))
			{
				statue.Control.Teleport(spot);
				moved++;
			}
		}

		Found.Clear();
		return moved;
	}
}
