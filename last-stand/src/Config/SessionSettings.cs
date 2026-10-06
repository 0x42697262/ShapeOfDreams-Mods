using System;
using System.Collections.Generic;
using LastStand.Combat;
using LastStand.Loot;
using LastStand.Waves;

namespace LastStand.Config;

internal sealed class SessionSettings
{
	private const int MinimumStep = 1;

	public string ArenaRoom { get; }

	public double FirstWaveDelaySeconds { get; }

	public bool ShowWaveInHud { get; }

	public bool RandomBossMusic { get; }

	public bool StripRoomModifiers { get; }

	public WaveSettings Waves { get; }

	public PainSettings Pain { get; }

	public LootSettings Loot { get; }

	private SessionSettings(string arenaRoom, double firstWaveDelaySeconds, bool showWaveInHud,
		bool randomBossMusic, bool stripRoomModifiers, WaveSettings waves, PainSettings pain, LootSettings loot)
	{
		RandomBossMusic = randomBossMusic;
		ArenaRoom = arenaRoom;
		FirstWaveDelaySeconds = firstWaveDelaySeconds;
		ShowWaveInHud = showWaveInHud;
		StripRoomModifiers = stripRoomModifiers;
		Waves = waves;
		Pain = pain;
		Loot = loot;
	}

	public static SessionSettings From(LastStandConfig config)
	{
		return new SessionSettings(config.arenaRoom, Math.Max(0f, config.firstWaveDelaySeconds),
			config.showWaveInHud, config.randomBossMusic, config.stripRoomModifiers, WavesFrom(config), PainFrom(config), LootFrom(config));
	}

	private static WaveSettings WavesFrom(LastStandConfig config)
	{
		return new WaveSettings
		{
			IntermissionSeconds = Math.Max(0f, config.intermissionSeconds),
			RegularBase = Math.Max(0, config.regularBase),
			RegularGrowthPerWave = Math.Max(0, config.regularGrowthPerWave),
			ExtraPlayerShare = Math.Max(0f, config.extraPlayerShare),
			Surges = CadenceFrom(config.surgeIntervalWaves, config.surgeIntervalWaves),
			SurgeMultiplier = Math.Max(0f, config.surgeMonsterMultiplier),
			MaxAliveBase = Math.Max(MinimumStep, config.maxAliveBase),
			MaxAlivePerExtraPlayer = Math.Max(0, config.maxAlivePerExtraPlayer),
			SpawnIntervalSeconds = Math.Max(0f, config.spawnIntervalSeconds),
			SpawnsPerInterval = Math.Max(MinimumStep, config.spawnsPerInterval),
			MinSpawnDistanceFromHeroes = Math.Max(0f, config.minSpawnDistanceFromHeroes),

			StartingAmbientLevel = Math.Max(Tuning.MinimumAmbientLevel, config.startingAmbientLevel),
			WavesPerAmbientLevel = Math.Max(MinimumStep, config.wavesPerAmbientLevel),
			HealthBonusPerWave = Math.Max(0f, config.healthBonusPercentPerWave),
			DamageBonusPerWave = Math.Max(0f, config.damageBonusPercentPerWave),

			MiniBosses = CadenceFrom(config.miniBossFromWave, config.miniBossIntervalWaves),
			MiniBossOccurrencesPerExtra = Math.Max(MinimumStep, config.miniBossOccurrencesPerExtra),
			MaxMiniBosses = Math.Max(0, config.maxMiniBossesPerWave),
			MiniBossHealthMultiplier = Math.Max(Tuning.MinimumStatMultiplier, config.miniBossHealthMultiplier),
			MiniBossDamageMultiplier = Math.Max(Tuning.MinimumStatMultiplier, config.miniBossDamageMultiplier),

			Pain = CadenceFrom(config.painFromWave, config.painIntervalWaves),
			PainOccurrencesPerExtra = Math.Max(MinimumStep, config.painOccurrencesPerExtra),
			MaxPain = Math.Max(0, config.maxPainPerWave),

			HeroicBosses = CadenceFrom(config.heroicBossFromWave, config.heroicBossIntervalWaves),
			HeroicBossStepWaves = Math.Max(MinimumStep, config.heroicBossStepWaves),
			MaxHeroicBosses = Math.Max(0, config.maxHeroicBossesPerWave),
			HeroicBossEntranceDelaySeconds = Math.Max(0f, config.heroicBossEntranceDelaySeconds),
			HeroicBossHealthMultiplier = Math.Max(Tuning.MinimumStatMultiplier, config.heroicBossHealthMultiplier),
			HeroicBossDamageMultiplier = Math.Max(Tuning.MinimumStatMultiplier, config.heroicBossDamageMultiplier),
			HeroicBossAbilityHaste = Math.Max(0f, config.heroicBossAbilityHaste),
			HeroicBossHastePerBossWave = Math.Max(0f, config.heroicBossAbilityHastePerBossWave),
			HeroicBossTypes = ListFrom(config.heroicBossTypes),

			ForeignTypes = CadenceFrom(config.foreignTypesFromWave, config.foreignTypesIntervalWaves),
			ForeignTypesPerUnlock = Math.Max(MinimumStep, config.foreignTypesPerUnlock),

			HunterFromWave = Math.Max(Tuning.FirstWave, config.hunterFromWave),
			HunterIntervalWaves = Math.Max(MinimumStep, config.hunterIntervalWaves),
			MaxHunters = Math.Max(0, config.maxHuntersPerWave),
			HuntedZoneIntervalWaves = Math.Max(MinimumStep, config.huntedZoneIntervalWaves),
			MaxHuntLevel = Math.Max(0, config.maxHuntLevel),

			Mirage = CadenceFrom(config.mirageFromWave, config.mirageIntervalWaves),
			MirageChance = ToChance(config.mirageChancePercent),
		};
	}

