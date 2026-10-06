using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Config;

public class LastStandConfig : ModConfig
{
	[HideInInspector]
	public int schemaVersion = ConfigSchema.Current;

	[Header(ConfigText.ArenaHeader)]
	[PickOne(ChoiceSet.ArenaRooms)]
	[LabelText(ConfigText.ArenaRoomLabel)]
	[Description(ConfigText.ArenaRoomDescription)]
	public string arenaRoom = GameNames.DefaultArenaRoom;

	[LabelText(ConfigText.StripRoomModifiersLabel)]
	[Description(ConfigText.StripRoomModifiersDescription)]
	public bool stripRoomModifiers = true;

	[LabelText(ConfigText.ShowWaveInHudLabel)]
	[Description(ConfigText.ShowWaveInHudDescription)]
	public bool showWaveInHud = true;

	[LabelText(ConfigText.RandomBossMusicLabel)]
	[Description(ConfigText.RandomBossMusicDescription)]
	public bool randomBossMusic = true;

	[Header(ConfigText.WavesHeader)]
	[LabelText(ConfigText.FirstWaveDelayLabel)]
	[Description(ConfigText.FirstWaveDelayDescription)]
	public float firstWaveDelaySeconds = 10f;

	[LabelText(ConfigText.IntermissionLabel)]
	[Description(ConfigText.IntermissionDescription)]
	public float intermissionSeconds = 5f;

	[LabelText(ConfigText.RegularBaseLabel)]
	[Description(ConfigText.RegularBaseDescription)]
	public int regularBase = 6;

	[LabelText(ConfigText.RegularGrowthLabel)]
	[Description(ConfigText.RegularGrowthDescription)]
	public int regularGrowthPerWave = 2;

	[LabelText(ConfigText.ExtraPlayerShareLabel)]
	[Description(ConfigText.ExtraPlayerShareDescription)]
	public float extraPlayerShare = 0.5f;

	[LabelText(ConfigText.SurgeIntervalLabel)]
	[Description(ConfigText.SurgeIntervalDescription)]
	public int surgeIntervalWaves = 5;

	[LabelText(ConfigText.SurgeMultiplierLabel)]
	[Description(ConfigText.SurgeMultiplierDescription)]
	public float surgeMonsterMultiplier = 2f;

	[LabelText(ConfigText.MaxAliveBaseLabel)]
	[Description(ConfigText.MaxAliveBaseDescription)]
	public int maxAliveBase = 200;

	[LabelText(ConfigText.MaxAlivePerPlayerLabel)]
	[Description(ConfigText.MaxAlivePerPlayerDescription)]
	public int maxAlivePerExtraPlayer = 0;

	[LabelText(ConfigText.SpawnIntervalLabel)]
	[Description(ConfigText.SpawnIntervalDescription)]
	public float spawnIntervalSeconds = 0.5f;

	[LabelText(ConfigText.SpawnsPerIntervalLabel)]
	[Description(ConfigText.SpawnsPerIntervalDescription)]
	public int spawnsPerInterval = 6;

	[LabelText(ConfigText.MinSpawnDistanceLabel)]
	[Description(ConfigText.MinSpawnDistanceDescription)]
	public float minSpawnDistanceFromHeroes = 8f;

	[Header(ConfigText.ScalingHeader)]
	[LabelText(ConfigText.HealthPerWaveLabel)]
	[Description(ConfigText.HealthPerWaveDescription)]
	public float healthBonusPercentPerWave = 10f;

	[LabelText(ConfigText.DamagePerWaveLabel)]
	[Description(ConfigText.DamagePerWaveDescription)]
	public float damageBonusPercentPerWave = 10f;

	[LabelText(ConfigText.StartingAmbientLevelLabel)]
	[Description(ConfigText.StartingAmbientLevelDescription)]
	public int startingAmbientLevel = 1;

	[LabelText(ConfigText.WavesPerAmbientLevelLabel)]
	[Description(ConfigText.WavesPerAmbientLevelDescription)]
	public int wavesPerAmbientLevel = 10;

	[Header(ConfigText.MiniBossHeader)]
	[LabelText(ConfigText.MiniBossFromWaveLabel)]
	[Description(ConfigText.MiniBossFromWaveDescription)]
	public int miniBossFromWave = 10;

	[LabelText(ConfigText.MiniBossIntervalLabel)]
	[Description(ConfigText.MiniBossIntervalDescription)]
	public int miniBossIntervalWaves = 10;

	[LabelText(ConfigText.MiniBossOccurrencesPerExtraLabel)]
	[Description(ConfigText.OccurrencesPerExtraDescription)]
	public int miniBossOccurrencesPerExtra = 2;

