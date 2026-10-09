using LastStand.Combat;
using LastStand.Loot;
using LastStand.Waves;
using System.Collections.Generic;
using System;

namespace LastStand.Config;

internal sealed class SessionSettings
{
	private const int MinimumStep = 1;

	public string ArenaRoom { get; }

	public double FirstWaveDelaySeconds { get; }

	public bool ShowWaveInQuestTracker { get; }

	public int StartingWorld { get; }

	public bool DreamWell { get; }

	public bool AltarOfCleansing { get; }

	public bool ShrineOfAscension { get; }

	public bool MawOfDoom { get; }

	public bool ShrineOfDestiny { get; }

	public int DestinyClaimsPerWave { get; }

	public bool HatredRemnant { get; }

	public int HatredDreamDustPerWave { get; }

	public int HatredShardGold { get; }

	public bool ReminiscenceOfLuck { get; }

	public CurseSettings Curses { get; }

	public Cadence AscensionResetWaves { get; }

	public GoldPriceRule GoldPrices { get; }

	public int StartingGold { get; }

	public int StartingDreamDust { get; }

	public bool CheatCommands { get; }

	public int MaxKillGoldPerPlayer { get; }

	public bool StripRoomModifiers { get; }

	public WaveSettings Waves { get; }

	public PainSettings Pain { get; }

	public LootSettings Loot { get; }

	private SessionSettings(string arenaRoom, double firstWaveDelaySeconds, bool showWaveInQuestTracker, int startingWorld, bool dreamWell, bool altarOfCleansing, bool shrineOfAscension, bool mawOfDoom, bool shrineOfDestiny, int destinyClaimsPerWave, bool hatredRemnant, int hatredDreamDustPerWave, int hatredShardGold, bool reminiscenceOfLuck, CurseSettings curses, Cadence ascensionResetWaves, GoldPriceRule goldPrices, int startingGold, int startingDreamDust, bool cheatCommands, int maxKillGoldPerPlayer, bool stripRoomModifiers, WaveSettings waves, PainSettings pain, LootSettings loot)
	{
		DreamWell = dreamWell;
		AltarOfCleansing = altarOfCleansing;
		ShrineOfAscension = shrineOfAscension;
		MawOfDoom = mawOfDoom;
		ShrineOfDestiny = shrineOfDestiny;
		DestinyClaimsPerWave = destinyClaimsPerWave;
		HatredRemnant = hatredRemnant;
		HatredDreamDustPerWave = hatredDreamDustPerWave;
		HatredShardGold = hatredShardGold;
		ReminiscenceOfLuck = reminiscenceOfLuck;
		Curses = curses;
		AscensionResetWaves = ascensionResetWaves;
		GoldPrices = goldPrices;
		StartingGold = startingGold;
		StartingDreamDust = startingDreamDust;
		CheatCommands = cheatCommands;
		MaxKillGoldPerPlayer = maxKillGoldPerPlayer;
		ArenaRoom = arenaRoom;
		FirstWaveDelaySeconds = firstWaveDelaySeconds;
		ShowWaveInQuestTracker = showWaveInQuestTracker;
		StartingWorld = startingWorld;
		StripRoomModifiers = stripRoomModifiers;
		Waves = waves;
		Pain = pain;
		Loot = loot;
	}

	public static SessionSettings From(LastStandConfig config)
	{
		return new SessionSettings(config.arenaRoom, Math.Max(0f, config.firstWaveDelaySeconds), config.showWaveInQuestTracker, Math.Max(1, config.startingWorld), config.dreamWell, config.altarOfCleansing, config.shrineOfAscension, config.mawOfDoom, config.shrineOfDestiny, Math.Max(1, config.destinyClaimsPerWave), config.hatredRemnant || config.curseToStartWaves, Math.Max(0, config.hatredDreamDustPerWave), Math.Max(0, config.hatredShardGold), config.reminiscenceOfLuck, CursesFrom(config), CadenceFrom(config.ascensionResetEveryWaves, config.ascensionResetEveryWaves), GoldPricesFrom(config), Math.Max(0, config.startingGold), StartingDreamDustRule.Amount(Math.Max(0, config.startingDreamDust), Math.Max(1, config.startingWorld)), config.cheatCommands, Math.Max(0, config.maxKillGoldPerPlayer), config.stripRoomModifiers, WavesFrom(config), PainFrom(config), LootFrom(config));
	}

