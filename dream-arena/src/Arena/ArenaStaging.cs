using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Text;

namespace DreamArena.Arena;

internal static class ArenaStaging
{
	private static readonly List<Hero> Heroes = new List<Hero>();

	public static bool TryPlaceInRing()
	{
		if (!ArenaLayout.TryFind(true, out ArenaPoints arena))
		{
			return false;
		}

		LiveHeroes.Fill(Heroes);
		if (Heroes.Count == 0)
		{
			return false;
		}

		float ring = HeroStaging.PlaceAround(arena, Heroes, Tuning.ArenaRingShare, Tuning.MinRingRadius);
		Log.Info(LogText.HeroesStaged(Heroes.Count, arena.Source, arena.Center, ring));
		Heroes.Clear();
		return true;
	}
}
