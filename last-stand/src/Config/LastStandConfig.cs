using LastStand.Game;
using LastStand.Text;
using LastStand.UI;
using UnityEngine;

namespace LastStand.Config;

public class LastStandConfig : ModConfig
{
	[HideInInspector]
	public int schemaVersion = ConfigSchema.Current;

	[Page(SettingsPage.General)]
	[Header(ConfigText.RunHeader)]
	[PickOne(ChoiceSet.ArenaRooms)]
	[LabelText(ConfigText.ArenaRoomLabel)]
	[Description(ConfigText.ArenaRoomDescription)]
	public string arenaRoom = GameNames.DefaultArenaRoom;

	[LabelText(ConfigText.StartingWorldLabel)]
	[Description(ConfigText.StartingWorldDescription)]
	public int startingWorld = 1;

	[LabelText(ConfigText.StartingGoldLabel)]
	[Description(ConfigText.StartingGoldDescription)]
	public int startingGold = 100;

	[LabelText(ConfigText.StartingDreamDustLabel)]
	[Description(ConfigText.StartingDreamDustDescription)]
	public int startingDreamDust = 10;

	[Header(ConfigText.ArenaHeader)]
	[LabelText(ConfigText.StripRoomModifiersLabel)]
	[Description(ConfigText.StripRoomModifiersDescription)]
	public bool stripRoomModifiers = true;

	[LabelText(ConfigText.ShowWaveInQuestTrackerLabel)]
	[Description(ConfigText.ShowWaveInQuestTrackerDescription)]
	public bool showWaveInQuestTracker = true;

	[Header(ConfigText.DeveloperHeader)]
	[LabelText(ConfigText.CheatCommandsLabel)]
	[Description(ConfigText.CheatCommandsDescription)]
	public bool cheatCommands;

	[Page(SettingsPage.Shrines)]
	[Header(ConfigText.ShrinesHeader)]
	[LabelText(ConfigText.DreamWellLabel)]
	[Description(ConfigText.DreamWellDescription)]
	public bool dreamWell = true;

	[LabelText(ConfigText.AltarOfCleansingLabel)]
	[Description(ConfigText.AltarOfCleansingDescription)]
	public bool altarOfCleansing = true;

	[LabelText(ConfigText.ShrineOfAscensionLabel)]
	[Description(ConfigText.ShrineOfAscensionDescription)]
	public bool shrineOfAscension = true;

	[LabelText(ConfigText.AscensionResetEveryWavesLabel)]
	[Description(ConfigText.AscensionResetEveryWavesDescription)]
	public int ascensionResetEveryWaves = 1;

	[LabelText(ConfigText.MawOfDoomLabel)]
	[Description(ConfigText.MawOfDoomDescription)]
	public bool mawOfDoom = true;

	[LabelText(ConfigText.ShrineOfDestinyLabel)]
	[Description(ConfigText.ShrineOfDestinyDescription)]
	public bool shrineOfDestiny = true;

	[LabelText(ConfigText.DestinyClaimsPerWaveLabel)]
	[Description(ConfigText.DestinyClaimsPerWaveDescription)]
	public int destinyClaimsPerWave = 2;

	[LabelText(ConfigText.ReminiscenceOfLuckLabel)]
	[Description(ConfigText.ReminiscenceOfLuckDescription)]
	public bool reminiscenceOfLuck = true;

	[LabelText(ConfigText.FreeGoldShrinesLabel)]
	[Description(ConfigText.FreeGoldShrinesDescription)]
	public bool freeGoldShrines;

	[LabelText(ConfigText.GoldPriceGrowthPercentLabel)]
	[Description(ConfigText.GoldPriceGrowthPercentDescription)]
	public float goldPriceGrowthPercent = 5f;

	[LabelText(ConfigText.GoldPriceGrowthEveryWavesLabel)]
	[Description(ConfigText.GoldPriceGrowthEveryWavesDescription)]
	public int goldPriceGrowthEveryWaves = 1;

	[LabelText(ConfigText.GoldPriceGrowthUntilWaveLabel)]
	[Description(ConfigText.GoldPriceGrowthUntilWaveDescription)]
	public int goldPriceGrowthUntilWave = 100;

