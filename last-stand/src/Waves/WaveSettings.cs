using System.Collections.Generic;

namespace LastStand.Waves;

internal sealed class WaveSettings
{
	public double IntermissionSeconds { get; set; }
	public int RegularBase { get; set; }
	public int RegularGrowthPerWave { get; set; }
	public double ExtraPlayerShare { get; set; }
	public Cadence Surges { get; set; }
	public double SurgeMultiplier { get; set; }
	public int MaxAliveBase { get; set; }
	public int MaxAlivePerExtraPlayer { get; set; }
	public double SpawnIntervalSeconds { get; set; }
	public int SpawnsPerInterval { get; set; }
	public float MinSpawnDistanceFromHeroes { get; set; }

	public int StartingAmbientLevel { get; set; }
	public int WavesPerAmbientLevel { get; set; }
	public double HealthBonusPerWave { get; set; }
	public double DamageBonusPerWave { get; set; }

	public Cadence MiniBosses { get; set; }
	public int MiniBossOccurrencesPerExtra { get; set; }
	public int MaxMiniBosses { get; set; }
	public double MiniBossHealthMultiplier { get; set; }
	public double MiniBossDamageMultiplier { get; set; }

	public Cadence Pain { get; set; }
	public int PainOccurrencesPerExtra { get; set; }
	public int MaxPain { get; set; }

	public Cadence HeroicBosses { get; set; }
	public int HeroicBossStepWaves { get; set; }
	public int MaxHeroicBosses { get; set; }
	public double HeroicBossEntranceDelaySeconds { get; set; }
	public double HeroicBossHealthMultiplier { get; set; }
	public double HeroicBossDamageMultiplier { get; set; }
	public double HeroicBossAbilityHaste { get; set; }
	public double HeroicBossHastePerBossWave { get; set; }
	public IReadOnlyList<string> HeroicBossTypes { get; set; }

	public Cadence ForeignTypes { get; set; }
	public int ForeignTypesPerUnlock { get; set; }

	public int HunterIntervalWaves { get; set; }
	public int MaxHunters { get; set; }
	public int HuntedZoneIntervalWaves { get; set; }
	public int MaxHuntLevel { get; set; }

	public Cadence Mirage { get; set; }
	public double MirageChance { get; set; }
}
