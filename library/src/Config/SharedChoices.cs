using System.Collections.Generic;

namespace SodLib.Config;

internal static class SharedChoices
{
	public static readonly IReadOnlyList<Choice> ArenaRooms = new[]
	{
		new Choice(SharedGameNames.PrimusArenaRoom, SharedConfigText.PrimusWorld),
		new Choice(SharedGameNames.ForestArenaRoom, SharedConfigText.ForestWorld),
		new Choice(SharedGameNames.DarkCaveArenaRoom, SharedConfigText.DarkCaveWorld),
		new Choice(SharedGameNames.DespairArenaRoom, SharedConfigText.DespairWorld),
		new Choice(SharedGameNames.SkyArenaRoom, SharedConfigText.SkyWorld),
		new Choice(SharedGameNames.SnowMountainArenaRoom, SharedConfigText.SnowMountainWorld),
		new Choice(SharedGameNames.LavaLandArenaRoom, SharedConfigText.LavaLandWorld),
		new Choice(SharedGameNames.InkArenaRoom, SharedConfigText.InkWorld),
	};
}
