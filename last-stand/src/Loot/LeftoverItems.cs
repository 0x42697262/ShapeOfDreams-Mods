namespace LastStand.Loot;

internal static class LeftoverItems
{
	public static int Release()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int released = 0;
		foreach (Actor actor in actors.allActors)
		{
			if (actor is SkillTrigger memory && Release(memory))
			{
				released++;
			}
			else if (actor is Gem essence && Release(essence))
			{
				released++;
			}
		}

		return released;
	}

	private static bool Release(SkillTrigger memory)
	{
		if (!memory.isActive || memory.tempOwner == null || memory.owner != null || memory.handOwner != null)
		{
			return false;
		}

		memory.tempOwner = null;
		return true;
	}

	private static bool Release(Gem essence)
	{
		if (!essence.isActive || essence.tempOwner == null || essence.owner != null || essence.handOwner != null)
		{
			return false;
		}

		essence.tempOwner = null;
		return true;
	}
}
