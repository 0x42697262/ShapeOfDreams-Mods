using System.Collections.Generic;

namespace SodLib.Game;

internal static class LiveHeroes
{
	public static void Fill(List<Hero> buffer)
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
