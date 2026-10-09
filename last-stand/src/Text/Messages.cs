using LastStand.Waves;

namespace LastStand.Text;

internal static class Messages
{
	public const string ModeName = "Last Stand";

	public const string ModeDescription = "Hold a boss arena against endless waves that grow stronger every wave.";

	public const string ModeTooltip = "Endless waves in a single arena";

	private const string NoticeColor = "#e8a33d";

	private const string WarningColor = "#ff4d4d";

	private const string SuccessColor = "#5ad45a";

	public static string HeroicBossArrives()
	{
		return Notice("A heroic boss enters the arena!");
	}

	public static string HeroesRevived(int count)
	{
		return Notice(count == 1 ? "The wave is over. A fallen hero rises!" : $"The wave is over. {count} fallen heroes rise!");
	}

	public static string HeroicBossDefeated(int chaosRewards)
	{
		return Notice($"The heroic boss falls! {chaosRewards} Corrupted Chaos rewards dropped.");
	}

	public static string LobbyName(int startingWorld)
	{
		return $"{ModeName} (Wave Mode) Z{startingWorld}";
	}

	public static string Notice(string text)
	{
		return Colored(NoticeColor, text);
	}

	public static string Success(string text)
	{
		return Colored(SuccessColor, text);
	}

	public static string Warning(string text)
	{
		return Colored(WarningColor, text);
	}

	private static string Colored(string color, string text)
	{
		return $"<color={color}>[{ModeName}] {text}</color>";
	}

	public static string ArenaReady(int secondsUntilFirstWave)
	{
		return Notice($"Hold the arena. The first wave arrives in {secondsUntilFirstWave} s.");
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
			text += $", and then {plan.HeroicBossCount} heroic boss(es)";
		}

		text += ".";

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

		return Notice(text);
	}

	public static string LeftoverItemsFreed(int count)
	{
		return Notice($"{count} unclaimed memory or essence drop(s) on the ground are now free for anyone.");
	}

	public static string FastClear(double seconds, int nextWave, double multiplier)
	{
		return Warning($"Wave cleared within {seconds:0.#}s. From wave {nextWave} on, monsters hit back with x{multiplier:0.##} health and damage.");
	}

	public static string ArenaReadyAwaitingCurse()
	{
		return Notice("Hold the arena. Choose a curse at the Hatred Remnant to start wave 1.");
	}

	public static string WaveClearedAwaitingCurse(int wave, int nextWave)
	{
		return Success($"Wave {wave} cleared. Choose a curse at the Hatred Remnant to start wave {nextWave}.");
	}

	public static string CurseStartsWave(string strength, int wave, int seconds)
	{
		return Notice($"A {strength} curse falls on everyone. Wave {wave} starts in {seconds} s.");
	}

	public static string WaveCleared(int wave, int secondsUntilNextWave)
	{
		return Success($"Wave {wave} cleared. The next wave arrives in {secondsUntilNextWave} s.");
	}

	public static string FriendlyFireStarted()
	{
		return Notice("A dark urge takes hold: every hero is cursed until this wave is cleared.");
	}

	public static string FriendlyFireEnded()
	{
		return Notice("The dark urge fades. Heroes are allies again.");
	}

	public static string EnvironmentStarted(string name)
	{
		return Notice($"The arena changes: {name}. It lasts until this wave is cleared.");
	}

	public static string EnvironmentEnded(string name)
	{
		return Notice($"{name} fades.");
	}

	public static string PainFell()
	{
		return Notice("The Pain minibosses have fallen. The aura fades.");
	}
}
