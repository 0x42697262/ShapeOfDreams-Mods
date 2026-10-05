using System;

namespace LastStand.Waves;

internal static class WaveSchedule
{
	private const int SoloPlayers = 1;

	private const int FirstOfTier = 1;

	private const int NoHuntLevel = 0;

	public static WavePlan Plan(int wave, int players, WaveSettings settings, WaveContent content)
	{
		int extraPlayers = Math.Max(0, players - SoloPlayers);
		int wavesSinceFirst = wave - FirstOfTier;
		double playerScale = SoloPlayers + settings.ExtraPlayerShare * extraPlayers;

		int foreign = ForeignTypesUnlocked(wave, settings, content.ForeignTypes);
		int mirage = MirageSkinsUnlocked(wave, settings, content.MirageSkins);
		int hunterStart = HunterStartWave(settings, content.ForeignTypes);

		return new WavePlan
		{
			Number = wave,
			RegularCount = (int)Math.Round(
				(settings.RegularBase + settings.RegularGrowthPerWave * wavesSinceFirst) * playerScale),
			HunterCount = Hunters(wave, hunterStart, settings),
			MiniBossCount = Scaled(wave, settings.MiniBosses, settings.MiniBossOccurrencesPerExtra,
				settings.MaxMiniBosses),
			PainCount = Scaled(wave, settings.Pain, settings.PainOccurrencesPerExtra, settings.MaxPain),
			HeroicBossCount = content.HeroicBossTypes > 0 ? HeroicBosses(wave, settings) : 0,
			AmbientLevel = settings.StartingAmbientLevel + wavesSinceFirst / settings.WavesPerAmbientLevel,
			MaxAlive = settings.MaxAliveBase + settings.MaxAlivePerExtraPlayer * extraPlayers,
			HealthBonusPercent = settings.HealthBonusPerWave * wavesSinceFirst,
			DamageBonusPercent = settings.DamageBonusPerWave * wavesSinceFirst,
			ForeignTypesUnlocked = foreign,
			NewForeignTypes = foreign > ForeignTypesUnlocked(wave - FirstOfTier, settings, content.ForeignTypes),
			HuntLevel = HuntLevel(wave, hunterStart, settings),
			MirageSkinsUnlocked = mirage,
			NewMirageSkin = mirage > MirageSkinsUnlocked(wave - FirstOfTier, settings, content.MirageSkins),
			MirageChance = mirage > 0 ? settings.MirageChance : 0,
		};
	}

	public static int HunterStartWave(WaveSettings settings, int foreignTypes)
	{
		int unlocksNeeded = (foreignTypes + settings.ForeignTypesPerUnlock - FirstOfTier) / settings.ForeignTypesPerUnlock;
		int lastUnlock = Math.Max(0, unlocksNeeded - FirstOfTier);
		return settings.ForeignTypes.WaveOfOccurrence(lastUnlock) + settings.HunterIntervalWaves;
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

	private static int Hunters(int wave, int hunterStart, WaveSettings settings)
	{
		Cadence hunters = new Cadence(hunterStart, settings.HunterIntervalWaves);
		return Math.Min(settings.MaxHunters, hunters.OccurrencesSoFar(wave));
	}

	private static int HuntLevel(int wave, int hunterStart, WaveSettings settings)
	{
		Cadence hunted = new Cadence(hunterStart + settings.HuntedZoneIntervalWaves, settings.HuntedZoneIntervalWaves);
		return hunted.IsOn(wave) ? Math.Min(settings.MaxHuntLevel, hunted.OccurrencesSoFar(wave)) : NoHuntLevel;
	}

	private static int HeroicBosses(int wave, WaveSettings settings)
	{
		Cadence bosses = settings.HeroicBosses;
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
