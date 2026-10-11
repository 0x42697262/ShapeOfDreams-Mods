using System.Collections.Generic;

namespace LastStand.Waves;

internal static class Milestones
{
	public static List<Milestone> StartingAt(int wave, WaveSettings settings)
	{
		List<Milestone> starting = new List<Milestone>();
		if (wave == settings.HunterFromWave)
		{
			starting.Add(Milestone.Hunters);
		}

		EnvironmentRule environment = settings.Environment;
		if (wave == environment.HuntedFromWave && environment.HuntedChance > 0 && environment.MaxPerBlock > 0 &&
		    settings.EnvironmentTypes.Count > 0)
		{
			starting.Add(Milestone.HuntedEnvironments);
		}

		if (wave == settings.Empowered.FirstWave && settings.EmpoweredMultiplier != 1)
		{
			starting.Add(Milestone.Empowered);
		}

		if (wave == settings.FriendlyFireFromWave && settings.FriendlyFireChance > 0)
		{
			starting.Add(Milestone.DarkUrge);
		}

		return starting;
	}
}
