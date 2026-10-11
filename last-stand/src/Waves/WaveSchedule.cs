using System;
using System.Collections.Generic;

namespace LastStand.Waves;

internal static class WaveSchedule
{
	private const int SoloPlayers = 1;

	private const int FirstOfTier = 1;

	private const double NoSurge = 1;

	private const int NoHuntLevel = 0;

	public const int NoMaxHuntLevel = 0;

	private const double Everyone = 1;

	public const double NoEnvironmentRoll = 1;

	public static WavePlan Plan(int wave, int players, WaveSettings settings, WaveContent content,
		double environmentRoll, int environmentsInBlock)
	{
		int extraPlayers = Math.Max(0, players - SoloPlayers);
		int wavesSinceFirst = wave - FirstOfTier;
		double playerScale = SoloPlayers + settings.ExtraPlayerShare * extraPlayers;

		int foreign = ForeignTypesUnlocked(wave, settings, content.ForeignTypes);
		int mirage = MirageSkinsUnlocked(wave, settings, content.MirageSkins);
		int hunterStart = settings.HunterFromWave;
		bool surge = settings.Surges.IsOn(wave);
		double surgeScale = surge ? settings.SurgeMultiplier : NoSurge;
		int monsters = (int)Math.Round(
			(settings.RegularBase + settings.RegularGrowthPerWave * wavesSinceFirst) * playerScale * surgeScale);
		MergeRule merge = settings.Merge;
		int mobs = merge.Mobs(wave, monsters);
		int hunters = (int)Math.Round(mobs * HunterShare(wave, settings));
		int pain = Scaled(wave, settings.Pain, settings.PainOccurrencesPerExtra, settings.MaxPain)
		           + merge.ExtraPain(wave, surge);
		int heroic = content.HeroicBossTypes > 0 ? HeroicBosses(wave, settings) : 0;
		double environmentChance = EnvironmentChance(wave, settings, pain, heroic, environmentsInBlock);
		bool environment = environmentChance > 0 && environmentRoll <= environmentChance;
		int huntLevel = environment ? NoHuntLevel : HuntLevel(wave, hunterStart, settings);

		return new WavePlan
		{
			Number = wave,
			RegularCount = mobs - hunters,
			IsSurge = surge,
			HunterCount = hunters,
			UnmergedMobCount = monsters,
			MergeMultiplier = merge.StrengthFor(wave, monsters),
			MiniBossHealthMultiple = merge.MiniBossHealthMultipleFor(wave),
			MiniBossCount = Scaled(wave, settings.MiniBosses, settings.MiniBossOccurrencesPerExtra,
				settings.MaxMiniBosses) + merge.ExtraMiniBosses(wave, surge),
			PainCount = pain,
			HeroicBossCount = heroic,
			HeroicBossHaste = HeroicBossHaste(wave, settings),
			HeroicBossWindowSeconds = heroic > 0 && wave >= settings.HeroicBossWindowFromWave
				? settings.HeroicBossWindowSeconds
				: 0,
			AmbientLevel = settings.StartingAmbientLevel + wavesSinceFirst / settings.WavesPerAmbientLevel,
			MaxAlive = (int)Math.Round(
				(settings.MaxAliveBase + settings.MaxAlivePerExtraPlayer * extraPlayers) * Math.Max(NoSurge, surgeScale)),
			ExtraWorlds = settings.World.ExtraWorlds(wave),
			ForeignTypesUnlocked = foreign,
			NewForeignTypes = foreign > ForeignTypesUnlocked(wave - FirstOfTier, settings, content.ForeignTypes),
			HuntLevel = huntLevel,
			MirageSkinsUnlocked = mirage,
			NewMirageSkin = mirage > MirageSkinsUnlocked(wave - FirstOfTier, settings, content.MirageSkins),
			MirageChance = mirage > 0 ? MirageChance(wave, settings) : 0,
			HasEnvironment = environment,
			FriendlyFireChance = wave >= settings.FriendlyFireFromWave && heroic == 0 ? settings.FriendlyFireChance : 0,
		};
	}

	public static double EnvironmentChance(int wave, WaveSettings settings, int pain, int heroic,
		int environmentsInBlock)
	{
		EnvironmentRule rule = settings.Environment;
		if (pain > 0 || (heroic > 0 && !rule.AllowsHeroic(wave)) || settings.EnvironmentTypes.Count == 0)
		{
			return 0;
		}

		return rule.ChanceFor(wave, environmentsInBlock);
	}

	private static int ForeignTypesUnlocked(int wave, WaveSettings settings, int total)
	{
		int unlocks = settings.ForeignTypes.OccurrencesSoFar(wave);
		return Math.Min(total, unlocks * settings.ForeignTypesPerUnlock);
	}

	private static int MirageSkinsUnlocked(int wave, WaveSettings settings, int total)
	{
		return Math.Min(total, settings.Mirage.OccurrencesSoFar(wave));
	}

	public static double HunterShare(int wave, WaveSettings settings)
	{
		return Ramp(wave, settings.HunterFromWave, settings.AllHuntersFromWave);
	}

	public static double MirageChance(int wave, WaveSettings settings)
	{
		double ramp = Ramp(wave, settings.MirageRampFromWave, settings.AllMirageFromWave);
		return settings.MirageChance + (Everyone - settings.MirageChance) * ramp;
	}

	private static double Ramp(int wave, int fromWave, int fullWave)
	{
		if (wave < fromWave)
		{
			return 0;
		}

		if (wave >= fullWave)
		{
			return Everyone;
		}

		double rampWaves = fullWave - fromWave + FirstOfTier;
		return (wave - fromWave + FirstOfTier) / rampWaves;
	}

	public static int HuntLevel(int wave, int hunterStart, WaveSettings settings)
	{
		Cadence levels = new Cadence(hunterStart, settings.HuntLevelStepWaves);
		int level = Math.Max(NoHuntLevel, levels.OccurrencesSoFar(wave));
		return settings.MaxHuntLevel == NoMaxHuntLevel ? level : Math.Min(settings.MaxHuntLevel, level);
	}

	public static double BreakAfter(WavePlan plan, WaveSettings settings)
	{
		return plan != null && plan.HasBoss ? settings.BossIntermissionSeconds : settings.IntermissionSeconds;
	}

	public static List<double> HeroicBossEntranceSeconds(int bosses, double windowSeconds, Func<double> roll)
	{
		List<double> entrances = new List<double>();
		if (windowSeconds <= 0)
		{
			return entrances;
		}

		for (int boss = 0; boss < bosses; boss++)
		{
			entrances.Add(roll() * windowSeconds);
		}

		entrances.Sort();
		return entrances;
	}

	public static double HeroicBossHaste(int wave, WaveSettings settings)
	{
		int laterBossWaves = Math.Max(0, settings.HeroicBosses.Occurrence(wave));
		return settings.HeroicBossAbilityHaste + settings.HeroicBossHastePerBossWave * laterBossWaves;
	}

	private static int HeroicBosses(int wave, WaveSettings settings)
	{
		ShiftingCadence bosses = settings.HeroicBosses;
		if (!bosses.IsOn(wave))
		{
			return 0;
		}

		return Math.Min(settings.MaxHeroicBosses, FirstOfTier + bosses.Occurrence(wave) * settings.HeroicBossesPerBossWave);
	}

	private static int Scaled(int wave, Cadence cadence, int occurrencesPerExtra, int max)
	{
		if (!cadence.IsOn(wave))
		{
			return 0;
		}

		return Math.Min(max, FirstOfTier + cadence.Occurrence(wave) / occurrencesPerExtra);
	}
}
