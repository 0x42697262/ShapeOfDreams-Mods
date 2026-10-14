

namespace LastStand.Game;

internal static class GameNames
{
	public const string CurseOnCreate = "OnCreate";

	public const string HatredDoCurse = "DoCurse";

	public const string ArenaPanelModifier = nameof(RoomMod_FragmentOfRadiance_MysteriousPlace);

	public const string ArenaPanelSidetrackRift = "Rift_Sidetrack_FragmentOfRadiance";

	public const string TrackerTitleKey = "InGame_ThreatLevelDisplay_ThreatLevelRising";

	public const string TrackerDescriptionKey = "RoomMod_HarderFightBetterReward_Name";

	public const string ForestBoss = "Mon_Forest_BossDemon";

	public const string DarkCaveBoss = "Mon_DarkCave_BossSeeker";

	public const string DespairBoss = "Mon_Despair_BossAzurak";

	public const string SkyBoss = "Mon_Sky_BossNyx";

	public const string SnowMountainBoss = "Mon_SnowMountain_BossSkoll";

	public const string LavaLandBoss = "Mon_LavaLand_BossInfernus";

	public const string HeroSoulModifier = "RoomMod_HeroSoul";

	public const string LimboModifier = "GameMod_Limbo";

	public const string ZonePrefix = "Zone_";

	public const string TitleViewStateChanged = "OnStateChanged";

	public const string EntityOnDeath = "OnDeath";

	public const string ShrineUseCounts = "_useCountByPlayer";

	public const string ShrineOnUse = "OnUse";

	public const string VoteStatusUpdate = "UpdateVoteStatus";

	public const string VoteStartedRpc = "RpcInvokeVoteStarted";

	public const string VoteCompletedRpc = "RpcInvokeVoteCompleted";

	public const string DefaultTestBoss = ForestBoss;

	public const string DefaultHeroicBosses =
		ForestBoss + ", " + DarkCaveBoss + ", " + DespairBoss + ", " + SkyBoss + ", " + SnowMountainBoss + ", " +
		LavaLandBoss;

	public const string AcceleratedTime = nameof(RoomMod_AcceleratedTime);

	public const string ArcticTerritory = nameof(RoomMod_ArcticTerritory);

	public const string BlackRain = nameof(RoomMod_BlackRain);

	public const string DarkCondensationZone = nameof(RoomMod_DarkCondensationZone);

	public const string EngulfedInFlame = nameof(RoomMod_EngulfedInFlame);

	public const string FireDevil = nameof(RoomMod_FireDevil);

	public const string GazeOfErebos = nameof(RoomMod_GazeOfErebos);

	public const string GoldEverywhere = nameof(RoomMod_GoldEverywhere);

	public const string GravityTraining = nameof(RoomMod_GravityTraining);

	public const string InkStrikeWarning = nameof(RoomMod_InkStrikeWarning);

	public const string InversionSigil = nameof(RoomMod_InversionSigil);

	public const string LeafPuppies = nameof(RoomMod_LeafPuppies);

	public const string LingeringAuraOfGuidance = nameof(RoomMod_LingeringAuraOfGuidance);

	public const string MeteoricLife = nameof(RoomMod_MeteoricLife);

	public const string PureDream = nameof(RoomMod_PureDream);

	public const string PureDreamSpawnPosition = "CalSpawnPosition";

	public const string RiskOfMeteors = nameof(RoomMod_RiskOfMeteors);

	public const string StarCookie = nameof(RoomMod_StarCookie);

	public const string SymbioteHabitat = nameof(RoomMod_SymbioteHabitat);

	public const string ToxicArea = nameof(RoomMod_ToxicArea);

	public const string UnstableRatSwarm = nameof(RoomMod_UnstableRatSwarm);

	public const string UnstableVeilOfTime = nameof(RoomMod_UnstableVeilOfTime);

	public const string VeilOfDark = nameof(RoomMod_VeilOfDark);

	public const string WarpingField = nameof(RoomMod_WarpingField);

	public const string DefaultEnvironments =
		AcceleratedTime + ", " + ArcticTerritory + ", " + BlackRain + ", " + DarkCondensationZone + ", " +
		EngulfedInFlame + ", " + FireDevil + ", " + GazeOfErebos + ", " + GoldEverywhere + ", " + GravityTraining +
		", " + InkStrikeWarning + ", " + InversionSigil + ", " + LeafPuppies + ", " + MeteoricLife + ", " +
		PureDream + ", " + RiskOfMeteors + ", " + SymbioteHabitat + ", " + ToxicArea + ", " + UnstableRatSwarm +
		", " + UnstableVeilOfTime + ", " + VeilOfDark + ", " + WarpingField;

	public const string MildStrength = "Mild";

	public const string PotentStrength = "Potent";

	public const string PowerfulStrength = "Powerful";

	public const string ActorOnCreate = "OnCreate";

	public const string ActorOnDestroy = "OnDestroyActor";
}
