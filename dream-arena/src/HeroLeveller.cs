using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Levels every hero to the cap.
/// </summary>
/// <remarks>
/// Experience is a PvE progression curve, and in a match it only rewards
/// whoever farmed most recently. Starting everyone at the cap means builds are
/// compared at full strength.
///
/// <c>EntityStatus.level</c> is server-only by design — it throws outright on a
/// client — and <c>Hero.maxLevel</c> resolves either the hero's own override or
/// the global <c>ges.maxHeroLevel</c>, so this respects whatever the game
/// considers the ceiling.
/// </remarks>
internal static class HeroLeveller
{
	public static void Tick(DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null
		    || !config.arenaEnabled || !config.maxPlayerLevel)
		{
			return;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.instance;
		if (actors == null)
		{
			return;
		}

		foreach (Hero hero in actors.allHeroes)
		{
			if (hero.IsNullOrInactive() || hero.Status == null)
			{
				continue;
			}

			int cap = hero.maxLevel;
			if (cap <= 0 || hero.Status.level >= cap)
			{
				continue;
			}

			hero.Status.level = cap;
			Debug.Log($"{ModIdentity.LogPrefix}levelled {hero.name} to {cap}.");
		}
	}
}