	[Header(ConfigText.HatredHeader)]
	[LabelText(ConfigText.HatredRemnantLabel)]
	[Description(ConfigText.HatredRemnantDescription)]
	public bool hatredRemnant = true;

	[LabelText(ConfigText.HatredDreamDustPerWaveLabel)]
	[Description(ConfigText.HatredDreamDustPerWaveDescription)]
	public int hatredDreamDustPerWave = 50;

	[LabelText(ConfigText.HatredShardGoldLabel)]
	[Description(ConfigText.HatredShardGoldDescription)]
	public int hatredShardGold = 500;

	[LabelText(ConfigText.CurseMildWavesLabel)]
	[Description(ConfigText.CurseWavesDescription)]
	public float curseMildWavesOfKills = 1f;

	[LabelText(ConfigText.CursePotentWavesLabel)]
	[Description(ConfigText.CurseWavesDescription)]
	public float cursePotentWavesOfKills = 1.25f;

	[LabelText(ConfigText.CursePowerfulWavesLabel)]
	[Description(ConfigText.CurseWavesDescription)]
	public float cursePowerfulWavesOfKills = 1.75f;

	[Page(SettingsPage.Waves)]
	[Header(ConfigText.TimingHeader)]
	[LabelText(ConfigText.FirstWaveDelayLabel)]
	[Description(ConfigText.FirstWaveDelayDescription)]
	public float firstWaveDelaySeconds = 10f;

	[LabelText(ConfigText.IntermissionLabel)]
	[Description(ConfigText.IntermissionDescription)]
	public float intermissionSeconds = 20f;

	[LabelText(ConfigText.BossIntermissionLabel)]
	[Description(ConfigText.BossIntermissionDescription)]
	public float bossIntermissionSeconds = 40f;

	[LabelText(ConfigText.CurseToStartLabel)]
	[Description(ConfigText.CurseToStartDescription)]
	public bool curseToStartWaves;

	[LabelText(ConfigText.CurseStartDelayLabel)]
	[Description(ConfigText.CurseStartDelayDescription)]
	public float curseStartDelaySeconds = 3f;

	[Header(ConfigText.WaveSizeHeader)]
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

	[LabelText(ConfigText.MergedWaveSizeLabel)]
	[Description(ConfigText.MergedWaveSizeDescription)]
	public int mergedWaveSize = 100;

	[LabelText(ConfigText.MergedSurgeMiniBossesLabel)]
	[Description(ConfigText.MergedSurgeMiniBossesDescription)]
	public int mergedSurgeMiniBosses = 2;

	[LabelText(ConfigText.MergedSurgePainLabel)]
	[Description(ConfigText.MergedSurgePainDescription)]
	public int mergedSurgePain = 2;

	[LabelText(ConfigText.MergedMiniBossHealthLabel)]
	[Description(ConfigText.MergedMiniBossHealthDescription)]
	public float mergedMiniBossHealthMultiple = 5f;

	[LabelText(ConfigText.MergedHeroicBossHealthLabel)]
	[Description(ConfigText.MergedHeroicBossHealthDescription)]
	public float mergedHeroicBossHealthMultiple = 20f;

	[Header(ConfigText.SpawningHeader)]
	[LabelText(ConfigText.MaxAliveBaseLabel)]
	[Description(ConfigText.MaxAliveBaseDescription)]
	public int maxAliveBase = 35;

	[LabelText(ConfigText.MaxAlivePerPlayerLabel)]
	[Description(ConfigText.MaxAlivePerPlayerDescription)]
	public int maxAlivePerExtraPlayer;

	[LabelText(ConfigText.SpawnIntervalLabel)]
	[Description(ConfigText.SpawnIntervalDescription)]
	public float spawnIntervalSeconds = 0.5f;

	[LabelText(ConfigText.SpawnsPerIntervalLabel)]
	[Description(ConfigText.SpawnsPerIntervalDescription)]
	public int spawnsPerInterval = 6;

	[LabelText(ConfigText.MinSpawnDistanceLabel)]
	[Description(ConfigText.MinSpawnDistanceDescription)]
	public float minSpawnDistanceFromHeroes = 8f;