	private static PainSettings PainFrom(LastStandConfig config)
	{
		return new PainSettings(
			damageRatioPerSecond: Math.Max(0f, config.painDamagePercentPerSecond) / Tuning.PercentScale,
			healthBonusPercent: config.painHealthBonusPercent,
			damageBonusPercent: config.painDamageBonusPercent);
	}

	private static LootSettings LootFrom(LastStandConfig config)
	{
		RarityChance[] rarestFirst =
		{
			new RarityChance(Rarity.Legendary, ToChance(config.legendaryDropPercent),
				ToChance(config.legendaryDropPercentAtTarget)),
			new RarityChance(Rarity.Epic, ToChance(config.epicDropPercent), ToChance(config.epicDropPercentAtTarget)),
			new RarityChance(Rarity.Rare, ToChance(config.rareDropPercent), ToChance(config.rareDropPercentAtTarget)),
			new RarityChance(Rarity.Common, ToChance(config.commonDropPercent),
				ToChance(config.commonDropPercentAtTarget)),
		};

		RarityChance[] chaosRarestFirst =
		{
			new RarityChance(Rarity.Legendary, ToChance(config.chaosLegendaryPercent),
				ToChance(config.chaosLegendaryPercentAtTarget)),
			new RarityChance(Rarity.Epic, ToChance(config.chaosEpicPercent),
				ToChance(config.chaosEpicPercentAtTarget)),
			new RarityChance(Rarity.Rare, ToChance(config.chaosRarePercent),
				ToChance(config.chaosRarePercentAtTarget)),
		};

		TierLoot mobs = new TierLoot(Math.Max(0f, config.regularDropMultiplier), config.regularDreamDust,
			config.regularGold);

		Dictionary<EnemyTier, TierLoot> tiers = new Dictionary<EnemyTier, TierLoot>
		{
			[EnemyTier.Regular] = mobs,
			[EnemyTier.Hunter] = mobs,
			[EnemyTier.MiniBoss] = new TierLoot(
				Math.Max(0f, config.miniBossDropMultiplier), config.miniBossDreamDust, config.miniBossGold),
			[EnemyTier.Pain] = new TierLoot(
				Math.Max(0f, config.painDropMultiplier), config.painDreamDust, config.painGold),
			[EnemyTier.HeroicBoss] = new TierLoot(
				Math.Max(0f, config.heroicBossDropMultiplier), config.heroicBossDreamDust, config.heroicBossGold),
		};

		return new LootSettings(
			new RarityTable(rarestFirst, config.itemTargetWave),
			ToChance(config.memorySharePercent),
			new RarityTable(chaosRarestFirst, config.chaosTargetWave),
			tiers,
			mobItemsPerPlayerPerWave: Math.Max(0, config.mobItemsPerPlayerPerWave),
			growthPerWave: Math.Max(0f, config.rewardGrowthPercentPerWave) / Tuning.PercentScale,
			guaranteedDropPerWave: config.guaranteedDropPerWave,
			heroicBossChaosRewards: Math.Max(0, config.heroicBossChaosRewards),
			itemRarityFloor: ToChance(Math.Max(0f, config.itemRarityFloorPercent)));
	}

	private static IReadOnlyList<string> ListFrom(string text)
	{
		return ChoiceList.Split(text);
	}

	private static Cadence CadenceFrom(int firstWave, int intervalWaves)
	{
		return new Cadence(Math.Max(Tuning.FirstWave, firstWave), Math.Max(MinimumStep, intervalWaves));
	}

	private static double ToChance(float percent)
	{
		return Math.Max(0f, Math.Min(Tuning.PercentScale, percent)) / Tuning.PercentScale;
	}
}
