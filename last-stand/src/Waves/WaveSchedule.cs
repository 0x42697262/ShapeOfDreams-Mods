using LastStand.Config;
using System;

namespace LastStand.Waves;

internal static class WaveSchedule
{
	private const int SoloPlayers = 1;

	private const int FirstOfTier = 1;

	private const double NoSurge = 1;

	private const int NoHuntLevel = 0;

	private const int NoLastEnvironmentWave = 0;

	private const double Everyone = 1;

	public static WavePlan Plan(int wave, int players, WaveSettings settings, WaveContent content)
	{
		int num = Math.Max(0, players - 1);
		int num2 = wave - 1;
		int num3 = Math.Min(wave, settings.RegularGrowthUntilWave) - 1;
		double num4 = 1.0 + settings.ExtraPlayerShare * (double)num;
		int num5 = ForeignTypesUnlocked(wave, settings, content.ForeignTypes);
		int num6 = MirageSkinsUnlocked(wave, settings, content.MirageSkins);
		int hunterFromWave = settings.HunterFromWave;
		bool flag = settings.Surges.IsOn(wave);
		double num7 = (flag ? settings.SurgeMultiplier : 1.0);
		int num8 = (int)Math.Round((double)(settings.RegularBase + settings.RegularGrowthPerWave * num3) * num4 * num7);
		int num9 = (int)Math.Round((double)num8 * HunterShare(wave, settings));
		int num10 = Scaled(wave, settings.Pain, settings.PainOccurrencesPerExtra, settings.MaxPain);
		bool flag2 = HasEnvironment(wave, settings, num10);
		int huntLevel = ((!flag2) ? HuntLevel(wave, hunterFromWave, settings) : 0);
		int num11 = ((content.HeroicBossTypes > 0) ? HeroicBosses(wave, settings) : 0);
		return new WavePlan
		{
			Number = wave,
			RegularCount = num8 - num9,
			IsSurge = flag,
			HunterCount = num9,
			MiniBossCount = Scaled(wave, settings.MiniBosses, settings.MiniBossOccurrencesPerExtra, settings.MaxMiniBosses),
			PainCount = num10,
			HeroicBossCount = num11,
			HeroicBossHaste = HeroicBossHaste(wave, settings),
			AmbientLevel = settings.StartingAmbientLevel + num2 / settings.WavesPerAmbientLevel,
			MaxAlive = (int)Math.Round((double)(settings.MaxAliveBase + settings.MaxAlivePerExtraPlayer * num) * Math.Max(1.0, num7)),
			HealthBonusPercent = StatBonus(wave, settings.HealthBonusPerWave, settings.LateHealthBonusPerWave, settings.LateScalingFromWave),
			DamageBonusPercent = StatBonus(wave, settings.DamageBonusPerWave, settings.LateDamageBonusPerWave, settings.LateScalingFromWave),
			ForeignTypesUnlocked = num5,
			NewForeignTypes = (num5 > ForeignTypesUnlocked(wave - 1, settings, content.ForeignTypes)),
			HuntLevel = huntLevel,
			MirageSkinsUnlocked = num6,
			NewMirageSkin = (num6 > MirageSkinsUnlocked(wave - 1, settings, content.MirageSkins)),
			MirageChance = ((num6 > 0) ? MirageChance(wave, settings) : 0.0),
			HasEnvironment = flag2,
			FriendlyFireChance = ((wave >= settings.FriendlyFireFromWave && num11 == 0) ? settings.FriendlyFireChance : 0.0)
		};
	}

	public static bool HasEnvironment(int wave, WaveSettings settings, int pain)
	{
		bool beforeLast = settings.EnvironmentUntilWave == NoLastEnvironmentWave ||
		                  wave <= settings.EnvironmentUntilWave;
		return settings.Environments.IsOn(wave) && beforeLast && pain == 0 && settings.EnvironmentTypes.Count > 0;
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

	public static double StatBonus(int wave, double perWave, double latePerWave, int lateFromWave)
	{
		int raises = Math.Max(0, wave - FirstOfTier);
		int lateRaises = Math.Min(raises, Math.Max(0, wave - lateFromWave + FirstOfTier));
		return perWave * (raises - lateRaises) + latePerWave * lateRaises;
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
		return Math.Max(NoHuntLevel, Math.Min(settings.MaxHuntLevel, levels.OccurrencesSoFar(wave)));
	}

	public static double BreakAfter(WavePlan plan, WaveSettings settings)
	{
		return plan != null && plan.HasBoss ? settings.BossIntermissionSeconds : settings.IntermissionSeconds;
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

		return Math.Min(settings.MaxHeroicBosses, FirstOfTier + (wave - bosses.FirstWave) / settings.HeroicBossStepWaves);
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