	[Page(SettingsPage.Monsters)]
	[Header(ConfigText.ScalingHeader)]
	[LabelText(ConfigText.HealthPerWaveLabel)]
	[Description(ConfigText.HealthPerWaveDescription)]
	public float healthBonusPercentPerWave = 10f;

	[LabelText(ConfigText.DamagePerWaveLabel)]
	[Description(ConfigText.DamagePerWaveDescription)]
	public float damageBonusPercentPerWave = 10f;

	[LabelText(ConfigText.LateScalingFromWaveLabel)]
	[Description(ConfigText.LateScalingFromWaveDescription)]
	public int lateScalingFromWave = 150;

	[LabelText(ConfigText.LateHealthPerWaveLabel)]
	[Description(ConfigText.LateHealthPerWaveDescription)]
	public float lateHealthBonusPercentPerWave = 50f;

	[LabelText(ConfigText.LateDamagePerWaveLabel)]
	[Description(ConfigText.LateDamagePerWaveDescription)]
	public float lateDamageBonusPercentPerWave = 50f;

	[LabelText(ConfigText.FastClearKillPercentLabel)]
	[Description(ConfigText.FastClearKillPercentDescription)]
	public float fastClearKillPercent = 80f;

	[LabelText(ConfigText.FastClearFromWaveLabel)]
	[Description(ConfigText.FastClearFromWaveDescription)]
	public int fastClearFromWave = 30;

	[LabelText(ConfigText.FastClearKillSecondsLabel)]
	[Description(ConfigText.FastClearKillSecondsDescription)]
	public float fastClearKillSeconds = 0.5f;

	[LabelText(ConfigText.FastClearStatMultiplierLabel)]
	[Description(ConfigText.FastClearStatMultiplierDescription)]
	public float fastClearStatMultiplier = 2f;

	[LabelText(ConfigText.FastClearHeroicBossesLabel)]
	[Description(ConfigText.FastClearHeroicBossesDescription)]
	public bool fastClearAffectsHeroicBosses = true;

	[LabelText(ConfigText.FastClearHeroicBossPercentLabel)]
	[Description(ConfigText.FastClearHeroicBossPercentDescription)]
	public float fastClearHeroicBossPercent = 50f;

	[LabelText(ConfigText.StartingAmbientLevelLabel)]
	[Description(ConfigText.StartingAmbientLevelDescription)]
	public int startingAmbientLevel = 1;

	[LabelText(ConfigText.WavesPerAmbientLevelLabel)]
	[Description(ConfigText.WavesPerAmbientLevelDescription)]
	public int wavesPerAmbientLevel = 4;

	[Header(ConfigText.ForeignHeader)]
	[LabelText(ConfigText.ForeignFromWaveLabel)]
	[Description(ConfigText.ForeignFromWaveDescription)]
	public int foreignTypesFromWave = 10;

	[LabelText(ConfigText.ForeignIntervalLabel)]
	[Description(ConfigText.ForeignIntervalDescription)]
	public int foreignTypesIntervalWaves = 5;

	[LabelText(ConfigText.ForeignPerUnlockLabel)]
	[Description(ConfigText.ForeignPerUnlockDescription)]
	public int foreignTypesPerUnlock = 3;

	[Header(ConfigText.HunterHeader)]
	[LabelText(ConfigText.HunterFromWaveLabel)]
	[Description(ConfigText.HunterFromWaveDescription)]
	public int hunterFromWave = 101;

	[LabelText(ConfigText.AllHuntersFromWaveLabel)]
	[Description(ConfigText.AllHuntersFromWaveDescription)]
	public int allHuntersFromWave = 130;

	[LabelText(ConfigText.HuntLevelStepLabel)]
	[Description(ConfigText.HuntLevelStepDescription)]
	public int huntLevelStepWaves = 10;

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

	[LabelText(ConfigText.MirageRampFromWaveLabel)]
	[Description(ConfigText.MirageRampFromWaveDescription)]
	public int mirageRampFromWave = 130;

	[LabelText(ConfigText.AllMirageFromWaveLabel)]
	[Description(ConfigText.AllMirageFromWaveDescription)]
	public int allMirageFromWave = 149;