	[LabelText(ConfigText.MaxMiniBossesLabel)]
	[Description(ConfigText.MaxMiniBossesDescription)]
	public int maxMiniBossesPerWave = 4;

	[Header(ConfigText.HeroicBossHeader)]
	[LabelText(ConfigText.HeroicBossFromWaveLabel)]
	[Description(ConfigText.HeroicBossFromWaveDescription)]
	public int heroicBossFromWave = 49;

	[LabelText(ConfigText.HeroicBossIntervalLabel)]
	[Description(ConfigText.HeroicBossIntervalDescription)]
	public int heroicBossIntervalWaves = 50;

	[LabelText(ConfigText.HeroicBossStepLabel)]
	[Description(ConfigText.HeroicBossStepDescription)]
	public int heroicBossStepWaves = 100;

	[LabelText(ConfigText.MaxHeroicBossesLabel)]
	[Description(ConfigText.MaxHeroicBossesDescription)]
	public int maxHeroicBossesPerWave = 10;

	[LabelText(ConfigText.HeroicBossDelayLabel)]
	[Description(ConfigText.HeroicBossDelayDescription)]
	public float heroicBossEntranceDelaySeconds = 3f;

	[LabelText(ConfigText.HeroicBossHealthMultiplierLabel)]
	[Description(ConfigText.HeroicBossStatMultiplierDescription)]
	public float heroicBossHealthMultiplier = 10f;

	[LabelText(ConfigText.HeroicBossDamageMultiplierLabel)]
	[Description(ConfigText.HeroicBossStatMultiplierDescription)]
	public float heroicBossDamageMultiplier = 3f;

	[LabelText(ConfigText.HeroicBossAbilityHasteLabel)]
	[Description(ConfigText.HeroicBossAbilityHasteDescription)]
	public float heroicBossAbilityHaste = 200f;

	[PickMany(ChoiceSet.HeroicBosses)]
	[LabelText(ConfigText.HeroicBossTypesLabel)]
	[Description(ConfigText.HeroicBossTypesDescription)]
	public string heroicBossTypes = GameNames.DefaultHeroicBosses;

	[LabelText(ConfigText.HeroicBossChaosLabel)]
	[Description(ConfigText.HeroicBossChaosDescription)]
	public int heroicBossChaosRewards = 5;

