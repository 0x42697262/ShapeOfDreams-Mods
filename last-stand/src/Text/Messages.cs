using System.Collections.Generic;
using LastStand.Waves;

namespace LastStand.Text;

internal static class Messages
{
	public const string ModeTooltip = "Endless waves in a single arena";

	public static string HeroicBossArrives()
	{
		return SharedMessages.Notice("A heroic boss enters the arena!");
	}

	public static string HeroesRevived(int count)
	{
		return SharedMessages.Notice(count == 1 ? "The wave is over. A fallen hero rises!" : $"The wave is over. {count} fallen heroes rise!");
	}

	public static string HeroicBossDefeated(int chaosRewards, int stardust)
	{
		return stardust > 0
			? SharedMessages.Notice($"The heroic boss falls! {chaosRewards} Corrupted Chaos rewards and {stardust} stardust dropped.")
			: SharedMessages.Notice($"The heroic boss falls! {chaosRewards} Corrupted Chaos rewards dropped.");
	}

	public static string LobbyName(int startingWorld)
	{
		return $"{ModText.ModeName} (Wave Mode) Z{startingWorld}";
	}

	public static string ArenaReady(int secondsUntilFirstWave)
	{
		return SharedMessages.Notice($"Hold the arena. The first wave arrives in {secondsUntilFirstWave} s.");
	}

	public static string WaveStarted(WavePlan plan)
	{
		string text = plan.IsSurge
			? $"Surge! Wave {plan.Number}: {plan.RegularCount} monsters"
			: $"Wave {plan.Number}: {plan.RegularCount} monsters";
		if (plan.HunterCount > 0)
		{
			text += $", {plan.HunterCount} hunter(s)";
		}

		if (plan.MiniBossCount > 0)
		{
			text += $", {plan.MiniBossCount} miniboss(es)";
		}

		if (plan.PainCount > 0)
		{
			text += $", {plan.PainCount} Pain miniboss(es)";
		}

		if (plan.HeroicBossCount > 0)
		{
			text += plan.HeroicBossWindowSeconds > 0
				? $", and {plan.HeroicBossCount} heroic boss(es) at any moment"
				: $", and then {plan.HeroicBossCount} heroic boss(es)";
		}

		text += ".";

		if (plan.IsMerged)
		{
			text += $" They fight with the strength of {plan.UnmergedMobCount}: x{plan.MergeMultiplier:0.##} health and damage each.";
		}

		text += $" Difficulty: {plan.MonsterWorld:0.0}.";

		if (plan.NewForeignTypes)
		{
			text += " Monsters from another world join the hunt.";
		}

		if (plan.NewMirageSkin)
		{
			text += " A stronger Mirage skin appears.";
		}

		if (plan.IsHunted)
		{
			text += $" The hunters bombard the arena (hunt level {plan.HuntLevel}).";
		}

		return SharedMessages.Notice(text);
	}

	public static string Milestones(List<Milestone> milestones, WaveSettings settings)
	{
		List<string> sentences = new List<string>();
		foreach (Milestone milestone in milestones)
		{
			sentences.Add(MilestoneText(milestone, settings));
		}

		return SharedMessages.Announcement(string.Join(" ", sentences));
	}

	private static string MilestoneText(Milestone milestone, WaveSettings settings)
	{
		EnvironmentRule environment = settings.Environment;
		switch (milestone)
		{
			case Milestone.Hunters:
				return $"The hunt begins! From now on, hunters join every wave and artillery rains on the arena, growing stronger every {settings.HuntLevelStepWaves} waves.";
			case Milestone.HuntedEnvironments:
				return $"Environments now come on {environment.HuntedChance * SharedTuning.PercentScale:0}% of waves, at most {environment.MaxPerBlock} in every {environment.BlockWaves}, and replace the hunter zone when they do.";
			case Milestone.FasterWorlds:
				return $"The monsters grow stronger, faster! From now on, they climb {settings.World.FasterRisePerStep:0.##} world(s) every {settings.World.FasterStepWaves} wave(s) instead of {settings.World.RisePerStep:0.##} every {settings.World.StepWaves}.";
			default:
				return $"A dark urge stirs! From now on, each co-op wave without a heroic boss has a {settings.FriendlyFireChance * SharedTuning.PercentScale:0}% chance to turn heroes against each other until it's cleared.";
		}
	}

	public static string LeftoverItemsFreed(int count)
	{
		return SharedMessages.Notice($"{count} unclaimed memory or essence drop(s) on the ground are now free for anyone.");
	}

	public static string HeroicFastKill(double projectedSeconds, double targetSeconds, double fromWorld,
		double toWorld)
	{
		return SharedMessages.Warning($"Your best burst would kill all of this wave's heroic bosses in {projectedSeconds:0.#}s, faster than {targetSeconds:0}s. Difficulty rises from {fromWorld:0.0} to {toWorld:0.0} for every monster.");
	}

	public static string ArenaReadyAwaitingCurse()
	{
		return SharedMessages.Notice("Hold the arena. Choose a curse at the Hatred Remnant to start wave 1.");
	}

	public static string WaveClearedAwaitingCurse(int wave, int nextWave)
	{
		return SharedMessages.Success($"Wave {wave} cleared. Choose a curse at the Hatred Remnant to start wave {nextWave}.");
	}

	public static string CurseStartsWave(string strength, int wave, int seconds)
	{
		return SharedMessages.Notice($"A {strength} curse falls on everyone. Wave {wave} starts in {seconds} s.");
	}

	public static string WaveCleared(int wave, int secondsUntilNextWave)
	{
		return SharedMessages.Success($"Wave {wave} cleared. The next wave arrives in {secondsUntilNextWave} s.");
	}

	public static string WaveVoteCanceled(int wave, int seconds)
	{
		return SharedMessages.Notice($"Vote canceled. Wave {wave} arrives in {seconds} s.");
	}

	public static string WaveVotePassed(int wave)
	{
		return SharedMessages.Notice($"Everyone is ready. Wave {wave} starts now.");
	}

	public static string FriendlyFireStarted(double chance)
	{
		return SharedMessages.Warning($"A dark urge takes hold ({chance * SharedTuning.PercentScale:0}% chance per wave): every hero is cursed and can hurt the others until this wave is cleared.");
	}

	public static string FriendlyFireEnded()
	{
		return SharedMessages.Notice("The dark urge fades. Heroes are allies again.");
	}

	public static string EnvironmentStarted(string name)
	{
		return SharedMessages.Notice($"The arena changes: {name}. It lasts until this wave is cleared.");
	}

	public static string EnvironmentEnded(string name)
	{
		return SharedMessages.Notice($"{name} fades.");
	}

	public static string PainFell()
	{
		return SharedMessages.Notice("The Pain minibosses have fallen. The aura fades.");
	}
}