	[Page(SettingsPage.Bosses)]
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
	public int maxMiniBossesPerWave = 10;

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

	[Header(ConfigText.HeroicBossHeader)]
	[LabelText(ConfigText.HeroicBossFromWaveLabel)]
	[Description(ConfigText.HeroicBossFromWaveDescription)]
	public int heroicBossFromWave = 49;

	[LabelText(ConfigText.HeroicBossIntervalLabel)]
	[Description(ConfigText.HeroicBossIntervalDescription)]
	public int heroicBossIntervalWaves = 50;

	[LabelText(ConfigText.HeroicBossFasterFromWaveLabel)]
	[Description(ConfigText.HeroicBossFasterFromWaveDescription)]
	public int heroicBossFasterFromWave = 100;

	[LabelText(ConfigText.HeroicBossLateIntervalLabel)]
	[Description(ConfigText.HeroicBossLateIntervalDescription)]
	public int heroicBossLateIntervalWaves = 25;

	[LabelText(ConfigText.HeroicBossStepLabel)]
	[Description(ConfigText.HeroicBossStepDescription)]
	public int heroicBossStepWaves = 50;

	[LabelText(ConfigText.MaxHeroicBossesLabel)]
	[Description(ConfigText.MaxHeroicBossesDescription)]
	public int maxHeroicBossesPerWave = 10;

	[LabelText(ConfigText.HeroicBossDelayLabel)]
	[Description(ConfigText.HeroicBossDelayDescription)]
	public float heroicBossEntranceDelaySeconds = 3f;

	[LabelText(ConfigText.HeroicBossAbilityHasteLabel)]
	[Description(ConfigText.HeroicBossAbilityHasteDescription)]
	public float heroicBossAbilityHaste = 200f;

	[LabelText(ConfigText.HeroicBossHasteGrowthLabel)]
	[Description(ConfigText.HeroicBossHasteGrowthDescription)]
	public float heroicBossAbilityHastePerBossWave = 50f;

	[LabelText(ConfigText.HeroicBossHealthMultiplierLabel)]
	[Description(ConfigText.HeroicBossHealthMultiplierDescription)]
	public float heroicBossHealthMultiplier = 5f;

	[LabelText(ConfigText.HeroicBossDamageMultiplierLabel)]
	[Description(ConfigText.HeroicBossDamageMultiplierDescription)]
	public float heroicBossDamageMultiplier = 8f;

	[PickMany(ChoiceSet.HeroicBosses)]
	[LabelText(ConfigText.HeroicBossTypesLabel)]
	[Description(ConfigText.HeroicBossTypesDescription)]
	public string heroicBossTypes = GameNames.DefaultHeroicBosses;

	[LabelText(ConfigText.HeroicBossChaosLabel)]
	[Description(ConfigText.HeroicBossChaosDescription)]
	public int heroicBossChaosRewards = 3;

	[LabelText(ConfigText.HeroicBossStardustLabel)]
	[Description(ConfigText.HeroicBossStardustDescription)]
	public bool heroicBossStardust = true;

