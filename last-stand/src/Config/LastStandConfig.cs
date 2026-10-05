using LastStand.Game;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Config;

public class LastStandConfig : ModConfig
{
	[HideInInspector]
	public int schemaVersion = ConfigSchema.Current;

	[Header(ConfigText.ArenaHeader)]
	[LabelText(ConfigText.ArenaRoomLabel)]
	[Description(ConfigText.ArenaRoomDescription)]
	public string arenaRoom = GameNames.DefaultArenaRoom;

	[LabelText(ConfigText.ShowWaveInHudLabel)]
	[Description(ConfigText.ShowWaveInHudDescription)]
	public bool showWaveInHud = true;

	[Header(ConfigText.WavesHeader)]
	[LabelText(ConfigText.FirstWaveDelayLabel)]
	[Description(ConfigText.FirstWaveDelayDescription)]
	public float firstWaveDelaySeconds = 15f;

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
	public int spawnsPerInterval = 2;

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
	public int maxHuntLevel = 3;

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

	[Header(ConfigText.LootHeader)]
	[LabelText(ConfigText.LootDespawnLabel)]
	[Description(ConfigText.LootDespawnDescription)]
	public float lootDespawnSeconds = 30f;

	[LabelText(ConfigText.MobItemsPerPlayerLabel)]
	[Description(ConfigText.MobItemsPerPlayerDescription)]
	public int mobItemsPerPlayerPerWave = 1;

	[LabelText(ConfigText.GuaranteedDropLabel)]
	[Description(ConfigText.GuaranteedDropDescription)]
	public bool guaranteedDropPerWave = false;

	[LabelText(ConfigText.RewardGrowthLabel)]
	[Description(ConfigText.RewardGrowthDescription)]
	public float rewardGrowthPercentPerWave = 10f;

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

	[LabelText(ConfigText.MemoryShareLabel)]
	[Description(ConfigText.MemoryShareDescription)]
	public float memorySharePercent = 50f;

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

	[LabelText(ConfigText.ChaosLegendaryLabel)]
	[Description(ConfigText.ChaosRarityDescription)]
	public float chaosLegendaryPercent = 5f;

	[LabelText(ConfigText.ChaosEpicLabel)]
	[Description(ConfigText.ChaosRarityDescription)]
	public float chaosEpicPercent = 15f;

	[LabelText(ConfigText.ChaosRareLabel)]
	[Description(ConfigText.ChaosRarityDescription)]
	public float chaosRarePercent = 35f;

	[LabelText(ConfigText.PainDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float painDropMultiplier = 2f;

	[LabelText(ConfigText.PainDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int painDreamDust = 150;

	[LabelText(ConfigText.PainGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int painGold = 100;
}
