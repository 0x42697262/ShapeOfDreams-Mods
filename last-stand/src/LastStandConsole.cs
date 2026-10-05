using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Mode;
using LastStand.Text;

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

	public void Cmd_TestBoss()
	{
		Cmd_TestBoss(GameNames.DefaultTestBoss);
	}

	public void Cmd_TestBoss(string typeName)
	{
		if (!LastStandMode.IsHosting())
		{
			Log.Warn(LogText.NotHosting);
			return;
		}

		LastStandMod.Current?.Session?.SpawnTestBoss(typeName);
	}

	[ConsoleCommand(CommandText.SoloDescription, CommandText.Solo)]
	public void Cmd_Solo()
	{
		ModeSelection.StartSolo();
	}
}