	[LabelText(ConfigText.HeroicBossDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float heroicBossDropMultiplier = 2f;

	[LabelText(ConfigText.HeroicBossDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int heroicBossDreamDust = 1000;

	[LabelText(ConfigText.HeroicBossGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int heroicBossGold = 200;

	[Page(SettingsPage.Events)]
	[Header(ConfigText.EnvironmentHeader)]
	[LabelText(ConfigText.EnvironmentFromWaveLabel)]
	[Description(ConfigText.EnvironmentFromWaveDescription)]
	public int environmentFromWave = 6;

	[LabelText(ConfigText.EnvironmentIntervalLabel)]
	[Description(ConfigText.EnvironmentIntervalDescription)]
	public int environmentIntervalWaves = 10;

	[LabelText(ConfigText.EnvironmentUntilWaveLabel)]
	[Description(ConfigText.EnvironmentUntilWaveDescription)]
	public int environmentUntilWave;

	[PickMany(ChoiceSet.Environments)]
	[LabelText(ConfigText.EnvironmentTypesLabel)]
	[Description(ConfigText.EnvironmentTypesDescription)]
	public string environmentTypes = GameNames.DefaultEnvironments;

	[Header(ConfigText.FriendlyFireHeader)]
	[LabelText(ConfigText.FriendlyFireFromWaveLabel)]
	[Description(ConfigText.FriendlyFireFromWaveDescription)]
	public int friendlyFireFromWave = 176;

	[LabelText(ConfigText.FriendlyFireChanceLabel)]
	[Description(ConfigText.FriendlyFireChanceDescription)]
	public float friendlyFireChancePercent = 20f;

	[PickOne(ChoiceSet.CurseStrengths)]
	[LabelText(ConfigText.DarkUrgeStrengthLabel)]
	[Description(ConfigText.DarkUrgeStrengthDescription)]
	public string darkUrgeStrength = GameNames.PotentStrength;

	[Page(SettingsPage.Rewards)]
	[Header(ConfigText.ItemDropsHeader)]
	[LabelText(ConfigText.MonsterItemDropsLabel)]
	[Description(ConfigText.MonsterItemDropsDescription)]
	public bool monsterItemDrops;

	[LabelText(ConfigText.MobItemsPerPlayerLabel)]
	[Description(ConfigText.MobItemsPerPlayerDescription)]
	public int mobItemsPerPlayerPerWave = 1;

	[LabelText(ConfigText.GuaranteedDropLabel)]
	[Description(ConfigText.GuaranteedDropDescription)]
	public bool guaranteedDropPerWave;

	[LabelText(ConfigText.FreeLeftoverItemsLabel)]
	[Description(ConfigText.FreeLeftoverItemsDescription)]
	public bool freeLeftoverItemsAtWaveEnd = true;

	[LabelText(ConfigText.CommonDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float commonDropPercent = 50f;

	[LabelText(ConfigText.RareDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float rareDropPercent = 20f;

	[LabelText(ConfigText.EpicDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float epicDropPercent = 8f;

	[LabelText(ConfigText.LegendaryDropLabel)]
	[Description(ConfigText.RarityDropDescription)]
	public float legendaryDropPercent = 2f;

	[LabelText(ConfigText.ItemTargetWaveLabel)]
	[Description(ConfigText.ItemTargetWaveDescription)]
	public int itemTargetWave = 100;

	[LabelText(ConfigText.CommonAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float commonDropPercentAtTarget = 25f;

	[LabelText(ConfigText.RareAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float rareDropPercentAtTarget = 25f;

	[LabelText(ConfigText.EpicAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float epicDropPercentAtTarget = 25f;

	[LabelText(ConfigText.LegendaryAtTargetLabel)]
	[Description(ConfigText.ItemAtTargetDescription)]
	public float legendaryDropPercentAtTarget = 25f;

	[LabelText(ConfigText.MemoryShareLabel)]
	[Description(ConfigText.MemoryShareDescription)]
	public float memorySharePercent = 50f;

	[LabelText(ConfigText.ItemRarityFloorLabel)]
	[Description(ConfigText.ItemRarityFloorDescription)]
	public float itemRarityFloorPercent = 25f;

	[Header(ConfigText.EnemyRewardsHeader)]
	[LabelText(ConfigText.MaxKillGoldLabel)]
	[Description(ConfigText.MaxKillGoldDescription)]
	public int maxKillGoldPerPlayer;

	[LabelText(ConfigText.RewardGrowthLabel)]
	[Description(ConfigText.RewardGrowthDescription)]
	public float rewardGrowthPercentPerWave = 1f;

	[LabelText(ConfigText.RegularDropMultiplierLabel)]
	[Description(ConfigText.DropMultiplierDescription)]
	public float regularDropMultiplier = 1f;

	[LabelText(ConfigText.RegularDreamDustLabel)]
	[Description(ConfigText.DreamDustDescription)]
	public int regularDreamDust = 2;

	[LabelText(ConfigText.RegularGoldLabel)]
	[Description(ConfigText.GoldDescription)]
	public int regularGold = 3;

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

	public override void BuildWidgets(Transform parent, out SafeAction onChanged, out SafeAction requestUpdate)
	{
		SettingsPanel.Build(this, parent, 0f, out onChanged, out requestUpdate);
	}
}
