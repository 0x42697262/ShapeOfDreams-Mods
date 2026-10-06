using System.Collections.Generic;
using LastStand.Game;
using LastStand.Text;

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

	public static IReadOnlyList<Choice> For(ChoiceSet set)
	{
		return set == ChoiceSet.HeroicBosses ? HeroicBosses : ArenaRooms;
	}
}
