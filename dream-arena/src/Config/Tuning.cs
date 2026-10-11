namespace DreamArena.Config;

internal static class Tuning
{
	public const float NoMonsters = 0f;

	public const float NormalMonsters = 1f;

	public const int TeamCount = 2;

	public const float StagingRetrySeconds = 0.25f;

	public const float ArenaRingShare = 0.75f;

	public const float MinRingRadius = 1f;

	public const float HalfCircleDegrees = 180f;

	public const float Half = 0.5f;

	public const int RemnantKinds = 2;

	public const float PveSweepSeconds = 1f;

	public const float RemnantOpeningDegrees = 90f;

	public static readonly float[] RemnantRingRadii = { 7f, 6.5f, 6f, 5.5f, 5f };

	public static readonly float[] RemnantRingRotations = { 0f, 15f, -15f, 30f, -30f, 45f, -45f };

	public const float GroundSlotTolerance = 0.65f;

	public const float MinOpeningDirectionSqr = 0.01f;

	public const float RemnantRetrySeconds = 1f;

	public const int RemnantMaxAttempts = 5;

	public const int MinOfferLevel = 1;

	public static readonly Rarity[] RemnantRarities =
	{
		Rarity.Common,
		Rarity.Rare,
		Rarity.Epic,
		Rarity.Legendary,
		Rarity.Unique,
	};

	public static readonly float[] DummyDistances = { 11f, 12.5f, 10f, 14f };

	public static readonly float[] DummyAngles = { 40f, -40f, 60f, -60f, 25f, -25f, 0f };

	public const float DummySpawnRetrySeconds = 1f;

	public const float DummyMaintainSeconds = 0.2f;

	public const float DummyHomeTolerance = 0.5f;

	public const float DummyResetSeconds = 3f;

	public const float HealthChangeEpsilon = 0.01f;

	public const int MinDummyLevel = 1;

	public const float HeroCheckSeconds = 0.5f;

	public const float NormalHealthMultiplier = 1f;

	public const int MemoryFloorOff = 1;

	public const int PlayerLevelOff = 0;

	public const int MinRevivePercent = 1;

	public const int MaxRevivePercent = 100;

	public const float FullHealthRatio = 1f;

	public static readonly HeroSkillLocation[] MemorySlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.W,
		HeroSkillLocation.E,
		HeroSkillLocation.R,
	};

	public static readonly HeroSkillLocation[] AllSkillSlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.W,
		HeroSkillLocation.E,
		HeroSkillLocation.R,
		HeroSkillLocation.Identity,
		HeroSkillLocation.Movement,
	};

	public static readonly HeroSkillLocation[] LoadoutMemorySlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.R,
	};

	public const int StarterMemoryLevel = 1;
}
