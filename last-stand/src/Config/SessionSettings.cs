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

	public WaveSettings Waves { get; }

	public PainSettings Pain { get; }

	public LootSettings Loot { get; }

	private SessionSettings(string arenaRoom, double firstWaveDelaySeconds, bool showWaveInHud,
		WaveSettings waves, PainSettings pain, LootSettings loot)
	{
		ArenaRoom = arenaRoom;
		FirstWaveDelaySeconds = firstWaveDelaySeconds;
		ShowWaveInHud = showWaveInHud;
		Waves = waves;
		Pain = pain;
		Loot = loot;
	}

	public static SessionSettings From(LastStandConfig config)
	{
		return new SessionSettings(config.arenaRoom, Math.Max(0f, config.firstWaveDelaySeconds),
			config.showWaveInHud, WavesFrom(config), PainFrom(config), LootFrom(config));
	}

	private static WaveSettings WavesFrom(LastStandConfig config)
	{
		return new WaveSettings
		{
			IntermissionSeconds = Math.Max(0f, config.intermissionSeconds),
			RegularBase = Math.Max(0, config.regularBase),
			RegularGrowthPerWave = Math.Max(0, config.regularGrowthPerWave),
			ExtraPlayerShare = Math.Max(0f, config.extraPlayerShare),
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

			Pain = CadenceFrom(config.painFromWave, config.painIntervalWaves),
			PainOccurrencesPerExtra = Math.Max(MinimumStep, config.painOccurrencesPerExtra),
			MaxPain = Math.Max(0, config.maxPainPerWave),

			ForeignTypes = CadenceFrom(config.foreignTypesFromWave, config.foreignTypesIntervalWaves),
			ForeignTypesPerUnlock = Math.Max(MinimumStep, config.foreignTypesPerUnlock),

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
			new RarityChance(Rarity.Legendary, ToChance(config.legendaryDropPercent)),
			new RarityChance(Rarity.Epic, ToChance(config.epicDropPercent)),
			new RarityChance(Rarity.Rare, ToChance(config.rareDropPercent)),
			new RarityChance(Rarity.Common, ToChance(config.commonDropPercent)),
		};

		RarityChance[] chaosRarestFirst =
		{
			new RarityChance(Rarity.Legendary, ToChance(config.chaosLegendaryPercent)),
			new RarityChance(Rarity.Epic, ToChance(config.chaosEpicPercent)),
			new RarityChance(Rarity.Rare, ToChance(config.chaosRarePercent)),
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
		};

		return new LootSettings(
			rarestFirst,
			ToChance(config.memorySharePercent),
			chaosRarestFirst,
			tiers,
			mobItemsPerPlayerPerWave: Math.Max(0, config.mobItemsPerPlayerPerWave),
			growthPerWave: Math.Max(0f, config.rewardGrowthPercentPerWave) / Tuning.PercentScale,
			despawnSeconds: Math.Max(0f, config.lootDespawnSeconds),
			guaranteedDropPerWave: config.guaranteedDropPerWave);
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
