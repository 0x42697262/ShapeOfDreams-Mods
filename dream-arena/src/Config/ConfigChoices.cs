using System.Collections.Generic;
using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.Config;

internal static class ConfigChoices
{
	public static readonly IReadOnlyList<Choice> ArenaRooms = SharedChoices.ArenaRooms;

	public static readonly IReadOnlyList<Choice> DummyMonsters = new[]
	{
		new Choice(GameNames.Treant, SharedConfigText.ForestWorld),
		new Choice(GameNames.SpiderWarrior, SharedConfigText.ForestWorld),
		new Choice(GameNames.Hound, SharedConfigText.ForestWorld),
		new Choice(GameNames.Oppressor, SharedConfigText.DarkCaveWorld),
		new Choice(GameNames.DarkElemental, SharedConfigText.DarkCaveWorld),
		new Choice(GameNames.Displacer, SharedConfigText.DespairWorld),
		new Choice(GameNames.DreadBug, SharedConfigText.DespairWorld),
		new Choice(GameNames.Baam, SharedConfigText.SkyWorld),
		new Choice(GameNames.StellaMatter, SharedConfigText.SkyWorld),
		new Choice(GameNames.VikingWarrior, SharedConfigText.SnowMountainWorld),
		new Choice(GameNames.IceElemental, SharedConfigText.SnowMountainWorld),
		new Choice(GameNames.Magmadon, SharedConfigText.LavaLandWorld),
		new Choice(GameNames.FireElemental, SharedConfigText.LavaLandWorld),
		new Choice(GameNames.InkTiger, SharedConfigText.InkWorld),
		new Choice(GameNames.DivineAnimal, SharedConfigText.InkWorld),
	};

	public static IReadOnlyList<Choice> For(ChoiceSet set)
	{
		return set == ChoiceSet.DummyMonsters ? DummyMonsters : ArenaRooms;
	}
}
