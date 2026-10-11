using System;
using System.Collections.Generic;
using LastStand.Combat;
using LastStand.Loot;
using LastStand.Waves;

namespace LastStand.Config;

internal sealed class SessionSettings
{
	private const int MinimumStep = 1;

	public IReadOnlyList<string> ArenaRooms { get; }

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

	public int HatredGoldPerWave { get; }

	public bool ReminiscenceOfLuck { get; }

	public CurseSettings Curses { get; }

	public Cadence AscensionResetWaves { get; }

	public GoldPriceRule GoldPrices { get; }

	public int StartingGold { get; }

	public int StartingDreamDust { get; }

	public bool CheatCommands { get; }

	public int MaxKillGoldPerPlayer { get; }

	public bool StripRoomModifiers { get; }

	public bool BossMusic { get; }

	public WaveSettings Waves { get; }

	public PainSettings Pain { get; }

	public LootSettings Loot { get; }

	private SessionSettings(IReadOnlyList<string> arenaRooms, double firstWaveDelaySeconds, bool showWaveInQuestTracker, int startingWorld,
		bool dreamWell, bool altarOfCleansing, bool shrineOfAscension, bool mawOfDoom, bool shrineOfDestiny,
		int destinyClaimsPerWave, bool hatredRemnant, int hatredDreamDustPerWave, int hatredGoldPerWave,
		bool reminiscenceOfLuck, CurseSettings curses, Cadence ascensionResetWaves, GoldPriceRule goldPrices,
		int startingGold, int startingDreamDust, bool cheatCommands, int maxKillGoldPerPlayer, bool stripRoomModifiers,
		bool bossMusic, WaveSettings waves, PainSettings pain, LootSettings loot)
	{
		DreamWell = dreamWell;
		AltarOfCleansing = altarOfCleansing;
		ShrineOfAscension = shrineOfAscension;
		MawOfDoom = mawOfDoom;
		ShrineOfDestiny = shrineOfDestiny;
		DestinyClaimsPerWave = destinyClaimsPerWave;
		HatredRemnant = hatredRemnant;
		HatredDreamDustPerWave = hatredDreamDustPerWave;
		HatredGoldPerWave = hatredGoldPerWave;
		ReminiscenceOfLuck = reminiscenceOfLuck;
		Curses = curses;
		AscensionResetWaves = ascensionResetWaves;
		GoldPrices = goldPrices;
		StartingGold = startingGold;
		StartingDreamDust = startingDreamDust;
		CheatCommands = cheatCommands;
		MaxKillGoldPerPlayer = maxKillGoldPerPlayer;
		ArenaRooms = arenaRooms;
		FirstWaveDelaySeconds = firstWaveDelaySeconds;
		ShowWaveInQuestTracker = showWaveInQuestTracker;
		StartingWorld = startingWorld;
		StripRoomModifiers = stripRoomModifiers;
		BossMusic = bossMusic;
		Waves = waves;
		Pain = pain;
		Loot = loot;
	}

