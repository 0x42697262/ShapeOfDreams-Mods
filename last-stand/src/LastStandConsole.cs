using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Mode;
using LastStand.Text;
using LastStand.UI;
using Mirror;

namespace LastStand;

public class LastStandConsole : ModBehaviour
{
	[ConsoleCommand(CommandText.StatusDescription, CommandText.Status)]
	public void Cmd_Status()
	{
		LastStandMod mod = LastStandMod.Current;
		if (mod?.Session != null)
		{
			Log.Info(mod.Session.Describe());
			BossFinder.LogAll();
		}
	}

	[ConsoleCommand(CommandText.NextWaveDescription, CommandText.NextWave)]
	public void Cmd_NextWave()
	{
		if (!LastStandMode.IsHosting())
		{
			Log.Warn(LogText.NotHosting);
			return;
		}

		LastStandMod.Current?.Session?.StartNextWaveNow();
	}

	[ConsoleCommand(CommandText.WaveDescription, CommandText.Wave)]
	public void Cmd_Wave(int wave)
	{
		if (!LastStandMode.IsHosting())
		{
			Log.Warn(LogText.NotHosting);
			return;
		}

		LastStandMod.Current?.Session?.JumpToWave(wave);
	}

	[ConsoleCommand(CommandText.GoldDescription, CommandText.Gold)]
	public void Cmd_Gold(int amount)
	{
		if (!CheatsAllowed())
		{
			return;
		}

		LastStandMod.Current?.Session?.GiveGold(amount);
	}

	[ConsoleCommand(CommandText.TestBossDescription, CommandText.TestBoss)]
	public void Cmd_TestBoss()
	{
		Cmd_TestBoss(GameNames.DefaultTestBoss);
	}

	[ConsoleCommand(CommandText.TestBossDescription, CommandText.TestBoss)]
	public void Cmd_TestBoss(string typeName)
	{
		if (!CheatsAllowed())
		{
			return;
		}

		LastStandMod.Current?.Session?.SpawnTestBoss(typeName);
	}

	[ConsoleCommand(CommandText.SkipDescription, CommandText.Skip)]
	public void Cmd_Skip()
	{
		Cmd_Skip(Tuning.DefaultSkipWaves);
	}

	[ConsoleCommand(CommandText.SkipDescription, CommandText.Skip)]
	public void Cmd_Skip(int waves)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.SkipWaves(waves);
		}
	}

	[ConsoleCommand(CommandText.DreamDustDescription, CommandText.DreamDust)]
	public void Cmd_DreamDust(int amount)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.GiveDreamDust(amount);
		}
	}

	[ConsoleCommand(CommandText.LevelDescription, CommandText.Level)]
	public void Cmd_Level(int level)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.SetHeroLevel(level);
		}
	}

	[ConsoleCommand(CommandText.EssenceDescription, CommandText.Essence)]
	public void Cmd_Essence(string query)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.DropEssence(query, null);
		}
	}

	[ConsoleCommand(CommandText.EssenceDescription, CommandText.Essence)]
	public void Cmd_Essence(string query, int quality)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.DropEssence(query, quality);
		}
	}

	[ConsoleCommand(CommandText.MemoryDescription, CommandText.Memory)]
	public void Cmd_Memory(string query)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.DropMemory(query, null);
		}
	}

	[ConsoleCommand(CommandText.MemoryDescription, CommandText.Memory)]
	public void Cmd_Memory(string query, int level)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.DropMemory(query, level);
		}
	}

	[ConsoleCommand(CommandText.SecondWindDescription, CommandText.SecondWind)]
	public void Cmd_SecondWind(int charges)
	{
		Cmd_SecondWind(charges, Tuning.SecondWindInvulnerableSeconds);
	}

	[ConsoleCommand(CommandText.SecondWindDescription, CommandText.SecondWind)]
	public void Cmd_SecondWind(int charges, float invulnerableSeconds)
	{
		if (CheatsAllowed())
		{
			LastStandMod.Current?.Session?.GrantSecondWind(charges, invulnerableSeconds);
		}
	}

	private static bool CheatsAllowed()
	{
		if (!LastStandMode.IsHosting())
		{
			Log.Warn(LogText.NotHosting);
			return false;
		}

		if (LastStandMod.Current?.Session?.Settings?.CheatCommands != true)
		{
			Log.Warn(LogText.CheatCommandsDisabled);
			return false;
		}

		return true;
	}

	[ConsoleCommand(CommandText.ConfigDescription, CommandText.Config)]
	public void Cmd_Config()
	{
		if (NetworkClient.active && !NetworkServer.active)
		{
			Log.Warn(LogText.ConfigHostOnly);
			return;
		}

		ConfigWindow.Open();
	}

	[ConsoleCommand(CommandText.SoloDescription, CommandText.Solo)]
	public void Cmd_Solo()
	{
		ModeSelection.StartSolo();
	}
}