	private static WaveSettings WavesFrom(LastStandConfig config)
	{
		return new WaveSettings
		{
			IntermissionSeconds = Math.Max(0f, config.intermissionSeconds),
			BossIntermissionSeconds = Math.Max(0f, config.bossIntermissionSeconds),
			CurseToStart = config.curseToStartWaves,
			CurseStartDelaySeconds = Math.Max(0f, config.curseStartDelaySeconds),
			RegularBase = Math.Max(0, config.regularBase),
			RegularGrowthPerWave = Math.Max(0, config.regularGrowthPerWave),
			RegularGrowthUntilWave = Math.Max(1, config.regularGrowthUntilWave),
			ExtraPlayerShare = Math.Max(0f, config.extraPlayerShare),
			Surges = CadenceFrom(config.surgeIntervalWaves, config.surgeIntervalWaves),
			SurgeMultiplier = Math.Max(0f, config.surgeMonsterMultiplier),
			MaxAliveBase = Math.Max(1, config.maxAliveBase),
			MaxAlivePerExtraPlayer = Math.Max(0, config.maxAlivePerExtraPlayer),
			SpawnIntervalSeconds = Math.Max(0f, config.spawnIntervalSeconds),
			SpawnsPerInterval = Math.Max(1, config.spawnsPerInterval),
			MinSpawnDistanceFromHeroes = Math.Max(0f, config.minSpawnDistanceFromHeroes),
			StartingAmbientLevel = Math.Max(1, config.startingAmbientLevel),
			WavesPerAmbientLevel = Math.Max(1, config.wavesPerAmbientLevel),
			HealthBonusPerWave = Math.Max(0f, config.healthBonusPercentPerWave),
			DamageBonusPerWave = Math.Max(0f, config.damageBonusPercentPerWave),
			LateScalingFromWave = Math.Max(1, config.lateScalingFromWave),
			LateHealthBonusPerWave = Math.Max(0f, config.lateHealthBonusPercentPerWave),
			LateDamageBonusPerWave = Math.Max(0f, config.lateDamageBonusPercentPerWave),
			FastClear = new FastClearRule(Math.Max(0f, config.fastClearKillPercent) / 100f, Math.Max(0f, config.fastClearSeconds), Math.Max(0f, config.fastClearStatMultiplier), Math.Max(1, config.fastClearFromWave), config.fastClearAffectsHeroicBosses ? (Math.Max(0f, config.fastClearHeroicBossPercent) / 100f) : 0f),
			MiniBosses = CadenceFrom(config.miniBossFromWave, config.miniBossIntervalWaves),
			MiniBossOccurrencesPerExtra = Math.Max(1, config.miniBossOccurrencesPerExtra),
			MaxMiniBosses = Math.Max(0, config.maxMiniBossesPerWave),
			Pain = CadenceFrom(config.painFromWave, config.painIntervalWaves),
			PainOccurrencesPerExtra = Math.Max(1, config.painOccurrencesPerExtra),
			MaxPain = Math.Max(0, config.maxPainPerWave),
			HeroicBosses = new ShiftingCadence(CadenceFrom(config.heroicBossFromWave, config.heroicBossIntervalWaves), config.heroicBossFasterFromWave, Math.Max(1, config.heroicBossLateIntervalWaves)),
			HeroicBossStepWaves = Math.Max(1, config.heroicBossStepWaves),
			MaxHeroicBosses = Math.Max(0, config.maxHeroicBossesPerWave),
			HeroicBossEntranceDelaySeconds = Math.Max(0f, config.heroicBossEntranceDelaySeconds),
			HeroicBossAbilityHaste = Math.Max(0f, config.heroicBossAbilityHaste),
			HeroicBossHastePerBossWave = Math.Max(0f, config.heroicBossAbilityHastePerBossWave),
			HeroicBossHealthMultiplier = Math.Max(0.1f, config.heroicBossHealthMultiplier),
			HeroicBossDamageMultiplier = Math.Max(0.1f, config.heroicBossDamageMultiplier),
			HeroicBossTypes = ListFrom(config.heroicBossTypes),
			AllHeroicBossTypes = ConfigChoices.Values(ChoiceSet.HeroicBosses),
			ForeignTypes = CadenceFrom(config.foreignTypesFromWave, config.foreignTypesIntervalWaves),
			ForeignTypesPerUnlock = Math.Max(1, config.foreignTypesPerUnlock),
			HunterFromWave = Math.Max(1, config.hunterFromWave),
			AllHuntersFromWave = Math.Max(Math.Max(1, config.hunterFromWave), config.allHuntersFromWave),
			HuntLevelStepWaves = Math.Max(1, config.huntLevelStepWaves),
			MaxHuntLevel = Math.Max(0, config.maxHuntLevel),
			Mirage = CadenceFrom(config.mirageFromWave, config.mirageIntervalWaves),
			MirageChance = ToChance(config.mirageChancePercent),
			MirageRampFromWave = Math.Max(1, config.mirageRampFromWave),
			AllMirageFromWave = Math.Max(Math.Max(1, config.mirageRampFromWave), config.allMirageFromWave),
			FriendlyFireFromWave = Math.Max(1, config.friendlyFireFromWave),
			FriendlyFireChance = ToChance(config.friendlyFireChancePercent),
			DarkUrgeStrength = StrengthFrom(config.darkUrgeStrength),
			Environments = CadenceFrom(config.environmentFromWave, config.environmentIntervalWaves),
			EnvironmentUntilWave = config.environmentUntilWave,
			EnvironmentTypes = KnownChoices(config.environmentTypes, ChoiceSet.Environments)
		};
	}

