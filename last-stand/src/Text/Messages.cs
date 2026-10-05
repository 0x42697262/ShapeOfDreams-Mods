using LastStand.Waves;

namespace LastStand.Text;

internal static class Messages
{
	public const string ModeName = "Last Stand";

	public const string ModeDescription =
		"Hold a boss arena against endless waves that grow stronger every wave. Minibosses " +
		"come every 10 waves, monsters from other worlds join over time, then hunters and " +
		"Mirage skins. Pain minibosses hurt everyone until they fall. Memories and essences " +
		"are scarce and fade if nobody claims them, and a Dream Well at the center turns " +
		"dream dust into upgrades. Only the host needs this mod.";

	public const string ModeTooltip = "Endless waves in a single arena";

	private const string NoticeColor = "#e8a33d";

	public static string Notice(string text)
	{
		return $"<color={NoticeColor}>[{ModeName}] {text}</color>";
	}

	public static string ArenaReady(int secondsUntilFirstWave)
	{
		return Notice($"Hold the arena. The first wave arrives in {secondsUntilFirstWave} s.");
	}

	public static string WaveStarted(WavePlan plan)
	{
		string text = $"Wave {plan.Number}: {plan.RegularCount} monsters";
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

	public static string WaveCleared(int wave, int secondsUntilNextWave)
	{
		return Notice($"Wave {wave} cleared. The next wave arrives in {secondsUntilNextWave} s.");
	}

	public static string PainFell()
	{
		return Notice("The Pain minibosses have fallen. The aura fades.");
	}
}
