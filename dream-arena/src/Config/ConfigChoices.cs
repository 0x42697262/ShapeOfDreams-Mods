using System.Collections.Generic;
using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.Config;

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

	public static readonly IReadOnlyList<Choice> DummyMonsters = new[]
	{
		new Choice(GameNames.Treant, ConfigText.ForestWorld),
		new Choice(GameNames.SpiderWarrior, ConfigText.ForestWorld),
		new Choice(GameNames.Hound, ConfigText.ForestWorld),
		new Choice(GameNames.Oppressor, ConfigText.DarkCaveWorld),
		new Choice(GameNames.DarkElemental, ConfigText.DarkCaveWorld),
		new Choice(GameNames.Displacer, ConfigText.DespairWorld),
		new Choice(GameNames.DreadBug, ConfigText.DespairWorld),
		new Choice(GameNames.Baam, ConfigText.SkyWorld),
		new Choice(GameNames.StellaMatter, ConfigText.SkyWorld),
		new Choice(GameNames.VikingWarrior, ConfigText.SnowMountainWorld),
		new Choice(GameNames.IceElemental, ConfigText.SnowMountainWorld),
		new Choice(GameNames.Magmadon, ConfigText.LavaLandWorld),
		new Choice(GameNames.FireElemental, ConfigText.LavaLandWorld),
		new Choice(GameNames.InkTiger, ConfigText.InkWorld),
		new Choice(GameNames.DivineAnimal, ConfigText.InkWorld),
	};

	public static IReadOnlyList<Choice> For(ChoiceSet set)
	{
		return set == ChoiceSet.DummyMonsters ? DummyMonsters : ArenaRooms;
	}
}
