using System.Collections.Generic;

namespace LastStand.Waves;

internal sealed class WaveSettings
{
	public double IntermissionSeconds { get; set; }

	public double BossIntermissionSeconds { get; set; }

	public bool CurseToStart { get; set; }

	public double CurseStartDelaySeconds { get; set; }

	public int RegularBase { get; set; }

	public int RegularGrowthPerWave { get; set; }


	public double ExtraPlayerShare { get; set; }

	public Cadence Surges { get; set; }

	public double SurgeMultiplier { get; set; }

	public MergeRule Merge { get; set; }

	public int MaxAliveBase { get; set; }

	public int MaxAlivePerExtraPlayer { get; set; }

	public double SpawnIntervalSeconds { get; set; }

	public int SpawnsPerInterval { get; set; }

	public float MinSpawnDistanceFromHeroes { get; set; }

	public int StartingAmbientLevel { get; set; }

	public int WavesPerAmbientLevel { get; set; }

	public WorldRule World { get; set; }

	public HeroicFastKillRule HeroicFastKill { get; set; }

	public Cadence MiniBosses { get; set; }

	public int MiniBossOccurrencesPerExtra { get; set; }

	public int MaxMiniBosses { get; set; }

	public Cadence Pain { get; set; }

	public int PainOccurrencesPerExtra { get; set; }

	public int MaxPain { get; set; }

	public ShiftingCadence HeroicBosses { get; set; }

	public int HeroicBossesPerBossWave { get; set; }

	public int MaxHeroicBosses { get; set; }

	public int HeroicBossWindowFromWave { get; set; }

	public double HeroicBossWindowSeconds { get; set; }

	public double HeroicBossEntranceDelaySeconds { get; set; }

	public double HeroicBossAbilityHaste { get; set; }

	public double HeroicBossHastePerBossWave { get; set; }

	public IReadOnlyList<string> HeroicBossTypes { get; set; }

	public IReadOnlyList<string> AllHeroicBossTypes { get; set; }

	public Cadence ForeignTypes { get; set; }

	public int ForeignTypesPerUnlock { get; set; }

	public int HunterFromWave { get; set; }

	public int AllHuntersFromWave { get; set; }

	public int HuntLevelStepWaves { get; set; }

	public int MaxHuntLevel { get; set; }

	public Cadence Mirage { get; set; }

	public double MirageChance { get; set; }

	public int MirageRampFromWave { get; set; }

	public int AllMirageFromWave { get; set; }

	public int FriendlyFireFromWave { get; set; }

	public double FriendlyFireChance { get; set; }

	public HatredStrengthType DarkUrgeStrength { get; set; }

	public EnvironmentRule Environment { get; set; }

	public IReadOnlyList<string> EnvironmentTypes { get; set; }
}
