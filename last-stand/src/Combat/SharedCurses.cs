using System;
using System.Collections.Generic;
using LastStand.Game;

namespace LastStand.Combat;

internal static class SharedCurses
{
	private const int PlaceholderKills = 1;

	private static readonly List<DewPlayer> Players = new List<DewPlayer>();
	private static readonly List<CurseStatusEffect> Pool = new List<CurseStatusEffect>();
	private static readonly List<CurseStatusEffect> Choices = new List<CurseStatusEffect>();
	private static readonly HashSet<Type> Taken = new HashSet<Type>();

	public static int CurseOthers(Hero chooser, HatredStrengthType strength)
	{
		if (chooser == null || strength == HatredStrengthType.None)
		{
			return 0;
		}

		FillPool();
		PlayerRoster.FillPlayersWithLiveHeroes(Players);
		Taken.Clear();
		foreach (DewPlayer player in Players)
		{
			AddCursesOf(player.hero);
		}

		int cursed = 0;
		foreach (DewPlayer player in Players)
		{
			Hero hero = player.hero;
			if (hero == chooser)
			{
				continue;
			}

			CurseStatusEffect curse = Pick(hero, strength);
			if (curse == null)
			{
				continue;
			}

			Taken.Add(curse.GetType());
			Give(hero, curse, strength);
			cursed++;
		}

		return cursed;
	}

	private static void FillPool()
	{
		Pool.Clear();
		foreach (CurseStatusEffect curse in DewResources.FindAllByType<CurseStatusEffect>(ResourceLoadSettings.Light))
		{
			if (curse != null && Dew.IsCurseIncludedInGame(curse.GetType().Name))
			{
				Pool.Add(curse);
			}
		}
	}

	private static void AddCursesOf(Hero hero)
	{
		foreach (StatusEffect effect in hero.Status.statusEffects)
		{
			if (effect is CurseStatusEffect)
			{
				Taken.Add(effect.GetType());
			}
		}
	}

	private static CurseStatusEffect Pick(Hero hero, HatredStrengthType strength)
	{
		FillChoices(hero, strength, true);
		if (Choices.Count == 0)
		{
			FillChoices(hero, strength, false);
		}

		return Choices.Count == 0 ? null : Dew.SelectRandomWeightedInList(Choices, curse => curse.chanceWeight);
	}

	private static void FillChoices(Hero hero, HatredStrengthType strength, bool skipTaken)
	{
		Choices.Clear();
		foreach (CurseStatusEffect curse in Pool)
		{
			if (curse.availableStrengths.HasFlag(strength) && curse.chanceWeight > 0 && curse.IsViable(hero) &&
			    !(skipTaken && Taken.Contains(curse.GetType())))
			{
				Choices.Add(curse);
			}
		}
	}

	private static void Give(Hero hero, CurseStatusEffect asset, HatredStrengthType strength)
	{
		hero.CreateStatusEffect(asset, hero, new CastInfo(hero, hero), (CurseStatusEffect curse) =>
		{
			curse.currentStrength = strength;
			curse.skillLevel = (int)(strength - 1);
			curse.progressType = QuestProgressType.Kills;
			curse.requiredAmount = PlaceholderKills;
		});
	}
}
