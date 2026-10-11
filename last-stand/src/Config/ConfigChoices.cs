using System.Collections.Generic;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Config;

internal static class ConfigChoices
{
	public static readonly IReadOnlyList<Choice> ArenaRooms = SharedChoices.ArenaRooms;

	public static readonly IReadOnlyList<Choice> HeroicBosses = new[]
	{
		new Choice(GameNames.ForestBoss, SharedConfigText.ForestWorld),
		new Choice(GameNames.DarkCaveBoss, SharedConfigText.DarkCaveWorld),
		new Choice(GameNames.DespairBoss, SharedConfigText.DespairWorld),
		new Choice(GameNames.SkyBoss, SharedConfigText.SkyWorld),
		new Choice(GameNames.SnowMountainBoss, SharedConfigText.SnowMountainWorld),
		new Choice(GameNames.LavaLandBoss, SharedConfigText.LavaLandWorld),
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

	public static readonly IReadOnlyList<Choice> CurseStrengths = new[]
	{
		new Choice(GameNames.MildStrength, ConfigText.MildDarkUrge),
		new Choice(GameNames.PotentStrength, ConfigText.PotentDarkUrge),
		new Choice(GameNames.PowerfulStrength, ConfigText.PowerfulDarkUrge),
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
		switch (set)
		{
			case ChoiceSet.HeroicBosses:
				return HeroicBosses;
			case ChoiceSet.Environments:
				return Environments;
			case ChoiceSet.CurseStrengths:
				return CurseStrengths;
			default:
				return ArenaRooms;
		}
	}
}