	private static GoldPriceRule GoldPricesFrom(LastStandConfig config)
	{
		return new GoldPriceRule(config.freeGoldShrines, Math.Max(0f, config.goldPriceGrowthPercent) / 100f, CadenceFrom(config.goldPriceGrowthEveryWaves, config.goldPriceGrowthEveryWaves), Math.Max(0, config.goldPriceGrowthUntilWave));
	}

	private static CurseSettings CursesFrom(LastStandConfig config)
	{
		return new CurseSettings(Math.Max(0.05f, config.curseMildWavesOfKills), Math.Max(0.05f, config.cursePotentWavesOfKills), Math.Max(0.05f, config.cursePowerfulWavesOfKills));
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
			itemRarityFloor: ToChance(Math.Max(0f, config.itemRarityFloorPercent)),
			freeLeftoverItems: config.freeLeftoverItemsAtWaveEnd,
			itemDrops: config.monsterItemDrops);
	}

	private static IReadOnlyList<string> ListFrom(string text)
	{
		return ChoiceList.Split(text);
	}

	private static HatredStrengthType StrengthFrom(string text)
	{
		if (!ChoiceList.Contains(ConfigChoices.CurseStrengths, text) || !Enum.TryParse<HatredStrengthType>(text, out var result))
		{
			return HatredStrengthType.Potent;
		}
		return result;
	}

	private static IReadOnlyList<string> KnownChoices(string text, ChoiceSet set)
	{
		IReadOnlyList<Choice> choices = ConfigChoices.For(set);
		return ChoiceList.Split(text).FindAll(item => ChoiceList.Contains(choices, item));
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