	[LabelText(ConfigText.HeroicBossDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float heroicBossDropMultiplier = 2f;

	[LabelText(ConfigText.HeroicBossDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int heroicBossDreamDust = 1000;

	[LabelText(ConfigText.HeroicBossGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int heroicBossGold = 200;

	[Header(ConfigText.PainHeader)]
	[LabelText(ConfigText.PainFromWaveLabel)]
	[Description(ConfigText.PainFromWaveDescription)]
	public int painFromWave = 20;

	[LabelText(ConfigText.PainIntervalLabel)]
	[Description(ConfigText.PainIntervalDescription)]
	public int painIntervalWaves = 20;

	[LabelText(ConfigText.PainOccurrencesPerExtraLabel)]
	[Description(ConfigText.OccurrencesPerExtraDescription)]
	public int painOccurrencesPerExtra = 2;

	[LabelText(ConfigText.MaxPainLabel)]
	[Description(ConfigText.MaxPainDescription)]
	public int maxPainPerWave = 3;

	[LabelText(ConfigText.PainDamageLabel)]
	[Description(ConfigText.PainDamageDescription)]
	public float painDamagePercentPerSecond = 1f;

	[LabelText(ConfigText.PainHealthBonusLabel)]
	[Description(ConfigText.PainHealthBonusDescription)]
	public float painHealthBonusPercent = 20f;

	[LabelText(ConfigText.PainDamageBonusLabel)]
	[Description(ConfigText.PainDamageBonusDescription)]
	public float painDamageBonusPercent = 30f;

	[Header(ConfigText.ForeignHeader)]
	[LabelText(ConfigText.ForeignFromWaveLabel)]
	[Description(ConfigText.ForeignFromWaveDescription)]
	public int foreignTypesFromWave = 10;

	[LabelText(ConfigText.ForeignIntervalLabel)]
	[Description(ConfigText.ForeignIntervalDescription)]
	public int foreignTypesIntervalWaves = 10;

	[LabelText(ConfigText.ForeignPerUnlockLabel)]
	[Description(ConfigText.ForeignPerUnlockDescription)]
	public int foreignTypesPerUnlock = 1;

	[Header(ConfigText.HunterHeader)]
	[LabelText(ConfigText.HunterIntervalLabel)]
	[Description(ConfigText.HunterIntervalDescription)]
	public int hunterIntervalWaves = 5;

	[LabelText(ConfigText.MaxHuntersLabel)]
	[Description(ConfigText.MaxHuntersDescription)]
	public int maxHuntersPerWave = 10;

	[LabelText(ConfigText.HuntedZoneIntervalLabel)]
	[Description(ConfigText.HuntedZoneIntervalDescription)]
	public int huntedZoneIntervalWaves = 10;

	[LabelText(ConfigText.MaxHuntLevelLabel)]
	[Description(ConfigText.MaxHuntLevelDescription)]
	public int maxHuntLevel = 6;

	[Header(ConfigText.MirageHeader)]
	[LabelText(ConfigText.MirageFromWaveLabel)]
	[Description(ConfigText.MirageFromWaveDescription)]
	public int mirageFromWave = 30;

	[LabelText(ConfigText.MirageIntervalLabel)]
	[Description(ConfigText.MirageIntervalDescription)]
	public int mirageIntervalWaves = 10;

	[LabelText(ConfigText.MirageChanceLabel)]
	[Description(ConfigText.MirageChanceDescription)]
	public float mirageChancePercent = 20f;

	[Header(ConfigText.ItemDropsHeader)]
	[LabelText(ConfigText.MobItemsPerPlayerLabel)]
	[Description(ConfigText.MobItemsPerPlayerDescription)]
	public int mobItemsPerPlayerPerWave = 1;

	[LabelText(ConfigText.GuaranteedDropLabel)]
	[Description(ConfigText.GuaranteedDropDescription)]
	public bool guaranteedDropPerWave = false;

	[LabelText(ConfigText.CommonDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float commonDropPercent = 25f;

	[LabelText(ConfigText.RareDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float rareDropPercent = 10f;

	[LabelText(ConfigText.EpicDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float epicDropPercent = 4f;

	[LabelText(ConfigText.LegendaryDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float legendaryDropPercent = 1f;

	[LabelText(ConfigText.ItemTargetWaveLabel)]
	[Description(ConfigText.ItemTargetWaveDescription)]
	public int itemTargetWave = 100;

	[LabelText(ConfigText.CommonAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float commonDropPercentAtTarget = 0f;

	[LabelText(ConfigText.RareAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float rareDropPercentAtTarget = 49.8f;

	[LabelText(ConfigText.EpicAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float epicDropPercentAtTarget = 35.2f;

	[LabelText(ConfigText.LegendaryAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float legendaryDropPercentAtTarget = 15f;

	[LabelText(ConfigText.MemoryShareLabel)]
	[Description(ConfigText.MemoryShareDescription)]
	public float memorySharePercent = 50f;

	[Header(ConfigText.EnemyRewardsHeader)]
	[LabelText(ConfigText.RewardGrowthLabel)]
	[Description(ConfigText.RewardGrowthDescription)]
	public float rewardGrowthPercentPerWave = 10f;

	[LabelText(ConfigText.RegularDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float regularDropMultiplier = 1f;

	[LabelText(ConfigText.RegularDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int regularDreamDust = 2;

	[LabelText(ConfigText.RegularGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int regularGold = 0;

	[LabelText(ConfigText.MiniBossDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float miniBossDropMultiplier = 1.5f;

	[LabelText(ConfigText.MiniBossDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int miniBossDreamDust = 50;

	[LabelText(ConfigText.MiniBossGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int miniBossGold = 40;

	[LabelText(ConfigText.PainDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float painDropMultiplier = 2f;

	[LabelText(ConfigText.PainDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int painDreamDust = 150;

	[LabelText(ConfigText.PainGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int painGold = 100;


	[Header(ConfigText.ChaosRarityHeader)]
	[LabelText(ConfigText.ChaosLegendaryLabel)]
	[Description(ConfigText.ChaosRarityDescription)]
	public float chaosLegendaryPercent = 5f;

	[LabelText(ConfigText.ChaosEpicLabel)]
	[Description(ConfigText.ChaosRarityDescription)]
	public float chaosEpicPercent = 15f;

	[LabelText(ConfigText.ChaosRareLabel)]
	[Description(ConfigText.ChaosRarityDescription)]
	public float chaosRarePercent = 35f;

	[LabelText(ConfigText.ChaosTargetWaveLabel)]
	[Description(ConfigText.ChaosTargetWaveDescription)]
	public int chaosTargetWave = 50;

	[LabelText(ConfigText.ChaosLegendaryAtTargetLabel)]
	[Description(ConfigText.ChaosAtTargetDescription)]
	public float chaosLegendaryPercentAtTarget = 17.5f;

	[LabelText(ConfigText.ChaosEpicAtTargetLabel)]
	[Description(ConfigText.ChaosAtTargetDescription)]
	public float chaosEpicPercentAtTarget = 35.2f;

	[LabelText(ConfigText.ChaosRareAtTargetLabel)]
	[Description(ConfigText.ChaosAtTargetDescription)]
	public float chaosRarePercentAtTarget = 47.3f;
}
