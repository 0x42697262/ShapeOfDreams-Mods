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