	public static SessionSettings From(LastStandConfig config)
	{
		return new SessionSettings(ArenaRoomsFrom(config), Math.Max(0f, config.firstWaveDelaySeconds),
			config.showWaveInQuestTracker, Math.Max(Tuning.MinStartingWorld, config.startingWorld), config.dreamWell,
			config.altarOfCleansing, config.shrineOfAscension, config.mawOfDoom, config.shrineOfDestiny,
			Math.Max(Tuning.MinimumDestinyClaims, config.destinyClaimsPerWave),
			config.hatredRemnant || config.curseToStartWaves,
			Math.Max(0, config.hatredDreamDustPerWave), Math.Max(0, config.hatredGoldPerWave),
			config.reminiscenceOfLuck, CursesFrom(config),
			CadenceFrom(config.ascensionResetEveryWaves, config.ascensionResetEveryWaves), GoldPricesFrom(config),
			Math.Max(0, config.startingGold),
			StartingDreamDustRule.Amount(Math.Max(0, config.startingDreamDust),
				Math.Max(Tuning.MinStartingWorld, config.startingWorld)),
			config.cheatCommands, Math.Max(0, config.maxKillGoldPerPlayer), config.stripRoomModifiers, config.bossMusic,
			WavesFrom(config), PainFrom(config), LootFrom(config));
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
			ExtraPlayerShare = Math.Max(0f, config.extraPlayerShare),
			Surges = CadenceFrom(config.surgeIntervalWaves, config.surgeIntervalWaves),
			SurgeMultiplier = Math.Max(0f, config.surgeMonsterMultiplier),
			Merge = new MergeRule(Math.Max(0, config.regularBase), Math.Max(0, config.regularGrowthPerWave),
				Math.Max(0, config.mergedWaveSize), Math.Max(0, config.mergedSurgeMiniBosses),
				Math.Max(0, config.mergedSurgePain), Math.Max(0f, config.mergedMiniBossHealthMultiple)),
			MaxAliveBase = Math.Max(MinimumStep, config.maxAliveBase),
			MaxAlivePerExtraPlayer = Math.Max(0, config.maxAlivePerExtraPlayer),
			SpawnIntervalSeconds = Math.Max(0f, config.spawnIntervalSeconds),
			SpawnsPerInterval = Math.Max(MinimumStep, config.spawnsPerInterval),
			MinSpawnDistanceFromHeroes = Math.Max(0f, config.minSpawnDistanceFromHeroes),

			StartingAmbientLevel = Math.Max(Tuning.MinimumAmbientLevel, config.startingAmbientLevel),
			WavesPerAmbientLevel = Math.Max(MinimumStep, config.wavesPerAmbientLevel),
			World = new WorldRule(config.worldRisePerStep, config.worldRiseStepWaves, config.fasterWorldRiseFromWave,
				config.fasterWorldRisePerStep, config.fasterWorldRiseStepWaves),
			HeroicFastKill = new HeroicFastKillRule(Math.Max(0f, config.heroicFastKillSeconds),
				Math.Max(0f, config.heroicFastKillBurstSeconds),
				Math.Max((float)HeroicFastKillRule.NoChange, config.heroicFastKillMaxStep)),

			MiniBosses = CadenceFrom(config.miniBossFromWave, config.miniBossIntervalWaves),
			MiniBossOccurrencesPerExtra = Math.Max(MinimumStep, config.miniBossOccurrencesPerExtra),
			MaxMiniBosses = Math.Max(0, config.maxMiniBossesPerWave),

			Pain = CadenceFrom(config.painFromWave, config.painIntervalWaves),
			PainOccurrencesPerExtra = Math.Max(MinimumStep, config.painOccurrencesPerExtra),
			MaxPain = Math.Max(0, config.maxPainPerWave),

			HeroicBosses = new ShiftingCadence(CadenceFrom(config.heroicBossFromWave, config.heroicBossIntervalWaves),
				config.heroicBossFasterFromWave, Math.Max(MinimumStep, config.heroicBossLateIntervalWaves)),
			HeroicBossesPerBossWave = Math.Max(0, config.heroicBossesAddedPerBossWave),
			MaxHeroicBosses = Math.Max(0, config.maxHeroicBossesPerWave),
			HeroicBossWindowFromWave = Math.Max(Tuning.FirstWave, config.heroicBossWindowFromWave),
			HeroicBossWindowSeconds = Math.Max(0f, config.heroicBossWindowSeconds),
			HeroicBossEntranceDelaySeconds = Math.Max(0f, config.heroicBossEntranceDelaySeconds),
			HeroicBossAbilityHaste = Math.Max(0f, config.heroicBossAbilityHaste),
			HeroicBossHastePerBossWave = Math.Max(0f, config.heroicBossAbilityHastePerBossWave),
			HeroicBossTypes = ListFrom(config.heroicBossTypes),
			AllHeroicBossTypes = ConfigChoices.Values(ChoiceSet.HeroicBosses),

			ForeignTypes = CadenceFrom(config.foreignTypesFromWave, config.foreignTypesIntervalWaves),
			ForeignTypesPerUnlock = Math.Max(MinimumStep, config.foreignTypesPerUnlock),

			HunterFromWave = Math.Max(Tuning.FirstWave, config.hunterFromWave),
			AllHuntersFromWave = Math.Max(Math.Max(Tuning.FirstWave, config.hunterFromWave),
				config.allHuntersFromWave),
			HuntLevelStepWaves = Math.Max(MinimumStep, config.huntLevelStepWaves),
			MaxHuntLevel = Math.Max(0, config.maxHuntLevel),

			Mirage = CadenceFrom(config.mirageFromWave, config.mirageIntervalWaves),
			MirageChance = ToChance(config.mirageChancePercent),
			MirageRampFromWave = Math.Max(Tuning.FirstWave, config.mirageRampFromWave),
			AllMirageFromWave = Math.Max(Math.Max(Tuning.FirstWave, config.mirageRampFromWave),
				config.allMirageFromWave),

			FriendlyFireFromWave = Math.Max(Tuning.FirstWave, config.friendlyFireFromWave),
			FriendlyFireChance = ToChance(config.friendlyFireChancePercent),
			DarkUrgeStrength = StrengthFrom(config.darkUrgeStrength),

			Environment = new EnvironmentRule(ToChance(config.environmentChancePercent), Math.Max(Tuning.FirstWave, config.environmentHighFromWave),
				ToChance(config.environmentHighChancePercent), Math.Max(Tuning.FirstWave, config.hunterFromWave),
				ToChance(config.environmentHuntedChancePercent), config.environmentHuntedMaxPerBlock,
				config.environmentHuntedBlockWaves, Math.Max(Tuning.FirstWave, config.environmentOnHeroicFromWave),
				config.environmentUntilWave),
			EnvironmentTypes = KnownChoices(config.environmentTypes, ChoiceSet.Environments),
		};
	}

	private static GoldPriceRule GoldPricesFrom(LastStandConfig config)
	{
		return new GoldPriceRule(config.freeGoldShrines,
			Math.Max(0f, config.goldPriceGrowthPercent) / SharedTuning.PercentScale,
			CadenceFrom(config.goldPriceGrowthEveryWaves, config.goldPriceGrowthEveryWaves),
			Math.Max(0, config.goldPriceGrowthUntilWave));
	}

	private static CurseSettings CursesFrom(LastStandConfig config)
	{
		return new CurseSettings(Math.Max(Tuning.MinimumCurseWaves, config.curseMildWavesOfKills),
			Math.Max(Tuning.MinimumCurseWaves, config.cursePotentWavesOfKills),
			Math.Max(Tuning.MinimumCurseWaves, config.cursePowerfulWavesOfKills));
	}

	private static PainSettings PainFrom(LastStandConfig config)
	{
		return new PainSettings(
			damageRatioPerSecond: Math.Max(0f, config.painDamagePercentPerSecond) / SharedTuning.PercentScale,
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
			growthPerWave: Math.Max(0f, config.rewardGrowthPercentPerWave) / SharedTuning.PercentScale,
			guaranteedDropPerWave: config.guaranteedDropPerWave,
			heroicBossChaosRewards: Math.Max(0, config.heroicBossChaosRewards),
			heroicBossStardust: config.heroicBossStardust,
			itemRarityFloor: ToChance(Math.Max(0f, config.itemRarityFloorPercent)),
			freeLeftoverItems: config.freeLeftoverItemsAtWaveEnd,
			itemDrops: config.monsterItemDrops);
	}

	private static IReadOnlyList<string> ArenaRoomsFrom(LastStandConfig config)
	{
		IReadOnlyList<string> picked = config.randomArenaRoom
			? KnownChoices(config.randomArenaRooms, ChoiceSet.ArenaRooms)
			: null;
		return picked != null && picked.Count > 0 ? picked : new[] { config.arenaRoom };
	}

	private static IReadOnlyList<string> ListFrom(string text)
	{
		return ChoiceList.Split(text);
	}

	private static HatredStrengthType StrengthFrom(string text)
	{
		if (!ChoiceList.Contains(ConfigChoices.CurseStrengths, text)
		    || !Enum.TryParse(text, out HatredStrengthType strength))
		{
			return HatredStrengthType.Potent;
		}

		return strength;
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
		return Math.Max(0f, Math.Min(SharedTuning.PercentScale, percent)) / SharedTuning.PercentScale;
	}
}
