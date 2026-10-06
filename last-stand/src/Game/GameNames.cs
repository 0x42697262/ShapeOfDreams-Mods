namespace LastStand.Game;

internal static class GameNames
{
	public const string PrimusArenaRoom = "Room_Primus_Boss_0";

	public const string ForestArenaRoom = "Room_Forest_Boss_0";

	public const string DarkCaveArenaRoom = "Room_DarkCave_Boss_0";

	public const string DespairArenaRoom = "Room_Despair_Boss_0";

	public const string SkyArenaRoom = "Room_Sky_Boss_0";

	public const string SnowMountainArenaRoom = "Room_SnowMountain_Boss_0";

	public const string LavaLandArenaRoom = "Room_LavaLand_Boss_0";

	public const string InkArenaRoom = "Room_Ink_Boss_0";

	public const string DefaultArenaRoom = PrimusArenaRoom;

	public const string ForestBoss = "Mon_Forest_BossDemon";

	public const string DarkCaveBoss = "Mon_DarkCave_BossSeeker";

	public const string DespairBoss = "Mon_Despair_BossAzurak";

	public const string SkyBoss = "Mon_Sky_BossNyx";

	public const string SnowMountainBoss = "Mon_SnowMountain_BossSkoll";

	public const string LavaLandBoss = "Mon_LavaLand_BossInfernus";

	public const string LocalizedNameSuffix = "_Name";

	public static readonly string[] BossMusicTrackGuids =
	{
		"44b4082316a67a34aa42c158823ffd18",
		"fdbb42f02a7667d4ebb445250c947bd3",
		"11dd0ca2f5eb7224bb06418696194032",
		"ec9380823cabf9d4e9de7843488f768f",
		"b9c1464d58f50954c8f3fd5c1fd704c3",
		"39aeb947b3bd006458ac51da21af0536",
		"add7fcbb30744e740bd82fc26d4974b7",
		"5c6e5c46604e8f8408d039c951f4cbb0",
		"b7df9f9f9b461e847aa4f227109da88b",
	};

	public const string HeroSoulModifier = "RoomMod_HeroSoul";

	public const string LobbySettingsStart = "Start";

	public const string LobbySettingsEnable = "OnEnable";

	public const string LimboModifier = "GameMod_Limbo";

	public const string ZonePrefix = "Zone_";

	public const string TitleViewStateChanged = "OnStateChanged";

	public const string CreateLobbyShow = "OnShow";

	public const string CreateLobbyUpdateState = "UpdateUIState";

	public const string CreateLobbyState = "_state";

	public const string GenerateWorld = "GenerateWorld_Imp";

	public const string LocationRoomLoaded = "OnRoomLoaded";

	public const string EntityOnDeath = "OnDeath";

	public const string DefaultTestBoss = ForestBoss;

	public const string DefaultHeroicBosses =
		ForestBoss + ", " + DarkCaveBoss + ", " + DespairBoss + ", " + SkyBoss + ", " + SnowMountainBoss + ", " +
		LavaLandBoss;

	public const string ActorOnCreate = "OnCreate";

	public const string ActorOnDestroy = "OnDestroyActor";
}
