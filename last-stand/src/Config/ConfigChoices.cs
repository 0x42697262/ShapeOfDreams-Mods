using LastStand.Game;
using LastStand.Text;
using System.Collections.Generic;

namespace LastStand.Config;

internal static class ConfigChoices
{
	public static readonly IReadOnlyList<Choice> ArenaRooms = new[]
	{
		new Choice(GameNames.PrimusArenaRoom, ConfigText.PrimusWorld),
		new Choice(GameNames.ForestArenaRoom, ConfigText.ForestWorld),
		new Choice(GameNames.DarkCaveArenaRoom, ConfigText.DarkCaveWorld),
		new Choice(GameNames.DespairArenaRoom, ConfigText.DespairWorld),
		new Choice(GameNames.SkyArenaRoom, ConfigText.SkyWorld),
		new Choice(GameNames.SnowMountainArenaRoom, ConfigText.SnowMountainWorld),
		new Choice(GameNames.LavaLandArenaRoom, ConfigText.LavaLandWorld),
		new Choice(GameNames.InkArenaRoom, ConfigText.InkWorld),
	};

	public static readonly IReadOnlyList<Choice> HeroicBosses = new[]
	{
		new Choice(GameNames.ForestBoss, ConfigText.ForestWorld),
		new Choice(GameNames.DarkCaveBoss, ConfigText.DarkCaveWorld),
		new Choice(GameNames.DespairBoss, ConfigText.DespairWorld),
		new Choice(GameNames.SkyBoss, ConfigText.SkyWorld),
		new Choice(GameNames.SnowMountainBoss, ConfigText.SnowMountainWorld),
		new Choice(GameNames.LavaLandBoss, ConfigText.LavaLandWorld),
	};

	public static readonly IReadOnlyList<Choice> Environments = new[]
	{
		new Choice(GameNames.AcceleratedTime, ConfigText.AcceleratedTimeDetail),
		new Choice(GameNames.UnstableVeilOfTime, ConfigText.UnstableVeilOfTimeDetail),
		new Choice(GameNames.GoldEverywhere, ConfigText.GoldEverywhereDetail),
		new Choice(GameNames.PureDream, ConfigText.PureDreamDetail),
		new Choice(GameNames.LingeringAuraOfGuidance, ConfigText.LingeringAuraOfGuidanceDetail),
		new Choice(GameNames.StarCookie, ConfigText.StarCookieDetail),
		new Choice(GameNames.BlackRain, ConfigText.BlackRainDetail),
		new Choice(GameNames.SymbioteHabitat, ConfigText.SymbioteHabitatDetail),
		new Choice(GameNames.InversionSigil, ConfigText.InversionSigilDetail),
		new Choice(GameNames.ToxicArea, ConfigText.ToxicAreaDetail),
		new Choice(GameNames.GravityTraining, ConfigText.GravityTrainingDetail),
		new Choice(GameNames.ArcticTerritory, ConfigText.ArcticTerritoryDetail),
		new Choice(GameNames.EngulfedInFlame, ConfigText.EngulfedInFlameDetail),
		new Choice(GameNames.DarkCondensationZone, ConfigText.DarkCondensationZoneDetail),
		new Choice(GameNames.VeilOfDark, ConfigText.VeilOfDarkDetail),
		new Choice(GameNames.WarpingField, ConfigText.WarpingFieldDetail),
		new Choice(GameNames.RiskOfMeteors, ConfigText.RiskOfMeteorsDetail),
		new Choice(GameNames.FireDevil, ConfigText.FireDevilDetail),
		new Choice(GameNames.InkStrikeWarning, ConfigText.InkStrikeWarningDetail),
		new Choice(GameNames.GazeOfErebos, ConfigText.GazeOfErebosDetail),
		new Choice(GameNames.UnstableRatSwarm, ConfigText.UnstableRatSwarmDetail),
		new Choice(GameNames.LeafPuppies, ConfigText.LeafPuppiesDetail),
		new Choice(GameNames.MeteoricLife, ConfigText.MeteoricLifeDetail),
	};

	public static readonly IReadOnlyList<Choice> CurseStrengths = new Choice[3]
	{
		new Choice("Mild", "Mild: 15% damage between players"),
		new Choice("Potent", "Potent: 25% damage between players"),
		new Choice("Powerful", "Powerful: 40% damage between players")
	};

	public static IReadOnlyList<string> Values(ChoiceSet set)
	{
		IReadOnlyList<Choice> choices = For(set);
		string[] values = new string[choices.Count];
		for (int i = 0; i < choices.Count; i++)
		{
			values[i] = choices[i].Value;
		}

		return values;
	}

	public static IReadOnlyList<Choice> For(ChoiceSet set)
	{
		return set switch
		{
			ChoiceSet.HeroicBosses => HeroicBosses, 
			ChoiceSet.Environments => Environments, 
			ChoiceSet.CurseStrengths => CurseStrengths, 
			_ => ArenaRooms, 
		};
	}
}
